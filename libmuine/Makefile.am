SUBDIRS = id3-vfs egg

INCLUDES =							\
	-I$(top_srcdir)						\
        -DG_LOG_DOMAIN=\"libmuine\"				\
	-DG_DISABLE_DEPRECATED -DGTK_DISABLE_DEPRECATED		\
	-DGDK_DISABLE_DEPRECATED -DGNOME_DISABLE_DEPRECATED	\
	$(MUINE_CFLAGS)						\
	$(OGG_CFLAGS)						\
	$(VORBIS_CFLAGS)					\
	$(VORBISFILE_CFLAGS)					\
	$(WARN_CFLAGS)						\
	-DG_DISABLE_DEPRECATED					\
	-DGTK_DISABLE_DEPRECATED				\
	-DGDK_DISABLE_DEPRECATED				\
	-DGDK_PIXBUF_DISABLE_DEPRECATED				\
	-DGNOME_DISABLE_DEPRECATED

muinelibdir = $(prefix)/lib/muine
muinelib_LTLIBRARIES = libmuine.la

libmuine_la_SOURCES =			\
	player.c			\
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
	volume-button.c			\
	volume-button.h			\
	db.c				\
	db.h
	
libmuine_la_LIBADD = id3-vfs/libid3-vfs.la egg/libegg.la $(MUINE_LIBS) $(OGG_LIBS) $(VORBIS_LIBS) $(VORBISFILE_LIBS) $(GDBM_LIBS)

EXTRA_DIST = metadata-gst.c player-gst-0.7.c
