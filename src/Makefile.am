CSC = $(MCS) $(MCS_FLAGS) $(CSFLAGS)

TARGET = muine.exe
WRAPPER = muine

MUINE_CSFILES =					\
	$(srcdir)/AddSongWindow.cs		\
	$(srcdir)/AddAlbumWindow.cs		\
	$(srcdir)/Global.cs			\
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
	$(srcdir)/CoverDatabase.cs		\
	$(srcdir)/CoverGetter.cs		\
	$(srcdir)/GnomeProxy.cs			\
	$(srcdir)/CoverImage.cs			\
	$(srcdir)/MmKeys.cs			\
	$(srcdir)/FileUtils.cs			\
	$(srcdir)/SignalUtils.cs		\
	$(srcdir)/Database.cs			\
	$(srcdir)/DBusService.cs		\
	$(srcdir)/PluginManager.cs		\
	$(srcdir)/AddWindow.cs			\
	$(srcdir)/Config.cs			\
	$(srcdir)/DndUtils.cs			\
	$(srcdir)/Item.cs			\
	$(srcdir)/ThreadBase.cs			\
	$(srcdir)/AddWindowList.cs		\
	$(srcdir)/AddWindowEntry.cs		\
	$(srcdir)/Actions.cs			\
	$(srcdir)/ImportDialog.cs		\
	$(srcdir)/OpenDialog.cs			\
	$(srcdir)/SaveDialog.cs

MUINE_GENERATED_CSFILES =			\
	AmazonSearchService.cs			\
	Defines.cs

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
	MONO_XMLSERIALIZER_THS=no wsdl -namespace:Amazon $(srcdir)/AmazonSearchService.wsdl

$(WRAPPER): $(srcdir)/$(WRAPPER).in
	sed -e "s|\@prefix\@|$(prefix)|g" 		\
	    -e "s|\@pkglibdir\@|$(pkglibdir)|g" 	\
	    -e "s|\@target\@|$(TARGET)|g" 		\
	    -e "s|\@mono\@|$(MONO)|g"			\
	    -e "s|\@mono_flags\@|$(MONO_FLAGS)|g"	\
	    < $^ > $@

$(TARGET).config: $(srcdir)/$(TARGET).config.in
	sed -e "s|\@pkglibdir\@|$(pkglibdir)|" \
	    < $^ > $@

Defines.cs: $(srcdir)/Defines.cs.in
	sed -e "s|\@version\@|$(VERSION)|"     \
	    -e "s|\@datadir\@|$(datadir)|"     \
	    -e "s|\@pkglibdir\@|$(pkglibdir)|" \
	    < $^ > $@

$(TARGET): $(MUINE_CSFILES) $(MUINE_GENERATED_CSFILES) $(top_builddir)/PluginLib/muine-plugin.dll $(top_builddir)/DBusLib/muine-dbus.dll
	$(CSC) -out:$@ $(MUINE_CSFILES) $(MUINE_GENERATED_CSFILES) $(MUINE_ASSEMBLIES) $(MUINE_RESOURCES) $(GTKSHARP_LIBS) $(DBUSSHARP_LIBS)

all: $(TARGET)

muinelibdir = $(pkglibdir)
muinelib_DATA = $(TARGET) $(TARGET).config

bin_SCRIPTS = $(WRAPPER)

EXTRA_DIST =					\
	$(MUINE_CSFILES)			\
	AmazonSearchService.wsdl		\
	$(WRAPPER).in				\
	$(TARGET).config.in			\
	Defines.cs.in

CLEANFILES =					\
	$(MUINE_GENERATED_CSFILES)		\
	$(TARGET)				\
	$(TARGET).config			\
	$(WRAPPER)
