/*
 * Copyright (C) 2003, 2004, 2005 Jorn Baayen <jbaayen@gnome.org>
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

using Gtk;
using GLib;

using Mono.Unix;

namespace Muine
{
	public class AddAlbumWindow : AddWindow
	{
		// GConf
		// GConf :: Width
		private const string GConfKeyWidth = "/apps/muine/add_album_window/width";
		private const int GConfDefaultWidth = 500;

		// GConf :: Height
		private const string GConfKeyHeight = "/apps/muine/add_album_window/height";
		private const int GConfDefaultHeight = 475; 

		// Strings
		private static readonly string string_title = 
			Catalog.GetString ("Play Album");

		private static readonly string string_artists = 
			Catalog.GetString ("Performed by {0}");

		// Static
		// Static :: Objects
		// Static :: Objects :: DnD Targets
		private static TargetEntry [] source_entries = {
			DndUtils.TargetMuineAlbumList,
			DndUtils.TargetUriList
		};

		// Widgets
		private CellRenderer pixbuf_renderer = new CellRendererPixbuf ();

		private Gdk.Pixbuf nothing_pixbuf =
		  new Gdk.Pixbuf (null, "muine-nothing.png");

		// Variables
		private bool drag_dest_enabled = false;
		private int pixbuf_column_width = CoverDatabase.CoverSize + (5 * 2);

		// Constructor
		/// <summary>
		///	Creates a new Add Album window.
		/// </summary>
		/// <remarks>
		///	This is created when "Play Album" is clicked.
		/// </remarks>
		public AddAlbumWindow ()
		{
			base.Title = string_title;

			base.SetGConfSize (GConfKeyWidth , GConfDefaultWidth, 
					   GConfKeyHeight, GConfDefaultHeight);

			base.Items = Global.DB.Albums.Values;
						
			base.List.Model.SortFunc = new HandleModel.CompareFunc (SortFunc);

			// Column
			TreeViewColumn col = new TreeViewColumn ();
			col.Sizing = TreeViewColumnSizing.Fixed;
			col.Spacing = 4;

			col.PackStart (pixbuf_renderer  , false);
			col.PackStart (base.TextRenderer, true );
			
			TreeCellDataFunc func1 = new TreeCellDataFunc (PixbufCellDataFunc);
			TreeCellDataFunc func2 = new TreeCellDataFunc (TextCellDataFunc  );
			
			col.SetCellDataFunc (pixbuf_renderer  , func1);
			col.SetCellDataFunc (base.TextRenderer, func2);

			base.List.AppendColumn (col);

			base.List.DragSource = source_entries;
			base.List.DragDataGet += OnDragDataGet;

			Global.DB.AlbumAdded   += base.OnAdded;
			Global.DB.AlbumChanged += base.OnChanged;
			Global.DB.AlbumRemoved += base.OnRemoved;

			Global.CoverDB.DoneLoading += OnCoversDoneLoading;

			if (!Global.CoverDB.Loading)
				EnableDragDest ();
		}

		// Methods
		// Methods :: Private
		// Methods :: Private :: EnableDragDest
		/// <summary>
		/// 	Turns on Drag-and-Drop.
		/// </summary>
		private void EnableDragDest ()
		{
			if (drag_dest_enabled)
				return;

			base.List.DragDataReceived += OnDragDataReceived;

			Gtk.Drag.DestSet (base.List, DestDefaults.All,
				CoverImage.DragEntries, Gdk.DragAction.Copy);

			drag_dest_enabled = true;
		}

		// Handlers
		// Handlers :: OnCoversDoneLoading
		/// <csummary>
		/// 	Handler called when the album covers are done loading.
		/// </summary>
		/// <remarks>
		///	Enables Drag-and-Drop and redraws the list.
		/// </remarks>
		private void OnCoversDoneLoading ()
		{
			EnableDragDest ();

			base.List.QueueDraw ();
		}

		// Handlers :: OnDragDataReceived
		/// <summary>
		/// 	Handler called when Drag-and-Drop data is received.
		/// </summary>
		/// <remarks>
		/// 	External covers may be Drag-and-Dropped onto an album.
		/// </remarks>
		private void OnDragDataReceived (object o, DragDataReceivedArgs args)
		{
			TreePath path;
			if (!base.List.GetPathAtPos (args.X, args.Y, out path))
				return;

			IntPtr album_ptr = base.List.Model.HandleFromPath (path);
			Album album = Album.FromHandle (album_ptr);

			CoverImage.HandleDrop ((Song) album.Songs [0], args);
		}

		// Handlers :: OnDragDataGet
		/// <summary>
		/// 	Handler to be activated when Drag-and-Drop data is requested.
		/// </summary>
		/// <remarks>
		/// 	Albums may be copied by dragging them to Nautilus.
		/// </remarks>
		private void OnDragDataGet (object o, DragDataGetArgs args)
		{
			List albums = base.List.SelectedHandles;
			
			string target;
			Gdk.Atom atom;
			byte [] bytes;

			switch (args.Info) {
			case (uint) DndUtils.TargetType.UriList:
				string files = "";

				foreach (int i in albums) {
					IntPtr p = new IntPtr (i);
					Album a = Album.FromHandle (p);

					foreach (Song s in a.Songs) {
						string uri = FileUtils.UriFromLocalPath (s.Filename);
						files += (uri + "\r\n");
					}
				}

				target = DndUtils.TargetUriList.Target;
				atom = Gdk.Atom.Intern (target, false);
				bytes = System.Text.Encoding.UTF8.GetBytes (files);
				args.SelectionData.Set (atom, 8, bytes);

				break;

			case (uint) DndUtils.TargetType.AlbumList:
				target = DndUtils.TargetMuineAlbumList.Target;
			
				string ptrs = String.Format ("\t{0}\t", target);
				
				foreach (int p in albums) {
					IntPtr s = new IntPtr (p);
					string s_s = s.ToString ();
					ptrs += s_s + "\r\n";
				}
				
				atom = Gdk.Atom.Intern (target, false);
				bytes = System.Text.Encoding.ASCII.GetBytes (ptrs); 
				args.SelectionData.Set (atom, 8, bytes);
							
				break;

			default:
				break;	
			}
		}

		// Delegate Functions		
		// Delegate Functions :: SortFunc
		/// <summary>
		/// 	Delegate used in sorting the album list.
		/// </summary>
		/// <param name="a_ptr">
		///	Handler for first <see cref="Album" />.
		/// </param>
		/// <param name="b_ptr">
		///	Handler for second <see cref="Album" />.
		/// </param>
		/// <returns>
		///	The result of comparing the albums with
		///	<see cref="Item.CompareTo" />.
		/// </returns>
		/// <seealso cref="Item.CompareTo" />
		private int SortFunc (IntPtr a_ptr, IntPtr b_ptr)
		{
			Album a = Album.FromHandle (a_ptr);
			Album b = Album.FromHandle (b_ptr);

			return a.CompareTo (b);
		}

		// Delegate Functions :: PixbufCellDataFunc
		/// <summary>
		/// 	Delegate used to render the covers.
		/// </summary>
		/// <param name="col">
		///	A <see cref="Gtk.TreeViewColumn" />.
		/// </param>
		/// <param name="cell">
		///	A <see cref="Gtk.CellRenderer" />.
		/// </param>
		/// <param name="model">
		///	A <see cref="Gtk.TreeModel" />.
		/// </param>
		/// <param name="iter">
		///	A <see cref="Gtk.TreeIter" />.
		/// </param>
		private void PixbufCellDataFunc
		  (TreeViewColumn col, CellRenderer cell, TreeModel model,
		   TreeIter iter)
		{
			CellRendererPixbuf r = (CellRendererPixbuf) cell;
			
			IntPtr handle = base.List.Model.HandleFromIter (iter);
			Album album = Album.FromHandle (handle);

			if (album.CoverImage != null)
				r.Pixbuf = album.CoverImage;

			else if (Global.CoverDB.Loading)
				r.Pixbuf = Global.CoverDB.DownloadingPixbuf;

			else
				r.Pixbuf = nothing_pixbuf;

			r.Width = r.Height = pixbuf_column_width;
		}

		// Delegate Functions :: TextCellDataFunc
		/// <summary>
		/// 	Delegate used to render the album text.
		/// </summary>		
		/// <param name="col">
		///	A <see cref="Gtk.TreeViewColumn" />.
		/// </param>
		/// <param name="cell">
		///	A <see cref="Gtk.CellRenderer" />.
		/// </param>
		/// <param name="model">
		///	A <see cref="Gtk.TreeModel" />.
		/// </param>
		/// <param name="iter">
		///	A <see cref="Gtk.TreeIter" />.
		/// </param>
		private void TextCellDataFunc
		  (TreeViewColumn col, CellRenderer cell, TreeModel model,
		   TreeIter iter)
		{
			CellRendererText r = (CellRendererText) cell;
			
			IntPtr handle = base.List.Model.HandleFromIter (iter);
			Album album = Album.FromHandle (handle);

			string performers = "";
			if (album.Performers.Length > 0) {
				string performers_tmp =
				  StringUtils.JoinHumanReadable (album.Performers, 2);
			
				string performers_tmp2 =
				  String.Format (string_artists, performers_tmp);
			
				performers = StringUtils.EscapeForPango (performers_tmp2);
			}

			string fmt = "<b>{0}</b>\n{1}\n\n{2}";
			string album_s = StringUtils.EscapeForPango (album.Name);

			string artists_tmp =
			  StringUtils.JoinHumanReadable (album.Artists, 3);
			  
			string artists = StringUtils.EscapeForPango (artists_tmp);

			r.Markup = String.Format (fmt, album_s, artists, performers);
		}
	}
}
