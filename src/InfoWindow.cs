/*
 * Copyright (C) 2004 Jorn Baayen <jorn@nl.linux.org>
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
using System.IO;
using System.Text;

using Gtk;
using GLib;

public class InfoWindow : Window
{
	[Glade.Widget]
	Window window;
	[Glade.Widget]
	ScrolledWindow scrolledwindow;
	[Glade.Widget]
	Viewport viewport;
	[Glade.Widget]
	Box box;
	[Glade.Widget]
	Label name_label;
	[Glade.Widget]
	Label year_label;
	[Glade.Widget]
	Table tracks_table;
	private CoverImage cover_image;

	/* FIXME albumchanged signal? */
	/* FIXME accessible from add album window? */
	/* FIXME songinfo on selection in playlist */

	public InfoWindow (string title)
	{
		Glade.XML gxml = new Glade.XML (null, "InfoWindow.glade", "window", null);
		gxml.Autoconnect (this);

		Raw = window.Handle;

		window.Title = title;

		int width;
		try {
			width = (int) Muine.GConfClient.Get ("/apps/muine/information_window/width");
		} catch {
			width = 350;
		}

		int height;
		try {
			height = (int) Muine.GConfClient.Get ("/apps/muine/information_window/height");
		} catch {
			height = 300;
		}

		window.SetDefaultSize (width, height);

		window.SizeAllocated += new SizeAllocatedHandler (HandleSizeAllocated);

		cover_image = new CoverImage ();
		((Container) gxml ["cover_image_container"]).Add (cover_image);

		/* Keynav */
		box.FocusHadjustment = scrolledwindow.Hadjustment;
		box.FocusVadjustment = scrolledwindow.Vadjustment;

		/* white background.. */
//		viewport.EnsureStyle ();
//		viewport.ModifyBg (StateType.Normal, viewport.Style.Base (StateType.Normal));
	}

	private void HandleSizeAllocated (object o, SizeAllocatedArgs args)
	{
		int width, height;

		window.GetSize (out width, out height);

		Muine.GConfClient.Set ("/apps/muine/information_window/width", width);
		Muine.GConfClient.Set ("/apps/muine/information_window/height", height);
	}

	public void Run ()
	{
		window.ShowAll ();

		window.Present ();
	}

	private void HandleCloseButtonClicked (object o, EventArgs args)
	{
		window.Destroy ();
	}

	private void InsertTrack (Song song)
	{
		tracks_table.NRows ++;

		Label number = new Label ("" + song.TrackNumber);
		number.Selectable = true;
		number.Xalign = 0.5F;
		number.Show ();
		tracks_table.Attach (number, 0, 1,
		                     tracks_table.NRows - 1,
				     tracks_table.NRows,
				     AttachOptions.Fill,
				     0, 0, 0);

		Label track = new Label (song.Title);
		track.Selectable = true;
		track.Xalign = 0.0F;
		track.Show ();
		tracks_table.Attach (track, 1, 2,
		                     tracks_table.NRows - 1,
				     tracks_table.NRows,
				     AttachOptions.Expand | AttachOptions.Shrink | AttachOptions.Fill,
				     0, 0, 0);
	}

	public void Load (Album album) 
	{
		cover_image.Song = (Song) album.Songs [0];

		name_label.Text = album.Name;
		MarkupUtils.LabelSetMarkup (name_label, 0, StringUtils.GetByteLength (album.Name),
		                            true, true, false);

		year_label.Text = album.Year;
		
		/* insert tracks */
		foreach (Song song in album.Songs)
			InsertTrack (song);
	}
}
