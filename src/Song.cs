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
 * TODO FileSystemWatcher, file import wizard (musicbrainz for tags, cover image, filename fixing)
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

	private int track_number;
	public int TrackNumber {
		get {
			return track_number;
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

	private Gdk.Pixbuf cover_image;
	public Gdk.Pixbuf CoverImage {
		get {
			return cover_image;
		}
	}

	private string mime_type;
	public string MimeType {
		get {
			return mime_type;
		}
	}

	private long mtime;
	public long MTime {
		get {
			return mtime;
		}
	}

	[DllImport ("libglib-2.0-0.dll")]
	private static extern string g_utf8_collate_key (string str, int len);

	private string sort_key = null;
	public string SortKey {
		get {
			if (sort_key == null)
				sort_key = g_utf8_collate_key (AllLowerTitles, -1);
			
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

	private Gdk.Pixbuf tmp_cover_image;

	private bool checked_cover_image;

	private bool Proxy ()
	{
		checked_cover_image = true;

		Muine.DB.UpdateSong (this);
		
		if (tmp_cover_image == null)
			return false;

		cover_image = tmp_cover_image;

		Muine.CoverDB.ReplaceCover (album, cover_image);
		
		Muine.DB.EmitSongChanged (this);
		Muine.DB.AlbumChangedForSong (this);
		
		return false;
	}

	/* This is run from the action thread */
	private void FetchAlbumCover (Action action)
	{
		string artist = (string) action.UserData0;
		string album = (string) action.UserData1;

		string url = Muine.CoverDB.GetAlbumCoverURL (artist, album);

		if (url != null)
			tmp_cover_image = Muine.CoverDB.CoverPixbufFromURL (url);
		
		GLib.Idle.Add (new GLib.IdleHandler (Proxy));
	}

	private void GetCoverImage ()
	{
		checked_cover_image = true;

		if (album.Length == 0 || artists.Length == 0) {
			cover_image = null;
			return;
		}

		/* Check the cache first */
		if (Muine.CoverDB.Covers.ContainsKey (album)) {
			cover_image = (Gdk.Pixbuf) Muine.CoverDB.Covers [album];
			return;
		}

		/* Search for popular image names */
		FileInfo info = new FileInfo (filename);

		foreach (string fn in cover_filenames) {
			FileInfo cover = new FileInfo (info.DirectoryName + "/" + fn);
			
			if (cover.Exists) {
				cover_image = Muine.CoverDB.AddCoverLocal (album, cover.ToString ());
				if (cover_image != null)
					return;
			}
		}

		/* Failed to find a cover on disk - try the web */
		Action action = new Action ();
		/* This assumes the right artist is always in artists [0] */
		action.UserData0 = (object) artists [0];
		action.UserData1 = (object) album;
		action.Perform += new Action.PerformHandler (FetchAlbumCover);
		Muine.ActionThread.QueueAction (action);

		checked_cover_image = false;
			
		Muine.CoverDB.AddCoverDummy (album);
	}

	private IntPtr handle;
	public IntPtr Handle {
		get {
			return handle;
		}
	}

	private static Hashtable pointers = Hashtable.Synchronized (new Hashtable ());
	private static IntPtr cur_ptr = IntPtr.Zero;

	private ArrayList handles;

	/* support for having multiple handles to the same song,
	 * used for, for example, having the same song in the playlist
	 * more than once.
	 */
	public IntPtr RegisterExtraHandle ()
	{
		cur_ptr = new IntPtr (((int) cur_ptr) + 1);
		pointers [cur_ptr] = this;

		handles.Add (cur_ptr);

		return cur_ptr;
	}

	public bool IsExtraHandle (IntPtr h)
	{
		return ((pointers [h] == this) &&
		        (handle != h));
	}

	public ArrayList Handles {
		get {
			return handles;
		}
	}

	public void UnregisterExtraHandle (IntPtr handle)
	{
		handles.Remove (cur_ptr);

		pointers.Remove (handle);
	}

	public void Sync (Metadata metadata)
	{
		if (metadata.Titles.Length > 0)
			titles = metadata.Titles;
		else {
			titles = new string [1];

			FileInfo finfo = new FileInfo (filename);
			titles [0] = finfo.Name;
		}
		
		artists = metadata.Artists;
		album = metadata.Album;
		track_number = metadata.TrackNumber;
		year = metadata.Year;
		duration = metadata.Duration;
		mime_type = metadata.MimeType;
		mtime = metadata.MTime;

		GetCoverImage ();
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

		cur_ptr = new IntPtr (((int) cur_ptr) + 1);
		pointers [cur_ptr] = this;
		handle = cur_ptr;

		handles = new ArrayList ();
		handles.Add (cur_ptr);
	}

	[DllImport ("libmuine")]
        private static extern IntPtr db_unpack_string (IntPtr p, out string str);
        [DllImport ("libmuine")]
        private static extern IntPtr db_unpack_int (IntPtr p, out int i);
        [DllImport ("libmuine")]
        private static extern IntPtr db_unpack_long (IntPtr p, out long l);
        [DllImport ("libmuine")]
        private static extern IntPtr db_unpack_bool (IntPtr p, out bool b);

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
		p = db_unpack_int (p, out track_number);
		p = db_unpack_string (p, out year);
		p = db_unpack_long (p, out duration);
		p = db_unpack_string (p, out mime_type);
		p = db_unpack_long (p, out mtime);
		p = db_unpack_bool (p, out checked_cover_image);

		/* cover image */
		if (album.Length == 0 || artists.Length == 0)
			cover_image = null;
		else
			cover_image = (Gdk.Pixbuf) Muine.CoverDB.Covers [album];

		cur_ptr = new IntPtr (((int) cur_ptr) + 1);
		pointers [cur_ptr] = this;
		handle = cur_ptr;

		handles = new ArrayList ();
		handles.Add (cur_ptr);

		if (checked_cover_image == false)
			GetCoverImage ();
	}

	~Song ()
	{
		pointers.Remove (handle);
	}

	[DllImport ("libmuine")]
	private static extern IntPtr db_pack_start ();
	[DllImport ("libmuine")]
	private static extern void db_pack_string (IntPtr p, string str);
	[DllImport ("libmuine")]
	private static extern void db_pack_int (IntPtr p, int i);
	[DllImport ("libmuine")]
	private static extern void db_pack_long (IntPtr p, long l);
	[DllImport ("libmuine")]
	private static extern void db_pack_bool (IntPtr p, bool b);
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
		db_pack_int (p, track_number);
		db_pack_string (p, year);
		db_pack_long (p, duration);
		db_pack_string (p, mime_type);
		db_pack_long (p, mtime);
		db_pack_bool (p, checked_cover_image);
		
		return db_pack_end (p, out length);
	}

	public static Song FromHandle (IntPtr handle)
	{
		return (Song) pointers [handle];
	}
}
