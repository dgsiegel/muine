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
	public class ImportDialog : FileChooserDialog
	{	
		// Constants
		// Constants :: GConf
		private const string GConfKeyImportFolder = "/apps/muine/default_import_folder";
		private const string GConfDefaultImportFolder = "~";

		// Strings		
		private static readonly string string_title =
			Catalog.GetString ("Import Folder");
		private static readonly string string_button =
			Catalog.GetString ("_Import");

		// Constructor
		public ImportDialog () 
		: base (string_title, Global.Playlist, FileChooserAction.SelectFolder)
		{
			base.LocalOnly = true;
			base.SelectMultiple = true;
			base.AddButton (Stock.Cancel, ResponseType.Cancel);
			base.AddButton (string_button, ResponseType.Ok);
			base.DefaultResponse = ResponseType.Ok;
			
			string start_dir = (string) Config.Get (GConfKeyImportFolder, GConfDefaultImportFolder);

			start_dir = start_dir.Replace ("~", FileUtils.HomeDirectory);

			base.SetCurrentFolder (start_dir);

			if (base.Run () != (int) ResponseType.Ok) {
				base.Destroy ();
				return;
			}

			base.Visible = false;

			Config.Set (GConfKeyImportFolder, base.CurrentFolder);

			ArrayList new_dinfos = new ArrayList ();
			foreach (string dir in base.Filenames) {
				DirectoryInfo dinfo = new DirectoryInfo (dir);
				
				if (!dinfo.Exists)
					continue;

				new_dinfos.Add (dinfo);
			}

			if (new_dinfos.Count > 0)
				Global.DB.AddFolders (new_dinfos);

			base.Destroy ();
		}
	}
}