$intrin = 'C:/Program Files (x86)/Microsoft Visual Studio/2019/Enterprise/VC/Tools/MSVC/14.28.29333/include/intrin0.h'
if (!(Test-Path $intrin)) {
  Write-Warning "This hack is no longer needed and should be removed."
}
else {
    $content = ((Get-Content $intrin) -replace 'ifdef __clang__', 'ifdef __avoid_this_path__')
    [IO.File]::WriteAllLines($intrin, $content)
}
