# Copyable rumble prompt

Give the following to an AI with repository access and replace the final line.

```text
Help me use or customize rumble/. Read root/module AGENTS.md, rumble/README.md, docs/configuration.md, config/FeedbackConfig.cs, src/LoLDamageRumble.cs and tests/LoLRumbleTests.cs first.

This is a Windows x64 USB DS4 v2 LoL feedback app using the local Live Client API, independent of Steam layouts. default preserves the original user's setup. Prefer editing documented thresholds, switches, intensity, waveforms and priorities in the config, then rebuild. Keep game key mapping outside this module.

Explain the relevant fields, units, ranges, trigger/rearm behavior and preemption before editing. Preserve deduplication, reconnect baselines, stale-effect expiry and motor cancellation on pause/exit. Test pure behavior with synthetic data and simulated output.

After reviewing the source/scripts, run scripts/build.ps1 locally with the chosen -Preset and -Language. Report test results and output paths. Keep English/Chinese documentation aligned. Do not recommend running an unknown downloaded executable or claim unsupported CC/visibility/ammo inputs exist.

My request: <describe the desired feedback>
```
