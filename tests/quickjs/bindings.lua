generator "quickjs"
architecture "x64"

includedirs
{
    "..",
    "../../include",
}

output "gen"

module "test"
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
