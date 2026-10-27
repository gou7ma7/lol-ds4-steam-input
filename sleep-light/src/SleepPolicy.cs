namespace DS4SleepLight {
    internal enum LightPhase { Ready, Suspended, WaitingForUser, WaitingForUnlock, RestorePending }
    // Pure state machine: no Windows, USB, clock or filesystem calls.
    // 纯状态机：不调用 Windows、USB、时钟或文件系统。
    internal sealed class SleepPolicy {
        internal LightPhase Phase { get; private set; }
        internal bool Locked { get; private set; }
        private readonly bool restore;
        internal SleepPolicy(bool restoreOnResume) { restore = restoreOnResume; }
        internal bool Suspend() {
            bool write = Phase != LightPhase.Suspended;
            Phase = LightPhase.Suspended;
            return write;
        }
        internal void AutomaticResume() {
            // PBT_APMRESUMEAUTOMATIC can precede an immediate return to S4.
            // 自动唤醒可能紧接着再次进入 S4，不能在此时点亮。
            if (Phase == LightPhase.Suspended) Phase = LightPhase.WaitingForUser;
        }
        internal bool UserResume() {
            if (Phase != LightPhase.Suspended && Phase != LightPhase.WaitingForUser) return false;
            if (Locked) { Phase = LightPhase.WaitingForUnlock; return false; }
            return ScheduleRestore();
        }
        internal void Lock() {
            Locked = true;
            if (Phase == LightPhase.RestorePending) Phase = LightPhase.WaitingForUnlock;
        }
        internal bool Unlock() {
            Locked = false;
            if (Phase == LightPhase.Suspended || Phase == LightPhase.WaitingForUser || Phase == LightPhase.WaitingForUnlock) return ScheduleRestore();
            return false;
        }
        private bool ScheduleRestore() {
            Phase = restore ? LightPhase.RestorePending : LightPhase.Ready;
            return restore;
        }
        internal void CompleteRestore() { if (Phase == LightPhase.RestorePending) Phase = LightPhase.Ready; }
    }
    internal static class LightReport {
        internal static byte[] Make(int red, int green, int blue) {
            if (red < 0 || red > 255 || green < 0 || green > 255 || blue < 0 || blue > 255) throw new System.ArgumentOutOfRangeException("RGB");
            byte[] report = new byte[32];
            report[0] = 0x05;
            // Validate lightbar and blink fields only. Motor-enable bit 0 remains clear.
            // 仅启用灯色和闪烁字段；马达启用位 bit 0 保持为 0。
            report[1] = 0x06;
            report[6] = (byte)red; report[7] = (byte)green; report[8] = (byte)blue;
            return report;
        }
        internal static bool Supported(ushort vendor, ushort product, ushort usagePage, ushort usage, ushort outputLength) {
            // 32-byte output excludes the Bluetooth report format.
            return vendor == 0x054c && product == 0x09cc && usagePage == 1 && usage == 5 && outputLength == 32;
        }
    }
}
