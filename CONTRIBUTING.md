# Contributing

[简体中文](CONTRIBUTING.zh-CN.md)

- Keep `mapping/`, `rumble/` and `sleep-light/` independently usable; shared root files provide navigation and licensing.
- Update paired English/Chinese documentation together. Keep native VDF binding commands stable unless an actual mapping change is requested.
- Prefer feedback customization in `rumble/config/FeedbackConfig.cs`. Preserve `default` unless its behavior change is intentional and documented.
- Run `rumble/scripts/build.ps1` after code/config edits. Tests must use synthetic data and simulated motor output.
- Run `sleep-light/scripts/build.ps1` after sleep-light code/config edits. Offline tests must not access USB or suspend the PC; separately report physical testing.
- State game version/mode and controller model/transport when reporting behavior. A successful write does not independently prove a physical response.
- Verify the USB report format and reliable stop behavior before supporting another controller; changing VID/PID alone is insufficient.
- Do not upload account IDs, local paths, device serials, complete game/Steam settings or identifiable match responses.
- This project covers manual input mapping, feedback and sleep light control. Keep automated casting/movement/target selection outside its scope.
- Review source before building or running it. Prebuilt binaries belong in optional release artifacts, not source commits.
