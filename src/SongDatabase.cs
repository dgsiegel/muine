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

namespace Muine
{
	public class SongDatabase 
	{
		// MCS doesn't support array constants yet (as of 1.0)
		private const string GConfKeyWatchedFolders = "/apps/muine/watched_folders";
		private readonly string [] GConfDefaultWatchedFolders = new string [0];

		/* when iterating either of these don't forget to lock the DB;
		   the hash might be changed by another thread while iterating
		   otherwise */
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

		// Constructor
		public SongDatabase (int version)
		{
			db = new Database (FileUtils.SongsDBFile, version);
			db.DecodeFunction = new Database.DecodeFunctionDelegate (DecodeFunction);
			db.EncodeFunction = new Database.EncodeFunctionDelegate (EncodeFunction);
			
			songs = new Hashtable ();
			albums = new Hashtable ();

			watched_folders = (string []) Config.Get (GConfKeyWatchedFolders, GConfDefaultWatchedFolders);
			Config.AddNotify (GConfKeyWatchedFolders,
					  new GConf.NotifyEventHandler (OnWatchedFoldersChanged));
		}

		// Database interaction
		private void DecodeFunction (string key, IntPtr data)
		{
			Song song = new Song (key, data);

			Songs.Add (key, song);
			
			/* we don't "Finish", as we do this before the UI is there,
			   we don't need to emit signals */
			StartAddToAlbum (song);
		}

		private IntPtr EncodeFunction (IntPtr handle, out int length)
		{
			Song song = Song.FromHandle (handle);

			return song.Pack (out length);
		}

		// Loading
		public void Load ()
		{
			lock (this)
				db.Load ();
		}

		// Song management
		public void AddSong (Song song)
		{
			SignalRequest rq = StartAddSong (song);
			if (rq != null)
				HandleSignalRequest (rq);
		}
	
		private SignalRequest StartAddSong (Song song)
		{
			lock (this) {
				SignalRequest rq = new SignalRequest (song);
			
				try {
					Songs.Add (song.Filename, song);
				} catch (ArgumentException e) {
					// Already exists, ignore
					return null;
				}

				StartAddToAlbum (rq);

				// Store after the album cover has been stored,
				// in case of unexpected exit
				db.Store (song.Filename, song.Handle);

				rq.SongAdded = true;

				return rq;
			}
		}

		private SignalRequest StartSyncSong (Song song, Metadata metadata)
		{
			lock (this) {
				if (song.Dead)
					return null;

				SignalRequest rq = new SignalRequest (song);
			
				song.Sync (metadata);

				/* update album */
				StartRemoveFromAlbum (rq);
				StartAddToAlbum (rq);
			
				SaveSongInternal (song);

				rq.SongChanged = true;
				
				return rq;
			}
		}

		public void SaveSong (Song song)
		{
			lock (this)
				SaveSongInternal (song);
		}

		private void SaveSongInternal (Song song)
		{
			db.Store (song.Filename, song.Handle, true);
		}

		public void RemoveSong (Song song)
		{
			SignalRequest rq = StartRemoveSong (song);
			if (rq != null)
				HandleSignalRequest (rq);
		}

		private SignalRequest StartRemoveSong (Song song)
		{
			lock (this) {
				if (song.Dead)
					return null;

				SignalRequest rq = new SignalRequest (song);

				db.Delete (song.Filename);

				Songs.Remove (rq.Song.Filename);

				StartRemoveFromAlbum (rq);

				rq.SongRemoved = true;

				return rq;
			}
		}

		private void EmitSongAdded (Song song)
		{
			if (SongAdded != null)
				SongAdded (song);
		}

		public void EmitSongChanged (Song song)
		{
			if (SongChanged != null)
				SongChanged (song);
		}

		private void EmitSongRemoved (Song song)
		{
			if (SongRemoved != null)
				SongRemoved (song);
		}

		// Album management
		private void AddToAlbum (Song song)
		{
			SignalRequest rq = new SignalRequest (song);
			StartAddToAlbum (rq);
			HandleSignalRequest (rq);
		}

		private void StartAddToAlbum (SignalRequest rq)
		{
			StartAddToAlbum (rq, null);
		}

		private void StartAddToAlbum (Song song)
		{
			StartAddToAlbum (null, song);
		}

		private void StartAddToAlbum (SignalRequest rq, Song s)
		{
			Song song;

			bool from_db = (s != null);
			if (from_db)
				song = s;
			else
				song = rq.Song;
			
			if (!song.HasAlbum)
				return;

			string key = song.AlbumKey;

			Album album = (Album) Albums [key];
			
			bool changed = false;
			bool added = false;
			bool songs_changed = false;

			if (album == null) {
				album = new Album (song, !from_db);
				Albums.Add (key, album);

				added = true;
			} else {
				album.Add (song,
					   !from_db,
				           out changed,
					   out songs_changed);
			}

			if (!from_db) {
				rq.Album = album;
				rq.AlbumAdded = added;
				rq.AlbumChanged = changed;
				rq.AlbumSongsChanged = songs_changed;
			}
		}

		private void StartRemoveFromAlbum (SignalRequest rq)
		{
			if (!rq.Song.HasAlbum)
				return;

			string key = rq.Song.AlbumKey;

			Album album = (Album) Albums [key];
			if (album == null)
				return;
				
			if (album.Remove (rq.Song)) {
				// album is empty
				Albums.Remove (key);

				rq.AlbumRemoved = true;
			}
		}

		private void EmitAlbumAdded (Album album)
		{
			if (AlbumAdded != null)
				AlbumAdded (album);
		}

		public void EmitAlbumChanged (Album album)
		{
			if (AlbumChanged != null)
				AlbumChanged (album);
		}

		private void EmitAlbumRemoved (Album album)
		{
			if (AlbumRemoved != null)
				AlbumRemoved (album);
		}

		// Getters
		public Song GetSong (string filename)
		{
			return (Song) Songs [filename];
		}

		public Album GetAlbum (Song song)
		{
			return GetAlbum (song.AlbumKey);
		}

		public Album GetAlbum (string key)
		{
			return (Album) Albums [key];
		}

		// Folder watching
		public void AddFolder (DirectoryInfo dinfo, ProgressWindow pw)
		{
			pw.Report (dinfo.Name, dinfo.Name);

			AddToConfig (dinfo.FullName);

			AddFolderThread t = new AddFolderThread (dinfo, pw);
		}

		private class AddFolderThread : ThreadBase
		{
			private DirectoryInfo dinfo;
			private ProgressWindow pw;
			private BooleanBox canceled_box = new BooleanBox (false);

			protected override void ThreadFunc ()
			{
				Global.DB.HandleDirectory (dinfo, queue, canceled_box);

				thread_done = true;
			}
			
			protected override bool MainLoopIdle ()
			{
				if (queue.Count == 0) {
					if (thread_done) {
						pw.Done ();
					
						return false;
					} else
						return true;
				}

				SignalRequest rq = (SignalRequest) queue.Dequeue ();

				bool canceled = pw.Report (dinfo.Name, Path.GetFileName (rq.Song.Filename));
				if (canceled) {
					canceled_box.Value = true;
					// finish what is in the queue
				}

				Global.DB.HandleSignalRequest (rq);
	
				return true;
			}
			
			public AddFolderThread (DirectoryInfo dinfo, ProgressWindow pw)
			{
				this.dinfo = dinfo;
				this.pw = pw;

				thread.Start ();
			}
		}

		private string [] watched_folders;
		public string [] WatchedFolders {
			get {
				return watched_folders;
			}
		}

		private void AddToConfig (string folder)
		{
			// Update config
			string [] new_folders = new string [watched_folders.Length + 1];

			int i = 0;
			foreach (string s in watched_folders) {
				// check if folder is already monitored at a higher
				// or same level
				if (folder.IndexOf (s) == 0)
					return;
				new_folders [i] = watched_folders [i];
				i++;
			}

			new_folders [watched_folders.Length] = folder;

			watched_folders = new_folders;
			Config.Set (GConfKeyWatchedFolders, watched_folders);
		}

		private void OnWatchedFoldersChanged (object o, GConf.NotifyEventArgs args)
		{
			string [] old_watched_folders = watched_folders;
			watched_folders = (string []) args.Value;

			foreach (string s in watched_folders) {
				if (Array.BinarySearch (old_watched_folders, s) < 0) {
					DirectoryInfo dinfo = new DirectoryInfo (s);
					if (!dinfo.Exists)
						continue;

					ProgressWindow pw = new ProgressWindow (Global.Playlist);
					pw.Report (dinfo.Name, dinfo.Name);
					AddFolderThread t = new AddFolderThread (dinfo, pw);
				}
			}
		}

		// Changes thread
		public void CheckChanges ()
		{
			CheckChangesThread t = new CheckChangesThread ();
		}
		
		private class CheckChangesThread : ThreadBase
		{
			protected override bool MainLoopIdle ()
			{
				if (queue.Count == 0)
					return !thread_done;

				SignalRequest rq = (SignalRequest) queue.Dequeue ();

				Global.DB.HandleSignalRequest (rq);

				return true;
			}

			protected override void ThreadFunc ()
			{
				Hashtable snapshot;
				lock (Global.DB)
					snapshot = (Hashtable) Global.DB.Songs.Clone ();

				/* check for removed songs and changes */
				foreach (string file in snapshot.Keys) {
					FileInfo finfo = new FileInfo (file);
					Song song = (Song) snapshot [file];

					SignalRequest rq = null;

					if (!finfo.Exists)
						rq = Global.DB.StartRemoveSong (song);
					else {
						if (FileUtils.MTimeToTicks (song.MTime) < finfo.LastWriteTimeUtc.Ticks) {
							try {
								Metadata metadata = new Metadata (song.Filename);
								rq = Global.DB.StartSyncSong (song, metadata);
							} catch {
								rq = Global.DB.StartRemoveSong (song);
							}
						}
					}

					if (rq != null)
						queue.Enqueue (rq);
				}

				/* check for new songs */
				foreach (string folder in Global.DB.WatchedFolders) {
					DirectoryInfo dinfo = new DirectoryInfo (folder);
					if (!dinfo.Exists)
						continue;

					BooleanBox canceled = new BooleanBox (false);
					Global.DB.HandleDirectory (dinfo, queue, canceled);
				}

				thread_done = true;
			}

			public CheckChangesThread ()
			{
				thread.Start ();
			}
		}

		// Directory walking
		private bool HandleDirectory (DirectoryInfo info,
					      Queue queue,
					      BooleanBox canceled_box)
		{
			FileInfo [] finfos;
			
			try {
				finfos = info.GetFiles ();
			} catch {
				return true;
			}

			foreach (FileInfo finfo in finfos) {
				if (canceled_box.Value)
					return false;

				if (Songs [finfo.FullName] == null) {
					Song song;

					try {
						song = new Song (finfo.FullName);
					} catch {
						continue;
					}
					
					SignalRequest rq = StartAddSong (song);
					if (rq != null)
						queue.Enqueue (rq);
				}
			}

			DirectoryInfo [] dinfos;
			
			try {
				dinfos = info.GetDirectories ();
			} catch {
				return true;
			}

			foreach (DirectoryInfo dinfo in dinfos) {
				bool ret = HandleDirectory (dinfo, queue, canceled_box);
				if (!ret)
					return false;
			}

			return true;
		}

		private class BooleanBox {
			private bool val;
			public bool Value {
				set { val = value; }
				get { return val; }
			}

			public BooleanBox (bool value)
			{
				val = value;
			}
		}
			
		// SignalRequest
		private class SignalRequest
		{
			private Song song;
			public Song Song {
				get { return song; }
			}
			
			private Album album;
			public Album Album {
				set { album = value; }
				get { return album; }
			}

			private bool song_added;
			public bool SongAdded {
				set { song_added = value; }
				get { return song_added; }
			}

			private bool song_changed;
			public bool SongChanged {
				set { song_changed = value; }
				get { return song_changed; }
			}

			private bool song_removed;
			public bool SongRemoved {
				set { song_removed = value; }
				get { return song_removed; }
			}
			
			private bool album_added;
			public bool AlbumAdded {
				set { album_added = value; }
				get { return album_added; }
			}

			private bool album_changed;
			public bool AlbumChanged {
				set { album_changed = value; }
				get { return album_changed; }
			}

			private bool album_removed;
			public bool AlbumRemoved {
				set { album_removed = value; }
				get { return album_removed; }
			}

			private bool album_songs_changed;
			public bool AlbumSongsChanged {
				set { album_songs_changed = value; }
				get { return album_songs_changed; }
			}

			public SignalRequest (Song song) {
				this.song = song;
				album = null;

				song_added = false;
				song_changed = false;
				song_removed = false;

				album_added = false;
				album_changed = false;
				album_removed = false;
				album_songs_changed = false;
			}
		}

		private void HandleSignalRequest (SignalRequest rq)
		{
			lock (this) {
				if (rq.Song.Dead)
					return;

				if (rq.SongAdded)
					EmitSongAdded (rq.Song);
				else if (rq.SongChanged)
					EmitSongChanged (rq.Song);
				else if (rq.SongRemoved) {
					EmitSongRemoved (rq.Song);

					rq.Song.Deregister ();
				}
				
				if (rq.AlbumAdded)
					EmitAlbumAdded (rq.Album);
				else if (rq.AlbumChanged) {
					EmitAlbumChanged (rq.Album);

					if (rq.AlbumSongsChanged)
						foreach (Song s in rq.Album.Songs)
							EmitSongChanged (s);
				} else if (rq.AlbumRemoved)
					EmitAlbumRemoved (rq.Album);
			}
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
