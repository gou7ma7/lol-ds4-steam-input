using System;
namespace LoLDamageRumble {
    // Fixed v2 reference inputs; do not change for normal production preset edits.
    internal static class RegressionSettings {
        internal static RumbleSettings Create() {
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

}
