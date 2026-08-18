# 运行、安装与回滚

## 便携预览

在发布包根目录运行：

```console
.\app\MechrevoCustomOSD.exe --demo
```

退出：

```console
.\app\MechrevoCustomOSD.exe --stop
```

便携运行不会改服务、计划任务或 ProgramData。

## 托盘菜单

- 自动：跟随 Windows 用户 UI 语言；中文 UI 使用简体中文，其他语言使用英文
- 简体中文
- English
- 显示测试
- 退出

设置与日志目录：

`%LOCALAPPDATA%\MechrevoCustomOSD`

## 安装

一键安装器会记录并停用官方 `BLDHotKeyService`，避免官方与自定义 OSD 同时运行造成冲突，但不会删除官方文件；随后复制发布文件到 `%ProgramData%\MechrevoCustomOSD`，并创建登录时启动的普通权限计划任务。

安装脚本会请求一次管理员权限；常驻程序仍以计划任务的 `RunLevel Limited` 运行。

## 状态和测试

通过右下角托盘菜单可显示测试卡片、切换语言或退出。

## 卸载与回滚

再次运行同一个一键安装器并选择卸载。卸载会删除自定义计划任务和固定 ProgramData 安装目录，并按首次安装时保存的状态恢复官方服务。官方程序文件从不删除。LocalAppData 中的语言设置与日志默认保留，可由用户手动清理。
