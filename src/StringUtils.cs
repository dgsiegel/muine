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

using System;
using System.Runtime.InteropServices;

using GLibSharp;

public class StringUtils
{
	public static string SecondsToString (long time)
	{
		int h, m, s;

		h = (int) (time / 3600);
		m = (int) ((time % 3600) / 60);
		s = (int) ((time % 3600) % 60);

		if (h > 0) {
			return h + ":" + m.ToString ("d2") + ":" + s.ToString ("d2");
		} else {
			return m + ":" + s.ToString ("d2");
		}
	}

	public static string JoinHumanReadable (string [] strings, int max)
	{
		string ret;

		if (strings.Length == 0)
			ret = Muine.Catalog.GetString ("Unknown");
		else if (strings.Length == 1) 
			ret = strings [0];
		else if (max > 1 && strings.Length > max)
			ret = String.Format (Muine.Catalog.GetString ("{0} and others"), String.Join (", ", strings, 0, max));
		else
			ret = String.Format (Muine.Catalog.GetString ("{0} and {1}"), String.Join (", ", strings, 0, strings.Length - 1), strings [strings.Length - 1]);

		return ret;
	}

	public static string JoinHumanReadable (string [] strings)
	{
		return JoinHumanReadable (strings, -1);
	}

	public static string PrefixToSuffix (string str, string prefix)
	{
		string ret;

		ret = str.Remove (0, prefix.Length + 1);
		ret = ret + " " + prefix;

		return ret;
	}

	[DllImport ("libc")]
	private static extern int strlen (string str);

	public static uint GetByteLength (string str)
	{
		return (uint) strlen (str);
	}
	
	[DllImport ("libc")]
	private static extern int strcmp (string a, string b);

	public static int StrCmp (string a, string b)
	{
		return strcmp (a, b);
	}

	[DllImport ("libglib-2.0-0.dll")]
	private static extern IntPtr g_utf8_collate_key (string str, int len);

	public static string CollateKey (string key)
	{
		IntPtr str_ptr = g_utf8_collate_key (key, -1);
		
		return Marshaller.PtrToStringGFree (str_ptr);
	}

	public static string SelectionDataToString (Gtk.SelectionData data)
	{
		string str = "";

		byte [] raw_data = data.Data;

		for (int i = 0; i < data.Length; i++)
			str = str + (char) raw_data [i];

		return str;
	}
}
