using System;
using System.IO;
using UnityEngine;

namespace OneMoreBuild
{
    public sealed class StudioApp : MonoBehaviour
    {
        const float Width = 1280, Height = 720;
        Simulation sim;
        SaveRepository saves;
        Texture2D room;
        readonly Texture2D[] upgrades = new Texture2D[6], developer = new Texture2D[4];
        Font font;
        GUIStyle body, small, title, heading, button, input, micro;
        Color ink = Hex("151C30"), panel = Hex("242E45"), muted = Hex("ABB6C8"), cream = Hex("F5DDC1"), mint = Hex("96DFC3"), pink = Hex("E9A8AF");
        string modal = "", projectTitle = "Котики за пределами орбиты", notice = "Первый шаг: придумай свою игру. Всё начинается с маленького билда.";
        int genre, theme = 1, scope, speed = 1;
        readonly GameClock clock = new GameClock();
        float autosave, celebrate, noticeUntil;
        bool sound = true;
        AudioSource audioSource;
        AudioClip click;
        ReleaseData lastRelease;
        string releaseReasons;
        Vector2 historyScroll;
        GameState S { get { return sim.State; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindObjectOfType<StudioApp>() != null) return;
            GameObject app = new GameObject("One More Build");
            DontDestroyOnLoad(app);
            app.AddComponent<StudioApp>();
        }
        void Awake()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = false;
            Camera camera = new GameObject("Studio Camera").AddComponent<Camera>();
            camera.transform.SetParent(transform);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ink;
            camera.orthographic = true;
            camera.gameObject.AddComponent<AudioListener>();
            font = Resources.Load<Font>("Fonts/DejaVuSans");
            room = LoadTexture("room");
            for (int i = 0; i < upgrades.Length; i++) upgrades[i] = LoadTexture("upgrade" + i);
            for (int i = 0; i < developer.Length; i++) developer[i] = LoadTexture("developer" + i);
            saves = new SaveRepository(Path.Combine(Application.persistentDataPath, "studio-save.dat"), s => JsonUtility.ToJson(s), json => JsonUtility.FromJson<GameState>(json));
            string message;
            sim = new Simulation(saves.Load(out message));
            if (message != null) notice = message;
            audioSource = gameObject.AddComponent<AudioSource>();
            click = AudioClip.Create("Soft UI chime", 4410, 1, 44100, false);
            float[] samples = new float[4410];
            for (int i = 0; i < samples.Length; i++) samples[i] = (float)Math.Sin(i * 2 * Math.PI * 660 / 44100) * .035f * (1 - i / (float)samples.Length);
            click.SetData(samples, 0);
        }
        Texture2D LoadTexture(string name)
        {
            Texture2D texture = Resources.Load<Texture2D>("Art/" + name);
            if (texture != null) texture.filterMode = FilterMode.Point;
            return texture;
        }
        void Update()
        {
            celebrate = Mathf.Max(0, celebrate - Time.unscaledDeltaTime);
            autosave += Time.unscaledDeltaTime;
            clock.Advance(Time.unscaledDeltaTime, speed, () => modal.Length > 0 || S.pendingEvent >= 0, sim.Tick);
            if (autosave >= 20) { Save(false); autosave = 0; }
            if (sim.GoalReached && !S.goalShown && modal.Length == 0 && S.pendingEvent < 0)
            { S.goalShown = true; modal = "goal"; Save(false); }
        }
        void OnApplicationPause(bool paused) { if (paused && sim != null) Save(false); }
        void OnApplicationQuit() { if (sim != null) Save(false); }
        void OnDestroy() { if (click != null) Destroy(click); }
        void Save(bool show)
        {
            string error;
            if (!saves.Save(S, out error)) Toast("Не удалось сохранить: " + error);
            else if (show) Toast("Сохранено. Твой следующий билд подождёт.");
        }
        void Toast(string text) { notice = text; noticeUntil = Time.unscaledTime + 8; }
        static Color Hex(string hex) { Color color; ColorUtility.TryParseHtmlString("#" + hex, out color); return color; }
        GUIStyle Style(int size, Color color, bool bold = false)
        {
            return new GUIStyle(GUI.skin.label) { font = font, fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal, wordWrap = true, richText = false, normal = { textColor = color }, padding = new RectOffset(0, 0, 0, 0) };
        }
        void InitStyles()
        {
            if (body != null) return;
            body = Style(17, cream); small = Style(14, muted); micro = Style(12, muted);
            title = Style(32, cream, true); heading = Style(22, cream, true);
            button = Style(16, cream, true); button.alignment = TextAnchor.MiddleCenter;
            input = new GUIStyle(GUI.skin.textField) { font = font, fontSize = 20, padding = new RectOffset(12, 12, 12, 10), normal = { textColor = Color.white }, focused = { textColor = Color.white } };
        }
        void Box(Rect r, Color color) { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
        void Label(float x, float y, float w, float h, string text, GUIStyle style = null) { GUI.Label(new Rect(x, y, w, h), text, style ?? body); }
        bool Button(float x, float y, float w, float h, string text, bool enabled = true, bool active = false)
        {
            Rect r = new Rect(x, y, w, h);
            bool usable = enabled && GUI.enabled;
            bool hover = usable && r.Contains(Event.current.mousePosition);
            Box(new Rect(x, y + 3, w, h), Hex("101728"));
            Box(r, !usable ? Hex("30394B") : active ? Hex("497F79") : hover ? Hex("52627C") : Hex("3A4963"));
            Box(new Rect(x, y, w, 2), active ? mint : Hex("65718A"));
            Color old = GUI.color;
            if (!usable) GUI.color = new Color(1, 1, 1, .45f);
            GUI.Label(r, text, button); GUI.color = old;
            bool prior = GUI.enabled; GUI.enabled = usable;
            bool pressed = GUI.Button(r, GUIContent.none, GUIStyle.none);
            GUI.enabled = prior;
            if (pressed && sound) audioSource.PlayOneShot(click);
            return pressed;
        }
        void Bar(float x, float y, float w, float fraction, Color color)
        {
            Box(new Rect(x, y, w, 7), Hex("121B2D"));
            Box(new Rect(x, y, Mathf.Max(0, w * Mathf.Clamp01(fraction)), 7), color);
        }
        void OnGUI()
        {
            if (sim == null) return;
            InitStyles();
            float scale = Mathf.Min(Screen.width / Width, Screen.height / Height);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - Width * scale) / 2, (Screen.height - Height * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            Box(new Rect(0, 0, Width, Height), ink);
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Escape && modal != "") { modal = ""; GUI.FocusControl(null); Event.current.Use(); }
                else if (Event.current.keyCode == KeyCode.Space && modal == "" && S.pendingEvent < 0) { speed = speed == 0 ? 1 : 0; Event.current.Use(); }
            }
            bool blocked = modal != "" || S.pendingEvent >= 0;
            GUI.enabled = !blocked;
            DrawHeader(); DrawRoom(); DrawProject(); DrawFooter();
            GUI.enabled = true;
            if (blocked)
            {
                Box(new Rect(0, 0, Width, Height), new Color(.035f, .05f, .10f, .88f));
                Box(new Rect(256, 76, 768, 568), Hex("101728"));
                Box(new Rect(260, 72, 760, 568), panel);
                Box(new Rect(260, 72, 760, 3), mint);
                if (S.pendingEvent >= 0) DrawEvent();
                else DrawModal();
            }
            GUI.matrix = previous;
        }
        void DrawHeader()
        {
            Label(32, 21, 540, 44, "ЕЩЁ ОДИН БИЛД", title);
            Label(34, 65, 620, 25, "Маленькая комната. Большие идеи.", small);
            Label(938, 28, 130, 25, "День " + (S.hour / 24 + 1) + " · " + (S.hour % 24).ToString("00") + ":00", small);
            if (Button(1080, 24, 48, 36, "Ⅱ", true, speed == 0)) speed = 0;
            if (Button(1136, 24, 48, 36, "1×", true, speed == 1)) speed = 1;
            if (Button(1192, 24, 56, 36, "3×", true, speed == 3)) speed = 3;
            Box(new Rect(32, 105, 1216, 48), panel);
            Label(50, 115, 198, 28, S.money + "  монет", heading);
            Label(278, 117, 175, 25, "Энергия  " + Mathf.RoundToInt(S.energy) + "%", small);
            Bar(457, 124, 116, S.energy / 100, S.energy < 20 ? pink : mint);
            Label(614, 117, 230, 26, "Поклонники  " + S.fans);
            Label(887, 117, 170, 26, "Опыт  " + S.experience);
            Label(1084, 117, 152, 26, "Игр  " + S.released + " / 3", small);
        }
        void DrawRoom()
        {
            Box(new Rect(32, 174, 792, 448), Hex("101728"));
            Rect art = new Rect(44, 182, 768, 432);
            if (room != null) GUI.DrawTexture(art, room, ScaleMode.StretchToFill);
            for (int i = 0; i < 6; i++) if (S.upgrades[i] && upgrades[i] != null) GUI.DrawTexture(art, upgrades[i]);
            bool rest = S.resting || S.project == null;
            int frame = celebrate > 0 ? 3 : rest ? 2 : speed == 0 ? 0 : (int)(Time.unscaledTime * 4) % 2;
            // Artwork is authored on a 384x216 pixel grid; all overlays share the grid.
            Rect person = rest ? new Rect(44 + 47 * 2, 182 + 132 * 2, 80, 96) : new Rect(44 + 175 * 2, 182 + 110 * 2, 80, 96);
            if (developer[frame] != null) GUI.DrawTexture(person, developer[frame]);
            // Quiet animated rain beyond the window; integer coordinates preserve pixel edges.
            for (int i = 0; i < 10; i++)
            {
                int px = 229 + (i * 19 % 101), py = 51 + ((int)(Time.unscaledTime * 17) + i * 9) % 48;
                Box(new Rect(44 + px * 2, 182 + py * 2, 2, 4), new Color(.7f, .8f, 1, .18f));
            }
            string status = celebrate > 0 ? "Билд отправлен в мир!" : rest ? "Пауза для хороших идей" : speed == 0 ? "Время на паузе" : "Тихий стук клавиш…";
            Box(new Rect(60, 198, 268, 32), new Color(.09f, .12f, .2f, .9f));
            Label(72, 205, 244, 24, status, small);
            if (Button(58, 566, 234, 34, rest ? "Вернуться за стол" : "Отдохнуть", S.project != null)) S.resting = !S.resting;
            if (Button(610, 566, 184, 34, "Улучшить комнату")) modal = "shop";
        }
        void DrawProject()
        {
            Box(new Rect(846, 174, 402, 448), panel);
            Label(870, 192, 350, 26, "НА РАБОЧЕМ СТОЛЕ", small);
            ProjectData p = S.project;
            if (p == null)
            {
                Label(870, 239, 346, 60, "Всё начинается\nс одной идеи.", heading);
                Label(870, 322, 342, 88, "Выбери тему и жанр. Сделай маленькую игру, которую захочется показать друзьям.", body);
                if (Button(870, 440, 354, 48, "Придумать игру")) modal = "new";
                Label(870, 514, 350, 78, "Цель главы: выпусти 3 игры и купи рабочую станцию. Играть можно и после этого.", small);
                return;
            }
            Label(870, 228, 352, 55, p.title, heading);
            Label(870, 287, 352, 24, Catalog.Genres[p.genre] + " · " + Catalog.Themes[p.theme], small);
            int stage = Math.Min(2, (int)(p.progress / 33.334f));
            Label(870, 325, 352, 25, Catalog.Stages[stage] + "   " + Mathf.FloorToInt(p.progress) + "%");
            Bar(870, 357, 354, p.progress / 100, mint);
            Label(870, 379, 352, 25, "Приоритет работы", small);
            for (int i = 0; i < 3; i++) if (Button(870 + i * 120, 411, 114, 38, Catalog.Focus[i], true, (int)S.focus == i)) S.focus = (WorkFocus)i;
            Label(870, 466, 354, 26, "Код " + (int)p.code + "   Арт " + (int)p.art + "   Лоск " + (int)p.polish, small);
            Label(870, 497, 354, 25, "Ошибки: " + Mathf.CeilToInt(p.bugs) + " · Прогноз: " + sim.Rating(p).ToString("F1") + "/10", small);
            int hours = Mathf.CeilToInt((100 - p.progress) / (.29f * sim.Speed / (p.scope == 0 ? 1 : 1.35f)));
            Label(870, 529, 354, 23, p.progress >= 100 ? "Можно полировать дальше: качество ещё растёт." : "До готовности ≈ " + hours + " игровых часов без отдыха", micro);
            if (Button(870, 566, 354, 36, p.progress >= 100 ? "Выпустить игру" : "Ранний релиз · от 60%", p.progress >= 60)) modal = "releaseConfirm";
        }
        void DrawFooter()
        {
            if (Button(32, 643, 132, 36, "История игр")) modal = "history";
            if (Button(174, 643, 154, 36, "Подработка", S.hour >= S.contractAt && S.energy >= 12))
            { if (sim.Contract()) { Save(false); Toast("Подработка: +35 монет, −12 энергии. Снова доступна через сутки."); } }
            if (Button(338, 643, 130, 36, "Сохранить")) Save(true);
            if (Button(478, 643, 104, 36, sound ? "Звук: да" : "Звук: нет")) sound = !sound;
            if (Button(592, 643, 90, 36, "Помощь")) modal = "help";
            Label(710, 643, 538, 55, noticeUntil > Time.unscaledTime || noticeUntil == 0 ? notice : "Совет: меняй приоритеты. Полировка уменьшает ошибки и повышает оценку.", small);
            Label(32, 694, 700, 20, "ПРОБЕЛ — пауза     ESC — закрыть окно     Автосохранение каждые 20 секунд", micro);
        }
        void ModalHeader(string text, string subtitle)
        {
            Label(292, 103, 668, 44, text, heading);
            Label(292, 151, 666, 54, subtitle, small);
        }
        void CloseButton(string text = "Вернуться в комнату")
        { if (Button(292, 574, 696, 42, text)) { modal = ""; GUI.FocusControl(null); } }
        void DrawModal()
        {
            switch (modal)
            {
                case "new": DrawNewProject(); break;
                case "shop": DrawShop(); break;
                case "releaseConfirm": DrawReleaseConfirm(); break;
                case "result": DrawResult(); break;
                case "history": DrawHistory(); break;
                case "goal":
                    ModalHeader("Из маленькой комнаты — к большим играм", "Первая глава пройдена. Три релиза и своя рабочая станция!");
                    Label(292, 247, 680, 110, "Ты уже умеешь превращать идеи в готовые игры. Теперь попробуй другие сочетания тем и жанров, собери все улучшения и побей свой рекорд оценки.", heading);
                    Label(292, 414, 680, 72, "Всего выпущено: " + S.released + "\nПоклонников: " + S.fans + " · Опыт: " + S.experience);
                    CloseButton("Сделать ещё один билд"); break;
                case "help":
                    ModalHeader("Твоя первая студия", "Здесь нет дедлайна. Экспериментируй в своём темпе.");
                    Label(292, 218, 690, 330,
                        "1. Создай игру: мини-проект стоит 90 монет, небольшой — 150.\n\n2. Меняй приоритеты: код, графика и полировка вместе дают лучший результат. Полировка также убирает ошибки.\n\n3. При энергии ниже 20% работа замедляется. Отдых бесплатный.\n\n4. Релиз доступен с 60% готовности. На 100% можно продолжить повышать качество. Продажи идут 120 игровых часов.\n\n5. Купи улучшения. Если не хватает денег, подработка приносит 35 монет раз в сутки за 12 энергии.\n\nОкна и события останавливают игровое время. 1 час = 1 секунда на скорости 1×.", body);
                    CloseButton(); break;
            }
        }
        void DrawNewProject()
        {
            ModalHeader("У каждой игры есть начало", "Небольшой проект — отличный способ проверить большую идею.");
            Label(292, 208, 680, 24, "Название", small);
            GUI.SetNextControlName("ProjectTitle");
            projectTitle = GUI.TextField(new Rect(292, 237, 696, 48), projectTitle, 32, input);
            Label(292, 308, 680, 24, "Жанр", small);
            for (int i = 0; i < 3; i++) if (Button(292 + i * 236, 338, 224, 40, Catalog.Genres[i], true, genre == i)) genre = i;
            Label(292, 397, 680, 24, "Тема", small);
            for (int i = 0; i < 4; i++) if (Button(292 + i * 177, 427, 165, 40, Catalog.Themes[i], true, theme == i)) theme = i;
            if (Button(292, 491, 340, 38, "Мини · 90 монет", true, scope == 0)) scope = 0;
            if (Button(648, 491, 340, 38, "Небольшая · 150 монет", true, scope == 1)) scope = 1;
            int cost = scope == 0 ? 90 : 150;
            if (Button(292, 566, 510, 46, "Начать разработку · " + cost + " монет", S.money >= cost))
            {
                if (sim.StartProject(projectTitle, genre, theme, scope)) { modal = ""; GUI.FocusControl(null); Save(false); Toast("Проект начат! Попробуй сначала заняться кодом, затем графикой и полировкой."); }
            }
            if (Button(816, 566, 172, 46, "Позже")) { modal = ""; GUI.FocusControl(null); }
        }
        void DrawShop()
        {
            ModalHeader("Место, где рождаются игры", "Каждое улучшение остаётся навсегда и появляется в комнате.");
            for (int i = 0; i < 6; i++)
            {
                float y = 210 + i * 57;
                Label(292, y, 463, 24, Catalog.Upgrades[i].Name);
                Label(292, y + 26, 463, 23, Catalog.Upgrades[i].Description, small);
                if (Button(780, y + 3, 208, 40, S.upgrades[i] ? "Установлено" : Catalog.Upgrades[i].Price + " монет", !S.upgrades[i] && S.money >= Catalog.Upgrades[i].Price))
                    if (sim.BuyUpgrade(i)) { Save(false); Toast("Комната стала лучше: " + Catalog.Upgrades[i].Name); }
            }
            CloseButton();
        }
        void DrawReleaseConfirm()
        {
            ProjectData p = S.project;
            if (p == null) { modal = ""; return; }
            ModalHeader("Пора показать игру миру?", p.title);
            Label(292, 227, 680, 185, "Готовность: " + (int)p.progress + "%\nКод: " + (int)p.code + " · Графика: " + (int)p.art + " · Полировка: " + (int)p.polish + "\nОшибок: " + Mathf.CeilToInt(p.bugs) + "\nСочетание темы и жанра: " + (Catalog.Fit(p.genre, p.theme) >= 1.2f ? "отличное" : Catalog.Fit(p.genre, p.theme) > 1 ? "удачное" : "обычное") + "\nОжидаемая оценка: " + sim.Rating(p).ToString("F1") + " / 10", heading);
            Label(292, 450, 680, 65, "Полировка перед релизом повышает качество и уменьшает ошибки. Дальнейшие продажи будут поступать в течение пяти игровых дней.", body);
            if (Button(292, 568, 340, 44, "Выпустить"))
            {
                releaseReasons = "Код " + (int)p.code + " · Графика " + (int)p.art + " · Полировка " + (int)p.polish + "\nОшибки: −" + (p.bugs * .11f).ToString("F1") + " к оценке. Незавершённость: −" + ((100-p.progress) * .04f).ToString("F1") + ".";
                lastRelease = sim.Release();
                if (lastRelease != null) { modal = "result"; celebrate = 6; Save(false); }
            }
            if (Button(648, 568, 340, 44, "Ещё немного поработать")) modal = "";
        }
        void DrawResult()
        {
            if (lastRelease == null) { modal = ""; return; }
            ModalHeader("Твой билд уже у игроков", lastRelease.title);
            Label(292, 220, 670, 66, lastRelease.rating.ToString("F1") + " / 10", title);
            Label(292, 302, 670, 60, lastRelease.review, heading);
            Label(292, 389, 670, 74, releaseReasons, body);
            Label(292, 485, 670, 60, "Прогноз продаж: " + lastRelease.totalRevenue + " монет за 5 дней.\nМожно отдыхать или сразу придумать следующий проект.", small);
            CloseButton("Моя следующая идея…");
        }
        void DrawHistory()
        {
            ModalHeader("Полка маленьких побед", "Деньги от выпущенных игр поступают автоматически, пока идёт время.");
            historyScroll = GUI.BeginScrollView(new Rect(292, 210, 696, 338), historyScroll, new Rect(0, 0, 670, Math.Max(330, S.releases.Count * 86)));
            if (S.releases.Count == 0) Label(0, 20, 650, 80, "Здесь будет твой первый релиз.\nИ первый отзыв, который захочется сохранить.", heading);
            for (int row = 0; row < S.releases.Count; row++)
            {
                ReleaseData r = S.releases[S.releases.Count - 1 - row];
                Label(0, row * 86, 560, 30, r.title, heading);
                Label(580, row * 86, 80, 30, r.rating.ToString("F1"), heading);
                Label(0, row * 86 + 39, 650, 35, "Получено " + r.paidRevenue + " / " + r.totalRevenue + " монет · " + (r.age == 120 ? "Продажи завершены" : "Продажи идут"), small);
            }
            GUI.EndScrollView(); CloseButton();
        }
        void DrawEvent()
        {
            EventDefinition e = Catalog.Events[S.pendingEvent];
            ModalHeader("Пока собирается билд…", "СОБЫТИЕ · время остановлено");
            Label(292, 225, 690, 66, e.Title, title);
            Label(292, 318, 690, 100, e.Body, heading);
            bool canPay = S.pendingEvent != 5 || S.money >= 25;
            if (Button(292, 456, 696, 66, e.Left, canPay)) { if (sim.ResolveEvent(true)) Save(false); }
            // The first choice may have cleared pendingEvent during this GUI event.
            if (Button(292, 540, 696, 66, e.Right)) { if (sim.ResolveEvent(false)) Save(false); }
        }
    }
}
