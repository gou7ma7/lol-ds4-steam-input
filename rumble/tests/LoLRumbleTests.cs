using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace LoLDamageRumble {
    internal static class Tests {
        private static readonly List<string> passed=new List<string>();
        private static readonly RumbleSettings defaults=RegressionSettings.Create();
        private static Pattern Make(Feedback feedback) { return Pattern.Make(feedback,defaults); }
        private static void Reject(Action action,string label) {
            bool rejected=false; try { action(); } catch(ArgumentException) { rejected=true; }
            Check(rejected,label);
        }
        private static string Signature(Pattern pattern) {
            List<string> items=new List<string>();
            foreach(MotorStep step in pattern.Steps) items.Add(step.Duration+":"+step.Fast.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture)+":"+step.Slow.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture));
            return pattern.Priority+"|"+String.Join("|",items.ToArray());
        }
        private static void Check(bool condition,string label) { if(!condition) throw new Exception("FAIL: "+label); passed.Add(label); }
        private static PlayerSnapshot P(double hp,double resource) { return P(hp,1000,resource,100,"MANA"); }
        private static PlayerSnapshot P(double hp,double max,double resource,double resourceMax,string type) {
            return new PlayerSnapshot { Health=hp,MaxHealth=max,Resource=resource,MaxResource=resourceMax,ResourceType=type,Identity="玩家#CN1",Names=new HashSet<string>(new string[]{"玩家#CN1","玩家"},StringComparer.OrdinalIgnoreCase) };
        }
        private static bool Has(List<Feedback> list,FeedbackKind kind) { foreach(Feedback item in list) if(item.Kind==kind) return true; return false; }
        private static Dictionary<string,object> E(int id,string kind,string killer,params string[] assists) {
            return new Dictionary<string,object> { {"EventID",id},{"EventTime",id*10.0},{"EventName",kind},{"KillerName",killer},{"Assisters",assists} };
        }
        private static void Main(string[] args) {
            try { Run(args); } catch(Exception ex) {
                while(ex.InnerException!=null) ex=ex.InnerException;
                Console.WriteLine(ex.GetType().Name+": "+ex.Message); Environment.ExitCode=1;
            }
        }
        private static void Run(string[] args) {
            FeedbackDetector detector=new FeedbackDetector(defaults.Detection);
            Check(detector.Player(P(1000,100),0).Count==0,"startup establishes baseline");
            Check(detector.Player(P(990,100),.1).Count==0,"small damage is silent");
            Check(detector.Player(P(970,100),.2).Count==0,"sub-threshold accumulated damage is silent");
            Check(Has(detector.Player(P(900,100),.4),FeedbackKind.HeavyDamage),"10 percent within 500 ms triggers heavy damage");
            Check(!Has(detector.Player(P(780,100),.5),FeedbackKind.HeavyDamage),"heavy damage cooldown prevents flooding");
            detector.Reset(); detector.Player(P(1000,100),0); detector.Player(P(950,100),.4);
            Check(!Has(detector.Player(P(900,100),.8),FeedbackKind.HeavyDamage),"slow chip damage outside window is silent");
            detector.Reset(); detector.Player(P(1000,100),0); detector.Player(P(950,100),.1); detector.Player(P(1020,100),.2);
            Check(!Has(detector.Player(P(970,100),.3),FeedbackKind.HeavyDamage),"healing does not accumulate as damage");
            detector.Reset(); detector.Player(P(1000,100),0);
            Check(!Has(detector.Player(P(700,1200,100,100,"MANA"),.1),FeedbackKind.HeavyDamage),"max health changes reset baseline");
            detector.Reset(); detector.Player(P(1000,100),0);
            Check(!Has(detector.Player(P(700,100),2),FeedbackKind.HeavyDamage),"data gaps suppress stale damage");
            detector.Reset(); detector.Player(P(1000,100),0);
            Check(Has(detector.Player(P(0,100),.1),FeedbackKind.HeavyDamage),"fatal large hit still triggers heavy damage");
            Check(!Has(detector.Player(P(0,100),.2),FeedbackKind.LowHealth),"dead players do not trigger low health");
            detector.Reset(); detector.Player(P(300,100),0);
            Check(Has(detector.Player(P(250,100),.1),FeedbackKind.LowHealth),"crossing 25 percent triggers low health");
            Check(!Has(detector.Player(P(200,100),.2),FeedbackKind.LowHealth),"remaining low does not repeat warning");
            detector.Player(P(300,100),.3);
            Check(!Has(detector.Player(P(250,100),.4),FeedbackKind.LowHealth),"hysteresis stops threshold chatter");
            detector.Player(P(400,100),9);
            Check(Has(detector.Player(P(240,100),9.1),FeedbackKind.LowHealth),"warning rearms after recovery");
            detector.Reset(); Check(detector.Player(P(100,5),0).Count==0,"starting low does not fabricate a new crossing");
            detector.Reset(); detector.Player(P(1000,100),0);
            Check(Has(detector.Player(P(1000,20),.1),FeedbackKind.LowResource),"low mana triggers at 20 percent");
            Check(!Has(detector.Player(P(1000,0),.2),FeedbackKind.LowResource),"empty resource does not continuously repeat");
            detector.Reset(); detector.Player(P(1000,1000,100,200,"ENERGY"),0);
            Check(Has(detector.Player(P(1000,1000,30,200,"ENERGY"),.1),FeedbackKind.LowResource),"energy resource uses the same low threshold");
            detector.Reset(); detector.Player(P(1000,1000,100,100,"FURY"),0);
            Check(!Has(detector.Player(P(1000,1000,0,100,"FURY"),.1),FeedbackKind.LowResource),"fury and rage are not mistaken for empty mana");
            detector.Reset(); detector.Player(P(1000,100),0);
            Check(!Has(detector.Player(P(1000,1000,10,200,"MANA"),.1),FeedbackKind.LowResource),"resource capacity changes reset crossing");

            HashSet<string> names=P(1000,100).Names;
            List<Dictionary<string,object>> events=new List<Dictionary<string,object>> { E(0,"GameStart",""),E(1,"ChampionKill","玩家#CN1") };
            detector.Reset(); Check(detector.Events(events,names).Count==0,"existing kills are skipped on startup");
            events.Add(E(2,"ChampionKill","玩家"));
            Check(Has(detector.Events(events,names),FeedbackKind.Kill),"kill matches legacy player alias");
            Check(detector.Events(events,names).Count==0,"cumulative events are deduplicated");
            events.Add(E(3,"ChampionKill","敌人","玩家#CN1"));
            Check(Has(detector.Events(events,names),FeedbackKind.Assist),"assists match the explicit assister list");
            events.Add(E(4,"ChampionKill","队友","别人"));
            Check(detector.Events(events,names).Count==0,"other players kills stay silent");
            events.Add(E(5,"ChampionKill","玩家#CN1"));
            Dictionary<string,object> multi=E(6,"Multikill","玩家#CN1"); multi["KillStreak"]=3; events.Add(multi);
            List<Feedback> batch=detector.Events(events,names);
            Check(batch.Count==1 && batch[0].Kind==FeedbackKind.Multikill && batch[0].Count==3,"multikill replaces ordinary kill in same batch");
            detector.ResetEvents(); events.Add(E(7,"ChampionKill","玩家#CN1"));
            Check(detector.Events(events,names).Count==0,"event reconnection does not replay a backlog");
            Check(detector.Events(new List<Dictionary<string,object>> { E(0,"GameStart","") },names).Count==0,"new matches reset old event ids");

            List<string> writes=new List<string>(); int starts=0;
            PatternScheduler scheduler=new PatternScheduler(delegate(double fast,double slow) { writes.Add(fast+","+slow); },delegate(Pattern pattern) { starts++; },defaults.Scheduling);
            scheduler.Enqueue(Make(new Feedback(FeedbackKind.LowResource,0,1)),0);
            scheduler.Tick(.08); Check(!scheduler.MotorsOn,"pattern gaps turn motors off");
            scheduler.Tick(.19); Check(scheduler.MotorsOn,"pattern resumes after intentional gap");
            scheduler.Tick(.27); scheduler.Tick(.38); scheduler.Tick(.46);
            Check(!scheduler.MotorsOn && writes[writes.Count-1]=="0,0" && starts==1,"multi-pulse pattern ends with motors off");
            scheduler.Enqueue(Make(new Feedback(FeedbackKind.Kill,0,1)),1);
            scheduler.Enqueue(Make(new Feedback(FeedbackKind.HeavyDamage,.2,1)),1.02);
            Check(starts==3 && scheduler.MotorsOn,"heavy hit immediately preempts kill celebration");
            scheduler.Enqueue(Make(new Feedback(FeedbackKind.LowHealth,0,1)),1.03);
            Check(starts==3,"low health queues without mixing into heavy hit");
            scheduler.Tick(1.24); Check(starts==4,"queued low health starts after heavy hit");
            scheduler.Cancel(); Check(!scheduler.MotorsOn && writes[writes.Count-1]=="0,0","pause and exit cancel queued effects and stop motors");
            scheduler.Tick(2); Check(starts==4,"cancelled effects do not replay");
            scheduler.Enqueue(Make(new Feedback(FeedbackKind.Multikill,0,5)),3);
            scheduler.Tick(4); Check(!scheduler.MotorsOn,"timer stalls cancel stale multi-pulse sequences");

            if(args.Length>1 && File.Exists(args[1])) {
                Dictionary<string,object> root=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(args[1]));
                Dictionary<string,object> active=Json.Object(Json.Value(root,"activePlayer"));
                bool rejected=false; try { PlayerSnapshot.Parse(active); } catch(IOException) { rejected=true; }
                Check(rejected,"zero-health-capacity placeholder in synthetic fixture is rejected");
                Dictionary<string,object> stats=Json.Object(Json.Value(active,"championStats")); stats["maxHealth"]=1000; stats["currentHealth"]=800;
                PlayerSnapshot sample=PlayerSnapshot.Parse(active);
                Check(sample.MaxHealth==1000 && sample.Names.Count>0,"Live Client schema parses with representative live health values");
                Dictionary<string,object> eventRoot=Json.Object(Json.Value(root,"events"));
                List<Dictionary<string,object>> fixtureEvents=new List<Dictionary<string,object>>();
                foreach(object item in Json.Array(Json.Value(eventRoot,"Events"))) fixtureEvents.Add(Json.Object(item));
                detector.Reset(); Check(detector.Events(fixtureEvents,sample.Names).Count==0,"synthetic event fixture initializes without false feedback");
            }
            ConfigurationTests(args);
            string report=new JavaScriptSerializer().Serialize(new { passed=passed.Count,preset=FeedbackConfig.SelectedPreset,englishUi=Ui.English,scenarios=passed });
            if(args.Length>0) File.WriteAllText(args[0],report);
            Console.WriteLine("PASS: "+passed.Count+" behavior checks; no controller writes performed.");
        }
        private static void ConfigurationTests(string[] args) {
            foreach(string preset in FeedbackConfig.PresetNames) {
                RumbleSettings c=FeedbackConfig.Create(preset); c.Validate();
                Check(c.Name==preset,"preset validates: "+preset);
            }
            if(args.Length>2) Check(FeedbackConfig.SelectedPreset==args[2] && FeedbackConfig.Current.Name==args[2],"compiled preset matches build selection");
            if(args.Length>3) { bool expected=args[3]=="auto" ? System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName!="zh" : args[3]=="en"; Check(Ui.English==expected,"compiled UI language matches build selection"); }
            Check(Ui.Text("中文","English")== (Ui.English?"English":"中文"),"UI localization resolves consistently");
            Check(defaults.MasterIntensity==.65,"reference inputs preserve 65 percent master intensity");
            bool enabled=true; foreach(EffectRule rule in defaults.Effects.Values) enabled=enabled&&rule.Enabled;
            Check(enabled,"reference inputs preserve all six enabled effects");
            Check(Signature(Make(new Feedback(FeedbackKind.HeavyDamage,.10,1)))=="100|220:0.75:0.82","reference inputs preserve heavy damage waveform");
            Check(Signature(Make(new Feedback(FeedbackKind.LowHealth,0,1)))=="80|160:0.25:1|100:0:0|160:0.25:1","reference inputs preserve low health waveform");
            Check(Signature(Make(new Feedback(FeedbackKind.LowResource,0,1)))=="40|80:0.9:0|110:0:0|80:0.9:0|110:0:0|80:0.9:0","reference inputs preserve low resource waveform");
            Check(Signature(Make(new Feedback(FeedbackKind.Kill,0,1)))=="60|90:0.9:0.2|80:0:0|150:1:0.45","reference inputs preserve kill waveform");
            Check(Signature(Make(new Feedback(FeedbackKind.Assist,0,1)))=="50|150:0.6:0.15","reference inputs preserve assist waveform");
            Check(Signature(Make(new Feedback(FeedbackKind.Multikill,0,3)))=="70|110:1:0.6|80:0:0|110:1:0.6|80:0:0|110:1:0.6","reference inputs preserve multikill waveform");
            RumbleSettings custom=RegressionSettings.Create();
            RumbleSettings copy=FeedbackConfig.Create("default"); double originalSlow=copy.Effects[FeedbackKind.HeavyDamage].Steps[0].Slow;
            copy.Effects[FeedbackKind.HeavyDamage].Steps[0].Slow=originalSlow>.5?.1:.9;
            Check(FeedbackConfig.Create("default").Effects[FeedbackKind.HeavyDamage].Steps[0].Slow==originalSlow,"presets do not share mutable waveform state");
            Pattern detached=Pattern.Make(new Feedback(FeedbackKind.Kill,0,1),custom); detached.Steps[0].Duration=99;
            Check(custom.Effects[FeedbackKind.Kill].Steps[0].Duration==90,"generated patterns do not mutate configuration steps");
            custom=RegressionSettings.Create(); custom.Detection.HeavyDamageRatio=.20; custom.Detection.HeavyWindowSeconds=.25; custom.Validate();
            FeedbackDetector detector=new FeedbackDetector(custom.Detection); detector.Player(P(1000,100),0);
            Check(!Has(detector.Player(P(850,100),.1),FeedbackKind.HeavyDamage),"custom damage threshold ignores 15 percent loss");
            Check(Has(detector.Player(P(800,100),.2),FeedbackKind.HeavyDamage),"custom damage threshold triggers at 20 percent");
            detector.Reset(); detector.Player(P(1000,100),0); detector.Player(P(850,100),.1);
            Check(!Has(detector.Player(P(800,100),.3),FeedbackKind.HeavyDamage),"custom rolling damage window excludes old peak");
            custom.Detection.LowHealthRatio=.10; custom.Detection.HealthRearmRatio=.20; custom.Validate();
            detector=new FeedbackDetector(custom.Detection); detector.Player(P(250,100),0);
            Check(!Has(detector.Player(P(150,100),.1),FeedbackKind.LowHealth) && Has(detector.Player(P(100,100),.2),FeedbackKind.LowHealth),"custom low health threshold controls crossing");
            custom.Detection.ResourceTypes=new string[]{"MANA"}; custom.Validate();
            detector=new FeedbackDetector(custom.Detection); detector.Player(P(1000,1000,100,100,"ENERGY"),0);
            Check(!Has(detector.Player(P(1000,1000,10,100,"ENERGY"),.1),FeedbackKind.LowResource),"MANA-only configuration excludes ENERGY");
            custom=RegressionSettings.Create(); custom.Effects[FeedbackKind.HeavyDamage].DamageSlowGain=0;
            Check(Pattern.Make(new Feedback(FeedbackKind.HeavyDamage,1,1),custom).Steps[0].Slow==.82,"zero gain disables dynamic heavy-hit scaling");
            custom.Effects[FeedbackKind.HeavyDamage].DamageSlowGain=10;
            Check(Pattern.Make(new Feedback(FeedbackKind.HeavyDamage,1,1),custom).Steps[0].Slow==1,"dynamic strength is capped at one");
            custom.Effects[FeedbackKind.LowResource].RepeatCount=1;
            Check(Pattern.Make(new Feedback(FeedbackKind.LowResource,0,1),custom).Steps.Length==1,"custom repeat count changes pulse sequence");
            custom.Effects[FeedbackKind.Multikill].RepeatForKillCount=false; custom.Effects[FeedbackKind.Multikill].RepeatCount=2;
            Check(Pattern.Make(new Feedback(FeedbackKind.Multikill,0,5),custom).Steps.Length==3,"multikill can use a fixed repeat count");
            custom=RegressionSettings.Create(); custom.Scheduling.QueueExpirySeconds=.1;
            int starts=0; PatternScheduler scheduler=new PatternScheduler(delegate(double f,double s){},delegate(Pattern p){starts++;},custom.Scheduling);
            scheduler.Enqueue(Pattern.Make(new Feedback(FeedbackKind.HeavyDamage,.1,1),custom),0);
            scheduler.Enqueue(Pattern.Make(new Feedback(FeedbackKind.LowHealth,0,1),custom),.02);
            scheduler.Tick(.22); Check(starts==1,"custom queue lifetime discards stale warnings"); scheduler.Cancel();
            custom=RegressionSettings.Create(); custom.Effects[FeedbackKind.Assist].Priority=200; starts=0;
            scheduler=new PatternScheduler(delegate(double f,double s){},delegate(Pattern p){starts++;},custom.Scheduling);
            scheduler.Enqueue(Pattern.Make(new Feedback(FeedbackKind.HeavyDamage,.1,1),custom),0);
            scheduler.Enqueue(Pattern.Make(new Feedback(FeedbackKind.Assist,0,1),custom),.01);
            Check(starts==2,"configured priority changes preemption behavior"); scheduler.Cancel();
            Reject(delegate { FeedbackConfig.Create("missing"); },"unknown presets fail validation");
            Reject(delegate { RumbleSettings c=RegressionSettings.Create(); c.MasterIntensity=1.1; c.Validate(); },"out-of-range master intensity rejected");
            Reject(delegate { RumbleSettings c=RegressionSettings.Create(); c.Detection.HealthRearmRatio=.20; c.Validate(); },"health rearm must exceed low threshold");
            Reject(delegate { RumbleSettings c=RegressionSettings.Create(); c.Detection.ResourceTypes=new string[]{"AMMO"}; c.Validate(); },"unsupported resources rejected");
            Reject(delegate { RumbleSettings c=RegressionSettings.Create(); c.Detection.ResourceTypes=new string[]{"MANA","MANA"}; c.Validate(); },"duplicate resource types rejected");
            Reject(delegate { RumbleSettings c=RegressionSettings.Create(); c.Effects[FeedbackKind.Kill].Steps[0].Duration=0; c.Validate(); },"zero-duration steps rejected");
            Reject(delegate { RumbleSettings c=RegressionSettings.Create(); c.Effects[FeedbackKind.Kill].Steps[0].Fast=double.NaN; c.Validate(); },"non-finite motor strength rejected");
            Reject(delegate { RumbleSettings c=RegressionSettings.Create(); c.Effects[FeedbackKind.Kill].RepeatForKillCount=true; c.Validate(); },"kill-count repetition restricted to multikill");
            Reject(delegate { RumbleSettings c=RegressionSettings.Create(); c.Effects[FeedbackKind.Assist].DamageSlowGain=1; c.Validate(); },"damage scaling restricted to heavy damage");
            Reject(delegate { RumbleSettings c=RegressionSettings.Create(); c.Effects[FeedbackKind.Multikill].Steps=new MotorStep[]{new MotorStep(2000,1,1),new MotorStep(2000,1,1)}; c.Validate(); },"overlong effect sequences rejected");
            Reject(delegate { RumbleSettings c=RegressionSettings.Create(); c.Scheduling.QueueCapacity=0; c.Validate(); },"empty queue capacity rejected");
        }
    }
}
