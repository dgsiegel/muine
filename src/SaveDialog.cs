/*
 * Copyright (C) 2005 Tamara Roberson <foxxygirltamara@gmail.com>
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
using System.Collections;
using System.IO;
using Gtk;
using Mono.Posix;

namespace Muine
{
	public class SaveDialog : FileSelector
	{	
		// Constants
		// Constants :: GConf
		private const string GConfKeyDefaultPlaylistFolder = "/apps/muine/default_playlist_folder";
		
		// Strings
		private static readonly string string_title =
			Catalog.GetString ("Save Playlist");
		private static readonly string string_save_default =
			Catalog.GetString ("Untitled");
		private static readonly string string_overwrite =
			Catalog.GetString ("File {0} will be overwritten.\n" +
					   "If you choose yes, the contents will be lost.\n\n" +
					   "Do you want to continue?");

		// Constructor
		public SaveDialog () 
		: base (string_title, Global.Playlist, FileChooserAction.Save, GConfKeyDefaultPlaylistFolder)
		{
			base.CurrentName = string_save_default;

			string fn = base.GetFile ();

			if (fn.Length == 0)
				return;

			// make sure the extension is ".m3u"
			if (!FileUtils.IsPlaylist (fn))
				fn += ".m3u";

			if (FileUtils.Exists (fn)) {
				YesNoDialog d = new YesNoDialog (String.Format (string_overwrite, FileUtils.MakeHumanReadable (fn)), 
								 Global.Playlist);
				if (!d.GetAnswer ()) // user said don't overwrite
					return;
			}
			
			Global.Playlist.SavePlaylist (fn, false, false);
		}
	}
}
