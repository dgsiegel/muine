/*
 * Copyright (C) 2005 Tamara Roberson <foxxygirltamara@gmail.com>
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
using System.IO;
using System.Collections;

using Mono.Posix;

using Gtk;

namespace Muine
{
	public class Actions
	{
		// Strings
		private static readonly string string_hide_window =
			Catalog.GetString ("Hide _Window");
		private static readonly string string_show_window =
			Catalog.GetString ("Show _Window");
		private static readonly string string_file =
			Catalog.GetString ("_File");
		private static readonly string string_song =
			Catalog.GetString ("_Song");
		private static readonly string string_playlist =
			Catalog.GetString ("_Playlist");
		private static readonly string string_help =
			Catalog.GetString ("_Help");
		private static readonly string string_import =
			Catalog.GetString ("_Import Folder...");
		private static readonly string string_open =
			Catalog.GetString ("_Open Playlist...");
		private static readonly string string_save =
			Catalog.GetString ("_Save Playlist As...");
		private static readonly string string_previous =
			Catalog.GetString ("_Previous");
		private static readonly string string_next =
			Catalog.GetString ("_Next");
		private static readonly string string_skip_to =
			Catalog.GetString ("_Skip to...");
		private static readonly string string_skip_back =
			Catalog.GetString ("Skip _Backwards");
		private static readonly string string_skip_forward =
			Catalog.GetString ("Skip _Forward");
		private static readonly string string_play_song =
			Catalog.GetString ("Play _Song...");
		private static readonly string string_play_album =
			Catalog.GetString ("Play _Album...");
		private static readonly string string_remove =
			Catalog.GetString ("_Remove Song");
		private static readonly string string_remove_played =
			Catalog.GetString ("Remove _Played Songs");
		private static readonly string string_clear =
			Catalog.GetString ("_Clear");
		private static readonly string string_shuffle =
			Catalog.GetString ("Shu_ffle");
		private static readonly string string_about =
			Catalog.GetString ("_About");
		private static readonly string string_play =
			Catalog.GetString ("_Play");
		private static readonly string string_repeat =
			Catalog.GetString ("R_epeat");

		// Entries
		private static ActionEntry [] entries = new ActionEntry [] {
			new ActionEntry ("FileMenu", null, string_file,
					 null, null, null),

			new ActionEntry ("SongMenu", null, string_song,
					 null, null, null),

			new ActionEntry ("PlaylistMenu", null, string_playlist,
					 null, null, null),

			new ActionEntry ("HelpMenu", null, string_help,
					 null, null, null),

			new ActionEntry ("ImportFolder", Stock.Execute, string_import,
					 null, null, null),

			new ActionEntry ("OpenPlaylist", Stock.Open, string_open,
					 "<control>O", null, null),

			new ActionEntry ("SavePlaylistAs", Stock.SaveAs, string_save,
					 "<shift><control>S", null, null),

			new ActionEntry ("ShowHideWindow", null, "",
					 "Escape", null, null),

			new ActionEntry ("Quit", Stock.Quit, null,
					 "<control>Q", null, null),
			
			new ActionEntry ("PreviousSong", "stock_media-prev", string_previous,
					 "P", null, null),

			new ActionEntry ("NextSong", "stock_media-next", string_next,
					 "N", null, null),

			new ActionEntry ("SkipTo", Stock.JumpTo, string_skip_to,
					 "T", null, null),

			new ActionEntry ("SkipBackwards", "stock_media-rew", string_skip_back,
					 "<control>Left", null, null),

			new ActionEntry ("SkipForward", "stock_media-fwd", string_skip_forward,
					 "<control>Right", null, null),

			new ActionEntry ("PlaySong", Stock.Add, string_play_song,
					 "S", null, null),

			new ActionEntry ("PlayAlbum", "gnome-dev-cdrom-audio", string_play_album,
					 "A", null, null),

			new ActionEntry ("RemoveSong", Stock.Remove, string_remove,
					 "Delete", null, null),

			new ActionEntry ("RemovePlayedSongs", null, string_remove_played,
					 "<control>Delete", null, null),

			new ActionEntry ("ClearPlaylist", Stock.Clear, string_clear,
					 null, null, null),

			new ActionEntry ("Shuffle", "stock_shuffle", string_shuffle,
					 "<control>S", null, null),

			new ActionEntry ("About", Gnome.Stock.About, string_about,
					 null, null, null)
		};

		// Toggle Entries
		private static ToggleActionEntry [] toggle_entries = new ToggleActionEntry [] {
			new ToggleActionEntry ("PlayPause", "stock_media-play", string_play,
					       "space", null, null, false),

			new ToggleActionEntry ("Repeat", null, string_repeat,
					       "<control>R", null, null, false),
		};
		
		// Static Properties
		public static string StringHideWindow {
			get { return string_hide_window; }
		}
		
		public static string StringShowWindow {
			get { return string_show_window; }
		}
		
		public static ActionEntry [] Entries {
			get { return entries; }
		}
		
		public static ToggleActionEntry [] ToggleEntries {
			get { return toggle_entries; }
		}

		// Objects
		private ActionGroup action_group = new ActionGroup ("Actions");
		private UIManager ui_manager = new UIManager ();

		// Constructor
		public Actions ()
		{
			action_group.Add (entries);
			action_group.Add (toggle_entries);

			ui_manager.InsertActionGroup (action_group, 0);
			ui_manager.AddUiFromResource ("PlaylistWindow.xml");
			
			// Setup Callbacks
                        this.Import.Activated += new EventHandler (OnImportFolder);
                        this.Open.Activated += new EventHandler (OnOpenPlaylist);
                        this.Save.Activated += new EventHandler (OnSavePlaylistAs);
                        this.Visibility.Activated += new EventHandler (OnToggleWindowVisibility);
                        this.Quit.Activated += new EventHandler (OnQuit);
                        this.Previous.Activated += new EventHandler (OnPrevious);
                        this.Next.Activated += new EventHandler (OnNext);
                        this.SkipTo.Activated += new EventHandler (OnSkipTo);
                        this.SkipBackwards.Activated += new EventHandler (OnSkipBackwards);
                        this.SkipForward.Activated += new EventHandler (OnSkipForward);
                        this.PlaySong.Activated += new EventHandler (OnPlaySong);
                        this.PlayAlbum.Activated += new EventHandler (OnPlayAlbum);
                        this.Remove.Activated += new EventHandler (OnRemoveSong);
                        this.RemovePlayed.Activated += new EventHandler (OnRemovePlayedSongs);
                        this.Clear.Activated += new EventHandler (OnClearPlaylist);
                        this.Shuffle.Activated += new EventHandler (OnShuffle);
                        this.About.Activated += new EventHandler (OnAbout);

                        this.PlayPause.Activated += new EventHandler (OnPlayPause);
                        this.Repeat.Activated += new EventHandler (OnRepeat);
			
		}
		
		// Properties
		public Action Import {
			get { return action_group.GetAction ("ImportFolder"); }
		}
		
		public Action Open {
			get { return action_group.GetAction ("OpenPlaylist"); }
		}
		
		public Action Save {
			get { return action_group.GetAction ("SavePlaylistAs"); }
		}

		public Action Visibility {
			get { return action_group.GetAction ("ShowHideWindow"); }
		}

		public Action Quit {
			get { return action_group.GetAction ("Quit"); }
		}
		
		public Action Previous {
			get { return action_group.GetAction ("PreviousSong"); }
		}
		
		public Action Next {
			get { return action_group.GetAction ("NextSong"); }
		}

		public Action SkipTo {
			get { return action_group.GetAction ("SkipTo"); }
		}
		
		public Action SkipBackwards {
			get { return action_group.GetAction ("SkipBackwards"); }
		}
		
		public Action SkipForward {
			get { return action_group.GetAction ("SkipForward"); }
		}
		
		public Action PlaySong {
			get { return action_group.GetAction ("PlaySong"); }
		}
		
		public Action PlayAlbum {
			get { return action_group.GetAction ("PlayAlbum"); }
		}

		public Action Remove {
			get { return action_group.GetAction ("RemoveSong"); }
		}
		
		public Action RemovePlayed {
			get { return action_group.GetAction ("RemovePlayedSongs"); }
		}
		
		public Action Clear {
			get { return action_group.GetAction ("ClearPlaylist"); }
		}
		
		public Action Shuffle {
			get { return action_group.GetAction ("Shuffle"); }		
		}
		
		public Action About {
			get { return action_group.GetAction ("About"); }
		}
		
		public ToggleAction PlayPause {
			get { return (ToggleAction) action_group.GetAction ("PlayPause"); }
		}

		public ToggleAction Repeat {
			get { return (ToggleAction) action_group.GetAction ("Repeat"); }
		}
		
		public UIManager UIManager {
			get { return ui_manager; }
		}
				
		public Gtk.Widget MenuBar {
			get { return ui_manager.GetWidget ("/MenuBar"); }
		}
		
		// Handlers
		private void OnImportFolder (object o, EventArgs args) 
		{
			Global.Playlist.RunImportDialog ();
		}

		private void OnOpenPlaylist (object o, EventArgs args)
		{
			Global.Playlist.RunOpenDialog ();
		}

		private void OnSavePlaylistAs (object o, EventArgs args)
		{
			Global.Playlist.RunSaveDialog ();
		}
		
		private void OnToggleWindowVisibility (object o, EventArgs args)
		{
			Global.Playlist.ToggleVisibility ();
		}

		private void OnQuit (object o, EventArgs args)
		{
			Global.Playlist.Quit ();
		}

		private void OnPrevious (object o, EventArgs args)
		{
			Global.Playlist.Previous ();
		}
		
		private void OnNext (object o, EventArgs args)
		{
			Global.Playlist.Next ();
		}
		
		private void OnSkipTo (object o, EventArgs args)
		{
			Global.Playlist.RunSkipToDialog ();
		}

		private void OnSkipBackwards (object o, EventArgs args)
		{
			Global.Playlist.SkipBackwards ();
		}

		private void OnSkipForward (object o, EventArgs args)
		{
			Global.Playlist.SkipForward ();
		}

		private void OnPlaySong (object o, EventArgs args)
		{
			Global.Playlist.PlaySong ();
		}

		private void OnPlayAlbum (object o, EventArgs args)
		{
			Global.Playlist.PlayAlbum ();
		}

		private void OnRemoveSong (object o, EventArgs args)
		{
			Global.Playlist.RemoveSelectedSong ();
		}

		private void OnRemovePlayedSongs (object o, EventArgs args)
		{
			Global.Playlist.RemovePlayedSongs ();
		}

		private void OnClearPlaylist (object o, EventArgs args)
		{
			Global.Playlist.Clear ();
		}

		private void OnShuffle (object o, EventArgs args)
		{
			Global.Playlist.Shuffle ();
		}

		private void OnAbout (object o, EventArgs args)
		{
			Muine.About.ShowWindow (Global.Playlist);
		}
		
		private void OnPlayPause (object o, EventArgs args)
		{
			Global.Playlist.TogglePlaying ();
		}

		private void OnRepeat (object o, EventArgs args)
		{
			Global.Playlist.ToggleRepeat ();
		}
	}
}
