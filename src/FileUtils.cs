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
using System.Runtime.InteropServices;

using Gnome.Vfs;

public class FileUtils
{
	public static bool IsFromRemovableMedia (string fn)
	{
		return (fn.StartsWith ("/mnt/") ||
			fn.StartsWith ("file:///mnt/") ||
			fn.StartsWith ("/media/") ||
			fn.StartsWith ("file:///media/"));
	}

	public static bool IsPlaylist (string fn)
	{
		string ext = Path.GetExtension (fn).ToLower ();

		return (ext == ".m3u");
	}

	public static bool Exists (string fn)
	{
		Gnome.Vfs.Uri u = new Gnome.Vfs.Uri (fn);

		return u.Exists;
	}

	public static string MakeHumanReadable (string fn)
	{
		System.Uri u = new System.Uri (fn);

		string ret = u.ToString ();

		if (ret.StartsWith ("file://"))
			ret = ret.Substring ("file://".Length);

		return ret;
	}

	/* these two go away once we have vfs support everywhere */
	[DllImport ("libgnomevfs-2-0.dll")]
	private static extern IntPtr gnome_vfs_get_local_path_from_uri (string str);

	public static string LocalPathFromUri (string uri)
	{
		IntPtr p = gnome_vfs_get_local_path_from_uri (uri);

		if (p == IntPtr.Zero)
			return null;
		else
			return GLib.Marshaller.PtrToStringGFree (p);
	}

	public static string UriFromLocalPath (string uri)
	{
		return "file://" + uri;
	}
}
