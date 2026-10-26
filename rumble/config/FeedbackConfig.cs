using System;
using System.Collections.Generic;
using System.Globalization;

namespace LoLDamageRumble {
    // EDIT THIS FILE, then rebuild. 本文件是编译时配置；修改后重新编译，不需要安装其他工具。
    // 0.25 means 25%, .50 seconds means 500 ms. 比例不是百分数整数；时间字段的单位写在名称中。
    // These presets change feedback only; they never change Steam/game key bindings.
    internal static class FeedbackConfig {
#if PRESET_GENTLE
        internal const string SelectedPreset = "gentle";
#elif PRESET_FOCUS
        internal const string SelectedPreset = "focus";
#elif PRESET_CELEBRATION
        internal const string SelectedPreset = "celebration";
#else
        internal const string SelectedPreset = "default";
#endif
        internal static readonly RumbleSettings Current = Create(SelectedPreset);
        internal static readonly string[] PresetNames = { "default", "gentle", "focus", "celebration" };

        internal static RumbleSettings Create(string name) {
            RumbleSettings c = Default(); // Each call returns an independent copy. 每次创建独立配置，不共享可变对象。
            c.Name = name;
            switch (name) {
                case "default": break; // 用户当前方案 / the user's current setup, preserved exactly.
                case "gentle": // 长时间游戏 / longer sessions: lighter output, fewer celebrations.
                    c.MasterIntensity = .45;
                    c.Effects[FeedbackKind.Assist].Enabled = false;
                    c.Effects[FeedbackKind.Kill].Steps = new MotorStep[] { new MotorStep(110, .55, .15) };
                    c.Effects[FeedbackKind.HeavyDamage].Steps = new MotorStep[] { new MotorStep(180, .50, .65) };
                    c.Effects[FeedbackKind.HeavyDamage].DamageSlowGain = .5;
                    break;
                case "focus": // 关键提醒 / critical alerts: no kill/assist/multikill celebration.
                    c.MasterIntensity = .55;
                    c.Effects[FeedbackKind.Kill].Enabled = false;
                    c.Effects[FeedbackKind.Assist].Enabled = false;
                    c.Effects[FeedbackKind.Multikill].Enabled = false;
                    c.Detection.HeavyDamageRatio = .15;
                    c.Detection.LowHealthRatio = .20;
                    c.Detection.LowResourceRatio = .15;
                    break;
                case "celebration": // 击杀庆祝 / celebration: no damage or resource warnings.
                    c.MasterIntensity = .65;
                    c.Effects[FeedbackKind.HeavyDamage].Enabled = false;
                    c.Effects[FeedbackKind.LowHealth].Enabled = false;
                    c.Effects[FeedbackKind.LowResource].Enabled = false;
                    break;
                default: throw new ArgumentException("Unknown preset: " + name);
            }
            c.Validate();
            return c;
        }

        private static RumbleSettings Default() {
            RumbleSettings c = new RumbleSettings();
            // 最终马达值 = 255 × 总强度 × 每一步的马达强度，四舍五入。
            // Final motor byte = round(255 * MasterIntensity * step strength). Range: 0..1.
            c.MasterIntensity = .65;
            c.IntensityChoices = new double[] { .45, .65, .85 }; // 托盘临时选项 / temporary tray choices; include your default if desired.

            DetectionSettings d = c.Detection;
            d.HeavyWindowSeconds = .50;     // 峰值到当前血量的净下降窗口 / rolling peak-to-current net-loss window.
            d.HeavyDamageRatio = .10;       // 重震门槛，按最大生命值比例 / heavy-hit threshold as fraction of maximum HP.
            d.HeavyCooldownSeconds = .65;   // 防止密集受击连续震动 / minimum interval between heavy-hit triggers.
            d.LowHealthRatio = .25;         // 进入此比例或以下才提醒 / warn when entering this ratio or below.
            d.HealthRearmRatio = .35;       // 回升至此比例才重新允许提醒 / recovery ratio needed to rearm.
            d.LowHealthCooldownSeconds = 8; // 再次提醒的最小间隔 / minimum time between low-HP alerts.
            d.LowResourceRatio = .20;       // 蓝量/能量不足门槛 / low mana or energy threshold.
            d.ResourceRearmRatio = .35;     // 资源恢复门槛 / mana or energy recovery ratio.
            d.LowResourceCooldownSeconds = 8;
            d.ResourceTypes = new string[] { "MANA", "ENERGY" }; // 可选其中一种；不接受怒气/热量/弹药。 Choose either/both; other resource types require new detection logic.
            d.MaxSampleGapSeconds = .80;    // 读取中断超过此值时重建基准 / gaps beyond this reset the baseline.
            d.MaxHealthChangeTolerance = .5; // 最大生命值变化容差，单位 HP / maximum-HP change tolerance, in HP points.
            d.MaxResourceChangeTolerance = .5; // 资源上限变化容差 / maximum-resource change tolerance, in resource points.

            SchedulingSettings s = c.Scheduling;
            s.QueueCapacity = 6;           // 待处理反馈上限；同类新反馈替换旧项 / max queued effects; newer same-kind events replace older queued ones.
            s.QueueExpirySeconds = 2;      // 太晚的反馈丢弃 / drop queued feedback after this age.
            s.MaxLatenessSeconds = .35;    // UI 定时器严重迟到时取消 / cancel a running sequence if the timer is this late.

            PollingSettings p = c.Polling; // 高级设置；通常无需修改 / advanced tuning, usually leave unchanged.
            p.PlayerIntervalMs = 100;      // 请求完成后的等待，不是严格采样频率 / sleep after the request, not a guaranteed sampling rate.
            p.EventIntervalSeconds = .4;   // 击杀等事件读取间隔 / event polling interval after each request.
            p.HttpTimeoutMs = 700;         // 固定本地 API 请求超时 / timeout for the fixed loopback API.
            p.MotorTimerMs = 20;           // 托盘线程调度 tick / motor scheduler timer tick.
            p.ReconnectSeconds = 1;        // USB 重新连接尝试间隔 / USB reconnect retry interval.
            p.StatusIntervalSeconds = 2;   // 可选状态文件更新间隔 / optional status-file update interval.

            // Rule(enabled, priority, 中文名, English name, steps...). All six effects are enabled by default.
            // MotorStep(durationMs, fast, slow): 快马达细碎，慢马达低沉。Both strengths are 0..1.
            // A (0,0) step is a deliberate pause. 两个强度为 0 表示间隔，duration 仍必须大于 0。
            // Priority: higher can interrupt lower. 高优先级可打断低优先级，不会把马达强度叠加。
            c.Effects[FeedbackKind.HeavyDamage] = Rule(true, 100, "大量掉血", "Heavy damage", new MotorStep(220, .75, .82));
            // Default slow strength = min(1, .82 + max(0, lossRatio - .10) * 1).
            // 动态强度以当前 HeavyDamageRatio 为起点；设为 0 可以禁用掉血量加成。
            c.Effects[FeedbackKind.HeavyDamage].DamageSlowGain = 1;
            c.Effects[FeedbackKind.LowHealth] = Rule(true, 80, "低血量", "Low health", new MotorStep(160, .25, 1));
            c.Effects[FeedbackKind.LowHealth].RepeatCount = 2;
            c.Effects[FeedbackKind.LowHealth].RepeatGapMs = 100;
            c.Effects[FeedbackKind.LowResource] = Rule(true, 40, "低蓝量 / 能量", "Low mana / energy", new MotorStep(80, .9, 0));
            c.Effects[FeedbackKind.LowResource].RepeatCount = 3;
            c.Effects[FeedbackKind.LowResource].RepeatGapMs = 110;
            c.Effects[FeedbackKind.Kill] = Rule(true, 60, "击杀", "Kill", new MotorStep(90, .9, .2), new MotorStep(80, 0, 0), new MotorStep(150, 1, .45));
            c.Effects[FeedbackKind.Assist] = Rule(true, 50, "助攻", "Assist", new MotorStep(150, .6, .15));
            c.Effects[FeedbackKind.Multikill] = Rule(true, 70, "连杀", "Multikill", new MotorStep(110, 1, .6));
            c.Effects[FeedbackKind.Multikill].RepeatForKillCount = true; // 2..5 次，按事件连杀数 / repeat 2..5 times according to the event.
            c.Effects[FeedbackKind.Multikill].RepeatGapMs = 80;
            return c;
        }
        private static EffectRule Rule(bool enabled, int priority, string zh, string en, params MotorStep[] steps) {
            return new EffectRule { Enabled=enabled, Priority=priority, LabelZh=zh, LabelEn=en, Steps=steps };
        }
    }

    // Configuration models and validation. 参数模型和范围检查；正常定制只修改上面的 Default/Create。
    internal sealed class RumbleSettings {
        internal string Name;
        internal double MasterIntensity;
        internal double[] IntensityChoices;
        internal readonly DetectionSettings Detection = new DetectionSettings();
        internal readonly SchedulingSettings Scheduling = new SchedulingSettings();
        internal readonly PollingSettings Polling = new PollingSettings();
        internal readonly Dictionary<FeedbackKind,EffectRule> Effects = new Dictionary<FeedbackKind,EffectRule>();
        internal void Validate() {
            Range(MasterIntensity, 0, 1, "MasterIntensity");
            if(IntensityChoices==null || IntensityChoices.Length<1 || IntensityChoices.Length>8) throw new ArgumentException("IntensityChoices: 1..8 entries required.");
            foreach(double v in IntensityChoices) Range(v, 0, 1, "IntensityChoices");
            Detection.Validate(); Scheduling.Validate(); Polling.Validate();
            foreach(FeedbackKind kind in Enum.GetValues(typeof(FeedbackKind))) {
                EffectRule rule; if(!Effects.TryGetValue(kind,out rule) || rule==null) throw new ArgumentException("Missing effect: " + kind);
                rule.Validate(kind);
            }
        }
        internal static void Range(double value, double min, double max, string name) {
            if(double.IsNaN(value) || double.IsInfinity(value) || value<min || value>max) throw new ArgumentException(name + " must be in " + min.ToString(CultureInfo.InvariantCulture) + ".." + max.ToString(CultureInfo.InvariantCulture));
        }
    }
    internal sealed class DetectionSettings {
        internal double HeavyWindowSeconds, HeavyDamageRatio, HeavyCooldownSeconds, LowHealthRatio, HealthRearmRatio, LowHealthCooldownSeconds;
        internal double LowResourceRatio, ResourceRearmRatio, LowResourceCooldownSeconds, MaxSampleGapSeconds, MaxHealthChangeTolerance, MaxResourceChangeTolerance;
        internal string[] ResourceTypes;
        internal void Validate() {
            RumbleSettings.Range(HeavyWindowSeconds,.05,5,"HeavyWindowSeconds");
            RumbleSettings.Range(HeavyDamageRatio,.001,1,"HeavyDamageRatio");
            RumbleSettings.Range(HeavyCooldownSeconds,0,60,"HeavyCooldownSeconds");
            RumbleSettings.Range(LowHealthRatio,.001,.99,"LowHealthRatio");
            RumbleSettings.Range(HealthRearmRatio,LowHealthRatio+.001,1,"HealthRearmRatio");
            RumbleSettings.Range(LowResourceRatio,.001,.99,"LowResourceRatio");
            RumbleSettings.Range(ResourceRearmRatio,LowResourceRatio+.001,1,"ResourceRearmRatio");
            RumbleSettings.Range(LowHealthCooldownSeconds,0,120,"LowHealthCooldownSeconds");
            RumbleSettings.Range(LowResourceCooldownSeconds,0,120,"LowResourceCooldownSeconds");
            RumbleSettings.Range(MaxSampleGapSeconds,HeavyWindowSeconds,30,"MaxSampleGapSeconds");
            RumbleSettings.Range(MaxHealthChangeTolerance,0,100,"MaxHealthChangeTolerance");
            RumbleSettings.Range(MaxResourceChangeTolerance,0,100,"MaxResourceChangeTolerance");
            if(ResourceTypes==null || ResourceTypes.Length<1 || ResourceTypes.Length>2) throw new ArgumentException("Choose MANA, ENERGY, or both.");
            HashSet<string> unique=new HashSet<string>();
            foreach(string type in ResourceTypes) if((type!="MANA" && type!="ENERGY") || !unique.Add(type)) throw new ArgumentException("ResourceTypes accepts unique MANA/ENERGY only.");
        }
    }
    internal sealed class SchedulingSettings {
        internal int QueueCapacity;
        internal double QueueExpirySeconds, MaxLatenessSeconds;
        internal void Validate() {
            RumbleSettings.Range(QueueCapacity,1,64,"QueueCapacity");
            RumbleSettings.Range(QueueExpirySeconds,.1,10,"QueueExpirySeconds");
            RumbleSettings.Range(MaxLatenessSeconds,.01,2,"MaxLatenessSeconds");
        }
    }
    internal sealed class PollingSettings {
        internal int PlayerIntervalMs, HttpTimeoutMs, MotorTimerMs;
        internal double EventIntervalSeconds, ReconnectSeconds, StatusIntervalSeconds;
        internal void Validate() {
            RumbleSettings.Range(PlayerIntervalMs,20,5000,"PlayerIntervalMs");
            RumbleSettings.Range(EventIntervalSeconds,.05,10,"EventIntervalSeconds");
            RumbleSettings.Range(HttpTimeoutMs,100,5000,"HttpTimeoutMs");
            RumbleSettings.Range(MotorTimerMs,5,100,"MotorTimerMs");
            RumbleSettings.Range(ReconnectSeconds,.1,30,"ReconnectSeconds");
            RumbleSettings.Range(StatusIntervalSeconds,.1,60,"StatusIntervalSeconds");
        }
    }
    internal sealed class EffectRule {
        internal bool Enabled, RepeatForKillCount;
        internal int Priority, RepeatCount=1, RepeatGapMs;
        internal double DamageSlowGain;
        internal string LabelZh, LabelEn;
        internal MotorStep[] Steps;
        internal void Validate(FeedbackKind kind) {
            RumbleSettings.Range(Priority,0,1000,"Priority");
            RumbleSettings.Range(RepeatCount,1,5,"RepeatCount");
            RumbleSettings.Range(RepeatGapMs,0,2000,"RepeatGapMs");
            RumbleSettings.Range(DamageSlowGain,0,10,"DamageSlowGain");
            if(RepeatForKillCount && kind!=FeedbackKind.Multikill) throw new ArgumentException("RepeatForKillCount is for Multikill only.");
            if(DamageSlowGain!=0 && kind!=FeedbackKind.HeavyDamage) throw new ArgumentException("DamageSlowGain is for HeavyDamage only.");
            if(String.IsNullOrWhiteSpace(LabelZh) || String.IsNullOrWhiteSpace(LabelEn)) throw new ArgumentException("Both effect labels are required.");
            if(Steps==null || Steps.Length<1 || Steps.Length>16) throw new ArgumentException("Steps: 1..16 entries required.");
            int duration=0;
            foreach(MotorStep step in Steps) {
                if(step==null) throw new ArgumentException("A motor step cannot be null.");
                RumbleSettings.Range(step.Duration,5,2000,"Step duration (ms)");
                RumbleSettings.Range(step.Fast,0,1,"Step fast strength");
                RumbleSettings.Range(step.Slow,0,1,"Step slow strength");
                duration+=step.Duration;
            }
            int repetitions=RepeatForKillCount?5:RepeatCount;
            if(duration*repetitions+RepeatGapMs*(repetitions-1)>10000) throw new ArgumentException("An effect must not exceed 10 seconds.");
        }
    }
    internal static class Ui {
#if UI_EN
        internal const bool English = true;
#elif UI_ZH
        internal const bool English = false;
#else
        internal static readonly bool English = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "zh";
#endif
        internal static string Text(string zh, string en) { return English ? en : zh; }
    }
}
