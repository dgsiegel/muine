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
		// Constants
		private const int Step = 5; // # seconds to skip back and forth
		private const int MinRestartTime = 3; // seconds after which song will restart on "previous"

		private const int MinShowHours = 6000; // seconds over which to show remaining time in hours
		private const int MinShowMinutes = 60; // seconds over which to show remaining time in minutes
		
		// GConf
		private const string GConfKeyWidth = "/apps/muine/playlist_window/width";
		private const int GConfDefaultWidth = -1; 

		private const string GConfKeyHeight = "/apps/muine/playlist_window/height";
		private const int GConfDefaultHeight = 450;

		private const string GConfKeyVolume = "/apps/muine/volume";
		private const int GConfDefaultVolume = 50;

		private const string GConfKeyRepeat = "/apps/muine/repeat";
		private const bool GConfDefaultRepeat = false;

		// Strings
		private static readonly string string_program = 
			Catalog.GetString ("Muine Music Player");
		private static readonly string string_playlist_filename =
			Catalog.GetString ("Playlist.m3u");
		private static readonly string string_playlist = 
			Catalog.GetString ("<b>Playlist</b>");
		private static readonly string string_playlist_repeating =
			Catalog.GetString ("<b>Playlist</b> (Repeating)");
		private static readonly string string_playlist_under_minute =
			Catalog.GetString ("<b>Playlist</b> (Less than one minute remaining)");
		private static readonly string string_artists =
			Catalog.GetString ("From \"{0}\"");
		private static readonly string string_album_unknown =
			Catalog.GetString ("Album unknown");
		private static readonly string string_performers =
			Catalog.GetString ("Performed by {0}");

		// Strings :: Window Titles
		private static readonly string string_title_main =
			Catalog.GetString ("{0} - Muine Music Player");

		// Strings :: Tooltips
		private static readonly string string_tooltip_toggle_play =
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

		// Strings :: Errors
		private static readonly string string_error_audio =
			Catalog.GetString ("Failed to initialize the audio backend:\n{0}");
		private static readonly string string_error_read =
			Catalog.GetString ("Failed to read {0}:");
		private static readonly string string_error_close =
			Catalog.GetString ("Failed to close {0}:");
		private static readonly string string_error_write =
			Catalog.GetString ("Failed to write {0}:");

		// Events
		// Events :: SongChangedEvent (IPlayer)
		public event SongChangedEventHandler SongChangedEvent;
		
		// Events :: StateChangedEvent (IPlayer)
		public event StateChangedEventHandler StateChangedEvent;

		// Events :: TickEvent (IPlayer)
		public event TickEventHandler TickEvent;
		
		// Events :: PlaylistChangedEvent (IPlayer)
		public event GenericEventHandler PlaylistChangedEvent;
		
		// Events :: SelectionChangedEvent (IPlayer)
		public event GenericEventHandler SelectionChangedEvent;

		// Internal Classes
		// Internal Classes :: InvalidSong
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

		// Internal Classes :: DragAddSongPosition
		private class DragAddSongPosition {
			public IntPtr               Pointer;
			public TreeViewDropPosition Position;
			public bool                 First;
		}

		// Widgets
		private Tooltips tooltips;
		private Gdk.Pixbuf empty_pixbuf;
		private CellRenderer pixbuf_renderer;
		private CellRenderer text_renderer;

		[Glade.Widget] private VBox           main_vbox;
		[Glade.Widget] private Box            menu_bar_box;
		[Glade.Widget] private ScrolledWindow scrolledwindow;
		
		// Widgets :: Containers
		[Glade.Widget] private Container volume_button_container;
		[Glade.Widget] private Container title_label_container;
		[Glade.Widget] private Container artist_label_container;
		[Glade.Widget] private Container cover_image_container;

		// Widgets :: Images
		[Glade.Widget] private Image toggle_play_image;
		[Glade.Widget] private Image previous_image;
		[Glade.Widget] private Image next_image;
		[Glade.Widget] private Image add_song_image;
		[Glade.Widget] private Image add_album_image;

		// Widgets :: Toolbar
		[Glade.Widget] private ToggleButton toggle_play_button;
		[Glade.Widget] private Button       previous_button;
		[Glade.Widget] private Button       next_button;
		[Glade.Widget] private Button       add_song_button;
		[Glade.Widget] private Button       add_album_button;		
		private VolumeButton volume_button;

		// Widgets :: Player
		private CoverImage cover_image;
		private EllipsizingLabel title_label;
		private EllipsizingLabel artist_label;
		[Glade.Widget] private Label time_label;

		// Widgets :: Playlist
		[Glade.Widget] private Label    playlist_label;
		[Glade.Widget] private EventBox playlist_label_event_box;
		private HandleView playlist;

		// Windows
		private SkipToWindow   skip_to_window   = null;
		private AddSongWindow  add_song_window  = null;
		private AddAlbumWindow add_album_window = null;

		// Objects
		// Objects :: Player
		private Player player;
		private bool had_last_eos;
		private bool ignore_song_change;

		// Drag-and-Drop
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

		// Variables
		private uint busy_level = 0;

		private int last_x = -1;
		private int last_y = -1;
		private bool window_visible;

		private long remaining_songs_time;

		private Hashtable random_sort_keys;

		// Constructor
		public PlaylistWindow () : base (WindowType.Toplevel)
		{
			// Build the interface
			Glade.XML glade_xml = 
				new Glade.XML (null, "PlaylistWindow.glade", "main_vbox", null);
			glade_xml.Autoconnect (this);	
			base.Add (main_vbox);

			// Hook up window signals
			base.WindowStateEvent += new WindowStateEventHandler (OnWindowStateEvent);
			base.DeleteEvent      += new DeleteEventHandler      (OnDeleteEvent     );
			base.DragDataReceived += new DragDataReceivedHandler (OnDragDataReceived);

			Gtk.Drag.DestSet (this, DestDefaults.All, drag_entries, Gdk.DragAction.Copy);

			// Keep track of window visibility
			base.VisibilityNotifyEvent += new VisibilityNotifyEventHandler (OnVisibilityNotifyEvent);
			AddEvents ((int) Gdk.EventMask.VisibilityNotifyMask);

			// Set up various other UI bits
			SetupPlayer (); // Has to be before the others, they need the Player object
						    
			// Setup Menus
			base.AddAccelGroup (Global.Actions.UIManager.AccelGroup);
			menu_bar_box.Add (Global.Actions.MenuBar);
			
			SetupButtons ();
			SetupPlaylist ();

			// Connect to song database signals
			Global.DB.SongChanged += new SongDatabase.SongChangedHandler (OnSongChanged);
			Global.DB.SongRemoved += new SongDatabase.SongRemovedHandler (OnSongRemoved);

			// Make sure the interface is up to date
			SelectionChanged ();
			StateChanged (false, true);
		}

		// Properties
		// 	Useful for Plug-Ins and DBus
		// Properties :: PlayingSong (get;) (IPlayer)
		public ISong PlayingSong {
			get {
				return (playlist.Playing == IntPtr.Zero)
				       ? null
				       : Song.FromHandle (playlist.Playing);
			}
		}

		// Properties :: Playing (set; get;) (IPlayer)
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
				} else {
					player.Pause ();
				}
			}

			get { return player.Playing; }
		}

		// Properties :: Volume (set; get;) (IPlayer)
		public int Volume {
			set {
				if (value > 100 || value < 0)
					value = GConfDefaultVolume;

				player.Volume = value;
				volume_button.Volume = value;

				Config.Set (GConfKeyVolume, value);
			}
			
			get { return player.Volume; }
		}

		// Properties :: Position (set; get;) (IPlayer)
		public int Position {
			set { SeekTo (value); }
			get { return player.Position; }
		}

		// Properties :: HasNext (get;) (IPlayer)
		public bool HasNext {
			get { return playlist.HasNext; }
		}

		// Properties :: HasPrevious (get;) (IPlayer)
		public bool HasPrevious {
			get { return playlist.HasPrevious; }
		}

		// Properties :: Playlist (get;) (IPlayer)
		public ISong [] Playlist {
			get { return ArrayFromList (playlist.Contents); }
		}

		// Properties :: Selection (get;) (IPlayer)
		public ISong [] Selection {
			get { return ArrayFromList (playlist.SelectedPointers); }
		}

		// Properties :: UIManager (get;) (IPlayer)
		public UIManager UIManager {
			get { return Global.Actions.UIManager; }
		}

		// Properties :: Window (get;) (IPlayer)
		public Window Window {
			get { return this; }
		}

		// Properties :: BusyLevel (set; get;) (IPlayer)
		public uint BusyLevel {
			set {
				if (busy_level == 0 && value > 0) {
					base.Realize ();
					base.GdkWindow.Cursor = new Gdk.Cursor (Gdk.CursorType.Watch);
					base.GdkWindow.Display.Flush ();

				} else if (busy_level > 0 && value == 0) {
					base.GdkWindow.Cursor = null;
				}

				busy_level = value;
			}

			get { return busy_level; }
		}

		// Properties :: WindowVisible (set; get;) (IPlayer)
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

		// Properties :: Repeat (set; get;)
		private bool repeat;
		public bool Repeat {
			set {
				repeat = value;

				Global.Actions.ToggleRepeat.Active = value;

				Config.Set (GConfKeyRepeat, value);			

				PlaylistChanged ();
			}

			get { return repeat; }
		}

		// Methods
		// Methods :: Public
		// Methods :: Public :: RestorePlaylist
		public void RestorePlaylist ()
		{
			// Load last playlist
			if (!File.Exists (FileUtils.PlaylistFile))
				return;

			OpenPlaylistInternal (FileUtils.PlaylistFile,
					      new PlaylistForeachFunc (RestorePlaylistForeachFunc),
					      null);
			EnsurePlaying ();
		}

		// Methods :: Public :: Run
		public void Run ()
		{
			if (!playlist.HasFirst)
				SongChanged (true); // make sure the UI is up to date

			RestoreState ();
			WindowVisible = true;
		}

		// Methods :: Public :: Quit (IPlayer)
		public void Quit ()
		{
			Global.Exit ();
		}

		// Methods :: Public :: UpdateWindowVisibilityUI
		public void UpdateWindowVisibilityUI ()
		{
			if (WindowVisible && playlist.Playing != IntPtr.Zero)
				playlist.Select (playlist.Playing);

			Global.Actions.ToggleVisible.Label = (WindowVisible)
							     ? Actions.StringToggleVisibleHide
							     : Actions.StringToggleVisibleShow;
		}

		// Methods :: Public :: PlayFile (IPlayer)
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

		// Methods :: Public :: QueueFile (IPlayer)
		public void QueueFile (string file)
		{
			Song song = GetSingleSong (file);

			if (song == null)
				return;

			IntPtr p = AddSong (song);
			EnsurePlaying ();
			PlaylistChanged ();
		}

		// Methods :: Public :: OpenPlaylist (IPlayer)
		public void OpenPlaylist (string fn)
		{
			BusyLevel ++;

			ClearPlaylist ();
			OpenPlaylistInternal (fn,
					      new PlaylistForeachFunc (RegularPlaylistForeachFunc),
					      null);
			EnsurePlaying ();
			PlaylistChanged ();
			Playing = true;

			BusyLevel --;
		}

		// Methods :: Public :: Previous (IPlayer)
		public void Previous ()
		{
			if (!playlist.HasFirst)
				return;

			if (player.Position < MinRestartTime) {
				if (playlist.HasPrevious) {
					playlist.Previous ();
					PlaylistChanged ();

				} else if (repeat) {
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

		// Methods :: Public :: Next (IPlayer)
		public void Next ()
		{
			if (playlist.HasNext)
				playlist.Next ();
			else if (repeat && playlist.HasFirst)
				playlist.First ();
			else
				return;

			playlist.Select (playlist.Playing);
			PlaylistChanged ();
			player.Play ();
		}

		// Methods :: Public :: PlaySong (IPlayer)
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

		// Methods :: Public :: PlayAlbum (IPlayer)
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

		// Methods :: Public :: AddChildWindowIfVisible
		public void AddChildWindowIfVisible (Window window)
		{
			window.TransientFor = (WindowVisible) ? this : null;
		}

		// Methods :: Public :: RunSkipToDialog
		public void RunSkipToDialog ()
		{
			playlist.Select (playlist.Playing);

			if (skip_to_window == null)
				skip_to_window = new SkipToWindow (this);

			skip_to_window.Run ();
		}

		// Methods :: Public :: ToggleVisible
		public void ToggleVisible ()
		{
			WindowVisible = !WindowVisible;
		}

		// Methods :: Public :: SkipBackwards		
		public void SkipBackwards ()
		{				
			SeekTo (player.Position - Step);
		}

		// Methods :: Public :: SkipForward
		public void SkipForward ()
		{
			SeekTo (player.Position + Step);
		}

		// Methods :: Public :: RemoveSelected
		public void RemoveSelected ()
		{
			List selected_pointers = playlist.SelectedPointers;

			int counter = 0, selected_pointers_count = selected_pointers.Count;
			bool song_changed = false;

			// HACK: To improve performance, only load new song once
			ignore_song_change = true; 

			foreach (int i in selected_pointers) {
				IntPtr sel = new IntPtr (i);

				if (sel == playlist.Playing) {
					OnPlayingSongRemoved ();		
					song_changed = true;
				}
				
				if (counter == selected_pointers_count - 1 &&
				    !playlist.SelectNext ()) {
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
		
		// Methods :: Public :: RemovePlayed
		public void RemovePlayed ()
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
		
		// Methods :: Public :: Clear
		public void Clear ()
		{
			ClearPlaylist ();
			PlaylistChanged ();
		}
		
		// Methods :: Public :: Shuffle
		public void Shuffle ()
		{
			Random rand = new Random ();

			random_sort_keys = new Hashtable ();

			foreach (int i in playlist.Contents) {
				Song song = Song.FromHandle ((IntPtr) i); // UNUSED

				double val = (i == (int) playlist.Playing) ? -1.0 : rand.NextDouble ();
				random_sort_keys.Add (i, val);
			}

			playlist.Sort (new HandleView.CompareFunc (ShuffleFunc));

			random_sort_keys = null;

			PlaylistChanged ();

			if (playlist.Playing != IntPtr.Zero)
				playlist.Select (playlist.Playing);
		}
		
		// Methods :: Public :: SavePlaylist
		public void SavePlaylist (string fn, bool exclude_played, bool store_playing)
		{
			bool remote = FileUtils.IsRemote (fn);

			if (remote)
				BusyLevel ++;

			VfsStream stream;
			StreamWriter writer;
			
			try {
				stream = new VfsStream (fn, System.IO.FileMode.Create);
				writer = new StreamWriter (stream);
			} catch (Exception e) {
				string msg = String.Format (string_error_write, FileUtils.MakeHumanReadable (fn));
				new ErrorDialog (this, msg, e.Message);
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
			} catch (Exception e) {
				string msg = String.Format (string_error_close, FileUtils.MakeHumanReadable (fn));
				new ErrorDialog (this, msg, e.Message);
			}

			if (remote)
				BusyLevel --;
		}

		// Methods :: Private
		// Methods :: Private :: SetupButtons
		private void SetupButtons ()
		{
			// Callbacks
			toggle_play_button.Clicked += new EventHandler (OnTogglePlayButtonClicked);
			previous_button   .Clicked += new EventHandler (OnPreviousButtonClicked );
			next_button       .Clicked += new EventHandler (OnNextButtonClicked     );
			add_song_button   .Clicked += new EventHandler (OnAddSongButtonClicked  );
			add_album_button  .Clicked += new EventHandler (OnAddAlbumButtonClicked );

			// Images
			Image image;
			toggle_play_image.SetFromStock ("stock_media-play"     , IconSize.LargeToolbar);
			previous_image   .SetFromStock ("stock_media-prev"     , IconSize.LargeToolbar);
			next_image       .SetFromStock ("stock_media-next"     , IconSize.LargeToolbar);
			add_song_image   .SetFromStock (Stock.Add              , IconSize.LargeToolbar);
			add_album_image  .SetFromStock ("gnome-dev-cdrom-audio", IconSize.LargeToolbar);

			// Volume
			volume_button = new VolumeButton ();
			volume_button_container.Add (volume_button);
			volume_button.Visible = true;
			volume_button.VolumeChanged += new VolumeButton.VolumeChangedHandler (OnVolumeChanged);

			// Tooltips
			tooltips = new Tooltips ();
			tooltips.SetTip (toggle_play_button, string_tooltip_toggle_play, null);
			tooltips.SetTip (previous_button   , string_tooltip_previous   , null);
			tooltips.SetTip (next_button       , string_tooltip_next       , null);
			tooltips.SetTip (add_album_button  , string_tooltip_add_album  , null);
			tooltips.SetTip (add_song_button   , string_tooltip_add_song   , null);
			tooltips.SetTip (volume_button     , string_tooltip_volume     , null);
		}

		// Methods :: Private :: SetupPlaylist
		private void SetupPlaylist ()
		{
			playlist = new HandleView ();

			playlist.Selection.Mode = SelectionMode.Multiple;

			pixbuf_renderer = new ColoredCellRendererPixbuf ();
			text_renderer   = new CellRendererText ();
			
			playlist.AddColumn (pixbuf_renderer, new HandleView.CellDataFunc (PixbufCellDataFunc), false);
			playlist.AddColumn (text_renderer  , new HandleView.CellDataFunc (TextCellDataFunc  ), true );

			playlist.RowActivated     += new HandleView.RowActivatedHandler     (OnPlaylistRowActivated    );
			playlist.SelectionChanged += new HandleView.SelectionChangedHandler (OnPlaylistSelectionChanged);
			playlist.PlayingChanged   += new HandleView.PlayingChangedHandler   (OnPlaylistPlayingChanged  );

			playlist.EnableModelDragSource (Gdk.ModifierType.Button1Mask, playlist_source_entries,
							Gdk.DragAction.Copy | Gdk.DragAction.Link | Gdk.DragAction.Ask);
			playlist.EnableModelDragDest   (playlist_dest_entries,
						        Gdk.DragAction.Copy);

			playlist.DragDataGet      += new DragDataGetHandler      (OnPlaylistDragDataGet     );
			playlist.DragDataReceived += new DragDataReceivedHandler (OnPlaylistDragDataReceived);

			playlist.Show ();

			scrolledwindow.Add (playlist);
			
			empty_pixbuf = new Gdk.Pixbuf (null, "muine-nothing.png");
		}

		// Methods :: Private :: SetupPlayer
		private void SetupPlayer ()
		{
			try {
				player = new Player ();
			} catch (Exception e) {
				throw new Exception (String.Format (string_error_audio, e.Message));
			}

			player.EndOfStreamEvent += new Player.EndOfStreamEventHandler (OnEndOfStreamEvent);
			player.TickEvent        += new Player.TickEventHandler        (OnTickEvent       );
			player.StateChanged     += new Player.StateChangedHandler     (OnStateChanged    );

			title_label = new EllipsizingLabel ("");
			title_label.Visible = true;
			title_label.Xalign = 0.0f;
			title_label.Selectable = true;
			title_label_container.Add (title_label);

			artist_label = new EllipsizingLabel ("");
			artist_label.Visible = true;
			artist_label.Xalign = 0.0f;
			artist_label.Selectable = true;
			artist_label_container.Add (artist_label);

			cover_image = new CoverImage ();
			cover_image_container.Add (cover_image);
			cover_image.ShowAll ();

			// playlist label dnd
			playlist_label_event_box.DragDataGet +=	new DragDataGetHandler (OnPlaylistLabelDragDataGet);
				
			Gtk.Drag.SourceSet (playlist_label_event_box, Gdk.ModifierType.Button1Mask,
					    drag_entries, Gdk.DragAction.Move);

			// FIXME depends on Ximian Bugzilla #71060
			// string icon = Gnome.Icon.Lookup (IconTheme.GetForScreen (this.Screen), null, null, null, null, 
			//				    "audio/x-mpegurl", Gnome.IconLookupFlags.None, null);

			Gtk.Drag.SourceSetIconStock (playlist_label_event_box, "gnome-mime-audio");
		}

		// Methods :: Private :: PlayAndSelect
		private void PlayAndSelect (IntPtr ptr)
		{
			playlist.Playing = ptr;
			playlist.Select (playlist.Playing);
		}

		// Methods :: Private :: PlayFirstAndSelect
		private void PlayFirstAndSelect ()
		{
			playlist.First ();
			playlist.Select (playlist.Playing);
		}

		// Methods :: Private :: EnsurePlaying
		private void EnsurePlaying ()
		{
			if (playlist.Playing == IntPtr.Zero && playlist.HasFirst)
				PlayFirstAndSelect ();
		}

		// Methods :: Private :: AddSong
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

		// Methods :: Private :: AddSongAtPos
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

		// Methods :: Private :: RemoveSong
		private void RemoveSong (IntPtr p)
		{
			playlist.Remove (p);

			Song song = Song.FromHandle (p);

			if (song.IsExtraHandle (p))
				song.UnregisterExtraHandle (p);
		}

		// Methods :: Private :: UpdateTimeLabels
		private void UpdateTimeLabels (int time)
		{
			if (playlist.Playing == IntPtr.Zero) {
				time_label.Text = "";
				playlist_label.Markup = string_playlist;
				return;
			}
			
			Song song = Song.FromHandle (playlist.Playing);

			String pos   = StringUtils.SecondsToString (time);
			String total = StringUtils.SecondsToString (song.Duration);

			time_label.Text = String.Format ("{0} / {1}", pos, total);

			// Calculate remaining time
			long r_seconds = (this.repeat)
					 ? remaining_songs_time
					 : remaining_songs_time + song.Duration - time;

			int hours   = (int) Math.Floor ((double) r_seconds / 3600.0 + 0.5);
			int minutes = (int) Math.Floor ((double) r_seconds /   60.0 + 0.5);

			// Possible strings
			string string_repeat_hour = 
				Catalog.GetPluralString ("<b>Playlist</b> (Repeating {0} hour)", 
							 "<b>Playlist</b> (Repeating {0} hours)", 
							 hours);

			string string_repeat_minute =
				Catalog.GetPluralString ("<b>Playlist</b> (Repeating {0} minute)", 
							 "<b>Playlist</b> (Repeating {0} minutes)", 
							 minutes);

			string string_normal_hour =
				Catalog.GetPluralString ("<b>Playlist</b> ({0} hour remaining)", 
							 "<b>Playlist</b> ({0} hours remaining)", 
							 hours);
							 
			string string_normal_minute =
				Catalog.GetPluralString ("<b>Playlist</b> ({0} minute remaining)", 
							 "<b>Playlist</b> ({0} minutes remaining)", 
							 minutes);

			// Choose string for each scenario based on whether we are repeating or not
			string string_hour = (repeat)
			                     ? string_repeat_hour 
			                     : string_normal_hour;
			
			string string_minute = (repeat)
			                       ? string_repeat_minute
			                       : string_normal_minute;
			
			string string_second = (repeat)
					       ? string_playlist_repeating
					       : string_playlist_under_minute;
			
			// Set the label
			playlist_label.Markup = (r_seconds > MinShowHours)
					        ? String.Format (string_hour, hours)
					        : 
					        (r_seconds > MinShowMinutes)
					        ? String.Format (string_minute, minutes)
				                : 
				                (r_seconds > 0)
					        ? string_second
					        : 
					        string_playlist;
		}

		// Methods :: Private :: PlaylistChanged
		private void PlaylistChanged ()
		{
			bool start_counting = this.repeat;
			
			remaining_songs_time = 0;

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

			previous_button   .Sensitive = has_first;
			toggle_play_button.Sensitive = has_first;
			next_button       .Sensitive = playlist.HasNext || (this.repeat && has_first);

			Global.Actions.TogglePlay.Sensitive = previous_button   .Sensitive;
			Global.Actions.Previous  .Sensitive = toggle_play_button.Sensitive;
			Global.Actions.Next      .Sensitive = next_button       .Sensitive;
			
			Global.Actions.SkipTo       .Sensitive = has_first;
			Global.Actions.SkipBackwards.Sensitive = has_first;
			Global.Actions.SkipForward  .Sensitive = has_first;
			Global.Actions.Shuffle      .Sensitive = has_first;

			UpdateTimeLabels (player.Position);

			SavePlaylist (FileUtils.PlaylistFile, !repeat, true);

			// Run PlaylistChangedEvent Handlers
			if (PlaylistChangedEvent != null)
				PlaylistChangedEvent ();
		}

		// Methods :: Private :: SongChanged
		private void SongChanged (bool restart)
		{
			Song song = null;

			if (playlist.Playing != IntPtr.Zero) {
				song = Song.FromHandle (playlist.Playing);

				cover_image.Song = song;

				string tip = (song.Album.Length > 0)
				             ? String.Format (string_artists, song.Album)
				             : string_album_unknown;

				if (song.Performers.Length > 0)
					tip += "\n\n" + String.Format (string_performers, StringUtils.JoinHumanReadable (song.Performers));
					
				if (song.CoverImage == null && !Global.CoverDB.Loading)
					tip += "\n\n" + string_tooltip_cover;
				
				tooltips.SetTip (cover_image, tip, null);

				title_label.Markup = String.Format ("<span size=\"large\" weight=\"bold\">{0}</span>",
								    StringUtils.EscapeForPango (song.Title));

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

				this.Title = String.Format (string_title_main, song.Title);

			} else {
				cover_image.Song = null;

				tooltips.SetTip (cover_image, null, null);

				title_label.Markup = "";

				artist_label.Text = "";
				time_label  .Text = "";

				this.Title = string_program;

				if (skip_to_window != null)
					skip_to_window.Hide ();
			}
			
			if (restart)
				had_last_eos = false;

			if (SongChangedEvent != null)
				SongChangedEvent (song);
		}

		// Methods :: Private :: SelectionChanged
		private void SelectionChanged ()
		{
			Global.Actions.Remove.Sensitive = (playlist.SelectedPointers.Count > 0);

			// Run SelectionChangedEvent Handlers
			if (SelectionChangedEvent != null)
				SelectionChangedEvent ();
		}

		// Methods :: Private :: StateChanged
		private new void StateChanged (bool playing, bool dont_signal)
		{
			// Update action entry and button states
			Global.Actions.TogglePlay.Active = playing;
			toggle_play_button.Active        = playing;

			// Update
			playlist.Changed (playlist.Playing);

			// Run StateChangedEvent Handlers
			if (!dont_signal && StateChangedEvent != null)
				StateChangedEvent (playing);
		}

		// Methods :: Private :: ClearPlaylist
		private void ClearPlaylist ()
		{
			playlist.Clear ();
			player.Stop ();
		}

		// Methods :: Private :: SeekTo
		private void SeekTo (int seconds)
		{
			Song song = Song.FromHandle (playlist.Playing);

			if (seconds >= song.Duration) {
				EndOfStream (song, true);

			} else {
				player.Position = (seconds < 0) ? 0 : seconds;
				player.Play ();
			}

			playlist.Select (playlist.Playing);
		}

		// Methods :: Private :: OpenPlaylistInternal
		private delegate void PlaylistForeachFunc (Song song, bool playing, object user_data);

		private void OpenPlaylistInternal (string              fn,
						   PlaylistForeachFunc func,
						   object              user_data)
		{
			VfsStream stream;
			StreamReader reader;

			try {
				stream = new VfsStream (fn, System.IO.FileMode.Open);
				reader = new StreamReader (stream);
			} catch (Exception e) {
				string msg = String.Format (string_error_read, FileUtils.MakeHumanReadable (fn));
				new ErrorDialog (this, msg, e.Message);
				return;
			}

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

				// DOS-to-UNIX
				line.Replace ('\\', '/');

				string basename = "";

				try {
					basename = System.IO.Path.GetFileName (line);
				} catch {
					continue;
				}

				// Get Song
				Song song = Global.DB.GetSong (line);
				
				// If that didn't work, try harder...
				if (song == null) { 
					lock (Global.DB) {
						foreach (string key in Global.DB.Songs.Keys) {
							string key_basename = System.IO.Path.GetFileName (key);

							if (basename != key_basename)
								continue;

							song = Global.DB.GetSong (key);
							break;
						}
					}
				}

				// If we don't have it in our Database, try adding it.
				if (song == null)
					song = AddSongToDB (line);

				// Give up if we don't have the song by now.
				if (song == null)
					return; 

				// Add song (and play) 
				func (song, playing_song, user_data);
				playing_song = false;
			}

			// Close File
			try {
				reader.Close ();
			} catch (Exception e) {
				string msg = String.Format (string_error_close, FileUtils.MakeHumanReadable (fn));
				new ErrorDialog (this, msg, e.Message);
				return;
			}
		}

		// Methods :: Private :: AddSongToDB
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

		// Methods :: Private :: GetSingleSong
		private Song GetSingleSong (string file)
		{
			// Get Song
			Song song = Global.DB.GetSong (file);

			// If we don't have it, try adding it
			if (song == null)
				song = AddSongToDB (file);

			return song;
		}

		// Methods :: Private :: EndOfStream
		private void EndOfStream (Song song, bool update_time)
		{
			// If we can, go to the next song
			if (playlist.HasNext) {
				playlist.Next ();

			// If we don't have another song and we are repeating,
			// go to the beginning.
			} else if (repeat) {
				playlist.First ();

			// We have nothing else to play.
			} else {
				if (update_time)
					player.Position = song.Duration;

				had_last_eos = true;

				player.Pause ();
			}

			// Update Changes
			PlaylistChanged ();
		}

		// Methods :: Private :: DragAddSong
		private IntPtr DragAddSong (Song song, DragAddSongPosition pos)
		{
			pos.Pointer = (pos.Pointer == IntPtr.Zero)
				      ? AddSong      (song.Handle)
				      : AddSongAtPos (song.Handle, pos.Pointer, pos.Position);

			pos.Position = TreeViewDropPosition.After;
				
			if (pos.First) {
				playlist.Select (pos.Pointer, false);
				pos.First = false;
			}

			return pos.Pointer;
		}

		// Methods :: Private :: ArrayFromList
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

		// Methods :: Private :: RestoreState
		private void RestoreState ()
		{
			// Window size
			int width  = (int) Config.Get (GConfKeyWidth , GConfDefaultWidth );
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

		// Handlers
		// Handlers :: OnStateChanged
		private void OnStateChanged (bool playing)
		{
			StateChanged (playing, false);
		}

		// Handlers :: OnWindowStateEvent
		private void OnWindowStateEvent (object o, WindowStateEventArgs args)
		{
			// If know we're not even visible, return
			if (!this.Visible)
				return;
			
			// If we're not Iconified or Withdrawn (minimized), show the window
			bool old_window_visible = window_visible;
			window_visible = ((args.Event.NewWindowState != Gdk.WindowState.Iconified) &&
					  (args.Event.NewWindowState != Gdk.WindowState.Withdrawn));

			// If we changed, update
			if (old_window_visible != window_visible)
				UpdateWindowVisibilityUI ();
		}

		// Handlers :: OnVisibilityNotifyEvent
		private void OnVisibilityNotifyEvent (object o, VisibilityNotifyEventArgs args)
		{
			// If we're not visible, iconified, or withdrawn (minimized), return.
			if (!Visible ||
			    GdkWindow.State == Gdk.WindowState.Iconified ||
			    GdkWindow.State == Gdk.WindowState.Withdrawn)
			    return;

			// See if we became visible (not FullyObscured)
			bool old_window_visible = window_visible;
			window_visible = (args.Event.State != Gdk.VisibilityState.FullyObscured);

			// If we did, update
			if (old_window_visible != window_visible)
				UpdateWindowVisibilityUI ();

			args.RetVal = false;
		}

		// Handlers :: OnDeleteEvent
		private void OnDeleteEvent (object o, DeleteEventArgs args)
		{
			Quit ();
		}

		// Handlers :: OnSizeAllocated
		private void OnSizeAllocated (object o, SizeAllocatedArgs args)
		{
			// Get Window size
			int width, height;
			GetSize (out width, out height);

			// Save it to GConf
			Config.Set (GConfKeyWidth , width );
			Config.Set (GConfKeyHeight, height);
		}

		// Handlers :: OnVolumeChanged
		private void OnVolumeChanged (int vol)
		{
			// Update volume
			if (vol != this.Volume)
				this.Volume = vol;
		}

		// Handlers :: OnConfigVolumeChanged
		private void OnConfigVolumeChanged (object o, GConf.NotifyEventArgs args)
		{
			// Get new volume from GConf
			int vol = (int) args.Value;

			// If we changed, update
			if (vol != this.Volume)
				this.Volume = (int) args.Value;
		}

		// Handlers :: OnQueueSongsEvent
		private void OnQueueSongsEvent (List songs)
		{
			// Add Songs
			foreach (int i in songs)
				AddSong (new IntPtr (i));

			// Play
			EnsurePlaying ();
			
			// Update
			PlaylistChanged ();
		}

		// Handlers :: OnPlaySongsEvent		
		private void OnPlaySongsEvent (List songs)
		{
			// Add Songs
			bool first = true;
			foreach (int i in songs) {
				IntPtr p = new IntPtr (i);
				
				IntPtr new_p = AddSong (p);
				
				if (!first)
					continue;

				// Select and Play the first song
				PlayAndSelect (new_p);
				player.Play ();
				
				// We only have one first
				first = false;
			}

			// Update
			PlaylistChanged ();
		}

		// Handlers :: OnQueueAlbumsEvent
		private void OnQueueAlbumsEvent (List albums)
		{
			// Add songs from albums
			foreach (int i in albums) {
				Album a = Album.FromHandle (new IntPtr (i));

				foreach (Song s in a.Songs)
					AddSong (s);
			}

			// Play
			EnsurePlaying ();
			
			// Update
			PlaylistChanged ();
		}

		// Handlers :: OnPlayAlbumsEvent
		private void OnPlayAlbumsEvent (List albums)
		{
			// Add songs from albums
			bool first = true;
			foreach (int i in albums) {
				Album a = Album.FromHandle (new IntPtr (i));

				foreach (Song s in a.Songs) {
					IntPtr new_p = AddSong (s);

					if (!first)
						continue;

					// Select and play the first song
					PlayAndSelect (new_p);
					player.Play ();

					// There's only one first
					first = false;
				}
			}

			// Update
			PlaylistChanged ();
		}

		// Handlers :: OnTickEvent
		private void OnTickEvent (int pos)
		{
			UpdateTimeLabels (pos);

			if (TickEvent != null)
				TickEvent (pos);
		}

		// Handlers :: OnEndOfStreamEvent
		private void OnEndOfStreamEvent ()
		{
			// Get current song
			Song song = Song.FromHandle (playlist.Playing);

			// If we're not really at the end, we must have had bad info
			// Update the SongDB with the new length
			if (song.Duration != player.Position) {
				song.Duration = player.Position;
				Global.DB.SaveSong (song);

				// So that any people listening to tick events
				// update their time labels with the new duration
				if (TickEvent != null)
					TickEvent (song.Duration);
			}
			
			// Do what else we need to do at the EOS
			EndOfStream (song, false);
		}

		// Handlers :: OnConfigRepeatChanged
		private void OnConfigRepeatChanged (object o, GConf.NotifyEventArgs args)
		{
			// Get new repeat setting from GConf
			bool val = (bool) args.Value;

			// If it changed, update.
			if (val != this.repeat)
				this.Repeat = val;
		}

		// Handlers :: OnPlaylistRowActivated
		private void OnPlaylistRowActivated (IntPtr handle)
		{
			// Play selected song
			playlist.Playing = handle;
			PlaylistChanged ();
			player.Play ();
		}

		// Handlers :: OnPlaylistSelectionChanged
		private void OnPlaylistSelectionChanged ()
		{
			SelectionChanged ();
		}

		// Handlers :: OnPlaylistPlayingChanged
		private void OnPlaylistPlayingChanged (IntPtr playing)
		{
			if (!ignore_song_change)
				SongChanged (true);
		}

		// Handlers :: OnSongChanged
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

		// Handlers :: OnPlayingSongRemoved
		private void OnPlayingSongRemoved ()
		{
			if (playlist.HasNext) {
				playlist.Next ();

			} else if (playlist.HasPrevious) {
				playlist.Previous ();

			} else { // playlist is empty now
				playlist.Playing = IntPtr.Zero;
				player.Stop ();
			}
		}

		// Handlers :: OnSongRemoved
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

		// Handlers :: OnPlaylistDragDataGet
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

		// Handlers :: OnPlaylistDragDataReceived
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

			// Work around gtk bug #164085
			string tree_model_row = String.Format ("\t{0}\t", DndUtils.TargetMuineTreeModelRow.Target);
			string song_list      = String.Format ("\t{0}\t", DndUtils.TargetMuineSongList    .Target);
			string album_list     = String.Format ("\t{0}\t", DndUtils.TargetMuineAlbumList   .Target);
			
			bool is_tree_model = data.StartsWith (tree_model_row);
			bool is_song_list  = data.StartsWith (song_list     );
			bool is_album_list = data.StartsWith (album_list    );
			
			type = (is_tree_model)
			       ? (uint) DndUtils.TargetType.ModelRow
			       :
			       (is_song_list)
	                       ? (uint) DndUtils.TargetType.SongList
	                       :
	                       (is_album_list)
			       ? (uint) DndUtils.TargetType.AlbumList
			       :
			       type;

			string head = (is_tree_model)
				      ? tree_model_row
			              :
			              (is_song_list)
				      ? song_list
				      :
				      (is_album_list)
				      ? album_list
				      :
				      "";

			data = data.Substring (head.Length);

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

					// Reorder part 1: remove old row
					if (type == (uint) DndUtils.TargetType.ModelRow) {
						if (ptr == pos.Pointer)
							break;

						if (ptr == playlist.Playing) {
							play = true;
							ignore_song_change = true;
						}
						
						RemoveSong (ptr);
					}

					ptr = DragAddSong (song, pos);

					// Reorder part 2: if the row was playing, keep it playing						
					if (play) {
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
					
					if (dinfo.Exists) {
						new_dinfos.Add (dinfo);

					} else {
						System.IO.FileInfo finfo = new System.IO.FileInfo (fn);
						
						if (!finfo.Exists)
							continue;
						
						if (FileUtils.IsPlaylist (fn)) {
							BusyLevel ++;
							OpenPlaylistInternal (fn,
									      new PlaylistForeachFunc (DragPlaylistForeachFunc),
									      pos);
							BusyLevel --;

							added_files = true;

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

		// Handlers :: OnTogglePlayButtonClicked
		private void OnTogglePlayButtonClicked (object o, EventArgs args)
		{
			if (toggle_play_button.Active != Playing)
				Playing = toggle_play_button.Active;
		}
		
		// Handlers :: OnPreviousButtonClicked
		private void OnPreviousButtonClicked (object o, EventArgs args)
		{
			Previous ();
		}
		
		// Handlers :: OnNextButtonClicked
		private void OnNextButtonClicked (object o, EventArgs args)
		{
			Next ();
		}
		
		// Handlers :: OnAddSongButtonClicked
		private void OnAddSongButtonClicked (object o, EventArgs args)
		{
			PlaySong ();
		}
		
		// Handlers :: OnAddAlbumButtonClicked
		private void OnAddAlbumButtonClicked (object o, EventArgs args)
		{
			PlayAlbum ();
		}
		
		// Handlers :: OnPlaylistLabelDragDataGet
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

		// Handlers :: OnDragDataReceived
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
		
		// Delegate Functions
		// Delegate Functions :: PixbufCellDataFunc
		private void PixbufCellDataFunc (HandleView view, CellRenderer cell, IntPtr handle)
		{
			ColoredCellRendererPixbuf r = (ColoredCellRendererPixbuf) cell;

			r.Pixbuf = (handle == view.Playing)
				   ? (player.Playing)
				     ? view.RenderIcon ("muine-playing", IconSize.Menu, null)
				     : view.RenderIcon ("muine-paused" , IconSize.Menu, null)
				   : empty_pixbuf;
		}

		// Delegate Functions :: TextCellDataFunc
		private void TextCellDataFunc (HandleView view, CellRenderer cell, IntPtr handle)
		{
			Song song = Song.FromHandle (handle);
			CellRendererText r = (CellRendererText) cell;

			r.Markup = String.Format ("<b>{0}</b>\n{1}",
			                          StringUtils.EscapeForPango (song.Title),
						  StringUtils.EscapeForPango (StringUtils.JoinHumanReadable (song.Artists)));
		}

		// Delegate Functions :: ShuffleFunc		
		private int ShuffleFunc (IntPtr ap, IntPtr bp)
		{
			double a = (double) random_sort_keys [(int) ap];
			double b = (double) random_sort_keys [(int) bp];

			return a.CompareTo (b);
		}		

		// Delegate functions: DragPlaylistForeachFunc
		private void DragPlaylistForeachFunc (Song song, bool playing, object user_data)
		{
			DragAddSongPosition pos = (DragAddSongPosition) user_data;

			DragAddSong (song, pos);
		}

		// Delegate functions: RestorePlaylistForeachFunc
		private void RestorePlaylistForeachFunc (Song song, bool playing, object user_data)
		{
			IntPtr p = AddSong (song);

			if (playing)
				PlayAndSelect (p);
		}
		
		// Delegate functions: RegularPlaylistForeachFunc
		private void RegularPlaylistForeachFunc (Song song, bool playing, object user_data)
		{
			AddSong (song);
		}
	}
}
