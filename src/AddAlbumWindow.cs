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

using System;

using Gtk;
using GtkSharp;
using GLib;

public class AddAlbumWindow
{
	[Glade.Widget]
	Window window;
	[Glade.Widget]
	Entry search_entry;
	[Glade.Widget]
	Image play_button_image;
	[Glade.Widget]
	Image queue_button_image;
	[Glade.Widget]
	ScrolledWindow scrolledwindow;
	
	public AddAlbumWindow (Window parent)
	{
		Glade.XML gxml = new Glade.XML (null, "AddWindow.glade", "window", null);
		gxml.Autoconnect (this);

		window.TransientFor = parent;

		window.Title = "Add Album";

		play_button_image.SetFromStock ("muine-play", IconSize.Button);
		queue_button_image.SetFromStock ("muine-queue", IconSize.Button);
	}

	public void Run ()
	{
		window.Visible = true;
	}

	public delegate void SeekEventHandler (int sec);
	public event SeekEventHandler SeekEvent;

	private void HandleWindowResponse (object o, EventArgs a)
	{
		window.Visible = false;

		ResponseArgs args = (ResponseArgs) a;

		switch (args.ResponseId) {
		case 1: /* Play */
			break;
		case 2: /* Queue */
			break;
		default:
			return;
		}
	}

	private void HandleWindowDeleteEvent (object o, EventArgs a)
	{
		window.Visible = false;

		DeleteEventArgs args = (DeleteEventArgs) a;

		args.RetVal = true;
	}

	private void HandleSearchEntryChanged (object o, EventArgs args)
	{
	}
}
