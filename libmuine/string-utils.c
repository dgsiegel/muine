/*
 * Copyright (C) 2005 Jorn Baayen <jbaayen@gnome.org>
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

#include <glib.h>

#include "string-utils.h"

char *
string_utils_strip_non_alnum (const char *in)
{
	GString *string;
	char *pos, *ret;

	string = g_string_new (NULL);

	pos = (char *) in;
	while (*pos != '\0') {
		gunichar c = g_utf8_get_char (pos);

		if (g_unichar_isalnum (c) || g_unichar_isspace (c))
			g_string_append_unichar (string, c);

		pos = g_utf8_next_char (pos);
	};

	ret = string->str;
	g_string_free (string, FALSE);
	return ret;
}
