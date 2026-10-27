using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace DS4SleepLight {
    internal sealed class WriteResult {
        internal string Outcome; internal int Matches, Writes, Win32Error;
        internal bool Success { get { return Outcome == "written"; } }
        internal WriteResult(string outcome, int matches, int writes, int error) { Outcome = outcome; Matches = matches; Writes = writes; Win32Error = error; }
    }
    internal static class NativeHid {
        [StructLayout(LayoutKind.Sequential)] private struct InterfaceData { internal int Size; internal Guid ClassGuid; internal int Flags; internal IntPtr Reserved; }
        [StructLayout(LayoutKind.Sequential)] private struct Attributes { internal int Size; internal ushort Vendor, Product, Version; }
        [StructLayout(LayoutKind.Sequential)] private struct Caps {
            internal ushort Usage, UsagePage, InputLength, OutputLength, FeatureLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)] internal ushort[] Reserved;
            internal ushort LinkNodes, InputButtons, InputValues, InputIndices, OutputButtons, OutputValues, OutputIndices, FeatureButtons, FeatureValues, FeatureIndices;
        }
        [StructLayout(LayoutKind.Sequential)] private struct Overlapped { internal UIntPtr Internal, InternalHigh; internal uint Offset, OffsetHigh; internal IntPtr Event; }
        [DllImport("hid.dll")] private static extern void HidD_GetHidGuid(out Guid guid);
        [DllImport("hid.dll")] private static extern bool HidD_GetAttributes(SafeFileHandle handle, ref Attributes attributes);
        [DllImport("hid.dll")] private static extern bool HidD_GetPreparsedData(SafeFileHandle handle, out IntPtr data);
        [DllImport("hid.dll")] private static extern bool HidD_FreePreparsedData(IntPtr data);
        [DllImport("hid.dll")] private static extern int HidP_GetCaps(IntPtr data, out Caps caps);
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr SetupDiGetClassDevs(ref Guid guid, string enumerator, IntPtr parent, uint flags);
        [DllImport("setupapi.dll", SetLastError = true)] private static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr device, ref Guid guid, uint index, ref InterfaceData data);
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref InterfaceData data, IntPtr detail, uint length, out uint required, IntPtr device);
        [DllImport("setupapi.dll")] private static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool WriteFile(SafeFileHandle file, IntPtr buffer, uint count, out uint written, IntPtr overlapped);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetOverlappedResult(SafeFileHandle file, IntPtr overlapped, out uint count, bool wait);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CancelIoEx(SafeFileHandle file, IntPtr overlapped);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern SafeWaitHandle CreateEvent(IntPtr security, bool manual, bool initial, string name);
        [DllImport("kernel32.dll")] private static extern uint WaitForSingleObject(SafeWaitHandle handle, uint timeout);

        private static List<string> Paths() {
            Guid guid; HidD_GetHidGuid(out guid);
            IntPtr set = SetupDiGetClassDevs(ref guid, null, IntPtr.Zero, 0x12);
            if (set == new IntPtr(-1)) throw new IOException("HID enumeration failed.");
            List<string> paths = new List<string>();
            try {
                for (uint index = 0; ; index++) {
                    InterfaceData item = new InterfaceData(); item.Size = Marshal.SizeOf(typeof(InterfaceData));
                    if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref guid, index, ref item)) {
                        if (Marshal.GetLastWin32Error() != 259) throw new IOException("HID enumeration incomplete.");
                        break;
                    }
                    uint required; SetupDiGetDeviceInterfaceDetail(set, ref item, IntPtr.Zero, 0, out required, IntPtr.Zero);
                    if (required < 8) continue;
                    IntPtr detail = Marshal.AllocHGlobal((int)required);
                    try {
                        Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                        if (!SetupDiGetDeviceInterfaceDetail(set, ref item, detail, required, out required, IntPtr.Zero)) continue;
                        string path = Marshal.PtrToStringUni(IntPtr.Add(detail, 4));
                        if (path == null || path.IndexOf("vid_054c&pid_09cc", StringComparison.OrdinalIgnoreCase) < 0) continue;
                        using (SafeFileHandle file = Open(path, 0)) {
                            if (file.IsInvalid) throw new IOException("Target capabilities inaccessible; enumeration refused.");
                            Attributes attributes = new Attributes(); attributes.Size = Marshal.SizeOf(typeof(Attributes));
                            IntPtr prep;
                            if (!HidD_GetAttributes(file, ref attributes) || !HidD_GetPreparsedData(file, out prep)) throw new IOException("Target capabilities incomplete; enumeration refused.");
                            try {
                                Caps caps;
                                if (HidP_GetCaps(prep, out caps) != 0x110000) throw new IOException("Target capabilities invalid; enumeration refused.");
                                if (LightReport.Supported(attributes.Vendor, attributes.Product, caps.UsagePage, caps.Usage, caps.OutputLength)) paths.Add(path);
                            } finally { HidD_FreePreparsedData(prep); }
                        }
                    } finally { Marshal.FreeHGlobal(detail); }
                }
            } finally { SetupDiDestroyDeviceInfoList(set); }
            return paths;
        }
        private static SafeFileHandle Open(string path, uint access) { return CreateFile(path, access, 3, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero); }
        internal static WriteResult Diagnose() { return Execute(null, 0, 0); }
        internal static WriteResult Send(byte[] report, int attempts, int repeatDelayMs) { return Execute(report, attempts, repeatDelayMs); }
        private static WriteResult Execute(byte[] report, int attempts, int repeatDelayMs) {
            List<string> paths;
            try { paths = Paths(); } catch (IOException) { return new WriteResult("enumeration_failed", 0, 0, Marshal.GetLastWin32Error()); }
            if (paths.Count != 1) return new WriteResult(paths.Count == 0 ? "no_supported_device" : "multiple_devices_refused", paths.Count, 0, 0);
            using (SafeFileHandle file = Open(paths[0], 0x40000000)) {
                if (file.IsInvalid) return new WriteResult("output_unavailable", 1, 0, Marshal.GetLastWin32Error());
                if (report == null) return new WriteResult("output_accessible_no_write", 1, 0, 0);
                int writes = 0, error = 0;
                for (int i = 0; i < attempts; i++) {
                    if (i > 0) System.Threading.Thread.Sleep(repeatDelayMs);
                    if (Write(file, report, out error)) writes++;
                    else break;
                }
                return new WriteResult(writes == attempts ? "written" : "write_failed", 1, writes, error);
            }
        }
        private static bool Write(SafeFileHandle file, byte[] report, out int error) {
            error = 0;
            using (SafeWaitHandle completion = CreateEvent(IntPtr.Zero, true, false, null)) {
                if (completion.IsInvalid) { error = Marshal.GetLastWin32Error(); return false; }
                GCHandle buffer = GCHandle.Alloc(report, GCHandleType.Pinned);
                IntPtr ov = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Overlapped)));
                bool pending = false;
                try {
                    Overlapped data = new Overlapped(); data.Event = completion.DangerousGetHandle();
                    Marshal.StructureToPtr(data, ov, false);
                    uint written;
                    if (!WriteFile(file, buffer.AddrOfPinnedObject(), (uint)report.Length, out written, ov)) {
                        error = Marshal.GetLastWin32Error();
                        if (error != 997) return false;
                        pending = true;
                        if (WaitForSingleObject(completion, (uint)SleepLightConfig.UsbWriteTimeoutMs) != 0) { error = 1460; return false; }
                        bool completed = GetOverlappedResult(file, ov, out written, false);
                        if (!completed) { error = Marshal.GetLastWin32Error(); pending = error == 996; return false; }
                        pending = false;
                    }
                    if (written != report.Length) { error = 29; return false; }
                    error = 0; return true;
                } finally {
                    // Keep the unmanaged OVERLAPPED and pinned buffer alive until canceled IO completes.
                    // 取消后等待 IO 完成，才释放 OVERLAPPED 与缓冲区。
                    if (pending) { uint ignored; CancelIoEx(file, ov); GetOverlappedResult(file, ov, out ignored, true); }
                    Marshal.FreeHGlobal(ov); buffer.Free();
                }
            }
        }
    }
}
