/*
 * Copyright (C) 2003, 2004 Jorn Baayen <jorn@nl.linux.org>
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

public class About
{
	public const string Version = "0.7.1";

	private static string [] authors = null;
	public static string [] Authors {
		get {
			if (authors == null) {
				authors = new string [5];

				authors [0] = Muine.Catalog.GetString ("Jorn Baayen <jbaayen@gnome.org>");
				authors [1] = Muine.Catalog.GetString ("Lee Willis <lee@leewillis.co.uk>");
				authors [2] = Muine.Catalog.GetString ("Việt Yên Nguyễn <nguyen@cs.utwente.nl>");
				authors [3] = "";
				authors [4] = Muine.Catalog.GetString ("Album covers are provided by amazon.com.");
			}
			
			return authors;
		}
	}

	public static void ShowWindow (Gtk.Window parent)
	{
		string [] documenters = new string [] {};
		string translator_credits = Muine.Catalog.GetString ("translator-credits");

		Pixbuf pixbuf = new Pixbuf (null, "muine-about.png");

		Gnome.About about;
		about = new Gnome.About (Muine.Catalog.GetString ("Muine"), Version,
					 Muine.Catalog.GetString ("Copyright © 2003, 2004 Jorn Baayen"),
					 Muine.Catalog.GetString ("A music player"),
					 Authors, documenters,
					 (translator_credits == "translator-credits") ? null : translator_credits,
					 pixbuf);

		about.TransientFor = parent;
		about.Show ();
	}
}
