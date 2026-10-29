using System;
using System.IO;
using UnityEngine;

namespace OneMoreBuild
{
    public sealed partial class StudioApp : MonoBehaviour
    {
        const float Width = 1280, Height = 720;
        Simulation sim;
        SaveRepository saves;
        Texture2D room;
        readonly Texture2D[] upgrades = new Texture2D[6], developer = new Texture2D[4];
        Font font;
        GUIStyle body, small, title, heading, button, input, micro;
        Color ink = Hex("849B8D"), panel = Hex("F6ECCE"), muted = Hex("687064"), cream = Hex("333E3D"), mint = Hex("527D9D"), pink = Hex("B35E43");
        string modal = "", projectTitle = "Новая игра", notice = "Нажми на компьютер, чтобы начать проект.";
        int genre, theme = 1, scope, speed = 1;
        readonly GameClock clock = new GameClock();
        float autosave, celebrate, noticeUntil;
        bool sound = true;
        AudioSource audioSource;
        AudioClip click;
        ReleaseData lastRelease;
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
            actorPosition = S.resting ? new Vector2(71, 142) : new Vector2(197, 121);
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
            UpdateOffice();
            celebrate = Mathf.Max(0, celebrate - Time.unscaledDeltaTime);
            autosave += Time.unscaledDeltaTime;
            int salesBefore = S.lifetimeSales;
            int contractBefore = S.lifetimeContracts;
            clock.Advance(Time.unscaledDeltaTime, speed, () => modal.Length > 0 || S.pendingEvent >= 0 || walking, sim.Tick);
            if (S.lifetimeSales > salesBefore) Toast("Недельный отчёт: +" + (S.lifetimeSales - salesBefore) + " мон. после возвратов и комиссии. Подробности — в релизах.");
            if (S.lifetimeContracts > contractBefore) Toast("Заказ сдан: +90 мон. Долг погашается в первую очередь.");
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
            else if (show) Toast("Игра сохранена.");
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
            input = new GUIStyle(GUI.skin.textField) { font = font, fontSize = 20, padding = new RectOffset(12, 12, 12, 10), normal = { textColor = cream }, focused = { textColor = cream } };
        }
        void Box(Rect r, Color color) { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
        void Label(float x, float y, float w, float h, string text, GUIStyle style = null) { GUI.Label(new Rect(x, y, w, h), text, style ?? body); }
        bool Button(float x, float y, float w, float h, string text, bool enabled = true, bool active = false)
        {
            Rect r = new Rect(x, y, w, h);
            bool usable = enabled && GUI.enabled;
            bool hover = usable && r.Contains(Event.current.mousePosition);
            Frame(r, !usable ? Hex("D2CCB5") : active ? Hex("F1CA73") : hover ? Hex("FFEDB4") : Hex("F7E8BD"));
            Color old = GUI.color;
            if (!usable) GUI.color = new Color(1, 1, 1, .4f);
            GUI.Label(new Rect(x + 8, y + 3, w - 16, h - 6), text, button); GUI.color = old;
            bool prior = GUI.enabled; GUI.enabled = usable;
            bool pressed = GUI.Button(r, GUIContent.none, GUIStyle.none);
            GUI.enabled = prior;
            if (pressed && sound) audioSource.PlayOneShot(click);
            return pressed;
        }
        void Bar(float x, float y, float w, float fraction, Color color)
        {
            Box(new Rect(x, y, w, 7), Hex("C2BDA3"));
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
                Frame(new Rect(260, 72, 760, 568), panel);
                Box(new Rect(264, 76, 752, 12), mint);
                if (S.pendingEvent >= 0) DrawEvent();
                else DrawModal();
            }
            GUI.matrix = previous;
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
                case "debug": DrawDebug(); break;
                case "desk": DrawDesk(); break;
                case "break": DrawBreak(); break;
                case "phone": DrawPhone(); break;
                case "menu": DrawMenu(); break;
                case "business": DrawBusiness(); break;
                case "report": DrawReleaseReport(); break;
                case "finance": DrawFinance(); break;
                case "market": DrawMarket(); break;
                case "research": DrawResearch(); break;
                case "new": DrawNewProject(); break;
                case "shop": DrawShop(); break;
                case "releaseConfirm": DrawReleaseConfirm(); break;
                case "result": DrawResult(); break;
                case "history": DrawHistory(); break;
                case "goal":
                    ModalHeader("Первая глава завершена", "Первая глава пройдена. Три релиза и своя рабочая станция!");
                    Label(292, 247, 680, 110, "Ты уже умеешь превращать идеи в готовые игры. Теперь попробуй другие сочетания тем и жанров, собери все улучшения и побей свой рекорд оценки.", heading);
                    Label(292, 414, 680, 72, "Всего выпущено: " + S.released + "\nПоклонников: " + S.fans + " · Опыт: " + S.experience);
                    CloseButton("Сделать ещё один билд"); break;
                case "help":
                    ModalHeader("Твоя первая студия", "Здесь нет дедлайна. Экспериментируй в своём темпе.");
                    Label(292, 218, 690, 330,
                        "1. Выбери тему и жанр, затем платформу, аудиторию, цену и рекламу. В меню «Рынок» видны тренды и требования жанров.\n\n2. Код, графика и полировка важны в разных пропорциях. Ошибки и ранний релиз снижают оценку.\n\n3. Студия оплачивает содержание ежедневно. Остаток долга виден в «Финансах»; доход сначала гасит его.\n\n4. Продажи считаются раз в неделю, всего 6 недель. Магазин берёт 30%, покупатели могут вернуть сырую игру. Итог — в отчёте релиза.\n\n5. Телефон: заказ на 48 часов, 90 монет, 20 энергии. Пока выполняется заказ, свой проект стоит. Это способ погасить долг.\n\nПовторение идей снижает спрос; успешной игре можно сделать продолжение. Исследования тратят опыт и деньги. Окна останавливают время.", body);
                    CloseButton(); break;
            }
        }
        void DrawNewProject()
        {
            ModalHeader("Новый проект", "Выбери жанр, тему и бюджет.");
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
            if (Button(292, 566, 510, 46, "Далее · платформа и бюджет", S.money >= cost))
            {
                modal = "business";
            }
            if (Button(816, 566, 172, 46, "Позже")) { modal = ""; GUI.FocusControl(null); }
        }
        void DrawShop()
        {
            ModalHeader("Оборудование", "Покупки меняют офис и характеристики работы.");
            for (int i = 0; i < 6; i++)
            {
                float y = 210 + i * 57;
                Label(292, y, 463, 24, Catalog.Upgrades[i].Name);
                Label(292, y + 26, 463, 23, Catalog.Upgrades[i].Description, small);
                if (Button(780, y + 3, 208, 40, S.upgrades[i] ? "Установлено" : Catalog.Upgrades[i].Price + " монет", !S.upgrades[i] && S.money >= Catalog.Upgrades[i].Price && S.arrears == 0))
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
            Label(292, 450, 680, 65, "Бюджет: " + p.cost + " мон. Цена: " + Market.Prices[p.priceIndex] + ". Комиссия магазина 30%. Продажи — 6 недель. Первый отчёт через 7 дней.", body);
            if (Button(292, 568, 340, 44, "Выпустить", S.arrears == 0 && S.contractRemaining == 0 && S.patchRemaining == 0))
            {

                lastRelease = sim.Release();
                if (lastRelease != null) { selectedRelease = S.releases.Count - 1; reportTab = 0; modal = "result"; celebrate = 6; Save(false); }
            }
            if (Button(648, 568, 340, 44, "Ещё немного поработать")) modal = "";
        }
        void DrawEvent()
        {
            EventDefinition e = Catalog.Events[S.pendingEvent];
            ModalHeader("Событие", "СОБЫТИЕ · время остановлено");
            Label(292, 225, 690, 66, e.Title, title);
            Label(292, 318, 690, 100, e.Body, heading);
            bool canPay = S.pendingEvent != 5 || (S.money >= 25 && S.arrears == 0);
            if (Button(292, 456, 696, 66, e.Left, canPay)) { if (sim.ResolveEvent(true)) Save(false); }
            // The first choice may have cleared pendingEvent during this GUI event.
            if (Button(292, 540, 696, 66, e.Right)) { if (sim.ResolveEvent(false)) Save(false); }
        }
    }
}
