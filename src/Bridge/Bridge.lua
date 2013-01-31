project "Bridge"

  kind  "SharedLib"
  language "C#"
  location "."

  files { "*.cs" }
  links { "System" }
  
  if _ACTION == "clean" then
    os.rmdir("lib")
  end

