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

using Muine.PluginLib;

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

		// Strings
		private static readonly string string_playlist_filename =
			Catalog.GetString ("Playlist.m3u");
		private static readonly string string_playlist = 
			Catalog.GetString ("Playlist");
		private static readonly string string_playlist_repeating =
			Catalog.GetString ("Playlist (Repeating)");
		private static readonly string string_playlist_under_minute =
			Catalog.GetString ("Playlist (Less than one minute remaining)");
		private static readonly string string_artists =
			Catalog.GetString ("From \"{0}\"");
		private static readonly string string_album_unknown =
			Catalog.GetString ("Album unknown");
		private static readonly string string_performers =
			Catalog.GetString ("Performed by {0}");
		private static readonly string string_program = 
			Catalog.GetString ("Muine Music Player");
		private static readonly string string_open_filter =
			Catalog.GetString ("Playlist files");
		private static readonly string string_save_default =
			Catalog.GetString ("Untitled");
		private static readonly string string_overwrite =
			Catalog.GetString ("File {0} will be overwritten.\n" +
					   "If you choose yes, the contents will be lost.\n\n" +
					   "Do you want to continue?");

		private static readonly string string_title_main =
			Catalog.GetString ("{0} - Muine Music Player");
		private static readonly string string_title_import =
			Catalog.GetString ("Import Folder");
		private static readonly string string_title_open =
			Catalog.GetString ("Open Playlist");
		private static readonly string string_title_save =
			Catalog.GetString ("Save Playlist");

		private static readonly string string_button_import =
			Catalog.GetString ("_Import");

		private static readonly string string_tooltip_play_pause =
			Catalog.GetString ("Switch music playback on or off");
		private static readonly string string_tooltip_previous =
			Catalog.GetString ("Play the previous song");
		private static readonly string string_tooltip_next =
			Catalog.GetString ("Play the next song");
		private static readonly string string_tooltip_add_album =
			Catalog.GetString ("Add an album to the playlist");
		private static readonly string string_tooltip_add_song =
			Catalog.GetString ("Add a song to the playlist");
		private static readonly string string_tooltip_volume =
			Catalog.GetString ("Change the volume level");
		private static readonly string string_tooltip_cover =
			Catalog.GetString ("Drop an image here to use it as album cover");

		private static readonly string string_error_audio =
			Catalog.GetString ("Failed to initialize the audio backend:\n{0}\n\nExiting...");
		private static readonly string string_error_read =
			Catalog.GetString ("Failed to open {0} for reading");
		private static readonly string string_error_close =
			Catalog.GetString ("Failed to close {0}");
		private static readonly string string_error_write =
			Catalog.GetString ("Failed to open {0} for writing");

		/* menu widgets */
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

		private bool setting_volume = false;
		public int Volume {
			set {
				if (value > 100 || value < 0)
					value = GConfDefaultVolume;

				setting_volume = true;

				volume_button.Volume = value;
				player.Volume = value;

				Config.Set (GConfKeyVolume, value);
				
				setting_volume = false;
			}
			
			get { return player.Volume; }
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

		public UIManager UIManager {
			get { return Global.Actions.UIManager; }
		}

		public Window Window {
			get { return this; }
		}

		private uint busy_level = 0;
		public uint BusyLevel {
			set {
				if (busy_level == 0 && value > 0) {
					this.Realize ();
					this.GdkWindow.Cursor =
						new Gdk.Cursor (Gdk.CursorType.Watch);
					this.GdkWindow.Display.Flush ();
				} else if (busy_level > 0 && value == 0)
					this.GdkWindow.Cursor = null;
				
				busy_level = value;
			}

			get { return busy_level; }
		}

		public event SongChangedEventHandler SongChangedEvent;

		public event StateChangedEventHandler StateChangedEvent;

		public event GenericEventHandler PlaylistChangedEvent;

		public event GenericEventHandler SelectionChangedEvent;

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
			SetupPlayer (glade_xml); /* Has to be before the others,
						    they need the Player object */
			SetupMenus (glade_xml);
			SetupButtons (glade_xml);
			SetupPlaylist (glade_xml);

			/* connect to song database signals */
			Global.DB.SongChanged += new SongDatabase.SongChangedHandler (OnSongChanged);
			Global.DB.SongRemoved += new SongDatabase.SongRemovedHandler (OnSongRemoved);

			/* make sure the interface is up to date */
			SelectionChanged ();
			StateChanged (false, true);
		}

		public void RestorePlaylist ()
		{
			/* load last playlist */
			System.IO.FileInfo finfo = new System.IO.FileInfo (FileUtils.PlaylistFile);
			if (finfo.Exists)
				OpenPlaylistInternal (FileUtils.PlaylistFile);
		}

		public void Run ()
		{
			if (!playlist.HasFirst)
				SongChanged (true); /* make sure the UI is up to date */

			RestoreState ();

			WindowVisible = true;
		}

		private void OnPlaylistLabelDragDataGet (object o, DragDataGetArgs args)
		{
			switch (args.Info) {
			case (uint) DndUtils.TargetType.UriList:
				string file = System.IO.Path.Combine (FileUtils.TempDirectory, string_playlist_filename);

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
			if (args.Info != (uint) DndUtils.TargetType.UriList) {
				Drag.Finish (args.Context, false, false, args.Time);
				return;
			}

			string [] bits = DndUtils.SplitSelectionData (args.SelectionData);

			ArrayList new_dinfos = new ArrayList ();

			bool success = false;

			foreach (string s in bits) {
				string fn = FileUtils.LocalPathFromUri (s);

				if (fn == null)
					continue;
		
				DirectoryInfo dinfo = new DirectoryInfo (fn);
					
				if (dinfo.Exists)
					new_dinfos.Add (dinfo);
				else {
					System.IO.FileInfo finfo = new System.IO.FileInfo (fn);
						
					if (!finfo.Exists)
						continue;
						
					if (!FileUtils.IsPlaylist (fn))
						continue;

					OpenPlaylist (fn);

					success = true;
				}
			}

			if (new_dinfos.Count > 0) {
				Global.DB.AddFolders (new_dinfos);

				success = true;
			}

			Drag.Finish (args.Context, success, false, args.Time);
		}

		private void RestoreState ()
		{
			// Window size
			int width = (int) Config.Get (GConfKeyWidth, GConfDefaultWidth);
			int height = (int) Config.Get (GConfKeyHeight, GConfDefaultHeight);

			SetDefaultSize (width, height);

			SizeAllocated += new SizeAllocatedHandler (OnSizeAllocated);

			// Volume
			Volume = (int) Config.Get (GConfKeyVolume, GConfDefaultVolume);
			Config.AddNotify (GConfKeyVolume,
					  new GConf.NotifyEventHandler (OnConfigVolumeChanged));

			// Repeat
			Repeat = (bool) Config.Get (GConfKeyRepeat, GConfDefaultRepeat);
			Config.AddNotify (GConfKeyRepeat,
					  new GConf.NotifyEventHandler (OnConfigRepeatChanged));
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

				Global.Actions.Visibility.Label = Actions.StringHideWindow;
			} else {
				Global.Actions.Visibility.Label = Actions.StringShowWindow;
			}
		}

		private void SetupMenus (Glade.XML glade_xml)
		{
			Global.Actions.Import.Activated += new EventHandler (OnImportFolder);
			Global.Actions.Open.Activated += new EventHandler (OnOpenPlaylist);
			Global.Actions.Save.Activated += new EventHandler (OnSavePlaylistAs);
			Global.Actions.Visibility.Activated += new EventHandler (OnToggleWindowVisibility);
			Global.Actions.Quit.Activated += new EventHandler (OnQuit);
			Global.Actions.Previous.Activated += new EventHandler (OnPrevious);
			Global.Actions.Next.Activated += new EventHandler (OnNext);
			Global.Actions.SkipTo.Activated += new EventHandler (OnSkipTo);
			Global.Actions.SkipBackwards.Activated += new EventHandler (OnSkipBackwards);
			Global.Actions.SkipForward.Activated += new EventHandler (OnSkipForward);
			Global.Actions.PlaySong.Activated += new EventHandler (OnPlaySong);
			Global.Actions.PlayAlbum.Activated += new EventHandler (OnPlayAlbum);
			Global.Actions.Remove.Activated += new EventHandler (OnRemoveSong);
			Global.Actions.RemovePlayed.Activated += new EventHandler (OnRemovePlayedSongs);
			Global.Actions.Clear.Activated += new EventHandler (OnClearPlaylist);
			Global.Actions.Shuffle.Activated += new EventHandler (OnShuffle);
			Global.Actions.About.Activated += new EventHandler (OnAbout);
			
			Global.Actions.PlayPause.Activated += new EventHandler (OnPlayPause);
			Global.Actions.Repeat.Activated += new EventHandler (OnRepeat);

			base.AddAccelGroup (Global.Actions.UIManager.AccelGroup);
			((Box) glade_xml ["menu_bar_box"]).Add (Global.Actions.MenuBar);
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
			tooltips.SetTip (play_pause_button, string_tooltip_play_pause, null);
			tooltips.SetTip (previous_button  , string_tooltip_previous  , null);
			tooltips.SetTip (next_button      , string_tooltip_next      , null);
			tooltips.SetTip (glade_xml ["add_album_button"], string_tooltip_add_album, null);
			tooltips.SetTip (glade_xml ["add_song_button"] , string_tooltip_add_song , null);

			volume_button = new VolumeButton ();
			((Container) glade_xml ["volume_button_container"]).Add (volume_button);
			volume_button.Visible = true;
			volume_button.VolumeChanged += new VolumeButton.VolumeChangedHandler (OnVolumeChanged);

			tooltips.SetTip (volume_button, string_tooltip_volume, null);
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
			
			MarkupUtils.LabelSetMarkup (playlist_label, 0, StringUtils.GetByteLength (string_playlist),
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
				throw new Exception (String.Format (string_error_audio, e.Message));
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
			playlist.Select (playlist.Playing);
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
				playlist_label.Text = string_playlist;

				return;
			}
			
			Song song = Song.FromHandle (playlist.Playing);

			String pos = StringUtils.SecondsToString (time);
			String total = StringUtils.SecondsToString (song.Duration);

			time_label.Text = pos + " / " + total;

			if (Repeat) {
				long r_seconds = remaining_songs_time;

				if (r_seconds > 6000) { /* 100 minutes */
					int hours = (int) Math.Floor ((double) r_seconds / 3600.0 + 0.5);
					playlist_label.Text = String.Format (Catalog.GetPluralString ("Playlist (Repeating {0} hour)", "Playlist (Repeating {0} hours)", hours), hours);
				} else if (r_seconds > 60) {
					int minutes = (int) Math.Floor ((double) r_seconds / 60.0 + 0.5);
					playlist_label.Text = String.Format (Catalog.GetPluralString ("Playlist (Repeating {0} minute)", "Playlist (Repeating {0} minutes)", minutes), minutes);
				} else if (r_seconds > 0) {
					playlist_label.Text = string_playlist_repeating;
				} else {
					playlist_label.Text = string_playlist;
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
					playlist_label.Text = string_playlist_under_minute;
				} else {
					playlist_label.Text = string_playlist;
				}
			} 
		}

		private void PlaylistChanged ()
		{
			bool start_counting;
			remaining_songs_time = 0;

			if (Repeat)
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
						(Repeat && has_first);

			Global.Actions.PlayPause.Sensitive = previous_button.Sensitive;
			Global.Actions.Previous.Sensitive = play_pause_button.Sensitive;
			Global.Actions.Next.Sensitive = next_button.Sensitive;
			
			Global.Actions.SkipTo.Sensitive = has_first;
			Global.Actions.SkipBackwards.Sensitive = has_first;
			Global.Actions.SkipForward.Sensitive = has_first;

			Global.Actions.Shuffle.Sensitive = has_first;

			UpdateTimeLabels (player.Position);

			SavePlaylist (FileUtils.PlaylistFile, !Repeat, true);

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
					tip = String.Format (string_artists, song.Album);
				else
					tip = string_album_unknown;

				if (song.Performers.Length > 0)
					tip += "\n\n" + String.Format (string_performers, StringUtils.JoinHumanReadable (song.Performers));
					
				if (song.CoverImage == null && !Global.CoverDB.Loading)
					tip += "\n\n" + string_tooltip_cover;
				
				tooltips.SetTip (cover_image, tip, null);

				title_label.Text = song.Title;

				artist_label.Text = StringUtils.JoinHumanReadable (song.Artists);

				if (player.Song != song || restart) {
					try {
						player.Song = song;
					} catch (PlayerException e) {
						// quietly remove the song
						// from an idle, not to interfere with song change routines
						InvalidSong ivs = new InvalidSong (song);
						Idle.Add (new IdleHandler (ivs.Handle));

						return;
					}
				}

				Title = String.Format (string_title_main, song.Title);
			} else {
				cover_image.Song = null;

				tooltips.SetTip (cover_image, null, null);

				title_label.Text = "";
				artist_label.Text = "";
				time_label.Text = "";

				Title = string_program;

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

		private class InvalidSong
		{
			private Song song;
			
			public InvalidSong (Song song)
			{
				this.song = song;
			}

			public bool Handle ()
			{
				Global.DB.RemoveSong (song);

				return false;
			}
		}

		private void SelectionChanged ()
		{
			Global.Actions.Remove.Sensitive = (playlist.SelectedPointers.Count > 0);

			if (SelectionChangedEvent != null)
				SelectionChangedEvent ();
		}

		private new void StateChanged (bool playing, bool dont_signal)
		{
			if (playing) {
				block_play_pause_action = true;
				Global.Actions.PlayPause.Active = true;
				play_pause_button.Active = true;
				block_play_pause_action = false;
			} else {
				block_play_pause_action = true;
				Global.Actions.PlayPause.Active = false;
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

			if (seconds >= song.Duration)
				HandleEndOfStream (song, true);
			else {
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
			BusyLevel ++;
			
			OpenPlaylistInternal (fn);

			PlaylistChanged ();

			Playing = true;

			BusyLevel --;
		}

		private void OpenPlaylistInternal (string fn)
		{
			VfsStream stream;
			StreamReader reader;

			try {
				stream = new VfsStream (fn, System.IO.FileMode.Open);
				reader = new StreamReader (stream);
			} catch {
				new ErrorDialog (String.Format (string_error_read, FileUtils.MakeHumanReadable (fn)), this);
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

				Song song = Global.DB.GetSong (line);
				if (song == null) {
					/* not found, lets see if we can find it anyway.. */
					lock (Global.DB) {
						foreach (string key in Global.DB.Songs.Keys) {
							string key_basename = System.IO.Path.GetFileName (key);

							if (basename == key_basename) {
								song = Global.DB.GetSong (key);
								break;
							}
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
				new ErrorDialog (String.Format (string_error_close, FileUtils.MakeHumanReadable (fn)), this);
				return;
			}

			EnsurePlaying ();
		}

		private void SavePlaylist (string fn, bool exclude_played, bool store_playing)
		{
			VfsStream stream;
			StreamWriter writer;

			bool remote = FileUtils.IsRemote (fn);

			if (remote)
				BusyLevel ++;
			
			try {
				stream = new VfsStream (fn, System.IO.FileMode.Create);
				writer = new StreamWriter (stream);
			} catch {
				new ErrorDialog (String.Format (string_error_write, FileUtils.MakeHumanReadable (fn)), this);
				if (remote)
					BusyLevel --;
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
				new ErrorDialog (String.Format (string_error_close, FileUtils.MakeHumanReadable (fn)), this);
			}

			if (remote)
				BusyLevel --;
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
			if (setting_volume)
				return;

			Volume = vol;
		}

		private void OnConfigVolumeChanged (object o, GConf.NotifyEventArgs args)
		{
			int vol = (int) args.Value;

			if (vol != Volume)
				Volume = (int) args.Value;
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

			BusyLevel ++;
			
			try {
				song = new Song (file);
			} catch {
				BusyLevel --;
				return null;
			}

			Global.DB.AddSong (song);

			BusyLevel --;

			return song;
		}

		private Song GetSingleSong (string file)
		{
			Song song = Global.DB.GetSong (file);

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

		private void HandleEndOfStream (Song song, bool update_time)
		{
			if (playlist.HasNext)
				playlist.Next ();
			else {
				if (Repeat)
					playlist.First ();
				else {
					if (update_time)
						player.Position = song.Duration;

					had_last_eos = true;

					player.Stop ();
				}
			}

			PlaylistChanged ();
		}

		private void OnEndOfStreamEvent ()
		{
			Song song = Song.FromHandle (playlist.Playing);

			if (song.Duration != player.Position) {
				song.Duration = player.Position;

				Global.DB.SaveSong (song);
			}
			
			HandleEndOfStream (song, false);
		}

		public void Previous ()
		{
			if (!playlist.HasFirst)
				return;

			/* restart song if not in the first 3 seconds */
			if (player.Position < 3) {
				if (playlist.HasPrevious) {
					playlist.Previous ();

					PlaylistChanged ();
				} else if (Repeat) {
					playlist.Last ();

					PlaylistChanged ();
				} else {
					player.Position = 0;
				}
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
			else if (Repeat && playlist.HasFirst)
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

		private void OnImportFolder (object o, EventArgs args) 
		{
			FileChooserDialog fc;

			fc = new FileChooserDialog (string_title_import, this,
						    FileChooserAction.SelectFolder);
			fc.LocalOnly = true;
			fc.SelectMultiple = true;
			fc.AddButton (Stock.Cancel, ResponseType.Cancel);
			fc.AddButton (string_button_import, ResponseType.Ok);
			fc.DefaultResponse = ResponseType.Ok;
			
			string start_dir = (string) Config.Get (GConfKeyImportFolder, GConfDefaultImportFolder);

			start_dir = start_dir.Replace ("~", FileUtils.HomeDirectory);

			fc.SetCurrentFolder (start_dir);

			if (fc.Run () != (int) ResponseType.Ok) {
				fc.Destroy ();

				return;
			}

			fc.Visible = false;

			Config.Set (GConfKeyImportFolder, fc.CurrentFolder);

			ArrayList new_dinfos = new ArrayList ();
			foreach (string dir in fc.Filenames) {
				DirectoryInfo dinfo = new DirectoryInfo (dir);
				
				if (dinfo.Exists)
					new_dinfos.Add (dinfo);
			}

			if (new_dinfos.Count > 0)
				Global.DB.AddFolders (new_dinfos);

			fc.Destroy ();
		}

		private void OnOpenPlaylist (object o, EventArgs args)
		{
			FileSelector sel = new FileSelector (string_title_open, this, FileChooserAction.Open,
							     "/apps/muine/default_playlist_folder");

			FileFilter filter = new FileFilter ();
			filter.Name = string_open_filter;
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
			FileSelector sel = new FileSelector (string_title_save, this, FileChooserAction.Save,
							     "/apps/muine/default_playlist_folder");
			sel.CurrentName = string_save_default;

			string fn = sel.GetFile ();

			if (fn.Length == 0)
				return;

			/* make sure the extension is ".m3u" */
			if (!FileUtils.IsPlaylist (fn))
				fn += ".m3u";

			if (FileUtils.Exists (fn)) {
				YesNoDialog d = new YesNoDialog (String.Format (string_overwrite, FileUtils.MakeHumanReadable (fn)), this);
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

		private bool setting_repeat = false;
		private bool Repeat {
			set {
				setting_repeat = true;
				
				Global.Actions.Repeat.Active = value;

				Config.Set (GConfKeyRepeat, value);
				
				PlaylistChanged ();

				setting_repeat = false;
			}

			get { return Global.Actions.Repeat.Active; }
		}

		private void OnRepeat (object o, EventArgs args)
		{
			if (setting_repeat)
				return;

			this.Repeat = Global.Actions.Repeat.Active;
		}

		private void OnConfigRepeatChanged (object o, GConf.NotifyEventArgs args)
		{
			bool val = (bool) args.Value;

			if (val != Repeat)
				Repeat = val;
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
			Global.Exit ();
		}

		private void OnQuit (object o, EventArgs args)
		{
			Quit ();
		}

		private void OnAbout (object o, EventArgs args)
		{
			About.ShowWindow (this);
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
			bool success = true;

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
				success = false;

				bool added_files = false;

				ArrayList new_dinfos = new ArrayList ();

				foreach (string s in bits) {
					string fn = FileUtils.LocalPathFromUri (s);

					if (fn == null)
						continue;
		
					DirectoryInfo dinfo = new DirectoryInfo (fn);
					
					if (dinfo.Exists)
						new_dinfos.Add (dinfo);
					else {
						System.IO.FileInfo finfo = new System.IO.FileInfo (fn);
						
						if (!finfo.Exists)
							continue;
						
						if (FileUtils.IsPlaylist (fn)) {
							OpenPlaylist (fn);

							pos.First = false;

							success = true;

							break;
						} else {
							Song song = GetSingleSong (finfo.FullName);
						
							if (song != null) {
								DragAddSong (song, pos);

								added_files = true;
							}
						}
					}
				}

				if (added_files) {
					EnsurePlaying ();

					PlaylistChanged ();

					success = true;
				}

				if (new_dinfos.Count > 0) {
					Global.DB.AddFolders (new_dinfos);

					success = true;
				}

				break;
			default:
				break;
			}

			Drag.Finish (args.Context, success, false, args.Time);
		}
	}
}
