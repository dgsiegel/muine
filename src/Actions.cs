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
	public class Actions : ActionGroup
	{
		// Strings
		// Strings :: Menu
		private static readonly string string_file_menu =
			Catalog.GetString ("_File");

		private static readonly string string_song_menu =
			Catalog.GetString ("_Song");

		private static readonly string string_playlist_menu =
			Catalog.GetString ("_Playlist");

		private static readonly string string_help_menu =
			Catalog.GetString ("_Help");

		// Strings :: Menu :: File
		private static readonly string string_import =
			Catalog.GetString ("_Import Folder...");

		private static readonly string string_open =
			Catalog.GetString ("_Open Playlist...");

		private static readonly string string_save =
			Catalog.GetString ("_Save Playlist As...");

		private static readonly string string_toggle_visible_hide =
			Catalog.GetString ("Hide _Window");

		private static readonly string string_toggle_visible_show =
			Catalog.GetString ("Show _Window");

		// Strings :: Menu :: Song
		private static readonly string string_toggle_play =
			Catalog.GetString ("_Play");

		private static readonly string string_previous =
			Catalog.GetString ("_Previous");

		private static readonly string string_next =
			Catalog.GetString ("_Next");

		private static readonly string string_skip_to =
			Catalog.GetString ("_Skip to...");

		private static readonly string string_skip_backwards =
			Catalog.GetString ("Skip _Backwards");

		private static readonly string string_skip_forward =
			Catalog.GetString ("Skip _Forward");

		// Strings :: Menu :: Playlist
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

		private static readonly string string_toggle_repeat =
			Catalog.GetString ("R_epeat");

		private static readonly string string_shuffle =
			Catalog.GetString ("Shu_ffle");

		// Strings :: Menu :: Help
		private static readonly string string_about =
			Catalog.GetString ("_About");

		// Static
		// Static :: Objects
		// Static :: Objects :: Entries
		private static ActionEntry [] entries = {
			new ActionEntry ("FileMenu", null, string_file_menu,
				null, null, null),

			new ActionEntry ("SongMenu", null, string_song_menu,
				null, null, null),

			new ActionEntry ("PlaylistMenu", null, string_playlist_menu,
				null, null, null),

			new ActionEntry ("HelpMenu", null, string_help_menu,
				null, null, null),

			new ActionEntry ("Import", Stock.Execute, string_import,
				null, null, null),

			new ActionEntry ("Open", Stock.Open, string_open,
				"<control>O", null, null),

			new ActionEntry ("Save", Stock.SaveAs, string_save,
				"<shift><control>S", null, null),

			new ActionEntry ("ToggleVisible", null, "", // string set dynamically
				"Escape", null, null),

			new ActionEntry ("Quit", Stock.Quit, null,
				"<control>Q", null, null),
			
			new ActionEntry ("Previous", "stock_media-prev", string_previous,
				"B", null, null),

			new ActionEntry ("Next", "stock_media-next", string_next,
				"N", null, null),

			new ActionEntry ("SkipTo", Stock.JumpTo, string_skip_to,
				"T", null, null),

			new ActionEntry ("SkipBackwards", "stock_media-rew", string_skip_backwards,
				"<control>Left", null, null),

			new ActionEntry ("SkipForward", "stock_media-fwd", string_skip_forward,
				"<control>Right", null, null),

			new ActionEntry ("PlaySong", Stock.Add, string_play_song,
				"S", null, null),

			new ActionEntry ("PlayAlbum", "gnome-dev-cdrom-audio", string_play_album,
				"A", null, null),

			new ActionEntry ("Remove", Stock.Remove, string_remove,
				"Delete", null, null),

			new ActionEntry ("RemovePlayed", null, string_remove_played,
				"<control>Delete", null, null),

			new ActionEntry ("Clear", Stock.Clear, string_clear,
				null, null, null),

			new ActionEntry ("Shuffle", "stock_shuffle", string_shuffle,
				"<control>S", null, null),

			new ActionEntry ("About", Gnome.Stock.About, string_about,
				null, null, null)
		};

		// Static :: Objects :: Toggle Entries
		private static ToggleActionEntry [] toggle_entries = {
			new ToggleActionEntry ("TogglePlay", "stock_media-play", string_toggle_play,
			       "P", null, null, false),

			new ToggleActionEntry ("ToggleRepeat", null, string_toggle_repeat,
			       "<control>R", null, null, false),
		};

		// Static :: Properties
		// Static :: Properties :: StringToggleVisibleHide (get;)
		public static string StringToggleVisibleHide {
			get { return string_toggle_visible_hide; }
		}

		// Static :: Properties :: StringToggleVisibleShow (get;)
		public static string StringToggleVisibleShow {
			get { return string_toggle_visible_show; }
		}
		
		// Static :: Properties :: Entries (get;)
		public static ActionEntry [] Entries {
			get { return entries; }
		}
		
		// Static :: Properties :: ToggleEntries (get;)
		public static ToggleActionEntry [] ToggleEntries {
			get { return toggle_entries; }
		}

		// Objects
		private UIManager ui_manager = new UIManager ();

		// Constructor
		public Actions () : base ("Actions")
		{
			Add (entries);
			Add (toggle_entries);

			ui_manager.InsertActionGroup (this, 0);
			ui_manager.AddUiFromResource ("PlaylistWindow.xml");
			
			// Setup Callbacks
                        this ["Import"       ].Activated += new EventHandler (OnImport       );
                        this ["Open"         ].Activated += new EventHandler (OnOpen         );
                        this ["Save"         ].Activated += new EventHandler (OnSave         );
                        this ["ToggleVisible"].Activated += new EventHandler (OnToggleVisible);
                        this ["Quit"         ].Activated += new EventHandler (OnQuit         );
                        this ["Previous"     ].Activated += new EventHandler (OnPrevious     );
                        this ["Next"         ].Activated += new EventHandler (OnNext         );
                        this ["SkipTo"       ].Activated += new EventHandler (OnSkipTo       );
                        this ["SkipBackwards"].Activated += new EventHandler (OnSkipBackwards);
                        this ["SkipForward"  ].Activated += new EventHandler (OnSkipForward  );
                        this ["PlaySong"     ].Activated += new EventHandler (OnPlaySong     );
                        this ["PlayAlbum"    ].Activated += new EventHandler (OnPlayAlbum    );
                        this ["Remove"       ].Activated += new EventHandler (OnRemove       );
                        this ["RemovePlayed" ].Activated += new EventHandler (OnRemovePlayed );
                        this ["Clear"        ].Activated += new EventHandler (OnClear        );
                        this ["Shuffle"      ].Activated += new EventHandler (OnShuffle      );
                        this ["About"        ].Activated += new EventHandler (OnAbout        );
                        this ["TogglePlay"   ].Activated += new EventHandler (OnTogglePlay   );
                        this ["ToggleRepeat" ].Activated += new EventHandler (OnToggleRepeat );
		}

		// Properties
		// Properties :: UIManager (get;)
		public UIManager UIManager {
			get { return ui_manager; }
		}
		
		// Properties :: MenuBar (get;)
		public Gtk.Widget MenuBar {
			get { return ui_manager.GetWidget ("/MenuBar"); }
		}
		
		// Handlers
		// Handlers :: OnImport
		private void OnImport (object o, EventArgs args)
		{
			new ImportDialog ();
		}

		// Handlers :: OnOpen
		private void OnOpen (object o, EventArgs args)
		{
			new OpenDialog ();
		}

		// Handlers :: OnSave
		private void OnSave (object o, EventArgs args)
		{
			new SaveDialog ();
		}
		
		// Handlers :: OnToggleWindowVisible
		private void OnToggleVisible (object o, EventArgs args)
		{
			Global.Playlist.ToggleVisible ();
		}

		// Handlers :: OnQuit
		private void OnQuit (object o, EventArgs args)
		{
			Global.Playlist.Quit ();
		}

		// Handlers :: OnPrevious
		private void OnPrevious (object o, EventArgs args)
		{
			Global.Playlist.Previous ();
		}
		
		// Handlers :: OnNext
		private void OnNext (object o, EventArgs args)
		{
			Global.Playlist.Next ();
		}
		
		// Handlers :: OnSkipTo
		private void OnSkipTo (object o, EventArgs args)
		{
			Global.Playlist.RunSkipToDialog ();
		}

		// Handlers :: OnSkipBackwards
		private void OnSkipBackwards (object o, EventArgs args)
		{
			Global.Playlist.SkipBackwards ();
		}

		// Handlers :: OnSkipForward
		private void OnSkipForward (object o, EventArgs args)
		{
			Global.Playlist.SkipForward ();
		}

		// Handlers :: OnPlaySong
		private void OnPlaySong (object o, EventArgs args)
		{
			Global.Playlist.PlaySong ();
		}

		// Handlers :: OnPlayAlbum
		private void OnPlayAlbum (object o, EventArgs args)
		{
			Global.Playlist.PlayAlbum ();
		}

		// Handlers :: OnRemove
		private void OnRemove (object o, EventArgs args)
		{
			Global.Playlist.RemoveSelected ();
		}

		// Handlers :: OnRemovePlayed
		private void OnRemovePlayed (object o, EventArgs args)
		{
			Global.Playlist.RemovePlayed ();
		}

		// Handlers :: OnClear
		private void OnClear (object o, EventArgs args)
		{
			Global.Playlist.Clear ();
		}

		// Handlers :: OnShuffle
		private void OnShuffle (object o, EventArgs args)
		{
			Global.Playlist.Shuffle ();
		}

		// Handlers :: OnAbout
		private void OnAbout (object o, EventArgs args)
		{
			new Muine.About (Global.Playlist);
		}

		// Handlers :: OnTogglePlay
		private void OnTogglePlay (object o, EventArgs args)
		{
			ToggleAction a = (ToggleAction) o;

			if (a.Active == Global.Playlist.Playing)
				return;

			Global.Playlist.Playing = a.Active;
		}

		// Handlers :: OnToggleRepeat
		private void OnToggleRepeat (object o, EventArgs args)
		{
			ToggleAction a = (ToggleAction) o;

			if (a.Active == Global.Playlist.Repeat)
				return;

			Global.Playlist.Repeat = a.Active;
		}
	}
}
