

# Warning: This is an automatically generated file, do not edit!

ASSEMBLY_COMPILER_FLAGS =

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS +=  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = bin/Debug/Plot.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =
BUILD_DIR = bin/Debug

PLOT_SHARP_DLL_MDB_SOURCE=bin/Debug/Plot.dll.mdb
PLOT_SHARP_DLL_MDB=$(BUILD_DIR)/Plot.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS +=  -noconfig -codepage:utf8 -warn:4 -optimize-
ASSEMBLY = bin/Release/Plot.dll
ASSEMBLY_MDB =
COMPILE_TARGET = library
PROJECT_REFERENCES =
BUILD_DIR = bin/Release

PLOT_SHARP_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(PLOT_SHARP_DLL_MDB)

LINUX_PKGCONFIG = \
	$(PLOT_SHARP_PC)


RESGEN=resgen2

all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG)

FILES = \
	Plot/AssemblyInfo.cs

DATA_FILES =

RESOURCES =

EXTRAS = \
	plot-sharp.pc.in

REFERENCES =  \
	System

DLL_REFERENCES =

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG)

PLOT_SHARP_PC = $(BUILD_DIR)/plot-sharp-@LIBPLOT_SHARP_API_VERSION@.pc
PLOT_SHARP_API_PC = plot-sharp-@LIBPLOT_SHARP_API_VERSION@.pc

pc_files = $(PLOT_SHARP_API_PC)

include $(top_srcdir)/Makefile.include

$(eval $(call emit-deploy-wrapper,PLOT_SHARP_PC,$(PLOT_SHARP_API_PC)))

$(PLOT_SHARP_API_PC): plot-sharp.pc
	cp $< $@

$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)

install-data-hook:
	for ASM in $(INSTALLED_ASSEMBLIES); do \
		$(INSTALL) -c -m 0755 $$ASM $(DESTDIR)$(pkglibdir); \
	done;

uninstall-hook:
	for ASM in $(INSTALLED_ASSEMBLIES); do \
		rm -f $(DESTDIR)$(pkglibdir)/`basename $$ASM`; \
	done
