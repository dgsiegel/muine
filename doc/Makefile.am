if ENABLE_MONODOC
TARGETS = muine-docs.zip muine-docs.tree muine-docs.source

install-data-local:
	mkdir -p $(DESTDIR)/`monodoc --get-sourcesdir`
	install -m 644 $(srcdir)/muine-docs.source \
		$(DESTDIR)/`monodoc --get-sourcesdir`
	install -m 644 muine-docs.tree \
		$(DESTDIR)/`monodoc --get-sourcesdir`
	install -m 644 muine-docs.zip \
		$(DESTDIR)/`monodoc --get-sourcesdir`

uninstall-local:
	rm -f $(DESTDIR)/`monodoc --get-sourcesdir`/muine-docs.source
	rm -f $(DESTDIR)/`monodoc --get-sourcesdir`/muine-docs.tree
	rm -f $(DESTDIR)/`monodoc --get-sourcesdir`/muine-docs.zip
else
TARGETS = 
endif

noinst_DATA = $(TARGETS)

ASSEMBLER = $(MONODOC) --assemble
UPDATER = $(MONODOC) --update

muine-docs.zip muine-docs.tree: $(srcdir)/en/*/*.xml $(srcdir)/en/*.xml
	$(ASSEMBLER) --ecma $(srcdir)/en -o muine-docs

update:
	$(UPDATER) $(top_builddir)/PluginLib/muine-plugin.dll -o ./en -f
	$(UPDATER) $(top_builddir)/DBusLib/muine-dbus.dll -o ./en -f

CLEANFILES = muine-docs.zip muine-docs.tree

EXTRA_DIST = muine-docs.source

dist-hook:
	mkdir -p $(distdir)/en
	cp $(srcdir)/en/*.xml $(distdir)/en/
	mkdir -p $(distdir)/en/MuineDBusLib
	cp $(srcdir)/en/MuineDBusLib/*.xml $(distdir)/en/MuineDBusLib/
	mkdir -p $(distdir)/en/MuinePluginLib
	cp $(srcdir)/en/MuinePluginLib/*.xml $(distdir)/en/MuinePluginLib/
