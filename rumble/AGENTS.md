# Rumble agent context / 振动反馈 Agent 说明

- Scope: this entire `rumble/` subtree; it must build without `mapping/` or Steam.
- Read README, `docs/configuration.md`, `config/FeedbackConfig.cs`, runtime, then tests.
- Customization entry: `config/FeedbackConfig.cs`. `default` preserves the user's current timings/thresholds/intensity.
- `gentle`, `focus`, `celebration` are independent copies derived from default; no shared mutable configuration.
- Build: `powershell.exe -NoProfile -File ./scripts/build.ps1 -Preset default -Language auto` from this module.
- The build validates all presets and runs simulated behavior tests before compiling the application.
- Pure detection, pattern generation and scheduling must be testable without HTTP/HID operations.
- Supported device: Windows x64, USB DS4 v2 054C:09CC. Do not claim Bluetooth/other-device support.
- Read only the fixed loopback Live Client API; do not read game memory or send gameplay inputs.
- Net health loss is inferred; no reliable CC/visibility/cooldown/ammo input exists in this implementation.
- Preserve stop/cancel behavior on pause, lock, suspend, disconnect and exit; never replay stale queued pulses.
- UI has Chinese/English build choices. Pair docs and explain units/ranges when adding config fields.
- Review source and build locally; do not instruct users to trust an arbitrary downloaded executable.
- Prompts: `prompts/agent.zh-CN.md` and `prompts/agent.en.md`.
