# 英雄联盟 USB DS4 振动反馈

简体中文 · [English](README.md)

> **先检查源码，再在本地自行编译。不要直接运行网上下载的 `.exe`。** 请优先采用以下构建方式。

可独立使用的 Windows x64 托盘程序，读取英雄联盟本地比赛接口，控制 USB DS4 第二代的两个马达。不需要 Steam、特定按键布局或 WASD 模式；不使用第三方库或 API Key。

## 检查和编译

阅读 [源码](src/LoLDamageRumble.cs)、[配置](config/FeedbackConfig.cs)、[构建脚本](scripts/build.ps1) 和 [测试](tests/LoLRumbleTests.cs)。从本模块目录运行：

```powershell
powershell.exe -NoProfile -File .\scripts\build.ps1
```

需要 Windows .NET Framework 4.x x64 C# 编译器。脚本验证所有预设并运行模拟测试，再生成 `dist/LoLDamageRumble.exe`；不下载依赖、不向手柄发送指令。检查后运行自己编译的程序：

```powershell
.\dist\LoLDamageRumble.exe
```

本文件夹可以单独复制和构建，`dist/` 生成文件不提交到源码仓库。

## 预设与定制

**[config/FeedbackConfig.cs](config/FeedbackConfig.cs)** 是编译时配置，带详细中英文注释。正常调节只改配置再编译，不需要修改业务逻辑。

- `default`：你的当前方案，65% 强度，六类反馈全开。
- `gentle`：45% 强度，重击/击杀更轻，关闭助攻。
- `focus`：55% 强度，仅关键提醒；大量掉血 15%、低血量 20%、低资源 15%。
- `celebration`：65% 强度，仅击杀、助攻、连杀。

```powershell
powershell.exe -NoProfile -File .\scripts\build.ps1 -Preset gentle -Language zh
```

界面语言可选 `auto`、`zh`、`en`。托盘显示编译预设、阈值和启用状态；托盘临时修改只对本次运行有效。见 [详细配置](docs/configuration.zh-CN.md) 和 [反馈行为](docs/feedback.zh-CN.md)。

## 运行与排错

支持 USB DS4 第二代 `054C:09CC`，未实现蓝牙、DS4 第一代、DualSense 或其他手柄。使用 Windows HID 驱动，需恰好连接一个匹配的手柄。

没有对局时静默等待。托盘可以开关反馈、调整强度、测试节奏、暂停和退出。托盘手动测试会真的振动，即使该反馈的自动触发已关闭。其他同时写马达的软件可能覆盖输出。

暂停、锁屏、休眠、失联和退出会取消队列并尝试停转，恢复后重新建立基准。不安装开机启动或偏好持久化。通过托盘或 `--stop` 正常结束：

```powershell
.\dist\LoLDamageRumble.exe --stop
```

可选只读诊断：

```powershell
Start-Process -FilePath .\dist\LoLDamageRumble.exe -ArgumentList '--diagnose', 'diagnostics.json' -Wait -WindowStyle Hidden
Get-Content .\diagnostics.json
```

诊断检查接口和手柄输出句柄，不发送振动。`--status-file <路径>` 定期写状态；`--test <路径>` 会真的短震 150 毫秒并停转。日常模式测试建议用托盘。

## Agent 与参考

[AGENTS.md](AGENTS.md) · [中文 Prompt](prompts/agent.zh-CN.md) · [English Prompt](prompts/agent.en.md) · [MIT 许可证](LICENSE)

[相似反馈项目](docs/related-work.zh-CN.md)。

[Riot 本地比赛接口](https://developer.riotgames.com/docs/lol#game-client-api_live-client-data-api)。检测的是净掉血，没有控制、视野、技能冷却或弹药反馈；添加新触发前先检查数据能力。
