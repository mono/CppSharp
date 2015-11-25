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

	execute(cmd)
end



