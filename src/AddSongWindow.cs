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

public class AddSongWindow : Window
{
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

	private static int FakeLength = 150;

	private static TargetEntry [] source_entries = new TargetEntry [] {
		new TargetEntry ("MUINE_SONG_LIST", TargetFlags.App, (uint) PlaylistWindow.TargetType.SongList),
		new TargetEntry ("text/uri-list", 0, (uint) PlaylistWindow.TargetType.UriList)
	};
	
	public AddSongWindow () : base (IntPtr.Zero)
	{
		Glade.XML gxml = new Glade.XML (null, "AddWindow.glade", "window", null);
		gxml.Autoconnect (this);

		Raw = window.Handle;

		window.Title = Muine.Catalog.GetString ("Play Song");

		int width;
		try {
			width = (int) Muine.GConfClient.Get ("/apps/muine/add_song_window/width");
		} catch {
			width = 350;
		}

		int height;
		try {
			height = (int) Muine.GConfClient.Get ("/apps/muine/add_song_window/height");
		} catch {
			height = 300;
		}

		window.SetDefaultSize (width, height);

		window.SizeAllocated += new SizeAllocatedHandler (HandleSizeAllocated);

		play_button_image.SetFromStock ("stock_media-play", IconSize.Button);
		queue_button_image.SetFromStock ("stock_timer", IconSize.Button);

		view = new HandleView ();

		view.Selection.Mode = SelectionMode.Multiple;
		view.SortFunc = new HandleView.CompareFunc (SortFunc);
		view.RowActivated += new HandleView.RowActivatedHandler (HandleRowActivated);
		view.SelectionChanged += new HandleView.SelectionChangedHandler (HandleSelectionChanged);

		text_renderer = new CellRendererText ();
		view.AddColumn (text_renderer, new HandleView.CellDataFunc (CellDataFunc), true);

		view.EnableModelDragSource (Gdk.ModifierType.Button1Mask, 
					    source_entries, Gdk.DragAction.Copy);
		view.DragDataGet += new DragDataGetHandler (DragDataGetCallback);
	
		scrolledwindow.Add (view);

		view.Realize ();
		view.Show ();

		Muine.DB.SongAdded += new SongDatabase.SongAddedHandler (HandleSongAdded);
		Muine.DB.SongChanged += new SongDatabase.SongChangedHandler (HandleSongChanged);
		Muine.DB.SongRemoved += new SongDatabase.SongRemovedHandler (HandleSongRemoved);

		int i = 0;
		foreach (Song s in Muine.DB.Songs.Values) {
			view.Append (s.Handle);

			i++;
			if (i >= FakeLength)
				break;
		}
	}

	public void Run ()
	{
		search_entry.GrabFocus ();

		view.SelectFirst ();

		window.Present ();
	}

	public delegate void QueueSongsEventHandler (List songs);
	public event QueueSongsEventHandler QueueSongsEvent;
	
	public delegate void PlaySongsEventHandler (List songs);
	public event PlaySongsEventHandler PlaySongsEvent;

	private int SortFunc (IntPtr a_ptr,
			      IntPtr b_ptr)
	{
		Song a = Song.FromHandle (a_ptr);
		Song b = Song.FromHandle (b_ptr);

		return StringUtils.StrCmp (a.SortKey, b.SortKey);
	}

	private void CellDataFunc (HandleView view,
				   CellRenderer cell,
				   IntPtr handle)
	{
		CellRendererText r = (CellRendererText) cell;
		Song song = Song.FromHandle (handle);

		r.Text = song.Title + "\n" + StringUtils.JoinHumanReadable (song.Artists);

		MarkupUtils.CellSetMarkup (r, 0, StringUtils.GetByteLength (song.Title),
					   false, true, false);
	}

	private void HandleWindowResponse (object o, EventArgs a)
	{
		ResponseArgs args = (ResponseArgs) a;

		switch ((int) args.ResponseId) {
		case 1: /* Play */
			window.Visible = false;
			
			if (PlaySongsEvent != null)
				PlaySongsEvent (view.SelectedPointers);

			Reset ();

			break;
		case 2: /* Queue */
			if (QueueSongsEvent != null)
				QueueSongsEvent (view.SelectedPointers);
				
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

		int max_len = -1;

		/* show max. FakeLength songs if < 3 chars are entered. this is to fake speed. */
		if (search_entry.Text.Length < 3)
			max_len = FakeLength;

		int i = 0;
		if (search_entry.Text.Length > 0) {
			string [] search_bits = search_entry.Text.ToLower ().Split (' ');

			foreach (Song s in Muine.DB.Songs.Values) {
				if (s.FitsCriteria (search_bits)) {
					l.Append (s.Handle);
				
					i++;
					if (max_len > 0 && i >= max_len)
						break;
				}	
			}
		} else {
			foreach (Song s in Muine.DB.Songs.Values) {
				l.Append (s.Handle);
				
				i++;
				if (max_len > 0 && i >= max_len)
					break;
			}
		}

		view.RemoveDelta (l);

		foreach (int p in l) {
			IntPtr ptr = new IntPtr (p);

			view.Append (ptr);
		}

		view.SelectFirst ();

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

		Muine.GConfClient.Set ("/apps/muine/add_song_window/width", width);
		Muine.GConfClient.Set ("/apps/muine/add_song_window/height", height);
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

	private void HandleSongAdded (Song song)
	{
		if (search_entry.Text.Length < 3 && view.Length >= FakeLength)
			return;

		string [] search_bits = search_entry.Text.ToLower ().Split (' ');
		if (song.FitsCriteria (search_bits))
			view.Append (song.Handle);
	}

	private void SelectFirstIfNeeded ()
	{
		/* it is insensitive if we have no selection, see HandleSelectionChanged */
		if (play_button.Sensitive == false)
			view.SelectFirst ();
	}

	private void HandleSongChanged (Song song)
	{
		string [] search_bits = search_entry.Text.ToLower ().Split (' ');
		if (song.FitsCriteria (search_bits)) {
			if (view.Contains (song.Handle))
				view.Changed (song.Handle);
			else
				view.Append (song.Handle);
		} else
			view.Remove (song.Handle);

		SelectFirstIfNeeded ();
	}

	private void HandleSongRemoved (Song song)
	{
		view.Remove (song.Handle);

		SelectFirstIfNeeded ();
	}

	private void Reset ()
	{
		process_changes_immediately = true;
		
		search_entry.Text = "";

		process_changes_immediately = false;
	}

	private void DragDataGetCallback (object o, DragDataGetArgs args)
	{
		List songs = view.SelectedPointers;

		switch (args.Info) {
		case (uint) PlaylistWindow.TargetType.UriList:
			string files = "";

			foreach (int p in songs) {
				IntPtr s = new IntPtr (p);
				files += StringUtils.UriFromLocalPath (Song.FromHandle (s).Filename) + "\r\n";
			}
	
			args.SelectionData.Set (Gdk.Atom.Intern ("text/uri-list", false),
						8, System.Text.Encoding.UTF8.GetBytes (files));
						
			break;	
		case (uint) PlaylistWindow.TargetType.SongList:
			string ptrs = "\tMUINE_SONG_LIST\t";
			
			foreach (int p in songs) {
				IntPtr s = new IntPtr (p);
				ptrs += s.ToString () + "\r\n";
			}
			
			args.SelectionData.Set (Gdk.Atom.Intern ("MUINE_SONG_LIST", false),
					        8, System.Text.Encoding.ASCII.GetBytes (ptrs));
					
			break;
		default:
			break;	
		}
	}
}
