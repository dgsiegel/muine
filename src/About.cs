/*
 * Copyright (C) 2003, 2004, 2005 Jorn Baayen <jbaayen@gnome.org>
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

using Gtk;
using Gdk;

using Mono.Posix;

namespace Muine
{
	public class About : Gnome.About
	{
		// Strings
		private static readonly string string_translators = 
			Catalog.GetString ("translator-credits");

		private static readonly string string_muine =
			Catalog.GetString ("Muine");

		private static readonly string string_copyright =
			Catalog.GetString ("Copyright © 2003, 2004, 2005, 2006 Jorn Baayen");

		private static readonly string string_description =
			Catalog.GetString ("A music player");
		
		// Authors
		private static readonly string [] authors = {
			Catalog.GetString ("Jorn Baayen <jbaayen@gnome.org>"),
			Catalog.GetString ("Lee Willis <lee@leewillis.co.uk>"),
			Catalog.GetString ("Việt Yên Nguyễn <nguyen@cs.utwente.nl>"),
			Catalog.GetString ("Tamara Roberson <foxxygirltamara@gmail.com>"),
			"",
			Catalog.GetString ("Album covers are provided by amazon.com."),
		};
		
		// Documenters
		private static readonly string [] documenters = {
		};

		// Icon
		private static readonly Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (null, "muine-about.png");
	
		// Variables
		private static string translators;
		
		// Static Constructor
		static About ()
		{
			// Translators
			if (string_translators == "translator-credits")
				translators = null;
			else
				translators = string_translators;
		}

		// Constructor
		/// <summary>
		/// 	The About window for Muine
		/// </summary>
		/// <param name="parent">
		///	The parent window
		/// </param>
		public About (Gtk.Window parent) 
		: base (string_muine, Defines.VERSION, string_copyright, string_description,
			authors, documenters, translators, pixbuf)
		{
			TransientFor = parent;

			Show ();
		}
	}
}
