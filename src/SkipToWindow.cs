/*
 * Copyright (C) 2004, 2005 Jorn Baayen <jbaayen@gnome.org>
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
using GLib;

namespace Muine
{
	public class SkipToWindow
	{
		// Widgets
		[Glade.Widget] private Window window;
		[Glade.Widget] private HScale song_slider;
		[Glade.Widget] private Label  song_position;

		// Objects
		Player player;

		// Variables
		bool from_tick;
		
		// Constructor
		public SkipToWindow (Window parent, Player p)
		{
			Glade.XML gxml = new Glade.XML (null, "SkipToWindow.glade", "window", null);
			gxml.Autoconnect (this);

			window.TransientFor = parent;

			player = p;
			player.TickEvent += new Player.TickEventHandler (OnTickEvent);

			OnTickEvent (player.Position);
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: Run
		public void Run ()
		{
			window.Visible = true;
			song_slider.GrabFocus ();
		}

		// Methods :: Public :: Hide
		public void Hide ()
		{
			window.Visible = false;
		}

		// Handlers
		// Handlers :: OnTickEvent
		private void OnTickEvent (int pos) 
		{
			// Update label
			String position   = StringUtils.SecondsToString (pos);
			String total_time = StringUtils.SecondsToString (player.Song.Duration);
			song_position.Text = String.Format ("{0} / {1}", position, total_time);

			// Update slider
			from_tick = true;
			song_slider.SetRange (0, player.Song.Duration);
			song_slider.Value = pos; 
		}

		// Handlers :: OnSongSliderValueChanged
		private void OnSongSliderValueChanged (object o, EventArgs a) 
		{
			if (!from_tick) {
				player.Position = (int) song_slider.Value;
				player.Play ();

			} else {
				from_tick = false;
			}
		}

		// Handlers :: OnWindowDeleteEvent
		private void OnWindowDeleteEvent (object o, EventArgs a)
		{
			window.Visible = false;
			
			DeleteEventArgs args = (DeleteEventArgs) a;
			args.RetVal = true;
		}

		// Handlers :: OnCloseButtonClicked
		private void OnCloseButtonClicked (object o, EventArgs a)
		{
			window.Visible = false;
		}
	}
}
