/*
 * Copyright (C) 2004 Lee Willis <lee@leewillis.co.uk>
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

public class MmKeys : GLib.Object
{
	[DllImport ("libmuine")]
	private static extern IntPtr mmkeys_new ();

	[DllImport ("libgobject-2.0-0.dll")]
	private static extern uint g_signal_connect_data (IntPtr obj, string name,
							  SignalDelegate cb, IntPtr data,
							  IntPtr p, int flags);

	public MmKeys () : base ()
	{
		Raw = mmkeys_new ();

		g_signal_connect_data (Raw, "mm_playpause", new SignalDelegate (MmPlayPauseCallback),
				       IntPtr.Zero, IntPtr.Zero, 0);
		g_signal_connect_data (Raw, "mm_prev", new SignalDelegate (MmPrevCallback),
				       IntPtr.Zero, IntPtr.Zero, 0);
		g_signal_connect_data (Raw, "mm_next", new SignalDelegate (MmNextCallback),
				       IntPtr.Zero, IntPtr.Zero, 0);
	}

	~MmKeys ()
	{
		Dispose ();
	}

	private delegate void SignalDelegate (IntPtr obj, int vol);

	public delegate void PlayPauseHandler (object o, EventArgs args);
	public event PlayPauseHandler PlayPause;

	public delegate void PreviousHandler (object o, EventArgs args);
	public event PreviousHandler Previous;

	public delegate void NextHandler (object o, EventArgs args);
	public event NextHandler Next;

	private static void MmPlayPauseCallback (IntPtr obj, int vol)
	{
		MmKeys mmkeys_object = GLib.Object.GetObject (obj, false) as MmKeys;
		mmkeys_object.PlayPause (null, null);
	}

	private static void MmNextCallback (IntPtr obj, int vol)
	{
		MmKeys mmkeys_object = GLib.Object.GetObject (obj, false) as MmKeys;
		mmkeys_object.Next (null, null);
	}

	private static void MmPrevCallback (IntPtr obj, int vol)
	{
		MmKeys mmkeys_object = GLib.Object.GetObject (obj, false) as MmKeys;
		mmkeys_object.Previous (null, null);
	}
}
