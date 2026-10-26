# LoL controller mapping & rumble feedback

[简体中文](README.zh-CN.md) · English

> **Review the source code and build the rumble program yourself. Do not directly run an `.exe` downloaded from the internet.** The build instructions below are the recommended starting point. Prebuilt files and CI artifacts are optional references, not a substitute for reviewing the code.

Two independent modules for League of Legends:

- **[mapping/](mapping/README.md)**: left-hand DualShock 4 + right-hand mouse, Steam Input VDF, WASD game settings and champion-specific usage notes. It works without the rumble program.
- **[rumble/](rumble/README.md)**: Windows USB DS4 feedback based on the local Live Client Data API. It works without this Steam layout and does not require WASD movement or Steam.

Use either module separately, or combine both. Neither module installs or changes the other.

## Review and build first

Inspect [runtime source](rumble/src/LoLDamageRumble.cs), [annotated configuration](rumble/config/FeedbackConfig.cs), [build script](rumble/scripts/build.ps1) and [tests](rumble/tests/LoLRumbleTests.cs). Then, from the repository root in Windows PowerShell:

```powershell
powershell.exe -NoProfile -File .\rumble\scripts\build.ps1
```

The script first validates all presets and runs simulated behavior tests, then compiles `rumble/dist/LoLDamageRumble.exe`. It does not start the application or send controller commands. After your review and successful build, start your own binary:

```powershell
.\rumble\dist\LoLDamageRumble.exe
```

Requires Windows x64 and the .NET Framework 4.x C# compiler; no package downloads, API Key or administrator rights are needed. The rumble device implementation supports USB DS4 v2 only (`054C:09CC`).

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
```

[Agent entry point](AGENTS.md) · [Contributing](CONTRIBUTING.md) · [MIT license](LICENSE)

## Verification and limits

Default timings and detection behavior are regression-tested against the previous setup. Tests use synthetic game data and simulated motor output. Real-game damage feedback was confirmed by the original user; other presets require personal feel testing. The mapping's functional data matches the exported layout, but importing it on a second computer and accessing its Steam link from another account remain unverified.

The rumble module infers **net health loss**, not individual damage events. It does not implement crowd control, minimap visibility, cooldown or Graves ammunition detection. It reads the fixed loopback API and writes motor commands, without reading game process memory or issuing game inputs. Details are in each module's documentation.

This project is not endorsed by Riot Games and does not reflect the views or opinions of Riot Games or anyone officially involved in producing or managing Riot Games properties. Riot Games and all associated properties are trademarks or registered trademarks of Riot Games, Inc.
