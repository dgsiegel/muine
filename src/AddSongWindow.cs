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
	public class AddSongWindow : AddWindow
	{
		// GConf
		private const string GConfKeyWidth = "/apps/muine/add_song_window/width";
		private const int GConfDefaultWidth = 500;

		private const string GConfKeyHeight = "/apps/muine/add_song_window/height";
		private const int GConfDefaultHeight = 475;  

		// Strings
		private static readonly string string_title = 
			Catalog.GetString ("Play Song");

		// Static
		// Static :: Objects
		// Static :: Objects :: DnD targets
		private static TargetEntry [] source_entries = {
			DndUtils.TargetMuineSongList,
			DndUtils.TargetUriList
		};

		// Constructor
		/// <summary>
		///	Creates a new Add Song window.
		/// </summary>
		/// <remarks>
		///	This is created when "Play Song" is clicked.
		/// </remarks>
		public AddSongWindow ()
		{
			base.Title = string_title;

			base.SetGConfSize (GConfKeyWidth , GConfDefaultWidth, 
				GConfKeyHeight, GConfDefaultHeight);

			base.Items = Global.DB.Songs.Values;
						
			base.List.Model.SortFunc = new HandleModel.CompareFunc (SortFunc);

			TreeViewColumn col = new TreeViewColumn ();
			col.Sizing = TreeViewColumnSizing.Fixed;
			col.PackStart (base.TextRenderer, true);
			col.SetCellDataFunc (base.TextRenderer, new TreeCellDataFunc (CellDataFunc));
			base.List.AppendColumn (col);
			
			base.List.DragSource = source_entries;
			base.List.DragDataGet += new DragDataGetHandler (OnDragDataGet);

			Global.DB.SongAdded   += new SongDatabase.SongAddedHandler   (base.OnAdded  );
			Global.DB.SongChanged += new SongDatabase.SongChangedHandler (base.OnChanged);
			Global.DB.SongRemoved += new SongDatabase.SongRemovedHandler (base.OnRemoved);
		}

		// Handlers
		// Handlers :: OnDragDataGet (Gtk.DragDataGetHandler)
		/// <summary>
		/// 	Handler to be activated when Drag-and-Drop data is requested.
		/// </summary>
		/// <remarks>
		/// 	Songs may be copied by dragging them to Nautilus.
		/// </remarks>
		private void OnDragDataGet (object o, DragDataGetArgs args)
		{
			List songs = base.List.SelectedHandles;

			switch (args.Info) {
			case (uint) DndUtils.TargetType.UriList:
				string files = "";

				foreach (int p in songs) {
					IntPtr s = new IntPtr (p);
					files += FileUtils.UriFromLocalPath (Song.FromHandle (s).Filename) + "\r\n";
				}
		
				args.SelectionData.Set (Gdk.Atom.Intern (DndUtils.TargetUriList.Target, false),
					8, System.Text.Encoding.UTF8.GetBytes (files));
							
				break;	
				
			case (uint) DndUtils.TargetType.SongList:
				string ptrs = String.Format ("\t{0}\t", DndUtils.TargetMuineSongList.Target);
				
				foreach (int p in songs) {
					IntPtr s = new IntPtr (p);
					ptrs += s.ToString () + "\r\n";
				}
				
				args.SelectionData.Set (Gdk.Atom.Intern (DndUtils.TargetMuineSongList.Target, false),
					8, System.Text.Encoding.ASCII.GetBytes (ptrs));

				break;

			default:
				break;	
			}
		}

		// Delegate Functions
		// Delegate Functions :: SortFunc		
		/// <summary>
		/// 	Delegate used in sorting the song list.
		/// </summary>		
		/// <param name="a_ptr">
		///	Handler for first <see cref="Song" />.
		/// </param>
		/// <param name="b_ptr">
		///	Handler for second <see cref="Song" />.
		/// </param>
		/// <returns>
		///	The result of comparing the songs with
		///	<see cref="Item.CompareTo" />.
		/// </returns>
		/// <seealso cref="Item.CompareTo" />
		private int SortFunc (IntPtr a_ptr, IntPtr b_ptr)
		{
			Song a = Song.FromHandle (a_ptr);
			Song b = Song.FromHandle (b_ptr);

			return a.CompareTo (b);
		}

		// Delegate Functions :: CellDataFunc
		/// <summary>
		/// 	Delegate used to render the song text.
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
		private void CellDataFunc (TreeViewColumn col, CellRenderer cell,
					   TreeModel model, TreeIter iter)
		{
			CellRendererText r = (CellRendererText) cell;
			Song song = Song.FromHandle (base.List.Model.HandleFromIter (iter));

			r.Markup = String.Format ("<b>{0}</b>\n{1}",
				StringUtils.EscapeForPango (song.Title),
				StringUtils.EscapeForPango (StringUtils.JoinHumanReadable (song.Artists)));
		}
	}
}
