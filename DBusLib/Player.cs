/*
 * Copyright (C) 2004 Sergio Rubio <sergio.rubio@hispalinux.es>
 *           (C) 2004, 2005 Jorn Baayen <jbaayen@gnome.org>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General public virtual License as
 * published by the Free Software Foundation; either version 2 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General public virtual License for more details.
 *
 * You should have received a copy of the GNU General public virtual
 * License along with this program; if not, write to the
 * Free Software Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */

using System;
using System.Collections;

using DBus;

using Muine.PluginLib;

namespace Muine.DBusLib
{
	[Interface ("org.gnome.Muine.Player")]
	public class Player
	{
		public static Player FindInstance ()
		{
			Connection conn = Bus.GetSessionBus ();
			
			Service service = Service.Get (conn, "org.gnome.Muine");
			
			return (Player) service.GetObject
				(typeof (Player), "/org/gnome/Muine/Player");
		}

		private IPlayer player = null;

		public void HookUp (IPlayer player)
		{
			this.player = player;
		
			player.SongChangedEvent +=
				new SongChangedEventHandler (OnSongChangedEvent);
			player.StateChangedEvent +=
				new StateChangedEventHandler (OnStateChangedEvent);
		}

		[Method]
		public virtual bool GetPlaying ()
		{
			return player.Playing;
		}
	
		[Method]
		public virtual void SetPlaying (bool playing)
		{
			player.Playing = playing;
		}

		[Method]
		public virtual bool HasNext ()
		{
			return player.HasNext;
		}

		[Method]
		public virtual void Next ()
		{
			player.Next ();
		}

		[Method]
		public virtual bool HasPrevious ()
		{
			return player.HasPrevious;
		}

		[Method]
		public virtual void Previous ()
		{
			player.Previous ();
		}

		[Method]
		public virtual string GetCurrentSong ()
		{
			string value = "";
		
			if (player.PlayingSong != null)
				value = SongToString (player.PlayingSong);
		
			return value;
		}

		[Method]
		public virtual bool GetWindowVisible ()
		{
			return player.WindowVisible;
		}
	
		[Method]
		public virtual void SetWindowVisible (bool visible)
		{
			player.WindowVisible = visible;
		}

		[Method]
		public virtual int GetVolume ()
		{
			return player.Volume;
		}

		[Method]
		public virtual void SetVolume (int volume)
		{
			player.Volume = volume;
		}

		[Method]
		public virtual int GetPosition ()
		{
			return player.Position;
		}

		[Method]
		public virtual void SetPosition (int pos)
		{
			player.Position = pos;
		}

		[Method]
		public virtual void PlayAlbum ()
		{
			player.PlayAlbum ();
		}

		[Method]
		public virtual void PlaySong ()
		{
			player.PlaySong ();
		}

		[Method]
		public virtual void OpenPlaylist (string uri)
		{
			player.OpenPlaylist (uri);
		}

		[Method]
		public virtual void PlayFile (string uri)
		{
			player.PlayFile (uri);
		}

		[Method]
		public virtual void QueueFile (string uri)
		{
			player.QueueFile (uri);
		}

		[Method]
		public virtual bool WriteAlbumCoverToFile (string file)
		{
			if (player.PlayingSong == null ||
			    player.PlayingSong.CoverImage == null)
				return false;
			
			try {
				player.PlayingSong.CoverImage.Savev (file, "png", null, null);
			} catch {
				return false;
			}

			return true;
		}

		[Method]
		public virtual byte [] GetAlbumCover ()
		{
			if (player.PlayingSong == null ||
			    player.PlayingSong.CoverImage == null)
				return null;

			Gdk.Pixdata pixdata = new Gdk.Pixdata ();
			pixdata.FromPixbuf (player.PlayingSong.CoverImage, true);
			
			return pixdata.Serialize ();
		}

		[Method]
		public virtual void Quit ()
		{
			player.Quit ();
		}

		[Signal] public event SongChangedHandler SongChanged;

		private void OnSongChangedEvent (ISong song)
		{
			string value = "";
		
			if (song != null)
				value = SongToString (song);

			if (SongChanged != null)
				SongChanged (value);
		}

		[Signal] public event StateChangedHandler StateChanged;

		private void OnStateChangedEvent (bool playing)
		{
			if (StateChanged != null)
				StateChanged (playing);
		}

		private string SongToString (ISong song)
		{
			string value = "";

			value += "uri: " + song.Filename + "\n";
			value += "title: " + song.Title + "\n";

			foreach (string s in song.Artists)
				value += "artist: " + s + "\n";
			
			foreach (string s in song.Performers)
				value += "performer: " + s + "\n";

			if (song.Album.Length > 0)
				value += "album: " + song.Album + "\n";

			if (song.Year.Length > 0)
				value += "year: " + song.Year + "\n";

			if (song.TrackNumber >= 0)
				value += "track_number: " + song.TrackNumber + "\n";
			
			if (song.DiscNumber >= 0)
				value += "disc_number: " + song.DiscNumber + "\n";

			value += "duration: " + song.Duration;

			return value;
		}
	}

	public delegate void SongChangedHandler (string song_data);
	public delegate void StateChangedHandler (bool playing);
}
