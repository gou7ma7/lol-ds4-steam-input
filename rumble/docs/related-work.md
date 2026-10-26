# Related feedback work

[简体中文](related-work.zh-CN.md)

Sources checked on 2026-10-27. We reviewed project descriptions and selected source files, without running their programs.

- [buttplug-of-legends](https://github.com/kinkyacedev/buttplug-of-legends): an archived Python project that reads `activeplayername` and `eventdata` from the same `127.0.0.1:2999/liveclientdata` API. Its [source](https://github.com/kinkyacedev/buttplug-of-legends/blob/master/league_thread.py) translates kills/assists into external-device feedback and stops on death. It targets Intiface/Buttplug-compatible intimate devices, rather than DS4 motors. Archived in 2021; it is not a current compatibility reference.
- [Buttplug-Of-Legends-Unity](https://github.com/Furimanejo/Buttplug-Of-Legends-Unity): a successor Windows/Unity application whose author describes scoring gameplay events and controlling external-device intensity. Its README marks the project deprecated. No code was copied from either project.

The general idea of mapping LoL local-game events to physical feedback has precedents. This module specializes in USB DS4 v2: net health-loss alerts, low HP/resource crossings, kill/assist/multikill pulse patterns, configurable presets and cancellation/expiry handling.

The search did not identify a verified identical package combining this specific one-handed Steam layout and this DS4 feedback implementation. That is a limited search result, not a claim of being the first such project.
