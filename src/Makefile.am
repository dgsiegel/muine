CSC = $(MCS) $(MCS_FLAGS)

MUINE_CSFILES =					\
	$(srcdir)/AddSongWindow.cs		\
	$(srcdir)/AddAlbumWindow.cs		\
	$(srcdir)/GroupWindow.cs		\
	$(srcdir)/Muine.cs			\
	$(srcdir)/NotificationAreaIcon.cs	\
	$(srcdir)/PlaylistWindow.cs		\
	$(srcdir)/Song.cs			\
	$(srcdir)/Album.cs			\
	$(srcdir)/SongDatabase.cs		\
	$(srcdir)/About.cs			\
	$(srcdir)/Metadata.cs			\
	$(srcdir)/Player.cs			\
	$(srcdir)/FileSelector.cs		\
	$(srcdir)/StringUtils.cs		\
	$(srcdir)/KeyUtils.cs			\
	$(srcdir)/SkipToWindow.cs		\
	$(srcdir)/SearchMusicWindow.cs		\
	$(srcdir)/ProgressWindow.cs		\
	$(srcdir)/ErrorDialog.cs		\
	$(srcdir)/YesNoDialog.cs		\
	$(srcdir)/VolumeButton.cs		\
	$(srcdir)/HandleView.cs			\
	$(srcdir)/StockIcons.cs			\
	$(srcdir)/ColoredCellRendererPixbuf.cs	\
	$(srcdir)/EllipsizingLabel.cs		\
	$(srcdir)/MarkupUtils.cs		\
	$(srcdir)/AmazonSearchService.cs	\
	$(srcdir)/CoverDatabase.cs

ASSEMBLIES =					\
	/r:atk-sharp				\
	/r:gconf-sharp				\
	/r:gdk-sharp				\
	/r:glade-sharp				\
	/r:glib-sharp				\
	/r:gnome-sharp				\
	/r:gtk-sharp				\
	/r:AmazonSearchService.dll		\
	/r:System.Web.Services			\
	/r:pango-sharp

RESOURCES =											\
	/resource:$(top_srcdir)/data/glade/PlaylistWindow.glade,PlaylistWindow.glade		\
	/resource:$(top_srcdir)/data/glade/SkipToWindow.glade,SkipToWindow.glade		\
	/resource:$(top_srcdir)/data/glade/SearchMusicWindow.glade,SearchMusicWindow.glade	\
	/resource:$(top_srcdir)/data/glade/ErrorDialog.glade,ErrorDialog.glade			\
	/resource:$(top_srcdir)/data/glade/YesNoDialog.glade,YesNoDialog.glade			\
	/resource:$(top_srcdir)/data/glade/AddWindow.glade,AddWindow.glade			\
	/resource:$(top_srcdir)/data/glade/ProgressWindow.glade,ProgressWindow.glade		\
	/resource:$(top_srcdir)/data/images/muine-previous.png,muine-previous.png		\
	/resource:$(top_srcdir)/data/images/muine-previous-16.png,muine-previous-16.png		\
	/resource:$(top_srcdir)/data/images/muine-play.png,muine-play.png			\
	/resource:$(top_srcdir)/data/images/muine-play-16.png,muine-play-16.png			\
	/resource:$(top_srcdir)/data/images/muine-pause.png,muine-pause.png			\
	/resource:$(top_srcdir)/data/images/muine-pause-16.png,muine-pause-16.png		\
	/resource:$(top_srcdir)/data/images/muine-next.png,muine-next.png			\
	/resource:$(top_srcdir)/data/images/muine-next-16.png,muine-next-16.png			\
	/resource:$(top_srcdir)/data/images/muine-rewind.png,muine-rewind.png			\
	/resource:$(top_srcdir)/data/images/muine-forward.png,muine-forward.png			\
	/resource:$(top_srcdir)/data/images/muine-queue.png,muine-queue.png			\
	/resource:$(top_srcdir)/data/images/muine-add-album.png,muine-add-album.png		\
	/resource:$(top_srcdir)/data/images/muine-add-album-16.png,muine-add-album-16.png	\
	/resource:$(top_srcdir)/data/images/muine-groups.png,muine-groups.png			\
	/resource:$(top_srcdir)/data/images/muine-playing.png,muine-playing.png			\
	/resource:$(top_srcdir)/data/images/muine-paused.png,muine-paused.png			\
	/resource:$(top_srcdir)/data/images/muine-nothing.png,muine-nothing.png			\
	/resource:$(top_srcdir)/data/images/muine-tray.png,muine-tray.png			\
	/resource:$(top_srcdir)/data/images/muine-default-cover.png,muine-default-cover.png	\
	/resource:$(top_srcdir)/data/images/muine-volume-zero.png,muine-volume-zero.png		\
	/resource:$(top_srcdir)/data/images/muine-volume-min.png,muine-volume-min.png		\
	/resource:$(top_srcdir)/data/images/muine-volume-medium.png,muine-volume-medium.png	\
	/resource:$(top_srcdir)/data/images/muine-volume-max.png,muine-volume-max.png		\
	/resource:$(top_srcdir)/data/images/muine-playlist.png,muine-playlist.png

muinelibdir = $(prefix)/lib/muine

AmazonSearchService.dll: AmazonSearchService.cs
	$(CSC) -target:library $(LIBFLAGS) -out:AmazonSearchService.dll $(srcdir)/AmazonSearchService.cs -r:System.Web.Services

muine.exe: $(MUINE_CSFILES) AmazonSearchService.dll
	$(CSC) -o $@ $(CSFLAGS) $(MUINE_CSFILES) $(ASSEMBLIES) $(RESOURCES)

all: AmazonSearchService.dll muine.exe

install-data-local: muine.exe AmazonSearchService.dll
	$(mkinstalldirs) $(DESTDIR)$(muinelibdir)
	$(INSTALL_DATA) muine.exe $(DESTDIR)$(muinelibdir)/muine.exe
	$(INSTALL_DATA) AmazonSearchService.dll $(DESTDIR)$(muinelibdir)/AmazonSearchService.dll

bin_SCRIPTS = muine

EXTRA_DIST =					\
	$(MUINE_CSFILES)			\
	muine.in

CLEANFILES =					\
	muine					\
	AmazonSearchService.dll			\
	muine.exe
