/*
 * Copyright (C) 2004, 2005 Jorn Baayen <jbaayen@gnome.org>
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
		// Strings :: Prefixes

		//      Space-separated list of prefixes that will be taken off the front
		// 	when sorting. For example, "The Beatles" will be sorted as "Beatles",
		// 	if "the" is included in this list. Also include the English "the"
		// 	if English is generally spoken in your country.
		private static readonly string string_prefixes = Catalog.GetString ("the dj");

		// Static
		// Static :: Methods
		// Static :: Methods :: FromHandle
		public static Album FromHandle (IntPtr handle)
		{
			return (Album) pointers [handle];
		}

		// Internal Classes
		// Internal Classes :: SongComparer
		private class SongComparer : IComparer {
			int IComparer.Compare (object a, object b)
			{
				Song song_a = (Song) a;
				Song song_b = (Song) b;

				int ret = song_a.DiscNumber.CompareTo (song_b.DiscNumber);
				
				if (ret == 0)
					ret = song_a.TrackNumber.CompareTo (song_b.TrackNumber);
				
				return ret;
			}
		}

		// Objects
		private IComparer  song_comparer = new SongComparer ();
		private Gdk.Pixbuf cover_image;

		// Variables
		private string name;
		private ArrayList songs;
		private ArrayList artists;
		private ArrayList performers;
		private string year;
		private string folder;
		private int n_tracks;
		private int total_n_tracks;
		private bool complete = false;

		private static Hashtable pointers = new Hashtable ();
		private static IntPtr cur_ptr = IntPtr.Zero;
		
		// Constructor
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

			n_tracks = 1;
			total_n_tracks = initial_song.NAlbumTracks;

			CheckCompleteness ();

			cur_ptr = new IntPtr (((int) cur_ptr) + 1);
			pointers [cur_ptr] = this;
			base.handle = cur_ptr;

			if (check_cover) {
				cover_image = GetCover (initial_song);

				initial_song.SetCoverImageQuiet (cover_image);
			}
		}

		// Properties
		// Properties :: Name (get;)
		public string Name {
			get { return name; }
		}

		// Properties :: Songs (get;)
		public ArrayList Songs {
			get {
				lock (this) {
					return (ArrayList) songs.Clone ();
				}
			}
		}

		// Properties :: Artists (get;)
		public string [] Artists {
			get {
				lock (this) {
					return (string []) artists.ToArray (typeof (string));
				}
			}
		}

		// Properties :: Performers (get;)
		public string [] Performers {
			get {
				lock (this) {
					return (string []) performers.ToArray (typeof (string));
				}
			}
		}

		// Properties :: Year (get;)
		public string Year {
			get { return year; }
		}

		// Properties :: CoverImage (set; get;) (Item)
		public override Gdk.Pixbuf CoverImage {
			set {
				cover_image = value;

				foreach (Song s in songs)
					s.CoverImage = value;

				Global.DB.EmitAlbumChanged (this);
			}

			get { return cover_image; }
		}

		// Properties :: Public (get;)
		public override bool Public {
			get { return complete; }
		}

		// Properties :: Key (get;)
		public string Key {
			get { return Global.DB.MakeAlbumKey (folder, name); }
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: Add
		public void Add (Song song, bool check_cover,
		                 out bool changed, out bool songs_changed)
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

				if (total_n_tracks != song.NAlbumTracks &&
				    song.NAlbumTracks > 0) {
					total_n_tracks = song.NAlbumTracks;

					changed = true;
				}

				n_tracks ++;

				bool complete_changed = CheckCompleteness ();
				if (complete_changed)
					changed = true;
			}
		}

		// Methods :: Public :: Remove
		public void Remove (Song song, out bool changed, out bool empty)
		{
			changed = false;

			lock (this) {
				n_tracks --;

				bool complete_changed = CheckCompleteness ();
				if (complete_changed)
					changed = true;

				songs.Remove (song);

				bool artists_changed = RemoveArtistsAndPerformers (song);
				if (artists_changed)
					changed = true;

				empty = (n_tracks == 0);
				if (empty) {
					pointers.Remove (base.handle);

					if (!FileUtils.IsFromRemovableMedia (folder))
						Global.CoverDB.RemoveCover (Key);
				}
			}
		}

		// Methods :: Public :: SetCoverLocal
		public void SetCoverLocal (string file)
		{
			CoverImage = Global.CoverDB.Getter.GetLocal (Key, file);
		}

		// Methods :: Public :: SetCoverWeb
		public void SetCoverWeb (string url)
		{
			CoverImage = Global.CoverDB.Getter.GetWeb (Key, url,
					new CoverGetter.GotCoverDelegate (OnGotCover));
		}

		// Methods :: Protected
		// Methods :: Protected :: GenerateSortKey
		protected override string GenerateSortKey ()
		{
			string [] prefixes = string_prefixes.Split (' ');

			string [] p_artists = new string [artists.Count];
			for (int i = 0; i < artists.Count; i ++) {
				p_artists [i] = ((string) artists [i]).ToLower ();
				
				foreach (string prefix in prefixes) {
					if (!p_artists [i].StartsWith (prefix + " "))
						continue;

					p_artists [i] = StringUtils.PrefixToSuffix (p_artists [i], prefix);
					break;
				}
			}

			string [] p_performers = new string [performers.Count];
			for (int i = 0; i < performers.Count; i ++) {
				p_performers [i] = ((string) performers [i]).ToLower ();
				
				foreach (string prefix in prefixes) {
					if (!p_performers [i].StartsWith (prefix + " "))
						continue;

					p_performers [i] = StringUtils.PrefixToSuffix (p_performers [i], prefix);
					break;
				}
			}

			string a = String.Join (" ", p_artists);
			string p = String.Join (" ", p_performers);

			// more than three artists, sort by album name
			// three or less artists, sort by artist
			string key;
			if (artists.Count > 3)
				key = String.Format ("{0} {1} {2} {3}", name.ToLower (), year, a, p);
			else
				key = String.Format ("{0} {1} {2} {3}", a, p, year, name.ToLower ());

			return GUnicode.Unistring.GetCollateKey (key);
		}				

		// Methods :: Protected :: GenerateSearchKey
		protected override string GenerateSearchKey ()
		{
			string a = String.Join (" ", Artists);
			string p = String.Join (" ", Performers);

			string key = String.Format ("{0} {1} {2}", name, a, p);

			return StringUtils.SearchKey (key);
		}

		// Methods :: Private
		// Methods :: Private :: AddArtistsAndPerformers
		private bool AddArtistsAndPerformers (Song song)
		{
			bool artists_changed = false;
			bool performers_changed = false;
			
			foreach (string artist in song.Artists) {
				if (!artists.Contains (artist)) {
					artists.Add (artist);

					artists_changed = true;
				}
			}

			if (artists_changed)
				artists.Sort ();

			foreach (string performer in song.Performers) {
				if (!performers.Contains (performer)) {
					performers.Add (performer);

					performers_changed = true;
				}
			}

			if (performers_changed)
				performers.Sort ();

			bool changed = (artists_changed || performers_changed);

			if (changed) {
				search_key = null;
				sort_key = null;
			}

			return changed;
		}

		// Methods :: Private :: RemoveArtistsAndPerformers
		private bool RemoveArtistsAndPerformers (Song song)
		{
			bool artists_changed = false;
			bool performers_changed = false;

			foreach (string artist in song.Artists) {
				bool found = false;

				foreach (Song s in songs) {
					foreach (string s_artist in s.Artists) {
						if (artist == s_artist) {
							found = true;
							break;
						}
					}

					if (found)
						break;
				}

				if (!found) {
					artists.Remove (artist);

					artists_changed = true;
				}
			}

			foreach (string performer in song.Performers) {
				bool found = false;

				foreach (Song s in songs) {
					foreach (string s_performer in s.Performers) {
						if (performer == s_performer) {
							found = true;
							break;
						}
					}

					if (found)
						break;
				}

				if (!found) {
					performers.Remove (performer);

					performers_changed = true;
				}
			}

			bool changed = (artists_changed || performers_changed);

			if (changed) {
				search_key = null;
				sort_key = null;
			}

			return changed;
		}

		// Methods :: Private :: GetCover
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

		// Methods :: Private :: HaveHalfAlbum
		private bool HaveHalfAlbum (int total_n_tracks, int n_tracks)
		{
			int min_n_tracks = (int) Math.Ceiling (total_n_tracks / 2);
			
			return (n_tracks >= min_n_tracks);
		}

		// Methods :: Private :: CheckCompleteness
		//	Returns true if completeness changed
		private bool CheckCompleteness ()
		{
			bool new_complete = false;

			if (total_n_tracks > 0) {
				int delta = total_n_tracks - n_tracks;
			
				if (delta <= 0)
					new_complete = true;
				else
					new_complete = HaveHalfAlbum (total_n_tracks, n_tracks);
			} else {
				// Take track number of last song
				Song last_song = (Song) songs [songs.Count - 1];
				int last_track = last_song.TrackNumber;

				if (last_track == 1) {
					// If we are dealing with a potential one-song album,
					// we only let it through if it is at least 10 minutes
					// long. This is to work around the case where any single
					// song with track number '1' would be seen as an album.
					if (n_tracks == 1 && last_song.Duration >= 600)
						new_complete = true;
				} else if (last_track > 1) {
					int delta = last_track - n_tracks;
					
					if (delta <= 0)
						new_complete = true;
					else if (last_track >= 8) {
						// Only do the half album checking if we have at least
						// 8 tracks. Otherwise too much rubbish falls through.
						new_complete = HaveHalfAlbum (last_track, n_tracks);
					}
				}
			}

			bool changed = (new_complete != complete);
			
			complete = new_complete;
			
			return changed;
		}

		// Handlers
		// Handlers :: OnGotCover
		private void OnGotCover (Pixbuf pixbuf)
		{
			CoverImage = pixbuf;
		}
	}
}
