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
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using Gnome;

public class SongDatabase 
{
	public Hashtable Songs; 

	public Hashtable Albums;

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

	/*** constructor ***/
	private IntPtr dbf;

	[DllImport ("libmuine")]
	private static extern IntPtr db_open (string filename, int version, out string error);

	public SongDatabase (int version)
	{
		DirectoryInfo dinfo = new DirectoryInfo (User.DirGet () + "/muine");
		if (!dinfo.Exists) {
			try {
				dinfo.Create ();
			} catch (Exception e) {
				throw e;
			}
		}
		
		string filename = dinfo.FullName + "/songs.db";

		string error = null;

		dbf = db_open (filename, version, out error);

		if (dbf == IntPtr.Zero)
			throw new Exception (error);

		Songs = new Hashtable ();
		Albums = new Hashtable ();
	}

	/*** loading ***/
	private void DecodeFunc (string key, IntPtr data, IntPtr user_data)
	{
		Song song = new Song (key, data);

		Songs.Add (key, song);

		AddToAlbum (song, false);
	}

	private delegate void DecodeFuncDelegate (string key, IntPtr data, IntPtr user_data);

	[DllImport ("libmuine")]
	private static extern void db_foreach (IntPtr dbf, DecodeFuncDelegate decode_func,
					       IntPtr user_data);

	public void Load ()
	{
		db_foreach (dbf, new DecodeFuncDelegate (DecodeFunc), IntPtr.Zero);

		/* add file monitors */
		string [] folders;
		try {
			folders = (string []) Muine.GConfClient.Get ("/apps/muine/watched_folders");
		} catch {
			folders = new string [0];
		}

		foreach (string folder in folders)
			AddMonitor (folder);
	}

	/*** storing ***/
	private IntPtr EncodeFunc (IntPtr handle, out int length)
	{
		Song song = Song.FromHandle (handle);

		return song.Pack (out length);
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

		AddToAlbum (song, true);

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

		Songs.Remove (song.Filename);

		RemoveFromAlbum (song);

		song.Dead = true;
	}

	private void SyncSongWithMetadata (Song song, Metadata metadata)
	{
		song.Sync (metadata);

		/* update album */
		RemoveFromAlbum (song);
		AddToAlbum (song, true);
		
		UpdateSong (song);
	}

	public void UpdateSong (Song song)
	{
		db_store (dbf, song.Filename, true,
			  new EncodeFuncDelegate (EncodeFunc), song.Handle);
	
		if (SongChanged != null)
			SongChanged (song);
	}

	/*** album management ***/
	public void SyncAlbumCoverImageWithSong (Song song)
	{
		if (song.Album.Length == 0)
			return;

		Album album = (Album) Albums [song.AlbumKey];
		if (album == null)
			return;

		album.CoverImage = song.CoverImage;

		EmitAlbumChanged (album);
	}

	public void EmitAlbumChanged (Album album)
	{
		if (AlbumChanged != null)
			AlbumChanged (album);
	}

	private void RemoveFromAlbum (Song song)
	{
		if (song.Album.Length == 0)
			return;

		string key = song.AlbumKey;

		Album album = (Album) Albums [key];
		if (album == null)
			return;
			
		bool album_empty;
		album.RemoveSong (song, out album_empty);
		
		if (album_empty) {
			Albums.Remove (key);

			/* only remove the album cover if we are not dealing
			 * with removable media */
			if (!FileUtils.IsFromRemovableMedia (song.Filename))
				Muine.CoverDB.RemoveCover (key);

			if (AlbumRemoved != null)
				AlbumRemoved (album);
		}
	}

	private void AddToAlbum (Song song, bool emit_signal)
	{
		if (song.Album.Length == 0)
			return;

		string key = song.AlbumKey;

		bool changed = false;

		Album album = (Album) Albums [key];
		
		if (album == null) {
			album = new Album (song);
			Albums.Add (key, album);

			changed = true;
		} else
			album.AddSong (song, out changed);

		if (emit_signal && changed && AlbumChanged != null)
			AlbumChanged (album);
	}

	/*** monitoring ***/
	public void AddWatchedFolder (string folder)
	{
		string [] folders;
		
		try {
			folders = (string []) Muine.GConfClient.Get ("/apps/muine/watched_folders");
		} catch {
			folders = new string [0];
		}

		string [] new_folders = new string [folders.Length + 1];

		int i = 0;
		foreach (string s in folders) {
			if (folder.IndexOf (s) == 0)
				return;
			new_folders [i] = folders [i];
			i++;
		}

		new_folders [folders.Length] = folder;

		Muine.GConfClient.Set ("/apps/muine/watched_folders", new_folders);

		AddMonitor (folder);
	}

	private void AddMonitor (string folder)
	{
	/*
		FileSystemWatcher watcher = new FileSystemWatcher (folder);

		watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite |
		                       NotifyFilters.Size | NotifyFilters.DirectoryName;
		
		watcher.IncludeSubdirectories = true;
		
		watcher.Changed += new FileSystemEventHandler (HandleFileChanged);
		watcher.Created += new FileSystemEventHandler (HandleFileCreated);
		watcher.Deleted += new FileSystemEventHandler (HandleFileDeleted);
		watcher.Renamed += new RenamedEventHandler (HandleFileRenamed);

		watcher.EnableRaisingEvents = true;
		*/
	}

/*
	private static void HandleFileChanged (object o, FileSystemEventArgs e)
	{
		Console.WriteLine (e.FullPath + " changed");
	}

	private static void HandleFileCreated (object o, FileSystemEventArgs e)
	{
		Console.WriteLine (e.FullPath + " created");
	}

	private static void HandleFileDeleted (object o, FileSystemEventArgs e)
	{
		Console.WriteLine (e.FullPath + " deleted");
	}

	private static void HandleFileRenamed (object o, RenamedEventArgs e)
	{
		Console.WriteLine (e.OldFullPath + " renamed to " + e.FullPath);
	}*/

	/*** the thread that checks for changes on startup ***/

	private bool thread_done;

	private Queue removed_songs;
	private Queue changed_songs;
	private Queue new_songs;

	public void CheckChanges ()
	{
		thread_done = false;

		removed_songs = Queue.Synchronized (new Queue ());
		changed_songs = Queue.Synchronized (new Queue ());
		new_songs = Queue.Synchronized (new Queue ());

		GLib.Idle.Add (new GLib.IdleHandler (ProcessActionsFromThread));

		Thread thread = new Thread (new ThreadStart (CheckChangesThread));
		thread.Priority = ThreadPriority.BelowNormal;
		thread.Start ();
	}
	
	private class ChangedSong {
		public Metadata Metadata;
		public Song Song;

		public ChangedSong (Song song, Metadata md) {
			Song = song;
			Metadata = md;
		}
	}

	/* this is run from the main thread */
	private bool ProcessActionsFromThread ()
	{
		int counter = 0;
		
		if (removed_songs.Count > 0) {
			while (removed_songs.Count > 0 && counter < 10) {
				counter++;
				
				Song song = (Song) removed_songs.Dequeue ();

				if (song.Dead)
					continue;

				RemoveSong (song);
			}

			return true;
		}

		if (changed_songs.Count > 0) {
			while (changed_songs.Count > 0 && counter < 10) {
				counter++;
				
				ChangedSong cs = (ChangedSong) changed_songs.Dequeue ();

				if (cs.Song.Dead)
					continue;

				SyncSongWithMetadata (cs.Song, cs.Metadata);
			}

			return true;
		}

		if (new_songs.Count > 0) {
			while (new_songs.Count > 0 && counter < 10) {
				counter++;
				
				Song song = (Song) new_songs.Dequeue ();

				if (Songs.ContainsKey (song.Filename))
					continue;

				AddSong (song);
			}

			return true;
		}

		return !thread_done;
	}

	private void HandleDirectory (DirectoryInfo info,
				      Queue new_songs)
	{
		foreach (FileInfo finfo in info.GetFiles ()) {
			if (Songs [finfo.FullName] == null) {
				Song song;

				try {
					song = new Song (finfo.FullName);
				} catch {
					continue;
				}

				new_songs.Enqueue (song);
			}
		}

		foreach (DirectoryInfo dinfo in info.GetDirectories ())
			HandleDirectory (dinfo, new_songs);
	}

	private long MTimeToTicks (int mtime)
	{
		return (long) mtime * (long) Math.Pow (10, 7) + 621356040000000000;
	}

	private void CheckChangesThread ()
	{
		/* check for removed songs and changes */
		Hashtable snapshot = (Hashtable) Songs.Clone ();

		foreach (string file in snapshot.Keys) {
			FileInfo finfo = new FileInfo (file);
			Song song = (Song) snapshot [file];
			
			if (!finfo.Exists)
				removed_songs.Enqueue (song);
			else {
				if (MTimeToTicks (song.MTime) < finfo.LastWriteTime.Ticks) {
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

		foreach (string folder in folders) {
			DirectoryInfo dinfo = new DirectoryInfo (folder);
			if (!dinfo.Exists)
				continue;

			HandleDirectory (dinfo, new_songs);
		}
		
		thread_done = true;
	}
}
