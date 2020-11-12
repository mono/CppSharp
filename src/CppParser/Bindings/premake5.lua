include ("CSharp")

if EnableNativeProjects() and os.ishost("windows") and not os.getenv("CI") then

include ("CLI")

end