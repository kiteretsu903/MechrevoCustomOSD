# 托盘图标生成工具

生成器读取 `..\winui3-osd\Assets\FluentSystemIcons\` 中微软提供的 16/20/24/32 px Gauge SVG，为每个目标尺寸独立渲染纯白透明图标，并输出包含 16、20、24、32、40、48、64、128、256 px 九帧的 `osd.ico`。

需要 Node.js 20 或更高版本。在本目录运行：

```console
npm install
npm run generate
```

生成结果：

- `..\winui3-osd\Assets\osd.ico`
- `modern-tray-icon-preview.png`

OSD 卡片内部的 Fluent Emoji 3D 图标不会被这个工具修改。
