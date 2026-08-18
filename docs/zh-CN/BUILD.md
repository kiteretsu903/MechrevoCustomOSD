# 构建说明

## 环境

- Windows 11 x64
- .NET 8 SDK x64
- 首次 NuGet 还原时可访问互联网

项目目标框架为 `net8.0-windows10.0.19041.0`，目标平台为 x64。Windows App SDK 组件按 self-contained 模式随发布目录输出；.NET 运行时本身使用 framework-dependent 模式，因此目标电脑仍需 .NET 8 Desktop Runtime x64。

## 推荐构建

在源码包根目录运行：

```console
dotnet restore .\winui3-osd\MechrevoCustomOSD.csproj -p:Platform=x64 --locked-mode
dotnet publish .\winui3-osd\MechrevoCustomOSD.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained false --no-restore -o .\dist\MechrevoCustomOSD-v1.0.0\app
```

先以 `--locked-mode` 校验 `packages.lock.json`，然后执行 Release/x64 发布。仓库中的构建脚本会在目标目录已经存在时停止，以避免误覆盖。

## 手动构建

```console
dotnet restore .\winui3-osd\MechrevoCustomOSD.csproj -p:Platform=x64 --locked-mode
dotnet publish .\winui3-osd\MechrevoCustomOSD.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained false --no-restore -o .\dist\manual\app
```

## 验证

```console
.\dist\MechrevoCustomOSD-v1.0.0\app\MechrevoCustomOSD.exe --demo
.\dist\MechrevoCustomOSD-v1.0.0\app\MechrevoCustomOSD.exe --stop
```

`--demo` 会复用已经运行的单实例并请求显示测试卡片；`--stop` 通过当前用户会话中的命名事件请求正常退出。

## 不打包的内容

源码包不包含 `.NET SDK`、NuGet 缓存、npm 缓存、`bin`、`obj` 或任何已编译 EXE。这样可以避免把本机路径、构建缓存和无关二进制带入源码归档。
