[CmdletBinding()]
param(
    [string]$Version = '1.0.2',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\release-output')
)

$ErrorActionPreference = 'Stop'
if ($Version -ne '1.0.2') { throw 'This source tree is prepared for version 1.0.2.' }

$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = [IO.Path]::GetFullPath($OutputDirectory)
$payloadDirectory = Join-Path $repository 'installer\OneClickInstaller\Payload'
$staging = Join-Path $output '.staging'
$payloadRoot = Join-Path $staging 'payload'
$appOutput = Join-Path $payloadRoot 'app'
$installerOutput = Join-Path $staging 'installer'
$dotnet = Get-Command dotnet -ErrorAction Stop
$payloadZip = Join-Path $payloadDirectory 'payload.zip'

if (Test-Path -LiteralPath $output) { throw "Output directory already exists: $output" }
if (Test-Path -LiteralPath $payloadDirectory) {
    $unexpectedPayloadEntries = Get-ChildItem -LiteralPath $payloadDirectory -Force | Where-Object Name -ne 'payload.zip'
    if ($unexpectedPayloadEntries) { throw "Generated payload directory contains unexpected files: $payloadDirectory" }
    if (Test-Path -LiteralPath $payloadZip -PathType Leaf) { [IO.File]::Delete($payloadZip) }
} else {
    New-Item -ItemType Directory -Path $payloadDirectory | Out-Null
}

New-Item -ItemType Directory -Path $output,$appOutput,$installerOutput | Out-Null

$appProject = Join-Path $repository 'winui3-osd\MechrevoCustomOSD.csproj'
& $dotnet.Source restore $appProject -p:Platform=x64 --locked-mode
if ($LASTEXITCODE -ne 0) { throw 'Locked NuGet restore failed.' }

& $dotnet.Source publish $appProject `
    -c Release `
    -p:Platform=x64 `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -r win-x64 `
    --self-contained true `
    --no-restore `
    -o $appOutput
if ($LASTEXITCODE -ne 0) { throw 'Application publish failed.' }

Copy-Item -LiteralPath (Join-Path $repository 'Setup-MechrevoCustomOSD.ps1') -Destination $payloadRoot

Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($payloadRoot,$payloadZip,[IO.Compression.CompressionLevel]::Optimal,$false)
Copy-Item -LiteralPath $payloadZip -Destination (Join-Path $output "MechrevoCustomOSD-v$Version-Portable.zip")

$installerProject = Join-Path $repository 'installer\OneClickInstaller\OneClickInstaller.csproj'
& $dotnet.Source publish $installerProject -c Release -r win-x64 --self-contained true -o $installerOutput
if ($LASTEXITCODE -ne 0) { throw 'One-click installer publish failed.' }

$builtInstaller = Join-Path $installerOutput 'MechrevoCustomOSD.Installer.exe'
$releaseInstaller = Join-Path $output "MechrevoCustomOSD-v$Version-OneClickInstaller.exe"
Copy-Item -LiteralPath $builtInstaller -Destination $releaseInstaller

$releaseNotes = Join-Path $repository "RELEASE_NOTES_v$Version.md"
Copy-Item -LiteralPath $releaseNotes -Destination $output

$checksumLines = Get-ChildItem -LiteralPath $output -File | Sort-Object Name | ForEach-Object {
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash.ToLowerInvariant()
    "$hash *$($_.Name)"
}
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[IO.File]::WriteAllLines((Join-Path $output 'SHA256SUMS.txt'), [string[]]$checksumLines, $utf8NoBom)

Write-Host "Release assets created in $output"
Get-ChildItem -LiteralPath $output -File | Select-Object Name,Length
