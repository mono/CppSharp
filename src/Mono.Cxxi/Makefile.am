
EXTRA_DIST = m4/expansions.m4

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = $(GMCS)
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
BUILD_DIR = $(top_srcdir)/bin/Debug

ASSEMBLY = $(BUILD_DIR)/Mono.Cxxi.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb

MONO_CXXI_DLL_MDB=$(BUILD_DIR)/Mono.Cxxi.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = $(GMCS)
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+
BUILD_DIR = $(top_srcdir)/bin/Release

ASSEMBLY = $(BUILD_DIR)/Mono.Cxxi.dll
ASSEMBLY_MDB = 

MONO_VISUALC_INTEROP_DLL_MDB=

endif

COMPILE_TARGET = library
PROJECT_REFERENCES = 

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(MONO_CXXI_DLL_MDB)

LINUX_PKGCONFIG = \
	$(MONO_CXXI_PC)


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES =  \
	Abi/CppAbi.cs \
	Abi/EmitInfo.cs \
	Abi/Impl/ItaniumAbi.cs \
	Abi/Impl/ItaniumTypeInfo.cs \
	Abi/Impl/MsvcAbi.cs \
	Abi/Impl/VirtualOnlyAbi.cs \
	Abi/MethodType.cs \
	Abi/VTable.cs \
	AssemblyInfo.cs \
	Attributes.cs \
	CppField.cs \
	CppInstancePtr.cs \
	CppLibrary.cs \
	CppModifiers.cs \
	CppObjectMarshaler.cs \
	CppType.cs \
	CppTypeInfo.cs \
	Interfaces.cs \
	Util/DelegateTypeCache.cs \
	Util/IEnumerableTransform.cs \
	Util/LazyGeneratedList.cs \
	Util/MethodSignature.cs \
	Util/ReflectionHelper.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	Abi \
	Abi/Impl \
	Util \
	mono.cxxi.pc.in

REFERENCES =  \
	System \
	System.Core

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

MONO_CXXI_PC = $(BUILD_DIR)/mono.cxxi.pc

$(eval $(call emit-deploy-wrapper,MONO_CXXI_PC,mono.cxxi.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
