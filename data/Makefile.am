SUBDIRS =	\
	glade	\
	images

@INTLTOOL_DESKTOP_RULE@

desktop_in_files = muine.desktop.in
desktop_files = $(desktop_in_files:.desktop.in=.desktop)

desktopdir = $(datadir)/applications
desktop_DATA = $(desktop_files)

applicationsdir = $(datadir)/application-registry
applications_DATA = muine.applications

schema_in_files = muine.schemas.in
schemadir = $(GCONF_SCHEMA_FILE_DIR)
schema_DATA = $(schema_in_files:.schemas.in=.schemas)

@INTLTOOL_SCHEMAS_RULE@

EXTRA_DIST =			\
	$(desktop_in_files)	\
	$(desktop_files)	\
	$(schema_in_files)	\
	$(schema_DATA)		\
	$(applications_DATA)

install-data-local:
	GCONF_CONFIG_SOURCE=$(GCONF_SCHEMA_CONFIG_SOURCE) $(GCONFTOOL) --makefile-install-rule $(srcdir)/$(schema_DATA)
