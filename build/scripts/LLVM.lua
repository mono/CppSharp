require "Build"
require "Utils"

local llvm = "../../deps/llvm"

-- If we are inside vagrant then clone and build LLVM outside the shared folder,
-- otherwise file I/O performance will be terrible.
if is_vagrant() then
	llvm = "~/llvm"
end

local llvm_build = llvm .. "/" .. os.get()

function clone_llvm()
  local llvm_release = cat("../LLVM-commit")
  print("LLVM release: " .. llvm_release)

  local clang_release = cat("../Clang-commit")
  print("Clang release: " .. clang_release)

  if os.isdir(llvm) and not os.isdir(llvm .. "/.git") then
    error("LLVM directory is not a git repository.")
  end

  if not os.isdir(llvm) then
    git.clone(llvm, "http://llvm.org/git/llvm.git")
  else
    git.reset_hard(llvm, "HEAD")
    git.pull_rebase(llvm)
  end

  local clang = llvm .. "/tools/clang"
  if not os.isdir(clang) then
    git.clone(clang, "http://llvm.org/git/clang.git")
  else
    git.reset_hard(clang, "HEAD")
    git.pull_rebase(clang)
  end

  git.reset_hard(llvm, llvm_release)
  git.reset_hard(clang, clang_release)
end

function get_llvm_package_name(rev, conf)
  return table.concat({"llvm", rev, os.get(), conf}, "-")
end

function extract_7z(archive, dest_dir)
	return execute(string.format("7z x %s -o%s -y", archive, dest_dir), true)
end

function download_llvm()
  local base = "https://dl.dropboxusercontent.com/u/194502/CppSharp/llvm/"
  local conf = os.is("windows") and "RelWithDebInfo" or "Release"
  local rev = string.sub(cat("../LLVM-commit"), 0, 6)
  local pkg_name = get_llvm_package_name(rev, conf)
  local archive = pkg_name .. ".7z"

  -- check if we already have the file downloaded
  if not os.isfile(archive) then
	  download(base .. archive, archive)
  end

  -- extract the package
  if not os.isdir(pkg_name) then
  	extract_7z(archive, pkg_name)
  end
end

function cmake(gen, conf)
	local cwd = os.getcwd()
	os.chdir(llvm_build)
	local cmd = "cmake -G " .. '"' .. gen .. '"'
		.. ' -DCLANG_BUILD_EXAMPLES=false -DCLANG_INCLUDE_DOCS=false -DCLANG_INCLUDE_TESTS=false'
		.. ' -DCLANG_ENABLE_ARCMT=false -DCLANG_ENABLE_REWRITER=false -DCLANG_ENABLE_STATIC_ANALYZER=false'
 		.. ' -DLLVM_INCLUDE_EXAMPLES=false -DLLVM_INCLUDE_DOCS=false -DLLVM_INCLUDE_TESTS=false'
 		.. ' -DLLVM_TARGETS_TO_BUILD="X86"'
 		.. ' -DCMAKE_BUILD_TYPE=' .. conf .. ' ..'
 	execute(cmd)
 	os.chdir(cwd)
end

function clean_llvm(llvm_build)
	if os.isdir(llvm_build) then os.rmdir(llvm_build)	end
	os.mkdir(llvm_build)
end

function build_llvm(llvm_build)
	if os.is("windows") then
		cmake("Visual Studio 12 2013", "RelWithDebInfo")

		local llvm_sln = path.join(llvm_build, "LLVM.sln")
		msbuild(llvm_sln, "RelWithDebInfo")
	else
		cmake("Ninja", "Release")
		execute("ninja")
		execute("ninja clang-headers")
	end
end

function package_llvm(conf, llvm, llvm_build)
	local rev = string.sub(git.rev_parse(llvm, "HEAD"), 0, 6)
	local out = get_llvm_package_name(rev, conf)

	if os.isdir(out) then os.rmdir(out)	end
	os.mkdir(out)

	os.copydir(llvm .. "/include", out .. "/include")
	os.copydir(llvm_build .. "/include", out .. "/build/include")

	local lib_dir =  os.is("windows") and "/" .. conf .. "/lib" or "/lib"
	local llvm_build_libdir = llvm_build .. lib_dir

	if os.is("windows") then
		os.copydir(llvm_build_libdir, out .. "/build" .. lib_dir, "*.lib")
	else
		os.copydir(llvm_build_libdir, out .. "/build/lib", "*.a")
	end

	os.copydir(llvm .. "/tools/clang/include", out .. "/tools/clang/include")
	os.copydir(llvm_build .. "/tools/clang/include", out .. "/build/tools/clang/include")

	os.copydir(llvm .. "/tools/clang/lib/CodeGen", out .. "/tools/clang/lib/CodeGen", "*.h")
	os.copydir(llvm .. "/tools/clang/lib/Driver", out .. "/tools/clang/lib/Driver", "*.h")

	local out_lib_dir = out .. "/build" .. lib_dir
	if os.is("windows") then
		os.rmfiles(out_lib_dir, "LLVM*ObjCARCOpts*.lib")
		os.rmfiles(out_lib_dir, "clang*ARC*.lib")
		os.rmfiles(out_lib_dir, "clang*Matchers*.lib")
		os.rmfiles(out_lib_dir, "clang*Rewrite*.lib")
		os.rmfiles(out_lib_dir, "clang*StaticAnalyzer*.lib")
		os.rmfiles(out_lib_dir, "clang*Tooling*.lib")		
	else
		os.rmfiles(out_lib_dir, "libllvm*ObjCARCOpts*.a")
		os.rmfiles(out_lib_dir, "libclang*ARC*.a")
		os.rmfiles(out_lib_dir, "libclang*Matchers*.a")
		os.rmfiles(out_lib_dir, "libclang*Rewrite*.a")
		os.rmfiles(out_lib_dir, "libclang*StaticAnalyzer*.a")
		os.rmfiles(out_lib_dir, "libclang*Tooling*.a")
	end

	return out
end

function archive_llvm(dir)
	execute("7z a " .. dir .. ".7z " .. "./" .. dir .. "/*")
end

if _ACTION == "clone_llvm" then
  clone_llvm()
  os.exit()
end

if _ACTION == "build_llvm" then
	clean_llvm(llvm_build)
	build_llvm(llvm_build)
  os.exit()
end

if _ACTION == "package_llvm" then
	local pkg = package_llvm("RelWithDebInfo", llvm, llvm_build)
	archive_llvm(pkg)
  os.exit()
end

if _ACTION == "download_llvm" then
  download_llvm()
  os.exit()
end
