/*
 * Copyright (C) 2005 Jorn Baayen <jbaayen@gnome.org>
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

using System.Text.RegularExpressions;

using Gtk;

namespace Muine
{
	public sealed class DndUtils 
	{
		// Enums
		// Enums :: Drag-and-Drop TargetType
		public enum TargetType {
			UriList,
			Uri,
			SongList,
			AlbumList,
			ModelRow
		};

		// Drag-and-Drop Targets
		public static readonly TargetEntry TargetUriList = 
			new TargetEntry ("text/uri-list", 0, (uint) TargetType.UriList);
			
		public static readonly TargetEntry TargetGnomeIconList = 
			new TargetEntry ("x-special/gnome-icon-list", 0, (uint) TargetType.UriList);
			
		public static readonly TargetEntry TargetNetscapeUrl = 
			new TargetEntry ("_NETSCAPE_URL", 0, (uint) TargetType.Uri);
			
		public static readonly TargetEntry TargetMuineAlbumList = 
			new TargetEntry ("MUINE_ALBUM_LIST", TargetFlags.App, (uint) TargetType.AlbumList);

		public static readonly TargetEntry TargetMuineSongList = 
			new TargetEntry ("MUINE_SONG_LIST", TargetFlags.App, (uint) TargetType.SongList);
			
		public static readonly TargetEntry TargetMuineTreeModelRow = 
			new TargetEntry ("MUINE_TREE_MODEL_ROW", TargetFlags.Widget, (uint) TargetType.ModelRow);

		// Methods
		// Methods :: Public
		// Methods :: Public :: SelectionDataToString
		public static string SelectionDataToString (Gtk.SelectionData data)
		{
			return System.Text.Encoding.UTF8.GetString (data.Data);
		}

		// Methods :: Public :: SplitSelectionData
		public static string [] SplitSelectionData (Gtk.SelectionData data)
		{
			string str = SelectionDataToString (data);
			return SplitSelectionData (str);
		}

		public static string [] SplitSelectionData (string data)
		{
			return Regex.Split (data, "\r\n");
		}
	}
}
