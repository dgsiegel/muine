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
 * Some code stolen from song-db.c from Jamboree.
 */

#include <glib.h>
#include <gdbm.h>
#include <string.h>

#include "db.h"

#define _ALIGN_VALUE(this, boundary) \
  (( ((unsigned long)(this)) + (((unsigned long)(boundary)) -1)) & (~(((unsigned long)(boundary))-1)))
#define _ALIGN_ADDRESS(this, boundary) \
  ((void*)_ALIGN_VALUE(this, boundary))

gpointer
db_open (const char *filename,
	 char **error_message_return)
{
	GDBM_FILE db = gdbm_open ((char *) filename, 4096,
				  GDBM_NOLOCK | GDBM_WRCREAT | GDBM_SYNC,
				  04644, NULL);

	if (db == NULL) {
		*error_message_return = gdbm_strerror (gdbm_errno);
	} else {
		*error_message_return = NULL;
	}

	return (gpointer) db;
}

gboolean
db_exists (gpointer db,
	   const char *key_str)
{
	datum key;

	memset (&key, 0, sizeof (key));
	key.dptr = (gpointer) key_str;
	key.dsize = strlen (key_str);

	return gdbm_exists ((GDBM_FILE) db, key);
}

void
db_delete (gpointer db,
	   const char *key_str)
{
	datum key;

	memset (&key, 0, sizeof (key));
	key.dptr = (gpointer) key_str;
	key.dsize = strlen (key_str);

	gdbm_delete ((GDBM_FILE) db, key);
}

void
db_store (gpointer db,
	  const char *key_str,
	  gboolean overwrite,
	  EncodeFunc func,
	  gpointer user_data)
{
	datum key, data;

	memset (&key, 0, sizeof (key));
	key.dptr = (gpointer) key_str;
	key.dsize = strlen (key_str);

	memset (&data, 0, sizeof (data));
	data.dptr = func (user_data, &data.dsize);

	gdbm_store ((GDBM_FILE) db, key, data,
		    overwrite ? GDBM_INSERT : GDBM_REPLACE);

	g_free (data.dptr);
}

void
db_foreach (gpointer db,
	    ForeachDecodeFunc func,
	    gpointer user_data)
{
	datum key, data;

	key = gdbm_firstkey ((GDBM_FILE) db);

	if (key.dptr == NULL)
		return;

	while (TRUE) {
		data = gdbm_fetch ((GDBM_FILE) db, key);

		char *keystr = g_strndup (key.dptr, key.dsize);
		func ((const char *) keystr, (gpointer) data.dptr, user_data);
		g_free (keystr);

		key = gdbm_nextkey ((GDBM_FILE) db, key);

		if (key.dptr == NULL)
			break;
	}
}

gpointer
db_unpack_string (gpointer p, char **str)
{
	int len;

	p = _ALIGN_ADDRESS (p, 4);

	len = *(int *) p;

	if (str)
		*str = g_malloc (len + 1);

	p += 4;

	if (str) {
		memcpy (*str, p, len);
		(*str)[len] = 0;
	}

	return p + len + 1;
}

gpointer
db_unpack_int (gpointer p, int *val)
{
	p = _ALIGN_ADDRESS (p, 4);

	if (val)
		*val = *(int *) p;

	p += 4;

	return p;
}

gpointer
db_unpack_long (gpointer p, long *val)
{
	p = _ALIGN_ADDRESS (p, 4);

	if (val)
		*val = *(long *) p;

	p += 4;

	return p;
}

static void
string_align (GString *string, int boundary)
{
	gpointer p;
	int padding;
	int i;

	p = string->str + string->len;

	padding = _ALIGN_ADDRESS (p, boundary) - p;

	for (i = 0; i < padding; i++)
		g_string_append_c (string, 0);
}

gpointer
db_pack_start (void)
{
	return (gpointer) g_string_new (NULL);
}

void
db_pack_string (gpointer p, const char *str)
{
	GString *string = (GString *) p;
	int len;

	if (str)
		len = strlen (str);
	else
		len = 0;

	db_pack_int (string, len);

	if (str)
		g_string_append (string, str);

	g_string_append_c (string, 0);
}

void
db_pack_int (gpointer p, int val)
{
	GString *string = (GString *) p;

	string_align (string, 4);

	g_string_append_len (string, (char *) &val, 4);
}

void
db_pack_long (gpointer p, long val)
{
	GString *string = (GString *) p;

	string_align (string, 4);

	g_string_append_len (string, (char *) &val, 4);
}

gpointer
db_pack_end (gpointer p, int *len)
{
	GString *string = (GString *) p;

	*len = string->len;

	return g_string_free (string, FALSE);
}