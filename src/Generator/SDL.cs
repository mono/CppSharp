
namespace Cxxi
{
	/// <summary>
	/// Transform the SDL library declarations to something more .NET friendly.
	/// </summary>
	class SDLTransforms : LibraryTransform
	{
		public override void Transform(Generator g)
		{
			g.IgnoreEnumWithMatchingItem("SDL_FALSE");
			g.IgnoreEnumWithMatchingItem("DUMMY_ENUM_VALUE");

			g.SetNameOfEnumWithMatchingItem("SDL_SCANCODE_UNKNOWN", "ScanCode");
			g.SetNameOfEnumWithMatchingItem("SDLK_UNKNOWN", "Key");
			g.SetNameOfEnumWithMatchingItem("KMOD_NONE", "KeyModifier");
			g.SetNameOfEnumWithMatchingItem("SDL_LOG_CATEGORY_CUSTOM", "LogCategory");

			g.GenerateEnumFromMacros("InitFlags", "SDL_INIT_(.*)").SetFlags();
			g.GenerateEnumFromMacros("Endianness", "SDL_(.*)_ENDIAN");
			g.GenerateEnumFromMacros("KeyState", "SDL_RELEASED", "SDL_PRESSED");

			g.GenerateEnumFromMacros("AlphaState", "SDL_ALPHA_(.*)");

			g.GenerateEnumFromMacros("HatState", "SDL_HAT_(.*)");

			g.IgnoreModuleWithName("SDL_atomic*");
			g.IgnoreModuleWithName("SDL_endian*");
			g.IgnoreModuleWithName("SDL_main*");
			g.IgnoreModuleWithName("SDL_mutex*");
			g.IgnoreModuleWithName("SDL_stdinc*");

			//g.IgnoreModuleWithName("SDL_error");

			g.IgnoreEnumWithMatchingItem("SDL_ENOMEM");
			g.IgnoreFunctionWithName("SDL_Error");

			g.RemovePrefix("SDL_");
			g.RemovePrefix("SCANCODE_");
			g.RemovePrefix("SDLK_");
			g.RemovePrefix("KMOD_");
			g.RemovePrefix("LOG_CATEGORY_");

			g.Process();

			g.SetNameOfEnumWithName("PIXELTYPE", "PixelType");
			g.SetNameOfEnumWithName("BITMAPORDER", "BitmapOrder");
			g.SetNameOfEnumWithName("PACKEDORDER", "PackedOrder");
			g.SetNameOfEnumWithName("ARRAYORDER", "ArrayOrder");
			g.SetNameOfEnumWithName("PACKEDLAYOUT", "PackedLayout");
			g.SetNameOfEnumWithName("PIXELFORMAT", "PixelFormats");
			g.SetNameOfEnumWithName("assert_state", "AssertState");
			g.SetNameOfClassWithName("assert_data", "AssertData");

			//gen.SetNameOfEnumWithName("LOG_CATEGORY", "LogCategory");
		}
	}
}
