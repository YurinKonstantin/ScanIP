<#
  fix-system-staged.ps1
  Removes 38673Yurin.ScannerIP packages that are left "Staged" for SYSTEM (S-1-5-18) by a failed Store install.
  Remove-AppxPackage from an admin session reports OK but the SYSTEM-staged record stays, so this script runs
  the removal *as SYSTEM* through a one-shot scheduled task, then deletes the task.
  No files or registry/StateRepository entries are edited by hand.

  Run from an ELEVATED PowerShell:
    powershell -ExecutionPolicy Bypass -File D:\source\YurinKonstantin\ScanIP\fix-system-staged.ps1
#>
param([string]$Name = '38673Yurin.ScannerIP')

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error 'Run this script from an elevated (Administrator) PowerShell.'; exit 1
}

function Show-State([string]$title) {
    Write-Host "`n===== $title =====" -ForegroundColor Cyan
    $pkgs = @(Get-AppxPackage -AllUsers -PackageTypeFilter All -Name "$Name*" -ErrorAction SilentlyContinue)
    Write-Host "Packages: $($pkgs.Count)"
    foreach ($p in $pkgs) {
        Write-Host "  $($p.PackageFullName)  sig=$($p.SignatureKind) location='$($p.InstallLocation)'"
        foreach ($u in $p.PackageUserInformation) { Write-Host "      $($u.UserSecurityId.Sid) $($u.UserSecurityId.Username) = $($u.InstallState)" }
    }
    $prov = @(Get-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue | Where-Object DisplayName -like "$Name*")
    Write-Host "Provisioned: $($prov.Count)"; $prov | ForEach-Object { Write-Host "  $($_.PackageName)" }
    $dirs = @(Get-ChildItem 'C:\Program Files\WindowsApps' -Directory -Filter "$Name*" -ErrorAction SilentlyContinue)
    $del  = @(Get-ChildItem 'C:\Program Files\WindowsApps\Deleted' -Directory -Filter "$Name*" -ErrorAction SilentlyContinue)
    Write-Host "WindowsApps folders: $($dirs.Count)"; $dirs | ForEach-Object { Write-Host "  $($_.FullName)" }
    Write-Host "WindowsApps\Deleted folders (pending cleanup, harmless): $($del.Count)"; $del | ForEach-Object { Write-Host "  $($_.Name)" }
    return ,$pkgs
}

$before = Show-State 'BEFORE'
if ($before.Count -eq 0) { Write-Host "`nNothing registered/staged. Deploy from VS should work." -ForegroundColor Green; exit 0 }

# Log in an admin-only folder (not user-writable), payload passed inline via -EncodedCommand (no script file a user could tamper with).
$logDir = Join-Path $env:windir 'Temp\ScannerIPFix'
New-Item -ItemType Directory -Path $logDir -Force | Out-Null
$log = Join-Path $logDir 'system-remove.log'
Remove-Item $log -ErrorAction SilentlyContinue

$payload = @"
`$ErrorActionPreference = 'Continue'
`$log = '$log'
"`$(Get-Date -Format s) running as `$([Security.Principal.WindowsIdentity]::GetCurrent().Name)" | Out-File `$log -Append -Encoding utf8
`$all = @(Get-AppxPackage -AllUsers -PackageTypeFilter All -Name '$Name*')
`$ordered = @(`$all | Where-Object IsBundle) + @(`$all | Where-Object { -not `$_.IsBundle })
foreach (`$p in `$ordered) {
  foreach (`$args2 in @(@{AllUsers=`$true}, @{})) {
    try { Remove-AppxPackage -Package `$p.PackageFullName @args2 -ErrorAction Stop; "OK   `$(`$p.PackageFullName) `$(`$args2.Keys)" | Out-File `$log -Append -Encoding utf8 }
    catch { "FAIL `$(`$p.PackageFullName) `$(`$args2.Keys): `$(`$_.Exception.Message)" | Out-File `$log -Append -Encoding utf8 }
  }
}
"DONE" | Out-File `$log -Append -Encoding utf8
"@
$enc = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($payload))

$taskName = 'ScannerIP-RemoveSystemStaged'
Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
$action    = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand $enc"
$principalS = New-ScheduledTaskPrincipal -UserId 'S-1-5-18' -LogonType ServiceAccount -RunLevel Highest
$settings  = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit (New-TimeSpan -Minutes 10)
Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principalS -Settings $settings -Force | Out-Null
Write-Host "`n-> Running removal as SYSTEM (task '$taskName')..." -ForegroundColor Yellow
Start-ScheduledTask -TaskName $taskName
$deadline = (Get-Date).AddMinutes(5)
do { Start-Sleep 2; $st = (Get-ScheduledTask -TaskName $taskName).State } while ($st -eq 'Running' -and (Get-Date) -lt $deadline)
$info = Get-ScheduledTaskInfo -TaskName $taskName
Write-Host "Task result: 0x$('{0:X}' -f $info.LastTaskResult)"
Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
Write-Host "Task removed.`n--- SYSTEM log ---"
if (Test-Path $log) { Get-Content $log } else { Write-Host '(no log written)' }

$after = Show-State 'AFTER'
if ($after.Count -eq 0) {
    Write-Host "`nClean. Deploy from Visual Studio now (profile 'ScanIPPro (Package)', x64)." -ForegroundColor Green
} else {
    Write-Host "`nStill present. Recent AppXDeployment-Server errors/warnings (last 30 min):" -ForegroundColor Red
    Get-WinEvent -FilterHashtable @{LogName='Microsoft-Windows-AppXDeploymentServer/Operational'; Level=2,3; StartTime=(Get-Date).AddMinutes(-30)} -ErrorAction SilentlyContinue |
        Where-Object { $_.Id -ne 471 } | Select-Object -First 20 |
        ForEach-Object { "  [$($_.TimeCreated.ToString('HH:mm:ss'))] $($_.Id): $($_.Message)" }
    Write-Host "`nIf the Store keeps re-staging 1.0.167.0: open Microsoft Store > Library/Downloads and cancel the ScannerIP update, then rerun."
}