#!/bin/sh
# Run this to generate all the initial makefiles, etc.

srcdir=`dirname $0`
test -z "$srcdir" && srcdir=.

PKG_NAME="muine"

(test -f $srcdir/configure.in) || {
    echo -n "**Error**: Directory "\`$srcdir\'" does not look like the"
    echo " top-level $PKG_NAME directory"
    exit 1
}

which gnome-autogen.sh || {
    echo "You need to install gnome-common from the GNOME CVS"
    exit 1
}
REQUIRED_AUTOMAKE_VERSION=1.7 USE_GNOME2_MACROS=1 NOCONFIGURE=1 ACLOCAL_FLAGS="$ACLOCAL_FLAGS -I $srcdir/m4" . gnome-autogen.sh
echo "Copying po/Makefile.in.in.override to po/Makefile.in.in"
cp $srcdir/po/Makefile.in.in.override $srcdir/po/Makefile.in.in
echo "Running $srcdir/configure $conf_flags "$@" ..."
$srcdir/configure --enable-maintainer-mode "$@" \
	&& echo Now type \`make\' to compile $PKG_NAME || exit 1
