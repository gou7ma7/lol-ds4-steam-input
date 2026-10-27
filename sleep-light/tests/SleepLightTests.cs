using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace DS4SleepLight {
    internal static class Tests {
        private static readonly List<string> checks = new List<string>();
        private static void Check(string name, bool condition) { if (!condition) throw new Exception(name); checks.Add(name); }
        private static void Case(string name, Action body) { body(); checks.Add(name); }
        internal static int Main(string[] args) {
            try {
                SleepLightConfig.Validate();
                Check("configuration valid", true);
                byte[] off = LightReport.Make(0, 0, 0), color = LightReport.Make(7, 83, 255);
                Check("USB report length", off.Length == 32);
                Check("USB report id", off[0] == 5);
                Check("only color and blink flags", off[1] == 6);
                Check("motor flag disabled", (off[1] & 1) == 0);
                Check("motor bytes unused", off[4] == 0 && off[5] == 0 && color[4] == 0 && color[5] == 0);
                Check("off RGB zero", off[6] == 0 && off[7] == 0 && off[8] == 0);
                Check("RGB positions", color[6] == 7 && color[7] == 83 && color[8] == 255);
                Check("blink cleared", color[9] == 0 && color[10] == 0);
                for (int i = 2; i < 32; i++) if (i < 6 || i > 8) Check("unrelated byte " + i + " untouched", color[i] == 0);
                Case("invalid RGB rejected", delegate { bool rejected = false; try { LightReport.Make(0, -1, 0); } catch (ArgumentOutOfRangeException) { rejected = true; } Check("negative channel rejected", rejected); });
                Case("large RGB rejected", delegate { bool rejected = false; try { LightReport.Make(256, 0, 0); } catch (ArgumentOutOfRangeException) { rejected = true; } Check("large channel rejected", rejected); });
                Check("USB v2 accepted", LightReport.Supported(0x054c, 0x09cc, 1, 5, 32));
                Check("DS4 v1 rejected", !LightReport.Supported(0x054c, 0x05c4, 1, 5, 32));
                Check("DualSense rejected", !LightReport.Supported(0x054c, 0x0ce6, 1, 5, 32));
                Check("Bluetooth report rejected", !LightReport.Supported(0x054c, 0x09cc, 1, 5, 78));
                Check("non-controller interface rejected", !LightReport.Supported(0x054c, 0x09cc, 1, 2, 32));
                SleepPolicy p = new SleepPolicy(true);
                Check("startup does not schedule a light write", p.Phase == LightPhase.Ready && !p.UserResume() && !p.Unlock());
                Check("first suspend requests off", p.Suspend());
                Check("duplicate suspend suppressed", !p.Suspend());
                p.AutomaticResume(); Check("automatic resume keeps lights off", p.Phase == LightPhase.WaitingForUser);
                Check("S3 automatic wake to S4 requests off again", p.Suspend());
                Check("S3 to S4 never restores", p.Phase == LightPhase.Suspended);
                p.AutomaticResume(); p.Lock();
                Check("locked user resume defers restoration", !p.UserResume() && p.Phase == LightPhase.WaitingForUnlock);
                Check("unlock schedules restoration", p.Unlock() && p.Phase == LightPhase.RestorePending);
                Check("duplicate resume does not restart delay", !p.UserResume());
                Check("duplicate unlock does not restart delay", !p.Unlock());
                p.Lock(); Check("lock cancels pending restoration", p.Phase == LightPhase.WaitingForUnlock);
                Check("later unlock reschedules restoration", p.Unlock());
                Check("another suspend cancels pending restore", p.Suspend() && p.Phase == LightPhase.Suspended);
                p.CompleteRestore(); Check("stale completion cannot clear suspend", p.Phase == LightPhase.Suspended);
                Check("direct interactive resume supported", p.UserResume());
                p.CompleteRestore(); Check("successful restore returns to ready", p.Phase == LightPhase.Ready);
                p.Lock(); Check("normal lock/unlock does not relight", !p.Unlock() && p.Phase == LightPhase.Ready);
                p.Suspend(); p.AutomaticResume(); Check("unlock-only resume supported", p.Unlock());
                SleepPolicy noRestore = new SleepPolicy(false); noRestore.Suspend(); noRestore.AutomaticResume();
                Check("restore disabled sends no color", !noRestore.UserResume() && noRestore.Phase == LightPhase.Ready);
                string json = new JavaScriptSerializer().Serialize(new { passed = checks.Count, hardwareWrites = 0, checks = checks });
                if (args.Length == 1) File.WriteAllText(args[0], json);
                Console.WriteLine("PASS: " + checks.Count + " offline checks; no USB operations."); return 0;
            } catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
        }
    }
}
