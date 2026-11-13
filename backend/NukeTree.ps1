# ============================================================
#  AppNuke 3D 后端服务
#  本地 PowerShell + HttpListener，为本机 3D 树状图应用提供 API
#  启动：powershell -NoProfile -ExecutionPolicy Bypass -File AppNuke3D.ps1
# ============================================================
[CmdletBinding()]
param(
    [int]$Port = 18765,
    [switch]$NoBrowser
)

$ErrorActionPreference = 'SilentlyContinue'
$script:Root   = $PSScriptRoot
$script:Web    = Join-Path $script:Root 'web'
$script:Backup = Join-Path $script:Root '备份3D'
$script:Prof   = Get-RealUserProfile
$script:IdCounter = 0
$script:AppsCache = $null
$script:AppsCacheTime = [datetime]::MinValue
$script:HitsCache = @{}

function Get-RealUserProfile {
    try {
        $sid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value
        $p = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\$sid" -ErrorAction SilentlyContinue).ProfileImagePath
        if ($p) { return [Environment]::ExpandEnvironmentVariables($p) }
    } catch {}
    return ("{0}{1}" -f $env:HOMEDRIVE, $env:HOMEPATH)
}
if (-not $script:Prof) { $script:Prof = Get-RealUserProfile }

# ---------------- 通用工具 ----------------
function Fmt-Time($t) { if (-not $t) { return '' }; try { return ([datetime]$t).ToString('yyyy-MM-dd HH:mm:ss') } catch { return '' } }
function Fmt-Size([double]$b) {
    if ($b -le 0) { return '0 B' }
    $u = @('B','KB','MB','GB','TB'); $i = 0; $v = $b
    while ($v -ge 1024 -and $i -lt 4) { $v = $v / 1024; $i++ }
    if ($i -eq 0) { return ("{0:N0} B" -f $v) }
    return ("{0:N1} {1}" -f $v, $u[$i])
}
function Get-DirStat([string]$Path) {
    $cnt = 0; $sum = 0.0
    try {
        $files = [System.IO.Directory]::EnumerateFiles($Path, '*', [System.IO.SearchOption]::AllDirectories)
        foreach ($f in $files) {
            $cnt++
            if ($cnt -gt 50000) { break }
            try { $sum += (New-Object System.IO.FileInfo $f).Length } catch {}
        }
    } catch {
        try {
            $items = Get-ChildItem -LiteralPath $Path -Recurse -File -Force -ErrorAction SilentlyContinue
            $cnt = ($items | Measure-Object).Count
            $sum = ($items | Measure-Object Length -Sum).Sum
        } catch {}
    }
    return @{ Count = $cnt; Size = [double]$sum }
}
function Is-Protected([string]$p) {
    if (-not $p) { return $false }
    if ($p -match '^[A-Za-z]:\\Windows(\\|$)') { return $true }
    if ($p -match '[\\/]\.dsh([\\/]|$)') { return $true }
    if ($p -match '[\\/]@deepseek-ai([\\/]|$)') { return $true }
    if ($p -match '[\\/]deepseek Harness[\\/]deepseek([\\/]|$)') { return $true }
    if ($p -match '[\\/]Common Files([\\/]|$)') { return $true }
    if ($p -match '[\\/]WindowsApps([\\/]|$)') { return $true }
    if ($p -match '[\\/]System32([\\/]|$)') { return $true }
    return $false
}
function Get-DiskFree {
    $out = @{}
    Get-PSDrive -PSProvider FileSystem -ErrorAction SilentlyContinue | ForEach-Object {
        if ($_.Free -ne $null) { $out[$_.Name] = [math]::Round($_.Free / 1GB, 2) }
    }
    return $out
}
function Get-NodeId { $script:IdCounter++; return ('n' + $script:IdCounter) }
function New-Node([string]$Name, [string]$Kind, [string]$Path, [double]$Size, [string]$Time, [string]$Detail, [string[]]$Refs, [string]$Del) {
    return @{
        id = (Get-NodeId); name = $Name; kind = $Kind; path = $Path
        size = [double]$Size; time = $Time; detail = $Detail; refs = @($Refs)
        del = $Del; children = @()
    }
}

# ---------------- 已安装应用 ----------------
function Resolve-AppExe([string]$name, [string]$loc, [string]$icon, [string]$uninst) {
    $cands = @()
    if ($icon) { $p = ($icon -split ',')[0].Trim().Trim('"'); if ($p -like '*.exe' -and (Test-Path -LiteralPath $p)) { $cands += $p } }
    if ($loc -and (Test-Path -LiteralPath $loc)) {
        $exes = @(Get-ChildItem -LiteralPath $loc -File -Filter '*.exe' -ErrorAction SilentlyContinue)
        foreach ($e in $exes) { $cands += $e.FullName }
    }
    if ($uninst) { $m = [regex]::Match($uninst, '"([^"]+\.exe)"'); if ($m.Success) { $cands += $m.Groups[1].Value } }
    if ($cands.Count -eq 0) { return '' }
    $base = $name.ToLower() -replace '[^a-z0-9]', ''
    foreach ($c in $cands) {
        $bn = ([IO.Path]::GetFileNameWithoutExtension($c)).ToLower() -replace '[^a-z0-9]', ''
        if ($bn -and $base -and ($base.Contains($bn) -or $bn.Contains($base))) { return $c }
    }
    $clean = @($cands | Where-Object { $_ -notmatch '(?i)(unins|uninstall|setup|update)' })
    if ($clean.Count -gt 0) { return $clean[0] }
    return ($cands | Select-Object -First 1)
}
function Get-Apps {
    if ($script:AppsCache -and ((Get-Date) - $script:AppsCacheTime).TotalSeconds -lt 120) { return $script:AppsCache }
    $list = New-Object System.Collections.ArrayList
    $roots = @(
        @{ P = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall'; S = '系统' },
        @{ P = 'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall'; S = '系统x86' },
        @{ P = 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall'; S = '用户' }
    )
    foreach ($r in $roots) {
        Get-ChildItem -LiteralPath $r.P -ErrorAction SilentlyContinue | ForEach-Object {
            $k = $_
            $p = Get-ItemProperty -LiteralPath $k.PSPath -ErrorAction SilentlyContinue
            if ($p -and $p.DisplayName) {
                $loc = ''
                if ($p.InstallLocation) { $loc = ([string]$p.InstallLocation).Trim().Trim('"') }
                $exe = Resolve-AppExe -name ([string]$p.DisplayName) -loc $loc -icon ([string]$p.DisplayIcon) -uninst ([string]$p.UninstallString)
                $sz = 0.0
                if ($exe -and (Test-Path -LiteralPath $exe)) { $sz = (Get-Item -LiteralPath $exe).Length }
                [void]$list.Add(@{
                    name = [string]$p.DisplayName; version = [string]$p.DisplayVersion
                    publisher = [string]$p.Publisher; location = $loc; exe = $exe
                    scope = $r.S; size = [double]$sz; key = $k.Name
                })
            }
        }
    }
    $script:AppsCache = $list
    $script:AppsCacheTime = Get-Date
    return $list
}

# ---------------- 目录扫描 ----------------
function Walk-Dir([string]$Dir, [int]$Depth, [string]$Kw, $Acc) {
    if ($Depth -lt 0) { return }
    try { $entries = [System.IO.Directory]::EnumerateFileSystemEntries($Dir) } catch { return }
    foreach ($e in $entries) {
        $nm = [IO.Path]::GetFileName($e)
        if ($nm -in @('node_modules', '.git', 'WinSxS', 'Windows', 'WindowsApps', 'System Volume Information', '$RECYCLE.BIN')) { continue }
        if ($nm.IndexOf($Kw, [StringComparison]::OrdinalIgnoreCase) -ge 0) { [void]$Acc.Add($e) }
        if ([IO.Directory]::Exists($e)) { Walk-Dir -Dir $e -Depth ($Depth - 1) -Kw $Kw -Acc $Acc }
    }
}
function Get-FsHits([string]$Kw) {
    $acc = New-Object System.Collections.ArrayList
    $Up = $script:Prof
    $roots = @()
    $roots += @{ P = $Up; D = 1 }
    foreach ($sub in @('Downloads', 'Documents', 'Desktop')) {
        $p = Join-Path $Up $sub
        if (Test-Path -LiteralPath $p) { $roots += @{ P = $p; D = 2 } }
    }
    $roots += @{ P = (Join-Path $Up 'AppData\Roaming'); D = 3 }
    $roots += @{ P = (Join-Path $Up 'AppData\Local'); D = 3 }
    $roots += @{ P = (Join-Path $Up 'AppData\LocalLow'); D = 2 }
    $roots += @{ P = (Join-Path $Up 'AppData\Local\Temp'); D = 2 }
    if ($env:ProgramData) { $roots += @{ P = $env:ProgramData; D = 2 } }
    foreach ($pf in @($env:ProgramFiles, ${env:ProgramFiles(x86)})) { if ($pf) { $roots += @{ P = $pf; D = 2 } } }
    foreach ($d in (Get-PSDrive -PSProvider FileSystem -ErrorAction SilentlyContinue)) {
        if ($d.Root -and (Test-Path -LiteralPath $d.Root)) { $roots += @{ P = $d.Root; D = 2 } }
    }
    foreach ($r in $roots) { if (Test-Path -LiteralPath $r.P) { Walk-Dir -Dir $r.P -Depth $r.D -Kw $Kw -Acc $acc } }
    $sorted = $acc | Sort-Object { $_.Length }
    $kept = New-Object System.Collections.ArrayList
    foreach ($p in $sorted) {
        $child = $false
        foreach ($k in $kept) { if ($p.StartsWith($k + '\', [StringComparison]::OrdinalIgnoreCase)) { $child = $true; break } }
        if (-not $child) { [void]$kept.Add($p) }
    }
    return $kept
}
function Get-ShortcutHits([string]$Kw) {
    $out = New-Object System.Collections.ArrayList
    $wsh = New-Object -ComObject WScript.Shell
    $roots = @(
        (Join-Path $script:Prof 'AppData\Roaming\Microsoft\Windows\Start Menu'),
        (Join-Path $script:Prof 'Desktop'),
        (Join-Path $script:Prof 'AppData\Roaming\Microsoft\Windows\Recent'),
        (Join-Path $script:Prof 'AppData\Roaming\Microsoft\Internet Explorer\Quick Launch'),
        ($env:ProgramData + '\Microsoft\Windows\Start Menu'),
        ($env:PUBLIC + '\Desktop')
    )
    foreach ($root in $roots) {
        if (-not (Test-Path -LiteralPath $root)) { continue }
        Get-ChildItem -LiteralPath $root -Recurse -Force -Filter '*.lnk' -ErrorAction SilentlyContinue | ForEach-Object {
            $f = $_
            $t = ''
            try { $lnk = $wsh.CreateShortcut($f.FullName); $t = [string]$lnk.TargetPath } catch {}
            $hay = ($f.Name + ' ' + $t)
            if ($hay.IndexOf($Kw, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                [void]$out.Add(@{ path = $f.FullName; name = $f.Name; target = $t; size = [double]$f.Length; time = (Fmt-Time $f.LastWriteTime) })
            }
        }
    }
    return $out
}
function Walk-RegKey([string]$Path, [int]$Depth, [string]$Kw, $Acc, [string[]]$Skip) {
    if ($Depth -lt 0) { return }
    Get-ChildItem -LiteralPath $Path -ErrorAction SilentlyContinue | ForEach-Object {
        if ($Skip -contains $_.PSChildName) { return }
        if ($_.PSChildName.IndexOf($Kw, [StringComparison]::OrdinalIgnoreCase) -ge 0) { [void]$Acc.Add($_.Name) }
        if ($Depth -gt 0) { Walk-RegKey -Path $_.PSPath -Depth ($Depth - 1) -Kw $Kw -Acc $Acc -Skip $Skip }
    }
}
function Get-RegKeyHits([string]$Kw) {
    $acc = New-Object System.Collections.ArrayList
    $roots = @(
        @{ P = 'HKCU:\SOFTWARE'; D = 3; S = @('Classes', 'Wow6432Node', 'Policies') },
        @{ P = 'HKLM:\SOFTWARE'; D = 2; S = @('Classes', 'WOW6432Node', 'Policies', 'Microsoft') },
        @{ P = 'HKLM:\SOFTWARE\Microsoft'; D = 2; S = @('Windows') },
        @{ P = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion'; D = 1; S = @() },
        @{ P = 'HKCU:\SOFTWARE\Classes'; D = 1; S = @() },
        @{ P = 'HKLM:\SOFTWARE\Classes'; D = 1; S = @() }
    )
    foreach ($r in $roots) { if (Test-Path -LiteralPath $r.P) { Walk-RegKey -Path $r.P -Depth $r.D -Kw $Kw -Acc $acc -Skip $r.S } }
    return ($acc | Sort-Object -Unique)
}
function Get-RegValueHits([string]$Kw) {
    $out = New-Object System.Collections.ArrayList
    $roots = @(
        @{ P = 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run'; D = 0; L = '自启' },
        @{ P = 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce'; D = 0; L = '自启' },
        @{ P = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run'; D = 0; L = '自启' },
        @{ P = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce'; D = 0; L = '自启' },
        @{ P = 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\RunMRU'; D = 0; L = '运行历史' },
        @{ P = 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MuiCache'; D = 0; L = '缓存' },
        @{ P = 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths'; D = 1; L = 'App Paths' },
        @{ P = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths'; D = 1; L = 'App Paths' },
        @{ P = 'HKCU:\SOFTWARE\DownloadManager'; D = 2; L = '下载器' },
        @{ P = 'HKCU:\SOFTWARE\Classes'; D = 1; L = '协议/关联' },
        @{ P = 'HKLM:\SOFTWARE\Classes'; D = 1; L = '协议/关联' },
        @{ P = 'HKLM:\SYSTEM\CurrentControlSet\Services'; D = 0; L = '服务' }
    )
    foreach ($r in $roots) {
        if (-not (Test-Path -LiteralPath $r.P)) { continue }
        $keys = New-Object System.Collections.ArrayList
        $addKeys = {
            param($p, $d)
            if (Test-Path -LiteralPath $p) { [void]$keys.Add((Get-Item -LiteralPath $p)) }
            if ($d -gt 0) { Get-ChildItem -LiteralPath $p -ErrorAction SilentlyContinue | ForEach-Object { & $addKeys $_.PSPath ($d - 1) } }
        }
        & $addKeys $r.P $r.D
        foreach ($k in $keys) {
            $props = Get-ItemProperty -LiteralPath $k.PSPath -ErrorAction SilentlyContinue
            if (-not $props) { continue }
            foreach ($pr in $props.PSObject.Properties) {
                if ($pr.Name -match '^PS') { continue }
                if ($pr.Value -isnot [string]) { continue }
                $hit = $pr.Name.IndexOf($Kw, [StringComparison]::OrdinalIgnoreCase) -ge 0
                if (-not $hit) { $hit = $pr.Value.IndexOf($Kw, [StringComparison]::OrdinalIgnoreCase) -ge 0 }
                if ($hit) {
                    [void]$out.Add(@{ cat = $r.L; keyName = $k.Name; keyPS = $k.PSPath; name = $pr.Name; value = $pr.Value })
                }
            }
        }
    }
    return $out
}
function Get-PrefetchHits([string]$Kw) {
    $out = @()
    Get-ChildItem -LiteralPath 'C:\Windows\Prefetch' -Force -Filter '*.pf' -ErrorAction SilentlyContinue | ForEach-Object {
        if ($_.Name.IndexOf($Kw, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $out += @{ path = $_.FullName; name = $_.Name; size = [double]$_.Length; time = (Fmt-Time $_.LastWriteTime) }
        }
    }
    return $out
}
function Get-ProcHits([string]$Kw) {
    $out = @()
    $all = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue)
    $parentOf = @{}
    foreach ($p in $all) { $parentOf[[int]$p.ProcessId] = [int]$p.ParentProcessId }
    $self = New-Object System.Collections.Generic.HashSet[int]
    $cur = [int]$PID; $guard = 0
    while ($cur -gt 0 -and $guard -lt 64) { if (-not $self.Add($cur)) { break }; if ($parentOf.ContainsKey($cur)) { $cur = $parentOf[$cur] } else { break }; $guard++ }
    foreach ($p in $all) {
        if ($self.Contains([int]$p.ProcessId)) { continue }
        $cl = '' + $p.CommandLine
        if ($cl -match 'AppNuke3D\.(ps1|cmd)') { continue }
        $hay = '' + $p.Name + ' ' + $cl
        if ($hay.IndexOf($Kw, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $ws = 0; $pm = 0
            try { $ws = [math]::Round($p.WorkingSetSize / 1MB, 1); $pm = [math]::Round($p.PrivatePageCount / 1MB, 1) } catch {}
            $out += @{ pid = [int]$p.ProcessId; name = [string]$p.Name; cmd = $cl; ws = $ws; pm = $pm }
        }
    }
    return $out
}
function Get-KeywordHits([string]$Kw) {
    if ($script:HitsCache.ContainsKey($Kw)) { return $script:HitsCache[$Kw] }
    $fs = @(Get-FsHits $Kw)
    $lnk = @(Get-ShortcutHits $Kw)
    $regk = @(Get-RegKeyHits $Kw)
    $regv = @(Get-RegValueHits $Kw)
    $pf = @(Get-PrefetchHits $Kw)
    $proc = @(Get-ProcHits $Kw)
    $hits = @()
    $seen = @{}
    foreach ($p in $fs) {
        if ($seen.ContainsKey($p)) { continue }
        $seen[$p] = $true
        $isDir = [IO.Directory]::Exists($p)
        $it = Get-Item -LiteralPath $p -Force -ErrorAction SilentlyContinue
        if (-not $it) { continue }
        if ($isDir) {
            $st = Get-DirStat $p
            $hits += @{ kind = 'dir'; path = $p; name = $it.Name; size = $st.Size; time = (Fmt-Time $it.LastWriteTime); detail = ('目录 · ' + $st.Count + ' 个文件'); del = 'dir' }
        } else {
            $ext = $it.Extension.ToLower()
            $cfg = @('.json', '.jsonc', '.yml', '.yaml', '.ini', '.toml', '.xml', '.conf', '.cfg', '.db', '.sqlite', '.log', '.env', '.reg')
            $kind = if ($cfg -contains $ext) { 'config' } else { 'file' }
            $hits += @{ kind = $kind; path = $p; name = $it.Name; size = [double]$it.Length; time = (Fmt-Time $it.LastWriteTime); detail = ('文件 ' + $ext); del = 'file' }
        }
    }
    foreach ($l in $lnk) { $hits += @{ kind = 'lnk'; path = $l.path; name = $l.name; size = $l.size; time = $l.time; detail = ('快捷方式 → ' + $l.target); del = 'file' } }
    foreach ($k in $regk) { $hits += @{ kind = 'regkey'; path = $k; name = ($k.Split('\')[-1]); size = 0; time = ''; detail = $k; del = 'regkey' } }
    foreach ($v in $regv) { $hits += @{ kind = 'regvalue'; path = $v.keyName + ' \ ' + $v.name; name = ($v.keyName.Split('\')[-1] + ' \ ' + $v.name); size = 0; time = ''; detail = ('[' + $v.cat + '] ' + $v.value); del = 'regvalue'; regKey = $v.keyName; regName = $v.name; regPS = $v.keyPS } }
    foreach ($p in $proc) { $hits += @{ kind = 'proc'; path = ''; name = $p.name; size = 0; time = ''; detail = ('PID ' + $p.pid + ' · 内存 ' + $p.ws + ' MB'); del = 'proc'; pid = $p.pid } }
    foreach ($p in $pf) { $hits += @{ kind = 'prefetch'; path = $p.path; name = $p.name; size = $p.size; time = $p.time; detail = 'Prefetch'; del = 'file' } }
    $script:HitsCache[$Kw] = $hits
    return $hits
}

# ---------------- 树构建 ----------------
function Get-ChildNodes([string]$Dir, [int]$Limit) {
    $out = @()
    $items = @(Get-ChildItem -LiteralPath $Dir -Force -ErrorAction SilentlyContinue | Select-Object -First $Limit)
    foreach ($i in $items) {
        if ($i.PSIsContainer) {
            $out += (New-Node $i.Name 'dir' $i.FullName 0 (Fmt-Time $i.LastWriteTime) '子目录' @() 'dir')
        } else {
            $ext = $i.Extension.ToLower()
            $cfg = @('.json', '.jsonc', '.yml', '.yaml', '.ini', '.toml', '.xml', '.conf', '.cfg', '.db', '.sqlite', '.log', '.env')
            $k = if ($cfg -contains $ext) { 'config' } else { 'file' }
            $out += (New-Node $i.Name $k $i.FullName ([double]$i.Length) (Fmt-Time $i.LastWriteTime) ('文件 ' + $ext) @() 'file')
        }
    }
    return $out
}
function Add-HitChildren($Parent, $Hits, [string]$ExcludePrefix) {
    foreach ($h in $Hits) {
        if ($ExcludePrefix -and $h.path -and $h.path.StartsWith($ExcludePrefix, [StringComparison]::OrdinalIgnoreCase)) { continue }
        $node = New-Node $h.name $h.kind $h.path ([double]$h.size) ([string]$h.time) ([string]$h.detail) @() ([string]$h.del)
        if ($h.kind -eq 'dir') { $node.children = @(Get-ChildNodes $h.path 40) }
        if ($h.kind -eq 'regkey') {
            $psp = 'Registry::' + $h.path
            $props = Get-ItemProperty -LiteralPath $psp -ErrorAction SilentlyContinue
            if ($props) {
                $vals = @()
                foreach ($pr in $props.PSObject.Properties) {
                    if ($pr.Name -match '^PS') { continue }
                    if ($pr.Value -isnot [string]) { continue }
                    $vn = New-Node ('值: ' + $pr.Name) 'regvalue' ($h.path + ' \ ' + $pr.Name) 0 '' $pr.Value @('regvalue') 'regvalue'
                    $vn.regKey = $h.path; $vn.regName = $pr.Name
                    $vals += $vn
                }
                $node.children = @($vals | Select-Object -First 30)
            }
        }
        if ($h.kind -eq 'regvalue') { $node.regKey = $h.regKey; $node.regName = $h.regName; $node.regPS = $h.regPS }
        if ($h.kind -eq 'proc') { $node.pid = $h.pid }
        $Parent.children += $node
    }
}
function Build-Tree([string]$Kw, [string]$ManualPath) {
    $script:IdCounter = 0
    $root = New-Node ('目标: ' + $Kw) 'root' '' 0 '' ('关键词「' + $Kw + '」') @() 'none'
    if ($ManualPath) {
        $p = $ManualPath.Trim().Trim('"')
        if (Test-Path -LiteralPath $p) {
            $it = Get-Item -LiteralPath $p -Force
            if ($it.PSIsContainer) {
                $st = Get-DirStat $p
                $l1 = New-Node $it.Name 'dir' $p $st.Size (Fmt-Time $it.LastWriteTime) ('手动选择 · 目录 · ' + $st.Count + ' 个文件') @() 'dir'
                $l1.children = @(Get-ChildNodes $p 60)
            } else {
                $l1 = New-Node $it.Name 'file' $p ([double]$it.Length) (Fmt-Time $it.LastWriteTime) '手动选择 · 文件' @() 'file'
                $l1.children = @(Get-ChildNodes (Split-Path $p -Parent) 30)
            }
            $root.children += $l1
        } else {
            $root.detail = '路径不存在: ' + $p
        }
        return $root
    }
    $apps = @(Get-Apps)
    $kwL = $Kw.ToLower()
    $matched = @($apps | Where-Object { (($_.name + ' ' + $_.publisher + ' ' + $_.location).ToLower()).Contains($kwL) })
    $hits = @(Get-KeywordHits $Kw)
    foreach ($a in $matched) {
        $exe = [string]$a.exe
        $size = 0.0; $time = ''
        if ($exe -and (Test-Path -LiteralPath $exe)) { $fi = Get-Item -LiteralPath $exe; $size = [double]$fi.Length; $time = Fmt-Time $fi.LastWriteTime }
        $l1 = New-Node $a.name 'app' $exe $size $time ('应用本体 · v' + $a.version + ' · ' + $a.publisher) @('范围: ' + $a.scope, '安装目录: ' + $a.location) 'file'
        $exclude = ''
        if ($a.location -and (Test-Path -LiteralPath $a.location)) {
            $di = Get-Item -LiteralPath $a.location
            $st = Get-DirStat $a.location
            $dn = New-Node $di.Name 'dir' $a.location $st.Size (Fmt-Time $di.LastWriteTime) ('安装目录 · ' + $st.Count + ' 个文件') @() 'dir'
            $dn.children = @(Get-ChildNodes $a.location 40)
            $l1.children += $dn
            $exclude = $a.location
        }
        Add-HitChildren $l1 $hits $exclude
        $root.children += $l1
    }
    if ($root.children.Count -eq 0) {
        if ($hits.Count -gt 0) {
            $l1 = New-Node $Kw 'item' '' 0 (Fmt-Time (Get-Date)) ('未匹配到已安装应用，以下为残留项 (' + $hits.Count + ')') @() 'none'
            Add-HitChildren $l1 $hits ''
            $root.children += $l1
        }
    }
    return $root
}

# ---------------- 下载目录 ----------------
function Get-Downloads([string]$Kw) {
    $out = @()
    $dirs = @()
    $dl = Join-Path $script:Prof 'Downloads'
    if (Test-Path -LiteralPath $dl) { $dirs += $dl }
    foreach ($d in (Get-PSDrive -PSProvider FileSystem -ErrorAction SilentlyContinue)) {
        foreach ($n in @('下载', 'Downloads')) {
            $p = Join-Path $d.Root $n
            if (Test-Path -LiteralPath $p) { $dirs += $p }
        }
    }
    $dirs = $dirs | Sort-Object -Unique
    foreach ($dir in $dirs) {
        Get-ChildItem -LiteralPath $dir -File -Recurse -Depth 2 -Force -ErrorAction SilentlyContinue | Where-Object {
            (-not $Kw) -or ($_.Name.IndexOf($Kw, [StringComparison]::OrdinalIgnoreCase) -ge 0)
        } | Select-Object -First 150 | ForEach-Object {
            $out += @{ name = $_.Name; path = $_.FullName; size = [double]$_.Length; time = (Fmt-Time $_.LastWriteTime); src = $dir; del = 'file' }
        }
    }
    Get-ChildItem 'HKCU:\SOFTWARE\DownloadManager' -ErrorAction SilentlyContinue | ForEach-Object {
        $p = Get-ItemProperty -LiteralPath $_.PSPath -ErrorAction SilentlyContinue
        if (-not $p) { return }
        $hay = '' + $p.FileName + ' ' + $p.LocalFileName + ' ' + $p.Url0
        if ($Kw -and $hay.IndexOf($Kw, [StringComparison]::OrdinalIgnoreCase) -lt 0) { return }
        $fp = [string]$p.LocalFileName
        $out += @{ name = [IO.Path]::GetFileName($fp); path = $fp; size = 0; time = (Fmt-Time $_.LastWriteTime); src = 'IDM 下载记录'; del = 'file' }
    }
    return $out
}

# ---------------- 在线查依赖 ----------------
function Get-OnlineInfo([string]$Kw) {
    $q = [uri]::EscapeDataString($Kw + ' 依赖 组件 分析 windows')
    $q2 = [uri]::EscapeDataString($Kw + ' dependencies windows exe')
    return @{
        keyword = $Kw
        links = @(
            @{ title = 'Bing: ' + $Kw + ' 依赖 / 组件分析'; url = 'https://www.bing.com/search?q=' + $q },
            @{ title = 'Google: ' + $Kw + ' dependencies (exe)'; url = 'https://www.google.com/search?q=' + $q2 },
            @{ title = 'GitHub: ' + $Kw + ' dependency tool'; url = 'https://github.com/search?q=' + [uri]::EscapeDataString($Kw) + '&type=repositories' },
            @{ title = 'Dependency Walker 官方'; url = 'https://www.dependencywalker.com/' },
            @{ title = 'Dependencies (lucasg, 现代版)'; url = 'https://github.com/lucasg/Dependencies/releases' },
            @{ title = 'Sysinternals Process Explorer'; url = 'https://learn.microsoft.com/sysinternals/downloads/process-explorer' }
        )
    }
}

# ---------------- 共用依赖 / 残留检测 ----------------
function Test-SharedDependency([string]$Path, $Apps, [string]$OwnLoc) {
    $refs = New-Object System.Collections.ArrayList
    $lp = ''
    try { $lp = $Path.ToLower() } catch { return @() }
    $sys = @(
        ($env:WINDIR + '\system32'),
        ($env:WINDIR + '\syswow64'),
        ($env:WINDIR + '\assembly'),
        ($env:CommonProgramFiles),
        ($env:CommonProgramFiles + ' (x86)'),
        ($env:ProgramData)
    )
    foreach ($s in $sys) {
        if ($s -and $lp.StartsWith($s.ToLower())) { [void]$refs.Add('Windows/系统共享'); break }
    }
    $own = ''
    if ($OwnLoc) { $own = $OwnLoc.TrimEnd('\').ToLower() }
    foreach ($a in $Apps) {
        if (-not $a.location) { continue }
        $al = ([string]$a.location).TrimEnd('\').ToLower()
        if ($al.Length -lt 6) { continue }
        if ($own -and $al -eq $own) { continue }
        if ($lp.StartsWith($al + '\')) { [void]$refs.Add([string]$a.name) }
    }
    return @($refs)
}
function Get-SharedDeps([string]$Kw) {
    if (-not $Kw) { $Kw = '' }
    $apps = @(Get-Apps)
    $targets = @($apps | Where-Object {
        ($Kw -eq '') -or
        ($_.name -and $_.name.IndexOf($Kw, [StringComparison]::OrdinalIgnoreCase) -ge 0) -or
        ($_.exe -and $_.exe.IndexOf($Kw, [StringComparison]::OrdinalIgnoreCase) -ge 0)
    } | Select-Object -First 4)
    $items = New-Object System.Collections.ArrayList
    $seen = @{}
    # 文件名 -> 使用它的应用（用于识别“多个目标共用同一依赖”）
    $owners = @{}
    foreach ($a in ($apps | Where-Object { $_.location -and (Test-Path -LiteralPath $_.location) } | Select-Object -First 25)) {
        try {
            $fs = @(Get-ChildItem -LiteralPath $a.location -Recurse -Depth 1 -File -ErrorAction SilentlyContinue | Select-Object -First 600)
            foreach ($f in $fs) {
                $lk = $f.Name.ToLower()
                if (-not $owners.ContainsKey($lk)) { $owners[$lk] = New-Object System.Collections.ArrayList }
                if (-not $owners[$lk].Contains([string]$a.name)) { [void]$owners[$lk].Add([string]$a.name) }
            }
        } catch { }
    }
    foreach ($t in $targets) {
        $loc = [string]$t.location
        if (-not $loc -or -not (Test-Path -LiteralPath $loc)) { continue }
        $deps = @()
        try {
            $deps = @(Get-ChildItem -LiteralPath $loc -Recurse -Depth 2 -File -ErrorAction SilentlyContinue |
                Where-Object { $_.Extension -in @('.dll','.exe','.mui','.json','.ini','.cfg','.dat','.pak','.node') } |
                Select-Object -First 250)
        } catch { }
        foreach ($d in $deps) {
            $key = $d.FullName.ToLower()
            if ($seen.ContainsKey($key)) { continue }
            $seen[$key] = $true
            $refs = @(Test-SharedDependency -Path $d.FullName -Apps $apps -OwnLoc $loc)
            $lk = $d.Name.ToLower()
            if ($owners.ContainsKey($lk)) {
                foreach ($on in $owners[$lk]) {
                    if ($on -ne [string]$t.name -and ($refs -notcontains $on)) { $refs += $on }
                }
            }
            [void]$items.Add(@{
                name = $d.Name
                path = $d.FullName
                size = [double]$d.Length
                owner = [string]$t.name
                shared = ($refs.Count -gt 0)
                refs = @($refs)
                kind = $(if ($d.Extension -eq '.dll') { 'DLL' } elseif ($d.Extension -eq '.exe') { 'EXE' } else { '数据' })
            })
        }
    }
    $sharedItems = @($items | Where-Object { $_.shared })
    return @{
        kw = $Kw
        targets = @($targets | ForEach-Object { @{ name = $_.name; location = $_.location; exe = $_.exe } })
        items = @($items)
        total = $items.Count
        sharedCount = $sharedItems.Count
        time = (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
    }
}
function Get-Residue([string]$Kw) {
    $out = New-Object System.Collections.ArrayList
    if (-not $Kw) { return @{ kw = ''; items = @(); count = 0 } }
    $roots = @(
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall',
        'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall'
    )
    foreach ($r in $roots) {
        Get-ChildItem -LiteralPath $r -ErrorAction SilentlyContinue | ForEach-Object {
            $p = Get-ItemProperty -LiteralPath $_.PSPath -ErrorAction SilentlyContinue
            if ($p -and $p.DisplayName -and ([string]$p.DisplayName).IndexOf($Kw, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                [void]$out.Add(@{ type = '注册表卸载项'; name = [string]$p.DisplayName; path = $_.Name })
            }
        }
    }
    try {
        Get-Service -ErrorAction SilentlyContinue | Where-Object { ($_.Name + ' ' + $_.DisplayName).IndexOf($Kw, [StringComparison]::OrdinalIgnoreCase) -ge 0 } | ForEach-Object {
            [void]$out.Add(@{ type = '服务'; name = $_.Name; path = ('状态 ' + $_.Status) })
        }
    } catch { }
    try {
        Get-ScheduledTask -ErrorAction SilentlyContinue | Where-Object { $_.TaskName.IndexOf($Kw, [StringComparison]::OrdinalIgnoreCase) -ge 0 } | ForEach-Object {
            [void]$out.Add(@{ type = '计划任务'; name = $_.TaskName; path = $_.TaskPath })
        }
    } catch { }
    foreach ($rk in @('HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run', 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run')) {
        $k = Get-Item -LiteralPath $rk -ErrorAction SilentlyContinue
        if ($k) {
            foreach ($n in $k.GetValueNames()) {
                $v = [string]$k.GetValue($n)
                if ($n.IndexOf($Kw, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or $v.IndexOf($Kw, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                    [void]$out.Add(@{ type = '启动项'; name = $n; path = $v })
                }
            }
        }
    }
    try {
        Get-ChildItem -LiteralPath (Join-Path $env:WINDIR 'Prefetch') -Filter '*.pf' -ErrorAction SilentlyContinue | Where-Object { $_.Name.IndexOf($Kw, [StringComparison]::OrdinalIgnoreCase) -ge 0 } | ForEach-Object {
            [void]$out.Add(@{ type = 'Prefetch'; name = $_.Name; path = $_.FullName })
        }
    } catch { }
    foreach ($base in @($env:APPDATA, $env:LOCALAPPDATA, $env:ProgramData)) {
        try {
            Get-ChildItem -LiteralPath $base -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name.IndexOf($Kw, [StringComparison]::OrdinalIgnoreCase) -ge 0 } | ForEach-Object {
                [void]$out.Add(@{ type = '残留目录'; name = $_.Name; path = $_.FullName })
            }
        } catch { }
    }
    return @{ kw = $Kw; items = @($out); count = $out.Count; time = (Get-Date -Format 'yyyy-MM-dd HH:mm:ss') }
}
# ---------------- 删除 / 核验 ----------------
function Invoke-DeleteItems($Items) {
    $res = @{ deleted = @(); failed = @(); skipped = @(); backup = ''; diskBefore = (Get-DiskFree) }
    $stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $bk = Join-Path $script:Backup ('del_' + $stamp)
    New-Item -ItemType Directory -Path (Join-Path $bk 'files') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $bk 'reg') -Force | Out-Null
    foreach ($it in $Items) {
        $del = [string]$it.del
        $p = [string]$it.path
        $nm = [string]$it.name
        try {
            switch ($del) {
                'file' {
                    if (-not $p -or -not (Test-Path -LiteralPath $p)) { $res.skipped += $nm; break }
                    if (Is-Protected $p) { $res.failed += @{ name = $nm; path = $p; error = '受保护路径，已跳过' }; break }
                    $name = [IO.Path]::GetFileName($p)
                    $h = [System.BitConverter]::ToString([System.Security.Cryptography.SHA1]::Create().ComputeHash([Text.Encoding]::UTF8.GetBytes($p))).Replace('-', '').Substring(0, 8)
                    Copy-Item -LiteralPath $p -Destination (Join-Path $bk ('files\' + $h + '_' + $name)) -Force -ErrorAction SilentlyContinue
                    Remove-Item -LiteralPath $p -Force -ErrorAction Stop
                    $res.deleted += $nm
                }
                'dir' {
                    if (-not $p -or -not (Test-Path -LiteralPath $p)) { $res.skipped += $nm; break }
                    if (Is-Protected $p) { $res.failed += @{ name = $nm; path = $p; error = '受保护路径，已跳过' }; break }
                    $h = [System.BitConverter]::ToString([System.Security.Cryptography.SHA1]::Create().ComputeHash([Text.Encoding]::UTF8.GetBytes($p))).Replace('-', '').Substring(0, 8)
                    Copy-Item -LiteralPath $p -Destination (Join-Path $bk ('files\' + $h + '_' + [IO.Path]::GetFileName($p))) -Recurse -Force -ErrorAction SilentlyContinue
                    Remove-Item -LiteralPath $p -Recurse -Force -ErrorAction Stop
                    $res.deleted += $nm
                }
                'regkey' {
                    if (-not $p) { $res.skipped += $nm; break }
                    if ($p -match '^HKEY_LOCAL_MACHINE\\SOFTWARE\\(Classes|WOW6432Node\\Classes|Microsoft\\Windows)') { $res.failed += @{ name = $nm; path = $p; error = '系统共享注册表，已跳过' }; break }
                    $regFile = Join-Path $bk ('reg\' + ($p -replace '[^A-Za-z0-9_]', '_') + '.reg')
                    reg export $p $regFile /y 2>$null | Out-Null
                    $ps = 'Registry::' + $p
                    Remove-Item -LiteralPath $ps -Recurse -Force -ErrorAction Stop
                    $res.deleted += $nm
                }
                'regvalue' {
                    $k = [string]$it.regKey; $n = [string]$it.regName
                    if (-not $k -or -not $n) { $res.skipped += $nm; break }
                    $regFile = Join-Path $bk ('reg\' + ($k -replace '[^A-Za-z0-9_]', '_') + '.reg')
                    reg export $k $regFile /y 2>$null | Out-Null
                    $ps = if ($it.regPS) { $it.regPS } else { 'Registry::' + $k }
                    Remove-ItemProperty -LiteralPath $ps -Name $n -Force -ErrorAction Stop
                    $res.deleted += $nm
                }
                'proc' { if ($it.pid) { Stop-Process -Id ([int]$it.pid) -Force -ErrorAction SilentlyContinue; $res.deleted += $nm } else { $res.skipped += $nm } }
                default { $res.skipped += $nm }
            }
        } catch {
            $res.failed += @{ name = $nm; path = $p; error = $_.Exception.Message }
        }
    }
    $res.backup = $bk
    $res.diskAfter = (Get-DiskFree)
    $script:HitsCache = @{}
    return $res
}
function Get-Verify([string]$Kw, $CheckPaths) {
    $left = @()
    if ($CheckPaths) {
        foreach ($p in $CheckPaths) { if ($p -and (Test-Path -LiteralPath $p)) { $left += $p } }
    }
    $procs = @(Get-ProcHits $Kw)
    return @{
        keyword = $Kw
        leftovers = $left
        leftoverCount = $left.Count
        procs = $procs
        procCount = $procs.Count
        memMB = [math]::Round((($procs | Measure-Object -Property ws -Sum).Sum), 1)
        diskFree = (Get-DiskFree)
        time = (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
    }
}

# ---------------- HTTP 服务（TcpListener：不依赖 http.sys / URL ACL） ----------------
function Send-Bytes($stream, [byte[]]$Bytes, [string]$ContentType, [int]$Code = 200) {
    $head = "HTTP/1.1 $Code OK`r`nContent-Type: $ContentType`r`nContent-Length: $($Bytes.Length)`r`nCache-Control: no-store`r`nConnection: close`r`nAccess-Control-Allow-Origin: *`r`n`r`n"
    $hb = [Text.Encoding]::ASCII.GetBytes($head)
    $stream.Write($hb, 0, $hb.Length)
    $stream.Write($Bytes, 0, $Bytes.Length)
    $stream.Flush()
}
function Send-Json($stream, $Obj, [int]$Code = 200) {
    $json = $Obj | ConvertTo-Json -Depth 14 -Compress
    Send-Bytes $stream ([Text.Encoding]::UTF8.GetBytes($json)) 'application/json; charset=utf-8' $Code
}
function Send-Static($stream, [string]$Rel) {
    $p = Join-Path $script:Web $Rel
    $p = [IO.Path]::GetFullPath($p)
    if (-not $p.StartsWith([IO.Path]::GetFullPath($script:Web), [StringComparison]::OrdinalIgnoreCase)) { Send-Json $stream @{ error = 'forbidden' } 403; return }
    if (-not (Test-Path -LiteralPath $p)) { Send-Json $stream @{ error = 'not found' } 404; return }
    $ext = [IO.Path]::GetExtension($p).ToLower()
    $ct = @{ '.html' = 'text/html; charset=utf-8'; '.js' = 'application/javascript; charset=utf-8'; '.css' = 'text/css; charset=utf-8'; '.png' = 'image/png'; '.ico' = 'image/x-icon'; '.svg' = 'image/svg+xml'; '.json' = 'application/json' }[$ext]
    if (-not $ct) { $ct = 'application/octet-stream' }
    Send-Bytes $stream ([IO.File]::ReadAllBytes($p)) $ct 200
}
function Parse-Query([string]$q) {
    $h = @{}
    if (-not $q) { return $h }
    $q = $q.TrimStart('?')
    foreach ($kv in $q.Split('&')) {
        if (-not $kv) { continue }
        $parts = $kv.Split('=', 2)
        $k = [uri]::UnescapeDataString($parts[0])
        $v = ''
        if ($parts.Count -gt 1) { $v = [uri]::UnescapeDataString($parts[1]) }
        $h[$k] = $v
    }
    return $h
}

$listener = New-Object System.Net.Sockets.TcpListener([System.Net.IPAddress]::Loopback, $Port)
try { $listener.Start() } catch {
    Write-Host ('启动失败: ' + $_.Exception.Message) -ForegroundColor Red
    Write-Host '端口可能被占用，可用 -Port 换一个端口。' -ForegroundColor Yellow
    Read-Host '按回车退出' | Out-Null
    exit 1
}
$prefix = "http://127.0.0.1:$Port/"
Write-Host ('=' * 62) -ForegroundColor DarkCyan
Write-Host '  AppNuke 3D 后端已启动（TcpListener）' -ForegroundColor Cyan
Write-Host ('  ' + $prefix) -ForegroundColor White
Write-Host ('  用户目录: ' + $script:Prof)
Write-Host '  关闭应用窗口即停止服务。' -ForegroundColor DarkGray
Write-Host ('=' * 62) -ForegroundColor DarkCyan
if (-not $NoBrowser) { Start-Process $prefix }

while ($true) {
    $client = $null
    try {
        $client = $listener.AcceptTcpClient()
        $client.ReceiveTimeout = 15000
        $stream = $client.GetStream()
        $reader = New-Object System.IO.StreamReader($stream, [Text.Encoding]::UTF8)
        $reqLine = $reader.ReadLine()
        if (-not $reqLine) { $client.Close(); continue }
        $parts = $reqLine.Split(' ')
        $rawUrl = $parts[1]
        $contentLength = 0
        while ($true) {
            $line = $reader.ReadLine()
            if ($null -eq $line -or $line -eq '') { break }
            $idx = $line.IndexOf(':')
            if ($idx -gt 0) {
                $hn = $line.Substring(0, $idx).Trim().ToLower()
                $hv = $line.Substring($idx + 1).Trim()
                if ($hn -eq 'content-length') { $contentLength = [int]$hv }
            }
        }
        $bodyText = ''
        if ($contentLength -gt 0) {
            $buf = New-Object char[] $contentLength
            $reader.ReadBlock($buf, 0, $contentLength) | Out-Null
            $bodyText = -join $buf
        }
        $bodyObj = $null
        if ($bodyText) { try { $bodyObj = $bodyText | ConvertFrom-Json } catch {} }

        $u = [uri]('http://localhost' + $rawUrl)
        $path = $u.AbsolutePath
        $qs = Parse-Query $u.Query
        if ($path -eq '/' -or $path -eq '') { Send-Static $stream 'index.html'; $client.Close(); continue }
        if ($path -like '/api/*') {
            switch ($path) {
                '/api/apps' {
                    $apps = @(Get-Apps | ForEach-Object { @{ name = $_.name; version = $_.version; publisher = $_.publisher; location = $_.location; exe = $_.exe; scope = $_.scope; size = $_.size } })
                    Send-Json $stream @{ apps = $apps }
                }
                '/api/scan' {
                    $kw = [string]$qs['kw']
                    $mp = [string]$qs['path']
                    Send-Json $stream @{ tree = (Build-Tree $kw $mp); prof = $script:Prof }
                }
                '/api/downloads' { Send-Json $stream @{ items = @(Get-Downloads ([string]$qs['kw'])) } }
                '/api/online' { $kw = [string]$qs['kw']; if (-not $kw) { $kw = '应用' }; Send-Json $stream (Get-OnlineInfo $kw) }
                '/api/delete' {
                    $items = @()
                    if ($bodyObj -and $bodyObj.items) { $items = @($bodyObj.items) }
                    Send-Json $stream (Invoke-DeleteItems $items)
                }
                '/api/verify' {
                    $paths = @()
                    if ($qs['paths']) { $paths = @(([string]$qs['paths']).Split('|')) }
                    Send-Json $stream (Get-Verify ([string]$qs['kw']) $paths)
                }
                '/api/ping' { Send-Json $stream @{ ok = $true; prof = $script:Prof; pid = $PID } }
                '/api/shared' { Send-Json $stream (Get-SharedDeps ([string]$qs['kw'])) }
                '/api/residue' { Send-Json $stream (Get-Residue ([string]$qs['kw'])) }
                default { Send-Json $stream @{ error = 'unknown api' } 404 }
            }
            $client.Close()
            continue
        }
        Send-Static $stream ($path.TrimStart('/'))
        $client.Close()
    } catch {
        try { if ($client) { $client.Close() } } catch {}
    }
}
