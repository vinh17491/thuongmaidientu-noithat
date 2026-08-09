$ErrorActionPreference = "Stop"

$solution = Join-Path $PSScriptRoot "ThuongMaiDienTu.slnx"
$project = Join-Path $PSScriptRoot "src\ThuongMaiDienTu\ThuongMaiDienTu.csproj"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET SDK chưa được cài hoặc chưa có trong PATH. Dự án yêu cầu .NET 8 SDK."
}

Write-Host "Restoring packages..."
dotnet restore $solution

Write-Host "Starting application at http://localhost:5205 ..."
dotnet run --project $project --launch-profile http
