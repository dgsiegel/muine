/* -*- mode: C; c-file-style: "gnu" -*- */
/*
 * Copyright (C) 2003 Richard Hult <richard@imendio.com>
 * Copyright (C) 2003 Johan Dahlin <jdahlin@gnome.org>
 * Copyright (C) 2004 Jorn Baayen <jorn@nl.linux.org>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 2 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public
 * License along with this program; if not, write to the
 * Free Software Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */

#include <config.h>
#include <string.h>
#include <math.h>
#include <gst/gst.h>
#include <gst/gconf/gconf.h>
#include "player.h"

#define TICK_TIMEOUT 250

static void        player_class_init (PlayerClass     *klass);
static void        player_init       (Player          *player);
static void        player_finalize   (GObject         *object);
static GstElement *create_source     (void);
static GstElement *create_sink       (void);
static void        eos_cb            (GstElement      *sink,
				      Player          *player);
static void        error_cb          (GObject         *object,
				      GstObject       *origin,
				      char            *error,
				      Player          *player);
static void        state_change_cb   (GstElement      *element,
				      GstElementState  old,
				      GstElementState  state,
				      Player          *player);


enum {
  END_OF_STREAM,
  TICK,
  STATE_CHANGED,
  ERROR,
  LAST_SIGNAL
};

struct _PlayerPriv {
  GstElement    *thread;
  GstElement    *source;
  GstElement    *decoder;
  GstElement    *volume;
  GstElement    *sink;

  char          *current_file;
  
  guint          tick_timeout_id;
  guint64        pause_offset;

  int		 cur_volume;
};

static GObjectClass *parent_class;
static guint signals[LAST_SIGNAL];

GType
player_get_type (void)
{
  static GType type = 0;
	
  if (!type)
    {
      static const GTypeInfo info =
	{
	  sizeof (PlayerClass),
	  NULL,           /* base_init */
	  NULL,           /* base_finalize */
	  (GClassInitFunc) player_class_init,
	  NULL,           /* class_finalize */
	  NULL,           /* class_data */
	  sizeof (Player),
	  0,
	  (GInstanceInitFunc) player_init,
	};
      
      type = g_type_register_static (G_TYPE_OBJECT,
				     "Player",
				     &info, 0);
    }

  return type;
}

static void
player_class_init (PlayerClass *klass)
{
  GObjectClass *object_class;

  parent_class = g_type_class_peek_parent (klass);
  object_class = (GObjectClass*) klass;

  object_class->finalize = player_finalize;

  signals[END_OF_STREAM] =
    g_signal_new ("end_of_stream",
		  G_TYPE_FROM_CLASS (klass),
		  G_SIGNAL_RUN_LAST,
		  0,
		  NULL, NULL,
		  g_cclosure_marshal_VOID__STRING,
		  G_TYPE_NONE, 1, G_TYPE_STRING);

  signals[TICK] =
    g_signal_new ("tick",
		  G_TYPE_FROM_CLASS (klass),
		  G_SIGNAL_RUN_LAST,
		  0,
		  NULL, NULL,
		  g_cclosure_marshal_VOID__LONG,
		  G_TYPE_NONE, 1, G_TYPE_LONG);

  signals[STATE_CHANGED] =
    g_signal_new ("state_changed",
		  G_TYPE_FROM_CLASS (klass),
		  G_SIGNAL_RUN_LAST,
		  0,
		  NULL, NULL,
		  g_cclosure_marshal_VOID__INT,
		  G_TYPE_NONE, 1, G_TYPE_INT);
  
  signals[ERROR] =
    g_signal_new ("error",
		  G_TYPE_FROM_CLASS (klass),
		  G_SIGNAL_RUN_LAST,
		  0,
		  NULL, NULL,
		  g_cclosure_marshal_VOID__STRING,
		  G_TYPE_NONE, 1, G_TYPE_STRING);
}

static void
player_init (Player *player)
{
  PlayerPriv *priv;
  GstElementFactory *factory;

  gst_init (NULL, NULL);
  
  priv = g_new0 (PlayerPriv, 1);
  player->priv = priv;

  priv->thread = gst_thread_new ("thread");
      
  factory = gst_element_factory_find ("volume");
  if (!factory)
    g_error ("Could not create volume element.");
  
  priv->volume = gst_element_factory_create (factory, "volume_float");
  
  priv->source = create_source ();
  priv->sink = create_sink ();
  
  gst_element_link (priv->volume, priv->sink);
  
  gst_bin_add_many (GST_BIN (priv->thread),
		    priv->source,
		    priv->volume,
		    priv->sink,
		    NULL);
  
  g_signal_connect (priv->thread,
		    "error",
		    G_CALLBACK (error_cb),
		    player);

  g_signal_connect (priv->sink,
		    "eos",
		    G_CALLBACK (eos_cb),
		    player);

  g_signal_connect (priv->thread,
		    "state_change",
		    G_CALLBACK (state_change_cb),
		    player);

}

static void
player_finalize (GObject *object)
{
  Player *player = PLAYER (object);

  player_stop (player);

  if (player->priv->tick_timeout_id)
    g_source_remove (player->priv->tick_timeout_id);

  g_free (player->priv);
  
  if (G_OBJECT_CLASS (parent_class)->finalize)
    (* G_OBJECT_CLASS (parent_class)->finalize) (object);
}

Player *
player_new (void)
{
  return g_object_new (TYPE_PLAYER, NULL);
}   

static GstElement*
create_source (void)
{
  GstElementFactory *factory;
  
  factory = gst_element_factory_find ("filesrc");
  if (!factory)
    g_error ("The 'filesrc' element is required.");
  
  return gst_element_factory_create (factory, "source");
}

static GstElement*
create_sink (void)
{
  GstElement *element;
  GstElementFactory *factory;
  
  /* First, try gconf. Check so default/audiosink isn't NULL before trying, to
   * avoid a warning if it's not found.
   */
  if (gst_gconf_get_string ("default/audiosink"))
    {
      element = gst_gconf_get_default_audio_sink ();
      if (element)
	return element;
    }
  
  /* Then esdsink. */      
  factory = gst_element_factory_find ("esdsink"); 
  if (factory)
    {
      element = gst_element_factory_create (factory, "sink");
      if (element)
	return element;
    }
  
  /* Then osssink. */
  factory = gst_element_factory_find ("osssink");
  if (factory)
    {
      element = gst_element_factory_create (factory, "sink");
      if (element)
	return element;
    }

  /* Finally alsasink. */
  factory = gst_element_factory_find ("alsasink");
  if (factory)
    {
      element = gst_element_factory_create (factory, "sink");
      if (element)
	return element;
    }

  g_error ("Could not create a sink.");
  
  return NULL;
}

static GstElement *
create_decoder (Player * player, const char *filename, const char *mime_type)
{
  PlayerPriv *priv = player->priv;
  
  GstElement *decoder = NULL;

  if (!strcmp (mime_type, "audio/x-mp3") ||
      !strcmp (mime_type, "audio/mpeg"))
    {
      decoder = gst_element_factory_make ("mad", "decoder");
      if (!decoder)
	gst_element_error (priv->thread, "Cannot load mp3 decoder");
    }
  else if (!strcmp (mime_type, "application/x-ogg") ||
	   !strcmp (mime_type, "application/ogg"))
    {
      decoder = gst_element_factory_make ("vorbisfile", "decoder");
      if (!decoder)
	gst_element_error (priv->thread, "Cannot load ogg decoder");
    }
  else
    gst_element_error (priv->thread,
		       "Don't recognize the file type '%s' of '%s'.", mime_type, filename);
  
  return decoder;
}

static PlayerState
gst_state_to_player_state (GstElementState state)
{
  switch (state)
    {
    case GST_STATE_PLAYING:
      return PLAYER_STATE_PLAYING;

    case GST_STATE_PAUSED:
      return PLAYER_STATE_PAUSED;

    case GST_STATE_NULL:
      return PLAYER_STATE_STOPPED;
      
    default:
      return PLAYER_STATE_STOPPED;
    }
}

PlayerState
player_get_state (Player *player)
{
  PlayerPriv *priv;

  g_return_val_if_fail (IS_PLAYER (player), PLAYER_STATE_STOPPED);

  priv = player->priv;

  if (priv->pause_offset > 0)
    return PLAYER_STATE_PAUSED;

  return gst_state_to_player_state (gst_element_get_state (priv->thread));
}

static gboolean
eos_idle_cb (Player *player)
{
  g_signal_emit (player, signals[END_OF_STREAM], 0,
		 player->priv->current_file, NULL);
  
  return FALSE;
}

static void
eos_cb (GstElement *sink, Player *player)
{
  g_idle_add ((GSourceFunc) eos_idle_cb, player);
}

typedef struct {
  Player *player;
  char *error;
} PlayerError;

static gboolean
error_idle_cb (PlayerError *data)
{
  g_signal_emit (data->player, signals[ERROR], 0, data->error);

  g_free (data->error);
  g_free (data);
  
  return FALSE;
}

static void
error_cb (GObject   *object,
	  GstObject *origin,
	  char      *error,
	  Player    *player)
{
  PlayerError *data = g_new0 (PlayerError, 1);

  /* Stop playing so we don't get repeated error messages. Might lead to
   * troubles with threads, not sure.
   */
  player_stop (player);

  data->player = player;
  data->error = g_strdup (error);
  
  g_idle_add ((GSourceFunc)error_idle_cb, data);
}

static gboolean 
tick_timeout_cb (Player *player)
{
  PlayerPriv *priv;
  GstClock *clock;
  long secs;

  priv = player->priv;

  if (gst_element_get_state (priv->thread) != GST_STATE_PLAYING)
    {
      priv->tick_timeout_id = 0;
      return FALSE;
    }
  
  clock = gst_bin_get_clock (GST_BIN (priv->thread));
  secs = gst_clock_get_time (clock) / GST_MSECOND;
  
  g_signal_emit (player, signals[TICK], 0, secs);

  return TRUE;
}

static void
state_change_cb (GstElement      *element,
		 GstElementState  old,
		 GstElementState  state,
		 Player          *player)
{
  GstElementState old_player_state;
  GstElementState new_player_state;

  if (state == GST_STATE_PLAYING && !player->priv->tick_timeout_id)
    player->priv->tick_timeout_id = g_timeout_add (TICK_TIMEOUT,
						   (GSourceFunc) tick_timeout_cb,
						   player);

  
  new_player_state = gst_state_to_player_state (state);
  old_player_state = gst_state_to_player_state (old);

  if (state == GST_STATE_PAUSED)
    gst_element_set_state (player->priv->sink, GST_STATE_NULL);
  
  if (new_player_state != old_player_state)
    g_signal_emit (player, signals[STATE_CHANGED], 0, (int) new_player_state);
}

static gboolean
player_setup_decoder (Player *player, const char *filename, const char *mime_type)
{
  GstElement *decoder;
  PlayerPriv *priv;

  priv = player->priv;

  decoder = create_decoder (player, filename, mime_type);
  if (!decoder)
    return FALSE;
      
  g_object_set (priv->source, "location", filename, NULL);
  
  if (priv->decoder)
    {
      gst_element_unlink_many (priv->source, priv->decoder, priv->volume, NULL);
      gst_bin_remove (GST_BIN (priv->thread), priv->decoder);
    }
  
  priv->decoder = decoder;
  
  gst_bin_add (GST_BIN (priv->thread), priv->decoder);
  gst_element_link_many (priv->source, priv->decoder, priv->volume, NULL);

  return TRUE;
}

gboolean
player_set_file (Player *player,
		 const char *file,
		 const char *mime_type)
{
  PlayerPriv *priv;
  GstElementState new_state;

  g_return_val_if_fail (IS_PLAYER (player), FALSE);

  priv = player->priv;

  if (!file)
    {
      player_stop (player);
      /* Should we return TRUE or FALSE here?
       * Need to think about the semantics
       */
      return FALSE;
    }

  switch (player_get_state (player))
    {
    case PLAYER_STATE_PLAYING:
      new_state = GST_STATE_PLAYING;
      player_stop (player);
      break;

    case PLAYER_STATE_STOPPED:
    case PLAYER_STATE_PAUSED:
    default:
      new_state = GST_STATE_NULL;
      break;
    }
  
  g_free (priv->current_file);
  priv->current_file = g_strdup (file);
  
  if (!player_setup_decoder (player, file, mime_type))
    {
      return FALSE;
    }
  
  gst_element_set_state (priv->thread, new_state);
  return TRUE;
}

gboolean
player_play (Player *player)
{
  PlayerPriv *priv;

  g_return_val_if_fail (IS_PLAYER (player), FALSE);

  priv = player->priv;

  gst_element_set_state (priv->thread, GST_STATE_PLAYING);

  if (!priv->tick_timeout_id)
    priv->tick_timeout_id = g_timeout_add (TICK_TIMEOUT,
					   (GSourceFunc) tick_timeout_cb,
					   player);
  
  return TRUE;
}

void
player_stop (Player *player)
{
  PlayerPriv *priv;

  priv = player->priv;

  g_free (priv->current_file);
  priv->current_file = NULL;
  
  gst_element_set_state (priv->thread, GST_STATE_NULL);

  if (priv->tick_timeout_id) 
    g_source_remove (priv->tick_timeout_id);
  
  priv->tick_timeout_id = 0;
}

void
player_pause (Player *player)
{
  PlayerPriv *priv;

  g_return_if_fail (IS_PLAYER (player));

  priv = player->priv;
  
  gst_element_set_state (priv->thread, GST_STATE_PAUSED);

  if (priv->tick_timeout_id) 
    g_source_remove (priv->tick_timeout_id);
  
  priv->tick_timeout_id = 0;
}

void
player_set_volume (Player *player, int volume)
{
  float f;

  g_return_if_fail (IS_PLAYER (player));
  g_return_if_fail (volume >= 0 && volume <= 100);

  player->priv->cur_volume = volume;

  f = volume / 100.0;

  g_object_set (player->priv->volume, "volume", f, NULL);
}

int
player_get_volume (Player *player)
{
  g_return_val_if_fail (IS_PLAYER (player), 0);

  return player->priv->cur_volume;
}

void
player_set_replaygain (Player *player, double gain, double peak)
{
  double scale;

  g_return_if_fail (IS_PLAYER (player));

  if (gain == 0)
    return;

  scale = pow (10., gain / 20);

  /* anti clip */
  if (peak != 0 && (scale * peak) > 1)
    scale = 1.0 / peak;

  /* For security */
  if (scale > 15)
    scale = 15;

  g_object_set (player->priv->volume, "volume",
		(player->priv->cur_volume / 100) * scale, NULL);
}

void
player_seek (Player *player, guint64 t)
{
  PlayerPriv *priv;
  GstElementState state;
  GstEvent *event;
  GstClock *clock;
  long secs;
  
  g_return_if_fail (IS_PLAYER (player));

  priv = player->priv;

  if (!priv->sink)
    return;
  
  state = gst_element_get_state (priv->thread);

  gst_element_set_state (priv->thread, GST_STATE_PAUSED);

  event = gst_event_new_seek (GST_FORMAT_TIME | GST_SEEK_METHOD_SET | GST_SEEK_FLAG_FLUSH, t * GST_MSECOND);
  if (gst_element_send_event (priv->sink, event))
    {
      clock = gst_bin_get_clock (GST_BIN (priv->thread));
      
      t = gst_clock_get_time (clock);
      secs = gst_clock_get_time (clock) / GST_MSECOND;
  
      g_signal_emit (player, signals [TICK], 0, secs);
    }
  
  gst_element_set_state (priv->thread, state);
}

guint64 
player_tell (Player *player)
{
  PlayerPriv *priv;
  GstClock *clock;

  g_return_val_if_fail (IS_PLAYER (player), 0);

  priv = player->priv;

  if (!priv->sink)
    return 0;

  if (gst_element_get_state (priv->thread) == GST_STATE_NULL)
    return 0;

  clock = gst_bin_get_clock (GST_BIN (priv->thread));
 
  return gst_clock_get_time (clock) / GST_MSECOND;
}

gboolean
player_is_playing (Player *player,
		   const char *file)
{
  PlayerState state;

  g_return_val_if_fail (IS_PLAYER (player), FALSE);
  g_return_val_if_fail (file != NULL, FALSE);

  state = player_get_state (player);

  if (!strcmp (file, player->priv->current_file) && state == PLAYER_STATE_PLAYING)
    return TRUE;
  
  return FALSE;
}

const char *
player_get_file (Player *player)
{
  g_return_val_if_fail (IS_PLAYER (player), NULL);

  return player->priv->current_file;
}
