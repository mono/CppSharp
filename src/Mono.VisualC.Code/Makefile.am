
EXTRA_DIST = m4/expansions.m4

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = mcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
BUILD_DIR = $(top_srcdir)/bin/Debug

ASSEMBLY = $(BUILD_DIR)/Mono.VisualC.Code.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb

MONO_VISUALC_INTEROP_DLL_SOURCE=$(BUILD_DIR)/Mono.VisualC.Interop.dll
MONO_VISUALC_INTEROP_DLL=$(BUILD_DIR)/Mono.VisualC.Interop.dll
MONO_VISUALC_CODE_DLL_MDB_SOURCE=$(BUILD_DIR)/Mono.VisualC.Code.dll.mdb
MONO_VISUALC_CODE_DLL_MDB=$(BUILD_DIR)/Mono.VisualC.Code.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = mcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+
BUILD_DIR = $(top_srcdir)/bin/Release

ASSEMBLY = $(BUILD_DIR)/Mono.VisualC.Code.dll
ASSEMBLY_MDB = 

MONO_VISUALC_INTEROP_DLL_SOURCE=$(BUILD_DIR)/Mono.VisualC.Interop.dll
MONO_VISUALC_INTEROP_DLL=$(BUILD_DIR)/Mono.VisualC.Interop.dll
MONO_VISUALC_CODE_DLL_MDB=

endif

COMPILE_TARGET = library

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(MONO_VISUALC_INTEROP_DLL) \
	$(MONO_VISUALC_CODE_DLL_MDB)

LINUX_PKGCONFIG = \
	$(MONO_VISUALC_CODE_PC)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES =  \
	Access.cs \
	AssemblyInfo.cs \
	Atoms/Class.cs \
	Atoms/Enumeration.cs \
	Atoms/Field.cs \
	Atoms/Method.cs \
	Atoms/Namespace.cs \
	Atoms/Preprocessor.cs \
	Atoms/Property.cs \
	Atoms/Union.cs \
	CodeAtom.cs \
	CodeContainer.cs \
	CodeDomExtensions.cs \
	CodeUnit.cs \
	NameTypePair.cs

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	Atoms \
	mono.visualc.code.pc.in 

REFERENCES =  \
	../Mono.VisualC.Interop/bin/Debug/Mono.VisualC.Interop.dll \
	System \
	System.Core

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

MONO_VISUALC_CODE_PC = $(BUILD_DIR)/mono.visualc.code.pc

$(eval $(call emit-deploy-target,MONO_VISUALC_INTEROP_DLL))
$(eval $(call emit-deploy-wrapper,MONO_VISUALC_CODE_PC,mono.visualc.code.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
