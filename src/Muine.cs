/*
 * Copyright © 2003, 2004 Jorn Baayen <jorn@nl.linux.org>
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
using GtkSharp;
using GLib;
using Gdk;

public class Muine : Gnome.Program
{
	private static PlaylistWindow playlist;

	public static GConf.Client GConfClient;

	public static SongDatabase DB;

	public static CoverDatabase CoverDB;

	public static ActionThread ActionThread;

	private static MessageConnection conn;

	public static void Main (string [] args)
	{
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
			new ErrorDialog ("Failed to load the cover database: " + e.ToString () + "\nExiting...");

			Exit ();
		}

		CoverDB.Load ();

		/* Load song database */
		try {
			DB = new SongDatabase (1);
		} catch (Exception e) {
			new ErrorDialog ("Failed to load the song database: " + e.ToString () + "\nExiting...");

			Exit ();
		}

		DB.Load ();

		/* Create playlist window */
		playlist = new PlaylistWindow ();
		playlist.DeleteEvent += new DeleteEventHandler (HandlePlaylistDeleteEvent);
		playlist.Run ();

		/* Hook up connection callback */
		conn.SetCallback (new MessageConnection.MessageReceivedHandler (HandleMessageReceived));
		ProcessCommandLine (args, false);
	}

	private void ProcessCommandLine (string [] args, bool use_conn)
	{
		if (args.Length > 0) {
			/* try to load first argument as playlist */
			FileInfo finfo = new FileInfo (args [0]);
			
			if (finfo.Exists) {
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
		List default_icon_list = new List ((IntPtr) 0, typeof (Pixbuf));
		Pixbuf pixbuf = new Pixbuf (null, "muine-playlist.png");
		default_icon_list.Append (pixbuf.Handle);
		Gtk.Window.DefaultIconList = default_icon_list;
	}

	private void HandlePlaylistDeleteEvent (object o, DeleteEventArgs args)
	{
		Exit ();
	}

	private void HandleMessageReceived (string message,
					    IntPtr user_data)
	{
		if (message == "ShowWindow")
			playlist.SetWindowVisible (true);
		else
			playlist.OpenPlaylist (message);
	}

	public static void Exit ()
	{
		conn.Close ();

		//Application.Quit ();
		Environment.Exit (0);
	}
}
