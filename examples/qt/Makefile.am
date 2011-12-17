top_srcdir = ../..

INTEROP_DLL = \
	$(top_srcdir)/bin/Debug/Mono.Cxxi.dll

HANDWRITTEN = \
	QString.cs \
	QApplication.cs \
	QCoreApplication.cs \
	QSize.cs \
	QPushButton.cs \
	QWidget.cs \
	QPoint.cs \
	QFlags.cs

all: hello.exe

# HACK: some Mac versions of Qt don't play nice with pkg-config
QT_FLAGS := `pkg-config --silence-errors --cflags QtCore QtGui || \
		(ln -s /Library/Frameworks/QtGui.framework/Versions/Current/Headers QtGui && \
		ln -s /Library/Frameworks/QtCore.framework/Versions/Current/Headers QtCore && \
		echo "-m32 -I. -I/Library/Frameworks/QtGui.framework/Versions/Current/Headers")`

qt-gui.xml: qt-gui.h
	$(GCCXML) -fxml=$@ --gccxml-cxxflags $(QT_FLAGS) $^

generated: qt-gui-filters.xml qt-gui.xml
	$(RM) -r generated
	$(MONO) --debug $(top_srcdir)/bin/Debug/generator.exe -o=generated -ns=Qt.Gui -lib=QtGui --filters=$^

libQtGui-inline.so: qt-gui.cpp
	$(CXX) -m32 -I. -framework QtGui -framework QtCore --shared -fPIC -o $@ -fkeep-inline-functions -fdump-class-hierarchy qt-gui.cpp

Qt.Gui-binding.dll: generated $(addprefix src/,$(HANDWRITTEN))
	$(GMCS) -debug -out:$@ -target:library -unsafe -r:$(INTEROP_DLL) generated/*.cs $(addprefix src/,$(HANDWRITTEN))

hello.exe: Qt.Gui-binding.dll demos/hello.cs #libQtGui-inline.so
	$(GMCS) -debug -out:$@ -target:exe -r:$(INTEROP_DLL) -r:Qt.Gui-binding.dll demos/hello.cs

clean:
	$(RM) -r generated QtCore QtGui hello.exe* qt-gui.xml Qt.Gui-binding.dll* libQtGui-inline.so

run: hello.exe
	MONO_PATH=.:$(top_srcdir)/bin/Debug $(MONO) --debug hello.exe
