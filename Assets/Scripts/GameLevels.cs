using System.Collections.Generic;
using UnityEngine;

// الجزء الثالث — فصل الألعاب والبرمجة (53 لين 56): عالم البكسل (خطوطك تصير مكعبات، عملات، وحش بكسل، لعبة تكسير الطوب)،
// الكود (أسهم تبرمج المصعد، حشرة باق تمسكينها، حلقة تكرار تقطعينها)، لعبة الأرقام، والزعيم الأخير (فيروس، إعلانات، زر الحفظ)
public static class GameFeatures
{
    public static void Build(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        if (data.pixelInk) root.gameObject.AddComponent<PixelInk>();
        foreach (Vector4 s in data.coinGates) CoinGate.Create(root, s, gm);
        foreach (Vector4 s in data.pixelCreatures) PixelCreature.Create(root, s, gm);
        foreach (Vector4 s in data.breakouts) Breakout.Create(root, s, gm);
        foreach (Vector4 s in data.commandLifts) CommandLift.Create(root, s, gm);
        foreach (Vector4 s in data.bugCatches) BugCatch.Create(root, s, gm);
        foreach (Vector4 s in data.loopDoors) LoopDoor.Create(root, s, gm);
        foreach (Vector4 s in data.tallyGates) TallyGate.Create(root, s, gm);
        foreach (Vector4 s in data.numberScales) NumberScale.Create(root, s, gm);
        foreach (Vector4 s in data.calculators) CalcRamp.Create(root, s, gm);
        if (data.virusBoss.z > 0f) VirusBoss.Create(root, data.virusBoss, gm);
        foreach (Vector2 s in data.popups) PopupAd.Create(root, s, gm);
        foreach (Vector2 s in data.saveButtons) SaveButton.Create(root, s, gm);
        if (data.gameStyle > 0) GameScenery.Create(root, data, gm, rng);
    }

    // مكعب بكسل بلون
    public static GameObject Px(string name, Transform parent, Vector3 pos, Vector3 scale, Color c, bool shadows = false)
    {
        GameManager gm = GameManager.Instance;
        return LevelBuilder.Visual(name, PrimitiveType.Cube, parent, pos, scale, gm.Mat("Px " + ColorUtility.ToHtmlStringRGB(c), c, 0.15f), shadows);
    }

    // مادة كتابة تحترم العمق (عشان الكتابة ما تطلع فوق الأشياء اللي قدامها)
    static Material textMat;
    public static Material TextMat
    {
        get
        {
            if (textMat == null)
            {
                textMat = new Material(ArabicText.Font.material) { name = "Text Depth" };
                textMat.SetFloat("unity_GUIZTestMode", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
            }
            return textMat;
        }
    }

    // كتابة بالعالم (العربي يتصلح تلقائيا)
    public static TextMesh Text(Transform parent, Vector3 pos, string text, float size, Color c, TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        var tm = new GameObject("Text").AddComponent<TextMesh>();
        tm.transform.SetParent(parent, false);
        tm.transform.position = pos;
        tm.font = ArabicText.Font;
        tm.GetComponent<MeshRenderer>().sharedMaterial = TextMat;
        tm.text = Fix(text);
        tm.fontSize = 64;
        tm.characterSize = size;
        tm.anchor = anchor;
        tm.alignment = TextAlignment.Center;
        tm.color = c;
        return tm;
    }

    public static void SetText(TextMesh tm, string text) => tm.text = Fix(text);

    // نصلح العربي بس (الإنجليزي والأرقام تبقى زي ما هي، لا تنقلب)
    static string Fix(string text)
    {
        foreach (char ch in text) if (ch >= '؀' && ch <= 'ۿ') return ArabicText.Fix(text);
        return text;
    }

    // أقرب مسافة من نقطة لخط مرسوم
    public static float PathDistance(Vector3[] path, Vector2 p)
    {
        float best = 999f;
        for (int i = 1; i < path.Length; i++)
        {
            Vector2 a = path[i - 1], b = path[i], ab = b - a;
            float k = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            best = Mathf.Min(best, Vector2.Distance(p, a + ab * k));
        }
        return best;
    }

    // نقطة داخل شكل مسكر؟ (نقاط الشكل بالعالم)
    public static bool Inside(InkMark m, Vector2 p)
    {
        if (m.outline == null || m.outline.Length < 3) return false;
        bool inside = false;
        int n = m.outline.Length;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector2 a = m.transform.TransformPoint(m.outline[i]), b = m.transform.TransformPoint(m.outline[j]);
            if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
        }
        return inside;
    }

    public static Vector3 Center(InkMark m)
    {
        var r = m.GetComponent<MeshRenderer>();
        return r != null ? r.bounds.center : m.transform.position;
    }

    // خطوط ورسمات جديدة (أول مرة نشوفها)
    public static List<InkMark> Fresh(HashSet<InkMark> seen)
    {
        var list = new List<InkMark>();
        foreach (InkMark m in Object.FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            if (!m.beingEaten && seen.Add(m)) list.Add(m);
        return list;
    }

    public static void Burst(Vector3 at, Color c, int n, float size = 0.18f, float speed = 4f)
    {
        for (int k = 0; k < n; k++)
        {
            var d = Px("Bit", null, at + Random.insideUnitSphere * 0.2f, Vector3.one * size, c);
            var fx = d.AddComponent<FxParticle>();
            fx.velocity = new Vector3(Random.Range(-speed, speed), Random.Range(speed * 0.4f, speed * 1.3f), Random.Range(-1f, 1f));
            fx.gravity = 12f; fx.life = 0.7f; fx.spin = Random.insideUnitSphere * 400f;
        }
    }
}

// ---------- خطوطك تصير بكسل: كل رسمة تنبني مكعبات ملونة مربع مربع ----------
public class PixelInk : MonoBehaviour
{
    const float G = 0.3f;
    readonly HashSet<InkMark> seen = new HashSet<InkMark>();
    readonly List<Transform> pop = new List<Transform>();
    readonly List<float> popAt = new List<float>();
    Material hide;
    int next;
    static readonly Color[] palette = { new Color(0.95f, 0.25f, 0.3f), new Color(1f, 0.75f, 0.1f), new Color(0.3f, 0.8f, 0.3f), new Color(0.25f, 0.55f, 1f), new Color(0.7f, 0.35f, 0.95f) };

    void Start()
    {
        hide = LabFeatures2.Tint("Hidden Ink", new Color(1f, 1f, 1f, 0f));
    }

    void Update()
    {
        float t = Time.time;
        foreach (InkMark m in GameFeatures.Fresh(seen))
        {
            var mr = m.GetComponent<MeshRenderer>();
            if (mr == null) continue;
            mr.sharedMaterial = hide;
            foreach (Renderer cr in m.GetComponentsInChildren<Renderer>()) if (cr != mr) cr.enabled = false;
            Color c = palette[next++ % palette.Length];
            // نقاط الرسمة (محلية) وبينها نقاط زيادة عشان ما يصير فراغ
            var pts = new List<Vector3>();
            if (m.closed && m.outline != null) { pts.AddRange(m.outline); pts.Add(m.outline[0]); }
            else if (m.path != null) foreach (Vector3 p in m.path) pts.Add(m.transform.InverseTransformPoint(p));
            var cells = new List<Vector2Int>();
            var set = new HashSet<Vector2Int>();
            for (int i = 1; i < pts.Count; i++)
            {
                int steps = Mathf.CeilToInt(Vector3.Distance(pts[i - 1], pts[i]) / (G * 0.4f)) + 1;
                for (int s = 0; s <= steps; s++)
                {
                    Vector3 p = Vector3.Lerp(pts[i - 1], pts[i], s / (float)steps);
                    var cell = new Vector2Int(Mathf.RoundToInt(p.x / G), Mathf.RoundToInt(p.y / G));
                    if (set.Add(cell)) cells.Add(cell);
                }
            }
            for (int i = 0; i < cells.Count; i++)
            {
                Color cc = (cells[i].x + cells[i].y) % 2 == 0 ? c : c * 0.75f;
                var cube = GameFeatures.Px("Pixel", m.transform, Vector3.zero, Vector3.zero, cc).transform;
                cube.localPosition = new Vector3(cells[i].x * G, cells[i].y * G, 0f);
                pop.Add(cube);
                popAt.Add(t + i * 0.012f);
            }
            LevelSfx.Play("blip", 0.35f, 1.2f);
        }
        for (int i = pop.Count - 1; i >= 0; i--)
        {
            if (pop[i] == null) { pop.RemoveAt(i); popAt.RemoveAt(i); continue; }
            float k = Mathf.Clamp01((t - popAt[i]) / 0.15f);
            float s = k <= 0f ? 0f : G * (k < 1f ? 1.25f - Mathf.Abs(k - 0.6f) * 0.6f : 0.98f);
            pop[i].localScale = Vector3.one * s;
            if (k >= 1f) { pop[i].localScale = Vector3.one * G * 0.98f; pop.RemoveAt(i); popAt.RemoveAt(i); }
        }
    }
}

// ---------- عملات طايرة: خط يمر عليها يجمعها، ولما تجمعين الكل ينكسر جدار الطوب ----------
public class CoinGate : MonoBehaviour
{
    float x0, x1, top, wallX;
    readonly List<Transform> coins = new List<Transform>();
    readonly List<Vector3> home = new List<Vector3>();
    bool[] got;
    float[] gotT;
    int count;
    readonly List<Transform> bricks = new List<Transform>();
    BoxCollider col;
    TextMesh counter;
    bool open, holding, prompted;

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Coin Gate");
        go.transform.SetParent(root, false);
        var g = go.AddComponent<CoinGate>();
        g.x0 = s.x; g.x1 = s.y; g.top = s.z; g.wallX = s.w;
        const int n = 5;
        g.got = new bool[n]; g.gotT = new float[n];
        Color gold = new Color(1f, 0.8f, 0.15f), goldDark = new Color(0.85f, 0.55f, 0.05f);
        for (int i = 0; i < n; i++)
        {
            float u = i / (n - 1f);
            Vector3 p = new Vector3(Mathf.Lerp(g.x0, g.x1, u), g.top + 2.7f + Mathf.Sin(u * Mathf.PI) * 1.4f, 0f);
            var c = new GameObject("Coin").transform;
            c.SetParent(go.transform, false);
            c.position = p;
            GameFeatures.Px("Coin Face", c, Vector3.zero, new Vector3(0.6f, 0.75f, 0.15f), gold);
            GameFeatures.Px("Coin Edge", c, new Vector3(0f, 0f, 0.01f), new Vector3(0.75f, 0.55f, 0.13f), gold);
            GameFeatures.Px("Coin Mark", c, new Vector3(0f, 0f, -0.08f), new Vector3(0.14f, 0.42f, 0.03f), goldDark);
            g.coins.Add(c); g.home.Add(p);
        }
        // جدار طوب و"؟" فوقه
        Color brick = new Color(0.8f, 0.35f, 0.15f), mortar = new Color(0.55f, 0.22f, 0.1f);
        var wall = new GameObject("Brick Wall");
        wall.transform.SetParent(go.transform, false);
        wall.transform.position = new Vector3(g.wallX, g.top, 0f);
        for (int row = 0; row < 7; row++)
            for (int k = 0; k < 2; k++)
            {
                float off = row % 2 == 0 ? 0f : 0.2f;
                var b = GameFeatures.Px("Brick", wall.transform, Vector3.zero, new Vector3(0.42f, 0.42f, 1.8f), (row + k) % 2 == 0 ? brick : mortar, true).transform;
                b.localPosition = new Vector3(-0.22f + k * 0.44f + off * 0f, 0.23f + row * 0.45f, 0f);
                g.bricks.Add(b);
            }
        var q = GameFeatures.Px("Question Block", wall.transform, Vector3.zero, new Vector3(0.9f, 0.9f, 0.9f), gold, true).transform;
        q.localPosition = new Vector3(0f, 3.65f, 0f);
        g.bricks.Add(q);
        GameFeatures.Text(q, q.position + new Vector3(0f, 0.02f, -0.47f), "?", 0.12f, new Color(0.6f, 0.3f, 0.05f));
        g.col = wall.AddComponent<BoxCollider>();
        g.col.center = new Vector3(0f, 1.6f, 0f);
        g.col.size = new Vector3(0.9f, 3.2f, 2f);
        g.counter = GameFeatures.Text(go.transform, new Vector3(g.wallX, g.top + 4.75f, -0.4f), "0 / 5", 0.07f, new Color(1f, 0.9f, 0.3f));
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        for (int i = 0; i < coins.Count; i++)
        {
            if (!got[i])
            {
                coins[i].position = home[i] + Vector3.up * Mathf.Sin(t * 3f + i) * 0.08f;
                coins[i].localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 4f + i) * 70f, 0f);
                continue;
            }
            // طارت لفوق واختفت
            float k = t - gotT[i];
            coins[i].position = home[i] + Vector3.up * k * 4f;
            coins[i].localRotation = Quaternion.Euler(0f, k * 1200f, 0f);
            coins[i].localScale = Vector3.one * Mathf.Max(0f, 1f - k * 2f);
        }
        if (!open)
        {
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (m.closed || m.path == null || m.path.Length < 2) continue;
                for (int i = 0; i < coins.Count; i++)
                {
                    if (got[i] || GameFeatures.PathDistance(m.path, home[i]) > 0.6f) continue;
                    got[i] = true; gotT[i] = t; count++;
                    LevelSfx.Play("coin", 0.7f, 1f + count * 0.06f);
                    Fx.Sparkle(home[i], new Color(1f, 0.9f, 0.3f), 10);
                    GameFeatures.SetText(counter, count + " / 5");
                }
            }
            if (count >= coins.Count)
            {
                open = true;
                col.enabled = false;
                foreach (Transform b in bricks)
                {
                    b.SetParent(null, true);
                    var fx = b.gameObject.AddComponent<FxParticle>();
                    fx.velocity = new Vector3(Random.Range(-1f, 4f), Random.Range(3f, 7f), Random.Range(-2f, 2f));
                    fx.gravity = 14f; fx.life = 1.2f; fx.spin = Random.insideUnitSphere * 300f;
                }
                LevelSfx.Play("powerup", 0.8f);
                GameFeatures.SetText(counter, "!");
                gm.UI.ShowMessage("", 2f, "جمعتي كل العملات! الجدار انكسر");
            }
        }
        bool wait = !open && HoldHelper.Before(wallX - 0.5f);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "جدار طوب! ارسمي خط يمر على العملات الخمس ويجمعها"); }
        HoldHelper.Hold(ref holding, wait);
        if (!open) LabFeatures.Show(x0 - 4f, wallX, new Vector3(x0 - 1f, top - 0.5f, 0f), new Vector3(wallX + 1f, top + 5.2f, 0f));
    }
}

// ---------- وحش بكسل يمشي رايح جاي: ارسمي صندوق (شكل مسكر) يطيح عليه ويحبسه ----------
public class PixelCreature : MonoBehaviour
{
    float x0, x1, top, x, dir = 1f, trapT = -1f;
    InkMark cage;
    Transform body, footL, footR;
    bool holding, prompted;

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Pixel Creature");
        go.transform.SetParent(root, false);
        var c = go.AddComponent<PixelCreature>();
        c.x0 = s.x; c.x1 = s.y; c.top = s.z; c.x = (c.x0 + c.x1) * 0.5f;
        c.body = new GameObject("Body").transform;
        c.body.SetParent(go.transform, false);
        Color brown = new Color(0.6f, 0.32f, 0.15f), dark = new Color(0.25f, 0.12f, 0.06f), skin = new Color(0.95f, 0.8f, 0.6f);
        // راس فطر بكسل
        int[] widths = { 4, 6, 6, 6 };
        for (int row = 0; row < widths.Length; row++)
            for (int k = 0; k < widths[row]; k++)
                GameFeatures.Px("Head", c.body, new Vector3((k - (widths[row] - 1) * 0.5f) * 0.2f, 1.25f - row * 0.2f, 0f), new Vector3(0.2f, 0.2f, 0.8f), brown, true);
        for (int k = 0; k < 4; k++) GameFeatures.Px("Face", c.body, new Vector3((k - 1.5f) * 0.2f, 0.45f, 0f), new Vector3(0.2f, 0.2f, 0.6f), skin);
        for (int e = -1; e <= 1; e += 2)
        {
            GameFeatures.Px("Eye", c.body, new Vector3(e * 0.2f, 0.85f, -0.42f), new Vector3(0.16f, 0.26f, 0.04f), Color.white);
            GameFeatures.Px("Pupil", c.body, new Vector3(e * 0.16f, 0.82f, -0.45f), new Vector3(0.08f, 0.14f, 0.04f), Color.black);
            GameFeatures.Px("Brow", c.body, new Vector3(e * 0.22f, 1.04f, -0.45f), new Vector3(0.22f, 0.06f, 0.04f), Color.black).transform.localRotation = Quaternion.Euler(0f, 0f, e * -25f);
        }
        c.footL = GameFeatures.Px("Foot", c.body, new Vector3(-0.22f, 0.12f, 0f), new Vector3(0.3f, 0.2f, 0.5f), dark).transform;
        c.footR = GameFeatures.Px("Foot", c.body, new Vector3(0.22f, 0.12f, 0f), new Vector3(0.3f, 0.2f, 0.5f), dark).transform;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        RowaidaController r = gm.Rowaida;
        if (trapT < 0f)
        {
            x += dir * 1.3f * dt;
            if (x > x1) { x = x1; dir = -1f; }
            if (x < x0) { x = x0; dir = 1f; }
            body.position = new Vector3(x, top, 0f);
            body.localRotation = Quaternion.Euler(0f, dir > 0f ? 0f : 180f, 0f);
            float step = Mathf.Sin(t * 12f) * 0.06f;
            footL.localPosition = new Vector3(-0.22f, 0.12f + Mathf.Max(0f, step), 0f);
            footR.localPosition = new Vector3(0.22f, 0.12f + Mathf.Max(0f, -step), 0f);
            // يصدم رويدا
            if (r != null && !r.IsKnocked && Mathf.Abs(r.transform.position.x - x) < 0.75f && r.transform.position.y - top < 1.6f)
            {
                r.Knockback(-1f);
                LevelSfx.Play("pxbuzz", 0.6f, 1.3f);
            }
            // صندوق طاح عليه؟
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (!m.closed || m.beingEaten) continue;
                Vector3 c = GameFeatures.Center(m);
                if (Mathf.Abs(c.x - x) > 0.85f || c.y - top > 1.8f || c.y < top - 0.5f) continue;
                cage = m;
                m.beingEaten = true;
                var rb = m.GetComponent<Rigidbody>();
                if (rb != null) { rb.isKinematic = true; rb.linearVelocity = Vector3.zero; }
                foreach (Collider cc in m.GetComponentsInChildren<Collider>()) cc.enabled = false;
                m.transform.position += new Vector3(x - c.x, top + 0.7f - c.y, 0f);
                trapT = 0f;
                LevelSfx.Play("snap", 0.8f);
                gm.UI.ShowMessage("", 2f, "انحبس الوحش!");
                break;
            }
        }
        else
        {
            trapT += dt;
            body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 30f) * 8f);
            if (trapT > 1f)
            {
                // يختفي ويطلع منه عملة
                float k = Mathf.Clamp01((trapT - 1f) / 0.4f);
                body.localScale = Vector3.one * (1f - k);
                if (cage != null) cage.transform.localScale = Vector3.one * (1f - k);
                if (k >= 1f && cage != null)
                {
                    Destroy(cage.gameObject);
                    cage = null;
                    LevelSfx.Play("coin", 0.8f);
                    Fx.Sparkle(new Vector3(x, top + 1f, 0f), new Color(1f, 0.9f, 0.3f), 18);
                }
            }
        }
        bool wait = trapT < 0f && HoldHelper.Before(x0);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.6f, "وحش بكسل! ارسمي صندوق (شكل مسكر) يطيح عليه ويحبسه"); }
        HoldHelper.Hold(ref holding, wait);
        if (trapT < 0f) LabFeatures.Show(x0 - 4f, x1, new Vector3(x0 - 1.5f, top - 0.5f, 0f), new Vector3(x1 + 1f, top + 4f, 0f));
    }
}

// ---------- لعبة تكسير الطوب: كورة تنط، وخطك فوق الحفرة مضرب يردها (ويصير جسر)، تكسر الطوب اللي سادّ الطريق ----------
public class Breakout : MonoBehaviour
{
    const float Speed = 9f, R = 0.18f;
    float pitFrom, pitTo, top, wallX, left, right, ceil, lostT = 0.6f;
    Vector2 pos, vel, launch;
    Transform ball;
    readonly List<Transform> bricks = new List<Transform>();
    readonly List<Rect> rects = new List<Rect>();
    bool holding, prompted;

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Breakout");
        go.transform.SetParent(root, false);
        var b = go.AddComponent<Breakout>();
        b.pitFrom = s.x; b.pitTo = s.y; b.top = s.z; b.wallX = s.w;
        b.left = b.pitFrom - 2f; b.right = b.wallX + 1f; b.ceil = b.top + 5.5f;
        b.launch = new Vector2(b.pitFrom - 1.6f, b.top + 4.2f);
        Color[] rows = { new Color(0.95f, 0.3f, 0.3f), new Color(1f, 0.6f, 0.15f), new Color(1f, 0.85f, 0.15f), new Color(0.35f, 0.85f, 0.35f), new Color(0.3f, 0.6f, 1f) };
        for (int row = 0; row < rows.Length; row++)
            for (int k = 0; k < 2; k++)
            {
                Vector3 p = new Vector3(b.wallX - 0.35f + k * 0.7f, b.top + 0.25f + row * 0.5f, 0f);
                var br = GameFeatures.Px("Brick", go.transform, p, new Vector3(0.66f, 0.46f, 1.6f), rows[row], true);
                br.AddComponent<BoxCollider>();
                b.bricks.Add(br.transform);
                b.rects.Add(new Rect(p.x - 0.33f, p.y - 0.23f, 0.66f, 0.46f));
            }
        // المدفع اللي يطلق الكورة
        var cannon = GameFeatures.Px("Launcher", go.transform, new Vector3(b.launch.x - 0.3f, b.launch.y + 0.1f, 0.2f), new Vector3(0.9f, 0.5f, 0.6f), new Color(0.3f, 0.3f, 0.35f), true).transform;
        cannon.localRotation = Quaternion.Euler(0f, 0f, -30f);
        GameFeatures.Px("Launcher Stand", go.transform, new Vector3(b.launch.x - 0.5f, (b.top + b.launch.y) * 0.5f, 0.6f), new Vector3(0.2f, b.launch.y - b.top, 0.2f), new Color(0.4f, 0.4f, 0.45f), true);
        b.ball = GameFeatures.Px("Ball", go.transform, Vector3.zero, Vector3.one * R * 2f, Color.white).transform;
        b.ball.GetComponent<Renderer>().sharedMaterial = gm.Mat("Ball Glow", new Color(2f, 2f, 1.8f), 0.4f);
        // إطار الملعب (خطوط منقطة خفيفة)
        for (float y = b.top + 0.5f; y < b.ceil; y += 0.6f) GameFeatures.Px("Arena", go.transform, new Vector3(b.left, y, 0.8f), new Vector3(0.08f, 0.3f, 0.08f), new Color(0.6f, 0.6f, 0.9f));
        for (float x = b.left; x < b.right; x += 0.6f) GameFeatures.Px("Arena", go.transform, new Vector3(x, b.ceil, 0.8f), new Vector3(0.3f, 0.08f, 0.08f), new Color(0.6f, 0.6f, 0.9f));
    }

    void Relaunch()
    {
        pos = launch;
        vel = new Vector2(0.75f, -0.66f).normalized * Speed;
        LevelSfx.Play("blip", 0.4f, 0.8f);
    }

    bool BricksLowGone()
    {
        for (int i = 0; i < bricks.Count; i++) if (bricks[i] != null && rects[i].yMin < top + 1.9f) return false;
        return true;
    }

    void Reflect(Vector2 n)
    {
        vel = vel - 2f * Vector2.Dot(vel, n) * n;
        // شوي عشوائية، وما نخليها تمشي عمودي تماما
        vel = (Vector2)(Quaternion.Euler(0f, 0f, Random.Range(-6f, 6f)) * vel);
        if (Mathf.Abs(vel.x) < Speed * 0.35f) vel.x = Mathf.Sign(vel.x == 0f ? 1f : vel.x) * Speed * 0.35f;
        vel = vel.normalized * Speed;
    }

    // المضرب يوجه الكورة لطوبة باقية تحت (عشان اللعبة ما تطول)
    void AimAtBrick()
    {
        var alive = new List<int>();
        for (int i = 0; i < bricks.Count; i++) if (bricks[i] != null && rects[i].yMin < top + 1.9f) alive.Add(i);
        if (alive.Count == 0) return;
        Vector2 target = rects[alive[Random.Range(0, alive.Count)]].center;
        Vector2 dir = (target - pos).normalized;
        if (dir.y < 0.2f) dir = new Vector2(dir.x, 0.2f).normalized;
        vel = dir * Speed;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float dt = Time.deltaTime;
        if (lostT >= 0f)
        {
            lostT -= dt;
            ball.position = new Vector3(launch.x, launch.y, -0.2f);
            ball.localScale = Vector3.one * R * 2f * Mathf.Clamp01(1f - lostT);
            if (lostT < 0f) Relaunch();
        }
        else
        {
            var segs = new List<Vector4>();
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (m.closed || m.path == null || m.beingEaten) continue;
                // نجمع النقاط القريبة مرة وحدة (قطع أطول عشان الارتداد يطلع صح)
                Vector3 last = m.path[0];
                for (int i = 1; i < m.path.Length; i++)
                {
                    if (Vector2.Distance(last, m.path[i]) < 0.3f && i < m.path.Length - 1) continue;
                    segs.Add(new Vector4(last.x, last.y, m.path[i].x, m.path[i].y));
                    last = m.path[i];
                }
            }
            for (int sub = 0; sub < 4; sub++)
            {
                float h = dt / 4f;
                Vector2 next = pos + vel * h;
                // جدران الملعب
                if (next.x < left + R) { vel.x = Mathf.Abs(vel.x); next.x = left + R; }
                if (next.x > right - R) { vel.x = -Mathf.Abs(vel.x); next.x = right - R; }
                if (next.y > ceil - R) { vel.y = -Mathf.Abs(vel.y); next.y = ceil - R; }
                // الطوب
                for (int i = 0; i < bricks.Count; i++)
                {
                    if (bricks[i] == null) continue;
                    Rect rr = rects[i];
                    if (next.x < rr.xMin - R || next.x > rr.xMax + R || next.y < rr.yMin - R || next.y > rr.yMax + R) continue;
                    float ox = Mathf.Min(next.x - (rr.xMin - R), rr.xMax + R - next.x), oy = Mathf.Min(next.y - (rr.yMin - R), rr.yMax + R - next.y);
                    Reflect(ox < oy ? new Vector2(1f, 0f) : new Vector2(0f, 1f));
                    GameFeatures.Burst(bricks[i].position, bricks[i].GetComponent<Renderer>().sharedMaterial.color, 8, 0.15f);
                    LevelSfx.Play("count", 0.5f, Random.Range(0.9f, 1.3f));
                    Destroy(bricks[i].gameObject);
                    bricks[i] = null;
                    next = pos + vel * h;
                    break;
                }
                // الخطوط المرسومة (مضرب)
                Vector2 d = next - pos;
                foreach (Vector4 sg in segs)
                {
                    Vector2 a = new Vector2(sg.x, sg.y), b = new Vector2(sg.z, sg.w);
                    Vector2 e = b - a, nrm = new Vector2(-e.y, e.x).normalized;
                    if (e.sqrMagnitude < 0.0025f) continue;
                    float side0 = Vector2.Dot(pos - a, nrm), side1 = Vector2.Dot(next - a, nrm);
                    if (Mathf.Abs(side1) > R && Mathf.Sign(side0) == Mathf.Sign(side1)) continue;
                    float u = Vector2.Dot(next - a, e) / Mathf.Max(e.sqrMagnitude, 1e-5f);
                    if (u < -0.05f || u > 1.05f) continue;
                    if (Vector2.Dot(vel, nrm * Mathf.Sign(side0 == 0f ? 1f : side0)) >= 0f) continue;
                    Reflect(nrm);
                    AimAtBrick();
                    next = pos + vel * h;
                    LevelSfx.Play("blip", 0.45f, 1.1f);
                    Fx.Sparkle(new Vector3(next.x, next.y, -0.2f), Color.white, 4);
                    break;
                }
                // الأرض (غير الحفرة)
                bool overPit = next.x > pitFrom && next.x < pitTo;
                if (!overPit && next.y < top + R && vel.y < 0f) { vel.y = Mathf.Abs(vel.y); next.y = top + R; }
                pos = next;
                if (overPit && pos.y < top - 1f)
                {
                    lostT = 1f;
                    LevelSfx.Play("pxbuzz", 0.4f, 1.5f);
                    break;
                }
            }
            ball.position = new Vector3(pos.x, pos.y, -0.2f);
            ball.localRotation = Quaternion.Euler(0f, 0f, Time.time * 400f);
        }
        bool done = BricksLowGone() && RowaidaController.BridgeCovers(pitFrom, pitTo, top);
        bool wait = !done && HoldHelper.Before(pitFrom);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.8f, "تكسير الطوب! ارسمي خط فوق الحفرة: يصير مضرب يرد الكورة للطوب، وجسر لرويدا"); }
        HoldHelper.Hold(ref holding, wait);
        if (!done) LabFeatures.Show(pitFrom - 5f, wallX, new Vector3(left - 0.3f, top - 1f, 0f), new Vector3(right + 0.3f, ceil + 0.4f, 0f));
    }
}

// ---------- مصعد مبرمج: ارسمي أسهم على الشاشة (فوق، يمين...) والمنصة تنفذها ----------
public class CommandLift : MonoBehaviour
{
    float platX, top, upH, rightD, moveT = -1f;
    int ix, iy;
    Vector3 from, to;
    Transform plat;
    Rigidbody rb;
    readonly Queue<int> cmds = new Queue<int>();
    readonly HashSet<InkMark> seen = new HashSet<InkMark>();
    readonly List<string> lines = new List<string>();
    TextMesh console;
    Rect screen;
    bool holding, prompted;
    float lastX;

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Command Lift");
        go.transform.SetParent(root, false);
        var c = go.AddComponent<CommandLift>();
        c.platX = s.x; c.top = s.y; c.upH = s.z; c.rightD = s.w;
        Color neon = new Color(0.3f, 1.6f, 1.2f);
        // المنصة
        var p = new GameObject("Code Plate");
        p.transform.SetParent(go.transform, false);
        p.transform.position = new Vector3(c.platX, c.top - 0.15f, 0f);
        GameFeatures.Px("Plate", p.transform, Vector3.zero, new Vector3(1.6f, 0.3f, 1.8f), new Color(0.2f, 0.25f, 0.35f), true);
        LevelBuilder.Visual("Plate Glow", PrimitiveType.Cube, p.transform, new Vector3(0f, 0.16f, -0.9f), new Vector3(1.6f, 0.04f, 0.04f), gm.Mat("Code Neon", neon, 0.5f), false);
        GameFeatures.Text(p.transform, p.transform.position + new Vector3(0f, -0.02f, -0.93f), "{ }", 0.05f, new Color(0.4f, 1f, 0.9f));
        p.AddComponent<BoxCollider>().size = new Vector3(1.6f, 0.3f, 1.8f);
        c.rb = p.AddComponent<Rigidbody>();
        c.rb.isKinematic = true;
        c.rb.interpolation = RigidbodyInterpolation.Interpolate;
        c.plat = p.transform;
        // سكة منقطة توضح الطريق الممكن
        for (float y = c.top + 0.4f; y < c.top + c.upH; y += 0.45f) LevelBuilder.Visual("Rail", PrimitiveType.Cube, go.transform, new Vector3(c.platX, y, 0.9f), new Vector3(0.08f, 0.22f, 0.08f), gm.Mat("Code Neon", neon, 0.5f), false);
        for (float x = c.platX + 0.4f; x < c.platX + c.rightD; x += 0.45f) LevelBuilder.Visual("Rail", PrimitiveType.Cube, go.transform, new Vector3(x, c.top + c.upH - 0.15f, 0.9f), new Vector3(0.22f, 0.08f, 0.08f), gm.Mat("Code Neon", neon, 0.5f), false);
        // الشاشة
        Vector3 sc = new Vector3(c.platX - 3f, c.top + 5f, 0.6f);
        c.screen = new Rect(sc.x - 2.1f, sc.y - 1.3f, 4.2f, 2.6f);
        GameFeatures.Px("Monitor", go.transform, sc + new Vector3(0f, 0f, 0.15f), new Vector3(4.6f, 3f, 0.3f), new Color(0.15f, 0.16f, 0.2f), true);
        LevelBuilder.Visual("Screen", PrimitiveType.Cube, go.transform, sc, new Vector3(4.2f, 2.6f, 0.05f), gm.Mat("Screen Dark", new Color(0.02f, 0.06f, 0.08f), 0.9f), false);
        GameFeatures.Px("Monitor Neck", go.transform, sc + new Vector3(0f, -1.9f, 0.3f), new Vector3(0.3f, 0.8f, 0.2f), new Color(0.15f, 0.16f, 0.2f));
        c.console = GameFeatures.Text(go.transform, new Vector3(sc.x - 1.95f, sc.y + 1.15f, sc.z - 0.1f), "", 0.045f, new Color(0.4f, 1f, 0.6f), TextAnchor.UpperLeft);
        c.console.alignment = TextAlignment.Left;
        GameFeatures.Text(go.transform, sc + new Vector3(0f, 1.75f, -0.2f), "ارسمي سهم هنا", 0.06f, new Color(0.5f, 1f, 1f));
        c.lines.Add("> robot.ready();");
        c.Refresh();
    }

    void Refresh()
    {
        while (lines.Count > 6) lines.RemoveAt(0);
        string txt = string.Join("\n", lines) + (Mathf.Repeat(Time.time, 1f) < 0.5f ? "\n> _" : "\n> ");
        console.text = txt;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        RowaidaController r = gm.Rowaida;
        // أسهم جديدة على الشاشة
        foreach (InkMark m in GameFeatures.Fresh(seen))
        {
            if (m.closed || m.path == null || m.path.Length < 2) continue;
            Vector3 c = GameFeatures.Center(m);
            if (!screen.Contains(new Vector2(c.x, c.y))) continue;
            Vector3 d = m.path[m.path.Length - 1] - m.path[0];
            Destroy(m.gameObject);
            if (d.magnitude < 0.6f) continue;
            int cmd = Mathf.Abs(d.x) > Mathf.Abs(d.y) ? (d.x > 0f ? 0 : 1) : (d.y > 0f ? 2 : 3);
            cmds.Enqueue(cmd);
            lines.Add(new[] { "right();", "left();", "up();", "down();" }[cmd]);
            LevelSfx.Play("type", 0.6f);
        }
        Refresh();
        bool on = r != null && Mathf.Abs(r.transform.position.x - plat.position.x) < 0.8f && r.transform.position.y > plat.position.y && r.transform.position.y - plat.position.y < 1.6f;
        float vx = 0f;
        if (moveT >= 0f)
        {
            moveT = Mathf.Min(1f, moveT + dt / 0.9f);
            Vector3 p = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, moveT));
            vx = (p.x - lastX) / Mathf.Max(dt, 1e-4f);
            lastX = p.x;
            rb.MovePosition(p);
            if (moveT >= 1f) moveT = -1f;
        }
        else if (on && cmds.Count > 0)
        {
            int cmd = cmds.Dequeue();
            int nx = Mathf.Clamp(ix + (cmd == 0 ? 1 : cmd == 1 ? -1 : 0), 0, 1), ny = Mathf.Clamp(iy + (cmd == 2 ? 1 : cmd == 3 ? -1 : 0), 0, 1);
            if (nx == ix && ny == iy)
            {
                LevelSfx.Play("pxbuzz", 0.6f);
                lines.Add("// error!");
            }
            else
            {
                ix = nx; iy = ny;
                from = plat.position; lastX = from.x;
                to = new Vector3(platX + ix * rightD, top - 0.15f + iy * upH, 0f);
                moveT = 0f;
                LevelSfx.Play("motor", 0.6f, 1.3f);
            }
        }
        bool done = ix == 1 && iy == 1 && moveT < 0f;
        bool wait = on && !done && gm.IsPlaying;
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.8f, "برمجي المنصة! ارسمي سهم على الشاشة: لفوق ترتفع، ولليمين تمشي"); }
        HoldHelper.Hold(ref holding, wait);
        if (holding && r != null) r.CarryX = vx;
        if (!done) LabFeatures.Show(platX - 7f, platX + rightD + 0.8f, new Vector3(screen.xMin - 0.5f, top - 0.5f, 0f), new Vector3(platX + rightD + 1.6f, screen.yMax + 0.8f, 0f));
    }
}

// ---------- حشرة باق: تطير وتخرب الطريق (جدار خطأ). حوطيها بدايرة وتنمسك ----------
public class BugCatch : MonoBehaviour
{
    float bx, top, wallX, nextGoal, caughtT = -1f, missTold;
    Vector3 bug, goal;
    Transform bugT, wingL, wingR, jar;
    BoxCollider col;
    readonly List<Transform> glitch = new List<Transform>();
    readonly HashSet<InkMark> seen = new HashSet<InkMark>();
    TextMesh err;
    bool holding, prompted;

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Bug Catch");
        go.transform.SetParent(root, false);
        var b = go.AddComponent<BugCatch>();
        b.bx = s.x; b.top = s.y; b.wallX = s.z;
        b.bug = b.goal = new Vector3(b.bx, b.top + 2f, 0f);
        // جدار الخطأ
        var w = new GameObject("Error Wall");
        w.transform.SetParent(go.transform, false);
        w.transform.position = new Vector3(b.wallX, b.top, 0f);
        Color[] gc = { new Color(1f, 0.15f, 0.3f), new Color(0.15f, 0.05f, 0.1f), new Color(1f, 0.2f, 0.8f), new Color(0.3f, 0.9f, 1f) };
        for (int row = 0; row < 17; row++)
            for (int k = 0; k < 4; k++)
                b.glitch.Add(GameFeatures.Px("Glitch", w.transform, new Vector3(b.wallX - 0.3f + k * 0.2f, b.top + 0.1f + row * 0.2f, 0f), new Vector3(0.2f, 0.2f, 1.8f), gc[(row * 7 + k * 3) % gc.Length]).transform);
        b.col = w.AddComponent<BoxCollider>();
        b.col.center = new Vector3(0f, 1.7f, 0f);
        b.col.size = new Vector3(0.8f, 3.4f, 2f);
        b.err = GameFeatures.Text(go.transform, new Vector3(b.wallX, b.top + 4.1f, -0.3f), "ERROR 404", 0.06f, new Color(1f, 0.3f, 0.35f));
        // الحشرة
        b.bugT = new GameObject("Bug").transform;
        b.bugT.SetParent(go.transform, false);
        Color green = new Color(0.3f, 0.85f, 0.35f), dark = new Color(0.1f, 0.35f, 0.15f);
        GameFeatures.Px("Bug Body", b.bugT, new Vector3(0f, 0f, 0f), new Vector3(0.5f, 0.35f, 0.35f), green);
        GameFeatures.Px("Bug Spots", b.bugT, new Vector3(-0.05f, 0.05f, -0.18f), new Vector3(0.12f, 0.12f, 0.02f), dark);
        GameFeatures.Px("Bug Head", b.bugT, new Vector3(0.32f, 0.02f, 0f), new Vector3(0.22f, 0.22f, 0.26f), dark);
        GameFeatures.Px("Bug Eye", b.bugT, new Vector3(0.38f, 0.06f, -0.14f), new Vector3(0.07f, 0.07f, 0.02f), new Color(1f, 0.2f, 0.2f));
        for (int k = 0; k < 2; k++) GameFeatures.Px("Antenna", b.bugT, new Vector3(0.42f, 0.2f, -0.06f + k * 0.12f), new Vector3(0.04f, 0.2f, 0.04f), dark).transform.localRotation = Quaternion.Euler(0f, 0f, -30f);
        for (int k = 0; k < 3; k++) GameFeatures.Px("Leg", b.bugT, new Vector3(-0.15f + k * 0.15f, -0.22f, -0.1f), new Vector3(0.04f, 0.14f, 0.04f), dark);
        b.wingL = GameFeatures.Px("Wing", b.bugT, new Vector3(-0.05f, 0.24f, 0.05f), new Vector3(0.35f, 0.04f, 0.2f), new Color(0.8f, 0.95f, 1f)).transform;
        b.wingR = GameFeatures.Px("Wing", b.bugT, new Vector3(-0.05f, 0.24f, -0.12f), new Vector3(0.35f, 0.04f, 0.2f), new Color(0.8f, 0.95f, 1f)).transform;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        for (int i = 0; i < glitch.Count; i++)
            if (caughtT < 0f && Random.value < 0.03f) glitch[i].localScale = Random.value < 0.5f ? new Vector3(0.2f, 0.2f, 1.8f) : new Vector3(0.24f, 0.12f, 1.8f);
        if (caughtT < 0f)
        {
            if (t > nextGoal) { nextGoal = t + Random.Range(0.8f, 1.6f); goal = new Vector3(bx + Random.Range(-1.6f, 1.6f), top + Random.Range(1.3f, 3.2f), 0f); }
            bug = Vector3.MoveTowards(bug, goal, dt * 1.4f);
            bugT.position = bug + new Vector3(0f, Mathf.Sin(t * 9f) * 0.05f, 0f);
            bugT.localRotation = Quaternion.Euler(0f, goal.x < bug.x ? 180f : 0f, 0f);
            wingL.localRotation = Quaternion.Euler(Mathf.Sin(t * 50f) * 35f, 0f, 0f);
            wingR.localRotation = Quaternion.Euler(-Mathf.Sin(t * 50f) * 35f, 0f, 0f);
            if (Random.value < 0.02f) bugT.position += new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), 0f);
            if (Random.value < 0.08f) Fx.Sparkle(bug, new Color(1f, 0.3f, 0.5f), 1);
            // دايرة حوالينها؟
            foreach (InkMark m in GameFeatures.Fresh(seen))
            {
                if (!m.closed) continue;
                if (GameFeatures.Inside(m, bug))
                {
                    caughtT = 0f;
                    Destroy(m.gameObject);
                    LevelSfx.Play("bugzap", 0.8f);
                    LevelSfx.Play("powerup", 0.6f);
                    jar = LevelBuilder.Visual("Jar", PrimitiveType.Cylinder, transform, bug, new Vector3(0.8f, 0.45f, 0.8f), LabFeatures.Glass(0.45f), false).transform;
                    LevelBuilder.Visual("Jar Lid", PrimitiveType.Cylinder, jar, new Vector3(0f, 1.05f, 0f), new Vector3(1.05f, 0.15f, 1.05f), gm.Mat("Lid Red", new Color(0.9f, 0.25f, 0.3f), 0.4f), false);
                    gm.UI.ShowMessage("", 2.2f, "مسكتي الباق! الخطأ انصلح");
                    break;
                }
                if (t - missTold > 4f && Vector3.Distance(GameFeatures.Center(m), bug) < 3f) { missTold = t; gm.UI.ShowMessage("", 2f, "فاتتك! حوطي الحشرة كلها بالدايرة"); }
            }
        }
        else
        {
            caughtT += dt;
            float k = Mathf.Clamp01(caughtT / 0.6f);
            bugT.localScale = Vector3.one * (1f - k * 0.6f);
            if (jar != null) jar.position = bugT.position + Vector3.down * 0.05f;
            if (col.enabled && caughtT > 0.3f)
            {
                col.enabled = false;
                foreach (Transform g in glitch)
                {
                    g.SetParent(null, true);
                    var fx = g.gameObject.AddComponent<FxParticle>();
                    fx.velocity = new Vector3(Random.Range(-3f, 3f), Random.Range(1f, 5f), Random.Range(-2f, 2f));
                    fx.gravity = 6f; fx.life = 0.9f; fx.spin = Random.insideUnitSphere * 300f;
                }
                glitch.Clear();
                GameFeatures.SetText(err, "FIXED");
                err.color = new Color(0.4f, 1f, 0.5f);
            }
        }
        bool wait = caughtT < 0f && HoldHelper.Before(wallX - 0.4f);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.8f, "حشرة باق خربت الطريق! حوطيها بدايرة عشان تمسكينها"); }
        HoldHelper.Hold(ref holding, wait);
        if (caughtT < 0f) LabFeatures.Show(bx - 5f, wallX, new Vector3(bx - 2.5f, top - 0.5f, 0f), new Vector3(wallX + 1f, top + 4.6f, 0f));
    }
}

// ---------- حلقة تكرار: باب ينفتح ويتسكر بسرعة على طول. ارسمي خط يقطع الحلقة وينفتح ويوقف ----------
public class LoopDoor : MonoBehaviour
{
    const float R = 0.9f;
    float doorX, top;
    Vector2 ring;
    Transform panel, ringT;
    BoxCollider col;
    bool broken, holding, prompted;
    float openK;
    TextMesh code;
    readonly List<Transform> ringBits = new List<Transform>();

    public static void Create(Transform root, Vector4 s, GameManager gm)
    {
        var go = new GameObject("Loop Door");
        go.transform.SetParent(root, false);
        var d = go.AddComponent<LoopDoor>();
        d.doorX = s.x; d.top = s.y;
        d.ring = new Vector2(d.doorX - 3f, d.top + 4.6f);
        Color frame = new Color(0.25f, 0.3f, 0.45f), neon = new Color(1f, 0.55f, 0.15f);
        for (int k = -1; k <= 1; k += 2) GameFeatures.Px("Door Post", go.transform, new Vector3(d.doorX, d.top + 1.8f, k * 1.05f), new Vector3(0.4f, 3.6f, 0.2f), frame, true);
        GameFeatures.Px("Door Top", go.transform, new Vector3(d.doorX, d.top + 3.7f, 0f), new Vector3(0.5f, 0.3f, 2.3f), frame, true);
        d.panel = new GameObject("Door Panel").transform;
        d.panel.SetParent(go.transform, false);
        d.panel.position = new Vector3(d.doorX, d.top, 0f);
        GameFeatures.Px("Panel", d.panel, new Vector3(0f, 1.7f, 0f), new Vector3(0.3f, 3.4f, 1.9f), new Color(0.35f, 0.4f, 0.6f), true);
        LevelBuilder.Visual("Panel Glow", PrimitiveType.Cube, d.panel, new Vector3(0f, 1.7f, -0.96f), new Vector3(0.32f, 3.2f, 0.02f), gm.Mat("Loop Neon", neon, 0.5f), false);
        d.col = d.panel.gameObject.AddComponent<BoxCollider>();
        d.col.center = new Vector3(0f, 1.7f, 0f);
        d.col.size = new Vector3(0.4f, 3.4f, 2f);
        // الحلقة (سهم دايري يلف)
        d.ringT = new GameObject("Loop Ring").transform;
        d.ringT.SetParent(go.transform, false);
        d.ringT.position = new Vector3(d.ring.x, d.ring.y, 0f);
        for (int k = 0; k < 18; k++)
        {
            float a = k / 18f * Mathf.PI * 2f;
            var bit = LevelBuilder.Visual("Ring Bit", PrimitiveType.Cube, d.ringT, new Vector3(Mathf.Cos(a) * R, Mathf.Sin(a) * R, 0f), new Vector3(0.32f, 0.14f, 0.14f), gm.Mat("Loop Neon", neon, 0.5f), false).transform;
            bit.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg + 90f);
            d.ringBits.Add(bit);
        }
        for (int k = -1; k <= 1; k += 2) LevelBuilder.Visual("Arrow", PrimitiveType.Cube, d.ringT, new Vector3(R + 0.12f * k, -0.15f, 0f), new Vector3(0.3f, 0.08f, 0.08f), gm.Mat("Loop Neon", neon, 0.5f), false).transform.localRotation = Quaternion.Euler(0f, 0f, k * 45f);
        d.code = GameFeatures.Text(go.transform, new Vector3(d.ring.x, d.ring.y + 1.45f, -0.2f), "while (true) { open(); close(); }", 0.045f, new Color(1f, 0.75f, 0.4f));
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        if (!broken)
        {
            ringT.localRotation = Quaternion.Euler(0f, 0f, -t * 160f);
            // يفتح لحظة صغيرة بس كل شوي
            float ph = Mathf.Repeat(t, 1.3f);
            float want = ph < 0.3f ? 1f : 0f;
            openK = Mathf.MoveTowards(openK, want, dt * 9f);
            if (ph < dt) LevelSfx.Play("count", 0.25f, 0.7f);
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (m.closed || m.path == null || m.path.Length < 2) continue;
                bool cut = false;
                for (int i = 1; i < m.path.Length && !cut; i++)
                {
                    float d0 = Vector2.Distance(m.path[i - 1], ring) - R, d1 = Vector2.Distance(m.path[i], ring) - R;
                    if (d0 * d1 <= 0f) cut = true;
                }
                if (!cut) continue;
                broken = true;
                Destroy(m.gameObject);
                foreach (Transform bit in ringBits)
                {
                    bit.SetParent(null, true);
                    var fx = bit.gameObject.AddComponent<FxParticle>();
                    fx.velocity = (bit.position - (Vector3)ring).normalized * 4f + Vector3.up * 2f;
                    fx.gravity = 8f; fx.life = 1f; fx.spin = Random.insideUnitSphere * 300f;
                }
                ringBits.Clear();
                GameFeatures.SetText(code, "while (true) { break; }");
                code.color = new Color(0.4f, 1f, 0.5f);
                LevelSfx.Play("powerup", 0.7f);
                gm.UI.ShowMessage("", 2f, "قطعتي الحلقة! الباب وقف مفتوح");
                break;
            }
        }
        else openK = Mathf.MoveTowards(openK, 1f, dt * 3f);
        panel.position = new Vector3(doorX, top + openK * 3.3f, 0f);
        col.enabled = openK < 0.6f;
        bool wait = !broken && HoldHelper.Before(doorX - 0.3f);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.8f, "باب يفتح ويتسكر بلا وقفة! ارسمي خط يقطع الحلقة البرتقالية"); }
        HoldHelper.Hold(ref holding, wait);
        if (!broken) LabFeatures.Show(doorX - 6f, doorX, new Vector3(ring.x - 2.5f, top - 0.5f, 0f), new Vector3(doorX + 1f, ring.y + 2f, 0f));
    }
}
