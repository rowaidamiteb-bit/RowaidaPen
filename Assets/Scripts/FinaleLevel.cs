using System.Collections.Generic;
using UnityEngine;

// الصفحة الأخيرة (المرحلة 60): قطرات حبر تعبي القلم، صور الذكريات تصير شريط فيلم جسر، صفحة عملاقة تنقلب وعلامة كتاب تثبتها،
// طيارة ورق تشيل رويدا فوق الوادي، أصحاب الرحلة من كل الفصول يشجعونها، بيت رويدا والألعاب النارية، وحبال صور كل المراحل بالسما
public static class FinaleFeatures
{
    public static void Build(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        if (!data.finale) return;
        if (data.inkDrops.y > data.inkDrops.x) InkDrops.Create(root, data.inkDrops, gm);
        if (data.photoBridge.y > data.photoBridge.x) PhotoBridge.Create(root, data.photoBridge, gm);
        if (data.turningPage.y > data.turningPage.x) TurningPage.Create(root, data.turningPage, gm);
        if (data.paperPlane.y > data.paperPlane.x) PaperPlane.Create(root, data.paperPlane, gm);
        if (data.friendsParade.y > data.friendsParade.x) FriendsParade.Create(root, data.friendsParade, gm);
        HomeHouse.Create(root, data.door, gm);
        MemoryGallery.Create(root, data, gm, rng);
    }

    static readonly Dictionary<Texture, Material> photoMats = new Dictionary<Texture, Material>();

    // مادة صورة (بدون إضاءة عشان الصورة تطلع واضحة)
    public static Material PhotoMat(Texture tex)
    {
        if (tex == null) return GameManager.Instance.Mat("Photo Blank", new Color(0.9f, 0.88f, 0.85f), 0.2f);
        if (photoMats.TryGetValue(tex, out Material m) && m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Photo " + tex.name };
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", new Color(1.05f, 1.05f, 1.05f));
        photoMats[tex] = m;
        return m;
    }

    public static Texture Thumb(int level) => Resources.Load<Texture2D>("LevelThumbs/level_" + level);

    // صورة بولارويد: إطار أبيض، الصورة، وشريط لاصق
    public static Transform Polaroid(Transform parent, Vector3 pos, Texture tex, float size, GameManager gm, string caption = null)
    {
        var p = new GameObject("Polaroid").transform;
        p.SetParent(parent, false);
        p.position = pos;
        LevelBuilder.Visual("Frame", PrimitiveType.Cube, p, new Vector3(0f, -0.08f * size, 0.02f), new Vector3(1.3f, 1.2f, 0.04f) * size, gm.Mat("Polaroid", new Color(0.98f, 0.97f, 0.94f), 0.3f), true);
        var q = LevelBuilder.Visual("Photo", PrimitiveType.Quad, p, new Vector3(0f, 0.04f * size, -0.005f), new Vector3(1.14f, 0.66f, 1f) * size, PhotoMat(tex), false);
        q.transform.localRotation = Quaternion.identity;
        if (caption != null) GameFeatures.Text(p, pos + new Vector3(0f, -0.48f * size, -0.03f), caption, 0.035f * size, new Color(0.55f, 0.35f, 0.55f));
        return p;
    }
}

// ---------- القلم قرب يخلص: قطرات حبر طايرة، خط يمر عليها يعبي القلم ----------
public class InkDrops : MonoBehaviour
{
    readonly List<Transform> drops = new List<Transform>();
    readonly List<Vector3> home = new List<Vector3>();
    readonly List<float> gotT = new List<float>();
    bool told;

    public static void Create(Transform root, Vector3 s, GameManager gm)
    {
        var go = new GameObject("Ink Drops");
        go.transform.SetParent(root, false);
        var d = go.AddComponent<InkDrops>();
        Material ink = gm.Mat("Ink Drop", new Color(0.12f, 0.16f, 0.5f), 0.95f, 0.1f);
        Material shine = gm.Mat("Ink Shine", new Color(1.6f, 1.6f, 1.8f), 0.9f);
        Material dark = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        const int n = 6;
        for (int i = 0; i < n; i++)
        {
            float u = i / (n - 1f);
            Vector3 p = new Vector3(Mathf.Lerp(s.x, s.y, u), s.z + Mathf.Sin(u * Mathf.PI) * 0.9f, 0f);
            var t = new GameObject("Drop").transform;
            t.SetParent(go.transform, false);
            t.position = p;
            LevelBuilder.Visual("Drop Body", PrimitiveType.Sphere, t, Vector3.zero, new Vector3(0.55f, 0.6f, 0.55f), ink, true);
            LevelBuilder.Visual("Drop Tip", PrimitiveType.Sphere, t, new Vector3(0f, 0.3f, 0f), new Vector3(0.28f, 0.45f, 0.28f), ink, false);
            LevelBuilder.Visual("Drop Shine", PrimitiveType.Sphere, t, new Vector3(-0.12f, 0.12f, -0.22f), new Vector3(0.1f, 0.16f, 0.05f), shine, false);
            for (int e = -1; e <= 1; e += 2) LevelBuilder.Visual("Drop Eye", PrimitiveType.Sphere, t, new Vector3(e * 0.1f, -0.02f, -0.26f), new Vector3(0.06f, 0.09f, 0.03f), shine, false);
            LevelBuilder.Visual("Drop Smile", PrimitiveType.Sphere, t, new Vector3(0f, -0.12f, -0.26f), new Vector3(0.08f, 0.035f, 0.02f), dark, false);
            d.drops.Add(t); d.home.Add(p); d.gotT.Add(-1f);
        }
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        if (!told && gm.IsPlaying && gm.Rowaida != null && gm.Rowaida.transform.position.x > -4.5f)
        {
            told = true;
            gm.UI.ShowMessage("", 4f, "حبر قلم رويدا يكاد ينفد! لنرسم خطًّا يمر على قطرات الحبر الطائرة ويملأ القلم");
        }
        for (int i = 0; i < drops.Count; i++)
        {
            if (gotT[i] < 0f)
            {
                drops[i].position = home[i] + new Vector3(0f, Mathf.Sin(t * 2.2f + i) * 0.12f, 0f);
                drops[i].localScale = new Vector3(1f + Mathf.Sin(t * 4f + i) * 0.05f, 1f - Mathf.Sin(t * 4f + i) * 0.05f, 1f);
                foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
                {
                    if (m.closed || m.path == null || GameFeatures.PathDistance(m.path, home[i]) > 0.6f) continue;
                    gotT[i] = t;
                    gm.RefundInk(6f);
                    LevelSfx.Play("plop", 0.8f, 0.9f + i * 0.08f);
                    LevelSfx.Play("pour", 0.4f, 1.4f);
                    var plus = GameFeatures.Text(null, home[i] + new Vector3(0f, 0.6f, -0.4f), "+6", 0.09f, new Color(0.3f, 0.45f, 1f));
                    var fx = plus.gameObject.AddComponent<FxParticle>();
                    fx.velocity = Vector3.up * 1.2f; fx.life = 1.2f;
                    break;
                }
            }
            else
            {
                // تطير للقلم وتختفي
                float k = (t - gotT[i]) / 0.6f;
                drops[i].position = Vector3.Lerp(home[i], home[i] + new Vector3(-2f, 4f, 0f), k);
                drops[i].localScale = Vector3.one * Mathf.Max(0f, 1f - k);
                if (k >= 1f) drops[i].gameObject.SetActive(false);
            }
        }
    }
}

// ---------- صور الذكريات فوق الحفرة: خط يمر على الثلاث صور، ينزلون ويصيرون شريط فيلم جسر ----------
public class PhotoBridge : MonoBehaviour
{
    float gapFrom, gapTo, top, settleT = -1f;
    readonly List<Transform> photos = new List<Transform>();
    readonly List<Vector3> home = new List<Vector3>();
    bool[] picked;
    Transform strip;
    BoxCollider col;
    bool holding, prompted;

    public static void Create(Transform root, Vector3 s, GameManager gm)
    {
        var go = new GameObject("Photo Bridge");
        go.transform.SetParent(root, false);
        var b = go.AddComponent<PhotoBridge>();
        b.gapFrom = s.x; b.gapTo = s.y; b.top = s.z;
        int[] levels = { 0, 20, 40 };
        string[] parts = { "الجزء الأول", "الجزء الثاني", "الجزء الثالث" };
        b.picked = new bool[3];
        for (int i = 0; i < 3; i++)
        {
            Vector3 p = new Vector3(Mathf.Lerp(b.gapFrom + 0.7f, b.gapTo - 0.7f, i / 2f), b.top + 2.7f + (i == 1 ? 0.7f : 0f), 0f);
            b.photos.Add(FinaleFeatures.Polaroid(go.transform, p, FinaleFeatures.Thumb(levels[i]), 1f, gm, parts[i]));
            b.home.Add(p);
        }
        // شريط الفيلم (يطلع لما الصور تنزل)
        b.strip = new GameObject("Film Strip").transform;
        b.strip.SetParent(go.transform, false);
        b.strip.position = new Vector3((b.gapFrom + b.gapTo) * 0.5f, b.top - 0.12f, 0f);
        float w = b.gapTo - b.gapFrom + 0.4f;
        var plank = LevelBuilder.Visual("Film", PrimitiveType.Cube, b.strip, Vector3.zero, new Vector3(w, 0.2f, 1.9f), gm.Mat("Film Black", new Color(0.12f, 0.11f, 0.13f), 0.6f), true);
        for (float x = -w * 0.5f + 0.2f; x < w * 0.5f; x += 0.35f)
            for (int side = -1; side <= 1; side += 2)
                LevelBuilder.Visual("Sprocket", PrimitiveType.Cube, b.strip, new Vector3(x, 0.105f, side * 0.82f), new Vector3(0.16f, 0.02f, 0.14f), gm.Mat("Badge White", Color.white, 0.4f), false);
        b.col = plank.AddComponent<BoxCollider>();
        b.col.enabled = false;
        b.strip.localScale = new Vector3(0.001f, 1f, 1f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        if (settleT < 0f)
        {
            for (int i = 0; i < photos.Count; i++)
            {
                photos[i].position = home[i] + new Vector3(0f, Mathf.Sin(t * 1.5f + i * 2f) * 0.15f, 0f);
                photos[i].localRotation = Quaternion.Euler(0f, Mathf.Sin(t + i) * 15f, Mathf.Sin(t * 0.8f + i) * 8f);
                photos[i].localScale = Vector3.one * (picked[i] ? 1.12f : 1f);
            }
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (m.closed || m.path == null) continue;
                for (int i = 0; i < photos.Count; i++)
                    if (!picked[i] && GameFeatures.PathDistance(m.path, home[i]) < 0.75f)
                    {
                        picked[i] = true;
                        LevelSfx.Play("chime", 0.6f, 1f + i * 0.15f);
                        Fx.Sparkle(home[i], new Color(1f, 0.85f, 0.95f), 12);
                    }
            }
            if (System.Array.TrueForAll(picked, p => p))
            {
                settleT = 0f;
                foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
                    if (!m.closed && m.path != null && GameFeatures.PathDistance(m.path, home[1]) < 1f) Destroy(m.gameObject);
                LevelSfx.Play("whoosh", 0.7f);
                gm.UI.ShowMessage("", 2.2f, "صارت الذكريات جسرًا!");
            }
        }
        else if (settleT < 1f)
        {
            settleT = Mathf.Min(1f, settleT + dt / 1.2f);
            float k = Mathf.SmoothStep(0f, 1f, settleT);
            strip.localScale = new Vector3(Mathf.Max(0.001f, k), 1f, 1f);
            for (int i = 0; i < photos.Count; i++)
            {
                Vector3 dest = new Vector3(Mathf.Lerp(gapFrom + 0.7f, gapTo - 0.7f, i / 2f), top + 0.02f, 0f);
                photos[i].position = Vector3.Lerp(home[i], dest, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.8f;
                photos[i].localRotation = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(90f, 0f, 0f), k);
                photos[i].localScale = Vector3.one * Mathf.Lerp(1.12f, 1.05f, k);
            }
            if (settleT >= 1f) { col.enabled = true; LevelSfx.Play("thud", 0.6f); }
        }
        bool wait = settleT < 1f && HoldHelper.Before(gapFrom);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "صور من الرحلة! لنرسم خطًّا يمر على الصور الثلاث فتصبح جسرًا"); }
        HoldHelper.Hold(ref holding, wait);
        if (settleT < 0f) LabFeatures.Show(gapFrom - 5f, gapFrom, new Vector3(gapFrom - 2.5f, top - 1f, 0f), new Vector3(gapTo + 1f, top + 4.6f, 0f));
    }
}

// ---------- صفحة عملاقة تنقلب فوق الحفرة: ارسمي علامة كتاب (خط عمودي يقطع الصفحة وهي نايمة) وتثبت جسر ----------
public class TurningPage : MonoBehaviour
{
    float gapFrom, gapTo, top, angle, t0, prevAngle;
    bool marked, holding, prompted, warned;
    Transform pivot;
    Rigidbody rb;

    public static void Create(Transform root, Vector3 s, GameManager gm)
    {
        var go = new GameObject("Turning Page");
        go.transform.SetParent(root, false);
        var p = go.AddComponent<TurningPage>();
        p.gapFrom = s.x; p.gapTo = s.y; p.top = s.z;
        p.t0 = Time.time;
        var pv = new GameObject("Page Pivot");
        pv.transform.SetParent(go.transform, false);
        pv.transform.position = new Vector3(p.gapTo, p.top - 0.06f, 0f);
        float w = p.gapTo - p.gapFrom + 0.25f;
        var sheet = LevelBuilder.Visual("Giant Page", PrimitiveType.Cube, pv.transform, new Vector3(-w * 0.5f, 0f, 0f), new Vector3(w, 0.1f, 2.6f), gm.Mat("Paper", Color.white, 0.08f), true);
        // سطور الدفتر وهامش أحمر
        for (int k = 0; k < 7; k++) LevelBuilder.Visual("Page Line", PrimitiveType.Cube, pv.transform, new Vector3(-w * 0.5f, 0.055f, -1.05f + k * 0.35f), new Vector3(w * 0.96f, 0.005f, 0.025f), gm.Mat("Rule Blue", new Color(0.55f, 0.7f, 0.95f), 0.2f), false);
        LevelBuilder.Visual("Page Margin", PrimitiveType.Cube, pv.transform, new Vector3(-w + 0.6f, 0.055f, 0f), new Vector3(0.03f, 0.005f, 2.5f), gm.Mat("Margin Red", new Color(0.95f, 0.45f, 0.5f), 0.2f), false);
        LevelBuilder.Visual("Page Back", PrimitiveType.Cube, pv.transform, new Vector3(-w * 0.5f, -0.055f, 0f), new Vector3(w * 0.98f, 0.005f, 2.55f), gm.Mat("Page Back", new Color(0.95f, 0.93f, 0.9f), 0.1f), false);
        sheet.AddComponent<BoxCollider>();
        p.rb = pv.AddComponent<Rigidbody>();
        p.rb.isKinematic = true;
        p.rb.interpolation = RigidbodyInterpolation.Interpolate;
        p.pivot = pv.transform;
        // كعب الدفتر (لولب كبير)
        for (int k = 0; k < 5; k++) LevelBuilder.Visual("Spine Ring", PrimitiveType.Cylinder, go.transform, new Vector3(p.gapTo, p.top, -1f + k * 0.5f), new Vector3(0.3f, 0.04f, 0.3f), gm.Mat("Spoon Steel", new Color(0.82f, 0.84f, 0.88f), 0.95f, 0.85f), false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;
        if (!marked)
        {
            // تنقلب: نايمة، تقوم وتنقلب يمين، ترجع
            float ph = Mathf.Repeat(t - t0, 4.6f);
            angle = ph < 1.4f ? 0f : ph < 2.3f ? Mathf.SmoothStep(0f, 180f, (ph - 1.4f) / 0.9f) : ph < 3.7f ? 180f : Mathf.SmoothStep(180f, 0f, (ph - 3.7f) / 0.9f);
            if (prevAngle < 1f && angle >= 1f) LevelSfx.Play("whoosh", 0.5f, 1.3f);
            prevAngle = angle;
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (m.closed || m.path == null) continue;
                // علامة كتاب: خط يقطع الصفحة من فوق لتحت وهي نايمة
                bool above = false, below = false;
                float cx = 0f; int n = 0;
                foreach (Vector3 p in m.path)
                {
                    if (p.x < gapFrom + 0.2f || p.x > gapTo - 0.2f) continue;
                    if (p.y > top + 0.6f) above = true;
                    if (p.y < top - 0.25f) below = true;
                    cx += p.x; n++;
                }
                if (!above || !below || n == 0) continue;
                if (angle > 4f)
                {
                    if (!warned) { warned = true; gm.UI.ShowMessage("", 2f, "لنرسم الخط وهي مستلقية على الحفرة!"); }
                    continue;
                }
                marked = true;
                angle = 0f;
                cx /= n;
                Destroy(m.gameObject);
                // شريطة ساتان حمرا معلقة على الصفحة
                Material satin = gm.Mat("Satin Red", new Color(0.85f, 0.15f, 0.25f), 0.85f);
                LevelBuilder.Visual("Bookmark", PrimitiveType.Cube, pivot, pivot.InverseTransformPoint(new Vector3(cx, top + 0.06f, 0f)), new Vector3(0.3f, 0.02f, 2.7f), satin, true);
                LevelBuilder.Visual("Bookmark Tail", PrimitiveType.Cube, transform, new Vector3(cx, top - 0.7f, -1.32f), new Vector3(0.3f, 1.4f, 0.02f), satin, false);
                for (int s = -1; s <= 1; s += 2) LevelBuilder.Visual("Bookmark Fork", PrimitiveType.Cube, transform, new Vector3(cx + s * 0.08f, top - 1.45f, -1.32f), new Vector3(0.14f, 0.3f, 0.02f), satin, false).transform.localRotation = Quaternion.Euler(0f, 0f, s * 25f);
                LevelSfx.Play("chime", 0.8f, 1.2f);
                gm.UI.ShowMessage("", 2.2f, "علامة الكتاب ثبتت الصفحة!");
                break;
            }
        }
        rb.MoveRotation(Quaternion.Euler(0f, 0f, -angle));
        bool wait = !marked && HoldHelper.Before(gapFrom);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 4f, "صفحة عملاقة تنقلب! لنرسم خطًّا من الأعلى إلى الأسفل يقطعها وهي مستلقية... فيصبح علامة كتاب تثبتها"); }
        HoldHelper.Hold(ref holding, wait);
        if (!marked) LabFeatures.Show(gapFrom - 5f, gapFrom, new Vector3(gapFrom - 2.5f, top - 2f, 0f), new Vector3(gapTo + w() + 0.5f, top + 5.6f, 0f));
    }

    float w() => gapTo - gapFrom + 0.25f;
}

// ---------- طيارة ورق: ارسمي شكل مسكر فوق المنصة، يصير طيارة ورق تنزل وتشيل رويدا فوق الوادي ----------
public class PaperPlane : MonoBehaviour
{
    float padX, landX, top, landTop, readyT = -1f, flyT = -1f, awayT = -1f, lastX;
    Transform plane;
    Rigidbody rb;
    readonly HashSet<InkMark> seen = new HashSet<InkMark>();
    bool holding, prompted;
    Vector3 seat;

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Paper Plane");
        go.transform.SetParent(root, false);
        var p = go.AddComponent<PaperPlane>();
        p.padX = s.x; p.landX = s.y; p.top = s.z; p.landTop = s.w;
        p.seat = new Vector3(p.padX + 0.5f, p.top - 0.08f, 0f);
        // منصة الإقلاع: علم ورقي وأسهم
        Material flag = gm.Mat("Pad Flag", new Color(1f, 0.55f, 0.65f), 0.3f);
        LevelBuilder.Visual("Pad Pole", PrimitiveType.Cylinder, go.transform, new Vector3(p.padX - 0.8f, p.top + 1.2f, 1.1f), new Vector3(0.08f, 1.2f, 0.08f), gm.Mat("Spoon Steel", new Color(0.82f, 0.84f, 0.88f), 0.95f, 0.85f), true);
        LevelBuilder.Visual("Pad Flag", PrimitiveType.Cube, go.transform, new Vector3(p.padX - 0.45f, p.top + 2.15f, 1.1f), new Vector3(0.7f, 0.45f, 0.02f), flag, false);
        for (int k = 0; k < 3; k++) LevelBuilder.Visual("Runway", PrimitiveType.Cube, go.transform, new Vector3(p.padX - 1.6f + k * 0.8f, p.top + 0.01f, 0f), new Vector3(0.45f, 0.01f, 0.12f), flag, false);
        // الطيارة (مخفية لين ترسمين)
        var pl = new GameObject("Plane");
        pl.transform.SetParent(go.transform, false);
        pl.transform.position = p.seat + new Vector3(0f, 6f, 0f);
        Material paper = gm.Mat("Plane Paper", new Color(0.98f, 0.96f, 0.9f), 0.25f);
        Material fold = gm.Mat("Plane Fold", new Color(0.85f, 0.83f, 0.8f), 0.25f);
        LevelBuilder.Visual("Plane Body", PrimitiveType.Cube, pl.transform, new Vector3(0f, 0f, 0f), new Vector3(2.4f, 0.16f, 1.7f), paper, true);
        for (int w = -1; w <= 1; w += 2)
        {
            var wing = LevelBuilder.Visual("Wing", PrimitiveType.Cube, pl.transform, new Vector3(-0.2f, 0.12f, w * 0.75f), new Vector3(2f, 0.04f, 1.1f), paper, true).transform;
            wing.localRotation = Quaternion.Euler(w * -10f, w * 12f, 0f);
        }
        LevelBuilder.Visual("Keel", PrimitiveType.Cube, pl.transform, new Vector3(0f, -0.2f, 0f), new Vector3(2.2f, 0.3f, 0.06f), fold, false);
        LevelBuilder.Visual("Nose Fold", PrimitiveType.Cube, pl.transform, new Vector3(1.15f, 0.05f, 0f), new Vector3(0.4f, 0.12f, 0.9f), fold, false).transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        for (int k = 0; k < 4; k++) LevelBuilder.Visual("Plane Stripe", PrimitiveType.Cube, pl.transform, new Vector3(-0.8f + k * 0.4f, 0.09f, -0.6f), new Vector3(0.12f, 0.005f, 0.3f), flag, false);
        var c = pl.AddComponent<BoxCollider>();
        c.center = Vector3.zero;
        c.size = new Vector3(2.4f, 0.16f, 1.7f);
        p.rb = pl.AddComponent<Rigidbody>();
        p.rb.isKinematic = true;
        p.rb.interpolation = RigidbodyInterpolation.Interpolate;
        p.plane = pl.transform;
        pl.SetActive(false);
    }

    Vector3 FlightPos(float k)
    {
        Vector3 a = seat, b = new Vector3(landX, landTop - 0.08f, 0f);
        return Vector3.Lerp(a, b, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 3f;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        RowaidaController r = gm.Rowaida;
        if (readyT < 0f)
        {
            foreach (InkMark m in GameFeatures.Fresh(seen))
            {
                if (!m.closed) continue;
                if (Vector2.Distance(GameFeatures.Center(m), seat + new Vector3(0f, 2.5f, 0f)) > 3.5f) continue;
                Fx.Sparkle(GameFeatures.Center(m), Color.white, 16);
                Destroy(m.gameObject);
                readyT = 0f;
                plane.gameObject.SetActive(true);
                LevelSfx.Play("rip", 0.5f, 1.4f);
                LevelSfx.Play("whoosh", 0.7f);
                gm.UI.ShowMessage("", 2.2f, "انطوت الورقة وصارت طائرة!");
                break;
            }
        }
        float vx = 0f;
        if (readyT >= 0f && readyT < 1f)
        {
            // تنزل وتلف وتوقف على المنصة
            readyT = Mathf.Min(1f, readyT + dt / 1.2f);
            float k = Mathf.SmoothStep(0f, 1f, readyT);
            rb.MovePosition(seat + new Vector3(Mathf.Sin(k * Mathf.PI) * 2f, (1f - k) * 6f, 0f));
            rb.MoveRotation(Quaternion.Euler(0f, 0f, (1f - k) * -25f));
        }
        bool onPlane = r != null && readyT >= 1f && Mathf.Abs(r.transform.position.x - plane.position.x) < 1.2f && r.transform.position.y > plane.position.y && r.transform.position.y - plane.position.y < 1.5f;
        bool atSeat = onPlane && flyT < 0f && Mathf.Abs(r.transform.position.x - seat.x) < 0.45f;
        if (readyT >= 1f && flyT < 0f && atSeat) { flyT = 0f; lastX = seat.x; LevelSfx.Play("whoosh", 0.8f, 0.8f); }
        if (flyT >= 0f && flyT < 1f)
        {
            flyT = Mathf.Min(1f, flyT + dt / 3.6f);
            float k = Mathf.SmoothStep(0f, 1f, flyT);
            Vector3 p = FlightPos(k);
            vx = (p.x - lastX) / Mathf.Max(dt, 1e-4f);
            lastX = p.x;
            rb.MovePosition(p);
            Vector3 ahead = FlightPos(Mathf.Min(1f, k + 0.02f)) - p;
            rb.MoveRotation(Quaternion.Euler(Mathf.Sin(t * 3f) * 4f, 0f, Mathf.Atan2(ahead.y, Mathf.Max(ahead.x, 0.01f)) * Mathf.Rad2Deg * 0.6f));
            if (Random.value < 0.4f) Fx.Sparkle(p + new Vector3(-1.2f, 0f, 0f), new Color(1f, 0.85f, 0.95f), 2);
            if (flyT >= 1f) { LevelSfx.Play("chime", 0.7f, 1.3f); awayT = 0f; }
        }
        else if (awayT >= 0f)
        {
            // بعد ما تنزل رويدا، تطير الطيارة بعيد وتلف بالسما
            awayT += dt;
            bool gone = r == null || r.transform.position.x > landX + 1.4f || awayT > 5f;
            if (gone)
            {
                Vector3 p = plane.position + new Vector3(4f, 2.5f, 1.5f) * dt;
                rb.MovePosition(p);
                rb.MoveRotation(Quaternion.Euler(awayT * 90f, 0f, 20f));
                if (p.y > landTop + 14f) plane.gameObject.SetActive(false);
            }
        }
        bool delivered = flyT >= 1f;
        bool wait = r != null && gm.IsPlaying && !delivered && (readyT < 1f ? HoldHelper.Before(padX + 0.9f) : flyT >= 0f && onPlane);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.8f, "وادٍ كبير! لنرسم شكلًا مغلقًا فوق المنصة... فينطوي ويصبح طائرة ورقية تحمل رويدا"); }
        HoldHelper.Hold(ref holding, wait);
        if (holding && r != null && flyT >= 0f) r.CarryX = vx;
        if (readyT < 1f) LabFeatures.Show(padX - 6f, padX + 1f, new Vector3(padX - 3f, top - 0.5f, 0f), new Vector3(padX + 3f, top + 5f, 0f));
    }
}

// ---------- أصحاب الرحلة: من كل فصل واحد، يطلعون ينطون ويشجعون لما تعدي رويدا ----------
public class FriendsParade : MonoBehaviour
{
    class Friend { public Transform t; public Vector3 home; public string sound; public float pitch; public float jumpT = -1f; public bool cheered; }
    readonly List<Friend> friends = new List<Friend>();
    float nextConfetti;

    public static void Create(Transform root, Vector3 s, GameManager gm)
    {
        var go = new GameObject("Friends Parade");
        go.transform.SetParent(root, false);
        var fp = go.AddComponent<FriendsParade>();
        float x0 = s.x, x1 = s.y, top = s.z;
        System.Func<Transform, GameManager, string>[] makers = { Crab, Ruler, Clock, Piano, Owl, Pot, Slime, Robot, Sheep, Cat };
        for (int i = 0; i < makers.Length; i++)
        {
            var t = new GameObject("Friend").transform;
            t.SetParent(go.transform, false);
            Vector3 p = new Vector3(Mathf.Lerp(x0, x1, i / (makers.Length - 1f)), top, i % 2 == 0 ? 1.5f : 2.6f);
            t.position = p;
            string snd = makers[i](t, gm);
            fp.friends.Add(new Friend { t = t, home = p, sound = snd, pitch = 1f });
        }
        // حبل أعلام فوق، وكتابة كبيرة
        Color[] flags = { new Color(1f, 0.5f, 0.6f), new Color(1f, 0.8f, 0.3f), new Color(0.5f, 0.8f, 1f), new Color(0.6f, 0.9f, 0.5f), new Color(0.8f, 0.6f, 1f) };
        float y = top + 4.6f;
        for (float x = x0 - 1f; x < x1 + 1f; x += 0.7f)
        {
            float sag = Mathf.Sin((x - x0 + 1f) / (x1 - x0 + 2f) * Mathf.PI) * 0.6f;
            var f = LevelBuilder.Visual("Pennant", PrimitiveType.Cube, go.transform, new Vector3(x, y - sag - 0.25f, 2f), new Vector3(0.38f, 0.45f, 0.02f), gm.Mat("Pennant " + Mathf.RoundToInt(x * 10f) % 5, flags[Mathf.Abs(Mathf.RoundToInt(x * 10f)) % 5], 0.3f), false).transform;
            f.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }
        GameFeatures.Text(go.transform, new Vector3((x0 + x1) * 0.5f, top + 5.6f, 2f), "شكرا يا رويدا على الرحلة!", 0.13f, new Color(0.9f, 0.35f, 0.55f));
    }

    // ---- الأصحاب ----
    static Material M(GameManager gm, string n, Color c, float sm = 0.4f) => gm.Mat(n, c, sm);

    static void Eyes(Transform t, Vector3 at, float s, GameManager gm)
    {
        for (int e = -1; e <= 1; e += 2)
        {
            LevelBuilder.Visual("Eye", PrimitiveType.Sphere, t, at + new Vector3(e * 0.13f * s, 0f, 0f), new Vector3(0.16f, 0.2f, 0.06f) * s, M(gm, "Badge White", Color.white), false);
            LevelBuilder.Visual("Pupil", PrimitiveType.Sphere, t, at + new Vector3(e * 0.13f * s, -0.02f * s, -0.03f), new Vector3(0.08f, 0.11f, 0.03f) * s, M(gm, "Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f), false);
        }
    }

    static string Crab(Transform t, GameManager gm)
    {
        Material red = M(gm, "Crab Red", new Color(0.95f, 0.35f, 0.3f));
        LevelBuilder.Visual("Crab Body", PrimitiveType.Sphere, t, new Vector3(0f, 0.4f, 0f), new Vector3(1f, 0.55f, 0.7f), red, true);
        for (int e = -1; e <= 1; e += 2)
        {
            LevelBuilder.Visual("Stalk", PrimitiveType.Cylinder, t, new Vector3(e * 0.18f, 0.8f, -0.1f), new Vector3(0.05f, 0.15f, 0.05f), red, false);
            LevelBuilder.Visual("Claw", PrimitiveType.Sphere, t, new Vector3(e * 0.7f, 0.65f, -0.1f), new Vector3(0.35f, 0.25f, 0.25f), red, true);
            for (int k = 0; k < 3; k++) LevelBuilder.Visual("Leg", PrimitiveType.Cube, t, new Vector3(e * (0.35f + k * 0.1f), 0.15f, -0.2f + k * 0.2f), new Vector3(0.06f, 0.3f, 0.06f), red, false).transform.localRotation = Quaternion.Euler(0f, 0f, e * 30f);
        }
        Eyes(t, new Vector3(0f, 0.98f, -0.12f), 1.2f, gm);
        return "clack";
    }

    static string Ruler(Transform t, GameManager gm)
    {
        LevelBuilder.Visual("Ruler", PrimitiveType.Cube, t, new Vector3(0f, 0.9f, 0f), new Vector3(0.45f, 1.8f, 0.12f), M(gm, "Ruler Yellow", new Color(1f, 0.85f, 0.3f)), true);
        for (int k = 0; k < 8; k++) LevelBuilder.Visual("Tick", PrimitiveType.Cube, t, new Vector3(-0.15f, 0.2f + k * 0.2f, -0.07f), new Vector3(k % 2 == 0 ? 0.14f : 0.08f, 0.02f, 0.01f), M(gm, "Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f), false);
        Eyes(t, new Vector3(0.05f, 1.45f, -0.08f), 1f, gm);
        return "clack";
    }

    static string Clock(Transform t, GameManager gm)
    {
        var face = LevelBuilder.Visual("Clock", PrimitiveType.Cylinder, t, new Vector3(0f, 0.75f, 0f), new Vector3(1f, 0.15f, 1f), M(gm, "Clock Blue", new Color(0.45f, 0.65f, 1f)), true).transform;
        face.localRotation = Quaternion.Euler(90f, 0f, 0f);
        LevelBuilder.Visual("Clock Face", PrimitiveType.Cylinder, t, new Vector3(0f, 0.75f, -0.16f), new Vector3(0.82f, 0.01f, 0.82f), M(gm, "Badge White", Color.white), false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        LevelBuilder.Visual("Hand", PrimitiveType.Cube, t, new Vector3(0f, 0.88f, -0.18f), new Vector3(0.04f, 0.26f, 0.01f), M(gm, "Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f), false);
        for (int e = -1; e <= 1; e += 2) LevelBuilder.Visual("Bell", PrimitiveType.Sphere, t, new Vector3(e * 0.35f, 1.3f, 0f), Vector3.one * 0.3f, M(gm, "Alarm Bell", new Color(1f, 0.8f, 0.3f), 0.8f), true);
        Eyes(t, new Vector3(0f, 0.65f, -0.2f), 0.9f, gm);
        return "tick";
    }

    static string Piano(Transform t, GameManager gm)
    {
        LevelBuilder.Visual("Piano", PrimitiveType.Cube, t, new Vector3(0f, 0.55f, 0f), new Vector3(1.2f, 0.9f, 0.6f), M(gm, "Piano Black", new Color(0.12f, 0.1f, 0.14f), 0.85f), true);
        for (int k = 0; k < 6; k++) LevelBuilder.Visual("Key", PrimitiveType.Cube, t, new Vector3(-0.5f + k * 0.2f, 0.35f, -0.32f), new Vector3(0.17f, 0.3f, 0.05f), M(gm, "Badge White", Color.white), false);
        Eyes(t, new Vector3(0f, 0.8f, -0.31f), 1.1f, gm);
        return "piano";
    }

    static string Owl(Transform t, GameManager gm)
    {
        Material brown = M(gm, "Owl Brown", new Color(0.6f, 0.42f, 0.28f));
        LevelBuilder.Visual("Owl", PrimitiveType.Sphere, t, new Vector3(0f, 0.6f, 0f), new Vector3(0.8f, 1.1f, 0.7f), brown, true);
        LevelBuilder.Visual("Belly", PrimitiveType.Sphere, t, new Vector3(0f, 0.45f, -0.25f), new Vector3(0.5f, 0.6f, 0.3f), M(gm, "Owl Belly", new Color(0.95f, 0.85f, 0.65f)), false);
        for (int e = -1; e <= 1; e += 2) LevelBuilder.Visual("Ear", PrimitiveType.Cube, t, new Vector3(e * 0.25f, 1.15f, 0f), new Vector3(0.15f, 0.25f, 0.1f), brown, false).transform.localRotation = Quaternion.Euler(0f, 0f, e * -20f);
        Eyes(t, new Vector3(0f, 0.85f, -0.32f), 1.6f, gm);
        return "owl";
    }

    static string Pot(Transform t, GameManager gm)
    {
        LevelBuilder.Visual("Pot", PrimitiveType.Cylinder, t, new Vector3(0f, 0.5f, 0f), new Vector3(1f, 0.5f, 1f), M(gm, "Pot Red", new Color(0.9f, 0.3f, 0.3f), 0.6f), true);
        LevelBuilder.Visual("Lid", PrimitiveType.Cylinder, t, new Vector3(0f, 1.05f, 0f), new Vector3(1.05f, 0.05f, 1.05f), M(gm, "Spoon Steel", new Color(0.82f, 0.84f, 0.88f), 0.95f), false);
        LevelBuilder.Visual("Lid Knob", PrimitiveType.Sphere, t, new Vector3(0f, 1.15f, 0f), Vector3.one * 0.18f, M(gm, "Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f), false);
        Eyes(t, new Vector3(0f, 0.6f, -0.5f), 1.1f, gm);
        return "boil";
    }

    static string Slime(Transform t, GameManager gm)
    {
        LevelBuilder.Visual("Slime", PrimitiveType.Sphere, t, new Vector3(0f, 0.45f, 0f), new Vector3(1f, 0.9f, 0.9f), LabFeatures2.Tint("Slime Friend", new Color(0.4f, 0.95f, 0.45f, 0.85f)), true);
        Eyes(t, new Vector3(0f, 0.6f, -0.45f), 1.4f, gm);
        return "gloop";
    }

    static string Robot(Transform t, GameManager gm)
    {
        LevelBuilder.Visual("Robot", PrimitiveType.Sphere, t, new Vector3(0f, 1.1f, 0f), new Vector3(0.9f, 0.8f, 0.8f), M(gm, "Robot White", new Color(0.92f, 0.94f, 0.97f), 0.6f), true);
        LevelBuilder.Visual("Screen", PrimitiveType.Sphere, t, new Vector3(0f, 1.15f, -0.3f), new Vector3(0.65f, 0.45f, 0.3f), M(gm, "Robot Screen", new Color(0.1f, 0.12f, 0.18f), 0.8f), false);
        for (int e = -1; e <= 1; e += 2) LevelBuilder.Visual("Eye", PrimitiveType.Sphere, t, new Vector3(e * 0.14f, 1.18f, -0.45f), new Vector3(0.12f, 0.16f, 0.05f), M(gm, "Robot Eye", new Color(0.4f, 2f, 2.2f), 0.5f), false);
        LevelBuilder.Visual("Propeller", PrimitiveType.Cube, t, new Vector3(0f, 1.55f, 0f), new Vector3(1.1f, 0.03f, 0.12f), M(gm, "Robot Orange", new Color(1f, 0.55f, 0.15f)), false);
        return "beep";
    }

    static string Sheep(Transform t, GameManager gm)
    {
        for (int k = 0; k < 5; k++) DreamFeatures.Puff(t, new Vector3(-0.3f + (k % 3) * 0.3f, 0.55f + (k / 3) * 0.25f, 0f), 0.5f, gm);
        LevelBuilder.Visual("Sheep Head", PrimitiveType.Sphere, t, new Vector3(0.5f, 0.7f, -0.1f), new Vector3(0.35f, 0.4f, 0.35f), M(gm, "Sheep Face", new Color(0.3f, 0.28f, 0.32f)), false);
        for (int k = 0; k < 4; k++) LevelBuilder.Visual("Leg", PrimitiveType.Cube, t, new Vector3(-0.3f + k * 0.2f, 0.18f, (k % 2) * 0.25f - 0.12f), new Vector3(0.08f, 0.35f, 0.08f), M(gm, "Sheep Face", new Color(0.3f, 0.28f, 0.32f)), false);
        return "baa";
    }

    static string Cat(Transform t, GameManager gm)
    {
        Material fur = M(gm, "Cute Fur", new Color(0.75f, 0.6f, 0.95f), 0.3f);
        LevelBuilder.Visual("Cat", PrimitiveType.Sphere, t, new Vector3(0f, 0.5f, 0f), new Vector3(0.8f, 0.9f, 0.7f), fur, true);
        for (int e = -1; e <= 1; e += 2) LevelBuilder.Visual("Cat Ear", PrimitiveType.Cube, t, new Vector3(e * 0.25f, 1f, 0f), new Vector3(0.22f, 0.22f, 0.15f), fur, false).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        LevelBuilder.Visual("Tail", PrimitiveType.Cylinder, t, new Vector3(0.45f, 0.5f, 0.2f), new Vector3(0.1f, 0.35f, 0.1f), fur, false).transform.localRotation = Quaternion.Euler(0f, 0f, -40f);
        Eyes(t, new Vector3(0f, 0.65f, -0.33f), 1.3f, gm);
        return "meow";
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        RowaidaController r = gm.Rowaida;
        foreach (Friend f in friends)
        {
            // يتمايلون بفرح، ولما تقرب رويدا ينطون
            if (r != null && !f.cheered && Mathf.Abs(r.transform.position.x - f.home.x) < 1.6f)
            {
                f.cheered = true; f.jumpT = 0f;
                LevelSfx.Play(f.sound, 0.7f);
                GameFeatures.Burst(f.home + Vector3.up * 1.5f, Color.HSVToRGB(Random.value, 0.6f, 1f), 10, 0.1f, 3f);
            }
            float jump = 0f;
            if (f.jumpT >= 0f)
            {
                f.jumpT += dt;
                jump = Mathf.Abs(Mathf.Sin(f.jumpT * 6f)) * 0.6f * Mathf.Max(0f, 1f - f.jumpT / 2.5f);
                if (f.jumpT > 2.5f) f.jumpT = -1f;
            }
            f.t.position = f.home + Vector3.up * jump;
            f.t.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 1.5f + f.home.x) * 15f, Mathf.Sin(t * 3f + f.home.x) * 5f);
        }
        if (r != null && friends.Count > 0 && r.transform.position.x > friends[0].home.x - 2f && r.transform.position.x < friends[friends.Count - 1].home.x + 2f && t > nextConfetti)
        {
            nextConfetti = t + 0.12f;
            var c = GameFeatures.Px("Confetti", null, r.transform.position + new Vector3(Random.Range(-4f, 4f), 6f, Random.Range(-1f, 2f)), new Vector3(0.12f, 0.06f, 0.02f), Color.HSVToRGB(Random.value, 0.65f, 1f));
            var fx = c.AddComponent<FxParticle>();
            fx.velocity = new Vector3(Random.Range(-0.5f, 0.5f), -1.6f, 0f); fx.flutter = 1.5f; fx.life = 4f; fx.spin = Random.insideUnitSphere * 300f;
        }
    }
}

// ---------- بيت رويدا حول الباب: جدار وسقف ومدخنة وشبابيك تضوي وورد، وألعاب نارية لما توصل ----------
public class HomeHouse : MonoBehaviour
{
    Vector3 door;
    float nextFirework;
    readonly List<Transform> smoke = new List<Transform>();

    public static void Create(Transform root, Vector3 door, GameManager gm)
    {
        var go = new GameObject("Home House");
        go.transform.SetParent(root, false);
        var h = go.AddComponent<HomeHouse>();
        h.door = door;
        Vector3 b = new Vector3(door.x, door.y, 0.9f);
        Material wall = gm.Mat("House Wall", new Color(1f, 0.86f, 0.82f), 0.3f);
        Material roof = gm.Mat("House Roof", new Color(0.85f, 0.35f, 0.4f), 0.4f);
        Material frame = gm.Mat("House Frame", new Color(0.98f, 0.97f, 0.94f), 0.3f);
        Material glow = gm.Mat("House Window", new Color(2.2f, 1.7f, 0.9f), 0.5f);
        LevelBuilder.Visual("House", PrimitiveType.Cube, go.transform, b + new Vector3(0f, 1.8f, 0.4f), new Vector3(5.6f, 3.6f, 0.6f), wall, true);
        for (int s = -1; s <= 1; s += 2)
        {
            var rf = LevelBuilder.Visual("Roof", PrimitiveType.Cube, go.transform, b + new Vector3(s * 1.55f, 4.55f, 0.3f), new Vector3(3.6f, 0.35f, 1.2f), roof, true).transform;
            rf.localRotation = Quaternion.Euler(0f, 0f, s * -32f);
            // شباك يضوي بستارة
            Vector3 w = b + new Vector3(s * 1.85f, 2.1f, -0.05f);
            LevelBuilder.Visual("Window Frame", PrimitiveType.Cube, go.transform, w, new Vector3(1.1f, 1.1f, 0.1f), frame, false);
            LevelBuilder.Visual("Window Light", PrimitiveType.Cube, go.transform, w + new Vector3(0f, 0f, -0.04f), new Vector3(0.9f, 0.9f, 0.04f), glow, false);
            LevelBuilder.Visual("Window Cross", PrimitiveType.Cube, go.transform, w + new Vector3(0f, 0f, -0.07f), new Vector3(0.06f, 0.9f, 0.02f), frame, false);
            LevelBuilder.Visual("Window Cross", PrimitiveType.Cube, go.transform, w + new Vector3(0f, 0f, -0.07f), new Vector3(0.9f, 0.06f, 0.02f), frame, false);
            LevelBuilder.Visual("Curtain", PrimitiveType.Cube, go.transform, w + new Vector3(s * -0.3f, 0.05f, -0.09f), new Vector3(0.25f, 0.85f, 0.02f), gm.Mat("Curtain Pink", new Color(1f, 0.6f, 0.75f), 0.3f), false);
            // صندوق ورد
            LevelBuilder.Visual("Flower Box", PrimitiveType.Cube, go.transform, w + new Vector3(0f, -0.65f, -0.15f), new Vector3(1.1f, 0.2f, 0.3f), gm.Mat("Shelf Wood", new Color(0.7f, 0.5f, 0.3f), 0.3f), false);
            for (int k = 0; k < 4; k++) LevelBuilder.Visual("Flower", PrimitiveType.Sphere, go.transform, w + new Vector3(-0.38f + k * 0.25f, -0.45f, -0.18f), Vector3.one * 0.18f, gm.Mat("Flower " + k, Color.HSVToRGB(k * 0.22f, 0.5f, 1f), 0.3f), false);
        }
        LevelBuilder.Visual("Chimney", PrimitiveType.Cube, go.transform, b + new Vector3(1.6f, 5.2f, 0.4f), new Vector3(0.6f, 1.2f, 0.6f), roof, true);
        for (int k = 0; k < 5; k++) h.smoke.Add(LevelBuilder.Visual("Smoke", PrimitiveType.Sphere, go.transform, b + new Vector3(1.6f, 6f, 0.4f), Vector3.one * 0.4f, gm.Mat("Dream Cloud", new Color(1f, 0.97f, 1f), 0.2f), false).transform);
        // لوحة "بيت رويدا" وصندوق بريد وحجار ممشى
        GameFeatures.Text(go.transform, b + new Vector3(0f, 3.85f, -0.05f), "بيت رويدا", 0.08f, new Color(0.75f, 0.3f, 0.45f));
        LevelBuilder.Visual("Mailbox", PrimitiveType.Cube, go.transform, b + new Vector3(-3.3f, 1.1f, -0.6f), new Vector3(0.5f, 0.4f, 0.4f), gm.Mat("Mailbox Blue", new Color(0.4f, 0.6f, 1f), 0.5f), true);
        LevelBuilder.Visual("Mailbox Post", PrimitiveType.Cube, go.transform, b + new Vector3(-3.3f, 0.45f, -0.6f), new Vector3(0.1f, 0.9f, 0.1f), gm.Mat("Shelf Wood", new Color(0.7f, 0.5f, 0.3f), 0.3f), false);
        for (int k = 0; k < 4; k++) LevelBuilder.Visual("Path Stone", PrimitiveType.Cylinder, go.transform, new Vector3(door.x - 2.6f + k * 0.6f, door.y + 0.02f, -0.6f + (k % 2) * 0.3f), new Vector3(0.4f, 0.02f, 0.3f), gm.Mat("Stone Grey", new Color(0.75f, 0.73f, 0.78f), 0.3f), false);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;
        for (int i = 0; i < smoke.Count; i++)
        {
            float k = Mathf.Repeat(t * 0.3f + i / (float)smoke.Count, 1f);
            smoke[i].position = new Vector3(door.x + 1.6f + k * 0.8f + Mathf.Sin(t + i) * 0.1f, door.y + 6f + k * 2f, 1.3f);
            smoke[i].localScale = Vector3.one * (0.3f + k * 0.8f);
        }
        // ألعاب نارية لما توصل رويدا قريب من البيت
        RowaidaController r = gm.Rowaida;
        if (r != null && r.transform.position.x > door.x - 8f && t > nextFirework)
        {
            nextFirework = t + Random.Range(0.6f, 1.2f);
            Vector3 at = new Vector3(door.x + Random.Range(-6f, 6f), door.y + Random.Range(6f, 9f), Random.Range(4f, 8f));
            Color c = Color.HSVToRGB(Random.value, 0.6f, 1f);
            Material m = gm.Mat("Firework " + Mathf.RoundToInt(c.r * 10f) + Mathf.RoundToInt(c.g * 10f), c * 2.2f, 0.4f);
            for (int k = 0; k < 18; k++)
            {
                var d = LevelBuilder.Visual("Spark", PrimitiveType.Sphere, null, at, Vector3.one * 0.14f, m, false);
                var fx = d.AddComponent<FxParticle>();
                fx.velocity = Random.onUnitSphere * 4f; fx.gravity = 2f; fx.drag = 1.2f; fx.life = 1.3f;
            }
            LevelSfx.Play("pop", 0.35f, Random.Range(0.6f, 1f));
        }
    }
}

// ---------- معرض الذكريات بالسما: حبال فيها صور كل المراحل بمشابك، تتمرجح مع الهوا ----------
public class MemoryGallery : MonoBehaviour
{
    readonly List<Transform> photos = new List<Transform>();
    readonly List<float> phase = new List<float>();

    public static void Create(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        var go = new GameObject("Memory Gallery");
        go.transform.SetParent(root, false);
        var g = go.AddComponent<MemoryGallery>();
        float minX = data.spawn.x - 6f, maxX = data.door.x + 8f;
        float[] heights = { 6.2f, 8.4f, 10.6f };
        float[] depth = { 6f, 7.5f, 9f };
        int total = Levels.All.Count - 1;
        int perRow = Mathf.CeilToInt(total / 3f);
        Material rope = gm.Mat("Rope", new Color(0.8f, 0.65f, 0.4f), 0.2f);
        Material pin = gm.Mat("Clothespin", new Color(0.85f, 0.65f, 0.45f), 0.3f);
        for (int row = 0; row < 3; row++)
        {
            float y = heights[row], z = depth[row];
            // الحبل بانحناءات بين الأعمدة
            int segs = 30;
            Vector3 prev = Vector3.zero;
            for (int k = 0; k <= segs; k++)
            {
                float u = k / (float)segs;
                float x = Mathf.Lerp(minX, maxX, u);
                Vector3 p = new Vector3(x, y - Mathf.Abs(Mathf.Sin(u * Mathf.PI * 4f)) * 0.5f, z);
                if (k > 0)
                {
                    var s = LevelBuilder.Visual("Gallery Rope", PrimitiveType.Cylinder, go.transform, Vector3.zero, Vector3.one, rope, false).transform;
                    LabFeatures2.Stretch(s, prev, p, 0.03f);
                }
                prev = p;
            }
            for (int i = 0; i < perRow; i++)
            {
                int level = row * perRow + i;
                if (level >= total) break;
                float u = (i + 0.5f) / perRow;
                float x = Mathf.Lerp(minX, maxX, u);
                Vector3 p = new Vector3(x, y - Mathf.Abs(Mathf.Sin(u * Mathf.PI * 4f)) * 0.5f - 0.75f, z);
                var ph = FinaleFeatures.Polaroid(go.transform, p, FinaleFeatures.Thumb(level), 1.1f, gm, (level + 1).ToString());
                LevelBuilder.Visual("Pin", PrimitiveType.Cube, ph, new Vector3(0f, 0.62f, -0.04f), new Vector3(0.12f, 0.3f, 0.08f), pin, false);
                g.photos.Add(ph);
                g.phase.Add((float)rng.NextDouble() * 6f);
            }
        }
    }

    void Update()
    {
        float t = Time.time;
        for (int i = 0; i < photos.Count; i++)
            photos[i].localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.6f + phase[i]) * 12f, Mathf.Sin(t * 0.9f + phase[i]) * 6f);
    }
}
