<#
  fix-stuck-package.ps1
  Removes every trace of the 38673Yurin.ScannerIP package (all users, staged, bundle/main/resource, provisioned)
  so Visual Studio can register the dev (loose) layout again (fixes DEP0700 / 0x80073CFB).
  Run from an ELEVATED PowerShell:
    powershell -ExecutionPolicy Bypass -File D:\source\YurinKonstantin\ScanIP\fix-stuck-package.ps1
  Use -WhatIf to only list what would be removed.
#>
[CmdletBinding()]
param(
    [string]$Name = '38673Yurin.ScannerIP',
    [switch]$WhatIf
)

$ErrorActionPreference = 'Continue'
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error 'Run this script from an elevated (Administrator) PowerShell.'
    exit 1
}

function Show-State([string]$title) {
    Write-Host "`n===== $title =====" -ForegroundColor Cyan
    $pkgs = @(Get-AppxPackage -AllUsers -PackageTypeFilter All -Name "$Name*" -ErrorAction SilentlyContinue)
    if ($pkgs.Count -eq 0) { Write-Host 'Packages (all users, all types): none' }
    foreach ($p in $pkgs) {
        $users = ($p.PackageUserInformation | ForEach-Object { "$($_.UserSecurityId.Username)=$($_.InstallState)" }) -join '; '
        Write-Host ("{0}  bundle={1} resource={2} sig={3} dev={4}`n    users: {5}`n    at: {6}" -f `
            $p.PackageFullName, $p.IsBundle, $p.IsResourcePackage, $p.SignatureKind, $p.IsDevelopmentMode, $users, $p.InstallLocation)
    }
    $prov = @(Get-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue | Where-Object DisplayName -like "$Name*")
    if ($prov.Count -eq 0) { Write-Host 'Provisioned: none' }
    $prov | ForEach-Object { Write-Host "Provisioned: $($_.PackageName)" }
    $dirs = @(Get-ChildItem 'C:\Program Files\WindowsApps' -Directory -Filter "$Name*" -ErrorAction SilentlyContinue) +
            @(Get-ChildItem 'C:\Program Files\WindowsApps\Deleted' -Directory -Filter "$Name*" -ErrorAction SilentlyContinue)
    if ($dirs.Count -eq 0) { Write-Host 'WindowsApps folders: none' }
    $dirs | ForEach-Object { Write-Host "Folder: $($_.FullName)" }
    return ,$pkgs
}

function Invoke-Step([string]$what, [scriptblock]$action) {
    if ($WhatIf) { Write-Host "[WhatIf] $what"; return }
    Write-Host "-> $what" -ForegroundColor Yellow
    try { & $action; Write-Host '   OK' -ForegroundColor Green }
    catch { Write-Host "   FAILED: $($_.Exception.Message)" -ForegroundColor Red }
}

$before = Show-State 'BEFORE'

# 1. Provisioned (machine-wide "install for new users") copies
Get-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue | Where-Object DisplayName -like "$Name*" | ForEach-Object {
    $pn = $_.PackageName
    Invoke-Step "Remove-AppxProvisionedPackage -Online -PackageName $pn -AllUsers" {
        Remove-AppxProvisionedPackage -Online -PackageName $pn -AllUsers -ErrorAction Stop | Out-Null
    }
}

# 2. Bundles first, then main/resource packages; covers installed-for-any-user and staged-only packages
$ordered = @($before | Where-Object IsBundle) + @($before | Where-Object { -not $_.IsBundle })
foreach ($p in $ordered) {
    $fn = $p.PackageFullName
    Invoke-Step "Remove-AppxPackage -Package $fn -AllUsers" {
        Remove-AppxPackage -Package $fn -AllUsers -ErrorAction Stop
    }
}

# 3. Per-user removal for every user that still has it (in case -AllUsers left something behind)
foreach ($p in @(Get-AppxPackage -AllUsers -PackageTypeFilter All -Name "$Name*" -ErrorAction SilentlyContinue)) {
    foreach ($u in $p.PackageUserInformation) {
        $sid = $u.UserSecurityId.Sid
        $fn  = $p.PackageFullName
        Invoke-Step "Remove-AppxPackage -Package $fn -User $sid ($($u.InstallState))" {
            Remove-AppxPackage -Package $fn -User $sid -ErrorAction Stop
        }
    }
}

$after = Show-State 'AFTER'

if (-not $WhatIf) {
    if ($after.Count -eq 0) {
        Write-Host "`nDone. Now deploy from Visual Studio (profile 'ScanIPPro (Package)', x64)." -ForegroundColor Green
        Write-Host "If VS still reports 0x80073CFB, reboot (locked files in WindowsApps\Deleted) and retry."
    } else {
        Write-Host "`nSomething is still registered. Reboot and run this script again." -ForegroundColor Red
        Write-Host "Last deployment errors:"
        Get-WinEvent -FilterHashtable @{LogName='Microsoft-Windows-AppXDeploymentServer/Operational'; Level=2,3; StartTime=(Get-Date).AddMinutes(-30)} -ErrorAction SilentlyContinue | Where-Object { $_.Id -ne 471 } |
            Select-Object -First 10 | ForEach-Object { "  [$($_.TimeCreated)] $($_.Id): $($_.Message)" }
    }
}