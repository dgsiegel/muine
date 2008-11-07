INCLUDES =							\
	-I$(top_srcdir)						\
        -DG_LOG_DOMAIN=\"libmuine\"				\
	-DGNOMELOCALEDIR=\""$(datadir)/locale"\"		\
	$(MUINE_CFLAGS)						\
	$(WARN_CFLAGS)						\
	-DG_DISABLE_DEPRECATED					\
	-DGTK_DISABLE_DEPRECATED				\
	-DGDK_DISABLE_DEPRECATED				\
	-DGDK_PIXBUF_DISABLE_DEPRECATED				\
	-DGNOME_DISABLE_DEPRECATED

muinelibdir = $(pkglibdir)
muinelib_LTLIBRARIES = libmuine.la

if HAVE_XINE
xine_sources = player-xine.c
endif

if HAVE_GST
gst_sources = player-gst.c
endif

libmuine_la_SOURCES =			\
	$(xine_sources) 		\
	$(gst_sources)			\
	macros.h			\
	player.h			\
	gsequence.c			\
	gsequence.h			\
	pointer-list-model.c		\
	pointer-list-model.h		\
	rb-cell-renderer-pixbuf.c	\
	rb-cell-renderer-pixbuf.h	\
	db.c				\
	db.h				\
	mm-keys.c			\
	mm-keys.h

libmuine_la_LIBADD = $(MUINE_LIBS) $(GDBM_LIBS)
