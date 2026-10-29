using System;
using UnityEngine;

namespace OneMoreBuild
{
    public sealed partial class StudioApp
    {
        Vector2 actorPosition, destination;
        bool walking;
        int routeStep;
        string arrivalAction, hoverHint = "";
        float bob, feedbackUntil;
        int lastCode, lastArt, lastPolish;
        string floating = "";
        static readonly Color Border = Hex("414D48"), Paper = Hex("F6ECCE"), Gold = Hex("E5B667");
        void Frame(Rect rect, Color fill)
        {
            Box(new Rect(rect.x + 3, rect.y + 4, rect.width, rect.height), new Color(.2f, .25f, .2f, .3f));
            Box(rect, Border);
            Box(new Rect(rect.x + 3, rect.y + 3, rect.width - 6, rect.height - 6), fill);
            Box(new Rect(rect.x + 4, rect.y + 4, rect.width - 8, 2), Hex("FFFADE"));
        }
        void UpdateOffice()
        {
            if (walking)
            {
                Vector2 target = routeStep == 0 ? new Vector2(actorPosition.x, 123) : routeStep == 1 ? new Vector2(destination.x, 123) : destination;
                actorPosition = Vector2.MoveTowards(actorPosition, target, Time.unscaledDeltaTime * 100);
                bob += Time.unscaledDeltaTime * 12;
                if (Vector2.Distance(actorPosition, target) < .1f && ++routeStep > 2)
                {
                    walking = false;
                    if (arrivalAction == "desk") { S.resting = false; modal = S.project == null ? "new" : "desk"; }
                    if (arrivalAction == "break") { S.resting = true; modal = "break"; }
                    if (arrivalAction == "phone") modal = "phone";
                    if (arrivalAction == "board") modal = "history";
                    if (arrivalAction == "shop") modal = "shop";
                }
            }
            ProjectData p = S.project;
            if (p == null) { lastCode = lastArt = lastPolish = 0; return; }
            int c = (int)p.code, a = (int)p.art, f = (int)p.polish;
            if (c > lastCode) Feedback("+" + (c - lastCode) + " Код");
            else if (a > lastArt) Feedback("+" + (a - lastArt) + " Графика");
            else if (f > lastPolish) Feedback("+" + (f - lastPolish) + " Качество");
            lastCode = c; lastArt = a; lastPolish = f;
        }
        void Feedback(string text) { floating = text; feedbackUntil = Time.unscaledTime + 1.5f; }
        void WalkTo(Vector2 point, string action)
        {
            if (walking) return;
            destination = point; arrivalAction = action; routeStep = 0; walking = true;
        }
        void Hotspot(float px, float py, float pw, float ph, string hint, Vector2 target, string action)
        {
            Rect rect = new Rect(64 + px * 3, 60 + py * 3, pw * 3, ph * 3);
            bool hover = GUI.enabled && !walking && rect.Contains(Event.current.mousePosition);
            if (hover)
            {
                hoverHint = hint;
                Color edge = Hex("F9D879");
                Box(new Rect(rect.x, rect.y, rect.width, 3), edge); Box(new Rect(rect.x, rect.yMax - 3, rect.width, 3), edge);
                Box(new Rect(rect.x, rect.y, 3, rect.height), edge); Box(new Rect(rect.xMax - 3, rect.y, 3, rect.height), edge);
            }
            bool prior = GUI.enabled; GUI.enabled = prior && !walking;
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) WalkTo(target, action);
            GUI.enabled = prior;
        }
        void DrawHeader()
        {
            Frame(new Rect(16, 10, 1248, 44), Paper);
            Box(new Rect(20, 14, 192, 36), mint);
            GUIStyle light = Style(16, Color.white, true);
            Label(30, 20, 175, 26, "ЕЩЁ ОДИН БИЛД", light);
            Label(232, 20, 160, 26, "День " + (S.hour / 24 + 1), heading);
            Label(405, 20, 220, 26, S.money + " мон.", heading);
            Label(632, 22, 205, 24, "Игроки: " + S.fans);
            Label(848, 22, 160, 24, "Релизы: " + S.released);
            if (Button(1030, 15, 65, 32, "Ⅱ", true, speed == 0)) speed = 0;
            if (Button(1105, 15, 65, 32, "1×", true, speed == 1)) speed = 1;
            if (Button(1180, 15, 72, 32, "3×", true, speed == 3)) speed = 3;
        }
        void DrawRoom()
        {
            hoverHint = "";
            Rect art = new Rect(64, 60, 1152, 528);
            if (room != null) GUI.DrawTexture(art, room);
            for (int i = 0; i < upgrades.Length; i++) if (S.upgrades[i] && upgrades[i] != null) GUI.DrawTexture(art, upgrades[i]);
            int frame = celebrate > 0 ? 3 : walking ? (int)bob % 2 : S.resting || S.project == null ? 2 : speed == 0 ? 0 : (int)(Time.unscaledTime * speed * 3) % 2;
            float bounce = walking ? ((int)bob % 2) * 3 : 0;
            Rect actor = new Rect(Mathf.Round(64 + actorPosition.x * 3 - 40), Mathf.Round(60 + actorPosition.y * 3 - 84 - bounce), 80, 96);
            if (developer[frame] != null) GUI.DrawTexture(actor, developer[frame]);
            if (Time.unscaledTime < feedbackUntil)
            {
                float y = actor.y - 28 - (1.5f - (feedbackUntil - Time.unscaledTime)) * 14;
                Frame(new Rect(actor.x - 20, y, 170, 30), Paper);
                Label(actor.x - 10, y + 5, 154, 24, floating, small);
            }
            if (S.resting && !walking)
                Label(actor.x + 58, actor.y - 12, 100, 24, "Z z z", heading);
            Hotspot(169, 46, 61, 48, "КОМПЬЮТЕР · проект и задания", new Vector2(197, 121), "desk");
            Hotspot(27, 126, 80, 28, "ДИВАН · отдых", new Vector2(71, 142), "break");
            Hotspot(154, 64, 13, 22, "ТЕЛЕФОН · заказы", new Vector2(152, 100), "phone");
            Hotspot(135, 8, 45, 19, "ДОСКА · выпущенные игры", new Vector2(155, 43), "board");
            Hotspot(275, 135, 26, 24, "ДОСТАВКА · купить оборудование", new Vector2(268, 150), "shop");
            Frame(new Rect(82, 496, 265, 76), Paper);
            Label(96, 505, 240, 24, walking ? "Идёт…" : S.resting ? "Отдыхает" : S.project == null ? "Ждёт задания" : "Разработчик · " + Catalog.Focus[(int)S.focus], body);
            Label(96, 540, 84, 22, "Силы " + (int)S.energy, small);
            Bar(187, 548, 140, S.energy / 100, S.energy < 20 ? pink : Hex("66906A"));
            if (hoverHint != "")
            {
                Frame(new Rect(750, 536, 440, 36), Paper);
                Label(765, 545, 410, 24, hoverHint, small);
            }
        }
        void DrawProject()
        {
            // A slim work order leaves the office visible; detailed controls live at the desk.
            if (S.project == null) return;
            ProjectData p = S.project;
            Frame(new Rect(765, 163, 420, 76), Paper);
            Label(780, 172, 300, 26, p.title, body);
            Label(1090, 172, 80, 25, ((int)p.progress) + "%", heading);
            Bar(780, 208, 230, p.progress / 100, mint);
            Label(1028, 205, 145, 25, Catalog.Stages[Math.Min(2, (int)(p.progress / 33.334f))], small);
        }
        void DrawFooter()
        {
            Frame(new Rect(16, 598, 1248, 112), Paper);
            string[] names = { "Разработка", "Оборудование", "Релизы", "Меню" };
            for (int i = 0; i < 4; i++)
            {
                if (!Button(28 + i * 193, 610, 182, 44, names[i], !walking)) continue;
                if (i == 0) WalkTo(new Vector2(197, 121), "desk");
                if (i == 1) WalkTo(new Vector2(268, 150), "shop");
                if (i == 2) modal = "history";
                if (i == 3) modal = "menu";
            }
            if (Button(828, 610, 200, 44, S.resting ? "За работу" : "Перерыв", !walking))
                WalkTo(S.resting ? new Vector2(197, 121) : new Vector2(71, 142), S.resting ? "desk" : "break");
            if (Button(1040, 610, 210, 44, "Релиз", S.project != null && S.project.progress >= 60 && !walking)) modal = "releaseConfirm";
            Label(32, 674, 930, 24, noticeUntil > Time.unscaledTime || noticeUntil == 0 ? notice : "Компьютер — задания   •   Диван — отдых   •   Телефон — подработка", small);
            Label(1010, 674, 230, 24, "Пробел: пауза", small);
        }
        void DrawDesk()
        {
            if (S.project == null) { modal = "new"; return; }
            ProjectData p = S.project;
            ModalHeader(p.title, "Выбери направление работы или разовое задание.");
            Label(292, 211, 680, 30, "Код " + (int)p.code + "    Графика " + (int)p.art + "    Качество " + (int)p.polish + "    Ошибки " + (int)p.bugs);
            for (int i = 0; i < 3; i++)
                if (Button(292 + i * 236, 258, 224, 42, Catalog.Focus[i], true, (int)S.focus == i))
                { S.focus = (WorkFocus)i; modal = ""; Feedback("Задание: " + Catalog.Focus[i]); Save(false); }
            Label(292, 325, 680, 24, "РАЗОВЫЕ ЗАДАНИЯ · по одному на каждый проект", small);
            string[] tasks = { "Найти баг  ·  8 энергии", "Плейтест  ·  30 монет, 12 энергии", "Рывок  ·  18 энергии" };
            string[] details = { "−5 ошибок, +3 качества", "+5 графики, +7 качества", "+10% готовности, +4 кода, +2 ошибки" };
            for (int i = 0; i < 3; i++)
            {
                bool used = (p.actionsUsed & (1 << i)) != 0;
                bool affordable = S.energy >= (i == 0 ? 8 : i == 1 ? 12 : 18) && (i != 1 || S.money >= 30);
                if (Button(292, 365 + i * 60, 456, 44, used ? "Выполнено" : tasks[i], !used && affordable))
                {
                    if (i == 0) { debugMessage = ""; modal = "debug"; continue; }
                    if (sim.ProductionTask(i)) { modal = ""; Feedback(details[i]); Toast("Задание выполнено: " + details[i]); Save(false); }
                }
                Label(765, 371 + i * 60, 225, 44, details[i], small);
            }
            CloseButton("Продолжить работу");
        }
        string debugMessage = "";
        void DrawDebug()
        {
            if (S.project == null) { modal = ""; return; }
            ModalHeader("Найти баг", "Один оператор отличается от остальных. Найди его в таблице.");
            int target = (S.project.genre * 4 + S.project.theme * 2 + S.project.scope) % 9;
            for (int i = 0; i < 9; i++)
            {
                if (!Button(360 + (i % 3) * 188, 235 + (i / 3) * 86, 176, 70, i == target ? "[ != ]" : "[ == ]")) continue;
                if (i == target)
                {
                    if (sim.ProductionTask(0)) { modal = ""; Feedback("−5 ошибок"); Toast("Баг исправлен: −5 ошибок, +3 качества."); Save(false); }
                }
                else debugMessage = "Здесь всё верно. Проверь соседние ячейки.";
            }
            Label(292, 505, 680, 30, debugMessage, small);
            Label(292, 537, 680, 25, "Исправление: 8 энергии. Награда: −5 ошибок, +3 качества.", small);
            if (Button(292, 574, 696, 42, "Вернуться к заданиям")) modal = "desk";
        }
        void DrawBreak()
        {
            ModalHeader("Комната отдыха", "Энергия: " + (int)S.energy + "/100");
            Label(292, 235, 680, 70, "Отдых восстанавливает силы. Закрой окно, чтобы пошло время.", heading);
            Label(292, 350, 680, 90, "Продажи продолжаются и во время отдыха. Когда будешь готов, нажми на компьютер или «За работу».");
            CloseButton("Отдыхать");
        }
        void DrawPhone()
        {
            ModalHeader("Заказчик на линии", "Небольшая задача для чужого проекта.");
            Label(292, 245, 680, 100, "Исправить меню сохранения\nОплата: 35 монет · Затраты: 12 энергии", heading);
            Label(292, 380, 680, 50, S.hour < S.contractAt ? "Новый заказ через " + (S.contractAt - S.hour) + " ч." : "Заказ доступен.");
            if (Button(292, 470, 696, 56, "Взять заказ", S.hour >= S.contractAt && S.energy >= 12))
            { if (sim.Contract()) { modal = ""; Feedback("+35 монет"); Toast("Заказ сдан. Следующий — через сутки."); Save(false); } }
            CloseButton();
        }
        void DrawMenu()
        {
            ModalHeader("Меню", "Игра сохраняется автоматически каждые 20 секунд.");
            if (Button(292, 238, 696, 54, "Сохранить")) Save(true);
            if (Button(292, 315, 696, 54, sound ? "Звук: включён" : "Звук: выключен")) sound = !sound;
            if (Button(292, 392, 696, 54, "Как играть")) modal = "help";
            CloseButton();
        }
    }
}
