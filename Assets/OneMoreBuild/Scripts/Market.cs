using System;

namespace OneMoreBuild
{
    [Serializable] public class SalesWeek
    {
        public int plannedUnits, units, gross, refunds, fee, net;
        public bool paid;
    }
    public static class Market
    {
        public const int WeekHours = 168, Weeks = 6;
        public static readonly string[] Platforms = { "ПК", "Телефон" }, Audiences = { "Казуальная", "Опытные игроки" };
        public static readonly int[] Prices = { 5, 10, 18 }, MarketingCosts = { 0, 70, 180 };
        public static readonly string[] Marketing = { "Без рекламы", "Сообщество", "Кампания" };
        public static readonly string[] ResearchNames = { "Свой движок", "Инструменты графики", "Аналитика рынка" };
        public static readonly string[] ResearchEffects = { "+15% скорости разработки", "+15% к развитию графики", "+15% охвата релизов" };
        public static readonly float[][] Weights = { new[] { .25f, .40f, .35f }, new[] { .50f, .20f, .30f }, new[] { .25f, .20f, .55f } };
        public static int TrendGenre(int hour) { return (hour / 672) % 3; }
        public static int TrendTheme(int hour) { return ((hour / 672) * 3 + 1) % 4; }
        public static int Competitors(int hour) { return 2 + (hour / WeekHours * 7 + 3) % 4; }
        public static float AudienceFit(int genre, int audience) { return (genre == 1 ? audience == 1 : audience == 0) ? 1.15f : .85f; }
        public static float PlatformFit(int genre, int platform) { return platform == 0 ? genre == 1 ? 1.15f : 1 : genre == 2 ? 1.2f : .85f; }
    }
    public sealed partial class Simulation
    {
        public int DailyCost
        {
            get
            {
                int bill = 3;
                foreach (bool upgrade in State.upgrades) if (upgrade) bill++;
                if (State.project != null && !State.resting && State.contractRemaining == 0 && State.patchRemaining == 0 && State.arrears == 0) bill += State.project.scope == 0 ? 3 : 5;
                return bill;
            }
        }
        bool Spend(int amount)
        {
            if (State.arrears > 0 || amount < 0 || State.money < amount) return false;
            State.money -= amount; State.lifetimeCosts += amount; return true;
        }
        void Credit(int amount)
        {
            int repay = Math.Min(State.arrears, amount);
            State.arrears -= repay; State.money += amount - repay;
        }
        public bool StartPatch(int index)
        {
            if(index<0 || index>=State.releases.Count || State.contractRemaining>0 || State.patchRemaining>0 || State.pendingEvent>=0 || State.energy<15) return false;
            ReleaseData r=State.releases[index];
            if(r.legacy || r.patchStarted || r.age>=Market.WeekHours*(Market.Weeks-1) || !Spend(40)) return false;
            State.energy-=15; State.patchRemaining=24; State.patchRelease=index; r.patchStarted=true; r.cost+=40;
            return true;
        }
        public bool Research(int index)
        {
            if (index < 0 || index >= 3 || State.research[index] > 0 || State.experience < 120 || !Spend(180)) return false;
            State.experience -= 120; State.research[index] = 1; return true;
        }
        public float Fatigue(ProjectData p)
        {
            int repeats = 0;
            for (int i = Math.Max(0, State.releases.Count - 3); i < State.releases.Count; i++)
            {
                ReleaseData r = State.releases[i];
                if (!r.legacy && r.genre == p.genre && r.theme == p.theme) repeats++;
            }
            return Math.Max(.55f, 1 - repeats * .15f);
        }
        ReleaseData MakeRelease(ProjectData p)
        {
            float rating = Rating(p);
            int price = Market.Prices[p.priceIndex];
            float trend = 1 + (Market.TrendGenre(State.hour) == p.genre ? .15f : 0) + (Market.TrendTheme(State.hour) == p.theme ? .1f : 0);
            float fatigue = Fatigue(p), competition = 1f / (1 + Market.Competitors(State.hour) * .12f);
            float fair = (p.platform == 0 ? 10 : 5) * (.7f + rating * .05f);
            double ratio = Math.Log(price / fair);
            double priceDemand = Math.Exp(-ratio * ratio * 1.7) * Math.Sqrt(fair / price);
            double reach = (140 + Math.Min(State.fans, 1200) * .12 + (p.marketing == 1 ? 60 : p.marketing == 2 ? 150 : 0)) * (1 + State.research[2] * .15);
            double demand = reach * Math.Pow(rating / 10, 1.8) * priceDemand * trend * fatigue * competition * Catalog.Fit(p.genre, p.theme) * Market.AudienceFit(p.genre, p.audience) * Market.PlatformFit(p.genre, p.platform) * (p.scope == 0 ? 1 : 1.55) * (p.sequel >= 0 ? 1.18 : 1);
            int totalUnits = Math.Max(0, (int)demand);
            SalesWeek[] weeks = new SalesWeek[Market.Weeks];
            int[] shares = { 38, 24, 16, 10, 7, 5 };
            int allocated = 0;
            for (int i = 0; i < weeks.Length; i++)
            {
                int units = i == weeks.Length - 1 ? totalUnits - allocated : totalUnits * shares[i] / 100;
                weeks[i] = new SalesWeek { plannedUnits = units }; allocated += units;
            }
            string analysis = "Код/графика/полировка: " + (int)p.code + "/" + (int)p.art + "/" + (int)p.polish + ".\nОшибки: −" + (p.bugs * .13f).ToString("F1") + "; ранний релиз: −" + ((100-p.progress)*.05f).ToString("F1") + " к оценке.\nСпрос: тренд ×" + trend.ToString("F2") + ", конкуренция ×" + competition.ToString("F2") + ", повторы ×" + fatigue.ToString("F2") + ".\nЖанр/тема ×" + Catalog.Fit(p.genre,p.theme).ToString("F2") + ", аудитория ×" + Market.AudienceFit(p.genre,p.audience).ToString("F2") + ", цена ×" + priceDemand.ToString("F2") + ".";
            int[] scores = new int[4];
            for (int i = 0; i < scores.Length; i++) scores[i] = (int)Math.Round(Clamp(rating + (NextRandom() % 11 - 5) * .12f, 1, 10));
            return new ReleaseData { title = p.title, rating = rating, genre = p.genre, theme = p.theme, platform = p.platform, audience = p.audience, marketing = p.marketing, price = price, cost = p.cost, launchedAt = State.hour, code = p.code, art = p.art, polish = p.polish, bugs = p.bugs, completion = p.progress, trend = trend, competition = competition, fatigue = fatigue, sequel = p.sequel, weeks = weeks, scores = scores, analysis = analysis, review = rating >= 8 ? "Сильный релиз. Игроки рекомендуют друзьям." : rating >= 6 ? "Хорошая основа. Есть что улучшить." : rating >= 4 ? "Интересная идея, слабая реализация." : "Сырой проект. Игроки требуют возвраты." };
        }
        void ProcessSales()
        {
            foreach (ReleaseData r in State.releases)
            {
                if (r.legacy)
                {
                    if (r.age >= 120) continue;
                    r.age++; int entitled = (int)((long)r.totalRevenue * r.age / 120);
                    int payment = entitled - r.paidRevenue; r.paidRevenue = entitled;
                    State.lifetimeSales += payment; Credit(payment); continue;
                }
                r.age = Math.Min(Market.Weeks * Market.WeekHours, r.age + 1);
                for (int i = 0; i < r.weeks.Length; i++)
                {
                    SalesWeek w = r.weeks[i];
                    if (w.paid || r.age < (i + 1) * Market.WeekHours) continue;
                    // Locked launch demand + bounded per-week variation. UI never consumes RNG.
                    float variation = .88f + (NextRandom() % 25) * .01f;
                    w.units = (int)(w.plannedUnits * variation);
                    float refundRate = Clamp(.025f + r.bugs * (r.patched ? .002f : .008f) + (100 - r.completion) * (r.patched ? .001f : .002f) + (r.rating < 5 ? .12f : 0), .02f, .55f);
                    w.refunds = (int)(w.units * refundRate);
                    w.gross = w.units * r.price;
                    int retained = w.gross - w.refunds * r.price;
                    w.fee = retained * 30 / 100; w.net = retained - w.fee; w.paid = true;
                    r.units += w.units; r.gross += w.gross; r.refunds += w.refunds; r.refundMoney += w.refunds * r.price; r.fee += w.fee; r.paidRevenue += w.net; r.totalRevenue = r.paidRevenue;
                    int fans = (int)((w.units - w.refunds) * (r.rating >= 7 ? .2f : r.rating >= 5 ? .06f : -.08f));
                    fans = Math.Max(-State.fans, fans); State.fans += fans; r.fanChange += fans;
                    State.lifetimeSales += w.net; Credit(w.net);
                }
            }
        }
        public static void Migrate(GameState s)
        {
            if (s == null || s.version != 1) return;
            if (s.releases != null) foreach (ReleaseData r in s.releases) if (r != null) r.legacy = true;
            if (s.project != null) { s.project.sequel = -1; s.project.priceIndex = 1; s.project.startedAt = s.hour; s.project.cost = s.project.scope == 0 ? 90 : 150; }
            s.research = new int[3]; s.patchRelease = -1; s.version = 2;
        }
        static bool ValidEconomy(GameState s)
        {
            if (s.arrears < 0 || s.lifetimeCosts < 0 || s.lifetimeSales < 0 || s.lifetimeContracts < 0 || s.contractRemaining < 0 || s.contractRemaining > 48 || (s.contractRemaining > 0 ? s.contractReward != 90 : s.contractReward != 0) || s.research == null || s.research.Length != 3) return false;
            if(s.patchRemaining<0 || s.patchRemaining>24 || (s.patchRemaining>0 && (s.patchRelease<0 || s.patchRelease>=s.releases.Count || s.contractRemaining>0 || s.releases[s.patchRelease]==null || !s.releases[s.patchRelease].patchStarted || s.releases[s.patchRelease].patched)) || (s.patchRemaining==0 && s.patchRelease!=-1)) return false;
            foreach (int level in s.research) if (level < 0 || level > 1) return false;
            ProjectData p = s.project;
            return p == null || (p.platform >= 0 && p.platform < 2 && p.audience >= 0 && p.audience < 2 && p.marketing >= 0 && p.marketing < 3 && p.priceIndex >= 0 && p.priceIndex < 3 && p.sequel >= -1 && p.sequel < s.releases.Count && p.startedAt >= 0 && p.startedAt <= s.hour && p.cost >= 0 && p.workHours >= 0);
        }
        static bool ValidRelease(ReleaseData r)
        {
            if (r == null || string.IsNullOrWhiteSpace(r.title) || r.review == null || !Bounded(r.rating,1,10) || r.age < 0 || r.paidRevenue < 0) return false;
            if (r.legacy) return r.age <= 120 && r.totalRevenue >= 0 && r.paidRevenue == (int)((long)r.totalRevenue * r.age / 120);
            if (r.weeks == null || r.weeks.Length != Market.Weeks || r.scores == null || r.scores.Length != 4 || r.analysis == null || r.genre < 0 || r.genre > 2 || r.theme < 0 || r.theme > 3 || r.platform < 0 || r.platform > 1 || r.audience < 0 || r.audience > 1 || Array.IndexOf(Market.Prices,r.price) < 0 || r.cost < 0 || r.age > Market.Weeks * Market.WeekHours || !Bounded(r.bugs,0,100) || !Bounded(r.completion,60,100) || !Bounded(r.trend,1,1.3f) || !Bounded(r.competition,0,1) || !Bounded(r.fatigue,.5f,1)) return false;
            foreach(int score in r.scores) if(score < 1 || score > 10) return false;
            int units=0,gross=0,refunds=0,fee=0,net=0;
            for(int i=0;i<r.weeks.Length;i++)
            {
                SalesWeek w=r.weeks[i]; if(w==null || w.plannedUnits<0 || w.units<0 || w.refunds<0 || w.refunds>w.units || w.paid != (r.age >= (i+1)*Market.WeekHours)) return false;
                if(!w.paid && (w.units!=0 || w.gross!=0 || w.refunds!=0 || w.fee!=0 || w.net!=0)) return false;
                if(w.paid && (w.gross!=w.units*r.price || w.fee!=(w.gross-w.refunds*r.price)*30/100 || w.net!=w.gross-w.refunds*r.price-w.fee)) return false;
                units+=w.units;gross+=w.gross;refunds+=w.refunds;fee+=w.fee;net+=w.net;
            }
            return r.units==units && r.gross==gross && r.refunds==refunds && r.refundMoney==refunds*r.price && r.fee==fee && r.paidRevenue==net && r.totalRevenue==net;
        }
    }
}
