require "Utils"

function clone()
  if not os.isdir("../deps") then
    print("mkdir")
    os.mkdir(".../deps")
  end

  local llvm_release = cat("LLVM-commit")
  print("LLVM release: " .. llvm_release)

  local clang_release = cat("Clang-commit")
  print("Clang release: " .. clang_release)

  if not os.isdir("../deps/llvm") then
    git.clone("../deps/llvm", "http://llvm.org/git/llvm.git")
    git.checkout("../deps/llvm", llvm_release)
  end
  
  if not os.isdir("../deps/llvm/tools/clang") then
    git.clone("../deps/llvm/tools/clang", "http://llvm.org/git/clang.git")
    git.checkout("../deps/llvm/tools/clang", clang_release)
  end
end

function download()
  if os.is("windows") then
    http.download("https://dl.dropboxusercontent.com/u/194502/CppSharp/llvm_windows_x86.7z")
  end

  -- extract the package
  7z x llvm_windows_x86.7z -o%DEPS_PATH%\llvm -y > nul
end

function nuget()
  if not os.isfile("nuget.exe")
    http.download("https://nuget.org/nuget.exe")
  end

  local nugetexe = os.is("windows") and "NuGet.exe" or "mono ./NuGet.exe"
  execute(nugetexe .. " restore packages.config -PackagesDirectory ../deps")
end

if _ACTION == "nuget" then
  nuget()
  os.exit()
end

if _ACTION == "clone" then
  clone()
  os.exit()
end

if _ACTION == "download" then
  download()
  os.exit()
end