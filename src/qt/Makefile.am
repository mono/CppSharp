top_srcdir = ../..

INTEROP_DLL = \
	$(top_srcdir)/src/Mono.VisualC.Interop/bin/Debug/Mono.VisualC.Interop.dll

all: hello.exe

qt-gui.xml: qt-gui.h
	gccxml -fxml=$@ --gccxml-cxxflags "`pkg-config --cflags QtCore QtGui`" qt-gui.h

generated: qt-gui.xml
	$(RM) -r generated
	mono --debug $(top_srcdir)/src/generator/bin/Debug/generator.exe -o=generated -ns=Qt.Gui -lib=QtGui --filters=qt-gui-filters.txt qt-gui.xml

libQtGui-inline.so: qt-gui.cpp
	g++ `pkg-config --cflags QtCore QtGui` --shared -fPIC -o $@ -fkeep-inline-functions qt-gui.cpp `pkg-config --libs QtCore QtGui`

Qt.Gui-binding.dll: generated
	$(GMCS) -out:$@ -target:library -unsafe -r:$(INTEROP_DLL) generated/*.cs

hello.exe: Qt.Gui-binding.dll hello.cs libQtGui-inline.so
	$(GMCS) -out:$@ -target:exe -r:$(INTEROP_DLL) -r:Qt.Gui-binding.dll hello.cs

clean:
	$(RM) -rf generated hello.exe qt-gui.xml Qt.Gui-binding.dll libQtGui-inline.so

run: hello.exe
	MONO_PATH=$(top_srcdir)/src/Mono.VisualC.Interop/bin/Debug mono hello.exe
