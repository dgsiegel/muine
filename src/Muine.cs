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

public class Muine : Gnome.Program
{
	private static PlaylistWindow playlist;

	public static GConf.Client GConfClient;

	public static SongDatabase DB;

	public static CoverDatabase CoverDB;

	public static ActionThread ActionThread;

	public static GettextCatalog Catalog;

	private static MessageConnection conn;

	public static void Main (string [] args)
	{
		Catalog = new GettextCatalog ("muine");

		Muine muine = new Muine (args);

		Application.Run ();
	}

	public Muine (string [] args) : base ("muine", About.Version, Gnome.Modules.UI, args)
	{
		/* Create message connection */
		conn = new MessageConnection ();
		if (!conn.IsServer) {
			ProcessCommandLine (args, true);
			conn.Close ();
			Gdk.Global.NotifyStartupComplete ();
			Environment.Exit (0);
		}

		/* Init GConf */
		GConfClient = new GConf.Client ();

		/* Register stock icons */
		StockIcons.Initialize ();

		/* Set default window icon */
		SetDefaultWindowIcon ();

		/* Start the action thread */
		ActionThread = new ActionThread ();

		/* Load cover database */
		try {
			CoverDB = new CoverDatabase (2);
		} catch (Exception e) {
			new ErrorDialog (String.Format (Catalog.GetString ("Failed to load the cover database: {0}\n\nExiting..."), e.Message));

			Exit ();
		}

		/* Load song database */
		try {
			DB = new SongDatabase (3);
		} catch (Exception e) {
			new ErrorDialog (String.Format (Catalog.GetString ("Failed to load the song database: {0}\n\nExiting..."), e.Message));

			Exit ();
		}

		DB.Load ();

		/* Create playlist window */
		playlist = new PlaylistWindow ();
		playlist.Run ();

		/* Hook up connection callback */
		conn.SetCallback (new MessageConnection.MessageReceivedHandler (HandleMessageReceived));
		ProcessCommandLine (args, false);

		/* Now we load the album covers, and after that start the changes thread */
		CoverDB.DoneLoading += new CoverDatabase.DoneLoadingHandler (HandleCoversDoneLoading);
		
		CoverDB.Load ();

		/* And finally, check if this is the first start */
		/* FIXME we dont do this for now as the issue isn't sorted out yet */
		//playlist.CheckFirstStartUp ();
	}

	private void ProcessCommandLine (string [] args, bool use_conn)
	{
		if (args.Length > 0) {
			/* try to load first argument as playlist */
			FileInfo finfo = new FileInfo (args [0]);
			
			if (finfo.Exists && FileUtils.IsPlaylist (args [0])) {
				if (use_conn)
					conn.Send (finfo.FullName);
				else
					playlist.OpenPlaylist (finfo.FullName);
			}
		}

		if (use_conn)
			conn.Send ("ShowWindow");
	}

	private void SetDefaultWindowIcon ()
	{
		Pixbuf [] default_icon_list = new Pixbuf [1];
		default_icon_list [0] = new Pixbuf (null, "muine-playlist.png");
		Gtk.Window.DefaultIconList = default_icon_list;
	}

	private void HandleMessageReceived (string message,
					    IntPtr user_data)
	{
		if (message == "ShowWindow")
			playlist.WindowVisible = true;
		else
			playlist.OpenPlaylist (message);
	}

	private void HandleCoversDoneLoading ()
	{
		/* covers done loading, start the changes thread */
		DB.CheckChanges ();
	}

	public static void Exit ()
	{
		conn.Close ();

		//Application.Quit ();
		Environment.Exit (0);
	}
}
