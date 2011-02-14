
top_srcdir=../..

default: libhello.so Hello.xml Hello.exe

libhello.so: Hello.cpp Hello.h
	$(CXX) -fPIC --shared -o $@ Hello.cpp

Hello.xml: Hello.h
	gccxml -fxml=$@ Hello.h

output gen:
	$(RM) -r output
	mono $(top_srcdir)/src/generator/bin/Debug/generator.exe -f=Hello.xml -ns=Hello -lib=hello

INTEROP_DLL = \
	$(top_srcdir)/src/Mono.VisualC.Interop/bin/Debug/Mono.VisualC.Interop.dll

HelloBinding.dll: output
	mcs -out:$@ -target:library -r:$(INTEROP_DLL) output/*.cs

Hello.exe: HelloBinding.dll Hello.cs
	mcs -out:$@ -target:exe -r:$(INTEROP_DLL) -r:HelloBinding.dll Hello.cs

clean:
	$(RM) -r output libhello.so Hello.xml Hello.exe HelloBinding.dll

run: Hello.exe
	MONO_PATH=$(top_srcdir)/src/Mono.VisualC.Interop/bin/Debug mono Hello.exe