Set-Location $PSScriptRoot
Write-Host "[prepare version]"
Set-Location ..\BetterInfinityNikki
$csproj = [xml](Get-Content BetterInfinityNikki.csproj)
$version = $csproj.Project.PropertyGroup.Version
Write-Host "current version is $version"
if ($args.Count -gt 0) { $b = $args[0] } else { $b = Read-Host "Input version (leave empty to use $version)" }
if (-not $b) { $b = $version }
Set-Location $PSScriptRoot
$publishDir = "$PSScriptRoot\..\BetterInfinityNikki\bin\x64\Release\net8.0-windows10.0.22621.0\publish\win-x64"
$distDir = "$PSScriptRoot\dist\BetterIN"
Write-Host "[build app]"
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
if (Test-Path "$PSScriptRoot\dist") { Remove-Item "$PSScriptRoot\dist" -Recurse -Force }
Set-Location ..
dotnet publish BetterInfinityNikki\BetterInfinityNikki.csproj -c Release -p:PublishProfile=FolderProfile
Set-Location $PSScriptRoot
Write-Host "[clean unnecessary files]"
Get-ChildItem $publishDir -Filter *.lib | Remove-Item -Force -ErrorAction SilentlyContinue
Get-ChildItem $publishDir -Filter *ffmpeg*.dll | Remove-Item -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $distDir -Force | Out-Null
Copy-Item "$publishDir\*" -Destination $distDir -Recurse -Force
Write-Host "[done] dist\BetterIN ready"
Read-Host "Press Enter to exit"