require "Utils"

local llvm_build = "../../deps/llvm/build"

function clean_build()
	os.rmdir(llvm_build)
	os.mkdir(llvm_build)
end

function cmake(gen)
	os.chdir(llvm_build)
	local cmd = "cmake -G " .. '"' .. gen .. '"'
		.. " -DCLANG_BUILD_EXAMPLES=false -DCLANG_INCLUDE_DOCS=false -DCLANG_INCLUDE_TESTS=false"
 		.. ' -DCLANG_INCLUDE_DOCS=false -DCLANG_BUILD_EXAMPLES=false -DLLVM_TARGETS_TO_BUILD="X86"'
 		.. ' -DLLVM_INCLUDE_EXAMPLES=false -DLLVM_INCLUDE_DOCS=false -DLLVM_INCLUDE_TESTS=false -DCMAKE_BUILD_TYPE=Release ..'
 	execute(cmd)
end

function get_msbuild_path()
	local msbuild = '%WINDIR%\\system32\\reg.exe query "HKLM\\SOFTWARE\\Microsoft\\MSBuild\\ToolsVersions\\4.0" /v MSBuildToolsPath'
	local val = execute(msbuild)

	for i in string.gmatch(val, "%S+") do
		if os.isdir(i) then
			return i
		end
	end

	error("MSBuild path could not be found in Windows registry.")
end

function msbuild()
	local msbuild_path = path.normalize(path.join(get_msbuild_path(), "msbuild.exe"))
	execute(msbuild_path .. " " .. llvm_build)
end

function ninja()
	execute("ninja")
	execute("ninja clang-headers")
end

clean_build()
cmake("Visual Studio 14 2015")
