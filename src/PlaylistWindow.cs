/*
 * Copyright © 2003, 2004 Jorn Baayen <jorn@nl.linux.org>
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
using GtkSharp;
using GLib;

public class PlaylistWindow : Window
{
	/* widgets */
	[Glade.Widget]
	private Menu file_menu;
	[Glade.Widget]
	private ImageMenuItem play_pause_menu_item;
	private Image play_pause_menu_item_image;
	[Glade.Widget]
	private ImageMenuItem previous_menu_item;
	private Image previous_menu_item_image;
	[Glade.Widget]
	private ImageMenuItem next_menu_item;
	private Image next_menu_item_image;
	[Glade.Widget]
	private ImageMenuItem skip_to_menu_item;
	[Glade.Widget]
	private ImageMenuItem skip_backwards_menu_item;
	private Image skip_backwards_menu_item_image;
	[Glade.Widget]
	private ImageMenuItem skip_forward_menu_item;
	private Image skip_forward_menu_item_image;
	[Glade.Widget]
	private ImageMenuItem remove_song_menu_item;
	[Glade.Widget]
	private ImageMenuItem volume_up_menu_item;
	private Image volume_up_menu_item_image;
	[Glade.Widget]
	private ImageMenuItem volume_down_menu_item;
	private Image volume_down_menu_item_image;
	[Glade.Widget]
	private Button previous_button;
	[Glade.Widget]
	private Button play_pause_button;
	[Glade.Widget]
	private Button next_button;
	[Glade.Widget]
	private Image play_pause_image;
	[Glade.Widget]
	private Button add_album_button;
	[Glade.Widget]
	private ImageMenuItem add_song_menu_item;
	private Image add_song_menu_item_image;
	[Glade.Widget]
	private ImageMenuItem add_album_menu_item;
	private Image add_album_menu_item_image;
	[Glade.Widget]
	private Button add_song_button;
	[Glade.Widget]
	private Image cover_image;
	[Glade.Widget]
	private EventBox cover_ebox;
	[Glade.Widget]
	private Container title_label_container;
	private EllipsizingLabel title_label;
	[Glade.Widget]
	private Container artist_label_container;
	private EllipsizingLabel artist_label;
	[Glade.Widget]
	private Label time_label;
	[Glade.Widget]
	private ScrolledWindow scrolledwindow;
	[Glade.Widget]
	private Label playlist_label;
	[Glade.Widget]
	private CheckButton random_check;
	private Tooltips tooltips;
	private HandleView playlist;
	[Glade.Widget]
	private Container volume_button_container;
	private VolumeButton volume_button;
	private Glade.XML gxml;
	private NotificationAreaIcon icon;

	private Player player;

	SkipToWindow skip_to_window;

	AddSongWindow add_song_window;
	AddAlbumWindow add_album_window;

	public PlaylistWindow () : base ("Muine Music Player")
	{
		gxml = new Glade.XML (null, "PlaylistWindow.glade", "main_vbox", null);
		gxml.Autoconnect (this);
			
		Add (gxml ["main_vbox"]);

		AddAccelGroup (file_menu.AccelGroup);

		KeyPressEvent += new KeyPressEventHandler (HandleWindowKeyPressEvent);

		SetupWindowSize ();
		SetupPlayer ();
		SetupButtonsAndMenuItems ();
		SetupPlaylist ();

		icon = new NotificationAreaIcon ();
		icon.ActivateEvent += new NotificationAreaIcon.ActivateEventHandler (HandleNotificationAreaIconActivateEvent);

		skip_to_window = new SkipToWindow (this);
		skip_to_window.SeekEvent += new SkipToWindow.SeekEventHandler (HandleSeekEvent);

		add_song_window = new AddSongWindow (this);
		add_song_window.QueueSongsEvent += new AddSongWindow.QueueSongsEventHandler (HandleQueueSongsEvent);
		add_song_window.PlaySongsEvent += new AddSongWindow.PlaySongsEventHandler (HandlePlaySongsEvent);
		
		add_album_window = new AddAlbumWindow (this);
		add_album_window.QueueAlbumsEvent += new AddAlbumWindow.QueueAlbumsEventHandler (HandleQueueAlbumsEvent);
		add_album_window.PlayAlbumsEvent += new AddAlbumWindow.PlayAlbumsEventHandler (HandlePlayAlbumsEvent);
			
		SongChanged ();
		SelectionChanged ();
		HandleStateChanged (false);

		/* show */
		try {
			Visible = (bool) Muine.GConfClient.Get ("/apps/muine/playlist_window/visible");
		} catch {
			Visible = true;
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

	private int last_x;
	private int last_y;

	private void SetWindowVisible (bool visible)
	{
		if (visible == false) {
			GetPosition (out last_x, out last_y);

			Visible = false;
		} else {
			Move (last_x, last_y);

			if (playlist.Playing != IntPtr.Zero)
				playlist.ScrollTo (playlist.Playing);

			Visible = true;

			Present ();
		}

		Muine.GConfClient.Set ("/apps/muine/playlist_window/visible", visible);
	}

	private void SetupButtonsAndMenuItems ()
	{
		Image image;
		image = (Image) gxml ["previous_image"];
		image.SetFromStock ("muine-previous", IconSize.LargeToolbar);
		image = (Image) gxml ["next_image"];
		image.SetFromStock ("muine-next", IconSize.LargeToolbar);
		image = (Image) gxml ["add_song_image"];
		image.SetFromStock (Stock.Add, IconSize.LargeToolbar);
		image = (Image) gxml ["add_album_image"];
		image.SetFromStock ("muine-add-album", IconSize.LargeToolbar);

		tooltips = new Tooltips ();
		tooltips.SetTip (previous_button, "Play the last song", null);
		tooltips.SetTip (next_button, "Play the next song", null);
		tooltips.SetTip (add_album_button, "Add an album to the playlist", null);
		tooltips.SetTip (add_song_button, "Add a song to the playlist", null);

		volume_button = new VolumeButton ();
		volume_button_container.Add (volume_button);
		volume_button.Visible = true;
		volume_button.VolumeChanged += new VolumeButton.VolumeChangedHandler (HandleVolumeChanged);

		tooltips.SetTip (volume_button, "Change the volume level", null);

		int vol;
		try {
			vol = (int) Muine.GConfClient.Get ("/apps/muine/volume");
		} catch {
			vol = 50;
		}

		volume_button.Volume = vol;
		
		add_song_menu_item_image = new Image (Stock.Add, IconSize.Menu);
		add_song_menu_item.Image = add_song_menu_item_image;
		add_song_menu_item_image.Visible = true;
		add_album_menu_item_image = new Image ("muine-add-album", IconSize.Menu);
		add_album_menu_item.Image = add_album_menu_item_image;
		add_album_menu_item_image.Visible = true;

		play_pause_menu_item_image = new Image ("muine-play", IconSize.Menu);
		play_pause_menu_item.Image = play_pause_menu_item_image;
		play_pause_menu_item_image.Visible = true;
		previous_menu_item_image = new Image ("muine-previous", IconSize.Menu);
		previous_menu_item.Image = previous_menu_item_image;
		previous_menu_item_image.Visible = true;
		next_menu_item_image = new Image ("muine-next", IconSize.Menu);
		next_menu_item.Image = next_menu_item_image;
		next_menu_item_image.Visible = true;

		skip_backwards_menu_item_image = new Image ("muine-rewind", IconSize.Menu);
		skip_backwards_menu_item.Image = skip_backwards_menu_item_image;
		skip_backwards_menu_item_image.Visible = true;
		skip_forward_menu_item_image = new Image ("muine-forward", IconSize.Menu);
		skip_forward_menu_item.Image = skip_forward_menu_item_image;
		skip_forward_menu_item_image.Visible = true;

		volume_up_menu_item_image = new Image ("muine-volume-medium", IconSize.Menu);
		volume_up_menu_item.Image = volume_up_menu_item_image;
		volume_up_menu_item_image.Visible = true;
		volume_down_menu_item_image = new Image ("muine-volume-min", IconSize.Menu);
		volume_down_menu_item.Image = volume_down_menu_item_image;
		volume_down_menu_item_image.Visible = true;
	}

	private Gdk.Pixbuf playing_pixbuf;
	private Gdk.Pixbuf paused_pixbuf;
	private Gdk.Pixbuf empty_pixbuf;

	private CellRenderer pixbuf_renderer;
	private CellRenderer text_renderer;

	private void SetupPlaylist ()
	{
		playlist = new HandleView ();

		playlist.Reorderable = true; 
		playlist.KeepSelection = true;
		playlist.Selection.Mode = SelectionMode.Single;

		pixbuf_renderer = new ColoredCellRendererPixbuf ();
		playlist.AddColumn (pixbuf_renderer, new HandleView.CellDataFunc (PixbufCellDataFunc));

		text_renderer = new CellRendererText ();
		playlist.AddColumn (text_renderer, new HandleView.CellDataFunc (TextCellDataFunc));

		playlist.RowActivated += new HandleView.RowActivatedHandler (HandlePlaylistRowActivated);
		playlist.RowsReordered += new HandleView.RowsReorderedHandler (HandlePlaylistRowsReordered);
		playlist.SelectionChanged += new HandleView.SelectionChangedHandler (HandlePlaylistSelectionChanged);

		playlist.Show ();

		scrolledwindow.Add (playlist);
		
		MarkupUtils.LabelSetMarkup (playlist_label, 0, (uint) "Playlist".Length,
		                            false, true, false);

		playing_pixbuf = new Gdk.Pixbuf (null, "muine-playing.png");
		paused_pixbuf = new Gdk.Pixbuf (null, "muine-paused.png");
		empty_pixbuf = new Gdk.Pixbuf (null, "muine-nothing.png");
	}

	private void PixbufCellDataFunc (HandleView view, CellRenderer cell, IntPtr handle)
	{
		ColoredCellRendererPixbuf r = (ColoredCellRendererPixbuf) cell;

		if (handle == view.Playing) {
			if (player.Playing)
				r.Pixbuf = playing_pixbuf;
			else
				r.Pixbuf = paused_pixbuf;
		} else {
			r.Pixbuf = empty_pixbuf;
		}
	}

	private void TextCellDataFunc (HandleView view, CellRenderer cell, IntPtr handle)
	{
		Song song = Song.FromHandle (handle);
		CellRendererText r = (CellRendererText) cell;

		string title = String.Join (", ", song.Titles);

		r.Text = title + "\n" + String.Join (", ", song.Artists);

		MarkupUtils.CellSetMarkup (r, 0, StringUtils.GetByteLength (title),
		                           false, true, false);
	}

	private void SetupPlayer ()
	{
		try {
			player = new Player ();
		} catch {
			new ErrorDialog ("Failed to create the required GStreamer elements.\nExiting...");

			Environment.Exit (0);
		}

		player.EndOfStreamEvent += new Player.EndOfStreamEventHandler (HandleEndOfStreamEvent);
		player.TickEvent += new Player.TickEventHandler (HandleTickEvent);
		player.StateChanged += new Player.StateChangedHandler (HandleStateChanged);

		title_label = new EllipsizingLabel ("");
		title_label.Visible = true;
		title_label.Xalign = 0.0f;
		title_label_container.Add (title_label);

		artist_label = new EllipsizingLabel ("");
		artist_label.Visible = true;
		artist_label.Xalign = 0.0f;
		artist_label_container.Add (artist_label);
	}

	private void AddSong (Song song)
	{
		AddSong (song.Handle);
	}

	private void AddSong (IntPtr p)
	{
		had_last_eos = false;

		if (playlist.Contains (p)) {
			Song song = Song.FromHandle (p);
			playlist.Append (song.RegisterExtraHandle ());
		} else {
			playlist.Append (p);
		}
		
		if (playlist.Playing == IntPtr.Zero) {
			playlist.First ();

			SongChanged ();
		}
	}

	private void RemoveSong (IntPtr p)
	{
		playlist.Remove (p);

		Song song = Song.FromHandle (p);

		if (song.IsExtraHandle (p))
			song.UnregisterExtraHandle (p);
	}

	private static string SecondsToString (long time)
	{
		int h, m, s;

		h = (int) (time / 3600);
		m = (int) ((time % 3600) / 60);
		s = (int) ((time % 3600) % 60);

		if (h > 0) {
			return h + ":" + m.ToString ("d2") + ":" + s.ToString ("d2");
		} else {
			return m + ":" + s.ToString ("d2");
		}
	}

	private long remaining_songs_time;

	private void UpdateTimeLabels (long time)
	{
		if (playlist.Playing == IntPtr.Zero) {
			time_label.Text = "";
			playlist_label.Text = "Playlist";

			return;
		}
		
		Song song = Song.FromHandle (playlist.Playing);

		String pos = SecondsToString (time / 1000);
		String total = SecondsToString (song.Duration / 1000);

		time_label.Text = pos + " / " + total;

		long r_seconds = (remaining_songs_time + song.Duration - (int) player.Position) / 1000;

		if (r_seconds > 6000) { /* 100 minutes */
			int hours = (int) Math.Floor ((double) r_seconds / 3600.0 + 0.5);
			playlist_label.Text = "Playlist (" + hours + " hours remaining)";
		} else if (r_seconds > 60) {
			int minutes = (int) Math.Floor ((double) r_seconds / 60.0 + 0.5);
			playlist_label.Text = "Playlist (" + minutes + " minutes remaining)";
		} else if (r_seconds > 0) {
			playlist_label.Text = "Playlist (Less than one minute remaining)";
		} else {
			playlist_label.Text = "Playlist";
		}
	}

	private void NSongsChanged ()
	{
		bool start_counting = false;
		remaining_songs_time = 0;

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
		next_button.Sensitive = playlist.HasNext;

		play_pause_menu_item.Sensitive = has_first;
		previous_menu_item.Sensitive = has_first;
		next_menu_item.Sensitive = playlist.HasNext;
		
		skip_to_menu_item.Sensitive = has_first;
		skip_backwards_menu_item.Sensitive = has_first;
		skip_forward_menu_item.Sensitive = has_first;

		UpdateTimeLabels (player.Position);
	}

	private void SongChanged ()
	{
		if (playlist.Playing != IntPtr.Zero) {
			Song song = Song.FromHandle (playlist.Playing);

			if (song.CoverImage != null)
				cover_image.FromPixbuf = song.CoverImage;
			else
				cover_image.FromPixbuf = new Gdk.Pixbuf (null, "muine-default-cover.png");

			if (song.Album.Length > 0 && song.Year.Length > 0)
				tooltips.SetTip (cover_ebox, song.Album + " (" + song.Year + ")", null);
			else if (song.Album.Length > 0)
				tooltips.SetTip (cover_ebox, song.Album, null);
			else if (song.Year.Length > 0)
				tooltips.SetTip (cover_ebox, "Album unknown (" + song.Year + ")", null);
			else
				tooltips.SetTip (cover_ebox, null, null);

			title_label.Text = String.Join (", ", song.Titles);
			artist_label.Text = String.Join (", ", song.Artists);

			player.Song = song;

			Title = title_label.Text + " - Muine Music Player";

			icon.Tooltip = artist_label.Text + " - " + title_label.Text;
		} else {
			cover_image.FromPixbuf = new Gdk.Pixbuf (null, "muine-default-cover.png");

			tooltips.SetTip (cover_ebox, null, null);

			title_label.Text = "";
			artist_label.Text = "";
			time_label.Text = "";

			Title = "Muine Music Player";

			icon.Tooltip = "Not playing";
		}

		MarkupUtils.LabelSetMarkup (title_label, 0, StringUtils.GetByteLength (title_label.Text),
		                            true, true, false);

		NSongsChanged ();
		UpdateTimeLabels (0);
	}

	private void SelectionChanged ()
	{
		remove_song_menu_item.Sensitive = (playlist.SelectedPointers.Count > 0);
	}

	private void SeekTo (long seconds)
	{
		Song song = Song.FromHandle (playlist.Playing);

		if (seconds >= song.Duration) {
			if (playlist.HasNext)
				HandleNextCommand (null, null);
			else {
				player.Position = song.Duration;

				player.Playing = false;

				had_last_eos = true;
			}
		} else {
			if (seconds < 0)
				player.Position = 0;
			else
				player.Position = seconds;

			player.Playing = true;
		}
	}

	private void HandleStateChanged (bool playing)
	{
		if (playing) {
			tooltips.SetTip (play_pause_button, "Pause music playback", null);
			play_pause_image.SetFromStock ("muine-pause", IconSize.LargeToolbar);

			play_pause_menu_item_image.SetFromStock ("muine-pause", IconSize.Menu);
			((Label) play_pause_menu_item.Child).LabelProp = "P_ause";
		} else if (playlist.Playing != IntPtr.Zero &&
		           player.Position > 0 &&
			   !had_last_eos) {
			tooltips.SetTip (play_pause_button, "Resume music playback", null);
			play_pause_image.SetFromStock ("muine-play", IconSize.LargeToolbar);

			play_pause_menu_item_image.SetFromStock ("muine-play", IconSize.Menu);
			((Label) play_pause_menu_item.Child).LabelProp = "Pl_ay";
		} else {
			tooltips.SetTip (play_pause_button, "Start music playback", null);
			play_pause_image.SetFromStock ("muine-play", IconSize.LargeToolbar);

			play_pause_menu_item_image.SetFromStock ("muine-play", IconSize.Menu);
			((Label) play_pause_menu_item.Child).LabelProp = "Pl_ay";
		}

		playlist.StateChanged ();
	}

	/* FIXME this is not called anymore with Gtk# 0.15? */
	private void HandleWindowKeyPressEvent (object o, KeyPressEventArgs args)
	{
		if (KeyUtils.HaveModifier (args.Event.state)) {
			args.RetVal = false;
			return;
		}

		args.RetVal = true;

		switch (args.Event.keyval) {
		case 0x020: /* space */
			if (playlist.HasFirst)
				HandlePlayPauseCommand (null, null);
			break;
		case 0x061: /* a */
		case 0x041: /* A */
			HandleAddAlbumCommand (null, null);
			break;
		case 0x070: /* p */
		case 0x050: /* P */
			if (playlist.HasFirst)
				HandlePreviousCommand (null, null);
			break;
		case 0x06e: /* n */
		case 0x04e: /* N */
			if (playlist.HasNext)
				HandleNextCommand (null, null);
			break;
		case 0x073: /* s */
		case 0x053: /* S */
			HandleAddSongCommand (null, null);
			break;
		case 0xFF51: /* left */
			if (playlist.HasFirst)
				HandleSkipBackwardsCommand (null, null);
			break;
		case 0xFF53: /* right */
			if (playlist.HasFirst)
				HandleSkipForwardCommand (null, null);
			break;
		default:
			args.RetVal = false;
			break;
		}
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

		bool up_sensitive = true, down_sensitive = true;

		if (vol == 0) {
			down_sensitive = false;
		} else if (vol == 100) {
			up_sensitive = false;
		}

		volume_up_menu_item.Sensitive = up_sensitive;
		volume_down_menu_item.Sensitive = down_sensitive;
	}

	private void HandleNotificationAreaIconActivateEvent ()
	{
		SetWindowVisible (!Visible);
	}

	private bool had_last_eos;

	private void HandleSeekEvent (int sec)
	{
		if (playlist.HasFirst == false)
			return;

		SeekTo (sec * 1000);
	}

	private void HandleQueueSongsEvent (List songs)
	{
		foreach (int i in songs)
			AddSong (new IntPtr (i));

		NSongsChanged ();
	}

	private void HandlePlaySongsEvent (List songs)
	{
		bool first = true;
		foreach (int i in songs) {
			IntPtr p = new IntPtr (i);
			
			AddSong (p);
			
			if (first == true) {
				playlist.Playing = p;
				playlist.ScrollTo (p);
				player.Playing = true;

				SongChanged ();
		
				first = false;
			}
		}

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

		NSongsChanged ();
	}

	private void HandlePlayAlbumsEvent (List albums)
	{
		bool first = true;
		foreach (int i in albums) {
			Album a = Album.FromHandle (new IntPtr (i));

			foreach (Song s in a.Songs) {
				AddSong (s);

				if (first == true) {
					playlist.Playing = s.Handle;
					playlist.ScrollTo (s.Handle);
					player.Playing = true;

					SongChanged ();
		
					first = false;
				}
			}
		}

		NSongsChanged ();
	}

	private void HandleTickEvent (long pos)
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

			SongChanged ();
		} else {
			had_last_eos = true;

			player.Playing = false;

			/* propagate the song.Duration changes */
			UpdateTimeLabels (player.Position);
		}
	}

	private void HandlePreviousCommand (object o, EventArgs args)
	{
		had_last_eos = false;

		/* restart song if not in the first 3 seconds */
		if ((player.Position < 3000) || !playlist.HasPrevious) {
			playlist.Previous ();

			SongChanged ();
		} else
			player.Position = 0;

		player.Playing = true;
	}

	private void HandlePlayPauseCommand (object o, EventArgs args)
	{
		if (had_last_eos) {
			playlist.First ();

			SongChanged ();

			had_last_eos = false;
		}
		
		player.Playing = !player.Playing;
	}

	private void HandleNextCommand (object o, EventArgs args)
	{
		playlist.Next ();

		SongChanged ();

		player.Playing = true;
	}

	private void HandleSkipToCommand (object o, EventArgs args)
	{
		skip_to_window.Run ();
	}

	private void HandleSkipBackwardsCommand (object o, EventArgs args)
	{
		SeekTo (player.Position - 5000);
	}

	private void HandleSkipForwardCommand (object o, EventArgs args)
	{
		SeekTo (player.Position + 5000);
	}

	private void HandleVolumeUpCommand (object o, EventArgs args)
	{
		int new_vol = player.Volume + 10;
		if (new_vol > 100)
			new_vol = 100;
		
		volume_button.Volume = new_vol;
	}

	private void HandleVolumeDownCommand (object o, EventArgs args)
	{
		int new_vol = player.Volume - 10;
		if (new_vol < 0)
			new_vol = 0;
			
		volume_button.Volume = new_vol;
	}

	private void HandleAddSongCommand (object o, EventArgs args)
	{
		add_song_window.Run ();
	}

	private void HandleAddAlbumCommand (object o, EventArgs args)
	{
		add_album_window.Run ();
	}

	private bool HandleDirectory (DirectoryInfo info,
				      ProgressWindow pw,
				      bool add_to_playlist)
	{
		foreach (FileInfo finfo in info.GetFiles ()) {
			Song song;

			song = Muine.DB.SongFromFile (finfo.ToString ());
			if (song == null) {
				bool ret = pw.ReportFile (finfo.Name);
				if (ret == false)
					return false;

				try {
					song = new Song (finfo.ToString ());
				} catch (Exception e) {
					continue;
				}

				Muine.DB.AddSong (song);
			}

			if (add_to_playlist)
				AddSong (song);
		}

		foreach (DirectoryInfo dinfo in info.GetDirectories ()) {
			bool ret = HandleDirectory (dinfo, pw, add_to_playlist);
			if (ret == false)
				return false;
		}

		return true;
	}

	private void HandleImportFolderCommand (object o, EventArgs args)
	{
		FileSelection fs;
		
		fs = new FileSelection ("Choose a Folder");
		fs.HideFileopButtons ();
		fs.HistoryPulldown.Visible = false;
		fs.FileList.Parent.Visible = false;
		fs.SetDefaultSize (350, 250);

		CheckButton check = new CheckButton ("_Add to Playlist");
		check.Visible = true;
		((Dialog) fs).VBox.PackEnd (check, false, false, 0);

		string start_dir;
		try {
			start_dir = (string) Muine.GConfClient.Get ("/apps/muine/default_import_folder");
		} catch {
			start_dir = "~";
		}

		if (start_dir == "~")
			start_dir = Environment.GetEnvironmentVariable ("HOME");

		if (start_dir.EndsWith ("/") == false)
			start_dir += "/";

		fs.Filename = start_dir;

		if (fs.Run () != (int) ResponseType.Ok) {
			fs.Destroy ();

			return;
		}

		bool add_to_playlist = check.Active;

		fs.Visible = false;

		Muine.GConfClient.Set ("/apps/muine/default_import_folder", fs.Filename);

		DirectoryInfo dinfo = new DirectoryInfo (fs.Filename);
			
		if (dinfo.Exists) {
			ProgressWindow pw = new ProgressWindow (this, dinfo.Name);
			HandleDirectory (dinfo, pw, add_to_playlist);
			pw.Done ();
		}

		fs.Destroy ();

		if (add_to_playlist)
			NSongsChanged ();
	}

	private void HandleRemoveSongCommand (object o, EventArgs args)
	{
		foreach (int i in playlist.SelectedPointers) {
			IntPtr sel = new IntPtr (i);

			if (sel == playlist.Playing) {
				if (playlist.HasNext)
					playlist.Next ();
				else if (playlist.HasPrevious)
					playlist.Previous ();
				else {
					playlist.Playing = IntPtr.Zero;

					player.Playing = false;
				}

				SongChanged ();
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
			HandleClearPlaylistCommand (null, null);
			return;
		}

		foreach (int i in playlist.Contents) {
			IntPtr current = new IntPtr (i);

			if (current == playlist.Playing)
				break;

			RemoveSong (current);
		}
	}

	private void HandleClearPlaylistCommand (object o, EventArgs args)
	{
		playlist.Clear ();

		player.Playing = false;

		SongChanged ();

		NSongsChanged ();
	}

	private void HandleHideWindowCommand (object o, EventArgs args)
	{
		SetWindowVisible (false);
	}

	private void HandlePlaylistRowActivated (IntPtr handle)
	{
		playlist.Playing = handle;
		player.Playing = true;

		SongChanged ();
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
		//Application.Quit ();
		Environment.Exit (0);
	}

	private void HandleAboutCommand (object o, EventArgs args)
	{
		About.ShowWindow (this);
	}
}
