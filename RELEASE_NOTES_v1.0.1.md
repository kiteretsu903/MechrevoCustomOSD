# Mechrevo Custom OSD v1.0.1

Patch release for tray-menu responsiveness.

## Fixed

- Moved the notification icon callback window and native context menu to a dedicated STA/message-pump thread.
- Firmware-event cards continue rendering while the tray menu is open.
- Duplicate tray callbacks are suppressed, preventing nested or repeated menus after one interaction.
- Rerunning the one-click installer still offers update/repair or uninstall and preserves the recorded OEM service state.

## Validation

- With the tray menu deliberately held open, a preview card reached the WinUI callback in 7.5 ms.
- Eleven simulated tray callbacks produced one menu instance.
- The process remained responsive throughout the regression tests.

## Compatibility

Tested only on a **Mechrevo WUJIE 14 Pro running Windows 11 x64**. Other models and firmware mappings remain untested.

The installer is unsigned. Verify `SHA256SUMS.txt` before running it.

---

# 机械革命自定义 OSD v1.0.1

托盘菜单响应修复版本。

## 修复

- 将托盘回调窗口和原生菜单移至独立 STA/消息循环线程。
- 托盘菜单保持打开时，固件事件卡片仍能正常显示。
- 抑制重复托盘回调，避免一次操作产生嵌套或连续菜单。
- 再次运行一键安装器仍可选择更新/修复或卸载，并保留官方服务的回滚状态。

## 验证

- 托盘菜单保持打开时，测试卡片在 7.5 毫秒内进入 WinUI 回调。
- 连续模拟 11 个托盘回调时只产生一个菜单实例。
- 回归测试期间进程始终保持响应。

## 兼容性

目前只在 **机械革命 无界14 Pro、Windows 11 x64** 上测试。其他型号和固件映射尚未验证。

安装器当前没有代码签名，运行前请核对 `SHA256SUMS.txt`。
