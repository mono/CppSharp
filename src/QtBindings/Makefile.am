
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -unsafe -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = bin/Debug/QtBindings.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../Mono.VisualC.Interop/bin/Debug/Mono.VisualC.Interop.dll
BUILD_DIR = bin/Debug

QTBINDINGS_DLL_CONFIG_SOURCE=QtBindings.dll.config
MONO_VISUALC_INTEROP_DLL_SOURCE=../Mono.VisualC.Interop/bin/Debug/Mono.VisualC.Interop.dll
MONO_VISUALC_INTEROP_DLL=$(BUILD_DIR)/Mono.VisualC.Interop.dll
QTBINDINGS_DLL_MDB_SOURCE=bin/Debug/QtBindings.dll.mdb
QTBINDINGS_DLL_MDB=$(BUILD_DIR)/QtBindings.dll.mdb
CPPINTEROP_DLL=

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -unsafe -warn:4 -optimize-
ASSEMBLY = bin/Release/QtBindings.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../Mono.VisualC.Interop/bin/Release/CPPInterop.dll
BUILD_DIR = bin/Release

QTBINDINGS_DLL_CONFIG_SOURCE=QtBindings.dll.config
MONO_VISUALC_INTEROP_DLL=
QTBINDINGS_DLL_MDB=
CPPINTEROP_DLL_SOURCE=../Mono.VisualC.Interop/bin/Release/CPPInterop.dll
CPPINTEROP_DLL=$(BUILD_DIR)/CPPInterop.dll

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(QTBINDINGS_DLL_CONFIG) \
	$(MONO_VISUALC_INTEROP_DLL) \
	$(QTBINDINGS_DLL_MDB) \
	$(CPPINTEROP_DLL)  

LINUX_PKGCONFIG = \
	$(QTBINDINGS_PC)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES = \
	AssemblyInfo.cs \
	Libs.cs \
	Gui/QApplication.cs \
	Core/QGlobal.cs \
	Core/QCoreApplication.cs \
	Core/QObject.cs \
	Gui/QPushButton.cs \
	Gui/QPaintDevice.cs \
	Gui/QWidget.cs \
	Gui/QAbstractButton.cs \
	Core/QString.cs \
	Core/QSize.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	QtBindings.dll.config \
	Gui \
	Core \
	qtbindings.pc.in 

REFERENCES =  \
	System

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

QTBINDINGS_DLL_CONFIG = $(BUILD_DIR)/QtBindings.dll.config
QTBINDINGS_PC = $(BUILD_DIR)/qtbindings.pc

$(eval $(call emit-deploy-target,QTBINDINGS_DLL_CONFIG))
$(eval $(call emit-deploy-target,MONO_VISUALC_INTEROP_DLL))
$(eval $(call emit-deploy-wrapper,QTBINDINGS_PC,qtbindings.pc))
$(eval $(call emit-deploy-target,CPPINTEROP_DLL))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
