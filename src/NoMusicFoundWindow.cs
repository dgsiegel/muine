/*
 * Copyright (C) 2004 Việt Yên Nguyễn <nguyen@cs.utwente.nl>
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

// UNUSED

using System;
using System.IO;

using Gtk;
using GLib;

namespace Muine
{
	public class NoMusicFoundWindow
	{
		[Glade.Widget] private Window      window;
		[Glade.Widget] private RadioButton empty_radiobutton;

		// Constructor
		public NoMusicFoundWindow (Window parent)
		{
			Glade.XML gxml = new Glade.XML (null, "NoMusicFoundWindow.glade", "window", null);
			gxml.Autoconnect (this);

			window.TransientFor = parent;
			window.Visible = true;
		}

		// Methods
		// Methods :: Private
		// Methods :: Private :: CreateEmptyMusicCollection
		private void CreateEmptyMusicCollection ()
		{
			// get $HOME from environment
			string homeDirectory = Environment.GetEnvironmentVariable ("HOME");
			if (!homeDirectory.EndsWith ("/"))
				homeDirectory += "/";
			
			// retrieve information about $HOME/Music and $HOME/Music/Playlists
			DirectoryInfo musicdir = new DirectoryInfo (homeDirectory + "Music/");
			DirectoryInfo playlistsdir = new DirectoryInfo (homeDirectory + "Music/Playlists/");

			if (!musicdir.Exists) 
				musicdir.Create ();

			if (!playlistsdir.Exists) 
				playlistsdir.Create ();

			Muine.DB.AddWatchedFolder (musicdir.FullName);
		}
		
		// Handlers
		// Handlers :: OnOkClicked
		private void OnOkClicked (object o, EventArgs a) 
		{
			window.Destroy ();

			if (empty_radiobutton.Active) 
				CreateEmptyMusicCollection ();
		}
	}
}
