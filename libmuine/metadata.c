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
#include <FLAC/metadata.h>
#include <FLAC/stream_decoder.h>
#include <glib.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>

#include "id3-vfs/id3-vfs.h"
#include "ogg-helper.h"
#include "metadata.h"

struct _Metadata {
	char *title;

	char **artists;
	int artists_count;

	char **performers;
	int performers_count;

	char *album;

	int track_number;

	char *year;

	long duration;

	char *mime_type;

	long mtime;

	double gain;
	double peak;
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

	metadata->title = get_mp3_comment_value (tag, ID3_FRAME_TITLE, 0);

	count = get_mp3_comment_count (tag, ID3_FRAME_ARTIST);
	metadata->artists = g_new (char *, count + 1);
	metadata->artists[count] = NULL;
	metadata->artists_count = count;
	for (i = 0; i < count; i++) {
		metadata->artists[i] = get_mp3_comment_value (tag, ID3_FRAME_ARTIST, i);
	}

	count = get_mp3_comment_count (tag, "TPE2");
	metadata->performers = g_new (char *, count + 1);
	metadata->performers[count] = NULL;
	metadata->performers_count = count;
	for (i = 0; i < count; i++) {
		metadata->performers[i] = get_mp3_comment_value (tag, "TPE2", i);
	}

	metadata->album = get_mp3_comment_value (tag, ID3_FRAME_ALBUM, 0);

	track_number_raw = get_mp3_comment_value (tag, ID3_FRAME_TRACK, 0);
	if (track_number_raw != NULL)
		metadata->track_number = atoi (track_number_raw);
	else
		metadata->track_number = -1;
	g_free (track_number_raw);

	metadata->year = get_mp3_comment_value (tag, ID3_FRAME_YEAR, 0);

	/* TODO implement */
	metadata->gain = 0.0;
	metadata->peak = 0.0;

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

static void
assign_metadata_vorbiscomment (Metadata *metadata,
			       vorbis_comment *comment)
{
	char *raw, *version, *title;
	int count, i;

	version = get_vorbis_comment_value (comment, "version", 0);

	title = get_vorbis_comment_value (comment, "title", 0);

	if (version != NULL && title != NULL) {
		metadata->title = g_strdup_printf ("%s (%s)", title, version);

		g_free (title);
		g_free (version);
	} else if (title != NULL) {
		metadata->title = title;
	} else if (version != NULL) {
		metadata->title = version;
	}

	count = vorbis_comment_query_count (comment, "artist");
	metadata->artists = g_new (char *, count + 1);
	metadata->artists[count] = NULL;
	metadata->artists_count = count;
	for (i = 0; i < count; i++) {
		metadata->artists[i] = get_vorbis_comment_value (comment, "artist", i);
	}

	count = vorbis_comment_query_count (comment, "performer");
	metadata->performers = g_new0 (char *, count + 1);
	metadata->performers[count] = NULL;
	metadata->performers_count = count;
	for (i = 0; i < count; i++) {
		metadata->performers[i] = get_vorbis_comment_value (comment, "performer", i);
	}

	metadata->album = get_vorbis_comment_value (comment, "album", 0);

	raw = vorbis_comment_query (comment, "tracknumber", 0);
	if (raw != NULL)
		metadata->track_number = atoi (raw);
	else
		metadata->track_number = -1;

	metadata->year = get_vorbis_comment_value (comment, "date", 0);

	raw = vorbis_comment_query (comment, "replaygain_album_gain", 0);
	if (raw == NULL) {
		raw = vorbis_comment_query (comment, "replaygain_track_gain", 0);
		if (raw == NULL) {
			raw = vorbis_comment_query (comment, "rg_audiophile", 0);
			if (raw == NULL) {
				raw = vorbis_comment_query (comment, "rg_radio", 0);
			}
		}
	}

	if (raw != NULL)
		metadata->gain = atof (raw);
	else
		metadata->gain = 0.0;

	raw = vorbis_comment_query (comment, "replaygain_album_peak", 0);
	if (raw == NULL) {
		raw = vorbis_comment_query (comment, "replaygain_track_peak", 0);
		if (raw == NULL) {
			raw = vorbis_comment_query (comment, "rg_peak", 0);
		}
	}

	if (raw != NULL)
		metadata->peak = atof (raw);
	else
		metadata->peak = 0.0;
}

static Metadata *
assign_metadata_ogg (const char *filename,
		     char **error_message_return)
{
	Metadata *metadata = NULL;
	GnomeVFSResult res;
	GnomeVFSHandle *handle;
	int rc;
	OggVorbis_File vf;
	vorbis_comment *comment;

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

	assign_metadata_vorbiscomment (metadata, comment);

	metadata->duration = (long) ov_time_total (&vf, -1) * 1000;

	*error_message_return = NULL;

out:
	ov_clear (&vf);
	ogg_helper_close (handle);

	return metadata;
}

typedef struct {
	GnomeVFSHandle *handle;

	vorbis_comment *comment;
	long duration;
} CallbackData;

static FLAC__StreamDecoderReadStatus
FLAC_read_callback (const FLAC__StreamDecoder *decoder, FLAC__byte buffer[], unsigned *bytes, void *client_data)
{
	CallbackData *data = (CallbackData *) client_data;
	GnomeVFSFileSize read;
	GnomeVFSResult result;

	result = gnome_vfs_read (data->handle, buffer, *bytes, &read);

	if (result == GNOME_VFS_OK) {
		*bytes = read;
		return FLAC__STREAM_DECODER_READ_STATUS_CONTINUE;
	} else if (result == GNOME_VFS_ERROR_EOF) {
		return FLAC__STREAM_DECODER_READ_STATUS_END_OF_STREAM;
	} else {
		return FLAC__STREAM_DECODER_READ_STATUS_ABORT;
	}
}

static FLAC__StreamDecoderWriteStatus
FLAC_write_callback (const FLAC__StreamDecoder *decoder, const FLAC__Frame *frame,
		     const FLAC__int32 *const buffer[], void *client_data)
{
	/* This callback should never be called, because we request that
	 * FLAC only decodes metadata, never actual sound data. */
	return FLAC__STREAM_DECODER_WRITE_STATUS_ABORT;
}

static void
FLAC_metadata_callback (const FLAC__StreamDecoder *decoder, const FLAC__StreamMetadata *metadata, void *client_data)
{
	CallbackData *data = (CallbackData *) client_data;

	if (metadata->type == FLAC__METADATA_TYPE_STREAMINFO) {
		data->duration = (long) (metadata->data.stream_info.total_samples * 1000) / metadata->data.stream_info.sample_rate;
	} else if (metadata->type == FLAC__METADATA_TYPE_VORBIS_COMMENT) {
		const FLAC__StreamMetadata_VorbisComment *vc_block = &metadata->data.vorbis_comment;
		vorbis_comment *comment = data->comment;
		int c;

		for (c = 0; c < vc_block->num_comments; c++) {
			FLAC__StreamMetadata_VorbisComment_Entry entry = vc_block->comments[c];
			char *null_terminated_comment = malloc (entry.length + 1);
			char **parts;

			memcpy (null_terminated_comment, entry.entry, entry.length);
			null_terminated_comment[entry.length] = '\0';
			parts = g_strsplit (null_terminated_comment, "=", 2);

			if (parts[0] == NULL || parts[1] == NULL)
				goto free_continue;

			vorbis_comment_add_tag (comment, parts[0], parts[1]);

		free_continue:
			g_strfreev (parts);
			free (null_terminated_comment);
		}
	}
}

static void
FLAC_error_callback (const FLAC__StreamDecoder *decoder, FLAC__StreamDecoderErrorStatus status, void *client_data)
{
}

static Metadata *
assign_metadata_flac (const char *filename,
		      char **error_message_return)
{
	Metadata *metadata = NULL;
	GnomeVFSResult res;
	GnomeVFSHandle *handle;
	vorbis_comment *comment;
	FLAC__StreamDecoder *flac_decoder;
	CallbackData *callback_data;

	res = gnome_vfs_open (&handle, filename, GNOME_VFS_OPEN_READ);
	if (res != GNOME_VFS_OK) {
		*error_message_return = g_strdup ("Failed to open file for reading");
		return NULL;
	}

	comment = g_new (vorbis_comment, 1);
	vorbis_comment_init (comment);

	flac_decoder = FLAC__stream_decoder_new ();

	FLAC__stream_decoder_set_read_callback (flac_decoder, FLAC_read_callback);
	FLAC__stream_decoder_set_write_callback (flac_decoder, FLAC_write_callback);
	FLAC__stream_decoder_set_metadata_callback (flac_decoder, FLAC_metadata_callback);
	FLAC__stream_decoder_set_error_callback (flac_decoder, FLAC_error_callback);

	callback_data = g_new0 (CallbackData, 1);
	callback_data->handle = handle;
	callback_data->comment = comment;
	FLAC__stream_decoder_set_client_data (flac_decoder, callback_data);

	/* by default, only the STREAMINFO block is parsed and passed to
	 * the metadata callback.  Here we instruct the decoder to also
	 * pass us the VORBISCOMMENT block if there is one. */
	FLAC__stream_decoder_set_metadata_respond (flac_decoder, FLAC__METADATA_TYPE_VORBIS_COMMENT);

	FLAC__stream_decoder_init (flac_decoder);

	/* this runs the decoding process, calling the callbacks as appropriate */
	if (FLAC__stream_decoder_process_until_end_of_metadata (flac_decoder) == 0) {
		*error_message_return = g_strdup ("Error decoding FLAC file");
		goto out;
	}

	metadata = g_new0 (Metadata, 1);

	assign_metadata_vorbiscomment (metadata, comment);

	metadata->duration = callback_data->duration;

	*error_message_return = NULL;

out:
	g_free (callback_data);

	FLAC__stream_decoder_finish (flac_decoder);
	FLAC__stream_decoder_delete (flac_decoder);
	gnome_vfs_close (handle);

	vorbis_comment_clear (comment);
	g_free (comment);

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
	else if (!strcmp (info->mime_type, "application/x-flac") ||
		 !strcmp (info->mime_type, "audio/x-flac"))
		m = assign_metadata_flac (filename, error_message_return);
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

	if (metadata->artists)
		g_strfreev (metadata->artists);
	if (metadata->performers)
		g_strfreev (metadata->performers);

	g_free (metadata->title);
	g_free (metadata->album);
	g_free (metadata->year);
	g_free (metadata->mime_type);

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

	return metadata->artists_count;
}

const char *
metadata_get_performer (Metadata *metadata, int index)
{
	g_return_val_if_fail (metadata != NULL, NULL);

	return (const char *) metadata->performers[index];
}

int
metadata_get_performer_count (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, -1);

	return metadata->performers_count;
}

const char *
metadata_get_album (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, NULL);

	return (const char *) metadata->album;
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

double
metadata_get_gain (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, -1);

	return metadata->gain;
}

double
metadata_get_peak (Metadata *metadata)
{
	g_return_val_if_fail (metadata != NULL, -1);

	return metadata->peak;
}
