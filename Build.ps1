[CmdletBinding()]
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist\MechrevoCustomOSD-v1.0.0')
)

$ErrorActionPreference = 'Stop'

$dotnet = Get-Command dotnet -ErrorAction Stop
$project = Join-Path $PSScriptRoot 'winui3-osd\MechrevoCustomOSD.csproj'
$output = [IO.Path]::GetFullPath($OutputDirectory)
$appOutput = Join-Path $output 'app'

if (Test-Path -LiteralPath $output) {
    throw "输出目录已经存在，为避免覆盖请删除它或通过 -OutputDirectory 指定新目录：$output"
}

New-Item -ItemType Directory -Path $appOutput | Out-Null

& $dotnet.Source restore $project -p:Platform=x64 --locked-mode
if ($LASTEXITCODE -ne 0) { throw 'NuGet 锁定还原失败。' }

& $dotnet.Source publish $project `
    -c Release `
    -p:Platform=x64 `
    -r win-x64 `
    --self-contained false `
    --no-restore `
    -o $appOutput
if ($LASTEXITCODE -ne 0) { throw '发布失败。' }

Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Setup-MechrevoCustomOSD.ps1') -Destination $output
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination $output
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'THIRD-PARTY-NOTICES.md') -Destination $output
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs') -Destination $output -Recurse

$executable = Join-Path $appOutput 'MechrevoCustomOSD.exe'
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
    throw '发布完成但未找到目标 EXE。'
}

$hash = Get-FileHash -Algorithm SHA256 -LiteralPath $executable
Write-Host "发布完成：$output"
Write-Host "EXE SHA256：$($hash.Hash)"
