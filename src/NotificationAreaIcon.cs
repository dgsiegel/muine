/*
 * Copyright (C) 2003, 2004 Jorn Baayen <jorn@nl.linux.org>
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

public class NotificationAreaIcon : Plug
{
	[DllImport ("libmuine")]
	private static extern IntPtr egg_tray_icon_new (string name);

	private EventBox ebox;
	private Gtk.Image image;
	private Tooltips tooltips;

	private int menu_x;
	private int menu_y;
	
	private Menu menu;
	public ImageMenuItem play_pause_menu_item;
	public Gtk.Image play_pause_menu_item_image;
	public ImageMenuItem previous_song_menu_item;
	public ImageMenuItem next_song_menu_item;
	public ImageMenuItem play_song_menu_item;
	public ImageMenuItem play_album_menu_item;
	public MenuItem show_window_menu_item;

	private bool button_down = false;

	private bool visible;

	public void Init ()
	{
		Raw = egg_tray_icon_new (Muine.Catalog.GetString ("Muine music player"));

		DestroyEvent += new DestroyEventHandler (HandleDestroyEvent);

		ebox = new EventBox ();
		ebox.ButtonPressEvent += new ButtonPressEventHandler (HandleButtonPressEvent);
		ebox.ButtonReleaseEvent += new ButtonReleaseEventHandler (HandleButtonReleaseEvent);
		
		image = new Gtk.Image ();

		ebox.Add (image);
		Add (ebox);

		UpdateImage ();
		UpdateTooltip ();

		if (visible)
			ShowAll ();
	}

	private void CreatePopupMenu ()
	{
		menu = new Menu ();
		menu.SelectionDone += new EventHandler (HandleSelectionDone);
		
		play_pause_menu_item = new ImageMenuItem ("");
		play_pause_menu_item_image = new Gtk.Image ();
		play_pause_menu_item.Image = play_pause_menu_item_image;
		menu.Append (play_pause_menu_item);

		SeparatorMenuItem sep = new SeparatorMenuItem ();
		menu.Append (sep);
		
		previous_song_menu_item = new ImageMenuItem (Muine.Catalog.GetString ("_Previous Song"));
		previous_song_menu_item.Image = new Gtk.Image ("muine-previous", IconSize.Menu);
		menu.Append (previous_song_menu_item);
		next_song_menu_item = new ImageMenuItem (Muine.Catalog.GetString ("_Next Song"));
		next_song_menu_item.Image = new Gtk.Image ("muine-next", IconSize.Menu);
		menu.Append (next_song_menu_item);

		sep = new SeparatorMenuItem ();
		menu.Append (sep);

		play_song_menu_item = new ImageMenuItem (Muine.Catalog.GetString ("Play _Song..."));
		play_song_menu_item.Image = new Gtk.Image (Stock.Add, IconSize.Menu);
		menu.Append (play_song_menu_item);
		play_album_menu_item = new ImageMenuItem (Muine.Catalog.GetString ("Play _Album..."));
		play_album_menu_item.Image = new Gtk.Image ("muine-add-album", IconSize.Menu);
		menu.Append (play_album_menu_item);

		sep = new SeparatorMenuItem ();
		menu.Append (sep);

		show_window_menu_item = new MenuItem (Muine.Catalog.GetString ("Show _Window"));
		menu.Append (show_window_menu_item);

		menu.ShowAll ();
	}

	public NotificationAreaIcon () : base (IntPtr.Zero)
	{
		tooltips = new Tooltips ();

		visible = false;

		/* create popup menu */
		CreatePopupMenu ();

		/* init icon */
		Init ();
	}

	~NotificationAreaIcon ()
	{
		Dispose ();
	}

	public void Run ()
	{
		visible = true;

		ShowAll ();
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
			image.SetFromStock ("muine-tray-playing", IconSize.Menu);
		else
			image.SetFromStock ("muine-tray-paused", IconSize.Menu);
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

	private void HandleSelectionDone (object o, EventArgs args)
	{
		State = StateType.Normal;
	}

	private int Clamp (int x, int low, int high)
	{
		return ((x > high) ? high : ((x < low) ? low : x));
	}

	private void PositionMenu (Menu menu, out int x, out int y, out bool push_in)
	{
		x = menu_x;
		y = menu_y;

		int monitor = menu.Screen.GetMonitorAtPoint (x, y);
		Gdk.Rectangle rect = menu.Screen.GetMonitorGeometry (monitor);

		int space_above = y - rect.Y;
		int space_below = rect.Y + rect.Height - y;

		Requisition requisition = menu.SizeRequest ();

		if (requisition.Height <= space_above ||
		    requisition.Height <= space_below) {
			if (requisition.Height <= space_below)
				y = y + ebox.Allocation.Height;
			else
				y = y - requisition.Height;
		} else if (requisition.Height > space_below && requisition.Height > space_above) {
			if (space_below >= space_above)
				y = rect.Y + rect.Height - requisition.Height;
			else
				y = rect.Y;
		} else {
			y = rect.Y;
		}

		push_in = true;
	}

	private void HandleButtonPressEvent (object o, ButtonPressEventArgs args)
	{
		switch (args.Event.Button)
		{
		case 1:
		case 3:
			State = StateType.Active;

			menu_x = (int) args.Event.XRoot - (int) args.Event.X;
			menu_y = (int) args.Event.YRoot - (int) args.Event.Y;

			menu.Popup (null, null, new MenuPositionFunc (PositionMenu), IntPtr.Zero,
			            args.Event.Button, args.Event.Time);
			
			break;

		case 2:
			show_window_menu_item.Activate ();

			break;

		default:
			break;
		}

		args.RetVal = false;
	}

	private void HandleButtonReleaseEvent (object o, ButtonReleaseEventArgs args)
	{
		switch (args.Event.Button)
		{
		case 1:
		case 3:
			menu.Popdown ();
			
			break;

		default:
			break;
		}

		State = StateType.Normal;

		args.RetVal = false;
	}

	private void HandleDestroyEvent (object o, DestroyEventArgs args)
	{
		Init ();
	}
}
