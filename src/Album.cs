/*
 * Copyright (C) 2004 Jorn Baayen <jbaayen@gnome.org>
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

using Gdk;

using Mono.Posix;

namespace Muine
{
	public class Album : Item
	{
		// Strings
		private static readonly string string_prefixes = 
			Catalog.GetString ("the dj");

		// Properties
		private string name;
		public string Name {
			get { return name; }
		}

		private ArrayList songs;
		public ArrayList Songs {
			get {
				lock (this) {
					return (ArrayList) songs.Clone ();
				}
			}
		}

		private ArrayList artists;
		public string [] Artists {
			get {
				lock (this) {
					return (string []) artists.ToArray (typeof (string));
				}
			}
		}

		private ArrayList performers;
		public string [] Performers {
			get {
				lock (this) {
					return (string []) performers.ToArray (typeof (string));
				}
			}
		}

		private string year;
		public string Year {
			get { return year; }
		}

		private Gdk.Pixbuf cover_image;
		public override Gdk.Pixbuf CoverImage {
			set {
				cover_image = value;

				foreach (Song s in songs)
					s.CoverImage = value;

				Global.DB.EmitAlbumChanged (this);
			}

			get { return cover_image; }
		}

		private string folder;

		public string Key {
			get {
				return Global.DB.MakeAlbumKey (folder, name);
			}
		}

		private static string [] prefixes = null;

		public Album (Song initial_song, bool check_cover)
		{
			songs = new ArrayList ();

			songs.Add (initial_song);

			artists = new ArrayList ();
			performers = new ArrayList ();

			AddArtistsAndPerformers (initial_song);

			name = initial_song.Album;
			year = initial_song.Year;

			folder = initial_song.Folder;

			cur_ptr = new IntPtr (((int) cur_ptr) + 1);
			pointers [cur_ptr] = this;
			base.handle = cur_ptr;

			if (check_cover) {
				cover_image = GetCover (initial_song);

				initial_song.SetCoverImageQuiet (cover_image);
			}
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
				if (!artists.Contains (artist)) {
					artists.Add (artist);

					artists_changed = true;
					search_key = null;
					sort_key = null;
				}
			}

			if (artists_changed)
				artists.Sort ();

			foreach (string performer in song.Performers) {
				if (!performers.Contains (performer)) {
					performers.Add (performer);

					performers_changed = true;
					search_key = null;
					sort_key = null;
				}
			}

			if (performers_changed)
				performers.Sort ();

			return (artists_changed || performers_changed);
		}

		private class SongComparer : IComparer {
			int IComparer.Compare (object a, object b)
			{
				Song song_a = (Song) a;
				Song song_b = (Song) b;

				if (song_a.DiscNumber < song_b.DiscNumber)
					return -1;
				else if (song_a.DiscNumber > song_b.DiscNumber)
					return 1;
				else {
					if (song_a.TrackNumber < song_b.TrackNumber)
						return -1;
					else if (song_a.TrackNumber > song_b.TrackNumber)
						return 1;
					else 
						return 0;
				}
			}
		}

		private static IComparer song_comparer = new SongComparer ();

		public void Add (Song song,
				 bool check_cover,
		                 out bool changed,
				 out bool songs_changed)
		{
			changed = false;
			songs_changed = false;

			lock (this) {
				if (check_cover) {
					if (cover_image == null && song.CoverImage != null) {
						// This is to pick up any embedded album covers
						changed = true;
						songs_changed = true;
				
						cover_image = song.CoverImage;
						foreach (Song s in Songs)
							s.SetCoverImageQuiet (cover_image);
					} else
						song.SetCoverImageQuiet (cover_image);
				}

				if (year.Length == 0 && song.Year.Length > 0) {
					year = song.Year;

					changed = true;
				}

				bool artists_changed = AddArtistsAndPerformers (song);
				if (artists_changed)
					changed = true;

				songs.Add (song);
				songs.Sort (song_comparer);
			}
		}

		// returns true if the album is now empty
		public bool Remove (Song song)
		{
			lock (this) {
				songs.Remove (song);

				if (songs.Count > 0)
					return false;

				pointers.Remove (base.handle);

				if (!FileUtils.IsFromRemovableMedia (folder))
					Global.CoverDB.RemoveCover (Key);

				return true;
			}
		}

		public bool FitsCriteria (string [] search_bits)
		{
			int n_matches = 0;
				
			foreach (string search_bit in search_bits) {
				if (SearchKey.IndexOf (search_bit) >= 0) {
					n_matches++;
					continue;
				}
			}

			return (n_matches == search_bits.Length);
		}

		private Pixbuf GetCover (Song initial_song)
		{
			string key = Key;

			Pixbuf pixbuf = (Pixbuf) Global.CoverDB.Covers [key];
			if (pixbuf != null)
				return pixbuf;

			pixbuf = initial_song.CoverImage;
			if (pixbuf != null)
				return pixbuf; // embedded cover image

			pixbuf = Global.CoverDB.Getter.GetFolderImage (key, folder);
			if (pixbuf != null)
				return pixbuf;

			return Global.CoverDB.Getter.GetAmazon (this);
		}

		public void SetCoverLocal (string file)
		{
			CoverImage = Global.CoverDB.Getter.GetLocal (Key, file);
		}

		public void SetCoverWeb (string url)
		{
			CoverImage = Global.CoverDB.Getter.GetWeb (Key, url,
					new CoverGetter.GotCoverDelegate (OnGotCover));
		}

		private void OnGotCover (Pixbuf pixbuf)
		{
			CoverImage = pixbuf;
		}

		private static Hashtable pointers = new Hashtable ();
		private static IntPtr cur_ptr = IntPtr.Zero;
		
		protected override string GenerateSortKey ()
		{
			if (prefixes == null) {
				/* Space-separated list of prefixes that will be taken off the front
				 * when sorting. For example, "The Beatles" will be sorted as "Beatles",
				 * if "the" is included in this list. Also include the English "the"
				 * if English is generally spoken in your country. */
				prefixes = string_prefixes.Split (' ');
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

			/* more than three artists, sort by album name
			 * three or less artists, sort by artist */

			string key = (artists.Count > 3)
				     ? String.Format ("{0} {1} {2} {3}", name.ToLower (), year, a, p)
				     : String.Format ("{0} {1} {2} {3}", a, p, year, name.ToLower ());

			return StringUtils.CollateKey (key);
		}				

		protected override string GenerateSearchKey ()
		{
			string a = String.Join (" ", Artists).ToLower ();
			string p = String.Join (" ", Performers).ToLower ();

			return String.Format ("{0} {1} {2}", name.ToLower (), a, p);
		}
	}
}
