namespace DS4SleepLight {
    // Edit this file and rebuild. 修改此文件后重新编译。
    internal static class SleepLightConfig {
        // Restore after an interactive resume/unlock; never on a background wake.
        // 用户恢复使用/解锁后恢复颜色；后台自动唤醒时不恢复。
        internal static readonly bool RestoreOnResume = true;
        // RGB channels, 0..255. Default blue. RGB 通道 0..255，默认蓝色。
        internal static readonly int Red = 0, Green = 0, Blue = 255;
        // Wait for the USB stack after user resume, milliseconds (0..10000).
        // 用户恢复后等待 USB 就绪的毫秒数（0..10000）。
        internal static readonly int RestoreDelayMs = 1500;
        // Retry missing/busy devices for this long after the delay (500..30000 ms).
        // 延迟结束后，设备尚未就绪时重试的总时间（500..30000 毫秒）。
        internal static readonly int RestoreRetryWindowMs = 5000;
        internal static readonly int RestoreRetryIntervalMs = 500; // 100..5000 ms / 毫秒
        // Short repeated off writes during the suspend notification, not while asleep.
        // 睡眠通知中短暂重复灭灯；不会在睡眠中持续运行。1..3 次。
        internal static readonly int OffWriteAttempts = 2;
        internal static readonly int OffRepeatDelayMs = 50; // 0..100 ms / 毫秒
        // Cancel a pending USB write after this timeout (50..500 ms).
        // USB 写入超过此毫秒数时请求取消（50..500）。
        internal static readonly int UsbWriteTimeoutMs = 200;
        // Local diagnostic log contains no device path/serial, account or game data.
        // 本地日志不包含设备路径/序列号、账号或游戏数据。
        internal static readonly bool EnableLocalLog = true;

        internal static void Validate() {
            Range("Red", Red, 0, 255); Range("Green", Green, 0, 255); Range("Blue", Blue, 0, 255);
            Range("RestoreDelayMs", RestoreDelayMs, 0, 10000);
            Range("RestoreRetryWindowMs", RestoreRetryWindowMs, 500, 30000);
            Range("RestoreRetryIntervalMs", RestoreRetryIntervalMs, 100, 5000);
            Range("OffWriteAttempts", OffWriteAttempts, 1, 3);
            Range("OffRepeatDelayMs", OffRepeatDelayMs, 0, 100);
            Range("UsbWriteTimeoutMs", UsbWriteTimeoutMs, 50, 500);
        }
        private static void Range(string name, int value, int min, int max) {
            if (value < min || value > max) throw new System.ArgumentOutOfRangeException(name, name + " must be " + min + ".." + max + ".");
        }
    }
}
