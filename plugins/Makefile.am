CSC = $(MCS) $(MCS_FLAGS) $(CSFLAGS)

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

ASSEMBLIES = -r:$(top_builddir)/PluginLib/muine-plugin.dll
	
DashboardPlugin.dll: $(DASHBOARD_PLUGIN_CSFILES) $(top_builddir)/PluginLib/muine-plugin.dll
	$(CSC) -target:library $(LIBFLAGS) -out:$@ $(DASHBOARD_PLUGIN_CSFILES) $(ASSEMBLIES)

TrayIcon.dll: $(TRAY_ICON_CSFILES) $(top_builddir)/PluginLib/muine-plugin.dll
	$(CSC) -target:library $(LIBFLAGS) -out:$@ $(TRAY_ICON_CSFILES) $(TRAY_ICON_EXTRA_CSFILES) $(ASSEMBLIES) $(TRAY_ICON_EXTRA_ASSEMBLIES) $(TRAY_ICON_RESOURCES)

all: DashboardPlugin.dll TrayIcon.dll

EXTRA_DIST = $(DASHBOARD_PLUGIN_CSFILES) $(TRAY_ICON_CSFILES) TrayIcon.xml muine-tray-playing.png muine-tray-paused.png

CLEANFILES = DashboardPlugin.dll TrayIcon.dll
