require "Utils"

function nuget()
  if not os.isfile("nuget.exe") then
    http.download("https://nuget.org/nuget.exe")
  end

  local nugetexe = os.is("windows") and "NuGet.exe" or "mono ./NuGet.exe"
  execute(nugetexe .. " restore packages.config -PackagesDirectory ../deps")
end

clone()

if _ACTION == "nuget" then
  nuget()
  os.exit()
end
