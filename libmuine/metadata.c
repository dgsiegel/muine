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
 */

#include <libgnomevfs/gnome-vfs.h>
#include <vorbis/vorbisfile.h>
#include <id3tag.h>
#include <glib.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>

#include "id3-vfs/id3-vfs.h"
#include "ogg-helper.h"
#include "metadata.h"

struct _Metadata {
	char **titles;
	int titles_count;

	char **artists;
	int artists_count;

	char **albums;
	int albums_count;

	int track_number;

	char *year;

	long duration;

	char *mime_type;

	long mtime;
};

static long
get_duration_from_tag (struct id3_tag *tag)
{
	struct id3_frame const *frame;
	union id3_field const *field;
	unsigned int nstrings;
	id3_latin1_t *latin1;
	long time;

	/* The following is based on information from the
	 * ID3 tag version 2.4.0 Native Frames informal standard.
	 */
	frame = id3_tag_findframe (tag, "TLEN", 0);
	if (frame == NULL)
		return -1;

	field = id3_frame_field (frame, 1);
	nstrings = id3_field_getnstrings (field);
	if (nstrings <= 0)
		return -1;

	latin1 = id3_ucs4_latin1duplicate (id3_field_getstrings (field, 0));
	if (latin1 == NULL)
		return -1;

	/* "The 'Length' frame contains the length
	 * of the audio file in milliseconds,
	 * represented as a numeric string."
	 */
	time = atol (latin1);
	g_free (latin1);

	if (time > 0)
		return time;

	return -1;
}

static int
get_mp3_comment_count (struct id3_tag *tag,
		       const char *field_name)
{
	const struct id3_frame *frame;
	const union id3_field *field;

	frame = id3_tag_findframe (tag, field_name, 0);
	if (frame == 0)
		return 0;

	field = id3_frame_field (frame, 1);

	return id3_field_getnstrings (field);
}

static char *
get_mp3_comment_value (struct id3_tag *tag,
		       const char *field_name,
		       int index)
{
	const struct id3_frame *frame;
	const union id3_field *field;
	const id3_ucs4_t *ucs4 = NULL;
        id3_utf8_t *utf8 = NULL;

	frame = id3_tag_findframe (tag, field_name, 0);
	if (frame == 0)
		return NULL;

	field = id3_frame_field (frame, 1);

	if (index >= id3_field_getnstrings (field))
		return NULL;

	ucs4 = id3_field_getstrings (field, index);
	if (ucs4 == NULL)
		return NULL;

	utf8 = id3_ucs4_utf8duplicate (ucs4);
	if (utf8 == NULL)
		return NULL;

	if (!g_utf8_validate (utf8, -1, NULL))
		return NULL;

	return utf8;
}

static Metadata *
assign_metadata_mp3 (const char *filename,
		     GnomeVFSFileInfo *info,
		     char **error_message_return)
{
	Metadata *metadata;
	struct id3_vfs_file *file;
	struct id3_tag *tag;
	int bitrate, samplerate, channels, version, vbr, count, i;
	long time, tag_time;
	char *track_number_raw;

	file = id3_vfs_open (filename, ID3_FILE_MODE_READONLY);
	if (file == NULL) {
		*error_message_return = g_strdup ("Failed to open file for reading");
		return NULL;
	}

	tag = id3_vfs_tag (file);

	if (id3_vfs_bitrate (file,
			     &bitrate,
			     &samplerate,
			     &time,
			     &version,
			     &vbr,
			     &channels) == 0) {
		id3_vfs_close (file);

		*error_message_return = g_strdup ("Failed to gather information about the file");
		return NULL;
	}

	metadata = g_new0 (Metadata, 1);

	tag_time = get_duration_from_tag (tag);

	if (tag_time > 0)
		metadata->duration = tag_time;
	else if (time > 0)
		metadata->duration = time;
	else {
		if (bitrate > 0) {
			metadata->duration = ((double) info->size) / ((double) bitrate / 8000.0f);
		} else {
			/* very rough guess */
			metadata->duration = ((double) info->size) / ((double) 128 / 8.0f);
		}
	}

	count = get_mp3_comment_count (tag, ID3_FRAME_TITLE);
	metadata->titles = g_new (char *, count + 1);
	metadata->titles[count] = NULL;
	metadata->titles_count = count;
	for (i = 0; i < count; i++) {
		metadata->titles[i] = get_mp3_comment_value (tag, ID3_FRAME_TITLE, i);
	}

	count = get_mp3_comment_count (tag, ID3_FRAME_ARTIST);
	metadata->artists = g_new (char *, count + 1);
	metadata->artists[count] = NULL;
	metadata->artists_count = count;
	for (i = 0; i < count; i++) {
		metadata->artists[i] = get_mp3_comment_value (tag, ID3_FRAME_ARTIST, i);
	}

	count = get_mp3_comment_count (tag, ID3_FRAME_ALBUM);
	metadata->albums = g_new (char *, count + 1);
	metadata->albums[count] = NULL;
	metadata->albums_count = count;
	for (i = 0; i < count; i++) {
		metadata->albums[i] = get_mp3_comment_value (tag, ID3_FRAME_ALBUM, i);
	}

	track_number_raw = get_mp3_comment_value (tag, ID3_FRAME_TRACK, 0);
	if (track_number_raw != NULL)
		metadata->track_number = atoi (track_number_raw);
	else
		metadata->track_number = -1;
	g_free (track_number_raw);

	metadata->year = get_mp3_comment_value (tag, ID3_FRAME_YEAR, 0);

	id3_vfs_close (file);

	*error_message_return = NULL;

	return metadata;
}

static ov_callbacks file_info_callbacks =
{
        ogg_helper_read,
        ogg_helper_seek,
        ogg_helper_close_dummy,
        ogg_helper_tell
};

static char *
get_vorbis_comment_value (vorbis_comment *comment,
			  const char *entry,
			  int index)
{
	const char *val;

	val = vorbis_comment_query (comment, (char *) entry, index);
	if (val == NULL)
		return NULL;

	/* vorbis comments should be in UTF-8 */
	if (!g_utf8_validate (val, -1, NULL))
		return NULL;

	return g_strdup (val);
}

static Metadata *
assign_metadata_ogg (const char *filename,
		     char **error_message_return)
{
	Metadata *metadata = NULL;
	GnomeVFSResult res;
	GnomeVFSHandle *handle;
	int rc, count, i;
	OggVorbis_File vf;
	vorbis_comment *comment;
	char *track_number_raw;

	res = gnome_vfs_open (&handle, filename, GNOME_VFS_OPEN_READ);
	if (res != GNOME_VFS_OK) {
		*error_message_return = g_strdup ("Failed to open file for reading");
		return NULL;
	}

	rc = ov_open_callbacks (handle, &vf, NULL, 0,
				file_info_callbacks);
	if (rc < 0) {
		ogg_helper_close (handle);
		*error_message_return = g_strdup ("Failed to open file as Ogg Vorbis");
		return NULL;
	}

	comment = ov_comment (&vf, -1);
	if (!comment) {
		*error_message_return = g_strdup ("Failed to read comments");
		goto out;
	}

	metadata = g_new0 (Metadata, 1);

	count = vorbis_comment_query_count (comment, "title");
	metadata->titles = g_new (char *, count + 1);
	metadata->titles[count] = NULL;
	metadata->titles_count = count;
	for (i = 0; i < count; i++) {
		metadata->titles[i] = get_vorbis_comment_value (comment, "title", i);
	}

	count = vorbis_comment_query_count (comment, "artist");
	metadata->artists = g_new (char *, count + 1);
	metadata->artists[count] = NULL;
	metadata->artists_count = count;
	for (i = 0; i < count; i++) {
		metadata->artists[i] = get_vorbis_comment_value (comment, "artist", i);
	}

	count = vorbis_comment_query_count (comment, "album");
	metadata->albums = g_new (char *, count + 1);
	metadata->albums[count] = NULL;
	metadata->albums_count = count;
	for (i = 0; i < count; i++) {
		metadata->albums[i] = get_vorbis_comment_value (comment, "album", i);
	}

	track_number_raw = vorbis_comment_query (comment, "tracknumber", 0);
	if (track_number_raw != NULL)
		metadata->track_number = atoi (track_number_raw);
	else
		metadata->track_number = -1;

	metadata->year = get_vorbis_comment_value (comment, "date", 0);

	metadata->duration = (long) ov_time_total (&vf, -1) * 1000;

	*error_message_return = NULL;

out:
	ov_clear (&vf);
	ogg_helper_close (handle);

	return metadata;
}

Metadata *
metadata_load (const char *filename,
               char **error_message_return)
{
	Metadata *m = NULL;
	GnomeVFSFileInfo *info;

	g_return_val_if_fail (filename != NULL, NULL);

	info = gnome_vfs_file_info_new ();
	gnome_vfs_get_file_info (filename, info,
				 GNOME_VFS_FILE_INFO_GET_MIME_TYPE | GNOME_VFS_FILE_INFO_FOLLOW_LINKS);

	if (!strcmp (info->mime_type, "audio/x-mp3") ||
	    !strcmp (info->mime_type, "audio/mpeg"))
		m = assign_metadata_mp3 (filename, info, error_message_return);
	else if (!strcmp (info->mime_type, "application/x-ogg") ||
		 !strcmp (info->mime_type, "application/ogg"))
		m = assign_metadata_ogg (filename, error_message_return);
	else
		*error_message_return = g_strdup ("Unknown format");

	if (m != NULL) {
		m->mime_type = g_strdup (info->mime_type);
		m->mtime = info->mtime;
	}

	gnome_vfs_file_info_unref (info);

	return m;
}

void
metadata_free (Metadata *metadata)
{
	g_return_if_fail (metadata != NULL);

	if (metadata->titles)
		g_strfreev (metadata->titles);
	if (metadata->artists)
		g_strfreev (metadata->artists);
	if (metadata->albums)
		g_strfreev (metadata->albums);

	g_free (metadata->year);
	g_free (metadata->mime_type);

	g_free (metadata);
}

const char *
metadata_get_title (Metadata *metadata, int index)
{
	g_return_val_if_fail (metadata != NULL, NULL);

	return (const char *) metadata->titles[index];
}

int
metadata_get_title_count (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, -1);

	return metadata->titles_count;
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

	return metadata->artists_count;
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

	return metadata->albums_count;
}

int
metadata_get_track_number (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, -1);

	return metadata->track_number;
}

const char *
metadata_get_year (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, NULL);

	return (const char *) metadata->year;
}

long
metadata_get_duration (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, -1);

	return metadata->duration;
}

const char *
metadata_get_mime_type (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, NULL);

	return (const char *) metadata->mime_type;
}

long
metadata_get_mtime (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, -1);

	return metadata->mtime;
}
