/*
 * Copyright (C) 2004 Jorn Baayen <jorn@nl.linux.org>
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
using System.Runtime.InteropServices;

public class Player : GLib.Object
{
	[DllImport ("libmuine")]
	private static extern bool player_set_file (IntPtr player,
	                                            string filename);
	[DllImport ("libmuine")]
	private static extern void player_set_replaygain (IntPtr player,
							  double gain,
							  double peak);
	[DllImport ("libmuine")]
	private static extern void player_stop (IntPtr player);

	private Song song;
	public Song Song {
		get {
			return song;
		}

		set {
			song = value;

			player_stop (Raw);
			player_set_file (Raw, song.Filename);
			player_set_replaygain (Raw, song.Gain, song.Peak);

			if (TickEvent != null)
				TickEvent (0);

			if (playing)
				player_play (Raw);
		}
	}

	[DllImport ("libmuine")]
	private static extern void player_play (IntPtr player);
	[DllImport ("libmuine")]
	private static extern void player_pause (IntPtr player);

	public delegate void StateChangedHandler (bool playing);
	public event StateChangedHandler StateChanged;
		
	private bool playing;
	public bool Playing {
		get {
			return playing;
		}

		set {
			if (playing == value)
				return;
				
			playing = value;

			if (playing)
				player_play (Raw);
			else
				player_pause (Raw);

			if (StateChanged != null)
				StateChanged (playing);
		}
	}

	public void Stop ()
	{
		player_stop (Raw);

		playing = false;

		if (StateChanged != null)
			StateChanged (playing);
	}

	public delegate void TickEventHandler (int pos);
	public event TickEventHandler TickEvent;

	[DllImport ("libmuine")]
	private static extern void player_seek (IntPtr player,
	                                        int t);
	[DllImport ("libmuine")]
	private static extern int player_tell (IntPtr player);

	public int Position {
		get {
			return player_tell (Raw);
		}

		set {
			player_seek (Raw, value);

			if (TickEvent != null)
				TickEvent (value);
		}
	}

	[DllImport ("libmuine")]
	private static extern void player_set_volume (IntPtr player,
						      int volume);
	[DllImport ("libmuine")]
	private static extern int player_get_volume (IntPtr player);

	public int Volume {
		get {
			return player_get_volume (Raw);
		}

		set {
			player_set_volume (Raw, value);
		}
	}

	[DllImport ("libmuine")]
	private static extern IntPtr player_new ();

	[DllImport ("libgobject-2.0-0.dll")]
	private static extern uint g_signal_connect_data (IntPtr obj, string name,
	                                                  SignalDelegate cb, IntPtr data,
							  IntPtr p, int flags);

	public Player () : base ()
	{
		try {
			Raw = player_new ();
		} catch {
			throw new Exception (Muine.Catalog.GetString ("Failed to create Player object"));
		}

		g_signal_connect_data (Raw, "tick", new SignalDelegate (TickCallback),
		                       IntPtr.Zero, IntPtr.Zero, 0);
		g_signal_connect_data (Raw, "end_of_stream", new SignalDelegate (EosCallback),
				       IntPtr.Zero, IntPtr.Zero, 0);

		playing = false;
		song = null;
	}

	~Player ()
	{
		Dispose ();
	}

	private delegate void SignalDelegate (IntPtr obj, int data);

	private static void TickCallback (IntPtr obj, int pos)
	{	
		Player player = GLib.Object.GetObject (obj, false) as Player;

		if (player.TickEvent != null)
			player.TickEvent (pos);
	}

	public delegate void EndOfStreamEventHandler ();
	public event EndOfStreamEventHandler EndOfStreamEvent;

	private static void EosCallback (IntPtr obj, int data)
	{
		Player player = GLib.Object.GetObject (obj, false) as Player;

		if (player.EndOfStreamEvent != null)
			player.EndOfStreamEvent ();
	}
}
