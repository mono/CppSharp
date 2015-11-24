require "Utils"

function download_ninja()
	local system = "";
	if os.is("windows") then
		system = "win"
	elseif os.is("macosx") then
		system = "mac"
	elseif os.is("linux") then
		system = "linux"
	else
		error("Error downloading Ninja for unknown system")
	end

	local url = "https://github.com/ninja-build/ninja/releases/download/v1.6.0/ninja-" .. system .. ".zip"
	local file = "ninja.zip"

	if not os.isfile(file) then
		download(url, file)
	end

	if os.isfile(file) then
		print("Extracting file " .. file)
		zip.extract(file, "ninja")
	end
end

function download_cmake()
	local system = "";
	if os.is("windows") then
		system = "win32-x86.zip"
	elseif os.is("macosx") then
		system = "Darwin-x86_64.dmg"
	elseif os.is("linux") then
		system = "Linux-x86_64.tar.gz"
	else
		error("Error downloading CMake for unknown system")
	end

	local url = "https://cmake.org/files/v3.4/cmake-3.4.0-" .. system
	local file = "cmake.zip"

	if not os.isfile(file) then
		download(url, file)
	end
end

download_ninja()
download_cmake()