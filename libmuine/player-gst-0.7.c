/* -*- mode: C; c-file-style: "gnu" -*- */
/*
 * Copyright (C) 2003 Richard Hult <richard@imendio.com>
 * Copyright (C) 2003 Johan Dahlin <johan@gnome.org>
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
#include <gst/control/dparam_smooth.h>
#include <gst/control/control.h>
#include "player.h"

#define TICK_TIMEOUT 250

static void        player_class_init   (PlayerClass *klass);
static void        player_init         (Player      *player);
static void        player_finalize     (GObject     *object);
static void        player_update_state (Player      *player);
static gboolean    player_setup        (Player      *player);
static gboolean    tick_timeout_cb     (Player      *player);
static GstElement *create_sink         (void);
static void        eos_cb              (GstElement  *sink,
					Player      *player);
static void        error_cb            (GObject     *object,
					GstObject   *origin,
					char        *error,
					Player      *player);



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

  gint           volume_level;
  gboolean       mute;
  GstDParam     *volume_dparam;

  gboolean       playing;
  char          *current_file;
  
  guint          tick_timeout_id;
  guint64        pause_offset;

  gboolean       has_error;
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

  gst_init (0, NULL);
  
  priv = g_new0 (PlayerPriv, 1);
  player->priv = priv;

  priv->tick_timeout_id = g_timeout_add (TICK_TIMEOUT,
					 (GSourceFunc) tick_timeout_cb,
					 player);

  priv->volume_dparam = gst_dpsmooth_new (G_TYPE_FLOAT);
}

static void
player_finalize (GObject *object)
{
  Player *player = PLAYER (object);

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

  /* Just to keep from crashing. */
  factory = gst_element_factory_find ("fakesink");
  if (factory)
    {
      element = gst_element_factory_create (factory, "sink");
      if (element)
	{
	  g_warning ("Could only create fake sink.");
	  return element;
	}
    }
  
  g_error ("Could not create a sink.");
  
  return NULL;
}

PlayerState
player_get_state (Player *player)
{
  PlayerPriv *priv;

  g_return_val_if_fail (IS_PLAYER (player), PLAYER_STATE_STOPPED);

  priv = player->priv;

  if (!priv->thread)
    return PLAYER_STATE_STOPPED;
  
  if (priv->playing)
    return PLAYER_STATE_PLAYING;

  return PLAYER_STATE_PAUSED;
}

static gboolean
eos_idle_cb (Player *player)
{
  PlayerPriv *priv = player->priv;
  
  if (!priv->has_error)
    g_signal_emit (player, signals[END_OF_STREAM], 0,
		   priv->current_file, NULL);
  else
    player_stop (player);
  
  return FALSE;
}

static void
eos_cb (GstElement *element, Player *player)
{
  gst_element_set_state (player->priv->sink, GST_STATE_NULL);
  
  g_idle_add ((GSourceFunc) eos_idle_cb, player);
}

typedef struct {
  Player *player;
  char *error;
} PlayerError;

static gboolean
error_idle_cb (PlayerError *data)
{
  Player *player = data->player;
  
  player_stop (player);

  g_signal_emit (player, signals[ERROR], 0, data->error);
  
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

  player->priv->has_error = TRUE;
  
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

  if (!priv->playing)
    return TRUE;

  clock = gst_bin_get_clock (GST_BIN (priv->thread));
  secs = gst_clock_get_time (clock) / GST_SECOND;
  
  g_signal_emit (player, signals[TICK], 0, secs);

  return TRUE;
}

gboolean
player_set_file (Player *player, const char *file)
{
  gboolean was_playing;
  
  PlayerPriv *priv;

  g_return_val_if_fail (IS_PLAYER (player), FALSE);

  priv = player->priv;

  was_playing = priv->playing;
  
  if (!file)
    {
      player_stop (player);
      return TRUE;
    }

  g_free (priv->current_file);
  priv->current_file = g_strdup (file);
  player_setup (player);

  priv->playing = was_playing;
  player_update_state (player);
  
  return TRUE;
}

gboolean
player_play (Player *player)
{
  PlayerPriv *priv;

  g_return_val_if_fail (IS_PLAYER (player), FALSE);

  priv = player->priv;

  if (!priv->thread)
    player_setup (player);

  priv->playing = TRUE;

  player_update_state (player);
  
  return TRUE;
}

void
player_play_file (Player *player,
		  const char *file)
{
  PlayerPriv *priv;
  
  g_return_if_fail (IS_PLAYER (player));
  g_return_if_fail (file != NULL);  

  priv = player->priv;
 
  g_free (priv->current_file);
  priv->current_file = g_strdup (file);

  player_setup (player);

  priv->playing = TRUE;
  player_update_state (player);
}

void
player_stop (Player *player)
{
  PlayerPriv *priv;
  gboolean emit;

  g_return_if_fail (IS_PLAYER (player));

  priv = player->priv;

  emit = priv->playing;
  
  priv->playing = FALSE;

  if (priv->thread)
    {
      gst_element_set_state (priv->thread, GST_STATE_NULL);

      gst_object_unref (GST_OBJECT (priv->thread));
      priv->thread = NULL;

      /* If we had a thread, we're paused. */
      emit = TRUE;
    }

  priv->source = NULL;
  priv->sink = NULL;
  priv->volume = NULL;
  
  if (emit)
    g_signal_emit (player, signals[STATE_CHANGED], 0, PLAYER_STATE_STOPPED);
}

void
player_pause (Player *player)
{
  g_return_if_fail (IS_PLAYER (player));

  player->priv->playing = FALSE;
  player_update_state (player);
}

void
player_set_volume (Player *player, int volume)
{
  float f;

  g_return_if_fail (IS_PLAYER (player));
  g_return_if_fail (volume >= 0 && volume <= 100);

  player->priv->volume_level = volume;
  
  f = volume / 100.0;

  if (player->priv->mute)
    f *= 0.25; 
  
  if (player->priv->volume_dparam)
    g_object_set (player->priv->volume_dparam, "value_float", f, NULL);
}

int
player_get_volume (Player *player)
{
  g_return_val_if_fail (IS_PLAYER (player), 0);

  return player->priv->volume_level;
}

void
player_toggle_mute (Player *player)
{
  g_return_if_fail (IS_PLAYER (player));

  player->priv->mute = !player->priv->mute;

  player_set_volume (player, player_get_volume (player));
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

  event = gst_event_new_seek (GST_FORMAT_TIME | GST_SEEK_METHOD_SET | GST_SEEK_FLAG_FLUSH, t);
  if (gst_element_send_event (priv->sink, event))
    {
      clock = gst_bin_get_clock (GST_BIN (priv->thread));
      
      t = gst_clock_get_time (clock);
      secs = gst_clock_get_time (clock) / GST_SECOND;
  
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

  clock = gst_bin_get_clock (GST_BIN (priv->thread));
 
  return gst_clock_get_time (clock) / GST_MSECOND;
}

gboolean
player_is_playing (Player *player,
		   const char *file)
{
  g_return_val_if_fail (IS_PLAYER (player), FALSE);
  g_return_val_if_fail (file != NULL, FALSE);

  return player->priv->playing && !strcmp (file, player->priv->current_file);
}

const char *
player_get_file (Player *player)
{
  g_return_val_if_fail (IS_PLAYER (player), NULL);

  return player->priv->current_file;
}

static void
player_update_state (Player *player)
{
  PlayerPriv *priv = player->priv;

  if (priv->playing)
    gst_element_set_state (priv->thread, GST_STATE_PLAYING);
  else
    {
      gst_element_set_state (priv->thread, GST_STATE_PAUSED);
      gst_element_set_state (priv->sink, GST_STATE_NULL);
    }
}

static gboolean
player_setup (Player *player)
{
  PlayerPriv *priv = player->priv;
  GstDParamManager *dpman;

  priv->has_error = FALSE;

  if (priv->thread)
    player_stop (player);
  
  priv->thread = gst_element_factory_make ("thread", "thread");
  g_signal_connect (priv->thread,
		    "error",
		    G_CALLBACK (error_cb),
		    player);

  priv->source = gst_element_factory_make ("gnomevfssrc", "src");
  if (!priv->source)
    {
      g_warning ("Couldn't create source");
      goto bail;
    }

  /* FIXME: get the file type some other way... Store it in the db perhaps as a
   * mime type. I don't believe in spider anyway.
   */
  if (priv->current_file)
    {
      if (g_str_has_suffix (priv->current_file, ".mp3"))
	priv->decoder = gst_element_factory_make ("mad", "decoder");
      else if (g_str_has_suffix (priv->current_file, ".ogg"))
	priv->decoder = gst_element_factory_make ("vorbisfile", "decoder");
      else
	{
	  g_warning ("Unknown format");
	  goto bail;
	}
    }

  if (!priv->decoder)
    {
      g_warning ("Couldn't create decoder");
      goto bail;
    }

  priv->volume = gst_element_factory_make ("volume", "volume");
  if (!priv->volume)
    {
      g_warning ("Couldn't create volume");
      goto bail;
    }
  
  dpman = gst_dpman_get_manager (priv->volume);
  gst_dpman_set_mode (dpman, "synchronous");
  
  gst_dpman_attach_dparam (dpman, "volume", priv->volume_dparam);
  
  player_set_volume (player, priv->volume_level);

  g_object_set (priv->volume,
		"mute", FALSE,
		NULL);
  
  priv->sink = create_sink ();
  if (!priv->sink)
    {
      g_warning ("Couldn't create sink");
      goto bail;
    }

  gst_bin_add_many (GST_BIN (priv->thread),
		    priv->source, priv->decoder, priv->volume, priv->sink, NULL);

  gst_element_link_many (priv->source, priv->decoder, priv->volume, priv->sink, NULL);
  
  g_signal_connect (priv->sink,
		    "eos",
		    G_CALLBACK (eos_cb),
		    player);

  if (priv->current_file)
    g_object_set (priv->source,
		  "location", priv->current_file,
		  NULL);

  player_update_state (player);
  
  return TRUE;

 bail:

  player_stop (player);

  return FALSE;
}
