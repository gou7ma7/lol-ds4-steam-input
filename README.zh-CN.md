# 英雄联盟：手柄按键映射、振动反馈与睡眠灭灯

简体中文 · [English](README.md)

> **强烈建议先检查源码，再自行编译程序。不要直接运行网上下载的 `.exe`。** 请优先使用下面的本地构建方式；预编译文件只能作为参考，不能代替源码检查。

仓库包含三个独立模块：

- **[mapping/ 按键映射](mapping/README.zh-CN.md)**：左手 DS4、右手鼠标，包含 Steam 原生 VDF、游戏 WASD 设置和英雄使用备注。不需要振动程序。
- **[rumble/ 振动反馈](rumble/README.zh-CN.md)**：读取英雄联盟本地比赛接口，为 USB DS4 提供振动。不依赖这套 Steam 布局，也不要求使用 WASD 模式或 Steam。
- **[sleep-light/ 睡眠灭灯](sleep-light/README.zh-CN.md)**：USB DS4 在睡眠/休眠前灭灯，用户恢复使用/解锁后恢复配置的蓝色，后台自动唤醒保持熄灭。不依赖 Steam 或游戏。

可以任选一个，也可以组合使用，模块不会安装或修改对方。

## 先检查，再编译

建议使用仓库自带脚本，在检查源码后本地编译。GitHub Actions 也会在 Windows 上运行相同的编译和离线检查：振动工具的四种预设各编译中英文，灭灯工具编译中英文，共 10 个构建版本。

成功运行后，可在 **Actions → Windows build and tests → 对应运行记录 → Artifacts** 找到程序、测试报告和构建清单，保留 7 天。这些仅供构建参考，仍建议检查源码后自行编译。工作流不会发布 Releases、安装或启动工具，也不会访问手柄。

请阅读 [运行源码](rumble/src/LoLDamageRumble.cs)、[详细配置](rumble/config/FeedbackConfig.cs)、[构建脚本](rumble/scripts/build.ps1) 和 [测试](rumble/tests/LoLRumbleTests.cs)。确认后，在 Windows PowerShell 中从仓库根目录运行：

```powershell
powershell.exe -NoProfile -File .\rumble\scripts\build.ps1
```

脚本先验证所有预设并运行模拟行为测试，再生成 `rumble/dist/LoLDamageRumble.exe`。构建过程不会启动用户程序，也不会给手柄发送指令。检查和编译完成后，运行自己编译的文件：

```powershell
.\rumble\dist\LoLDamageRumble.exe
```

需要 Windows x64 和 .NET Framework 4.x 的 C# 编译器，不下载依赖，不要求 API Key 或管理员权限。振动实现目前只支持 USB DS4 第二代（`054C:09CC`）。

## 独立编译睡眠灭灯工具

先阅读 [灭灯源码与配置说明](sleep-light/README.zh-CN.md)，再自行编译并运行：

```powershell
powershell.exe -NoProfile -File .\sleep-light\scripts\build.ps1
.\sleep-light\dist\DS4SleepLight.exe
```

构建运行离线检查，不发送 USB 指令。[当前用户登录自启动](sleep-light/README.zh-CN.md#可选登录自启动) 是独立、可撤销的步骤。工具仅更新灯色/闪烁字段，不改变马达输出或按键映射。[详细配置与验证](sleep-light/docs/configuration.zh-CN.md)。

## 定制振动

编辑 **[rumble/config/FeedbackConfig.cs](rumble/config/FeedbackConfig.cs)**，重新编译即可。每个参数都有中英文注释，配置会编入程序，不需要另带运行时配置文件。

- `default`：你的当前方案，65% 强度，大量掉血、低血量、低蓝量/能量、击杀、助攻、连杀全部启用。
- `gentle`：轻柔模式，45% 强度，重击和击杀更轻，关闭助攻提醒。
- `focus`：关键提醒，关闭击杀庆祝，提高大量掉血门槛，降低低血量和低资源提醒门槛。
- `celebration`：庆祝模式，仅击杀、助攻和连杀。

```powershell
powershell.exe -NoProfile -File .\rumble\scripts\build.ps1 -Preset focus -Language zh
```

默认 `-Language auto` 按 Windows 界面语言选择中文或英文；`zh`、`en` 可强制选择。预设在编译时选定，更多说明见 [详细配置指南](rumble/docs/configuration.zh-CN.md)。

## 目录与 Agent 入口

`mapping/` 自带布局、游戏参考设置、双语文档、`AGENTS.md` 和可复制 Prompt；`rumble/` 自带配置、源码、编译脚本、测试、双语文档和自己的 Agent 入口。`sleep-light/` 自带独立灭灯程序、配置、构建/自启动脚本、离线测试和双语 Agent 入口。三个文件夹均可单独使用。

AI 可先读取根目录 [AGENTS.md](AGENTS.md)，再进入对应模块。使用者也可以直接复制模块 `prompts/` 中的中英文任务模板。

[贡献说明](CONTRIBUTING.zh-CN.md) · [MIT 许可证](LICENSE)

## 验证与限制

灭灯离线检查覆盖后台唤醒与用户恢复、重复通知、锁屏/解锁及待恢复任务取消。原使用者已在 Steam、振动程序运行时肉眼确认三秒灭灯测试，以及真实 S3 睡眠 → 恢复/解锁 → 蓝灯恢复；Windows 与工具日志吻合。S4 和自动 S3 转 S4 已通过模拟通知/离线检查，真实 S4 周期尚未验证。

测试覆盖当前默认方案的阈值和节奏、预设差异、配置范围、事件去重和调度。使用模拟比赛数据与模拟马达输出，不直接振动。真实对局的受击反馈已由原使用者确认；新预设的手感需要自行测试。布局功能数据与导出文件一致，跨电脑导入及其他 Steam 账号访问尚未验证。

振动模块使用的是净掉血差分，没有被控制、小地图敌人出现、冷却或男枪弹药检测。只读取固定本地接口和发送马达指令，不读取游戏进程内存、不发送游戏操作。具体限制见模块文档。

本项目未经 Riot Games 背书，不代表 Riot Games 的观点；英雄联盟及相关商标属于其权利人。
