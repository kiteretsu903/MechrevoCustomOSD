# Mechrevo Custom OSD v1.0.0

First public release.

## Highlights

- Modern WinUI 3 Desktop Acrylic OSD that stays above ordinary application windows without taking focus
- Firmware-event support for performance mode, keyboard backlight, touchpad, Fn Lock, Windows-key lock, airplane mode, ambient-light sensor, and refresh rate
- Automatic Chinese/English UI and a language selector in the tray menu
- Monochrome Fluent Gauge tray icon
- One-click elevated installer plus a portable inspection package
- Standard-user installed runtime (`RunLevel Limited`) with no runtime network access

## Compatibility

Tested only on a **Mechrevo WUJIE 14 Pro running Windows 11 x64**. Other Mechrevo models, Windows 10, and different BIOS/EC versions are untested. Firmware event IDs may differ.

## Installer behavior

The installer saves the current state of `BLDHotKeyService`, disables that OEM service to prevent the official and custom OSDs from conflicting, and creates a standard-user logon task. It never deletes OEM files. Run the installer again and select uninstall to restore the recorded OEM service state.

The installer is unsigned. Windows may show an Unknown publisher warning; verify `SHA256SUMS.txt` before running it.

---

# 机械革命自定义 OSD v1.0.0

首个公开版本。

- 只在 **机械革命 无界14 Pro、Windows 11 x64** 上测试过
- 提供 WinUI 3 亚克力置顶 OSD、中英双语和纯白 Fluent 托盘图标
- Release 包含一键安装器与便携检查包
- 安装器会保存并停用 `BLDHotKeyService`，避免官方与自定义 OSD 冲突，但不删除官方文件；卸载时恢复记录的服务状态
- 常驻 OSD 使用普通用户权限，运行时不访问网络

安装器当前没有代码签名，运行前请核对 `SHA256SUMS.txt`。再次运行安装器并选择卸载，即可恢复安装时记录的官方服务状态。
