/*
 * Copyright (C) 2003, 2004, 2005 Jorn Baayen <jbaayen@gnome.org>
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

using Gtk;
using GLib;

using Mono.Posix;

namespace Muine
{
	public class AddAlbumWindow : AddWindow
	{
		// GConf
		private const string GConfKeyWidth = "/apps/muine/add_album_window/width";
		private const int GConfDefaultWidth = 500;

		private const string GConfKeyHeight = "/apps/muine/add_album_window/height";
		private const int GConfDefaultHeight = 475; 

		private const string GConfKeyEnableSpeedHacks = "/apps/muine/add_album_window/enable_speed_hacks";
		private const bool GConfDefaultEnableSpeedHacks = false;

		// Strings
		private static readonly string string_title = 
			Catalog.GetString ("Play Album");
		private static readonly string string_artists = 
			Catalog.GetString ("Performed by {0}");

		// DnD Targets	
		private static TargetEntry [] source_entries = new TargetEntry [] {
			DndUtils.TargetMuineAlbumList,
			DndUtils.TargetUriList
		};

		// Widgets
		private CellRenderer pixbuf_renderer = new CellRendererPixbuf ();
		private Gdk.Pixbuf nothing_pixbuf = new Gdk.Pixbuf (null, "muine-nothing.png");

		// Variables
		private bool drag_dest_enabled = false;

		// Constructor
		public AddAlbumWindow ()
		{
			base.Title = string_title;

			base.SetGConfSize (GConfKeyWidth , GConfDefaultWidth, 
					   GConfKeyHeight, GConfDefaultHeight);

			base.SetGConfSpeedHacks (GConfKeyEnableSpeedHacks, GConfDefaultEnableSpeedHacks);

			base.Items = Global.DB.Albums.Values;
						
			base.List.SortFunc = new AddWindowList.CompareFunc (SortFunc);

			base.List.AddColumn (pixbuf_renderer  , new AddWindowList.CellDataFunc (PixbufCellDataFunc), false);
			base.List.AddColumn (base.TextRenderer, new AddWindowList.CellDataFunc (TextCellDataFunc  ), true );

			base.List.DragSource = source_entries;
			base.List.DragDataGet += new DragDataGetHandler (OnDragDataGet);

			// Requires Mono 1.1+:
			// Global.DB.AlbumAdded   += new SongDatabase.AlbumAddedHandler   (base.OnAdded  );
			// Global.DB.AlbumChanged += new SongDatabase.AlbumChangedHandler (base.OnChanged);
			// Global.DB.AlbumRemoved += new SongDatabase.AlbumRemovedHandler (base.OnRemoved);

			Global.DB.AlbumAdded   += new SongDatabase.AlbumAddedHandler   (OnAdded  );
			Global.DB.AlbumChanged += new SongDatabase.AlbumChangedHandler (OnChanged);
			Global.DB.AlbumRemoved += new SongDatabase.AlbumRemovedHandler (OnRemoved);

			Global.CoverDB.DoneLoading += new CoverDatabase.DoneLoadingHandler (OnCoversDoneLoading);

			lock (Global.DB) {
				foreach (Album a in Global.DB.Albums.Values) 
					base.List.Append (a.Handle);
			}

			if (!Global.CoverDB.Loading)
				EnableDragDest ();
		}

		// Methods
		// Methods :: Private
		// Methods :: Private :: EnableDragDest
		private void EnableDragDest ()
		{
			if (drag_dest_enabled)
				return;

			base.List.DragDataReceived += new DragDataReceivedHandler (OnDragDataReceived);
			Gtk.Drag.DestSet (base.List, DestDefaults.All,
					  CoverImage.DragEntries, Gdk.DragAction.Copy);

			drag_dest_enabled = true;
		}

		// Handlers
		// Handlers :: OnCoversDoneLoading
		private void OnCoversDoneLoading ()
		{
			EnableDragDest ();

			base.List.QueueDraw ();
		}

		// Handlers :: OnDragDataReceived
		private void OnDragDataReceived (object o, DragDataReceivedArgs args)
		{
			TreePath path;

			if (!base.List.GetPathAtPos (args.X, args.Y, out path))
				return;

			IntPtr album_ptr = base.List.GetHandleFromPath (path);
			Album album = Album.FromHandle (album_ptr);

			CoverImage.HandleDrop ((Song) album.Songs [0], args);
		}

		// Handlers :: OnDragDataGet
		private void OnDragDataGet (object o, DragDataGetArgs args)
		{
			List albums = base.List.SelectedPointers;

			switch (args.Info) {
			case (uint) DndUtils.TargetType.UriList:
				string files = "";

				foreach (int i in albums) {
					IntPtr p = new IntPtr (i);
					Album a = Album.FromHandle (p);

					foreach (Song s in a.Songs)
						files += FileUtils.UriFromLocalPath (s.Filename) + "\r\n";
				}
		
				args.SelectionData.Set (Gdk.Atom.Intern (DndUtils.TargetUriList.Target, false),
							8, System.Text.Encoding.UTF8.GetBytes (files));
							
				break;

			case (uint) DndUtils.TargetType.AlbumList:
				string ptrs = String.Format ("\t{0}\t", DndUtils.TargetMuineAlbumList.Target);
				
				foreach (int p in albums) {
					IntPtr s = new IntPtr (p);
					ptrs += s.ToString () + "\r\n";
				}
				
				args.SelectionData.Set (Gdk.Atom.Intern (DndUtils.TargetMuineAlbumList.Target, false),
							8, System.Text.Encoding.ASCII.GetBytes (ptrs));
							
				break;

			default:
				break;	
			}
		}

		// Handlers :: OnAdded
		// 	Remove if we depend on Mono 1.1+
		protected void OnAdded (Album album)
		{
			if (base.EnableSpeedHacks &&
			    base.Entry.Text.Length < base.Entry.MinQueryLength &&
			    base.List.Length >= base.List.FakeLength)
				return;

			base.List.HandleAdded (album.Handle, 
					       album.FitsCriteria (base.Entry.SearchBits));
		}

		// Handlers :: OnChanged
		// 	Remove if we depend on Mono 1.1+
		protected void OnChanged (Album album)
		{
			bool may_append = true;
			if (base.EnableSpeedHacks) {
				may_append = (base.Entry.Text.Length >= base.Entry.MinQueryLength ||
			                      base.List.Length < base.List.FakeLength);
			}
			
			base.List.HandleChanged (album.Handle, 
						 album.FitsCriteria (base.Entry.SearchBits),
						 may_append);
		}

		// Handlers :: OnRemoved
		// 	Remove if we depend on Mono 1.1+
		protected void OnRemoved (Album album)
		{
			base.List.HandleRemoved (album.Handle);
		}

		// Delegate Functions		
		// Delegate Functions :: SortFunc
		private int SortFunc (IntPtr a_ptr, IntPtr b_ptr)
		{
			Album a = Album.FromHandle (a_ptr);
			Album b = Album.FromHandle (b_ptr);

			return String.CompareOrdinal (a.SortKey, b.SortKey);
		}

		// Delegate Functions :: PixbufCellDataFunc
		private void PixbufCellDataFunc (HandleView view, CellRenderer cell, IntPtr album_ptr)
		{
			CellRendererPixbuf r = (CellRendererPixbuf) cell;
			Album album = Album.FromHandle (album_ptr);

			r.Pixbuf = (album.CoverImage != null)
				? album.CoverImage
				: (Global.CoverDB.Loading)
					? Global.CoverDB.DownloadingPixbuf
					: nothing_pixbuf;

			r.Width = r.Height = CoverDatabase.CoverSize + 5 * 2;
		}

		// Delegate Functions :: TextCellDataFunc
		private void TextCellDataFunc (HandleView view, CellRenderer cell, IntPtr album_ptr)
		{
			CellRendererText r = (CellRendererText) cell;
			Album album = Album.FromHandle (album_ptr);

			string performers = "";
			if (album.Performers.Length > 0)
				performers = StringUtils.EscapeForPango (String.Format (string_artists, StringUtils.JoinHumanReadable (album.Performers, 2)));

			r.Markup = String.Format ("<span weight=\"bold\">{0}</span>\n{1}\n\n{2}",
						  StringUtils.EscapeForPango (album.Name),
						  StringUtils.EscapeForPango (StringUtils.JoinHumanReadable (album.Artists, 3)),
						  performers);
		}
	}
}
