using System;
using System.IO;
using System.Text.Json;
using OneMoreBuild;

class Program
{
    static int assertions;
    static void Check(bool value, string name) { assertions++; if (!value) throw new Exception(name); }
    static readonly JsonSerializerOptions Options = new JsonSerializerOptions { IncludeFields = true };
    static string Encode(GameState s) { return JsonSerializer.Serialize(s, Options); }
    static GameState Decode(string s) { return JsonSerializer.Deserialize<GameState>(s, Options); }
    static void Advance(Simulation sim, int hours)
    {
        for (int i = 0; i < hours; i++) { if (sim.State.pendingEvent >= 0) sim.ResolveEvent(false); sim.Tick(); }
    }
    static void Main()
    {
        int ticks = 0;
        var clock = new GameClock();
        for (int i = 0; i < 8; i++) clock.Advance(.125f, 0, () => false, () => ticks++);
        Check(ticks == 0, "pause stops time");
        for (int i = 0; i < 8; i++) clock.Advance(.125f, 1, () => true, () => ticks++);
        Check(ticks == 0, "modal stops time");
        for (int i = 0; i < 8; i++) clock.Advance(.125f, 1, () => false, () => ticks++);
        Check(ticks == 1, "normal speed");
        for (int i = 0; i < 8; i++) clock.Advance(.125f, 3, () => false, () => ticks++);
        Check(ticks == 4, "triple speed");
        var sim = new Simulation(new GameState());
        Check(!sim.StartProject("x", -1, 0, 0), "reject invalid genre");
        Check(sim.StartProject("Первая", 0, 2, 0), "start");
        Check(!sim.StartProject("Вторая", 0, 0, 0), "one active project");
        Check(sim.Release() == null, "no premature release");
        for (int game = 0; game < 3; game++)
        {
            if (game > 0) Check(sim.StartProject("Проект " + game, game, game, 0), "affordable next project");
            for (int i = 0; sim.State.project.progress < 100 && i < 1500; i++)
            {
                sim.State.focus = sim.State.project.progress < 33 ? WorkFocus.Code : sim.State.project.progress < 66 ? WorkFocus.Art : WorkFocus.Polish;
                if (sim.State.energy < 15) sim.State.resting = true;
                if (sim.State.energy > 90) sim.State.resting = false;
                Advance(sim, 1);
            }
            if (sim.State.pendingEvent >= 0) sim.ResolveEvent(false);
            Check(sim.State.project.progress == 100, "project completes");
            ReleaseData release = sim.Release();
            Check(release != null && release.rating >= 4, "reasonable first projects");
            Check(sim.Release() == null, "release cannot duplicate");
            int before = sim.State.money;
            Advance(sim, 120);
            Check(sim.State.money == before + release.totalRevenue, "exact cumulative sales");
            before = sim.State.money;
            Advance(sim, 100);
            Check(sim.State.money == before, "sales stop");
            Console.WriteLine("Project " + game + ": rating " + release.rating.ToString("F1") + ", money " + sim.State.money);
        }
        Check(sim.BuyUpgrade(5), "can afford goal after three releases");
        Check(sim.GoalReached, "goal");
        int cash = sim.State.money;
        Check(!sim.BuyUpgrade(5) && sim.State.money == cash, "no duplicate purchase");
        Check(!sim.BuyUpgrade(-1), "invalid upgrade");
        var eventSim = new Simulation(new GameState());
        eventSim.StartProject("События", 0, 0, 0);
        for (int i = 0; i < 8; i++)
        {
            eventSim.State.pendingEvent = i;
            int hour = eventSim.State.hour;
            eventSim.Tick();
            Check(eventSim.State.hour == hour, "event pauses clock");
            Check(eventSim.ResolveEvent(true), "event left");
            eventSim.State.pendingEvent = i;
            Check(eventSim.ResolveEvent(false), "event right");
            Check(Simulation.IsValid(eventSim.State), "event state valid");
        }
        var tasks = new Simulation(new GameState());
        Check(!tasks.ProductionTask(0), "task needs project");
        tasks.StartProject("Tasks", 0, 0, 0);
        tasks.State.energy = 0;
        Check(!tasks.ProductionTask(2) && tasks.State.project.actionsUsed == 0, "no free task without energy");
        tasks.State.energy = 100;
        tasks.State.resting = true;
        Check(!tasks.ProductionTask(0), "cannot do task while resting");
        tasks.State.resting = false;
        tasks.State.pendingEvent = 1;
        Check(!tasks.ProductionTask(0), "event blocks task");
        tasks.ResolveEvent(false);
        tasks.State.money = 0;
        Check(!tasks.ProductionTask(1), "playtest needs money");
        tasks.State.money = 100;
        for (int task = 0; task < 3; task++)
        {
            Check(tasks.ProductionTask(task), "task executes");
            int taskCash = tasks.State.money;
            float taskEnergy = tasks.State.energy;
            Check(!tasks.ProductionTask(task) && tasks.State.money == taskCash && tasks.State.energy == taskEnergy, "task cannot replay");
        }
        Check(tasks.State.project.bugs == 2 && tasks.State.project.progress == 10 && tasks.State.project.art == 10 && tasks.State.project.polish == 10, "task effects exact");
        Check(Simulation.IsValid(tasks.State), "task state valid");
        Check(!tasks.ProductionTask(3), "unknown task rejected");
        var low = new Simulation(new GameState { money = 0 });
        Check(low.Contract(), "recovery contract");
        Check(!low.Contract(), "contract cooldown");
        Advance(low, 24);
        Check(low.Contract(), "contract unlock");
        var a = new Simulation(new GameState()); var b = new Simulation(new GameState());
        a.StartProject("seed", 0, 0, 0); b.StartProject("seed", 0, 0, 0);
        Advance(a, 400); Advance(b, 400);
        Check(Encode(a.State) == Encode(b.State), "deterministic simulation");
        string dir = Path.Combine(Path.GetTempPath(), "devgame-tests-" + Guid.NewGuid());
        string path = Path.Combine(dir, "save.dat");
        var store = new SaveRepository(path, Encode, Decode);
        try
        {
            string msg;
            Check(store.Load(out msg).money == 400, "missing save");
            Check(store.Save(a.State, out msg), "save active project");
            var loaded = store.Load(out msg);
            Check(Encode(loaded) == Encode(a.State), "full active project roundtrip");
            var resumed = new Simulation(loaded);
            Advance(a, 300); Advance(resumed, 300);
            Check(Encode(a.State) == Encode(resumed.State), "resume deterministic");
            Check(store.Save(sim.State, out msg), "save release history");
            loaded = store.Load(out msg);
            cash = loaded.money;
            Advance(new Simulation(loaded), 240);
            Check(loaded.money == cash, "no paid sale replay after load");
            File.WriteAllText(path, "broken");
            Check(store.Load(out msg).project != null && msg != null, "backup recovery");
            Check(Directory.GetFiles(dir, "*.corrupt").Length > 0, "corrupt save preserved");
            File.WriteAllText(path, "broken"); File.WriteAllText(path + ".bak", "broken too");
            Check(store.Load(out msg).money == 400 && msg != null, "both corrupt safe reset");
            var selling = new Simulation(new GameState());
            selling.StartProject("Продажи", 0, 2, 0);
            selling.State.project.progress = 100;
            selling.Release();
            Advance(selling, 37);
            Check(store.Save(selling.State, out msg), "save partial sales");
            var partial = store.Load(out msg);
            int remaining = partial.releases[0].totalRevenue - partial.releases[0].paidRevenue;
            cash = partial.money;
            Advance(new Simulation(partial), 200);
            Check(partial.money == cash + remaining, "partial sales pay only remainder");
            Check(a.State.project != null, "active test fixture");
            a.State.pendingEvent = 3;
            Check(store.Save(a.State, out msg), "save pending event");
            var pending = store.Load(out msg);
            Check(pending.pendingEvent == 3 && pending.project.title == "seed", "pending event roundtrip");
            Check(store.Save(tasks.State, out msg), "save tasks");
            var savedTasks = new Simulation(store.Load(out msg));
            Check(savedTasks.State.project.actionsUsed == 7 && !savedTasks.ProductionTask(1), "used tasks persist across reload");
            savedTasks.State.project.actionsUsed = 0;
            Check(savedTasks.ProductionTask(1), "old saves allow unused tasks");
            var invalid = new GameState { energy = float.NaN };
            Check(!store.Save(invalid, out msg), "reject invalid state");
            invalid = new GameState(); invalid.pendingEvent = 0;
            Check(!Simulation.IsValid(invalid), "reject event without project");
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        Console.WriteLine("PASS: " + assertions + " assertions");
    }
}
