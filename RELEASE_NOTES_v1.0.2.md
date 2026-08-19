# Mechrevo Custom OSD v1.0.2

Patch release for OSD window ordering.

## Fixed

- Keeps the visible WinUI OSD card above ordinary application windows, including Codex.
- Applies the supported WinUI always-on-top presenter state after window activation.
- Reasserts that state before each notification while preserving no-activation and click-through behavior.

## Validation

- The OSD retained its topmost extended style across repeated samples.
- In a live Codex-window regression, the OSD ranked above the Codex window in the desktop Z order.
- The installed v1.0.0 instance was restored after isolated testing; this release was not installed automatically.

## Compatibility

Tested only on a **Mechrevo WUJIE 14 Pro running Windows 11 x64**. Other models and firmware mappings remain untested.

The installer is unsigned. Verify `SHA256SUMS.txt` before running it.

---

# 机械革命自定义 OSD v1.0.2

OSD 窗口层级修复版本。

## 修复

- 让可见的 WinUI OSD 卡片保持在包括 Codex 在内的普通应用窗口之上。
- 在窗口激活完成后设置 WinUI 官方的始终置顶 presenter 状态。
- 每次显示通知前复核该状态，同时保留不抢焦点和鼠标穿透行为。

## 验证

- 多次采样中 OSD 始终保留置顶扩展样式。
- 在 Codex 窗口的实机回归测试中，OSD 的桌面 Z 顺序位于 Codex 之上。
- 隔离测试结束后已恢复当前安装的 v1.0.0；本版本未自动安装。

## 兼容性

目前只在 **机械革命 无界14 Pro、Windows 11 x64** 上测试。其他型号和固件映射尚未验证。

安装器当前没有代码签名，运行前请核对 `SHA256SUMS.txt`。
