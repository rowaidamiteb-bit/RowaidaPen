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

        var canvasGo = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 0.5f;
        canvasRef = canvas;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        Transform root = canvasGo.transform;

        // شريط الحبر
        Label(root, ArabicText.Fix("الحبر"), 32, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(100f, 44f));
        Image frame = Panel(root, "Ink Bar", Color.black);
        Place(frame.rectTransform, new Vector2(0f, 1f), new Vector2(120f, -40f), new Vector2(420f, 44f));
        Image inner = Panel(frame.transform, "Inner", Color.white);
        Stretch(inner.rectTransform, 4f);
        Image area = Panel(inner.transform, "Fill Area", Color.clear);
        Stretch(area.rectTransform, 4f);
        Image fill = Panel(area.transform, "Fill", Color.black);
        inkFill = fill.rectTransform;
        inkFill.anchorMin = Vector2.zero;
        inkFill.anchorMax = Vector2.one;
        inkFill.offsetMin = inkFill.offsetMax = Vector2.zero;

        levelText = Label(root, "", 40, TextAnchor.MiddleRight, new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(400f, 50f));
        BuildHearts(root);
        BuildWindArrow(root);
        messageText = Label(root, "", 80, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1600f, 140f));
        subtitleText = Label(root, "", 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1700f, 70f));
        subtitleText.color = Color.white;
        subtitleText.gameObject.AddComponent<Outline>().effectColor = new Color(0.05f, 0.03f, 0.1f, 0.9f);
        messageText.gameObject.AddComponent<Outline>().effectColor = Color.white;
        Label(root, ArabicText.Fix("ارسمي بالماوس   •   زر R يعيد المرحلة"), 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1200f, 40f))
            .color = new Color(0f, 0f, 0f, 0.6f);
    }

    public void SetInk(float fraction)
    {
        if (inkFill != null) inkFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
    }

    public void SetLevel(int number)
    {
        if (levelText != null) levelText.text = ArabicText.Fix("المرحلة " + number);
    }

    public void ShowMessage(string message, float duration, string subtitle = null)
    {
        messageText.text = ArabicText.Fix(message);
        messageText.enabled = true;
        subtitleText.text = ArabicText.Fix(subtitle ?? "");
        subtitleText.enabled = true;
        messageTimer = duration;
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
        AnimateHearts();
        if (messageTimer <= 0f) return;
        messageTimer -= Time.deltaTime;
        if (messageTimer <= 0f) messageText.enabled = subtitleText.enabled = false;
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
        t.text = text;
        t.fontSize = size;
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
