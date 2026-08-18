# 权限与安全边界

## 常驻程序

- manifest 明确声明 `requestedExecutionLevel="asInvoker"`、`uiAccess="false"`
- 不请求管理员权限，不绕过 UIPI，不注入其他进程
- WMI 只订阅 `root\WMI:HID_EVENT20` 事件，不写 WMI、BIOS 或 EC
- 运行时业务代码不访问网络，不下载或执行外部内容
- 托盘菜单只接受 1001–1005 五个固定命令，不解析命令行或任意 shell 文本
- 未识别的固件事件被忽略，不参与路径、进程或命令构造

## 本地数据

- 设置只允许 `auto`、`zh`、`en`，读取大小上限 4096 字节
- 日志单份上限约 2 MB，只保留当前和上一份
- 数据仅写入当前用户的 `%LOCALAPPDATA%\MechrevoCustomOSD`
- 原始固件事件会进入日志；它们是热键字节，不应包含密码或文档内容

## 命名对象

`--demo` 和 `--stop` 使用当前登录会话的 `Local\` 命名事件。相同用户会话中的其他进程可以触发预览或退出，最坏结果是本地拒绝服务或弹出测试 OSD；该通道不能传入文本、路径或可执行内容。

## 安装脚本

管理员权限只用于以下操作：

- 保存、停用和恢复固定名称的官方服务 `BLDHotKeyService`
- 写入固定目录 `%ProgramData%\MechrevoCustomOSD`
- 创建或删除固定名称的计划任务 `Mechrevo Custom OSD`

常驻任务显式使用 `RunLevel Limited`。递归删除前，脚本把路径标准化并与预期的 ProgramData 绝对路径做精确比较。官方服务和程序文件不被删除。

## 发布前检查建议

1. 保持 NuGet 锁定还原，审阅依赖升级差异。
2. 对最终 EXE 和 ZIP 计算 SHA256；若对外分发，使用可信代码签名证书签名。
3. 在标准用户账户测试 `--demo`、托盘菜单、WMI 监听和退出。
4. 在目标机型验证固件事件映射，再停用官方 OSD。
5. 不要把日志、用户设置、ProgramData 状态快照或个人签名证书加入源码仓库。
