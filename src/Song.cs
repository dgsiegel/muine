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
using System.IO;
using System.Net;
using System.Threading;

using Gdk;

using Muine.PluginLib;

namespace Muine
{
	public class Song : Item, ISong
	{
		private string filename;
		public string Filename {
			get { return filename; }
		}

		public string Folder {
			get {
				return Path.GetDirectoryName (filename);
			}
		}
		
		private string title;
		public string Title {
			get { return title; }
		}

		private string [] artists;
		public string [] Artists {
			get { return artists; }
		}

		private string [] performers;
		public string [] Performers {
			get { return performers; }
		}

		private string album;
		public string Album {
			get { return album; }
		}

		public bool HasAlbum {
			get {
				return (album != null && album.Length > 0);
			}
		}

		private int track_number;
		public int TrackNumber {
			get { return track_number; }
		}

		private int disc_number;
		public int DiscNumber {
			get { return disc_number; }
		}

		private string year;
		public string Year {
			get { return year; }
		}

		private int duration;
		public int Duration {
			/* we have a setter too, because sometimes we want
			 * to correct the duration. */
			set { duration = value; }
		
			get { return duration; }
		}

		private Gdk.Pixbuf cover_image;
		public override Gdk.Pixbuf CoverImage {
			set {
				cover_image = value;

				Global.DB.EmitSongChanged (this);
			}
		
			get { return cover_image; }
		}

		public void SetCoverImageQuiet (Pixbuf cover_image)
		{
			this.cover_image = cover_image;
		}
	
		private int mtime;
		public int MTime {
			get { return mtime; }
		}

		private double gain;
		public double Gain {
			get { return gain; }
		}

		private double peak;
		public double Peak {
			get { return peak; }
		}

		public string AlbumKey {
			get {
				return Global.DB.MakeAlbumKey (Folder, album);
			}
		}

		private bool dead = false;
		public bool Dead {
			get { return dead; }
		}

		public void Deregister ()
		{
			dead = true;

			pointers.Remove (this.Handle);

			foreach (IntPtr extra_handle in handles)
				pointers.Remove (extra_handle);
				
			if (!HasAlbum && !FileUtils.IsFromRemovableMedia (filename))
				Global.CoverDB.RemoveCover (filename);
		}

		public new IntPtr Handle {
			get { return (IntPtr) handles [0]; }
		}

		private ArrayList handles;
		public ArrayList Handles {
			get { return handles; }
		}

		/* support for having multiple handles to the same song,
		 * used for, for example, having the same song in the playlist
		 * more than once.
		 */
		public IntPtr RegisterHandle ()
		{
			cur_ptr = new IntPtr (((int) cur_ptr) + 1);
			pointers [cur_ptr] = this;

			handles.Add (cur_ptr);

			return cur_ptr;
		}
	
		public IntPtr RegisterExtraHandle ()
		{
			return RegisterHandle ();
		}

		public void UnregisterExtraHandle (IntPtr handle)
		{
			handles.Remove (cur_ptr);

			pointers.Remove (handle);
		}

		public bool IsExtraHandle (IntPtr h)
		{
			return ((pointers [h] == this) &&
				(Handle != h));
		}

		public void Sync (Metadata metadata)
		{
			bool had_album = HasAlbum;

			if (metadata.Title.Length > 0)
				title = metadata.Title;
			else
				title = Path.GetFileNameWithoutExtension (filename);
			
			artists = metadata.Artists;
			performers = metadata.Performers;
			album = metadata.Album;
			track_number = metadata.TrackNumber;
			disc_number = metadata.DiscNumber;
			year = metadata.Year;
			duration = metadata.Duration;
			mtime = metadata.MTime;
			gain = metadata.Gain;
			peak = metadata.Peak;

			/* we need to do cover stuff here too, as we support setting covers
			   to songs that are not associated with any album. and, we also need
			   this to support ID3 embedded cover images.
			   Note that CoverDB has the required thread safety for us to be able
			   to do this from a thread. */
			if (!had_album && HasAlbum && cover_image != null) {
				/* This used to be a single song, but not anymore, and it does
				   have a cover- migrate the cover to the album, if there is
				   none there yet */
				Global.CoverDB.RemoveCover (filename);

				string akey = AlbumKey;
				if (Global.CoverDB.Covers [akey] == null)
					Global.CoverDB.SetCover (akey, cover_image);
			} else if (!HasAlbum) /* See if there is a cover for this single song */
				cover_image = (Pixbuf) Global.CoverDB.Covers [filename];

			if (cover_image == null && metadata.AlbumArt != null) {
				/* Look for an ID3 embedded cover image, if it is there, and no
				   cover image is set yet, set it as cover image if it is a single
				   song, or as album cover image if it belongs to an album */
				string key = HasAlbum ? AlbumKey : filename;

				if (Global.CoverDB.Covers [key] == null)
					cover_image = Global.CoverDB.Getter.GetEmbedded (key, metadata.AlbumArt);
				/* Album itself will pick up change when this song is added to it */
			}

			sort_key = null;
			search_key = null;
		}

		public Song (string fn)
		{
			filename = fn;

			Metadata metadata;
				
			try {
				metadata = new Metadata (filename);
			} catch (Exception e) {
				throw e;
			}

			Sync (metadata);

			handles = new ArrayList ();

			RegisterHandle ();
		}

		public Song (string fn, IntPtr data)
		{
			IntPtr p = data;
			int len;

			filename = fn;

			p = Database.UnpackString (p, out title);

			p = Database.UnpackInt (p, out len);
			artists = new string [len];
			for (int i = 0; i < len; i++)
				p = Database.UnpackString (p, out artists [i]);

			p = Database.UnpackInt (p, out len);
			performers = new string [len];
			for (int i = 0; i < len; i++)
				p = Database.UnpackString (p, out performers [i]);

			p = Database.UnpackString (p, out album);
			p = Database.UnpackInt (p, out track_number);
			p = Database.UnpackInt (p, out disc_number);
			p = Database.UnpackString (p, out year);
			p = Database.UnpackInt (p, out duration);
			p = Database.UnpackInt (p, out mtime);
			p = Database.UnpackDouble (p, out gain);
			p = Database.UnpackDouble (p, out peak);

			/* cover image is loaded later */

			handles = new ArrayList ();

			RegisterHandle ();
		}

		public IntPtr Pack (out int length)
		{
			IntPtr p;
			
			p = Database.PackStart ();

			Database.PackString (p, title);

			Database.PackInt (p, artists.Length);
			foreach (string artist in artists)
				Database.PackString (p, artist);

			Database.PackInt (p, performers.Length);
			foreach (string performer in performers)
				Database.PackString (p, performer);
			
			Database.PackString (p, album);
			Database.PackInt (p, track_number);
			Database.PackInt (p, disc_number);
			Database.PackString (p, year);
			Database.PackInt (p, duration);
			Database.PackInt (p, mtime);
			Database.PackDouble (p, gain);
			Database.PackDouble (p, peak);

			return Database.PackEnd (p, out length);
		}

		public static Song FromHandle (IntPtr handle)
		{
			return (Song) pointers [handle];
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

		/* Only call these if it is a single song */
		public void SetCoverLocal (string file)
		{
			CoverImage = Global.CoverDB.Getter.GetLocal (filename, file);
		}

		public void SetCoverWeb (string url)
		{
			CoverImage = Global.CoverDB.Getter.GetWeb (filename, url,
				new CoverGetter.GotCoverDelegate (OnGotCover));
		}

		private void OnGotCover (Pixbuf pixbuf)
		{
			CoverImage = pixbuf;
		}

		private static Hashtable pointers = Hashtable.Synchronized (new Hashtable ());
		private static IntPtr cur_ptr = IntPtr.Zero;
		
		protected override string GenerateSortKey ()
		{
			string a = String.Join (" ", artists).ToLower ();
			string p = String.Join (" ", performers).ToLower ();
				
			return StringUtils.CollateKey (title.ToLower () + " " + a + " " + p);
		}

		protected override string GenerateSearchKey ()
		{
			string a = String.Join (" ", artists).ToLower ();
			string p = String.Join (" ", performers).ToLower ();
				
			return title.ToLower () + " " + a + " " + p + " " + album.ToLower ();
		}
	}
}
