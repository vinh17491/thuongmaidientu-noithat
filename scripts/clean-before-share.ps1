$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Get-ChildItem $root -Directory -Recurse -Force |
    Where-Object { $_.Name -in @("bin", "obj", ".vs") } |
    Sort-Object FullName -Descending |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Get-ChildItem $root -File -Recurse -Force |
    Where-Object { $_.Extension -in @(".user", ".log", ".bak") } |
    Remove-Item -Force -ErrorAction SilentlyContinue

Write-Host "Đã xóa file build, IDE, log và backup cục bộ."
