/*
 * Copyright © 2004 Jorn Baayen <jorn@nl.linux.org>
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

public class FileSelector : FileSelection
{
	private string gconf_path;

	public FileSelector (string title, string gcp) : base (title)
	{
		gconf_path = gcp;

		string start_dir;
		try {
			start_dir = (string) Muine.GConfClient.Get (gconf_path);
		} catch {
			start_dir = "~";
		}

		start_dir.Replace ("~", Environment.GetEnvironmentVariable ("HOME"));

		if (start_dir.EndsWith ("/") == false)
			start_dir += "/";

		Filename = start_dir;
	}

	public string GetFile (out bool exists)
	{
		if (Run () != (int) ResponseType.Ok) {
			Destroy ();

			exists = false;

			return "";
		}
		
		FileInfo finfo = new FileInfo (Filename);

		Muine.GConfClient.Set (gconf_path, finfo.DirectoryName + "/");

		string ret = Filename;

		exists = finfo.Exists;

		Destroy ();

		return ret;
	}
}
