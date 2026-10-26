using System.Collections.Generic;
using UnityEngine;

// عقبات الغابة الثانية: شجرة يابسة تنطاح جسر، شبكة عنكبوت، بومة تراقب، حبل كرمة تتمرجح عليه رويدا،
// قنفذ يتدحرج، بركة سلاحف تصير جسر، وحجار تطلع وتغطس
public static class ForestFeatures2
{
    public static void Build(Transform root, LevelData data, GameManager gm)
    {
        foreach (Vector3 s in data.deadTrees) DeadTree.Create(root, s, gm);
        foreach (Vector2 s in data.webs) SpiderWeb.Create(root, s, gm);
        foreach (Vector3 s in data.owls) OwlWatch.Create(root, s, gm);
        foreach (Vector3 s in data.vines) VineSwing.Create(root, s, gm);
        foreach (Vector3 s in data.pastaSwings) VineSwing.Create(root, s, gm, true);
        foreach (Vector3 s in data.hedgehogs) Hedgehog.Create(root, s, gm);
        foreach (Vector3 s in data.turtlePonds) TurtlePond.Create(root, s, gm);
        foreach (Vector3 s in data.stonePonds) SteppingStones.Create(root, s, gm);
    }

    // سطح مويه هادي بتموج خفيف (للبرك)
    public static Material PondWater(Transform parent, float from, float to, float y, out Transform quad)
    {
        var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
        for (int py = 0; py < 32; py++)
            for (int px = 0; px < 32; px++)
            {
                float k = Mathf.Sin(px / 32f * Mathf.PI * 4f + Mathf.Sin(py / 32f * Mathf.PI * 2f) * 2f) * Mathf.Sin(py / 32f * Mathf.PI * 4f);
                tex.SetPixel(px, py, Color.Lerp(new Color(0.25f, 0.55f, 0.6f), new Color(0.55f, 0.82f, 0.85f), Mathf.Clamp01(k * 1.5f)));
            }
        tex.Apply();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Pond Water" };
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetTextureScale("_BaseMap", new Vector2((to - from) / 3f, 4f));
        quad = LevelBuilder.Visual("Pond", PrimitiveType.Quad, parent, new Vector3((from + to) * 0.5f, y, 0.5f), new Vector3(to - from + 0.2f, 13f, 1f), mat, false).transform;
        quad.localRotation = Quaternion.Euler(90f, 0f, 0f);
        return mat;
    }

    // البركة تبلع الخطوط اللي فوقها (بعد شوي) — عشان ما ينرسم جسر عادي
    public static void SwallowLines(float from, float to, float top, Dictionary<InkMark, float> seen, float delay)
    {
        float t = Time.time;
        foreach (InkMark m in Object.FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (m.closed || m.beingEaten) continue;
            var r = m.GetComponent<MeshRenderer>();
            if (r == null) continue;
            Bounds b = r.bounds;
            if (b.center.x < from || b.center.x > to || b.center.y > top + 1.5f) continue;
            if (!seen.ContainsKey(m)) { seen[m] = t; continue; }
            if (t - seen[m] < delay) continue;
            m.beingEaten = true;
            foreach (Collider c in m.GetComponentsInChildren<Collider>()) c.enabled = false;
            Fx.Sparkle(b.center, new Color(0.6f, 0.85f, 0.95f), 10);
            LevelSfx.Play("plop", 0.7f);
            Object.Destroy(m.gameObject);
        }
    }
}

// ---------- شجرة يابسة ----------

// شجرة يابسة واقفة على طرف حفرة: ادفعيها بخط من فوق، تصرّ وتطيح عرض الحفرة وتصير جسر
public class DeadTree : MonoBehaviour
{
    float from, to, top, len, fallT = -1f;
    bool holding, prompted, landed;
    Transform pivot;
    BoxCollider col;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Dead Tree");
        go.transform.SetParent(root, false);
        var d = go.AddComponent<DeadTree>();
        d.from = spec.x; d.to = spec.y; d.top = spec.z;
        d.len = d.to - d.from + 1.6f;
        Material bark = gm.Mat("Dead Bark", new Color(0.55f, 0.47f, 0.4f), 0.15f);
        Material barkDark = gm.Mat("Dead Bark Dark", new Color(0.38f, 0.32f, 0.27f), 0.1f);
        Material moss = gm.Mat("Moss", new Color(0.4f, 0.65f, 0.3f), 0.2f);
        Material hole = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        d.pivot = new GameObject("Pivot").transform;
        d.pivot.SetParent(go.transform, false);
        d.pivot.position = new Vector3(d.from - 0.45f, d.top, 1f);
        LevelBuilder.Visual("Trunk", PrimitiveType.Cylinder, d.pivot, new Vector3(0f, d.len * 0.5f, 0f), new Vector3(0.62f, d.len * 0.5f, 0.62f), bark);
        // شقوق وطحالب وفتحة
        for (int i = 0; i < 5; i++)
            LevelBuilder.Visual("Crack", PrimitiveType.Cube, d.pivot, new Vector3(0f, 0.8f + i * d.len * 0.17f, -0.31f), new Vector3(0.06f, 0.5f, 0.02f), barkDark, false).transform.localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 12f : -10f));
        LevelBuilder.Visual("Moss", PrimitiveType.Sphere, d.pivot, new Vector3(0.1f, d.len * 0.3f, -0.25f), new Vector3(0.5f, 0.35f, 0.2f), moss, false);
        LevelBuilder.Visual("Hole", PrimitiveType.Sphere, d.pivot, new Vector3(0f, d.len * 0.62f, -0.3f), new Vector3(0.25f, 0.35f, 0.06f), hole, false);
        // أغصان يابسة
        for (int i = 0; i < 4; i++)
        {
            float h = d.len * (0.55f + i * 0.12f);
            var br = LevelBuilder.Visual("Branch", PrimitiveType.Cylinder, d.pivot, new Vector3((i % 2 == 0 ? 0.45f : -0.45f), h, 0.1f), new Vector3(0.12f, 0.55f, 0.12f), bark, false).transform;
            br.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? -50f : 50f);
        }
        // جذور
        for (int i = 0; i < 4; i++)
        {
            float a = i / 4f * Mathf.PI * 2f + 0.4f;
            var root2 = LevelBuilder.Visual("Root", PrimitiveType.Capsule, go.transform, new Vector3(d.from - 0.45f + Mathf.Cos(a) * 0.45f, d.top + 0.1f, 1f + Mathf.Sin(a) * 0.45f), new Vector3(0.18f, 0.35f, 0.18f), barkDark, false).transform;
            root2.localRotation = Quaternion.Euler(Mathf.Sin(a) * 60f, 0f, -Mathf.Cos(a) * 60f);
        }
        var c = new GameObject("Bridge Collider");
        c.transform.SetParent(d.pivot, false);
        c.transform.localPosition = new Vector3(0f, d.len * 0.5f, 0f);
        d.col = c.AddComponent<BoxCollider>();
        d.col.size = new Vector3(0.62f, d.len, 1.6f);
        d.col.enabled = false;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;
        if (fallT < 0f)
        {
            pivot.localRotation = Quaternion.Euler(0f, 0f, -3f + Mathf.Sin(t * 1.1f) * 1.5f);
            // خط لمس الشجرة من فوق (النص الفوقاني)؟
            Bounds upper = new Bounds(new Vector3(from - 0.45f, top + len * 0.7f, 0f), new Vector3(1.6f, len * 0.6f, 4f));
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                var r = m.GetComponent<MeshRenderer>();
                if (r == null || !r.bounds.Intersects(upper)) continue;
                Destroy(m.gameObject);
                fallT = 0f;
                LevelSfx.Play("timber", 0.9f);
                break;
            }
        }
        else if (!landed)
        {
            fallT += Time.deltaTime / 1.2f;
            float k = Mathf.Clamp01(fallT);
            float ang = -3f - 87f * k * k;
            pivot.localRotation = Quaternion.Euler(0f, 0f, ang);
            pivot.position = new Vector3(from - 0.45f, top - 0.3f * k, Mathf.Lerp(1f, 0f, k));
            if (k >= 1f)
            {
                landed = true;
                pivot.localRotation = Quaternion.Euler(0f, 0f, -90f);
                col.enabled = true;
                for (int i = 0; i < 4; i++) Fx.Dust(new Vector3(from + i * (to - from) / 3f, top, 0f), 0.7f);
                var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
                if (cam != null) cam.Shake(0.18f, 0.35f);
                Fx.Sparkle(new Vector3(to, top + 0.3f, 0f), new Color(0.9f, 0.8f, 0.6f), 16);
            }
        }

        bool wait = HoldHelper.Before(from) && !landed && !RowaidaController.BridgeCovers(from, to, top);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.2f, "شجرة يابسة! ادفعيها بخط من فوق... تطيح وتصير جسر"); }
        HoldHelper.Hold(ref holding, wait);
    }
}

// ---------- شبكة عنكبوت ----------

// شبكة عنكبوت تسد الطريق بين غصنين، والعنكبوت معلق فوقها. قطعيها بخط والعنكبوت يهرب لفوق
public class SpiderWeb : MonoBehaviour
{
    float x, top, cutT = -1f;
    bool holding, prompted;
    Transform web, spider, thread;
    readonly List<Transform> strands = new List<Transform>();

    public static void Create(Transform root, Vector2 spec, GameManager gm)
    {
        var go = new GameObject("Spider Web");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<SpiderWeb>();
        s.x = spec.x; s.top = spec.y;
        Material silk = gm.Mat("Web Silk", new Color(0.97f, 0.97f, 1f), 0.9f);
        Material twig = gm.Mat("Tree Bark", new Color(0.5f, 0.33f, 0.2f), 0.2f);
        Material body = gm.Mat("Spider Body", new Color(0.18f, 0.14f, 0.22f), 0.5f);
        Material white = gm.Mat("Monster Eye", Color.white, 0.7f);
        Material pink = gm.Mat("Bunny Pink", new Color(1f, 0.7f, 0.75f), 0.3f);
        // الشبكة مايلة شوي عشان تبان من الكاميرا (تسد الطريق)
        s.web = new GameObject("Web").transform;
        s.web.SetParent(go.transform, false);
        s.web.position = new Vector3(s.x, s.top + 1.5f, 0f);
        s.web.localRotation = Quaternion.Euler(0f, 55f, 0f);
        for (int side = -1; side <= 1; side += 2)
            LevelBuilder.Visual("Twig", PrimitiveType.Cylinder, s.web, new Vector3(side * 1.25f, 0.1f, 0f), new Vector3(0.12f, 1.7f, 0.12f), twig);
        for (int i = 0; i < 10; i++)
        {
            float a = i / 10f * Mathf.PI * 2f;
            var st = LevelBuilder.Visual("Spoke", PrimitiveType.Cube, s.web, new Vector3(Mathf.Cos(a) * 0.65f, Mathf.Sin(a) * 0.65f, 0f), new Vector3(1.3f, 0.025f, 0.025f), silk, false).transform;
            st.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
            s.strands.Add(st);
        }
        for (int ring = 1; ring <= 4; ring++)
        {
            float rr = ring * 0.3f;
            for (int i = 0; i < 10; i++)
            {
                float a0 = i / 10f * Mathf.PI * 2f, a1 = (i + 1) / 10f * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * rr, p1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * rr;
                var seg = LevelBuilder.Visual("Ring", PrimitiveType.Cube, s.web, (p0 + p1) * 0.5f, new Vector3((p1 - p0).magnitude, 0.02f, 0.02f), silk, false).transform;
                seg.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(p1.y - p0.y, p1.x - p0.x) * Mathf.Rad2Deg);
                s.strands.Add(seg);
            }
        }
        // خيوط تربط الشبكة بالغصنين
        for (int side = -1; side <= 1; side += 2)
            for (int k = -1; k <= 1; k++)
            {
                var anchor = LevelBuilder.Visual("Anchor", PrimitiveType.Cube, s.web, new Vector3(side * 1f, k * 0.8f, 0f), new Vector3(0.6f, 0.02f, 0.02f), silk, false).transform;
                anchor.localRotation = Quaternion.Euler(0f, 0f, side * k * -25f);
                s.strands.Add(anchor);
            }
        // العنكبوت
        s.thread = LevelBuilder.Visual("Thread", PrimitiveType.Cube, go.transform, Vector3.zero, Vector3.one, silk, false).transform;
        s.spider = new GameObject("Spider").transform;
        s.spider.SetParent(go.transform, false);
        LevelBuilder.Visual("Body", PrimitiveType.Sphere, s.spider, Vector3.zero, new Vector3(0.4f, 0.36f, 0.36f), body);
        LevelBuilder.Visual("Head", PrimitiveType.Sphere, s.spider, new Vector3(0f, -0.22f, 0f), Vector3.one * 0.24f, body);
        for (int e = -1; e <= 1; e += 2)
        {
            LevelBuilder.Visual("Eye", PrimitiveType.Sphere, s.spider, new Vector3(e * 0.06f, -0.22f, -0.11f), Vector3.one * 0.08f, white, false);
            LevelBuilder.Visual("Cheek", PrimitiveType.Sphere, s.spider, new Vector3(e * 0.1f, -0.3f, -0.08f), Vector3.one * 0.05f, pink, false);
        }
        for (int i = 0; i < 8; i++)
        {
            int side = i < 4 ? -1 : 1;
            var leg = LevelBuilder.Visual("Leg", PrimitiveType.Cube, s.spider, new Vector3(side * 0.25f, 0.05f - (i % 4) * 0.06f, 0f), new Vector3(0.35f, 0.03f, 0.03f), body, false).transform;
            leg.localRotation = Quaternion.Euler(0f, 0f, side * (-30f + (i % 4) * 20f));
        }
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;
        Vector3 hang = new Vector3(x + 0.2f, top + 3.4f + Mathf.Sin(t * 1.4f) * 0.25f, -0.3f);
        if (cutT < 0f)
        {
            web.localScale = Vector3.one * (1f + Mathf.Sin(t * 2f) * 0.01f);
            // خط قطع الشبكة؟
            Bounds b = new Bounds(new Vector3(x, top + 1.5f, 0f), new Vector3(1.4f, 2.8f, 4f));
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                var r = m.GetComponent<MeshRenderer>();
                if (r == null || !r.bounds.Intersects(b)) continue;
                Destroy(m.gameObject);
                cutT = 0f;
                LevelSfx.Play("rip", 0.9f);
                foreach (Transform st in strands)
                {
                    st.SetParent(null, true);
                    var fx = st.gameObject.AddComponent<FxParticle>();
                    fx.velocity = new Vector3(Random.Range(-2f, 2f), Random.Range(0f, 2f), Random.Range(-1f, 1f));
                    fx.gravity = 4f; fx.spin = Random.insideUnitSphere * 200f; fx.life = 1.4f;
                }
                // الغصنين اللي كانوا ماسكين الشبكة يطيحون بعد
                var twigs = new List<Transform>();
                foreach (Transform c in web) twigs.Add(c);
                foreach (Transform c in twigs)
                {
                    c.SetParent(null, true);
                    var fx = c.gameObject.AddComponent<FxParticle>();
                    fx.velocity = new Vector3(Random.Range(-1f, 1f), 1f, Random.Range(0.5f, 1.5f));
                    fx.gravity = 9f; fx.spin = new Vector3(0f, 0f, Random.Range(-120f, 120f)); fx.life = 1.2f;
                }
                Fx.Sparkle(new Vector3(x, top + 1.5f, 0f), Color.white, 20);
                break;
            }
        }
        else
        {
            cutT += Time.deltaTime;
            hang += Vector3.up * cutT * 4f;   // العنكبوت يهرب لفوق
        }
        spider.position = hang;
        spider.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 3f) * 8f);
        float topY = hang.y + 6f;
        thread.position = new Vector3(hang.x, (topY + hang.y) * 0.5f, hang.z);
        thread.localScale = new Vector3(0.02f, topY - hang.y, 0.02f);
        if (cutT > 3f) spider.gameObject.SetActive(false);

        bool wait = cutT < 0f && HoldHelper.Before(x - 0.4f);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3f, "شبكة عنكبوت تسد الطريق! قطعيها بخط"); }
        HoldHelper.Hold(ref holding, wait);
    }
}

// ---------- البومة ----------

// بومة على غصن فوق الطريق: تنام شوي وبعدين تفتح عيونها وتراقب (ضو أصفر). إذا شافت رويدا ترجعها لورا.
// رويدا تستنى لين تغمض... أو ارسمي دايرة على عيونها: تصير غطاء عيون وتنام على طول
public class OwlWatch : MonoBehaviour
{
    const float Period = 6.5f, Sleep = 3.8f;
    float x, top, range, start, nextSpot;
    bool holding, prompted, blind, spottedOnce;
    Transform owl, head, lids, mask, beam;
    readonly List<Transform> zs = new List<Transform>();
    Material eyeOn;

    float Local => Mathf.Repeat(Time.time - start, Period);
    bool Watching => !blind && Local > Sleep + 0.3f;
    float UntilWatch => Local < Sleep + 0.3f ? Sleep + 0.3f - Local : 0f;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Owl Watch");
        go.transform.SetParent(root, false);
        var o = go.AddComponent<OwlWatch>();
        o.x = spec.x; o.top = spec.y; o.range = spec.z;
        o.start = Time.time;
        Material bark = gm.Mat("Tree Bark", new Color(0.5f, 0.33f, 0.2f), 0.2f);
        Material feather = gm.Mat("Owl Feather", new Color(0.55f, 0.42f, 0.32f), 0.3f);
        Material belly = gm.Mat("Owl Belly", new Color(0.88f, 0.78f, 0.62f), 0.3f);
        Material beak = gm.Mat("Bird Beak", new Color(1f, 0.62f, 0.25f), 0.3f);
        Material black = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        o.eyeOn = gm.Mat("Owl Eye Glow", new Color(2f, 1.6f, 0.3f), 0.6f);
        Material leaf = gm.Mat("Leaf Green", new Color(0.35f, 0.72f, 0.3f), 0.35f);
        // شجرة وغصن
        LevelBuilder.Visual("Owl Tree", PrimitiveType.Cylinder, go.transform, new Vector3(o.x + 2.8f, o.top + 2.8f, 2.2f), new Vector3(0.9f, 4f, 0.9f), bark);
        var br = LevelBuilder.Visual("Owl Branch", PrimitiveType.Cylinder, go.transform, new Vector3(o.x + 1.2f, o.top + 3.4f, 1.2f), new Vector3(0.22f, 1.8f, 0.22f), bark).transform;
        br.localRotation = Quaternion.Euler(30f, 0f, 80f);
        o.owl = new GameObject("Owl").transform;
        o.owl.SetParent(go.transform, false);
        o.owl.position = new Vector3(o.x, o.top + 3.9f, 0.5f);
        LevelBuilder.Visual("Body", PrimitiveType.Sphere, o.owl, Vector3.zero, new Vector3(0.75f, 0.95f, 0.7f), feather);
        LevelBuilder.Visual("Belly", PrimitiveType.Sphere, o.owl, new Vector3(0f, -0.1f, -0.2f), new Vector3(0.5f, 0.65f, 0.4f), belly, false);
        for (int s = -1; s <= 1; s += 2)
            LevelBuilder.Visual("Wing", PrimitiveType.Sphere, o.owl, new Vector3(s * 0.36f, -0.05f, 0f), new Vector3(0.22f, 0.7f, 0.5f), feather, false);
        o.head = new GameObject("Head").transform;
        o.head.SetParent(o.owl, false);
        o.head.localPosition = new Vector3(0f, 0.6f, 0f);
        LevelBuilder.Visual("Head", PrimitiveType.Sphere, o.head, Vector3.zero, new Vector3(0.75f, 0.6f, 0.65f), feather);
        for (int s = -1; s <= 1; s += 2)
        {
            var tuft = LevelBuilder.Visual("Tuft", PrimitiveType.Cube, o.head, new Vector3(s * 0.25f, 0.32f, 0f), new Vector3(0.12f, 0.25f, 0.08f), feather, false).transform;
            tuft.localRotation = Quaternion.Euler(0f, 0f, s * -25f);
            LevelBuilder.Visual("Eye Ring", PrimitiveType.Sphere, o.head, new Vector3(s * 0.16f, 0.02f, -0.27f), new Vector3(0.26f, 0.26f, 0.08f), belly, false);
            LevelBuilder.Visual("Eye", PrimitiveType.Sphere, o.head, new Vector3(s * 0.16f, 0.02f, -0.31f), Vector3.one * 0.15f, o.eyeOn, false);
            LevelBuilder.Visual("Pupil", PrimitiveType.Sphere, o.head, new Vector3(s * 0.16f, 0.02f, -0.37f), Vector3.one * 0.07f, black, false);
        }
        LevelBuilder.Visual("Beak", PrimitiveType.Cube, o.head, new Vector3(0f, -0.1f, -0.32f), new Vector3(0.08f, 0.12f, 0.06f), beak, false).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        // جفون (وهي نايمة)
        o.lids = new GameObject("Lids").transform;
        o.lids.SetParent(o.head, false);
        for (int s = -1; s <= 1; s += 2)
            LevelBuilder.Visual("Lid", PrimitiveType.Sphere, o.lids, new Vector3(s * 0.16f, 0.02f, -0.33f), new Vector3(0.24f, 0.2f, 0.1f), feather, false);
        // غطاء عيون من ورق الشجر (لما ترسمين دايرة)
        o.mask = new GameObject("Leaf Mask").transform;
        o.mask.SetParent(o.head, false);
        LevelBuilder.Visual("Mask", PrimitiveType.Cube, o.mask, new Vector3(0f, 0.03f, -0.38f), new Vector3(0.7f, 0.18f, 0.04f), leaf, false);
        LevelBuilder.Visual("Knot", PrimitiveType.Sphere, o.mask, new Vector3(0.38f, 0.05f, -0.1f), Vector3.one * 0.12f, leaf, false);
        o.mask.gameObject.SetActive(false);
        Material z = gm.Mat("Zzz", new Color(0.55f, 0.65f, 1f), 0.4f);
        for (int i = 0; i < 3; i++) o.zs.Add(LevelBuilder.Visual("Z", PrimitiveType.Cube, go.transform, Vector3.zero, new Vector3(0.16f, 0.04f, 0.02f), z, false).transform);
        // ضو المراقبة (مخروط أصفر على الطريق)
        var tex = new Texture2D(16, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int py = 0; py < 32; py++)
            for (int px = 0; px < 16; px++)
            {
                float width = Mathf.Lerp(0.15f, 1f, px / 15f);
                float a = Mathf.Clamp01(1f - Mathf.Abs(py / 31f - 0.5f) * 2f / width);
                tex.SetPixel(px, py, new Color(1f, 1f, 1f, a * a));
            }
        tex.Apply();
        o.beam = LevelBuilder.Visual("Watch Beam", PrimitiveType.Quad, go.transform, Vector3.zero, Vector3.one, NightSky.Transparent("Owl Beam", tex, new Color(1.6f, 1.4f, 0.5f, 0.28f)), false).transform;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;
        RowaidaController r = gm.Rowaida;

        // دايرة قريبة من راس البومة؟ تصير غطاء عيون
        if (!blind)
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                if (!m.closed || m.beingEaten) continue;
                var mr = m.GetComponent<MeshRenderer>();
                if (mr == null || Vector2.Distance(mr.bounds.center, head.position) > 1.8f) continue;
                Destroy(m.gameObject);
                blind = true;
                mask.gameObject.SetActive(true);
                LevelSfx.Play("owl", 0.5f, 0.8f);
                Fx.Sparkle(head.position, new Color(0.5f, 0.9f, 0.4f), 14);
                gm.UI.ShowMessage("", 2.2f, "البومة نامت! تقدرين تعدين");
                break;
            }

        bool watching = Watching;
        bool open = !blind && Local > Sleep;
        lids.gameObject.SetActive(!open);
        float look = watching ? Mathf.Sin((Local - Sleep) * 2.2f) * 25f : 0f;
        head.localRotation = Quaternion.Euler(watching ? 20f : 0f, look, open ? 0f : Mathf.Sin(t * 1.5f) * 6f);
        owl.localScale = new Vector3(1f, 1f + (open ? 0f : Mathf.Sin(t * 1.6f) * 0.03f), 1f);
        // Zzz وهي نايمة
        for (int i = 0; i < zs.Count; i++)
        {
            float k = Mathf.Repeat(t * 0.4f + i / 3f, 1f);
            zs[i].gameObject.SetActive(!open && k < 0.9f);
            zs[i].position = owl.position + new Vector3(0.5f + k * 0.5f, 0.9f + k * 1f, -0.3f);
            zs[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 3f + i) * 20f);
        }
        // الضو: من العيون للطريق، يتحرك مع راسها
        beam.gameObject.SetActive(watching);
        if (watching)
        {
            Vector3 eye = head.position + new Vector3(0f, 0f, -0.3f);
            Vector3 hit = new Vector3(x + look / 25f * range * 0.8f, top, 0f);
            Vector3 d = hit - eye;
            beam.position = (eye + hit) * 0.5f + new Vector3(0f, 0f, -0.4f);
            beam.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            beam.localScale = new Vector3(d.magnitude, range * 1.6f, 1f);
        }
        if (open && Local - Sleep < Time.deltaTime * 1.5f && r != null && Mathf.Abs(r.transform.position.x - x) < 12f) LevelSfx.Play("owl", 0.45f);

        // شافت رويدا؟ ترجعها لورا
        if (watching && r != null && gm.IsPlaying && t >= nextSpot)
        {
            float rx = r.transform.position.x;
            if (rx > x - range && rx < x + range && r.transform.position.y - top < 3f)
            {
                nextSpot = t + 0.8f;
                r.Knockback(-1.6f);
                LevelSfx.Play("owl", 0.9f, 1.2f);
                Fx.Sparkle(r.transform.position + Vector3.up * 0.8f, new Color(1f, 0.9f, 0.4f), 12);
                if (!spottedOnce) { spottedOnce = true; gm.UI.ShowMessage("", 2.4f, "البومة شافتك! استني لين تغمض عيونها"); }
            }
        }

        float cross = (range * 2f + 1.6f) / (r != null ? r.walkSpeed : 2.8f);
        bool wait = !blind && HoldHelper.Before(x - range) && (watching || UntilWatch < cross);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.4f, "البومة تراقب! استني لين تغمض... أو ارسمي دايرة على عيونها"); }
        HoldHelper.Hold(ref holding, wait);
    }
}

// ---------- حبل الكرمة ----------

// كرمة معلقة فوق حفرة، بس قصيرة. ارسمي خط تحتها يطولها، وبعدين تتأرجح ورويدا تمسكها وتتمرجح للجهة الثانية
public class VineSwing : MonoBehaviour
{
    const float ShortLen = 3.2f, LongLen = 4.9f, Amp = 50f, Period = 2.6f;
    float from, to, top, len = ShortLen, start;
    bool extended, holding, prompted, riding, done, pasta;
    Vector3 pivot;
    readonly List<Transform> segs = new List<Transform>();
    readonly List<Transform> leaves = new List<Transform>();
    Rigidbody rrb;
    float prevAngle;

    float Angle => extended ? Amp * Mathf.Sin((Time.time - start) * Mathf.PI * 2f / Period) : Mathf.Sin(Time.time * 1.3f) * 8f;
    Vector3 End(float ang) => pivot + new Vector3(Mathf.Sin(ang * Mathf.Deg2Rad), -Mathf.Cos(ang * Mathf.Deg2Rad), 0f) * len;

    public static void Create(Transform root, Vector3 spec, GameManager gm, bool pasta = false)
    {
        var go = new GameObject("Vine Swing");
        go.transform.SetParent(root, false);
        var v = go.AddComponent<VineSwing>();
        v.from = spec.x; v.to = spec.y; v.top = spec.z;
        v.pivot = new Vector3((v.from + v.to) * 0.5f, v.top + 5f, 0f);
        Material vine = pasta ? gm.Mat("Spaghetti", new Color(1f, 0.88f, 0.55f), 0.5f) : gm.Mat("Vine", new Color(0.35f, 0.55f, 0.25f), 0.3f);
        Material leaf = pasta ? gm.Mat("Tomato Sauce", new Color(0.9f, 0.25f, 0.15f), 0.7f) : gm.Mat("Leaf Light", new Color(0.55f, 0.85f, 0.35f), 0.35f);
        Material bark = gm.Mat("Tree Bark", new Color(0.5f, 0.33f, 0.2f), 0.2f);
        // غصن فوق
        var br = LevelBuilder.Visual("Vine Branch", PrimitiveType.Cylinder, go.transform, v.pivot + new Vector3(0f, 0.15f, 0.3f), new Vector3(0.35f, (v.to - v.from) * 0.5f + 1f, 0.35f), bark).transform;
        br.localRotation = Quaternion.Euler(0f, 0f, 90f);
        for (int i = 0; i < 4; i++)
            LevelBuilder.Visual("Leaves", PrimitiveType.Sphere, go.transform, v.pivot + new Vector3(-2f + i * 1.3f, 0.6f + (i % 2) * 0.3f, 0.8f), Vector3.one * 1.4f, gm.Mat("Leaf Green", new Color(0.35f, 0.72f, 0.3f), 0.35f));
        for (int i = 0; i < 16; i++)
        {
            v.segs.Add(LevelBuilder.Visual("Vine Seg", PrimitiveType.Capsule, go.transform, Vector3.zero, new Vector3(0.11f, 0.2f, 0.11f), vine, false).transform);
            if (i % 3 == 1) v.leaves.Add(LevelBuilder.Visual("Vine Leaf", PrimitiveType.Sphere, go.transform, Vector3.zero, new Vector3(0.28f, 0.08f, 0.16f), leaf, false).transform);
        }
        if (pasta)
        {
            // مصفاة مكرونة معلقة بدال الغصن، وخيوط مكرونة ثانية تتدلى
            br.gameObject.SetActive(false);
            Material steel = gm.Mat("Spoon Steel", new Color(0.82f, 0.84f, 0.88f), 0.95f, 0.85f);
            var col = LevelBuilder.Visual("Colander", PrimitiveType.Sphere, go.transform, v.pivot + new Vector3(0f, 0.6f, 0.2f), new Vector3(2.6f, 1.4f, 2.2f), steel).transform;
            for (int i = 0; i < 8; i++)
                LevelBuilder.Visual("Hole", PrimitiveType.Sphere, col, new Vector3(-0.35f + (i % 4) * 0.23f, -0.1f - (i / 4) * 0.15f, -0.48f), new Vector3(0.06f, 0.08f, 0.02f), gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f), false);
            LevelBuilder.Visual("Handle", PrimitiveType.Cube, go.transform, v.pivot + new Vector3(2f, 0.8f, 0.2f), new Vector3(1.6f, 0.15f, 0.3f), steel);
            for (int i = 0; i < 5; i++)
            {
                var strand = LevelBuilder.Visual("Strand", PrimitiveType.Capsule, go.transform, v.pivot + new Vector3(-0.9f + i * 0.45f, -0.6f - (i % 2) * 0.3f, 0.3f), new Vector3(0.08f, 0.7f + (i % 3) * 0.2f, 0.08f), vine, false).transform;
                strand.localRotation = Quaternion.Euler(0f, 0f, (i - 2) * 6f);
            }
            v.pasta = true;
        }
    }

    void FixedUpdate()
    {
        if (!riding || rrb == null) return;
        Vector3 want = End(Angle) + new Vector3(0f, -0.95f, 0f);
        Vector3 vel = (want - rrb.position) / Time.fixedDeltaTime;
        rrb.linearVelocity = vel;
        GameManager.Instance.Rowaida.CarryX = vel.x;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float ang = Angle;
        // الكرمة: قطع ورا بعض من الغصن لطرفها
        for (int i = 0; i < segs.Count; i++)
        {
            float k = (i + 0.5f) / segs.Count;
            float a = ang * k + (extended ? 0f : Mathf.Sin(Time.time * 2f + i * 0.4f) * 3f * k);
            Vector3 p = pivot + new Vector3(Mathf.Sin(ang * Mathf.Deg2Rad), -Mathf.Cos(ang * Mathf.Deg2Rad), 0f) * len * k;
            segs[i].position = p;
            segs[i].localRotation = Quaternion.Euler(0f, 0f, ang);
            segs[i].localScale = new Vector3(0.11f, len / segs.Count * 0.6f, 0.11f);
        }
        for (int i = 0; i < leaves.Count; i++)
        {
            Transform s = segs[i * 3 + 1];
            leaves[i].position = s.position + new Vector3(i % 2 == 0 ? 0.12f : -0.12f, 0f, -0.05f);
            leaves[i].localRotation = Quaternion.Euler(0f, 0f, ang + (i % 2 == 0 ? 30f : -30f));
        }

        // خط تحت طرف الكرمة؟ يطولها
        if (!extended)
        {
            Vector3 e = End(ang);
            Bounds b = new Bounds(new Vector3(e.x, e.y - 1f, 0f), new Vector3(2.4f, 2.6f, 4f));
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                var mr = m.GetComponent<MeshRenderer>();
                if (mr == null || !mr.bounds.Intersects(b)) continue;
                Destroy(m.gameObject);
                extended = true;
                len = LongLen;
                start = Time.time - Period * 0.25f;   // تبدا من اليمين وتروح لرويدا
                LevelSfx.Play("rope", 0.8f, 1.1f);
                Fx.Sparkle(End(Angle), new Color(0.6f, 1f, 0.45f), 14);
                break;
            }
        }

        RowaidaController r = gm.Rowaida;
        if (r != null && rrb == null) rrb = r.GetComponent<Rigidbody>();
        Vector3 end = End(ang);
        float dAng = ang - prevAngle;
        prevAngle = ang;

        // تمسك الكرمة لما توصل لها (الكرمة عند أقصى اليسار)
        if (!riding && !done && extended && holding && end.x < from - 0.1f && dAng >= -0.05f && r != null)
        {
            riding = true;
            rrb.useGravity = false;
            LevelSfx.Play("whoosh", 0.7f);
            gm.Sound.PlaySqueak(1.3f);
        }
        // تفلت عند أقصى اليمين فوق الصفحة الثانية
        if (riding && end.x > to + 0.1f && dAng <= 0.05f)
        {
            riding = false;
            done = true;
            rrb.useGravity = true;
            rrb.linearVelocity = new Vector3(2.8f, 2f, 0f);
            r.CarryX = 0f;
            r.Held = false;
            holding = false;
            Fx.Sparkle(r.transform.position, new Color(0.6f, 1f, 0.45f), 12);
        }
        if (riding) { r.Held = true; return; }

        bool wait = HoldHelper.Before(from) && !done && !RowaidaController.BridgeCovers(from, to, top);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.2f, pasta ? "المكرونة قصيرة! ارسمي خط تحتها يطولها وتمرجحي" : extended ? "استني الكرمة توصل لك!" : "الكرمة قصيرة! ارسمي خط تحتها يطولها"); }
        HoldHelper.Hold(ref holding, wait);
    }

    void OnDisable()
    {
        if (rrb != null) rrb.useGravity = true;
    }
}

// ---------- القنفذ ----------

// قنفذ ملفوف مثل كورة شوك يتدحرج رايح جاي على الطريق. ارسمي خط قدامه: يصطدم، يفك نفسه، يدوخ، ويروح للشجيرات
public class Hedgehog : MonoBehaviour
{
    float from, to, top, dir = 1f, stopT = -1f;
    bool holding, prompted;
    Transform ball, face, stars;
    float nextHit;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Hedgehog");
        go.transform.SetParent(root, false);
        var h = go.AddComponent<Hedgehog>();
        h.from = spec.x; h.to = spec.y; h.top = spec.z;
        go.transform.position = new Vector3((h.from + h.to) * 0.5f, h.top + 0.42f, 0f);
        Material spike = gm.Mat("Hedgehog Spike", new Color(0.45f, 0.32f, 0.22f), 0.3f);
        Material body = gm.Mat("Hedgehog Body", new Color(0.62f, 0.47f, 0.33f), 0.3f);
        Material faceM = gm.Mat("Hedgehog Face", new Color(0.98f, 0.88f, 0.72f), 0.3f);
        Material black = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        Material star = gm.Mat("Dizzy Star", new Color(1.5f, 1.3f, 0.3f), 0.5f);
        h.ball = new GameObject("Ball").transform;
        h.ball.SetParent(go.transform, false);
        LevelBuilder.Visual("Body", PrimitiveType.Sphere, h.ball, Vector3.zero, Vector3.one * 0.7f, body);
        for (int i = 0; i < 26; i++)
        {
            Vector3 d = Random.onUnitSphere;
            var s = LevelBuilder.Visual("Spike", PrimitiveType.Capsule, h.ball, d * 0.38f, new Vector3(0.07f, 0.16f, 0.07f), spike, false).transform;
            s.up = d;
        }
        h.face = new GameObject("Face").transform;
        h.face.SetParent(go.transform, false);
        LevelBuilder.Visual("Snout", PrimitiveType.Sphere, h.face, new Vector3(0.35f, -0.08f, 0f), new Vector3(0.35f, 0.28f, 0.3f), faceM, false);
        LevelBuilder.Visual("Nose", PrimitiveType.Sphere, h.face, new Vector3(0.52f, -0.06f, 0f), Vector3.one * 0.09f, black, false);
        LevelBuilder.Visual("Eye", PrimitiveType.Sphere, h.face, new Vector3(0.36f, 0.05f, -0.13f), Vector3.one * 0.07f, black, false);
        h.face.gameObject.SetActive(false);
        h.stars = new GameObject("Dizzy").transform;
        h.stars.SetParent(go.transform, false);
        for (int i = 0; i < 3; i++)
        {
            float a = i / 3f * Mathf.PI * 2f;
            LevelBuilder.Visual("Star", PrimitiveType.Cube, h.stars, new Vector3(Mathf.Cos(a) * 0.35f, 0f, Mathf.Sin(a) * 0.35f), Vector3.one * 0.1f, star, false).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }
        h.stars.localPosition = new Vector3(0.1f, 0.55f, 0f);
        h.stars.gameObject.SetActive(false);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        RowaidaController r = gm.Rowaida;
        Vector3 p = transform.position;
        if (stopT < 0f)
        {
            p.x += dir * 2.6f * dt;
            if (p.x > to - 0.4f) { dir = -1f; LevelSfx.Play("thud", 0.3f, 1.4f); }
            if (p.x < from + 0.4f) { dir = 1f; LevelSfx.Play("thud", 0.3f, 1.4f); }
            transform.position = p;
            ball.Rotate(0f, 0f, -dir * 2.6f / 0.35f * Mathf.Rad2Deg * dt, Space.World);
            // خط قدامه؟
            Bounds me = new Bounds(p, new Vector3(1.2f, 1.2f, 4f));
            foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
            {
                var mr = m.GetComponent<MeshRenderer>();
                if (mr == null || !mr.bounds.Intersects(me)) continue;
                Destroy(m.gameObject);
                stopT = 0f;
                LevelSfx.Play("thud", 0.8f);
                LevelSfx.Play("boing", 0.5f, 1.6f);
                Fx.Sparkle(p, new Color(1f, 0.9f, 0.4f), 14);
                break;
            }
            // لمس رويدا؟ تنشك وترجع
            if (r != null && gm.IsPlaying && t >= nextHit && Mathf.Abs(r.transform.position.x - p.x) < 0.65f && r.transform.position.y - top < 1.6f)
            {
                nextHit = t + 1.2f;
                r.Knockback(-1.4f);
                gm.Sound.PlaySqueak(1.5f);
                Fx.Sparkle(r.transform.position, new Color(0.8f, 0.6f, 0.4f), 10);
            }
        }
        else
        {
            stopT += dt;
            // يفك نفسه، يدوخ، ويمشي للشجيرات ورا
            ball.localRotation = Quaternion.Slerp(ball.localRotation, Quaternion.identity, dt * 6f);
            face.gameObject.SetActive(true);
            stars.gameObject.SetActive(stopT < 2.2f);
            stars.localRotation = Quaternion.Euler(0f, t * 300f, 0f);
            ball.localScale = new Vector3(1.25f, 0.85f, 1f);
            if (stopT > 2.2f)
            {
                p.z = Mathf.MoveTowards(p.z, 2f, dt * 1.2f);
                p.x += Mathf.Sin(t * 10f) * 0.002f;
                transform.position = p;
                transform.localRotation = Quaternion.Euler(0f, -60f, 0f);
                transform.localPosition += new Vector3(0f, Mathf.Abs(Mathf.Sin(t * 12f)) * 0.004f, 0f);
            }
        }

        bool wait = stopT < 0f && HoldHelper.Before(from);
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3f, "قنفذ يتدحرج! ارسمي خط قدامه يوقفه"); }
        HoldHelper.Hold(ref holding, wait);
    }
}

// ---------- بركة السلاحف ----------

// بركة فيها سلاحف تسبح. ارسمي دايرة بالمويه: تصير ورقة خس، والسلاحف يتجمعون صف وظهورهم تصير جسر شوي
public class TurtlePond : MonoBehaviour
{
    class Turtle { public Transform t; public BoxCollider col; public Vector3 wander; public float nextWander; public Transform[] legs; }

    const float FeedTime = 9f;
    float from, to, top, waterY, feedUntil;
    bool holding, prompted;
    readonly List<Turtle> turtles = new List<Turtle>();
    readonly Dictionary<InkMark, float> seen = new Dictionary<InkMark, float>();
    Material water;
    Transform lettuce;

    bool Feeding => lettuce != null && Time.time < feedUntil;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Turtle Pond");
        go.transform.SetParent(root, false);
        var p = go.AddComponent<TurtlePond>();
        p.from = spec.x; p.to = spec.y; p.top = spec.z;
        p.waterY = p.top - 0.9f;
        p.water = ForestFeatures2.PondWater(go.transform, p.from, p.to, p.waterY, out _);
        Material shell = gm.Mat("Turtle Shell", new Color(0.35f, 0.6f, 0.35f), 0.5f);
        Material shellRim = gm.Mat("Turtle Shell Rim", new Color(0.55f, 0.75f, 0.4f), 0.4f);
        Material skin = gm.Mat("Turtle Skin", new Color(0.6f, 0.78f, 0.45f), 0.35f);
        Material black = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        Material plate = gm.Mat("Turtle Plate", new Color(0.28f, 0.48f, 0.28f), 0.4f);
        int n = Mathf.Max(2, Mathf.RoundToInt((p.to - p.from) / 1.35f));
        for (int i = 0; i < n; i++)
        {
            var t = new GameObject("Turtle").transform;
            t.SetParent(go.transform, false);
            t.position = new Vector3(Random.Range(p.from + 0.6f, p.to - 0.6f), p.waterY - 0.15f, Random.Range(-4f, 6f));
            LevelBuilder.Visual("Shell", PrimitiveType.Sphere, t, new Vector3(0f, 0.12f, 0f), new Vector3(1.25f, 0.55f, 1.05f), shell);
            LevelBuilder.Visual("Rim", PrimitiveType.Cylinder, t, new Vector3(0f, -0.02f, 0f), new Vector3(1.3f, 0.05f, 1.1f), shellRim, false);
            for (int k = 0; k < 3; k++)
                LevelBuilder.Visual("Plate", PrimitiveType.Sphere, t, new Vector3(-0.3f + k * 0.3f, 0.36f, 0f), new Vector3(0.28f, 0.08f, 0.3f), plate, false);
            LevelBuilder.Visual("Head", PrimitiveType.Sphere, t, new Vector3(0.72f, 0.08f, 0f), new Vector3(0.32f, 0.26f, 0.26f), skin, false);
            LevelBuilder.Visual("Eye", PrimitiveType.Sphere, t, new Vector3(0.82f, 0.16f, -0.1f), Vector3.one * 0.06f, black, false);
            var legs = new Transform[4];
            for (int k = 0; k < 4; k++)
                legs[k] = LevelBuilder.Visual("Flipper", PrimitiveType.Sphere, t, new Vector3(k < 2 ? 0.4f : -0.4f, -0.05f, k % 2 == 0 ? -0.5f : 0.5f), new Vector3(0.3f, 0.08f, 0.18f), skin, false).transform;
            var col = t.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.12f, 0f);
            col.size = new Vector3(1.3f, 0.5f, 1.2f);
            col.enabled = false;
            var rb = t.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            p.turtles.Add(new Turtle { t = t, col = col, legs = legs, wander = t.position });
        }
        // ضفادع وقصب على الضفاف
        PondDecor(go.transform, p.from, p.to, p.top, p.waterY, gm);
    }

    public static void PondDecor(Transform parent, float from, float to, float top, float waterY, GameManager gm)
    {
        Material reed = gm.Mat("Reed", new Color(0.4f, 0.65f, 0.3f), 0.3f);
        Material cat = gm.Mat("Cattail", new Color(0.45f, 0.3f, 0.18f), 0.3f);
        Material frog = gm.Mat("Frog", new Color(0.45f, 0.78f, 0.35f), 0.45f);
        Material white = gm.Mat("Monster Eye", Color.white, 0.7f);
        Material black = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        Material lily = gm.Mat("Lily Pad", new Color(0.35f, 0.7f, 0.35f), 0.4f);
        for (int i = 0; i < 4; i++)
        {
            float ex = i % 2 == 0 ? from + 0.3f : to - 0.3f;
            var rd = LevelBuilder.Visual("Reed", PrimitiveType.Cylinder, parent, new Vector3(ex, waterY + 0.6f, 2f + i * 0.6f), new Vector3(0.05f, 0.65f, 0.05f), reed, false).transform;
            LevelBuilder.Visual("Cattail", PrimitiveType.Capsule, rd, new Vector3(0f, 0.85f, 0f), new Vector3(1.6f, 0.2f, 1.6f), cat, false);
        }
        for (int i = 0; i < 3; i++)
            LevelBuilder.Visual("Lily Pad", PrimitiveType.Cylinder, parent, new Vector3(from + (to - from) * (0.2f + i * 0.3f), waterY + 0.02f, 3f + i * 1.3f), new Vector3(0.8f, 0.02f, 0.8f), lily, false);
        // ضفدع على ورقة زنبق ورا
        var f = new GameObject("Frog").transform;
        f.SetParent(parent, false);
        f.position = new Vector3(from + (to - from) * 0.5f, waterY + 0.15f, 3f + 1.3f);
        LevelBuilder.Visual("Body", PrimitiveType.Sphere, f, Vector3.zero, new Vector3(0.5f, 0.32f, 0.42f), frog, false);
        for (int s = -1; s <= 1; s += 2)
        {
            LevelBuilder.Visual("Eye", PrimitiveType.Sphere, f, new Vector3(0.12f, 0.2f, s * 0.12f), Vector3.one * 0.14f, white, false);
            LevelBuilder.Visual("Pupil", PrimitiveType.Sphere, f, new Vector3(0.18f, 0.22f, s * 0.12f), Vector3.one * 0.07f, black, false);
        }
        f.localRotation = Quaternion.Euler(0f, 90f, 0f);
        f.gameObject.AddComponent<Bobber>();
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, dt = Time.deltaTime;
        water.SetTextureOffset("_BaseMap", new Vector2(Mathf.Sin(t * 0.3f) * 0.1f, t * 0.03f));
        RowaidaController r = gm.Rowaida;
        bool rowaidaOn = r != null && r.transform.position.x > from - 0.2f && r.transform.position.x < to + 0.2f;

        // دايرة بالمويه؟ تصير خس
        foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (!m.closed || m.beingEaten) continue;
            var mr = m.GetComponent<MeshRenderer>();
            if (mr == null || mr.bounds.center.x < from - 0.5f || mr.bounds.center.x > to + 0.5f) continue;
            Destroy(m.gameObject);
            if (lettuce != null) Destroy(lettuce.gameObject);
            lettuce = MakeLettuce(gm);
            feedUntil = t + FeedTime;
            LevelSfx.Play("splash", 0.6f, 1.2f);
            break;
        }
        ForestFeatures2.SwallowLines(from, to, top, seen, 1.3f);
        if (lettuce != null && t > feedUntil && !rowaidaOn) { Destroy(lettuce.gameObject); lettuce = null; }
        if (lettuce != null) lettuce.position = new Vector3(lettuce.position.x, waterY + 0.05f + Mathf.Sin(t * 2f) * 0.03f, 1.6f);

        // السلاحف: يسبحون عشوائي، أو يصفون عرض البركة وظهورهم فوق المويه
        bool feeding = lettuce != null;
        bool allIn = true;
        for (int i = 0; i < turtles.Count; i++)
        {
            Turtle tu = turtles[i];
            Vector3 p = tu.t.position;
            Vector3 target;
            if (feeding)
            {
                target = new Vector3(from + (i + 0.5f) * (to - from) / turtles.Count, top - 0.4f, 0f);
            }
            else
            {
                if (t > tu.nextWander) { tu.nextWander = t + Random.Range(2f, 4f); tu.wander = new Vector3(Random.Range(from + 0.6f, to - 0.6f), waterY - 0.15f, Random.Range(-4f, 6f)); }
                target = tu.wander;
            }
            p = Vector3.MoveTowards(p, target, (feeding ? 3.5f : 1.2f) * dt);
            bool inPlace = feeding && (p - target).sqrMagnitude < 0.01f;
            if (!inPlace) allIn = false;
            tu.col.enabled = inPlace;
            tu.t.GetComponent<Rigidbody>().MovePosition(p);
            tu.t.position = p;
            Vector3 dir = target - p;
            if (!inPlace && dir.sqrMagnitude > 0.001f) tu.t.rotation = Quaternion.Slerp(tu.t.rotation, Quaternion.Euler(0f, Mathf.Atan2(-dir.z, dir.x) * Mathf.Rad2Deg, 0f), dt * 4f);
            if (inPlace) tu.t.rotation = Quaternion.Slerp(tu.t.rotation, Quaternion.Euler(0f, 90f, 0f), dt * 4f);
            for (int k = 0; k < tu.legs.Length; k++) tu.legs[k].localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 6f + k * 1.5f + i) * 30f, 0f);
            if (inPlace && Random.value < 0.01f) LevelSfx.Play("munch", 0.3f, Random.Range(0.9f, 1.2f));
        }

        float cross = (to - from + 1.6f) / (r != null ? r.walkSpeed : 2.8f);
        bool ready = feeding && allIn && (feedUntil - t > cross || rowaidaOn);
        bool wait = HoldHelper.Before(from) && !ready;
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.4f, "بركة سلاحف! ارسمي دايرة بالمويه تصير خس... يتجمعون ويصيرون جسر"); }
        HoldHelper.Hold(ref holding, wait);
    }

    Transform MakeLettuce(GameManager gm)
    {
        var l = new GameObject("Lettuce").transform;
        l.SetParent(transform, false);
        l.position = new Vector3((from + to) * 0.5f, waterY + 0.05f, 1.6f);
        Material leaf = gm.Mat("Lettuce", new Color(0.55f, 0.88f, 0.4f), 0.4f);
        Material leaf2 = gm.Mat("Lettuce Light", new Color(0.75f, 0.95f, 0.55f), 0.4f);
        for (int i = 0; i < 6; i++)
        {
            float a = i / 6f * Mathf.PI * 2f;
            var lf = LevelBuilder.Visual("Leaf", PrimitiveType.Sphere, l, new Vector3(Mathf.Cos(a) * 0.25f, 0.05f, Mathf.Sin(a) * 0.25f), new Vector3(0.45f, 0.12f, 0.35f), i % 2 == 0 ? leaf : leaf2, false).transform;
            lf.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 15f);
        }
        Fx.Sparkle(l.position + Vector3.up * 0.3f, new Color(0.6f, 1f, 0.5f), 12);
        return l;
    }
}

// شي صغير يطلع وينزل (ضفدع على ورقة زنبق)، وينق أحيانًا
public class Bobber : MonoBehaviour
{
    Vector3 home;
    float next;
    void Start() { home = transform.position; next = Time.time + Random.Range(2f, 6f); }
    void Update()
    {
        float t = Time.time;
        transform.position = home + Vector3.up * Mathf.Sin(t * 2f) * 0.03f;
        if (t > next)
        {
            next = t + Random.Range(4f, 8f);
            transform.localScale = new Vector3(1f, 1.15f, 1f);
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.Rowaida != null && Mathf.Abs(gm.Rowaida.transform.position.x - home.x) < 10f) LevelSfx.Play("croak", 0.35f, Random.Range(0.9f, 1.2f));
        }
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * 4f);
    }
}

// ---------- حجار تطلع وتغطس ----------

// بركة فيها حجار: كل حجر يطلع ويغطس ورا بعض. رويدا تمشي عليها وتستنى على الحجر لين اللي بعده يطلع.
// والبركة تبلع أي خط ينرسم فوقها
public class SteppingStones : MonoBehaviour
{
    const float Lag = 0.55f;
    float from, to, top, period, start, waterY, stepW;
    bool holding, prompted;
    readonly List<Rigidbody> stones = new List<Rigidbody>();
    readonly Dictionary<InkMark, float> seen = new Dictionary<InkMark, float>();
    readonly List<Transform> ripples = new List<Transform>();
    Material water;

    public static void Create(Transform root, Vector3 spec, GameManager gm)
    {
        var go = new GameObject("Stepping Stones");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<SteppingStones>();
        s.from = spec.x; s.to = spec.y; s.period = spec.z;
        s.top = 0f;
        s.waterY = s.top - 0.75f;
        s.start = Time.time;
        s.water = ForestFeatures2.PondWater(go.transform, s.from, s.to, s.waterY, out _);
        int n = Mathf.Max(2, Mathf.RoundToInt((s.to - s.from) / 1.5f));
        s.stepW = (s.to - s.from) / n;
        Material rock = gm.Mat("Stepping Rock", new Color(0.62f, 0.6f, 0.58f), 0.35f);
        Material rockDark = gm.Mat("Stepping Rock Dark", new Color(0.48f, 0.47f, 0.45f), 0.3f);
        Material moss = gm.Mat("Moss", new Color(0.4f, 0.65f, 0.3f), 0.2f);
        Material ripple = gm.Mat("Ripple", new Color(0.85f, 0.95f, 1f), 0.6f);
        for (int i = 0; i < n; i++)
        {
            var st = new GameObject("Stone " + i);
            st.transform.SetParent(go.transform, false);
            st.transform.position = new Vector3(s.from + (i + 0.5f) * s.stepW, s.top, 0f);
            LevelBuilder.Visual("Rock", PrimitiveType.Sphere, st.transform, new Vector3(0f, -0.62f, 0f), new Vector3(s.stepW - 0.1f, 1.25f, 1.5f), rock);
            LevelBuilder.Visual("Rock Lump", PrimitiveType.Sphere, st.transform, new Vector3(0.25f, -0.35f, 0.35f), new Vector3(0.6f, 0.5f, 0.6f), rockDark, false);
            LevelBuilder.Visual("Wet Shine", PrimitiveType.Sphere, st.transform, new Vector3(-0.2f, -0.06f, -0.35f), new Vector3(0.4f, 0.06f, 0.25f), gm.Mat("Wet Shine", new Color(0.85f, 0.9f, 0.95f), 0.95f), false);
            if (i % 2 == 0) LevelBuilder.Visual("Moss", PrimitiveType.Sphere, st.transform, new Vector3(0.2f, 0.02f, 0.4f), new Vector3(0.5f, 0.1f, 0.4f), moss, false);
            var col = st.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, -0.6f, 0f);
            col.size = new Vector3(s.stepW - 0.15f, 1.2f, 1.4f);
            var rb = st.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            s.stones.Add(rb);
            var rp = LevelBuilder.Visual("Ripple", PrimitiveType.Cylinder, go.transform, new Vector3(st.transform.position.x, s.waterY + 0.02f, 0f), new Vector3(1f, 0.01f, 1f), ripple, false).transform;
            s.ripples.Add(rp);
        }
        TurtlePond.PondDecor(go.transform, s.from, s.to, s.top, s.waterY, gm);
    }

    // ارتفاع سطح الحجر: فوق (مستوى الأرض) أغلب الوقت، وبعدين يغطس ويطلع
    float TopAt(int i, float t)
    {
        float ph = Mathf.Repeat((t - start) / period - i * Lag / period, 1f);
        float up;
        if (ph < 0.55f) up = 1f;
        else if (ph < 0.67f) up = 1f - (ph - 0.55f) / 0.12f;
        else if (ph < 0.88f) up = 0f;
        else up = (ph - 0.88f) / 0.12f;
        return top - 1.25f * (1f - Mathf.SmoothStep(0f, 1f, up));
    }

    void FixedUpdate()
    {
        float t = Time.time;
        for (int i = 0; i < stones.Count; i++)
        {
            Vector3 p = stones[i].position;
            stones[i].MovePosition(new Vector3(p.x, TopAt(i, t), p.z));
        }
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;
        water.SetTextureOffset("_BaseMap", new Vector2(t * 0.02f, Mathf.Sin(t * 0.4f) * 0.05f));
        ForestFeatures2.SwallowLines(from, to, top, seen, 1.2f);
        for (int i = 0; i < ripples.Count; i++)
        {
            float h = TopAt(i, t);
            bool moving = h < top - 0.05f && h > top - 1.2f;
            float k = Mathf.Repeat(t * 1.5f + i * 0.3f, 1f);
            ripples[i].localScale = moving ? new Vector3(0.8f + k * 1.5f, 0.01f, 0.8f + k * 1.2f) : Vector3.zero;
        }

        RowaidaController r = gm.Rowaida;
        bool wait = false;
        if (r != null && gm.IsPlaying)
        {
            float x = r.transform.position.x;
            if (x > from - 1.6f && x < to + 0.2f)
            {
                int cur = Mathf.FloorToInt((x - from) / stepW);
                int next = Mathf.FloorToInt((x + 0.5f - from) / stepW);
                float curTop = cur < 0 || cur >= stones.Count ? top : TopAt(cur, t);
                float nextTop = next < 0 || next >= stones.Count ? top : TopAt(next, t + 0.4f);
                float nextNow = next < 0 || next >= stones.Count ? top : TopAt(next, t);
                if (next != cur && (Mathf.Abs(nextTop - curTop) > 0.2f || Mathf.Abs(nextNow - curTop) > 0.2f)) wait = true;
                // ما تمشي على حجر بيغطس قريب
                if (next != cur && next >= 0 && next < stones.Count && TopAt(next, t + 1.4f) < top - 0.2f && curTop > top - 0.1f) wait = true;
            }
        }
        if (wait && !prompted) { prompted = true; gm.UI.ShowMessage("", 3.2f, "الحجار تطلع وتغطس! رويدا تنط عليها لما تطلع... والمويه تبلع الخطوط"); }
        HoldHelper.Hold(ref holding, wait);
    }
}
