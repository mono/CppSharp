require "Utils"

function get_msbuild_path()
	local msbuild = '%SystemRoot%\\system32\\reg.exe query "HKLM\\SOFTWARE\\Microsoft\\MSBuild\\ToolsVersions\\4.0" /v MSBuildToolsPath'
	local val = execute(msbuild)

	for i in string.gmatch(val, "%S+") do
		if os.isdir(i) then
			return i
		end
	end

	error("MSBuild path could not be found in Windows registry.")
end

function msbuild(sln, conf)
	local msbuild_path = path.normalize(path.join(get_msbuild_path(), "msbuild.exe"))
	local sln = path.normalize(sln)

	local cmd = msbuild_path .. " " .. sln
	if conf ~= nil then
		cmd = cmd .. " /p:Configuration=" .. conf
	end

	execute_or_die(cmd)
end

function ninja(dir, action)
	local cmd = "ninja -C " .. dir
	if action then
		cmd = cmd .. " " .. action
	end
	return execute_or_die(cmd)
end

function run_premake(file, action)
	-- search for file with extension Lua
	if os.isfile(file .. ".lua") then
		file = file .. ".lua"
	end

	local cmd = string.format("%s --file=%s %s", _PREMAKE_COMMAND, file, action)
	return execute_or_die(cmd)
end

function build_cppsharp()
	-- install dependencies
	run_premake("Provision", "provision")

	-- if there is an llvm git clone then use it
	-- otherwise download and extract llvm/clang build

	run_premake("LLVM", "download_llvm")

	-- generate project files
	run_premake("../premake4", "gmake")

	-- build cppsharp
	--[[BUILD_CONF=release_x32;
	config=$BUILD_CONF make -C build/gmake/
	BUILD_DIR=`ls build/gmake/lib`
	mkdir -p "$PWD"/build/gmake/lib/lib/"$BUILD_DIR"
	cp "$PWD"/build/gmake/lib/"$BUILD_DIR"/libNamespacesBase.* "$PWD"/build/gmake/lib/lib/"$BUILD_DIR"
	]]
end

if _ACTION == "build_cppsharp" then
  build_cppsharp()
  os.exit()
end


