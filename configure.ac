dnl Process this file with autoconf to produce a configure script.
AC_PREREQ([2.54])
AC_INIT([CPPInterop], [0.99.1])
AM_INIT_AUTOMAKE([foreign])
AM_MAINTAINER_MODE

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([You need to install pkg-config])
fi

SHAMROCK_EXPAND_LIBDIR
SHAMROCK_EXPAND_BINDIR
SHAMROCK_EXPAND_DATADIR

AC_PROG_INSTALL

AC_PATH_PROG(MONO, mono, no)
if test "x$MONO" = "xno"; then
	AC_MSG_ERROR([mono Not found])
fi

AC_PATH_PROG(GMCS, mcs, no)
if test "x$GMCS" = "xno"; then
        AC_MSG_ERROR([mcs Not found])
fi


AC_ARG_ENABLE(debug,
	AC_HELP_STRING([--enable-debug],
		[Use 'DEBUG' Configuration [default=YES]]),
		enable_debug=yes, enable_debug=no)
AM_CONDITIONAL(ENABLE_DEBUG, test x$enable_debug = xyes)
if test "x$enable_debug" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
AC_ARG_ENABLE(release,
	AC_HELP_STRING([--enable-release],
		[Use 'RELEASE' Configuration [default=NO]]),
		enable_release=yes, enable_release=no)
AM_CONDITIONAL(ENABLE_RELEASE, test x$enable_release = xyes)
if test "x$enable_release" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
if test -z "$CONFIG_REQUESTED" ; then
	AM_CONDITIONAL(ENABLE_DEBUG, true)
	enable_debug=yes
fi


dnl package checks, common for all configs

PKG_CHECK_MODULES([MONO_NUNIT], [mono-nunit])

dnl package checks, per config


AC_CONFIG_FILES([
src/Mono.VisualC.Interop/mono.visualc.interop.pc
src/Mono.VisualC.Interop/Makefile
src/QtBindings/qtbindings.pc
src/QtBindings/Makefile
src/Mono.VisualC.Code/mono.visualc.code.pc
src/Mono.VisualC.Code/Makefile
src/generator/generator
src/generator/Makefile
src/qt/Makefile
src/Makefile
tests/Makefile
examples/Makefile
examples/Hello/Makefile
Makefile
])

AC_OUTPUT
