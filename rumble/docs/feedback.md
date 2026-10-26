# Feedback behavior

[简体中文](feedback.zh-CN.md)

The following describes the shipped `default`. Other presets and edited configs can change thresholds, switches and waveforms.

- Heavy damage: ≥10% maximum HP net loss within 500 ms, one 220 ms heavy pulse. Minimum interval 650 ms. Small hits are silent individually but may accumulate to the threshold.
- Low HP: entering ≤25% gives two 160 ms low-tone pulses, gap 100 ms. Rearm at ≥35%, minimum interval 8 s. No low alert while dead.
- Low mana/energy: entering ≤20% gives three 80 ms short pulses, gap 110 ms. Rearm at ≥35%, minimum interval 8 s. MANA/ENERGY only.
- Kill: 90 ms pulse, 80 ms pause, 150 ms pulse.
- Assist: one lighter 150 ms pulse, identified by the event's assister list.
- Multikill: 2..5 × 110 ms pulses according to double..penta kill, gaps 80 ms.

Priority: heavy damage → low HP → multikill → kill → assist → resource. Higher priority interrupts lower; old queued items expire after 2 s. Same-batch multikill replaces ordinary kill, and same-batch kill/multikill suppresses assist. Events arriving in different batches may produce kill then multikill.

## Baselines and stopping

Startup/reconnect/resume skips historical events and establishes a player baseline. Already-low HP/resource does not create a new crossing. Maximum-HP/resource changes and long read gaps reset related baselines. Pause, lock, suspend, missing data and exit cancel feedback and attempt to stop motors. Resume does not replay old effects.

## API limitations

Health changes are **net loss**, not a complete damage-event stream. Health-cost skills can trigger and healing can hide damage. There is no damage source/per-hit attribution here.

No crowd-control, slow debuff, vision entry, minimap coordinate, cooldown or Graves ammunition trigger is implemented. Movement speed cannot reliably stand in for a debuff. An `AMMO` resource enum does not establish readable Graves bullets; current low-resource logic only supports MANA/ENERGY.

The fixed loopback service is `https://127.0.0.1:2999/liveclientdata/`; player and event polling are configurable. TLS handling is confined to this service, with no game-memory access, input automation or external upload.

## Verification

Regression tests use fixed reference settings plus synthetic data; configurable behavior has separate tests. They do not send controller commands. Real damage feedback was confirmed by the user; new preset feel is unverified until tested physically. Written pulse counts alone do not prove physical response.
