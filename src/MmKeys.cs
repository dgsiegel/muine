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

	public MmKeys () : base (IntPtr.Zero)
	{
		Raw = mmkeys_new ();

		SignalUtils.SignalConnect (Raw, "mm_playpause", 
					   new SignalUtils.SignalDelegateInt (MmPlayPauseCallback));
		SignalUtils.SignalConnect (Raw, "mm_prev", 
					   new SignalUtils.SignalDelegateInt (MmPrevCallback));
		SignalUtils.SignalConnect (Raw, "mm_next", 
					   new SignalUtils.SignalDelegateInt (MmNextCallback));
		SignalUtils.SignalConnect (Raw, "mm_stop", 
					   new SignalUtils.SignalDelegateInt (MmStopCallback));
	}

	~MmKeys ()
	{
		Dispose ();
	}

	public event EventHandler PlayPause;
	public event EventHandler Previous;
	public event EventHandler Next;
	public event EventHandler Stop;

	private void MmPlayPauseCallback (IntPtr obj, int vol)
	{
		PlayPause (null, null);
	}

	private void MmNextCallback (IntPtr obj, int vol)
	{
		Next (null, null);
	}

	private void MmPrevCallback (IntPtr obj, int vol)
	{
		Previous (null, null);
	}

	private void MmStopCallback (IntPtr obj, int vol)
	{
		Stop (null, null);
	}
}
