using UnityEngine;
using UnityEngine.UI;

// واجهة اللعبة: شريط الحبر، رقم المرحلة، ورسائل في نص الشاشة
// ملاحظة: النصوص بالإنجليزي لأن نصوص Unity العادية ما تدعم الحروف العربية المتصلة
public class GameUI : MonoBehaviour
{
    RectTransform inkFill;
    Text levelText;
    Text messageText, subtitleText;
    Canvas canvasRef;

    public void SetVisible(bool visible)
    {
        if (canvasRef != null) canvasRef.enabled = visible;
    }
    float messageTimer;
    Font font;

    public void Build()
    {
        font = ArabicText.Font;

        var canvasGo = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 0.5f;
        canvasRef = canvas;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        // كل الواجهة داخل المنطقة الآمنة (بعيد عن النوتش وخط الشاشة الرئيسية بالآيفون)
        var safe = new GameObject("Safe Area", typeof(RectTransform), typeof(SafeArea)).GetComponent<RectTransform>();
        safe.SetParent(canvasGo.transform, false);
        Transform root = safe;

        // شريط الحبر
        // حافة بيضا عشان الكلام الأسود يبان حتى في المراحل الغامقة
        Label(root, ArabicText.Fix("الحبر"), 32, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(36f, -38f), new Vector2(90f, 44f)).gameObject.AddComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0.85f);
        // شريط الحبر على شكل قلم رصاص: ممحاة وردية، حلقة معدن، جسم فيه الحبر، ورأس خشب ورصاص
        Sprite round = MenuUI.RoundedSprite(), tri = MenuUI.TriangleSprite();
        Image frame = Panel(root, "Ink Pencil", Color.clear);
        Place(frame.rectTransform, new Vector2(0f, 1f), new Vector2(130f, -38f), new Vector2(430f, 46f));
        Color outline = new Color(0.22f, 0.16f, 0.24f);
        Image Piece(string n, Color c, Sprite sp, Vector2 min, Vector2 max)
        {
            Image im = Panel(frame.transform, n, c);
            im.sprite = sp;
            if (sp == round) im.type = Image.Type.Sliced;
            im.rectTransform.anchorMin = im.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            im.rectTransform.pivot = new Vector2(0f, 0.5f);
            im.rectTransform.anchoredPosition = new Vector2(min.x, (min.y + max.y) * 0.5f);
            im.rectTransform.sizeDelta = new Vector2(max.x - min.x, max.y - min.y);
            return im;
        }
        // الرأس (مثلث خشب) وطرف الرصاص
        var tipWood = Piece("Tip Outline", outline, tri, new Vector2(372f, -25f), new Vector2(432f, 25f));
        tipWood.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        tipWood.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        tipWood.rectTransform.anchoredPosition = new Vector2(404f, 0f);
        tipWood.rectTransform.sizeDelta = new Vector2(48f, 56f);   // بعد التدوير: 48 طول و 56 عرض
        var wood = Piece("Tip Wood", new Color(0.98f, 0.82f, 0.6f), tri, new Vector2(0f, 0f), new Vector2(50f, 40f));
        wood.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        wood.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        wood.rectTransform.anchoredPosition = new Vector2(400f, 0f);
        wood.rectTransform.sizeDelta = new Vector2(38f, 44f);
        var lead = Piece("Tip Lead", outline, tri, new Vector2(0f, 0f), new Vector2(16f, 14f));
        lead.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        lead.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        lead.rectTransform.anchoredPosition = new Vector2(418f, 0f);
        lead.rectTransform.sizeDelta = new Vector2(13f, 15f);
        // الجسم: حدود غامقة ثم ورق فاتح (المكان الفاضي) ثم الحبر
        Piece("Body Outline", outline, round, new Vector2(34f, -23f), new Vector2(384f, 23f));
        Piece("Body Paper", new Color(1f, 0.97f, 0.9f), round, new Vector2(38f, -19f), new Vector2(380f, 19f));
        // الممحاة والحلقة المعدن
        Piece("Eraser Outline", outline, round, new Vector2(0f, -23f), new Vector2(48f, 23f));
        Piece("Eraser", new Color(1f, 0.62f, 0.7f), round, new Vector2(4f, -19f), new Vector2(46f, 19f));
        Piece("Ferrule", new Color(0.78f, 0.8f, 0.84f), null, new Vector2(30f, -21f), new Vector2(52f, 21f));
        for (int k = 0; k < 3; k++) Piece("Ferrule Line", new Color(0.55f, 0.57f, 0.62f), null, new Vector2(34f + k * 6f, -21f), new Vector2(36f + k * 6f, 21f));
        Image area = Panel(frame.transform, "Fill Area", Color.clear);
        area.rectTransform.anchorMin = area.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        area.rectTransform.pivot = new Vector2(0f, 0.5f);
        area.rectTransform.anchoredPosition = new Vector2(54f, 0f);
        area.rectTransform.sizeDelta = new Vector2(322f, 30f);
        area.gameObject.AddComponent<RectMask2D>();
        Image fill = Panel(area.transform, "Fill", Color.black);
        fill.sprite = round;
        fill.type = Image.Type.Sliced;
        // لمعة فوق الحبر
        Image shine = Panel(fill.transform, "Shine", new Color(1f, 1f, 1f, 0.22f));
        shine.rectTransform.anchorMin = new Vector2(0.02f, 0.62f);
        shine.rectTransform.anchorMax = new Vector2(0.98f, 0.82f);
        shine.rectTransform.offsetMin = shine.rectTransform.offsetMax = Vector2.zero;
        inkFillImage = fill;
        inkFrame = frame.rectTransform;
        inkFill = fill.rectTransform;
        inkFill.anchorMin = Vector2.zero;
        inkFill.anchorMax = Vector2.one;
        inkFill.offsetMin = inkFill.offsetMax = Vector2.zero;

        levelText = Label(root, "", 40, TextAnchor.MiddleRight, new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(400f, 50f));
        levelText.gameObject.AddComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0.85f);
        BuildHearts(root);
        BuildWindArrow(root);
        BuildPalette(root);
        BuildSideButtons(root);
        timerText = Label(root, "", 38, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -45f), new Vector2(400f, 50f));
        timerText.gameObject.AddComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0.8f);
        BuildAirBar(root);
        // لافتة ورق ملصوقة بشريط لاصق ورا عنوان المرحلة
        banner = new GameObject("Title Banner", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
        banner.SetParent(root, false);
        Place(banner, new Vector2(0.5f, 0.5f), new Vector2(0f, 152f), new Vector2(700f, 128f));
        banner.pivot = new Vector2(0.5f, 0.5f);
        banner.localRotation = Quaternion.Euler(0f, 0f, -1.5f);
        bannerGroup = banner.GetComponent<CanvasGroup>();
        bannerGroup.blocksRaycasts = false;
        Sprite roundSp = MenuUI.RoundedSprite();
        Image bShadow = Panel(banner, "Shadow", new Color(0.1f, 0.05f, 0.15f, 0.28f));
        bShadow.sprite = roundSp; bShadow.type = Image.Type.Sliced;
        Stretch(bShadow.rectTransform, 0f); bShadow.rectTransform.anchoredPosition = new Vector2(8f, -8f);
        Image bPaper = Panel(banner, "Paper", new Color(1f, 0.98f, 0.92f));
        bPaper.sprite = roundSp; bPaper.type = Image.Type.Sliced;
        Stretch(bPaper.rectTransform, 0f);
        Image bLine = Panel(banner, "Margin Line", new Color(0.95f, 0.55f, 0.6f, 0.6f));
        bLine.rectTransform.anchorMin = new Vector2(0.06f, 0.12f); bLine.rectTransform.anchorMax = new Vector2(0.94f, 0.12f);
        bLine.rectTransform.sizeDelta = new Vector2(0f, 3f);
        for (int k = -1; k <= 1; k += 2)
        {
            Image tape = Panel(banner, "Tape", new Color(1f, 0.92f, 0.55f, 0.75f));
            tape.rectTransform.anchorMin = tape.rectTransform.anchorMax = new Vector2(k < 0 ? 0f : 1f, 1f);
            tape.rectTransform.sizeDelta = new Vector2(110f, 34f);
            tape.rectTransform.anchoredPosition = new Vector2(-k * 18f, -6f);
            tape.rectTransform.localRotation = Quaternion.Euler(0f, 0f, k * -28f);
        }
        banner.gameObject.SetActive(false);
        messageText = Label(root, "", 80, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1600f, 140f));
        subtitleText = Label(root, "", 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1700f, 70f));
        subtitleText.color = Color.white;
        subtitleText.gameObject.AddComponent<Outline>().effectColor = new Color(0.05f, 0.03f, 0.1f, 0.9f);
        messageText.gameObject.AddComponent<Outline>().effectColor = Color.white;
    }

    Image inkFillImage;
    RectTransform inkFrame;
    float shownInk = 1f;

    // الشريط ينزل بنعومة، ولما يقل الحبر يصير أحمر وينبض

    // ---------- شريط الهوا (تحت المويه) ----------

    RectTransform airRoot, airFill;
    Image airFillImage;
    float shownAir = 1f;

    void BuildAirBar(Transform root)
    {
        airRoot = new GameObject("Air", typeof(RectTransform)).GetComponent<RectTransform>();
        airRoot.SetParent(root, false);
        Place(airRoot, new Vector2(0.5f, 1f), new Vector2(0f, -45f), new Vector2(560f, 50f));
        Label(airRoot, ArabicText.Fix("الهواء"), 34, TextAnchor.MiddleRight, new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(110f, 50f)).gameObject.AddComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0.85f);
        Image frame = Panel(airRoot, "Air Bar", new Color(0.1f, 0.3f, 0.5f));
        Place(frame.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(430f, 40f));
        Image inner = Panel(frame.transform, "Inner", Color.white);
        Stretch(inner.rectTransform, 4f);
        Image area = Panel(inner.transform, "Fill Area", Color.clear);
        Stretch(area.rectTransform, 4f);
        airFillImage = Panel(area.transform, "Fill", new Color(0.35f, 0.75f, 1f));
        airFill = airFillImage.rectTransform;
        airFill.anchorMin = Vector2.zero;
        airFill.anchorMax = Vector2.one;
        airFill.offsetMin = airFill.offsetMax = Vector2.zero;
        airRoot.gameObject.SetActive(false);
    }

    public void SetAir(bool visible, float fraction)
    {
        if (airRoot == null) return;
        airRoot.gameObject.SetActive(visible);
        if (!visible) return;
        shownAir = Mathf.Lerp(shownAir, fraction, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 10f));
        airFill.anchorMax = new Vector2(shownAir, 1f);
        bool low = fraction < 0.3f;
        float pulse = low ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f) : 0f;
        airFillImage.color = low ? Color.Lerp(new Color(0.9f, 0.3f, 0.3f), new Color(1f, 0.6f, 0.4f), pulse) : new Color(0.35f, 0.75f, 1f);
        airRoot.localScale = Vector3.one * (1f + pulse * 0.04f);
    }
    public void SetInk(float fraction)
    {
        if (inkFill == null) return;
        fraction = Mathf.Clamp01(fraction);
        shownInk = fraction > shownInk ? fraction : Mathf.Lerp(shownInk, fraction, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 12f));
        inkFill.anchorMax = new Vector2(shownInk, 1f);
        bool low = fraction < 0.2f;
        float pulse = low ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f) : 0f;
        inkFillImage.color = low ? Color.Lerp(new Color(0.55f, 0.08f, 0.12f), new Color(0.9f, 0.2f, 0.25f), pulse) : new Color(0.2f, 0.19f, 0.28f);
        inkFrame.localScale = Vector3.one * (1f + pulse * 0.03f);
    }

    public void SetLevel(int number)
    {
        if (levelText != null) levelText.text = ArabicText.Fix("المرحلة " + number);
    }

    // فيه كلام على الشاشة الحين (تعليمات أو تلميح)
    public bool MessageShowing => messageTimer > 0f;

    public void ShowMessage(string message, float duration, string subtitle = null)
    {
        messageText.text = ArabicText.Fix(message);
        messageText.enabled = true;
        if (banner != null)
        {
            banner.gameObject.SetActive(!string.IsNullOrEmpty(message));
            banner.sizeDelta = new Vector2(Mathf.Clamp(messageText.preferredWidth + 140f, 420f, 1500f), 128f);
        }
        subtitleText.text = ArabicText.Fix(subtitle ?? "");
        subtitleText.enabled = true;
        messageTimer = duration;
        messageAge = 0f;
        messageLength = duration;
    }

    float messageAge, messageLength;
    RectTransform banner;
    CanvasGroup bannerGroup;

    // العنوان ينط لداخل ويختفي بهدوء
    void AnimateMessage()
    {
        if (!messageText.enabled) return;
        messageAge += Time.deltaTime;
        float t = Mathf.Clamp01(messageAge / 0.35f);
        float pop = t < 1f ? Mathf.LerpUnclamped(0.7f, 1f, 1f - Mathf.Pow(1f - t, 3f) + Mathf.Sin(t * Mathf.PI) * 0.12f) : 1f;
        float fade = Mathf.Clamp01(messageTimer / 0.4f) * Mathf.Clamp01(messageAge / 0.2f);
        messageText.rectTransform.localScale = Vector3.one * pop;
        if (banner != null) { banner.localScale = Vector3.one * pop; bannerGroup.alpha = fade; }
        Color c = messageText.color; c.a = fade; messageText.color = c;
        Color s = subtitleText.color; s.a = Mathf.Clamp01(messageTimer / 0.4f) * Mathf.Clamp01((messageAge - 0.15f) / 0.3f); subtitleText.color = s;
        subtitleText.rectTransform.anchoredPosition = new Vector2(0f, 60f - (1f - Mathf.Clamp01((messageAge - 0.15f) / 0.3f)) * 20f);
    }

    // ---------- أزرار جنب الشاشة: إعادة واستراحة ----------

    void BuildSideButtons(Transform root)
    {
        AudioClip click = SynthAudio.Click();
        SideButton(root, "Pause Button", new Vector2(-70f, -125f), IconSprite(false), () => GameManager.Instance.TogglePause(), click);
        SideButton(root, "Restart Button", new Vector2(-160f, -125f), IconSprite(true), () => GameManager.Instance.RestartLevel(), click);

        // زر القفز (للجوال): كبير تحت يمين، يطلع بس بالمراحل اللي فيها نط، وينط أول ما ينلمس
        Image jump = Panel(root, "Jump Button", Color.white);
        jump.sprite = JumpSprite();
        jump.raycastTarget = true;
        Place(jump.rectTransform, new Vector2(1f, 0f), new Vector2(-150f, 150f), new Vector2(170f, 170f));
        jump.gameObject.AddComponent<TapDown>().onDown = () =>
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.IsPlaying && gm.Rowaida != null) gm.Rowaida.Jump();
            jump.rectTransform.localScale = Vector3.one * 0.88f;
        };
        jumpButton = jump.rectTransform;
        jumpButton.gameObject.SetActive(false);
    }

    RectTransform jumpButton;

    void UpdateJumpButton()
    {
        var gm = GameManager.Instance;
        bool show = gm != null && gm.IsPlaying && !gm.IntroActive && gm.CurrentLevel != null && gm.CurrentLevel.CanJump;
        if (jumpButton.gameObject.activeSelf != show) jumpButton.gameObject.SetActive(show);
        if (show) jumpButton.localScale = Vector3.MoveTowards(jumpButton.localScale, Vector3.one, Time.unscaledDeltaTime * 2f);
    }

    // أيقونة زر القفز: دايرة وردية بحافة بيضا وفيها سهم أبيض لفوق
    public static Sprite JumpSprite()
    {
        const int s = 128;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, true) { name = "Jump Icon", wrapMode = TextureWrapMode.Clamp };
        var px = new Color[s * s];
        Color pink = new Color(1f, 0.47f, 0.56f), dark = new Color(0.86f, 0.33f, 0.45f);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s * 2f - 1f, v = (y + 0.5f) / s * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                float disc = Mathf.Clamp01((0.96f - r) * s * 0.5f);
                float rim = Mathf.Clamp01((r - 0.84f) * s * 0.5f);
                // سهم: راس مثلث فوق وعمود تحته
                bool head = v > 0.02f && v < 0.5f && Mathf.Abs(u) < (0.5f - v) * 0.95f;
                bool stem = v > -0.45f && v <= 0.05f && Mathf.Abs(u) < 0.13f;
                Color c = Color.Lerp(pink, dark, Mathf.Clamp01(-v * 0.6f));
                if (head || stem || rim > 0f) c = Color.Lerp(c, Color.white, Mathf.Max(rim, head || stem ? 1f : 0f));
                c.a = disc;
                px[y * s + x] = c;
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }

    void SideButton(Transform root, string name, Vector2 pos, Sprite icon, System.Action onClick, AudioClip click)
    {
        Image img = Panel(root, name, Color.white);
        img.sprite = icon;
        img.raycastTarget = true;
        Place(img.rectTransform, new Vector2(1f, 1f), pos, new Vector2(76f, 76f));
        var button = img.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() =>
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.IntroActive) return;
            onClick();
        });
        img.gameObject.AddComponent<ButtonJuice>().Init(GameManager.Instance, click);
    }

    // أيقونة مرسومة بالكود: دايرة بيضا بحافة غامقة وفيها سهم دايري (إعادة) أو خطين (استراحة)
    public static Sprite IconSprite(bool restart)
    {
        const int s = 128;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, true) { name = restart ? "Restart Icon" : "Pause Icon", wrapMode = TextureWrapMode.Clamp };
        var px = new Color[s * s];
        Color ink = new Color(0.16f, 0.15f, 0.2f);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float u = (x + 0.5f) / s * 2f - 1f, v = (y + 0.5f) / s * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                float disc = Mathf.Clamp01((0.96f - r) * s * 0.5f);
                float rim = Mathf.Clamp01((r - 0.86f) * s * 0.5f);
                float icon;
                if (restart)
                {
                    // قوس ثلاث أرباع دايرة وفي آخره راس سهم
                    float ang = Mathf.Atan2(v, u) * Mathf.Rad2Deg;
                    float ring = Mathf.Clamp01((0.11f - Mathf.Abs(r - 0.45f)) * s * 0.5f);
                    float gap = ang > 30f && ang < 95f ? 0f : 1f;
                    // راس السهم عند زاوية 30 درجة
                    Vector2 tip = new Vector2(Mathf.Cos(30f * Mathf.Deg2Rad), Mathf.Sin(30f * Mathf.Deg2Rad)) * 0.45f;
                    Vector2 p = new Vector2(u, v) - tip;
                    float head = (p.y > -0.02f && p.y < 0.26f && Mathf.Abs(p.x) < (0.26f - p.y) * 0.9f) ? 1f : 0f;
                    icon = Mathf.Max(ring * gap, head);
                }
                else
                {
                    bool bar = Mathf.Abs(v) < 0.42f && (Mathf.Abs(u - 0.2f) < 0.1f || Mathf.Abs(u + 0.2f) < 0.1f);
                    icon = bar ? 1f : 0f;
                }
                Color c = Color.Lerp(new Color(1f, 0.99f, 0.96f), ink, Mathf.Max(rim, icon));
                c.a = disc;
                px[y * s + x] = c;
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }

    // ---------- الوقت ----------

    Text timerText;

    public void SetTimer(bool visible, float seconds)
    {
        timerText.enabled = visible;
        if (!visible) return;
        int s = Mathf.CeilToInt(seconds);
        timerText.text = ArabicText.Fix("الصفحة تنطوي بعد " + s);
        timerText.color = s <= 8 ? new Color(0.85f, 0.15f, 0.2f) : Color.black;
    }

    // ---------- ألوان الحبر ----------

    readonly Image[] swatches = new Image[4];
    readonly Image[] swatchRings = new Image[4];

    void BuildPalette(Transform root)
    {
        Sprite dot = DotSprite();
        string[] keys = { "1", "2", "3", "4" };
        for (int i = 0; i < 4; i++)
        {
            Vector2 pos = new Vector2(-135f + i * 90f, -60f);
            Image ring = Panel(root, "Swatch Ring", Color.white);
            ring.sprite = dot;
            Place(ring.rectTransform, new Vector2(0.5f, 1f), pos, new Vector2(78f, 78f));
            Image s = Panel(root, "Swatch", InkColorEffect.ColorOf((InkColor)i));
            s.sprite = dot;
            Place(s.rectTransform, new Vector2(0.5f, 1f), pos, new Vector2(60f, 60f));
            Text key = Label(s.transform, keys[i], 26, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
            key.color = Color.white;
            swatches[i] = s;
            swatchRings[i] = ring;
        }
        SetPalette(false, InkColor.Black);
    }

    public void SetPalette(bool visible, InkColor selected)
    {
        if (swatches[0] == null) return;
        for (int i = 0; i < 4; i++)
        {
            swatches[i].enabled = visible;
            swatches[i].GetComponentInChildren<Text>().enabled = visible;
            bool on = (int)selected == i;
            swatchRings[i].enabled = visible && on;
            swatches[i].rectTransform.localScale = Vector3.one * (on ? 1.12f : 0.9f);
        }
    }

    // الضغطة على لون؟
    public bool PaletteHit(Vector2 screen, out InkColor color)
    {
        color = InkColor.Black;
        for (int i = 0; i < 4; i++)
        {
            if (!swatches[i].enabled) continue;
            if (RectTransformUtility.RectangleContainsScreenPoint(swatches[i].rectTransform, screen, canvasRef.worldCamera))
            {
                color = (InkColor)i;
                return true;
            }
        }
        return false;
    }

    // دائرة ناعمة (للألوان)
    static Sprite DotSprite()
    {
        const int s = 64;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, true) { name = "Dot", wrapMode = TextureWrapMode.Clamp };
        var px = new Color[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x + 0.5f) / s * 2f - 1f, dy = (y + 0.5f) / s * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                px[y * s + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * s * 0.5f));
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }

    // ---------- سهم الريح ----------

    Image windArrow;
    Text windLabel;

    void BuildWindArrow(Transform root)
    {
        windLabel = Label(root, ArabicText.Fix("الريح"), 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(200f, 40f));
        windLabel.color = Color.white;
        windLabel.gameObject.AddComponent<Outline>().effectColor = new Color(0.05f, 0.03f, 0.1f, 0.9f);
        windArrow = Panel(root, "Wind Arrow", Color.white);
        windArrow.sprite = ArrowSprite();
        Place(windArrow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(150f, 48f));
        windLabel.enabled = windArrow.enabled = false;
    }

    // يوري اتجاه الريح وقوتها (0 = نخفي السهم)
    public void SetWind(float wind, bool visible)
    {
        windLabel.enabled = windArrow.enabled = visible;
        if (!visible) return;
        float strength = Mathf.Clamp01(Mathf.Abs(wind) / 1.5f);
        float dir = wind >= 0f ? 1f : -1f;
        float wobble = 1f + Mathf.Sin(Time.time * 9f) * 0.05f * strength;
        windArrow.rectTransform.localScale = new Vector3(dir * (0.35f + 0.65f * strength) * wobble, 1f, 1f);
        windArrow.color = Color.Lerp(new Color(1f, 1f, 1f, 0.5f), new Color(0.75f, 0.9f, 1f, 1f), strength);
    }

    // سهم يأشر لليمين
    static Sprite ArrowSprite()
    {
        const int w = 192, h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "Wind Arrow", wrapMode = TextureWrapMode.Clamp };
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w, v = (y + 0.5f) / h - 0.5f;
                float shaft = (u < 0.68f && Mathf.Abs(v) < 0.12f) ? 1f : 0f;
                float head = (u >= 0.6f && Mathf.Abs(v) < (1f - u) * 1.25f) ? 1f : 0f;
                float a = Mathf.Max(shaft, head);
                float edge = Mathf.Max((u < 0.68f && Mathf.Abs(v) > 0.07f) ? shaft : 0f, (Mathf.Abs(v) > (1f - u) * 1.25f - 0.06f) ? head : 0f);
                px[y * w + x] = Color.Lerp(Color.white, new Color(0.2f, 0.25f, 0.4f), edge * 0.8f) * new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    // ---------- القلوب ----------

    const int MaxHearts = 3;
    readonly Image[] hearts = new Image[MaxHearts];
    readonly float[] heartPop = new float[MaxHearts];
    static readonly Color HeartColor = new Color(1f, 0.42f, 0.55f);
    static readonly Color HeartLost = new Color(0.35f, 0.33f, 0.4f, 0.55f);

    void BuildHearts(Transform root)
    {
        Sprite sprite = HeartSprite();
        for (int i = 0; i < MaxHearts; i++)
        {
            Image img = Panel(root, "Heart " + (i + 1), HeartColor);
            img.sprite = sprite;
            img.preserveAspect = true;
            Place(img.rectTransform, new Vector2(0f, 1f), new Vector2(40f + i * 62f, -96f), new Vector2(56f, 56f));
            hearts[i] = img;
        }
    }

    // عدد القلوب الباقية. لو animateLoss = true القلب اللي راح ينط ويصير رمادي
    public void SetHearts(int remaining, bool animateLoss)
    {
        for (int i = 0; i < MaxHearts; i++)
        {
            bool alive = i < remaining;
            if (!alive && animateLoss && i == remaining) heartPop[i] = 1f;
            hearts[i].color = alive ? HeartColor : HeartLost;
            if (alive) heartPop[i] = 0f;
        }
    }

    void AnimateHearts()
    {
        for (int i = 0; i < MaxHearts; i++)
        {
            float p = heartPop[i];
            if (p > 0f) heartPop[i] = Mathf.Max(0f, p - Time.deltaTime * 2f);
            float scale = 1f + Mathf.Sin(p * Mathf.PI) * 0.5f;
            float shake = p > 0f ? Mathf.Sin(Time.time * 60f) * 6f * p : 0f;
            hearts[i].rectTransform.localScale = Vector3.one * scale;
            hearts[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, shake);
        }
    }

    // صورة قلب مرسومة بالكود: لون وردي مع لمعة وحافة غامقة
    static Sprite HeartSprite()
    {
        const int s = 128;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, true) { name = "Heart", wrapMode = TextureWrapMode.Clamp };
        var px = new Color[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float coverage = 0f, inner = 0f;
                for (int sy = 0; sy < 3; sy++)
                    for (int sx = 0; sx < 3; sx++)
                    {
                        float u = ((x + (sx + 0.5f) / 3f) / s - 0.5f) * 2.6f;
                        float v = ((y + (sy + 0.5f) / 3f) / s - 0.45f) * 2.6f;
                        float a = u * u + v * v - 1f;
                        float f = a * a * a - u * u * v * v * v;
                        if (f <= 0f) coverage += 1f / 9f;
                        // قلب أصغر شوي: الفرق بينه وبين الكبير هو الحافة
                        float iu = u * 1.12f, iv = v * 1.12f + 0.03f;
                        float ia = iu * iu + iv * iv - 1f;
                        if (ia * ia * ia - iu * iu * iv * iv * iv <= 0f) inner += 1f / 9f;
                    }
                float hu = (float)x / s - 0.36f, hv = (float)y / s - 0.66f;
                float shine = Mathf.Clamp01(1f - Mathf.Sqrt(hu * hu * 2.2f + hv * hv * 5f) / 0.12f);
                Color c = new Color(0.86f, 0.8f, 0.83f);
                c = Color.Lerp(c, Color.white, shine);
                c = Color.Lerp(c, new Color(0.45f, 0.15f, 0.25f), Mathf.Clamp01(coverage - inner));
                c.a = coverage;
                px[y * s + x] = c;
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }

    void Update()
    {
        if (hearts[0] == null) return;   // الواجهة انحذفت (وقفنا اللعب)
        AnimateHearts();
        AnimateMessage();
        if (jumpButton != null) UpdateJumpButton();
        if (messageTimer <= 0f) return;
        messageTimer -= Time.deltaTime;
        if (messageTimer <= 0f) { messageText.enabled = subtitleText.enabled = false; if (banner != null) banner.gameObject.SetActive(false); }
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

    Text Label(Transform parent, string text, int size, TextAnchor align, Vector2 anchor, Vector2 pos, Vector2 box)
    {
        var go = new GameObject(text.Length > 0 ? text : "Message", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.verticalOverflow = VerticalWrapMode.Overflow;   // الخط العربي سطره عالي: ما نخليه ينقص ويختفي
        t.text = text;
        t.fontSize = ArabicText.Size(size);
        t.fontStyle = FontStyle.Bold;
        t.alignment = align;
        t.color = Color.black;
        t.raycastTarget = false;
        Place(t.rectTransform, anchor, pos, box);
        return t;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.one * inset;
        rt.offsetMax = -Vector2.one * inset;
    }
}

// زر يشتغل أول ما ينلمس (مو لما ترفعين إصبعك)، مثل زر القفز
public class TapDown : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler
{
    public System.Action onDown;
    public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e) => onDown?.Invoke();
}

// يحط الواجهة داخل المنطقة الآمنة للشاشة (الجوالات اللي فيها نوتش أو حواف مدورة)
public class SafeArea : MonoBehaviour
{
    Rect last;
    Vector2Int lastSize;

    void Awake() => Apply();
    void Update() { if (Screen.safeArea != last || lastSize.x != Screen.width || lastSize.y != Screen.height) Apply(); }

    void Apply()
    {
        var rt = (RectTransform)transform;
        last = Screen.safeArea;
        lastSize = new Vector2Int(Screen.width, Screen.height);
        if (Screen.width <= 0 || Screen.height <= 0) return;
        rt.anchorMin = new Vector2(last.xMin / Screen.width, last.yMin / Screen.height);
        rt.anchorMax = new Vector2(last.xMax / Screen.width, last.yMax / Screen.height);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
