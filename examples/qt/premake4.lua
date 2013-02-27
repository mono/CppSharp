project "Qt"

  kind "ConsoleApp"
  language "C#"
  location "."

  files { "**.cs", "./*.lua" }

  links { "Bridge", "Generator" }


