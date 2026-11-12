using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// القوائم: القائمة الرئيسية، خريطة المراحل، صندوق الأقلام، الاستراحة، شاشة الفوز، المتجر، الإعدادات، والانتقال بين المراحل (صفحة تنقلب)
// الشكل كرتوني: دفتر بسلك ووجه مبتسم (نفس الدفتر اللي تمشي عليه رويدا)، أزرار ستيكر دايرية، أزرار على شكل قلم، حواف غامقة وظل حاد
// الصور كلها في Resources/UI (مرسومة بضعف المقاس عشان تطلع حادة بالجوال)، والخط كرتوني (لاله زار)
public class MenuUI : MonoBehaviour
{
    // ألوان التصميم الكرتوني
    static readonly Color Ink = new Color(0.176f, 0.106f, 0.271f);       // الحواف والكلام
    static readonly Color InkSoft = new Color(0.42f, 0.38f, 0.5f);       // كلام ثانوي
    static readonly Color InkFaint = new Color(0.62f, 0.59f, 0.7f);      // شي مقفل أو ما تحقق
    static readonly Color CPink = new Color(1f, 0.424f, 0.596f);
    static readonly Color CBlue = new Color(0.247f, 0.753f, 0.961f);
    static readonly Color CGreen = new Color(0.431f, 0.859f, 0.369f);
    static readonly Color CPurple = new Color(0.725f, 0.549f, 1f);
    static readonly Color COrange = new Color(1f, 0.608f, 0.239f);
    static readonly Color CYellow = new Color(1f, 0.851f, 0.231f);
    static readonly Color CGrey = new Color(0.85f, 0.83f, 0.89f);
    static readonly Color CGold = new Color(1f, 0.72f, 0.12f);
    static readonly Color Cream = new Color(1f, 0.97f, 0.86f);
    // ألوان الرسومات الصغيرة (طريقة اللعب وصندوق الأقلام)
    static readonly Color Navy = new Color(0.17f, 0.24f, 0.31f);
    static readonly Color Grey = new Color(0.45f, 0.47f, 0.5f);
    static readonly Color LineBlue = new Color(0.6f, 0.75f, 0.9f, 0.35f);
    static readonly Color Pink = new Color(1f, 0.47f, 0.56f);
    static readonly Color PinkDark = new Color(0.86f, 0.33f, 0.45f);
    static readonly Color PinkSoft = new Color(1f, 0.6f, 0.72f);
    static readonly Color Yellow = new Color(1f, 0.82f, 0.33f);
    static readonly Color YellowDark = new Color(0.93f, 0.66f, 0.2f);
    static readonly Color Gold = new Color(1f, 0.78f, 0.22f);
    static readonly Color PaperWhite = new Color(1f, 0.995f, 0.97f);
    static readonly Color SkyBlue = new Color(0.35f, 0.68f, 1f);

    GameManager gm;
    Font font;
    RectTransform canvasRect, dim, desk, mainPanel, levelsPanel, pausePanel, completePanel;
    RectTransform pageTurn;
    Image flash;
    Sprite rounded, circle, star, triangle, coin;
    bool levelsFromPause;
    AudioClip clickClip, turnClip;
    readonly List<RectTransform> panels = new List<RectTransform>();
    readonly Dictionary<RectTransform, RectTransform> corners = new Dictionary<RectTransform, RectTransform>();

    public bool Transitioning { get; private set; }
    public bool PauseOpen => pausePanel.gameObject.activeSelf || (levelsPanel.gameObject.activeSelf && levelsFromPause) || (settingsPanel.gameObject.activeSelf && settingsInGame);
    public bool AnyOpen
    {
        get
        {
            foreach (RectTransform p in panels) if (p.gameObject.activeSelf) return true;
            return false;
        }
    }

    // صورة من قطع الواجهة الكرتونية (Resources/UI)
    static readonly Dictionary<string, Sprite> art = new Dictionary<string, Sprite>();
    public static Sprite Art(string name)
    {
        if (!art.TryGetValue(name, out Sprite s) || s == null)
        {
            s = Resources.Load<Sprite>("UI/" + name);
            art[name] = s;
        }
        return s;
    }

    public void Build(GameManager manager)
    {
        gm = manager;
        font = ArabicText.Font;
        rounded = RoundedSprite();
        circle = CircleSprite();
        star = StarSprite();
        triangle = TriangleSprite();
        coin = Art("coin") ?? CoinSprite();
        clickClip = SynthAudio.Click();
        turnClip = SynthAudio.PageTurn();

        // الأزرار تحتاج EventSystem يشتغل مع نظام الإدخال الجديد
        if (FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var canvasGo = new GameObject("Menus", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;   // فوق كل شي وبألوانها الحقيقية (بدون مؤثرات الكاميرا)
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 0.4f;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.layer = 5;   // طبقة الواجهة
        canvasRect = canvasGo.GetComponent<RectTransform>();

        // وقت اللعب: بنفسجي غامق شفاف ورا النوافذ (مثل التصميم)
        dim = Panel(canvasRect, "Dim", new Color(0.11f, 0.06f, 0.21f, 0.55f)).rectTransform;
        Stretch(dim);
        dim.GetComponent<Image>().raycastTarget = true;   // يمنع الرسم ورا القوائم
        desk = BuildDesk();

        BuildMain();
        BuildLevels();
        BuildPause();
        BuildComplete();
        BuildPencil();
        BuildConfirm();
        BuildSettings();
        BuildHowTo();
        BuildUnlock();
        BuildShop();

        flash = Panel(canvasRect, "Flash", new Color(1f, 1f, 1f, 0f));
        Stretch(flash.rectTransform);
        pageTurn = BuildPageTurn();
        BuildToast();
        HideAll();
        Coins.Changed += RefreshCoins;
        RefreshCoins();
    }

    void OnDestroy()
    {
        Coins.Changed -= RefreshCoins;
    }

    // ---------- فتح وسكر ----------

    public void HideAll()
    {
        dim.gameObject.SetActive(false);
        desk.gameObject.SetActive(false);
        foreach (RectTransform p in panels) if (p != null) p.gameObject.SetActive(false);
        foreach (RectTransform c in corners.Values) if (c != null) c.gameObject.SetActive(false);
    }

    // onDesk: خلفية العالم الكرتوني (للقوائم برا اللعب)، وإلا تظلل اللعبة ورا
    void Open(RectTransform panel, bool onDesk = false)
    {
        HideAll();
        dim.gameObject.SetActive(!onDesk);
        desk.gameObject.SetActive(onDesk);
        panel.gameObject.SetActive(true);
        if (corners.TryGetValue(panel, out RectTransform c)) c.gameObject.SetActive(true);
        StartCoroutine(PopIn(panel));
    }

    IEnumerator PopIn(RectTransform panel)
    {
        var group = panel.GetComponent<CanvasGroup>();
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.28f)
        {
            float e = 1f - Mathf.Pow(1f - t, 3f);
            panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.86f, 1f, e + Mathf.Sin(t * Mathf.PI) * 0.06f);
            group.alpha = Mathf.Clamp01(t * 2.5f);
            yield return null;
        }
        panel.localScale = Vector3.one;
        group.alpha = 1f;
    }

    public void ShowMain()
    {
        RefreshMain();
        Open(mainPanel, true);
    }

    // شاشة البداية صارت هي القائمة الرئيسية (فيها الشعار وزر اللعب)
    public void ShowTitle() => ShowMain();

    public void ShowLevels(bool fromPause)
    {
        levelsFromPause = fromPause;
        selected = Mathf.Min(SaveData.Unlocked, Levels.All.Count - 1);
        RefreshLevels();
        ShowPart(selected / PerPart);
        levelsClose.SetActive(fromPause);
        Open(levelsPanel, !fromPause);
    }

    public void ShowPause()
    {
        Open(pausePanel);
    }

    // ---------- الانتقال: صفحة دفتر تغطي الشاشة وتنقلب ----------

    public void Transition(Action middle)
    {
        if (Transitioning) return;
        StartCoroutine(TransitionRoutine(middle));
    }

    IEnumerator TransitionRoutine(Action middle)
    {
        Transitioning = true;
        float w = canvasRect.rect.width + 200f;
        pageTurn.gameObject.SetActive(true);
        gm.Sound.Play(turnClip, 0.7f);
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.38f)
        {
            float e = t * t * (3f - 2f * t);
            pageTurn.anchoredPosition = new Vector2(Mathf.Lerp(w, 0f, e), 0f);
            pageTurn.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-6f, 0f, e));
            yield return null;
        }
        pageTurn.anchoredPosition = Vector2.zero;
        pageTurn.localRotation = Quaternion.identity;
        middle?.Invoke();
        yield return new WaitForSecondsRealtime(0.15f);
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.38f)
        {
            float e = t * t * (3f - 2f * t);
            pageTurn.anchoredPosition = new Vector2(Mathf.Lerp(0f, -w, e), 0f);
            pageTurn.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 6f, e));
            yield return null;
        }
        pageTurn.gameObject.SetActive(false);
        Transitioning = false;
    }

    // ومضة بيضا سريعة (لما ترجع رويدا بعد ما تخسر قلب)
    public void Flash()
    {
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.4f)
        {
            flash.color = new Color(1f, 0.98f, 0.95f, 0.75f * (1f - t) * (1f - t));
            yield return null;
        }
        flash.color = Color.clear;
    }

    // صفحة دفتر كريمية بخطوط والشعار باهت بالنص
    RectTransform BuildPageTurn()
    {
        var page = Panel(canvasRect, "Page Turn", new Color(1f, 0.98f, 0.93f)).rectTransform;
        Place(page, Vector2.zero, new Vector2(2400f, 1500f));
        Lines(page, new Vector2(2400f, 1500f), 54f);
        var logo = Pic(page, "logo", Vector2.zero, 0.8f);
        logo.color = new Color(1f, 1f, 1f, 0.35f);
        page.gameObject.SetActive(false);
        return page;
    }

    // خلفية القوائم: عالم كرتوني (سما بأشعة، شمس تغمز، غيوم بوجوه، تلال وأشجار حلاوة)، تغطي الشاشة كلها بأي مقاس
    RectTransform BuildDesk()
    {
        var d = Panel(canvasRect, "Desk", Color.white);
        d.sprite = Art("bg_world");
        d.raycastTarget = true;
        Place(d.rectTransform, Vector2.zero, new Vector2(1920f, 1080f));
        var fit = d.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = 16f / 9f;
        return d.rectTransform;
    }

    // ---------- الإعدادات: مفاتيح الموسيقى والأصوات وصوت رويدا ----------

    RectTransform settingsPanel;
    Action settingsBack;
    bool settingsInGame;
    Image musicToggle, soundToggle, voiceToggle;
    Text musicState, soundState, voiceState, settingsDone;
    GameObject settingsMenuRow;

    // from: النافذة اللي جينا منها (نرجع لها)، و null = القائمة الرئيسية
    public void ShowSettings(RectTransform from)
    {
        if (from == pausePanel) OpenSettings(ShowPause, true);
        else if (from == levelsPanel) { bool p = levelsFromPause; OpenSettings(() => ShowLevels(p), p); }
        else OpenSettings(ShowMain, false);
    }

    // زر الإعدادات فوق وقت اللعب: لما تتسكر نكمل اللعب
    public void ShowSettingsInGame() => OpenSettings(() => gm.Resume(), true);

    void OpenSettings(Action back, bool inGame)
    {
        settingsBack = back;
        settingsInGame = inGame;
        settingsMenuRow.SetActive(!inGame);
        SetText(settingsDone, ArabicText.Fix(inGame ? "نكمل اللعب" : "رجوع"));
        RefreshIcons();
        Open(settingsPanel, !inGame);
    }

    void BuildSettings()
    {
        settingsPanel = NotebookCard(canvasRect, "Settings", "nb_settings", new Vector2(980f, 820f));
        TopRibbon(settingsPanel, 340, ArabicText.Fix("الإعدادات"), 48, 2f);
        CloseSticker(settingsPanel, () => settingsBack?.Invoke());

        musicToggle = SettingRow(settingsPanel, 230f, "music", "الموسيقى", null, () =>
        {
            GameSettings.MusicOn = !GameSettings.MusicOn;
            if (GameSettings.MusicOn && GameSettings.Music < 0.05f) GameSettings.Music = 1f;
            RefreshIcons();
        }, out musicState);
        soundToggle = SettingRow(settingsPanel, 95f, "speaker", "الأصوات", null, () =>
        {
            GameSettings.EffectsOn = !GameSettings.EffectsOn;
            if (GameSettings.EffectsOn && GameSettings.Effects < 0.05f) GameSettings.Effects = 1f;
            RefreshIcons();
        }, out soundState);
        voiceToggle = SettingRow(settingsPanel, -40f, "mic", "صوت رويدا", "حين تروي القصة", () =>
        {
            GameSettings.NarrationOn = !GameSettings.NarrationOn;
            RefreshIcons();
        }, out voiceState);

        // برا اللعب بس: البدء من جديد (والخروج بالكمبيوتر، لأن الجوال ما يسمح فيه)
        var row = new GameObject("Menu Row", typeof(RectTransform)).GetComponent<RectTransform>();
        row.SetParent(settingsPanel, false);
        Place(row, new Vector2(0f, -170f), new Vector2(900f, 100f));
        settingsMenuRow = row.gameObject;
        bool desktop = !Application.isMobilePlatform;
        MakeButton(row, "من جديد", new Vector2(desktop ? 140f : 0f, 0f), new Vector2(250f, 84f), COrange, ShowConfirm, 36);
        if (desktop) MakeButton(row, "الخروج", new Vector2(-140f, 0f), new Vector2(250f, 84f), CPurple, Application.Quit, 36);

        settingsDone = PencilBtn(settingsPanel, "short", "نكمل اللعب", new Vector2(14f, -310f), () => settingsBack?.Invoke(), 42);
    }

    // سطر إعدادات: أيقونة يمين، الاسم جنبها، ومفتاح (يعمل / مطفأ) يسار
    Image SettingRow(RectTransform parent, float y, string icon, string name, string sub, Action toggle, out Text state)
    {
        Pic(parent, "st_" + icon, new Vector2(330f, y));
        var t = Label(parent, ArabicText.Fix(name), 42, new Vector2(80f, sub != null ? y + 16f : y), new Vector2(360f, 60f));
        t.alignment = TextAnchor.MiddleRight;
        if (sub != null)
        {
            var s = Label(parent, ArabicText.Fix(sub), 26, new Vector2(80f, y - 30f), new Vector2(360f, 40f));
            s.alignment = TextAnchor.MiddleRight;
            s.color = InkSoft;
        }
        var sw = Pic(parent, "toggle_on", new Vector2(-250f, y));
        Click(sw, toggle);
        state = Label(sw.rectTransform, "", 32, Vector2.zero, new Vector2(120f, 60f));
        Toon(state, 3f);
        return sw;
    }

    void RefreshIcons()
    {
        SetToggle(musicToggle, musicState, GameSettings.MusicOn);
        SetToggle(soundToggle, soundState, GameSettings.EffectsOn);
        SetToggle(voiceToggle, voiceState, GameSettings.NarrationOn);
    }

    void SetToggle(Image sw, Text state, bool on)
    {
        if (sw == null) return;
        sw.sprite = Art(on ? "toggle_on" : "toggle_off");
        state.text = ArabicText.Fix(on ? "يعمل" : "مطفأ");
        state.rectTransform.anchoredPosition = new Vector2(on ? -33f : 21f, 4f);
    }

    // ---------- طريقة اللعب: بطاقات تشرح فكرة اللعبة والرسم والحبر والأزرار، كل بطاقة فيها رسمة صغيرة ----------

    RectTransform howToPanel;
    readonly List<RectTransform> howToScenes = new List<RectTransform>();
    readonly List<Image> howToDots = new List<Image>();
    Text howToTitle, howToText, howToNext;
    GameObject howToPrev;
    int howToPage;
    Action howToDone;
    RectTransform drawLine, drawPointer;

    static readonly string[] HowToTitles = { "فكرة اللعبة", "كيف نرسم؟", "الأشكال المغلقة", "الحبر", "أزرار الشاشة" };
    static readonly string[] HowToTexts =
    {
        "نامت رويدا فوجدت نفسها داخل دفتر رسمها! لنساعدها تعبر الصفحات حتى تصل إلى آخر صفحة وتعود إلى غرفتها",
        "نرسم بإصبعنا على الشاشة... يصبح الخط جسرًا أو طريقًا، ورويدا تمشي عليه وحدها",
        "إذا رسمنا شكلًا مغلقًا مثل الدائرة صار كرة نرميها، أو بالونًا، أو شيئًا آخر حسب المرحلة",
        "حبر القلم محدود! نراقب القلم في أعلى الشاشة، ونرسم بقدر الحاجة فقط",
        "في بداية كل مرحلة تقف رويدا وتشرح لنا المطلوب... لنستمع إليها جيدًا!",
    };

    // done: وش يصير بعد ما تخلص (أول مرة: يبدأ المشهد الأول، ومن القائمة: يرجع لها)
    public void ShowHowTo(Action done)
    {
        howToDone = done;
        SetHowToPage(0);
        Open(howToPanel, true);
    }

    void BuildHowTo()
    {
        howToPanel = NotebookCard(canvasRect, "How To Play", "nb_howto", new Vector2(1180f, 900f));
        howToTitle = TopRibbon(howToPanel, 560, "", 52, -1f);
        Label(howToPanel, ArabicText.Fix("طريقة اللعب"), 30, new Vector2(0f, 345f), new Vector2(600f, 50f)).color = InkSoft;

        var stage = CartoonBox(howToPanel, "Stage", new Vector2(0f, 120f), new Vector2(860f, 350f), Cream, 0f);
        for (int i = 0; i < HowToTitles.Length; i++)
        {
            var scene = new GameObject("Scene " + i, typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
            scene.SetParent(stage, false);
            Stretch(scene);
            howToScenes.Add(scene);
        }
        SceneIdea(howToScenes[0]);
        SceneDraw(howToScenes[1]);
        SceneShapes(howToScenes[2]);
        SceneInk(howToScenes[3]);
        SceneKeys(howToScenes[4]);

        howToText = Label(howToPanel, "", 36, new Vector2(0f, -145f), new Vector2(1000f, 150f));
        howToText.horizontalOverflow = HorizontalWrapMode.Wrap;
        howToText.lineSpacing = 1.05f;

        for (int i = 0; i < HowToTitles.Length; i++)
        {
            var ring = Panel(howToPanel, "Dot", Ink);
            ring.sprite = circle;
            Place(ring.rectTransform, new Vector2((2 - i) * 40f, -250f), new Vector2(28f, 28f));
            var dot = Panel(ring.rectTransform, "Fill", CGrey);
            dot.sprite = circle;
            Stretch(dot.rectTransform);
            dot.rectTransform.offsetMin = new Vector2(5f, 5f);
            dot.rectTransform.offsetMax = new Vector2(-5f, -5f);
            howToDots.Add(dot);
        }
        // التالي قلم يسار (القراءة من اليمين)، والسابق سهم يمين
        howToNext = PencilBtn(howToPanel, "mid", "التالي", new Vector2(-90f, -350f), () =>
        {
            if (howToPage < HowToTitles.Length - 1) SetHowToPage(howToPage + 1);
            else { HideAll(); howToDone?.Invoke(); }
        }, 44);
        howToPrev = Sticker(howToPanel, "right", new Vector2(400f, -345f), () => SetHowToPage(howToPage - 1)).gameObject;
    }

    public void SetHowToPage(int page)
    {
        howToPage = Mathf.Clamp(page, 0, HowToTitles.Length - 1);
        SetText(howToTitle, ArabicText.Fix(HowToTitles[howToPage]));
        howToText.text = WrapFix(HowToTexts[howToPage], 46);
        for (int i = 0; i < howToScenes.Count; i++) howToScenes[i].gameObject.SetActive(i == howToPage);
        for (int i = 0; i < howToDots.Count; i++)
        {
            howToDots[i].color = i == howToPage ? CPink : CGrey;
            ((RectTransform)howToDots[i].transform.parent).sizeDelta = Vector2.one * (i == howToPage ? 36f : 28f);
        }
        SetText(howToNext, ArabicText.Fix(howToPage == HowToTitles.Length - 1 ? "فهمت! لنلعب" : "التالي"));
        howToPrev.SetActive(howToPage > 0);
        if (howToPanel.gameObject.activeSelf) StartCoroutine(PopIn(howToScenes[howToPage]));
    }

    // قطعة رسمة: مستطيل (أو شكل) بلون ومكان ودوران
    Image Bit(RectTransform parent, Vector2 pos, Vector2 size, Color color, float rot = 0f, Sprite sprite = null, bool sliced = false)
    {
        var img = Panel(parent, "Bit", color);
        img.sprite = sprite;
        if (sliced) { img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 3f; }
        Place(img.rectTransform, pos, size);
        img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rot);
        return img;
    }

    // صفحة دفتر صغيرة (أرض المرحلة)
    void MiniPage(RectTransform p, Vector2 pos, float w)
    {
        Bit(p, pos + new Vector2(4f, -6f), new Vector2(w, 60f), new Color(0.1f, 0.2f, 0.35f, 0.15f), 0f, rounded, true);
        Bit(p, pos, new Vector2(w, 60f), PaperWhite, 0f, rounded, true);
        Bit(p, pos + new Vector2(0f, 8f), new Vector2(w - 16f, 3f), LineBlue);
        Bit(p, pos + new Vector2(0f, -10f), new Vector2(w - 16f, 3f), LineBlue);
        Bit(p, pos + new Vector2(0f, 30f), new Vector2(w, 5f), new Color(0.55f, 0.6f, 0.68f));
    }

    // رويدا صغيرة: شعر أسود طويل، وجه، بلوزة بيضا، وبنطلون أسود
    void MiniRowaida(RectTransform p, Vector2 feet)
    {
        Color hair = new Color(0.1f, 0.08f, 0.1f), skin = new Color(1f, 0.85f, 0.74f);
        Bit(p, feet + new Vector2(0f, 76f), new Vector2(50f, 62f), hair, 0f, rounded, true);
        Bit(p, feet + new Vector2(0f, 46f), new Vector2(30f, 32f), Color.white, 0f, rounded, true);
        for (int s = -1; s <= 1; s += 2) Bit(p, feet + new Vector2(s * 7f, 16f), new Vector2(11f, 30f), Navy, 0f, rounded, true);
        Bit(p, feet + new Vector2(0f, 80f), new Vector2(40f, 40f), skin, 0f, circle);
        Bit(p, feet + new Vector2(0f, 96f), new Vector2(44f, 16f), hair, 0f, rounded, true);
        for (int s = -1; s <= 1; s += 2) Bit(p, feet + new Vector2(s * 8f, 80f), new Vector2(7f, 9f), hair, 0f, circle);
        for (int s = -1; s <= 1; s += 2) Bit(p, feet + new Vector2(s * 13f, 72f), new Vector2(9f, 5f), PinkSoft, 0f, circle);
    }

    void SceneIdea(RectTransform p)
    {
        // مثل اللعبة: رويدا يسار وتمشي لليمين لين الباب
        MiniPage(p, new Vector2(-270f, -80f), 200f);
        MiniPage(p, new Vector2(0f, -80f), 200f);
        MiniPage(p, new Vector2(270f, -80f), 200f);
        for (int k = 0; k < 11; k++) Bit(p, new Vector2(-230f + k * 44f, 50f + Mathf.Sin(k * 0.6f) * 16f), new Vector2(12f, 12f), PinkSoft, 0f, circle);
        MiniRowaida(p, new Vector2(-300f, -50f));
        // الباب
        Bit(p, new Vector2(300f, 30f), new Vector2(84f, 150f), new Color(0.62f, 0.38f, 0.22f), 0f, rounded, true);
        Bit(p, new Vector2(300f, 30f), new Vector2(66f, 132f), new Color(0.75f, 0.5f, 0.3f), 0f, rounded, true);
        Bit(p, new Vector2(276f, 26f), new Vector2(14f, 14f), Gold, 0f, circle);
        var st = Bit(p, new Vector2(300f, 135f), new Vector2(44f, 44f), Gold, 0f, star);
        st.name = "Spin Star";
    }

    void SceneDraw(RectTransform p)
    {
        MiniPage(p, new Vector2(-230f, -80f), 300f);
        MiniPage(p, new Vector2(230f, -80f), 300f);
        MiniRowaida(p, new Vector2(-280f, -50f));
        // الخط اللي يترسم فوق الحفرة من اليسار لليمين (يطول ويرجع)، والإصبع عند آخره
        drawLine = Bit(p, new Vector2(-85f, -48f), new Vector2(170f, 10f), Navy, 0f, rounded, true).rectTransform;
        drawLine.pivot = new Vector2(0f, 0.5f);
        drawLine.anchoredPosition = new Vector2(-85f, -48f);
        var ptr = new GameObject("Finger", typeof(RectTransform)).GetComponent<RectTransform>();
        ptr.SetParent(p, false);
        Place(ptr, Vector2.zero, new Vector2(40f, 40f));
        // إصبع: طرفه عند آخر الخط، ومايل لتحت يمين
        var finger = Bit(ptr, new Vector2(14f, -36f), new Vector2(34f, 84f), new Color(1f, 0.82f, 0.7f), 20f, rounded, true);
        finger.gameObject.AddComponent<Outline>().effectColor = new Color(0.75f, 0.5f, 0.42f);
        Bit(ptr, new Vector2(6f, -14f), new Vector2(20f, 22f), new Color(1f, 0.93f, 0.9f), 20f, rounded, true);
        drawPointer = ptr;
    }

    void SceneShapes(RectTransform p)
    {
        // دائرة مرسومة (حلقة حبر)
        Bit(p, new Vector2(230f, 10f), new Vector2(150f, 150f), Navy, 0f, circle);
        Bit(p, new Vector2(230f, 10f), new Vector2(132f, 132f), new Color(1f, 0.98f, 0.93f), 0f, circle);
        // سهم
        Bit(p, new Vector2(40f, 10f), new Vector2(110f, 16f), PinkSoft, 0f, rounded, true);
        Bit(p, new Vector2(-25f, 10f), new Vector2(40f, 40f), PinkSoft, 90f, triangle);
        // كرة وبالونة
        Bit(p, new Vector2(-170f, -40f), new Vector2(110f, 110f), new Color(0.95f, 0.35f, 0.4f), 0f, circle).name = "Bounce Ball";
        Bit(p, new Vector2(-185f, -20f), new Vector2(30f, 22f), new Color(1f, 1f, 1f, 0.6f), -30f, circle);
        Bit(p, new Vector2(-300f, 60f), new Vector2(90f, 110f), SkyBlue, 0f, circle).name = "Float Balloon";
        Bit(p, new Vector2(-300f, -40f), new Vector2(4f, 90f), Grey);
    }

    void SceneInk(RectTransform p)
    {
        // قلم الحبر مثل اللي فوق الشاشة: جسم أصفر، حبر غامق ينقص، ممحاة، وسن
        Bit(p, new Vector2(0f, 20f), new Vector2(520f, 76f), YellowDark, 0f, rounded, true);
        Bit(p, new Vector2(0f, 24f), new Vector2(500f, 60f), Yellow, 0f, rounded, true);
        var fill = Bit(p, new Vector2(-240f, 24f), new Vector2(300f, 40f), Navy, 0f, rounded, true).rectTransform;
        fill.pivot = new Vector2(0f, 0.5f);
        fill.name = "Ink Fill";
        Bit(p, new Vector2(290f, 20f), new Vector2(70f, 76f), PinkSoft, 0f, rounded, true);
        Bit(p, new Vector2(-295f, 20f), new Vector2(60f, 60f), new Color(0.96f, 0.76f, 0.55f), 90f, triangle);
        Bit(p, new Vector2(-322f, 20f), new Vector2(18f, 18f), Navy, 90f, triangle);
        Label(p, ArabicText.Fix("الحبر"), 40, new Vector2(0f, 100f), new Vector2(300f, 60f)).color = Grey;
        // قطرات حبر صغيرة
        for (int k = 0; k < 3; k++) Bit(p, new Vector2(-120f + k * 120f, -95f), new Vector2(34f, 44f), Navy, 0f, circle);
    }


    void SceneKeys(RectTransform p)
    {
        // نفس الأزرار اللي تطلع على الشاشة وقت اللعب: القفز، ألوان الحبر، الإعدادات، والإعادة
        string[] what = { "القفز (في بعض المراحل)", "لون الحبر (في مراحل الألوان)", "الإعدادات والاستراحة", "إعادة المرحلة" };
        string[] icons = { "st_jump", null, "st_gear", "st_restart" };
        Color[] inks = { new Color(0.16f, 0.15f, 0.2f), new Color(0.92f, 0.25f, 0.32f), new Color(0.25f, 0.55f, 1f), new Color(0.3f, 0.8f, 0.38f) };
        for (int i = 0; i < what.Length; i++)
        {
            float y = 112f - i * 74f;
            if (icons[i] != null) Bit(p, new Vector2(250f, y), new Vector2(78f, 78f), Color.white, 0f, Art(icons[i]));
            else
                for (int k = 0; k < 4; k++)
                {
                    Bit(p, new Vector2(196f + k * 36f, y), new Vector2(36f, 36f), Ink, 0f, circle);
                    Bit(p, new Vector2(196f + k * 36f, y), new Vector2(26f, 26f), inks[k], 0f, circle);
                }
            var d = Label(p, ArabicText.Fix(what[i]), 32, new Vector2(-120f, y), new Vector2(480f, 56f));
            d.alignment = TextAnchor.MiddleRight;
            d.color = InkSoft;
        }
    }

    void AnimateHowTo(float t)
    {
        // الخط يترسم فوق الحفرة والمؤشر يمشي معه
        if (drawLine != null && howToPage == 1)
        {
            float k = Mathf.Clamp01(Mathf.Repeat(t, 2.6f) / 1.6f);
            drawLine.sizeDelta = new Vector2(Mathf.Max(1f, 170f * k), 10f);
            drawPointer.anchoredPosition = drawLine.anchoredPosition + new Vector2(170f * k, 4f + Mathf.Sin(k * 9f) * 3f);
        }
        if (howToPage == 0)
            foreach (Transform c in howToScenes[0]) if (c.name == "Spin Star") c.localRotation = Quaternion.Euler(0f, 0f, t * 60f);
        if (howToPage == 2)
            foreach (Transform c in howToScenes[2])
            {
                if (c.name == "Bounce Ball") ((RectTransform)c).anchoredPosition = new Vector2(-170f, -40f + Mathf.Abs(Mathf.Sin(t * 3f)) * 40f);
                if (c.name == "Float Balloon") ((RectTransform)c).anchoredPosition = new Vector2(-300f, 60f + Mathf.Sin(t * 1.5f) * 8f);
            }
        if (howToPage == 3)
            foreach (Transform c in howToScenes[3]) if (c.name == "Ink Fill") ((RectTransform)c).sizeDelta = new Vector2(Mathf.Lerp(470f, 120f, Mathf.Repeat(t, 4f) / 4f), 40f);
    }

    // ---------- القائمة الرئيسية: الشعار، قلم "هيا نلعب!"، أربع أزرار ستيكر، والكوينز والنجوم والإعدادات بالزوايا ----------

    Text playLabel, starsTotal;
    RectTransform logoRect;

    void BuildMain()
    {
        mainPanel = NotebookCard(canvasRect, "Main Menu", "nb_main", new Vector2(1320f, 664f));
        mainPanel.anchoredPosition = new Vector2(0f, -32f);
        logoRect = Pic(mainPanel, "logo", new Vector2(0f, 178f)).rectTransform;
        playLabel = PencilBtn(mainPanel, "long", "هيا نلعب!", new Vector2(34f, -60f), () => gm.PlayFromMenu(), 64);
        Sticker(mainPanel, "map", new Vector2(273f, -205f), () => ShowLevels(false), 1f, -4f, "الخريطة");
        Sticker(mainPanel, "bag", new Vector2(93f, -211f), () => ShowShop(ShowMain), 1f, 3f, "المتجر");
        Sticker(mainPanel, "pencil", new Vector2(-87f, -205f), ShowPencil, 1f, -3f, "الأقلام");
        Sticker(mainPanel, "question", new Vector2(-267f, -211f), () => ShowHowTo(ShowMain), 1f, 4f, "طريقة اللعب");

        var c = CornersOf(mainPanel);
        CoinPill(c, new Vector2(210f, -378f), () => ShowShop(ShowMain), 4f);
        starsTotal = StarPill(c, new Vector2(210f, -500f), -3f);
        Anchor(Sticker(c, "gear", Vector2.zero, () => ShowSettings(mainPanel), 1.06f, -8f).rectTransform, new Vector2(1f, 1f), new Vector2(-110f, -110f));
    }

    void RefreshMain()
    {
        SetText(playLabel, ArabicText.Fix(SaveData.HasProgress ? "نكمل المرحلة " + (SaveData.Unlocked + 1) : "هيا نلعب!"));
        starsTotal.text = SaveData.TotalStars + "/" + Levels.All.Count * SaveData.MaxStars;
    }

    // ---------- خريطة المراحل: دواير ستيكر بطريق منقط، بطاقة بولارويد للمرحلة المختارة، وقلم "لنبدأ!" ----------

    const int PerPart = 20;
    static readonly string[] PartNames = { "الجزء الأول", "الجزء الثاني", "الجزء الثالث", "الجزء الرابع" };
    readonly List<RectTransform> partPages = new List<RectTransform>();
    readonly List<Image> nodeImages = new List<Image>();
    readonly List<float> nodeScales = new List<float>();
    readonly List<Text> nodeNumbers = new List<Text>();
    readonly List<Image[]> nodeStars = new List<Image[]>();
    readonly List<Image> nodeCoins = new List<Image>();
    Text levelsTitle, infoTitle, infoHint, startLabel;
    RawImage infoThumb;
    Image[] infoStars;
    Text infoLocked;
    Image nextPart, prevPart, startButton, marker;
    GameObject levelsClose;
    Vector2 markerBase;
    int part, selected;

    void ShowPart(int p)
    {
        part = Mathf.Clamp(p, 0, partPages.Count - 1);
        for (int i = 0; i < partPages.Count; i++) partPages[i].gameObject.SetActive(i == part);
        SetText(levelsTitle, ArabicText.Fix(PartNames[Mathf.Min(part, PartNames.Length - 1)]));
        SetArrow(nextPart, "left", part < partPages.Count - 1);
        SetArrow(prevPart, "right", part > 0);
    }

    // سهم الأجزاء: وردي يشتغل، أو رمادي ما ينضغط
    void SetArrow(Image arrow, string dir, bool on)
    {
        arrow.sprite = Art("st_" + dir + (on ? "" : "_off"));
        arrow.raycastTarget = on;
    }

    // صورة المرحلة (لقطة من المرحلة نفسها)، ولو ما فيه صورة نسوي تدرج بألوان سماها
    static Texture Thumbnail(int index)
    {
        // الصفحة الأخيرة لها صورة ثابتة (رقمها يتغير كل ما زادت مراحل)
        string file = Levels.All[index].drawDoor ? "level_last" : "level_" + index;
        var tex = Resources.Load<Texture2D>("LevelThumbs/" + file);
        if (tex != null) return tex;
        LevelTheme theme = Levels.All[index].theme;
        var g = new Texture2D(1, 3, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        g.SetPixels(new[] { theme.paper, theme.skyMid, theme.skyTop });
        g.Apply();
        return g;
    }

    // مكان كل دايرة على الخريطة: أربع صفوف، تمشي مثل الأفعى (من اليمين أول)
    static Vector2 NodePos(int k)
    {
        int row = k / 5, col = k % 5;
        if (row % 2 == 1) col = 4 - col;
        return new Vector2(670f - col * 192f, 196f - row * 160f + (k % 2 == 1 ? 6f : -6f));
    }

    void BuildLevels()
    {
        levelsPanel = NotebookCard(canvasRect, "Levels", "nb_levels", new Vector2(1700f, 760f));
        levelsPanel.anchoredPosition = new Vector2(0f, -18f);
        levelsTitle = Ribbon(levelsPanel, 420, "", new Vector2(244f, 307f), 46);

        // كل جزء (20 مرحلة) بخريطة لحاله، والطريق نقط وردية بين الدواير
        Color dots = new Color(1f, 0.62f, 0.71f);
        int partCount = (Levels.All.Count + PerPart - 1) / PerPart;
        for (int p = 0; p < partCount; p++)
        {
            var page = new GameObject("Part " + (p + 1), typeof(RectTransform)).GetComponent<RectTransform>();
            page.SetParent(levelsPanel, false);
            Stretch(page);
            partPages.Add(page);
            int count = Mathf.Min(PerPart, Levels.All.Count - p * PerPart);
            for (int k = 0; k < count - 1; k++)
            {
                Vector2 a = NodePos(k), b = NodePos(k + 1);
                float len = Vector2.Distance(a, b);
                for (float d = 56f; d < len - 50f; d += 26f)
                {
                    var dot = Panel(page, "Dot", dots);
                    dot.sprite = circle;
                    Place(dot.rectTransform, Vector2.Lerp(a, b, d / len), new Vector2(12f, 12f));
                }
            }
        }

        for (int i = 0; i < Levels.All.Count; i++)
        {
            int index = i;
            RectTransform page = partPages[i / PerPart];
            Vector2 pos = NodePos(i % PerPart);
            var hit = Panel(page, "Level " + (i + 1), Color.clear);
            Place(hit.rectTransform, pos, new Vector2(120f, 120f));
            Click(hit, () => Select(index));
            // الستيكر مرسوم مزحوح شوي عن نص الصورة (الظل الحاد)، فنرجعه عشان الدايرة تجي بالنص
            var node = Pic(hit.rectTransform, "node_locked", new Vector2(3f, -5f), 1f, -((i % 3) - 1));
            nodeImages.Add(node);
            nodeScales.Add(1f);
            nodeNumbers.Add(Label(node.rectTransform, ArabicDigits(i + 1), 46, new Vector2(-3f, 6f), new Vector2(100f, 100f)));
            Toon(nodeNumbers[i], 3f);
            var badge = Pic(node.rectTransform, "coin", new Vector2(28f, -24f), 0.38f);
            nodeCoins.Add(badge);
            var stars = new Image[SaveData.MaxStars];
            for (int s = 0; s < stars.Length; s++)
                stars[s] = Pic(hit.rectTransform, "star_on", new Vector2((1 - s) * 26f, s == 1 ? -68f : -63f), s == 1 ? 0.42f : 0.35f);
            nodeStars.Add(stars);
        }
        // علامة "أنتِ هنا": قلم صغير بوجه يأشر على المرحلة الحالية
        marker = Pic(partPages[0], "marker", Vector2.zero, 0.8f, -40f);

        // بطاقة بولارويد للمرحلة المختارة (يسار): الصورة ورا الإطار
        var pol = new GameObject("Info", typeof(RectTransform)).GetComponent<RectTransform>();
        pol.SetParent(levelsPanel, false);
        Place(pol, new Vector2(-490f, -8f), new Vector2(470f, 600f));
        pol.localRotation = Quaternion.Euler(0f, 0f, 3f);
        infoThumb = new GameObject("Thumbnail", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        infoThumb.transform.SetParent(pol, false);
        infoThumb.raycastTarget = false;
        Place(infoThumb.rectTransform, new Vector2(0f, 123f), new Vector2(358f, 222f));
        Pic(pol, "polaroid", Vector2.zero);
        infoLocked = Label(pol, ArabicText.Fix("مقفلة"), 48, new Vector2(0f, 123f), new Vector2(340f, 200f));
        Toon(infoLocked, 4f);
        infoTitle = Label(pol, "", 36, new Vector2(0f, -36f), new Vector2(420f, 60f));
        Fit(infoTitle, 400f);
        infoStars = new Image[SaveData.MaxStars];
        for (int s = 0; s < infoStars.Length; s++)
            infoStars[s] = Pic(pol, "star_off", new Vector2((1 - s) * 66f, s == 1 ? -98f : -104f), s == 1 ? 0.8f : 0.64f);
        infoHint = Label(pol, "", 23, new Vector2(0f, -162f), new Vector2(400f, 90f));
        infoHint.color = InkSoft;

        startLabel = PencilBtn(levelsPanel, "short", "لنبدأ!", new Vector2(-476f, -320f), () =>
        {
            if (selected <= SaveData.Unlocked) gm.StartLevelFromMenu(selected);
        }, 44, 1f, 3f);
        startButton = startLabel.transform.parent.GetComponent<Image>();

        // أسهم بين الأجزاء: اليسار للجزء اللي بعده (مثل قلب صفحات كتاب عربي)
        nextPart = Sticker(levelsPanel, "left", new Vector2(-205f, -57f), () => ShowPart(part + 1));
        prevPart = Sticker(levelsPanel, "right", new Vector2(833f, -57f), () => ShowPart(part - 1));
        // من الاستراحة: زر سكر يرجعنا لها
        levelsClose = CloseSticker(levelsPanel, ShowPause).gameObject;

        var c = CornersOf(levelsPanel);
        CoinPill(c, new Vector2(206f, -82f), () => ShowShop(() => ShowLevels(levelsFromPause)), 2f);
        Anchor(Sticker(c, "home", Vector2.zero, () => { if (levelsFromPause) gm.GoToMainMenu(); else gm.BackToMain(); }, 0.94f, 4f).rectTransform, new Vector2(1f, 1f), new Vector2(-220f, -88f));
        Anchor(Sticker(c, "gear", Vector2.zero, () => ShowSettings(levelsPanel), 0.88f, -6f).rectTransform, new Vector2(1f, 1f), new Vector2(-86f, -88f));
    }

    void Select(int index)
    {
        selected = index;
        RefreshLevels();
        if (index <= SaveData.Unlocked) gm.Sound.Play(SynthAudio.Ding(1.2f), 0.3f);
    }

    void RefreshLevels()
    {
        for (int i = 0; i < nodeImages.Count; i++)
        {
            bool open = i <= SaveData.Unlocked;
            int got = SaveData.Stars(i);
            bool current = i == SaveData.Unlocked;
            bool paid = Coins.NeedsBuying(i);
            // الحالية وردية (أو كريمية بكوينز لو تبي فتح)، اللي خلصت صفرا، والمقفلة رمادية
            string look = current ? (paid ? "node_coin" : "node_current") : open ? "node_done" : "node_locked";
            nodeImages[i].sprite = Art(look);
            float size = (current ? 1.16f : 0.94f) * (i == selected ? 1.12f : 1f);
            nodeScales[i] = size;
            nodeImages[i].rectTransform.localScale = Vector3.one * size;
            bool white = look == "node_current" || look == "node_done";
            nodeNumbers[i].color = white ? Color.white : look == "node_coin" ? Ink : new Color(0.56f, 0.51f, 0.66f);
            nodeNumbers[i].GetComponent<Outline>().enabled = white;
            for (int s = 0; s < SaveData.MaxStars; s++)
            {
                nodeStars[i][s].enabled = open && !current && got > 0;
                nodeStars[i][s].sprite = Art(s < got ? "star_on" : "star_off");
            }
            nodeCoins[i].enabled = paid;
        }
        int cur = Mathf.Min(SaveData.Unlocked, nodeImages.Count - 1);
        marker.rectTransform.SetParent(partPages[cur / PerPart], false);
        markerBase = NodePos(cur % PerPart) + new Vector2(70f, 78f);
        marker.rectTransform.anchoredPosition = markerBase;

        // بطاقة المعلومات
        int sel = Mathf.Clamp(selected, 0, Levels.All.Count - 1);
        bool selOpen = sel <= SaveData.Unlocked;
        LevelData d = Levels.All[sel];
        Texture tex = Thumbnail(sel);
        infoThumb.texture = tex;
        // نقص الصورة من الجنبين عشان تعبي فتحة البولارويد بدون ما تنمط
        float aspect = tex.width / (float)tex.height, want = 358f / 222f;
        infoThumb.uvRect = aspect > want ? new Rect((1f - want / aspect) * 0.5f, 0f, want / aspect, 1f) : new Rect(0f, 0f, 1f, 1f);
        infoThumb.color = selOpen ? Color.white : new Color(0.35f, 0.34f, 0.38f);
        infoLocked.enabled = !selOpen;
        SetText(infoTitle, ArabicText.Fix("المرحلة " + (sel + 1) + ": " + d.title));
        int st = SaveData.Stars(sel);
        for (int s = 0; s < infoStars.Length; s++) infoStars[s].sprite = Art(s < st ? "star_on" : "star_off");
        bool paidSel = Coins.NeedsBuying(sel);
        string hint = selOpen ? (paidSel ? "تُفتح هذه المرحلة بـ " + Coins.Price + " كوينز" : d.subtitle)
                              : paidSel ? "تُفتح بعد إنهاء المراحل السابقة، وسعرها " + Coins.Price + " كوينز" : "تُفتح بعد إنهاء المراحل السابقة";
        infoHint.text = WrapFix(hint, 34);
        // وصلتها بس تبي كوينز: "فتحها بالكوينز"، وما وصلتها: القلم باهت
        bool buy = selOpen && paidSel;
        SetText(startLabel, ArabicText.Fix(buy ? "فتحها بـ " + Coins.Price + " كوينز" : "لنبدأ!"));
        startButton.color = selOpen ? Color.white : new Color(0.72f, 0.7f, 0.78f);
    }

    // ---------- الكوينز: الشريط، فتح المراحل، والمتجر ----------

    readonly List<Text> coinLabels = new List<Text>();

    void RefreshCoins()
    {
        foreach (Text t in coinLabels) if (t != null) SetText(t, Coins.Balance.ToString());
        if (shopPanel != null && shopPanel.gameObject.activeSelf) RefreshShop();
    }

    // شريط الكوينز (بزاوية الشاشة فوق يسار): زر + أخضر، العدد، وقطعة كوينز بوجه
    void CoinPill(RectTransform parent, Vector2 pos, Action onClick, float rot)
    {
        var pill = Pic(parent, "pill", Vector2.zero, 1f, rot);
        Anchor(pill.rectTransform, new Vector2(0f, 1f), pos);
        Pic(pill.rectTransform, "coin", new Vector2(100f, 2f));
        var n = Label(pill.rectTransform, Coins.Balance.ToString(), 52, new Vector2(-6f, 4f), new Vector2(150f, 80f));
        Fit(n, 140f);
        coinLabels.Add(n);
        var plus = Panel(pill.rectTransform, "Plus", Ink);
        plus.sprite = circle;
        Place(plus.rectTransform, new Vector2(-108f, 6f), new Vector2(64f, 64f));
        var g = Panel(plus.rectTransform, "Fill", CGreen);
        g.sprite = circle;
        Place(g.rectTransform, Vector2.zero, new Vector2(50f, 50f));
        for (int k = 0; k < 2; k++)
        {
            var bar = Panel(g.rectTransform, "Bar", Color.white);
            bar.sprite = rounded;
            bar.type = Image.Type.Sliced;
            bar.pixelsPerUnitMultiplier = 8f;
            Place(bar.rectTransform, Vector2.zero, k == 0 ? new Vector2(28f, 9f) : new Vector2(9f, 28f));
        }
        Click(pill, onClick);
    }

    // شريط النجوم: العدد ونجمة بوجه
    Text StarPill(RectTransform parent, Vector2 pos, float rot)
    {
        var pill = Pic(parent, "pill", Vector2.zero, 1f, rot);
        Anchor(pill.rectTransform, new Vector2(0f, 1f), pos);
        Pic(pill.rectTransform, "star_on", new Vector2(98f, 4f));
        var n = Label(pill.rectTransform, "", 48, new Vector2(-34f, 4f), new Vector2(200f, 80f));
        Fit(n, 190f);
        return n;
    }

    // نفس اللي ورا (عالم كرتوني أو اللعبة مظللة) لما نفتح نافذة من نافذة
    bool OnDeskNow => desk.gameObject.activeSelf;

    // نافذة فتح مرحلة بالكوينز
    RectTransform unlockPanel;
    Text unlockTitle, unlockInfo, unlockHint, unlockAction;
    int unlockLevel;
    Action unlockBack;

    void BuildUnlock()
    {
        unlockPanel = NotebookCard(canvasRect, "Unlock Level", "nb_unlock", new Vector2(900f, 640f));
        unlockTitle = TopRibbon(unlockPanel, 420, "", 46, -2f);
        CloseSticker(unlockPanel, () => unlockBack?.Invoke());
        Pic(unlockPanel, "coin_pile", new Vector2(0f, 125f), 0.72f);
        unlockInfo = Label(unlockPanel, "", 40, new Vector2(0f, -30f), new Vector2(820f, 60f));
        Fit(unlockInfo, 800f);
        unlockHint = Label(unlockPanel, "", 28, new Vector2(0f, -90f), new Vector2(820f, 44f));
        unlockHint.color = InkSoft;
        Fit(unlockHint, 800f);
        unlockAction = PencilBtn(unlockPanel, "short", "", new Vector2(14f, -215f), () =>
        {
            if (Coins.CanAfford)
            {
                if (!Coins.Buy(unlockLevel)) return;
                gm.Sound.Play(SynthAudio.Ding(1.6f), 0.6f);
                gm.StartLevelFromMenu(unlockLevel);
            }
            else ShowShop(() => ShowUnlock(unlockLevel, unlockBack));
        }, 42);
    }

    public void ShowUnlock(int index) => ShowUnlock(index, BackFromHere());

    void ShowUnlock(int index, Action back)
    {
        unlockLevel = index;
        unlockBack = back;
        bool afford = Coins.CanAfford;
        SetText(unlockTitle, ArabicText.Fix("لنفتح المرحلة " + (index + 1)));
        SetText(unlockInfo, ArabicText.Fix("سعرها " + Coins.Price + " كوينز، ومعنا " + Coins.Balance));
        SetText(unlockHint, ArabicText.Fix(afford ? "كل مرحلة ننهيها أول مرة تعطينا " + Coins.Reward + " كوينز" : "الكوينز لا تكفي، نجمعها من المراحل أو من المتجر"));
        SetText(unlockAction, ArabicText.Fix(afford ? "فتح المرحلة" : "المتجر"));
        Open(unlockPanel, OnDeskNow);
    }

    // من وين جينا: نرجع لها بزر الرجوع
    Action BackFromHere()
    {
        if (levelsPanel.gameObject.activeSelf) { bool p = levelsFromPause; return () => ShowLevels(p); }
        if (mainPanel.gameObject.activeSelf) return ShowMain;
        if (completePanel.gameObject.activeSelf) return () => gm.GoToMainMenu();
        return ShowMain;
    }

    // المتجر: حزمة كوينز تكفي عشرين مرحلة
    RectTransform shopPanel;
    Text shopBalance, shopBuy;
    Action shopBack;
    bool buying;

    void BuildShop()
    {
        shopPanel = NotebookCard(canvasRect, "Shop", "nb_shop", new Vector2(1040f, 700f));
        TopRibbon(shopPanel, 340, ArabicText.Fix("المتجر"), 52, -2f);
        CloseSticker(shopPanel, () => shopBack?.Invoke());
        var bal = Pic(shopPanel, "pill_wide", new Vector2(3f, 200f), 0.87f);
        Pic(bal.rectTransform, "coin", new Vector2(146f, 5f), 0.8f);
        shopBalance = Label(bal.rectTransform, "", 36, new Vector2(-33f, 7f), new Vector2(300f, 70f));
        Fit(shopBalance, 280f);

        // بطاقة الحزمة: كومة كوينز، كم تعطي، وقلم الشراء بالسعر
        CartoonBox(shopPanel, "Pack", new Vector2(0f, -55f), new Vector2(880f, 330f), new Color(1f, 0.953f, 0.81f), 1f, 8f);
        Pic(shopPanel, "coin_pile", new Vector2(-260f, -60f));
        var amount = Label(shopPanel, ArabicText.Fix(Coins.Pack + " كوينز"), 68, new Vector2(165f, 50f), new Vector2(520f, 100f));
        amount.color = CGold;
        var edge = amount.gameObject.AddComponent<Outline>();
        edge.effectColor = Ink;
        edge.effectDistance = new Vector2(4f, -4f);
        var drop = amount.gameObject.AddComponent<Shadow>();
        drop.effectColor = Ink;
        drop.effectDistance = new Vector2(5f, -8f);
        amount.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 3f);
        Label(shopPanel, ArabicText.Fix("تكفي لفتح 20 مرحلة كاملة"), 28, new Vector2(165f, -36f), new Vector2(520f, 46f)).color = InkSoft;
        shopBuy = PencilBtn(shopPanel, "short", "", new Vector2(174f, -158f), BuyPack, 42, 0.95f, 3f);

        var note = Label(shopPanel, ArabicText.Fix("كل مرحلة جديدة تعطينا " + Coins.Reward + " كوينز، والإعلان بعدها يعطي " + Coins.Reward + " زيادة!"), 28, new Vector2(0f, -278f), new Vector2(960f, 46f));
        Fit(note, 940f);
    }

    public void ShowShop(Action back)
    {
        shopBack = back;
        buying = false;
        RefreshShop();
        Open(shopPanel, OnDeskNow);
    }

    void RefreshShop()
    {
        SetText(shopBalance, ArabicText.Fix("معنا " + Coins.Balance + " كوينز"));
        // سعر المتجر بالإنجليزي (مثل US$1.99): نحطه يسار الكلام بدون ما ينقلب
        string storePrice = Shop.StorePrice;
        if (buying) SetText(shopBuy, ArabicText.Fix("لحظة..."));
        else if (storePrice != null) SetText(shopBuy, storePrice + "  " + ArabicText.Fix("شراء —"));
        else SetText(shopBuy, ArabicText.Fix("شراء — " + Shop.Price));
    }

    void BuyPack()
    {
        if (buying) return;
        buying = true;
        RefreshShop();
        Shop.BuyPack(ok =>
        {
            buying = false;
            RefreshShop();
            if (ok)
            {
                gm.Sound.Play(SynthAudio.Ding(1.8f), 0.6f);
                Toast(Shop.TestMode ? "وصلت " + Coins.Pack + " كوينز (شراء تجريبي)" : "وصلت " + Coins.Pack + " كوينز، شكرًا لكِ!");
            }
            else Toast("لم يكتمل الشراء");
        });
    }

    // رسالة صغيرة فوق (مثل: حصلنا على كوينز!) تطلع وتختفي لحالها
    RectTransform toast;
    Text toastText;
    Coroutine toastRoutine;

    void BuildToast()
    {
        toast = CartoonBox(canvasRect, "Toast", Vector2.zero, new Vector2(780f, 100f), Cream, 0f, 7f, 0.6f).parent as RectTransform;
        toast.anchorMin = toast.anchorMax = new Vector2(0.5f, 1f);
        toast.anchoredPosition = new Vector2(0f, -100f);
        Pic(toast, "coin", new Vector2(330f, 2f), 0.75f);
        toastText = Label(toast, "", 36, new Vector2(-20f, 4f), new Vector2(640f, 70f));
        Fit(toastText, 600f);
        toast.gameObject.SetActive(false);
    }

    public void Toast(string text)
    {
        SetText(toastText, ArabicText.Fix(text));
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine());
    }

    IEnumerator ToastRoutine()
    {
        toast.gameObject.SetActive(true);
        toast.SetAsLastSibling();
        for (float t = 0f; t < 3f; t += Time.unscaledDeltaTime)
        {
            float pop = Mathf.Clamp01(t / 0.25f), hide = Mathf.Clamp01((3f - t) / 0.3f);
            toast.localScale = Vector3.one * Mathf.Min(pop < 1f ? Mathf.Lerp(0.6f, 1.08f, pop) : 1f, Mathf.Lerp(0.6f, 1f, hide));
            yield return null;
        }
        toast.gameObject.SetActive(false);
    }

    // ---------- من جديد: نتأكد قبل ما نمسح التقدم ----------

    RectTransform confirmPanel;
    Action confirmBack;

    public void ShowConfirm()
    {
        Action back = settingsBack ?? ShowMain;
        confirmBack = () => OpenSettings(back, false);
        Open(confirmPanel, true);
    }

    void BuildConfirm()
    {
        confirmPanel = NotebookCard(canvasRect, "Confirm Restart", "nb_confirm", new Vector2(820f, 600f));
        TopRibbon(confirmPanel, 420, ArabicText.Fix("البدء من جديد؟"), 46, 2f);
        Pic(confirmPanel, "st_restart", new Vector2(0f, 120f), 1.1f, 8f);
        var warn = Label(confirmPanel, ArabicText.Fix("ستُمسح كل المراحل المفتوحة والنجوم"), 34, new Vector2(0f, -15f), new Vector2(760f, 50f));
        warn.color = new Color(0.85f, 0.2f, 0.42f);
        Fit(warn, 740f);
        // من اليمين: "نعم" وبعدها "لا"
        MakeButton(confirmPanel, "نعم، من جديد", new Vector2(160f, -150f), new Vector2(320f, 100f), CPink, () =>
        {
            SaveData.ResetProgress();
            DrawingMemory.Clear();
            gm.PlayFromMenu();
        }, 38);
        MakeButton(confirmPanel, "لا", new Vector2(-180f, -150f), new Vector2(240f, 100f), CGreen, () => confirmBack?.Invoke(), 42);
    }

    // ---------- صندوق الأقلام: لون وشكل خط القلم ----------

    RectTransform pencilPanel;
    readonly List<RectTransform> pencils = new List<RectTransform>();
    readonly List<Text> pencilNames = new List<Text>();
    readonly List<Image> styleButtons = new List<Image>();
    RawImage preview;
    Outline previewGlow;
    Text previewName;

    void BuildPencil()
    {
        pencilPanel = NotebookCard(canvasRect, "Pencil", "nb_pencil", new Vector2(1160f, 960f));
        TopRibbon(pencilPanel, 420, ArabicText.Fix("صندوق الأقلام"), 50, -2f);
        CloseSticker(pencilPanel, ShowMain);
        Label(pencilPanel, ArabicText.Fix("كل قلم له لمسته السحرية"), 32, new Vector2(0f, 360f), new Vector2(800f, 50f)).color = InkSoft;

        // أقلام ملونة واقفة بحافة غامقة
        for (int i = 0; i < PencilStyle.Colors.Length; i++)
        {
            int index = i;
            Color c = PencilStyle.Colors[i];
            var holder = new GameObject("Pencil " + i, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            holder.SetParent(pencilPanel, false);
            holder.GetComponent<Image>().color = Color.clear;
            holder.GetComponent<Image>().raycastTarget = true;
            Place(holder, new Vector2(455f - i * 130f, 100f), new Vector2(110f, 290f));
            var body = Framed(holder, "Body", new Vector2(0f, -30f), new Vector2(92f, 200f), c, Ink, 6f, 3f);
            // رأس القلم: خشب ورصاص
            var tipEdge = Panel(holder, "Tip Edge", Ink);
            tipEdge.sprite = triangle;
            Place(tipEdge.rectTransform, new Vector2(0f, 108f), new Vector2(92f, 66f));
            var wood = Panel(holder, "Tip", new Color(1f, 0.88f, 0.7f));
            wood.sprite = triangle;
            Place(wood.rectTransform, new Vector2(0f, 104f), new Vector2(74f, 50f));
            var lead = Panel(holder, "Lead", Ink);
            lead.sprite = triangle;
            Place(lead.rectTransform, new Vector2(0f, 120f), new Vector2(26f, 20f));
            var band = Panel(body, "Shine", new Color(1f, 1f, 1f, 0.45f));
            Place(band.rectTransform, new Vector2(-24f, 10f), new Vector2(12f, 150f));
            Click(holder.GetComponent<Image>(), () => { PencilStyle.ColorIndex = index; RefreshPencil(); });
            pencils.Add(holder);
            pencilNames.Add(Label(pencilPanel, ArabicText.Fix(PencilStyle.ColorNames[i]), 28, new Vector2(455f - i * 130f, -70f), new Vector2(120f, 40f)));
        }

        Label(pencilPanel, ArabicText.Fix("شكل الخط"), 38, new Vector2(0f, -125f), new Vector2(400f, 50f));
        for (int i = 0; i < PencilStyle.StyleNames.Length; i++)
        {
            int index = i;
            MakeButton(pencilPanel, PencilStyle.StyleNames[i], new Vector2(400f - i * 200f, -195f), new Vector2(184f, 76f),
                Color.white, () => { PencilStyle.Current = (PencilStyle.Style)index; RefreshPencil(); }, 32);
            styleButtons.Add(pencilPanel.GetChild(pencilPanel.childCount - 1).GetChild(0).GetComponent<Image>());
        }

        previewName = Label(pencilPanel, "", 28, new Vector2(0f, -262f), new Vector2(800f, 44f));
        previewName.color = InkSoft;
        preview = new GameObject("Preview", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        preview.transform.SetParent(pencilPanel, false);
        preview.raycastTarget = false;
        Place(preview.rectTransform, new Vector2(0f, -310f), new Vector2(640f, 36f));
        previewGlow = preview.gameObject.AddComponent<Outline>();

        PencilBtn(pencilPanel, "mid", "هذا قلمي!", new Vector2(14f, -412f), ShowMain, 44);
    }

    public void ShowPencil()
    {
        RefreshPencil();
        Open(pencilPanel, true);
    }

    void RefreshPencil()
    {
        int c = PencilStyle.ColorIndex;
        PencilStyle.Style st = PencilStyle.Current;
        for (int i = 0; i < pencils.Count; i++)
        {
            bool on = i == c;
            pencils[i].anchoredPosition = new Vector2(455f - i * 130f, on ? 130f : 100f);
            pencils[i].localRotation = Quaternion.Euler(0f, 0f, on ? 0f : (i % 2 == 0 ? 3f : -3f));
            pencilNames[i].color = on ? PinkDark : Grey;
        }
        for (int i = 0; i < styleButtons.Count; i++) styleButtons[i].color = i == (int)st ? new Color(1f, 0.88f, 0.9f) : Color.white;

        // معاينة شكل الخط
        Color color = PencilStyle.CurrentColor;
        preview.texture = null;
        preview.uvRect = new Rect(0f, 0f, 1f, 1f);
        preview.color = color;
        previewGlow.enabled = false;
        switch (st)
        {
            case PencilStyle.Style.Dotted:
                var dash = new Texture2D(16, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
                var px = new Color[16];
                for (int i = 0; i < 16; i++) px[i] = new Color(1f, 1f, 1f, i < 10 ? 1f : 0f);
                dash.SetPixels(px);
                dash.Apply();
                preview.texture = dash;
                preview.uvRect = new Rect(0f, 0f, 9f, 1f);
                break;
            case PencilStyle.Style.Glitter:
                preview.color = Color.Lerp(color, Color.white, 0.3f);
                previewGlow.enabled = true;
                previewGlow.effectColor = new Color(1f, 1f, 1f, 0.9f);
                previewGlow.effectDistance = new Vector2(3f, -3f);
                break;
            case PencilStyle.Style.Neon:
                preview.color = Color.Lerp(color, Color.white, 0.4f);
                previewGlow.enabled = true;
                previewGlow.effectColor = new Color(color.r, color.g, color.b, 0.8f);
                previewGlow.effectDistance = new Vector2(8f, -8f);
                break;
            case PencilStyle.Style.Rainbow:
                preview.texture = PencilStyle.RainbowTexture();
                preview.color = Color.white;
                preview.uvRect = new Rect(0f, 0f, 2f, 1f);
                break;
        }
        previewName.text = ArabicText.Fix("الخط: " + PencilStyle.StyleNames[(int)st] + " — " + PencilStyle.ColorNames[c]);
    }

    // ---------- الاستراحة ----------

    RectTransform pauseIcon;

    void BuildPause()
    {
        pausePanel = NotebookCard(canvasRect, "Pause", "nb_pause", new Vector2(720f, 900f));
        TopRibbon(pausePanel, 420, ArabicText.Fix("استراحة"), 52, 2f);
        pauseIcon = Sticker(pausePanel, "play", new Vector2(0f, 245f), () => gm.Resume(), 1.5f, 0f, "متابعة اللعب", 38).rectTransform;
        Sticker(pausePanel, "restart", new Vector2(150f, 15f), () => gm.RestartLevel(), 1f, -4f, "إعادة المرحلة");
        Sticker(pausePanel, "map", new Vector2(-150f, 15f), () => ShowLevels(true), 1f, 4f, "الخريطة");
        Sticker(pausePanel, "gear", new Vector2(150f, -210f), () => ShowSettings(pausePanel), 1f, 3f, "الإعدادات");
        Sticker(pausePanel, "home", new Vector2(-150f, -210f), () => gm.GoToMainMenu(), 1f, -3f, "الرئيسية");
        Label(pausePanel, ArabicText.Fix("الدفتر في الانتظار..."), 28, new Vector2(0f, -385f), new Vector2(500f, 44f)).color = InkSoft;
    }

    // ---------- شاشة الفوز: شريط "أحسنتِ!"، نجوم بوجوه، الكوينز وزر الإعلان، وقلم الصفحة التالية ----------

    Text completeTitle, completeLevel, nextLabel;
    readonly Image[] bigStars = new Image[SaveData.MaxStars];
    readonly bool[] bigStarOn = new bool[SaveData.MaxStars];
    readonly Text[] reasons = new Text[SaveData.MaxStars];
    readonly Image[] reasonChecks = new Image[SaveData.MaxStars];
    RectTransform burst, completeCoins, confettiRoot;
    Text completeCoinsText;
    Action nextAction;
    readonly List<RectTransform> confetti = new List<RectTransform>();
    readonly List<Vector3> confettiVel = new List<Vector3>();

    void BuildComplete()
    {
        completePanel = NotebookCard(canvasRect, "Complete", "nb_complete", new Vector2(880f, 700f));
        completePanel.anchoredPosition = new Vector2(0f, -10f);

        // قصاصات ملونة بحافة غامقة تطيح ورا الدفتر
        confettiRoot = new GameObject("Confetti", typeof(RectTransform)).GetComponent<RectTransform>();
        confettiRoot.SetParent(completePanel, false);
        confettiRoot.SetSiblingIndex(0);
        Place(confettiRoot, Vector2.zero, new Vector2(1920f, 1080f));
        Color[] cols = { CPink, CYellow, CBlue, CGreen, CPurple, COrange };
        var rng = new System.Random(7);
        for (int i = 0; i < 46; i++)
        {
            var piece = Panel(confettiRoot, "Confetti", cols[i % cols.Length]);
            piece.sprite = rounded;
            piece.type = Image.Type.Sliced;
            piece.pixelsPerUnitMultiplier = 6f;
            var o = piece.gameObject.AddComponent<Outline>();
            o.effectColor = Ink;
            o.effectDistance = new Vector2(2f, -2f);
            Place(piece.rectTransform, new Vector2(rng.Next(-980, 980), rng.Next(-560, 560)), new Vector2(18f + (i % 3) * 6f, 12f));
            confetti.Add(piece.rectTransform);
            confettiVel.Add(new Vector3(rng.Next(-30, 30), -70f - rng.Next(90), rng.Next(-200, 200)));
        }

        burst = Pic(completePanel, "burst", new Vector2(0f, 170f)).rectTransform;
        // النجمة الأولى يمين (القراءة من اليمين)، واللي بالنص أكبر
        Vector2[] starPos = { new Vector2(160f, 150f), new Vector2(0f, 190f), new Vector2(-160f, 150f) };
        for (int i = 0; i < SaveData.MaxStars; i++)
            bigStars[i] = Pic(completePanel, "star_off", starPos[i], i == 1 ? 1.7f : 1.25f);
        completeTitle = TopRibbon(completePanel, 420, "", 52, 2f);
        completeLevel = Label(completePanel, "", 34, new Vector2(0f, 34f), new Vector2(800f, 56f));
        Fit(completeLevel, 760f);

        for (int i = 0; i < SaveData.MaxStars; i++)
        {
            float y = -38f - i * 52f;
            reasonChecks[i] = Pic(completePanel, "check_on", new Vector2(252f, y));
            reasons[i] = Label(completePanel, "", 28, new Vector2(36f, y + 2f), new Vector2(360f, 44f));
            reasons[i].alignment = TextAnchor.MiddleRight;
        }

        // الكوينز اللي أخذتها (أول مرة بس)، وتحتها زر إعلان بمكافأة يعطي ٥٠ زيادة (١٠٠ بدل ٥٠)
        var pill = Pic(completePanel, "pill", new Vector2(-259f, -54f), 0.75f, 6f);
        completeCoins = pill.rectTransform;
        Pic(completeCoins, "coin", new Vector2(72f, 2f), 0.62f);
        completeCoinsText = Label(completeCoins, "", 46, new Vector2(-22f, 5f), new Vector2(140f, 70f));
        completeCoinsText.color = CGold;
        var ce = completeCoinsText.gameObject.AddComponent<Outline>();
        ce.effectColor = Ink;
        ce.effectDistance = new Vector2(2.5f, -2.5f);

        var reward = Pic(completePanel, "btn_green", new Vector2(-250f, -156f), 1f, 4f);
        Click(reward, WatchRewardAd);
        var rt = Label(reward.rectTransform, ArabicText.Fix("شاهد إعلانًا واربح " + Coins.Reward), 28, new Vector2(-33f, 7f), new Vector2(230f, 60f));
        Toon(rt, 2.5f);
        Fit(rt, 205f);
        rewardButton = reward.gameObject;

        nextLabel = PencilBtn(completePanel, "mid", "", new Vector2(14f, -271f), () => nextAction?.Invoke(), 46);
        Sticker(completePanel, "restart", new Vector2(-397f, -285f), () => gm.RestartLevel(), 0.9f, 6f);
        Sticker(completePanel, "map", new Vector2(403f, -285f), () => ShowLevels(false), 0.85f, -6f);
    }

    GameObject rewardButton;
    int completeWon;

    void WatchRewardAd()
    {
        if (!rewardButton.activeSelf) return;
        rewardButton.SetActive(false);
        Ads.ShowReward(ok =>
        {
            if (!ok) { rewardButton.SetActive(Ads.RewardReady); return; }
            Coins.Add(Coins.Reward);
            completeWon += Coins.Reward;
            completeCoinsText.text = "+" + completeWon;
            gm.Sound.Play(SynthAudio.Ding(2f), 0.6f);
            Toast("حصلنا على " + Coins.Reward + " كوينز إضافية");
        });
    }

    public void ShowComplete(int index, bool[] earned, bool last, int coinsWon, Action next)
    {
        nextAction = next;
        completeWon = coinsWon;
        rewardButton.SetActive(coinsWon > 0 && Ads.RewardReady);
        completeCoins.gameObject.SetActive(coinsWon > 0);
        completeCoins.localScale = Vector3.zero;
        completeCoinsText.text = "+" + coinsWon;
        SetText(completeTitle, ArabicText.Fix(last ? "اكتملت كل المراحل!" : "أحسنتِ!"));
        SetText(completeLevel, ArabicText.Fix("المرحلة " + (index + 1) + " — " + Levels.All[index].title));
        SetText(nextLabel, ArabicText.Fix(last ? "القائمة الرئيسية" : "الصفحة التالية"));
        string[] texts = { "إنهاء المرحلة", "بلا خسارة أي قلب", "توفير الحبر" };
        for (int i = 0; i < SaveData.MaxStars; i++)
        {
            reasons[i].text = ArabicText.Fix(texts[i]);
            reasons[i].color = earned[i] ? Ink : InkFaint;
            reasonChecks[i].sprite = Art(earned[i] ? "check_on" : "check_off");
            bigStars[i].sprite = Art("star_off");
            bigStarOn[i] = false;
            bigStars[i].rectTransform.localScale = Vector3.one;
            bigStars[i].rectTransform.localRotation = Quaternion.identity;
        }
        Open(completePanel);
        StartCoroutine(StarsRoutine(earned));
    }

    IEnumerator StarsRoutine(bool[] earned)
    {
        // النجوم تطلع وحدة وحدة: من اليمين لليسار
        int count = 0;
        for (int i = 0; i < SaveData.MaxStars; i++)
        {
            yield return new WaitForSecondsRealtime(i == 0 ? 0.45f : 0.32f);
            if (!earned[i]) continue;
            count++;
            gm.Sound.Play(SynthAudio.Ding(1f + count * 0.25f), 0.6f);
            Image s = bigStars[i];
            s.sprite = Art("star_on");
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
            {
                float scale = t < 0.6f ? Mathf.Lerp(0f, 1.3f, t / 0.6f) : Mathf.Lerp(1.3f, 1f, (t - 0.6f) / 0.4f);
                s.rectTransform.localScale = Vector3.one * scale;
                s.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (1f - t) * 40f);
                yield return null;
            }
            s.rectTransform.localScale = Vector3.one;
            s.rectTransform.localRotation = Quaternion.identity;
            bigStarOn[i] = true;
        }
        // وبعد النجوم تنط الكوينز
        if (!completeCoins.gameObject.activeSelf) yield break;
        yield return new WaitForSecondsRealtime(0.25f);
        gm.Sound.Play(SynthAudio.Ding(2f), 0.5f);
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
        {
            completeCoins.localScale = Vector3.one * (t < 0.6f ? Mathf.Lerp(0f, 1.2f, t / 0.6f) : Mathf.Lerp(1.2f, 1f, (t - 0.6f) / 0.4f));
            yield return null;
        }
        completeCoins.localScale = Vector3.one;
    }

    void Update()
    {
        if (mainPanel == null || completePanel == null) return;   // القوائم انحذفت (وقفنا اللعب)
        float t = Time.unscaledTime, dt = Time.unscaledDeltaTime;
        if (howToPanel != null && howToPanel.gameObject.activeSelf) AnimateHowTo(t);
        // الشعار يتمايل بهدوء
        if (mainPanel.gameObject.activeSelf)
        {
            logoRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.3f) * 1.5f);
            logoRect.localScale = Vector3.one * (1f + Mathf.Sin(t * 2.1f) * 0.02f);
        }
        // الخريطة: علامة "أنتِ هنا" تنط، والمرحلة المختارة تنبض
        if (levelsPanel.gameObject.activeSelf)
        {
            marker.rectTransform.anchoredPosition = markerBase + new Vector2(0f, Mathf.Abs(Mathf.Sin(t * 4f)) * 12f);
            if (selected >= 0 && selected < nodeImages.Count)
                nodeImages[selected].rectTransform.localScale = Vector3.one * nodeScales[selected] * (1f + Mathf.Sin(t * 4f) * 0.05f);
        }
        if (pausePanel.gameObject.activeSelf)
            pauseIcon.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.5f) * 6f);
        // شاشة الفوز: الانفجار يتنفس، النجوم تتمايل، والقصاصات تطيح
        if (completePanel.gameObject.activeSelf)
        {
            burst.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.9f) * 5f);
            burst.localScale = Vector3.one * (1f + Mathf.Sin(t * 2.2f) * 0.04f);
            for (int i = 0; i < SaveData.MaxStars; i++)
                if (bigStarOn[i]) bigStars[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2f + i) * 6f);
            for (int i = 0; i < confetti.Count; i++)
            {
                Vector3 v = confettiVel[i];
                Vector2 p = confetti[i].anchoredPosition;
                p += new Vector2(v.x + Mathf.Sin(t * 2f + i) * 30f, v.y) * dt;
                if (p.y < -600f) p = new Vector2(UnityEngine.Random.Range(-980f, 980f), 600f);
                confetti[i].anchoredPosition = p;
                confetti[i].localRotation = Quaternion.Euler(0f, 0f, t * v.z);
            }
        }
    }

    // ---------- أدوات البناء ----------

    // دفتر كرتوني (نفس الدفتر اللي تمشي عليه رويدا): السلك فوق والحافة بوجه تحت، والصفحة نفسها بمقاس النافذة
    RectTransform NotebookCard(Transform parent, string name, string sprite, Vector2 size, float tilt = 0f)
    {
        var root = Panel(parent, name, Color.clear);
        root.raycastTarget = true;
        RectTransform rt = root.rectTransform;
        Place(rt, Vector2.zero, size);
        rt.localRotation = Quaternion.Euler(0f, 0f, tilt);
        root.gameObject.AddComponent<CanvasGroup>();
        var book = Panel(rt, "Notebook", Color.white);
        book.sprite = Art(sprite);
        // الصورة أكبر من الصفحة: 20 يمين ويسار، 66 فوق للسلك، و94 تحت للحافة
        Place(book.rectTransform, new Vector2(0f, -14f), size + new Vector2(40f, 160f));
        panels.Add(rt);
        return rt;
    }

    // صورة من قطع الواجهة بمقاسها الطبيعي (الصور مرسومة بضعف المقاس)
    Image Pic(RectTransform parent, string sprite, Vector2 pos, float scale = 1f, float rot = 0f)
    {
        var img = Panel(parent, sprite, Color.white);
        img.sprite = Art(sprite);
        Vector2 size = img.sprite != null ? img.sprite.rect.size * 0.5f : new Vector2(100f, 100f);
        Place(img.rectTransform, pos, size * scale);
        img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rot);
        return img;
    }

    // يخلي الصورة زر: ينضغط، يكبر شوي، ويطلع صوت نقرة
    GameObject Click(Graphic g, Action onClick)
    {
        g.raycastTarget = true;
        var button = g.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => onClick());
        g.gameObject.AddComponent<ButtonJuice>().Init(gm, clickClip);
        return g.gameObject;
    }

    // زر ستيكر دايري بأيقونة (وتحته اسمه لو انعطى)
    Image Sticker(RectTransform parent, string icon, Vector2 pos, Action onClick, float scale = 1f, float rot = 0f, string caption = null, int captionSize = 32)
    {
        var img = Pic(parent, "st_" + icon, pos, scale, rot);
        Click(img, onClick);
        if (caption != null)
        {
            float half = img.rectTransform.sizeDelta.y * 0.5f;
            // اسم الزر بلون الحبر الغامق (أوضح على ورق الدفتر من الأبيض بحافة)
            Label(parent, ArabicText.Fix(caption), captionSize, pos + new Vector2(0f, -half + 2f), new Vector2(300f, 50f));
        }
        return img;
    }

    // زر X وردي بزاوية الدفتر فوق يمين (مثل التصميم)
    Image CloseSticker(RectTransform card, Action onClick)
    {
        Vector2 s = card.sizeDelta;
        return Sticker(card, "close", new Vector2(s.x * 0.5f - 24f, s.y * 0.5f - 18f), onClick, 1f, -6f);
    }

    // زر على شكل قلم (اللعب، التالي، الشراء...): الكلام على جسم القلم يسار الوجه
    // long: قلم طويل بوجه، mid: متوسط بوجه، short: قصير بدون وجه
    Text PencilBtn(RectTransform parent, string kind, string label, Vector2 pos, Action onClick, int fontSize, float scale = 1f, float rot = -3f)
    {
        var img = Pic(parent, "pencil_" + kind, pos, scale, rot);
        Click(img, onClick);
        float x = kind == "long" ? -110f : kind == "mid" ? -66f : -16f;
        float w = kind == "long" ? 330f : kind == "mid" ? 240f : 260f;
        var t = Label(img.rectTransform, ArabicText.Fix(label), fontSize, new Vector2(x, 6f) * scale, new Vector2(w + 60f, 110f) * scale);
        Toon(t, 4f);
        Fit(t, w * scale);
        return t;
    }

    // شريط عنوان أصفر بكلام غامق
    Text Ribbon(RectTransform parent, int width, string text, Vector2 pos, int size, float rot = 0f)
    {
        var img = Pic(parent, "ribbon_" + width, pos, 1f, rot);
        var t = Label(img.rectTransform, text, size, new Vector2(-4f, -2f), new Vector2(width, 90f));
        Fit(t, width - 30f);
        return t;
    }

    // الشريط فوق الدفتر على السلك (مثل التصميم)
    Text TopRibbon(RectTransform card, int width, string text, int size, float rot = 0f)
    {
        return Ribbon(card, width, text, new Vector2(4f, card.sizeDelta.y * 0.5f - 12f), size, rot);
    }

    // مربع كرتوني: ظل حاد غامق، حافة غامقة، ولون من جوا (يرجع الجزء اللي جوا)
    RectTransform CartoonBox(RectTransform parent, string name, Vector2 pos, Vector2 size, Color fill, float rot, float edge = 7f, float round = 1.2f)
    {
        var outer = Panel(parent, name, Ink);
        outer.sprite = rounded;
        outer.type = Image.Type.Sliced;
        outer.pixelsPerUnitMultiplier = round;
        Place(outer.rectTransform, pos, size);
        outer.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rot);
        var sh = outer.gameObject.AddComponent<Shadow>();
        sh.effectColor = Ink;
        sh.effectDistance = new Vector2(5f, -8f);
        var inner = Panel(outer.rectTransform, "Fill", fill);
        inner.sprite = rounded;
        inner.type = Image.Type.Sliced;
        inner.pixelsPerUnitMultiplier = round * 1.15f;
        Stretch(inner.rectTransform);
        inner.rectTransform.offsetMin = new Vector2(edge, edge);
        inner.rectTransform.offsetMax = new Vector2(-edge, -edge);
        return inner.rectTransform;
    }

    // مجموعة أزرار بزوايا الشاشة لنافذة (تطلع وتختفي معها)، داخل المنطقة الآمنة للجوال
    RectTransform CornersOf(RectTransform panel)
    {
        var c = new GameObject(panel.name + " Corners", typeof(RectTransform)).GetComponent<RectTransform>();
        c.SetParent(canvasRect, false);
        Stretch(c);
        c.gameObject.AddComponent<SafeArea>();
        corners[panel] = c;
        return c;
    }

    static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
    }

    // كلام أبيض بحافة غامقة (فوق الأزرار والستيكرات)
    static void Toon(Text t, float edge)
    {
        t.color = Color.white;
        var o = t.gameObject.AddComponent<Outline>();
        o.effectColor = Ink;
        o.effectDistance = new Vector2(edge, -edge);
    }

    // لو الكلام أعرض من مكانه نصغر الخط لين يدخل (المقاس الأصلي وأقصى عرض محفوظين)
    readonly Dictionary<Text, Vector2> fits = new Dictionary<Text, Vector2>();

    void Fit(Text t, float maxWidth)
    {
        if (!fits.ContainsKey(t)) fits[t] = new Vector2(t.fontSize, maxWidth);
        Vector2 f = fits[t];
        t.fontSize = (int)f.x;
        if (string.IsNullOrEmpty(t.text)) return;
        while (t.fontSize > 16 && t.preferredWidth > f.y) t.fontSize -= 2;
    }

    void SetText(Text t, string fixedText)
    {
        t.text = fixedText;
        if (fits.ContainsKey(t)) Fit(t, 0f);
    }

    // خطوط دفتر خفيفة
    void Lines(RectTransform parent, Vector2 size, float gap)
    {
        for (float y = size.y * 0.5f - gap; y > -size.y * 0.5f; y -= gap)
        {
            var line = Panel(parent, "Line", LineBlue);
            Place(line.rectTransform, new Vector2(0f, y - 40f), new Vector2(size.x, 3f));
        }
    }

    // مستطيل مدور بحافة ملونة: الإطار، وجواه اللون
    RectTransform Framed(RectTransform parent, string name, Vector2 pos, Vector2 size, Color fill, Color border, float borderWidth, float roundness)
    {
        var outer = Panel(parent, name, border);
        outer.sprite = rounded;
        outer.type = Image.Type.Sliced;
        outer.pixelsPerUnitMultiplier = roundness;
        Place(outer.rectTransform, pos, size);
        var inner = Panel(outer.rectTransform, "Fill", fill);
        inner.sprite = rounded;
        inner.type = Image.Type.Sliced;
        inner.pixelsPerUnitMultiplier = roundness * 1.1f;
        Stretch(inner.rectTransform);
        inner.rectTransform.offsetMin = new Vector2(borderWidth, borderWidth);
        inner.rectTransform.offsetMax = new Vector2(-borderWidth, -borderWidth);
        return inner.rectTransform;
    }

    Text PrimaryButton(RectTransform parent, string label, Vector2 pos, Vector2 size, Action onClick, int fontSize)
    {
        return MakeButton(parent, label, pos, size, CPink, onClick, fontSize);
    }

    Text SecondaryButton(RectTransform parent, string label, Vector2 pos, Vector2 size, Action onClick, int fontSize, Color? color = null)
    {
        return MakeButton(parent, label, pos, size, color ?? CBlue, onClick, fontSize);
    }

    // زر كرتوني مدور: حافة غامقة وظل حاد، لون فاتح، جزء أغمق تحت، ولمعة بيضا فوق
    Text MakeButton(RectTransform parent, string label, Vector2 pos, Vector2 size, Color fill, Action onClick, int fontSize)
    {
        bool white = fill.r > 0.95f && fill.g > 0.95f && fill.b > 0.95f;
        var outer = Panel(parent, "Button " + label, Ink);
        outer.sprite = rounded;
        outer.type = Image.Type.Sliced;
        outer.pixelsPerUnitMultiplier = 0.5f;   // أطراف مدورة كاملة
        Place(outer.rectTransform, pos, size);
        var sh = outer.gameObject.AddComponent<Shadow>();
        sh.effectColor = Ink;
        sh.effectDistance = new Vector2(4f, -7f);
        var inner = Panel(outer.rectTransform, "Fill", fill);
        inner.sprite = rounded;
        inner.type = Image.Type.Sliced;
        inner.pixelsPerUnitMultiplier = 0.55f;
        Stretch(inner.rectTransform);
        inner.rectTransform.offsetMin = new Vector2(7f, 7f);
        inner.rectTransform.offsetMax = new Vector2(-7f, -7f);
        var shade = Panel(inner.rectTransform, "Shade", new Color(0f, 0f, 0f, white ? 0.06f : 0.12f));
        shade.sprite = rounded;
        shade.type = Image.Type.Sliced;
        shade.pixelsPerUnitMultiplier = 0.9f;
        shade.rectTransform.anchorMin = Vector2.zero;
        shade.rectTransform.anchorMax = new Vector2(1f, 0.4f);
        shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;
        var shine = Panel(inner.rectTransform, "Shine", new Color(1f, 1f, 1f, 0.6f));
        shine.sprite = rounded;
        shine.type = Image.Type.Sliced;
        shine.pixelsPerUnitMultiplier = 6f;
        shine.rectTransform.anchorMin = new Vector2(0f, 1f);
        shine.rectTransform.anchorMax = new Vector2(1f, 1f);
        shine.rectTransform.offsetMin = new Vector2(size.y * 0.4f, -15f);
        shine.rectTransform.offsetMax = new Vector2(-size.y * 0.9f, -8f);
        Click(outer, onClick);
        Text t = Label(outer.rectTransform, ArabicText.Fix(label), fontSize, new Vector2(0f, 3f), size);
        if (white) t.color = Ink;
        else Toon(t, 3f);
        Fit(t, size.x - 40f);
        return t;
    }

    Text Label(RectTransform parent, string text, int size, Vector2 pos, Vector2 box)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.verticalOverflow = VerticalWrapMode.Overflow;   // الخط العربي سطره عالي: ما نخليه ينقص ويختفي
        t.text = text;
        t.fontSize = ArabicText.Size(size);
        t.fontStyle = FontStyle.Normal;   // الخط الكرتوني عريض من نفسه
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Ink;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        Place(t.rectTransform, pos, box);
        return t;
    }

    // نقسم النص العربي لسطور (من البداية)، وبعدين نقلب كل سطر لحاله عشان ما ينعكس ترتيب السطور
    static string WrapFix(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var lines = new List<string>();
        string line = "";
        foreach (string w in text.Split(' '))
        {
            if (line.Length > 0 && line.Length + 1 + w.Length > maxChars) { lines.Add(line); line = w; }
            else line = line.Length == 0 ? w : line + " " + w;
        }
        if (line.Length > 0) lines.Add(line);
        for (int i = 0; i < lines.Count; i++) lines[i] = ArabicText.Fix(lines[i]);
        return string.Join("\n", lines);
    }

    // أرقام عربية (١ ٢ ٣)
    static string ArabicDigits(int n)
    {
        char[] s = n.ToString().ToCharArray();
        for (int i = 0; i < s.Length; i++) s[i] = (char)('٠' + (s[i] - '0'));
        return new string(s);
    }

    static Image Panel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ---------- رسومات ----------

    // مستطيل بزوايا دائرية ناعمة (يتمدد بدون ما تخرب الزوايا، و pixelsPerUnitMultiplier يتحكم بكبر الزاوية)
    public static Sprite RoundedSprite()
    {
        const int s = 128;
        const float r = 40f;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { name = "Rounded", wrapMode = TextureWrapMode.Clamp };
        var px = new Color[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = Mathf.Max(r - (x + 0.5f), (x + 0.5f) - (s - r), 0f);
                float dy = Mathf.Max(r - (y + 0.5f), (y + 0.5f) - (s - r), 0f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                px[y * s + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.5f));
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r + 2, r + 2, r + 2, r + 2));
    }

    public static Sprite CircleSprite()
    {
        const int s = 128;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, true) { name = "Circle", wrapMode = TextureWrapMode.Clamp };
        var px = new Color[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(s * 0.5f, s * 0.5f));
                px[y * s + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(s * 0.5f - 1f - d + 0.5f));
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }

    // قطعة كوينز ذهبية: حافة أغمق، دايرة محفورة جوا، نجمة بالنص، ولمعة فوق
    public static Sprite CoinSprite()
    {
        const int s = 128;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, true) { name = "Coin", wrapMode = TextureWrapMode.Clamp };
        var px = new Color[s * s];
        Color rim = new Color(0.86f, 0.55f, 0.12f), body = new Color(1f, 0.79f, 0.22f), face = new Color(1f, 0.86f, 0.36f), line = new Color(0.88f, 0.6f, 0.14f);
        float R = s * 0.5f - 1f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                Vector2 p = new Vector2(x + 0.5f - s * 0.5f, y + 0.5f - s * 0.5f);
                float d = p.magnitude;
                Color c = d > R * 0.88f ? rim : d > R * 0.7f ? body : face;
                float ring = Mathf.Abs(d - R * 0.7f);
                if (ring < 2.5f) c = Color.Lerp(c, line, Mathf.Clamp01(2.5f - ring));
                // نجمة خماسية بالنص
                float a = Mathf.Atan2(p.y, p.x) - Mathf.PI * 0.5f;
                float k = Mathf.Repeat(a, Mathf.PI * 0.4f) / (Mathf.PI * 0.4f);
                float edge = Mathf.Lerp(R * 0.22f, R * 0.5f, Mathf.Abs(k - 0.5f) * 2f);
                if (d < edge) c = Color.Lerp(c, line, Mathf.Clamp01(edge - d));
                // لمعة فوق يسار
                float sh = Vector2.Distance(p, new Vector2(-R * 0.42f, R * 0.42f));
                if (sh < R * 0.16f) c = Color.Lerp(c, Color.white, 0.7f * Mathf.Clamp01((R * 0.16f - sh) / 4f));
                c.a = Mathf.Clamp01(R - d + 0.5f);
                px[y * s + x] = c;
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }

    // مثلث رأسه فوق (رأس القلم)
    public static Sprite TriangleSprite()
    {
        const int s = 64;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, true) { name = "Triangle", wrapMode = TextureWrapMode.Clamp };
        var px = new Color[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float half = (1f - (y + 0.5f) / s) * s * 0.5f;
                float d = half - Mathf.Abs(x + 0.5f - s * 0.5f);
                px[y * s + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(d + 0.5f));
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }

    // وجه مبتسم برتقالي
    static Sprite SmileySprite()
    {
        const int s = 256;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, true) { name = "Smiley", wrapMode = TextureWrapMode.Clamp };
        var px = new Color[s * s];
        Color face = new Color(1f, 0.62f, 0.36f), edge = new Color(0.62f, 0.33f, 0.22f), eye = new Color(0.36f, 0.2f, 0.15f);
        Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float d = Vector2.Distance(p, c);
                float a = Mathf.Clamp01(s * 0.5f - 2f - d + 0.5f);
                Color col = d > s * 0.5f - 12f ? edge : face;
                // عيون
                for (int e = -1; e <= 1; e += 2)
                {
                    Vector2 ep = c + new Vector2(e * 42f, 30f);
                    float ed = Vector2.Distance(p, ep);
                    if (ed < 13f) col = Color.Lerp(col, eye, Mathf.Clamp01(13f - ed));
                }
                // ابتسامة: قوس تحت
                Vector2 m = p - (c + new Vector2(0f, 20f));
                float md = Mathf.Abs(m.magnitude - 70f);
                if (m.y < -25f && md < 7f) col = Color.Lerp(col, edge, Mathf.Clamp01(7f - md));
                // خدود وردية
                for (int e = -1; e <= 1; e += 2)
                {
                    float cd = Vector2.Distance(p, c + new Vector2(e * 72f, -18f));
                    if (cd < 18f) col = Color.Lerp(col, new Color(1f, 0.5f, 0.5f), 0.35f * Mathf.Clamp01((18f - cd) / 6f));
                }
                col.a = a;
                px[y * s + x] = col;
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }

    // نجمة بخمس أطراف مع حافة ولمعة
    static Sprite StarSprite()
    {
        const int s = 128;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, true) { name = "Star", wrapMode = TextureWrapMode.Clamp };
        var px = new Color[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float coverage = 0f, inner = 0f;
                for (int sy = 0; sy < 3; sy++)
                    for (int sx = 0; sx < 3; sx++)
                    {
                        float u = (x + (sx + 0.5f) / 3f) / s - 0.5f, v = (y + (sy + 0.5f) / 3f) / s - 0.47f;
                        float ang = Mathf.Atan2(u, v);
                        float r = Mathf.Sqrt(u * u + v * v);
                        // نجمة بحواف مستقيمة: الشعاع يقطع الخط اللي بين الطرف (0.46) والزاوية الداخلية (0.2)
                        float seg = Mathf.PI / 5f;
                        float phi = Mathf.Abs(Mathf.Repeat(ang + seg, seg * 2f) - seg);
                        Vector2 p0 = new Vector2(0.46f, 0f), p1 = new Vector2(0.2f * Mathf.Cos(seg), 0.2f * Mathf.Sin(seg));
                        Vector2 e = p1 - p0, d = new Vector2(Mathf.Cos(phi), Mathf.Sin(phi));
                        float limit = (p0.x * e.y - p0.y * e.x) / (d.x * e.y - d.y * e.x);
                        if (r < limit) coverage += 1f / 9f;
                        if (r < limit - 0.045f) inner += 1f / 9f;
                    }
                float hu = (float)x / s - 0.42f, hv = (float)y / s - 0.62f;
                float shine = Mathf.Clamp01(1f - Mathf.Sqrt(hu * hu + hv * hv) / 0.13f) * 0.6f;
                Color c = Color.Lerp(Color.white * 0.92f, Color.white, shine);
                c = Color.Lerp(c, new Color(0.7f, 0.55f, 0.4f), Mathf.Clamp01(coverage - inner));
                c.a = coverage;
                px[y * s + x] = c;
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }
}

// الزر يكبر شوي لما يمر عليه الماوس وينضغط لما تضغطين، مع صوت نقرة
public class ButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    GameManager gm;
    AudioClip click;
    bool hover, down;
    float scale = 1f;

    public void Init(GameManager manager, AudioClip clip)
    {
        gm = manager;
        click = clip;
    }

    public void OnPointerEnter(PointerEventData e) { hover = true; }
    public void OnPointerExit(PointerEventData e) { hover = false; down = false; }
    public void OnPointerDown(PointerEventData e)
    {
        down = true;
        if (gm != null && gm.Sound != null) gm.Sound.Play(click, 0.8f);
    }
    public void OnPointerUp(PointerEventData e) { down = false; }

    void OnDisable()
    {
        hover = down = false;
        scale = 1f;
        transform.localScale = Vector3.one;
    }

    void Update()
    {
        float goal = down ? 0.94f : hover ? 1.06f : 1f;
        scale = Mathf.Lerp(scale, goal, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 18f));
        transform.localScale = Vector3.one * scale;
    }
}
