/*
 * Copyright © 2004 Jorn Baayen <jorn@nl.linux.org>
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

public class VolumeButton : Button
{
	[DllImport ("libmuine")]
	private static extern IntPtr volume_button_new ();

	[DllImport ("libgobject-2.0-0.dll")]
	private static extern uint g_signal_connect_data (IntPtr obj, string name,
							  SignalDelegate cb, IntPtr data,
							  IntPtr p, int flags);

	public VolumeButton () : base ()
	{
		Raw = volume_button_new ();

		g_signal_connect_data (Raw, "volume_changed", new SignalDelegate (VolumeChangedCallback),
				       IntPtr.Zero, IntPtr.Zero, 0);
	}

	~VolumeButton ()
	{
		Dispose ();
	}

	[DllImport ("libmuine")]
	private static extern void volume_button_set_volume (IntPtr btn, int vol);
	
	public int Volume {
		set {
			volume_button_set_volume (Raw, value);
		}
	}

	public delegate void VolumeChangedHandler (int vol);
	public event VolumeChangedHandler VolumeChanged;

	private delegate void SignalDelegate (IntPtr obj, int vol);

	private static void VolumeChangedCallback (IntPtr obj, int vol)
	{
		VolumeButton btn = GLib.Object.GetObject (obj, false) as VolumeButton;

		if (btn.VolumeChanged != null)
			btn.VolumeChanged (vol);
	}
}
