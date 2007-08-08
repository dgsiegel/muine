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

using System;
using Gtk;
using Gdk;

using Mono.Unix;

namespace Muine
{
	public class About : Gtk.AboutDialog
	{
		// Strings
		private static readonly string string_translators = 
			Catalog.GetString ("translator-credits");

		private static readonly string string_muine =
			Catalog.GetString ("Muine");

		private static readonly string string_copyright =
			Catalog.GetString ("Copyright © 2003-2007 Jorn Baayen");

		private static readonly string string_description =
			Catalog.GetString ("A music player");

		private static readonly string string_website =
			"http://www.muine-player.org/";
		
		// Authors
		private static readonly string [] authors = {
			Catalog.GetString ("Jorn Baayen <jbaayen@gnome.org>"),
			Catalog.GetString ("Lee Willis <lee@leewillis.co.uk>"),
			Catalog.GetString ("Việt Yên Nguyễn <nguyen@cs.utwente.nl>"),
			Catalog.GetString ("Tamara Roberson <tamara.roberson@gmail.com>"),
			Catalog.GetString ("Peter Johanson <peter@peterjohanson.com>"),
			String.Empty,
			Catalog.GetString ("Album covers are provided by amazon.com and musicbrainz.org."),
			null,
		};
		
		// Documenters
		private static readonly string [] documenters = {
			null,
		};

		// Icon
		private static readonly Gdk.Pixbuf pixbuf =
		  new Gdk.Pixbuf (null, "muine-about.png");
	
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
		public About (Gtk.Window parent) : base ()
		{
			base.Authors           = authors;
			base.Copyright         = string_copyright;
			base.Comments          = string_description;
			base.Documenters       = documenters;
			base.Logo              = pixbuf;
			base.Name              = string_muine;
			base.TranslatorCredits = translators;
			base.Version           = Defines.VERSION;
			base.Website           = string_website;

			base.TransientFor = parent;

			base.Response += OnResponse;

			base.Show ();
		}

		// Handlers :: OnResponse
		private void OnResponse (object obj, EventArgs args)
		{
			base.Hide ();
		}
	}
}
