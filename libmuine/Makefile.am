SUBDIRS = id3-vfs egg

INCLUDES =							\
	-I$(top_srcdir)						\
        -DG_LOG_DOMAIN=\"libmuine\"				\
	-DGNOMELOCALEDIR=\""$(datadir)/locale"\"		\
	$(MUINE_CFLAGS)						\
	$(OGG_CFLAGS)						\
	$(ID3TAG_CFLAGS)					\
	$(VORBIS_CFLAGS)					\
	$(VORBISFILE_CFLAGS)					\
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
	player.h			\
	metadata.c			\
	metadata.h			\
	ogg-helper.c			\
	ogg-helper.h			\
	gsequence.c			\
	gsequence.h			\
	pointer-list-model.c		\
	pointer-list-model.h		\
	pointer-list-view.c		\
	pointer-list-view.h		\
	markup-utils.c			\
	markup-utils.h			\
	rb-cell-renderer-pixbuf.c	\
	rb-cell-renderer-pixbuf.h	\
	rb-ellipsizing-label.c		\
	rb-ellipsizing-label.h		\
	db.c				\
	db.h				\
	mm-keys.c			\
	mm-keys.h
	
libmuine_la_LIBADD = id3-vfs/libid3-vfs.la egg/libegg.la $(MUINE_LIBS) $(OGG_LIBS) $(VORBIS_LIBS) $(VORBISFILE_LIBS) $(GDBM_LIBS) $(ID3TAG_LIBS) $(FLAC_LIBS)
