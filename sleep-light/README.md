# DS4 sleep light

[简体中文](README.zh-CN.md) · English

> **Review the source and build locally. Do not directly run an `.exe` downloaded from the internet.**

An independent Windows tray utility for **one USB DualShock 4 v2 (`054C:09CC`)**. It sends a light-off command before sleep/hibernate and restores a configured color after an interactive resume/unlock. It needs neither Steam, League of Legends, nor the other repository modules.

## Behavior

- Starting or exiting the watcher does not change the light.
- On `PBT_APMSUSPEND`, send a 32-byte USB report with RGB zero. Only lightbar/blink fields are enabled (`0x06`); motor fields are disabled. USB power stays connected.
- On a background automatic resume, keep the light off. This includes a timer wake that may immediately transition from S3 to S4.
- On an interactive resume (`PBT_APMRESUMESUSPEND`) or an unlock following suspend, wait 1.5 seconds, then restore the configured RGB color, **blue (`0,0,255`) by default**. If the session is locked, wait for unlock. Retry an unavailable USB device briefly, then stop.
- The utility sends no periodic light commands during normal use. Steam can subsequently change the light. It cannot read or automatically restore Steam's last RGB/brightness setting.
- Normal lock/unlock without sleep does not change the light. This is not a screen-off or idle timer.

USB DS4 v1, Bluetooth DS4, DualSense and multiple connected target controllers are unsupported. Multiple matching controllers result in no writes, avoiding an ambiguous target.

## Review, build and run

Read [source](src/Program.cs), [power policy and USB report](src/SleepPolicy.cs), [native USB access](src/NativeHid.cs), [configuration](config/SleepLightConfig.cs), [build script](scripts/build.ps1) and [offline tests](tests/SleepLightTests.cs).

From the repository root, in Windows PowerShell:

```powershell
powershell.exe -NoProfile -File .\sleep-light\scripts\build.ps1
.\sleep-light\dist\DS4SleepLight.exe
```

Requires Windows x64 and the .NET Framework 4.x C# compiler. No downloaded packages, administrator rights or added device driver. The build runs offline checks and produces `dist/DS4SleepLight.exe`, `test-results.json` and `build-info.json`; it does not start the watcher or send USB commands.

The folder is standalone: after copying `sleep-light/` elsewhere, run `scripts/build.ps1` there. `-Language auto` uses the Windows UI language; `zh` and `en` force Chinese or English. Stop a running watcher before rebuilding its executable.

## Optional login autostart

After reviewing and building your own executable:

```powershell
powershell.exe -NoProfile -File .\sleep-light\scripts\autostart.ps1 -Action Enable
```

This adds only a **current-user Startup shortcut** pointing to your local executable. It neither launches it immediately nor installs a service/driver. Keep the executable at that location; run it once for the current session. The watcher allows one instance per session.

To undo:

```powershell
powershell.exe -NoProfile -File .\sleep-light\scripts\autostart.ps1 -Action Disable
.\sleep-light\dist\DS4SleepLight.exe --stop
```

The tray menu also provides Exit and a connection check that does not change lights. Use `-Executable` with the autostart script when you keep your compiled binary elsewhere.

## Configure and verify

Edit [SleepLightConfig.cs](config/SleepLightConfig.cs) and rebuild. See the [configuration and verification guide](docs/configuration.md).

Read-only USB diagnosis, from the repository root:

```powershell
Start-Process .\sleep-light\dist\DS4SleepLight.exe -ArgumentList '--diagnose sleep-light/dist/diagnostic.json' -WindowStyle Hidden -Wait
Get-Content .\sleep-light\dist\diagnostic.json
```

Optional **physical light test**: this changes the light for three seconds and then sends the configured restore color; it does not suspend the computer.

```powershell
Start-Process .\sleep-light\dist\DS4SleepLight.exe -ArgumentList '--test-cycle 3 sleep-light/dist/cycle-result.json' -WindowStyle Hidden -Wait
Get-Content .\sleep-light\dist\cycle-result.json
```

Successful HID write completion does not independently confirm the visible light or full sleep-cycle behavior. For the real test, start the watcher, manually choose Windows Sleep or Hibernate, observe the light, then resume/unlock and observe its return. Do not automate suspending another person's active PC as a test.

The original user confirmed a real **S3 sleep → resume/unlock → configured blue** cycle while Steam and the rumble program were running; Windows power events and the watcher log agreed. S4 and automatic S3-to-S4 handling were checked with offline/simulated notifications, not a physical S4 cycle.

## Coexistence and limits

The repository's rumble reports enable only motors (`0x01`); this module enables only lightbar/blink (`0x06`). Their report fields do not replace one another. Both use shared USB handles. No integration or dependency is required between the modules.

Other software that also writes the lightbar, exclusive HID access, device removal during transition, or missing/delayed Windows power notifications may prevent a visible response. The program does not keep the computer awake, disconnect USB, change BIOS/power settings, remap buttons, read game data or read game memory. Physical behavior must be checked on your device; no guarantee is made solely from a successful write.

The optional rotating local log is `%LOCALAPPDATA%\DS4SleepLight\events.log`, with UTC timestamps, action names, matching-device counts and write result/error numbers. It contains no device path, serial, account or game data. Diagnosis/test output files also omit those identifiers.

[Agent context](AGENTS.md) · [Agent prompts](prompts/agent.en.md) · [MIT license](LICENSE)
