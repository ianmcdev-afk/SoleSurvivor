# Copies a freshly published SoleSurvivor.exe over the installed copy, so you don't have to run the new Setup.
# Runs automatically after every publish (see the UpdateInstalledCopy target in SoleSurvivor.App.csproj).
# Skip it with:  dotnet publish ... -p:UpdateInstall=false   or   .\publish.ps1 -NoInstallUpdate
#
# It only ever writes to the folder the installer registered (found by the installer's AppId),
# and only if SoleSurvivor.exe is already there. It never elevates: if the install is in Program Files
# and needs admin rights, it says so and leaves it alone. It never fails the build.
param(
    [Parameter(Mandatory = $true)][string]$Exe,
    [string]$Version = ''
)

$ErrorActionPreference = 'Stop'
$appKey = '{91792052-73D7-4F65-A185-2A01BD78DE90}_is1'   # the AppId in installer\SoleSurvivor.iss
$roots = @(
    'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
    'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
    'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall'
)

function Say([string]$text, [string]$colour = 'Gray') { Write-Host "[update-install] $text" -ForegroundColor $colour }

try {
    if (-not (Test-Path -LiteralPath $Exe)) { Say "No published exe at '$Exe'; nothing to copy." 'Yellow'; exit 0 }
    $source = (Resolve-Path -LiteralPath $Exe).Path

    $installs = foreach ($root in $roots) {
        $key = Join-Path $root $appKey
        if (Test-Path -LiteralPath $key) {
            $loc = (Get-ItemProperty -LiteralPath $key -Name InstallLocation -ErrorAction SilentlyContinue).InstallLocation
            if ($loc) { [pscustomobject]@{ Key = $key; Folder = $loc.TrimEnd('\') } }
        }
    }
    if (-not $installs) {
        Say "Sole Survivor isn't installed on this PC. Run the Setup once; after that, every publish updates it." 'Yellow'
        exit 0
    }

    foreach ($install in $installs | Sort-Object Folder -Unique) {
        $target = Join-Path $install.Folder 'SoleSurvivor.exe'
        if (-not (Test-Path -LiteralPath $target)) {
            Say "The install at '$($install.Folder)' has no SoleSurvivor.exe; leaving it alone." 'Yellow'
            continue
        }

        # Don't pull the exe out from under a running game.
        $running = Get-Process -Name 'SoleSurvivor' -ErrorAction SilentlyContinue |
            Where-Object { try { $_.Path -eq $target } catch { $false } }
        if ($running) {
            Say "Sole Survivor is running from '$($install.Folder)'. Close it and publish again to update it." 'Yellow'
            continue
        }

        if ((Get-FileHash -LiteralPath $source).Hash -eq (Get-FileHash -LiteralPath $target).Hash) {
            Say "Installed copy at '$($install.Folder)' is already up to date." 'Green'
            continue
        }

        try {
            Copy-Item -LiteralPath $source -Destination $target -Force
        } catch [System.UnauthorizedAccessException] {
            Say "No permission to write to '$($install.Folder)' (installed for all users). Run Visual Studio or PowerShell as administrator, or run the new Setup." 'Yellow'
            continue
        } catch [System.IO.IOException] {
            Say "Couldn't replace '$target' ($($_.Exception.Message)). Is the game still open?" 'Yellow'
            continue
        }

        # Keep Settings > Apps showing the right version.
        if ($Version) {
            try {
                Set-ItemProperty -LiteralPath $install.Key -Name DisplayVersion -Value $Version
                $name = (Get-ItemProperty -LiteralPath $install.Key -Name DisplayName -ErrorAction SilentlyContinue).DisplayName
                if ($name -match '^Sole Survivor version ') {
                    Set-ItemProperty -LiteralPath $install.Key -Name DisplayName -Value "Sole Survivor version $Version"
                }
            } catch {
                Say "Updated the exe, but couldn't update the version shown in Settings > Apps (needs admin)." 'Yellow'
            }
        }
        Say "Updated the installed copy at '$($install.Folder)'$(if ($Version) { " to $Version" })." 'Green'
    }
} catch {
    Say "Skipped updating the installed copy: $($_.Exception.Message)" 'Yellow'
}
exit 0
