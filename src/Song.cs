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
 *
 * TODO fam (wrapped C code), file import wizard (musicbrainz for tags, cover image, filename fixing)
 * TODO use gnomevfs pixbuf loader for cover images
 */

using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;

public class Song
{
	private string filename;
	public string Filename {
		get {
			return filename;
		}
	}
		
	private string [] titles;
	public string [] Titles {
		get {
			return titles;
		}
	}

	private string all_lower_titles = null;
	public string AllLowerTitles {
		get {
			if (all_lower_titles == null) {
				string [] lower_titles = new string [titles.Length];
				for (int i = 0; i < titles.Length; i++)
					lower_titles [i] = titles [i].ToLower ();
				all_lower_titles = String.Join (", ", lower_titles);
			}

			return all_lower_titles;
		}
	}

	private string [] artists;
	public string [] Artists {
		get {
			return artists;
		}
	}

	private string all_lower_artists = null;
	public string AllLowerArtists {
		get {
			if (all_lower_artists == null) {
				string [] lower_artists = new string [artists.Length];
				for (int i = 0; i < artists.Length; i++)
					lower_artists [i] = artists [i].ToLower ();
				all_lower_artists = String.Join (", ", lower_artists);
			}

			return all_lower_artists;
		}
	}

	private string album;
	public string Album {
		get {
			return album;
		}
	}

	private string lower_album = null;
	public string LowerAlbum {
		get {
			if (lower_album == null)
				lower_album = album.ToLower ();

			return lower_album;
		}
	}

	private string year;
	public string Year {
		get {
			return year;
		}
	}

	private long duration;
	public long Duration {
		/* we have a setter too, because sometimes we want
		 * to correct the duration. */
		set {
			duration = value;
		}
		
		get {
			return duration;
		}
	}

	private string cover_image_filename;
	public string CoverImageFilename {
		get {
			return cover_image_filename;
		}
	}

	private string mime_type;
	public string MimeType {
		get {
			return mime_type;
		}
	}

	[DllImport ("libglib-2.0-0.dll")]
	private static extern string g_utf8_collate_key (string str, int len);

	private string sort_key = null;
	public string SortKey {
		get {
			if (sort_key == null)
				sort_key = g_utf8_collate_key (titles [0], -1);
			
			return sort_key;
		}
	}

	private static string [] cover_filenames = {
		"cover.jpg",
		"Cover.jpg",
		"cover.jpeg",
		"Cover.jpeg",
		"cover.png",
		"Cover.png",
		"cover.gif",
		"Cover.gif"
	};

	/* TODO vfs-ify */
	/* TODO nautilus dir image */
	private void GetCoverImageFilename ()
	{
		FileInfo info = new FileInfo (filename);

		cover_image_filename = "";

		foreach (string fn in cover_filenames) {
			FileInfo cover = new FileInfo (info.DirectoryName + "/" + fn);
			
			if (cover.Exists) {
				cover_image_filename = cover.ToString ();
				break;
			}
		}
	}

	private IntPtr handle;
	public IntPtr Handle {
		get {
			return handle;
		}
	}

	private static Hashtable pointers = new Hashtable ();
	private static IntPtr cur_ptr = IntPtr.Zero;

	/* support for having multiple handles to the same song,
	 * used for, for example, having the same song in the playlist
	 * more than once.
	 */
	public IntPtr RegisterExtraHandle ()
	{
		cur_ptr = new IntPtr (((int) cur_ptr) + 1);
		pointers [cur_ptr] = this;

		return cur_ptr;
	}

	public bool IsExtraHandle (IntPtr h)
	{
		return ((pointers [h] == this) &&
		        (handle != h));
	}

	public void UnregisterExtraHandle (IntPtr handle)
	{
		pointers.Remove (handle);
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

		titles = metadata.Titles;
		if (titles.Length == 0) {
			titles = new string [1];
			titles[0] = "Untitled";
		}
				
		artists = metadata.Artists;
		if (artists.Length == 0) {
			artists = new string [1];
			artists[0] = "Unknown";
		}
			
		album = metadata.Album;
		year = metadata.Year;
		duration = metadata.Duration;
		mime_type = metadata.MimeType;

		GetCoverImageFilename ();

		cur_ptr = new IntPtr (((int) cur_ptr) + 1);
		pointers [cur_ptr] = this;
		handle = cur_ptr;
	}

	[DllImport ("libmuine")]
        private static extern IntPtr db_unpack_string (IntPtr p, out string str);
        [DllImport ("libmuine")]
        private static extern IntPtr db_unpack_int (IntPtr p, out int i);
        [DllImport ("libmuine")]
        private static extern IntPtr db_unpack_long (IntPtr p, out long l);

	public Song (string fn,
	             IntPtr data)
	{
		IntPtr p = data;
		int len;

		filename = fn;

		p = db_unpack_int (p, out len);
		titles = new string [len];
		for (int i = 0; i < len; i++) {
			p = db_unpack_string (p, out titles [i]);
		}

		p = db_unpack_int (p, out len);
		artists = new string [len];
		for (int i = 0; i < len; i++) {
			p = db_unpack_string (p, out artists [i]);
		}

		p = db_unpack_string (p, out album);
		p = db_unpack_string (p, out year);
		p = db_unpack_long (p, out duration);
		p = db_unpack_string (p, out cover_image_filename);
		p = db_unpack_string (p, out mime_type);

		cur_ptr = new IntPtr (((int) cur_ptr) + 1);
		pointers [cur_ptr] = this;
		handle = cur_ptr;
	}

	~Song ()
	{
		pointers.Remove (handle);
	}

	[DllImport ("libmuine")]
	private static extern IntPtr db_pack_start ();
	[DllImport ("libmuine")]
	private static extern IntPtr db_pack_string (IntPtr p, string str);
	[DllImport ("libmuine")]
	private static extern IntPtr db_pack_int (IntPtr p, int i);
	[DllImport ("libmuine")]
	private static extern IntPtr db_pack_long (IntPtr p, long l);
	[DllImport ("libmuine")]
	private static extern IntPtr db_pack_end (IntPtr p, out int length);

	public IntPtr Pack (out int length)
	{
		IntPtr p;

		p = db_pack_start ();
		
		db_pack_int (p, titles.Length);
		foreach (string title in titles) {
			db_pack_string (p, title);
		}
		
		db_pack_int (p, artists.Length);
		foreach (string artist in artists) {
			db_pack_string (p, artist);
		}
		
		db_pack_string (p, album);
		db_pack_string (p, year);
		db_pack_long (p, duration);
		db_pack_string (p, cover_image_filename);
		db_pack_string (p, mime_type);
		
		return db_pack_end (p, out length);
	}

	public static Song FromHandle (IntPtr handle)
	{
		return (Song) pointers [handle];
	}
}
