# Third-party notices / 第三方说明

This repository does not commit NuGet or npm binary caches. Build tools restore dependencies from their normal package sources.

本仓库不提交 NuGet 或 npm 二进制缓存；构建时由正常的软件源恢复依赖。

The project source is licensed under the repository-level `LICENSE`. Third-party materials listed below retain their own license notices.

## Microsoft Fluent Emoji

The 3D artwork used in OSD cards comes from Microsoft `fluentui-emoji`. Its MIT license text is included at:

`winui3-osd/Assets/FluentEmoji3D/LICENSE.txt`

https://github.com/microsoft/fluentui-emoji

## Microsoft Fluent UI System Icons

The tray Gauge vector comes from Microsoft `fluentui-system-icons`. Original SVG files and the MIT license are included under:

`winui3-osd/Assets/FluentSystemIcons/`

https://github.com/microsoft/fluentui-system-icons

## NuGet dependencies

- `Microsoft.WindowsAppSDK` `1.8.260710003`
- `System.Management` `8.0.0`

Exact transitive dependencies are recorded in `winui3-osd/packages.lock.json` and remain subject to their publishers' licenses.

## Icon build dependency

`icon-tools` declares `sharp` `0.35.3` to render SVG input into the multi-resolution ICO. The npm package itself is not committed.
