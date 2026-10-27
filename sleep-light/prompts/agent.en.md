# Copyable sleep light prompt

Give the following to an AI with access to this repository; replace the final line.

```text
Help me use or customize sleep-light/. First read root/module AGENTS.md, sleep-light/README.md, docs/configuration.md, config/SleepLightConfig.cs, all src/ files and tests/SleepLightTests.cs.

This is an independent Windows x64 USB DS4 v2 sleep light utility. It sends only lightbar/blink fields before suspend, keeps lights off during background automatic wake, and restores a configured color after interactive resume/unlock. Default restore is blue; Steam's last color cannot be read reliably. Preserve module independence and never enable motor fields or add gameplay automation.

Prefer annotated compile-time configuration, validate ranges, build from source using scripts/build.ps1, and keep bilingual docs aligned. Offline tests must not send USB commands or suspend my computer. Clearly distinguish successful write completion from physically verified behavior. Explain optional current-user autostart and its removal. Do not run internet-downloaded binaries or install a driver just for lighting.

My specific request: <desired behavior or configuration>
```
