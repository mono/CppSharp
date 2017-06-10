project "CppSharp.Parser.CSharp"

  SetupManagedProject()

  kind "SharedLib"
  language "C#"
  clr "Unsafe"

  dependson { "CppSharp.CppParser" }

  files { "**.lua" }
  vpaths { ["*"] = "*" }

  links { "CppSharp.Runtime" }

  if os.is("windows") then
      files { "i686-pc-win32-msvc/**.cs" }
  elseif os.is("macosx") then
      local file = io.popen("lipo -info `which mono`")
      local output = file:read('*all')
      if string.find(output, "x86_64") or _OPTIONS["arch"] == "x64" then  
        files { "x86_64-apple-darwin12.4.0/**.cs" }
      else
        files { "i686-apple-darwin12.4.0/**.cs" }
      end

  elseif os.is("linux") then
      files { "x86_64-linux-gnu/**.cs" }
  else
      print "Unknown architecture"
  end

  configuration ""

project "Std-symbols"

  kind "SharedLib"
  language "C++"
  SetupNativeProject()
  rtti "Off"

  configuration "vs*"
    buildoptions { clang_msvc_flags }

  configuration "*"

  if os.is("windows") then
      files { "i686-pc-win32-msvc/Std-symbols.cpp" }
  elseif os.is("macosx") then
      local file = io.popen("lipo -info `which mono`")
      local output = file:read('*all')
      if string.find(output, "x86_64") then
        files { "x86_64-apple-darwin12.4.0/Std-symbols.cpp" }
      else
        files { "i686-apple-darwin12.4.0/Std-symbols.cpp" }
      end

  elseif os.is("linux") then
      files { "x86_64-linux-gnu/Std-symbols.cpp" }
  else
      print "Unknown architecture"
  end

  configuration "*"

function SetupParser()
  links
  {
    "CppSharp.Parser.CSharp",
    "CppSharp.Runtime"
  }
end