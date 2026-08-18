# Firmware events

The app subscribes to `HID_EVENT20` in `\\.\root\WMI`. Only events with `EventDetail[0] == 1` reach the UI.

| Name | Feature | Value |
|---:|---|---|
| 4 | Airplane mode | 0 off; any other value on |
| 5 | Keyboard backlight | 0 off, 1 low, 2 medium, 3 high, 128 automatic |
| 6 | Touchpad | 0 off; any other value on |
| 7 | Fn Lock | 0 off; any other value on |
| 10 | Ambient-light sensor | 0 off; any other value on |
| 15 | Performance mode | 0 high performance, 1 balanced, 2 quiet |
| 25 | Refresh rate | Values >= 30 display as `N Hz` |
| 33 | Windows-key lock | 0 off; any other value on |

Names 26 and 32 are parsed as two-byte values but currently have no UI mapping. Unknown names are ignored safely.

These mappings were tested only on the development **Mechrevo WUJIE 14 Pro**. Treat all other models and firmware versions as unverified.
