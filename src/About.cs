/*
 * Copyright © 2003, 2004 Jorn Baayen <jorn@nl.linux.org>
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
	private static string version = "0.3.1.1";
	public static string Version {
		get {
			return version;
		}
	}

	private static string [] authors = {
		"Jorn Baayen (jorn@nl.linux.org)"
	};
	public static string [] Authors {
		get {
			return authors;
		}
	}

	private static Pixbuf pixbuf = null;

	public static void ShowWindow (Gtk.Window parent)
	{
		string [] documenters = new string [] {};
		string translators = null;

		if (pixbuf == null)
			pixbuf = new Pixbuf (null, "muine-playlist.png");

		Gnome.About about;
		about = new Gnome.About ("Muine", version,
					 "Copyright \xa9 2003, 2004 Jorn Baayen",
					 "A music player",
					 authors, documenters, translators,
					 pixbuf);

		Gnome.HRef href = new Gnome.HRef ("http://www.amazon.com/", "Amazon.com");
		about.VBox.PackStart (href, false, false, 5);
		href.Visible = true;

		Tooltips tooltips = new Tooltips ();
		tooltips.SetTip (href, "Thanks to Amazon.com for providing album cover images!", null);
					 
		about.TransientFor = parent;
		about.Show ();
	}
}
