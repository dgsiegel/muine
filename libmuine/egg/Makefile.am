INCLUDES = \
  $(MUINE_CFLAGS) \
  -DCURSOR_DIR=\"$(pkgdatadir)\" 

noinst_LTLIBRARIES = libegg.la

EGGSOURCES = \
  eggtrayicon.c \
  eggtraymanager.c 

libegg_la_SOURCES = \
  $(EGGSOURCES) \
  eggmarshalers.c

EGGHEADERS = \
  eggtrayicon.h \
  eggtraymanager.h 

noinst_HEADERS = \
  $(EGGHEADERS) \
  eggmarshalers.h \
  eggintl.h

eggmarshalers.h: eggmarshalers.list
	cd $(srcdir) \
	&& $(GLIB_GENMARSHAL) --prefix=_egg_marshal eggmarshalers.list --header > xgen-emh \
	&& cp xgen-emh eggmarshalers.h \
	&& rm -f xgen-emh xgen-emh~

eggmarshalers.c: eggmarshalers.list
	cd $(srcdir) \
	&& $(GLIB_GENMARSHAL) --prefix=_egg_marshal eggmarshalers.list --body > xgen-emc \
	&& cp xgen-emc eggmarshalers.c \
	&& rm -f xgen-emc xgen-emc~

egg-marshal.c: eggmarshalers.h eggmarshalers.c

EXTRA_DIST=			\
	eggmarshalers.list

EGGFILES=$(EGGSOURCES) $(EGGHEADERS)
EGGDIR=$(srcdir)/../../../libegg/libegg

regenerate-built-sources:
	EGGFILES="$(EGGFILES) eggmarshalers.list" EGGDIR="$(EGGDIR)" $(srcdir)/update-from-egg.sh

