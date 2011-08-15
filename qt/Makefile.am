top_srcdir = ../..

INTEROP_DLL = \
	$(top_srcdir)/bin/Debug/Mono.Cxxi.dll

all: hello.exe

qt-gui.xml: qt-gui.h
	$(GCCXML) -fxml=$@ --gccxml-cxxflags "-m32 -I." /Library/Frameworks/QtGui.framework/Versions/Current/Headers/QtGui

generated: qt-gui.xml
	$(RM) -r generated
	$(MONO) --debug $(top_srcdir)/bin/Debug/generator.exe -o=generated -ns=Qt.Gui -lib=QtGui --filters=qt-gui-filters.txt qt-gui.xml

#libQtGui-inline.so: qt-gui.cpp
#	$(CXX) -m32 -I. -framework QtGui -framework QtCore -DQ_WS_MAC --shared -fPIC -o $@ -fkeep-inline-functions qt-gui.cpp

Qt.Gui-binding.dll: generated QString.cs QSize.cs QApplication.cs QCoreApplication.cs
	$(RM) -f generated/QString.cs generated/QSize.cs
	$(GMCS) -debug -out:$@ -target:library -unsafe -r:$(INTEROP_DLL) generated/*.cs QString.cs QApplication.cs QCoreApplication.cs QSize.cs

hello.exe: Qt.Gui-binding.dll hello.cs #libQtGui-inline.so
	$(GMCS) -debug -out:$@ -target:exe -r:$(INTEROP_DLL) -r:Qt.Gui-binding.dll hello.cs

clean:
	$(RM) -rf generated hello.exe* qt-gui.xml Qt.Gui-binding.dll* libQtGui-inline.so

run: hello.exe
	MONO_PATH=.:$(top_srcdir)/bin/Debug $(MONO) --debug --trace=__CppLibraryImplAssembly hello.exe
