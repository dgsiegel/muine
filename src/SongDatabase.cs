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
using System.IO;
using System.Runtime.InteropServices;

using Gnome;

public class SongDatabase 
{
	private IntPtr dbf;

	public Hashtable Songs; 

	public Hashtable Albums;

	private delegate void DecodeFuncDelegate (string key, IntPtr data, IntPtr user_data);
	
	[DllImport ("libmuine")]
	private static extern IntPtr db_open (string filename, out string error);
	[DllImport ("libmuine")]
	private static extern void db_foreach (IntPtr dbf, DecodeFuncDelegate decode_func,
					       IntPtr user_data);

	public SongDatabase ()
	{
		DirectoryInfo dinfo = new DirectoryInfo (User.DirGet () + "/muine");
		if (!dinfo.Exists) {
			try {
				dinfo.Create ();
			} catch (Exception e) {
				throw e;
			}
		}
		
		string filename = dinfo.ToString () + "/songs.db";

		string error = null;

		dbf = db_open (filename, out error);

		if (dbf == IntPtr.Zero) {
			throw new Exception ("Failed to open database: " + error);
		}

		Songs = Hashtable.Synchronized (new Hashtable ());
		Albums = new Hashtable ();
	}

	public delegate void SongAddedHandler (Song song);
	public event SongAddedHandler SongAdded;

	public delegate void SongChangedHandler (Song song);
	public event SongChangedHandler SongChanged;

	public delegate void SongRemovedHandler (Song song);
	public event SongRemovedHandler SongRemoved;

	public delegate void AlbumAddedHandler (Album album);
	public event AlbumAddedHandler AlbumAdded;

	public delegate void AlbumChangedHandler (Album album);
	public event AlbumChangedHandler AlbumChanged;
	
	public delegate void AlbumRemovedHandler (Album album);
	public event AlbumRemovedHandler AlbumRemoved;

	private void HandleDirectory (DirectoryInfo info,
				      Queue new_songs)
	{
		foreach (FileInfo finfo in info.GetFiles ()) {
			if (Songs [finfo.ToString ()] == null) {
				Song song;

				try {
					song = new Song (finfo.ToString ());
				} catch {
					continue;
				}

				new_songs.Enqueue (song);
			}
		}

		foreach (DirectoryInfo dinfo in info.GetDirectories ())
			HandleDirectory (dinfo, new_songs);
	}

	private Queue removed_songs;
	private Queue changed_songs;
	private Queue new_songs;

	private class ChangedSong {
		public Metadata Metadata;
		public Song Song;

		public ChangedSong (Song song, Metadata md) {
			Song = song;
			Metadata = md;
		}
	}

	/* this is run from the main thread */
	private bool Proxy ()
	{
		if (removed_songs.Count > 0) {
			Song song = (Song) removed_songs.Dequeue ();
			RemoveSong (song);
			return true;
		}

		if (changed_songs.Count > 0) {
			ChangedSong cs = (ChangedSong) changed_songs.Dequeue ();
			cs.Song.Sync (cs.Metadata);
			UpdateSong (cs.Song);
			return true;
		}

		if (new_songs.Count > 0) {
			Song song = (Song) new_songs.Dequeue ();
			AddSong (song);
			return true;
		}

		return false;
	}

	/* this is run from the action thread */
	private void CheckChanges (Action action)
	{
		/* check for removed songs and changes */
		removed_songs = new Queue ();
		changed_songs = new Queue ();
		foreach (string file in Songs.Keys) {
			FileInfo finfo = new FileInfo (file);
			Song song = (Song) Songs [file];
			
			if (!finfo.Exists)
				removed_songs.Enqueue (song);
			else {
				/* mtime is in seconds (Pow (10, (9 - 2)) 100-nanosecond units) */
				long jorns_constant = 621356040000000000; /* mtime starts at 1970, Ticks at 0001 */
				long mtime_ticks = song.MTime * (long) Math.Pow (10, 7) + jorns_constant;
				if (mtime_ticks < finfo.LastWriteTime.Ticks) {
					Metadata metadata;

					try {
						metadata = new Metadata (song.Filename);
					} catch {
						removed_songs.Enqueue (song);
						continue;
					}
					
					ChangedSong cs = new ChangedSong (song, metadata);
					changed_songs.Enqueue (cs);
				}
			}
		}

		/* check for new songs */
		string [] folders;
		try {
			folders = (string []) Muine.GConfClient.Get ("/apps/muine/watched_folders");
		} catch {
			folders = new string [0];
		}

		new_songs = new Queue ();
		foreach (string folder in folders) {
			DirectoryInfo dinfo = new DirectoryInfo (folder);
			if (!dinfo.Exists)
				continue;

			HandleDirectory (dinfo, new_songs);
		}
		
		GLib.Timeout.Add (10, new GLib.TimeoutHandler (Proxy));
	}

	public void Load ()
	{
		db_foreach (dbf, new DecodeFuncDelegate (DecodeFunc), IntPtr.Zero);

		/* check for changes */
		Action action = new Action ();
		action.Perform += new Action.PerformHandler (CheckChanges);
		Muine.ActionThread.QueueAction (action);
	}
	
	private void DecodeFunc (string key, IntPtr data, IntPtr user_data)
	{
		Song song = new Song (key, data);

		Muine.DB.Songs.Add (key, song);

		Muine.DB.DoAlbum (song, false);
	}

	private delegate IntPtr EncodeFuncDelegate (IntPtr handle, out int length);

	[DllImport ("libmuine")]
	private static extern void db_store (IntPtr dbf, string key, bool overwrite,
					     EncodeFuncDelegate encode_func,
					     IntPtr user_data);

	public void AddSong (Song song)
	{
		db_store (dbf, song.Filename, false,
		          new EncodeFuncDelegate (EncodeFunc), song.Handle);

		Songs.Add (song.Filename, song);

		DoAlbum (song, true);

		if (SongAdded != null)
			SongAdded (song);
	}

	[DllImport ("libmuine")]
	private static extern void db_delete (IntPtr dbf, string key);

	public void RemoveSong (Song song)
	{
		db_delete (dbf, song.Filename);

		if (SongRemoved != null)
			SongRemoved (song);

		Songs.Remove (song);

		RemoveFromAlbum (song);
	}

	private IntPtr EncodeFunc (IntPtr handle, out int length)
	{
		Song song = Song.FromHandle (handle);

		return song.Pack (out length);
	}

	public void UpdateSong (Song song)
	{
		db_store (dbf, song.Filename, true,
			  new EncodeFuncDelegate (EncodeFunc), song.Handle);
	
		/* update album */
		RemoveFromAlbum (song);
		DoAlbum (song, true);

		EmitSongChanged (song);
	}

	public void EmitSongChanged (Song song)
	{
		if (SongChanged != null)
			SongChanged (song);
	}

	public void AlbumChangedForSong (Song song)
	{
		if (song.Album.Length == 0)
			return;

		Album album = (Album) Albums [song.Album];
		album.SyncCoverImageWith (song);
		if (AlbumChanged != null)
			AlbumChanged (album);
	}

	private void RemoveFromAlbum (Song song)
	{
		if (song.Album.Length == 0)
			return;

		Album album = (Album) Albums [song.Album];
		if (album.RemoveSong (song)) {
			Albums.Remove (album.Name);

			if (AlbumRemoved != null)
				AlbumRemoved (album);
		}
	}

	public void DoAlbum (Song song, bool emit_signal)
	{
		if (song.Album.Length == 0)
			return;

		Album album = (Album) Albums [song.Album];
		if (album == null) {
			album = new Album (song);
			Albums.Add (album.Name, album);

			if (emit_signal && AlbumAdded != null)
				AlbumAdded (album);
		} else {
			bool changed = album.AddSong (song);
			if (changed && AlbumChanged != null)
				AlbumChanged (album);
		}
	}

	public bool HaveFile (string filename)
	{
		return (Songs [filename] != null);
	}

	public bool Empty {
		get {
			return (Songs.Count == 0);
		}
	}

	public Song SongFromFile (string filename)
	{
		return (Song) Songs [filename];
	}
}
