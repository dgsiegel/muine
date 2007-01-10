ASSEMBLY_NAME = muine-dbus
ASSEMBLY = $(ASSEMBLY_NAME).dll
TARGET = $(ASSEMBLY)

CSC = $(MCS) $(MCS_FLAGS) $(CSFLAGS)

CLIENT_LIB_CSFILES =			\
	$(srcdir)/IPlayer.cs		\
	$(srcdir)/Player.cs		\
	$(srcdir)/AssemblyInfo.cs

CLIENT_LIB_ASSEMBLIES = 						\
	-r:$(top_builddir)/PluginLib/muine-plugin.dll			\
	-r:$(top_builddir)/deps/dbus-sharp/NDesk.DBus.dll

muine.snk: $(top_srcdir)/muine.snk
	cp $(top_srcdir)/muine.snk .

$(ASSEMBLY): $(CLIENT_LIB_CSFILES) muine.snk
	$(CSC) -target:library $(LIBFLAGS) -out:$@ $(CLIENT_LIB_CSFILES) $(GTKSHARP_LIBS) $(CLIENT_LIB_ASSEMBLIES)

all: $(ASSEMBLY)

EXTRA_DIST = $(CLIENT_LIB_CSFILES)

CLEANFILES = $(ASSEMBLY) muine.snk

muinelibdir = $(pkglibdir)
muinelib_DATA = $(TARGET)
