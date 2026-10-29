using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using OneMoreBuild;

class Program
{
    static int assertions;
    static void Check(bool value,string name){assertions++;if(!value)throw new Exception(name);}
    static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    static string Encode(GameState s){return JsonSerializer.Serialize(s,Options);}
    static GameState Decode(string s){return JsonSerializer.Deserialize<GameState>(s,Options);}
    static void Tick(Simulation sim,int hours)
    {
        for(int i=0;i<hours;i++){if(sim.State.pendingEvent>=0)sim.ResolveEvent(false);sim.Tick();}
    }
    static void Ledger(GameState s)
    {
        Check(s.money-s.arrears==400+s.lifetimeSales+s.lifetimeContracts-s.lifetimeCosts,"cash ledger reconciles");
        Check(Simulation.IsValid(s),"valid persisted state");
    }
    static Simulation Ready(float quality,float bugs=0,int price=1,int platform=0,int marketing=0,int genre=0,int theme=2)
    {
        var sim=new Simulation(new GameState());
        Check(sim.StartProject("Fixture",genre,theme,0,platform,price,0,marketing,-1),"fixture starts");
        sim.State.project.progress=100;sim.State.project.code=sim.State.project.art=sim.State.project.polish=quality;sim.State.project.bugs=bugs;
        return sim;
    }
    static int ForecastUnits(ReleaseData r){int n=0;foreach(SalesWeek w in r.weeks)n+=w.plannedUnits;return n;}
    static void Main()
    {
        int ticks=0;var clock=new GameClock();
        for(int i=0;i<8;i++)clock.Advance(.125f,0,()=>false,()=>ticks++);
        Check(ticks==0,"pause");
        for(int i=0;i<8;i++)clock.Advance(.125f,1,()=>true,()=>ticks++);
        Check(ticks==0,"modal pauses");
        for(int i=0;i<8;i++)clock.Advance(.125f,3,()=>false,()=>ticks++);
        Check(ticks==3,"speed");
        var good=Ready(55);var bad=Ready(0,30);var expensive=Ready(35,0,2,1);var fair=Ready(35,0,0,1);
        ReleaseData g=good.Release(),b=bad.Release();
        Check(g.rating>b.rating && ForecastUnits(g)>ForecastUnits(b),"quality affects demand");
        Check(ForecastUnits(fair.Release())>ForecastUnits(expensive.Release()),"mobile overpriced demand falls");
        var ads=Ready(55,0,1,0,2);ReleaseData ad=ads.Release();
        Check(ad.cost==270 && ForecastUnits(ad)>ForecastUnits(g),"marketing has cost and reach");
        Check(good.Release()==null,"no double release");
        Tick(good,167);Check(g.units==0&&g.paidRevenue==0,"no instant payout");
        Tick(good,1);Check(g.weeks[0].paid&&g.units>0,"first week settles");Ledger(good.State);
        Tick(good,840);Check(g.age==1008,"six week lifespan");
        int total=0;foreach(SalesWeek w in g.weeks){Check(w.paid,"all weeks settle");Check(w.net==w.gross-w.refunds*g.price-w.fee,"net calculation");total+=w.net;}
        Check(total==g.paidRevenue,"weekly totals reconcile");int revenue=g.paidRevenue;Tick(good,168);Check(g.paidRevenue==revenue,"no repeat sales");Ledger(good.State);
        Tick(bad,1008);Check(b.paidRevenue-b.cost<0,"bad game can lose money");Check(b.refunds>0 || b.units==0,"bad quality refunds or no buyers");Ledger(bad.State);
        var patchSim=Ready(75,25);ReleaseData patched=patchSim.Release();Tick(patchSim,168);
        int paidBeforePatch=patched.weeks[0].net;float oldRating=patched.rating;
        Check(patchSim.StartPatch(0), "patch starts");Check(!patchSim.StartPatch(0)&&!patchSim.Contract(), "patch occupies developer");
        Check(!patchSim.StartProject("Busy",0,0,0), "patch blocks new project");
        Tick(patchSim,23);Check(!patched.patched&&patchSim.State.patchRemaining==1,"patch takes time");Ledger(patchSim.State);
        Tick(patchSim,1);Check(patched.patched&&patched.patchStarted&&patchSim.State.patchRelease==-1,"patch completes once");
        Check(patched.rating==oldRating&&patched.weeks[0].net==paidBeforePatch,"patch does not rewrite critic scores or paid sales");
        Check(!patchSim.StartPatch(0),"patch cannot repeat");Tick(patchSim,840);Ledger(patchSim.State);
        var unfinished=Ready(55);unfinished.State.project.progress=60;Check(unfinished.Rating(unfinished.State.project)<g.rating,"early release penalty");
        Check(!unfinished.StartProject("x",0,0,0),"one project only");
        unfinished.State.project.progress=59;Check(unfinished.Release()==null,"minimum release progress");
        var repeats=Ready(55);ReleaseData first=repeats.Release();
        Check(repeats.StartProject("Again",0,2,0),"repeat starts");Check(repeats.Fatigue(repeats.State.project)<1,"market fatigue");
        repeats.State.project=null;
        Check(!repeats.StartProject("Wrong sequel",1,1,0,0,1,0,0,0),"sequel must match");
        Check(repeats.StartProject("Sequel",0,2,0,0,1,0,0,0),"successful sequel available");
        var poor=Ready(0,40);poor.Release();Check(!poor.StartProject("Sequel",0,2,0,0,1,0,0,0),"bad release cannot seed sequel");
        var costs=new Simulation(new GameState());Tick(costs,24);Check(costs.State.money==397,"daily bill");
        Check(costs.BuyUpgrade(3),"upgrade purchase");int cash=costs.State.money;Check(!costs.BuyUpgrade(3)&&costs.State.money==cash,"no duplicate upgrade");
        Tick(costs,24);Check(costs.State.money==cash-4,"equipment upkeep");Ledger(costs.State);
        var debt=new Simulation(new GameState());Tick(debt,24*140);Check(debt.State.money==0&&debt.State.arrears==20,"unpaid bills tracked");
        Check(!debt.StartProject("No",0,0,0)&&!debt.BuyUpgrade(0),"debt blocks investment");
        Check(debt.Contract(),"contract available in debt");int before=debt.State.money;Tick(debt,47);Check(debt.State.money==before,"contract not instant");
        Tick(debt,1);Check(debt.State.arrears==0&&debt.State.money>0,"contract repays debt");Ledger(debt.State);
        Check(!debt.Contract(),"contract cooldown");Tick(debt,24);Check(debt.Contract(),"contract returns");
        var tasks=Ready(30);Check(tasks.ProductionTask(1),"playtest works");int budget=tasks.State.project.cost;Check(budget==120,"task charged to budget");
        Check(!tasks.ProductionTask(1)&&tasks.State.project.cost==budget,"no repeated task");
        Check(tasks.Contract(),"contract pauses active project");float progress=tasks.State.project.progress;Tick(tasks,48);Check(tasks.State.project.progress==progress,"contract consumes developer time");Ledger(tasks.State);
        tasks.State.experience=240;Check(tasks.Research(0),"research spends experience");Check(tasks.State.experience==120&&!tasks.Research(0),"research not repeatable");Ledger(tasks.State);
        for(int id=0;id<8;id++)
        {
            var events=Ready(20);events.State.pendingEvent=id;int hour=events.State.hour;events.Tick();Check(events.State.hour==hour,"event pauses");
            Check(events.ResolveEvent(false),"event free option");Ledger(events.State);
        }
        // A conservative player should be able to keep making games; contracts have real time cost.
        var career=new Simulation(new GameState());int contracts=0;
        for(int game=0;game<5;game++)
        {
            for(int guard=0;!career.StartProject("Career "+game,game%3,game%4,0)&&guard<2000;guard++)
            {if(career.State.contractRemaining==0&&career.Contract())contracts++;Tick(career,1);}
            Check(career.State.project!=null,"career can finance project");
            for(int guard=0;career.State.project.progress<100&&guard<3000;guard++)
            {
                ProjectData p=career.State.project;
                career.State.focus=p.progress<33?WorkFocus.Code:p.progress<66?WorkFocus.Art:WorkFocus.Polish;
                if(career.State.energy<18)career.State.resting=true;
                if(career.State.energy>90)career.State.resting=false;
                if(career.State.arrears>0&&career.State.contractRemaining==0&&career.Contract())contracts++;
                Tick(career,1);
            }
            if(career.State.pendingEvent>=0)career.ResolveEvent(false);
            Check(career.State.project.progress==100,"career development completes");
            for(int guard=0;career.State.arrears>0&&guard<2000;guard++){career.Contract();Tick(career,1);}
            while(career.State.contractRemaining>0)Tick(career,1);
            ReleaseData r=career.Release();Check(r!=null,"career releases");Ledger(career.State);
            Console.WriteLine("Career "+game+": rating "+r.rating.ToString("F1")+", cash "+career.State.money+", budget "+r.cost+", planned units "+ForecastUnits(r));
        }
        Tick(career,1008);Ledger(career.State);Console.WriteLine("Career end: cash "+career.State.money+", debt "+career.State.arrears+", contracts "+contracts);
        string dir=Path.Combine(Path.GetTempPath(),"devgame-v2-"+Guid.NewGuid()),path=Path.Combine(dir,"save.dat");
        var store=new SaveRepository(path,Encode,Decode);
        try
        {
            string message;Check(store.Load(out message).version==2,"new save version");
            var active=Ready(30);active.ProductionTask(0);active.Contract();
            Check(store.Save(active.State,out message),"save active project");var resumed=new Simulation(store.Load(out message));
            Tick(active,350);Tick(resumed,350);Check(Encode(active.State)==Encode(resumed.State),"active project and rng resume exactly");
            var selling=Ready(50);selling.Release();Tick(selling,200);Check(store.Save(selling.State,out message),"save mid sales");
            resumed=new Simulation(store.Load(out message));Tick(selling,900);Tick(resumed,900);Check(Encode(selling.State)==Encode(resumed.State),"weekly sales do not duplicate after reload");Ledger(resumed.State);
            var patchSave=Ready(75,20);patchSave.Release();patchSave.StartPatch(0);Tick(patchSave,12);
            Check(store.Save(patchSave.State,out message),"save ongoing patch");
            var patchLoad=new Simulation(store.Load(out message));Tick(patchSave,1050);Tick(patchLoad,1050);
            Check(Encode(patchSave.State)==Encode(patchLoad.State),"patch and sales resume exactly");
            var legacy=new GameState{version=1,money=215,released=1};legacy.releases.Add(new ReleaseData{title="Old",review="old",rating=6,totalRevenue=600,paidRevenue=300,age=60});
            legacy.project=new ProjectData{title="Old project",progress=40};
            string body=Encode(legacy),hash;using(var sha=SHA256.Create())hash=Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(body)));
            File.WriteAllText(path,"ONE_MORE_BUILD_1\n"+hash+"\n"+body);
            GameState migrated=store.Load(out message);Check(migrated.version==2&&migrated.releases[0].legacy&&migrated.money==215,"legacy migration preserves money");
            Check(migrated.project.sequel==-1&&migrated.project.priceIndex==1,"legacy active project migrated");
            var oldSim=new Simulation(migrated);oldSim.State.resting=true;Tick(oldSim,60);Check(migrated.releases[0].paidRevenue==600,"legacy remaining revenue only");
            Check(store.Save(migrated,out message),"migrated save valid");File.WriteAllText(path,"broken");
            Check(store.Load(out message).version==2&&message!=null,"backup recovery");
            File.WriteAllText(path,"broken");File.WriteAllText(path+".bak","also broken");Check(store.Load(out message).money==400&&message!=null,"both corrupt reset safely");
            var invalid=new GameState{energy=float.NaN};Check(!store.Save(invalid,out message),"invalid save rejected");
        }
        finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        Console.WriteLine("PASS: "+assertions+" assertions");
    }
}
