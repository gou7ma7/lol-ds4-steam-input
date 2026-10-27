# Detailed configuration guide

[简体中文](configuration.zh-CN.md)

## How editing works

Review `config/FeedbackConfig.cs` and the runtime first. The configuration is C# source compiled into the executable, with named fields and bilingual comments. Normal customization means changing numbers, `true`/`false`, pulse arrays or the four preset overrides. No external runtime JSON file is loaded.

`Default()` defines the base settings. `Create(name)` builds an independent copy and applies a preset's overrides. Edits to the base also affect presets except for fields they override. The shipped `default` matches the original user's setup; after your edits it becomes your own default.

From `rumble/`, rebuild:

```powershell
powershell.exe -NoProfile -File .\scripts\build.ps1 -Preset default -Language auto
```

No source/config change affects an already running program. After a successful build, exit the old app normally and start the newly compiled one. If Windows prevents overwriting a running file, stop that instance and rebuild. Tray choices are temporary; persistent changes belong here.

## Presets

- `default`: 65%, all six effects; heavy damage 10%/500 ms, low HP 25%, low resource 20%.
- `gentle`: 45%, assist disabled; heavy hit 180 ms at fast .50/slow .65, gain .5; kill is one 110 ms pulse at .55/.15.
- `focus`: 55%, kill/assist/multikill disabled; heavy threshold 15%, low HP 20%, low resource 15%. Other timings stay at base values.
- `celebration`: 65%, heavy/low-HP/low-resource disabled; kill/assist/multikill unchanged.

Changing a preset is a compile-time choice. `-Language auto` follows Windows UI language (Chinese for `zh`, English otherwise), while `zh`/`en` force it. To add a new preset name, update `Create`, `PresetNames`, the selection `#if`, and both scripts' `ValidateSet`. For straightforward personal tuning, edit an existing preset.

## Intensity

`MasterIntensity` is 0..1, default .65. `IntensityChoices` contains 1..8 values, each 0..1, initially .45/.65/.85. The tray also adds the selected preset's master value if it is missing. Zero gives silent output.

Each motor byte is `round(255 × master × step strength)`. Fast/slow are two separate DS4 motors, not frequencies in Hz. Fast tends to feel finer; slow tends to feel heavier. Pulse timing and intensity form a pattern; they do not provide DualSense adaptive-trigger behavior.

## Detection fields

All ratios use fractions: `.25` is 25%, not 0.25%. Times ending in `Seconds` use seconds; `Ms` uses milliseconds.

- `HeavyWindowSeconds`: .50 by default, valid .05..5. Rolling peak-to-current **net** HP loss within this window. Increase it to include slower accumulated loss.
- `HeavyDamageRatio`: .10, valid .001..1. Divide net loss by current maximum HP. Lower values trigger more often; there is no separate small-damage effect.
- `HeavyCooldownSeconds`: .65, valid 0..60. Minimum time between heavy triggers; history clears after a trigger.
- `LowHealthRatio`: .25, valid .001.. .99. Entry threshold, inclusive. Starting already low does not trigger a new alert.
- `HealthRearmRatio`: .35, must be at least .001 above the low threshold and at most 1. Recovery needed before another entry can warn.
- `LowHealthCooldownSeconds`: 8, valid 0..120. Minimum interval between alerts, in addition to recovery/re-entry. Entering low during cooldown consumes that crossing; it is not delayed until cooldown expires.
- `LowResourceRatio`: .20, valid .001.. .99. Same crossing semantics for mana/energy.
- `ResourceRearmRatio`: .35, at least .001 above the resource threshold and at most 1.
- `LowResourceCooldownSeconds`: 8, valid 0..120. Same non-delayed cooldown behavior.
- `ResourceTypes`: unique `MANA`, `ENERGY`, or both. Do not substitute `AMMO`, heat or fury: these require different data/meaning and are rejected.
- `MaxSampleGapSeconds`: .80, at least the heavy window and at most 30. A larger data gap resets the baseline instead of generating stale damage.
- `MaxHealthChangeTolerance`: .5 HP, valid 0..100. Changes to maximum HP beyond it reset detection. This is not a damage threshold.
- `MaxResourceChangeTolerance`: .5 resource points, valid 0..100. Resource-capacity changes beyond it reset resource crossings.

Changing hero identity, going backward in sample time, death and reconnects also affect baselines. Death suppresses low warnings; a fatal large hit can still trigger heavy feedback. Healing and health-cost skills can change net-loss interpretation.

## Effects and pulse sequences

Six keys: `HeavyDamage`, `LowHealth`, `LowResource`, `Kill`, `Assist`, `Multikill`. Every key must remain present, even when disabled.

- `Enabled`: whether automatic feedback is enabled at startup. Manual tray tests can still play it.
- `Priority`: integer 0..1000; higher immediately interrupts lower. Equal priority waits. Defaults: 100/80/40/60/50/70 in the key order above.
- `LabelZh`, `LabelEn`: both must be nonempty. Multikill uses localized 2..5-kill labels.
- `Steps`: 1..16 `MotorStep(durationMs, fast, slow)` entries; duration 5..2000 ms, motor strengths 0..1. `(0,0)` is a pause. Timing shorter than the scheduler tick may not be reproduced precisely.
- `RepeatCount`: 1..5, default 1. Repeats the entire step array. Low HP uses 2; low resource uses 3.
- `RepeatGapMs`: 0..2000, gap between repetitions; 0 inserts no extra step. Low HP 100, resource 110, multikill 80 by default.
- `RepeatForKillCount`: allowed only for `Multikill`; uses event count clamped to 2..5 instead of RepeatCount. Disable it for a fixed multikill pattern.
- `DamageSlowGain`: 0..10, allowed only for `HeavyDamage`. Adds `max(0, lossRatio − HeavyDamageRatio) × gain` to slow strength on vibrating steps, capped at 1. Zero disables this scaling; pauses stay pauses.

The longest generated effect (including repetitions/gaps) must not exceed 10 seconds. This is a validation limit, not a recommendation for long pulses.

Example: one light low-resource pulse:

```csharp
c.Effects[FeedbackKind.LowResource].RepeatCount = 1;
c.Effects[FeedbackKind.LowResource].Steps = new MotorStep[] {
    new MotorStep(100, .45, 0)
};
```

Example: lower overall intensity and disable assist:

```csharp
c.MasterIntensity = .50;
c.Effects[FeedbackKind.Assist].Enabled = false;
```

Put overrides in the chosen `Create` case after the base is created, or modify the base `Default`. Do not edit the regression fixture to match personal preferences.

## Queue and scheduling

- `QueueCapacity`: 6, valid 1..64. New same-kind feedback replaces queued older feedback. When full, the oldest queued item is removed, irrespective of priority.
- `QueueExpirySeconds`: 2, valid .1..10. Old pending feedback expires rather than playing late.
- `MaxLatenessSeconds`: .35, valid .01..2. If a running step's deadline is missed by more than this, cancel the sequence instead of rushing old pulses.

A preempted effect is discarded, not resumed. Motor strengths are never added together. Same-batch multikill replaces ordinary kill; same-batch kill/multikill suppresses assist. Those event rules are runtime behavior, not parameter switches.

## Advanced polling

- `PlayerIntervalMs`: 100, valid 20..5000; wait after player requests, not a guaranteed sampling interval.
- `EventIntervalSeconds`: .4, valid .05..10; next event read after the previous request.
- `HttpTimeoutMs`: 700, valid 100..5000; fixed loopback API timeout. A long timeout delays disconnect detection.
- `MotorTimerMs`: 20, valid 5..100; UI-thread scheduling tick. Windows timing can vary.
- `ReconnectSeconds`: 1, valid .1..30; retry connecting the USB controller.
- `StatusIntervalSeconds`: 2, valid .1..60; optional status file write interval.

Lower intervals increase work. Keep the damage window/gap settings consistent with actual polling latency. The server address and device report format are intentionally not ordinary configuration fields.

## Validation and tests

Builds reject missing rules, invalid ranges, non-finite strengths, invalid resource types and overlong patterns before producing a new application. Every production preset is validated. `tests/RegressionSettings.cs` is a fixed reference for testing previous v2 detection and timing, independent of your editable presets. Editing your preferred thresholds or waveforms does not require changing those reference values. Separate tests verify custom thresholds/windows, repetition, gain, queue lifetime and priorities.

`dist/build-info.json` records selected preset/language and source/config/executable SHA-256 hashes. It does not prove a downloaded executable is trustworthy; source review and local compilation remain the preferred path.
