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

using MuinePluginLib;

public class NotificationAreaIcon : Plug
{
	[DllImport ("libmuine")]
	private static extern IntPtr egg_tray_icon_new (string name);

	private EventBox ebox;
	private Gtk.Image image;
	private Tooltips tooltips;

	private PlayerInterface player;

	private int menu_x;
	private int menu_y;
	
	private Menu menu;

	private bool button_down = false;

	private bool visible;

	public void Init ()
	{
		Raw = egg_tray_icon_new (Muine.Catalog.GetString ("Muine music player"));

		DestroyEvent += new DestroyEventHandler (HandleDestroyEvent);

		ebox = new EventBox ();
		ebox.ButtonPressEvent += new ButtonPressEventHandler (HandleButtonPressEvent);
		
		image = new Gtk.Image ();

		ebox.Add (image);
		Add (ebox);

		UpdateImage ();
		UpdateTooltip ();

		if (visible)
			ShowAll ();
	}

	private const string ui_info =
		"<popup name=\"Menu\">\n" +
		"  <menuitem action=\"PlayPause\" />\n" +
		"  <separator />\n" +
		"  <menuitem action=\"PreviousSong\" />\n" +
		"  <menuitem action=\"NextSong\" />\n" +
		"  <separator />\n" +
		"  <menuitem action=\"PlaySong\" />\n" +
		"  <menuitem action=\"PlayAlbum\" />\n" +
		"  <separator />\n" +
		"  <menuitem action=\"ShowHideWindow\" />\n" +
		"</popup>\n";

	public NotificationAreaIcon (PlayerInterface player) : base (IntPtr.Zero)
	{
		/* connect to player */
		this.player = player;
		
		player.SongChangedEvent +=
			new Plugin.SongChangedEventHandler (HandleSongChangedEvent);
		player.StateChangedEvent +=
			new Plugin.StateChangedEventHandler (HandleStateChangedEvent);
		
		/* build menu */
		UIManager uim = new UIManager ();
		uim.InsertActionGroup (player.ActionGroup, 0);
		uim.AddUiFromString (ui_info);
		
		menu = (Menu) uim.GetWidget ("/Menu");
		menu.Deactivated += new EventHandler (HandleMenuDeactivated);

		/* init tooltips */
		tooltips = new Tooltips ();
		tooltips.Disable ();

		/* not visible yet */
		visible = false;

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

	private bool playing = false;

	private void UpdateImage ()
	{
		string icon = (playing) ? "muine-tray-playing" : "muine-tray-paused";
		image.SetFromStock (icon, IconSize.Menu);
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

		int monitor = ((Widget) menu).Screen.GetMonitorAtPoint (x, y);
		Gdk.Rectangle rect = ((Widget) menu).Screen.GetMonitorGeometry (monitor);

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
			player.WindowVisible = !player.WindowVisible;

			break;

		default:
			break;
		}

		args.RetVal = false;
	}

	private void HandleMenuDeactivated (object o, EventArgs args)
	{
		State = StateType.Normal;
	}

	private void HandleDestroyEvent (object o, DestroyEventArgs args)
	{
		Init ();
	}

	private string CreateTooltip (SongInterface song)
	{
		/* song artists - song title */
		return String.Format (Muine.Catalog.GetString ("{0} - {1}"),
				      StringUtils.JoinHumanReadable (song.Artists),
				      song.Title);
	}

	private void HandleSongChangedEvent (SongInterface song)
	{
		if (song != null)
			tooltip = CreateTooltip (song);
		else
			tooltip = null;

		UpdateTooltip ();
	}

	private void HandleStateChangedEvent (bool playing)
	{
		if (playing)
			tooltips.Enable ();
		else
			tooltips.Disable ();

		this.playing = playing;

		UpdateImage ();
	}
}
