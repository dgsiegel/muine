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
	HScale song_slider;
	[Glade.Widget]
	Label song_position;

	Player player;

	bool from_tick;
	
	public SkipToWindow (Window parent, Player p)
	{
		Glade.XML gxml = new Glade.XML (null, "SkipToWindow.glade", "window", null);
		gxml.Autoconnect (this);

		window.TransientFor = parent;

		player = p;
		player.TickEvent += new Player.TickEventHandler (HandleTickEvent);
	}

	public void Run ()
	{
		window.Visible = true;
		
		song_slider.GrabFocus ();
	}

	public void Hide ()
	{
		window.Visible = false;
	}

	private void HandleTickEvent (int pos) 
	{
		/* update label */
		String position = StringUtils.SecondsToString (pos);
		String total_time = StringUtils.SecondsToString (player.Song.Duration);
		song_position.Text = position + " / " + total_time;

		/* update slider */
		from_tick = true;
		song_slider.Value = pos; 
		song_slider.SetRange (0, player.Song.Duration);
	}

	private uint timeout_id = 0;

	private bool TimeoutFunc ()
	{
		player.Seeking = false;

		timeout_id = 0;

		return false;
	}

	private void HandleSongSliderValueChanged (object o, EventArgs a) 
	{
		if (!from_tick) {
			player.Seeking = true;

			player.Position = (int) song_slider.Value;

			if (timeout_id != 0)
				Gtk.Timeout.Remove (timeout_id);
			timeout_id = Gtk.Timeout.Add (100, new Function (TimeoutFunc));
		} else
			from_tick = false;
	}

	private void HandleWindowDeleteEvent (object o, EventArgs a)
	{
		window.Visible = false;
		
		DeleteEventArgs args = (DeleteEventArgs) a;
		args.RetVal = true;
	}

	private void HandleCloseButtonClicked (object o, EventArgs a)
	{
		window.Visible = false;
	}
}
