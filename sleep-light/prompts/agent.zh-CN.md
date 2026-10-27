# 可复制的睡眠灭灯 Prompt

交给能访问仓库的 AI，替换最后一行。

```text
请帮助我使用或定制 sleep-light/。先读取根目录/模块 AGENTS.md、sleep-light/README.zh-CN.md、docs/configuration.zh-CN.md、config/SleepLightConfig.cs、src/ 全部源码和 tests/SleepLightTests.cs。

这是独立的 Windows x64、USB DS4 第二代睡眠灭灯工具。睡眠前仅更新灯色/闪烁字段，后台自动唤醒保持熄灭，用户恢复使用/解锁后恢复配置灯色。默认恢复蓝色，无法可靠读取 Steam 最后设置的颜色。保持模块独立，不启用马达字段，不加入游戏操作自动化。

优先修改有注释的编译时配置，校验范围，通过 scripts/build.ps1 自行编译，同步双语文档。离线测试不发送 USB 指令，不自动休眠我的电脑。明确区分写入成功与实际肉眼/睡眠验证。解释可选的当前用户自启动及撤销方式。不运行网上下载的二进制，不为了灯光安装额外驱动。

我的具体需求：<期望的行为或配置>
```
