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
using System.IO;

using Gtk;
using GLib;
using Gnome.Vfs;

using Mono.Posix;

using MuinePluginLib;

namespace Muine
{
	public class PlaylistWindow : Window, IPlayer
	{
		private const string GConfKeyWidth = "/apps/muine/playlist_window/width";
		private const int GConfDefaultWidth = 450; 

		private const string GConfKeyHeight = "/apps/muine/playlist_window/height";
		private const int GConfDefaultHeight = 475;

		private const string GConfKeyVolume = "/apps/muine/volume";
		private const int GConfDefaultVolume = 50;

		private const string GConfKeyRepeat = "/apps/muine/repeat";
		private const bool GConfDefaultRepeat = false;

		private const string GConfKeyImportFolder = "/apps/muine/default_import_folder";
		private const string GConfDefaultImportFolder = "~";

		/* menu widgets */
		private Action previous_action;
		private Action next_action;
		private Action skip_to_action;
		private Action skip_forward_action;
		private Action skip_backwards_action;
		private Action remove_song_action;
		private Action shuffle_action;
		private Action window_visibility_action;
		private ToggleAction play_pause_action;
		private ToggleAction repeat_action;
		private bool block_repeat_action = false;
		private bool block_play_pause_action = false;

		/* toolbar widgets */
		[Glade.Widget]
		private Button previous_button;
		[Glade.Widget]
		private ToggleButton play_pause_button;
		[Glade.Widget]
		private Button next_button;
		[Glade.Widget]
		private Image play_pause_image;
		private VolumeButton volume_button;

		/* player area */
		private CoverImage cover_image;
		private EllipsizingLabel title_label;
		private EllipsizingLabel artist_label;
		[Glade.Widget]
		private Label time_label;

		/* playlist area */
		[Glade.Widget]
		private Label playlist_label;
		[Glade.Widget]
		private EventBox playlist_label_event_box;
		private HandleView playlist;

		/* other widgets */
		private Tooltips tooltips;

		/* windows */
		SkipToWindow skip_to_window = null;
		AddSongWindow add_song_window = null;
		AddAlbumWindow add_album_window = null;
		ProgressWindow checking_changes_progress_window = null;

		/* the player object */
		private Player player;
		private bool had_last_eos;
		private bool ignore_song_change;

		/* Drag and drop targets. */
		private static TargetEntry [] drag_entries = new TargetEntry [] {
			DndUtils.TargetUriList
		};

		private static TargetEntry [] playlist_source_entries = new TargetEntry [] {
			DndUtils.TargetMuineTreeModelRow,
			DndUtils.TargetUriList
		};
			
		private static TargetEntry [] playlist_dest_entries = new TargetEntry [] {
			DndUtils.TargetMuineTreeModelRow,
			DndUtils.TargetMuineSongList,
			DndUtils.TargetMuineAlbumList,
			DndUtils.TargetUriList
		};

		/* public properties, for plug-ins and for dbus interface */
		public ISong PlayingSong {
			get {
				if (playlist.Playing != IntPtr.Zero)
					return Song.FromHandle (playlist.Playing);
				else
					return null;
			}
		}

		public bool Playing {
			set {
				if (!playlist.HasFirst)
					return;

				if (value) {
					if (had_last_eos) {
						PlayFirstAndSelect ();
		
						PlaylistChanged ();
					}

					player.Play ();
				} else
					player.Pause ();
			}

			get { return player.Playing; }
		}

		public int Volume {
			set {
				if (value <= 100 && value >= 0)
					volume_button.Volume = value;
			}
			
			get { return volume_button.Volume; }
		}

		public int Position {
			set { SeekTo (value); }

			get { return player.Position; }
		}

		public bool HasNext {
			get { return playlist.HasNext; }
		}

		public bool HasPrevious {
			get { return playlist.HasPrevious; }
		}

		private ISong [] ArrayFromList (List list)
		{
			ISong [] array = new ISong [list.Count];
				
			int i = 0;
			foreach (int p in list) {
				array [i] = Song.FromHandle (new IntPtr (p));

				i ++;
			}

			return array;
		}

		public ISong [] Playlist {
			get {
				return ArrayFromList (playlist.Contents);
			}
		}

		public ISong [] Selection {
			get {
				return ArrayFromList (playlist.SelectedPointers);
			}
		}

		private UIManager ui_manager;
		public UIManager UIManager {
			get { return ui_manager; }
		}

		public Window Window {
			get { return this; }
		}

		public event Plugin.SongChangedEventHandler SongChangedEvent;

		public event Plugin.StateChangedEventHandler StateChangedEvent;

		public event Plugin.GenericEventHandler PlaylistChangedEvent;

		public event Plugin.GenericEventHandler SelectionChangedEvent;

		/* Constructor */
		public PlaylistWindow () : base (WindowType.Toplevel)
		{
			/* build the interface */
			Glade.XML glade_xml = new Glade.XML (null, "PlaylistWindow.glade", "main_vbox", null);
			glade_xml.Autoconnect (this);
				
			Add (glade_xml ["main_vbox"]);

			/* hook up window signals */
			WindowStateEvent += new WindowStateEventHandler (OnWindowStateEvent);
			DeleteEvent += new DeleteEventHandler (OnDeleteEvent);
			DragDataReceived += new DragDataReceivedHandler (OnDragDataReceived);

			Gtk.Drag.DestSet (this, DestDefaults.All,
					  drag_entries, Gdk.DragAction.Copy);

			/* keep track of window visibility */
			VisibilityNotifyEvent += new VisibilityNotifyEventHandler (OnVisibilityNotifyEvent);
			AddEvents ((int) Gdk.EventMask.VisibilityNotifyMask);

			/* set up various other UI bits */
			SetupWindowSize ();
			SetupPlayer (glade_xml); /* Has to be before the others,
						    they need the Player object */
			SetupMenus (glade_xml);
			SetupButtons (glade_xml);
			SetupPlaylist (glade_xml);

			checking_changes_progress_window = new ProgressWindow (this);

			/* connect to song database signals */
			Muine.DB.DoneCheckingChanges +=
				new SongDatabase.DoneCheckingChangesHandler (OnDoneCheckingChanges);
			
			Muine.DB.SongAdded   += new SongDatabase.SongAddedHandler (OnSongAdded);
			Muine.DB.SongChanged += new SongDatabase.SongChangedHandler (OnSongChanged);
			Muine.DB.SongRemoved += new SongDatabase.SongRemovedHandler (OnSongRemoved);

			/* make sure the interface is up to date */
			SelectionChanged ();
			StateChanged (false, true);
		}

		public void RestorePlaylist ()
		{
			/* load last playlist */
			System.IO.FileInfo finfo = new System.IO.FileInfo (FileUtils.PlaylistFile);
			if (finfo.Exists)
				OpenPlaylist (FileUtils.PlaylistFile);
		}

		public void Run ()
		{
			if (!playlist.HasFirst) {
				SongChanged (true); /* make sure the UI is up to date */

				PlaylistChanged ();
			}

			WindowVisible = true;
		}

		private void OnPlaylistLabelDragDataGet (object o, DragDataGetArgs args)
		{
			switch (args.Info) {
			case (uint) DndUtils.TargetType.UriList:
				string file = System.IO.Path.Combine (FileUtils.TempDirectory,
							Catalog.GetString ("Playlist.m3u"));

				SavePlaylist (file, false, false);
				
				string uri = FileUtils.UriFromLocalPath (file);

				args.SelectionData.Set (Gdk.Atom.Intern (DndUtils.TargetUriList.Target, false),
							8, System.Text.Encoding.UTF8.GetBytes (uri));
							
				break;
			default:
				break;
			}
		}

		private void OnDragDataReceived (object o, DragDataReceivedArgs args)
		{
			string [] uri_list;
			string fn;

			switch (args.Info) {
			case (uint) DndUtils.TargetType.UriList:
				uri_list = DndUtils.SplitSelectionData (args.SelectionData);
				fn = FileUtils.LocalPathFromUri (uri_list [0]);

				if (fn == null) {
					Drag.Finish (args.Context, false, false, args.Time);
					return;
				}
					
				break;

			default:
				Drag.Finish (args.Context, false, false, args.Time);
				return;
			}

			DirectoryInfo dinfo = new DirectoryInfo (fn);
			if (!dinfo.Exists) {
				Drag.Finish (args.Context, false, false, args.Time);
				return;
			}
				
			ProgressWindow pw = new ProgressWindow (this);
			pw.ReportFolder (dinfo.Name);
			
			Muine.DB.AddWatchedFolder (dinfo.FullName);
			HandleDirectory (dinfo, pw);
			
			pw.Done ();

			Drag.Finish (args.Context, true, false, args.Time);
		}

		/*
		public void CheckFirstStartUp () 
		{
			bool first_start = (bool) Muine.GetGConfValue (GConfKeyFirstStart, GConfDefaultFirstStart);

			if (!first_start)
				return;

			DirectoryInfo musicdir = new DirectoryInfo (Muine.MusicDirectory);
	  
			if (!musicdir.Exists) {
				NoMusicFoundWindow w = new NoMusicFoundWindow (this);

				Muine.SetGConfValue (GConfKeyFirstStart, false);
			} else { 
				// create a playlists directory if it still doesn't exists
				DirectoryInfo playlistsdir = new DirectoryInfo (Muine.PlaylistsDirectory);
				if (!playlistsdir.Exists)
					playlistsdir.Create ();

				ProgressWindow pw = new ProgressWindow (this, musicdir.Name);

				// seems to be that MusicDirectory does exists, but user hasn't started Muine before!
				Muine.DB.AddWatchedFolder (musicdir.FullName);

				// do this here, because the folder is watched now
				Muine.SetGConfValue (GConfKeyFirstStart, false);
		
				HandleDirectory (musicdir, pw);

				pw.Done ();
			}
		}*/
		
		private void SetupWindowSize ()
		{
			int width = (int) Config.Get (GConfKeyWidth, GConfDefaultWidth);
			int height = (int) Config.Get (GConfKeyHeight, GConfDefaultHeight);

			SetDefaultSize (width, height);

			SizeAllocated += new SizeAllocatedHandler (OnSizeAllocated);
		}

		private int last_x = -1;
		private int last_y = -1;

		private bool window_visible;
		public bool WindowVisible {
			set {
				window_visible = value;

				if (window_visible) {
					if (!Visible && last_x >= 0 && last_y >= 0)
						Move (last_x, last_y);

					Present ();
				} else {
					GetPosition (out last_x, out last_y);

					Visible = false;
				}

				UpdateWindowVisibilityUI ();
			}

			get { return window_visible; }
		}

		public void UpdateWindowVisibilityUI ()
		{
			if (WindowVisible) {
				if (playlist.Playing != IntPtr.Zero)
					playlist.Select (playlist.Playing);

				window_visibility_action.Label = Catalog.GetString ("Hide _Window");
			} else {
				window_visibility_action.Label = Catalog.GetString ("Show _Window");
			}
		}

		private void SetupMenus (Glade.XML glade_xml)
		{
			ActionEntry [] action_entries = new ActionEntry [] {
				new ActionEntry ("FileMenu", null, Catalog.GetString ("_File"),
						 null, null, null),
				new ActionEntry ("SongMenu", null, Catalog.GetString ("_Song"),
						 null, null, null),
				new ActionEntry ("PlaylistMenu", null, Catalog.GetString ("_Playlist"),
						 null, null, null),
				new ActionEntry ("HelpMenu", null, Catalog.GetString ("_Help"),
						 null, null, null),
				new ActionEntry ("ImportFolder", Stock.Execute, Catalog.GetString ("_Import Folder..."),
						 null, null,
						 new EventHandler (OnImportFolder)),
				new ActionEntry ("OpenPlaylist", Stock.Open, Catalog.GetString ("_Open Playlist..."),
						 "<control>O", null,
						 new EventHandler (OnOpenPlaylist)),
				new ActionEntry ("SavePlaylistAs", Stock.SaveAs, Catalog.GetString ("_Save Playlist As..."),
						 "<shift><control>S", null,
						 new EventHandler (OnSavePlaylistAs)),
				new ActionEntry ("ShowHideWindow", null, "",
						 "Escape", null,
						 new EventHandler (OnToggleWindowVisibility)),
				new ActionEntry ("Quit", Stock.Quit, null,
						 "<control>Q", null,
						 new EventHandler (OnQuit)),
				new ActionEntry ("PreviousSong", "stock_media-prev", Catalog.GetString ("_Previous"),
						 "P", null,
						 new EventHandler (OnPrevious)),
				new ActionEntry ("NextSong", "stock_media-next", Catalog.GetString ("_Next"),
						 "N", null,
						 new EventHandler (OnNext)),
				new ActionEntry ("SkipTo", Stock.JumpTo, Catalog.GetString ("_Skip to..."),
						 "T", null,
						 new EventHandler (OnSkipTo)),
				new ActionEntry ("SkipBackwards", "stock_media-rew", Catalog.GetString ("Skip _Backwards"),
						 "<control>Left", null,
						 new EventHandler (OnSkipBackwards)),
				new ActionEntry ("SkipForward", "stock_media-fwd",
						 Catalog.GetString ("Skip _Forward"), "<control>Right", null,
						 new EventHandler (OnSkipForward)),
				new ActionEntry ("PlaySong", Stock.Add, Catalog.GetString ("Play _Song..."),
						 "S", null,
						 new EventHandler (OnPlaySong)),
				new ActionEntry ("PlayAlbum", "gnome-dev-cdrom-audio", Catalog.GetString ("Play _Album..."),
						 "A", null,
						 new EventHandler (OnPlayAlbum)),
				new ActionEntry ("RemoveSong", Stock.Remove, Catalog.GetString ("_Remove Song"),
						 "Delete", null,
						 new EventHandler (OnRemoveSong)),
				new ActionEntry ("RemovePlayedSongs", null, Catalog.GetString ("Remove _Played Songs"),
						 "<control>Delete", null,
						 new EventHandler (OnRemovePlayedSongs)),
				new ActionEntry ("ClearPlaylist", Stock.Clear, Catalog.GetString ("_Clear"),
						 null, null,
						 new EventHandler (OnClearPlaylist)),
				new ActionEntry ("Shuffle", "stock_shuffle", Catalog.GetString ("Shu_ffle"),
						 "<control>S", null,
						 new EventHandler (OnShuffle)),
				new ActionEntry ("About", Gnome.Stock.About, Catalog.GetString ("_About"),
						 null, null,
						 new EventHandler (OnAbout))
			};

			ToggleActionEntry [] toggle_action_entries = new ToggleActionEntry [] {
				new ToggleActionEntry ("PlayPause", "stock_media-play", Catalog.GetString ("_Play"),
						       "space", null,
						       new EventHandler (OnPlayPause), false),
				new ToggleActionEntry ("Repeat", null, Catalog.GetString ("R_epeat"),
						       "<control>R", null,
						       new EventHandler (OnRepeat), false)
			};
		
			ActionGroup action_group = new ActionGroup ("Actions");
			action_group.Add (action_entries);
			action_group.Add (toggle_action_entries);

			previous_action          = action_group.GetAction ("PreviousSong");
			next_action              = action_group.GetAction ("NextSong");
			skip_to_action           = action_group.GetAction ("SkipTo");
			skip_forward_action      = action_group.GetAction ("SkipForward");
			skip_backwards_action    = action_group.GetAction ("SkipBackwards");
			remove_song_action       = action_group.GetAction ("RemoveSong");
			shuffle_action           = action_group.GetAction ("Shuffle");
			window_visibility_action = action_group.GetAction ("ShowHideWindow");
			play_pause_action        = (ToggleAction) action_group.GetAction ("PlayPause");
			repeat_action            = (ToggleAction) action_group.GetAction ("Repeat");
			
			ui_manager = new UIManager ();
			ui_manager.InsertActionGroup (action_group, 0);
			ui_manager.AddUiFromResource ("PlaylistWindow.xml");

			AddAccelGroup (ui_manager.AccelGroup);
			
			((Box) glade_xml ["menu_bar_box"]).Add (ui_manager.GetWidget ("/MenuBar"));

			block_repeat_action = true;
			repeat_action.Active = (bool) Config.Get (GConfKeyRepeat, GConfDefaultRepeat);
			block_repeat_action = false;
		}

		private void SetupButtons (Glade.XML glade_xml)
		{
			Image image;

			image = (Image) glade_xml ["play_pause_image"];
			image.SetFromStock ("stock_media-play", IconSize.LargeToolbar);
			image = (Image) glade_xml ["previous_image"];
			image.SetFromStock ("stock_media-prev", IconSize.LargeToolbar);
			image = (Image) glade_xml ["next_image"];
			image.SetFromStock ("stock_media-next", IconSize.LargeToolbar);
			image = (Image) glade_xml ["add_song_image"];
			image.SetFromStock (Stock.Add, IconSize.LargeToolbar);
			image = (Image) glade_xml ["add_album_image"];
			image.SetFromStock ("gnome-dev-cdrom-audio", IconSize.LargeToolbar);

			tooltips = new Tooltips ();
			tooltips.SetTip (play_pause_button,
					 Catalog.GetString ("Switch music playback on or off"), null);
			tooltips.SetTip (previous_button,
					 Catalog.GetString ("Play the previous song"), null);
			tooltips.SetTip (next_button,
					 Catalog.GetString ("Play the next song"), null);
			tooltips.SetTip (glade_xml ["add_album_button"],
					 Catalog.GetString ("Add an album to the playlist"), null);
			tooltips.SetTip (glade_xml ["add_song_button"],
					 Catalog.GetString ("Add a song to the playlist"), null);

			volume_button = new VolumeButton ();
			((Container) glade_xml ["volume_button_container"]).Add (volume_button);
			volume_button.Visible = true;
			volume_button.VolumeChanged += new VolumeButton.VolumeChangedHandler (OnVolumeChanged);

			tooltips.SetTip (volume_button,
					 Catalog.GetString ("Change the volume level"), null);

			int vol = (int) Config.Get (GConfKeyVolume, GConfDefaultVolume);

			volume_button.Volume = vol;
			player.Volume = vol;
		}

		private Gdk.Pixbuf empty_pixbuf;

		private CellRenderer pixbuf_renderer;
		private CellRenderer text_renderer;

		private void SetupPlaylist (Glade.XML glade_xml)
		{
			playlist = new HandleView ();

			playlist.Selection.Mode = SelectionMode.Multiple;

			pixbuf_renderer = new ColoredCellRendererPixbuf ();
			playlist.AddColumn (pixbuf_renderer, new HandleView.CellDataFunc (PixbufCellDataFunc), false);

			text_renderer = new CellRendererText ();
			playlist.AddColumn (text_renderer, new HandleView.CellDataFunc (TextCellDataFunc), true);

			playlist.RowActivated += new HandleView.RowActivatedHandler (OnPlaylistRowActivated);
			playlist.SelectionChanged += new HandleView.SelectionChangedHandler (OnPlaylistSelectionChanged);
			playlist.PlayingChanged += new HandleView.PlayingChangedHandler (OnPlaylistPlayingChanged);

			playlist.EnableModelDragSource (Gdk.ModifierType.Button1Mask,
							playlist_source_entries,
							Gdk.DragAction.Copy | Gdk.DragAction.Move);
			playlist.EnableModelDragDest (playlist_dest_entries,
						      Gdk.DragAction.Copy | Gdk.DragAction.Move);

			playlist.DragDataGet += new DragDataGetHandler (OnPlaylistDragDataGet);
			playlist.DragDataReceived += new DragDataReceivedHandler (OnPlaylistDragDataReceived);

			playlist.Show ();

			((Container) glade_xml ["scrolledwindow"]).Add (playlist);
			
			MarkupUtils.LabelSetMarkup (playlist_label, 0, StringUtils.GetByteLength (Catalog.GetString ("Playlist")),
						    false, true, false);

			empty_pixbuf = new Gdk.Pixbuf (null, "muine-nothing.png");
		}

		private void PixbufCellDataFunc (HandleView view, CellRenderer cell, IntPtr handle)
		{
			ColoredCellRendererPixbuf r = (ColoredCellRendererPixbuf) cell;

			if (handle == view.Playing) {
				if (player.Playing)
					r.Pixbuf = view.RenderIcon ("muine-playing", IconSize.Menu, null);
				else
					r.Pixbuf = view.RenderIcon ("muine-paused", IconSize.Menu, null);
			} else {
				r.Pixbuf = empty_pixbuf;
			}
		}

		private void TextCellDataFunc (HandleView view, CellRenderer cell, IntPtr handle)
		{
			Song song = Song.FromHandle (handle);
			CellRendererText r = (CellRendererText) cell;

			r.Text = song.Title + "\n" + StringUtils.JoinHumanReadable (song.Artists);

			MarkupUtils.CellSetMarkup (r, 0, StringUtils.GetByteLength (song.Title),
						   false, true, false);
		}

		private void SetupPlayer (Glade.XML glade_xml)
		{
			try {
				player = new Player ();
			} catch (Exception e) {
				throw new Exception (String.Format (Catalog.GetString ("Failed to initialize the audio backend:\n{0}\n\nExiting..."), e.Message));
			}

			player.EndOfStreamEvent += new Player.EndOfStreamEventHandler (OnEndOfStreamEvent);
			player.TickEvent += new Player.TickEventHandler (OnTickEvent);
			player.StateChanged += new Player.StateChangedHandler (OnStateChanged);

			title_label = new EllipsizingLabel ("");
			title_label.Visible = true;
			title_label.Xalign = 0.0f;
			title_label.Selectable = true;
			((Container) glade_xml ["title_label_container"]).Add (title_label);

			artist_label = new EllipsizingLabel ("");
			artist_label.Visible = true;
			artist_label.Xalign = 0.0f;
			artist_label.Selectable = true;
			((Container) glade_xml ["artist_label_container"]).Add (artist_label);

			cover_image = new CoverImage ();
			((Container) glade_xml ["cover_image_container"]).Add (cover_image);
			cover_image.ShowAll ();

			// playlist label dnd
			playlist_label_event_box.DragDataGet +=
				new DragDataGetHandler (OnPlaylistLabelDragDataGet);
				
			Gtk.Drag.SourceSet (playlist_label_event_box,
					    Gdk.ModifierType.Button1Mask,
					    drag_entries, Gdk.DragAction.Move);

			/* FIXME depends on Ximian Bugzilla #71060
			string icon = Gnome.Icon.Lookup (IconTheme.GetForScreen (this.Screen), null, null, null, null, "audio/x-mpegurl", Gnome.IconLookupFlags.None, null);	*/

			Gtk.Drag.SourceSetIconStock (playlist_label_event_box,
						     "gnome-mime-audio");
		}

		private void PlayAndSelect (IntPtr ptr)
		{
			playlist.Playing = ptr;
			playlist.Select (ptr);
		}

		private void PlayFirstAndSelect ()
		{
			playlist.First ();
			playlist.Select (playlist.Playing);
		}

		private void EnsurePlaying ()
		{
			if (playlist.Playing == IntPtr.Zero) {
				if (playlist.HasFirst)
					PlayFirstAndSelect ();
			} 
		}

		private IntPtr AddSong (Song song)
		{
			return AddSong (song.Handle);
		}

		private IntPtr AddSong (IntPtr p)
		{
			IntPtr ret = AddSongAtPos (p, IntPtr.Zero, TreeViewDropPosition.Before);

			if (had_last_eos)
				PlayAndSelect (ret);

			return ret;
		}

		private IntPtr AddSongAtPos (IntPtr p, IntPtr pos,
					     TreeViewDropPosition dp)
		{
			IntPtr new_p = p;

			if (playlist.Contains (p)) {
				Song song = Song.FromHandle (p);
				new_p = song.RegisterExtraHandle ();
			} 
		
			if (pos == IntPtr.Zero)
				playlist.Append (new_p);
			else
				playlist.Insert (new_p, pos, dp);

			return new_p;
		}

		private void RemoveSong (IntPtr p)
		{
			playlist.Remove (p);

			Song song = Song.FromHandle (p);

			if (song.IsExtraHandle (p))
				song.UnregisterExtraHandle (p);
		}

		private long remaining_songs_time;

		private void UpdateTimeLabels (int time)
		{
			if (playlist.Playing == IntPtr.Zero) {
				time_label.Text = "";
				playlist_label.Text = Catalog.GetString ("Playlist");

				return;
			}
			
			Song song = Song.FromHandle (playlist.Playing);

			String pos = StringUtils.SecondsToString (time);
			String total = StringUtils.SecondsToString (song.Duration);

			time_label.Text = pos + " / " + total;

			if (repeat_action.Active) {
				long r_seconds = remaining_songs_time;

				if (r_seconds > 6000) { /* 100 minutes */
					int hours = (int) Math.Floor ((double) r_seconds / 3600.0 + 0.5);
					playlist_label.Text = String.Format (Catalog.GetPluralString ("Playlist (Repeating {0} hour)", "Playlist (Repeating {0} hours)", hours), hours);
				} else if (r_seconds > 60) {
					int minutes = (int) Math.Floor ((double) r_seconds / 60.0 + 0.5);
					playlist_label.Text = String.Format (Catalog.GetPluralString ("Playlist (Repeating {0} minute)", "Playlist (Repeating {0} minutes)", minutes), minutes);
				} else if (r_seconds > 0) {
					playlist_label.Text = Catalog.GetString ("Playlist (Repeating)");
				} else {
					playlist_label.Text = Catalog.GetString ("Playlist");
				}
			} else {
				long r_seconds = remaining_songs_time + song.Duration - time;
				
				if (r_seconds > 6000) { /* 100 minutes */
					int hours = (int) Math.Floor ((double) r_seconds / 3600.0 + 0.5);
					playlist_label.Text = String.Format (Catalog.GetPluralString ("Playlist ({0} hour remaining)", "Playlist ({0} hours remaining)", hours), hours);
				} else if (r_seconds > 60) {
					int minutes = (int) Math.Floor ((double) r_seconds / 60.0 + 0.5);
					playlist_label.Text = String.Format (Catalog.GetPluralString ("Playlist ({0} minute remaining)", "Playlist ({0} minutes remaining)", minutes), minutes);
				} else if (r_seconds > 0) {
					playlist_label.Text = Catalog.GetString ("Playlist (Less than one minute remaining)");
				} else {
					playlist_label.Text = Catalog.GetString ("Playlist");
				}
			} 
		}

		private void PlaylistChanged ()
		{
			bool start_counting;
			remaining_songs_time = 0;

			if (repeat_action.Active)
				start_counting = true;
			else
				start_counting = false;

			foreach (int i in playlist.Contents) {
				IntPtr current = new IntPtr (i);

				if (start_counting) {
					Song song = Song.FromHandle (current);
					remaining_songs_time += song.Duration;
				}
					
				if (current == playlist.Playing)
					start_counting = true;
			}

			bool has_first = playlist.HasFirst;

			previous_button.Sensitive = has_first;
			play_pause_button.Sensitive = has_first;
			next_button.Sensitive = playlist.HasNext ||
						(repeat_action.Active && has_first);

			play_pause_action.Sensitive = previous_button.Sensitive;
			previous_action.Sensitive = play_pause_button.Sensitive;
			next_action.Sensitive = next_button.Sensitive;
			
			skip_to_action.Sensitive = has_first;
			skip_backwards_action.Sensitive = has_first;
			skip_forward_action.Sensitive = has_first;

			shuffle_action.Sensitive = has_first;

			UpdateTimeLabels (player.Position);

			SavePlaylist (FileUtils.PlaylistFile, !repeat_action.Active, true);

			if (PlaylistChangedEvent != null)
				PlaylistChangedEvent ();
		}

		private void SongChanged (bool restart)
		{
			Song song = null;

			if (playlist.Playing != IntPtr.Zero) {
				song = Song.FromHandle (playlist.Playing);

				cover_image.Song = song;

				string tip;
				if (song.Album.Length > 0)
					tip = String.Format (Catalog.GetString ("From \"{0}\""), song.Album);
				else
					tip = Catalog.GetString ("Album unknown");
				if (song.Performers.Length > 0)
					tip += "\n\n" + String.Format (Catalog.GetString ("Performed by {0}"), StringUtils.JoinHumanReadable (song.Performers));
					
				if (song.CoverImage == null && !Muine.CoverDB.Loading)
					tip += "\n\n" + Catalog.GetString ("Drop an image here to use it as album cover");
				
				tooltips.SetTip (cover_image, tip, null);

				title_label.Text = song.Title;

				artist_label.Text = StringUtils.JoinHumanReadable (song.Artists);

				if (player.Song != song || restart) {
					try {
						player.Song = song;
					} catch (Exception e) {
						/* quietly remove the song */
						Muine.DB.RemoveSong (song);

						return;
					}
				}

				Title = String.Format (Catalog.GetString ("{0} - Muine Music Player"), song.Title);
			} else {
				cover_image.Song = null;

				tooltips.SetTip (cover_image, null, null);

				title_label.Text = "";
				artist_label.Text = "";
				time_label.Text = "";

				Title = Catalog.GetString ("Muine Music Player");

				if (skip_to_window != null)
					skip_to_window.Hide ();
			}
			
			if (restart) {
				had_last_eos = false;

				if (SongChangedEvent != null)
					SongChangedEvent (song);
			}

			MarkupUtils.LabelSetMarkup (title_label, 0, StringUtils.GetByteLength (title_label.Text),
						    true, true, false);
		}

		private void SelectionChanged ()
		{
			remove_song_action.Sensitive = (playlist.SelectedPointers.Count > 0);

			if (SelectionChangedEvent != null)
				SelectionChangedEvent ();
		}

		private new void StateChanged (bool playing, bool dont_signal)
		{
			if (playing) {
				block_play_pause_action = true;
				play_pause_action.Active = true;
				play_pause_button.Active = true;
				block_play_pause_action = false;
			} else {
				block_play_pause_action = true;
				play_pause_action.Active = false;
				play_pause_button.Active = false;
				block_play_pause_action = false;
			}

			playlist.Changed (playlist.Playing);

			if (!dont_signal && StateChangedEvent != null)
				StateChangedEvent (playing);
		}

		private void ClearPlaylist ()
		{
			playlist.Clear ();

			player.Stop ();
		}

		private void SeekTo (int seconds)
		{
			Song song = Song.FromHandle (playlist.Playing);

			if (seconds >= song.Duration) {
				if (playlist.HasNext ||
				    (repeat_action.Active && playlist.HasFirst))
					Next ();
				else {
					player.Position = song.Duration;

					HadLastEos ();

					PlaylistChanged ();
				}
			} else {
				if (seconds < 0)
					player.Position = 0;
				else
					player.Position = seconds;

				player.Play ();
			}

			playlist.Select (playlist.Playing);
		}

		public void OpenPlaylist (string fn)
		{
			VfsStream stream;
			StreamReader reader;
			
			try {
				stream = new VfsStream (fn, System.IO.FileMode.Open);
				reader = new StreamReader (stream);
			} catch {
				new ErrorDialog (String.Format (Catalog.GetString ("Failed to open {0} for reading"), FileUtils.MakeHumanReadable (fn)), this);
				return;
			}

			ClearPlaylist ();

			string line = null;

			bool playing_song = false;

			while ((line = reader.ReadLine ()) != null) {
				if (line.Length == 0)
					continue;

				if (line[0] == '#') {
					if (line == "# PLAYING")
						playing_song = true;

					continue;
				}

				/* DOS-to-UNIX */
				line.Replace ('\\', '/');

				string basename = "";

				try {
					basename = System.IO.Path.GetFileName (line);
				} catch {
					continue;
				}

				Song song = Muine.DB.GetSong (line);
				if (song == null) {
					/* not found, lets see if we can find it anyway.. */
					foreach (string key in Muine.DB.Songs.Keys) {
						string key_basename = System.IO.Path.GetFileName (key);

						if (basename == key_basename) {
							song = Muine.DB.GetSong (key);
							break;
						}
					}
				}

				if (song == null)
					song = AddSongToDB (line);

				if (song != null) {
					IntPtr p = AddSong (song);

					if (playing_song) {
						PlayAndSelect (p);

						playing_song = false;
					}
				}
			}

			try {
				reader.Close ();
			} catch {
				new ErrorDialog (String.Format (Catalog.GetString ("Failed to close {0}"), FileUtils.MakeHumanReadable (fn)), this);
				return;
			}

			EnsurePlaying ();

			PlaylistChanged ();
		}

		private void SavePlaylist (string fn, bool exclude_played, bool store_playing)
		{
			VfsStream stream;
			StreamWriter writer;
			
			try {
				stream = new VfsStream (fn, System.IO.FileMode.Create);
				writer = new StreamWriter (stream);
			} catch {
				new ErrorDialog (String.Format (Catalog.GetString ("Failed to open {0} for writing"), FileUtils.MakeHumanReadable (fn)), this);
				return;
			}

			if (!(exclude_played && had_last_eos)) {
				bool had_playing_song = false;
				foreach (int i in playlist.Contents) {
					IntPtr ptr = new IntPtr (i);

					if (exclude_played) {
						if (ptr == playlist.Playing)
							had_playing_song = true;
						else if (!had_playing_song)
							continue;
					}
				
					if (store_playing &&
					    ptr == playlist.Playing) {
							writer.WriteLine ("# PLAYING");
					}
				
					Song song = Song.FromHandle (ptr);

					writer.WriteLine (song.Filename);
				}
			}

			try {
				writer.Close ();
			} catch {
				new ErrorDialog (String.Format (Catalog.GetString ("Failed to close {0}"), FileUtils.MakeHumanReadable (fn)), this);
				return;
			}
		}

		private void OnStateChanged (bool playing)
		{
			StateChanged (playing, false);
		}

		private void OnWindowStateEvent (object o, WindowStateEventArgs args)
		{
			if (!Visible)
				return;
				
			bool old_window_visible = window_visible;
			window_visible = ((args.Event.NewWindowState != Gdk.WindowState.Iconified) &&
					  (args.Event.NewWindowState != Gdk.WindowState.Withdrawn));

			if (old_window_visible != window_visible)
				UpdateWindowVisibilityUI ();
		}

		private void OnVisibilityNotifyEvent (object o, VisibilityNotifyEventArgs args)
		{
			if (!Visible ||
			    GdkWindow.State == Gdk.WindowState.Iconified ||
			    GdkWindow.State == Gdk.WindowState.Withdrawn)
			    return;

			bool old_window_visible = window_visible;
			window_visible = (args.Event.State != Gdk.VisibilityState.FullyObscured);

			if (old_window_visible != window_visible)
				UpdateWindowVisibilityUI ();

			args.RetVal = false;
		}

		private void OnDeleteEvent (object o, DeleteEventArgs args)
		{
			Quit ();
		}

		private void OnSizeAllocated (object o, SizeAllocatedArgs args)
		{
			int width, height;

			GetSize (out width, out height);

			Config.Set (GConfKeyWidth, width);
			Config.Set (GConfKeyHeight, height);
		}

		private void OnVolumeChanged (int vol)
		{
			player.Volume = vol;

			Config.Set (GConfKeyVolume, vol);
		}

		private void OnToggleWindowVisibility (object o, EventArgs args)
		{
			WindowVisible = !WindowVisible;
		}

		private void OnQueueSongsEvent (List songs)
		{
			foreach (int i in songs)
				AddSong (new IntPtr (i));

			EnsurePlaying ();

			PlaylistChanged ();
		}

		private Song AddSongToDB (string file)
		{
			Song song;
			
			try {
				song = new Song (file);
			} catch {
				return null;
			}

			Muine.DB.AddSong (song);

			return song;
		}

		private Song GetSingleSong (string file)
		{
			Song song = Muine.DB.GetSong (file);

			if (song == null)
				song = AddSongToDB (file);

			return song;
		}

		public void PlayFile (string file)
		{
			Song song = GetSingleSong (file);

			if (song == null)
				return;

			IntPtr p = AddSong (song);

			PlayAndSelect (p);

			player.Play ();

			PlaylistChanged ();
		}

		public void QueueFile (string file)
		{
			Song song = GetSingleSong (file);

			if (song == null)
				return;

			IntPtr p = AddSong (song);

			EnsurePlaying ();

			PlaylistChanged ();
		}
		
		private void OnPlaySongsEvent (List songs)
		{
			bool first = true;
			foreach (int i in songs) {
				IntPtr p = new IntPtr (i);
				
				IntPtr new_p = AddSong (p);
				
				if (!first)
					continue;

				PlayAndSelect (new_p);

				player.Play ();
			
				first = false;
			}

			PlaylistChanged ();
		}

		private void OnQueueAlbumsEvent (List albums)
		{
			foreach (int i in albums) {
				Album a = Album.FromHandle (new IntPtr (i));

				foreach (Song s in a.Songs)
					AddSong (s);
			}

			EnsurePlaying ();

			PlaylistChanged ();
		}

		private void OnPlayAlbumsEvent (List albums)
		{
			bool first = true;
			foreach (int i in albums) {
				Album a = Album.FromHandle (new IntPtr (i));

				foreach (Song s in a.Songs) {
					IntPtr new_p = AddSong (s);

					if (!first)
						continue;

					PlayAndSelect (new_p);

					player.Play ();

					first = false;
				}
			}

			PlaylistChanged ();
		}

		private void OnTickEvent (int pos)
		{
			UpdateTimeLabels (pos);
		}

		private void HadLastEos ()
		{
			had_last_eos = true;

			player.Stop ();
		}

		private void OnEndOfStreamEvent ()
		{
			Song song = Song.FromHandle (playlist.Playing);

			if (song.Duration != player.Position) {
				song.Duration = player.Position;

				Muine.DB.SaveSong (song);
			}
			
			if (playlist.HasNext)
				playlist.Next ();
			else
				if (repeat_action.Active)
					playlist.First ();
				else
					HadLastEos ();

			PlaylistChanged ();
		}

		public void Previous ()
		{
			if (!playlist.HasFirst)
				return;

			/* restart song if not in the first 3 seconds */
			if (player.Position < 3 &&
			    playlist.HasPrevious) {
				playlist.Previous ();

				PlaylistChanged ();
			} else if (player.Position < 3 &&
				   !playlist.HasPrevious &&
				   repeat_action.Active) {
				playlist.Last ();

				PlaylistChanged ();
			} else {
				player.Position = 0;
			}

			playlist.Select (playlist.Playing);

			player.Play ();
		}

		private void OnPrevious (object o, EventArgs args)
		{
			Previous ();
		}

		private void OnPlayPause (object o, EventArgs args)
		{
			if (block_play_pause_action)
				return;

			Playing = !Playing;
		}

		public void Next ()
		{
			if (playlist.HasNext)
				playlist.Next ();
			else if (repeat_action.Active && playlist.HasFirst)
				playlist.First ();
			else
				return;

			playlist.Select (playlist.Playing);

			PlaylistChanged ();

			player.Play ();
		}

		private void OnNext (object o, EventArgs args)
		{
			Next ();
		}

		private void OnSkipTo (object o, EventArgs args)
		{
			playlist.Select (playlist.Playing);

			if (skip_to_window == null)
				skip_to_window = new SkipToWindow (this, player);

			skip_to_window.Run ();
		}

		private void OnSkipBackwards (object o, EventArgs args)
		{
			SeekTo (player.Position - 5);
		}

		private void OnSkipForward (object o, EventArgs args)
		{
			SeekTo (player.Position + 5);
		}

	/*
		private void OnInformation (object o, EventArgs args)
		{
			//FIXME deal with selection
			Song song = Song.FromHandle (playlist.Playing);

			if (song.Album.Length == 0)
				return;
			Album album = (Album) Muine.DB.Albums [song.AlbumKey];
			
			InfoWindow id = new InfoWindow ("Information for " + song.Title);
			id.Load (album);
			
			id.Run ();
			
			AddChildWindowIfVisible (id);
		}*/

		public void PlaySong ()
		{
			if (add_song_window == null) {
				add_song_window = new AddSongWindow ();

				add_song_window.QueueEvent += new AddSongWindow.QueueEventHandler (OnQueueSongsEvent);
				add_song_window.PlayEvent  += new AddSongWindow.PlayEventHandler  (OnPlaySongsEvent );
			}

			add_song_window.Run ();
			
			AddChildWindowIfVisible (add_song_window);
		}

		private void OnPlaySong (object o, EventArgs args)
		{
			PlaySong ();
		}

		public void PlayAlbum ()
		{
			if (add_album_window == null) {
				add_album_window = new AddAlbumWindow ();
				
				add_album_window.QueueEvent += new AddAlbumWindow.QueueEventHandler (OnQueueAlbumsEvent);
				add_album_window.PlayEvent  += new AddAlbumWindow.PlayEventHandler  (OnPlayAlbumsEvent );
			}

			add_album_window.Run ();

			AddChildWindowIfVisible (add_album_window);
		}

		private void OnPlayAlbum (object o, EventArgs args)
		{
			PlayAlbum ();
		}

		private bool HandleDirectory (DirectoryInfo info,
					      ProgressWindow pw)
		{
			System.IO.FileInfo [] finfos;
			
			try {
				finfos = info.GetFiles ();
			} catch {
				return true;
			}
			
			foreach (System.IO.FileInfo finfo in finfos) {
				Song song;

				song = Muine.DB.GetSong (finfo.FullName);
				if (song == null) {
					bool ret = pw.ReportFile (finfo.Name);
					if (!ret)
						return false;

					AddSongToDB (finfo.FullName);
				}
			}

			DirectoryInfo [] dinfos;
			
			try {
				dinfos = info.GetDirectories ();
			} catch {
				return true;
			}

			foreach (DirectoryInfo dinfo in dinfos) {
				bool ret = HandleDirectory (dinfo, pw);
				if (!ret)
					return false;
			}

			return true;
		}

		private void OnImportFolder (object o, EventArgs args) 
		{
			FileChooserDialog fc;

			fc = new FileChooserDialog (Catalog.GetString ("Import Folder"), this,
						    FileChooserAction.SelectFolder);
			fc.LocalOnly = true;
			fc.AddButton (Stock.Cancel, ResponseType.Cancel);
			fc.AddButton (Catalog.GetString ("_Import"), ResponseType.Ok);
			fc.DefaultResponse = ResponseType.Ok;
			
			string start_dir = (string) Config.Get (GConfKeyImportFolder, GConfDefaultImportFolder);

			start_dir = start_dir.Replace ("~", FileUtils.HomeDirectory);

			fc.SetCurrentFolder (start_dir);

			if (fc.Run () != (int) ResponseType.Ok) {
				fc.Destroy ();

				return;
			}

			fc.Visible = false;

			string res = FileUtils.LocalPathFromUri (fc.Uri);

			Config.Set (GConfKeyImportFolder, res);

			DirectoryInfo dinfo = new DirectoryInfo (res);
				
			if (dinfo.Exists) {
				ProgressWindow pw = new ProgressWindow (this);
				pw.ReportFolder (dinfo.Name);

				Muine.DB.AddWatchedFolder (dinfo.FullName);
				HandleDirectory (dinfo, pw);

				pw.Done ();
			}

			fc.Destroy ();
		}

		private void OnOpenPlaylist (object o, EventArgs args)
		{
			FileSelector sel = new FileSelector (Catalog.GetString ("Open Playlist"),
							     this, FileChooserAction.Open,
							     "/apps/muine/default_playlist_folder");

			FileFilter filter = new FileFilter ();
			filter.Name = Catalog.GetString ("Playlist files");
			filter.AddMimeType ("audio/x-mpegurl");
			filter.AddPattern ("*.m3u");
			sel.AddFilter (filter);

			string fn = sel.GetFile ();

			if (fn.Length == 0 || !FileUtils.IsPlaylist (fn))
				return;

			if (FileUtils.Exists (fn))
				OpenPlaylist (fn);
		}

		private void OnSavePlaylistAs (object o, EventArgs args)
		{
			FileSelector sel = new FileSelector (Catalog.GetString ("Save Playlist"),
							     this, FileChooserAction.Save,
							     "/apps/muine/default_playlist_folder");
			sel.CurrentName = Catalog.GetString ("Untitled");

			string fn = sel.GetFile ();

			if (fn.Length == 0)
				return;

			/* make sure the extension is ".m3u" */
			if (!FileUtils.IsPlaylist (fn))
				fn += ".m3u";

			if (FileUtils.Exists (fn)) {
				YesNoDialog d = new YesNoDialog (String.Format (Catalog.GetString ("File {0} will be overwritten.\nIf you choose yes, the contents will be lost.\n\nDo you want to continue?"), FileUtils.MakeHumanReadable (fn)), this);
				if (d.GetAnswer ())
					SavePlaylist (fn, false, false);
			} else
				SavePlaylist (fn, false, false);
		}

		private void OnRemoveSong (object o, EventArgs args)
		{
			List selected_pointers = playlist.SelectedPointers;

			int counter = 0, selected_pointers_count = selected_pointers.Count;
			bool song_changed = false;

			ignore_song_change = true; // Hack to improve performance-
						   // only load new song once

			foreach (int i in selected_pointers) {
				IntPtr sel = new IntPtr (i);

				if (sel == playlist.Playing) {
					OnPlayingSongRemoved ();
					
					song_changed = true;
				}
				
				if (counter == selected_pointers_count - 1) {
					if (!playlist.SelectNext ())
						playlist.SelectPrevious ();
				}

				RemoveSong (sel);

				counter ++;
			}

			ignore_song_change = false;

			if (song_changed)
				SongChanged (true);

			PlaylistChanged ();
		}

		private void OnRemovePlayedSongs (object o, EventArgs args)
		{
			if (playlist.Playing == IntPtr.Zero)
				return;

			if (had_last_eos) {
				ClearPlaylist ();
				PlaylistChanged ();
				return;
			}

			foreach (int i in playlist.Contents) {
				IntPtr current = new IntPtr (i);

				if (current == playlist.Playing)
					break;

				RemoveSong (current);
			}

			playlist.Select (playlist.Playing);

			PlaylistChanged ();
		}

		private void OnClearPlaylist (object o, EventArgs args)
		{
			ClearPlaylist ();
			PlaylistChanged ();
		}

		private void OnRepeat (object o, EventArgs args)
		{
			if (block_repeat_action)
				return;

			Config.Set (GConfKeyRepeat, repeat_action.Active);

			PlaylistChanged ();
		}

		private Hashtable random_sort_keys;

		private int ShuffleFunc (IntPtr ap, IntPtr bp)
		{
			double a = (double) random_sort_keys [(int) ap];
			double b = (double) random_sort_keys [(int) bp];

			if (a > b)
				return 1;
			else if (a < b)
				return -1;
			else
				return 0;
		}

		private void OnShuffle (object o, EventArgs args)
		{
			Random rand = new Random ();

			random_sort_keys = new Hashtable ();

			foreach (int i in playlist.Contents) {
				Song song = Song.FromHandle ((IntPtr) i);

				if (i == (int) playlist.Playing)
					random_sort_keys.Add (i, -1.0);
				else
					random_sort_keys.Add (i, rand.NextDouble ());
			}

			playlist.Sort (new HandleView.CompareFunc (ShuffleFunc));

			random_sort_keys = null;

			PlaylistChanged ();

			if (playlist.Playing != IntPtr.Zero)
				playlist.Select (playlist.Playing);
		}

		private void OnPlaylistRowActivated (IntPtr handle)
		{
			playlist.Playing = handle;

			PlaylistChanged ();

			player.Play ();
		}

		private void OnPlaylistSelectionChanged ()
		{
			SelectionChanged ();
		}

		private void OnPlaylistPlayingChanged (IntPtr playing)
		{
			if (!ignore_song_change)
				SongChanged (true);
		}

		public void Quit ()
		{
			Muine.Exit ();
		}

		private void OnQuit (object o, EventArgs args)
		{
			Quit ();
		}

		private void OnAbout (object o, EventArgs args)
		{
			About.ShowWindow (this);
		}

		private void OnDoneCheckingChanges ()
		{
			checking_changes_progress_window.Done ();
			checking_changes_progress_window = null;
		}

		private void OnSongAdded (Song song)
		{
			if (!Muine.DB.CheckingChanges)
				return;

			string basename = System.IO.Path.GetFileName (song.Filename);
			string folder = System.IO.Path.GetDirectoryName (song.Filename);

			DirectoryInfo dinfo = new DirectoryInfo (folder);
			string folder_basename = dinfo.Name;

			checking_changes_progress_window.ReportFolder (folder_basename);
			if (!checking_changes_progress_window.ReportFile (basename))
				Muine.DB.CheckingChanges = false;
		}

		private void OnSongChanged (Song song)
		{
			bool song_changed = false;
			
			foreach (IntPtr h in song.Handles) {
				if (!playlist.Contains (h))
					continue;

				song_changed = true;
				
				if (h == playlist.Playing)
					SongChanged (false);

				playlist.Changed (h);
			}
			
			if (song_changed)
				PlaylistChanged ();
		}

		private void OnPlayingSongRemoved ()
		{
			if (playlist.HasNext)
				playlist.Next ();
			else if (playlist.HasPrevious)
				playlist.Previous ();
			else {
				// playlist is empty now
				playlist.Playing = IntPtr.Zero;

				player.Stop ();
			}
		}

		private void OnSongRemoved (Song song)
		{
			bool n_songs_changed = false;
			
			foreach (IntPtr h in song.Handles) {
				if (!playlist.Contains (h))
					continue;

				n_songs_changed = true;
				
				if (h == playlist.Playing)
					OnPlayingSongRemoved ();

				if ((playlist.SelectedPointers.Count == 1) &&
				    ((int) playlist.SelectedPointers [0] == (int) h)) {
					if (!playlist.SelectNext ())
						playlist.SelectPrevious ();
				}

				playlist.Remove (h);
			}
			
			if (n_songs_changed)
				PlaylistChanged ();
		}

		public void AddChildWindowIfVisible (Window window)
		{
			if (WindowVisible)
				window.TransientFor = this;
			else
				window.TransientFor = null;
		}

		private void OnPlaylistDragDataGet (object o, DragDataGetArgs args)
		{
			List songs = playlist.SelectedPointers;

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

			case (uint) DndUtils.TargetType.ModelRow:
				string ptrs = String.Format ("\t{0}\t", DndUtils.TargetMuineTreeModelRow.Target);

				foreach (int p in songs) {
					IntPtr s = new IntPtr (p);
					ptrs += s.ToString () + "\r\n";
				}
				
				args.SelectionData.Set (Gdk.Atom.Intern (DndUtils.TargetMuineTreeModelRow.Target, false),
							8, System.Text.Encoding.ASCII.GetBytes (ptrs));
						
				break;

			default:
				break;	
			}
		}

		private class DragAddSongPosition {
			private IntPtr pointer;
			public IntPtr Pointer {
				set { pointer = value; }

				get { return pointer; }
			}

			private TreeViewDropPosition position;
			public TreeViewDropPosition Position {
				set { position = value; }

				get { return position; }
			}

			private bool first;
			public bool First {
				set {
					first = value;
				}

				get {
					return first;
				}
			}
		}

		private IntPtr DragAddSong (Song song, DragAddSongPosition pos)
		{
			if (pos.Pointer != IntPtr.Zero)
				pos.Pointer = AddSongAtPos (song.Handle, pos.Pointer, pos.Position);
			else
				pos.Pointer = AddSong (song.Handle);

			pos.Position = TreeViewDropPosition.After;
				
			if (pos.First) {
				playlist.Select (pos.Pointer, false);

				pos.First = false;
			}

			return pos.Pointer;
		}

		private void OnPlaylistDragDataReceived (object o, DragDataReceivedArgs args)
		{
			string data = DndUtils.SelectionDataToString (args.SelectionData);
			TreePath path;
			TreeViewDropPosition tmp_pos;

			DragAddSongPosition pos = new DragAddSongPosition ();
			pos.First = true;

			if (playlist.GetDestRowAtPos (args.X, args.Y, out path, out tmp_pos)) {
				pos.Pointer = playlist.GetHandleFromPath (path);
				pos.Position = tmp_pos;
			}

			uint type = (uint) DndUtils.TargetType.UriList;

			/* work around gtk bug .. */
			string tree_model_row = String.Format ("\t{0}\t", DndUtils.TargetMuineTreeModelRow.Target);
			string song_list      = String.Format ("\t{0}\t", DndUtils.TargetMuineSongList.Target);
			string album_list     = String.Format ("\t{0}\t", DndUtils.TargetMuineAlbumList.Target);
			
			if (data.StartsWith (tree_model_row)) {
				type = (uint) DndUtils.TargetType.ModelRow;
				data = data.Substring (tree_model_row.Length);
				
			} else if (data.StartsWith (song_list)) {
				type = (uint) DndUtils.TargetType.SongList;
				data = data.Substring (song_list.Length);
				
			} else if (data.StartsWith (album_list)) {
				type = (uint) DndUtils.TargetType.AlbumList;
				data = data.Substring (album_list.Length);
			}

			string [] bits = DndUtils.SplitSelectionData (data);

			switch (type) {
			case (uint) DndUtils.TargetType.SongList:
			case (uint) DndUtils.TargetType.ModelRow:
				foreach (string s in bits) {
					IntPtr ptr;

					try { 
						ptr = new IntPtr (Int64.Parse (s)); 
					} catch { 	
						continue;
					}

					Song song = Song.FromHandle (ptr);

					bool play = false;

					if (type == (uint) DndUtils.TargetType.ModelRow) {
						// Reorder part 1: remove old row
						if (ptr == pos.Pointer)
							break;

						if (ptr == playlist.Playing) {
							play = true;

							ignore_song_change = true;
						}
						
						RemoveSong (ptr);
					}

					ptr = DragAddSong (song, pos);
						
					if (play) {
						// Reorder part 2: if the row was playing, keep it playing
						playlist.Playing = ptr;
						
						ignore_song_change = false;
					}
				}

				EnsurePlaying ();

				PlaylistChanged ();

				break;
				
			case (uint) DndUtils.TargetType.AlbumList:
				foreach (string s in bits) {
					IntPtr ptr;
					
					try {
						ptr = new IntPtr (Int64.Parse (s));
					} catch {
						continue;
					}
					
					Album album = Album.FromHandle (ptr);
					
					foreach (Song song in album.Songs)
						DragAddSong (song, pos);
				}
				
				EnsurePlaying ();

				PlaylistChanged ();

				break;

			case (uint) DndUtils.TargetType.UriList:
				foreach (string s in bits) {
					string fn = FileUtils.LocalPathFromUri (s);

					if (fn == null)
						break;
		
					DirectoryInfo dinfo = new DirectoryInfo (fn);
					
					if (dinfo.Exists) {
						ProgressWindow pw = new ProgressWindow (this);
						pw.ReportFolder (dinfo.Name);
			
						Muine.DB.AddWatchedFolder (dinfo.FullName);
						HandleDirectory	(dinfo, pw);
			
						pw.Done ();
					} else {
						System.IO.FileInfo finfo = new System.IO.FileInfo (fn);
						
						if (!finfo.Exists) {
							Drag.Finish (args.Context, false, false, args.Time);

							return;
						}	
						
						if (FileUtils.IsPlaylist (fn)) {
							OpenPlaylist (fn);

							pos.First = false;
						} else {
							Song song = GetSingleSong (finfo.FullName);
						
							if (song != null)
								DragAddSong (song, pos);
						}

						EnsurePlaying ();

						PlaylistChanged ();
					}
				}

				break;
			default:
				break;
			}

			Drag.Finish (args.Context, true, false, args.Time);
		}
	}
}
