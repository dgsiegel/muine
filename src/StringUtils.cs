/*
 * Copyright (C) 2004, 2005 Jorn Baayen <jbaayen@gnome.org>
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
using System.Collections;

using Mono.Posix;

namespace Muine
{
	public sealed class StringUtils
	{
		// Strings
		private static readonly string string_unknown =
			Catalog.GetString ("Unknown");
		private static readonly string string_many =
			Catalog.GetString ("{0} and others");
		private static readonly string string_several =
			Catalog.GetString ("{0} and {1}");
		
		// Methods
		// Methods :: Public
		// Methods :: Public :: SecondsToString		
		public static string SecondsToString (long time)
		{
			long h, m, s;

			h = (time / 3600);
			m = ((time % 3600) / 60);
			s = ((time % 3600) % 60);

			return (h > 0)
  			       ? String.Format ("{0}:{1}:{2}", h, m.ToString ("d2"), s.ToString ("d2"))
			       : String.Format (    "{0}:{1}",    m                , s.ToString ("d2"));
		}

		// Methods :: Public :: CleanStringList
		public static string [] CleanStringList (string [] orig_strings)
		{
			ArrayList strings = new ArrayList ();
			foreach (string s in orig_strings) {
				string s2 = s.Trim ();
				if (s2.Length == 0)
					continue;
				strings.Add (s2);
			}
			
			string [] array = new string [strings.Count];
			strings.CopyTo (array);
			return array;
		}

		// Methods :: Public :: JoinHumanReadable
		public static string JoinHumanReadable (string [] strings)
		{
			return JoinHumanReadable (strings, -1);
		}

		public static string JoinHumanReadable (string [] orig_strings, int max)
		{
		
			string [] strings = CleanStringList (orig_strings);
		
			return (strings.Length == 0)
			       ? string_unknown
			       :
			       (strings.Length == 1)
			       ? strings [0]
			       :
			       (max > 1 && strings.Length > max)
			       ? String.Format (string_many   , String.Join (", ", strings, 0, max               ))
			       : String.Format (string_several, String.Join (", ", strings, 0, strings.Length - 1), 
			       						     strings [strings.Length - 1]);
		}

		// Methods :: Public :: PrefixToSuffix
		public static string PrefixToSuffix (string str, string prefix)
		{
			string ret;

			ret = str.Remove (0, prefix.Length + 1);
			ret = ret + " " + prefix;

			return ret;
		}

		// Methods :: Public :: SearchKey
		public static string SearchKey (string key)
		{
			string lower = key.ToLower ();

			bool different = false;
			string stripped = "";
			foreach (char c in lower) {
				if (Char.IsLetterOrDigit (c) ||
				    Char.IsWhiteSpace    (c) ||
				    Char.IsSurrogate     (c))
					stripped += c;
				else
					different = true;
			}

			// Both, so that "R.E.M." will yield only "R.E.M.", but "rem"
			// both "remix and "R.E.M.".
			if (different)
				return String.Format ("{0} {1}", stripped, lower);
			else
				return stripped;
		}

		// Methods :: Public :: EscapeForPango
		public static string EscapeForPango (string original)
		{
			string str = original;
                        str = str.Replace ("&", "&amp;");
                        str = str.Replace ("<", "&lt;");
			return str;
		}
	}
}
