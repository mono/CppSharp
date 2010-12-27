
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = bin/Debug/generator.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../Mono.VisualC.Code/bin/Debug/Mono.VisualC.Code.dll \
	../Mono.VisualC.Interop/bin/Debug/Mono.VisualC.Interop.dll
BUILD_DIR = bin/Debug

MONO_VISUALC_CODE_DLL_SOURCE=../Mono.VisualC.Code/bin/Debug/Mono.VisualC.Code.dll
MONO_VISUALC_INTEROP_DLL_SOURCE=../Mono.VisualC.Interop/bin/Debug/Mono.VisualC.Interop.dll
MONO_VISUALC_INTEROP_DLL=$(BUILD_DIR)/Mono.VisualC.Interop.dll
GENERATOR_EXE_MDB_SOURCE=bin/Debug/generator.exe.mdb
GENERATOR_EXE_MDB=$(BUILD_DIR)/generator.exe.mdb
CPPINTEROP_DLL=

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize-
ASSEMBLY = bin/Release/generator.exe
ASSEMBLY_MDB = 
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../Mono.VisualC.Code/bin/Release/Mono.VisualC.Code.dll \
	../Mono.VisualC.Interop/bin/Release/CPPInterop.dll
BUILD_DIR = bin/Release

MONO_VISUALC_CODE_DLL_SOURCE=../Mono.VisualC.Code/bin/Release/Mono.VisualC.Code.dll
MONO_VISUALC_INTEROP_DLL=
GENERATOR_EXE_MDB=
CPPINTEROP_DLL_SOURCE=../Mono.VisualC.Interop/bin/Release/CPPInterop.dll
CPPINTEROP_DLL=$(BUILD_DIR)/CPPInterop.dll

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(MONO_VISUALC_CODE_DLL) \
	$(MONO_VISUALC_INTEROP_DLL) \
	$(GENERATOR_EXE_MDB) \
	$(CPPINTEROP_DLL)  

BINARIES = \
	$(GENERATOR)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(BINARIES) 

FILES = \
	Main.cs \
	Options.cs \
	MethodSignature.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	generator.in 

REFERENCES =  \
	System \
	System.Xml \
	System.Xml.Linq \
	System.Core

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(BINARIES) 

include $(top_srcdir)/Makefile.include

MONO_VISUALC_CODE_DLL = $(BUILD_DIR)/Mono.VisualC.Code.dll
GENERATOR = $(BUILD_DIR)/generator

$(eval $(call emit-deploy-target,MONO_VISUALC_CODE_DLL))
$(eval $(call emit-deploy-target,MONO_VISUALC_INTEROP_DLL))
$(eval $(call emit-deploy-wrapper,GENERATOR,generator,x))
$(eval $(call emit-deploy-target,CPPINTEROP_DLL))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
