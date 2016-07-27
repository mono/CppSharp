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

	local url = "https://cmake.org/files/v3.6/cmake-3.6.1-" .. system
	local file = "cmake" .. path.getextension(system)

	if not os.isfile(file) then
		download(url, file)
	end
end

function download_nuget()
  if not os.isfile("nuget.exe") then
    download("https://nuget.org/nuget.exe", "nuget.exe")
  end
end

function restore_nuget_packages()
  local nugetexe = os.is("windows") and "NuGet.exe" or "mono ./NuGet.exe"
  execute(nugetexe .. " restore packages.config -PackagesDirectory " .. depsdir)
end

local compile_llvm = is_vagrant()

function provision_linux()
	-- Add Repos
	sudo("apt-key adv --keyserver http://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF")
	sudo("echo \"deb http://download.mono-project.com/repo/debian wheezy main\" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list")

	sudo("apt-get update")

	-- Build tools
	sudo("apt-get install -y git build-essential clang")

	-- Mono
	sudo("apt-get install -y mono-devel")

	-- LLVM/Clang build tools
	if compile_llvm then
		sudo("apt-get install -y ninja-build")
		download_cmake()
	end
end

function brew_install(pkg)
	-- check if package is already installed
	local res = outputof("brew ls --versions " .. pkg)
	if string.is_empty(res) then
		execute("brew install " .. pkg)
	end 
end

function provision_osx()
	if compile_llvm then
		execute("brew cask install virtualbox vagrant")
	end
  	download_cmake()
end

if _ACTION == "cmake" then
	download_cmake()
	os.exit()
end

if _ACTION == "provision" then
  if os.is("linux") then
  	provision_linux()
  elseif os.is("macosx") then
  	provision_osx()
  end
  os.exit()
end


