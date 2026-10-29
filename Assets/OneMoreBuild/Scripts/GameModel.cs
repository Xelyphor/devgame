using System;
using System.Collections.Generic;

namespace OneMoreBuild
{
    public enum WorkFocus { Code, Art, Polish }
    [Serializable] public class ProjectData
    {
        public string title;
        public int genre, theme, scope;
        public int actionsUsed;
        public int platform, priceIndex = 1, audience, marketing, sequel = -1, startedAt, cost, workHours;
        public float progress, code, art, polish, bugs;
    }
    [Serializable] public class ReleaseData
    {
        public string title, review;
        public float rating;
        public int totalRevenue, paidRevenue, age;
        public bool legacy, patchStarted, patched;
        public int genre, theme, platform, price, audience, marketing, cost, launchedAt, units, gross, fee, refunds, refundMoney, fanChange, sequel = -1;
        public float code, art, polish, bugs, completion, trend, competition, fatigue;
        public string analysis;
        public int[] scores;
        public SalesWeek[] weeks;
    }
    [Serializable] public class GameState
    {
        public int version = 2, money = 400, fans, experience, hour, released;
        public int randomState = 72631, eventAt = 65, lastEvent = -1, pendingEvent = -1, contractAt;
        public float energy = 100;
        public bool resting;
        public WorkFocus focus;
        public bool[] upgrades = new bool[6];
        public ProjectData project;
        public List<ReleaseData> releases = new List<ReleaseData>();
        public bool goalShown;
        public int arrears, lifetimeCosts, lifetimeSales, lifetimeContracts, contractRemaining, contractReward;
        public int[] research = new int[3];
        public int patchRemaining, patchRelease = -1;
    }
    public sealed class UpgradeDefinition
    {
        public readonly string Name, Description;
        public readonly int Price;
        public UpgradeDefinition(string name, string description, int price)
        { Name = name; Description = description; Price = price; }
    }
    public sealed class EventDefinition
    {
        public readonly string Title, Body, Left, Right;
        public EventDefinition(string title, string body, string left, string right)
        { Title = title; Body = body; Left = left; Right = right; }
    }
    public static class Catalog
    {
        public static readonly string[] Genres = { "Приключение", "Стратегия", "Головоломка" };
        public static readonly string[] Themes = { "Космос", "Котики", "Подземелья", "Город" };
        public static readonly string[] Stages = { "Прототип", "Производство", "Доводка" };
        public static readonly string[] Focus = { "Код", "Графика", "Полировка" };
        // Rows: genres; columns: themes. No combination makes a project unviable.
        static readonly float[,] Fits = { { 1.1f, 1.05f, 1.2f, 1f }, { 1.2f, 1f, 1.05f, 1.1f }, { 1.05f, 1.2f, 1f, 1.1f } };
        public static float Fit(int genre, int theme) { return Fits[genre, theme]; }
        public static readonly UpgradeDefinition[] Upgrades = {
            new UpgradeDefinition("Тихий компьютер", "+25% к скорости разработки", 300),
            new UpgradeDefinition("Удобное кресло", "Усталость от работы ниже на 30%", 200),
            new UpgradeDefinition("Отладчик", "Полировка исправляет ошибки вдвое быстрее", 250),
            new UpgradeDefinition("Растение и лампа", "Отдых восстанавливает на 50% больше энергии", 150),
            new UpgradeDefinition("Графический планшет", "+35% к развитию графики", 280),
            new UpgradeDefinition("Рабочая станция", "+40% к скорости. Цель первой главы", 1100)
        };
        public static readonly EventDefinition[] Events = {
            new EventDefinition("Он же вчера работал", "В коде поселился упрямый баг. Кажется, он уже платит аренду.", "Разобраться: −12 энергии, −5 ошибок", "Обойти: +3 ошибки, +4% прогресса"),
            new EventDefinition("Внезапное вдохновение", "Идея пришла, пока ты смотрел на чайник.", "Записать: +5 к коду", "Нарисовать: +5 к графике"),
            new EventDefinition("Ещё одна маленькая фича", "А что, если добавить рыбалку? В любую игру.", "Добавить: −4% прогресса, +7 к коду", "Беречь замысел: +4 к полировке"),
            new EventDefinition("Привет, это стример", "Небольшой канал хочет показать ранний билд.", "Отправить: +18 поклонников, +2 ошибки", "Подготовить: −10 энергии, +6 к полировке"),
            new EventDefinition("Кот на клавиатуре", "На экране: ыыыыы. Зато кот доволен.", "Погладить: +15 энергии", "Новый маскот: +5 к графике"),
            new EventDefinition("Тестировщик-друг", "Друг готов проверить игру за пиццу.", "Заказать: −25 монет, −7 ошибок", "Проверить вместе: −15 энергии, −4 ошибки"),
            new EventDefinition("Ночь саундтреков", "Идеальная мелодия нашлась в три часа ночи.", "Поймать настроение: −12 энергии, +6 к полировке", "Пойти спать: +18 энергии"),
            new EventDefinition("Маленькое сообщество", "Игроки спорят, какого цвета должна быть дверь.", "Послушать: +12 поклонников", "Объяснить идею: +5 к полировке")
        };
    }
    // No Unity dependency: deterministic simulation can be tested outside the editor.
    public sealed partial class Simulation
    {
        public readonly GameState State;
        public Simulation(GameState state) { State = state ?? new GameState(); }
        public bool GoalReached { get { return State.released >= 3 && State.upgrades[5]; } }
        public float Speed
        {
            get { return (1 + State.research[0] * .15f + (State.upgrades[0] ? .25f : 0) + (State.upgrades[5] ? .4f : 0)) * (State.energy < 20 ? .55f : 1); }
        }
        public bool StartProject(string title, int genre, int theme, int scope)
        { return StartProject(title, genre, theme, scope, 0, 1, 0, 0, -1); }
        public bool StartProject(string title, int genre, int theme, int scope, int platform, int priceIndex, int audience, int marketing, int sequel)
        {
            if (State.project != null || State.pendingEvent >= 0 || State.contractRemaining > 0 || State.patchRemaining > 0 || State.arrears > 0 || genre < 0 || genre > 2 || theme < 0 || theme > 3 || scope < 0 || scope > 1 || platform < 0 || platform > 1 || priceIndex < 0 || priceIndex > 2 || audience < 0 || audience > 1 || marketing < 0 || marketing > 2) return false;
            if (sequel < -1 || sequel >= State.releases.Count) return false;
            if (sequel >= 0 && (State.releases[sequel].legacy || State.releases[sequel].rating < 6 || State.releases[sequel].genre != genre || State.releases[sequel].theme != theme)) return false;
            int cost = (scope == 0 ? 90 : 150) + Market.MarketingCosts[marketing];
            if (!Spend(cost)) return false;
            title = (title ?? "").Trim();
            if (title.Length == 0) title = "Проект " + (State.released + 1);
            State.project = new ProjectData { title = title.Substring(0, Math.Min(32, title.Length)), genre = genre, theme = theme, scope = scope, bugs = 3, platform = platform, priceIndex = priceIndex, audience = audience, marketing = marketing, sequel = sequel, cost = cost, startedAt = State.hour };
            State.resting = false;
            State.eventAt = State.hour + 65;
            return true;
        }
        public bool BuyUpgrade(int index)
        {
            if (index < 0 || index >= 6 || State.upgrades[index] || State.money < Catalog.Upgrades[index].Price) return false;
            if (State.arrears > 0 || !Spend(Catalog.Upgrades[index].Price)) return false;
            State.upgrades[index] = true;
            return true;
        }
        // Each production task may be used once per project. Old v1 saves default to zero.
        public bool ProductionTask(int task)
        {
            ProjectData p = State.project;
            if (p == null || State.resting || State.pendingEvent >= 0 || State.contractRemaining > 0 || State.patchRemaining > 0 || task < 0 || task > 2 || (p.actionsUsed & (1 << task)) != 0) return false;
            int energy = task == 0 ? 8 : task == 1 ? 12 : 18;
            int money = task == 1 ? 30 : 0;
            if (State.energy < energy || State.money < money || State.arrears > 0) return false;
            Spend(money); p.cost += money; State.energy -= energy;
            p.actionsUsed |= 1 << task;
            if (task == 0) { p.bugs = Clamp(p.bugs - 5, 0, 100); p.polish = Clamp(p.polish + 3, 0, 100); }
            if (task == 1) { p.art = Clamp(p.art + 5, 0, 100); p.polish = Clamp(p.polish + 7, 0, 100); }
            if (task == 2) { p.progress = Clamp(p.progress + 10, 0, 100); p.code = Clamp(p.code + 4, 0, 100); p.bugs = Clamp(p.bugs + 2, 0, 100); }
            return true;
        }
        public bool Contract()
        {
            if (State.contractRemaining > 0 || State.patchRemaining > 0 || State.hour < State.contractAt || State.energy < 20 || State.pendingEvent >= 0) return false;
            State.energy -= 20;
            State.contractRemaining = 48;
            State.contractReward = 90;
            State.contractAt = State.hour + 72;
            return true;
        }
        public void Tick()
        {
            if (State.pendingEvent >= 0) return;
            State.hour++;
            ProcessSales();
            if (State.hour % 24 == 0)
            {
                int bill = DailyCost;
                State.lifetimeCosts += bill;
                int paid = Math.Min(State.money, bill); State.money -= paid; State.arrears += bill - paid;
                if (State.project != null && State.contractRemaining == 0 && State.patchRemaining == 0 && !State.resting) State.project.cost += bill;
                if (State.patchRemaining > 0) State.releases[State.patchRelease].cost += bill;
            }
            if (State.contractRemaining > 0)
            {
                State.contractRemaining--;
                State.energy = Clamp(State.energy + .4f, 0, 100);
                if (State.contractRemaining == 0) { State.lifetimeContracts += State.contractReward; Credit(State.contractReward); State.contractReward = 0; }
                return;
            }
            if (State.patchRemaining > 0)
            {
                State.patchRemaining--;
                if (State.patchRemaining == 0)
                {
                    ReleaseData patched = State.releases[State.patchRelease];
                    patched.patched = true;
                    foreach (SalesWeek week in patched.weeks) if (!week.paid) week.plannedUnits = (int)(week.plannedUnits * 1.1f);
                    State.patchRelease = -1;
                }
                return;
            }
            ProjectData p = State.project;
            if (State.resting || p == null || State.arrears > 0)
            {
                State.energy = Clamp(State.energy + (State.upgrades[3] ? 1.8f : 1.2f), 0, 100);
                return;
            }
            State.energy = Clamp(State.energy - (State.upgrades[1] ? .17f : .24f), 0, 100);
            p.workHours++;
            float delta = .29f * Speed / (p.scope == 0 ? 1 : 1.35f);
            p.progress = Clamp(p.progress + delta, 0, 100);
            p.code = Clamp(p.code + (State.focus == WorkFocus.Code ? .20f : .045f) * Speed, 0, 100);
            p.art = Clamp(p.art + (State.focus == WorkFocus.Art ? .20f : .045f) * Speed * (State.upgrades[4] ? 1.35f : 1) * (1 + State.research[1] * .15f), 0, 100);
            p.polish = Clamp(p.polish + (State.focus == WorkFocus.Polish ? .20f : .025f) * Speed, 0, 100);
            if (State.focus == WorkFocus.Polish) p.bugs -= State.upgrades[2] ? .26f : .13f;
            else p.bugs += State.energy < 20 ? .075f : .025f;
            p.bugs = Clamp(p.bugs, 0, 100);
            if (State.hour >= State.eventAt)
            {
                int next = NextRandom() % Catalog.Events.Length;
                if (next == State.lastEvent) next = (next + 1) % Catalog.Events.Length;
                State.pendingEvent = next;
                State.lastEvent = next;
                State.eventAt = State.hour + 85 + NextRandom() % 40;
            }
        }
        public bool ResolveEvent(bool left)
        {
            if (State.pendingEvent < 0 || State.project == null) return false;
            int id = State.pendingEvent;
            if (id == 5 && left && (State.money < 25 || State.arrears > 0)) return false;
            ProjectData p = State.project;
            switch (id)
            {
                case 0: if (left) { State.energy -= 12; p.bugs -= 5; } else { p.bugs += 3; p.progress += 4; } break;
                case 1: if (left) p.code += 5; else p.art += 5; break;
                case 2: if (left) { p.progress -= 4; p.code += 7; } else p.polish += 4; break;
                case 3: if (left) { State.fans += 18; p.bugs += 2; } else { State.energy -= 10; p.polish += 6; } break;
                case 4: if (left) State.energy += 15; else p.art += 5; break;
                case 5: if (left) { Spend(25); p.cost += 25; p.bugs -= 7; } else { State.energy -= 15; p.bugs -= 4; } break;
                case 6: if (left) { State.energy -= 12; p.polish += 6; } else State.energy += 18; break;
                case 7: if (left) State.fans += 12; else p.polish += 5; break;
            }
            State.energy = Clamp(State.energy, 0, 100);
            p.progress = Clamp(p.progress, 0, 100); p.bugs = Clamp(p.bugs, 0, 100);
            p.code = Clamp(p.code, 0, 100); p.art = Clamp(p.art, 0, 100); p.polish = Clamp(p.polish, 0, 100);
            State.pendingEvent = -1;
            return true;
        }
        public float Rating(ProjectData p)
        {
            float[] weights = Market.Weights[p.genre];
            float quality = p.code * weights[0] + p.art * weights[1] + p.polish * weights[2];
            return Clamp(2 + quality * .11f + (Catalog.Fit(p.genre, p.theme) - 1) * 4 - p.bugs * .13f - (100 - p.progress) * .05f, 1, 10);
        }
        public ReleaseData Release()
        {
            ProjectData p = State.project;
            if (p == null || p.progress < 60 || State.pendingEvent >= 0 || State.contractRemaining > 0 || State.patchRemaining > 0 || State.arrears > 0) return null;
            ReleaseData r = MakeRelease(p);
            State.releases.Add(r);
            State.experience += (int)(r.rating * 12);
            State.released++;
            State.project = null;
            State.resting = false;
            return r;
        }
        int NextRandom()
        {
            uint x = (uint)State.randomState;
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            State.randomState = (int)x;
            return (int)(x & 0x7fffffff);
        }
        public static float Clamp(float value, float min, float max) { return Math.Max(min, Math.Min(max, value)); }
        static bool Bounded(float value, float min, float max) { return !float.IsNaN(value) && !float.IsInfinity(value) && value >= min && value <= max; }
        public static bool IsValid(GameState s)
        {
            if (s == null || s.version != 2 || s.money < 0 || s.money > 100000000 || s.fans < 0 || s.experience < 0 || s.hour < 0 || s.released < 0 || s.randomState == 0 || !Bounded(s.energy, 0, 100)) return false;
            if (s.upgrades == null || s.upgrades.Length != 6 || s.releases == null || s.releases.Count != s.released || s.released > 100000 || s.pendingEvent < -1 || s.pendingEvent >= 8 || s.lastEvent < -1 || s.lastEvent >= 8 || (int)s.focus < 0 || (int)s.focus > 2 || s.eventAt < 0 || s.contractAt < 0) return false;
            if (!ValidEconomy(s)) return false;
            if (s.pendingEvent >= 0 && s.project == null) return false;
            ProjectData p = s.project;
            if (p != null && (string.IsNullOrWhiteSpace(p.title) || p.title.Length > 32 || p.genre < 0 || p.genre > 2 || p.theme < 0 || p.theme > 3 || p.scope < 0 || p.scope > 1 || p.actionsUsed < 0 || p.actionsUsed > 7 || !Bounded(p.progress, 0, 100) || !Bounded(p.bugs, 0, 100) || !Bounded(p.code, 0, 100) || !Bounded(p.art, 0, 100) || !Bounded(p.polish, 0, 100))) return false;
            foreach (ReleaseData r in s.releases)
                if (!ValidRelease(r)) return false;
            return true;
        }
    }
}
