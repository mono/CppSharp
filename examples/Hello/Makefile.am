
top_srcdir=../..

if ENABLE_DEBUG
BUILD_DIR=$(top_srcdir)/bin/Debug
endif

if ENABLE_RELEASE
BUILD_DIR=$(top_srcdir)/bin/Release
endif

INTEROP_DLL = \
	$(BUILD_DIR)/Mono.Cxxi.dll

HELLO_EXE = \
	$(BUILD_DIR)/Hello.exe

all: $(BUILD_DIR)/libhello.so Hello.xml $(HELLO_EXE)

$(BUILD_DIR)/libhello.so: Hello.cpp Hello.h
	$(CXX) -fPIC --shared -o $@ Hello.cpp

Hello.xml: Hello.h
	$(GCCXML) -fxml=$@ Hello.h

generated: Hello.xml
	$(RM) -r generated
	$(MONO) $(BUILD_DIR)/generator.exe -ns=Hello -lib=hello -o=$@ Hello.xml

$(HELLO_EXE): generated Hello.cs
	$(GMCS) -unsafe -out:$@ -target:exe -r:$(INTEROP_DLL) generated/*.cs Hello.cs

clean:
	$(RM) -r generated libhello.so Hello.xml $(HELLO_EXE)

run: Hello.exe
	$(MONO) --debug $(HELLO_EXE)
