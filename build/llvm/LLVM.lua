require "../scripts/Build"
require "../scripts/Utils"
require "../Helpers"

local llvm = path.getabsolute(builddir .. "/llvm/llvm-project")

-- Prevent premake from inserting /usr/lib64 search path on linux. GCC does not need this path specified
-- as compiler automatically knows where to look for system libs. Insertion of this path causes issues
-- when system has clang/llvm libs installed. Build would link to system libraries instead of custom
-- build required by CppSharp.
if os.istarget("linux") then
	premake.tools.gcc.libraryDirectories['architecture']['x86_64'] = function(cfg) return {} end
end

-- If we are inside vagrant then clone and build LLVM outside the shared folder,
-- otherwise file I/O performance will be terrible.
if is_vagrant() then
	llvm = os.getenv("HOME") .. "/llvm"
end

function get_llvm_rev()
	return cat(builddir .. "/llvm/LLVM-commit")
end

function clone_llvm()
  local llvm_release = get_llvm_rev()
  print("LLVM release: " .. llvm_release)

  if os.ishost("windows") then
    extract = extract_7z_tar_gz
  else
    extract = extract_tar_gz
  end

  local archive = 'llvm-'..llvm_release..'.tar.gz'
  if os.isfile(archive) then
    print('Archive '..archive..' already exists.')
  else
    download('https://github.com/llvm/llvm-project/archive/'..llvm_release..'.tar.gz', archive)
  end

  if os.isdir(llvm) then
    os.rmdir(llvm)
    if os.isdir(llvm) then
      print('Removing '..llvm..' directory failed. Please remove it manually and restart.')
      return
    end
  end

  extract(archive, '.')
  os.rename('llvm-project-'..llvm_release, llvm)

  -- delete the extracted archive
  print("Cleaning up downloaded artifacts...")
  os.remove(archive)
end

function get_vs_version()
  local function map_msvc_to_vs_version(major, minor)
	if major == "19" and minor >= "30" then return "vs2022"
    elseif major == "19" and minor >= "20" then return "vs2019"
    elseif major == "19" and minor >= "10" then return "vs2017"
    end
  end

  local function map_msbuild_to_vs_version(major, minor)
	if major == "17" then return "vs2022"
    elseif major == "16" then return "vs2019"
    elseif major == "15" then return "vs2017"
	end
  end

  local out = outputof("cl")  
  local major, minor = string.match(out, '(%d+).(%d+).%d+.?%d*%s+')  
  local version = map_msvc_to_vs_version(major, minor)
  if version ~= nil then
    return version
  end

  out = outputof("msbuild -version -nologo")
  major, minor = string.match(out, '(%d+).(%d+).') 
  version = map_msbuild_to_vs_version(major, minor)
  if version ~= nil then
    return version
  end

  if major ~= nil then
    error("Unsupported MSVC compiler version.") 
  else
    error("Unknown MSVC compiler version, run in VS command prompt.")
  end
end

function get_toolset_configuration_name(arch)
  if not arch then
    arch = target_architecture()
  end

  if os.istarget("windows") then
    local vsver = _ACTION

    if not string.starts(vsver, "vs") then
      vsver = get_vs_version()
    end

    return table.concat({vsver, arch}, "-")
  end
  -- FIXME: Implement for non-Windows platforms
  return table.concat({arch}, "-")
end

-- Returns a string describing the package configuration.
-- Example: llvm-f79c5c-windows-vs2015-x86-Debug
function get_llvm_package_name(rev, conf, arch)
  if not rev then
  	rev = get_llvm_rev()
  end
  rev = string.sub(rev, 0, 6)

  local components = {"llvm", rev, os.target()}

  local toolset = get_toolset_configuration_name(arch)
  table.insert(components, toolset)

	if os.istarget("linux") then
		if UseClang() then
			table.insert(components, "clang")
		else
			local version = string.match(GccVersion(), "%d+")
			table.insert(components, "gcc-"..version)
		end
  end

  if not conf then
  	conf = get_llvm_configuration_name()
  end

  table.insert(components, conf)

  return table.concat(components, "-")
end

function get_llvm_configuration_name(debug)
	if string.find(_OPTIONS["configuration"], "DebugOpt") then
		return "RelWithDebInfo"
	end
	if string.find(_OPTIONS["configuration"], "Debug") then
		return "Debug"
	end
	return "Release"
end

function get_7z_path()
	local path = "C:\\Program Files\\7-Zip\\7z.exe"
	if os.isfile(path) then
		return string.format('"%s"', path)
	end
	return "7z.exe"
end

function extract_7z_tar_gz(archive, dest_dir)
	extract_7z(archive, dest_dir)
	local tar = string.sub(archive, 1, -4)
	extract_7z(tar, dest_dir)
end

function extract_7z(archive, dest_dir)
	return execute(string.format("%s x %s -o%s -y", get_7z_path(),
		archive, dest_dir), false)
end

function extract_tar_xz(archive, dest_dir)
	execute("mkdir -p " .. dest_dir, true)
	return execute_or_die(string.format("tar xJf %s -C %s", archive, dest_dir), true)
end

function extract_tar_gz(archive, dest_dir)
	execute("mkdir -p " .. dest_dir, true)
	return execute_or_die(string.format("tar xf %s -C %s", archive, dest_dir), true)
end

local use_7zip = os.istarget("windows")
local archive_ext = use_7zip and ".7z" or ".tar.xz"

function download_llvm()
  local toolset = get_toolset_configuration_name()
  if toolset == "vs2012" or toolset == "vs2013" or toolset == "vs2015" then
    error("Pre-compiled LLVM packages for your VS version are not available.\n" ..
          "Please upgrade to a newer VS version or compile LLVM manually.")
  end

  local base = "https://github.com/mono/CppSharp/releases/download/CppSharp/"
  local pkg_name = get_llvm_package_name()
  local archive = pkg_name .. archive_ext

  -- check if we already have the file downloaded
  if os.isfile(archive) then
    print("Archive " .. archive .. " already exists.")
	return
  elseif os.isdir(pkg_name) then
    print("Directory " .. pkg_name .. " already exists.")
	return
  end
	
  msg, code = download(base .. archive, archive, true)
  
  if msg ~= "OK" then
  	if code == 404 then
      print("Error: " .. archive .. " is unavailable.")
      print("Please create your own LLVM package by executing the following commands:")
      print("./build.sh clone_llvm")
      print("./build.sh build_llvm")
      print("./build.sh package_llvm")
      os.exit(1)
    end
    
    error(msg)
  end

  -- extract the package
  if use_7zip then
    extract_7z(archive, pkg_name)
  else
    extract_tar_xz(archive, pkg_name)
  end

  -- delete the extracted archive
  print("Cleaning up downloaded artifacts...")
  os.remove(archive)
end

function cmake(gen, conf, builddir, options)
    local cwd = os.getcwd()
	os.chdir(builddir)

	local cmake = "cmake"
	result, error = os.outputof(cmake)
	local found = error ~= 127 -- command not found

	if not found and os.ishost("macosx") then
		local cmake = "/Applications/CMake.app/Contents/bin/cmake"
	end

	if options == nil then
		options = ""
	end

	if UseClang() and not os.ishost("macosx") then
		options = options .. " -DLLVM_USE_LINKER=/usr/bin/ld.lld"
    end

	if os.ishost("windows") then
		options = options .. " -Thost=x64"
		-- add flag to fix compile error C2226 in non English locales
		-- see: https://github.com/llvm/llvm-project/issues/60549 
		options = options .. ' -DCMAKE_CXX_FLAGS="/utf-8"'
	end

	local cmd = cmake .. " -G " .. '"' .. gen .. '"'
		.. ' -DLLVM_BUILD_TOOLS=false'
		.. ' -DLLVM_ENABLE_DUMP=true'
		.. ' -DLLVM_ENABLE_PROJECTS="clang;lld"'
		.. ' -DLLVM_INCLUDE_TESTS=false'
		.. ' -DLLVM_ENABLE_LIBEDIT=false'
		.. ' -DLLVM_ENABLE_LIBXML2=false'
		.. ' -DLLVM_ENABLE_MLGO=false'
		.. ' -DLLVM_ENABLE_TERMINFO=false'
		.. ' -DLLVM_ENABLE_ZLIB=false'
		.. ' -DLLVM_ENABLE_ZSTD=false'
		.. ' -DLLVM_INCLUDE_DOCS=false'
		.. ' -DLLVM_INCLUDE_EXAMPLES=false'
		.. ' -DLLVM_TARGETS_TO_BUILD="X86;AArch64"'
		.. ' -DLLVM_TOOL_BUGPOINT_BUILD=false'
		.. ' -DLLVM_TOOL_BUGPOINT_PASSES_BUILD=false'
		.. ' -DLLVM_TOOL_CLANG_TOOLS_EXTRA_BUILD=false'
		.. ' -DLLVM_TOOL_COMPILER_RT_BUILD=false'
		.. ' -DLLVM_TOOL_CROSS_PROJECT_TESTS_BUILD=false'
		.. ' -DLLVM_TOOL_DRAGONEGG_BUILD=false'
		.. ' -DLLVM_TOOL_DSYMUTIL_BUILD=false'
		.. ' -DLLVM_TOOL_FLANG_BUILD=false'
		.. ' -DLLVM_TOOL_GOLD_BUILD=false'
		.. ' -DLLVM_TOOL_LIBCLC_BUILD=false'
		.. ' -DLLVM_TOOL_LIBCXXABI_BUILD=false'
		.. ' -DLLVM_TOOL_LIBCXX_BUILD=false'
		.. ' -DLLVM_TOOL_LIBC_BUILD=false'
		.. ' -DLLVM_TOOL_LIBUNWIND_BUILD=false'
		.. ' -DLLVM_TOOL_LLC_BUILD=false'
		.. ' -DLLVM_TOOL_LLDB_BUILD=false'
		.. ' -DLLVM_TOOL_LLI_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_AR_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_AS_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_AS_FUZZER_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_BCANALYZER_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_CAT_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_CFI_VERIFY_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_CONFIG_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_COV_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_CVTRES_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_CXXDUMP_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_CXXFILT_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_CXXMAP_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_C_TEST_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_DIFF_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_DIS_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_DWARFDUMP_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_DWP_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_EXEGESIS_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_EXTRACT_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_GO_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_GSYMUTIL_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_IFS_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_ISEL_FUZZER_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_ITANIUM_DEMANGLE_FUZZER_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_JITLINK_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_JITLISTENER_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_LIBTOOL_DARWIN_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_LINK_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_LIPO_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_LTO2_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_LTO_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_MCA_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_MC_ASSEMBLE_FUZZER_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_MC_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_MC_DISASSEMBLE_FUZZER_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_MICROSOFT_DEMANGLE_FUZZER_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_ML_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_MODEXTRACT_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_MT_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_NM_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_OBJCOPY_BUILD=true'
		.. ' -DLLVM_TOOL_LLVM_OBJDUMP_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_OPT_FUZZER_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_OPT_REPORT_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_PDBUTIL_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_PROFDATA_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_PROFGEN_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_RC_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_READOBJ_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_REDUCE_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_RTDYLD_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_RUST_DEMANGLE_FUZZER_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_SHLIB_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_SIM_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_SIZE_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_SPECIAL_CASE_LIST_FUZZER_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_SPLIT_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_STRESS_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_STRINGS_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_SYMBOLIZER_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_TAPI_DIFF_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_UNDNAME_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_XRAY_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_YAML_NUMERIC_PARSER_FUZZER_BUILD=false'
		.. ' -DLLVM_TOOL_LLVM_YAML_PARSER_FUZZER_BUILD=false'
		.. ' -DLLVM_TOOL_LTO_BUILD=false'
		.. ' -DLLVM_TOOL_MLIR_BUILD=false'
		.. ' -DLLVM_TOOL_OBJ2YAML_BUILD=false'
		.. ' -DLLVM_TOOL_OPENMP_BUILD=false'
		.. ' -DLLVM_TOOL_OPT_BUILD=false'
		.. ' -DLLVM_TOOL_OPT_VIEWER_BUILD=false'
		.. ' -DLLVM_TOOL_POLLY_BUILD=false'
		.. ' -DLLVM_TOOL_PSTL_BUILD=false'
		.. ' -DLLVM_TOOL_REMARKS_SHLIB_BUILD=false'
		.. ' -DLLVM_TOOL_SANCOV_BUILD=false'
		.. ' -DLLVM_TOOL_SANSTATS_BUILD=false'
		.. ' -DLLVM_TOOL_SPLIT_FILE_BUILD=false'
		.. ' -DLLVM_TOOL_VERIFY_USELISTORDER_BUILD=false'
		.. ' -DLLVM_TOOL_VFABI_DEMANGLE_FUZZER_BUILD=false'
		.. ' -DLLVM_TOOL_XCODE_TOOLCHAIN_BUILD=false'
		.. ' -DLLVM_TOOL_YAML2OBJ_BUILD=true'
		.. ' -DLLVM_TOOL_LLVM_OBJCOPY_BUILD=true'
		.. ' -DLLVM_HAVE_LIBXAR=false'
		.. ' -DCLANG_BUILD_EXAMPLES=false '
		.. ' -DCLANG_BUILD_TOOLS=false'
		.. ' -DCLANG_ENABLE_ARCMT=false'
		.. ' -DCLANG_ENABLE_STATIC_ANALYZER=false'
		.. ' -DCLANG_INCLUDE_DOCS=false '
		.. ' -DCLANG_INCLUDE_TESTS=false'
		.. ' -DCLANG_TOOL_AMDGPU_ARCH_BUILD=false'
		.. ' -DCLANG_TOOL_APINOTES_TEST_BUILD=false'
		.. ' -DCLANG_TOOL_ARCMT_TEST_BUILD=false'
		.. ' -DCLANG_TOOL_CLANG_CHECK_BUILD=false'
		.. ' -DCLANG_TOOL_CLANG_DIFF_BUILD=false'
		.. ' -DCLANG_TOOL_CLANG_EXTDEF_MAPPING_BUILD=false'
		.. ' -DCLANG_TOOL_CLANG_FORMAT_BUILD=false'
		.. ' -DCLANG_TOOL_CLANG_FORMAT_VS_BUILD=false'
		.. ' -DCLANG_TOOL_CLANG_FUZZER_BUILD=false'
		.. ' -DCLANG_TOOL_CLANG_IMPORT_TEST_BUILD=false'
		.. ' -DCLANG_TOOL_CLANG_NVLINK_WRAPPER_BUILD=false'
		.. ' -DCLANG_TOOL_CLANG_LINKER_WRAPPER_BUILD=false'
		.. ' -DCLANG_TOOL_CLANG_OFFLOAD_PACKAGER_BUILD=false'
		.. ' -DCLANG_TOOL_CLANG_OFFLOAD_BUNDLER_BUILD=false'
		.. ' -DCLANG_TOOL_CLANG_OFFLOAD_WRAPPER_BUILD=false'
		.. ' -DCLANG_TOOL_CLANG_REFACTOR_BUILD=false'
		.. ' -DCLANG_TOOL_CLANG_RENAME_BUILD=false'
		.. ' -DCLANG_TOOL_CLANG_REPL_BUILD=false'
		.. ' -DCLANG_TOOL_CLANG_SCAN_DEPS_BUILD=false'
		.. ' -DCLANG_TOOL_CLANG_SHLIB_BUILD=false'
		.. ' -DCLANG_TOOL_C_ARCMT_TEST_BUILD=false'
		.. ' -DCLANG_TOOL_C_INDEX_TEST_BUILD=false'
		.. ' -DCLANG_TOOL_DIAGTOOL_BUILD=false'
		.. ' -DCLANG_TOOL_DRIVER_BUILD=false'
		.. ' -DCLANG_TOOL_LIBCLANG_BUILD=false'
		.. ' -DCLANG_TOOL_SCAN_BUILD_BUILD=false'
		.. ' -DCLANG_TOOL_SCAN_BUILD_PY_BUILD=false'
		.. ' -DCLANG_TOOL_SCAN_VIEW_BUILD=false'
		.. ' -DCMAKE_BUILD_TYPE=' .. conf 
		.. ' ../llvm'
		.. ' -DCMAKE_OSX_DEPLOYMENT_TARGET=10.13'
		.. ' ' .. options
 	execute_or_die(cmd)
 	os.chdir(cwd)
end

function clean_llvm(llvm_build)
	if os.isdir(llvm_build) then os.rmdir(llvm_build) end
end

function get_cmake_generator()
	local vsver = get_vs_version()
	if vsver == "vs2022" then
		return "Visual Studio 17 2022", (target_architecture() == "x86") and "-A Win32" or nil
	elseif vsver == "vs2019" then
		return "Visual Studio 16 2019", (target_architecture() == "x86") and "-A Win32" or nil
	elseif vsver == "vs2017" then
		return "Visual Studio 15 2017" .. (target_architecture() == "x64" and " Win64" or ""), nil
	elseif vsver == "vs2015" then
		return "Visual Studio 14 2015" .. (target_architecture() == "x64" and " Win64" or ""), nil
	else
		error("Cannot map to CMake configuration due to unknown MSVC version")
	end
end

function build_llvm(llvm_build)
	if not os.isdir(llvm) then
		print("LLVM/Clang directory does not exist, cloning...")
		clone_llvm()
	end

	os.mkdir(llvm_build)

	local conf = get_llvm_configuration_name()
	local use_msbuild = true
	if os.ishost("windows") and use_msbuild then
		local cmake_generator, options = get_cmake_generator()
		cmake(cmake_generator, conf, llvm_build, options)
		local llvm_sln = path.join(llvm_build, "LLVM.sln")
		platform = nil
		if target_architecture() == "x86" then
			platform = "Win32"
		end
		msbuild('"' .. llvm_sln .. '"', conf, platform)
	else
		local options = os.ishost("macosx") and
			"-DLLVM_ENABLE_LIBCXX=true" or ""
		local is32bits = target_architecture() == "x86"
		local targetIsArm64 = target_architecture() == "arm64"

		if is32bits then
			options = options .. (is32bits and " -DLLVM_BUILD_32_BITS=true" or "")
		end

		if targetIsArm64 then
			if os.host() == "linux" then
				local host_arch = unix_host_architecture()
				if host_arch ~= "aarch64" then
					options = options .. "" 
						.. ' -DCMAKE_SYSTEM_NAME=Linux'
						.. ' -DCMAKE_SYSTEM_PROCESSOR=aarch64'
						.. ' -DCMAKE_C_COMPILER=/usr/bin/aarch64-linux-gnu-gcc'
						.. ' -DCMAKE_CXX_COMPILER=/usr/bin/aarch64-linux-gnu-g++'
						.. ' -DCMAKE_ASM_COMPILER=/usr/bin/aarch64-linux-gnu-as'
						.. ' -DCMAKE_FIND_ROOT_PATH_MODE_PROGRAM=NEVER'
						.. ' -DCMAKE_FIND_ROOT_PATH_MODE_LIBRARY=ONLY'
						.. ' -DCMAKE_FIND_ROOT_PATH_MODE_INCLUDE=ONLY'
						.. ' -DCMAKE_FIND_ROOT_PATH_MODE_PACKAGE=ONLY'
						.. ' ' .. options
				end
			end
			if os.host() == "macosx" then
				local host_arch = unix_host_architecture()
				if host_arch ~= "arm64" then
					options = options .. "" 
						.. ' -DCMAKE_SYSTEM_NAME=Darwin'
						.. ' -DCMAKE_SYSTEM_PROCESSOR=arm64'
						.. ' -DCMAKE_C_COMPILER_TARGET=arm64-apple-darwin21.6.0'
						.. ' -DCMAKE_CXX_COMPILER_TARGET=arm64-apple-darwin21.6.0'
						.. ' -DCMAKE_ASM_COMPILER_TARGET=arm64-apple-darwin21.6.0'
						.. ' ' .. options
				end
			end
		end


		cmake("Ninja", conf, llvm_build, options)
		ninja('"' .. llvm_build .. '"')
		ninja('"' .. llvm_build .. '"', "clang-headers")
	end
end

function package_llvm(conf, llvm_base, llvm_build)
	local rev = git.rev_parse('"' .. llvm_base .. '"', "HEAD")
	if string.is_empty(rev) then
		rev = get_llvm_rev()
	end

	local out = get_llvm_package_name(rev, conf)

	if os.isdir(out) then os.rmdir(out)	end
	os.mkdir(out)

	os.copydir(llvm_base .. "/llvm/include", out .. "/llvm/include")
	os.copydir(llvm_base .. "/lld/include", out .. "/lld/include")
	os.copydir(llvm_build .. "/include", out .. "/build/include")

	local llvm_msbuild_libdir = "/" .. conf .. "/lib"
	local lib_dir =  (os.ishost("windows") and os.isdir(llvm_build .. llvm_msbuild_libdir))
		and llvm_msbuild_libdir or "/lib"
	local llvm_build_libdir = llvm_build .. lib_dir

	if os.ishost("windows") and os.isdir(llvm_build_libdir) then
		os.copydir(llvm_build_libdir, out .. "/build/lib", "*.lib")
	else
		os.copydir(llvm_build_libdir, out .. "/build/lib", "*.a")
	end

    -- Copy natvis visualizers for a better debugging experience
	os.copyfile(llvm_base .. "/clang/utils/ClangVisualizers/clang.natvis", out .. "/utils/clang.natvis")
	os.copyfile(llvm_base .. "/llvm/utils/LLVMVisualizers/llvm.natvis", out .. "/utils/llvm.natvis")

	os.copydir(llvm_base .. "/clang/include", out .. "/clang/include")
	os.copydir(llvm_build .. "/tools/clang/include", out .. "/build/clang/include")

	os.copydir(llvm_build_libdir .. "/clang", out .. "/lib/clang")

	os.copydir(llvm_base .. "/clang/lib/CodeGen", out .. "/clang/lib/CodeGen", "*.h")
	os.copydir(llvm_base .. "/clang/lib/Driver", out .. "/clang/lib/Driver", "*.h")
	os.copydir(llvm_base .. "/clang/lib/Driver/ToolChains", out .. "/clang/lib/Driver/ToolChains", "*.h")

	local out_lib_dir = out .. "/build/lib"
	if os.ishost("windows") then
		os.rmfiles(out_lib_dir, "clang*ARC*.lib")
		os.rmfiles(out_lib_dir, "clang*Rewrite*.lib")
		os.rmfiles(out_lib_dir, "clang*StaticAnalyzer*.lib")
	else
		os.rmfiles(out_lib_dir, "libclang*ARC*.a")
		os.rmfiles(out_lib_dir, "libclang*Rewrite*.a")
		os.rmfiles(out_lib_dir, "libclang*StaticAnalyzer*.a")
	end

	return out
end

function archive_llvm(dir)
	local archive = dir .. archive_ext
	if os.isfile(archive) then os.remove(archive) end
	local cwd = os.getcwd()
	os.chdir(dir)
	if use_7zip then
		execute_or_die(string.format("%s a -mx9 %s *", get_7z_path(),
			path.join("..", archive)))
	else
		execute_or_die("tar cJf " .. path.join("..", archive) .. " *")
	end
	
	os.chdir(cwd)
	if is_vagrant() then
		os.copyfile(archive, "/cppsharp")
	end
end

if _ACTION == "clone_llvm" then
  clone_llvm()
  os.exit()
end

if _ACTION == "build_llvm" then
  local llvm_build = path.join(llvm, get_llvm_package_name())
  clean_llvm(llvm_build)
  build_llvm(llvm_build)
  os.exit()
end

if _ACTION == "package_llvm" then
  local conf = get_llvm_configuration_name()
  local llvm_build = path.join(llvm, get_llvm_package_name())
  local pkg = package_llvm(conf, llvm, llvm_build)
  archive_llvm(pkg)
  os.exit()
end

if _ACTION == "download_llvm" then
  download_llvm()
  os.exit()
end
