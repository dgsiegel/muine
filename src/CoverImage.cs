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
using System.Text.RegularExpressions;

using Gtk;

public class CoverImage : EventBox
{
	private Image image;
	
	public CoverImage () : base ()
	{
		image = new Image ();	
		image.SetSizeRequest (CoverDatabase.AlbumCoverSize, 
				      CoverDatabase.AlbumCoverSize);
		
		Add (image);

		DragDataReceived += new DragDataReceivedHandler (HandleDragDataReceived);

		Muine.CoverDB.DoneLoading += new CoverDatabase.DoneLoadingHandler (HandleDoneLoading);
	}

	~CoverImage ()
	{
		Dispose ();
	}

	private static TargetEntry [] cover_drag_entries = new TargetEntry [] {
		new TargetEntry ("text/uri-list", 0, (uint) PlaylistWindow.TargetType.UriList),
		new TargetEntry ("x-special/gnome-icon-list", 0, (uint) PlaylistWindow.TargetType.UriList),
		new TargetEntry ("_NETSCAPE_URL", 0, (uint) PlaylistWindow.TargetType.Uri)
	};

	private void Sync ()
	{
		if (song != null && song.CoverImage != null)
			image.FromPixbuf = song.CoverImage;
		else if (song != null && Muine.CoverDB.Loading)
			image.FromPixbuf = Muine.CoverDB.DownloadingPixbuf;
		else {
			image.SetFromStock ("muine-default-cover",
				            StockIcons.AlbumCoverSize);
		}
	
		if (song != null && song.Album.Length > 0 && !Muine.CoverDB.Loading) {
			Gtk.Drag.DestSet (this, DestDefaults.All,
					  cover_drag_entries, Gdk.DragAction.Copy);
		} else {
			Gtk.Drag.DestSet (this, DestDefaults.All,
					  null, Gdk.DragAction.Copy);
		}
	}

	private Song song;
	public Song Song {
		set {
			song = value;

			Sync ();
		}
	}

	public static void HandleDrop (Song song, DragDataReceivedArgs args)
	{
		string data = StringUtils.SelectionDataToString (args.SelectionData);

		bool success = false;

		string [] uri_list;
		string fn;
		
		switch (args.Info) {
		case (uint) PlaylistWindow.TargetType.Uri:
			uri_list = Regex.Split (data, "\n");
			fn = uri_list [0];
			
			Uri uri = new Uri (fn);

			if (!(uri.Scheme == "http"))
				break;

			success = true;

			try {
				if (Muine.CoverDB.Covers.ContainsKey (song.AlbumKey))
					Muine.CoverDB.RemoveCover (song.AlbumKey);
				song.CoverImage = Muine.CoverDB.AddCoverDownloading (song.AlbumKey);
				Muine.DB.SyncAlbumCoverImageWithSong (song);
				
				song.DownloadNewCoverImage (uri.AbsoluteUri);

				success = true;
			} catch (Exception e) {
				success = false;
				
				break;
			}

			break;
		case (uint) PlaylistWindow.TargetType.UriList:
			uri_list = StringUtils.SplitSelectionData (data);
			fn = FileUtils.LocalPathFromUri (uri_list [0]);

			if (fn == null)
				break;

			try {
				if (Muine.CoverDB.Covers.ContainsKey (song.AlbumKey))
					Muine.CoverDB.RemoveCover (song.AlbumKey);
				song.CoverImage = Muine.CoverDB.AddCoverLocal (song.AlbumKey, fn);
				Muine.DB.SyncAlbumCoverImageWithSong (song);

				success = true;
			} catch {
				success = false;
				
				break;
			}
			
			break;
		default:
			break;
		}

		Drag.Finish (args.Context, success, false, args.Time);
	}

	private void HandleDragDataReceived (object o, DragDataReceivedArgs args)
	{
		HandleDrop (song, args);
	}

	private void HandleDoneLoading ()
	{
		Sync ();
	}
}
