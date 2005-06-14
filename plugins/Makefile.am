CSC = $(MCS) $(MCS_FLAGS) $(CSFLAGS)

plugindir = $(libdir)/muine/plugins
plugin_DATA = InotifyPlugin.dll InotifyPlugin.dll.config

muinelibdir = $(pkglibdir)
muinelib_LTLIBRARIES = libinotifyglue.la

libinotifyglue_la_SOURCES = inotify-glue.c inotify.h

DASHBOARD_PLUGIN_CSFILES =		\
	$(srcdir)/DashboardPlugin.cs

TRAY_ICON_CSFILES =			\
	$(srcdir)/TrayIcon.cs		\
	$(srcdir)/egg-sharp.cs

TRAY_ICON_EXTRA_CSFILES =		\
	$(top_builddir)/src/Defines.cs	\
	$(top_srcdir)/src/StringUtils.cs

TRAY_ICON_EXTRA_ASSEMBLIES =		\
	$(GTKSHARP_LIBS)		\
	-r:Mono.Posix

TRAY_ICON_RESOURCES =							  \
	-resource:$(srcdir)/TrayIcon.xml,TrayIcon.xml			  \
	-resource:$(srcdir)/muine-tray-playing.png,muine-tray-playing.png \
	-resource:$(srcdir)/muine-tray-paused.png,muine-tray-paused.png

INOTIFY_CSFILES =		\
	$(srcdir)/Inotify.cs		\
	$(srcdir)/InotifyPlugin.cs

INOTIFY_EXTRA_ASSEMBLIES =		\
	$(GTKSHARP_LIBS)		\
	-r:Mono.Posix

ASSEMBLIES = -r:$(top_builddir)/PluginLib/muine-plugin.dll
	
DashboardPlugin.dll: $(DASHBOARD_PLUGIN_CSFILES) $(top_builddir)/PluginLib/muine-plugin.dll
	$(CSC) -target:library $(LIBFLAGS) -out:$@ $(DASHBOARD_PLUGIN_CSFILES) $(ASSEMBLIES)

TrayIcon.dll: $(TRAY_ICON_CSFILES) $(top_builddir)/PluginLib/muine-plugin.dll
	$(CSC) -target:library $(LIBFLAGS) -out:$@ $(TRAY_ICON_CSFILES) $(TRAY_ICON_EXTRA_CSFILES) $(ASSEMBLIES) $(TRAY_ICON_EXTRA_ASSEMBLIES) $(TRAY_ICON_RESOURCES)

InotifyPlugin.dll: $(INOTIFY_CSFILES) $(top_builddir)/PluginLib/muine-plugin.dll
	$(CSC) -unsafe -target:library $(LIBFLAGS) -out:$@ $(INOTIFY_CSFILES) $(ASSEMBLIES) $(INOTIFY_EXTRA_ASSEMBLIES)

InotifyPlugin.dll.config: $(srcdir)/InotifyPlugin.dll.config.in
	sed -e "s|\@pkglibdir\@|$(pkglibdir)|" \
	< $^ > $@


all: DashboardPlugin.dll TrayIcon.dll InotifyPlugin.dll

EXTRA_DIST = $(DASHBOARD_PLUGIN_CSFILES) $(TRAY_ICON_CSFILES) $(INOTIFY_CSFILES) TrayIcon.xml muine-tray-playing.png muine-tray-paused.png InotifyPlugin.dll.config.in

CLEANFILES = DashboardPlugin.dll TrayIcon.dll InotifyPlugin.dll
