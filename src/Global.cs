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
	public class Global : Gnome.Program
	{
		// Strings
		private static readonly string string_dbus_failed =
			Catalog.GetString ("Failed to export D-Bus object: {0}");		
		private static readonly string string_coverdb_failed =
			Catalog.GetString ("Failed to load the cover database: {0}\n\nExiting...");
		private static readonly string string_songdb_failed =
			Catalog.GetString ("Failed to load the song database: {0}\n\nExiting...");
	
		// Properties
		private static SongDatabase db;
		public static SongDatabase DB {
			get { return db; }
		}

		private static CoverDatabase cover_db;
		public static CoverDatabase CoverDB {
			get { return cover_db; }
		}	

		private static PlaylistWindow playlist;
		public static PlaylistWindow Playlist {
			get { return playlist; }
		}

		private static DBusLib.Player dbus_object = null;
		private static NotificationAreaIcon icon;
		private static MmKeys mmkeys;
		private static Gnome.Client session_client;

		public static void Main (string [] args)
		{
			Catalog.Init ("muine", Defines.GNOME_LOCALE_DIR);

			Gnome.Program program = new Gnome.Program ("muine",
								   Defines.VERSION,
								   Gnome.Modules.UI,
								   args);

			/* Try to find a running Muine */
			try {
				dbus_object = DBusLib.Player.FindInstance ();
			} catch {}

			if (dbus_object != null) {
				/* An instance already exists. Handle command line args
				   and exit. */
				if (args.Length > 0)
					ProcessCommandLine (args);
				else
					dbus_object.SetWindowVisible (true);
				
				Gdk.Global.NotifyStartupComplete ();
				
				Environment.Exit (0);
			} else {
				/* Register with D-Bus ASAP.
				   Actual hooking up to IPlayer happens later,
				   but this is safe, as D-Bus communication happens
				   through the main thread anyway. For now it is
				   just important to have the thing registered. */
				try {
					dbus_object = new DBusLib.Player ();				
					MuineDBusService.Instance.RegisterObject
						(dbus_object, "/org/gnome/Muine/Player");

				} catch (Exception e) {
					Console.WriteLine (string_dbus_failed, e.Message);
				}
			}

			/* Init GConf */
			Config.Init ();

			/* Init files */
			try {
				FileUtils.Init ();
			} catch (Exception e) {
				Error (e.Message);
			}

			/* Register stock icons */
			StockIcons.Initialize ();

			/* Set default window icon */
			SetDefaultWindowIcon ();

			/* Open cover database */
			try {
				cover_db = new CoverDatabase (3);
			} catch (Exception e) {
				Error (String.Format (string_coverdb_failed, e.Message));
			}

			/* Load song database */
			try {
				db = new SongDatabase (5);
			} catch (Exception e) {
				Error (String.Format (string_songdb_failed, e.Message));
			}

			db.Load ();

			/* Create playlist window */
			try {
				playlist = new PlaylistWindow ();
			} catch (PlayerException e) {
				Error (e.Message);
			}

			/* Hook up D-Bus object before loading any songs into the
			   playlist, to make sure that the song change gets emitted
			   to the bus */
			dbus_object.HookUp (playlist);

			/* Initialize plug-ins (also before loading any songs, to make
			   sure that the song change gets through to all the
			   plug-ins) */
			PluginManager pm = new PluginManager (playlist);

			/* Hook up multimedia keys */
			mmkeys = new MmKeys (playlist);

			/* Create tray icon */
			icon = new NotificationAreaIcon (playlist);

			/* Process command line options */
			bool opened_file = ProcessCommandLine (args);

			/* Load playlist */
			if (!opened_file)
				playlist.RestorePlaylist ();

			/* Show UI */
			playlist.Run ();

			icon.Run ();

			/* put on the screen immediately */
			while (MainContext.Pending ())
				Gtk.Main.Iteration ();

			/* Now we load the album covers, and after that start the changes thread */
			cover_db.DoneLoading += new CoverDatabase.DoneLoadingHandler (OnCoversDoneLoading);
			
			cover_db.Load ();

			/* Hook up to the session manager */
			session_client = Gnome.Global.MasterClient ();

			session_client.Die += new EventHandler (OnDieEvent);
			session_client.SaveYourself += new Gnome.SaveYourselfHandler (OnSaveYourselfEvent);

			/* And run */
			Application.Run ();
		}

		private static bool ProcessCommandLine (string [] args)
		{
			bool opened_file = false;

			for (int i = 0; i < args.Length; i++) {
				System.IO.FileInfo finfo = new System.IO.FileInfo (args [i]);
			
				if (!finfo.Exists)
					continue;

				if (FileUtils.IsPlaylist (args [i]))
					/* load as playlist */
					dbus_object.OpenPlaylist (finfo.FullName);
				else {
					/* load as music file */
					if (i == 0)
						dbus_object.PlayFile (finfo.FullName);
					else
						dbus_object.QueueFile (finfo.FullName);
				}

				opened_file = true;
			}

			return opened_file;
		}
		
		private static void SetDefaultWindowIcon ()
		{
			Pixbuf [] default_icon_list = new Pixbuf [1];
			default_icon_list [0] = new Pixbuf (null, "muine.png");
			Gtk.Window.DefaultIconList = default_icon_list;
		}

		private static void Error (string message)
		{
			new ErrorDialog (message);

			Environment.Exit (1);
		}

		private static void OnCoversDoneLoading ()
		{
			/* covers done loading, start the changes thread */
			db.CheckChanges ();
		}

		private static void OnDieEvent (object o, EventArgs args)
		{
			Exit ();
		}

		private static void OnSaveYourselfEvent (object o, Gnome.SaveYourselfArgs args)
		{
			/* FIXME */
			string [] argv = { "muine" };

			session_client.SetRestartCommand (1, argv);
		}

		public static void Exit ()
		{
			Environment.Exit (0);
		}
	}
}
