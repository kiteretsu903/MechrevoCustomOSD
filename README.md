# Mechrevo Custom OSD

[简体中文](README.zh-CN.md)

A modern, non-activating WinUI 3 on-screen display for the **Mechrevo WUJIE 14 Pro** laptop.

![Mechrevo Custom OSD card](docs/images/demo-2x.png)

> Compatibility status: tested only on a Mechrevo WUJIE 14 Pro running Windows 11 x64. Windows 10, other Mechrevo models, and other BIOS/EC versions have not been tested.

## Motivation

The OEM Mechrevo OSD can sometimes appear behind every normal application window, making it visible only on the desktop. Reinstalling the OEM utility may fix it temporarily, but the problem can return. Generic always-on-top tools are not a reliable fit for a short-lived OSD window.

This project replaces only the display layer: it listens to the laptop firmware's `root\WMI:HID_EVENT20` events and renders its own click-through, non-focus-stealing `HWND_TOPMOST` notification with WinUI 3 Desktop Acrylic.

## Features

- WinUI 3 Desktop Acrylic with a fallback color when Acrylic is unavailable
- Fluent Emoji 3D artwork inside OSD cards
- A crisp monochrome Fluent Gauge tray icon with native 16–256 px frames
- Automatic Chinese/English UI selection, plus a manual tray-menu override
- A dedicated tray thread so an open tray menu cannot block firmware-event cards
- Performance mode, keyboard backlight, touchpad, Fn Lock, Windows-key lock, airplane mode, ambient-light sensor, and refresh-rate notifications
- Standard-user runtime; the OSD does not request elevation or UIAccess
- No runtime network access

Brightness and Caps/Num/Scroll Lock notifications are intentionally omitted because Windows 11 already provides them.

## Install v1.0.1

Download `MechrevoCustomOSD-v1.0.1-OneClickInstaller.exe` from the [v1.0.1 release](../../releases/tag/v1.0.1), verify its SHA256 value, and run it. The installer requests administrator permission once to save and disable the OEM `BLDHotKeyService`, preventing the official and custom OSDs from conflicting without deleting OEM files. The installed OSD itself starts at sign-in with `RunLevel Limited`.

The release executable is currently unsigned, so Microsoft Defender SmartScreen may show an **Unknown publisher** warning. Do not disable SmartScreen globally; verify the release checksum before running it.

To uninstall, run the same one-click installer again and select the uninstall option. It restores the previously recorded OEM service state.

The release also includes a portable ZIP for inspection and manual testing.

## Build from source

Requirements: Windows 11 x64 and the .NET 8 SDK x64.

```console
dotnet restore .\winui3-osd\MechrevoCustomOSD.csproj -p:Platform=x64 --locked-mode
dotnet publish .\winui3-osd\MechrevoCustomOSD.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained false --no-restore -o .\dist\manual\app
```

See [Build](docs/BUILD.md), [Architecture](docs/ARCHITECTURE.md), [Firmware events](docs/FIRMWARE-EVENTS.md), [Usage](docs/USAGE.md), and [Security](docs/SECURITY.md).

## Model-specific warning

`HID_EVENT20` values are firmware-specific. The mappings in this repository were verified only on the Mechrevo WUJIE 14 Pro used for development. On another model, capture and inspect the raw event log before changing or disabling the OEM utility.

## Third-party assets

The 3D OSD artwork comes from Microsoft's `fluentui-emoji`; the tray Gauge glyph comes from Microsoft's `fluentui-system-icons`. Their MIT license texts are included with the assets. See [third-party notices](THIRD-PARTY-NOTICES.md).

The project source is released under the [MIT License](LICENSE). Included third-party assets remain subject to their respective MIT license texts.
