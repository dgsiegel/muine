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
using System.Text.RegularExpressions;

using Gtk;
using Gdk;

public class CoverImage : EventBox
{
	private Gtk.Image image;
	
	public CoverImage () : base ()
	{
		image = new Gtk.Image ();	
		image.SetSizeRequest (CoverDatabase.AlbumCoverSize, 
				      CoverDatabase.AlbumCoverSize);
		
		Add (image);

		DragDataReceived += new DragDataReceivedHandler (OnDragDataReceived);

		Muine.CoverDB.DoneLoading += new CoverDatabase.DoneLoadingHandler (OnCoversDoneLoading);
	}

	~CoverImage ()
	{
		Dispose ();
	}

	private static TargetEntry [] cover_drag_entries = new TargetEntry [] {
		DndUtils.TargetUriList,
		DndUtils.TargetGnomeIconList,
		DndUtils.TargetNetscapeUrl
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
		string data = DndUtils.SelectionDataToString (args.SelectionData);

		bool success = false;

		string [] uri_list;
		string fn;
		
		switch (args.Info) {
		case (uint) DndUtils.TargetType.Uri:
			uri_list = Regex.Split (data, "\n");
			fn = uri_list [0];
			
			Uri uri = new Uri (fn);

			if (uri.Scheme != "http")
				break;

			if (Muine.CoverDB.Covers.ContainsKey (song.AlbumKey))
				Muine.CoverDB.RemoveCover (song.AlbumKey);

			song.CoverImage = Muine.CoverDB.AddCoverDownloading (song.AlbumKey);
			Muine.DB.SyncAlbumCoverImageWithSong (song);
				
			song.DownloadNewCoverImage (uri.AbsoluteUri);

			success = true;

			break;
			
		case (uint) DndUtils.TargetType.UriList:
			uri_list = DndUtils.SplitSelectionData (data);
			fn = FileUtils.LocalPathFromUri (uri_list [0]);

			if (fn == null)
				break;

			Pixbuf pixbuf;

			try {
				pixbuf = new Pixbuf (fn);
			} catch {
				success = false;
				
				break;
			}

			if (Muine.CoverDB.Covers.ContainsKey (song.AlbumKey))
				Muine.CoverDB.RemoveCover (song.AlbumKey);
			song.CoverImage = Muine.CoverDB.AddCoverEmbedded (song.AlbumKey, pixbuf);
			Muine.DB.SyncAlbumCoverImageWithSong (song);

			success = true;
			
			break;

		default:
			break;
		}

		Gtk.Drag.Finish (args.Context, success, false, args.Time);
	}

	private void OnDragDataReceived (object o, DragDataReceivedArgs args)
	{
		HandleDrop (song, args);
	}

	private void OnCoversDoneLoading ()
	{
		Sync ();
	}
}
