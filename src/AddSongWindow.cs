/*
 * Copyright © 2003, 2004 Jorn Baayen <jorn@nl.linux.org>
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
using GtkSharp;
using GLib;

public class AddSongWindow
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
	
	public AddSongWindow (Window parent)
	{
		Glade.XML gxml = new Glade.XML (null, "AddWindow.glade", "window", null);
		gxml.Autoconnect (this);

		window.TransientFor = parent;

		window.Title = "Add Song";

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

		play_button_image.SetFromStock ("muine-play", IconSize.Button);
		queue_button_image.SetFromStock ("muine-queue", IconSize.Button);

		view = new HandleView ();

		view.Reorderable = false;
		view.Selection.Mode = SelectionMode.Multiple;
		view.SortFunc = new HandleView.CompareFunc (SortFunc);
		view.RowActivated += new HandleView.RowActivatedHandler (HandleRowActivated);
		view.SelectionChanged += new HandleView.SelectionChangedHandler (HandleSelectionChanged);

		text_renderer = new CellRendererText ();
		view.AddColumn (text_renderer, new HandleView.CellDataFunc (CellDataFunc));

		scrolledwindow.Add (view);

		view.Realize ();
		view.Show ();

		Muine.DB.SongAdded += new SongDatabase.SongAddedHandler (HandleSongAdded);
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
		view.ScrollToPoint (0, 0);

		if (window.Visible == false)
			window.Visible = true;
		else
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

		return String.Compare (a.SortKey, b.SortKey);
	}

	private void CellDataFunc (HandleView view,
				   CellRenderer cell,
				   IntPtr handle)
	{
		CellRendererText r = (CellRendererText) cell;
		Song song = Song.FromHandle (handle);

		string title = String.Join (", ", song.Titles);

		r.Text = title + "\n" + String.Join (", ", song.Artists);

		MarkupUtils.CellSetMarkup (r, 0, StringUtils.GetByteLength (title),
					   false, true, false);
	}

	private void HandleWindowResponse (object o, EventArgs a)
	{
		window.Visible = false;

		ResponseArgs args = (ResponseArgs) a;

		switch (args.ResponseId) {
		case 1: /* Play */
			if (PlaySongsEvent != null)
				PlaySongsEvent (view.SelectedPointers);

			break;
		case 2: /* Queue */
			if (QueueSongsEvent != null)
				QueueSongsEvent (view.SelectedPointers);
				
			break;
		default:
			break;
		}

		search_entry.Text = "";
	}

	private void HandleWindowDeleteEvent (object o, EventArgs a)
	{
		window.Visible = false;

		DeleteEventArgs args = (DeleteEventArgs) a;

		args.RetVal = true;

		search_entry.Text = "";
	}

	private bool FitsCriteria (Song s, string [] search_bits)
	{
		int n_matches = 0;
			
		foreach (string search_bit in search_bits) {
			if (s.AllLowerTitles.IndexOf (search_bit) >= 0) {
				n_matches++;
				continue;
			}

			if (s.AllLowerArtists.IndexOf (search_bit) >= 0) {
				n_matches++;
				continue;
			}
		}

		return (n_matches == search_bits.Length);
	}

	private void HandleSearchEntryChanged (object o, EventArgs args)
	{
		List l = new List (IntPtr.Zero, typeof (int));

		int max_len = -1;

		/* show max. FakeLength songs if < 3 chars are entered. this is to fake speed. */
		if (search_entry.Text.Length < 3)
			max_len = FakeLength;

		string [] search_bits = search_entry.Text.ToLower ().Split (' ');

		int i = 0;
		foreach (Song s in Muine.DB.Songs.Values) {
			if (FitsCriteria (s, search_bits)) {
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
		view.ScrollToPoint (0, 0);
	}

	private void HandleSearchEntryKeyPressEvent (object o, EventArgs a)
	{
		KeyPressEventArgs args = (KeyPressEventArgs) a;

		args.RetVal = false;
		
		if (KeyUtils.HaveModifier (args.Event.state))
			return;

		switch (args.Event.keyval) {
		case 0xFF52: /* up */
			view.SelectPrevious ();
			args.RetVal = true;
			break;
		case 0xFF54: /* down */
			view.SelectNext ();
			args.RetVal = true;
			break;
		default:
			break;
		}
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
		Song song = Song.FromHandle (handle);

		if (song == null)
			return;

		if (PlaySongsEvent != null)
			PlaySongsEvent (view.SelectedPointers);

		window.Visible = false;

		search_entry.Text = "";
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
		if (FitsCriteria (song, search_bits))
			view.Append (song.Handle);
	}

	private void HandleSongRemoved (Song song)
	{
		view.Remove (song.Handle);
	}
}
