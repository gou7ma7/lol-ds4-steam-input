using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LoLDamageRumble {
    internal static class Hid {
        [StructLayout(LayoutKind.Sequential)] internal struct InterfaceData {
            internal int Size; internal Guid ClassGuid; internal int Flags; internal IntPtr Reserved;
        }
        [StructLayout(LayoutKind.Sequential)] internal struct Overlapped {
            internal UIntPtr Internal, InternalHigh; internal uint Offset, OffsetHigh; internal IntPtr Event;
        }
        [DllImport("hid.dll")] private static extern void HidD_GetHidGuid(out Guid guid);
        [DllImport("setupapi.dll", CharSet=CharSet.Unicode, SetLastError=true)] private static extern IntPtr SetupDiGetClassDevs(ref Guid guid, string enumerator, IntPtr parent, uint flags);
        [DllImport("setupapi.dll", SetLastError=true)] private static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr device, ref Guid guid, uint index, ref InterfaceData data);
        [DllImport("setupapi.dll", CharSet=CharSet.Unicode, SetLastError=true)] private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref InterfaceData data, IntPtr detail, uint length, out uint required, IntPtr device);
        [DllImport("setupapi.dll")] private static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
        [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] private static extern IntPtr CreateFile(string path, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError=true)] private static extern bool WriteFile(IntPtr file, IntPtr buffer, uint count, out uint written, ref Overlapped overlapped);
        [DllImport("kernel32.dll", SetLastError=true)] private static extern bool GetOverlappedResult(IntPtr file, ref Overlapped overlapped, out uint count, bool wait);
        [DllImport("kernel32.dll")] private static extern bool CancelIoEx(IntPtr file, ref Overlapped overlapped);
        [DllImport("kernel32.dll", CharSet=CharSet.Unicode)] private static extern IntPtr CreateEvent(IntPtr security, bool manual, bool initial, string name);
        [DllImport("kernel32.dll")] private static extern uint WaitForSingleObject(IntPtr handle, uint timeout);
        [DllImport("kernel32.dll")] internal static extern bool CloseHandle(IntPtr handle);
        internal static readonly IntPtr Invalid = new IntPtr(-1);

        internal static List<string> Paths() {
            Guid guid; HidD_GetHidGuid(out guid);
            IntPtr set = SetupDiGetClassDevs(ref guid, null, IntPtr.Zero, 0x12);
            List<string> result = new List<string>();
            if (set == Invalid) return result;
            try {
                for (uint index=0; ; index++) {
                    InterfaceData data = new InterfaceData(); data.Size=Marshal.SizeOf(typeof(InterfaceData));
                    if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref guid, index, ref data)) break;
                    uint required;
                    SetupDiGetDeviceInterfaceDetail(set, ref data, IntPtr.Zero, 0, out required, IntPtr.Zero);
                    if (required < 8) continue;
                    IntPtr detail=Marshal.AllocHGlobal((int)required);
                    try {
                        Marshal.WriteInt32(detail, IntPtr.Size==8 ? 8 : 6);
                        if (SetupDiGetDeviceInterfaceDetail(set, ref data, detail, required, out required, IntPtr.Zero)) {
                            string path=Marshal.PtrToStringUni(IntPtr.Add(detail, 4));
                            if (path!=null && path.IndexOf("vid_054c&pid_09cc", StringComparison.OrdinalIgnoreCase)>=0) result.Add(path);
                        }
                    } finally { Marshal.FreeHGlobal(detail); }
                }
            } finally { SetupDiDestroyDeviceInfoList(set); }
            return result;
        }
        internal static IntPtr Open(string path) { return CreateFile(path, 0x40000000, 3, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero); }
        internal static void Motors(IntPtr handle, byte fast, byte slow) {
            byte[] report=new byte[32]; report[0]=5; report[1]=1; report[4]=fast; report[5]=slow;
            // The USB report enables motor output fields only.
            IntPtr ev=CreateEvent(IntPtr.Zero, true, false, null);
            if (ev==IntPtr.Zero) throw new IOException("Cannot create USB completion event.");
            GCHandle pinned=GCHandle.Alloc(report, GCHandleType.Pinned);
            try {
                Overlapped ov=new Overlapped(); ov.Event=ev; uint written;
                bool ok=WriteFile(handle, pinned.AddrOfPinnedObject(), (uint)report.Length, out written, ref ov);
                if (!ok) {
                    if (Marshal.GetLastWin32Error()!=997) throw new IOException("USB motor write failed.");
                    if (WaitForSingleObject(ev, 500)!=0) {
                        CancelIoEx(handle, ref ov); GetOverlappedResult(handle, ref ov, out written, true);
                        throw new IOException("USB motor write timed out.");
                    }
                    if (!GetOverlappedResult(handle, ref ov, out written, false)) throw new IOException("USB motor write completion failed.");
                }
                if (written!=32) throw new IOException("Incomplete USB motor report.");
            } finally { pinned.Free(); CloseHandle(ev); }
        }
    }

    internal static class Json {
        internal static Dictionary<string,object> Object(object value) { return value as Dictionary<string,object>; }
        internal static object Value(Dictionary<string,object> data, string key) { object value; return data!=null && data.TryGetValue(key,out value) ? value : null; }
        internal static string Text(Dictionary<string,object> data, string key) { return Convert.ToString(Value(data,key)); }
        internal static double Number(Dictionary<string,object> data, string key, double fallback) {
            object value=Value(data,key); if(value==null) return fallback;
            try { double number=Convert.ToDouble(value,System.Globalization.CultureInfo.InvariantCulture); return double.IsNaN(number)||double.IsInfinity(number) ? fallback : number; } catch { return fallback; }
        }
        internal static System.Collections.IEnumerable Array(object value) { return value is string ? null : value as System.Collections.IEnumerable; }
    }

    internal sealed class PlayerSnapshot {
        internal double Health, MaxHealth, Resource, MaxResource;
        internal string ResourceType, Identity;
        internal HashSet<string> Names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        internal static PlayerSnapshot Parse(Dictionary<string,object> data) {
            Dictionary<string,object> stats=Json.Object(Json.Value(data,"championStats"));
            double hp=Json.Number(stats,"currentHealth",-1), max=Json.Number(stats,"maxHealth",-1);
            if(max<=0 || hp<0) throw new IOException("No valid active-player health.");
            PlayerSnapshot result=new PlayerSnapshot { Health=hp,MaxHealth=max,Resource=Json.Number(stats,"resourceValue",-1),MaxResource=Json.Number(stats,"resourceMax",-1),ResourceType=Json.Text(stats,"resourceType").ToUpperInvariant() };
            foreach(string key in new string[]{"riotId","summonerName","riotIdGameName"}) {
                string name=Json.Text(data,key); if(name.Length>0) result.Names.Add(name);
            }
            result.Identity=Json.Text(data,"riotId"); if(result.Identity.Length==0) result.Identity=Json.Text(data,"summonerName");
            return result;
        }
    }

    internal static class LiveClient {
        internal static string LastError="";
        internal static Dictionary<string,object> Get(string endpoint) {
            // Endpoint names are internal constants; the certificate exception is confined to this loopback service.
            HttpWebRequest req=(HttpWebRequest)WebRequest.Create("https://127.0.0.1:2999/liveclientdata/"+endpoint);
            req.Proxy=null; req.AllowAutoRedirect=false; req.Timeout=FeedbackConfig.Current.Polling.HttpTimeoutMs; req.ReadWriteTimeout=req.Timeout;
            req.ServerCertificateValidationCallback=delegate { return true; };
            using(HttpWebResponse response=(HttpWebResponse)req.GetResponse())
            using(StreamReader reader=new StreamReader(response.GetResponseStream())) {
                return new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(reader.ReadToEnd());
            }
        }
        internal static bool Player(out PlayerSnapshot player) {
            player=null;
            try { player=PlayerSnapshot.Parse(Get("activeplayer")); LastError=""; return true; }
            catch(Exception ex) { LastError=ex.GetType().Name+": "+ex.Message; return false; }
        }
        internal static bool Events(out List<Dictionary<string,object>> events) {
            events=new List<Dictionary<string,object>>();
            try {
                System.Collections.IEnumerable items=Json.Array(Json.Value(Get("eventdata"),"Events"));
                if(items==null) return false;
                foreach(object value in items) { Dictionary<string,object> item=Json.Object(value); if(item!=null) events.Add(item); }
                return true;
            } catch { return false; }
        }
    }

    internal enum FeedbackKind { HeavyDamage, LowHealth, LowResource, Kill, Assist, Multikill }
    internal sealed class Feedback {
        internal FeedbackKind Kind; internal double Magnitude; internal int Count;
        internal Feedback(FeedbackKind kind,double magnitude,int count) { Kind=kind; Magnitude=magnitude; Count=count; }
    }

    // Deterministic state detection, independent of HTTP and physical motors.
    internal sealed class FeedbackDetector {
        private sealed class HealthSample { internal double At,Health; internal HealthSample(double at,double hp) { At=at; Health=hp; } }
        private readonly Queue<HealthSample> history=new Queue<HealthSample>();
        private readonly DetectionSettings cfg;
        internal FeedbackDetector(DetectionSettings settings=null) { cfg=settings ?? FeedbackConfig.Current.Detection; }
        private PlayerSnapshot previous;
        private bool healthArmed,resourceArmed;
        private double lastHeavy=-100,lastHealth=-100,lastResource=-100,lastSample=-100;
        private bool eventsInitialized;
        private int lastEventId=-1;
        private double lastEventTime=-1;
        internal void Reset() {
            previous=null; history.Clear(); healthArmed=resourceArmed=false;
            lastHeavy=lastHealth=lastResource=lastSample=-100;
            eventsInitialized=false; lastEventId=-1; lastEventTime=-1;
        }
        internal void ResetEvents() { eventsInitialized=false; lastEventId=-1; lastEventTime=-1; }
        internal List<Feedback> Player(PlayerSnapshot current,double now) {
            List<Feedback> result=new List<Feedback>();
            if(previous==null || !String.Equals(previous.Identity,current.Identity,StringComparison.OrdinalIgnoreCase) || Math.Abs(previous.MaxHealth-current.MaxHealth)>cfg.MaxHealthChangeTolerance || now-lastSample>cfg.MaxSampleGapSeconds || now<lastSample) {
                history.Clear(); healthArmed=current.Health/current.MaxHealth>cfg.LowHealthRatio;
                resourceArmed=ResourceSupported(current) && current.Resource/current.MaxResource>cfg.LowResourceRatio;
            } else {
                while(history.Count>0 && now-history.Peek().At>cfg.HeavyWindowSeconds+1e-9) history.Dequeue();
                double peak=current.Health; foreach(HealthSample sample in history) peak=Math.Max(peak,sample.Health);
                double loss=(peak-current.Health)/current.MaxHealth;
                if(previous.Health>0 && loss>=cfg.HeavyDamageRatio-1e-9 && now-lastHeavy>=cfg.HeavyCooldownSeconds) {
                    result.Add(new Feedback(FeedbackKind.HeavyDamage,loss,1)); lastHeavy=now; history.Clear();
                }
                if(current.Health>0) {
                    double ratio=current.Health/current.MaxHealth;
                    if(ratio>=cfg.HealthRearmRatio) healthArmed=true;
                    if(healthArmed && ratio<=cfg.LowHealthRatio) {
                        healthArmed=false;
                        if(now-lastHealth>=cfg.LowHealthCooldownSeconds) { result.Add(new Feedback(FeedbackKind.LowHealth,ratio,1)); lastHealth=now; }
                    }
                    bool sameResource=ResourceSupported(current) && ResourceSupported(previous) && current.ResourceType==previous.ResourceType && Math.Abs(current.MaxResource-previous.MaxResource)<=cfg.MaxResourceChangeTolerance;
                    if(!sameResource) resourceArmed=ResourceSupported(current) && current.Resource/current.MaxResource>cfg.LowResourceRatio;
                    else {
                        double resourceRatio=current.Resource/current.MaxResource;
                        if(resourceRatio>=cfg.ResourceRearmRatio) resourceArmed=true;
                        if(resourceArmed && resourceRatio<=cfg.LowResourceRatio) {
                            resourceArmed=false;
                            if(now-lastResource>=cfg.LowResourceCooldownSeconds) { result.Add(new Feedback(FeedbackKind.LowResource,resourceRatio,1)); lastResource=now; }
                        }
                    }
                } else { healthArmed=resourceArmed=false; history.Clear(); }
            }
            history.Enqueue(new HealthSample(now,current.Health)); previous=current; lastSample=now;
            return result;
        }
        private bool ResourceSupported(PlayerSnapshot player) {
            return (Array.IndexOf(cfg.ResourceTypes,player.ResourceType)>=0) && player.MaxResource>0 && player.Resource>=0;
        }
        internal List<Feedback> Events(List<Dictionary<string,object>> events,HashSet<string> names) {
            List<Feedback> result=new List<Feedback>(); int highId=-1; double highTime=-1;
            foreach(Dictionary<string,object> item in events) { highId=Math.Max(highId,(int)Json.Number(item,"EventID",-1)); highTime=Math.Max(highTime,Json.Number(item,"EventTime",-1)); }
            if(!eventsInitialized || highId<lastEventId || highTime<lastEventTime) {
                eventsInitialized=true; lastEventId=highId; lastEventTime=highTime; return result;
            }
            int kills=0,assists=0,multi=0;
            foreach(Dictionary<string,object> item in events) {
                int id=(int)Json.Number(item,"EventID",-1);
                if(id<=lastEventId) continue;
                string eventName=Json.Text(item,"EventName");
                bool mine=names.Contains(Json.Text(item,"KillerName"));
                if(eventName=="ChampionKill") {
                    if(mine) kills++;
                    else {
                        System.Collections.IEnumerable list=Json.Array(Json.Value(item,"Assisters"));
                        if(list!=null) foreach(object name in list) if(names.Contains(Convert.ToString(name))) { assists++; break; }
                    }
                } else if(eventName=="Multikill" && mine) multi=Math.Max(multi,(int)Json.Number(item,"KillStreak",0));
            }
            lastEventId=highId; lastEventTime=highTime;
            // A multikill supersedes the ordinary kill feedback from the same event batch.
            if(multi>=2) result.Add(new Feedback(FeedbackKind.Multikill,0,Math.Min(5,multi)));
            else if(kills>0) result.Add(new Feedback(FeedbackKind.Kill,0,1));
            if(assists>0 && kills==0 && multi<2) result.Add(new Feedback(FeedbackKind.Assist,0,1));
            return result;
        }
    }

    internal sealed class MotorStep {
        internal int Duration; internal double Fast,Slow;
        internal MotorStep(int duration,double fast,double slow) { Duration=duration; Fast=fast; Slow=slow; }
    }
    internal sealed class Pattern {
        internal FeedbackKind Kind; internal string Label; internal int Priority;
        internal MotorStep[] Steps;
        internal static Pattern Make(Feedback feedback, RumbleSettings settings=null) {
            RumbleSettings cfg=settings ?? FeedbackConfig.Current;
            EffectRule rule=cfg.Effects[feedback.Kind];
            int count=rule.RepeatForKillCount ? Math.Max(2,Math.Min(5,feedback.Count)) : rule.RepeatCount;
            string label=Ui.Text(rule.LabelZh,rule.LabelEn);
            if(feedback.Kind==FeedbackKind.Multikill) {
                string[] zh={"","","双杀","三杀","四杀","五杀"};
                string[] en={"","","Double kill","Triple kill","Quadra kill","Penta kill"};
                label=Ui.Text(zh[Math.Max(2,Math.Min(5,feedback.Count))],en[Math.Max(2,Math.Min(5,feedback.Count))]);
            }
            List<MotorStep> steps=new List<MotorStep>();
            for(int i=0;i<count;i++) {
                if(i>0 && rule.RepeatGapMs>0) steps.Add(new MotorStep(rule.RepeatGapMs,0,0));
                foreach(MotorStep step in rule.Steps) {
                    double slow=step.Slow;
                    if(feedback.Kind==FeedbackKind.HeavyDamage && (step.Fast>0 || step.Slow>0))
                        slow=Math.Min(1,slow+Math.Max(0,feedback.Magnitude-cfg.Detection.HeavyDamageRatio)*rule.DamageSlowGain);
                    steps.Add(new MotorStep(step.Duration,step.Fast,slow));
                }
            }
            return new Pattern { Kind=feedback.Kind,Label=label,Priority=rule.Priority,Steps=steps.ToArray() };
        }
    }

    // Each pattern owns the motors until completion. Important feedback can interrupt a lower-priority pattern.
    internal sealed class PatternScheduler {
        private sealed class Pending { internal Pattern Pattern; internal double Expires; }
        private readonly List<Pending> pending=new List<Pending>();
        private readonly Action<double,double> write;
        private readonly Action<Pattern> started;
        private readonly SchedulingSettings cfg;
        private Pattern active; private int step; private double deadline;
        internal bool MotorsOn { get; private set; }
        internal PatternScheduler(Action<double,double> output,Action<Pattern> onStart,SchedulingSettings settings=null) { write=output; started=onStart; cfg=settings ?? FeedbackConfig.Current.Scheduling; }
        internal void Enqueue(Pattern pattern,double now) {
            pending.RemoveAll(delegate(Pending item) { return item.Expires<now || item.Pattern.Kind==pattern.Kind; });
            if(active!=null && pattern.Priority>active.Priority) { active=null; write(0,0); MotorsOn=false; }
            pending.Add(new Pending { Pattern=pattern,Expires=now+cfg.QueueExpirySeconds });
            if(pending.Count>cfg.QueueCapacity) pending.RemoveAt(0);
            Tick(now);
        }
        internal void Tick(double now) {
            if(active!=null && now>=deadline) {
                // Never rush multiple overdue steps after a stall; cancel instead of replaying stale pulses.
                if(now-deadline>cfg.MaxLatenessSeconds) { active=null; write(0,0); MotorsOn=false; }
                else { step++; if(step>=active.Steps.Length) { active=null; write(0,0); MotorsOn=false; } else ApplyStep(now); }
            }
            if(active!=null) return;
            pending.RemoveAll(delegate(Pending item) { return item.Expires<now; });
            if(pending.Count==0) return;
            int best=0; for(int i=1;i<pending.Count;i++) if(pending[i].Pattern.Priority>pending[best].Pattern.Priority) best=i;
            active=pending[best].Pattern; pending.RemoveAt(best); step=0;
            ApplyStep(now); if(started!=null) started(active);
        }
        private void ApplyStep(double now) {
            MotorStep current=active.Steps[step]; write(current.Fast,current.Slow);
            MotorsOn=current.Fast>0 || current.Slow>0; deadline=now+current.Duration/1000.0;
        }
        internal void Cancel() { pending.Clear(); active=null; write(0,0); MotorsOn=false; }
    }

    internal sealed class RumbleApp : Form {
        private readonly NotifyIcon tray=new NotifyIcon();
        private readonly Icon appIcon=Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? (Icon)SystemIcons.Application.Clone();
        private readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();
        private readonly ToolStripMenuItem toggle=new ToolStripMenuItem(Ui.Text("暂停全部反馈","Pause all feedback"));
        private readonly ToolStripMenuItem status=new ToolStripMenuItem(Ui.Text("等待比赛数据","Waiting for match data"));
        private readonly ToolStripMenuItem counter=new ToolStripMenuItem(Ui.Text("已触发：0 次","Triggered: 0"));
        private readonly string statusFile;
        private readonly System.Diagnostics.Stopwatch clock=System.Diagnostics.Stopwatch.StartNew();
        private readonly PatternScheduler scheduler;
        private readonly Dictionary<FeedbackKind,int> counts=new Dictionary<FeedbackKind,int>();
        private readonly HashSet<FeedbackKind> enabledKinds=new HashSet<FeedbackKind>((FeedbackKind[])Enum.GetValues(typeof(FeedbackKind)));
        private IntPtr controller=Hid.Invalid;
        private volatile bool ending,suspended,locked,enabled=true,apiAvailable,eventsAvailable;
        private long reset;
        private double intensity=FeedbackConfig.Current.MasterIntensity,nextConnect,nextStatus;
        private int pulses; private string lastFeedback="";

        internal RumbleApp(string diagnosticStatusFile) {
            statusFile=diagnosticStatusFile;
            Text=Ui.Text("LoL 手柄反馈","LoL rumble feedback"); ShowInTaskbar=false; WindowState=FormWindowState.Minimized;
            IntPtr hwnd=Handle;
            scheduler=new PatternScheduler(WriteMotors,delegate(Pattern pattern) {
                pulses++; lastFeedback=pattern.Label;
                int count; counts.TryGetValue(pattern.Kind,out count); counts[pattern.Kind]=count+1;
                counter.Text=Ui.Text("已触发：","Triggered: ")+pulses+Ui.Text(" 次",""); State(Ui.Text("反馈：","Feedback: ")+pattern.Label);
            });
            ContextMenuStrip menu=new ContextMenuStrip(); status.Enabled=false; counter.Enabled=false;
            menu.Items.Add(status); menu.Items.Add(counter); menu.Items.Add(new ToolStripSeparator());
            toggle.Click+=delegate { enabled=!enabled; toggle.Text=enabled?Ui.Text("暂停全部反馈","Pause all feedback"):Ui.Text("启用全部反馈","Enable all feedback"); Interlocked.Increment(ref reset); CancelFeedback(); State(enabled?Ui.Text("等待比赛数据","Waiting for match data"):Ui.Text("已暂停","Paused")); }; menu.Items.Add(toggle);
            ToolStripMenuItem kinds=new ToolStripMenuItem(Ui.Text("反馈开关","Enable effects"));
            ToolStripMenuItem tests=new ToolStripMenuItem(Ui.Text("测试不同振动","Test vibration patterns"));
            foreach(FeedbackKind kind in Enum.GetValues(typeof(FeedbackKind))) {
                FeedbackKind captured=kind; Pattern pattern=Pattern.Make(new Feedback(kind,.15,3));
                ToolStripMenuItem choice=new ToolStripMenuItem(kind==FeedbackKind.Multikill?Ui.Text("连杀（双杀至五杀）","Multikill (2 to 5)"):pattern.Label); choice.Checked=FeedbackConfig.Current.Effects[kind].Enabled; if(!choice.Checked) enabledKinds.Remove(kind); choice.CheckOnClick=true;
                choice.CheckedChanged+=delegate { if(choice.Checked) enabledKinds.Add(captured); else enabledKinds.Remove(captured); CancelFeedback(); };
                kinds.DropDownItems.Add(choice);
                ToolStripMenuItem test=new ToolStripMenuItem(pattern.Label); test.Click+=delegate { CancelFeedback(); Submit(new Feedback(captured,.15,3),true); }; tests.DropDownItems.Add(test);
            }
            menu.Items.Add(kinds); menu.Items.Add(tests);
            DetectionSettings d=FeedbackConfig.Current.Detection;
            ToolStripMenuItem preset=new ToolStripMenuItem(Ui.Text("预设：", "Preset: ")+FeedbackConfig.SelectedPreset); preset.Enabled=false; menu.Items.Add(preset);
            ToolStripMenuItem thresholds=new ToolStripMenuItem(Ui.Text("阈值：掉血 / 低血 / 低资源 ","Thresholds: damage / HP / resource ")+(100*d.HeavyDamageRatio).ToString("0.#",CultureInfo.InvariantCulture)+"% / "+(100*d.LowHealthRatio).ToString("0.#",CultureInfo.InvariantCulture)+"% / "+(100*d.LowResourceRatio).ToString("0.#",CultureInfo.InvariantCulture)+"%"); thresholds.Enabled=false; menu.Items.Add(thresholds);
            ToolStripMenuItem levels=new ToolStripMenuItem(Ui.Text("振动强度","Intensity"));
            List<double> choices=new List<double>(FeedbackConfig.Current.IntensityChoices);
            if(!choices.Contains(intensity)) choices.Add(intensity);
            foreach(double percentage in choices) {
                double value=percentage; ToolStripMenuItem level=new ToolStripMenuItem((100*value).ToString("0.#",CultureInfo.InvariantCulture)+"%"); level.Checked=value==intensity;
                level.Click+=delegate { intensity=value; foreach(ToolStripMenuItem item in levels.DropDownItems) item.Checked=item==level; }; levels.DropDownItems.Add(level);
            }
            menu.Items.Add(levels); ToolStripMenuItem exit=new ToolStripMenuItem(Ui.Text("退出并停止振动","Exit and stop vibration")); exit.Click+=delegate { Close(); }; menu.Items.Add(exit);
            tray.Icon=appIcon; tray.Text=Ui.Text("LoL 手柄反馈：等待比赛","LoL rumble: waiting for match"); tray.ContextMenuStrip=menu; tray.Visible=true;
            timer.Interval=FeedbackConfig.Current.Polling.MotorTimerMs; timer.Tick+=delegate {
                try { scheduler.Tick(clock.Elapsed.TotalSeconds); } catch(IOException) { ControllerFailure(); }
                if(statusFile!=null && clock.Elapsed.TotalSeconds>=nextStatus) { nextStatus=clock.Elapsed.TotalSeconds+FeedbackConfig.Current.Polling.StatusIntervalSeconds; WriteStatus(true); }
            }; timer.Start(); SystemEvents.SessionSwitch+=SessionChanged;
            Thread worker=new Thread(Poll); worker.IsBackground=true; worker.Start();
        }
        protected override void SetVisibleCore(bool value) { base.SetVisibleCore(false); }
        protected override void WndProc(ref Message m) {
            if(m.Msg==0x218) {
                int eventId=m.WParam.ToInt32();
                if(eventId==4) { suspended=true; Interlocked.Increment(ref reset); CancelFeedback(); }
                if(eventId==7 || eventId==18) { suspended=false; Interlocked.Increment(ref reset); }
            }
            base.WndProc(ref m);
        }
        private void SessionChanged(object sender,SessionSwitchEventArgs e) {
            if(e.Reason==SessionSwitchReason.SessionLock) { locked=true; Interlocked.Increment(ref reset); Post(delegate { CancelFeedback(); }); }
            else if(e.Reason==SessionSwitchReason.SessionUnlock) { locked=false; Interlocked.Increment(ref reset); }
        }
        private void Post(Action action) { if(!ending) try { BeginInvoke(action); } catch(InvalidOperationException) {} }
        private void State(string text) {
            if(status.Text!=text) {
                status.Text=text;
                string tooltip=Ui.Text("LoL 手柄反馈：","LoL rumble: ")+text;
                tray.Text=tooltip.Length>63 ? tooltip.Substring(0,63) : tooltip;
            }
        }
        private bool Connect() {
            if(controller!=Hid.Invalid) return true;
            double now=clock.Elapsed.TotalSeconds; if(now<nextConnect) return false; nextConnect=now+FeedbackConfig.Current.Polling.ReconnectSeconds;
            List<string> paths=Hid.Paths(); if(paths.Count!=1) return false;
            controller=Hid.Open(paths[0]); return controller!=Hid.Invalid;
        }
        private void CloseController() { if(controller!=Hid.Invalid) { Hid.CloseHandle(controller); controller=Hid.Invalid; } }
        private void WriteMotors(double fast,double slow) {
            if(fast==0 && slow==0) { if(controller!=Hid.Invalid) Hid.Motors(controller,0,0); return; }
            if(!Connect()) throw new IOException("Target USB controller unavailable.");
            Hid.Motors(controller,(byte)Math.Round(255*intensity*fast),(byte)Math.Round(255*intensity*slow));
        }
        private void CancelFeedback() { try { scheduler.Cancel(); } catch(IOException) { CloseController(); scheduler.Cancel(); } }
        private void ControllerFailure() { CloseController(); scheduler.Cancel(); State(Ui.Text("未连接目标手柄","Target controller unavailable")); }
        private void Submit(Feedback feedback,bool test) {
            if(ending || suspended || locked || (!test && (!enabled || !enabledKinds.Contains(feedback.Kind)))) return;
            try { scheduler.Enqueue(Pattern.Make(feedback),clock.Elapsed.TotalSeconds); } catch(IOException) { ControllerFailure(); }
        }
        private void Poll() {
            FeedbackDetector detector=new FeedbackDetector(); long revision=Interlocked.Read(ref reset);
            bool lastAvailable=false; double nextEvent=0,nextState=0;
            while(!ending) {
                if(!enabled || suspended || locked) { detector.Reset(); lastAvailable=false; Thread.Sleep(FeedbackConfig.Current.Polling.PlayerIntervalMs); continue; }
                long currentRevision=Interlocked.Read(ref reset);
                if(currentRevision!=revision) { detector.Reset(); revision=currentRevision; nextEvent=0; }
                PlayerSnapshot player; bool available=LiveClient.Player(out player); apiAvailable=available;
                double now=clock.Elapsed.TotalSeconds;
                if(!available) {
                    detector.Reset(); eventsAvailable=false;
                    if(lastAvailable || now>=nextState) { Post(delegate { CancelFeedback(); CloseController(); State(Ui.Text("等待比赛数据","Waiting for match data")); }); nextState=now+5; }
                } else {
                    if(!lastAvailable) { nextEvent=0; Post(delegate { State(Ui.Text("正在监测比赛反馈","Monitoring match feedback")); }); }
                    List<Feedback> feedback=detector.Player(player,now);
                    foreach(Feedback item in feedback) { Feedback captured=item; long version=currentRevision; Post(delegate { if(version==Interlocked.Read(ref reset)) Submit(captured,false); }); }
                    if(now>=nextEvent) {
                        List<Dictionary<string,object>> events; eventsAvailable=LiveClient.Events(out events);
                        nextEvent=clock.Elapsed.TotalSeconds+FeedbackConfig.Current.Polling.EventIntervalSeconds;
                        if(eventsAvailable) foreach(Feedback item in detector.Events(events,player.Names)) {
                            Feedback captured=item; long version=currentRevision; Post(delegate { if(version==Interlocked.Read(ref reset)) Submit(captured,false); });
                        }
                        else detector.ResetEvents();
                    }
                }
                lastAvailable=available; Thread.Sleep(FeedbackConfig.Current.Polling.PlayerIntervalMs);
            }
        }
        private void WriteStatus(bool running) {
            if(statusFile==null) return;
            Dictionary<string,int> feedbackCounts=new Dictionary<string,int>(); foreach(KeyValuePair<FeedbackKind,int> pair in counts) feedbackCounts[pair.Key.ToString()]=pair.Value;
            try { File.WriteAllText(statusFile,new JavaScriptSerializer().Serialize(new { running=running,version=3,preset=FeedbackConfig.SelectedPreset,liveClientAvailable=apiAvailable,eventDataAvailable=eventsAvailable,feedbackEnabled=enabled,pulsesWritten=pulses,motorsOn=scheduler.MotorsOn,state=status.Text,lastFeedback=lastFeedback,feedbackCounts=feedbackCounts })); } catch(IOException) {} catch(UnauthorizedAccessException) {}
        }
        protected override void OnFormClosing(FormClosingEventArgs e) {
            ending=true; Interlocked.Increment(ref reset); timer.Stop(); CancelFeedback(); CloseController(); WriteStatus(false);
            SystemEvents.SessionSwitch-=SessionChanged; tray.Visible=false; tray.Dispose(); appIcon.Dispose(); timer.Dispose(); base.OnFormClosing(e);
        }
    }

    internal static class Program {
        [STAThread] private static void Main(string[] args) {
            FeedbackConfig.Current.Validate();
            ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
            if(args.Length==1 && args[0]=="--stop") {
                try { using(EventWaitHandle stop=EventWaitHandle.OpenExisting("Local\\LoLDamageRumble_Stop_054C_09CC")) stop.Set(); } catch(WaitHandleCannotBeOpenedException) {} return;
            }
            if(args.Length==2 && args[0]=="--diagnose") {
                PlayerSnapshot player; bool live=LiveClient.Player(out player); List<Dictionary<string,object>> events; bool eventApi=LiveClient.Events(out events);
                List<string> paths=Hid.Paths(); bool writable=false;
                if(paths.Count==1) { IntPtr h=Hid.Open(paths[0]); writable=h!=Hid.Invalid; if(writable) Hid.CloseHandle(h); }
                File.WriteAllText(args[1],new JavaScriptSerializer().Serialize(new { version=3,preset=FeedbackConfig.SelectedPreset,liveClientAvailable=live,validHealth=live,eventDataAvailable=eventApi,resourceType=live?player.ResourceType:"",matchingControllers=paths.Count,hidOutputAccessible=writable,apiError=LiveClient.LastError })); return;
            }
            if(args.Length==2 && args[0]=="--test") {
                List<string> paths=Hid.Paths(); if(paths.Count!=1) throw new IOException("Expected one target controller.");
                IntPtr h=Hid.Open(paths[0]); if(h==Hid.Invalid) throw new IOException("Cannot open controller output.");
                try { Hid.Motors(h,85,85); Thread.Sleep(150); }
                finally { try { Hid.Motors(h,0,0); } finally { Hid.CloseHandle(h); } }
                File.WriteAllText(args[1],"{\"motorOnAndOffWritten\":true,\"pulseMilliseconds\":150}"); return;
            }
            bool created; using(Mutex singleton=new Mutex(true,"Local\\LoLDamageRumble_054C_09CC",out created)) {
                if(!created) { MessageBox.Show(Ui.Text("程序已在运行，请查看系统托盘。","Already running. Check the system tray."),Ui.Text("LoL 手柄反馈","LoL rumble feedback")); return; }
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                string statusFile=args.Length==2 && args[0]=="--status-file" ? Path.GetFullPath(args[1]) : null;
                using(EventWaitHandle stop=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\LoLDamageRumble_Stop_054C_09CC"))
                using(RumbleApp app=new RumbleApp(statusFile)) {
                    RegisteredWaitHandle wait=ThreadPool.RegisterWaitForSingleObject(stop,delegate(object state,bool timedOut) { try { app.BeginInvoke(new Action(app.Close)); } catch(InvalidOperationException) {} },null,-1,true);
                    try { Application.Run(app); } finally { wait.Unregister(null); }
                }
            }
        }
    }
}
