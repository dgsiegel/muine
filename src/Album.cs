/*
 * Copyright (C) 2004 Jorn Baayen <jorn@nl.linux.org>
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

	public Gdk.Pixbuf cover_image;
	public Gdk.Pixbuf CoverImage {
		set {
			cover_image = value;

			foreach (Song s in Songs) {
				s.CoverImage = CoverImage;

				Muine.DB.UpdateSong (s);
			}
		}

		get {
			return cover_image;
		}
	}

	private static string [] prefixes = null;

	private string sort_key = null;
	public string SortKey {
		get {
			if (sort_key == null) {
				if (prefixes == null) {
					/* Space-separated list of prefixes that will be taken off the front
					 * when sorting. For example, "The Beatles" will be sorted as "Beatles",
					 * if "the" is included in this list. Also include the English "the"
					 * if English is generally spoken in your country. */
					prefixes = Muine.Catalog.GetString ("the dj").Split (' ');
				}
					
				string [] p_artists = new string [artists.Count];
				for (int i = 0; i < artists.Count; i++) {
					p_artists [i] = ((string) artists [i]).ToLower ();
					
					foreach (string prefix in prefixes) {
						if (p_artists [i].StartsWith (prefix + " ")) {
							p_artists [i] = StringUtils.PrefixToSuffix (p_artists [i], prefix);

							break;
						}
					}
				}

				string [] p_performers = new string [performers.Count];
				for (int i = 0; i < performers.Count; i++) {
					p_performers [i] = ((string) performers [i]).ToLower ();
					
					foreach (string prefix in prefixes) {
						if (p_performers [i].StartsWith (prefix + " ")) {
							p_performers [i] = StringUtils.PrefixToSuffix (p_performers [i], prefix);

							break;
						}
					}
				}

				string a = String.Join (" ", p_artists);
				string p = String.Join (" ", p_performers);

				if (artists.Count > 3) {
					/* more than three artists, sort by album name */
					sort_key = StringUtils.CollateKey (name.ToLower () + " " + year + " " + a + " " + p);
				} else {
					/* three or less artists, sort by artist */
					sort_key = StringUtils.CollateKey (a + " " + p + " " + year + " " + name.ToLower ());
				}
			}
			
			return sort_key;
		}
	}

	private string search_key = null;
	public string SearchKey {
		get {
			if (search_key == null) {
				string a = String.Join (" ", Artists).ToLower ();
				string p = String.Join (" ", Performers).ToLower ();

				search_key = name.ToLower () + " " + a + " " + p;
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
		cover_image = initial_song.CoverImage;
		year = initial_song.Year;

		cur_ptr = new IntPtr (((int) cur_ptr) + 1);
		pointers [cur_ptr] = this;
		handle = cur_ptr;
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

	public void AddSong (Song song, out bool album_changed)
	{
		Songs.Add (song);
		Songs.Sort (song_comparer);

		bool cover_changed = false;
		if (CoverImage == null && song.CoverImage != null) {
			CoverImage = song.CoverImage;

			cover_changed = true;
		} else
			song.CoverImage = CoverImage;

		bool year_changed = false;
		if (year.Length == 0 && song.Year.Length > 0) {
			year = song.Year;

			year_changed = true;
		}

		bool artists_changed = AddArtistsAndPerformers (song);

		album_changed = (cover_changed || artists_changed || year_changed);
	}

	public void RemoveSong (Song song, out bool album_empty)
	{
		Songs.Remove (song);

		album_empty = (Songs.Count == 0);

		if (album_empty)
			pointers.Remove (handle);
	}
}
