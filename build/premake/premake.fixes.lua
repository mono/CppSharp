-- https://github.com/premake/premake-core/issues/1559
premake.override(premake.vstudio.vc2010, "targetFramework", function(oldfn, prj)
  if prj.clr == "NetCore" then 
    local action = premake.action.current()
    local tools = string.format(' ToolsVersion="%s"', action.vstudio.toolsVersion)
    local framework = prj.dotnetframework or action.vstudio.targetFramework or "4.0"    
    
    premake.w('<TargetFramework>%s</TargetFramework>', framework)  
  else
    oldfn(prj)
  end
end)
