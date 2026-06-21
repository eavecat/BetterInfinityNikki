Set-Location $PSScriptRoot

$kachina = "$PSScriptRoot\kachina-builder.exe"
if (-not (Test-Path $kachina)) {
    Write-Host "[ERROR] kachina-builder.exe not found"
    Write-Host "  Download from https://github.com/YuehaiTeam/kachina-installer/releases"
    Read-Host "Press Enter to exit"
    return
}

Write-Host "[prepare version]"
Set-Location ..\BetterInfinityNikki
$csproj = [xml](Get-Content BetterInfinityNikki.csproj)
$version = $csproj.Project.PropertyGroup.Version
Write-Host "current version is $version"
if ($args.Count -gt 0) { $b = $args[0] } else { $b = Read-Host "Input version (leave empty to use $version)" }
if (-not $b) { $b = $version }

Set-Location $PSScriptRoot
$distDir = "$PSScriptRoot\dist\BetterIN"
$icon = "$PSScriptRoot\..\BetterInfinityNikki\Resources\Images\logo.ico"
$left_icon = "$PSScriptRoot\left.webp"

if (-not (Test-Path $distDir)) {
    Write-Host "[ERROR] dist\BetterIN not found, run upload_1_build_dist.ps1 first"
    Read-Host "Press Enter to exit"
    return
}

Write-Host "[gen updater: BetterIN.update.exe]"
cmd /c "cd /d `"$distDir`" && `"$kachina`" pack -c `"$PSScriptRoot\kachina_nikki.json`" -o `"$distDir\BetterIN.update.exe`" --icon `"$icon`" -t `"$left_icon`" 2>nul"
Start-Sleep -Milliseconds 500
if (-not (Test-Path "$distDir\BetterIN.update.exe")) {
    Write-Host "[ERROR] Failed to generate BetterIN.update.exe"
    Read-Host "Press Enter to exit"
    return
}

Write-Host "[gen metadata + installer]"
cmd /c "cd /d `"$distDir`" && `"$kachina`" gen -j 6 -i . -m `"$PSScriptRoot\metadata.json`" -o `"$PSScriptRoot\hashed`" -t $b -r liuxia/betterin -u BetterIN.update.exe 2>nul"
cmd /c "cd /d `"$distDir`" && `"$kachina`" pack -c `"$PSScriptRoot\kachina_nikki.json`" -m `"$PSScriptRoot\metadata.json`" -d `"$PSScriptRoot\hashed`" -o `"$PSScriptRoot\BetterIN.Install.$b.exe`" --icon `"$icon`" -t `"$left_icon`" 2>nul"

Write-Host ""
if (Test-Path "BetterIN.Install.$b.exe") {
    Write-Host "========================================"
    Write-Host " Installer created: BetterIN.Install.$b.exe"
    Write-Host " Updater created: dist\BetterIN\BetterIN.update.exe"
    Write-Host "========================================"
} else {
    Write-Host "[ERROR] Failed to generate installer"
}
Read-Host "Press Enter to exit"