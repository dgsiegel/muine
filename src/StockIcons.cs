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

using System;

public class StockIcons 
{
	private static string [] stock_icons = {
		"muine-play",
		"muine-pause",
		"muine-previous",
		"muine-queue",
		"muine-next",
		"muine-rewind",
		"muine-forward",
		"muine-add-album",
		"muine-groups",
		"muine-volume-zero",
		"muine-volume-min",
		"muine-volume-medium",
		"muine-volume-max"
	};

	public static void Initialize ()
	{
		IconFactory factory = new IconFactory ();
		factory.AddDefault ();

		foreach (string name in stock_icons) {
			Pixbuf pixbuf = new Pixbuf (null, name + ".png");
			IconSet iconset = new IconSet (pixbuf);

			/* add menu variant if we have it */
			IO.Stream menu_stream = System.Reflection.Assembly.GetCallingAssembly ().GetManifestResourceStream (name + "-16.png");
			if (menu_stream != null) {
				IconSource source = new IconSource ();
				source.Pixbuf = new Pixbuf (menu_stream);
				source.Size = IconSize.Menu;
				source.SizeWildcarded = false;
				iconset.AddSource (source);
			}
			
			factory.Add (name, iconset);
		}
	}
}
