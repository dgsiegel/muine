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
#include <libgnomevfs/gnome-vfs-utils.h>
#include <glib/gi18n.h>

#include "player.h"

static void player_class_init (PlayerClass     *klass);
static void player_init       (Player          *player);
static void player_finalize   (GObject         *object);
static void eos_cb            (GstElement      *sink,
		               Player          *player);
static void error_cb          (GstElement      *element,
	                       GstElement      *source,
	                       GError          *error,
                               char            *debug,
	                       Player          *player);
static void state_change_cb   (GstElement      *play,
			       GstElementState  old_state,
		               GstElementState  new_state,
			       Player          *player);
static gboolean tick_timeout  (Player          *player);

enum {
	END_OF_STREAM,
	TICK,
	ERROR,
	LAST_SIGNAL
};

struct _PlayerPriv {
	GstElement *play;

	char       *current_file;

	int	    cur_volume;
	double      volume_scale;

	guint	    eos_idle_id;
	guint       iterate_idle_id;
	guint       tick_timeout_id;

	GTimer     *timer;
	long        timer_add;
};

static GObjectClass *parent_class;
static guint signals[LAST_SIGNAL];

GType
player_get_type (void)
{
	static GType type = 0;

	if (!type) {
		static const GTypeInfo info = {
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
	object_class = (GObjectClass *) klass;

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

static void
player_init (Player *player)
{
}

static void
player_construct (Player *player, char **error)
{
	PlayerPriv *priv;
	GstElement *sink;

	gst_init (NULL, NULL);

	priv = g_new0 (PlayerPriv, 1);
	player->priv = priv;

	priv->timer = g_timer_new ();
	g_timer_stop (priv->timer);
	priv->timer_add = 0;

	priv->eos_idle_id = 0;
	priv->tick_timeout_id = g_timeout_add (200, (GSourceFunc) tick_timeout, player);

	priv->play = gst_element_factory_make ("playbin", "play");
	if (!priv->play) {
		*error = g_strdup (_("Failed to create a GStreamer play object"));

		return;
	}

	sink = gst_gconf_get_default_audio_sink ();
	if (!sink) {
		*error = g_strdup (_("Could not render default GStreamer audio output sink"));

		return;
	}

	g_object_set (G_OBJECT (priv->play), "audio-sink",
		      sink, NULL);

	g_signal_connect (priv->play,
			  "error",
			  G_CALLBACK (error_cb),
			  player);

	g_signal_connect (priv->play,
			  "eos",
			  G_CALLBACK (eos_cb),
			  player);

	g_signal_connect (priv->play,
			  "state_change",
			  G_CALLBACK (state_change_cb),
			  player);
}

static void
player_finalize (GObject *object)
{
	Player *player = PLAYER (object);

	player_stop (player);

	g_timer_destroy (player->priv->timer);

	if (player->priv->iterate_idle_id > 0)
		g_source_remove (player->priv->iterate_idle_id);
	if (player->priv->tick_timeout_id > 0)
		g_source_remove (player->priv->tick_timeout_id);

	g_object_unref (player->priv->play);

	g_free (player->priv);

	if (G_OBJECT_CLASS (parent_class)->finalize)
		(* G_OBJECT_CLASS (parent_class)->finalize) (object);
}

Player *
player_new (char **error)
{
	Player *player;

	player = g_object_new (TYPE_PLAYER, NULL);

	*error = NULL;

	player_construct (player, error);

	return player;
}

static gboolean
tick_timeout (Player *player)
{
	if (gst_element_get_state (player->priv->play) != GST_STATE_PLAYING)
		return TRUE;

	g_signal_emit (player, signals[TICK], 0, player_tell (player));

	return TRUE;
}

static gboolean
eos_idle_cb (Player *player)
{
	player->priv->timer_add += floor (g_timer_elapsed (player->priv->timer, NULL) + 0.5);
	g_timer_stop (player->priv->timer);
	g_timer_reset (player->priv->timer);

	player->priv->eos_idle_id = 0;

	g_signal_emit (player, signals[END_OF_STREAM], 0);

	return FALSE;
}

static void
eos_cb (GstElement *sink, Player *player)
{
	player->priv->eos_idle_id = g_idle_add ((GSourceFunc) eos_idle_cb, player);
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
error_cb (GstElement *element,
	  GstElement *source,
	  GError     *error,
          char       *debug,
	  Player     *player)
{
	PlayerError *data = g_new0 (PlayerError, 1);

	/* Stop playing so we don't get repeated error messages. Might lead to
	 * troubles with threads, not sure.
	 */
	player_stop (player);

	data->player = player;
	data->error = g_strdup (error->message);

	g_idle_add ((GSourceFunc) error_idle_cb, data);
}

static gboolean
iterate_cb (Player *player)
{
	gboolean res;

	if (!GST_FLAG_IS_SET (player->priv->play, GST_BIN_SELF_SCHEDULABLE)) {
		res = gst_bin_iterate (GST_BIN (player->priv->play));
	} else {
		g_usleep (100);
		res = (gst_element_get_state (player->priv->play) == GST_STATE_PLAYING);
	}

	if (!res)
		player->priv->iterate_idle_id = 0;

	return res;
}

static void
state_change_cb (GstElement *play, GstElementState old_state,
		 GstElementState new_state, Player *player)
{
	if (old_state == GST_STATE_PLAYING) {
		if (player->priv->iterate_idle_id != 0) {
			g_source_remove (player->priv->iterate_idle_id);
			player->priv->iterate_idle_id = 0;
		}
	} else if (new_state == GST_STATE_PLAYING) {
		if (player->priv->iterate_idle_id != 0)
			g_source_remove (player->priv->iterate_idle_id);
		player->priv->iterate_idle_id = g_idle_add ((GSourceFunc) iterate_cb, player);
	}
}

gboolean
player_set_file (Player     *player,
		 const char *file,
		 char      **error)
{
	char *escaped;

	g_return_val_if_fail (IS_PLAYER (player), FALSE);

	*error = NULL;

	player_stop (player);

	if (!file)
		return FALSE;

	g_timer_stop (player->priv->timer);
	g_timer_reset (player->priv->timer);
	player->priv->timer_add = 0;

	escaped = gnome_vfs_escape_path_string (file);
	// FIXME get rid of this one when the switch to gnome-vfs is made
	player->priv->current_file = g_strdup_printf ("file://%s", escaped);
	g_free (escaped);

	g_object_set (G_OBJECT (player->priv->play), "uri",
		      player->priv->current_file, NULL);

	return TRUE;
}

void
player_play (Player *player)
{
	g_return_if_fail (IS_PLAYER (player));

	gst_element_set_state (GST_ELEMENT (player->priv->play), GST_STATE_PLAYING);

	g_timer_start (player->priv->timer);
}

void
player_stop (Player *player)
{
	g_return_if_fail (IS_PLAYER (player));

	if (player->priv->eos_idle_id > 0) {
		g_source_remove (player->priv->eos_idle_id);
		player->priv->eos_idle_id = 0;
	}

	g_free (player->priv->current_file);
	player->priv->current_file = NULL;

	g_timer_stop (player->priv->timer);
	g_timer_reset (player->priv->timer);
	player->priv->timer_add = 0;

	gst_element_set_state (GST_ELEMENT (player->priv->play), GST_STATE_READY);
}

void
player_pause (Player *player)
{
	g_return_if_fail (IS_PLAYER (player));

	gst_element_set_state (GST_ELEMENT (player->priv->play), GST_STATE_PAUSED);

	player->priv->timer_add += floor (g_timer_elapsed (player->priv->timer, NULL) + 0.5);
	g_timer_stop (player->priv->timer);
	g_timer_reset (player->priv->timer);
}

static void
update_volume (Player *player)
{
	int real_vol;
	double d;

	real_vol = player->priv->cur_volume * player->priv->volume_scale;

	d = CLAMP (real_vol, 0, 100) / 100.0;

	g_object_set (G_OBJECT (player->priv->play), "volume", d, NULL);
}

void
player_set_volume (Player *player, int volume)
{
	g_return_if_fail (IS_PLAYER (player));
	g_return_if_fail (volume >= 0 && volume <= 100);

	player->priv->cur_volume = volume;

	update_volume (player);
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

	g_return_if_fail (IS_PLAYER (player));

	if (gain == 0) {
		player->priv->volume_scale = 1.0;
		update_volume (player);

		return;
	}

	scale = pow (10., gain / 20);

	/* anti clip */
	if (peak != 0 && (scale * peak) > 1)
		scale = 1.0 / peak;

	/* For security */
	if (scale > 15)
		scale = 15;

	player->priv->volume_scale = scale;
	update_volume (player);
}

void
player_seek (Player *player, int t)
{
	g_return_if_fail (IS_PLAYER (player));

	gst_element_seek (player->priv->play, GST_SEEK_METHOD_SET |
		          GST_SEEK_FLAG_FLUSH | GST_FORMAT_TIME,
		          t * GST_SECOND);

	g_timer_reset (player->priv->timer);
	player->priv->timer_add = t;
}

int
player_tell (Player *player)
{
	g_return_val_if_fail (IS_PLAYER (player), -1);

	return (int) floor (g_timer_elapsed (player->priv->timer, NULL) + 0.5) + player->priv->timer_add;
}
