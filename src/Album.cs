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

	public ArrayList Artists;

	private ArrayList lower_artists = null;
	public ArrayList LowerArtists {
		get {
			if (lower_artists == null) {
				lower_artists = new ArrayList ();
				foreach (string str in Artists)
					lower_artists.Add (str.ToLower ());
			}

			return lower_artists;
		}
	}

	private string year;
	public string Year {
		get {
			return year;
		}
	}

	private string cover_image_filename;
	public string CoverImageFilename {
		get {
			return cover_image_filename;
		}
	}

	[DllImport ("libglib-2.0-0.dll")]
	private static extern string g_utf8_collate_key (string str, int len);

	private string sort_key = null;
	public string SortKey {
		get {
			if (sort_key == null)
				sort_key = g_utf8_collate_key (name, -1);
			
			return sort_key;
		}
	}

	private static Hashtable pointers = new Hashtable ();
	private static IntPtr cur_ptr = IntPtr.Zero;

	private static IntPtr handle;

	public Album (Song initial_song)
	{
		Songs = new ArrayList ();

		Songs.Add (initial_song);

		Artists = new ArrayList ();

		AddArtists (initial_song);

		name = String.Copy (initial_song.Album);
		year = String.Copy (initial_song.Year);
		cover_image_filename = String.Copy (initial_song.CoverImageFilename);

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
			if (Artists.BinarySearch (artist) < 0) {
				Artists.Add (artist);
			}
		}
	}

	public void AddSong (Song song)
	{
		Songs.Add (song);
		
		AddArtists (song);
	}

	/* returns true if empty now */
	public bool RemoveSong (Song song)
	{
		Songs.Remove (song);

		return (Songs.Count == 0);
	}
}
