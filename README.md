# LoL controller mapping, rumble & sleep light

[简体中文](README.zh-CN.md) · English

> **Review the source code and build the programs yourself. Do not directly run an `.exe` downloaded from the internet.** The build instructions below are the recommended starting point. Prebuilt files are optional references, not a substitute for reviewing the code.

Three independent modules for a Windows controller setup:

- **[mapping/](mapping/README.md)**: left-hand DualShock 4 + right-hand mouse, Steam Input VDF, WASD game settings and champion-specific usage notes. It works without the rumble program.
- **[rumble/](rumble/README.md)**: Windows USB DS4 feedback based on the local Live Client Data API. It works without this Steam layout and does not require WASD movement or Steam.
- **[sleep-light/](sleep-light/README.md)**: turn off a USB DS4 lightbar before sleep/hibernate, restore configured blue after user resume/unlock, and keep it off during background automatic wake. No Steam or game dependency.

Use any module separately or together. None installs or changes another.

## Review and build first

Build verification runs locally using the included scripts. This repository currently has no GitHub Actions workflow.

Inspect [runtime source](rumble/src/LoLDamageRumble.cs), [annotated configuration](rumble/config/FeedbackConfig.cs), [build script](rumble/scripts/build.ps1) and [tests](rumble/tests/LoLRumbleTests.cs). Then, from the repository root in Windows PowerShell:

```powershell
powershell.exe -NoProfile -File .\rumble\scripts\build.ps1
```

The script first validates all presets and runs simulated behavior tests, then compiles `rumble/dist/LoLDamageRumble.exe`. It does not start the application or send controller commands. After your review and successful build, start your own binary:

```powershell
.\rumble\dist\LoLDamageRumble.exe
```

Requires Windows x64 and the .NET Framework 4.x C# compiler; no package downloads, API Key or administrator rights are needed. The rumble device implementation supports USB DS4 v2 only (`054C:09CC`).

## Build sleep light independently

Review [sleep light source and configuration](sleep-light/README.md#review-build-and-run), then build and run your own executable:

```powershell
powershell.exe -NoProfile -File .\sleep-light\scripts\build.ps1
.\sleep-light\dist\DS4SleepLight.exe
```

The build runs offline checks without USB writes. [Optional current-user login autostart](sleep-light/README.md#optional-login-autostart) is a separate, reversible step. The watcher only updates lightbar/blink fields; it does not change motor output or remap buttons. [Detailed configuration and verification](sleep-light/docs/configuration.md).

## Customize feedback

Edit **[rumble/config/FeedbackConfig.cs](rumble/config/FeedbackConfig.cs)** and rebuild. Comments explain every parameter in Chinese and English. No separate runtime configuration file is needed.

- `default`: the user's current setup, 65% intensity, heavy damage, low HP, low mana/energy, kill, assist and multikill.
- `gentle`: 45% intensity, gentler hit/kill pulses, no assist pulse.
- `focus`: critical alerts; no kill celebrations, higher heavy-hit threshold and lower warning thresholds.
- `celebration`: kill, assist and multikill only.

```powershell
powershell.exe -NoProfile -File .\rumble\scripts\build.ps1 -Preset focus -Language en
```

`-Language auto` is the default: Chinese on a Chinese Windows UI, English otherwise; `zh` and `en` force a language. Presets are selected at build time. See the [detailed configuration guide](rumble/docs/configuration.md).

## Repository structure

```text
mapping/                 Independent key-mapping module
  steam-input/           Original-format VDF and sanitized metadata
  game-settings/         Reference data, not a Riot import file
  docs/                  Setup and champion/interaction notes, both languages
  AGENTS.md, prompts/    Agent context and copyable prompts
rumble/                  Independent feedback module
  config/                Editable presets and detailed parameter comments
  src/, scripts/, tests/ Runtime, local build and offline behavior tests
  docs/                  Configuration and feedback guides, both languages
  AGENTS.md, prompts/    Agent context and copyable prompts
sleep-light/             Independent sleep light module
  config/                Annotated compile-time RGB and timing settings
  src/, scripts/, tests/ Power events, USB light-only output, build and offline tests
  docs/, prompts/        Bilingual guidance and Agent entry points
```

[Agent entry point](AGENTS.md) · [Contributing](CONTRIBUTING.md) · [MIT license](LICENSE)

## Verification and limits

Sleep light offline checks cover background wake versus user resume, repeat notifications, lock/unlock and pending restoration cancellation. The original user visually confirmed USB off/restore and a real S3 sleep → resume/unlock → blue restore cycle with Steam and rumble running. Windows and watcher logs agreed. S4 and the automatic S3-to-S4 sequence are covered by simulated notifications/offline checks; a physical S4 cycle remains unverified.

Default timings and detection behavior are regression-tested against the previous setup. Tests use synthetic game data and simulated motor output. Real-game damage feedback was confirmed by the original user; other presets require personal feel testing. The mapping's functional data matches the exported layout, but importing it on a second computer and accessing its Steam link from another account remain unverified.

The rumble module infers **net health loss**, not individual damage events. It does not implement crowd control, minimap visibility, cooldown or Graves ammunition detection. It reads the fixed loopback API and writes motor commands, without reading game process memory or issuing game inputs. Details are in each module's documentation.

This project is not endorsed by Riot Games and does not reflect the views or opinions of Riot Games or anyone officially involved in producing or managing Riot Games properties. Riot Games and all associated properties are trademarks or registered trademarks of Riot Games, Inc.
