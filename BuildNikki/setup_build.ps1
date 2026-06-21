Set-Location $PSScriptRoot
if (Test-Path dist) { Remove-Item dist -Recurse -Force }
New-Item -ItemType Directory -Path dist\BetterIN -Force | Out-Null
Write-Host "[prepare version]"
Set-Location ..\BetterInfinityNikki
$csproj = [xml](Get-Content BetterInfinityNikki.csproj)
$version = $csproj.Project.PropertyGroup.Version
Write-Host "current version is $version"
if ($args.Count -gt 0) { $b = $args[0] } else { $b = Read-Host "Input version (leave empty to use $version)" }
if (-not $b) { $b = $version }
$tmpfolder = "$PSScriptRoot\dist\BetterIN"
$archiveFile = "BetterIN_v$b.7z"
Write-Host "[build app]"
Set-Location $PSScriptRoot
$publishDir = "..\BetterInfinityNikki\bin\x64\Release\net8.0-windows10.0.22621.0\publish\win-x64"
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
Set-Location ..
dotnet publish BetterInfinityNikki\BetterInfinityNikki.csproj -c Release -p:PublishProfile=FolderProfile
Set-Location $PSScriptRoot
Write-Host "[pack app using 7z]"
$srcDir = "..\BetterInfinityNikki\bin\x64\Release\net8.0-windows10.0.22621.0\publish\win-x64"
Copy-Item "$srcDir\*" -Destination $tmpfolder -Recurse -Force
Set-Location $PSScriptRoot
Get-ChildItem $tmpfolder -Filter *.lib | Remove-Item -Force -ErrorAction SilentlyContinue
Get-ChildItem $tmpfolder -Filter *ffmpeg*.dll | Remove-Item -Force -ErrorAction SilentlyContinue
Get-ChildItem $tmpfolder -Filter *.pdb | Remove-Item -Force -ErrorAction SilentlyContinue
& MicaSetup.Tools\7-Zip\7z a publish.7z "$tmpfolder\*" -t7z -mx=5 -mf=BCJ2 -r -y 2>$null
if (Test-Path $archiveFile) { Remove-Item $archiveFile -Force }
Rename-Item publish.7z $archiveFile
Remove-Item dist -Recurse -Force
Write-Host ""
Write-Host "========================================"
Write-Host " 7z archive created: $archiveFile"
Write-Host "========================================"
Read-Host "Press Enter to exit"