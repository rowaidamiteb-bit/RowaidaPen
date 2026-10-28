using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// ذاكرة الرسومات: كل خط ترسمه اللاعبة ينحفظ مع رقم المرحلة، عشان يطلع في دفتر رويدا في النهاية
public static class DrawingMemory
{
    const int MaxPerLevel = 14;
    static readonly Dictionary<int, List<List<Vector2>>> strokes = new Dictionary<int, List<List<Vector2>>>();

    public static void Record(int level, List<Vector3> points)
    {
        if (points.Count < 2) return;
        if (!strokes.TryGetValue(level, out var list)) strokes[level] = list = new List<List<Vector2>>();
        var copy = new List<Vector2>(points.Count);
        foreach (Vector3 p in points) copy.Add(new Vector2(p.x, p.y));
        list.Add(copy);
        if (list.Count > MaxPerLevel) list.RemoveAt(0);
    }

    public static void Clear() => strokes.Clear();

    // المراحل اللي فيها رسومات بالترتيب
    public static List<int> Levels()
    {
        var keys = new List<int>(strokes.Keys);
        keys.Sort();
        return keys;
    }

    // صفحة دفتر فيها رسومات مرحلة (مصغرة عشان تدخل في الصفحة)
    public static Texture2D PageFor(int level, int seed, float pixelsPerUnit = 500f)
    {
        var canvas = new PaperCanvas(new Vector2(0.32f, 0.42f), seed, pixelsPerUnit);
        float px = pixelsPerUnit / 500f;   // عرض الخطوط يكبر مع دقة الصفحة
        if (level < 0 || !strokes.TryGetValue(level, out var list) || list.Count == 0)
        {
            canvas.RandomDoodle(new Vector2(0f, 0.06f), 0.18f);
            canvas.RandomDoodle(new Vector2(0.04f, -0.1f), 0.12f);
            return canvas.ToTexture("Memory Page");
        }
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        foreach (var s in list)
            foreach (Vector2 p in s) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        Vector2 size = Vector2.Max(max - min, new Vector2(0.5f, 0.5f));
        float scale = Mathf.Min(0.27f / size.x, 0.3f / size.y, 0.03f);
        Vector2 center = (min + max) * 0.5f;
        foreach (var s in list)
        {
            var pts = new List<Vector2>(s.Count);
            foreach (Vector2 p in s) pts.Add((p - center) * scale + new Vector2(0f, -0.02f));
            canvas.Polyline(pts, 2.2f * px, 0.95f);
        }
        // خط أرض خفيف تحت الرسومات (مثل صفحة المرحلة)
        canvas.Line(new Vector2(-0.14f, -0.02f - size.y * scale * 0.5f - 0.02f), new Vector2(0.14f, -0.02f - size.y * scale * 0.5f - 0.02f), 1.2f * px, 0.35f);
        return canvas.ToTexture("Memory Page");
    }
}

// الباب الأخير: إطار منقط ترسمين فوقه باب. أول ما ترسمينه يضوي وينفتح، ورويدا تدخل وترجع لغرفتها
public class FinalDoor : MonoBehaviour
{
    const float Width = 1.3f, Height = 2.1f;
    Vector3 bottom;
    bool opened;
    float nextSparkle;

    public static void Create(Transform root, Vector3 bottom, GameManager gm)
    {
        var go = new GameObject("Final Door Spot");
        go.transform.SetParent(root, false);
        go.transform.position = bottom;
        var d = go.AddComponent<FinalDoor>();
        d.bottom = bottom;

        // إطار منقط: جنبين وفوق مقوس شوي، ومقبض
        Color hint = new Color(0.95f, 0.65f, 0.35f);
        float l = bottom.x - Width * 0.5f, r = bottom.x + Width * 0.5f, y0 = bottom.y + 0.12f, y1 = bottom.y + Height;
        LevelBuilder.DashedHint(go.transform, new HintLine(new Vector3(l, y0, 0f), new Vector3(l, y1 - 0.2f, 0f), hint), gm);
        LevelBuilder.DashedHint(go.transform, new HintLine(new Vector3(r, y0, 0f), new Vector3(r, y1 - 0.2f, 0f), hint), gm);
        LevelBuilder.DashedHint(go.transform, new HintLine(new Vector3(l, y1 - 0.2f, 0f), new Vector3(bottom.x, y1, 0f), hint), gm);
        LevelBuilder.DashedHint(go.transform, new HintLine(new Vector3(bottom.x, y1, 0f), new Vector3(r, y1 - 0.2f, 0f), hint), gm);
        LevelBuilder.DashedHint(go.transform, new HintLine(new Vector3(l, y0, 0f), new Vector3(r, y0, 0f), hint), gm);
        LevelBuilder.DashedHint(go.transform, new HintLine(new Vector3(r - 0.3f, bottom.y + 1f, 0f), new Vector3(r - 0.2f, bottom.y + 1f, 0f), hint), gm);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Rowaida == null || opened) return;

        // لمعات خفيفة حول الإطار تلفت الانتباه
        if (Time.time >= nextSparkle)
        {
            nextSparkle = Time.time + 0.18f;
            float side = Random.value < 0.5f ? -1f : 1f;
            Fx.Sparkle(bottom + new Vector3(side * Width * 0.5f, Random.Range(0.2f, Height), 0f), new Color(1f, 0.85f, 0.5f), 3);
        }

        // رويدا توقف قدام الباب وتستنى
        RowaidaController rowaida = gm.Rowaida;
        if (gm.IsPlaying && rowaida.transform.position.x >= bottom.x - Width * 0.5f - 1.4f) rowaida.Held = true;

        InkMark door = FindDrawnDoor();
        if (door != null) StartCoroutine(Open(door, gm));
    }

    // رسمة كبيرة (عرضها وطولها مثل الباب) فوق الإطار المنقط
    InkMark FindDrawnDoor()
    {
        foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            var rend = mark.GetComponent<MeshRenderer>();
            if (rend == null) continue;
            Bounds b = rend.bounds;
            bool near = Mathf.Abs(b.center.x - bottom.x) < 1.2f && b.center.y > bottom.y + 0.3f && b.center.y < bottom.y + 2f;
            if (near && b.size.x >= 0.7f && b.size.y >= 1.1f) return mark;
        }
        return null;
    }

    IEnumerator Open(InkMark door, GameManager gm)
    {
        opened = true;
        // الرسمة تثبت مكانها (حتى لو كانت شكل مغلق يطيح) وما تصد رويدا
        var rb = door.GetComponent<Rigidbody>();
        if (rb != null) { rb.linearVelocity = Vector3.zero; rb.isKinematic = true; }
        foreach (Collider c in door.GetComponentsInChildren<Collider>()) c.enabled = false;
        Destroy(door);

        Bounds b = door.GetComponent<MeshRenderer>().bounds;
        gm.Sound.Play(SoundClips.Magic, 1f);

        // ضوء دافي ورا الباب
        var lightMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Door Light" };
        lightMat.SetColor("_BaseColor", new Color(3f, 2.5f, 1.6f));
        var glow = LevelBuilder.Visual("Door Light", PrimitiveType.Quad, transform, Vector3.zero, new Vector3(b.size.x * 0.85f, b.size.y * 0.85f, 1f), lightMat, false);
        glow.transform.position = new Vector3(b.center.x, b.center.y, 0.06f);
        var light = new GameObject("Door Glow").AddComponent<Light>();
        light.transform.SetParent(transform, false);
        light.transform.position = new Vector3(b.center.x, b.center.y, -0.6f);
        light.type = LightType.Point;
        light.color = new Color(1f, 0.85f, 0.6f);
        light.range = 6f;

        // ضلفة الباب (وردي فاتح) تنفتح من جهة اليسار
        var hinge = new GameObject("Door Hinge").transform;
        hinge.SetParent(transform, false);
        hinge.position = new Vector3(b.min.x + 0.1f, b.center.y, -0.02f);
        Material leafMat = gm.Mat("Final Door Leaf", new Color(0.98f, 0.8f, 0.85f), 0.3f);
        float leafW = b.size.x * 0.82f;
        LevelBuilder.Visual("Door Leaf", PrimitiveType.Cube, hinge, new Vector3(leafW * 0.5f, 0f, 0f), new Vector3(leafW, b.size.y * 0.85f, 0.06f), leafMat);
        LevelBuilder.Visual("Knob", PrimitiveType.Sphere, hinge, new Vector3(leafW * 0.85f, -0.05f, -0.06f), Vector3.one * 0.1f, gm.Mat("Knob Gold", new Color(0.9f, 0.75f, 0.45f), 0.8f, 0.9f));

        for (float t = 0f; t < 1f; t += Time.deltaTime / 1.3f)
        {
            light.intensity = Mathf.Lerp(0f, 4f, t);
            glow.transform.localScale = new Vector3(b.size.x * 0.85f, b.size.y * 0.85f, 1f) * (0.95f + Mathf.Sin(Time.time * 6f) * 0.03f);
            hinge.localRotation = Quaternion.Euler(0f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.3f) / 0.7f)) * -115f, 0f);
            if (Random.value < 0.4f) Fx.Sparkle(b.center + Random.insideUnitSphere * 0.8f, new Color(1f, 0.9f, 0.6f), 2);
            yield return null;
        }

        // رويدا تمشي للباب، ولما توصل وسطه تنتهي المرحلة
        var trigger = new GameObject("Final Door Trigger");
        trigger.transform.SetParent(transform, false);
        trigger.transform.position = new Vector3(b.center.x, bottom.y + 0.9f, 0f);
        var box = trigger.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(0.4f, 1.8f, 2f);
        trigger.AddComponent<Door>();
        if (gm.Rowaida != null) gm.Rowaida.Held = false;
    }
}

// النهاية: رويدا تصحى في غرفتها الصبح، تتمطط، الدفتر يطير لحضنها، وتقلّب صفحاته وتشوف كل الرسومات اللي رسمتيها
public class EndingCutscene : MonoBehaviour
{
    static readonly Vector3 RoomOrigin = new Vector3(-300f, 0f, 0f);
    const float FlipEvery = 1.8f, PagesStart = 10.5f;

    GameManager gm;
    System.Action onFinished;
    Bedroom room;
    Camera cam;
    float t, endTime, realT;
    bool finished, magicPlayed;
    TimeWarp warp;
    SkipButton skip;
    float[] voiceAt;
    static readonly string[] Voices = { "ending_0", "ending_1", "ending_2", "ending_3", "ending_4", "ending_5" };

    Image fade;
    Text caption, bigText;
    Canvas canvas;

    Light sun, windowLight;
    float sunIntensity;
    Color sunColor, ambSky, ambEq, ambGround;
    Quaternion sunRot;

    List<int> memoryLevels;
    int spreads, shownSpread = -1;

    public void Play(GameManager manager, System.Action finishedCallback)
    {
        gm = manager;
        onFinished = finishedCallback;
        cam = Camera.main;
        var follow = cam.GetComponent<CameraFollow>();
        if (follow != null) follow.SetTarget(null, false);
        room = Bedroom.Build(gm, RoomOrigin);

        memoryLevels = DrawingMemory.Levels();
        spreads = Mathf.Max(1, Mathf.CeilToInt(memoryLevels.Count / 2f));
        endTime = PagesStart + spreads * FlipEvery + 8f;

        // صوت رويدا: كل جملة تبدأ بنقطة، والمشهد يبطئ لو الجملة تبي وقت أطول
        float spreadsEnd = PagesStart + spreads * FlipEvery;
        voiceAt = new[] { 1f, 5f, 8.8f, PagesStart, spreadsEnd + 0.3f, spreadsEnd + 3.8f };
        float[] marks = { 0f, 1f, 5f, 8.8f, PagesStart, spreadsEnd, spreadsEnd + 0.3f, spreadsEnd + 3.8f, endTime };
        var minReal = new float[marks.Length - 1];
        int[] seg = { 1, 2, 3, 4, 6, 7 };
        for (int i = 0; i < Voices.Length; i++) minReal[seg[i]] = Narration.Length(Voices[i]) + (i == 5 ? 1.2f : 0.6f);
        warp = new TimeWarp(marks, minReal);

        SetupLighting();
        BuildOverlay();
        Apply();
    }

    void Update()
    {
        if (room == null || finished) return;
        // زر التخطي يطلع بعد ما يمر نص المشهد
        bool half = realT >= warp.RealEnd * 0.5f;
        if (half && !skip.Shown && t < endTime - 1.2f) skip.SetVisible(true);
        if (half && SkipPressed()) Skip();
        float prev = t;
        realT += Time.deltaTime;
        t = warp.Anim(realT);
        for (int i = 0; i < Voices.Length; i++) if (prev < voiceAt[i] && t >= voiceAt[i]) Narration.Say(Voices[i]);
        Apply();
        if (t >= endTime) { skip.SetVisible(false); StartCoroutine(Finish()); }
    }

    void Skip()
    {
        if (finished || t >= endTime - 1.2f) return;
        realT = warp.Real(endTime - 1.2f);
        t = endTime - 1.2f;
        Narration.Stop();
        skip.SetVisible(false);
    }

    static bool SkipPressed()
    {
        var kb = Keyboard.current;
        return kb != null && (kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame);
    }

    // ---------- كل إطار ----------

    void Apply()
    {
        Transform root = room.Root;
        Vector3 lap = room.HipSitting + room.Facing * new Vector3(0f, 0.12f, 0.32f);

        // الكاميرا: من بعيد، بعدين أقرب، بعدين على الدفتر
        Vector3 wideP = new Vector3(2.2f, 1.75f, -2.6f), wideL = new Vector3(-0.7f, 0.85f, 1.45f);
        Vector3 nearP = new Vector3(0.7f, 1.3f, -0.3f), nearL = new Vector3(-0.85f, 0.95f, 1.45f);
        float k1 = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5.5f, 8f, t));
        Vector3 camPos = Vector3.Lerp(wideP, nearP, k1);
        Vector3 camLook = Vector3.Lerp(wideL, nearL, k1);
        cam.transform.position = root.TransformPoint(camPos);
        cam.transform.LookAt(root.TransformPoint(camLook));

        PoseRowaida();
        AnimateNotebook(lap);
        AnimatePages();

        // الكلام
        float spreadsEnd = PagesStart + spreads * FlipEvery;
        string text = "";
        float alpha = 0f;
        void Say(float from, float to, string s)
        {
            if (t < from || t > to) return;
            text = s;
            alpha = Mathf.Clamp01((t - from) / 0.4f) * Mathf.Clamp01((to - t) / 0.4f);
        }
        Say(1f, 4.6f, "استيقظت... وأنا في غرفتي! والشمس مشرقة");
        Say(5f, 8.4f, "هل كان كله حلما؟");
        Say(8.8f, 10.4f, "لكن الدفتر...");
        Say(PagesStart, spreadsEnd, "فيه كل الرسومات التي أعادتني!");
        Say(spreadsEnd + 0.3f, spreadsEnd + 3.6f, "شكرا على رسم طريقي إلى البيت");
        string shaped = ArabicText.Fix(text);
        if (caption.text != shaped) caption.text = shaped;
        caption.color = new Color(1f, 1f, 1f, alpha);

        // "النهاية" في الآخر
        float theEnd = Mathf.Clamp01((t - (spreadsEnd + 3.8f)) / 0.8f);
        bigText.color = new Color(1f, 1f, 1f, theEnd);
        bigText.rectTransform.localScale = Vector3.one * (0.85f + 0.15f * theEnd);

        // تبدأ بيضا (من نور الباب) وتفتح، وفي الآخر تبيض مرة ثانية
        if (t < 1.4f) fade.color = new Color(1f, 0.98f, 0.94f, 1f - t / 1.4f);
        else if (t > endTime - 1.2f) fade.color = new Color(1f, 0.98f, 0.94f, Mathf.Clamp01((t - (endTime - 1.2f)) / 1.1f));
        else fade.color = Color.clear;
    }

    void PoseRowaida()
    {
        RowaidaModel r = room.Rowaida;

        // نايمة، بعدين تقعد
        float lie = (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3.2f, 5f, t))) * 82f;
        Quaternion rot = room.Facing * Quaternion.Euler(-lie, 0f, 0f);
        Vector3 hipOffset = new Vector3(0f, -0.2f, 0f);
        r.transform.localRotation = rot;
        r.transform.localPosition = room.HipSitting - rot * hipOffset + Vector3.up * (lie / 82f) * 0.06f;

        // تتمطط وتهز راسها شوي
        float stretch = Mathf.Sin(Mathf.Clamp01(Mathf.InverseLerp(5.2f, 7.4f, t)) * Mathf.PI);
        float look = t > 7.4f ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(7.4f, 8.6f, t)) * 18f : 0f;
        r.Model.localRotation = Quaternion.Euler(8f + look - stretch * 8f, 0f, Mathf.Sin(t * 1.3f) * 2f * stretch);

        Quaternion legs = Quaternion.Euler(-90f + lie, 0f, 0f);
        r.LegL.localRotation = legs * Quaternion.Euler(0f, 0f, -3f);
        r.LegR.localRotation = legs * Quaternion.Euler(0f, 0f, 3f);

        // اليدين: جنبها، بعدين لفوق (تتمطط)، بعدين ماسكة الدفتر
        float hold = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(8f, 9f, t));
        Quaternion restL = Quaternion.Euler(-10f, 0f, -8f), restR = Quaternion.Euler(-10f, 0f, 8f);
        Quaternion upL = Quaternion.Euler(-165f, 0f, -18f), upR = Quaternion.Euler(-165f, 0f, 18f);
        Quaternion holdL = Quaternion.Euler(-50f, 0f, -12f), holdR = Quaternion.Euler(-50f, 0f, 12f);
        r.ArmL.localRotation = Quaternion.Slerp(Quaternion.Slerp(restL, upL, stretch), holdL, hold);
        r.ArmR.localRotation = Quaternion.Slerp(Quaternion.Slerp(restR, upR, stretch), holdR, hold);

        // عيونها مسكرة لين تصحى، وترمش مرتين
        bool closed = t < 2.4f || (t > 3f && t < 3.15f) || (t > 6f && t < 6.4f);
        r.SetEyesClosed(closed);
    }

    void AnimateNotebook(Vector3 lap)
    {
        Transform nb = room.Notebook;
        Quaternion lapRot = room.Facing * Quaternion.Euler(-22f, 0f, 0f);
        Quaternion floorRot = Quaternion.Euler(0f, 25f, 0f);

        // على الأرض، بعدين يضوي ويطير لحضنها
        float fly = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(7.4f, 8.8f, t));
        Vector3 p = Vector3.Lerp(room.NotebookFloor, lap, fly) + Vector3.up * Mathf.Sin(fly * Mathf.PI) * 0.35f;
        nb.localPosition = p;
        nb.localRotation = Quaternion.Slerp(floorRot, lapRot, fly) * Quaternion.Euler(0f, Mathf.Sin(fly * Mathf.PI) * 40f, 0f);

        float glow = Mathf.Clamp01(Mathf.InverseLerp(6.8f, 7.6f, t)) * (1f - Mathf.Clamp01(Mathf.InverseLerp(9.5f, 11f, t)));
        room.NotebookGlow.intensity = glow * 1.4f;
        if (!magicPlayed && t > 7f)
        {
            magicPlayed = true;
            gm.Sound.Play(SoundClips.Magic, 0.8f);
        }
    }

    // ---------- الدفتر الكبير على الشاشة: صفحات فيها رسومات كل مرحلة، تنقلب من اليسار لليمين (مثل الكتب العربية) ----------

    const float PagePixels = 1600f;
    RectTransform book;
    CanvasGroup bookGroup;
    Image bookDim;
    RawImage pageRight, pageLeft, flipLeft, flipRight;
    Text labelRight, labelLeft;
    readonly Dictionary<int, Texture2D> pageCache = new Dictionary<int, Texture2D>();

    int SlotLevel(int slot) => slot < memoryLevels.Count ? memoryLevels[slot] : -1;

    Texture2D Page(int slot)
    {
        if (!pageCache.TryGetValue(slot, out var tex)) pageCache[slot] = tex = DrawingMemory.PageFor(SlotLevel(slot), 100 + slot, PagePixels);
        return tex;
    }

    string Label(int slot)
    {
        int level = SlotLevel(slot);
        return level < 0 || level >= Levels.All.Count ? "" : ArabicText.Fix("المرحلة " + (level + 1) + " — " + Levels.All[level].title);
    }

    void BuildBook(Transform parent)
    {
        var dim = new GameObject("Book Dim", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(parent, false);
        var dimRt = dim.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = dimRt.offsetMax = Vector2.zero;
        dim.GetComponent<Image>().color = Color.clear;
        dim.GetComponent<Image>().raycastTarget = false;

        book = new GameObject("Book", typeof(RectTransform)).GetComponent<RectTransform>();
        book.SetParent(parent, false);
        book.sizeDelta = new Vector2(1280f, 840f);
        book.anchoredPosition = new Vector2(0f, 25f);
        bookGroup = book.gameObject.AddComponent<CanvasGroup>();
        bookDim = dim.GetComponent<Image>();

        Panel(book, "Cover", new Color(0.45f, 0.38f, 0.7f), Vector2.zero, new Vector2(1280f, 840f));
        pageLeft = PageImage(book, "Page Left", new Vector2(-300f, 0f), new Vector2(0.5f, 0.5f));
        pageRight = PageImage(book, "Page Right", new Vector2(300f, 0f), new Vector2(0.5f, 0.5f));
        // الصفحات اللي تنقلب: تلف حول السلك اللي في النص
        flipLeft = PageImage(book, "Flip Left", new Vector2(0f, 0f), new Vector2(1f, 0.5f));
        flipRight = PageImage(book, "Flip Right", new Vector2(0f, 0f), new Vector2(0f, 0.5f));
        flipLeft.enabled = flipRight.enabled = false;
        // السلك الحلزوني
        Panel(book, "Spine", new Color(0.18f, 0.17f, 0.22f), Vector2.zero, new Vector2(10f, 780f));
        for (float y = -360f; y <= 360f; y += 40f)
            Panel(book, "Ring", new Color(0.35f, 0.34f, 0.4f), new Vector2(0f, y), new Vector2(46f, 12f));

        labelLeft = BookLabel(pageLeft.rectTransform);
        labelRight = BookLabel(pageRight.rectTransform);
        book.localScale = Vector3.zero;
    }

    static Image Panel(Transform parent, string name, Color color, Vector2 pos, Vector2 size)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        img.color = color;
        img.raycastTarget = false;
        img.rectTransform.anchoredPosition = pos;
        img.rectTransform.sizeDelta = size;
        return img;
    }

    static RawImage PageImage(Transform parent, string name, Vector2 pos, Vector2 pivot)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        img.transform.SetParent(parent, false);
        img.raycastTarget = false;
        img.rectTransform.pivot = pivot;
        img.rectTransform.anchoredPosition = pos;
        img.rectTransform.sizeDelta = new Vector2(590f, 775f);
        return img;
    }

    static Text BookLabel(RectTransform page)
    {
        var t = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        t.transform.SetParent(page, false);
        t.font = ArabicText.Font;
        t.fontSize = ArabicText.Size(34);
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.2f, 0.2f, 0.28f);
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.rectTransform.anchoredPosition = new Vector2(0f, 330f);
        t.rectTransform.sizeDelta = new Vector2(560f, 60f);
        return t;
    }

    void SetSpread(int s)
    {
        // من اليمين: الصفحة الأولى يمين والثانية يسار
        pageRight.texture = Page(s * 2);
        pageLeft.texture = Page(s * 2 + 1);
        labelRight.text = Label(s * 2);
        labelLeft.text = Label(s * 2 + 1);
    }

    void AnimatePages()
    {
        float start = PagesStart - 0.8f, spreadsEnd = PagesStart + spreads * FlipEvery;
        if (t < start) return;

        // الدفتر يكبر ويطلع، وقبل "النهاية" يختفي
        float show = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, start + 0.6f, t)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(spreadsEnd + 3f, spreadsEnd + 3.6f, t)));
        book.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, show) * (show > 0.001f ? 1f : 0f);
        bookGroup.alpha = show;
        bookDim.color = new Color(0.05f, 0.03f, 0.1f, 0.45f * show);

        int spread = Mathf.Clamp(Mathf.FloorToInt((t - PagesStart) / FlipEvery), 0, spreads - 1);
        float since = t - (PagesStart + spread * FlipEvery);
        if (spread != shownSpread)
        {
            if (shownSpread >= 0)
            {
                // الصفحة اليسار القديمة تنقلب على اليمين
                flipLeft.texture = pageLeft.texture;
                flipRight.texture = Page(spread * 2);
                gm.Sound.Play(SynthAudio.PageTurn(), 0.6f);
                pageLeft.texture = Page(spread * 2 + 1);
                labelLeft.text = Label(spread * 2 + 1);
            }
            else SetSpread(spread);
            shownSpread = spread;
        }

        const float flipTime = 0.6f;
        bool flipping = spread > 0 && since < flipTime;
        flipLeft.enabled = flipping && since < flipTime * 0.5f;
        flipRight.enabled = flipping && since >= flipTime * 0.5f;
        if (flipping)
        {
            float k = since / flipTime;
            flipLeft.rectTransform.localScale = new Vector3(Mathf.Clamp01(1f - k * 2f), 1f, 1f);
            flipRight.rectTransform.localScale = new Vector3(Mathf.Clamp01(k * 2f - 1f), 1f, 1f);
        }
        else if (spread > 0 && pageRight.texture != Page(spread * 2))
        {
            pageRight.texture = Page(spread * 2);
            labelRight.text = Label(spread * 2);
        }
    }

    // ---------- النهاية ----------

    IEnumerator Finish()
    {
        finished = true;
        fade.color = new Color(1f, 0.98f, 0.94f, 1f);
        yield return null;
        RestoreLighting();
        Destroy(room.Root.gameObject);
        room = null;
        onFinished?.Invoke();
        yield return null;
        for (float k = 0f; k < 1f; k += Time.deltaTime / 0.8f)
        {
            fade.color = new Color(1f, 0.98f, 0.94f, 1f - k);
            caption.color = Color.clear;
            bigText.color = Color.clear;
            yield return null;
        }
        Destroy(canvas.gameObject);
        Destroy(this);
    }

    // ---------- الإضاءة والواجهة ----------

    void SetupLighting()
    {
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional && l.name != "Rim Light") { sun = l; break; }
        if (sun != null)
        {
            sunIntensity = sun.intensity;
            sunColor = sun.color;
            sunRot = sun.transform.rotation;
            sun.intensity = 1.25f;
            sun.color = new Color(1f, 0.9f, 0.75f);
            sun.transform.rotation = Quaternion.Euler(35f, -150f, 0f);   // من جهة الشباك
        }
        ambSky = RenderSettings.ambientSkyColor;
        ambEq = RenderSettings.ambientEquatorColor;
        ambGround = RenderSettings.ambientGroundColor;
        RenderSettings.ambientSkyColor = new Color(0.78f, 0.74f, 0.82f);
        RenderSettings.ambientEquatorColor = new Color(0.62f, 0.56f, 0.6f);
        RenderSettings.ambientGroundColor = new Color(0.32f, 0.28f, 0.3f);

        // نور الصبح من الشباك
        windowLight = new GameObject("Morning Light").AddComponent<Light>();
        windowLight.transform.SetParent(room.Root, false);
        windowLight.transform.localPosition = new Vector3(1.8f, 1.9f, 1.9f);
        windowLight.type = LightType.Point;
        windowLight.color = new Color(1f, 0.85f, 0.6f);
        windowLight.range = 5f;
        windowLight.intensity = 2.2f;
    }

    void RestoreLighting()
    {
        if (sun != null)
        {
            sun.intensity = sunIntensity;
            sun.color = sunColor;
            sun.transform.rotation = sunRot;
        }
        RenderSettings.ambientSkyColor = ambSky;
        RenderSettings.ambientEquatorColor = ambEq;
        RenderSettings.ambientGroundColor = ambGround;
    }

    void BuildOverlay()
    {
        var go = new GameObject("Ending UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 0.4f;
        canvas.sortingOrder = 100;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        fade = new GameObject("Fade", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        fade.transform.SetParent(go.transform, false);
        fade.rectTransform.anchorMin = Vector2.zero;
        fade.rectTransform.anchorMax = Vector2.one;
        fade.rectTransform.offsetMin = fade.rectTransform.offsetMax = Vector2.zero;
        fade.color = new Color(1f, 0.98f, 0.94f, 1f);
        fade.raycastTarget = false;
        BuildBook(go.transform);

        caption = NewText(go.transform, 50, new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(1700f, 90f));
        bigText = NewText(go.transform, 150, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1200f, 220f));
        bigText.text = ArabicText.Fix("النهاية");
        bigText.color = Color.clear;
        skip = SkipButton.Create(go.transform, Skip);
    }

    static Text NewText(Transform parent, int size, Vector2 anchor, Vector2 pos, Vector2 box)
    {
        var t = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        t.transform.SetParent(parent, false);
        t.font = ArabicText.Font;
        t.fontSize = ArabicText.Size(size);
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.gameObject.AddComponent<Outline>().effectColor = new Color(0.05f, 0.03f, 0.1f, 0.9f);
        var shadow = t.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(2f, -2f);
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        return t;
    }
}
