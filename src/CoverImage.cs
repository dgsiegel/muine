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

namespace Muine
{
	public class CoverImage : EventBox
	{
		private Gtk.Image image;
		
		public CoverImage () : base ()
		{
			image = new Gtk.Image ();	
			image.SetSizeRequest (CoverDatabase.CoverSize, 
					      CoverDatabase.CoverSize);
			
			Add (image);

			DragDataReceived += new DragDataReceivedHandler (OnDragDataReceived);

			Global.CoverDB.DoneLoading += new CoverDatabase.DoneLoadingHandler (OnCoversDoneLoading);
		}

		~CoverImage ()
		{
			Dispose ();
		}

		private static TargetEntry [] drag_entries = new TargetEntry [] {
			DndUtils.TargetUriList,
			DndUtils.TargetGnomeIconList,
			DndUtils.TargetNetscapeUrl
		};
		public static TargetEntry [] DragEntries {
			get { return drag_entries; }
		}

		private void Sync ()
		{
			if (song != null && song.CoverImage != null)
				image.FromPixbuf = song.CoverImage;
			else if (song != null && Global.CoverDB.Loading)
				image.FromPixbuf = Global.CoverDB.DownloadingPixbuf;
			else {
				image.SetFromStock ("muine-default-cover",
						    StockIcons.CoverSize);
			}
		
			if (song != null && !Global.CoverDB.Loading) {
				Gtk.Drag.DestSet (this, DestDefaults.All,
						  drag_entries, Gdk.DragAction.Copy);
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

				if (song.HasAlbum) {
					Album a = Global.DB.GetAlbum (song);

					a.SetCoverWeb (uri.AbsoluteUri);
				} else
					song.SetCoverWeb (uri.AbsoluteUri);

				success = true;

				break;
				
			case (uint) DndUtils.TargetType.UriList:
				uri_list = DndUtils.SplitSelectionData (data);
				fn = FileUtils.LocalPathFromUri (uri_list [0]);

				if (fn == null)
					break;

				try {
					if (song.HasAlbum) {
						Album a = Global.DB.GetAlbum (song);

						a.SetCoverLocal (fn);
					} else
						song.SetCoverLocal (fn);
						
					success = true;
				} catch {
					success = false;
				}
				
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
}
