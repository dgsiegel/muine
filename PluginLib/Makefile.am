CSC = $(MCS) -codepage:utf8 $(MCS_FLAGS)

PLUGIN_LIB_CSFILES =			\
	$(srcdir)/Plugin.cs		\
	$(srcdir)/PlayerInterface.cs	\
	$(srcdir)/SongInterface.cs

PluginLib.dll: $(PLUGIN_LIB_CSFILES)
	$(CSC) -target:library $(LIBFLAGS) -out:$@ $(PLUGIN_LIB_CSFILES) $(GTKSHARP_LIBS)

all: PluginLib.dll

muinelibdir = $(prefix)/lib/muine
muinelib_DATA = PluginLib.dll

EXTRA_DIST = $(PLUGIN_LIB_CSFILES)

CLEANFILES = PluginLib.dll
