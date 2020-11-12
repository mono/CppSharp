project "CppSharp.Parser.CSharp"

  SetupManagedProject()

  kind "SharedLib"
  language "C#"
  clr "Unsafe"

  dependson { "CppSharp.CppParser" }

  files { "**.lua" }
  vpaths { ["*"] = "*" }

  links { "CppSharp.Runtime" }

  if os.istarget("windows") then
      filter { "action:vs*", "platforms:x86" }
        files { "i686-pc-win32-msvc/**.cs" }
      filter { "action:vs*", "platforms:x64" }
        files { "x86_64-pc-win32-msvc/**.cs" }
  elseif os.istarget("macosx") then
      if is_64_bits_mono_runtime() or _OPTIONS["arch"] == "x64" then  
        files { "x86_64-apple-darwin12.4.0/**.cs" }
      else
        files { "i686-apple-darwin12.4.0/**.cs" }
      end

  elseif os.istarget("linux") then
      local abi = ""
      if UseCxx11ABI() then
          abi = "-cxx11abi"
      end
      files { "x86_64-linux-gnu"..abi.."/**.cs" }
  else
      print "Unknown architecture"
  end

  filter {}

function SetupParser()
  links
  {
    "CppSharp.Parser.CSharp",
    "CppSharp.Runtime"
  }
end