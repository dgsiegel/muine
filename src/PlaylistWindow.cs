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
	public enum TargetType {
		UriList,
		Uri,
		SongList,
		AlbumList,
		ModelRow
	};

	private static TargetEntry [] drag_entries = new TargetEntry [] {
		new TargetEntry ("text/uri-list", 0, (uint) TargetType.UriList)
	};

	private static TargetEntry [] playlist_source_entries = new TargetEntry [] {
		new TargetEntry ("MUINE_TREE_MODEL_ROW", TargetFlags.Widget, (uint) TargetType.ModelRow),
		new TargetEntry ("text/uri-list", 0, (uint) TargetType.UriList)
 	};
		
	private static TargetEntry [] playlist_dest_entries = new TargetEntry [] {
		new TargetEntry ("MUINE_TREE_MODEL_ROW", TargetFlags.Widget, (uint) TargetType.ModelRow),
		new TargetEntry ("MUINE_SONG_LIST", TargetFlags.App, (uint) TargetType.SongList),
		new TargetEntry ("MUINE_ALBUM_LIST", TargetFlags.App, (uint) TargetType.AlbumList),
		new TargetEntry ("text/uri-list", 0, (uint) TargetType.UriList)
	};

	public delegate void PlayerChangedSongHandler (Song song, bool hasfocus);

	/* Event to notify when song changed: plugins like
	   Dashboard can use this.  */
	public event PlayerChangedSongHandler PlayerChangedSong;

	/* Actions */
	private const string ui_info = 
		"  <menubar name=\"MenuBar\">\n" +
		"    <menu action=\"FileMenu\">\n" +
		"      <menuitem action=\"ImportFolder\" />\n" +
		"      <separator />\n" +
		"      <menuitem action=\"OpenPlaylist\" />\n" +
		"      <menuitem action=\"SavePlaylistAs\" />\n" +
		"      <separator />\n" +
		"      <menuitem action=\"HideWindow\" />\n" +
		"      <separator />\n" +
		"      <menuitem action=\"Quit\" />\n" +
		"    </menu>\n" +
		"    <menu action=\"SongMenu\">\n" +
		"      <menuitem action=\"PlayPause\" />\n" +
		"      <separator />\n" +
		"      <menuitem action=\"PreviousSong\" />\n" +
		"      <menuitem action=\"NextSong\" />\n" +
		"      <separator />\n" +
		"      <menuitem action=\"SkipTo\" />\n" +
		"      <menuitem action=\"SkipBackwards\" />\n" +
		"      <menuitem action=\"SkipForward\" />\n" +
		"    </menu>\n" +
		"    <menu action=\"PlaylistMenu\">\n" +
		"      <menuitem action=\"PlaySong\" />\n" +
		"      <menuitem action=\"PlayAlbum\" />\n" +
		"      <separator />\n" +
		"      <menuitem action=\"RemoveSong\" />\n" +
		"      <menuitem action=\"RemovePlayedSongs\" />\n" +
		"      <menuitem action=\"ClearPlaylist\" />\n" +
		"      <separator />\n" +
		"      <menuitem action=\"Repeat\" />\n" +
		"      <menuitem action=\"Shuffle\" />\n" +
		"    </menu>\n" +
		"    <menu action=\"HelpMenu\">\n" +
		"      <menuitem action=\"About\" />\n" +
		"    </menu>\n" +
		"  </menubar>\n" +
		"  <popup name=\"NotificationAreaIconMenu\">\n" +
		"    <menuitem action=\"PlayPause\" />\n" +
		"    <separator />\n" +
		"    <menuitem action=\"PreviousSong\" />\n" +
		"    <menuitem action=\"NextSong\" />\n" +
		"    <separator />\n" +
		"    <menuitem action=\"PlaySong\" />\n" +
		"    <menuitem action=\"PlayAlbum\" />\n" +
		"    <separator />\n" +
		"    <menuitem action=\"ShowHideWindow\" />\n" +
		"  </popup>\n";

	public PlaylistWindow () : base (WindowType.Toplevel)
	{
		/* build the interface */
		Glade.XML glade_xml = new Glade.XML (null, "PlaylistWindow.glade", "main_vbox", null);
		glade_xml.Autoconnect (this);
			
		Add (glade_xml ["main_vbox"]);

		/* set up menus */
		ActionEntry [] action_entries = new ActionEntry [] {
			new ActionEntry ("FileMenu", null, Muine.Catalog.GetString ("_File"),
			                 null, null, null),
			new ActionEntry ("SongMenu", null, Muine.Catalog.GetString ("_Song"),
			                 null, null, null),
			new ActionEntry ("PlaylistMenu", null, Muine.Catalog.GetString ("_Playlist"),
			                 null, null, null),
			new ActionEntry ("HelpMenu", null, Muine.Catalog.GetString ("_Help"),
			                 null, null, null),
			new ActionEntry ("ImportFolder", Stock.Execute, Muine.Catalog.GetString ("_Import Folder..."),
			                 null, null,
					 new EventHandler (HandleImportFolderCommand)),
			new ActionEntry ("OpenPlaylist", Stock.Open, Muine.Catalog.GetString ("_Open Playlist..."),
			                 "<control>O", null,
					 new EventHandler (HandleOpenPlaylistCommand)),
			new ActionEntry ("SavePlaylistAs", Stock.SaveAs, Muine.Catalog.GetString ("_Save Playlist As..."),
			                 "<shift><control>S", null,
					 new EventHandler (HandleSavePlaylistAsCommand)),
			new ActionEntry ("HideWindow", null, Muine.Catalog.GetString ("_Hide Window"),
			                 "Escape", null,
					 new EventHandler (HandleHideWindowCommand)),
			new ActionEntry ("Quit", Stock.Quit, null,
			                 "<control>Q", null,
					 new EventHandler (HandleQuitCommand)),
			new ActionEntry ("PreviousSong", "stock_media-prev", Muine.Catalog.GetString ("_Previous"),
			                 "P", null,
					 new EventHandler (HandlePreviousCommand)),
			new ActionEntry ("NextSong", "stock_media-next", Muine.Catalog.GetString ("_Next"),
			                 "N", null,
					 new EventHandler (HandleNextCommand)),
			new ActionEntry ("SkipTo", Stock.JumpTo, Muine.Catalog.GetString ("_Skip to..."),
			                 "T", null,
					 new EventHandler (HandleSkipToCommand)),
			new ActionEntry ("SkipBackwards", "stock_media-rew", Muine.Catalog.GetString ("Skip _Backwards"),
			                 "<control>Left", null,
					 new EventHandler (HandleSkipBackwardsCommand)),
			new ActionEntry ("SkipForward", "stock_media-fwd",
			                 Muine.Catalog.GetString ("Skip _Forward"), "<control>Right", null,
					 new EventHandler (HandleSkipForwardCommand)),
			new ActionEntry ("PlaySong", Stock.Add, Muine.Catalog.GetString ("Play _Song..."),
			                 "S", null,
					 new EventHandler (HandleAddSongCommand)),
			new ActionEntry ("PlayAlbum", "gnome-dev-cdrom-audio", Muine.Catalog.GetString ("Play _Album..."),
			                 "A", null,
					 new EventHandler (HandleAddAlbumCommand)),
			new ActionEntry ("RemoveSong", Stock.Remove, Muine.Catalog.GetString ("_Remove Song"),
			                 "Delete", null,
					 new EventHandler (HandleRemoveSongCommand)),
			new ActionEntry ("RemovePlayedSongs", null, Muine.Catalog.GetString ("Remove _Played Songs"),
			                 "<control>Delete", null,
					 new EventHandler (HandleRemovePlayedSongsCommand)),
			new ActionEntry ("ClearPlaylist", Stock.Clear, Muine.Catalog.GetString ("_Clear"),
			                 null, null,
					 new EventHandler (HandleClearPlaylistCommand)),
			new ActionEntry ("Shuffle", "stock_shuffle", Muine.Catalog.GetString ("Shu_ffle"),
			                 "<control>S", null,
					 new EventHandler (HandleShuffleCommand)),
			new ActionEntry ("About", Gnome.Stock.About, Muine.Catalog.GetString ("_About"),
			                 null, null,
					 new EventHandler (HandleAboutCommand)),
			new ActionEntry ("ShowHideWindow", null, "",
			                 null, null,
					 new EventHandler (HandleToggleWindowVisibilityCommand))
		};

		ToggleActionEntry [] toggle_action_entries = new ToggleActionEntry [] {
			new ToggleActionEntry ("PlayPause", "stock_media-play", Muine.Catalog.GetString ("_Playing"),
					       "space", null,
					       new EventHandler (HandlePlayPauseCommand), false),
			new ToggleActionEntry ("Repeat", null, Muine.Catalog.GetString ("R_epeat"),
			                       "<control>R", null,
					       new EventHandler (HandleRepeatCommand), false)
		};
	
		ActionGroup group = new ActionGroup ("Actions");
		group.Add (action_entries);
		group.Add (toggle_action_entries);

		previous_action = group.GetAction ("PreviousSong");
		next_action = group.GetAction ("NextSong");
		skip_to_action = group.GetAction ("SkipTo");
		skip_forward_action = group.GetAction ("SkipForward");
		skip_backwards_action = group.GetAction ("SkipBackwards");
		remove_song_action = group.GetAction ("RemoveSong");
		shuffle_action = group.GetAction ("Shuffle");
		window_visibility_action = group.GetAction ("ShowHideWindow");
		play_pause_action = (ToggleAction) group.GetAction ("PlayPause");
		repeat_action = (ToggleAction) group.GetAction ("Repeat");
		
		UIManager uim = new UIManager ();
		uim.InsertActionGroup (group, 0);
		uim.AddUiFromString (ui_info);

		AddAccelGroup (uim.AccelGroup);
		
		((Box) glade_xml ["menu_bar_box"]).Add (uim.GetWidget ("/MenuBar"));

		/* hook up window signals */
		WindowStateEvent += new WindowStateEventHandler (HandleWindowStateEvent);
		DeleteEvent += new DeleteEventHandler (HandleDeleteEvent);
		DragDataReceived += new DragDataReceivedHandler (HandleDragDataReceived);
		Gtk.Drag.DestSet (this, DestDefaults.All,
				  drag_entries, Gdk.DragAction.Copy);

		/* keep track of window visibility */
		VisibilityNotifyEvent += new VisibilityNotifyEventHandler (HandleWindowVisibilityNotifyEvent);
		AddEvents ((int) Gdk.EventMask.VisibilityNotifyMask);

		icon = new NotificationAreaIcon ((Menu) uim.GetWidget ("/NotificationAreaIconMenu"),
						 window_visibility_action);

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

			window_visibility_action.Label = Muine.Catalog.GetString ("Hide _Window");
		} else {
			window_visibility_action.Label = Muine.Catalog.GetString ("Show _Window");
		}
	}

	private void SetupButtonsAndMenuItems (Glade.XML glade_xml)
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
		                 Muine.Catalog.GetString ("Toggle music playback"), null);
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

		block_repeat_action = true;
		try {
			repeat_action.Active = (bool) Muine.GConfClient.Get ("/apps/muine/repeat");
		} catch {
			repeat_action.Active = false;
		}
		block_repeat_action = false;
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

		playlist.RowActivated += new HandleView.RowActivatedHandler (HandlePlaylistRowActivated);
		playlist.SelectionChanged += new HandleView.SelectionChangedHandler (HandlePlaylistSelectionChanged);

		playlist.EnableModelDragSource (Gdk.ModifierType.Button1Mask,
						playlist_source_entries,
						Gdk.DragAction.Copy | Gdk.DragAction.Link | Gdk.DragAction.Move);
		playlist.EnableModelDragDest (playlist_dest_entries,
					      Gdk.DragAction.Copy | Gdk.DragAction.Move);

		playlist.DragDataGet += new DragDataGetHandler (HandlePlaylistDragDataGet);
		playlist.DragDataReceived += new DragDataReceivedHandler (HandlePlaylistDragDataReceived);

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
		return AddSongAtPos (p, IntPtr.Zero, TreeViewDropPosition.Before);
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

		if (repeat_action.Active) {
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

		if (repeat_action.Active)
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
		                        (repeat_action.Active && has_first);

		play_pause_action.Sensitive = previous_button.Sensitive;
		previous_action.Sensitive = play_pause_button.Sensitive;
		next_action.Sensitive = next_button.Sensitive;
		
		skip_to_action.Sensitive = has_first;
		skip_backwards_action.Sensitive = has_first;
		skip_forward_action.Sensitive = has_first;

		shuffle_action.Sensitive = has_first;

		UpdateTimeLabels (player.Position);

		SavePlaylist (playlist_filename, !repeat_action.Active, true);
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
		remove_song_action.Sensitive = (playlist.SelectedPointers.Count > 0);
	}

	private new void StateChanged (bool playing)
	{
		if (playing) {
			block_play_pause_action = true;
			play_pause_action.Active = true;
			play_pause_button.Active = true;
			block_play_pause_action = false;

			icon.Tooltip = artist_label.Text + " - " + title_label.Text;
		} else {
			block_play_pause_action = true;
			play_pause_action.Active = false;
			play_pause_button.Active = false;
			block_play_pause_action = false;

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
			    (repeat_action.Active && playlist.HasFirst))
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

	private Song GetSingleSong (string file)
	{
		Song song = (Song) Muine.DB.Songs [file];

		if (song == null) {
			/* try to create a new song object */
			try {
				song = new Song (file);
			} catch {
				return null;
			}

			song.Orphan = true;
		}

		return song;
	}

	public void PlayFile (string file)
	{
		Song song = GetSingleSong (file);

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
		Song song = GetSingleSong (file);

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
			if (repeat_action.Active) {
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
			   repeat_action.Active) {
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
		if (!playlist.HasFirst || block_play_pause_action)
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
		else if (repeat_action.Active && playlist.HasFirst)
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
		if (block_repeat_action)
			return;

		Muine.GConfClient.Set ("/apps/muine/repeat", repeat_action.Active);

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

	private void HandlePlaylistDragDataGet (object o, DragDataGetArgs args)
	{
		List songs = playlist.SelectedPointers;

		switch (args.Info) {
		case (uint) TargetType.UriList:
			string files = "";

			foreach (int p in songs) {
				IntPtr s = new IntPtr (p);
				files += StringUtils.UriFromLocalPath (Song.FromHandle (s).Filename) + "\r\n";
			}
	
			args.SelectionData.Set (Gdk.Atom.Intern ("text/uri-list", false),
						8, System.Text.Encoding.UTF8.GetBytes (files));
						
			break;	
		case (uint) TargetType.ModelRow:
			string ptrs = "\tMUINE_TREE_MODEL_ROW\t";

			foreach (int p in songs) {
				IntPtr s = new IntPtr (p);
				ptrs += s.ToString () + "\r\n";
			}
			
			args.SelectionData.Set (Gdk.Atom.Intern ("MUINE_TREE_MODEL_ROW", false),
					        8, System.Text.Encoding.ASCII.GetBytes (ptrs));
					
			break;
		default:
			break;	
		}
	}

	private void HandlePlaylistDragDataReceived (object o, DragDataReceivedArgs args)
	{
		IntPtr pos_ptr = IntPtr.Zero;
		TreePath path;
		TreeViewDropPosition pos;

		if (playlist.GetDestRowAtPos (args.X, args.Y, out path, out pos))
			pos_ptr = playlist.GetHandleFromPath (path);

		string data = StringUtils.SelectionDataToString (args.SelectionData);

		uint type = (uint) TargetType.UriList;

		/* work around gtk bug .. */
		if (data.StartsWith ("\tMUINE_TREE_MODEL_ROW\t")) {
			type = (uint) TargetType.ModelRow;
			data = data.Substring ("\tMUINE_TREE_MODEL_ROW\t".Length);
		} else if (data.StartsWith ("\tMUINE_SONG_LIST\t")) {
			type = (uint) TargetType.SongList;
			data = data.Substring ("\tMUINE_SONG_LIST\t".Length);
		} else if (data.StartsWith ("\tMUINE_ALBUM_LIST\t")) {
			type = (uint) TargetType.AlbumList;
			data = data.Substring ("\tMUINE_ALBUM_LIST\t".Length);
		}

		bool first = true;
		
		switch (type) {
		case (uint) TargetType.SongList:
		case (uint) TargetType.ModelRow:
			string [] sngs = Regex.Split (data, "\r\n");

			foreach (string newsong in sngs) {
				IntPtr new_ptr;

				try { 
					new_ptr = new IntPtr (Int64.Parse (newsong)); 
				} catch { 	
					continue;
				}

				Song song = Song.FromHandle (new_ptr);

				bool play = false;

				if (type == (uint) TargetType.ModelRow) {
					if (new_ptr == pos_ptr)
						break;

					if (new_ptr == playlist.Playing)
						play = true;
					
					RemoveSong (new_ptr);
				}
					
				if (pos_ptr != IntPtr.Zero)
					new_ptr = AddSongAtPos (song.Handle, pos_ptr, pos);
				else
					new_ptr = AddSong (song.Handle);

				pos_ptr = new_ptr;
				pos = TreeViewDropPosition.After;
				
				if (play) {
					playlist.Playing = new_ptr;
				}
				
				if (first) {
					if (type == (uint) TargetType.ModelRow) {
						/* scroll if the first/last & moved, because it will have scrolled out of
						   view during the move - hack, hack, hack :( */
						playlist.Select (new_ptr, playlist.IsFirst (new_ptr) ||
									  playlist.IsLast (new_ptr));
					} else
						playlist.Select (new_ptr);

					first = false;
				}
			}

			EnsurePlaying ();

			NSongsChanged ();

			break;
		case (uint) TargetType.AlbumList:
			string [] albms = Regex.Split (data, "\r\n");

			foreach (string newalbum in albms) {
				IntPtr new_ptr;
				
				try {
					new_ptr = new IntPtr (Int64.Parse (newalbum));
				} catch {
					continue;
				}
				
				Album album = Album.FromHandle (new_ptr);
				
				foreach (Song song in album.Songs) {
					if (pos_ptr != IntPtr.Zero)
						new_ptr = AddSongAtPos (song.Handle, pos_ptr, pos);
					else
						new_ptr = AddSong (song.Handle);

					pos_ptr = new_ptr;
					pos = TreeViewDropPosition.After;
					
					if (first) {
						playlist.Select (new_ptr);

						first = false;
					}
				}	
			}
			
			EnsurePlaying ();

			NSongsChanged ();

			break;
		case (uint) TargetType.UriList:
			string [] uri_list = Regex.Split (data, "\r\n");
			
			foreach (string s in uri_list) {
				string fn = StringUtils.LocalPathFromUri (s);

				if (fn == null) {
					Drag.Finish (args.Context, false, false, args.Time);
					
					return;
				}
	
				DirectoryInfo dinfo = new DirectoryInfo (fn);
				
				if (dinfo.Exists) {
					ProgressWindow pw = new ProgressWindow (this, dinfo.Name);
		
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
						first = false;
					} else {
						Song song = GetSingleSong (finfo.FullName);
					
						if (song != null) {
							IntPtr new_ptr;

							if (pos_ptr != IntPtr.Zero)
								new_ptr = AddSongAtPos (song.Handle, pos_ptr, pos);
							else
								new_ptr = AddSong (song.Handle);

							pos_ptr = new_ptr;
							pos = TreeViewDropPosition.After;
					
							if (first) {
								playlist.Select (new_ptr);
	
								first = false;
							}
						}
					}

					EnsurePlaying ();

					NSongsChanged ();
				}
			}

			break;
		default:
			break;
		}

		Drag.Finish (args.Context, true, false, args.Time);
	}
}
