/*
 * Copyright (C) 2004 Lee Willis <lee@leewillis.co.uk>
 *           (C) 2004, 2005 Jorn Baayen <jbaayen@gnome.org>
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

	private SignalUtils.SignalDelegate playpause_cb;
	private SignalUtils.SignalDelegate prev_cb;
	private SignalUtils.SignalDelegate next_cb;
	private SignalUtils.SignalDelegate stop_cb;

	IPlayer player;

	public MmKeys (IPlayer player) : base (IntPtr.Zero)
	{
		Raw = mmkeys_new ();

		this.player = player;

		playpause_cb = new SignalUtils.SignalDelegate (OnPlayPause);
		prev_cb      = new SignalUtils.SignalDelegate (OnPrev);
		next_cb      = new SignalUtils.SignalDelegate (OnNext);
		stop_cb      = new SignalUtils.SignalDelegate (OnStop);

		SignalUtils.SignalConnect (Raw, "mm_playpause", playpause_cb);
		SignalUtils.SignalConnect (Raw, "mm_prev", prev_cb);
		SignalUtils.SignalConnect (Raw, "mm_next", next_cb);
		SignalUtils.SignalConnect (Raw, "mm_stop", stop_cb);
	}

	~MmKeys ()
	{
		Dispose ();
	}

	private void OnPlayPause (IntPtr obj)
	{
		player.Playing = !player.Playing;
	}

	private void OnNext (IntPtr obj)
	{
		player.Next ();
	}

	private void OnPrev (IntPtr obj)
	{
		player.Previous ();
	}

	private void OnStop (IntPtr obj)
	{
		player.Playing = false;
	}
}
