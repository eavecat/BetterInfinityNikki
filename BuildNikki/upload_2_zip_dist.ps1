Set-Location $PSScriptRoot

$directoryPath = "$PSScriptRoot\dist\BetterIN"
$outputJsonPath = "$PSScriptRoot\dist\hash.json"
$destinationDir = "$PSScriptRoot\dist\installation"

$absoluteDirectoryPath = (Resolve-Path -Path $directoryPath).Path

$excludedDirectories = @(
    "$PSScriptRoot\dist\BetterIN\Script",
    "$PSScriptRoot\dist\BetterIN\User"
) | Where-Object { Test-Path $_ } | ForEach-Object { (Resolve-Path -Path $_).Path }

$fileHashes = @{}

$files = Get-ChildItem -Path $directoryPath -Recurse -File

foreach ($file in $files) {
    if ($file.Extension -eq ".zip") { continue }

    $skipFile = $false
    foreach ($excludedDir in $excludedDirectories) {
        if ($file.FullName.StartsWith($excludedDir)) {
            $skipFile = $true
            break
        }
    }
    if ($skipFile) {
        Write-Host "Skipping: $($file.FullName)"
        continue
    }

    $hash = Get-FileHash -Path $file.FullName -Algorithm SHA256
    if ($null -eq $hash) {
        Write-Host "Failed to compute hash: $($file.FullName)"
        continue
    }

    $relativePath = $file.FullName.Replace($absoluteDirectoryPath, "").TrimStart("\")
    $fileHashes[$relativePath] = $hash.Hash

    $zipFilePath = "$($file.FullName).zip"
    Compress-Archive -Path $file.FullName -DestinationPath $zipFilePath -Force
}

$jsonContent = $fileHashes | ConvertTo-Json -Depth 10
[System.IO.File]::WriteAllText($outputJsonPath, $jsonContent, [System.Text.Encoding]::UTF8)

$zipFiles = Get-ChildItem -Path $absoluteDirectoryPath -Recurse -Filter *.zip

foreach ($file in $zipFiles) {
    $relativePath = $file.FullName.Substring($absoluteDirectoryPath.Length)
    $destinationPath = Join-Path $destinationDir $relativePath

    $destinationDirPath = Split-Path $destinationPath
    if (-not (Test-Path $destinationDirPath)) {
        New-Item -ItemType Directory -Path $destinationDirPath -Force | Out-Null
    }

    Copy-Item -Path $file.FullName -Destination $destinationPath -Force
}

Remove-Item -Path $absoluteDirectoryPath -Recurse -Force

Write-Host ""
Write-Host "========================================"
Write-Host " hash.json: $outputJsonPath"
Write-Host " installation: $destinationDir"
Write-Host "========================================"
Read-Host "Press Enter to exit"