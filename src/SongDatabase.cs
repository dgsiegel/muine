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
using System.IO;
using System.Threading;

using Gnome;

namespace Muine
{
	public class SongDatabase 
	{
		// MCS doesn't support array constants yet (as of 1.0)
		private const string GConfKeyWatchedFolders = "/apps/muine/watched_folders";
		private readonly string [] GConfDefaultWatchedFolders = new string [0];

		private Hashtable songs;
		public Hashtable Songs {
			get { return songs; }
		}

		private Hashtable albums;
		public Hashtable Albums {
			get { return albums; }
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

		private Database db;

		/*** constructor ***/
		public SongDatabase (int version)
		{
			db = new Database (FileUtils.SongsDBFile, version);
			db.DecodeFunction = new Database.DecodeFunctionDelegate (DecodeFunction);
			db.EncodeFunction = new Database.EncodeFunctionDelegate (EncodeFunction);
			
			songs = new Hashtable ();
			albums = new Hashtable ();
		}

		/*** loading ***/
		private void DecodeFunction (string key, IntPtr data)
		{
			Song song = new Song (key, data);

			Songs.Add (key, song);
			
			AddToAlbum (song);
		}

		private bool loading;

		public void Load ()
		{
			loading = true;
			
			db.Load ();

			loading = false;
			
			/* add file monitors */
			string [] folders = (string []) Config.Get (GConfKeyWatchedFolders, GConfDefaultWatchedFolders);

			foreach (string folder in folders)
				AddMonitor (folder);
		}

		/*** storing ***/
		private IntPtr EncodeFunction (IntPtr handle, out int length)
		{
			Song song = Song.FromHandle (handle);

			return song.Pack (out length);
		}

		public void AddSong (Song song)
		{
			db.Store (song.Filename, song.Handle);

			Songs.Add (song.Filename, song);

			AddToAlbum (song);

			if (SongAdded != null)
				SongAdded (song);
		}

		public void RemoveSong (Song song)
		{
			db.Delete (song.Filename);

			if (SongRemoved != null)
				SongRemoved (song);

			Songs.Remove (song.Filename);

			RemoveFromAlbum (song);

			song.Dead = true;
		}

		private void SyncSong (Song song, Metadata metadata)
		{
			song.Sync (metadata);

			/* update album */
			RemoveFromAlbum (song);
			AddToAlbum (song);
			
			SaveSong (song);
		}

		public void SaveSong (Song song)
		{
			db.Store (song.Filename, song.Handle, true);
		}

		public void EmitSongChanged (Song song)
		{
			if (SongChanged != null && !loading)
				SongChanged (song);
		}

		public Song GetSong (string filename)
		{
			return (Song) Songs [filename];
		}

		/*** album management ***/
		private void EmitAlbumAdded (Album album)
		{
			if (AlbumAdded != null && !loading)
				AlbumAdded (album);
		}

		public void EmitAlbumChanged (Album album)
		{
			if (AlbumChanged != null && !loading)
				AlbumChanged (album);
		}

		private void RemoveFromAlbum (Song song)
		{
			if (!song.HasAlbum)
				return;

			string key = song.AlbumKey;

			Album album = (Album) Albums [key];
			if (album == null)
				return;
				
			if (!album.RemoveSong (song))
				return;

			// album is empty
			Albums.Remove (key);

			if (AlbumRemoved != null)
				AlbumRemoved (album);
		}

		private void AddToAlbum (Song song)
		{
			if (!song.HasAlbum)
				return;

			string key = song.AlbumKey;

			Album album = (Album) Albums [key];
			
			if (album == null) {
				album = new Album (song);
				Albums.Add (key, album);

				EmitAlbumAdded (album);
			} else {
				bool changed = album.AddSong (song);

				if (changed)
					EmitAlbumChanged (album);
			}
		}

		public Album GetAlbum (Song song)
		{
			return GetAlbum (song.AlbumKey);
		}

		public Album GetAlbum (string key)
		{
			return (Album) Albums [key];
		}

		/*** monitoring ***/
		public void AddWatchedFolder (string folder)
		{
			string [] folders = (string []) Config.Get (GConfKeyWatchedFolders, GConfDefaultWatchedFolders);

			string [] new_folders = new string [folders.Length + 1];

			int i = 0;
			foreach (string s in folders) {
				// check if folder is already monitored at a higher
				// level
				if (folder.IndexOf (s) == 0)
					return;
				new_folders [i] = folders [i];
				i++;
			}

			new_folders [folders.Length] = folder;

			Config.Set (GConfKeyWatchedFolders, new_folders);

			AddMonitor (folder);
		}

		private void AddMonitor (string folder)
		{
		/*
			FileSystemWatcher watcher = new FileSystemWatcher (folder);

			watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite |
					       NotifyFilters.Size | NotifyFilters.DirectoryName;
			
			watcher.IncludeSubdirectories = true;
			
			watcher.Changed += new FileSystemEventHandler (OnFileChanged);
			watcher.Created += new FileSystemEventHandler (OnFileCreated);
			watcher.Deleted += new FileSystemEventHandler (OnFileDeleted);
			watcher.Renamed += new RenamedEventHandler (OnFileRenamed);

			watcher.EnableRaisingEvents = true;
			*/
		}

	/*
		private static void OnFileChanged (object o, FileSystemEventArgs e)
		{
			Console.WriteLine (e.FullPath + " changed");
		}

		private static void OnFileCreated (object o, FileSystemEventArgs e)
		{
			Console.WriteLine (e.FullPath + " created");
		}

		private static void OnFileDeleted (object o, FileSystemEventArgs e)
		{
			Console.WriteLine (e.FullPath + " deleted");
		}

		private static void OnFileRenamed (object o, RenamedEventArgs e)
		{
			Console.WriteLine (e.OldFullPath + " renamed to " + e.FullPath);
		}*/

		/*** the thread that checks for changes on startup ***/

		public event DoneCheckingChangesHandler DoneCheckingChanges;
		public delegate void DoneCheckingChangesHandler ();

		private bool checking_changes = true;
		public bool CheckingChanges {
			set {
				checking_changes = value;

				if (!value) {
					thread.Abort ();

					thread_done = true;
					
					if (DoneCheckingChanges != null)
						DoneCheckingChanges ();
				}
			}

			get { return checking_changes; }
		}
		
		private Thread thread;

		private bool thread_done;

		private Queue removed_songs;
		private Queue changed_songs;
		private Queue new_songs;
		
		private GLib.IdleHandler process_actions_from_thread;

		public void CheckChanges ()
		{
			thread_done = false;

			removed_songs = Queue.Synchronized (new Queue ());
			changed_songs = Queue.Synchronized (new Queue ());
			new_songs = Queue.Synchronized (new Queue ());

			process_actions_from_thread = new GLib.IdleHandler (ProcessActionsFromThread);
			GLib.Idle.Add (process_actions_from_thread);

			thread = new Thread (new ThreadStart (CheckChangesThread));
			thread.Priority = ThreadPriority.BelowNormal;
			thread.Start ();
		}

		private struct ChangedSong {
			private Metadata metadata;
			public Metadata Metadata {
				get {
					return metadata;
				}
			}
			
			private Song song;
			public Song Song {
				get {
					return song;
				}
			}		

			public ChangedSong (Song song, Metadata metadata) {
				this.song = song;
				this.metadata = metadata;
			}
		}

		/* this is run from the main thread */
		private const int BatchSize = 10;
		private bool ProcessActionsFromThread ()
		{
			int counter = 0;
			
			if (removed_songs.Count > 0) {
				while (removed_songs.Count > 0 && counter < BatchSize) {
					counter++;
					
					Song song = (Song) removed_songs.Dequeue ();

					if (song.Dead)
						continue;

					RemoveSong (song);
				}

				return true;
			}

			if (changed_songs.Count > 0) {
				while (changed_songs.Count > 0 && counter < BatchSize) {
					counter++;
					
					ChangedSong cs = (ChangedSong) changed_songs.Dequeue ();

					if (cs.Song.Dead)
						continue;

					SyncSong (cs.Song, cs.Metadata);
				}

				return true;
			}

			if (new_songs.Count > 0) {
				while (new_songs.Count > 0 && counter < BatchSize) {
					counter++;
					
					Song song = (Song) new_songs.Dequeue ();

					if (Songs.ContainsKey (song.Filename))
						continue;

					AddSong (song);
				}

				return true;
			}

			if (thread_done) {
				checking_changes = false;

				if (DoneCheckingChanges != null)
					DoneCheckingChanges ();
				
				return false;
			} else
				return true;
		}

		private void HandleDirectory (DirectoryInfo info,
					      Queue new_songs)
		{
			FileInfo [] finfos;
			
			try {
				finfos = info.GetFiles ();
			} catch {
				return;
			}

			foreach (FileInfo finfo in finfos) {
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

			DirectoryInfo [] dinfos;
			
			try {
				dinfos = info.GetDirectories ();
			} catch {
				return;
			}

			foreach (DirectoryInfo dinfo in dinfos)
				HandleDirectory (dinfo, new_songs);
		}

		readonly static DateTime datetTime1970 = new DateTime (1970, 1, 1, 0, 0, 0, 0);

		private long MTimeToTicks (int mtime)
		{
			return (long) (mtime * 10000000L) + datetTime1970.Ticks;
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
					if (MTimeToTicks (song.MTime) < finfo.LastWriteTimeUtc.Ticks) {
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
			string [] folders = (string []) Config.Get (GConfKeyWatchedFolders, GConfDefaultWatchedFolders);

			foreach (string folder in folders) {
				DirectoryInfo dinfo = new DirectoryInfo (folder);
				if (!dinfo.Exists)
					continue;

				HandleDirectory (dinfo, new_songs);
			}

			thread_done = true;
		}

		/*
		The album key is "folder:album name" because of the following
		reasons:
		We cannot do artist/performer matching, because it is very common for
		albums to be made by different artists. Random example, the Sigur
		Rós/Radiohead split. Using "Various Artists" as artist tag is whacky.
		But, we cannot match only by album name either: a user may very well
		have multiple albums with the title "Greatest Hits". We don't want to
		incorrectly group all these together.
		So, the best thing we've managed to come up with so far is using
		folder:albumname. This because most people who even have whole albums
		have those organised in folders, or at the very least all music files in
		the same folder. So for those it should more or less work. And for those
		who have a decently organised music collection, the original target user
		base, it should work flawlessly. And for those who have a REALLY poorly
		organised collection, well, bummer. Moving all files to the same dir
		will help a bit.
		*/
		public string MakeAlbumKey (string folder, string album_name)
		{
			return folder + ":" + album_name.ToLower ();
		}
	}
}
