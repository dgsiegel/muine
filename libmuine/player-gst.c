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
#include <gst/play/play.h>

#include "player.h"

static void player_class_init (PlayerClass     *klass);
static void player_init       (Player          *player);
static void player_finalize   (GObject         *object);
static void eos_cb            (GstElement      *sink,
		               Player          *player);
static void error_cb          (GObject         *object,
			       GstObject       *origin,
			       char            *error,
			       Player          *player);
static void time_tick_cb      (GstPlay         *play,
			       gint64           time_nanos,
			       Player          *player);


enum {
	END_OF_STREAM,
	TICK,
	ERROR,
	LAST_SIGNAL
};

struct _PlayerPriv {
	GstPlay    *play;

	GstElement *source;
	GstElement *volume;
	GstElement *sink;

	char       *current_file;

	int	    cur_volume;

	guint	    eos_idle_id;

	gint64      pos;
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
	PlayerPriv *priv;

	gst_init (NULL, NULL);

	priv = g_new0 (PlayerPriv, 1);
	player->priv = priv;

	priv->eos_idle_id = 0;

	priv->play = gst_play_new (NULL);
	if (!priv->play)
		g_error ("Failed to create GstPlay object");

	priv->source = gst_element_factory_make ("gnomevfssrc", "source");
	if (!priv->source)
		g_error ("The gnomevfssrc element is required.");
	gst_play_set_data_src (priv->play, priv->source);

	priv->sink = gst_gconf_get_default_audio_sink ();
	if (!priv->sink)
		g_error ("Could not render default GStreamer audio output sink "
                         "from GConf /system/gstreamer/default/audiosink key. "
                         "Check if it is set correctly.");
	gst_play_set_audio_sink (priv->play, priv->sink);

	priv->volume = gst_bin_get_by_name (GST_BIN (priv->play), "volume");

	g_signal_connect (priv->play,
			  "error",
			  G_CALLBACK (error_cb),
			  player);

	g_signal_connect (priv->play,
			  "eos",
			  G_CALLBACK (eos_cb),
			  player);

	g_signal_connect (priv->play,
			  "time_tick",
			  G_CALLBACK (time_tick_cb),
			  player);
}

static void
player_finalize (GObject *object)
{
	Player *player = PLAYER (object);

	player_stop (player);

	g_free (player->priv);

	if (G_OBJECT_CLASS (parent_class)->finalize)
		(* G_OBJECT_CLASS (parent_class)->finalize) (object);
}

Player *
player_new (void)
{
	return g_object_new (TYPE_PLAYER, NULL);
}

static gboolean
eos_idle_cb (Player *player)
{
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

	g_idle_add ((GSourceFunc) error_idle_cb, data);
}

static void
time_tick_cb (GstPlay *play, gint64 time_nanos, Player *player)
{
	player->priv->pos = time_nanos;

	g_signal_emit (player, signals[TICK], 0, time_nanos / GST_SECOND);
}

gboolean
player_set_file (Player *player,
		 const char *file)
{
	GstElementState new_state;

	g_return_val_if_fail (IS_PLAYER (player), FALSE);

	if (player->priv->eos_idle_id > 0) {
		g_source_remove (player->priv->eos_idle_id);
		player->priv->eos_idle_id = 0;
	}

	if (!file) {
		player_stop (player);

		return FALSE;
	}

	switch (gst_element_get_state (GST_ELEMENT (player->priv->play))) {
	case GST_STATE_PLAYING:
		new_state = GST_STATE_PLAYING;
		player_stop (player);
		break;

	default:
		new_state = GST_STATE_READY;
		break;
	}

	g_free (player->priv->current_file);
	player->priv->current_file = g_strdup (file);

	gst_play_set_location (player->priv->play, file);

	gst_element_set_state (GST_ELEMENT (player->priv->play), new_state);

	player->priv->pos = 0;

	return TRUE;
}

void
player_play (Player *player)
{
	g_return_if_fail (IS_PLAYER (player));

	gst_element_set_state (GST_ELEMENT (player->priv->play), GST_STATE_PLAYING);
}

void
player_stop (Player *player)
{
	g_return_if_fail (IS_PLAYER (player));

	g_free (player->priv->current_file);
	player->priv->current_file = NULL;

	player->priv->pos = 0;

	gst_element_set_state (GST_ELEMENT (player->priv->play), GST_STATE_READY);
}

void
player_pause (Player *player)
{
	g_return_if_fail (IS_PLAYER (player));

	gst_element_set_state (GST_ELEMENT (player->priv->play), GST_STATE_PAUSED);
}

void
player_set_volume (Player *player, int volume)
{
	double d;

	g_return_if_fail (IS_PLAYER (player));
	g_return_if_fail (volume >= 0 && volume <= 100);

	player->priv->cur_volume = volume;

	d = volume / 100.0;

	g_object_set (player->priv->volume, "volume", d, NULL);
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
player_seek (Player *player, int t)
{
	g_return_if_fail (IS_PLAYER (player));

	gst_play_seek_to_time (player->priv->play, t * GST_SECOND);
}

int
player_tell (Player *player)
{
	g_return_val_if_fail (IS_PLAYER (player), -1);

	return player->priv->pos / GST_SECOND;
}
