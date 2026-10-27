# DS4 睡眠灭灯工具

简体中文 · [English](README.md)

> **先检查源码，再自行编译。不要直接运行网上下载的 `.exe`。**

独立的 Windows 托盘工具，支持 **一只 USB DS4 第二代（`054C:09CC`）**：睡眠/休眠前发送灭灯指令，用户恢复使用或解锁后恢复配置灯色。不需要 Steam、英雄联盟或仓库的其他模块。

## 实际行为

- 启动、退出程序时不改变灯光。
- 收到 `PBT_APMSUSPEND` 后发送 32 字节 USB 报告，RGB 为零。仅启用灯色/闪烁字段（`0x06`），不启用马达字段。USB 仍然供电。
- 后台自动唤醒时保持熄灭，包括可能紧接着从 S3 转入 S4 的定时唤醒。
- 用户主动恢复（`PBT_APMRESUMESUSPEND`）或睡眠后的解锁：等待 1.5 秒，再发送配置的 RGB，**默认蓝色 `0,0,255`**。已锁屏时等待解锁；USB 尚未就绪则短暂重试，超时后停止。
- 正常使用时不周期性发送灯光指令，Steam 后续可以改变灯色。程序无法读取并自动还原 Steam 最后设置的颜色/亮度。
- 没有睡眠的普通锁屏/解锁不会改灯；不负责单独的显示器关闭或空闲倒计时。

暂不支持 USB DS4 第一代、蓝牙 DS4、PS5 DualSense 或多只目标手柄。发现多个匹配设备时不发送，避免目标不明确。

## 检查、编译与运行

请阅读 [运行源码](src/Program.cs)、[电源状态机与 USB 报告](src/SleepPolicy.cs)、[原生 USB 访问](src/NativeHid.cs)、[配置](config/SleepLightConfig.cs)、[构建脚本](scripts/build.ps1) 和 [离线测试](tests/SleepLightTests.cs)。

从仓库根目录，在 Windows PowerShell 中执行：

```powershell
powershell.exe -NoProfile -File .\sleep-light\scripts\build.ps1
.\sleep-light\dist\DS4SleepLight.exe
```

需要 Windows x64 和 .NET Framework 4.x C# 编译器；不下载依赖，不要求管理员权限，不安装额外驱动。构建先运行离线检查，再生成 `dist/DS4SleepLight.exe`、`test-results.json`、`build-info.json`；不会启动工具或发送 USB 指令。

整个 `sleep-light/` 文件夹可以单独复制使用，在该文件夹运行 `scripts/build.ps1` 即可。默认 `-Language auto` 跟随 Windows 界面语言；`zh`、`en` 强制中英文。重新编译正在运行的程序前，先退出该工具。

## 可选：登录自启动

检查源码并自行编译后：

```powershell
powershell.exe -NoProfile -File .\sleep-light\scripts\autostart.ps1 -Action Enable
```

只添加一个**当前用户的“启动”文件夹快捷方式**，指向本机编译文件；不会立即启动，不安装服务或驱动。请保留该位置的程序；当前登录会话需要手动运行一次。同一会话只允许一个实例。

撤销：

```powershell
powershell.exe -NoProfile -File .\sleep-light\scripts\autostart.ps1 -Action Disable
.\sleep-light\dist\DS4SleepLight.exe --stop
```

托盘菜单也可退出，并提供不改变灯光的连接检查。若把程序放在其他位置，自启动脚本可用 `-Executable` 指定自己编译的文件。

## 配置与验证

修改 [SleepLightConfig.cs](config/SleepLightConfig.cs)，重新编译即可。详见 [配置与验证指南](docs/configuration.zh-CN.md)。

只读 USB 诊断，从仓库根目录执行：

```powershell
Start-Process .\sleep-light\dist\DS4SleepLight.exe -ArgumentList '--diagnose sleep-light/dist/diagnostic.json' -WindowStyle Hidden -Wait
Get-Content .\sleep-light\dist\diagnostic.json
```

可选的**实际灯光测试**：熄灭三秒，再发送配置的恢复灯色；不会让电脑睡眠。

```powershell
Start-Process .\sleep-light\dist\DS4SleepLight.exe -ArgumentList '--test-cycle 3 sleep-light/dist/cycle-result.json' -WindowStyle Hidden -Wait
Get-Content .\sleep-light\dist\cycle-result.json
```

写入成功不能单独证明肉眼灯光或完整睡眠流程。实际验证需要启动工具，手动选择 Windows“睡眠”或“休眠”，观察灭灯，再恢复/解锁观察灯色。不要用测试脚本自动休眠他人正在使用的电脑。

原使用者已在 Steam 与振动程序运行时确认真实 **S3 睡眠 → 恢复/解锁 → 配置蓝色** 流程正常，Windows 电源事件与工具日志吻合。S4、自动 S3 转 S4 已通过离线/模拟通知检查，尚未实测真实 S4 周期。

## 共存与限制

仓库振动模块仅启用马达（`0x01`）；此模块仅启用灯色/闪烁（`0x06`），两者不覆盖对方的报告字段，均使用共享 USB 句柄。两个模块不需要互相集成。

其他软件也写灯色、独占 HID、设备在转换时掉线、Windows 电源通知缺失/延迟，都可能影响实际效果。程序不阻止电脑睡眠，不断开 USB，不修改 BIOS/电源设置，不映射按键，不读取游戏数据或游戏内存。实际手柄行为需要自行观察，不能只凭写入成功保证有效。

可选的轮换本地日志位于 `%LOCALAPPDATA%\DS4SleepLight\events.log`，记录 UTC 时间、操作名称、匹配设备数及写入结果/错误编号；不包含设备路径、序列号、账号或游戏数据。诊断和测试文件也不包含这些标识。

[Agent 说明](AGENTS.md) · [Agent Prompt](prompts/agent.zh-CN.md) · [MIT 许可证](LICENSE)
