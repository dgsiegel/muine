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
	public class AddSongWindow : AddWindow
	{
		// Constants
		private const int FakeLength = 150;
		private const int MinQueryLength = 3;

	        private const string GConfKeyWidth = "/apps/muine/add_song_window/width";
	        private const int GConfDefaultWidth = 500;
	        
	        private const string GConfKeyHeight = "/apps/muine/add_song_window/height";
	        private const int GConfDefaultHeight = 475;  

		// Strings
		private static readonly string string_play_song = 
			Catalog.GetString ("Play Song");

		// DnD targets	
		private static TargetEntry [] source_entries = new TargetEntry [] {
			DndUtils.TargetMuineSongList,
			DndUtils.TargetUriList
		};

		// Constructor	
		public AddSongWindow ()
		{
			base.Title = string_play_song;

			base.SetGConfSize (GConfKeyWidth, GConfKeyHeight, GConfDefaultWidth, GConfDefaultHeight);
			
			base.List.SortFunc = new HandleView.CompareFunc (SortFunc);
			
			base.List.AddColumn (base.TextRenderer, new AddWindowList.CellDataFunc (CellDataFunc), true);

			base.List.DragSource = source_entries;
			base.List.DragDataGet += new DragDataGetHandler (OnDragDataGet);
		
			Global.DB.SongAdded   += new SongDatabase.SongAddedHandler   (OnSongAdded);
			Global.DB.SongChanged += new SongDatabase.SongChangedHandler (OnSongChanged);
			Global.DB.SongRemoved += new SongDatabase.SongRemovedHandler (OnSongRemoved);

			lock (Global.DB) {
				int i = 0;

				foreach (Song s in Global.DB.Songs.Values) {
					base.List.Append (s.Handle);

					i++;
					if (i >= FakeLength)
						break;
				}
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
			if (base.Entry.Text.Length < MinQueryLength)
				max_len = FakeLength;

			lock (Global.DB) {
				int i = 0;
				if (base.Entry.Text.Length > 0) {
					foreach (Song s in Global.DB.Songs.Values) {
						if (!s.FitsCriteria (base.Entry.SearchBits))
							continue;

						l.Append (s.Handle);
					
						i++;
						if (max_len > 0 && i >= max_len)
							break;
					}
				} else {
					foreach (Song s in Global.DB.Songs.Values) {
						l.Append (s.Handle);
					
						i++;
						if (max_len > 0 && i >= max_len)
							break;
					}
				}
			}

			base.List.RemoveDelta (l);

			foreach (int p in l) {
				IntPtr ptr = new IntPtr (p);

				base.List.Append (ptr);
			}

			base.List.SelectFirst ();

			return false;
		}

		private void OnSongAdded (Item item)
		{
			if (base.Entry.Text.Length < MinQueryLength &&
			    base.List.Length >= FakeLength)
				return;

			base.List.HandleAdded (item.Handle, item.FitsCriteria (base.Entry.SearchBits));
		}

		private void OnSongChanged (Item item)
		{
			bool may_append = (base.Entry.Text.Length >= MinQueryLength ||
			                   base.List.Length < FakeLength);
			
			base.List.HandleChanged (item.Handle, item.FitsCriteria (base.Entry.SearchBits),
				may_append);
		}

		private void OnSongRemoved (Item item)
		{
			base.List.HandleRemoved (item.Handle);
		}

		private void OnDragDataGet (object o, DragDataGetArgs args)
		{
			List songs = base.List.SelectedPointers;

			switch (args.Info) {
			case (uint) DndUtils.TargetType.UriList:
				string files = "";

				foreach (int p in songs) {
					IntPtr s = new IntPtr (p);
					files += FileUtils.UriFromLocalPath (Song.FromHandle (s).Filename) + "\r\n";
				}
		
				args.SelectionData.Set (Gdk.Atom.Intern (DndUtils.TargetUriList.Target, false),
							8, System.Text.Encoding.UTF8.GetBytes (files));
							
				break;	
				
			case (uint) DndUtils.TargetType.SongList:
				string ptrs = String.Format ("\t{0}\t", DndUtils.TargetMuineSongList.Target);
				
				foreach (int p in songs) {
					IntPtr s = new IntPtr (p);
					ptrs += s.ToString () + "\r\n";
				}
				
				args.SelectionData.Set (Gdk.Atom.Intern (DndUtils.TargetMuineSongList.Target, false),
						        8, System.Text.Encoding.ASCII.GetBytes (ptrs));

				break;

			default:
				break;	
			}
		}
	}
}
