using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// القوائم: القائمة الرئيسية، اختيار المراحل، الاستراحة، شاشة الفوز بالنجوم، والانتقال بين المراحل (صفحة تنقلب)
// كل القوائم شكلها صفحات دفتر: ورق، خطوط زرقا، وهامش أحمر
public class MenuUI : MonoBehaviour
{
    static readonly Color Paper = new Color(0.99f, 0.97f, 0.92f);
    static readonly Color Ink = new Color(0.16f, 0.15f, 0.2f);
    static readonly Color LineBlue = new Color(0.55f, 0.7f, 0.95f, 0.45f);
    static readonly Color Margin = new Color(0.95f, 0.45f, 0.45f, 0.6f);
    static readonly Color Gold = new Color(1f, 0.78f, 0.22f);
    static readonly Color StarOff = new Color(0.78f, 0.76f, 0.74f);
    static readonly Color Primary = new Color(1f, 0.82f, 0.4f);
    static readonly Color Secondary = new Color(0.93f, 0.92f, 0.9f);

    GameManager gm;
    Font font;
    RectTransform canvasRect, dim, mainPanel, levelsPanel, pausePanel, completePanel;
    RectTransform pageTurn;
    Image flash;
    Sprite rounded, star;
    bool levelsFromPause;
    AudioClip clickClip, turnClip;

    public bool Transitioning { get; private set; }
    public bool PauseOpen => pausePanel.gameObject.activeSelf || (levelsPanel.gameObject.activeSelf && levelsFromPause);
    public bool AnyOpen => mainPanel.gameObject.activeSelf || levelsPanel.gameObject.activeSelf || pausePanel.gameObject.activeSelf || completePanel.gameObject.activeSelf;

    public void Build(GameManager manager)
    {
        gm = manager;
        font = ArabicText.Font;
        rounded = RoundedSprite();
        star = StarSprite();
        clickClip = SynthAudio.Click();
        turnClip = SynthAudio.PageTurn();

        // الأزرار تحتاج EventSystem يشتغل مع نظام الإدخال الجديد
        if (FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var canvasGo = new GameObject("Menus", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 0.4f;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect = canvasGo.GetComponent<RectTransform>();

        dim = Panel(canvasRect, "Dim", new Color(0.05f, 0.04f, 0.1f, 0.45f)).rectTransform;
        Stretch(dim);
        dim.GetComponent<Image>().raycastTarget = true;   // يمنع الرسم ورا القوائم

        BuildMain();
        BuildLevels();
        BuildPause();
        BuildComplete();

        flash = Panel(canvasRect, "Flash", new Color(1f, 1f, 1f, 0f));
        Stretch(flash.rectTransform);
        pageTurn = BuildPageTurn();
        HideAll();
    }

    // ---------- فتح وسكر ----------

    public void HideAll()
    {
        dim.gameObject.SetActive(false);
        mainPanel.gameObject.SetActive(false);
        levelsPanel.gameObject.SetActive(false);
        pausePanel.gameObject.SetActive(false);
        completePanel.gameObject.SetActive(false);
    }

    void Open(RectTransform panel)
    {
        HideAll();
        dim.gameObject.SetActive(true);
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
        Open(mainPanel);
    }

    public void ShowLevels(bool fromPause)
    {
        levelsFromPause = fromPause;
        RefreshLevels();
        Open(levelsPanel);
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
        var page = NotebookPage(canvasRect, "Page Turn", new Vector2(2400f, 1500f), 0f);
        page.anchorMin = page.anchorMax = page.pivot = new Vector2(0.5f, 0.5f);
        var title = Label(page, ArabicText.Fix("قلم رويدا"), 90, new Vector2(0f, 0f), new Vector2(1000f, 140f));
        title.color = new Color(Ink.r, Ink.g, Ink.b, 0.25f);
        page.gameObject.SetActive(false);
        return page;
    }

    // ---------- القائمة الرئيسية ----------

    Text playLabel, starsTotal;
    RectTransform titleRect;

    void BuildMain()
    {
        mainPanel = NotebookPage(canvasRect, "Main Menu", new Vector2(820f, 900f), -1.5f);
        titleRect = Label(mainPanel, ArabicText.Fix("قلم رويدا"), 140, new Vector2(0f, 290f), new Vector2(780f, 190f)).rectTransform;
        Label(mainPanel, ArabicText.Fix("ارسمي طريق رويدا لين ترجع لغرفتها"), 38, new Vector2(0f, 175f), new Vector2(760f, 60f)).color = new Color(0.4f, 0.38f, 0.45f);

        playLabel = Button(mainPanel, "", new Vector2(0f, 40f), new Vector2(520f, 120f), Primary, () => gm.PlayFromMenu(), 52);
        Button(mainPanel, "المراحل", new Vector2(0f, -110f), new Vector2(520f, 110f), Secondary, () => ShowLevels(false), 46);
        Button(mainPanel, "اطلعي من اللعبة", new Vector2(0f, -245f), new Vector2(520f, 100f), Secondary, Application.Quit, 40);

        var starIcon = Panel(mainPanel, "Star", Gold);
        starIcon.sprite = star;
        Place(starIcon.rectTransform, new Vector2(70f, -365f), new Vector2(58f, 58f));
        starsTotal = Label(mainPanel, "", 40, new Vector2(-40f, -365f), new Vector2(200f, 60f));
    }

    void RefreshMain()
    {
        playLabel.text = ArabicText.Fix(SaveData.HasProgress ? "كمّلي — المرحلة " + (SaveData.Unlocked + 1) : "ابدئي الحكاية");
        starsTotal.text = SaveData.TotalStars + " / " + Levels.All.Count * SaveData.MaxStars;
    }

    // ---------- المراحل ----------

    readonly List<Text> levelNumbers = new List<Text>();
    readonly List<Text> levelNames = new List<Text>();
    readonly List<Image> levelCards = new List<Image>();
    readonly List<Image[]> levelStars = new List<Image[]>();

    void BuildLevels()
    {
        levelsPanel = NotebookPage(canvasRect, "Levels", new Vector2(1560f, 960f), 0.8f);
        Label(levelsPanel, ArabicText.Fix("المراحل"), 80, new Vector2(0f, 390f), new Vector2(800f, 110f));

        int cols = 5;
        for (int i = 0; i < Levels.All.Count; i++)
        {
            int index = i;
            int row = i / cols, col = i % cols;
            // من اليمين لليسار مثل الكتابة العربية
            Vector2 pos = new Vector2(520f - col * 280f, 215f - row * 225f);
            var card = Panel(levelsPanel, "Level " + (i + 1), Secondary);
            card.sprite = rounded;
            card.type = Image.Type.Sliced;
            card.raycastTarget = true;
            Place(card.rectTransform, pos, new Vector2(260f, 200f));
            var button = card.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => { if (index <= SaveData.Unlocked) gm.StartLevelFromMenu(index); });
            card.gameObject.AddComponent<ButtonJuice>().Init(gm, clickClip);

            levelNumbers.Add(Label(card.rectTransform, "", 64, new Vector2(0f, 40f), new Vector2(300f, 80f)));
            var name = Label(card.rectTransform, "", 26, new Vector2(0f, -18f), new Vector2(290f, 40f));
            name.color = new Color(0.35f, 0.33f, 0.4f);
            levelNames.Add(name);
            var stars = new Image[SaveData.MaxStars];
            for (int s = 0; s < stars.Length; s++)
            {
                stars[s] = Panel(card.rectTransform, "Star", StarOff);
                stars[s].sprite = star;
                Place(stars[s].rectTransform, new Vector2((s - 1) * 52f, -65f), new Vector2(46f, 46f));
            }
            levelStars.Add(stars);
            levelCards.Add(card);
        }

        Button(levelsPanel, "رجوع", new Vector2(0f, -400f), new Vector2(320f, 90f), Secondary, () =>
        {
            if (levelsFromPause) ShowPause();
            else gm.BackToMain();
        }, 40);
    }

    void RefreshLevels()
    {
        for (int i = 0; i < levelCards.Count; i++)
        {
            bool open = i <= SaveData.Unlocked;
            int got = SaveData.Stars(i);
            levelCards[i].color = open ? (got == SaveData.MaxStars ? new Color(1f, 0.93f, 0.7f) : Secondary) : new Color(0.8f, 0.79f, 0.78f);
            levelNumbers[i].text = ArabicText.Fix(open ? (i + 1).ToString() : "مقفلة");
            levelNumbers[i].fontSize = open ? 64 : 40;
            levelNumbers[i].color = open ? Ink : new Color(0.45f, 0.44f, 0.48f);
            levelNames[i].text = open ? ArabicText.Fix(Levels.All[i].title) : "";
            for (int s = 0; s < SaveData.MaxStars; s++)
            {
                levelStars[i][s].enabled = open;
                levelStars[i][s].color = s < got ? Gold : StarOff;
            }
        }
    }

    // ---------- الاستراحة ----------

    void BuildPause()
    {
        pausePanel = NotebookPage(canvasRect, "Pause", new Vector2(700f, 780f), 1.2f);
        Label(pausePanel, ArabicText.Fix("استراحة"), 90, new Vector2(0f, 270f), new Vector2(600f, 120f));
        Button(pausePanel, "كمّلي", new Vector2(0f, 110f), new Vector2(460f, 110f), Primary, () => gm.Resume(), 48);
        Button(pausePanel, "أعيدي المرحلة", new Vector2(0f, -20f), new Vector2(460f, 100f), Secondary, () => gm.RestartLevel(), 40);
        Button(pausePanel, "المراحل", new Vector2(0f, -140f), new Vector2(460f, 100f), Secondary, () => ShowLevels(true), 40);
        Button(pausePanel, "القائمة الرئيسية", new Vector2(0f, -260f), new Vector2(460f, 100f), Secondary, () => gm.GoToMainMenu(), 40);
    }

    // ---------- شاشة الفوز ----------

    Text completeTitle, completeLevel, nextLabel;
    readonly Image[] bigStars = new Image[SaveData.MaxStars];
    readonly Text[] reasons = new Text[SaveData.MaxStars];
    readonly Image[] reasonStars = new Image[SaveData.MaxStars];
    Action nextAction;

    void BuildComplete()
    {
        completePanel = NotebookPage(canvasRect, "Complete", new Vector2(980f, 880f), -1f);
        completeTitle = Label(completePanel, "", 96, new Vector2(0f, 330f), new Vector2(900f, 120f));
        completeLevel = Label(completePanel, "", 40, new Vector2(0f, 245f), new Vector2(900f, 60f));
        completeLevel.color = new Color(0.4f, 0.38f, 0.45f);
        for (int i = 0; i < SaveData.MaxStars; i++)
        {
            // النجمة اللي بالنص أكبر وأعلى شوي
            bigStars[i] = Panel(completePanel, "Big Star", StarOff);
            bigStars[i].sprite = star;
            float size = i == 1 ? 190f : 150f;
            Place(bigStars[i].rectTransform, new Vector2((1 - i) * 200f, i == 1 ? 105f : 80f), new Vector2(size, size));

            reasonStars[i] = Panel(completePanel, "Reason Star", StarOff);
            reasonStars[i].sprite = star;
            Place(reasonStars[i].rectTransform, new Vector2(250f, -40f - i * 62f), new Vector2(44f, 44f));
            reasons[i] = Label(completePanel, "", 36, new Vector2(-30f, -40f - i * 62f), new Vector2(480f, 56f));
            reasons[i].alignment = TextAnchor.MiddleRight;
        }
        nextLabel = Button(completePanel, "", new Vector2(-170f, -330f), new Vector2(420f, 110f), Primary, () => nextAction?.Invoke(), 44);
        Button(completePanel, "أعيدي", new Vector2(160f, -330f), new Vector2(200f, 100f), Secondary, () => gm.RestartLevel(), 40);
        Button(completePanel, "المراحل", new Vector2(340f, -330f), new Vector2(150f, 100f), Secondary, () => ShowLevels(false), 32);
    }

    public void ShowComplete(int index, bool[] earned, bool last, Action next)
    {
        nextAction = next;
        completeTitle.text = ArabicText.Fix(last ? "خلصتي كل المراحل!" : "أحسنتِ!");
        completeLevel.text = ArabicText.Fix("المرحلة " + (index + 1) + " — " + Levels.All[index].title);
        nextLabel.text = ArabicText.Fix(last ? "القائمة الرئيسية" : "المرحلة اللي بعدها");
        string[] texts = { "خلصتي المرحلة", "ما خسرتي ولا قلب", "وفّرتي حبر" };
        for (int i = 0; i < SaveData.MaxStars; i++)
        {
            reasons[i].text = ArabicText.Fix(texts[i]);
            reasons[i].color = earned[i] ? Ink : new Color(0.55f, 0.53f, 0.58f);
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
        // العنوان يتمايل بهدوء
        if (mainPanel.gameObject.activeSelf)
        {
            float t = Time.unscaledTime;
            titleRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.3f) * 2f);
            titleRect.localScale = Vector3.one * (1f + Mathf.Sin(t * 2.1f) * 0.02f);
        }
        // النجوم الذهبية في شاشة الفوز تلمع
        if (completePanel.gameObject.activeSelf)
            for (int i = 0; i < SaveData.MaxStars; i++)
                if (bigStars[i].color == Gold && bigStars[i].rectTransform.localScale == Vector3.one)
                    bigStars[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 2f + i) * 5f);
    }

    // ---------- أدوات البناء ----------

    // صفحة دفتر: ورق بخطوط زرقا وهامش أحمر، مايلة شوي كأنها مرسومة باليد
    RectTransform NotebookPage(Transform parent, string name, Vector2 size, float tilt)
    {
        var page = Panel(parent, name, Paper);
        page.sprite = rounded;
        page.type = Image.Type.Sliced;
        page.raycastTarget = true;
        RectTransform rt = page.rectTransform;
        Place(rt, Vector2.zero, size);
        rt.localRotation = Quaternion.Euler(0f, 0f, tilt);
        page.gameObject.AddComponent<CanvasGroup>();

        var shadow = page.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        shadow.effectDistance = new Vector2(10f, -12f);

        for (float y = size.y * 0.5f - 120f; y > -size.y * 0.5f + 30f; y -= 58f)
        {
            var line = Panel(rt, "Line", LineBlue);
            Place(line.rectTransform, new Vector2(0f, y), new Vector2(size.x - 40f, 3f));
        }
        var margin = Panel(rt, "Margin", Margin);
        Place(margin.rectTransform, new Vector2(size.x * 0.5f - 110f, 0f), new Vector2(4f, size.y - 30f));
        return rt;
    }

    Text Button(RectTransform parent, string label, Vector2 pos, Vector2 size, Color color, Action onClick, int fontSize)
    {
        var img = Panel(parent, "Button " + label, color);
        img.sprite = rounded;
        img.type = Image.Type.Sliced;
        img.raycastTarget = true;
        Place(img.rectTransform, pos, size);
        var button = img.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => onClick());
        img.gameObject.AddComponent<ButtonJuice>().Init(gm, clickClip);
        return Label(img.rectTransform, ArabicText.Fix(label), fontSize, Vector2.zero, size);
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
        t.color = Ink;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        Place(t.rectTransform, pos, box);
        return t;
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

    // مستطيل بزوايا دائرية وحافة غامقة خفيفة (يتمدد بدون ما تخرب الزوايا)
    static Sprite RoundedSprite()
    {
        const int s = 96;
        const float r = 30f, edge = 4f;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { name = "Rounded", wrapMode = TextureWrapMode.Clamp };
        var px = new Color[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = Mathf.Max(r - (x + 0.5f), (x + 0.5f) - (s - r), 0f);
                float dy = Mathf.Max(r - (y + 0.5f), (y + 0.5f) - (s - r), 0f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);       // 0 داخل، يزيد عند الزوايا
                float a = Mathf.Clamp01(r - d + 0.5f);
                float inside = Mathf.Min(Mathf.Min(x + 0.5f, s - x - 0.5f), Mathf.Min(y + 0.5f, s - y - 0.5f));
                float border = Mathf.Max(Mathf.Clamp01(edge - (r - d) + 0.5f) * (d > 0f ? 1f : 0f), Mathf.Clamp01(edge - inside + 0.5f));
                float shade = Mathf.Lerp(1f, 0.55f, border);
                px[y * s + x] = new Color(shade, shade, shade, a);
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r + 2, r + 2, r + 2, r + 2));
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
                c = Color.Lerp(c, new Color(0.55f, 0.45f, 0.35f), Mathf.Clamp01(coverage - inner));
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
