package.path = package.path .. "../build/?.lua;../../build/scripts/?.lua"
require "Helpers"
require "Utils"

function clone_sdl()
  local sdl = path.getabsolute(examplesdir .. "/SDL/SDL-2.0")
  local repo = "https://github.com/spurious/SDL-mirror.git"

  if os.isdir(sdl) and not os.isdir(sdl .. "/.git") then
    error("SDL directory is not a git repository.")
  end

  if not os.isdir(sdl) then
    git.clone(sdl, repo)
  end
end

if _ACTION == "clone_sdl" then
  clone_sdl()
  os.exit()
end
