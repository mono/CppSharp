premake.api.register {
  name = "workspacefiles",
  scope = "workspace",
  kind = "list:string",
}

-- https://github.com/premake/premake-core/issues/1061#issuecomment-441417853
premake.override(premake.vstudio.sln2005, "projects", function(base, wks)
  if wks.workspacefiles and #wks.workspacefiles > 0 then
    premake.push('Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Solution Items", "Solution Items", "{' .. os.uuid("Solution Items:"..wks.name) .. '}"')
    premake.push("ProjectSection(SolutionItems) = preProject")
    for _, pattern in ipairs(wks.workspacefiles) do
      local matches = os.matchfiles(pattern)
      for _, file in ipairs(matches) do
        premake.w(file.." = "..file)
      end
    end
    premake.pop("EndProjectSection")
    premake.pop("EndProject")
  end
  base(wks)
end)
