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

	private string lower_name = null;
	public string LowerName {
		get {
			if (lower_name == null)
				lower_name = name.ToLower ();

			return lower_name;
		}
	}

	public ArrayList Songs;

	public ArrayList artists;
	public string [] Artists {
		get {
			return (string []) artists.ToArray (typeof (string));
		}
	}

	private string all_lower_artists = null;
	public string AllLowerArtists {
		get {
			if (all_lower_artists == null) {
				string [] lower_artists = new string [artists.Count];
				for (int i = 0; i < artists.Count; i++)
					lower_artists [i] = ((string) artists [i]).ToLower ();
				all_lower_artists = String.Join (", ", lower_artists);
			}

			return all_lower_artists;
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
			/* + name because we first sort on artist, then on album name */
			if (sort_key == null)
				sort_key = g_utf8_collate_key (AllLowerArtists + name, -1);
			
			return sort_key;
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

		AddArtists (initial_song);

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

	private void AddArtists (Song song)
	{
		foreach (string artist in song.Artists) {
			if (artists.Contains (artist) == false) {
				artists.Add (artist);
				all_lower_artists = null;
			}
		}

		artists.Sort ();
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

	public void AddSong (Song song)
	{
		Songs.Add (song);
		Songs.Sort (song_comparer);
		
		AddArtists (song);
	}

	/* returns true if empty now */
	public bool RemoveSong (Song song)
	{
		Songs.Remove (song);

		return (Songs.Count == 0);
	}
}
