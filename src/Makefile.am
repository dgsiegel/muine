CSC = $(MCS) -codepage:utf8 $(MCS_FLAGS)

MUINE_CSFILES =					\
	$(srcdir)/ActionThread.cs		\
	$(srcdir)/AddSongWindow.cs		\
	$(srcdir)/AddAlbumWindow.cs		\
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
	$(srcdir)/NoMusicFoundWindow.cs		\
	$(srcdir)/ProgressWindow.cs		\
	$(srcdir)/ErrorDialog.cs		\
	$(srcdir)/YesNoDialog.cs		\
	$(srcdir)/VolumeButton.cs		\
	$(srcdir)/HandleView.cs			\
	$(srcdir)/StockIcons.cs			\
	$(srcdir)/ColoredCellRendererPixbuf.cs	\
	$(srcdir)/EllipsizingLabel.cs		\
	$(srcdir)/MarkupUtils.cs		\
	$(srcdir)/CoverDatabase.cs		\
	$(srcdir)/DashboardFrontend.cs		\
	$(srcdir)/MessageConnection.cs		\
	$(srcdir)/GnomeProxy.cs			\
	$(srcdir)/InfoWindow.cs			\
	$(srcdir)/CoverImage.cs			\
	$(srcdir)/MmKeys.cs			\
	$(srcdir)/GettextCatalog.cs		\
	$(srcdir)/FileUtils.cs

ASSEMBLIES =					\
	-r:AmazonSearchService.dll		\
	-r:System.Web.Services			\
	-r:Mono.Posix
	

RESOURCES =												\
	/resource:$(top_srcdir)/data/glade/PlaylistWindow.glade,PlaylistWindow.glade			\
	/resource:$(top_srcdir)/data/glade/SkipToWindow.glade,SkipToWindow.glade			\
	/resource:$(top_srcdir)/data/glade/NoMusicFoundWindow.glade,NoMusicFoundWindow.glade		\
	/resource:$(top_srcdir)/data/glade/ErrorDialog.glade,ErrorDialog.glade				\
	/resource:$(top_srcdir)/data/glade/YesNoDialog.glade,YesNoDialog.glade				\
	/resource:$(top_srcdir)/data/glade/AddWindow.glade,AddWindow.glade				\
	/resource:$(top_srcdir)/data/glade/ProgressWindow.glade,ProgressWindow.glade			\
	/resource:$(top_srcdir)/data/glade/InfoWindow.glade,InfoWindow.glade				\
	/resource:$(top_srcdir)/data/images/muine-add-album.png,muine-add-album.png			\
	/resource:$(top_srcdir)/data/images/muine-add-album-16.png,muine-add-album-16.png		\
	/resource:$(top_srcdir)/data/images/muine-playing.png,muine-playing.png				\
	/resource:$(top_srcdir)/data/images/muine-paused.png,muine-paused.png				\
	/resource:$(top_srcdir)/data/images/muine-nothing.png,muine-nothing.png				\
	/resource:$(top_srcdir)/data/images/muine-tray-playing.png,muine-tray-playing.png		\
	/resource:$(top_srcdir)/data/images/muine-tray-paused.png,muine-tray-paused.png			\
	/resource:$(top_srcdir)/data/images/muine-default-cover.png,muine-default-cover.png		\
	/resource:$(top_srcdir)/data/images/muine-cover-downloading.png,muine-cover-downloading.png	\
	/resource:$(top_srcdir)/data/images/muine-about.png,muine-about.png				\
	/resource:$(top_srcdir)/data/images/muine.png,muine.png						

# FIXME MONO_XMLSERIALIZER_THS=no works around Ximian Bugzilla #58194
AmazonSearchService.cs: AmazonSearchService.wsdl
	MONO_XMLSERIALIZER_THS=no wsdl $(srcdir)/AmazonSearchService.wsdl

AmazonSearchService.dll: AmazonSearchService.cs
	$(CSC) -target:library $(LIBFLAGS) -out:AmazonSearchService.dll AmazonSearchService.cs -r:System.Web.Services

muine.exe: $(MUINE_CSFILES) AmazonSearchService.dll
	$(CSC) -o $@ $(CSFLAGS) $(MUINE_CSFILES) $(ASSEMBLIES) $(RESOURCES) $(GTKSHARP_LIBS)

all: AmazonSearchService.dll muine.exe

muinelibdir = $(prefix)/lib/muine
muinelib_DATA = muine.exe muine.exe.config AmazonSearchService.dll

bin_SCRIPTS = muine

EXTRA_DIST =					\
	$(MUINE_CSFILES)			\
	AmazonSearchService.wsdl		\
	muine.in				\
	muine.exe.config.in

CLEANFILES =					\
	muine					\
	AmazonSearchService.cs			\
	AmazonSearchService.dll			\
	muine.exe
