using System;
using System.Collections.Generic;

namespace OneMoreBuild
{
    public enum WorkFocus { Code, Art, Polish }
    [Serializable] public class ProjectData
    {
        public string title;
        public int genre, theme, scope;
        public float progress, code, art, polish, bugs;
    }
    [Serializable] public class ReleaseData
    {
        public string title, review;
        public float rating;
        public int totalRevenue, paidRevenue, age;
    }
    [Serializable] public class GameState
    {
        public int version = 1, money = 400, fans, experience, hour, released;
        public int randomState = 72631, eventAt = 65, lastEvent = -1, pendingEvent = -1, contractAt;
        public float energy = 100;
        public bool resting;
        public WorkFocus focus;
        public bool[] upgrades = new bool[6];
        public ProjectData project;
        public List<ReleaseData> releases = new List<ReleaseData>();
        public bool goalShown;
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
    public sealed class Simulation
    {
        public readonly GameState State;
        public Simulation(GameState state) { State = state ?? new GameState(); }
        public bool GoalReached { get { return State.released >= 3 && State.upgrades[5]; } }
        public float Speed
        {
            get { return (1 + (State.upgrades[0] ? .25f : 0) + (State.upgrades[5] ? .4f : 0)) * (State.energy < 20 ? .55f : 1); }
        }
        public bool StartProject(string title, int genre, int theme, int scope)
        {
            if (State.project != null || State.pendingEvent >= 0 || genre < 0 || genre > 2 || theme < 0 || theme > 3 || scope < 0 || scope > 1) return false;
            int price = scope == 0 ? 90 : 150;
            if (State.money < price) return false;
            title = (title ?? "").Trim();
            if (title.Length == 0) title = "Безымянный шедевр " + (State.released + 1);
            State.money -= price;
            State.project = new ProjectData { title = title.Substring(0, Math.Min(32, title.Length)), genre = genre, theme = theme, scope = scope, bugs = 3 };
            State.resting = false;
            State.eventAt = State.hour + 65;
            return true;
        }
        public bool BuyUpgrade(int index)
        {
            if (index < 0 || index >= 6 || State.upgrades[index] || State.money < Catalog.Upgrades[index].Price) return false;
            State.money -= Catalog.Upgrades[index].Price;
            State.upgrades[index] = true;
            return true;
        }
        public bool Contract()
        {
            if (State.hour < State.contractAt || State.energy < 12 || State.pendingEvent >= 0) return false;
            State.energy -= 12;
            State.money += 35;
            State.contractAt = State.hour + 24;
            return true;
        }
        public void Tick()
        {
            if (State.pendingEvent >= 0) return;
            State.hour++;
            foreach (ReleaseData release in State.releases)
            {
                if (release.age >= 120) continue;
                release.age++;
                // Cumulative entitlement avoids rounding losses and duplicate payments on reload.
                int entitled = (int)((long)release.totalRevenue * release.age / 120);
                State.money += entitled - release.paidRevenue;
                release.paidRevenue = entitled;
            }
            ProjectData p = State.project;
            if (State.resting || p == null)
            {
                State.energy = Clamp(State.energy + (State.upgrades[3] ? 1.8f : 1.2f), 0, 100);
                return;
            }
            State.energy = Clamp(State.energy - (State.upgrades[1] ? .17f : .24f), 0, 100);
            float delta = .29f * Speed / (p.scope == 0 ? 1 : 1.35f);
            p.progress = Clamp(p.progress + delta, 0, 100);
            p.code = Clamp(p.code + (State.focus == WorkFocus.Code ? .20f : .045f) * Speed, 0, 100);
            p.art = Clamp(p.art + (State.focus == WorkFocus.Art ? .20f : .045f) * Speed * (State.upgrades[4] ? 1.35f : 1), 0, 100);
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
            if (id == 5 && left && State.money < 25) return false;
            ProjectData p = State.project;
            switch (id)
            {
                case 0: if (left) { State.energy -= 12; p.bugs -= 5; } else { p.bugs += 3; p.progress += 4; } break;
                case 1: if (left) p.code += 5; else p.art += 5; break;
                case 2: if (left) { p.progress -= 4; p.code += 7; } else p.polish += 4; break;
                case 3: if (left) { State.fans += 18; p.bugs += 2; } else { State.energy -= 10; p.polish += 6; } break;
                case 4: if (left) State.energy += 15; else p.art += 5; break;
                case 5: if (left) { State.money -= 25; p.bugs -= 7; } else { State.energy -= 15; p.bugs -= 4; } break;
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
            float quality = p.code * .36f + p.art * .34f + p.polish * .30f;
            return Clamp(3 + quality * .095f + (Catalog.Fit(p.genre, p.theme) - 1) * 5 - p.bugs * .11f - (100 - p.progress) * .04f, 1, 10);
        }
        public ReleaseData Release()
        {
            ProjectData p = State.project;
            if (p == null || p.progress < 60 || State.pendingEvent >= 0) return null;
            float rating = Rating(p);
            int revenue = (int)((180 + rating * 90 + Math.Min(State.fans, 2000) * .4f) * (p.scope == 0 ? 1 : 1.4f));
            ReleaseData release = new ReleaseData {
                title = p.title, rating = rating, totalRevenue = revenue,
                review = rating >= 8 ? "«Ещё одну партию — и спать. Обещаю»." : rating >= 6 ? "«Небольшая игра с большим сердцем»." : rating >= 4 ? "«Идея хорошая. Ждём патч!»" : "«Разработчик явно чему-то научился»."
            };
            State.releases.Add(release);
            State.fans += (int)(rating * 4);
            State.experience += (int)(rating * 10);
            State.released++;
            State.project = null;
            State.resting = false;
            return release;
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
            if (s == null || s.version != 1 || s.money < 0 || s.money > 100000000 || s.fans < 0 || s.experience < 0 || s.hour < 0 || s.released < 0 || s.randomState == 0 || !Bounded(s.energy, 0, 100)) return false;
            if (s.upgrades == null || s.upgrades.Length != 6 || s.releases == null || s.releases.Count != s.released || s.released > 100000 || s.pendingEvent < -1 || s.pendingEvent >= 8 || s.lastEvent < -1 || s.lastEvent >= 8 || (int)s.focus < 0 || (int)s.focus > 2 || s.eventAt < 0 || s.contractAt < 0) return false;
            if (s.pendingEvent >= 0 && s.project == null) return false;
            ProjectData p = s.project;
            if (p != null && (string.IsNullOrWhiteSpace(p.title) || p.title.Length > 32 || p.genre < 0 || p.genre > 2 || p.theme < 0 || p.theme > 3 || p.scope < 0 || p.scope > 1 || !Bounded(p.progress, 0, 100) || !Bounded(p.bugs, 0, 100) || !Bounded(p.code, 0, 100) || !Bounded(p.art, 0, 100) || !Bounded(p.polish, 0, 100))) return false;
            foreach (ReleaseData r in s.releases)
                if (r == null || string.IsNullOrWhiteSpace(r.title) || r.review == null || !Bounded(r.rating, 1, 10) || r.age < 0 || r.age > 120 || r.totalRevenue < 0 || r.paidRevenue != (int)((long)r.totalRevenue * r.age / 120)) return false;
            return true;
        }
    }
}
