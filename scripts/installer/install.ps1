# NukeTree 安装脚本（便携式安装：复制到 %LOCALAPPDATA%\NukeTree，创建开始菜单快捷方式，无需管理员）
param([switch]$Desktop)
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$payload = Join-Path $here 'payload'
if (-not (Test-Path (Join-Path $payload 'NukeTree.exe'))) { throw '安装包损坏：缺少 payload\NukeTree.exe' }
$target = Join-Path $env:LOCALAPPDATA 'NukeTree'
if (Test-Path $target) {
    $ans = Read-Host "目标目录已存在：$target`n覆盖升级？(Y/N)"
    if ($ans -notmatch '^(y|Y)') { Write-Host '已取消'; exit 1 }
}
New-Item -ItemType Directory -Path $target -Force | Out-Null
Copy-Item (Join-Path $payload '*') $target -Recurse -Force

$startDir = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
$lnkPath = Join-Path $startDir 'NukeTree.lnk'
$ws = New-Object -ComObject WScript.Shell
$lnk = $ws.CreateShortcut($lnkPath)
$lnk.TargetPath = Join-Path $target 'NukeTree.exe'
$lnk.WorkingDirectory = $target
$lnk.IconLocation = (Join-Path $target '应用图标.ico') + ',0'
$lnk.Description = 'NukeTree · 3D 应用彻底清理'
$lnk.Save()
if ($Desktop) {
    $d = Join-Path ([Environment]::GetFolderPath('Desktop')) 'NukeTree.lnk'
    $lnk2 = $ws.CreateShortcut($d)
    $lnk2.TargetPath = Join-Path $target 'NukeTree.exe'
    $lnk2.WorkingDirectory = $target
    $lnk2.IconLocation = (Join-Path $target '应用图标.ico') + ',0'
    $lnk2.Save()
}

$un = @"
@echo off
chcp 65001 >nul
echo 正在卸载 NukeTree...
del "%APPDATA%\Microsoft\Windows\Start Menu\Programs\NukeTree.lnk" 2>nul
del "%USERPROFILE%\Desktop\NukeTree.lnk" 2>nul
timeout /t 1 >nul
rmdir /s /q "%LOCALAPPDATA%\NukeTree"
echo 卸载完成。
pause
"@
Set-Content -Path (Join-Path $target '卸载.cmd') -Value $un -Encoding UTF8

Write-Host ''
Write-Host "安装完成：$target" -ForegroundColor Green
Write-Host '开始菜单已创建 NukeTree 快捷方式。'
Write-Host '删除系统目录/服务时请右键「以管理员身份运行」。'
Start-Process (Join-Path $target 'NukeTree.exe')
