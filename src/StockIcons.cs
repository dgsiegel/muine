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

using System;
using System.IO;

public class StockIcons 
{
	private static string [] stock_icons = {
		"muine-add-album",
		"muine-tray-playing",
		"muine-tray-paused",
		"muine-default-cover",
		"muine-cover-downloading",
		"muine-playing",
		"muine-paused"
	};

	private static string [] icon_theme_icons = {
		"stock_media-fwd",
		"stock_media-next",
		"stock_media-pause",
		"stock_media-play",
		"stock_media-prev",
		"stock_media-rew",
		"stock_shuffle",
		"stock_timer",
		"volume-zero",
		"volume-min",
		"volume-medium",
		"volume-max"
	};

	public static IconSize AlbumCoverSize;

	public static void Initialize ()
	{
		IconFactory factory = new IconFactory ();
		factory.AddDefault ();

		foreach (string name in stock_icons) {
			Pixbuf pixbuf = new Pixbuf (null, name + ".png");
			IconSet iconset = new IconSet (pixbuf);

			/* add menu variant if we have it */
			Stream menu_stream = System.Reflection.Assembly.GetCallingAssembly ().GetManifestResourceStream (name + "-16.png");
			if (menu_stream != null) {
				IconSource source = new IconSource ();
				source.Pixbuf = new Pixbuf (menu_stream);
				source.Size = IconSize.Menu;
				source.SizeWildcarded = false;

				iconset.AddSource (source);
			}
			
			factory.Add (name, iconset);
		}

		foreach (string name in icon_theme_icons) {
			IconSet iconset = new IconSet ();
			IconSource iconsource = new IconSource ();

			iconsource.IconName = name;

			iconset.AddSource (iconsource);

			factory.Add (name, iconset);
		}

		/* register cover image icon size */
		AlbumCoverSize = Icon.SizeRegister ("muine-album-cover-size",
						    CoverDatabase.AlbumCoverSize,
						    CoverDatabase.AlbumCoverSize);
	}
}
