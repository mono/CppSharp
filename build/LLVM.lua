-- Setup the LLVM dependency directories

LLVMInstallDir = "../../deps/llvm/install"
ClangSrcDir = "../../deps/llvm/tools/clang"
LLVMConfig = path.join(LLVMInstallDir, "bin/llvm-config")

-- TODO: Search for available system dependencies

function SetupLLVMIncludes()
  local c = configuration()

  includedirs
  {
    path.join(LLVMInstallDir, "include"),
    -- We need this to include the private clang CodeGen stuff
    path.join(ClangSrcDir, "lib"),
  }

  configuration(c)
end

function SetupLLVMLibs()
  local c = configuration()

  libdirs { path.join(LLVMInstallDir, "lib") }

  configuration "not vs*"
    buildoptions { "-fpermissive" }
    defines { "__STDC_CONSTANT_MACROS", "__STDC_LIMIT_MACROS" }

  configuration "macosx"
    links { "c++", "curses", "pthread", "z" }

  configuration "*"
    links
    {
      "clangAnalysis",
      "clangAST",
      "clangBasic",
      "clangCodeGen",
      "clangDriver",
      "clangEdit",
      "clangFrontend",
      "clangLex",
      "clangParse",
      "clangSema",
      "clangSerialization",
      "clangIndex",
    }

    local libs = os.outputof(path.getabsolute(LLVMConfig) .. " --libs engine option")
    libs = string.gsub(libs, "\n", "")
    libs = string.gsub(libs, "-l", "")
    libs = string.explode(libs, " ")
    links (libs)

  configuration(c)
end
