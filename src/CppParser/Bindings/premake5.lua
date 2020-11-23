include ("CSharp")

if EnabledCLIProjects() and not os.getenv("CI") then

include ("CLI")

end