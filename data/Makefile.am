SUBDIRS =	\
	glade	\
	images

@INTLTOOL_DESKTOP_RULE@

desktop_in_files = muine.desktop.in
desktop_files = $(desktop_in_files:.desktop.in=.desktop)

desktopdir = $(datadir)/applications
desktop_DATA = $(desktop_files)

schema_in_files = muine.schemas.in
schemadir = $(GCONF_SCHEMA_FILE_DIR)
schema_DATA = $(schema_in_files:.schemas.in=.schemas)

@INTLTOOL_SCHEMAS_RULE@

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = muine.pc

EXTRA_DIST =			\
	$(desktop_in_files)	\
	$(desktop_DATA)		\
	$(schema_in_files)	\
	$(schema_DATA)		\
	$(pkgconfig_DATA)

DISTCLEANFILES = 		\
	$(desktop_DATA) 	\
	$(schema_DATA)		\
	$(pkgconfig_DATA)

if GCONF_SCHEMAS_INSTALL
install-data-local:
	GCONF_CONFIG_SOURCE=$(GCONF_SCHEMA_CONFIG_SOURCE) $(GCONFTOOL) --makefile-install-rule $(srcdir)/$(schema_DATA)
endif
