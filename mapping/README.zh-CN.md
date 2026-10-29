# 左手 DS4 + 右手鼠标按键映射

简体中文 · [English](README.md)

本模块可独立使用，包含 Steam 原生布局和把手柄输出转换成英雄技能的游戏 WASD 设置，不需要振动程序。

> [!IMPORTANT]
> **必做：以管理员身份启动 Steam，再使用这套 LoL 映射。** 先完全退出原有 Steam 进程，再右键 Steam → **以管理员身份运行**，确认 Windows 提示。普通权限的 Steam 可能在桌面上正常应用布局，却没有权限控制英雄联盟。详见 [启动与排查步骤](docs/setup.zh-CN.md)。此要求仅针对 Steam 映射，独立反馈工具不需要管理员权限。

## 核心操作

- 左摇杆：WASD 移动。
- L2：Q 技能，Steam 实际输出 E。
- L1：W 技能，Steam 实际输出左 Shift。
- 鼠标右键：E 技能，在游戏中直接设置。
- L3：R 技能，Steam 实际输出 R。

手柄只使用左半边，右手用真实鼠标瞄准、交互和执行额外操作。详见 [完整设置](docs/setup.zh-CN.md)。

## 手动应用

1. 完全退出 Steam，再右键选择 **以管理员身份运行**。
2. 连接 DS4，开启 Steam Input 支持。
3. 在 Steam 打开 `steam://controllerconfig/413080/3827898683`，预览后应用到桌面布局，名称为「英雄联盟 LOL WASD和鼠标」。
4. 在游戏 WASD 模式中设置 Q→E、W→左 Shift、E→鼠标右键、R→R。
5. 先在训练模式验证移动和技能，再进入正常游戏。

413080 是 Steam 桌面布局标识，不是英雄联盟 AppID。需要时先导出原桌面布局，方便恢复。分享链接已由本机导出和加载记录确认，其他账号访问尚未验证。链接不可用时可按设置指南重建核心操作；下载或双击 VDF 不等于自动导入。

## 英雄和交互备注

持续施放 R 时，可用方向键下，避免按 L3 时推动摇杆中断大招。原布局这里输出 **3**，需把对应英雄的 R 设到 3。提莫、卡牌等被动 E 英雄，或高频使用 Q/W 的英雄，可以单独调整鼠标右键。冰霜传送门及选择落点等交互，在使用者的这套设置中需要 **Ctrl + 左键**。这些是手动使用备注，见 [详细说明](docs/usage-notes.zh-CN.md)。

## 文件与 AI 入口

- [原生 VDF](steam-input/lol-left-hand-ds4.vdf)、[格式和来源](steam-input/README.zh-CN.md)、[元数据](steam-input/metadata.json)。
- [游戏键位参考 JSON](game-settings/wasd-keybinds.reference.json)：仅供参考，不可直接导入 Riot 配置。
- [AGENTS.md](AGENTS.md)、[中文 Prompt](prompts/agent.zh-CN.md) / [English Prompt](prompts/agent.en.md)。
- [MIT 许可证](LICENSE)。
- [相似映射方案](docs/related-work.zh-CN.md)。

布局保留旧绑定和未启用动作组，便于忠实备份；功能数据与导出文件一致，跨电脑导入尚未测试。
