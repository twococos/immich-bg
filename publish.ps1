# Builds publish\ImmichBg.exe (single self-contained file, no .NET install needed)
# and installer\Output\ImmichBg-Setup-<version>.exe (needs Inno Setup 6: winget install JRSoftware.InnoSetup).
# The exe is not compressed on purpose: a compressed single file unpacks into RAM (~55 MB instead of ~16 MB);
# the installer compresses it instead.
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$version = ([xml](Get-Content src/ImmichBg/ImmichBg.csproj)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1

dotnet test
if ($LASTEXITCODE -ne 0) { throw "Tests failed" }
dotnet publish src/ImmichBg -c Release -r win-x64 --self-contained `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
if ($LASTEXITCODE -ne 0) { throw "Publish failed" }

$iscc = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw "Inno Setup 6 not found (winget install JRSoftware.InnoSetup)" }

& $iscc /Qp "/DAppVersion=$version" installer\ImmichBg.iss
if ($LASTEXITCODE -ne 0) { throw "Installer build failed" }

Write-Host "OK -> publish\ImmichBg.exe"
Write-Host "OK -> installer\Output\ImmichBg-Setup-$version.exe"
