premake.api.register {
  name = "workspacefiles",
  scope = "workspace",
  kind = "list:string",
}

premake.api.register {
  name = "removecompilefiles",
  scope = "project",
  kind = "list:string",
}

premake.api.register {
  name = "enabledefaultnoneitems",
  scope = "project",
  kind = "boolean",
}

premake.override(premake.vstudio.dotnetbase.netcore, "enableDefaultCompileItems", function(base, cfg) 
  base(cfg);

  if cfg.enabledefaultnoneitems ~= nil then
    premake.w('<EnableDefaultNoneItems>%s</EnableDefaultNoneItems>', iif(cfg.enabledefaultnoneitems, "true", "false"))
  end
end)

premake.override(premake.vstudio.dotnetbase, "files", function(base, prj) 
  base(prj);

  if prj.removecompilefiles ~= nil then
    for _, file in ipairs(prj.removecompilefiles) do
        premake.w("<Compile Remove=\"%s\" />", file)
    end
  end
end)

-- https://github.com/premake/premake-core/issues/1061#issuecomment-441417853
premake.override(premake.vstudio.sln2005, "projects", function(base, wks)
  if wks.workspacefiles and #wks.workspacefiles > 0 then
    premake.push('Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Solution Items", "Solution Items", "{' .. os.uuid("Solution Items:"..wks.name) .. '}"')
    premake.push("ProjectSection(SolutionItems) = preProject")
    for _, file in ipairs(wks.workspacefiles) do
      file = path.rebase(file, ".", wks.location)
      premake.w(file.." = "..file)
    end
    premake.pop("EndProjectSection")
    premake.pop("EndProject")
  end
  base(wks)
end)
