ASSEMBLY_NAME = muine-dbus
ASSEMBLY = $(ASSEMBLY_NAME).dll
TARGET = $(ASSEMBLY)

CSC = $(MCS) -codepage:utf8 $(MCS_FLAGS) $(CSFLAGS)

CLIENT_LIB_CSFILES =			\
	$(srcdir)/Player.cs		\
	$(srcdir)/AssemblyInfo.cs

CLIENT_LIB_ASSEMBLIES = 						\
	-r:$(top_builddir)/PluginLib/muine-plugin.dll

muine.snk: $(top_srcdir)/muine.snk
	cp $(top_srcdir)/muine.snk .

$(ASSEMBLY): $(CLIENT_LIB_CSFILES) muine.snk
	$(CSC) -target:library $(LIBFLAGS) -out:$@ $(CLIENT_LIB_CSFILES) $(GTKSHARP_LIBS) $(DBUSSHARP_LIBS) $(CLIENT_LIB_ASSEMBLIES)

all: $(ASSEMBLY)

EXTRA_DIST = $(CLIENT_LIB_CSFILES)

CLEANFILES = $(ASSEMBLY) muine.snk

install-data-local:
	@if test -n '$(TARGET)'; then						\
	  echo "$(GACUTIL) /i $(ASSEMBLY) /f $(GACUTIL_FLAGS)";	\
	  $(GACUTIL) /i $(ASSEMBLY) /f $(GACUTIL_FLAGS) || exit 1;	\
	fi

uninstall-local:
	@if test -n '$(TARGET)'; then						\
	  echo "$(GACUTIL) /u $(ASSEMBLY_NAME) $(GACUTIL_FLAGS)";	\
	  $(GACUTIL) /u $(ASSEMBLY_NAME) $(GACUTIL_FLAGS) || exit 1;	\
	fi
