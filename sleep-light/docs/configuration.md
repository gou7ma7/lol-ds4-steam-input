# Configuration and verification

[简体中文](configuration.zh-CN.md)

All settings are compiled from `config/SleepLightConfig.cs`; edit, exit the running watcher, rebuild, then restart it. No runtime configuration file or shared module is needed.

- `RestoreOnResume`: default `true`. Restore the configured color after interactive resume/unlock. `false` leaves the light off until another application writes it.
- `Red`, `Green`, `Blue`: integers `0..255`; default `0,0,255`. For dim blue use `0,0,32`; for purple use `96,0,96`. These are raw RGB levels, not Steam's brightness slider. All-zero also keeps it dark after resume.
- `RestoreDelayMs`: `0..10000`, default `1500`. Delay following interactive resume/unlock so USB can become available. Increasing it helps when the controller enumerates slowly.
- `RestoreRetryWindowMs`: `500..30000`, default `5000`. Retry window after the initial delay. After expiry, wait for the next sleep cycle; no continuous writes.
- `RestoreRetryIntervalMs`: `100..5000`, default `500`. Time between failed restore attempts. Success ends retries immediately.
- `OffWriteAttempts`: `1..3`, default `2`. Brief repetitions during the suspend callback. This does not create a task that runs during sleep.
- `OffRepeatDelayMs`: `0..100`, default `50`. Gap between those repeated off reports.
- `UsbWriteTimeoutMs`: `50..500`, default `200`. Timeout before requesting cancellation of a pending write. The buffer and OVERLAPPED memory remain alive until the OS completes/cancels the operation; a faulty driver can delay cancellation, so this is not an absolute wall-clock bound.
- `EnableLocalLog`: default `true`. Set `false` to stop writing the local rotating log. No identifying device/game data is logged.

Example: keep the original default blue but reduce intensity, and wait longer for USB:

```csharp
internal static readonly int Red = 0, Green = 0, Blue = 32;
internal static readonly int RestoreDelayMs = 2500;
```

Build from the module folder:

```powershell
powershell.exe -NoProfile -File .\scripts\build.ps1 -Language en
```

The build validates ranges and runs synthetic checks for report fields, device capability filtering, duplicate notifications, deferred unlock restoration, pending-restore cancellation, and the S3 → automatic wake → S4 sequence. No native USB call is made by the test runner.

For a real check, keep the watcher running and manually test S3 sleep and S4 hibernate separately. Observe both off and restored color, and check `%LOCALAPPDATA%\DS4SleepLight\events.log`. `suspend_off ... outcome=written` means Windows completed the reports, not that the visible result was independently measured. Background resumes should record `automatic_resume_no_light_write`; no restore should happen until user resume/unlock.

If an application overwrites the light shortly after our command, changing the delay/repetitions may help but is not a guarantee. Do not disable USB power saving, install HID-hiding drivers, or replace a working Steam mapping merely as a default troubleshooting step. Keep any further change tied to observed behavior.
