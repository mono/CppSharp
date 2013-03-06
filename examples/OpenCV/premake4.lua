project "OpenCV"

  kind "ConsoleApp"
  language "C#"
  location "."
  debugdir "."
  
  files { "OpenCV.cs", "./*.lua" }

  links { "Bridge", "Generator", "Parser" }


project "OpenCV.Tests"

  kind     "ConsoleApp"
  language "C#"
  location "."
  
  dependson "OpenCV"

  files
  {
      libdir .. "/OpenCV/OpenCV.cs",
      "*.lua"
  }
