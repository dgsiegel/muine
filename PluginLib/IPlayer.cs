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

namespace Muine.PluginLib
{
	public interface IPlayer
	{
		ISong PlayingSong {
			get;
		}

		bool Playing {
			get;
			set;
		}

		int Volume {
			get;
			set;
		}
	
		int Position {
			get;
			set;
		}
	
		bool HasNext {
			get;
		}
	
		bool HasPrevious {
			get;
		}
	
		void Next ();
		void Previous ();

		void PlaySong ();
		void PlayAlbum ();

		ISong [] Playlist {
			get;
		}

		ISong [] Selection {
			get;
		}

		void OpenPlaylist (string uri);

		void PlayFile (string uri);
		void QueueFile (string uri);

		void Quit ();

		bool WindowVisible {
			get;
			set;
		}
	
		Gtk.UIManager UIManager {
			get;
		}

		Gtk.Window Window {
			get;
		}

		uint BusyLevel {
			set;
			get;
		}

		event SongChangedEventHandler SongChangedEvent;
	
		event StateChangedEventHandler StateChangedEvent;

		event TickEventHandler TickEvent;

		event GenericEventHandler PlaylistChangedEvent;

		event GenericEventHandler SelectionChangedEvent;
	}

	public delegate void SongChangedEventHandler (ISong song);
	public delegate void StateChangedEventHandler (bool playing);
	public delegate void TickEventHandler (int position);
	public delegate void GenericEventHandler ();
}
