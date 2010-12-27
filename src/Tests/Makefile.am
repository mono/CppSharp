
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = bin/Debug/Tests.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../Mono.VisualC.Interop/bin/Debug/Mono.VisualC.Interop.dll
BUILD_DIR = bin/Debug

MONO_VISUALC_INTEROP_DLL_SOURCE=../Mono.VisualC.Interop/bin/Debug/Mono.VisualC.Interop.dll
MONO_VISUALC_INTEROP_DLL=$(BUILD_DIR)/Mono.VisualC.Interop.dll
TESTS_DLL_MDB_SOURCE=bin/Debug/Tests.dll.mdb
TESTS_DLL_MDB=$(BUILD_DIR)/Tests.dll.mdb
CPPINTEROP_DLL=

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize-
ASSEMBLY = bin/Release/Tests.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../Mono.VisualC.Interop/bin/Release/CPPInterop.dll
BUILD_DIR = bin/Release

MONO_VISUALC_INTEROP_DLL=
TESTS_DLL_MDB=
CPPINTEROP_DLL_SOURCE=../Mono.VisualC.Interop/bin/Release/CPPInterop.dll
CPPINTEROP_DLL=$(BUILD_DIR)/CPPInterop.dll

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(MONO_VISUALC_INTEROP_DLL) \
	$(TESTS_DLL_MDB) \
	$(CPPINTEROP_DLL)  

LINUX_PKGCONFIG = \
	$(TESTS_PC)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES = \
	CppInstancePtrTests.cs \
	Support/CSimpleClass.cs \
	CppLibraryTests.cs \
	ItaniumAbiTests.cs \
	MsvcAbiTests.cs \
	Support/CppMockObject.cs \
	Support/CppNUnitAsserts.cs \
	SharedAbiTests.cs \
	Support/VirtualMethodTestClass.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	Support \
	tests.pc.in 

REFERENCES =  \
	System \
	nunit.core \
	nunit.framework

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

TESTS_PC = $(BUILD_DIR)/tests.pc

$(eval $(call emit-deploy-target,MONO_VISUALC_INTEROP_DLL))
$(eval $(call emit-deploy-wrapper,TESTS_PC,tests.pc))
$(eval $(call emit-deploy-target,CPPINTEROP_DLL))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
