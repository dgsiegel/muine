/*
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
using System.IO;

using Gtk;
using GLib;
using Gdk;

using DBus;

using Mono.Posix;

namespace Muine
{
	public sealed class Global : Gnome.Program
	{
		// Strings
		private static readonly string string_dbus_failed =
			Catalog.GetString ("Failed to export D-Bus object: {0}");		

		private static readonly string string_coverdb_failed =
			Catalog.GetString ("Failed to load the cover database: {0}");

		private static readonly string string_songdb_failed =
			Catalog.GetString ("Failed to load the song database: {0}");

		private static readonly string string_error_initializing =
			Catalog.GetString ("Error initializing Muine.");
	
		// Variables
		private static SongDatabase   db;
		private static CoverDatabase  cover_db;
		private static PlaylistWindow playlist;
		private static Actions        actions;

		private static DBusLib.Player       dbus_object = null;
		private static NotificationAreaIcon icon;
		private static Gnome.Client         session_client;
		
		// Properties
		// Properties :: DB (get;)
		public static SongDatabase DB {
			get { return db; }
		}

		// Properties :: CoverDB (get;)
		public static CoverDatabase CoverDB {
			get { return cover_db; }
		}	

		// Properties :: Playlist (get;)
		public static PlaylistWindow Playlist {
			get { return playlist; }
		}

		// Properties :: Actions (get;)
		public static Actions Actions {
			get { return actions; }
		}

		// Main
		public static void Main (string [] args)
		{
			Catalog.Init ("muine", Defines.GNOME_LOCALE_DIR);

			new Gnome.Program ("muine", Defines.VERSION, Gnome.Modules.UI, args);

			// Try to find a running Muine
			try {
				dbus_object = DBusLib.Player.FindInstance ();
			} catch {
			}

			// Check if an instance of Muine is already running
			if (dbus_object != null) {

				// Handle command line args and exit.
				if (args.Length > 0)
					ProcessCommandLine (args);
				else
					dbus_object.SetWindowVisible (true);
				
				Gdk.Global.NotifyStartupComplete ();

				return;
			}

			// Initialize D-Bus
			//	We initialize here but don't connect to it until later.
			try {
				dbus_object = new DBusLib.Player ();

				DBusService.Instance.RegisterObject (dbus_object, 
					"/org/gnome/Muine/Player");

			} catch (Exception e) {
				Console.WriteLine (string_dbus_failed, e.Message);
			}

			// Init GConf
			Config.Init ();

			// Init files
			try {
				FileUtils.Init ();

			} catch (Exception e) {
				Error (e.Message);
			}

			// Register stock icons
			StockIcons.Initialize ();

			// Set default window icon
			SetDefaultWindowIcon ();

			// Open cover database
			try {
				cover_db = new CoverDatabase (3);

			} catch (Exception e) {
				Error (String.Format (string_coverdb_failed, e.Message));
			}

			cover_db.DoneLoading += new CoverDatabase.DoneLoadingHandler (OnCoversDoneLoading);

			// Load song database
			try {
				db = new SongDatabase (6);

			} catch (Exception e) {
				Error (String.Format (string_songdb_failed, e.Message));
			}

			db.Load ();

			// Setup Actions
			actions = new Actions ();

			// Create playlist window
			try {
				playlist = new PlaylistWindow ();
			} catch (PlayerException e) {
				Error (e.Message);
			}

			// D-Bus
			// 	Hook up D-Bus object before loading any songs into the
			//	playlist, to make sure that the song change gets emitted
			//	to the bus 
			dbus_object.HookUp (playlist);
		
			// PluginManager
			//	Initialize plug-ins (also before loading any songs, to make
			//	sure that the song change gets through to all the plug-ins)
			new PluginManager (playlist);

			// Hook up multimedia keys
			new MmKeys (playlist);

			// Create tray icon
			icon = new NotificationAreaIcon (playlist);

			// Process command line options
			bool opened_file = ProcessCommandLine (args);

			// Load playlist
			if (!opened_file)
				playlist.RestorePlaylist ();

			// Show UI
			playlist.Run ();
			icon.Run ();

			while (MainContext.Pending ())
				Gtk.Main.Iteration ();

			// Load Covers
			cover_db.Load ();

			// Hook up to the session manager
			session_client = Gnome.Global.MasterClient ();
			session_client.Die          += new EventHandler              (OnDieEvent         );
			session_client.SaveYourself += new Gnome.SaveYourselfHandler (OnSaveYourselfEvent);

			// Run!
			Application.Run ();
		}

		// Methods
		// Methods :: Public :: Exit
		public static void Exit ()
		{
			Application.Quit ();
		}

		// Methods :: Private
		// Methods :: Private :: ProcessCommandLine 
		private static bool ProcessCommandLine (string [] args)
		{
			bool opened_file = false;

			foreach (string arg in args) {
				System.IO.FileInfo finfo = new System.IO.FileInfo (arg);
				
				if (!finfo.Exists)
					continue;

				opened_file = true;

				// See the file is a Playlist
				if (FileUtils.IsPlaylist (arg)) { // load as playlist
					dbus_object.OpenPlaylist (finfo.FullName);
					continue;
				}
				
				// Must be a music file
				//	TODO: Run a filetype check
				
				// If it's the first song, start playing it
				if (arg == args [0]) {
					dbus_object.PlayFile (finfo.FullName);
					continue;
				}
				
				// Queue the song
				dbus_object.QueueFile (finfo.FullName);
			}

			return opened_file;
		}

		// Methods :: Private :: SetDefaultWindowIcon
		private static void SetDefaultWindowIcon ()
		{
			Pixbuf [] default_icon_list = { new Pixbuf (null, "muine.png") };
			Gtk.Window.DefaultIconList = default_icon_list;
		}

		// Methods :: Private :: Error
		private static void Error (string message)
		{
			new ErrorDialog (string_error_initializing, message);

			Environment.Exit (1);
		}

		// Handlers
		// Handlers :: OnCoversDoneLoading
		private static void OnCoversDoneLoading ()
		{
			// covers done loading, start the changes thread
			db.CheckChanges ();
		}

		// Handlers :: OnDieEvent
		private static void OnDieEvent (object o, EventArgs args)
		{
			Exit ();
		}

		// Handlers :: OnSaveYourselfEvent
		private static void OnSaveYourselfEvent (object o, Gnome.SaveYourselfArgs args)
		{
			// FIXME
			string [] argv = { "muine" };

			session_client.SetRestartCommand (1, argv);
		}
	}
}
