$intrin = @(
    'C:/Program Files (x86)/Microsoft Visual Studio/2019/Enterprise/VC/Tools/MSVC/14.28.29333/include/intrin0.h',
    'C:/Program Files (x86)/Microsoft Visual Studio/2019/Community/VC/Tools/MSVC/14.28.29333/include/intrin0.h',
    'C:/Program Files (x86)/Microsoft Visual Studio/2019/Professional/VC/Tools/MSVC/14.28.29333/include/intrin0.h')
$patched = $false
for ($i = 0; $i -lt $intrin.Length; $i++) {
    if (Test-Path $intrin[$i]) {
        $content = ((Get-Content $intrin[$i]) -replace 'ifdef __clang__', 'ifdef __avoid_this_path__')
        [IO.File]::WriteAllLines($intrin[$i], $content)
        $patched = $true
    }
}

if (!$patched) {
    Write-Warning "This hack is no longer needed and should be removed."
}
