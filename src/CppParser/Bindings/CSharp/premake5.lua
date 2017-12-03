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
      files { "i686-pc-win32-msvc/**.cs" }
  elseif os.istarget("macosx") then
      local file = io.popen("lipo -info `which mono`")
      local output = file:read('*all')
      if string.find(output, "x86_64") or _OPTIONS["arch"] == "x64" then  
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