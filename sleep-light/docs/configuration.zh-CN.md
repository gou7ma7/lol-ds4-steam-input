# 配置与验证

[English](configuration.md)

设置来自 `config/SleepLightConfig.cs`，会编入程序。修改后退出正在运行的工具，重新编译并启动。不需要运行时配置文件或其他模块。

- `RestoreOnResume`：默认 `true`，用户恢复使用/解锁后恢复配置灯色。设为 `false` 则保持熄灭，直到其他软件写入。
- `Red`、`Green`、`Blue`：整数 `0..255`，默认 `0,0,255`。低亮度蓝可用 `0,0,32`，紫色可用 `96,0,96`。这是原始 RGB 数值，不是 Steam 的亮度滑块。全部为零则恢复后也保持黑色。
- `RestoreDelayMs`：`0..10000`，默认 `1500` 毫秒。用户恢复使用/解锁后等待 USB 就绪；设备重新枚举慢时可以增加。
- `RestoreRetryWindowMs`：`500..30000`，默认 `5000` 毫秒。从初始延迟结束后计算的重试窗口。超时后等待下一次睡眠周期，不持续写入。
- `RestoreRetryIntervalMs`：`100..5000`，默认 `500` 毫秒。恢复写入失败后的重试间隔，成功则立即停止重试。
- `OffWriteAttempts`：`1..3`，默认 `2` 次。在睡眠回调中短暂重复灭灯，不会产生睡眠期间运行的任务。
- `OffRepeatDelayMs`：`0..100`，默认 `50` 毫秒。重复灭灯报告的间隔。
- `UsbWriteTimeoutMs`：`50..500`，默认 `200` 毫秒。待处理写入超时后请求取消；缓冲区和 OVERLAPPED 内存保留到系统完成或取消 IO。异常驱动可能延迟取消，因此不是绝对的总耗时上限。
- `EnableLocalLog`：默认 `true`，改为 `false` 可停止写轮换本地日志；日志不记录设备/游戏身份数据。

示例：保留默认蓝色但调暗，增加 USB 恢复等待：

```csharp
internal static readonly int Red = 0, Green = 0, Blue = 32;
internal static readonly int RestoreDelayMs = 2500;
```

从模块目录编译：

```powershell
powershell.exe -NoProfile -File .\scripts\build.ps1 -Language zh
```

构建验证配置范围，并使用模拟检查覆盖报告字段、设备能力筛选、重复通知、解锁延迟恢复、待恢复任务取消，以及 S3 → 自动唤醒 → S4。测试程序不调用原生 USB 接口。

实际验证时保持工具运行，分别手动测试 S3 睡眠和 S4 休眠。观察灭灯与恢复灯色，查看 `%LOCALAPPDATA%\DS4SleepLight\events.log`。`suspend_off ... outcome=written` 只表示 Windows 完成了报告写入，不能代替肉眼确认。后台唤醒应记录 `automatic_resume_no_light_write`，直到用户恢复使用/解锁才恢复灯色。

若其他软件紧接着覆盖灯色，调整延迟/重复次数可能有帮助，但不能保证。不要默认通过关闭 USB 节电、安装 HID 隐藏驱动或替换已正常工作的 Steam 布局来排查；进一步改动应依据观察到的行为。
