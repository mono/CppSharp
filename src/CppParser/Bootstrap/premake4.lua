project "CppSharp.Parser.Bootstrap"

  kind "ConsoleApp"
  language "C#"
  SetupManagedProject()
  debugdir "."

  flags { "Unsafe" }
  files { "Bootstrap.cs", "*.lua" }
  links { "CppSharp", "CppSharp.AST", "CppSharp.Generator", "System", "System.Core" }

  local bindings_dir = "Bindings/CSharp"

  if os.is_windows() then
      files { bindings_dir .. "/i686-pc-win32-msvc/**.cs" }
  elseif os.is_osx() then
      local file = io.popen("lipo -info `which mono`")
      local output = file:read('*all')
      if string.find(output, "x86_64") then
        files { bindings_dir .. "/x86_64-apple-darwin12.4.0/**.cs" }
      else
        files { bindings_dir .. "/i686-apple-darwin12.4.0/**.cs" }
      end

  elseif os.is_linux() then
      files { bindings_dir .. "/x86_64-linux-gnu/**.cs" }
  else
      print "Unknown architecture"
  end

  SetupParser()
