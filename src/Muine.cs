/*
 * Copyright (C) 2003, 2004 Jorn Baayen <jorn@nl.linux.org>
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

public class Muine : Gnome.Program
{
	private static PlaylistWindow playlist;

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

	private static GettextCatalog catalog;
	public static GettextCatalog Catalog {
		get {
			return catalog;
		}
	}

	private static Gnome.Client client;

	private static bool opened_playlist = false;

	public static void Main (string [] args)
	{
		catalog = new GettextCatalog ("muine");

		Muine muine = new Muine (args);

		Application.Run ();
	}

	public Muine (string [] args) : base ("muine", About.Version, Gnome.Modules.UI, args)
	{
		PlayerDBusObject dbo = null;

		/* Try to find a running Muine */
		try {
			Connection conn = Bus.GetSessionBus ();
			
			Service service = Service.Get (conn, "org.gnome.Muine");
			
			dbo = (PlayerDBusObject) service.GetObject
					(typeof (PlayerDBusObject), "/org/gnome/Muine/Player");

		} catch {}

		/* An instance already exists. Handle command line args and exit. */
		if (dbo != null)
		{
			ProcessCommandLine (args, dbo);
			Gdk.Global.NotifyStartupComplete ();
			Environment.Exit (0);
		}

		/* Init GConf */
		gconf_client = new GConf.Client ();

		/* Register stock icons */
		StockIcons.Initialize ();

		/* Set default window icon */
		SetDefaultWindowIcon ();

		/* Start the action thread */
		action_thread = new ActionThread ();

		/* Load cover database */
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

		DB.Load ();

		/* Create playlist window */
		playlist = new PlaylistWindow ();

		/* Process command line options */
		ProcessCommandLine (args, null);

		/* Load playlist */
		if (!opened_playlist)
			playlist.RestorePlaylist ();

		/* Show playlist window */
		playlist.Run ();

		/* Now we load the album covers, and after that start the changes thread */
		CoverDB.DoneLoading += new CoverDatabase.DoneLoadingHandler (HandleCoversDoneLoading);
		
		CoverDB.Load ();

		/* And finally, check if this is the first start */
		/* FIXME we dont do this for now as the issue isn't sorted out yet */
		//playlist.CheckFirstStartUp ();

		/* Hook up to the session manager */
		client = Gnome.Global.MasterClient ();

		client.Die += new EventHandler (HandleDieEvent);
		client.SaveYourself += new Gnome.SaveYourselfHandler (HandleSaveYourselfEvent);
	}

	private void ProcessCommandLine (string [] args, PlayerDBusObject dbo)
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

	private void HandleCoversDoneLoading ()
	{
		/* covers done loading, start the changes thread */
		db.CheckChanges ();
	}

	private void HandleDieEvent (object o, EventArgs args)
	{
		Exit ();
	}

	private void HandleSaveYourselfEvent (object o, Gnome.SaveYourselfArgs args)
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
