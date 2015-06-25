project "CppSharp.Parser.Gen"

  kind "ConsoleApp"
  language "C#"
  SetupManagedProject()
  debugdir "."
  
  files { "ParserGen.cs", "*.lua" }
  links { "CppSharp", "CppSharp.AST", "CppSharp.Generator", "System.Core" }

  SetupParser()
  
project "CppSharp.Parser.CSharp"
  
  kind "SharedLib"
  language "C#"
  SetupManagedProject()
  
  dependson { "CppSharp.CppParser" }
  flags { common_flags, "Unsafe" }

  files
  {
    "**.lua"
  }

  links { "CppSharp.Runtime" }

  if os.is_windows() then
      files { "CSharp/i686-pc-win32-msvc/**.cs" }
  elseif os.is_osx() then
      local file = io.popen("lipo -info `which mono`")
      local output = file:read('*all')
      if string.find(output, "x86_64") then  
        files { "CSharp/x86_64-apple-darwin12.4.0/**.cs" }
      else
        files { "CSharp/i686-apple-darwin12.4.0/**.cs" }
      end

  elseif os.is_linux() then
      files { "CSharp/x86_64-linux-gnu/**.cs" }
  else
      print "Unknown architecture"
  end

  configuration ""

if string.starts(action, "vs") and os.is_windows() then

  project "CppSharp.Parser.CLI"
    
    kind "SharedLib"
    language "C++"
    SetupNativeProject()
    SetupLLVMIncludes()
    
    dependson { "CppSharp.CppParser" }
    flags { common_flags, "Managed" }

    configuration "vs*"
      buildoptions { clang_msvc_flags }  

    configuration "*"
    
    files
    {
      "CLI/AST.h",
      "CLI/AST.cpp",
      "CLI/**.h",
      "CLI/**.cpp",
      "**.lua"
    }
    
    includedirs { "../../../include/", "../../../src/CppParser/" }
    
    configuration "*"
    links { "CppSharp.CppParser" }

end
