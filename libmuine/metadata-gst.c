/*
 * Copyright © 2004 Jorn Baayen <jorn@nl.linux.org>
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
 *
 * FIXME better mime detection, duration, ogg
 */

#include <gst/gst.h>
#include <gst/gsttag.h>
#include <glib.h>

#include "metadata.h"

struct _Metadata {
	GstElement *pipeline;
	char *error;
	gboolean eos;

	char *title;
	char **artists;
	char **albums;
	guint64 duration;
};

static void
eos_cb (GstElement *element,
	Metadata *metadata)
{
	metadata->eos = TRUE;
}

static void
error_cb (GstElement *element,
	  GObject *arg1,
	  char *errmsg,
	  Metadata *metadata)
{
	metadata->error = g_strdup (errmsg);
}

static void
load_tag (const GstTagList *list,
	  const char *tag,
	  Metadata *metadata)
{
	int count, i;
	const GValue *val;

	count = gst_tag_list_get_tag_size (list, tag);
	if (count < 1)
		return;

	if (!strcmp (tag, GST_TAG_TITLE)) {
		val = gst_tag_list_get_value_index (list, tag, 0);
		metadata->title = g_value_dup_string (val);
	} else if (!strcmp (tag, GST_TAG_ARTIST) ||
		   !strcmp (tag, GST_TAG_PERFORMER)) {
		metadata->artists = g_new (char *, count + 1);
		metadata->artists[count] = NULL;
		for (i = 0; i < count; i++) {
			val = gst_tag_list_get_value_index (list, tag, i);
			metadata->artists[i] = g_value_dup_string (val);
		}
	} else if (!strcmp (tag, GST_TAG_ALBUM)) {
		metadata->albums = g_new (char *, count + 1);
		metadata->albums[count] = NULL;
		for (i = 0; i < count; i++) {
			val = gst_tag_list_get_value_index (list, tag, i);
			metadata->albums[i] = g_value_dup_string (val);
		}
	} else if (!strcmp (tag, GST_TAG_DURATION)) {
		GValue newval = { 0, };
		val = gst_tag_list_get_value_index (list, tag, 0);
		g_value_init (&newval, G_TYPE_INT64);
		if (g_value_transform (val, &newval)) {
			metadata->duration = g_value_get_int64 (&newval);
		}
		g_value_unset (&newval);
	}
}

static void
found_tag_cb (GObject *pipeline,
	      GstElement *src,
	      GstTagList *tags,
	      Metadata *metadata)
{
	gst_tag_list_foreach (tags, (GstTagForeachFunc) load_tag, metadata);
}

Metadata *
metadata_load (const char *filename,
               char **error_message_return)
{
	Metadata *metadata;
	GstElement *pipeline, *decoder, *src, *sink;
	const char *plugin_name = NULL;

	g_return_val_if_fail (filename != NULL, NULL);

	if (g_str_has_suffix (filename, ".mp3"))
		plugin_name = "id3tag";
	else if (g_str_has_suffix (filename, ".ogg"))
		plugin_name = "vorbisfile";
	else {
		*error_message_return = g_strdup ("Unknown format");
		return NULL;
	}

	metadata = g_new0 (Metadata, 1);

	pipeline = gst_pipeline_new ("pipeline");

	g_signal_connect (pipeline, "error", G_CALLBACK (error_cb), metadata);
	g_signal_connect (pipeline, "found_tag", G_CALLBACK (found_tag_cb), metadata);

	decoder = gst_element_factory_make (plugin_name, plugin_name);
	if (!decoder)
		goto missing_plugin;
	gst_bin_add (GST_BIN (pipeline), decoder);

	plugin_name = "gnomevfssrc";
	src = gst_element_factory_make (plugin_name, plugin_name);
	if (!src)
		goto missing_plugin;
	gst_bin_add (GST_BIN (pipeline), src);
	g_object_set (G_OBJECT (src), "location", filename, NULL);

	plugin_name = "fakesink";
	sink = gst_element_factory_make (plugin_name, plugin_name);
	if (!sink)
		goto missing_plugin;
	gst_bin_add (GST_BIN (pipeline), sink);
	g_signal_connect (sink, "eos", G_CALLBACK (eos_cb), metadata);

	gst_element_link (src, decoder);

	caps = gst_caps_new ("caps", "application/x-gst-tags", NULL);
	gst_element_link_filtered (decoder, sink, caps);

	metadata->pipeline = pipeline;
	gst_element_set_state (pipeline, GST_STATE_PLAYING);
	while (gst_bin_iterate (GST_BIN (pipeline)) &&
	       metadata->error == NULL &&
	       !metadata->eos);
	gst_element_set_state (pipeline, GST_STATE_NULL);
	if (metadata->error) {
		*error_message_return = metadata->error;
	}

	gst_object_unref (GST_OBJECT (pipeline));
	gst_caps_unref (caps);

	return metadata;

missing_plugin:
	*error_message_return = g_strdup_printf ("Couldn't create %s element", plugin_name);

	gst_object_unref (GST_OBJECT (pipeline));

	metadata_free (metadata);

	return NULL;
}

void
metadata_free (Metadata *metadata)
{
	g_return_if_fail (metadata != NULL);

	g_free (metadata->title);
	if (metadata->artists)
		g_strfreev (metadata->artists);
	if (metadata->albums)
		g_strfreev (metadata->albums);

	g_free (metadata);
}

const char *
metadata_get_title (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, NULL);

	return (const char *) metadata->title;
}

const char *
metadata_get_artist (Metadata *metadata, int index)
{
	g_return_val_if_fail (metadata != NULL, NULL);

	return (const char *) metadata->artists[index];
}

int
metadata_get_artist_count (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, -1);

	return G_N_ELEMENTS (metadata->artists);
}

const char *
metadata_get_album (Metadata *metadata, int index)
{
	g_return_val_if_fail (metadata != NULL, NULL);

	return (const char *) metadata->albums[index];
}

int
metadata_get_album_count (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, -1);

	return G_N_ELEMENTS (metadata->albums);
}

guint64
metadata_get_duration (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, -1);

	return metadata->duration;
}
