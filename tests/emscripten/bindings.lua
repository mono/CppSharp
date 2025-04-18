generator "emscripten"
platform "emscripten"
architecture "wasm32"

includedirs
{
    "..",
    "../../include",
}

output "gen"

module "tests"
    namespace "test"
    headers
    {
        "Builtins.h",
        "Classes.h",
        "Classes2.h",
        "Delegates.h",
        "Enums.h",
        "Overloads.h"
    }
