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

using Mono.Posix;

namespace Muine
{
	public sealed class FileUtils
	{
		// Constants
		private const string playlist_filename = "playlist.m3u";
		private const string songsdb_filename = "songs.db";
		private const string coversdb_filename = "covers.db";
		private const string plugin_dirname = "plugins";

		private readonly static DateTime date_time_1970 = 
			new DateTime (1970, 1, 1, 0, 0, 0, 0);

		// Strings
		private static readonly string string_init_config_failed = 
			Catalog.GetString ("Failed to initialize the configuration folder: {0}\n\nExiting...");
		private static readonly string string_init_temp_failed =
			Catalog.GetString ("Failed to initialize the temporary files folder: {0}\n\nExiting...");

		// Variables
		private static string home_directory;
		private static string config_directory;
		private static string playlist_file;
		private static string songsdb_file;
		private static string coversdb_file;
		private static string user_plugin_directory;
		private static string temp_directory;

		// Methods
		// Methods :: Public
		// Methods :: Public :: Init
		public static void Init ()
		{
			home_directory = Environment.GetEnvironmentVariable ("HOME");
			
			try {
				config_directory = Path.Combine (Gnome.User.DirGet (), "muine");
				CreateDirectory (config_directory);

			} catch (Exception e) {
				throw new Exception (String.Format (string_init_config_failed, e.Message));
			}

			playlist_file = Path.Combine (config_directory, playlist_filename);
			songsdb_file  = Path.Combine (config_directory, songsdb_filename );
			coversdb_file = Path.Combine (config_directory, coversdb_filename);
			user_plugin_directory = Path.Combine (config_directory, plugin_dirname);
			
			try {
				temp_directory = Path.Combine (System.IO.Path.GetTempPath (),
								"muine-" + Environment.UserName);
				CreateDirectory (temp_directory);

			} catch (Exception e) {
				throw new Exception (String.Format (string_init_temp_failed, e.Message));
			}
		}

		// Properties
		// Properties :: HomeDirectory (get;)
		public static string HomeDirectory {
			get { return home_directory; }
		}

		// Properties :: ConfigDirectory (get;)		
		public static string ConfigDirectory {
			get { return config_directory; }
		}
		
		// Properties :: PlaylistFile (get;)
		public static string PlaylistFile {
			get { return playlist_file; }
		}

		// Properties :: SongsDBFile (get;)
		public static string SongsDBFile {
			get { return songsdb_file; }
		}

		// Properties :: CoversDBFile (get;)
		public static string CoversDBFile {
			get { return coversdb_file; }
		}

		// Properties :: SystemPluginDirectory (get;)
		public static string SystemPluginDirectory {
			get { return Defines.PLUGIN_DIR; }
		}

		// Properties :: UserPluginDirectory (get;)
		public static string UserPluginDirectory {
			get { return user_plugin_directory; }
		}

		// Properties :: TempDirectory (get;)
		public static string TempDirectory {
			get { return temp_directory; }
		}
		
		// Methods
		// Methods :: Public
		// Methods :: Public :: IsFromRemovableMedia
		public static bool IsFromRemovableMedia (string fn)
		{
			return (fn.StartsWith ("/mnt/") ||
				fn.StartsWith ("file:///mnt/") ||
				fn.StartsWith ("/media/") ||
				fn.StartsWith ("file:///media/"));
		}

		// Methods :: Public :: IsPlaylist
		public static bool IsPlaylist (string fn)
		{
			string ext = Path.GetExtension (fn).ToLower ();
			return (ext == ".m3u");
		}

		// Methods :: Public :: Exists
		public static bool Exists (string fn)
		{
			Gnome.Vfs.Uri u = new Gnome.Vfs.Uri (fn);
			return u.Exists;
		}

		// Methods :: Public :: MakeHumanReadable
		public static string MakeHumanReadable (string fn)
		{
			System.Uri u = new System.Uri (fn);
			string ret = u.ToString ();

			if (ret.StartsWith ("file://"))
				ret = ret.Substring ("file://".Length);

			return ret;
		}

		// Methods :: Public :: MTimeToTicks
		public static long MTimeToTicks (int mtime)
		{
			return (long) (mtime * 10000000L) + date_time_1970.Ticks;
		}

		// Methods :: Public :: CreateDirectory
		private static void CreateDirectory (string dir)
		{
			DirectoryInfo dinfo = new DirectoryInfo (dir);
			if (dinfo.Exists)
				return;
					
			dinfo.Create ();
		}

		// Methods :: Public :: LocalPathFromUri
		// 	TODO: Replace with GnomeVfs#
		[DllImport ("libgnomevfs-2-0.dll")]
		private static extern IntPtr gnome_vfs_get_local_path_from_uri (string str);

		public static string LocalPathFromUri (string uri)
		{
			IntPtr p = gnome_vfs_get_local_path_from_uri (uri);

			return (p == IntPtr.Zero)
				? null
				: GLib.Marshaller.PtrToStringGFree (p);
		}

		// Methods :: Public :: UriFromLocalPath
		//	TODO: Replace with GnomeVfs#
		public static string UriFromLocalPath (string uri)
		{
			return "file://" + uri;
		}

		// Methods :: Public ::: IsRemove
		// 	TODO: Make portable
		public static bool IsRemote (string uri)
		{
			return (uri [0] != '/' && !uri.StartsWith ("file://"));
		}
	}
}
