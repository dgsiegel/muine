/*
 * Copyright (C) 2004 Lee Willis <lee@leewillis.co.uk>
 *           (C) 2004 Jorn Baayen <jbaayen@gnome.org>
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
using System.Collections;
using System.Runtime.InteropServices;

using Gtk;
using GLib;

using MuinePluginLib;

public class MmKeys : GLib.Object
{
	[DllImport ("libmuine")]
	private static extern IntPtr mmkeys_new ();

	private SignalUtils.SignalDelegate mm_playpause_cb;
	private SignalUtils.SignalDelegate mm_prev_cb;
	private SignalUtils.SignalDelegate mm_next_cb;
	private SignalUtils.SignalDelegate mm_stop_cb;

	PlayerInterface player;

	public MmKeys (PlayerInterface player) : base (IntPtr.Zero)
	{
		Raw = mmkeys_new ();

		this.player = player;

		mm_playpause_cb = new SignalUtils.SignalDelegate (MmPlayPauseCallback);
		mm_prev_cb      = new SignalUtils.SignalDelegate (MmPrevCallback);
		mm_next_cb      = new SignalUtils.SignalDelegate (MmNextCallback);
		mm_stop_cb      = new SignalUtils.SignalDelegate (MmStopCallback);

		SignalUtils.SignalConnect (Raw, "mm_playpause", mm_playpause_cb);
		SignalUtils.SignalConnect (Raw, "mm_prev", mm_prev_cb);
		SignalUtils.SignalConnect (Raw, "mm_next", mm_next_cb);
		SignalUtils.SignalConnect (Raw, "mm_stop", mm_stop_cb);
	}

	~MmKeys ()
	{
		Dispose ();
	}

	private void MmPlayPauseCallback (IntPtr obj)
	{
		player.Playing = !player.Playing;
	}

	private void MmNextCallback (IntPtr obj)
	{
		player.Next ();
	}

	private void MmPrevCallback (IntPtr obj)
	{
		player.Previous ();
	}

	private void MmStopCallback (IntPtr obj)
	{
		player.Playing = false;
	}
}
