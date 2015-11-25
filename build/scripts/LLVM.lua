require "Build"
require "Utils"

local llvm = "../../deps/llvm/"
local llvm_build = "../../deps/llvm/" .. os.get()

function clone_llvm()
  local llvm_release = cat("LLVM-commit")
  print("LLVM release: " .. llvm_release)

  local clang_release = cat("Clang-commit")
  print("Clang release: " .. clang_release)

  if not os.isdir(llvm) then
    git.clone(llvm, "http://llvm.org/git/llvm.git")
    git.checkout(llvm, llvm_release)
  end
  
  if not os.isdir(llvm .. "/tools/clang") then
    git.clone(llvm .. "/tools/clang", "http://llvm.org/git/clang.git")
    git.checkout(llvm .. "/tools/clang", clang_release)
  end
end

function download_llvm()
  if os.is("windows") then
    http.download("https://dl.dropboxusercontent.com/u/194502/CppSharp/llvm_windows_x86.7z")
  end

  -- extract the package
  execute("7z x llvm_windows_x86.7z -o%DEPS_PATH%\llvm -y > nul")
end

function cmake(gen, conf)
	print(os.getcwd())
	print(llvm_build)
	os.chdir(llvm_build)
	print(os.getcwd())
	local cmd = "cmake -G " .. '"' .. gen .. '"'
		.. ' -DCLANG_BUILD_EXAMPLES=false -DCLANG_INCLUDE_DOCS=false -DCLANG_INCLUDE_TESTS=false'
		.. ' -DCLANG_ENABLE_ARCMT=false -DCLANG_ENABLE_REWRITER=false -DCLANG_ENABLE_STATIC_ANALYZER=false'
 		.. ' -DLLVM_INCLUDE_EXAMPLES=false -DLLVM_INCLUDE_DOCS=false -DLLVM_INCLUDE_TESTS=false'
 		.. ' -DLLVM_TARGETS_TO_BUILD="X86"'
 		.. ' -DCMAKE_BUILD_TYPE=' .. conf .. ' ..'
 	execute(cmd)
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
	local out = "llvm-" .. os.get() .. "-" .. conf

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
	execute("7z a " .. out .. ".7z " .. "./" .. out .. "/*")
end

clean_llvm(llvm_build)
build_llvm(llvm_build)
--local out = package_llvm("RelWithDebInfo", llvm, llvm_build)
--archive_llvm(out)