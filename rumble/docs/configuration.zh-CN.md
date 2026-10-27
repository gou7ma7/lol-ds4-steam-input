# 详细配置指南

[English](configuration.md)

## 怎样修改

先检查 `config/FeedbackConfig.cs` 和运行源码。配置采用编入程序的 C# 源文件，字段有名称和双语注释。日常定制只需要调整数字、`true/false`、震动步骤或四个预设的覆盖项，不读取外部运行时 JSON。

`Default()` 定义基础配置，`Create(name)` 创建独立副本后覆盖对应预设。修改基础值会影响没有覆盖该字段的其他预设。随仓库提供的 `default` 保留原使用者方案；编辑后就是你的新默认值。

从 `rumble/` 重新编译：

```powershell
powershell.exe -NoProfile -File .\scripts\build.ps1 -Preset default -Language auto
```

修改不会影响已经运行的程序。编译成功后正常退出旧程序，再运行新文件；如果 Windows 因程序运行中禁止覆盖文件，先退出再编译。托盘修改只对当前运行有效，长期偏好写入这里。

## 四个预设

- `default`：65%，六类全开；大量掉血 10%/500 毫秒，低血量 25%，低资源 20%。
- `gentle`：45%，关闭助攻；重击为 180 毫秒、快 .50/慢 .65、加成 .5；击杀改为单次 110 毫秒、.55/.15。
- `focus`：55%，关闭击杀/助攻/连杀；重击门槛 15%、低血量 20%、低资源 15%，其他节奏沿用基础配置。
- `celebration`：65%，关闭重击/低血量/低资源，保留击杀/助攻/连杀。

通过编译选择预设。语言 `auto` 随 Windows 界面语言选择中文或英文，`zh/en` 强制指定。增加新预设名称需要同时修改 `Create`、`PresetNames`、选择预设的 `#if` 和两个脚本的 `ValidateSet`。普通个人定制直接编辑现有预设最方便。

## 总强度

`MasterIntensity` 为 0..1，默认 .65；`IntensityChoices` 是 1..8 个 0..1 的托盘选项，初始 .45/.65/.85。若没有包含预设默认强度，托盘会自动补上。0 表示静默。

最终马达字节 = `round(255 × 总强度 × 步骤强度)`。快、慢是 DS4 的两个马达，不是 Hz 频率；快马达通常较细碎，慢马达较低沉。组合节奏和强度产生不同震感，不具备 DualSense 自适应扳机能力。

## 检测参数

比例用小数：`.25` 为 25%；`Seconds` 用秒，`Ms` 用毫秒。

- `HeavyWindowSeconds`：默认 .50，范围 .05..5。窗口内从峰值到当前的**净掉血**，增大可包含更慢的连续掉血。
- `HeavyDamageRatio`：.10，范围 .001..1。净掉血除以当前最大生命值；降低会更频繁，没有单独小伤害反馈。
- `HeavyCooldownSeconds`：.65，范围 0..60。重击最小触发间隔，触发后清空掉血历史。
- `LowHealthRatio`：.25，范围 .001.. .99。降至此比例或以下才提醒；启动时已经低血不算新触发。
- `HealthRearmRatio`：.35，至少比低血门槛高 .001、最多 1。回升到这里才允许再次进入低血触发。
- `LowHealthCooldownSeconds`：8，范围 0..120。除恢复和重新下降外，还需满足这个间隔。冷却期间进入低血会消耗这次下降，不会到期后补发。
- `LowResourceRatio`：.20，范围 .001.. .99，蓝量/能量使用相同下降语义。
- `ResourceRearmRatio`：.35，至少比低资源门槛高 .001、最多 1。
- `LowResourceCooldownSeconds`：8，范围 0..120，同样不会延迟补发。
- `ResourceTypes`：不重复的 `MANA`、`ENERGY`，可选一个或两个。不接受弹药、热量、怒气；它们的数据含义需要新检测逻辑。
- `MaxSampleGapSeconds`：.80，至少等于掉血窗口、最多 30。读取中断超过此值重建基准，避免补报旧掉血。
- `MaxHealthChangeTolerance`：.5 点 HP，范围 0..100。最大生命值改变超过此值重置检测，它不是掉血门槛。
- `MaxResourceChangeTolerance`：.5 点资源，范围 0..100。资源上限变化超过此值重置资源下降。

身份改变、采样时间倒退、死亡和重连也会影响基准。死亡不发低血提醒，但致命大掉血可以重震；回血和消耗生命的技能会影响净掉血含义。

## 每种反馈和节奏

六个键分别是 `HeavyDamage`、`LowHealth`、`LowResource`、`Kill`、`Assist`、`Multikill`；关闭也要保留配置项。

- `Enabled`：启动时是否自动触发；托盘仍可手动测试。
- `Priority`：0..1000 整数，高者立即打断低者，同级排队。依次默认 100/80/40/60/50/70。
- `LabelZh/LabelEn`：必须均非空，连杀使用本地化双杀至五杀名称。
- `Steps`：1..16 个 `MotorStep(毫秒, 快强度, 慢强度)`。时长 5..2000，强度 0..1；两个强度为 0 表示间隔。短于调度 tick 的步骤可能无法精确复现。
- `RepeatCount`：1..5，默认 1，重复整个 Steps。低血量默认 2、低资源 3。
- `RepeatGapMs`：重复间隔 0..2000，0 不插入额外间隔；低血默认 100、资源 110、连杀 80。
- `RepeatForKillCount`：只允许连杀，按事件次数限制到 2..5；关闭后使用固定 RepeatCount。
- `DamageSlowGain`：0..10，只允许大量掉血。振动步骤的慢马达增加 `max(0, 掉血比例 − 重击门槛) × 加成`，最终不超过 1；0 禁用动态加成，间隔始终保持停转。

完整节奏含重复和间隔不得超过 10 秒；这是校验上限，不是建议使用长时间振动。

例如：低资源只轻震一次：

```csharp
c.Effects[FeedbackKind.LowResource].RepeatCount = 1;
c.Effects[FeedbackKind.LowResource].Steps = new MotorStep[] {
    new MotorStep(100, .45, 0)
};
```

例如：整体减轻并关闭助攻：

```csharp
c.MasterIntensity = .50;
c.Effects[FeedbackKind.Assist].Enabled = false;
```

在选定 `Create` 分支中基础配置创建后加入覆盖，或直接改 `Default`。不用修改回归测试的固定旧版参数来迎合个人偏好。

## 排队和调度

- `QueueCapacity`：6，范围 1..64。同类新反馈替换队列中的旧项；超出容量时删除最旧待处理项，不按优先级决定淘汰。
- `QueueExpirySeconds`：2，范围 .1..10。过期队列项目不再播放。
- `MaxLatenessSeconds`：.35，范围 .01..2。步骤截止时间迟到超过此值，取消整段节奏，不加速补播旧脉冲。

被打断的反馈丢弃，不恢复；不同强度不会叠加。同一批连杀覆盖普通击杀，同批击杀/连杀不叠助攻。这些事件规则属于运行逻辑，不是配置开关。

## 高级读取参数

- `PlayerIntervalMs`：100，范围 20..5000，请求完成后等待，不保证严格采样周期。
- `EventIntervalSeconds`：.4，范围 .05..10，从上次事件请求完成后计时。
- `HttpTimeoutMs`：700，范围 100..5000，固定本地 API 超时；很长的超时会延迟失联发现。
- `MotorTimerMs`：20，范围 5..100，UI 线程调度 tick；Windows 定时可能抖动。
- `ReconnectSeconds`：1，范围 .1..30，USB 重连尝试间隔。
- `StatusIntervalSeconds`：2，范围 .1..60，可选状态文件写入间隔。

间隔降低会增加工作量，掉血窗口和采样中断阈值需与实际延迟匹配。服务器地址和设备报告格式不属于普通定制参数。

## 校验和测试

构建会拒绝缺少反馈、非法范围、非有限强度、不支持资源和过长节奏，再生成程序；所有生产预设均验证。`tests/RegressionSettings.cs` 是旧第 2 版固定回归输入，与可编辑生产配置分开。修改偏好不需要调整它。另有定制阈值/窗口、重复次数、加成、队列寿命和优先级测试。

`dist/build-info.json` 记录预设、语言和源码/配置/程序的 SHA-256；它不能证明下载程序可信，仍建议检查源码后本地编译。
