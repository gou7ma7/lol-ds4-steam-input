# Contributing

[简体中文](CONTRIBUTING.zh-CN.md)

- Keep `mapping/` and `rumble/` independently usable; shared root files are navigation, license and CI only.
- Update paired English/Chinese documentation together. Keep native VDF binding commands stable unless an actual mapping change is requested.
- Prefer feedback customization in `rumble/config/FeedbackConfig.cs`. Preserve `default` unless its behavior change is intentional and documented.
- Run `rumble/scripts/build.ps1` after code/config edits. Tests must use synthetic data and simulated motor output.
- State game version/mode and controller model/transport when reporting behavior. A successful write does not independently prove a physical response.
- Verify the USB report format and reliable stop behavior before supporting another controller; changing VID/PID alone is insufficient.
- Do not upload account IDs, local paths, device serials, complete game/Steam settings or identifiable match responses.
- This project covers manual input mapping and feedback. Keep automated casting/movement/target selection outside its scope.
- Review source before building or running it. Prebuilt binaries belong in optional release artifacts, not source commits.
