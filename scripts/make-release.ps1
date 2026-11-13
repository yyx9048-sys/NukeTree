param(
    [string]$Version = "1.0.0"
)

# 一键生成发布包：build -> 绿色便携版 zip -> 安装版 zip -> sha256
# 用法：powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\make-release.ps1 -Version 1.0.0

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $root 'dist'
$rel = Join-Path $root 'release'
$installer = Join-Path $PSScriptRoot 'installer'

Write-Host '== 1/4 构建 ==' -ForegroundColor Cyan
& (Join-Path $root 'build.ps1')
if ($LASTEXITCODE -ne 0) { throw "构建失败（exit $LASTEXITCODE）" }

New-Item -ItemType Directory -Path $rel -Force | Out-Null

function New-Portable {
    param([string]$Name)
    $pkg = Join-Path $rel $Name
    if (Test-Path $pkg) { Remove-Item $pkg -Recurse -Force }
    New-Item -ItemType Directory -Path $pkg -Force | Out-Null
    Copy-Item (Join-Path $dist 'NukeTree.exe') $pkg -Force
    Copy-Item (Join-Path $dist 'NukeTree.ps1') $pkg -Force
    Copy-Item (Join-Path $dist '应用图标.ico') $pkg -Force
    Copy-Item (Join-Path $dist '应用图标.png') $pkg -Force
    Copy-Item (Join-Path $dist 'plugins') (Join-Path $pkg 'plugins') -Recurse -Force
    Remove-Item (Join-Path $pkg 'plugins\state.json') -ErrorAction SilentlyContinue
    Copy-Item (Join-Path $root 'README.md') $pkg -Force
    Copy-Item (Join-Path $root 'LICENSE') $pkg -Force
    Copy-Item (Join-Path $root 'PUBLISH.md') $pkg -Force

    $readme = @"
NukeTree $Version · 3D 应用彻底清理（Windows 便携版）
================================================

【启动】
  双击 NukeTree.exe 即可。删除系统目录 / 服务 / 计划任务时，请右键「以管理员身份运行」。

【功能速览】
  · 3D 依赖树：拖动旋转 / 滚轮缩放 / 点击选中；顶部「全盘」查看整机
  · 拖拽文件或文件夹进 3D 画布，直接加入依赖树
  · 顶栏「共用依赖」：标出被其它应用或系统共用的 DLL，避免误删
  · 删除前自动备份到 备份3D\del_<时间>\（文件 + 注册表导出）
  · 删除后核验：磁盘回收空间 + 残留（注册表/服务/计划任务/启动项/Prefetch/目录）
  · 鼠标悬停 3D 球体或文件行：显示文件作用（可接 AI，见设置）
  · 设置（左下角）：AI 接入（API / 同步 DSH）、背景与标注颜色

【插件】
  把插件放到 plugins\<名字>\，包含 plugin.json 和脚本或 exe：
    { "name":"名称", "version":"1.0", "description":"说明",
      "author":"作者", "command":"run.ps1", "args":"\"{path}\"" }
  插件页支持：运行（对当前选中节点）/ 启用停用 / 刷新 / 打开插件目录。

【注意】
  · 本工具会真正删除文件与注册表项，删除前请确认目标；
  · 受保护路径（C:\Windows、.dsh、Common Files、WindowsApps 等）会跳过；
  · 出错可从 备份3D 目录恢复文件与 .reg。

【卸载】
  直接删除整个 NukeTree 文件夹即可（本身不写系统服务、不装驱动）。
"@
    [System.IO.File]::WriteAllText((Join-Path $pkg '使用说明.txt'), $readme, (New-Object System.Text.UTF8Encoding($true)))
    return $pkg
}

function New-Zip {
    param([string]$Dir, [string]$ZipPath)
    if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
    Compress-Archive -Path $Dir -DestinationPath $ZipPath -CompressionLevel Optimal
    $hash = (Get-FileHash $ZipPath -Algorithm SHA256).Hash
    $name = [IO.Path]::GetFileName($ZipPath)
    [System.IO.File]::WriteAllText(($ZipPath -replace '\.zip$', '.sha256.txt'), "$hash  $name`r`n", (New-Object System.Text.ASCIIEncoding))
    Write-Host ("  生成 " + $name + "  (" + [math]::Round((Get-Item $ZipPath).Length / 1KB, 1) + " KB)  sha256=" + $hash) -ForegroundColor Green
}

Write-Host '== 2/4 绿色便携版 ==' -ForegroundColor Cyan
$portableName = "NukeTree-$Version-win64"
$portableDir = New-Portable -Name $portableName
New-Zip -Dir $portableDir -ZipPath (Join-Path $rel ($portableName + '.zip'))

Write-Host '== 3/4 安装版 ==' -ForegroundColor Cyan
$setupName = "$portableName-setup"
$setupDir = Join-Path $rel $setupName
if (Test-Path $setupDir) { Remove-Item $setupDir -Recurse -Force }
New-Item -ItemType Directory -Path (Join-Path $setupDir 'payload') -Force | Out-Null
Copy-Item (Join-Path $portableDir '*') (Join-Path $setupDir 'payload') -Recurse -Force
Copy-Item (Join-Path $installer 'install.ps1') $setupDir -Force
Copy-Item (Join-Path $installer '安装.cmd') $setupDir -Force
New-Zip -Dir $setupDir -ZipPath (Join-Path $rel ($setupName + '.zip'))

Write-Host '== 4/4 npm tarball（可选） ==' -ForegroundColor Cyan
$npmPkg = Join-Path $rel 'npm-package\package'
if (Test-Path $npmPkg) { Remove-Item $npmPkg -Recurse -Force }
New-Item -ItemType Directory -Path (Join-Path $npmPkg 'bin') -Force | Out-Null
Copy-Item (Join-Path $root 'package.json') $npmPkg -Force
Copy-Item (Join-Path $root 'bin\nuketree.js') (Join-Path $npmPkg 'bin') -Force
Copy-Item (Join-Path $root 'README.md') $npmPkg -Force
Copy-Item (Join-Path $root 'LICENSE') $npmPkg -Force
New-Item -ItemType Directory -Path (Join-Path $npmPkg 'dist') -Force | Out-Null
foreach ($f in @('NukeTree.exe', 'NukeTree.ps1', '应用图标.ico', '应用图标.png')) {
    Copy-Item (Join-Path $dist $f) (Join-Path $npmPkg 'dist') -Force
}
Copy-Item (Join-Path $dist 'plugins') (Join-Path $npmPkg 'dist\plugins') -Recurse -Force
Remove-Item (Join-Path $npmPkg 'dist\plugins\state.json') -ErrorAction SilentlyContinue
$tgz = Join-Path $rel ("nuketree-$Version.tgz")
if (Test-Path $tgz) { Remove-Item $tgz -Force }
& tar -czf $tgz -C (Join-Path $rel 'npm-package') package
if ($LASTEXITCODE -eq 0) {
    Write-Host ("  生成 nuketree-$Version.tgz (" + [math]::Round((Get-Item $tgz).Length / 1KB, 1) + " KB)") -ForegroundColor Green
} else {
    Write-Warning 'tar 不可用，跳过 npm tarball（可改用 npm pack）'
}

Write-Host ''
Write-Host '全部完成，产物在 release\：' -ForegroundColor Cyan
Get-ChildItem $rel -File | Select-Object Name, @{n = 'KB'; e = { [math]::Round($_.Length / 1KB, 1) } } | Format-Table -AutoSize
Write-Host '发布步骤见 PUBLISH.md'
