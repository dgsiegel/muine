/*
 * Copyright (C) 2003, 2004 Jorn Baayen <jorn@nl.linux.org>
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
using System.Text.RegularExpressions;

using Gtk;
using GLib;
using Gnome.Vfs;

public class PlaylistWindow : Window
{
	/* menu widgets */
	[Glade.Widget]
	private ImageMenuItem play_pause_menu_item;
	private Image play_pause_menu_item_image;
	[Glade.Widget]
	private ImageMenuItem previous_menu_item;
	[Glade.Widget]
	private ImageMenuItem next_menu_item;
	[Glade.Widget]
	private ImageMenuItem skip_to_menu_item;
	[Glade.Widget]
	private ImageMenuItem skip_backwards_menu_item;
	[Glade.Widget]
	private ImageMenuItem skip_forward_menu_item;
	[Glade.Widget]
	private ImageMenuItem information_menu_item;
	[Glade.Widget]
	private ImageMenuItem remove_song_menu_item;
	[Glade.Widget]
	private CheckMenuItem repeat_menu_item;
	private bool setting_repeat_menu_item;
	[Glade.Widget]
	private ImageMenuItem shuffle_menu_item;

	/* toolbar widgets */
	[Glade.Widget]
	private Button previous_button;
	[Glade.Widget]
	private Button play_pause_button;
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
	private HandleView playlist;

	/* other widgets */
	private Tooltips tooltips;
	private NotificationAreaIcon icon;

	/* windows */
	SkipToWindow skip_to_window = null;
	AddSongWindow add_song_window = null;
	AddAlbumWindow add_album_window = null;

	/* the player object */
	private Player player;

	/* the playlist filename */
	private string playlist_filename;

	/* Multimedia Key handler */
	private MmKeys mmkeys;

	/* Drag and drop targets. */
	private enum TargetType {
		UriList
	};

	private static TargetEntry [] drag_entries = new TargetEntry [] {
		new TargetEntry ("text/uri-list", 0, (uint) TargetType.UriList)
	};

	public delegate void PlayerChangedSongHandler (Song song, bool hasfocus);

	/* Event to notify when song changed: plugins like
	   Dashboard can use this.  */
	public event PlayerChangedSongHandler PlayerChangedSong;

	public PlaylistWindow () : base (WindowType.Toplevel)
	{
		/* build the interface */
		Glade.XML glade_xml = new Glade.XML (null, "PlaylistWindow.glade", "main_vbox", null);
		glade_xml.Autoconnect (this);
			
		Add (glade_xml ["main_vbox"]);

		AddAccelGroup (((Menu) glade_xml ["file_menu"]).AccelGroup);

		WindowStateEvent += new WindowStateEventHandler (HandleWindowStateEvent);
		DeleteEvent += new DeleteEventHandler (HandleDeleteEvent);
		DragDataReceived += new DragDataReceivedHandler (HandleDragDataReceived);
		Gtk.Drag.DestSet (this, DestDefaults.All,
				  drag_entries, Gdk.DragAction.Copy);

		/* keep track of window visibility */
		VisibilityNotifyEvent += new VisibilityNotifyEventHandler (HandleWindowVisibilityNotifyEvent);
		AddEvents ((int) Gdk.EventMask.VisibilityNotifyMask);

		icon = new NotificationAreaIcon ();

		SetupWindowSize ();
		SetupPlayer (glade_xml);
		SetupButtonsAndMenuItems (glade_xml);
		SetupPlaylist (glade_xml);

		/* connect to song database signals */
		Muine.DB.SongChanged += new SongDatabase.SongChangedHandler (HandleSongChanged);
		Muine.DB.SongRemoved += new SongDatabase.SongRemovedHandler (HandleSongRemoved);

		/* Create multimedia key handler */
		mmkeys = new MmKeys ();
		mmkeys.Next += new EventHandler (HandleNextCommand);
		mmkeys.Previous += new EventHandler (HandlePreviousCommand);
		mmkeys.PlayPause += new EventHandler (HandlePlayPauseCommand);
		mmkeys.Stop += new EventHandler (HandleStopCommand);

		/* Add Dashboard support */
		PlayerChangedSong += DashboardFrontend.PlayerChangedSong;

		/* set up playlist filename */
		playlist_filename = Gnome.User.DirGet () + "/muine/playlist.m3u";
		
		/* make sure the interface is up to date */
		SelectionChanged ();
		StateChanged (false);
	}

	public void RestorePlaylist ()
	{
		/* load last playlist */
		System.IO.FileInfo finfo = new System.IO.FileInfo (playlist_filename);
		if (finfo.Exists)
			OpenPlaylist (playlist_filename);
	}

	public void Run ()
	{
		if (!playlist.HasFirst) {
			EnsurePlaying ();

			NSongsChanged ();
		}

		WindowVisible = true;

		icon.Run ();

		/* put on the screen immediately */
		while (MainContext.Pending ())
			Main.Iteration ();
	}

	private void HandleDragDataReceived (object o, DragDataReceivedArgs args)
	{
		string data = StringUtils.SelectionDataToString (args.SelectionData);
		string [] uri_list;
		string fn;

		switch (args.Info) {
		case (uint) TargetType.UriList:
			uri_list = Regex.Split (data, "\r\n");
			fn = StringUtils.LocalPathFromUri (uri_list [0]);

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
			
		ProgressWindow pw = new ProgressWindow (this, dinfo.Name);
		
		Muine.DB.AddWatchedFolder (dinfo.FullName);
		HandleDirectory (dinfo, pw);
		
		pw.Done ();

		Drag.Finish (args.Context, true, false, args.Time);
	}

	public void CheckFirstStartUp () 
 	{
		bool first_start;
 		try { 
 			first_start = (bool) Muine.GConfClient.Get ("/apps/muine/first_start");
 		} catch {
 			first_start = true;
 		}

		if (first_start == false)
			return;

 		string dir = Environment.GetEnvironmentVariable ("HOME");
 		if (dir.EndsWith ("/") == false)
 			dir += "/";
 		
 		DirectoryInfo musicdir = new DirectoryInfo (dir + "Music/");
  
 		if (!musicdir.Exists) {
 			NoMusicFoundWindow w = new NoMusicFoundWindow (this);

	 		Muine.GConfClient.Set ("/apps/muine/first_start", false);
 		} else { 
 			/* create a playlists directory if it still doesn't exists */
 			DirectoryInfo playlistsdir = new DirectoryInfo (dir + "Music/Playlists/");
 			if (!playlistsdir.Exists)
 				playlistsdir.Create ();

 			ProgressWindow pw = new ProgressWindow (this, musicdir.Name);

 			/* seems to be that $HOME/Music does exists, but user hasn't started Muine before! */
 			Muine.DB.AddWatchedFolder (musicdir.FullName);

			/* do this here, because the folder is watched now */
	 		Muine.GConfClient.Set ("/apps/muine/first_start", false);
	
 			HandleDirectory (musicdir, pw);

 			pw.Done ();
  		}
  	}
	
	private void SetupWindowSize ()
	{
		int width;
		try {
			width = (int) Muine.GConfClient.Get ("/apps/muine/playlist_window/width");
		} catch {
			width = 500;
		}
		
		int height;
		try {
			height = (int) Muine.GConfClient.Get ("/apps/muine/playlist_window/height");
		} catch {
			height = 400;
		}

		SetDefaultSize (width, height);

		SizeAllocated += new SizeAllocatedHandler (HandleSizeAllocated);
	}

	private int last_x = -1;
	private int last_y = -1;

	private bool window_visible;
	public bool WindowVisible {
		set {
			window_visible = value;

			if (window_visible) {
				if (Visible == false && last_x >= 0 && last_y >= 0)
					Move (last_x, last_y);

				Present ();
			} else {
				GetPosition (out last_x, out last_y);

				Visible = false;
			}

			UpdateWindowVisibilityUI ();
		}

		get {
			return window_visible;
		}
	}

	public void UpdateWindowVisibilityUI ()
	{
		if (WindowVisible) {
			if (playlist.Playing != IntPtr.Zero)
				playlist.Select (playlist.Playing);

			((Label) icon.show_window_menu_item.Child).LabelProp = Muine.Catalog.GetString ("Hide _Window");
		} else {
			((Label) icon.show_window_menu_item.Child).LabelProp = Muine.Catalog.GetString ("Show _Window");
		}
	}

	private void SetupButtonsAndMenuItems (Glade.XML glade_xml)
	{
		Image image;

		image = (Image) glade_xml ["previous_image"];
		image.SetFromStock ("muine-previous", IconSize.LargeToolbar);
		image = (Image) glade_xml ["next_image"];
		image.SetFromStock ("muine-next", IconSize.LargeToolbar);
		image = (Image) glade_xml ["add_song_image"];
		image.SetFromStock (Stock.Add, IconSize.LargeToolbar);
		image = (Image) glade_xml ["add_album_image"];
		image.SetFromStock ("muine-add-album", IconSize.LargeToolbar);

		tooltips = new Tooltips ();
		tooltips.SetTip (previous_button,
		                 Muine.Catalog.GetString ("Play the previous song"), null);
		tooltips.SetTip (next_button,
		                 Muine.Catalog.GetString ("Play the next song"), null);
		tooltips.SetTip (glade_xml ["add_album_button"],
		                 Muine.Catalog.GetString ("Add an album to the playlist"), null);
		tooltips.SetTip (glade_xml ["add_song_button"],
			         Muine.Catalog.GetString ("Add a song to the playlist"), null);

		volume_button = new VolumeButton ();
		((Container) glade_xml ["volume_button_container"]).Add (volume_button);
		volume_button.Visible = true;
		volume_button.VolumeChanged += new VolumeButton.VolumeChangedHandler (HandleVolumeChanged);

		tooltips.SetTip (volume_button,
				 Muine.Catalog.GetString ("Change the volume level"), null);

		int vol;
		try {
			vol = (int) Muine.GConfClient.Get ("/apps/muine/volume");
		} catch {
			vol = 50;
		}

		volume_button.Volume = vol;
		player.Volume = vol;

		image = new Image (Stock.Add, IconSize.Menu);
		((ImageMenuItem) glade_xml ["add_song_menu_item"]).Image = image;
		image.Visible = true;
		image = new Image ("muine-add-album", IconSize.Menu);
		((ImageMenuItem) glade_xml ["add_album_menu_item"]).Image = image;
		image.Visible = true;

		play_pause_menu_item_image = new Image ("muine-play", IconSize.Menu);
		play_pause_menu_item.Image = play_pause_menu_item_image;
		play_pause_menu_item_image.Visible = true;
		image = new Image ("muine-previous", IconSize.Menu);
		previous_menu_item.Image = image;
		image.Visible = true;
		image = new Image ("muine-next", IconSize.Menu);
		next_menu_item.Image = image;
		image.Visible = true;

		image = new Image ("muine-rewind", IconSize.Menu);
		skip_backwards_menu_item.Image = image;
		image.Visible = true;
		image = new Image ("muine-forward", IconSize.Menu);
		skip_forward_menu_item.Image = image;
		image.Visible = true;

		image = new Image ("muine-shuffle", IconSize.Menu);
                shuffle_menu_item.Image = image;
                image.Visible = true;

		/* FIXME */
		glade_xml ["information_menu_item_separator"].Visible = false;
		information_menu_item.Visible = false;

		setting_repeat_menu_item = true;
		try {
			repeat_menu_item.Active = (bool) Muine.GConfClient.Get ("/apps/muine/repeat");
		} catch {
			repeat_menu_item.Active = false;
		}
		setting_repeat_menu_item = false;

		/* connect tray icon signals */
		icon.play_pause_menu_item.Activated += new EventHandler (HandlePlayPauseCommand);
		icon.previous_song_menu_item.Activated += new EventHandler (HandlePreviousCommand);
		icon.next_song_menu_item.Activated += new EventHandler (HandleNextCommand);
		icon.play_song_menu_item.Activated += new EventHandler (HandleAddSongCommand);
		icon.play_album_menu_item.Activated += new EventHandler (HandleAddAlbumCommand);
		icon.show_window_menu_item.Activated += new EventHandler (HandleToggleWindowVisibilityCommand);
	}

	private Gdk.Pixbuf empty_pixbuf;

	private CellRenderer pixbuf_renderer;
	private CellRenderer text_renderer;

	private void SetupPlaylist (Glade.XML glade_xml)
	{
		playlist = new HandleView ();

		playlist.Reorderable = true; 
		playlist.Selection.Mode = SelectionMode.Multiple;

		pixbuf_renderer = new ColoredCellRendererPixbuf ();
		playlist.AddColumn (pixbuf_renderer, new HandleView.CellDataFunc (PixbufCellDataFunc), false);

		text_renderer = new CellRendererText ();
		playlist.AddColumn (text_renderer, new HandleView.CellDataFunc (TextCellDataFunc), true);

		playlist.RowActivated += new HandleView.RowActivatedHandler (HandlePlaylistRowActivated);
		playlist.RowsReordered += new HandleView.RowsReorderedHandler (HandlePlaylistRowsReordered);
		playlist.SelectionChanged += new HandleView.SelectionChangedHandler (HandlePlaylistSelectionChanged);

		playlist.Show ();

		((Container) glade_xml ["scrolledwindow"]).Add (playlist);
		
		MarkupUtils.LabelSetMarkup (playlist_label, 0, StringUtils.GetByteLength (Muine.Catalog.GetString ("Playlist")),
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
			new ErrorDialog (String.Format (Muine.Catalog.GetString ("Failed to initialize the audio backend:\n{0}\n\nExiting..."), e.Message));

			Muine.Exit ();
		}

		player.EndOfStreamEvent += new Player.EndOfStreamEventHandler (HandleEndOfStreamEvent);
		player.TickEvent += new Player.TickEventHandler (HandleTickEvent);
		player.StateChanged += new Player.StateChangedHandler (HandleStateChanged);

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
	}

	private void EnsurePlaying ()
	{
		if (playlist.Playing == IntPtr.Zero) {
			if (playlist.HasFirst) {
				playlist.First ();
				playlist.Select (playlist.Playing);
			}

			SongChanged (true);
		} 
	}

	private IntPtr AddSong (Song song)
	{
		return AddSong (song.Handle);
	}

	private IntPtr AddSong (IntPtr p)
	{
		IntPtr new_p = p;

		if (playlist.Contains (p)) {
			Song song = Song.FromHandle (p);
			new_p = song.RegisterExtraHandle ();
		} 
		
		playlist.Append (new_p);

		if (had_last_eos == true) {
			playlist.Playing = new_p;
			playlist.Select (new_p);

			SongChanged (true);
		}

		had_last_eos = false;
		
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
			playlist_label.Text = Muine.Catalog.GetString ("Playlist");

			return;
		}
		
		Song song = Song.FromHandle (playlist.Playing);

		String pos = StringUtils.SecondsToString (time);
		String total = StringUtils.SecondsToString (song.Duration);

		time_label.Text = pos + " / " + total;

		if (repeat_menu_item.Active) {
			long r_seconds = remaining_songs_time;

			if (r_seconds > 6000) { /* 100 minutes */
				int hours = (int) Math.Floor ((double) r_seconds / 3600.0 + 0.5);
				playlist_label.Text = String.Format (Muine.Catalog.GetPluralString ("Playlist (Repeating {0} hour)", "Playlist (Repeating {0} hours)", hours), hours);
			} else if (r_seconds > 60) {
				int minutes = (int) Math.Floor ((double) r_seconds / 60.0 + 0.5);
				playlist_label.Text = String.Format (Muine.Catalog.GetPluralString ("Playlist (Repeating {0} minute)", "Playlist (Repeating {0} minutes)", minutes), minutes);
			} else if (r_seconds > 0) {
				playlist_label.Text = Muine.Catalog.GetString ("Playlist (Repeating)");
			} else {
				playlist_label.Text = Muine.Catalog.GetString ("Playlist");
			}
		} else {
			long r_seconds = remaining_songs_time + song.Duration - time;
			
			if (r_seconds > 6000) { /* 100 minutes */
				int hours = (int) Math.Floor ((double) r_seconds / 3600.0 + 0.5);
				playlist_label.Text = String.Format (Muine.Catalog.GetPluralString ("Playlist ({0} hour remaining)", "Playlist ({0} hours remaining)", hours), hours);
			} else if (r_seconds > 60) {
				int minutes = (int) Math.Floor ((double) r_seconds / 60.0 + 0.5);
				playlist_label.Text = String.Format (Muine.Catalog.GetPluralString ("Playlist ({0} minute remaining)", "Playlist ({0} minutes remaining)", minutes), minutes);
			} else if (r_seconds > 0) {
				playlist_label.Text = Muine.Catalog.GetString ("Playlist (Less than one minute remaining)");
			} else {
				playlist_label.Text = Muine.Catalog.GetString ("Playlist");
			}
		} 
	}

	private void NSongsChanged ()
	{
		bool start_counting;
		remaining_songs_time = 0;

		if (repeat_menu_item.Active)
			start_counting = true;
		else
			start_counting = false;

		foreach (int i in playlist.Contents) {
			IntPtr current = new IntPtr (i);

			if (start_counting == true) {
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
		                        (repeat_menu_item.Active && has_first);

		play_pause_menu_item.Sensitive = previous_button.Sensitive;
		icon.play_pause_menu_item.Sensitive = previous_button.Sensitive;
		previous_menu_item.Sensitive = play_pause_button.Sensitive;
		icon.previous_song_menu_item.Sensitive = play_pause_button.Sensitive;
		next_menu_item.Sensitive = next_button.Sensitive;
		icon.next_song_menu_item.Sensitive = next_button.Sensitive;
		
		skip_to_menu_item.Sensitive = has_first;
		skip_backwards_menu_item.Sensitive = has_first;
		skip_forward_menu_item.Sensitive = has_first;

		information_menu_item.Sensitive = has_first;
		shuffle_menu_item.Sensitive = has_first;

		UpdateTimeLabels (player.Position);

		SavePlaylist (playlist_filename, !repeat_menu_item.Active, true);
	}

	private void SongChanged (bool restart)
	{
		if (playlist.Playing != IntPtr.Zero) {
			Song song = Song.FromHandle (playlist.Playing);

			cover_image.Song = song;

			string tip;
			if (song.Album.Length > 0)
				tip = String.Format (Muine.Catalog.GetString ("From \"{0}\""), song.Album);
			else
				tip = Muine.Catalog.GetString ("Album unknown");
			if (song.Performers.Length > 0)
				tip += "\n\n" + String.Format (Muine.Catalog.GetString ("Performed by {0}"), StringUtils.JoinHumanReadable (song.Performers));
				
			if (song.CoverImage == null && !Muine.CoverDB.Loading)
				tip += "\n\n" + Muine.Catalog.GetString ("Drop an image here to use it as album cover");
			
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

			Title = String.Format (Muine.Catalog.GetString ("{0} - Muine Music Player"), song.Title);

			if (player.Playing)
				icon.Tooltip = artist_label.Text + " - " + title_label.Text;

			if (restart) {
				PlayerChangedSong (song, HasToplevelFocus);
			}

		} else {
			cover_image.Song = null;

			tooltips.SetTip (cover_image, null, null);

			title_label.Text = "";
			artist_label.Text = "";
			time_label.Text = "";

			Title = Muine.Catalog.GetString ("Muine Music Player");

			icon.Tooltip = null;

			if (skip_to_window != null)
				skip_to_window.Hide ();
		}

		MarkupUtils.LabelSetMarkup (title_label, 0, StringUtils.GetByteLength (title_label.Text),
		                            true, true, false);
	}

	private void SelectionChanged ()
	{
		remove_song_menu_item.Sensitive = (playlist.SelectedPointers.Count > 0);
	}

	private new void StateChanged (bool playing)
	{
		if (playing) {
			tooltips.SetTip (play_pause_button, Muine.Catalog.GetString ("Pause music playback"), null);
			play_pause_image.SetFromStock ("muine-pause", IconSize.LargeToolbar);

			play_pause_menu_item_image.SetFromStock ("muine-pause", IconSize.Menu);
			((Label) play_pause_menu_item.Child).LabelProp = Muine.Catalog.GetString ("P_ause");

			icon.play_pause_menu_item_image.SetFromStock ("muine-pause", IconSize.Menu);
			((Label) icon.play_pause_menu_item.Child).LabelProp = Muine.Catalog.GetString ("P_ause");

			icon.Tooltip = artist_label.Text + " - " + title_label.Text;
		} else if (playlist.Playing != IntPtr.Zero &&
		           player.Position > 0 &&
			   !had_last_eos) {
			tooltips.SetTip (play_pause_button, Muine.Catalog.GetString ("Resume music playback"), null);
			play_pause_image.SetFromStock ("muine-play", IconSize.LargeToolbar);

			play_pause_menu_item_image.SetFromStock ("muine-play", IconSize.Menu);
			((Label) play_pause_menu_item.Child).LabelProp = Muine.Catalog.GetString ("Pl_ay");

			icon.play_pause_menu_item_image.SetFromStock ("muine-play", IconSize.Menu);
			((Label) icon.play_pause_menu_item.Child).LabelProp = Muine.Catalog.GetString ("Pl_ay");
			
			icon.Tooltip = null;
		} else {
			tooltips.SetTip (play_pause_button, Muine.Catalog.GetString ("Start music playback"), null);
			play_pause_image.SetFromStock ("muine-play", IconSize.LargeToolbar);

			play_pause_menu_item_image.SetFromStock ("muine-play", IconSize.Menu);
			((Label) play_pause_menu_item.Child).LabelProp = Muine.Catalog.GetString ("Pl_ay");

			icon.play_pause_menu_item_image.SetFromStock ("muine-play", IconSize.Menu);
			((Label) icon.play_pause_menu_item.Child).LabelProp = Muine.Catalog.GetString ("Pl_ay");

			icon.Tooltip = null;
		}

		icon.Playing = playing;

		playlist.Changed (playlist.Playing);
	}

	private void ClearPlaylist ()
	{
		playlist.Clear ();

		player.Stop ();

		had_last_eos = false;
	}

	private void SeekTo (int seconds)
	{
		Song song = Song.FromHandle (playlist.Playing);

		if (seconds >= song.Duration) {
			if (playlist.HasNext ||
			    (repeat_menu_item.Active && playlist.HasFirst))
				HandleNextCommand (null, null);
			else {
				player.Position = song.Duration;

				had_last_eos = true;

				player.Stop ();

				NSongsChanged ();
			}
		} else {
			if (seconds < 0)
				player.Position = 0;
			else
				player.Position = seconds;

			player.Playing = true;
		}

		playlist.Select (playlist.Playing);
	}

	public void OpenPlaylist (string fn)
	{
		VfsStream stream;
		StreamReader reader;
		
		try {
			stream = new VfsStream (fn, FileMode.Open);
			reader = new StreamReader (stream);
		} catch {
			new ErrorDialog (String.Format (Muine.Catalog.GetString ("Failed to open {0} for reading"), FileUtils.HumanReadable (fn)), this);
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

			Song song = (Song) Muine.DB.Songs [line];
			if (song == null) {
				/* not found, lets see if we can find it anyway.. */
				foreach (string key in Muine.DB.Songs.Keys) {
					string key_basename = System.IO.Path.GetFileName (key);

					if (basename == key_basename) {
						song = (Song) Muine.DB.Songs [key];
						break;
					}
				}
			}

			if (song == null) {
				try {
					song = new Song (line);
				} catch {
					song = null;
				}

				if (song != null)
					song.Orphan = true;
			}

			if (song != null) {
				AddSong (song);

				if (playing_song) {
					playlist.Playing = song.Handle;
					playlist.Select (song.Handle);

					SongChanged (true);

					playing_song = false;
				}
			}
		}

		try {
			reader.Close ();
		} catch {
			new ErrorDialog (String.Format (Muine.Catalog.GetString ("Failed to close {0}"), FileUtils.HumanReadable (fn)), this);
			return;
		}

		EnsurePlaying ();

		NSongsChanged ();
	}

	private void SavePlaylist (string fn, bool exclude_played, bool store_playing)
	{
		VfsStream stream;
		StreamWriter writer;
		
		try {
			stream = new VfsStream (fn, FileMode.Create);
			writer = new StreamWriter (stream);
		} catch {
			new ErrorDialog (String.Format (Muine.Catalog.GetString ("Failed to open {0} for writing"), FileUtils.HumanReadable (fn)), this);
			return;
		}

		bool had_playing_song = false;
		foreach (int i in playlist.Contents) {
			IntPtr ptr = new IntPtr (i);

			if (exclude_played) {
				if (ptr == playlist.Playing) {
					had_playing_song = true;

					if (had_last_eos)
						continue;
				}

				if (!had_playing_song)
					continue;
			}
			
			if (store_playing) {
				if (ptr == playlist.Playing)
					writer.WriteLine ("# PLAYING");
			}
			
			Song song = Song.FromHandle (ptr);

			writer.WriteLine (song.Filename);
		}

		try {
			writer.Close ();
		} catch {
			new ErrorDialog (String.Format (Muine.Catalog.GetString ("Failed to close {0}"), FileUtils.HumanReadable (fn)), this);
			return;
		}
	}

	private void HandleStateChanged (bool playing)
	{
		StateChanged (playing);
	}

	private void HandleWindowStateEvent (object o, WindowStateEventArgs args)
	{
		if (!Visible)
			return;
			
		bool old_window_visible = window_visible;
		window_visible = ((args.Event.NewWindowState != Gdk.WindowState.Iconified) &&
				  (args.Event.NewWindowState != Gdk.WindowState.Withdrawn));

		if (old_window_visible != window_visible)
			UpdateWindowVisibilityUI ();
	}

	private void HandleWindowVisibilityNotifyEvent (object o, VisibilityNotifyEventArgs args)
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

	private void HandleDeleteEvent (object o, DeleteEventArgs args)
	{
		Muine.Exit ();
	}

	private void HandleSizeAllocated (object o, SizeAllocatedArgs args)
	{
		int width, height;

		GetSize (out width, out height);

		Muine.GConfClient.Set ("/apps/muine/playlist_window/width", width);
		Muine.GConfClient.Set ("/apps/muine/playlist_window/height", height);
	}

	private void HandleVolumeChanged (int vol)
	{
		player.Volume = vol;

		Muine.GConfClient.Set ("/apps/muine/volume", vol);
	}

	private void HandleToggleWindowVisibilityCommand (object o, EventArgs args)
	{
		WindowVisible = !WindowVisible;
	}

	private bool had_last_eos;

	private void HandleQueueSongsEvent (List songs)
	{
		foreach (int i in songs)
			AddSong (new IntPtr (i));

		EnsurePlaying ();

		NSongsChanged ();
	}

	public void PlayFile (string file)
	{
		Song song = (Song) Muine.DB.Songs [file];

		if (song == null) {
			/* try to create a new song object */
			try {
				song = new Song (file);
			} catch {
				return;
			}

			song.Orphan = true;
		}

		if (song == null)
			return;

		IntPtr p = AddSong (song);

		playlist.Playing = p;
		playlist.Select (p);

		SongChanged (true);

		player.Playing = true;

		EnsurePlaying ();

		NSongsChanged ();
	}

	public void QueueFile (string file)
	{
		Song song = (Song) Muine.DB.Songs [file];

		if (song == null) {
			/* try to create a new song object */
			try {
				song = new Song (file);
			} catch {
				return;
			}

			song.Orphan = true;
		}

		if (song == null)
			return;

		IntPtr p = AddSong (song);

		EnsurePlaying ();

		NSongsChanged ();
	}

	private void HandlePlaySongsEvent (List songs)
	{
		bool first = true;
		foreach (int i in songs) {
			IntPtr p = new IntPtr (i);
			
			IntPtr new_p = AddSong (p);
			
			if (first == true) {
				playlist.Playing = new_p;
				playlist.Select (new_p);

				SongChanged (true);

				player.Playing = true;
		
				first = false;
			}
		}

		EnsurePlaying ();

		NSongsChanged ();
	}

	private void HandleQueueAlbumsEvent (List albums)
	{
		foreach (int i in albums) {
			Album a = Album.FromHandle (new IntPtr (i));

			foreach (Song s in a.Songs) {
				AddSong (s);
			}
		}

		EnsurePlaying ();

		NSongsChanged ();
	}

	private void HandlePlayAlbumsEvent (List albums)
	{
		bool first = true;
		foreach (int i in albums) {
			Album a = Album.FromHandle (new IntPtr (i));

			foreach (Song s in a.Songs) {
				IntPtr new_p = AddSong (s);

				if (first == true) {
					playlist.Playing = new_p;
					playlist.Select (new_p);

					SongChanged (true);

					player.Playing = true;
		
					first = false;
				}
			}
		}

		EnsurePlaying ();

		NSongsChanged ();
	}

	private void HandleTickEvent (int pos)
	{
		UpdateTimeLabels (pos);
	}

	private void HandleEndOfStreamEvent ()
	{
		Song song = Song.FromHandle (playlist.Playing);

		if (song.Duration != player.Position) {
			song.Duration = player.Position;

			Muine.DB.UpdateSong (song);
		}
		
		if (playlist.HasNext) {
			playlist.Next ();

			SongChanged (true);
		} else {
			if (repeat_menu_item.Active) {
				playlist.First ();

				SongChanged (true);
			} else {
				had_last_eos = true;

				player.Stop ();
			}
		}

		NSongsChanged ();
	}

	private void HandlePreviousCommand (object o, EventArgs args)
	{
		if (!playlist.HasFirst)
			return;

		had_last_eos = false;

		/* restart song if not in the first 3 seconds */
		if (player.Position < 3 &&
		    playlist.HasPrevious) {
			playlist.Previous ();

			SongChanged (true);

			NSongsChanged ();
		} else if (player.Position < 3 &&
		           !playlist.HasPrevious &&
			   repeat_menu_item.Active) {
			playlist.Last ();

			SongChanged (true);

			NSongsChanged ();
		} else {
			player.Position = 0;
		}

		playlist.Select (playlist.Playing);

		player.Playing = true;
	}

	private void HandlePlayPauseCommand (object o, EventArgs args)
	{
		if (!playlist.HasFirst)
			return;

		if (had_last_eos) {
			playlist.First ();
			playlist.Select (playlist.Playing);

			SongChanged (true);

			NSongsChanged ();

			had_last_eos = false;
		}

		player.Playing = !player.Playing;
	}

	private void HandleStopCommand (object o, EventArgs args)
	{
		if (!playlist.HasFirst)
			return;

		player.Playing = false;
	}

	private void HandleNextCommand (object o, EventArgs args)
	{
		if (playlist.HasNext)
			playlist.Next ();
		else if (repeat_menu_item.Active && playlist.HasFirst)
			playlist.First ();
		else
			return;

		playlist.Select (playlist.Playing);

		SongChanged (true);

		NSongsChanged ();

		player.Playing = true;
	}

	private void HandleSkipToCommand (object o, EventArgs args)
	{
		playlist.Select (playlist.Playing);

		if (skip_to_window == null)
			skip_to_window = new SkipToWindow (this, player);

		skip_to_window.Run ();
	}

	private void HandleSkipBackwardsCommand (object o, EventArgs args)
	{
		SeekTo (player.Position - 5);
	}

	private void HandleSkipForwardCommand (object o, EventArgs args)
	{
		SeekTo (player.Position + 5);
	}

	private void HandleInformationCommand (object o, EventArgs args)
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
	}

	private void HandleAddSongCommand (object o, EventArgs args)
	{
		if (add_song_window == null) {
			add_song_window = new AddSongWindow ();

			add_song_window.QueueSongsEvent += new AddSongWindow.QueueSongsEventHandler (HandleQueueSongsEvent);
			add_song_window.PlaySongsEvent += new AddSongWindow.PlaySongsEventHandler (HandlePlaySongsEvent);
		}

		add_song_window.Run ();
		
		AddChildWindowIfVisible (add_song_window);
	}

	private void HandleAddAlbumCommand (object o, EventArgs args)
	{
		if (add_album_window == null) {
			add_album_window = new AddAlbumWindow ();
			
			add_album_window.QueueAlbumsEvent += new AddAlbumWindow.QueueAlbumsEventHandler (HandleQueueAlbumsEvent);
			add_album_window.PlayAlbumsEvent += new AddAlbumWindow.PlayAlbumsEventHandler (HandlePlayAlbumsEvent);
		}

		add_album_window.Run ();

		AddChildWindowIfVisible (add_album_window);
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

			song = (Song) Muine.DB.Songs [finfo.FullName];
			if (song == null) {
				bool ret = pw.ReportFile (finfo.Name);
				if (ret == false)
					return false;

				try {
					song = new Song (finfo.FullName);
				} catch {
					continue;
				}

				Muine.DB.AddSong (song);
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
			if (ret == false)
				return false;
		}

		return true;
	}

	private void HandleImportFolderCommand (object o, EventArgs args) 
	{
		FileChooserDialog fc;

		fc = new FileChooserDialog (Muine.Catalog.GetString ("Import Folder"), this,
					    FileChooserAction.SelectFolder);
		fc.LocalOnly = true;
		fc.AddButton (Stock.Cancel, ResponseType.Cancel);
		fc.AddButton (Muine.Catalog.GetString ("_Import"), ResponseType.Ok);
		fc.DefaultResponse = ResponseType.Ok;
		
		string start_dir;
		try {
			start_dir = (string) Muine.GConfClient.Get ("/apps/muine/default_import_folder");
		} catch {
			start_dir = "~";
		}

		start_dir.Replace ("~", Environment.GetEnvironmentVariable ("HOME"));

		if (start_dir.EndsWith ("/") == false)
			start_dir += "/";

		fc.SetCurrentFolderUri (start_dir);

		if (fc.Run () != (int) ResponseType.Ok) {
			fc.Destroy ();

			return;
		}

		fc.Visible = false;

		string res = StringUtils.LocalPathFromUri (fc.Uri);

		Muine.GConfClient.Set ("/apps/muine/default_import_folder", res);

		DirectoryInfo dinfo = new DirectoryInfo (res);
			
		if (dinfo.Exists) {
			ProgressWindow pw = new ProgressWindow (this, dinfo.Name);

			Muine.DB.AddWatchedFolder (dinfo.FullName);
			HandleDirectory (dinfo, pw);

			pw.Done ();
		}

		fc.Destroy ();
	}

	private void HandleOpenPlaylistCommand (object o, EventArgs args)
	{
		FileSelector sel = new FileSelector (Muine.Catalog.GetString ("Open Playlist"),
						     this, FileChooserAction.Open,
						     "/apps/muine/default_playlist_folder");

		FileFilter filter = new FileFilter ();
		filter.Name = Muine.Catalog.GetString ("Playlist files");
		filter.AddMimeType ("audio/x-mpegurl");
		filter.AddPattern ("*.m3u");
		sel.AddFilter (filter);

		string fn = sel.GetFile ();

		if (fn.Length == 0 || !FileUtils.IsPlaylist (fn))
			return;

		if (FileUtils.Exists (fn))
			OpenPlaylist (fn);
	}

	private void HandleSavePlaylistAsCommand (object o, EventArgs args)
	{
		FileSelector sel = new FileSelector (Muine.Catalog.GetString ("Save Playlist"),
						     this, FileChooserAction.Save,
						     "/apps/muine/default_playlist_folder");
		sel.CurrentName = Muine.Catalog.GetString ("Untitled");

		string fn = sel.GetFile ();

		if (fn.Length == 0)
			return;

		/* make sure the extension is ".m3u" */
		if (!FileUtils.IsPlaylist (fn))
			fn += ".m3u";

		if (FileUtils.Exists (fn)) {
			YesNoDialog d = new YesNoDialog (String.Format (Muine.Catalog.GetString ("File {0} will be overwritten.\nIf you choose yes, the contents will be lost.\n\nDo you want to continue?"), FileUtils.HumanReadable (fn)), this);
			if (d.GetAnswer () == true)
				SavePlaylist (fn, false, false);
		} else
			SavePlaylist (fn, false, false);
	}

	private void HandleRemoveSongCommand (object o, EventArgs args)
	{
		List selected_pointers = playlist.SelectedPointers;

		bool have_only_one = (selected_pointers.Count == 1);
		
		foreach (int i in selected_pointers) {
			IntPtr sel = new IntPtr (i);

			if (sel == playlist.Playing) {
				had_last_eos = false;

				if (playlist.HasNext)
					playlist.Next ();
				else if (playlist.HasPrevious)
					playlist.Previous ();
				else {
					playlist.Playing = IntPtr.Zero;

					player.Stop ();
				}

				SongChanged (true);
			}
			
			if (have_only_one) {
				if (!playlist.SelectNext (false, false))
					playlist.SelectPrevious (false, false);
			}

			RemoveSong (sel);
		}

		NSongsChanged ();
	}

	private void HandleRemovePlayedSongsCommand (object o, EventArgs args)
	{
		if (playlist.Playing == IntPtr.Zero)
			return;

		if (had_last_eos) {
			ClearPlaylist ();
			SongChanged (true);
			NSongsChanged ();
			return;
		}

		foreach (int i in playlist.Contents) {
			IntPtr current = new IntPtr (i);

			if (current == playlist.Playing)
				break;

			RemoveSong (current);
		}

		playlist.Select (playlist.Playing);

		NSongsChanged ();
	}

	private void HandleClearPlaylistCommand (object o, EventArgs args)
	{
		ClearPlaylist ();
		SongChanged (true);
		NSongsChanged ();
	}

	private void HandleRepeatCommand (object o, EventArgs args)
	{
		if (setting_repeat_menu_item)
			return;

		Muine.GConfClient.Set ("/apps/muine/repeat", repeat_menu_item.Active);

		NSongsChanged ();
	}

	private int ShuffleFunc (IntPtr a, IntPtr b)
	{
		Song songa = Song.FromHandle (a);
		Song songb = Song.FromHandle (b);

		int res = (int) Math.Round (songa.RandomSortKey - songb.RandomSortKey);

		return res;
	}

	private void HandleShuffleCommand (object o, EventArgs args)
	{
		Random rand = new Random ();

		foreach (int i in playlist.Contents) {
			Song song = Song.FromHandle ((IntPtr) i);

			if (i == (int) playlist.Playing)
				song.RandomSortKey = -1;
			else
				song.RandomSortKey = rand.NextDouble ();
		}

		playlist.Sort (new HandleView.CompareFunc (ShuffleFunc));

		NSongsChanged ();

		if (playlist.Playing != IntPtr.Zero)
			playlist.Select (playlist.Playing);
	}

	private void HandleHideWindowCommand (object o, EventArgs args)
	{
		WindowVisible = false;
	}

	private void HandlePlaylistRowActivated (IntPtr handle)
	{
		had_last_eos = false;

		playlist.Playing = handle;

		SongChanged (true);

		NSongsChanged ();

		player.Playing = true;
	}

	private void HandlePlaylistRowsReordered ()
	{
		NSongsChanged ();
	}

	private void HandlePlaylistSelectionChanged ()
	{
		SelectionChanged ();
	}

	private void HandleQuitCommand (object o, EventArgs args)
	{
		Muine.Exit ();
	}

	private void HandleAboutCommand (object o, EventArgs args)
	{
		About.ShowWindow (this);
	}

	private void HandleSongChanged (Song song)
	{
		bool n_songs_changed = false;
		
		foreach (IntPtr h in song.Handles) {
			if (!playlist.Contains (h))
				continue;

			n_songs_changed = true;
			
			if (h == playlist.Playing)
				SongChanged (false);

			playlist.Changed (h);
		}
		
		if (n_songs_changed)
			NSongsChanged ();
	}

	private void HandleSongRemoved (Song song)
	{
		bool n_songs_changed = false;
		
		foreach (IntPtr h in song.Handles) {
			if (!playlist.Contains (h))
				continue;

			n_songs_changed = true;
			
			if (h == playlist.Playing) {
				if (playlist.HasNext)
					playlist.Next ();
				else if (playlist.HasPrevious)
					playlist.Previous ();
				else {
					playlist.Playing = IntPtr.Zero;

					player.Stop ();
				}

				SongChanged (true);
			}

			if ((playlist.SelectedPointers.Count == 1) &&
                            ((int) playlist.SelectedPointers [0] == (int) h)) {
				if (!playlist.SelectNext (false, false))
                        		playlist.SelectPrevious (false, false);
			}

			playlist.Remove (h);
		}
		
		if (n_songs_changed)
			NSongsChanged ();
	}

	public void AddChildWindowIfVisible (Window window)
	{
		if (WindowVisible)
			window.TransientFor = this;
		else
			window.TransientFor = null;
	}
}
