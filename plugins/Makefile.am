CSC = $(MCS) -codepage:utf8 $(MCS_FLAGS)

DASHBOARD_PLUGIN_CSFILES =				\
	$(top_srcdir)/plugins/DashboardPlugin.cs

ASSEMBLIES = -r:$(top_builddir)/PluginLib/PluginLib.dll
	
DashboardPlugin.dll: $(DASHBOARD_PLUGIN_CSFILES)
	$(CSC) -target:library $(LIBFLAGS) -out:$@ $(DASHBOARD_PLUGIN_CSFILES) $(ASSEMBLIES)

all: DashboardPlugin.dll

EXTRA_DIST = $(DASHBOARD_PLUGIN_CSFILES)

CLEANFILES = DashboardPlugin.dll
