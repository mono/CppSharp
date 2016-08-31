include "../../build/Tests.lua"

project "SDL"
  SetupExampleProject()

if config ~= nil and config.ExampleTests then

project "SDL.Tests"

  kind     "ConsoleApp"
  language "C#"
  location "."
  
  dependson "SDL"

  files
  {
      libdir .. "/SDL/SDL.cs",
      libdir .. "/SDL/SDL_assert.cs",
      libdir .. "/SDL/SDL_audio.cs",
      libdir .. "/SDL/SDL_blendmode.cs",
      libdir .. "/SDL/SDL_clipboard.cs",
      libdir .. "/SDL/SDL_cpuinfo.cs",
      libdir .. "/SDL/SDL_error.cs",
      libdir .. "/SDL/SDL_events.cs",
      libdir .. "/SDL/SDL_gamecontroller.cs",
      libdir .. "/SDL/SDL_gesture.cs",
      libdir .. "/SDL/SDL_hints.cs",
      libdir .. "/SDL/SDL_joystick.cs",
      libdir .. "/SDL/SDL_keyboard.cs",
      libdir .. "/SDL/SDL_keycode.cs",
      libdir .. "/SDL/SDL_loadso.cs",
      libdir .. "/SDL/SDL_log.cs",
      libdir .. "/SDL/SDL_messagebox.cs",
      libdir .. "/SDL/SDL_mouse.cs",
      libdir .. "/SDL/SDL_pixels.cs",
      libdir .. "/SDL/SDL_platform.cs",
      libdir .. "/SDL/SDL_power.cs",
      libdir .. "/SDL/SDL_rect.cs",
      libdir .. "/SDL/SDL_render.cs",
      libdir .. "/SDL/SDL_rwops.cs",
      libdir .. "/SDL/SDL_scancode.cs",
      libdir .. "/SDL/SDL_surface.cs",
      libdir .. "/SDL/SDL_thread.cs",
      libdir .. "/SDL/SDL_timer.cs",
      libdir .. "/SDL/SDL_touch.cs",
      libdir .. "/SDL/SDL_version.cs",
      libdir .. "/SDL/SDL_video.cs",
      "*.lua"
  }
end
