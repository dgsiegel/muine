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

using Gtk;
using GtkSharp;
using GLib;

public class SkipToWindow
{
	[Glade.Widget]
	Window window;
	[Glade.Widget]
	SpinButton seconds_spin_button;
	[Glade.Widget]
	Label time_label;
	
	public SkipToWindow (Window parent)
	{
		Glade.XML gxml = new Glade.XML (null, "SkipToWindow.glade", "window", null);
		gxml.Autoconnect (this);

		window.TransientFor = parent;
	}

	public void Run ()
	{
		window.Visible = true;

		seconds_spin_button.GrabFocus ();
	}

	public delegate void SeekEventHandler (int sec);
	public event SeekEventHandler SeekEvent;

	private void HandleWindowResponse (object o, EventArgs a)
	{
		window.Visible = false;

		ResponseArgs args = (ResponseArgs) a;

		if (args.ResponseId != (int) ResponseType.Ok)
			return;

		int sec = (int) seconds_spin_button.Value;

		if (SeekEvent != null)
			SeekEvent (sec);
	}

	private void HandleWindowDeleteEvent (object o, EventArgs a)
	{
		window.Visible = false;

		DeleteEventArgs args = (DeleteEventArgs) a;

		args.RetVal = true;
	}

	private void HandleSecondsSpinButtonValueChanged (object o, EventArgs args)
	{
		int time = (int) seconds_spin_button.Value;

		string label = "0 seconds";

		int hours = time / 3600;
		int minutes = (time % 3600) / 60;
		int seconds = (time % 3600) % 60;

		if (hours > 0) {
			label = hours + " hours " + minutes + " minutes " + seconds + " seconds";
		} else if (minutes > 0) {
			label = minutes + " minutes " + seconds + " seconds";
		} else if (seconds > 0) {
			label = seconds + " seconds";
		} 
		
		time_label.Text = label;
	}

	private void HandleSecondsSpinButtonActivated (object o, EventArgs args)
	{
		window.Visible = false;
		
		int sec = (int) seconds_spin_button.Value;

		if (SeekEvent != null)
			SeekEvent (sec);
	}
}
