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
using System.Net;
using System.Threading;
using System.Runtime.InteropServices;

public class Song
{
	private string filename;
	public string Filename {
		get {
			return filename;
		}
	}
		
	private string title;
	public string Title {
		get {
			return title;
		}
	}

	private string [] artists;
	public string [] Artists {
		get {
			return artists;
		}
	}

	private string [] performers;
	public string [] Performers {
		get {
			return performers;
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
		set {
			cover_image = value;

			if (cover_image != null && cover_image != Muine.CoverDB.DownloadingPixbuf)
				checked_cover_image = true;
		}
		
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

	private double gain;
	public double Gain {
		get {
			return gain;
		}
	}

	private double peak;
	public double Peak {
		get {
			return peak;
		}
	}

	[DllImport ("libglib-2.0-0.dll")]
	private static extern string g_utf8_collate_key (string str, int len);

	private string sort_key = null;
	public string SortKey {
		get {
			if (sort_key == null)
				sort_key = g_utf8_collate_key (title, -1);
			
			return sort_key;
		}
	}

	private string search_key = null;
	public string SearchKey {
		get {
			if (search_key == null) {
				string [] lower_artists = new string [artists.Length + performers.Length];
				for (int i = 0; i < artists.Length; i++)
					lower_artists [i] = artists [i].ToLower ();
				for (int i = 0; i < performers.Length; i++)
					lower_artists [artists.Length + i] = performers [i].ToLower ();
				search_key = String.Join (" ", lower_artists) + " " + title.ToLower ();
			}

			return search_key;
		}
	}

	public string AlbumKey {
		get {
			if (album.Length == 0)
				return null;
				
			FileInfo finfo = new FileInfo (filename);
			return finfo.DirectoryName + ":" + album;
		}
	}

	private static string [] cover_filenames = {
		"cover.jpg",
		"Cover.jpg",
		"cover.jpeg",
		"Cover.jpeg",
		"cover.png",
		"Cover.png",
		"folder.jpg",
		"Folder.jpg",
		"cover.gif",
		"Cover.gif"
	};

	private Gdk.Pixbuf tmp_cover_image;

	private bool checked_cover_image;

	private bool Proxy ()
	{
		if (checked_cover_image == true) {
			tmp_cover_image = null;
			return false;
		}

		checked_cover_image = true;

		Muine.DB.UpdateSong (this);

		cover_image = tmp_cover_image;
		tmp_cover_image = null;
		
		Muine.CoverDB.ReplaceCover (AlbumKey, cover_image);
		
		Muine.DB.AlbumChangedForSong (this);
		
		return false;
	}

	/* This is run from the action thread */
	private void FetchAlbumCover (Action action)
	{
		string artist = (string) action.UserData0;
		string album = (string) action.UserData1;
		string url = null;

		try {
			url = Muine.CoverDB.GetAlbumCoverURL (artist, album);
		} catch (WebException e) {
			/* Temporary web problem (Timeout etc.) - re-queue */
			Thread.Sleep (60000); /* wait for a minute first */
			Muine.ActionThread.QueueAction (action);
			
			return;
		} catch (Exception e) {
			url = null;
		}

		if (url != null) {
			try {
				tmp_cover_image = Muine.CoverDB.CoverPixbufFromURL (url);
			} catch (WebException e) {
				/* Temporary web problem (Timeout etc.) - re-queue */
				Thread.Sleep (60000); /* wait for a minute first */
				Muine.ActionThread.QueueAction (action);
				
				return;
			} catch (Exception e) {
				tmp_cover_image = null;
			}
		}

		GLib.Idle.Add (new GLib.IdleHandler (Proxy));
	}

	private void GetCoverImage ()
	{
		checked_cover_image = true;

		if (album.Length == 0) {
			cover_image = null;
			return;
		}

		/* Check the cache first */
		if (Muine.CoverDB.Covers.ContainsKey (AlbumKey)) {
			cover_image = (Gdk.Pixbuf) Muine.CoverDB.Covers [AlbumKey];
			return;
		}

		/* Search for popular image names */
		FileInfo info = new FileInfo (filename);

		foreach (string fn in cover_filenames) {
			FileInfo cover = new FileInfo (info.DirectoryName + "/" + fn);
			
			if (cover.Exists) {
				cover_image = Muine.CoverDB.AddCoverLocal (AlbumKey, cover.ToString ());
				if (cover_image != null)
					return;
			}
		}

		if (artists.Length == 0) {
			cover_image = null;
			return;
		}

		/* Failed to find a cover on disk - try the web */
		Action action = new Action ();
		/* This assumes the right artist is always in artists [0] */
		action.UserData0 = (object) artists [0];
		action.UserData1 = (object) album;
		action.Perform += new Action.PerformHandler (FetchAlbumCover);
		Muine.ActionThread.QueueAction (action);

		checked_cover_image = false;

		cover_image = Muine.CoverDB.AddCoverDownloading (AlbumKey);
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
		if (metadata.Title.Length > 0)
			title = metadata.Title;
		else {
			FileInfo finfo = new FileInfo (filename);
			title = finfo.Name;
		}
		
		artists = metadata.Artists;
		performers = metadata.Performers;
		album = metadata.Album;
		track_number = metadata.TrackNumber;
		year = metadata.Year;
		duration = metadata.Duration;
		mime_type = metadata.MimeType;
		mtime = metadata.MTime;
		gain = metadata.Gain;
		peak = metadata.Peak;

		sort_key = null;
		search_key = null;

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
        [DllImport ("libmuine")]
        private static extern IntPtr db_unpack_double (IntPtr p, out double d);

	public Song (string fn,
	             IntPtr data)
	{
		IntPtr p = data;
		int len;

		filename = fn;

		p = db_unpack_string (p, out title);

		p = db_unpack_int (p, out len);
		artists = new string [len];
		for (int i = 0; i < len; i++) {
			p = db_unpack_string (p, out artists [i]);
		}

		p = db_unpack_int (p, out len);
		performers = new string [len];
		for (int i = 0; i < len; i++) {
			p = db_unpack_string (p, out performers [i]);
		}

		p = db_unpack_string (p, out album);
		p = db_unpack_int (p, out track_number);
		p = db_unpack_string (p, out year);
		p = db_unpack_long (p, out duration);
		p = db_unpack_string (p, out mime_type);
		p = db_unpack_long (p, out mtime);
		p = db_unpack_bool (p, out checked_cover_image);
		p = db_unpack_double (p, out gain);
		p = db_unpack_double (p, out peak);

		/* cover image */
		if (album.Length == 0)
			cover_image = null;
		else
			cover_image = (Gdk.Pixbuf) Muine.CoverDB.Covers [AlbumKey];

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
	private static extern void db_pack_double (IntPtr p, double d);
	[DllImport ("libmuine")]
	private static extern IntPtr db_pack_end (IntPtr p, out int length);

	public IntPtr Pack (out int length)
	{
		IntPtr p;

		p = db_pack_start ();
		
		db_pack_string (p, title);
		
		db_pack_int (p, artists.Length);
		foreach (string artist in artists) {
			db_pack_string (p, artist);
		}
		
		db_pack_int (p, performers.Length);
		foreach (string performer in performers) {
			db_pack_string (p, performer);
		}
		
		db_pack_string (p, album);
		db_pack_int (p, track_number);
		db_pack_string (p, year);
		db_pack_long (p, duration);
		db_pack_string (p, mime_type);
		db_pack_long (p, mtime);
		db_pack_bool (p, checked_cover_image);
		db_pack_double (p, gain);
		db_pack_double (p, peak);
		
		return db_pack_end (p, out length);
	}

	public static Song FromHandle (IntPtr handle)
	{
		return (Song) pointers [handle];
	}
}
