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

public class Muine : Gnome.Program
{
	// directories
	private static void CreateDirectory (string dir)
	{
		DirectoryInfo dinfo = new DirectoryInfo (dir);
		if (dinfo.Exists)
			return;
				
		dinfo.Create ();
	}
	
	private static string home_directory = Environment.GetEnvironmentVariable ("HOME");
	public static string HomeDirectory {
		get {
			return home_directory;
		}
	}
	
	// We set these proper in the constructor because we use 
	// Gnome.User.DirGet () and Gtk has to be initialized for that
	private static string config_directory;
	public static string ConfigDirectory {
		get {
			return config_directory;
		}
		
		set {
			config_directory = value;
			CreateDirectory (config_directory);
			playlist_file = Path.Combine (config_directory, playlist_filename);
			songsdb_file = Path.Combine (config_directory, songsdb_filename);
			coversdb_file = Path.Combine (config_directory, coversdb_filename);
			user_plugin_directory = Path.Combine (config_directory, plugin_dirname);
		}
	}
	
	private static string playlist_file;
	private const string playlist_filename = "playlist.m3u";
	public static string PlaylistFile {
		get {
			return playlist_file;
		}
	}

	private static string songsdb_file;
	private const string songsdb_filename = "songs.db";
	public static string SongsDBFile {
		get {
			return songsdb_file;
		}
	}

	private static string coversdb_file;
	private const string coversdb_filename = "covers.db";
	public static string CoversDBFile {
		get {
			return coversdb_file;
		}
	}

	public static string SystemPluginDirectory {
		get {
			return Defines.PLUGIN_DIR;
		}
	}

	private const string plugin_dirname = "plugins";
	private static string user_plugin_directory;
	public static string UserPluginDirectory {
		get {
			return user_plugin_directory;
		}
	}
	
	// DnD targets
	public enum TargetType {
		UriList,
		Uri,
		SongList,
		AlbumList,
		ModelRow
	};

	public static readonly TargetEntry TargetUriList = 
		new TargetEntry ("text/uri-list", 0, (uint) TargetType.UriList);
		
	public static readonly TargetEntry TargetGnomeIconList = 
		new TargetEntry ("x-special/gnome-icon-list", 0, (uint) TargetType.UriList);
		
	public static readonly TargetEntry TargetNetscapeUrl = 
		new TargetEntry ("_NETSCAPE_URL", 0, (uint) TargetType.Uri);
		
	public static readonly TargetEntry TargetMuineAlbumList = 
		new TargetEntry ("MUINE_ALBUM_LIST", TargetFlags.App, (uint) TargetType.AlbumList);

	public static readonly TargetEntry TargetMuineSongList = 
		new TargetEntry ("MUINE_SONG_LIST", TargetFlags.App, (uint) TargetType.SongList);
		
	public static readonly TargetEntry TargetMuineTreeModelRow = 
		new TargetEntry ("MUINE_TREE_MODEL_ROW", TargetFlags.Widget, (uint) TargetType.ModelRow);
	
	// objects
	private static PlaylistWindow playlist;

	private static NotificationAreaIcon icon;

	private static MmKeys mmkeys;

	private static GConf.Client gconf_client;

	private static SongDatabase db;
	public static SongDatabase DB {
		get {
			return db;
		}
	}

	private static CoverDatabase cover_db;
	public static CoverDatabase CoverDB {
		get {
			return cover_db;
		}
	}	

	private static ActionThread action_thread; 
	public static ActionThread ActionThread {
		get {
			return action_thread;
		}
	}

	private static Gnome.Client client;

	private static bool opened_playlist = false;

	public static void Main (string [] args)
	{
		Muine muine = new Muine (args);

		Application.Run ();
	}

	public Muine (string [] args) : base ("muine", Defines.VERSION, Gnome.Modules.UI, args)
	{
		Catalog.Init ("muine", Defines.GNOME_LOCALE_DIR);

		MuineDBusLib.Player dbo = null;

		/* Try to find a running Muine */
		try {
			dbo = MuineDBusLib.Player.FindInstance ();
		} catch {}

		if (dbo != null) {
			/* An instance already exists. Handle command line args
			   and exit. */
			ProcessCommandLine (args, dbo);
			
			Gdk.Global.NotifyStartupComplete ();
			
			Environment.Exit (0);
		} else {
			/* Register with D-Bus ASAP.
			   Actual hooking up to IPlayer happens later,
			   but this is safe, as D-Bus communication happens
			   through the main thread anyway. For now it is
			   just important to have the thing registered. */
			try {
				dbo = new MuineDBusLib.Player ();
			
				MuineDBusService.Instance.RegisterObject
					(dbo, "/org/gnome/Muine/Player");
			} catch (Exception e) {
				Console.WriteLine (Catalog.GetString ("Failed to export D-Bus object: {0}"), e.Message);
			}
		}

		/* Init GConf */
		gconf_client = new GConf.Client ();

		/* Register stock icons */
		StockIcons.Initialize ();

		/* Set default window icon */
		SetDefaultWindowIcon ();

		/* Setup config directory (~/.gnome2/muine) */
		try {
			ConfigDirectory = Path.Combine (Gnome.User.DirGet (), "muine");
		} catch (Exception e) {
			new ErrorDialog (String.Format (Catalog.GetString ("Failed to initialize the configuration folder: {0}\n\nExiting..."), e.Message));

			Exit ();
		}
		
		/* Start the action thread */
		action_thread = new ActionThread ();

		/* Open cover database */
		try {
			cover_db = new CoverDatabase (2);
		} catch (Exception e) {
			new ErrorDialog (String.Format (Catalog.GetString ("Failed to load the cover database: {0}\n\nExiting..."), e.Message));

			Exit ();
		}

		/* Load song database */
		try {
			db = new SongDatabase (4);
		} catch (Exception e) {
			new ErrorDialog (String.Format (Catalog.GetString ("Failed to load the song database: {0}\n\nExiting..."), e.Message));

			Exit ();
		}

		db.Load ();

		/* Create playlist window */
		playlist = new PlaylistWindow ();

		/* Hook up D-Bus object before loading any songs into the
		   playlist, to make sure that the song change gets emitted
		   to the bus */
		dbo.HookUp (playlist);

		/* Initialize plug-ins (also before loading any songs, to make
		   sure that the song change gets through to all the
		   plug-ins) */
		PluginManager pm = new PluginManager (playlist);

		/* Hook up multimedia keys */
		mmkeys = new MmKeys (playlist);

		/* Create tray icon */
		icon = new NotificationAreaIcon (playlist);

		/* Process command line options */
		ProcessCommandLine (args, null);

		/* Load playlist */
		if (!opened_playlist)
			playlist.RestorePlaylist ();

		/* Show UI */
		Run ();

		/* Now we load the album covers, and after that start the changes thread */
		cover_db.DoneLoading += new CoverDatabase.DoneLoadingHandler (OnCoversDoneLoading);
		
		cover_db.Load ();

		/* And finally, check if this is the first start */
		/* FIXME we dont do this for now as the issue isn't sorted out yet */
		//playlist.CheckFirstStartUp ();

		/* Hook up to the session manager */
		client = Gnome.Global.MasterClient ();

		client.Die += new EventHandler (OnDieEvent);
		client.SaveYourself += new Gnome.SaveYourselfHandler (OnSaveYourselfEvent);
	}

	private new void Run ()
	{
		playlist.Run ();

		icon.Run ();

		/* put on the screen immediately */
		while (MainContext.Pending ())
			Gtk.Main.Iteration ();
	}

	private void ProcessCommandLine (string [] args, MuineDBusLib.Player dbo)
	{
		for (int i = 0; i < args.Length; i++) {
			System.IO.FileInfo finfo = new System.IO.FileInfo (args [i]);
		
			if (finfo.Exists) {
				if (FileUtils.IsPlaylist (args [i])) {
					/* load as playlist */
					if (dbo != null)
						dbo.OpenPlaylist (finfo.FullName);
					else
						playlist.OpenPlaylist (finfo.FullName);
				} else {
					/* load as music file */
					if (i == 0) {
						if (dbo != null)
							dbo.PlayFile (finfo.FullName);
						else
							playlist.PlayFile (finfo.FullName);
					} else {
						if (dbo != null)
							dbo.QueueFile (finfo.FullName);
						else
							playlist.QueueFile (finfo.FullName);
					}
				}

				opened_playlist = true;
			}
		}

		if (dbo != null && args.Length == 0)
			dbo.SetWindowVisible (true);
	}
	
	private void SetDefaultWindowIcon ()
	{
		Pixbuf [] default_icon_list = new Pixbuf [1];
		default_icon_list [0] = new Pixbuf (null, "muine.png");
		Gtk.Window.DefaultIconList = default_icon_list;
	}

	private void OnCoversDoneLoading ()
	{
		/* covers done loading, start the changes thread */
		db.CheckChanges ();
	}

	private void OnDieEvent (object o, EventArgs args)
	{
		Exit ();
	}

	private void OnSaveYourselfEvent (object o, Gnome.SaveYourselfArgs args)
	{
		/* FIXME */
		string [] argv = { "muine" };

		client.SetRestartCommand (1, argv);
	}

	public static void Exit ()
	{
		//Application.Quit ();
		Environment.Exit (0);
	}
	
	public static object GetGConfValue (string key)
	{
	       return gconf_client.Get (key);
	}
	
	public static object GetGConfValue (string key, object default_val)
        {
                object val;

                try {
                        val = GetGConfValue (key);
                } catch {
                        val = default_val;
                }

                return val;
        }
        
        public static void SetGConfValue (string key, object val)
        {
        	gconf_client.Set (key, val);        	
        }
}
