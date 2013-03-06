using Cxxi.Generators;
using Cxxi.Passes;

namespace Cxxi
{
    /// <summary>
    /// Transform the SDL library declarations to something more .NET friendly.
    /// </summary>
    class SDL : ILibrary
    {
        public void Setup(DriverOptions options)
        {
            options.LibraryName = "SDL";
            options.Headers.Add("SDL.h");
            options.IncludeDirs.Add("../../../examples/SDL/SDL-2.0/include");
            options.OutputDir = "SDL";
        }

        public void Preprocess(Library lib)
        {
            lib.IgnoreEnumWithMatchingItem("SDL_FALSE");
            lib.IgnoreEnumWithMatchingItem("DUMMY_ENUM_VALUE");

            lib.SetNameOfEnumWithMatchingItem("SDL_SCANCODE_UNKNOWN", "ScanCode");
            lib.SetNameOfEnumWithMatchingItem("SDLK_UNKNOWN", "Key");
            lib.SetNameOfEnumWithMatchingItem("KMOD_NONE", "KeyModifier");
            lib.SetNameOfEnumWithMatchingItem("SDL_LOG_CATEGORY_CUSTOM", "LogCategory");

            lib.GenerateEnumFromMacros("InitFlags", "SDL_INIT_(.*)").SetFlags();
            lib.GenerateEnumFromMacros("Endianness", "SDL_(.*)_ENDIAN");
            lib.GenerateEnumFromMacros("InputState", "SDL_RELEASED", "SDL_PRESSED");
            lib.GenerateEnumFromMacros("AlphaState", "SDL_ALPHA_(.*)");
            lib.GenerateEnumFromMacros("HatState", "SDL_HAT_(.*)");

            lib.IgnoreHeadersWithName("SDL_atomic*");
            lib.IgnoreHeadersWithName("SDL_endian*");
            lib.IgnoreHeadersWithName("SDL_main*");
            lib.IgnoreHeadersWithName("SDL_mutex*");
            lib.IgnoreHeadersWithName("SDL_stdinc*");
            //lib.IgnoreModuleWithName("SDL_error");

            lib.IgnoreEnumWithMatchingItem("SDL_ENOMEM");
            lib.IgnoreFunctionWithName("SDL_Error");
        }

        public void Postprocess(Library lib)
        {
            lib.SetNameOfEnumWithName("PIXELTYPE", "PixelType");
            lib.SetNameOfEnumWithName("BITMAPORDER", "BitmapOrder");
            lib.SetNameOfEnumWithName("PACKEDORDER", "PackedOrder");
            lib.SetNameOfEnumWithName("ARRAYORDER", "ArrayOrder");
            lib.SetNameOfEnumWithName("PACKEDLAYOUT", "PackedLayout");
            lib.SetNameOfEnumWithName("PIXELFORMAT", "PixelFormats");
            lib.SetNameOfEnumWithName("assert_state", "AssertState");
            lib.SetClassBindName("assert_data", "AssertData");
            lib.SetNameOfEnumWithName("eventaction", "EventAction");
            //lib.SetNameOfEnumWithName("LOG_CATEGORY", "LogCategory");
        }

        public void SetupPasses(PassBuilder p)
        {
            p.RemovePrefix("SDL_");
            p.RemovePrefix("SCANCODE_");
            p.RemovePrefix("SDLK_");
            p.RemovePrefix("KMOD_");
            p.RemovePrefix("LOG_CATEGORY_");
        }

        public void GenerateStart(TextTemplate template)
        {
        }

        public void GenerateAfterNamespaces(TextTemplate template)
        {
        }

        static class Program
        {
            public static void Main(string[] args)
            {
                Driver.Run(new SDL());
            }
        }
    }
}
