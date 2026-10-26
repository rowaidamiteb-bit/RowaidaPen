using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// القوائم: القائمة الرئيسية، خريطة المراحل، صندوق الأقلام، الاستراحة، شاشة الفوز، والانتقال بين المراحل (صفحة تنقلب)
// الشكل: بطاقات كريمية بحافة كحلية عريضة (مثل شاشة جوال) على خلفية دفتر، أزرار وردية مدورة، ورسومات بسيطة ملونة
public class MenuUI : MonoBehaviour
{
    static readonly Color Cream = new Color(1f, 0.95f, 0.82f);
    static readonly Color Card = new Color(1f, 0.97f, 0.9f);
    static readonly Color Navy = new Color(0.17f, 0.24f, 0.31f);
    static readonly Color Grey = new Color(0.45f, 0.47f, 0.5f);
    static readonly Color LineBlue = new Color(0.6f, 0.75f, 0.9f, 0.35f);
    static readonly Color Pink = new Color(1f, 0.47f, 0.56f);
    static readonly Color PinkDark = new Color(0.86f, 0.33f, 0.45f);
    static readonly Color PinkSoft = new Color(1f, 0.6f, 0.72f);
    static readonly Color Teal = new Color(0.36f, 0.78f, 0.78f);
    static readonly Color Yellow = new Color(1f, 0.82f, 0.33f);
    static readonly Color YellowDark = new Color(0.93f, 0.66f, 0.2f);
    static readonly Color Mint = new Color(0.9f, 0.97f, 0.93f);
    static readonly Color MintBorder = new Color(0.47f, 0.8f, 0.62f);
    static readonly Color Locked = new Color(0.78f, 0.77f, 0.75f);
    static readonly Color LockedDark = new Color(0.6f, 0.59f, 0.57f);
    static readonly Color Gold = new Color(1f, 0.78f, 0.22f);
    static readonly Color StarOff = new Color(0.82f, 0.8f, 0.78f);
    static readonly Color Dots = new Color(1f, 0.55f, 0.45f);
    static readonly Color PaperWhite = new Color(1f, 0.995f, 0.97f);
    static readonly Color SkyBlue = new Color(0.35f, 0.68f, 1f);
    static readonly Color MintC = new Color(0.3f, 0.8f, 0.62f);
    static readonly Color Lilac = new Color(0.66f, 0.52f, 0.95f);
    static readonly Color Orange = new Color(1f, 0.6f, 0.28f);

    GameManager gm;
    Font font;
    RectTransform canvasRect, dim, desk, mainPanel, levelsPanel, pausePanel, completePanel;
    RectTransform pageTurn;
    Image flash;
    Sprite rounded, circle, star, triangle, smiley;
    bool levelsFromPause;
    AudioClip clickClip, turnClip;

    public bool Transitioning { get; private set; }
    public bool PauseOpen => pausePanel.gameObject.activeSelf || (levelsPanel.gameObject.activeSelf && levelsFromPause);
    public bool AnyOpen => mainPanel.gameObject.activeSelf || levelsPanel.gameObject.activeSelf || pausePanel.gameObject.activeSelf || completePanel.gameObject.activeSelf || pencilPanel.gameObject.activeSelf || confirmPanel.gameObject.activeSelf;

    public void Build(GameManager manager)
    {
        gm = manager;
        font = ArabicText.Font;
        rounded = RoundedSprite();
        circle = CircleSprite();
        star = StarSprite();
        triangle = TriangleSprite();
        smiley = SmileySprite();
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

        dim = Panel(canvasRect, "Dim", new Color(0.12f, 0.08f, 0.05f, 0.42f)).rectTransform;
        Stretch(dim);
        dim.GetComponent<Image>().raycastTarget = true;   // يمنع الرسم ورا القوائم
        desk = BuildDesk();

        BuildMain();
        BuildLevels();
        BuildPause();
        BuildComplete();
        BuildPencil();
        BuildConfirm();

        flash = Panel(canvasRect, "Flash", new Color(1f, 1f, 1f, 0f));
        Stretch(flash.rectTransform);
        pageTurn = BuildPageTurn();
        HideAll();
    }

    // ---------- فتح وسكر ----------

    public void HideAll()
    {
        dim.gameObject.SetActive(false);
        desk.gameObject.SetActive(false);
        mainPanel.gameObject.SetActive(false);
        levelsPanel.gameObject.SetActive(false);
        pausePanel.gameObject.SetActive(false);
        completePanel.gameObject.SetActive(false);
        if (pencilPanel != null) pencilPanel.gameObject.SetActive(false);
        if (confirmPanel != null) confirmPanel.gameObject.SetActive(false);
    }

    // onDesk: خلفية الدفتر الكريمية (للقوائم برا اللعب)، وإلا تظلل اللعبة ورا
    void Open(RectTransform panel, bool onDesk = false)
    {
        HideAll();
        dim.gameObject.SetActive(!onDesk);
        desk.gameObject.SetActive(onDesk);
        panel.gameObject.SetActive(true);
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

    public void ShowLevels(bool fromPause)
    {
        levelsFromPause = fromPause;
        selected = Mathf.Min(SaveData.Unlocked, Levels.All.Count - 1);
        RefreshLevels();
        ShowPart(selected / PerPart);
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

    RectTransform BuildPageTurn()
    {
        var page = Panel(canvasRect, "Page Turn", Cream).rectTransform;
        Place(page, Vector2.zero, new Vector2(2400f, 1500f));
        Lines(page, new Vector2(2400f, 1500f), 54f);
        var title = Label(page, ArabicText.Fix("قلم رويدا"), 90, Vector2.zero, new Vector2(1000f, 140f));
        title.color = new Color(Navy.r, Navy.g, Navy.b, 0.25f);
        page.gameObject.SetActive(false);
        return page;
    }

    // خلفية القوائم: سما فاتحة بتدرج، غيوم تمشي، شمس بأشعة تلف، شريط أعلام ملونة يتمايل، وقصاصات ملونة تطيح
    readonly List<RectTransform> doodles = new List<RectTransform>();
    readonly List<RectTransform> flags = new List<RectTransform>();
    readonly List<RectTransform> clouds = new List<RectTransform>();
    readonly List<RectTransform> confetti = new List<RectTransform>();
    readonly List<Vector3> confettiVel = new List<Vector3>();
    RectTransform sun, sunRays;

    RectTransform BuildDesk()
    {
        var d = new GameObject("Desk", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        d.transform.SetParent(canvasRect, false);
        Stretch(d.rectTransform);
        var grad = new Texture2D(1, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        grad.SetPixels(new[] { new Color(0.86f, 0.97f, 0.9f), new Color(0.8f, 0.94f, 1f), new Color(0.66f, 0.87f, 1f), new Color(0.55f, 0.8f, 1f) });
        grad.Apply();
        d.texture = grad;
        d.raycastTarget = true;
        RectTransform root = d.rectTransform;

        // الشمس وأشعتها
        sunRays = new GameObject("Sun Rays", typeof(RectTransform)).GetComponent<RectTransform>();
        sunRays.SetParent(root, false);
        sunRays.anchorMin = sunRays.anchorMax = new Vector2(0f, 1f);
        sunRays.anchoredPosition = new Vector2(150f, -150f);
        for (int i = 0; i < 12; i++)
        {
            var ray = Panel(sunRays, "Ray", new Color(1f, 0.85f, 0.35f, 0.55f));
            ray.sprite = rounded;
            ray.type = Image.Type.Sliced;
            ray.pixelsPerUnitMultiplier = 6f;
            ray.rectTransform.pivot = new Vector2(0.5f, 0f);
            ray.rectTransform.anchoredPosition = Vector2.zero;
            ray.rectTransform.sizeDelta = new Vector2(16f, 150f);
            ray.rectTransform.localRotation = Quaternion.Euler(0f, 0f, i * 30f);
        }
        var s = Panel(root, "Sun", Yellow);
        s.sprite = circle;
        s.rectTransform.anchorMin = s.rectTransform.anchorMax = new Vector2(0f, 1f);
        s.rectTransform.anchoredPosition = new Vector2(150f, -150f);
        s.rectTransform.sizeDelta = new Vector2(150f, 150f);
        sun = s.rectTransform;

        // غيوم
        var rng = new System.Random(3);
        for (int i = 0; i < 5; i++)
        {
            var c = new GameObject("Cloud", typeof(RectTransform)).GetComponent<RectTransform>();
            c.SetParent(root, false);
            c.anchorMin = c.anchorMax = new Vector2((float)rng.NextDouble(), 0.62f + (float)rng.NextDouble() * 0.25f);
            float k = 0.7f + (float)rng.NextDouble() * 0.6f;
            float[,] puffs = { { -70f, 0f, 110f }, { 0f, 25f, 140f }, { 75f, 0f, 105f }, { 0f, -20f, 120f } };
            for (int p = 0; p < 4; p++)
            {
                var puff = Panel(c, "Puff", new Color(1f, 1f, 1f, 0.85f));
                puff.sprite = circle;
                Place(puff.rectTransform, new Vector2(puffs[p, 0], puffs[p, 1]) * k, Vector2.one * puffs[p, 2] * k);
            }
            clouds.Add(c);
        }

        // شريط أعلام ملونة فوق (قوس)
        Color[] flagColors = { Pink, Yellow, MintC, SkyBlue, Lilac, Orange };
        int nFlags = 22;
        for (int i = 0; i < nFlags; i++)
        {
            float u = i / (float)(nFlags - 1);
            float x = Mathf.Lerp(-1100f, 1100f, u);
            float y = -30f - Mathf.Sin(u * Mathf.PI) * 70f;
            var f = Panel(root, "Flag", flagColors[i % flagColors.Length]);
            f.sprite = triangle;
            f.rectTransform.anchorMin = f.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            f.rectTransform.pivot = new Vector2(0.5f, 1f);
            f.rectTransform.anchoredPosition = new Vector2(x, y);
            f.rectTransform.sizeDelta = new Vector2(64f, 76f);
            f.rectTransform.localScale = new Vector3(1f, -1f, 1f);   // رأس المثلث تحت
            flags.Add(f.rectTransform);
            if (i < nFlags - 1)
            {
                float u2 = (i + 1) / (float)(nFlags - 1);
                Vector2 a = new Vector2(x, y), b = new Vector2(Mathf.Lerp(-1100f, 1100f, u2), -30f - Mathf.Sin(u2 * Mathf.PI) * 70f);
                var str = Panel(root, "String", new Color(0.45f, 0.4f, 0.5f));
                str.rectTransform.anchorMin = str.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                str.rectTransform.anchoredPosition = (a + b) * 0.5f;
                str.rectTransform.sizeDelta = new Vector2(Vector2.Distance(a, b) + 2f, 3f);
                str.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
                str.transform.SetSiblingIndex(f.transform.GetSiblingIndex());
            }
        }

        // نجوم ودوائر صغيرة على الأطراف
        Color[] cols = { Yellow, PinkSoft, MintC, SkyBlue };
        for (int i = 0; i < 12; i++)
        {
            var g = Panel(root, "Doodle", cols[i % cols.Length] * new Color(1f, 1f, 1f, 0.8f));
            g.sprite = i % 2 == 0 ? star : circle;
            float size = i % 2 == 0 ? 44f + rng.Next(30) : 18f + rng.Next(14);
            var rt = g.rectTransform;
            float x = (float)rng.NextDouble(), y = 0.1f + (float)rng.NextDouble() * 0.7f;
            if (x > 0.22f && x < 0.78f) x = x < 0.5f ? x * 0.35f : 1f - (1f - x) * 0.35f;
            rt.anchorMin = rt.anchorMax = new Vector2(x, y);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(size, size);
            doodles.Add(rt);
        }

        // قصاصات ملونة تطيح
        for (int i = 0; i < 40; i++)
        {
            var c = Panel(root, "Confetti", flagColors[i % flagColors.Length]);
            var rt = c.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = i % 3 == 0 ? new Vector2(14f, 14f) : new Vector2(10f, 22f);
            if (i % 3 == 0) c.sprite = circle;
            rt.anchoredPosition = new Vector2(rng.Next(-1100, 1100), rng.Next(-600, 600));
            confetti.Add(rt);
            confettiVel.Add(new Vector3(rng.Next(-30, 30), -60f - rng.Next(80), rng.Next(-200, 200)));
        }
        return root;
    }

    // ---------- القائمة الرئيسية ----------

    Text playLabel, starsTotal;
    RectTransform titleRect;

    void BuildMain()
    {
        mainPanel = NotebookCard(canvasRect, "Main Menu", new Vector2(860f, 960f), -1.2f);
        var title = Label(mainPanel, ArabicText.Fix("قلم رويدا"), 120, new Vector2(0f, 305f), new Vector2(780f, 160f));
        title.color = Pink;
        var ol = title.gameObject.AddComponent<Outline>();
        ol.effectColor = Color.white;
        ol.effectDistance = new Vector2(5f, -5f);
        var tsh = title.gameObject.AddComponent<Shadow>();
        tsh.effectColor = new Color(0.86f, 0.33f, 0.45f, 0.6f);
        tsh.effectDistance = new Vector2(4f, -9f);
        titleRect = title.rectTransform;
        var pen = MiniPencil(mainPanel, new Vector2(-290f, 340f));
        pen.localScale = Vector3.one * 2.6f;
        Label(mainPanel, ArabicText.Fix("ارسمي طريق رويدا لين ترجع لغرفتها"), 32, new Vector2(0f, 190f), new Vector2(760f, 50f)).color = Grey;

        playLabel = PrimaryButton(mainPanel, "", new Vector2(0f, 75f), new Vector2(560f, 120f), () => gm.PlayFromMenu(), 50);
        SecondaryButton(mainPanel, "خريطة المراحل", new Vector2(0f, -55f), new Vector2(560f, 100f), () => ShowLevels(false), 42, SkyBlue);
        SecondaryButton(mainPanel, "صندوق الأقلام", new Vector2(140f, -170f), new Vector2(280f, 96f), ShowPencil, 36, Lilac);
        SecondaryButton(mainPanel, "من جديد", new Vector2(-150f, -170f), new Vector2(260f, 96f), ShowConfirm, 36, Orange);
        SecondaryButton(mainPanel, "اطلعي من اللعبة", new Vector2(0f, -285f), new Vector2(560f, 90f), Application.Quit, 34, MintC);

        // عداد النجوم بشريط أصفر صغير
        var pill = Framed(mainPanel, "Stars Pill", new Vector2(0f, -385f), new Vector2(300f, 70f), new Color(1f, 0.93f, 0.7f), YellowDark, 4f, 1.6f);
        var starIcon = Panel(pill, "Star", Gold);
        starIcon.sprite = star;
        Place(starIcon.rectTransform, new Vector2(95f, 2f), new Vector2(50f, 50f));
        starsTotal = Label(pill, "", 34, new Vector2(-30f, 0f), new Vector2(220f, 60f));
    }

    void RefreshMain()
    {
        playLabel.text = ArabicText.Fix(SaveData.HasProgress ? "كمّلي — المرحلة " + (SaveData.Unlocked + 1) : "ابدئي الحكاية!");
        starsTotal.text = SaveData.TotalStars + " / " + Levels.All.Count * SaveData.MaxStars;
    }

    // ---------- خريطة المراحل ----------

    const int PerPart = 20;
    static readonly string[] PartNames = { "الجزء الأول", "الجزء الثاني", "الجزء الثالث", "الجزء الرابع" };
    readonly List<RectTransform> partPages = new List<RectTransform>();
    readonly List<Image> nodeRings = new List<Image>();
    readonly List<Image> nodeFills = new List<Image>();
    readonly List<Text> nodeNumbers = new List<Text>();
    readonly List<Image[]> nodeStars = new List<Image[]>();
    readonly List<Image> nodeStickers = new List<Image>();
    Text levelsTitle, infoTitle, infoHint, startLabel;
    RawImage infoThumb;
    Image[] infoStars;
    Text infoLocked;
    GameObject nextPart, prevPart, startButton;
    int part, selected;

    void ShowPart(int p)
    {
        part = Mathf.Clamp(p, 0, partPages.Count - 1);
        for (int i = 0; i < partPages.Count; i++) partPages[i].gameObject.SetActive(i == part);
        levelsTitle.text = ArabicText.Fix("خريطة الدفتر — " + PartNames[Mathf.Min(part, PartNames.Length - 1)]);
        nextPart.SetActive(part < partPages.Count - 1);
        prevPart.SetActive(part > 0);
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
        float wobble = (k % 2 == 0 ? 1f : -1f) * 14f;
        return new Vector2(690f - col * 175f, 235f - row * 165f + wobble);
    }

    void BuildLevels()
    {
        levelsPanel = NotebookCard(canvasRect, "Levels", new Vector2(1640f, 1000f), 0.4f);
        levelsTitle = Label(levelsPanel, "", 58, new Vector2(240f, 405f), new Vector2(1000f, 90f));
        Label(levelsPanel, ArabicText.Fix("اختاري الصفحة الجاية!"), 30, new Vector2(240f, 345f), new Vector2(800f, 44f)).color = Grey;

        // كل جزء (20 مرحلة) بخريطة لحاله
        int partCount = (Levels.All.Count + PerPart - 1) / PerPart;
        for (int p = 0; p < partCount; p++)
        {
            var page = new GameObject("Part " + (p + 1), typeof(RectTransform)).GetComponent<RectTransform>();
            page.SetParent(levelsPanel, false);
            Stretch(page);
            partPages.Add(page);
            // الطريق المنقط بين الدواير
            int count = Mathf.Min(PerPart, Levels.All.Count - p * PerPart);
            for (int k = 0; k < count - 1; k++)
            {
                Vector2 a = NodePos(k), b = NodePos(k + 1);
                Vector2 dir = (b - a).normalized, side = new Vector2(-dir.y, dir.x);
                int n = Mathf.RoundToInt(Vector2.Distance(a, b) / 24f);
                for (int j = 2; j < n - 1; j++)
                {
                    float u = j / (float)n;
                    var dot = Panel(page, "Dot", Dots);
                    dot.sprite = circle;
                    Place(dot.rectTransform, Vector2.Lerp(a, b, u) + side * Mathf.Sin(u * Mathf.PI) * 22f * (k % 2 == 0 ? 1f : -1f), new Vector2(12f, 12f));
                }
            }
        }

        for (int i = 0; i < Levels.All.Count; i++)
        {
            int index = i;
            RectTransform page = partPages[i / PerPart];
            Vector2 pos = NodePos(i % PerPart);
            var ring = Panel(page, "Level " + (i + 1), Locked);
            ring.sprite = circle;
            ring.raycastTarget = true;
            Place(ring.rectTransform, pos, new Vector2(98f, 98f));
            var fill = Panel(ring.rectTransform, "Fill", Locked);
            fill.sprite = circle;
            Place(fill.rectTransform, Vector2.zero, new Vector2(84f, 84f));
            var button = ring.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => Select(index));
            ring.gameObject.AddComponent<ButtonJuice>().Init(gm, clickClip);
            nodeRings.Add(ring);
            nodeFills.Add(fill);
            nodeNumbers.Add(Label(ring.rectTransform, ArabicDigits(i + 1), 40, new Vector2(0f, 2f), new Vector2(98f, 98f)));
            var stars = new Image[SaveData.MaxStars];
            for (int s = 0; s < stars.Length; s++)
            {
                stars[s] = Panel(ring.rectTransform, "Star", StarOff);
                stars[s].sprite = star;
                Place(stars[s].rectTransform, new Vector2((s - 1) * 26f, -60f + (s == 1 ? -4f : 0f)), new Vector2(24f, 24f));
            }
            nodeStars.Add(stars);
            // ملصق صغير على الدايرة إذا لقيتيه
            var badge = Panel(ring.rectTransform, "Sticker", Pink);
            badge.sprite = circle;
            Place(badge.rectTransform, new Vector2(38f, 38f), new Vector2(36f, 36f));
            var bstar = Panel(badge.rectTransform, "Star", Yellow);
            bstar.sprite = star;
            Place(bstar.rectTransform, Vector2.zero, new Vector2(26f, 26f));
            nodeStickers.Add(badge);
        }

        // بطاقة المرحلة المختارة (يسار)
        var info = Framed(levelsPanel, "Info", new Vector2(-540f, 40f), new Vector2(480f, 600f), Mint, MintBorder, 5f, 2.2f);
        var frame = Framed(info, "Thumb Frame", new Vector2(0f, 150f), new Vector2(420f, 240f), Color.white, MintBorder, 4f, 4f);
        infoThumb = new GameObject("Thumbnail", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        infoThumb.transform.SetParent(frame, false);
        infoThumb.raycastTarget = false;
        Place(infoThumb.rectTransform, Vector2.zero, new Vector2(406f, 226f));
        infoLocked = Label(frame, ArabicText.Fix("مقفلة"), 44, Vector2.zero, new Vector2(400f, 220f));
        infoTitle = Label(info, "", 38, new Vector2(0f, -15f), new Vector2(460f, 60f));
        infoStars = new Image[SaveData.MaxStars];
        for (int s = 0; s < infoStars.Length; s++)
        {
            infoStars[s] = Panel(info, "Star", StarOff);
            infoStars[s].sprite = star;
            Place(infoStars[s].rectTransform, new Vector2((1 - s) * 62f, -82f), new Vector2(54f, 54f));
        }
        infoHint = Label(info, "", 26, new Vector2(0f, -190f), new Vector2(420f, 150f));
        infoHint.horizontalOverflow = HorizontalWrapMode.Overflow;
        infoHint.color = Grey;
        infoHint.fontStyle = FontStyle.Normal;

        startLabel = PrimaryButton(levelsPanel, "ابدئي اللعب!", new Vector2(-540f, -350f), new Vector2(420f, 104f), () =>
        {
            if (selected <= SaveData.Unlocked) gm.StartLevelFromMenu(selected);
        }, 44);
        startButton = startLabel.transform.parent.gameObject;

        // أسهم بين الأجزاء: اليسار للجزء اللي بعده (مثل قلب صفحات كتاب عربي)
        nextPart = RoundArrow(levelsPanel, "<", new Vector2(-260f, 405f), () => ShowPart(part + 1));
        prevPart = RoundArrow(levelsPanel, ">", new Vector2(740f, 405f), () => ShowPart(part - 1));

        SecondaryButton(levelsPanel, "رجوع", new Vector2(240f, -415f), new Vector2(280f, 80f), () =>
        {
            if (levelsFromPause) ShowPause();
            else gm.BackToMain();
        }, 36);
    }

    void Select(int index)
    {
        selected = index;
        RefreshLevels();
        if (index <= SaveData.Unlocked) gm.Sound.Play(SynthAudio.Ding(1.2f), 0.3f);
    }

    void RefreshLevels()
    {
        for (int i = 0; i < nodeRings.Count; i++)
        {
            bool open = i <= SaveData.Unlocked;
            int got = SaveData.Stars(i);
            bool current = i == SaveData.Unlocked;
            bool chosen = i == selected;
            Color fill = !open ? Locked : current ? Pink : Yellow;
            Color ring = !open ? LockedDark : current ? PinkDark : YellowDark;
            nodeFills[i].color = fill;
            nodeRings[i].color = chosen ? Navy : ring;
            float size = chosen ? 118f : current ? 108f : 98f;
            nodeRings[i].rectTransform.sizeDelta = new Vector2(size, size);
            nodeFills[i].rectTransform.sizeDelta = new Vector2(size - (chosen ? 18f : 14f), size - (chosen ? 18f : 14f));
            nodeNumbers[i].color = open ? (current ? Color.white : Navy) : new Color(0.5f, 0.5f, 0.52f);
            nodeNumbers[i].rectTransform.sizeDelta = new Vector2(size, size);
            nodeStickers[i].gameObject.SetActive(open && SaveData.HasSticker(i));
            nodeStickers[i].rectTransform.anchoredPosition = new Vector2(size * 0.38f, size * 0.38f);
            for (int s = 0; s < SaveData.MaxStars; s++)
            {
                nodeStars[i][s].enabled = open && got > 0;
                nodeStars[i][s].color = s < got ? Gold : StarOff;
                nodeStars[i][s].rectTransform.anchoredPosition = new Vector2((s - 1) * 26f, -size * 0.5f - 12f + (s == 1 ? -4f : 0f));
            }
        }

        // بطاقة المعلومات
        int sel = Mathf.Clamp(selected, 0, Levels.All.Count - 1);
        bool selOpen = sel <= SaveData.Unlocked;
        LevelData d = Levels.All[sel];
        infoThumb.texture = Thumbnail(sel);
        infoThumb.color = selOpen ? Color.white : new Color(0.35f, 0.34f, 0.38f);
        infoLocked.enabled = !selOpen;
        infoLocked.color = Color.white;
        infoTitle.text = ArabicText.Fix("المرحلة " + (sel + 1) + ": " + d.title);
        int st = SaveData.Stars(sel);
        for (int s = 0; s < infoStars.Length; s++) infoStars[s].color = s < st ? Gold : StarOff;
        string hint = selOpen ? d.subtitle : "خلصي المراحل اللي قبلها عشان تنفتح";
        if (selOpen && d.sticker.HasValue) hint += SaveData.HasSticker(sel) ? " — لقيتي الملصق!" : " — فيها ملصق مخفي";
        infoHint.text = WrapFix(hint, 30);
        startButton.GetComponent<Image>().color = selOpen ? PinkDark : LockedDark;
        startButton.transform.GetChild(0).GetComponent<Image>().color = selOpen ? Pink : Locked;
    }

    // ---------- من جديد: نتأكد قبل ما نمسح التقدم ----------

    RectTransform confirmPanel;

    public void ShowConfirm() => Open(confirmPanel, true);

    void BuildConfirm()
    {
        confirmPanel = NotebookCard(canvasRect, "Confirm Restart", new Vector2(820f, 600f), 1f);
        Label(confirmPanel, ArabicText.Fix("تبين تبدين من جديد؟"), 60, new Vector2(0f, 160f), new Vector2(760f, 90f));
        var warn = Label(confirmPanel, ArabicText.Fix("بتنمسح كل المراحل اللي فتحتيها والنجوم"), 32, new Vector2(0f, 75f), new Vector2(760f, 50f));
        warn.color = PinkDark;
        // من اليمين: "إيه" وبعدها "لا"
        PrimaryButton(confirmPanel, "إيه، من جديد", new Vector2(160f, -90f), new Vector2(300f, 100f), () =>
        {
            SaveData.ResetProgress();
            DrawingMemory.Clear();
            gm.PlayFromMenu();
        }, 36);
        SecondaryButton(confirmPanel, "لا", new Vector2(-170f, -90f), new Vector2(240f, 100f), ShowMain, 40, MintC);
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
        pencilPanel = NotebookCard(canvasRect, "Pencil", new Vector2(1160f, 960f), -0.8f);
        Label(pencilPanel, ArabicText.Fix("صندوق الأقلام"), 80, new Vector2(0f, 360f), new Vector2(800f, 110f));
        Label(pencilPanel, ArabicText.Fix("كل قلم له لمسته السحرية"), 30, new Vector2(0f, 290f), new Vector2(800f, 46f)).color = Grey;

        // أقلام ملونة واقفة
        for (int i = 0; i < PencilStyle.Colors.Length; i++)
        {
            int index = i;
            Color c = PencilStyle.Colors[i];
            Color dark = Color.Lerp(c, Color.black, 0.25f);
            var holder = new GameObject("Pencil " + i, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            holder.SetParent(pencilPanel, false);
            holder.GetComponent<Image>().color = Color.clear;
            holder.GetComponent<Image>().raycastTarget = true;
            Place(holder, new Vector2(455f - i * 130f, 100f), new Vector2(110f, 290f));
            var body = Framed(holder, "Body", new Vector2(0f, -30f), new Vector2(92f, 200f), c, dark, 5f, 3f);
            // رأس القلم: خشب ورصاص
            var wood = Panel(holder, "Tip", new Color(0.96f, 0.76f, 0.55f));
            wood.sprite = triangle;
            Place(wood.rectTransform, new Vector2(0f, 104f), new Vector2(80f, 56f));
            var lead = Panel(holder, "Lead", dark);
            lead.sprite = triangle;
            Place(lead.rectTransform, new Vector2(0f, 120f), new Vector2(26f, 20f));
            var band = Panel(body, "Band", new Color(1f, 1f, 1f, 0.35f));
            Place(band.rectTransform, new Vector2(0f, -60f), new Vector2(80f, 14f));
            var btn = holder.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { PencilStyle.ColorIndex = index; RefreshPencil(); });
            holder.gameObject.AddComponent<ButtonJuice>().Init(gm, clickClip);
            pencils.Add(holder);
            pencilNames.Add(Label(pencilPanel, ArabicText.Fix(PencilStyle.ColorNames[i]), 24, new Vector2(455f - i * 130f, -70f), new Vector2(120f, 36f)));
        }

        Label(pencilPanel, ArabicText.Fix("شكل الخط"), 34, new Vector2(0f, -125f), new Vector2(400f, 50f));
        for (int i = 0; i < PencilStyle.StyleNames.Length; i++)
        {
            int index = i;
            SecondaryButton(pencilPanel, PencilStyle.StyleNames[i], new Vector2(400f - i * 200f, -195f), new Vector2(184f, 76f),
                () => { PencilStyle.Current = (PencilStyle.Style)index; RefreshPencil(); }, 30, Color.white);
            styleButtons.Add(pencilPanel.GetChild(pencilPanel.childCount - 1).GetChild(0).GetComponent<Image>());
        }

        previewName = Label(pencilPanel, "", 28, new Vector2(0f, -268f), new Vector2(800f, 44f));
        previewName.color = Grey;
        preview = new GameObject("Preview", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        preview.transform.SetParent(pencilPanel, false);
        preview.raycastTarget = false;
        Place(preview.rectTransform, new Vector2(0f, -320f), new Vector2(640f, 36f));
        previewGlow = preview.gameObject.AddComponent<Outline>();

        PrimaryButton(pencilPanel, "اختاري قلمك!", new Vector2(0f, -405f), new Vector2(380f, 90f), ShowMain, 40);
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
        previewName.text = ArabicText.Fix("خطك: " + PencilStyle.StyleNames[(int)st] + " — " + PencilStyle.ColorNames[c]);
    }

    // ---------- الاستراحة ----------

    RectTransform pauseIcon;

    void BuildPause()
    {
        pausePanel = NotebookCard(canvasRect, "Pause", new Vector2(720f, 900f), 1.2f);
        Label(pausePanel, ArabicText.Fix("نأخذ استراحة؟"), 72, new Vector2(0f, 320f), new Vector2(640f, 100f));
        var icon = Panel(pausePanel, "Pause Icon", Yellow);
        icon.sprite = circle;
        Place(icon.rectTransform, new Vector2(0f, 170f), new Vector2(150f, 150f));
        for (int s = -1; s <= 1; s += 2)
        {
            var bar = Panel(icon.rectTransform, "Bar", new Color(0.55f, 0.35f, 0.1f));
            bar.sprite = rounded;
            bar.type = Image.Type.Sliced;
            bar.pixelsPerUnitMultiplier = 4f;
            Place(bar.rectTransform, new Vector2(s * 22f, 0f), new Vector2(26f, 74f));
        }
        pauseIcon = icon.rectTransform;
        PrimaryButton(pausePanel, "كمّلي اللعب", new Vector2(0f, 20f), new Vector2(460f, 104f), () => gm.Resume(), 44);
        SecondaryButton(pausePanel, "أعيدي المرحلة", new Vector2(0f, -100f), new Vector2(460f, 92f), () => gm.RestartLevel(), 36, MintC);
        SecondaryButton(pausePanel, "خريطة المراحل", new Vector2(0f, -205f), new Vector2(460f, 92f), () => ShowLevels(true), 36, SkyBlue);
        SecondaryButton(pausePanel, "القائمة الرئيسية", new Vector2(0f, -310f), new Vector2(460f, 92f), () => gm.GoToMainMenu(), 36, Lilac);
        Label(pausePanel, ArabicText.Fix("دفترك ينتظرك"), 28, new Vector2(-10f, -395f), new Vector2(400f, 44f)).color = Grey;
        MiniPencil(pausePanel, new Vector2(120f, -395f));
    }

    // قلم صغير مرسوم (بدل الإيموجي)
    RectTransform MiniPencil(RectTransform parent, Vector2 pos)
    {
        var root = new GameObject("Mini Pencil", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false);
        Place(root, pos, new Vector2(60f, 20f));
        root.localRotation = Quaternion.Euler(0f, 0f, 35f);
        var body = Panel(root, "Body", new Color(1f, 0.6f, 0.35f));
        Place(body.rectTransform, new Vector2(6f, 0f), new Vector2(34f, 12f));
        var tip = Panel(root, "Tip", new Color(0.96f, 0.76f, 0.55f));
        tip.sprite = triangle;
        Place(tip.rectTransform, new Vector2(-17f, 0f), new Vector2(12f, 12f));
        tip.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        var er = Panel(root, "Eraser", PinkSoft);
        Place(er.rectTransform, new Vector2(26f, 0f), new Vector2(8f, 12f));
        return root;
    }

    // ---------- شاشة الفوز ----------

    Text completeTitle, completeLevel, nextLabel;
    readonly Image[] bigStars = new Image[SaveData.MaxStars];
    readonly Text[] reasons = new Text[SaveData.MaxStars];
    readonly Image[] reasonStars = new Image[SaveData.MaxStars];
    RectTransform face, decoStarA, decoStarB;
    Action nextAction;

    void BuildComplete()
    {
        completePanel = NotebookCard(canvasRect, "Complete", new Vector2(1000f, 980f), -1f);
        var a = Panel(completePanel, "Deco Star", Yellow);
        a.sprite = star;
        Place(a.rectTransform, new Vector2(-385f, 360f), new Vector2(150f, 150f));
        decoStarA = a.rectTransform;
        var b = Panel(completePanel, "Deco Star", PinkSoft);
        b.sprite = star;
        Place(b.rectTransform, new Vector2(390f, 190f), new Vector2(100f, 100f));
        decoStarB = b.rectTransform;

        completeTitle = Label(completePanel, "", 80, new Vector2(0f, 330f), new Vector2(900f, 110f));
        completeTitle.color = Pink;
        completeLevel = Label(completePanel, "", 34, new Vector2(0f, 250f), new Vector2(900f, 54f));
        completeLevel.color = Grey;

        var f = Panel(completePanel, "Smiley", Color.white);
        f.sprite = smiley;
        Place(f.rectTransform, new Vector2(0f, 90f), new Vector2(230f, 230f));
        face = f.rectTransform;

        for (int i = 0; i < SaveData.MaxStars; i++)
        {
            // نجوم الفوز تحت الوجه (اللي بالنص أكبر شوي)
            bigStars[i] = Panel(completePanel, "Big Star", StarOff);
            bigStars[i].sprite = star;
            float size = i == 1 ? 100f : 82f;
            Place(bigStars[i].rectTransform, new Vector2((1 - i) * 105f, -75f + (i == 1 ? 6f : 0f)), new Vector2(size, size));

            reasonStars[i] = Panel(completePanel, "Reason Star", StarOff);
            reasonStars[i].sprite = star;
            Place(reasonStars[i].rectTransform, new Vector2(170f, -160f - i * 46f), new Vector2(36f, 36f));
            reasons[i] = Label(completePanel, "", 28, new Vector2(-30f, -160f - i * 46f), new Vector2(360f, 44f));
            reasons[i].alignment = TextAnchor.MiddleRight;
        }
        nextLabel = PrimaryButton(completePanel, "", new Vector2(0f, -330f), new Vector2(520f, 104f), () => nextAction?.Invoke(), 42);
        SecondaryButton(completePanel, "أعيدي", new Vector2(130f, -428f), new Vector2(230f, 72f), () => gm.RestartLevel(), 32, MintC);
        SecondaryButton(completePanel, "خريطة المراحل", new Vector2(-130f, -428f), new Vector2(250f, 72f), () => ShowLevels(false), 30, SkyBlue);
    }

    public void ShowComplete(int index, bool[] earned, bool last, Action next)
    {
        nextAction = next;
        completeTitle.text = ArabicText.Fix(last ? "خلصتي كل المراحل!" : "أحسنتِ يا رويدا!");
        completeLevel.text = ArabicText.Fix("المرحلة " + (index + 1) + " — " + Levels.All[index].title);
        nextLabel.text = ArabicText.Fix(last ? "القائمة الرئيسية" : "للصفحة التالية  <");
        string[] texts = { "خلصتي المرحلة", "ما خسرتي ولا قلب", "وفّرتي حبر" };
        for (int i = 0; i < SaveData.MaxStars; i++)
        {
            reasons[i].text = ArabicText.Fix(texts[i]);
            reasons[i].color = earned[i] ? Navy : new Color(0.62f, 0.6f, 0.64f);
            reasonStars[i].color = earned[i] ? Gold : StarOff;
            bigStars[i].color = StarOff;
            bigStars[i].rectTransform.localScale = Vector3.one;
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
            s.color = Gold;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
            {
                float scale = t < 0.6f ? Mathf.Lerp(0f, 1.3f, t / 0.6f) : Mathf.Lerp(1.3f, 1f, (t - 0.6f) / 0.4f);
                s.rectTransform.localScale = Vector3.one * scale;
                s.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (1f - t) * 40f);
                yield return null;
            }
            s.rectTransform.localScale = Vector3.one;
            s.rectTransform.localRotation = Quaternion.identity;
        }
    }

    void Update()
    {
        if (mainPanel == null || completePanel == null) return;   // القوائم انحذفت (وقفنا اللعب)
        float t = Time.unscaledTime;
        // العنوان يتمايل بهدوء
        if (mainPanel.gameObject.activeSelf)
        {
            titleRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.3f) * 2f);
            titleRect.localScale = Vector3.one * (1f + Mathf.Sin(t * 2.1f) * 0.02f);
        }
        // الشمس والنجوم الصغيرة بالخلفية
        if (desk.gameObject.activeSelf)
        {
            float dt = Time.unscaledDeltaTime;
            sun.localScale = Vector3.one * (1f + Mathf.Sin(t * 1.5f) * 0.05f);
            sunRays.localRotation = Quaternion.Euler(0f, 0f, t * 12f);
            sunRays.localScale = Vector3.one * (1f + Mathf.Sin(t * 2f) * 0.08f);
            for (int i = 0; i < doodles.Count; i++) doodles[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.8f + i) * 12f);
            for (int i = 0; i < flags.Count; i++) flags[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.2f + i * 0.7f) * 8f);
            float halfW = canvasRect.rect.width * 0.5f + 200f;
            for (int i = 0; i < clouds.Count; i++)
            {
                Vector2 p = clouds[i].anchoredPosition;
                p.x += (12f + i * 4f) * dt;
                float ax = clouds[i].anchorMin.x * canvasRect.rect.width;
                if (ax + p.x > canvasRect.rect.width + 200f) p.x -= canvasRect.rect.width + 400f;
                clouds[i].anchoredPosition = p;
            }
            float halfH = canvasRect.rect.height * 0.5f + 40f;
            for (int i = 0; i < confetti.Count; i++)
            {
                Vector3 v = confettiVel[i];
                Vector2 p = confetti[i].anchoredPosition;
                p += new Vector2(v.x + Mathf.Sin(t * 2f + i) * 30f, v.y) * dt;
                if (p.y < -halfH) p = new Vector2(UnityEngine.Random.Range(-halfW, halfW), halfH);
                confetti[i].anchoredPosition = p;
                confetti[i].localRotation = Quaternion.Euler(0f, 0f, t * v.z);
            }
        }
        // المرحلة الحالية بالخريطة تنبض
        if (levelsPanel.gameObject.activeSelf && SaveData.Unlocked < nodeRings.Count)
            nodeFills[SaveData.Unlocked].transform.localScale = Vector3.one * (1f + Mathf.Sin(t * 4f) * 0.06f);
        if (pausePanel.gameObject.activeSelf)
            pauseIcon.localScale = Vector3.one * (1f + Mathf.Sin(t * 2.5f) * 0.04f);
        // شاشة الفوز: الوجه ينط، والنجوم تلمع وتلف
        if (completePanel.gameObject.activeSelf)
        {
            face.anchoredPosition = new Vector2(0f, 90f + Mathf.Abs(Mathf.Sin(t * 3f)) * 12f);
            face.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.7f) * 5f);
            decoStarA.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.1f) * 12f);
            decoStarB.localRotation = Quaternion.Euler(0f, 0f, t * 30f);
            for (int i = 0; i < SaveData.MaxStars; i++)
                if (bigStars[i].color == Gold && bigStars[i].rectTransform.localScale == Vector3.one)
                    bigStars[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2f + i) * 6f);
        }
    }

    // ---------- أدوات البناء ----------

    // صفحة دفتر: ورق أبيض بخطوط زرقا وهامش أحمر، سلك حلزوني فوق، وظل ناعم، ومايلة شوي كأنها على مكتب
    RectTransform NotebookCard(Transform parent, string name, Vector2 size, float tilt = 0f)
    {
        var root = Panel(parent, name, Color.clear);
        root.raycastTarget = true;
        RectTransform rt = root.rectTransform;
        Place(rt, Vector2.zero, size);
        rt.localRotation = Quaternion.Euler(0f, 0f, tilt);
        root.gameObject.AddComponent<CanvasGroup>();

        var shadow = Panel(rt, "Shadow", new Color(0.1f, 0.2f, 0.35f, 0.18f));
        shadow.sprite = rounded;
        shadow.type = Image.Type.Sliced;
        shadow.pixelsPerUnitMultiplier = 2.5f;
        Place(shadow.rectTransform, new Vector2(12f, -16f), size);

        var paper = Panel(rt, "Paper", PaperWhite);
        paper.sprite = rounded;
        paper.type = Image.Type.Sliced;
        paper.pixelsPerUnitMultiplier = 2.5f;
        Stretch(paper.rectTransform);
        Lines(paper.rectTransform, size - new Vector2(30f, 140f), 46f);
        var margin = Panel(paper.rectTransform, "Margin", new Color(1f, 0.45f, 0.5f, 0.5f));
        Place(margin.rectTransform, new Vector2(size.x * 0.5f - 90f, -20f), new Vector2(4f, size.y - 90f));

        // السلك الحلزوني
        for (float x = -size.x * 0.5f + 60f; x < size.x * 0.5f - 40f; x += 54f)
        {
            var hole = Panel(rt, "Hole", new Color(0.82f, 0.86f, 0.92f));
            hole.sprite = circle;
            Place(hole.rectTransform, new Vector2(x, size.y * 0.5f - 30f), new Vector2(20f, 20f));
            var ring = Panel(rt, "Ring", new Color(0.55f, 0.6f, 0.68f));
            ring.sprite = rounded;
            ring.type = Image.Type.Sliced;
            ring.pixelsPerUnitMultiplier = 8f;
            Place(ring.rectTransform, new Vector2(x + 3f, size.y * 0.5f - 6f), new Vector2(12f, 54f));
        }
        return rt;
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
        return MakeButton(parent, label, pos, size, Pink, onClick, fontSize);
    }

    // زر ملون (كل زر لونه): أزرق سما إذا ما انعطى لون
    Text SecondaryButton(RectTransform parent, string label, Vector2 pos, Vector2 size, Action onClick, int fontSize, Color? color = null)
    {
        return MakeButton(parent, label, pos, size, color ?? SkyBlue, onClick, fontSize);
    }

    // زر مثل حبة حلاوة: لون فاتح، حافة أغمق تحت (كأنه منفوخ)، ولمعة فوق
    Text MakeButton(RectTransform parent, string label, Vector2 pos, Vector2 size, Color fill, Action onClick, int fontSize)
    {
        bool white = fill.r > 0.95f && fill.g > 0.95f && fill.b > 0.95f;
        Color under = white ? new Color(0.78f, 0.82f, 0.88f) : Color.Lerp(fill, Color.black, 0.22f);
        var outer = Panel(parent, "Button " + label, under);
        outer.sprite = rounded;
        outer.type = Image.Type.Sliced;
        outer.pixelsPerUnitMultiplier = 0.5f;   // زوايا مدورة كثير
        outer.raycastTarget = true;
        Place(outer.rectTransform, pos, size);
        var inner = Panel(outer.rectTransform, "Fill", fill);
        inner.sprite = rounded;
        inner.type = Image.Type.Sliced;
        inner.pixelsPerUnitMultiplier = 0.5f;
        Stretch(inner.rectTransform);
        inner.rectTransform.offsetMin = new Vector2(0f, 9f);
        var shine = Panel(inner.rectTransform, "Shine", new Color(1f, 1f, 1f, 0.28f));
        shine.sprite = rounded;
        shine.type = Image.Type.Sliced;
        shine.pixelsPerUnitMultiplier = 1.2f;
        shine.rectTransform.anchorMin = new Vector2(0f, 0.8f);
        shine.rectTransform.anchorMax = new Vector2(1f, 1f);
        shine.rectTransform.offsetMin = new Vector2(size.y * 0.35f, 0f);
        shine.rectTransform.offsetMax = new Vector2(-size.y * 0.35f, -5f);
        var button = outer.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => onClick());
        outer.gameObject.AddComponent<ButtonJuice>().Init(gm, clickClip);
        Text t = Label(outer.rectTransform, ArabicText.Fix(label), fontSize, new Vector2(0f, 3f), size);
        t.color = white ? Navy : Color.white;
        if (!white)
        {
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(under.r * 0.7f, under.g * 0.7f, under.b * 0.7f, 0.8f);
            sh.effectDistance = new Vector2(0f, -3f);
        }
        return t;
    }

    GameObject RoundArrow(RectTransform parent, string label, Vector2 pos, Action onClick)
    {
        var outer = Panel(parent, "Arrow " + label, PinkDark);
        outer.sprite = circle;
        outer.raycastTarget = true;
        Place(outer.rectTransform, pos, new Vector2(78f, 78f));
        var inner = Panel(outer.rectTransform, "Fill", Pink);
        inner.sprite = circle;
        Place(inner.rectTransform, Vector2.zero, new Vector2(66f, 66f));
        var button = outer.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => onClick());
        outer.gameObject.AddComponent<ButtonJuice>().Init(gm, clickClip);
        Label(outer.rectTransform, label, 48, new Vector2(0f, 3f), new Vector2(78f, 78f)).color = Color.white;
        return outer.gameObject;
    }

    Text Label(RectTransform parent, string text, int size, Vector2 pos, Vector2 box)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Navy;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        Place(t.rectTransform, pos, box);
        return t;
    }

    // نقسم النص العربي لسطور (من البداية)، وبعدين نقلب كل سطر لحاله عشان ما ينعكس ترتيب السطور
    static string WrapFix(string text, int maxChars)
    {
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
    static Sprite RoundedSprite()
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

    static Sprite CircleSprite()
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

    // مثلث رأسه فوق (رأس القلم)
    static Sprite TriangleSprite()
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
