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
using System.Collections;

using Gtk;
using GLib;

public class AddAlbumWindow : Window
{
        private const string GConfKeyWidth = "/apps/muine/add_album_window/width";
        private const int GConfDefaultWidth = 500;

        private const string GConfKeyHeight = "/apps/muine/add_album_window/height";
        private const int GConfDefaultHeight = 475; 

	// Widgets
	[Glade.Widget]
	Window window;
	[Glade.Widget]
	Entry search_entry;
	[Glade.Widget]
	Button play_button;
	[Glade.Widget]
	Image play_button_image;
	[Glade.Widget]
	Button queue_button;
	[Glade.Widget]
	Image queue_button_image;
	[Glade.Widget]
	ScrolledWindow scrolledwindow;
	private HandleView view;
	private CellRenderer text_renderer;
	private CellRenderer pixbuf_renderer;
	private Gdk.Pixbuf nothing_pixbuf;

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
	public AddAlbumWindow () : base (IntPtr.Zero)
	{
		Glade.XML gxml = new Glade.XML (null, "AddWindow.glade", "window", null);
		gxml.Autoconnect (this);

		Raw = window.Handle;

		window.Title = Muine.Catalog.GetString ("Play Album");

		int width = (int) Muine.GetGConfValue (GConfKeyWidth, GConfDefaultWidth);                
		int height = (int) Muine.GetGConfValue (GConfKeyHeight, GConfDefaultHeight);

		window.SetDefaultSize (width, height);

		window.SizeAllocated += new SizeAllocatedHandler (HandleSizeAllocated);

		play_button_image.SetFromStock ("stock_media-play", IconSize.Button);
		queue_button_image.SetFromStock ("stock_timer", IconSize.Button);

		view = new HandleView ();

		view.Selection.Mode = SelectionMode.Multiple;
		view.SortFunc = new HandleView.CompareFunc (SortFunc);
		view.RowActivated += new HandleView.RowActivatedHandler (HandleRowActivated);
		view.SelectionChanged += new HandleView.SelectionChangedHandler (HandleSelectionChanged);

		pixbuf_renderer = new CellRendererPixbuf ();
		view.AddColumn (pixbuf_renderer, new HandleView.CellDataFunc (PixbufCellDataFunc), false);
		text_renderer = new CellRendererText ();
		view.AddColumn (text_renderer, new HandleView.CellDataFunc (TextCellDataFunc), true);

		view.EnableModelDragSource (Gdk.ModifierType.Button1Mask, 
					    source_entries, Gdk.DragAction.Copy);
		view.DragDataGet += new DragDataGetHandler (DragDataGetCallback);

		scrolledwindow.Add (view);

		view.Realize ();
		view.Show ();

		nothing_pixbuf = new Gdk.Pixbuf (null, "muine-nothing.png");

		Muine.DB.AlbumAdded += new SongDatabase.AlbumAddedHandler (HandleAlbumAdded);
		Muine.DB.AlbumChanged += new SongDatabase.AlbumChangedHandler (HandleAlbumChanged);
		Muine.DB.AlbumRemoved += new SongDatabase.AlbumRemovedHandler (HandleAlbumRemoved);

		Muine.CoverDB.DoneLoading += new CoverDatabase.DoneLoadingHandler (HandleDoneLoading);

		foreach (Album a in Muine.DB.Albums.Values) 
			view.Append (a.Handle);
		SelectFirst ();

		view.DragDataReceived += new DragDataReceivedHandler (HandleDragDataReceived);
		view.DragMotion += new DragMotionHandler (HandleDragMotion);
		Gtk.Drag.DestSet (view, DestDefaults.All,
				  cover_drag_entries, Gdk.DragAction.Copy);
	}

	public void Run ()
	{
		search_entry.GrabFocus ();

		SelectFirst ();

		window.Present ();
	}

	public delegate void QueueAlbumsEventHandler (List songs);
	public event QueueAlbumsEventHandler QueueAlbumsEvent;
	
	public delegate void PlayAlbumsEventHandler (List songs);
	public event PlayAlbumsEventHandler PlayAlbumsEvent;

	private int SortFunc (IntPtr a_ptr,
			      IntPtr b_ptr)
	{
		Album a = Album.FromHandle (a_ptr);
		Album b = Album.FromHandle (b_ptr);

		return String.CompareOrdinal (a.SortKey, b.SortKey);
	}

	private void PixbufCellDataFunc (HandleView view,
					 CellRenderer cell,
					 IntPtr handle)
	{
		CellRendererPixbuf r = (CellRendererPixbuf) cell;
		Album album = Album.FromHandle (handle);

		if (album.CoverImage != null)
			r.Pixbuf = album.CoverImage;
		else if (Muine.CoverDB.Loading)
			r.Pixbuf = Muine.CoverDB.DownloadingPixbuf;
		else
			r.Pixbuf = nothing_pixbuf;

		r.Height = CoverDatabase.AlbumCoverSize + 5 * 2;
		r.Width = CoverDatabase.AlbumCoverSize + 5 * 2;
	}

	private void TextCellDataFunc (HandleView view,
				       CellRenderer cell,
				       IntPtr handle)
	{
		CellRendererText r = (CellRendererText) cell;
		Album album = Album.FromHandle (handle);

		string performers = "";
		if (album.Performers.Length > 0)
			performers = String.Format (Muine.Catalog.GetString ("Performed by {0}"), StringUtils.JoinHumanReadable (album.Performers, 2));

		r.Text = album.Name + "\n" + StringUtils.JoinHumanReadable (album.Artists, 3) + "\n\n" + performers;

		MarkupUtils.CellSetMarkup (r, 0, StringUtils.GetByteLength (album.Name),
					   false, true, false);
	}

	private void HandleWindowResponse (object o, EventArgs a)
	{
		ResponseArgs args = (ResponseArgs) a;

		switch ((int) args.ResponseId) {
		case 1: /* Play */
			window.Visible = false;

			if (PlayAlbumsEvent != null)
				PlayAlbumsEvent (view.SelectedPointers);

			Reset ();

			break;
		case 2: /* Queue */
			if (QueueAlbumsEvent != null)
				QueueAlbumsEvent (view.SelectedPointers);

			search_entry.GrabFocus ();

			view.SelectNext ();

			break;
		default:
			window.Visible = false;

			Reset ();

			break;
		}
	}

	private void HandleWindowDeleteEvent (object o, EventArgs a)
	{
		window.Visible = false;

		DeleteEventArgs args = (DeleteEventArgs) a;

		args.RetVal = true;

		Reset ();
	}

	private bool Search ()
	{
		List l = new List (IntPtr.Zero, typeof (int));

		if (search_entry.Text.Length > 0) {
			string [] search_bits = search_entry.Text.ToLower ().Split (' ');

			foreach (Album a in Muine.DB.Albums.Values) {
				if (a.FitsCriteria (search_bits))
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

	private uint search_idle_id = 0;

	private bool process_changes_immediately = false;
	
	private void HandleSearchEntryChanged (object o, EventArgs args)
	{
		if (process_changes_immediately)
			Search ();
		else {
			if (search_idle_id > 0)
				GLib.Source.Remove (search_idle_id);

			search_idle_id = GLib.Idle.Add (new GLib.IdleHandler (Search));
		}
	}

	private void HandleSearchEntryKeyPressEvent (object o, EventArgs a)
	{
		KeyPressEventArgs args = (KeyPressEventArgs) a;

		args.RetVal = view.ForwardKeyPress (search_entry, args.Event);
	}

	private void HandleSizeAllocated (object o, SizeAllocatedArgs args)
	{
		int width, height;

		window.GetSize (out width, out height);

		Muine.SetGConfValue (GConfKeyWidth, width);
		Muine.SetGConfValue (GConfKeyHeight, height);
	}

	private void HandleRowActivated (IntPtr handle)
	{
		play_button.Click ();
	}

	private void HandleSelectionChanged ()
	{
		bool has_sel = (view.SelectedPointers.Count > 0);
		
		play_button.Sensitive = has_sel;
		queue_button.Sensitive = has_sel;
	}

	private void HandleAlbumAdded (Album album)
	{
		string [] search_bits = search_entry.Text.ToLower ().Split (' ');
		if (album.FitsCriteria (search_bits))
			view.Append (album.Handle);
	}

	private void SelectFirst ()
	{
		scrolledwindow.Hadjustment.Value = 0.0;

		view.SelectFirst ();
	}

	private void SelectFirstIfNeeded ()
	{
		/* it is insensitive if we have no selection, see HandleSelectionChanged */
		if (!play_button.Sensitive)
			SelectFirst ();
	}

	private void HandleAlbumChanged (Album album)
	{
		string [] search_bits = search_entry.Text.ToLower ().Split (' ');
		if (album.FitsCriteria (search_bits)) {
			if (view.Contains (album.Handle))
				view.Changed (album.Handle);
			else
				view.Append (album.Handle);
		} else
			view.Remove (album.Handle);

		SelectFirstIfNeeded ();
	}

	private void HandleAlbumRemoved (Album album)
	{
		view.Remove (album.Handle);

		SelectFirstIfNeeded ();
	}

	private void HandleDragDataReceived (object o, DragDataReceivedArgs args)
	{
		TreePath path;

		if (!view.GetPathAtPos (args.X, args.Y, out path))
			return;

		IntPtr album_ptr = view.GetHandleFromPath (path);
		Album album = Album.FromHandle (album_ptr);

		CoverImage.HandleDrop ((Song) album.Songs [0], args);
	}

	private void HandleDragMotion (object o, DragMotionArgs args)
	{
		TreePath path;

		if (!view.GetPathAtPos (args.X, args.Y, out path))
			return;

		if (Muine.CoverDB.Loading)
			return;
		
		view.SetDragDestRow (path, Gtk.TreeViewDropPosition.IntoOrAfter);
	}

	private void HandleDoneLoading ()
	{
		view.QueueDraw ();
	}

	private void Reset ()
	{
		process_changes_immediately = true;
		
		search_entry.Text = "";

		process_changes_immediately = false;
	}

	private void DragDataGetCallback (object o, DragDataGetArgs args)
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
