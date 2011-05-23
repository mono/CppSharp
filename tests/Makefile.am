top_srcdir = ../

INTEROP_DLL = \
	$(top_srcdir)/src/Mono.VisualC.Interop/bin/Debug/Mono.VisualC.Interop.dll

all: test.exe

test.xml: test.h
	gccxml -fxml=$@ --gccxml-cxxflags -c test.h

libTest.so: test.h test.cpp
	g++ -fPIC --shared -o $@ test.cpp

libTest-inline.so: test.h test.cpp
	g++ -fPIC --shared -fkeep-inline-functions -o $@ test.cpp

generated: test.xml
	$(RM) -r generated
	mono --debug $(top_srcdir)/src/generator2/bin/Debug/generator.exe -o=generated -ns=CppTests -lib=Test test.xml

CppTestBinding.dll: generated
	mcs -out:$@ -target:library -unsafe -r:$(INTEROP_DLL) generated/*.cs

test.exe: test.cs TestDriver.cs CppTestBinding.dll libTest.so libTest-inline.so
	mcs -out:$@ -target:exe -r:$(INTEROP_DLL) -r:CppTestBinding.dll test.cs TestDriver.cs

clean:
	$(RM) -rf CppTestBinding.dll test.exe generated libTest.so libTest-inline.so test.xml

run: test.exe
	MONO_PATH=$(top_srcdir)/src/Mono.VisualC.Interop/bin/Debug mono test.exe