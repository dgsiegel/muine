/*
 * Copyright © 2004 Jorn Baayen <jorn@nl.linux.org>
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
using System.Runtime.InteropServices;

public class Album
{
	private string name;
	public string Name {
		get {
			return name;
		}
	}

	public ArrayList Songs;

	private ArrayList artists;
	public string [] Artists {
		get {
			return (string []) artists.ToArray (typeof (string));
		}
	}

	private ArrayList performers;
	public string [] Performers {
		get {
			return (string []) performers.ToArray (typeof (string));
		}
	}

	private string year;
	public string Year {
		get {
			return year;
		}
	}

	public Gdk.Pixbuf CoverImage;

	[DllImport ("libglib-2.0-0.dll")]
	private static extern string g_utf8_collate_key (string str, int len);

	private string sort_key = null;
	public string SortKey {
		get {
			if (sort_key == null)
				sort_key = g_utf8_collate_key (SearchKey, -1);
			
			return sort_key;
		}
	}

	private string search_key = null;
	public string SearchKey {
		get {
			if (search_key == null) {
				/* need to keep this in the order for sorting too */
				string [] lower_artists = new string [artists.Count];
				string [] lower_performers = new string [performers.Count];
				for (int i = 0; i < artists.Count; i++)
					lower_artists [i] = ((string) artists [i]).ToLower ();
				for (int i = 0; i < performers.Count; i++)
					lower_performers [i] = ((string) performers [i]).ToLower ();
				search_key = String.Join (" ", lower_artists) + " " + name.ToLower () + " " + String.Join (" ", lower_performers);
			}

			return search_key;
		}
	}

	private static Hashtable pointers = new Hashtable ();
	private static IntPtr cur_ptr = IntPtr.Zero;

	private IntPtr handle;
	public IntPtr Handle {
		get {
			return handle;
		}
	}

	public Album (Song initial_song)
	{
		Songs = new ArrayList ();

		Songs.Add (initial_song);

		artists = new ArrayList ();
		performers = new ArrayList ();

		AddArtistsAndPerformers (initial_song);

		name = initial_song.Album;
		year = initial_song.Year;
		CoverImage = initial_song.CoverImage;

		cur_ptr = new IntPtr (((int) cur_ptr) + 1);
		pointers [cur_ptr] = this;
		handle = cur_ptr;
	}

	~Album ()
	{
		pointers.Remove (handle);
	}

	public static Album FromHandle (IntPtr handle)
	{
		return (Album) pointers [handle];
	}

	private bool AddArtistsAndPerformers (Song song)
	{
		bool artists_changed = false;
		bool performers_changed = false;
		
		foreach (string artist in song.Artists) {
			if (artists.Contains (artist) == false) {
				artists.Add (artist);
				artists_changed = true;
				search_key = null;
				sort_key = null;
			}
		}

		if (artists_changed == true)
			artists.Sort ();

		foreach (string performer in song.Performers) {
			if (performers.Contains (performer) == false) {
				performers.Add (performer);
				performers_changed = true;
				search_key = null;
				sort_key = null;
			}
		}

		if (performers_changed == true)
			performers.Sort ();

		return (artists_changed || performers_changed);
	}

	private class SongComparer : IComparer {
		int IComparer.Compare (object a, object b)
		{
			Song song_a = (Song) a;
			Song song_b = (Song) b;

			if (song_a.TrackNumber < song_b.TrackNumber)
				return -1;
			else if (song_a.TrackNumber > song_b.TrackNumber)
				return 1;
			else
				return 0;
		}
	}

	private static IComparer song_comparer = new SongComparer ();

	public void SyncCoverImageWith (Song song)
	{
		CoverImage = song.CoverImage;

		foreach (Song s in Songs) {
			s.CoverImage = CoverImage;
			Muine.DB.EmitSongChanged (s);
		}
	}

	public bool AddSong (Song song)
	{
		Songs.Add (song);
		Songs.Sort (song_comparer);

		if (CoverImage == null && song.CoverImage != null)
			SyncCoverImageWith (song);
		else
			song.CoverImage = CoverImage;
		
		return AddArtistsAndPerformers (song);
	}

	/* returns true if empty now */
	public bool RemoveSong (Song song)
	{
		Songs.Remove (song);

		return (Songs.Count == 0);
	}
}
