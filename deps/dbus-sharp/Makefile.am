ASSEMBLY_NAME = NDesk.DBus
ASSEMBLY = $(ASSEMBLY_NAME).dll
TARGET = $(ASSEMBLY)

CSC = $(MCS) $(MCS_FLAGS) $(CSFLAGS)

DBUSSHARP_CSFILES =				\
	$(srcdir)/Address.cs			\
	$(srcdir)/AssemblyInfo.cs		\
	$(srcdir)/Authentication.cs		\
	$(srcdir)/Bus.cs			\
	$(srcdir)/BusObject.cs			\
	$(srcdir)/Connection.cs			\
	$(srcdir)/DBus.cs			\
	$(srcdir)/DProxy.cs			\
	$(srcdir)/ExportObject.cs		\
	$(srcdir)/Introspection.cs		\
	$(srcdir)/IntrospectionSchema.cs	\
	$(srcdir)/Mapper.cs			\
	$(srcdir)/MatchRule.cs			\
	$(srcdir)/Message.cs			\
	$(srcdir)/MessageFilter.cs		\
	$(srcdir)/MessageReader.cs		\
	$(srcdir)/MessageWriter.cs		\
	$(srcdir)/PendingCall.cs		\
	$(srcdir)/Protocol.cs			\
	$(srcdir)/Server.cs			\
	$(srcdir)/Signature.cs			\
	$(srcdir)/SocketTransport.cs		\
	$(srcdir)/Transport.cs			\
	$(srcdir)/TypeDefiner.cs		\
	$(srcdir)/TypeImplementer.cs		\
	$(srcdir)/UnixNativeTransport.cs	\
	$(srcdir)/UnixTransport.cs		\
	$(srcdir)/Wrapper.cs

DBUSSHARP_ASSEMBLIES =		\
	-r:Mono.Posix

if !EXTERNAL_DBUS
$(ASSEMBLY): $(DBUSSHARP_CSFILES)
	$(CSC) -t:library -unsafe $(LIBFLAGS) -out:$@ $(DBUSSHARP_CSFILES) $(DBUSSHARP_ASSEMBLIES)

all: $(ASSEMBLY)

dbussharpdir = $(pkglibdir)
dbussharp_DATA = $(TARGET)
endif

EXTRA_DIST = $(DBUSSHARP_CSFILES)

CLEANFILES = $(ASSEMBLY)
