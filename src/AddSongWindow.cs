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

public class AddSongWindow : AddWindow
{
	private const int FakeLength = 150;
	private const int MinQueryLength = 3;

        private const string GConfKeyWidth = "/apps/muine/add_song_window/width";
        private const int GConfDefaultWidth = 500;
        
        private const string GConfKeyHeight = "/apps/muine/add_song_window/height";
        private const int GConfDefaultHeight = 475;  

	// DnD targets	
	private static TargetEntry [] source_entries = new TargetEntry [] {
		Muine.TargetMuineSongList,
		Muine.TargetUriList
	};

	// Constructor	
	public AddSongWindow ()
	{
		window.Title = Muine.Catalog.GetString ("Play Song");

		SetGConfSize (GConfKeyWidth, GConfKeyHeight, GConfDefaultWidth, GConfDefaultHeight);
		
		view.SortFunc = new HandleView.CompareFunc (SortFunc);
		
		view.AddColumn (text_renderer, new HandleView.CellDataFunc (CellDataFunc), true);

		view.EnableModelDragSource (Gdk.ModifierType.Button1Mask, source_entries, Gdk.DragAction.Copy);
		view.DragDataGet += new DragDataGetHandler (OnDragDataGet);
	
		Muine.DB.SongAdded   += new SongDatabase.SongAddedHandler   (OnSongAdded);
		Muine.DB.SongChanged += new SongDatabase.SongChangedHandler (OnSongChanged);
		Muine.DB.SongRemoved += new SongDatabase.SongRemovedHandler (OnSongRemoved);

		int i = 0;
		foreach (Song s in Muine.DB.Songs.Values) {
			view.Append (s.Handle);

			i++;
			if (i >= FakeLength)
				break;
		}
	}

	private int SortFunc (IntPtr a_ptr,
			      IntPtr b_ptr)
	{
		Song a = Song.FromHandle (a_ptr);
		Song b = Song.FromHandle (b_ptr);

		return String.CompareOrdinal (a.SortKey, b.SortKey);
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

	protected override bool Search ()
	{
		List l = new List (IntPtr.Zero, typeof (int));

		int max_len = -1;

		/* show max. FakeLength songs if < MinQueryLength chars are entered. this is to fake speed. */
		if (search_entry.Text.Length < MinQueryLength)
			max_len = FakeLength;

		int i = 0;
		if (search_entry.Text.Length > 0) {
			foreach (Song s in Muine.DB.Songs.Values) {
				if (!s.FitsCriteria (SearchBits))
					continue;

				l.Append (s.Handle);
				
				i++;
				if (max_len > 0 && i >= max_len)
					break;
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

		SelectFirst ();

		return false;
	}

	private void OnSongAdded (Song song)
	{
		if (search_entry.Text.Length < MinQueryLength &&
		    view.Length >= FakeLength)
			return;

		base.HandleAdded (song.Handle, song.FitsCriteria (SearchBits));
	}

	private void OnSongChanged (Song song)
	{
		bool may_append = (search_entry.Text.Length >= MinQueryLength ||
		                   view.Length < FakeLength);
		
		base.HandleChanged (song.Handle, song.FitsCriteria (SearchBits),
		                    may_append);
	}

	private void OnSongRemoved (Song song)
	{
		base.HandleRemoved (song.Handle);
	}

	private void OnDragDataGet (object o, DragDataGetArgs args)
	{
		List songs = view.SelectedPointers;

		switch (args.Info) {
		case (uint) Muine.TargetType.UriList:
			string files = "";

			foreach (int p in songs) {
				IntPtr s = new IntPtr (p);
				files += FileUtils.UriFromLocalPath (Song.FromHandle (s).Filename) + "\r\n";
			}
	
			args.SelectionData.Set (Gdk.Atom.Intern (Muine.TargetUriList.Target, false),
						8, System.Text.Encoding.UTF8.GetBytes (files));
						
			break;	
			
		case (uint) Muine.TargetType.SongList:
			string ptrs = String.Format ("\t{0}\t", Muine.TargetMuineSongList.Target);
			
			foreach (int p in songs) {
				IntPtr s = new IntPtr (p);
				ptrs += s.ToString () + "\r\n";
			}
			
			args.SelectionData.Set (Gdk.Atom.Intern (Muine.TargetMuineSongList.Target, false),
					        8, System.Text.Encoding.ASCII.GetBytes (ptrs));

			break;

		default:
			break;	
		}
	}
}
