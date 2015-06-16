-- This module downloads the required dependencies.

llvm_release = "0e8abfa6ed986c892ec723236e32e78fd9c47b88"
clang_release = "3457cd5516ac741fa106623d9578f5ac88593f4d"

function OSExecute(cmd)
  -- There seems to be a problem with putting http links into os.execute under linux
  local file = assert(io.popen(cmd, "r"))
  local output = file:read('*all')
  file:close()
  return output
end

function DownloadNuGet()
  local nugetexe = "mono ./NuGet.exe"
  if os.is("windows") then nugetexe = "NuGet.exe" end
  OSExecute(nugetexe .. " restore packages.config -PackagesDirectory ../deps")
end

function DownloadGitClone()
  local mkdircmd = "mkdir ../deps"
  if os.is("windows") then mkdircmd = "mkdir ..\\deps" end
  OSExecute(mkdircmd)
  OSExecute("git -C ../deps clone http://llvm.org/git/llvm.git llvm")
  OSExecute("git -C ../deps/llvm checkout " .. llvm_release)
  OSExecute("git -C ../deps/llvm/tools clone http://llvm.org/git/clang.git clang")
  OSExecute("git -C ../deps/llvm/tools/clang checkout " .. clang_release)
end

if _ACTION == "depend_nuget" then
  DownloadNuGet()
  os.exit()
end

if _ACTION == "depend_gitclone" then
  DownloadGitClone()
  os.exit()
end
