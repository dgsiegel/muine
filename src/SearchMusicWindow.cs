/*
 * Copyright © 2004 Viet Yen Nguyen <nguyen@cs.utwente.nl>
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

public class SearchMusicWindow
{
	[Glade.Widget]
	Window window;
	[Glade.Widget]
	Button search_button;
	[Glade.Widget]
	Button cancel_button;
	
	public SearchMusicWindow (Window parent)
	{
		Glade.XML gxml = new Glade.XML (null, "SearchMusicWindow.glade", "window", null);
		gxml.Autoconnect (this);

		window.TransientFor = parent;
	}

	public void Run ()
	{
		window.Visible = true;
	}

	private void HandleCancelClicked (object o, EventArgs a) 
	{
		window.Destroy ();
	}

	private void HandleSearchClicked (object o, EventArgs a) 
	{
		window.Destroy ();
		
		if (ImportHomeFolderEvent != null)
			ImportHomeFolderEvent ();
	}

	public delegate void ImportHomeFolderEventHandler ();
	public event ImportHomeFolderEventHandler ImportHomeFolderEvent;
}
