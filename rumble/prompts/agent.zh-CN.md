# 可复制的振动反馈 Prompt

把下面内容交给能访问此仓库的 AI，最后一行替换成你的具体需求。

```text
请帮助我使用或定制 rumble/ 模块。先读取根目录和模块 AGENTS.md、rumble/README.zh-CN.md、docs/configuration.zh-CN.md、config/FeedbackConfig.cs、src/LoLDamageRumble.cs 和 tests/LoLRumbleTests.cs。

这是 Windows x64、USB DS4 第二代的英雄联盟本地比赛数据振动程序，独立于 Steam 按键布局。当前 default 是原使用者的默认方案。优先通过带注释的配置修改阈值、开关、强度、节奏和优先级，再重新编译；不要把游戏按键映射混进此模块。

先说明我需要改哪些参数，以及单位、取值范围、触发/恢复门槛和震动重叠行为。保留事件去重、失联重建基准、过期反馈丢弃和暂停/退出停转。使用模拟数据验证，不靠实际振动验证纯逻辑。

检查源码和脚本后，在本地运行 scripts/build.ps1，选择适当的 -Preset 和 -Language。检查测试结果并提供生成文件位置。同步中英文注释/说明；不要建议我直接运行来历不明的 .exe，也不要声称不存在的控制状态、地图视野或弹药接口可用。

我的具体需求：<填写希望的反馈体验>
```
