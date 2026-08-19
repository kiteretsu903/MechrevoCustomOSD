# Build

## Requirements

- Windows 11 x64
- .NET 8 SDK x64
- Internet access for the first locked NuGet restore

The app targets `net8.0-windows10.0.19041.0` and x64. Windows App SDK components are deployed self-contained. The ordinary `Build.ps1` output is .NET framework-dependent; the release script publishes a fully self-contained .NET payload.

## Development build

```console
dotnet restore .\winui3-osd\MechrevoCustomOSD.csproj -p:Platform=x64 --locked-mode
dotnet publish .\winui3-osd\MechrevoCustomOSD.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained false --no-restore -o .\dist\MechrevoCustomOSD-v1.0.2\app
```
The example output is `dist\MechrevoCustomOSD-v1.0.2`. The included build script also refuses to overwrite an existing output directory.

## Release assets

The included release-build script creates a self-contained portable ZIP, a self-contained one-click installer, bilingual release notes, and `SHA256SUMS.txt` under `release-output`.

## Test without installing

```console
.\dist\MechrevoCustomOSD-v1.0.2\app\MechrevoCustomOSD.exe --demo
.\dist\MechrevoCustomOSD-v1.0.2\app\MechrevoCustomOSD.exe --stop
```
