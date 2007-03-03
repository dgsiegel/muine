ASSEMBLY_NAME = NDesk.DBus.GLib
ASSEMBLY = $(ASSEMBLY_NAME).dll
TARGET = $(ASSEMBLY)

CSC = $(MCS) $(MCS_FLAGS) $(CSFLAGS)

DBUSSHARP_CSFILES =			\
	$(srcdir)/AssemblyInfo.cs	\
	$(srcdir)/GLib.cs		\
	$(srcdir)/GLib.IO.cs

DBUSSHARP_ASSEMBLIES =		\
	-r:Mono.Posix

if !EXTERNAL_DBUS
$(ASSEMBLY): $(DBUSSHARP_CSFILES)
	$(CSC) -t:library $(LIBFLAGS) -out:$@ -r:$(top_builddir)/deps/dbus-sharp/NDesk.DBus.dll $(DBUSSHARP_CSFILES) $(DBUSSHARP_ASSEMBLIES)

all: $(ASSEMBLY)

dbussharpdir = $(pkglibdir)
dbussharp_DATA = $(TARGET) $(TARGET).config
endif

EXTRA_DIST = $(DBUSSHARP_CSFILES)

CLEANFILES = $(ASSEMBLY)

DISTCLEANFILES = $(TARGET).config
