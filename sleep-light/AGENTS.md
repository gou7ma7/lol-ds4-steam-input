# Sleep light agent context / 睡眠灭灯 Agent 说明

- Scope: all of `sleep-light/`; build and run independently of mapping, rumble, Steam and LoL.
- Read README, `docs/configuration.md`, configuration, all `src/` files and offline tests.
- Windows x64, one USB DS4 v2 054C:09CC, gamepad usage and 32-byte output only. Refuse ambiguous multiple targets.
- Preserve report 0x05 / flags 0x06: RGB and blink only; do not enable motor, audio or unrelated fields.
- Starting/exiting must not write lights. No gameplay input, game API, game memory, power setting or BIOS changes.
- Background PBT_APMRESUMEAUTOMATIC must not restore RGB; S3 can automatically wake only to enter S4.
- Restore configured RGB after interactive resume/unlock; cancel pending restore on another suspend/lock.
- Config: `config/SleepLightConfig.cs`; compile-time RGB default blue. Do not claim restoration of Steam's last color.
- Build `scripts/build.ps1`; offline checks must not invoke HID or suspend the PC.
- Native write completion is not proof of visible light or actual sleep behavior; describe verification accurately.
- Keep pinned data and unmanaged OVERLAPPED alive until pending/canceled IO completes.
- Autostart is an optional current-user shortcut. Never silently install drivers or services for this module.
- No device paths/serials, accounts, personal match data or local absolute paths in public files.
- Keep Chinese and English docs/prompts aligned. Source-first build guidance must stay prominent.
- Copyable prompts: `prompts/agent.en.md`, `prompts/agent.zh-CN.md`.
