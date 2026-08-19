# 机械革命自定义 OSD

[English](README.md)

面向 **机械革命 无界14 Pro** 的现代 WinUI 3 屏幕提示程序。

![机械革命自定义 OSD 卡片](docs/images/demo-2x.png)

> 兼容性状态：目前只在机械革命 无界14 Pro、Windows 11 x64 上测试通过。尚未测试 Windows 10、其他机械革命型号或其他 BIOS/EC 版本。

## 开发动机

机械革命官方 OSD 有时会被所有普通应用窗口遮挡，只在桌面上可见。重新安装官方工具可能暂时恢复，但问题仍可能再次出现；通用的“窗口置顶”工具也不适合这种短暂出现的 OSD 窗口。

本项目只替换显示层：直接监听笔记本固件提供的 `root\WMI:HID_EVENT20` 事件，用 WinUI 3 Desktop Acrylic 绘制不抢焦点、鼠标穿透的 `HWND_TOPMOST` 提示卡片。

## 功能

- WinUI 3 Desktop Acrylic；不支持 Acrylic 时自动使用回退颜色
- OSD 卡片使用 Fluent Emoji 3D 图标
- 透明背景、纯白 Fluent Gauge 托盘图标，包含 16–256 px 原生尺寸
- 根据 Windows UI 语言自动切换中文/英文，也可在托盘菜单手动选择
- 托盘菜单使用独立线程，保持打开时不会阻塞固件事件卡片
- 由 WinUI 管理始终置顶状态，可覆盖包括 Codex 在内的普通应用窗口
- 性能模式、键盘背光、触控板、Fn Lock、Windows 键锁定、飞行模式、环境光感应和刷新率提示
- 常驻程序使用普通用户权限，不请求管理员权限或 UIAccess
- 运行时不访问网络

亮度以及 Caps/Num/Scroll Lock 提示有意不实现，继续使用 Windows 11 自带提示。

## 安装 v1.0.2

从 [v1.0.2 Release](../../releases/tag/v1.0.2) 下载 `MechrevoCustomOSD-v1.0.2-OneClickInstaller.exe`，核对 SHA256 后运行。安装器会请求一次管理员权限，保存并停用官方 `BLDHotKeyService`，避免官方与自定义 OSD 同时运行造成冲突，但不删除任何官方文件；安装后的 OSD 登录任务明确使用 `RunLevel Limited`。

当前安装器没有代码签名，因此 Microsoft Defender SmartScreen 可能显示“未知发布者”。不要全局关闭 SmartScreen，请先核对 Release 校验值。

需要卸载时，再次运行同一个一键安装器并选择卸载；程序会恢复安装前记录的官方服务状态。

Release 还提供便携 ZIP，便于检查文件或手动测试。

## 从源码构建

需要 Windows 11 x64 和 .NET 8 SDK x64。

```console
dotnet restore .\winui3-osd\MechrevoCustomOSD.csproj -p:Platform=x64 --locked-mode
dotnet publish .\winui3-osd\MechrevoCustomOSD.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained false --no-restore -o .\dist\manual\app
```

中文详细文档位于 [docs/zh-CN](docs/zh-CN/)，英文文档位于 [docs](docs/)。

## 机型相关警告

`HID_EVENT20` 的数值与固件相关。本仓库中的映射只在开发用机械革命 无界14 Pro 上验证过。其他型号必须先记录和检查原始事件日志，再决定是否修改映射或停用官方工具。

## 第三方资源

OSD 的 3D 图标来自微软 `fluentui-emoji`，托盘 Gauge 图形来自微软 `fluentui-system-icons`；对应 MIT 许可文本随资源提供。详见 [第三方说明](THIRD-PARTY-NOTICES.md)。

项目源码采用 [MIT License](LICENSE) 发布；第三方资源继续适用随附的各自 MIT 许可文本。
