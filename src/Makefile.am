CSC = $(MCS) -codepage:utf8 $(MCS_FLAGS) $(CSFLAGS)

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
	$(srcdir)/GnomeProxy.cs			\
	$(srcdir)/CoverImage.cs			\
	$(srcdir)/MmKeys.cs			\
	$(srcdir)/FileUtils.cs			\
	$(srcdir)/SignalUtils.cs		\
	$(srcdir)/Database.cs			\
	$(srcdir)/MuineDBusService.cs		\
	$(srcdir)/PluginManager.cs		\
	$(srcdir)/AddWindow.cs			\
	$(srcdir)/AmazonSearchService.cs	\
	$(srcdir)/Defines.cs

MUINE_ASSEMBLIES =					\
	-r:$(top_builddir)/PluginLib/muine-plugin.dll	\
	-r:$(top_builddir)/DBusLib/muine-dbus.dll	\
	-r:System.Web.Services				\
	-r:Mono.Posix
	
MUINE_RESOURCES =												\
	-resource:$(top_srcdir)/data/glade/PlaylistWindow.glade,PlaylistWindow.glade			\
	-resource:$(top_srcdir)/data/glade/SkipToWindow.glade,SkipToWindow.glade			\
	-resource:$(top_srcdir)/data/glade/ErrorDialog.glade,ErrorDialog.glade				\
	-resource:$(top_srcdir)/data/glade/YesNoDialog.glade,YesNoDialog.glade				\
	-resource:$(top_srcdir)/data/glade/AddWindow.glade,AddWindow.glade				\
	-resource:$(top_srcdir)/data/glade/ProgressWindow.glade,ProgressWindow.glade			\
	-resource:$(top_srcdir)/data/ui/PlaylistWindow.xml,PlaylistWindow.xml				\
	-resource:$(top_srcdir)/data/ui/NotificationAreaIcon.xml,NotificationAreaIcon.xml		\
	-resource:$(top_srcdir)/data/images/muine-playing.png,muine-playing.png				\
	-resource:$(top_srcdir)/data/images/muine-paused.png,muine-paused.png				\
	-resource:$(top_srcdir)/data/images/muine-volume-zero.png,muine-volume-zero.png			\
	-resource:$(top_srcdir)/data/images/muine-volume-min.png,muine-volume-min.png			\
	-resource:$(top_srcdir)/data/images/muine-volume-medium.png,muine-volume-medium.png		\
	-resource:$(top_srcdir)/data/images/muine-volume-max.png,muine-volume-max.png			\
	-resource:$(top_srcdir)/data/images/muine-nothing.png,muine-nothing.png				\
	-resource:$(top_srcdir)/data/images/muine-tray-playing.png,muine-tray-playing.png		\
	-resource:$(top_srcdir)/data/images/muine-tray-paused.png,muine-tray-paused.png			\
	-resource:$(top_srcdir)/data/images/muine-default-cover.png,muine-default-cover.png		\
	-resource:$(top_srcdir)/data/images/muine-cover-downloading.png,muine-cover-downloading.png	\
	-resource:$(top_srcdir)/data/images/muine-about.png,muine-about.png				\
	-resource:$(top_srcdir)/data/images/muine.png,muine.png						

# FIXME MONO_XMLSERIALIZER_THS=no works around Ximian Bugzilla #58194
AmazonSearchService.cs: AmazonSearchService.wsdl
	MONO_XMLSERIALIZER_THS=no wsdl $(srcdir)/AmazonSearchService.wsdl

Defines.cs: $(srcdir)/Defines.cs.in
	sed -e "s|\@version\@|$(VERSION)|"     \
	    -e "s|\@datadir\@|$(datadir)|"     \
	    -e "s|\@pkglibdir\@|$(pkglibdir)|" \
	    < $^ > $@

muine.exe: $(MUINE_CSFILES) $(top_builddir)/PluginLib/muine-plugin.dll $(top_builddir)/DBusLib/muine-dbus.dll Defines.cs
	$(CSC) -out:$@ $(MUINE_CSFILES) $(MUINE_ASSEMBLIES) $(MUINE_RESOURCES) $(GTKSHARP_LIBS) $(DBUSSHARP_LIBS)

all: muine.exe

muinelibdir = $(pkglibdir)
muinelib_DATA = muine.exe muine.exe.config

bin_SCRIPTS = muine

EXTRA_DIST =					\
	$(MUINE_CSFILES)			\
	AmazonSearchService.wsdl		\
	muine.in				\
	muine.exe.config.in			\
	Defines.cs.in

CLEANFILES =					\
	AmazonSearchService.cs			\
	muine.exe

DISTCLEANFILES =				\
	muine					\
	muine.exe.config			\
	Defines.cs
