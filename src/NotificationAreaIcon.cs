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
using System.Runtime.InteropServices;

using Gtk;
using Gdk;

public class NotificationAreaIcon : GLib.Object
{
	[DllImport ("libmuine")]
	private static extern IntPtr egg_status_icon_new ();

	[DllImport ("libgobject-2.0-0.dll")]
	private static extern uint g_signal_connect_data (IntPtr obj, string name,
							  SignalDelegate cb, IntPtr data,
							  IntPtr p, int flags);

	private Pixbuf playing_pixbuf;
	private Pixbuf paused_pixbuf;

	public void Init ()
	{
		Raw = egg_status_icon_new ();

		playing_pixbuf = new Pixbuf (null, "muine-tray-playing.png");
		paused_pixbuf = new Pixbuf (null, "muine-tray-paused.png");

		g_signal_connect_data (Raw, "activate", new SignalDelegate (ActivateCallback),
				       IntPtr.Zero, IntPtr.Zero, 0);
	}

	public NotificationAreaIcon () : base ()
	{
		Init ();
	}

	~NotificationAreaIcon ()
	{
		Dispose ();
	}

	[DllImport ("libmuine")]
	private static extern void egg_status_icon_set_tooltip (IntPtr status_icon,
								string tooltip_text,
								string tooltip_private);

	public string Tooltip {
		set {
			egg_status_icon_set_tooltip (Raw, value, null);
		}
	}

	[DllImport ("libmuine")]
	private static extern void egg_status_icon_set_from_pixbuf (IntPtr status_icon,
								    IntPtr pixbuf);

	public bool Playing {
		set {
			if (value == true)
				egg_status_icon_set_from_pixbuf (Raw, playing_pixbuf.Handle);
			else
				egg_status_icon_set_from_pixbuf (Raw, paused_pixbuf.Handle);
		}
	}

	private delegate void SignalDelegate (IntPtr obj);

	public delegate void ActivateEventHandler ();
	public event ActivateEventHandler ActivateEvent;

	private static void ActivateCallback (IntPtr obj)
	{
		NotificationAreaIcon icon = GLib.Object.GetObject (obj, false) as NotificationAreaIcon;

		if (icon.ActivateEvent != null)
			icon.ActivateEvent ();
	}
}
