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
using GtkSharp;

public class NotificationAreaIcon : Plug
{
	[DllImport ("libmuine")]
	private static extern IntPtr egg_tray_icon_new (string name);

	private Gtk.Image image;
	private Tooltips tooltips;

	private bool button_down = false;

	private Pixbuf playing_pixbuf;
	private Pixbuf paused_pixbuf;

	public void Init ()
	{
		Raw = egg_tray_icon_new ("Muine music player");

		ButtonPressEvent += new ButtonPressEventHandler (HandleButtonPressEvent);
		ButtonReleaseEvent += new ButtonReleaseEventHandler (HandleButtonReleaseEvent);
		DestroyEvent += new DestroyEventHandler (HandleDestroyEvent);

		EventBox ebox = new EventBox ();
		image = new Gtk.Image ();

		ebox.Add (image);
		Add (ebox);

		UpdateImage ();
		UpdateTooltip ();

		ShowAll ();
	}

	public NotificationAreaIcon () : base ()
	{
		playing_pixbuf = new Pixbuf (null, "muine-tray-playing.png");
		paused_pixbuf = new Pixbuf (null, "muine-tray-paused.png");

		tooltips = new Tooltips ();

		Init ();
	}

	~NotificationAreaIcon ()
	{
		Dispose ();
	}

	private string tooltip = "";

	private void UpdateTooltip ()
	{
		tooltips.SetTip (this, tooltip, null);
	}

	public string Tooltip {
		set {
			tooltip = value;

			UpdateTooltip ();
		}

		get {
			return tooltip;
		}
	}

	private bool playing = false;

	private void UpdateImage ()
	{
		if (playing == true)
			image.FromPixbuf = playing_pixbuf;
		else
			image.FromPixbuf = paused_pixbuf;
	}

	public bool Playing {
		set {
			playing = value;

			UpdateImage ();
		}

		get {
			return playing;
		}
	}

	private void HandleButtonPressEvent (object o, ButtonPressEventArgs args)
	{
		if ((args.Event.button == 1) && !button_down) {
			button_down = true;
			args.RetVal = true;
			return;
		}

		args.RetVal = false;
	}

	public delegate void ActivateEventHandler ();
	public event ActivateEventHandler ActivateEvent;

	private void HandleButtonReleaseEvent (object o, ButtonReleaseEventArgs args)
	{
		if ((args.Event.button == 1) && button_down) {
			button_down = false;
			
			if (ActivateEvent != null)
				ActivateEvent ();
		}
	}

	private void HandleDestroyEvent (object o, DestroyEventArgs args)
	{
		Init ();
	}
}
