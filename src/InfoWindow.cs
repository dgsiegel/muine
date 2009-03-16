/*
 * Copyright (C) 2004 Jorn Baayen <jbaayen@gnome.org>
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
 * Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
 * Boston, MA 02110-1301, USA.
 */

// TODO: albumchanged signal?
// TODO: accessible from add album window? 
// TODO: songinfo on selection in playlist

using System;
using System.IO;
using System.Text;

using Gtk;
using GLib;

namespace Muine
{
	public class InfoWindow : Window
	{
		// GConf
		private const string GConfKeyWidth = "/apps/muine/information_window/width";
		private const int GConfDefaultWidth = 350; 

		private const string GConfKeyHeight = "/apps/muine/information_window/height";
		private const int GConfDefaultHeight = 300; 

		// Widgets
		[Glade.Widget] private Window         window;
		[Glade.Widget] private ScrolledWindow scrolledwindow;
		[Glade.Widget] private Viewport       viewport;
		[Glade.Widget] private Box            box;
		[Glade.Widget] private Label          name_label;
		[Glade.Widget] private Label          year_label;
		[Glade.Widget] private Table          tracks_table;

		private CoverImage cover_image;

		// Constructor
		public InfoWindow (string title) : base (IntPtr.Zero)
		{
			Glade.XML gxml = new Glade.XML (null, "InfoWindow.glade", "window", null);
			gxml.Autoconnect (this);

			Raw = window.Handle;

			window.Title = title;

			int width  = (int) Muine.GetGConfValue (GConfKeyWidth , GConfDefaultWidth );
			int height = (int) Muine.GetGConfValue (GConfKeyHeight, GConfDefaultHeight);

			window.SetDefaultSize (width, height);

			window.SizeAllocated += OnSizeAllocated;

			cover_image = new CoverImage ();
			((Container) gxml ["cover_image_container"]).Add (cover_image);

			// Keynav
			box.FocusHadjustment = scrolledwindow.Hadjustment;
			box.FocusVadjustment = scrolledwindow.Vadjustment;

			// White background
//			viewport.EnsureStyle ();
//			viewport.ModifyBg (StateType.Normal, viewport.Style.Base (StateType.Normal));
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: Run
		public void Run ()
		{
			window.ShowAll ();
			window.Present ();
		}

		// Methods :: Public
		public void Load (Album album) 
		{
			cover_image.Song = (Song) album.Songs [0];

			name_label.Text = album.Name;

			MarkupUtils.LabelSetMarkup (name_label, 0, 
				StringUtils.GetByteLength (album.Name),
				true, true, false);

			year_label.Text = album.Year;
			
			// Insert tracks
			foreach (Song song in album.Songs)
				InsertTrack (song);
		}

		// Methods :: Private
		// Methods :: Private :: InsertTrack
		private void InsertTrack (Song song)
		{
			tracks_table.NRows ++;

			// Number
			Label number = new Label ("" + song.TrackNumber);
			number.Selectable = true;
			number.Xalign = 0.5F;
			number.Show ();

			tracks_table.Attach (number, 0, 1,
				tracks_table.NRows - 1, tracks_table.NRows,
				AttachOptions.Fill, 
				0, 0, 0);

			// Track
			Label track = new Label (song.Title);
			track.Selectable = true;
			track.Xalign = 0.0F;
			track.Show ();

			AttachOptions opts =
			  ( AttachOptions.Expand
			  | AttachOptions.Shrink
			  | AttachOptions.Fill );

			tracks_table.Attach (track, 1, 2,
				tracks_table.NRows - 1, tracks_table.NRows,
				opts, 0, 0, 0);
		}


		// Handlers
		// Handlers :: OnSizeAllocated
		private void OnSizeAllocated (object o, SizeAllocatedArgs args)
		{
			int width, height;
			window.GetSize (out width, out height);

			Muine.SetGConfValue (GConfKeyWidth , width );
			Muine.SetGConfValue (GConfKeyHeight, height);
		}

		// Handlers :: OnCloseButtonClicked
		private void OnCloseButtonClicked (object o, EventArgs args)
		{
			window.Destroy ();
		}
	}
}
