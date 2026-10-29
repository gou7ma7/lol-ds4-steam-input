# Application icon / 应用图标

Original generic controller and moon artwork, covered by this module's MIT license. 原创通用手柄与月亮图形，使用本模块的 MIT 许可证。

- `app.svg`: editable vector source / 可编辑矢量源文件。
- `app.ico`: transparent Windows icon with 16, 20, 24, 32, 40, 48, 64, 128 and 256 px images / 透明背景、多尺寸 Windows 图标。
- The build embeds `app.ico` in the executable; the tray uses that embedded icon. No external icon file is needed when running. 编译时嵌入图标，托盘读取程序内的图标；运行时无需外部图标文件。
- After editing the SVG, export the listed sizes to an ICO file with a graphics editor, replace `app.ico`, then rebuild. 修改 SVG 后，用图形编辑器导出上述尺寸的 ICO，替换后重新编译。
