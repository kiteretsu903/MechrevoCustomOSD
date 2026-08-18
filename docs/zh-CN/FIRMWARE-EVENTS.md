# 固件事件映射

监听范围：`\\.\root\WMI` 中的 `HID_EVENT20`。只有 `EventDetail[0] == 1` 的热键事件进入 UI；其他类型只写入日志。

`EventDetail` 当前按以下方式解析：

- `Type = detail[0]`
- `Name = detail[1]`
- `Value = detail[2]`
- `Name` 为 26 或 32 时，`Value = detail[2] << 8 | detail[3]`

## 当前显示映射

| Name | 功能 | Value 解释 |
|---:|---|---|
| 4 | 飞行模式 | 0 关闭，其他值开启 |
| 5 | 键盘背光 | 0 关闭、1 低、2 中、3 高、128 自动；其他值原样显示 |
| 6 | 触控板 | 0 关闭，其他值开启 |
| 7 | Fn Lock | 0 关闭，其他值开启 |
| 10 | 环境光感应 | 0 关闭，其他值开启 |
| 15 | 性能模式 | 0 高性能、1 平衡、2 静音；其他值显示未知模式 |
| 25 | 刷新率 | 大于等于 30 时显示 `N Hz`，否则显示模式编号 |
| 33 | Windows 键锁定 | 0 关闭，其他值开启 |

`Name` 26 和 32 虽然支持双字节解析，目前没有 UI 映射。未知 `Name` 会被安全忽略。

亮度、Caps Lock、Num Lock 和 Scroll Lock 有意不实现，继续使用 Windows 11 自带提示。

## 兼容性

这些映射只在开发用机械革命无界14 Pro、Windows 11 x64 上测试过。更换型号、主板、BIOS 或 EC 后，应先检查 `%LOCALAPPDATA%\MechrevoCustomOSD\MechrevoCustomOSD.log` 中的原始字节，再修改 `OSDWindow.xaml.cs` 的映射。
