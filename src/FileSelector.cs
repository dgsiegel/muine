/*
 * Copyright (C) 2004 Jorn Baayen <jbaayen@gnome.org>
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
using GLib;

namespace Muine
{
	public class FileSelector : FileChooserDialog
	{
		// Constants
		private const string GConfDefaultStartDir = "~";
		
		// Variables
		private string gconf_path;

		// Constructor
		public FileSelector (string title, FileChooserAction action, string gcp) : base (title, Global.Playlist, action, "gnome-vfs")
		{
			LocalOnly = false;

			AddButton (Stock.Cancel, ResponseType.Cancel);

			switch (action) {
			case FileChooserAction.Open:
				AddButton (Stock.Open, ResponseType.Ok);
				break;
			
			case FileChooserAction.Save:
				AddButton (Stock.Save, ResponseType.Ok);
				break;
			
			default:
				break;
			}
			
			DefaultResponse = ResponseType.Ok;

			gconf_path = gcp;

			string start_dir = (string) Config.Get (gconf_path, GConfDefaultStartDir);

			start_dir = start_dir.Replace ("~",
				FileUtils.UriFromLocalPath (FileUtils.HomeDirectory));

			SetCurrentFolderUri (start_dir);

			base.Response += new ResponseHandler (OnResponse);
		}

		// Handlers
		// Handlers :: OnResponse
		private void OnResponse (object o, ResponseArgs args)
		{
			if (args.ResponseId == ResponseType.Ok)
				Config.Set (gconf_path, CurrentFolderUri);
		}
	}
}
