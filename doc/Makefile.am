if ENABLE_MONODOC
TARGETS = muine-docs.zip muine-docs.tree muine-docs.source

install-data-local:
	mkdir -p $(DESTDIR)/$(MONODOC_SOURCESDIR)
	install -m 644 $(srcdir)/muine-docs.source \
		$(DESTDIR)/$(MONODOC_SOURCESDIR)
	install -m 644 muine-docs.tree \
		$(DESTDIR)/$(MONODOC_SOURCESDIR)
	install -m 644 muine-docs.zip \
		$(DESTDIR)/$(MONODOC_SOURCESDIR)

uninstall-local:
	rm -f $(DESTDIR)/$(MONODOC_SOURCESDIR)/muine-docs.source
	rm -f $(DESTDIR)/$(MONODOC_SOURCESDIR)/muine-docs.tree
	rm -f $(DESTDIR)/$(MONODOC_SOURCESDIR)/muine-docs.zip
else
TARGETS = 
endif

noinst_DATA = $(TARGETS)

muine-docs.zip muine-docs.tree: $(srcdir)/en/*/*.xml $(srcdir)/en/*.xml
	$(MDASSEMBLER) --ecma $(srcdir)/en -o muine-docs

update:
	$(MONODOCER) -assembly:$(top_builddir)/PluginLib/muine-plugin.dll -path:en
	$(MONODOCER) -assembly:$(top_builddir)/DBusLib/muine-dbus.dll -path:en

CLEANFILES = muine-docs.zip muine-docs.tree

EXTRA_DIST = muine-docs.source

dist-hook:
	mkdir -p $(distdir)/en
	cp $(srcdir)/en/*.xml $(distdir)/en/
	mkdir -p $(distdir)/en/Muine.DBusLib
	cp $(srcdir)/en/Muine.DBusLib/*.xml $(distdir)/en/Muine.DBusLib/
	mkdir -p $(distdir)/en/Muine.PluginLib
	cp $(srcdir)/en/Muine.PluginLib/*.xml $(distdir)/en/Muine.PluginLib/
