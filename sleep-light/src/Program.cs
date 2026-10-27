using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DS4SleepLight {
    internal static class Ui {
        internal static string Text(string zh, string en) {
#if UI_ZH
            return zh;
#elif UI_EN
            return en;
#else
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh" ? zh : en;
#endif
        }
    }
    internal static class Log {
        internal static void Write(string action, WriteResult result) {
            if (!SleepLightConfig.EnableLocalLog) return;
            try {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DS4SleepLight");
                Directory.CreateDirectory(folder);
                string file = Path.Combine(folder, "events.log");
                if (File.Exists(file) && new FileInfo(file).Length > 1024 * 1024) { string old = file + ".old"; if (File.Exists(old)) File.Delete(old); File.Move(file, old); }
                string detail = result == null ? "" : " outcome=" + result.Outcome + " matches=" + result.Matches + " writes=" + result.Writes + " win32=" + result.Win32Error;
                File.AppendAllText(file, DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) + " " + action + detail + Environment.NewLine);
            } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
        internal static void SaveDiagnostic(string path, WriteResult result) {
            File.WriteAllText(Path.GetFullPath(path), new JavaScriptSerializer().Serialize(new {
                version = 1, supportedDevice = "USB DS4 v2 054C:09CC", outcome = result.Outcome,
                matchingControllers = result.Matches, reportsWritten = result.Writes, win32Error = result.Win32Error
            }));
        }
    }
    internal sealed class LightApp : Form {
        private readonly SleepPolicy policy = new SleepPolicy(SleepLightConfig.RestoreOnResume);
        private readonly NotifyIcon tray = new NotifyIcon();
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly ToolStripMenuItem status;
        private double restoreAt, restoreDeadline, nextAttempt;
        private bool ending;

        internal LightApp() {
            Text = "DS4SleepLight";
            // Create a top-level HWND so WM_POWERBROADCAST arrives even without a visible window.
            // 创建顶层窗口句柄；不显示窗口，也能接收电源广播。
            IntPtr window = Handle;
            ContextMenuStrip menu = new ContextMenuStrip();
            status = new ToolStripMenuItem(Ui.Text("等待睡眠/休眠", "Waiting for sleep/hibernate")); status.Enabled = false; menu.Items.Add(status);
            ToolStripMenuItem diagnostic = new ToolStripMenuItem(Ui.Text("检查连接（不改变灯光）", "Check connection (no light changes)"));
            diagnostic.Click += delegate { WriteResult result = NativeHid.Diagnose(); Log.Write("diagnose", result); SetStatus(result.Outcome == "output_accessible_no_write" ? Ui.Text("支持的手柄可写入", "Supported controller accessible") : FailureText(result)); };
            menu.Items.Add(diagnostic);
            ToolStripMenuItem exit = new ToolStripMenuItem(Ui.Text("退出", "Exit")); exit.Click += delegate { Close(); }; menu.Items.Add(exit);
            tray.Icon = SystemIcons.Application; tray.ContextMenuStrip = menu; tray.Visible = true;
            SetStatus(status.Text);
            timer.Interval = 100; timer.Tick += delegate { RestoreTick(); }; timer.Start();
            SystemEvents.SessionSwitch += SessionChanged;
            Log.Write("watcher_started_no_light_write", null);
        }
        protected override void SetVisibleCore(bool value) { base.SetVisibleCore(false); }
        protected override void WndProc(ref Message message) {
            if (message.Msg == 0x0218) {
                int notification = message.WParam.ToInt32();
                if (notification == 4) Suspend(); // PBT_APMSUSPEND
                else if (notification == 18) { policy.AutomaticResume(); Log.Write("automatic_resume_no_light_write", null); }
                else if (notification == 7 && policy.UserResume()) ScheduleRestore(); // PBT_APMRESUMESUSPEND
            }
            base.WndProc(ref message);
        }
        private void Suspend() {
            if (!policy.Suspend()) return;
            WriteResult result = NativeHid.Send(LightReport.Make(0, 0, 0), SleepLightConfig.OffWriteAttempts, SleepLightConfig.OffRepeatDelayMs);
            Log.Write("suspend_off", result);
            SetStatus(result.Success ? Ui.Text("已发送灭灯指令", "Light-off command sent") : FailureText(result));
        }
        private void SessionChanged(object sender, SessionSwitchEventArgs e) {
            if (ending) return;
            try {
                BeginInvoke(new Action(delegate {
                    if (e.Reason == SessionSwitchReason.SessionLock) policy.Lock();
                    else if (e.Reason == SessionSwitchReason.SessionUnlock && policy.Unlock()) ScheduleRestore();
                }));
            } catch (InvalidOperationException) { }
        }
        private void ScheduleRestore() {
            restoreAt = clock.Elapsed.TotalMilliseconds + SleepLightConfig.RestoreDelayMs;
            nextAttempt = restoreAt;
            restoreDeadline = restoreAt + SleepLightConfig.RestoreRetryWindowMs;
            SetStatus(Ui.Text("恢复使用，等待 USB 就绪", "User resumed; waiting for USB"));
            Log.Write("interactive_resume_restore_scheduled", null);
        }
        private void RestoreTick() {
            if (policy.Phase != LightPhase.RestorePending || clock.Elapsed.TotalMilliseconds < nextAttempt) return;
            if (clock.Elapsed.TotalMilliseconds > restoreDeadline) {
                policy.CompleteRestore(); SetStatus(Ui.Text("恢复灯色超时，请检查连接", "Color restore timed out; check connection")); Log.Write("restore_window_expired", null); return;
            }
            WriteResult result = NativeHid.Send(LightReport.Make(SleepLightConfig.Red, SleepLightConfig.Green, SleepLightConfig.Blue), 1, 0);
            Log.Write("resume_restore", result);
            if (result.Success) { policy.CompleteRestore(); SetStatus(Ui.Text("已恢复配置灯色，等待睡眠", "Configured color restored; waiting for sleep")); }
            else { nextAttempt = clock.Elapsed.TotalMilliseconds + SleepLightConfig.RestoreRetryIntervalMs; SetStatus(FailureText(result)); }
        }
        private static string FailureText(WriteResult result) {
            if (result.Outcome == "no_supported_device") return Ui.Text("未连接支持的 USB DS4", "Supported USB DS4 not connected");
            if (result.Outcome == "multiple_devices_refused") return Ui.Text("多个手柄，未发送指令", "Multiple controllers; no command sent");
            return Ui.Text("写入未完成，请检查连接/日志", "Write unavailable; check connection/log");
        }
        private void SetStatus(string text) { status.Text = text; string tooltip = "DS4SleepLight: " + text; tray.Text = tooltip.Length > 63 ? tooltip.Substring(0, 63) : tooltip; }
        protected override void OnFormClosing(FormClosingEventArgs e) {
            ending = true; timer.Stop(); SystemEvents.SessionSwitch -= SessionChanged;
            tray.Visible = false; tray.Dispose(); timer.Dispose();
            Log.Write("watcher_stopped_no_light_write", null); base.OnFormClosing(e);
        }
    }
    internal static class Program {
        private const string StopEvent = "Local\\DS4SleepLight_Stop_054C_09CC";
        [STAThread] private static int Main(string[] args) {
            try {
                SleepLightConfig.Validate();
                if (args.Length == 1 && args[0] == "--stop") {
                    try { using (EventWaitHandle stop = EventWaitHandle.OpenExisting(StopEvent)) stop.Set(); } catch (WaitHandleCannotBeOpenedException) { }
                    return 0;
                }
                if (args.Length == 2 && args[0] == "--diagnose") { Log.SaveDiagnostic(args[1], NativeHid.Diagnose()); return 0; }
                if (args.Length == 3 && args[0] == "--test-cycle") {
                    int seconds; if (!int.TryParse(args[1], out seconds) || seconds < 1 || seconds > 30) throw new ArgumentException("Test duration must be 1..30 seconds.");
                    WriteResult off = NativeHid.Send(LightReport.Make(0, 0, 0), SleepLightConfig.OffWriteAttempts, SleepLightConfig.OffRepeatDelayMs);
                    WriteResult restored;
                    try { if (off.Success) Thread.Sleep(seconds * 1000); }
                    finally { restored = NativeHid.Send(LightReport.Make(SleepLightConfig.Red, SleepLightConfig.Green, SleepLightConfig.Blue), 1, 0); }
                    File.WriteAllText(Path.GetFullPath(args[2]), new JavaScriptSerializer().Serialize(new { offWritten = off.Success, configuredColorWritten = restored.Success, offOutcome = off.Outcome, restoreOutcome = restored.Outcome, seconds = seconds }));
                    return off.Success && restored.Success ? 0 : 1;
                }
                if (args.Length != 0) throw new ArgumentException("Usage: DS4SleepLight.exe [--stop | --diagnose file.json | --test-cycle seconds file.json]");
                bool created;
                using (Mutex singleton = new Mutex(true, "Local\\DS4SleepLight_054C_09CC", out created)) {
                    if (!created) return 0;
                    Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                    using (EventWaitHandle stop = new EventWaitHandle(false, EventResetMode.AutoReset, StopEvent))
                    using (LightApp app = new LightApp()) {
                        RegisteredWaitHandle wait = ThreadPool.RegisterWaitForSingleObject(stop, delegate(object state, bool timedOut) { try { app.BeginInvoke(new Action(app.Close)); } catch (InvalidOperationException) { } }, null, -1, true);
                        try { Application.Run(app); } finally { wait.Unregister(null); }
                    }
                }
                return 0;
            } catch (Exception error) {
                Log.Write("fatal_" + error.GetType().Name, null);
                MessageBox.Show(Ui.Text("DS4 灭灯工具无法完成操作：", "DS4 sleep light operation failed: ") + error.Message, "DS4SleepLight"); return 1;
            }
        }
    }
}
