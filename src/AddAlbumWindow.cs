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

public class AddAlbumWindow : AddWindow
{
        private const string GConfKeyWidth = "/apps/muine/add_album_window/width";
        private const int GConfDefaultWidth = 500;

        private const string GConfKeyHeight = "/apps/muine/add_album_window/height";
        private const int GConfDefaultHeight = 475; 

	// Widgets
	private CellRenderer pixbuf_renderer = new CellRendererPixbuf ();
	private Gdk.Pixbuf nothing_pixbuf = new Gdk.Pixbuf (null, "muine-nothing.png");

	// DnD Targets	
	private static TargetEntry [] cover_drag_entries = new TargetEntry [] { 
		Muine.TargetUriList, 
		Muine.TargetGnomeIconList, 
		Muine.TargetNetscapeUrl 
	};

	private static TargetEntry [] source_entries = new TargetEntry [] {
		Muine.TargetMuineAlbumList,
		Muine.TargetUriList
	};

	// Constructor
	public AddAlbumWindow ()
	{
		window.Title = Muine.Catalog.GetString ("Play Album");

		SetGConfSize (GConfKeyWidth, GConfKeyHeight, GConfDefaultWidth, GConfDefaultHeight);
		
		view.SortFunc = new HandleView.CompareFunc (SortFunc);

		view.AddColumn (pixbuf_renderer, new HandleView.CellDataFunc (PixbufCellDataFunc), false);
		view.AddColumn (text_renderer, new HandleView.CellDataFunc (TextCellDataFunc), true);

		view.EnableModelDragSource (Gdk.ModifierType.Button1Mask, 
					    source_entries, Gdk.DragAction.Copy);
		view.DragDataGet += new DragDataGetHandler (OnDragDataGet);

		Muine.DB.AlbumAdded += new SongDatabase.AlbumAddedHandler (OnAlbumAdded);
		Muine.DB.AlbumChanged += new SongDatabase.AlbumChangedHandler (OnAlbumChanged);
		Muine.DB.AlbumRemoved += new SongDatabase.AlbumRemovedHandler (OnAlbumRemoved);

		Muine.CoverDB.DoneLoading += new CoverDatabase.DoneLoadingHandler (OnCoversDoneLoading);

		foreach (Album a in Muine.DB.Albums.Values) 
			view.Append (a.Handle);
		SelectFirst ();

		view.DragDataReceived += new DragDataReceivedHandler (OnDragDataReceived);
		view.DragMotion += new DragMotionHandler (OnDragMotion);
		Gtk.Drag.DestSet (view, DestDefaults.All,
				  cover_drag_entries, Gdk.DragAction.Copy);
	}

	private int SortFunc (IntPtr a_ptr,
			      IntPtr b_ptr)
	{
		Album a = Album.FromHandle (a_ptr);
		Album b = Album.FromHandle (b_ptr);

		return String.CompareOrdinal (a.SortKey, b.SortKey);
	}

	private void PixbufCellDataFunc (HandleView view,
					 CellRenderer cell,
					 IntPtr album_ptr)
	{
		CellRendererPixbuf r = (CellRendererPixbuf) cell;
		Album album = Album.FromHandle (album_ptr);

		r.Pixbuf = (album.CoverImage != null)
			? album.CoverImage
			: (Muine.CoverDB.Loading)
				? Muine.CoverDB.DownloadingPixbuf
				: nothing_pixbuf;

		r.Width = r.Height = CoverDatabase.AlbumCoverSize + 5 * 2;
	}

	private void TextCellDataFunc (HandleView view,
				       CellRenderer cell,
				       IntPtr album_ptr)
	{
		CellRendererText r = (CellRendererText) cell;
		Album album = Album.FromHandle (album_ptr);

		string performers = "";
		if (album.Performers.Length > 0)
			performers = String.Format (Muine.Catalog.GetString ("Performed by {0}"), StringUtils.JoinHumanReadable (album.Performers, 2));

		r.Text = album.Name + "\n" + StringUtils.JoinHumanReadable (album.Artists, 3) + "\n\n" + performers;

		MarkupUtils.CellSetMarkup (r, 0, StringUtils.GetByteLength (album.Name),
					   false, true, false);
	}

	protected override bool Search ()
	{
		List l = new List (IntPtr.Zero, typeof (int));

		if (search_entry.Text.Length > 0) {
			foreach (Album a in Muine.DB.Albums.Values) {
				if (a.FitsCriteria (SearchBits))
					l.Append (a.Handle);
			}
		} else {
			foreach (Album a in Muine.DB.Albums.Values)
				l.Append (a.Handle);
		}

		view.RemoveDelta (l);

		foreach (int i in l) {
			IntPtr ptr = new IntPtr (i);

			view.Append (ptr);
		}

		SelectFirst ();

		return false;
	}
	
	private void OnAlbumAdded (Album album)
	{
		base.HandleAdded (album.Handle, album.FitsCriteria (SearchBits));
	}

	private void OnAlbumChanged (Album album)
	{
		base.HandleChanged (album.Handle, album.FitsCriteria (SearchBits));
	}

	private void OnAlbumRemoved (Album album)
	{
		base.HandleRemoved (album.Handle);
	}

	private void OnDragDataReceived (object o, DragDataReceivedArgs args)
	{
		TreePath path;

		if (!view.GetPathAtPos (args.X, args.Y, out path))
			return;

		IntPtr album_ptr = view.GetHandleFromPath (path);
		Album album = Album.FromHandle (album_ptr);

		CoverImage.HandleDrop ((Song) album.Songs [0], args);
	}

	private void OnDragMotion (object o, DragMotionArgs args)
	{
		TreePath path;

		if (!view.GetPathAtPos (args.X, args.Y, out path))
			return;

		if (Muine.CoverDB.Loading)
			return;
		
		view.SetDragDestRow (path, Gtk.TreeViewDropPosition.IntoOrAfter);
	}

	private void OnCoversDoneLoading ()
	{
		view.QueueDraw ();
	}

	private void OnDragDataGet (object o, DragDataGetArgs args)
	{
		List albums = view.SelectedPointers;

		switch (args.Info) {
		case (uint) Muine.TargetType.UriList:
			string files = "";

			foreach (int i in albums) {
				IntPtr p = new IntPtr (i);
				Album a = Album.FromHandle (p);

				foreach (Song s in a.Songs)
					files += FileUtils.UriFromLocalPath (s.Filename) + "\r\n";
			}
	
			args.SelectionData.Set (Gdk.Atom.Intern (Muine.TargetUriList.Target, false),
						8, System.Text.Encoding.UTF8.GetBytes (files));
						
			break;

		case (uint) Muine.TargetType.AlbumList:
			string ptrs = String.Format ("\t{0}\t", Muine.TargetMuineAlbumList.Target);
			
			foreach (int p in albums) {
				IntPtr s = new IntPtr (p);
				ptrs += s.ToString () + "\r\n";
			}
			
			args.SelectionData.Set (Gdk.Atom.Intern (Muine.TargetMuineAlbumList.Target, false),
					        8, System.Text.Encoding.ASCII.GetBytes (ptrs));
						
			break;

		default:
			break;	
		}
	}
}
