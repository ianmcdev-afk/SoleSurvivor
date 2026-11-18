# Builds a single self-contained SoleSurvivor.exe (no .NET install needed on the target PC).
# Usage:  .\publish.ps1                     -> publish\win-x64\SoleSurvivor.exe
#         .\publish.ps1 -Installer          -> also builds installer\Output\SoleSurvivor-Setup-<version>.exe
#         .\publish.ps1 -Installer -Iscc "D:\Tools\Inno Setup 6\ISCC.exe"   (if Inno Setup is somewhere unusual)
param(
    [switch]$Installer,
    [string]$Iscc
)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

dotnet publish src\SoleSurvivor.App\SoleSurvivor.App.csproj -p:PublishProfile=win-x64
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }
Write-Host "Built: $PSScriptRoot\publish\win-x64\SoleSurvivor.exe" -ForegroundColor Green

if (-not $Installer) { return }

function Find-Iscc {
    # 1. On PATH?
    $cmd = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    # 2. The usual install folders: all users (Program Files) and "install for me only" (LocalAppData).
    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )
    foreach ($c in $candidates) { if ($c -and (Test-Path $c)) { return $c } }

    # 3. Ask Windows where it was installed (uninstall entries, machine and user, 32- and 64-bit views).
    $keys = @(
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1',
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1',
        'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1'
    )
    foreach ($k in $keys) {
        $loc = (Get-ItemProperty -Path $k -Name InstallLocation -ErrorAction SilentlyContinue).InstallLocation
        if ($loc) {
            $exe = Join-Path $loc 'ISCC.exe'
            if (Test-Path $exe) { return $exe }
        }
    }
    return $null
}

if ($Iscc) {
    if (-not (Test-Path $Iscc)) { throw "ISCC.exe not found at '$Iscc'" }
} else {
    $Iscc = Find-Iscc
}
if (-not $Iscc) {
    throw @"
Inno Setup 6 not found (looked on PATH, in Program Files, in %LOCALAPPDATA%\Programs and in the registry).
Install it with:   winget install JRSoftware.InnoSetup
or download it from https://jrsoftware.org/isinfo.php, or pass its location:  .\publish.ps1 -Installer -Iscc "<path>\ISCC.exe"
"@
}
Write-Host "Using Inno Setup: $Iscc"

$version = ([xml](Get-Content Directory.Build.props)).Project.PropertyGroup.Version
& $Iscc "/DAppVersion=$version" installer\SoleSurvivor.iss
if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed" }
Write-Host "Installer: $PSScriptRoot\installer\Output\SoleSurvivor-Setup-$version.exe" -ForegroundColor Green
