# build.ps1 — 编译 NukeTree 并组装 dist 目录
# 用法：powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1
param(
    [string]$OutDir = "dist"
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $root 'src'
$assets = Join-Path $root 'assets'
$out = Join-Path $root $OutDir

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$swe = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\System.Web.Extensions.dll'
if (-not (Test-Path $csc)) { throw "找不到 csc.exe（需要 .NET Framework 4.x）：$csc" }

New-Item -ItemType Directory -Path $out -Force | Out-Null

$sources = @('ProgramNative.cs', 'ProgramUI.cs', 'CanvasPlus.cs', 'SettingsForm.cs', 'AiHelper.cs', 'Plugins.cs') |
    ForEach-Object { Join-Path $src $_ }

& $csc /nologo /platform:x64 /target:winexe `
    "/out:$out\NukeTree.exe" `
    "/win32icon:$assets\应用图标.ico" `
    "/win32manifest:$assets\app.manifest" `
    /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "/r:$swe" `
    $sources

if ($LASTEXITCODE -ne 0) { throw "编译失败（exit $LASTEXITCODE）" }

Copy-Item (Join-Path $root 'backend\NukeTree.ps1') $out -Force
Copy-Item (Join-Path $assets '应用图标.ico') $out -Force
Copy-Item (Join-Path $assets '应用图标.png') $out -Force

$pluginsSrc = Join-Path $root 'plugins'
if (Test-Path $pluginsSrc) {
    Copy-Item $pluginsSrc (Join-Path $out 'plugins') -Recurse -Force
    Remove-Item (Join-Path $out 'plugins\state.json') -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "构建完成：$out\NukeTree.exe" -ForegroundColor Green
Write-Host "提示：删除系统目录/服务需要以管理员身份运行。"
