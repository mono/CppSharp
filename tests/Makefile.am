top_srcdir = ../
BUILD_DIR = $(top_srcdir)/bin/Debug

INTEROP_DLL = \
	$(BUILD_DIR)/Mono.VisualC.Interop.dll

TEST_DLL = $(BUILD_DIR)/Test.dll

HDR = \
	Native/ByValTests.h

NATIVE = \
	Native/NUnit.cpp \
	Native/ByValTests.cpp

MANAGED =  \
	ByValTests.cs \
	Support/CppMockObject.cs \
	Support/CppNUnitAsserts.cs
	
REFERENCES =  \
	-pkg:mono-nunit

all: $(TEST_DLL)

test.xml: $(HDR)
	gccxml -fxml=$@ --gccxml-cxxflags -c $(HDR)

$(BUILD_DIR)/libTest.so: $(HEADERS) $(NATIVE)
	g++ -fPIC --shared -m32 -o $@ $(NATIVE)

$(BUILD_DIR)/libTest-inline.so: $(HEADERS) $(NATIVE)
	g++ -fPIC --shared -m32 -fkeep-inline-functions -o $@ $(NATIVE)

generated: test.xml
	$(RM) -r generated
	mono --debug $(BUILD_DIR)/generator.exe -o=$@ -ns=Tests -lib=Test -inline=surrogatelib test.xml

$(TEST_DLL): generated $(MANAGED) $(BUILD_DIR)/libTest.so $(BUILD_DIR)/libTest-inline.so
	mcs -debug -out:$@ -target:library -unsafe $(REFERENCES) -r:$(INTEROP_DLL) generated/*.cs $(MANAGED)

clean:
	$(RM) -rf $(TEST_DLL) generated $(BUILD_DIR)/libTest.so $(BUILD_DIR)/libTest-inline.so test.xml

run: $(TEST_DLL)
	nunit-console -nologo $(TEST_DLL)
