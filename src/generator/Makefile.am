
EXTRA_DIST = m4/expansions.m4

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = mcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
BUILD_DIR = $(top_srcdir)/bin/Debug

ASSEMBLY = $(BUILD_DIR)/generator.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb

MONO_CXXI_DLL_SOURCE=$(BUILD_DIR)/Mono.Cxxi.dll
MONO_CXXI_DLL=$(BUILD_DIR)/Mono.Cxxi.dll
GENERATOR_EXE_MDB_SOURCE=$(BUILD_DIR)/generator.exe.mdb
GENERATOR_EXE_MDB=$(BUILD_DIR)/generator.exe.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = mcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+
BUILD_DIR = $(top_srcdir)/bin/Release

ASSEMBLY = $(BUILD_DIR)/generator.exe
ASSEMBLY_MDB = 

MONO_CXXI_DLL_SOURCE=$(BUILD_DIR)/Mono.Cxxi.dll
MONO_CXXI_DLL=$(BUILD_DIR)/Mono.Cxxi.dll
GENERATOR_EXE_MDB= 

endif

COMPILE_TARGET = exe

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(MONO_CXXI_DLL) \
	$(GENERATOR_EXE_MDB)

BINARIES = \
	$(GENERATOR)  


RESGEN=resgen2

all: $(ASSEMBLY) $(PROGRAMFILES) $(BINARIES) 

FILES =  \
	Access.cs \
	Class.cs \
	Field.cs \
	Generator.cs \
	Method.cs \
	Node.cs \
	Options.cs \
	Parameter.cs \
	Property.cs \
	Templates/Base.cs \
	Templates/BaseMembers.cs \
	Templates/CSharp/CSharpClass.cs \
	Templates/CSharp/CSharpLanguage.cs \
	Templates/CSharp/CSharpLibs.cs

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	generator.in 

REFERENCES =  \
	../../bin/Debug/Mono.Cxxi.dll \
	System \
	System.Core \
	System.Xml \
	System.Xml.Linq

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(BINARIES) 

include $(top_srcdir)/Makefile.include

GENERATOR = $(BUILD_DIR)/generator

$(eval $(call emit-deploy-target,MONO_CXXI_DLL))
$(eval $(call emit-deploy-wrapper,GENERATOR,generator,x))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
