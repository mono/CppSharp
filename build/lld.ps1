$driver = 'C:\CppSharp\build\llvm\llvm-project\lld\COFF\Driver.cpp'
$patched = $false
if (Test-Path $driver) {
	$content = ((Get-Content $driver) -replace 'static void createImportLibrary(bool asLib) {', 'static void createImportLibrary(bool asLib) { return;')
	[IO.File]::WriteAllLines($driver, $content)
	$patched = $true
}

if (!$patched) {
    Write-Warning "This hack is no longer needed and should be removed."
}
