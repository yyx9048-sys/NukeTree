param([string]$Path)
if (-not (Test-Path -LiteralPath $Path)) { Write-Output '路径不存在'; exit }
$i = Get-Item -LiteralPath $Path
Write-Output ('名称: ' + $i.Name)
Write-Output ('大小: ' + [math]::Round($i.Length / 1MB, 3) + ' MB (' + $i.Length + ' 字节)')
Write-Output ('修改: ' + $i.LastWriteTime)
if (-not $i.PSIsContainer) {
  $h = Get-FileHash -LiteralPath $Path -Algorithm SHA256
  Write-Output ('SHA256: ' + $h.Hash)
}
