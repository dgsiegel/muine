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

		window.Resize (width, height);

		play_button_image.SetFromStock ("muine-play", IconSize.Button);
		queue_button_image.SetFromStock ("muine-queue", IconSize.Button);

		view = new HandleView ();

		view.Reorderable = false;
		view.SortFunc = new HandleView.CompareFunc (SortFunc);
		view.RowActivated += new HandleView.RowActivatedHandler (HandleRowActivated);
		view.SelectionChanged += new HandleView.SelectionChangedHandler (HandleSelectionChanged);

		text_renderer = new CellRendererText ();
		view.AddColumn (text_renderer, new HandleView.CellDataFunc (CellDataFunc));

		view.Show ();

		scrolledwindow.Add (view);

		foreach (Song s in Muine.DB.Songs.Values)
			view.Append (s.Handle);

		Muine.DB.SongAdded += new SongDatabase.SongAddedHandler (HandleSongAdded);
		Muine.DB.SongRemoved += new SongDatabase.SongRemovedHandler (HandleSongRemoved);

		UpdateSelection ();

		HandleSelectionChanged ();

		/* FIXME multiple selection */
	}

	public void Run ()
	{
		search_entry.Text = "";
		search_entry.GrabFocus ();

		window.Visible = true;
	}

	private void UpdateSelection ()
	{
		/* FIXME: if no selection, select first (signal?) */
	}

	public delegate void QueueSongsEventHandler (ArrayList songs);
	public event QueueSongsEventHandler QueueSongsEvent;
	
	public delegate void PlaySongsEventHandler (ArrayList songs);
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
		Song song = Song.FromHandle (handle);
		CellRendererText r = (CellRendererText) cell;

		String title = String.Join (", ", song.Titles);

		r.Text = title + "\n" + String.Join (", ", song.Artists);

		MarkupUtils.CellSetMarkup (r, 0, StringUtils.GetByteLength (title),
					   false, true, false);
	}

	public delegate void SeekEventHandler (int sec);
	public event SeekEventHandler SeekEvent;

	private void HandleWindowResponse (object o, EventArgs a)
	{
		window.Visible = false;

		ResponseArgs args = (ResponseArgs) a;

		switch (args.ResponseId) {
		case 1: /* Play */
			if (PlaySongsEvent != null)
				PlaySongsEvent (null);

			break;
		case 2: /* Queue */
			if (QueueSongsEvent != null)
				QueueSongsEvent (null);
				
			break;
		default:
			return;
		}
	}

	private void HandleWindowDeleteEvent (object o, EventArgs a)
	{
		window.Visible = false;

		DeleteEventArgs args = (DeleteEventArgs) a;

		args.RetVal = true;
	}

	/* FIXME timeout ? */
	private void HandleSearchEntryChanged (object o, EventArgs args)
	{
		List l = new List (IntPtr.Zero, typeof (int));

		foreach (Song s in Muine.DB.Songs.Values) {
			string [] search_bits = search_entry.Text.ToLower ().Split (' ');

			int n_matches = 0;
			
			foreach (string search_bit in search_bits) {
				bool do_continue = false;

				foreach (string str in s.LowerTitles) {
					if (str.IndexOf (search_bit) >= 0) {
						n_matches++;
						do_continue = true;
						break;
					}
				}

				if (do_continue)
					continue;
	
				foreach (string str in s.LowerArtists) {
					if (str.IndexOf (search_bit) >= 0) {
						n_matches++;
						do_continue = true;
						break;
					}
				}

				if (do_continue)
					continue;

				if (s.LowerAlbum.IndexOf (search_bit) >= 0)
					n_matches++;
			}

			if (n_matches == search_bits.Length)
				l.Append (s.Handle);
		}

		view.RemoveDelta (l);

		foreach (int i in l) {
			IntPtr ptr = new IntPtr (i);

			view.Append (ptr);
		}

		UpdateSelection ();
	}

	private void HandleSearchEntryKeyPressEvent (object o, EventArgs a)
	{
		KeyPressEventArgs args = (KeyPressEventArgs) a;

		args.RetVal = false;
		
		if (KeyUtils.HaveModifier (args.Event.state))
			return;

		switch (args.Event.keyval) {
		case 0xFF52: /* up */
			/* FIXME */
			args.RetVal = true;
			Console.WriteLine ("up");
			break;
		case 0xFF54: /* down */
			/* FIXME */
			args.RetVal = true;
			Console.WriteLine ("down");
			break;
		default:
			break;
		}
	}

	private void HandleClearButtonClicked (object o, EventArgs args)
	{
		search_entry.Text = "";
		search_entry.GrabFocus ();
	}

	private void HandleConfigureEvent (object o, EventArgs args)
	{
		int width, height;

		window.GetSize (out width, out height);

		Muine.GConfClient.Set ("/apps/muine/add_song_window/width", width);
		Muine.GConfClient.Set ("/apps/muine/add_song_window/height", height);
	}

	private void HandleRowActivated (IntPtr handle)
	{
		Song song = Song.FromHandle (handle);

		if (PlaySongsEvent != null)
			PlaySongsEvent (null);

		window.Visible = false;
	}

	private void HandleSelectionChanged ()
	{
		bool has_sel = (view.Selection != IntPtr.Zero);
		
		play_button.Sensitive = has_sel;
		queue_button.Sensitive = has_sel;
	}

	private void HandleSongAdded (Song song)
	{
		view.Append (song.Handle);
	}

	private void HandleSongRemoved (Song song)
	{
		view.Remove (song.Handle);
	}
}
