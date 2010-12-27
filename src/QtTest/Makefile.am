
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = bin/Debug/QtTest.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../QtBindings/bin/Debug/QtBindings.dll \
	../Mono.VisualC.Interop/bin/Debug/Mono.VisualC.Interop.dll
BUILD_DIR = bin/Debug

QTBINDINGS_DLL_SOURCE=../QtBindings/bin/Debug/QtBindings.dll
QTBINDINGS_DLL_CONFIG_SOURCE=../QtBindings/QtBindings.dll.config
MONO_VISUALC_INTEROP_DLL_SOURCE=../Mono.VisualC.Interop/bin/Debug/Mono.VisualC.Interop.dll
MONO_VISUALC_INTEROP_DLL=$(BUILD_DIR)/Mono.VisualC.Interop.dll
QTTEST_EXE_MDB_SOURCE=bin/Debug/QtTest.exe.mdb
QTTEST_EXE_MDB=$(BUILD_DIR)/QtTest.exe.mdb
CPPINTEROP_DLL=

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize-
ASSEMBLY = bin/Release/QtTest.exe
ASSEMBLY_MDB = 
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../QtBindings/bin/Release/QtBindings.dll \
	../Mono.VisualC.Interop/bin/Release/CPPInterop.dll
BUILD_DIR = bin/Release

QTBINDINGS_DLL_SOURCE=../QtBindings/bin/Release/QtBindings.dll
QTBINDINGS_DLL_CONFIG_SOURCE=../QtBindings/QtBindings.dll.config
MONO_VISUALC_INTEROP_DLL=
QTTEST_EXE_MDB=
CPPINTEROP_DLL_SOURCE=../Mono.VisualC.Interop/bin/Release/CPPInterop.dll
CPPINTEROP_DLL=$(BUILD_DIR)/CPPInterop.dll

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(QTBINDINGS_DLL) \
	$(QTBINDINGS_DLL_CONFIG) \
	$(MONO_VISUALC_INTEROP_DLL) \
	$(QTTEST_EXE_MDB) \
	$(CPPINTEROP_DLL)  

BINARIES = \
	$(QTTEST)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(BINARIES) 

FILES = \
	Main.cs \
	AssemblyInfo.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	qttest.in 

REFERENCES =  \
	System

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(BINARIES) 

include $(top_srcdir)/Makefile.include

QTBINDINGS_DLL = $(BUILD_DIR)/QtBindings.dll
QTBINDINGS_DLL_CONFIG = $(BUILD_DIR)/QtBindings.dll.config
QTTEST = $(BUILD_DIR)/qttest

$(eval $(call emit-deploy-target,QTBINDINGS_DLL))
$(eval $(call emit-deploy-target,QTBINDINGS_DLL_CONFIG))
$(eval $(call emit-deploy-target,MONO_VISUALC_INTEROP_DLL))
$(eval $(call emit-deploy-wrapper,QTTEST,qttest,x))
$(eval $(call emit-deploy-target,CPPINTEROP_DLL))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
