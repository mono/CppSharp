
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = bin/Debug/Mono.VisualC.Interop.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Debug

MONO_VISUALC_INTEROP_DLL_MDB_SOURCE=bin/Debug/Mono.VisualC.Interop.dll.mdb
MONO_VISUALC_INTEROP_DLL_MDB=$(BUILD_DIR)/Mono.VisualC.Interop.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize-
ASSEMBLY = bin/Release/CPPInterop.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Release

MONO_VISUALC_INTEROP_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(MONO_VISUALC_INTEROP_DLL_MDB)  

LINUX_PKGCONFIG = \
	$(MONO_VISUALC_INTEROP_PC)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES = \
	CppLibrary.cs \
	AssemblyInfo.cs \
	ABI/CppAbi.cs \
	Interfaces.cs \
	ABI/VTableManaged.cs \
	Attributes.cs \
	CppInstancePtr.cs \
	CppField.cs \
	ABI/VTable.cs \
	ABI/MethodType.cs \
	ABI/Impl/ItaniumAbi.cs \
	ABI/Impl/VirtualOnlyAbi.cs \
	ABI/Impl/MsvcAbi.cs \
	CppObjectMarshaler.cs \
	CppType.cs \
	CppTypeInfo.cs \
	ABI/Impl/ItaniumTypeInfo.cs \
	Util/IEnumerableTransform.cs \
	Util/LazyGeneratedList.cs \
	Util/DelegateTypeCache.cs \
	Util/ReflectionHelper.cs \
	ABI/Impl/MsvcTypeInfo.cs \
	Util/MethodSignature.cs \
	CppModifiers.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	ABI \
	ABI/Impl \
	Util \
	mono.visualc.interop.pc.in 

REFERENCES =  \
	System \
	System.Core

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

MONO_VISUALC_INTEROP_PC = $(BUILD_DIR)/mono.visualc.interop.pc

$(eval $(call emit-deploy-wrapper,MONO_VISUALC_INTEROP_PC,mono.visualc.interop.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
