/*
 * Copyright (C) 2004 Richard Hult <richard@imendio.com>
 * Copyright (C) 2003, 2004 Jorn Baayen <jorn@nl.linux.org>
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
#include <xine.h>
#include <glib/gi18n.h>
#include <libgnomevfs/gnome-vfs-utils.h>

#include "player.h"

static void     player_class_init (PlayerClass  *klass);
static void     player_init       (Player       *player);
static void     player_finalize   (GObject      *object);
static gboolean player_playing    (Player       *player);
static void     player_close      (Player       *player);
static gboolean player_open       (Player       *player,
				   const char   *uri,
				   GError      **error);

#define PLAYER_ERROR player_error_quark ()

GQuark player_error_quark (void);

enum {
  PLAYER_ERROR_NO_INPUT_PLUGIN,
  PLAYER_ERROR_NO_DEMUX_PLUGIN,
  PLAYER_ERROR_DEMUX_FAILED,
  PLAYER_ERROR_INTERNAL,
  PLAYER_ERROR_NO_AUDIO
};

enum {
  END_OF_STREAM,
  TICK,
  ERROR,
  LAST_SIGNAL
};

typedef struct {
  int signal;
} SignalData;

struct _PlayerPriv {
  char *current_file;
  
  xine_t *xine;
  xine_ao_driver_t *audio_driver;
  xine_vo_driver_t *video_driver;

  xine_stream_t *stream;
  xine_event_queue_t *event_queue;

  int cur_volume;

  GTimer *timer;
  long timer_add;

  guint tick_timeout_id;
  GAsyncQueue *queue;
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
		  g_cclosure_marshal_VOID__VOID,
		  G_TYPE_NONE, 0);
  
  signals[TICK] =
    g_signal_new ("tick",
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

static gboolean
tick_timeout (Player *player)
{
  if (!player_playing (player))
    return TRUE;

  g_signal_emit (player, signals[TICK], 0, player_tell (player));
  
  return TRUE;
}

static void
player_init (Player *player)
{
  PlayerPriv *priv;

  priv = g_new0 (PlayerPriv, 1);
  player->priv = priv;

  priv->tick_timeout_id = g_timeout_add (200, (GSourceFunc) tick_timeout, player);
}

static void
player_finalize (GObject *object)
{
  Player *player;
  PlayerPriv *priv;

  player = PLAYER (object);
  priv = player->priv;
  
  g_source_remove (priv->tick_timeout_id);
  
  if (priv->stream != NULL)
    {
      xine_stop (priv->stream);
      xine_close (priv->stream);
      xine_event_dispose_queue (priv->event_queue);
      xine_dispose (priv->stream);
    }
  
  if (priv->audio_driver != NULL)
    xine_close_audio_driver (priv->xine, priv->audio_driver);
  
  if (priv->video_driver != NULL)
    xine_close_video_driver (priv->xine, priv->video_driver);
  
  xine_exit (priv->xine);

  g_timer_destroy (priv->timer);
  g_free (priv->current_file);
  g_async_queue_unref (priv->queue);

  g_free (priv);
  
  G_OBJECT_CLASS (parent_class)->finalize (object);
}

static gboolean
signal_idle (Player *player)
{
  PlayerPriv *priv = player->priv;
  int queue_length;
  SignalData *data;

  data = g_async_queue_try_pop (priv->queue);
  if (data == NULL)
    return FALSE;

  switch (data->signal)
    {
    case END_OF_STREAM:
      g_signal_emit (player, signals[END_OF_STREAM], 0);
      break;
    }
  
  g_object_unref (player);
  g_free (data);
  queue_length = g_async_queue_length (priv->queue);
  
  return (queue_length > 0);
}

static void
xine_event (Player *player,
	    const xine_event_t *event)
{
  PlayerPriv *priv = player->priv;
  SignalData *data;
  xine_progress_data_t *prg;

  switch (event->type)
    {
    case XINE_EVENT_UI_PLAYBACK_FINISHED:
      g_object_ref (player);
      data = g_new0 (SignalData, 1);
      data->signal = END_OF_STREAM;
      g_async_queue_push (priv->queue, data);
      g_idle_add ((GSourceFunc) signal_idle, player);
      break;
    case XINE_EVENT_PROGRESS:
      prg = event->data;
      /*
	if (prg->percent == 0 || prg->percent == 100)
	{
	g_object_ref (player);
	data = g_new0 (SignalData, 1);
	data->signal = prg->percent ? BUFFERING_END : BUFFERING_BEGIN;
	g_idle_add ((GSourceFunc) signal_idle, player);
	break;
	}
      */
    }
}

static void
player_construct (Player *player,
		  GError **error)
{
  PlayerPriv *priv = player->priv;
  const char *audio_driver;
  char *configfile;

  priv->xine = xine_new ();

  configfile = g_build_filename (g_get_home_dir (),
				 ".gnome2", "muine",
				 "xine-config",
				 NULL);
  xine_config_load (priv->xine, configfile);

  xine_init (priv->xine);

  audio_driver = "auto";

  if (strcmp (audio_driver, "null") == 0)
    priv->audio_driver = NULL;
  else
    {
      if (strcmp (audio_driver, "auto") != 0) {
	/* first try the requested driver */
	priv->audio_driver = xine_open_audio_driver (priv->xine,
						     audio_driver, NULL);
      }
      
      /* autoprobe */
      if (priv->audio_driver == NULL)
	priv->audio_driver = xine_open_audio_driver (priv->xine, NULL, NULL);
    }
  
  if (priv->audio_driver == NULL)
    g_set_error (error,
		 PLAYER_ERROR,
		 PLAYER_ERROR_NO_AUDIO,
		 _("Failed to set up an audio driver; check your installation"));
  
  priv->video_driver = NULL;

  priv->stream = xine_stream_new (priv->xine,
				  priv->audio_driver,
				  priv->video_driver);
  priv->event_queue = xine_event_new_queue (priv->stream);
  priv->queue = g_async_queue_new ();

  xine_event_create_listener_thread (priv->event_queue,
				     (xine_event_listener_cb_t) xine_event, player);

  xine_config_register_range (priv->xine,
			      "misc.amp_level",
			      50, 0, 100, "amp volume level",
			      NULL, 10, NULL, NULL);
  priv->cur_volume = -1;

  priv->timer = g_timer_new ();
  g_timer_stop (priv->timer);
  g_timer_reset (priv->timer);
  priv->timer_add = 0;

  xine_config_save (priv->xine, configfile);
  g_free (configfile);
}

Player *
player_new (void)
{
  Player *player;

  player = g_object_new (TYPE_PLAYER, NULL);
  player_construct (player, NULL);

  return player;
}

GQuark
player_error_quark (void)
{
  static GQuark quark = 0;

  if (!quark)
    quark = g_quark_from_static_string ("player_error");

  return quark;
}

static gboolean
player_open (Player *player,
	     const char *uri,
	     GError **error)
{
  PlayerPriv *priv = player->priv;
  int xine_error;
  char *unesc;

  g_return_val_if_fail (IS_PLAYER (player), FALSE);

  player_close (player);

  if (uri == NULL)
    return FALSE;

  if (!xine_open (priv->stream, uri))
    xine_error = xine_get_error (priv->stream);
  else
    xine_error = XINE_ERROR_NONE;

  if (xine_error != XINE_ERROR_NONE)
    {
      switch (xine_error)
	{
	case XINE_ERROR_NO_INPUT_PLUGIN:
	  unesc = gnome_vfs_unescape_string_for_display (uri);
	  g_set_error (error,
		       PLAYER_ERROR,
		       PLAYER_ERROR_NO_INPUT_PLUGIN,
		       _("No plugin available for \"%s\", check your installation."),
		       unesc);
	  g_free (unesc);
	  break;
	case XINE_ERROR_NO_DEMUX_PLUGIN:
	  unesc = gnome_vfs_unescape_string_for_display (uri);
	  g_set_error (error,
		       PLAYER_ERROR,
		       PLAYER_ERROR_NO_DEMUX_PLUGIN,
		       _("No plugin available for \"%s\", check your installation."),
		       unesc);
	  g_free (unesc);
	  break;
	case XINE_ERROR_DEMUX_FAILED:
	  unesc = gnome_vfs_unescape_string_for_display (uri);
	  g_set_error (error,
		       PLAYER_ERROR,
		       PLAYER_ERROR_DEMUX_FAILED,
		       _("Failed playing \"%s\", check your installation."),
		       unesc);
	  g_free (unesc);
	  break;
	default:
	  g_set_error (error,
		       PLAYER_ERROR,
		       PLAYER_ERROR_INTERNAL,
		       _("Internal error, check your installation."));
	  break;
	}

      return FALSE;
    }
  else if (!xine_get_stream_info (priv->stream, XINE_STREAM_INFO_AUDIO_HANDLED))
    {
      unesc = gnome_vfs_unescape_string_for_display (uri);
      g_set_error (error,
		   PLAYER_ERROR,
		   PLAYER_ERROR_NO_AUDIO,
		   _("Could not play \"%s\", check your installation."),
		   unesc);
      g_free (unesc);
      return FALSE;
    }
  else
    {
      g_timer_stop (priv->timer);
      g_timer_reset (priv->timer);
      priv->timer_add = 0;
    }
  
  g_free (priv->current_file);
  priv->current_file = g_strdup (uri);
  return TRUE;
}

static void
player_close (Player *player)
{
  PlayerPriv *priv = player->priv;
    
  g_return_if_fail (IS_PLAYER (player));

  if (priv->stream != NULL)
    {
      xine_stop (priv->stream);
      xine_close (priv->stream);
    }
  
  g_free (priv->current_file);
  priv->current_file = NULL;
}

static gboolean
player_playing (Player *player)
{
  PlayerPriv *priv = player->priv;

  g_return_val_if_fail (IS_PLAYER (player), FALSE);

  if (priv->stream == NULL)
    return FALSE;

  return (xine_get_status (priv->stream) == XINE_STATUS_PLAY &&
	  xine_get_param (priv->stream, XINE_PARAM_SPEED) == XINE_SPEED_NORMAL);
}

gboolean
player_set_file (Player *player,
		 const char *file)
{
  PlayerPriv *priv;
  gboolean start;

  g_return_val_if_fail (IS_PLAYER (player), FALSE);

  priv = player->priv;

  start = player_playing (player);
 
  if (!player_open (player, file, NULL))
    return FALSE;

  if (start)
    player_play (player);
  
  return TRUE;
}

void
player_play (Player *player)
{
  PlayerPriv *priv = player->priv;
  int speed, status;

  g_return_if_fail (IS_PLAYER (player));

  if (priv->stream == NULL)
    return;

  speed = xine_get_param (priv->stream, XINE_PARAM_SPEED);
  status = xine_get_status (priv->stream);

  if (speed != XINE_SPEED_NORMAL && status == XINE_STATUS_PLAY)
    xine_set_param (priv->stream, XINE_PARAM_SPEED, XINE_SPEED_NORMAL);
  else
    xine_play (priv->stream, 0, 0);

  g_timer_start (priv->timer);
}

void
player_stop (Player *player)
{
  PlayerPriv *priv = player->priv;
    
  g_return_if_fail (IS_PLAYER (player));

  g_free (priv->current_file);
  priv->current_file = NULL;

  if (priv->stream != NULL)
      xine_stop (priv->stream);
}

void
player_pause (Player *player)
{
  PlayerPriv *priv = player->priv;

  g_return_if_fail (IS_PLAYER (player));

  if (priv->stream != NULL)
    {
      xine_set_param (priv->stream, XINE_PARAM_SPEED, XINE_SPEED_PAUSE);
      
      /* Close the audio device when on pause */
      xine_set_param (priv->stream, XINE_PARAM_AUDIO_CLOSE_DEVICE, 1);
    }

  priv->timer_add += floor (g_timer_elapsed (priv->timer, NULL) + 0.5);
  g_timer_stop (priv->timer);
  g_timer_reset (priv->timer);
}

void
player_set_volume (Player *player,
		   int     volume)
{
  PlayerPriv *priv = player->priv;

  g_return_if_fail (IS_PLAYER (player));
  g_return_if_fail (volume >= 0 && volume <= 100);

  if (priv->stream != NULL)
    {
      xine_set_param (priv->stream, XINE_PARAM_AUDIO_AMP_LEVEL, volume);
    }
  
  priv->cur_volume = volume;
}

int
player_get_volume (Player *player)
{
  g_return_val_if_fail (IS_PLAYER (player), -1);

  return player->priv->cur_volume;
}

void
player_set_replaygain (Player *player, double gain, double peak)
{
  double scale;
  int real_vol;

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

  real_vol = player->priv->cur_volume * scale;

  xine_set_param (player->priv->stream, XINE_PARAM_AUDIO_AMP_LEVEL, CLAMP (real_vol, 0, 100));
}

void
player_seek (Player *player, int t)
{
  PlayerPriv *priv;
  
  g_return_if_fail (IS_PLAYER (player));
  g_return_if_fail (t >= 0);

  priv = player->priv;
  
  if (priv->stream != NULL)
    {
      xine_play (priv->stream, 0, t * 1000);
      
      g_timer_reset (priv->timer);
      priv->timer_add = t;
    }
}

int
player_tell (Player *player)
{
  PlayerPriv *priv;
  
  g_return_val_if_fail (IS_PLAYER (player), -1);

  priv = player->priv;
  
  if (priv->stream != NULL)
    return (int) floor (g_timer_elapsed (priv->timer, NULL) + 0.5) + priv->timer_add;
  else
    return -1;
}
