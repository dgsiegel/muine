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

		// Strings
		private static readonly string string_title = 
			Catalog.GetString ("Play Album");

		private static readonly string string_artists = 
			Catalog.GetString ("Performed by {0}");

		// Static
		// Static :: Objects
		// Static :: Objects :: DnD Targets
		private static TargetEntry [] source_entries = {
			DndUtils.TargetMuineAlbumList,
			DndUtils.TargetUriList
		};

		// Widgets
		private CellRenderer pixbuf_renderer = new CellRendererPixbuf ();
		private Gdk.Pixbuf nothing_pixbuf = new Gdk.Pixbuf (null, "muine-nothing.png");

		// Variables
		private bool drag_dest_enabled = false;
		private int pixbuf_column_width = CoverDatabase.CoverSize + (5 * 2);

		// Constructor
		public AddAlbumWindow ()
		{
			base.Title = string_title;

			base.SetGConfSize (GConfKeyWidth , GConfDefaultWidth, 
					   GConfKeyHeight, GConfDefaultHeight);

			base.Items = Global.DB.Albums.Values;
						
			base.List.Model.SortFunc = new HandleModel.CompareFunc (SortFunc);

			TreeViewColumn col = new TreeViewColumn ();
			col.Sizing = TreeViewColumnSizing.Fixed;
			col.Spacing = 4;
			col.PackStart (pixbuf_renderer  , false);
			col.PackStart (base.TextRenderer, true );
			col.SetCellDataFunc (pixbuf_renderer  , new TreeCellDataFunc (PixbufCellDataFunc));
			col.SetCellDataFunc (base.TextRenderer, new TreeCellDataFunc (TextCellDataFunc  ));
			base.List.AppendColumn (col);

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

			IntPtr album_ptr = base.List.Model.HandleFromPath (path);
			Album album = Album.FromHandle (album_ptr);

			CoverImage.HandleDrop ((Song) album.Songs [0], args);
		}

		// Handlers :: OnDragDataGet
		private void OnDragDataGet (object o, DragDataGetArgs args)
		{
			List albums = base.List.SelectedHandles;

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
		// 	FIXME: Remove if we depend on Mono 1.1+
		protected void OnAdded (Album album)
		{
			base.List.HandleAdded (album.Handle, 
				album.FitsCriteria (base.Entry.SearchBits));
		}

		// Handlers :: OnChanged
		// 	FIXME: Remove if we depend on Mono 1.1+
		protected void OnChanged (Album album)
		{
			base.List.HandleChanged (album.Handle, 
				album.FitsCriteria (base.Entry.SearchBits));
		}

		// Handlers :: OnRemoved
		// 	FIXME: Remove if we depend on Mono 1.1+
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

			return a.CompareTo (b);
		}

		// Delegate Functions :: PixbufCellDataFunc
		private void PixbufCellDataFunc (TreeViewColumn col, CellRenderer cell,
						 TreeModel model, TreeIter iter)
		{
			CellRendererPixbuf r = (CellRendererPixbuf) cell;
			Album album = Album.FromHandle (base.List.Model.HandleFromIter (iter));

			if (album.CoverImage != null)
				r.Pixbuf = album.CoverImage;

			else if (Global.CoverDB.Loading)
				r.Pixbuf = Global.CoverDB.DownloadingPixbuf;

			else
				r.Pixbuf = nothing_pixbuf;

			r.Width = r.Height = pixbuf_column_width;
		}

		// Delegate Functions :: TextCellDataFunc
		private void TextCellDataFunc (TreeViewColumn col, CellRenderer cell,
					       TreeModel model, TreeIter iter)
		{
			CellRendererText r = (CellRendererText) cell;
			Album album = Album.FromHandle (base.List.Model.HandleFromIter (iter));

			string performers = "";
			if (album.Performers.Length > 0)
				performers = StringUtils.EscapeForPango (String.Format (string_artists, 
					StringUtils.JoinHumanReadable (album.Performers, 2)));

			r.Markup = String.Format ("<b>{0}</b>\n{1}\n\n{2}",
				StringUtils.EscapeForPango (album.Name),
				StringUtils.EscapeForPango (StringUtils.JoinHumanReadable (album.Artists, 3)),
				performers);
		}
	}
}
