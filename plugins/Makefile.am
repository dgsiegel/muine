CSC = $(MCS) -codepage:utf8 $(MCS_FLAGS) $(CSFLAGS)

DASHBOARD_PLUGIN_CSFILES =		\
	$(srcdir)/DashboardPlugin.cs

ASSEMBLIES = -r:$(top_builddir)/PluginLib/muine-plugin.dll
	
DashboardPlugin.dll: $(DASHBOARD_PLUGIN_CSFILES) $(top_builddir)/PluginLib/muine-plugin.dll
	$(CSC) -target:library $(LIBFLAGS) -out:$@ $(DASHBOARD_PLUGIN_CSFILES) $(ASSEMBLIES)

all: DashboardPlugin.dll

EXTRA_DIST = $(DASHBOARD_PLUGIN_CSFILES)

CLEANFILES = DashboardPlugin.dll
