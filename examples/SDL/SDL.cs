using Cxxi.Generators;

namespace Cxxi
{
    /// <summary>
    /// Transform the SDL library declarations to something more .NET friendly.
    /// </summary>
    class SDL : ILibrary
    {
        public void Preprocess(LibraryHelpers g)
        {
            g.IgnoreEnumWithMatchingItem("SDL_FALSE");
            g.IgnoreEnumWithMatchingItem("DUMMY_ENUM_VALUE");

            g.SetNameOfEnumWithMatchingItem("SDL_SCANCODE_UNKNOWN", "ScanCode");
            g.SetNameOfEnumWithMatchingItem("SDLK_UNKNOWN", "Key");
            g.SetNameOfEnumWithMatchingItem("KMOD_NONE", "KeyModifier");
            g.SetNameOfEnumWithMatchingItem("SDL_LOG_CATEGORY_CUSTOM", "LogCategory");

            g.GenerateEnumFromMacros("InitFlags", "SDL_INIT_(.*)").SetFlags();
            g.GenerateEnumFromMacros("Endianness", "SDL_(.*)_ENDIAN");
            g.GenerateEnumFromMacros("InputState", "SDL_RELEASED", "SDL_PRESSED");

            g.GenerateEnumFromMacros("AlphaState", "SDL_ALPHA_(.*)");

            g.GenerateEnumFromMacros("HatState", "SDL_HAT_(.*)");

            g.IgnoreModulessWithName("SDL_atomic*");
            g.IgnoreModulessWithName("SDL_endian*");
            g.IgnoreModulessWithName("SDL_main*");
            g.IgnoreModulessWithName("SDL_mutex*");
            g.IgnoreModulessWithName("SDL_stdinc*");

            //g.IgnoreModuleWithName("SDL_error");

            g.IgnoreEnumWithMatchingItem("SDL_ENOMEM");
            g.IgnoreFunctionWithName("SDL_Error");
        }

        public void Postprocess(LibraryHelpers generator)
        {
            generator.SetNameOfEnumWithName("PIXELTYPE", "PixelType");
            generator.SetNameOfEnumWithName("BITMAPORDER", "BitmapOrder");
            generator.SetNameOfEnumWithName("PACKEDORDER", "PackedOrder");
            generator.SetNameOfEnumWithName("ARRAYORDER", "ArrayOrder");
            generator.SetNameOfEnumWithName("PACKEDLAYOUT", "PackedLayout");
            generator.SetNameOfEnumWithName("PIXELFORMAT", "PixelFormats");
            generator.SetNameOfEnumWithName("assert_state", "AssertState");
            generator.SetClassBindName("assert_data", "AssertData");
            generator.SetNameOfEnumWithName("eventaction", "EventAction");

            //gen.SetNameOfEnumWithName("LOG_CATEGORY", "LogCategory");
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
            throw new System.NotImplementedException();
        }

        public void GenerateAfterNamespaces(TextTemplate template)
        {
            throw new System.NotImplementedException();
        }
    }
}
