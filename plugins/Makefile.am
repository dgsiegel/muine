CSC = $(MCS) -codepage:utf8 $(MCS_FLAGS)

DASHBOARD_PLUGIN_CSFILES = DashboardPlugin.cs

ASSEMBLIES = -r:$(top_srcdir)/src/MuinePluginLib.dll
	
DashboardPlugin.dll: $(DASHBOARD_PLUGIN_CSFILES)
	$(CSC) -target:library $(LIBFLAGS) -out:$@ $(DASHBOARD_PLUGIN_CSFILES) $(ASSEMBLIES)

all: DashboardPlugin.dll

CLEANFILES = DashboardPlugin.dll
