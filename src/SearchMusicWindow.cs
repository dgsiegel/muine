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
using System.IO;

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
	[Glade.Widget]
	Label title_label;
	[Glade.Widget]
	Label label;

	private DirectoryInfo dinfo;
	
	public SearchMusicWindow (Window parent)
	{
		Glade.XML gxml = new Glade.XML (null, "SearchMusicWindow.glade", "window", null);
		gxml.Autoconnect (this);

		window.TransientFor = parent;

		MarkupUtils.LabelSetMarkup (title_label, 0, StringUtils.GetByteLength (title_label.Text), false, true, false);

		string dir = Environment.GetEnvironmentVariable ("HOME");
		if (dir.EndsWith ("/") == false)
			dir += "/";

		string folder_name;

		dinfo = new DirectoryInfo (dir + "Music/");
		if (dinfo.Exists)
			folder_name = "music";
		else {
			folder_name = "home";
			
			dinfo = new DirectoryInfo (dir);
		}

		label.Text = "No music has been imported yet. Shall I now search your " + folder_name + 
		             " folder for music? This may take a few minutes.";
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

		if (ImportFolderEvent != null)
			ImportFolderEvent (dinfo);
	}

	public delegate void ImportFolderEventHandler (DirectoryInfo dinfo);
	public event ImportFolderEventHandler ImportFolderEvent;
}
