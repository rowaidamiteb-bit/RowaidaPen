using System.Collections.Generic;
using UnityEngine;

// سلايم عملاق (تجربة خربت) يلحق رويدا من ورا ويبلع كل شي: إذا وصلها تخسر قلب
public class SlimeChase : MonoBehaviour
{
    const float RushSpeed = 4f, CloseSpeed = 2.4f, WaitSpeed = 1.1f;
    float endX, delay = 3f, nextBlob, nextRumble, nextGloop;
    Transform body;
    readonly List<Transform> blobs = new List<Transform>();
    readonly List<Transform> eyes = new List<Transform>();
    Material slime, drip;

    float Front => transform.position.x + 2.2f;

    public static void Create(Transform root, float startX, float endX, GameManager gm)
    {
        var go = new GameObject("Slime Chase");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(startX, 0f, 0f);
        var s = go.AddComponent<SlimeChase>();
        s.endX = endX;
        s.Build(gm);
        gm.Respawned += s.OnRespawned;
    }

    void Build(GameManager gm)
    {
        var tex = new Texture2D(2, 2);
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        slime = NightSky.Transparent("Big Slime", tex, new Color(0.4f, 1.4f, 0.5f, 0.82f));
        drip = gm.Mat("Slime Core", new Color(0.25f, 0.75f, 0.3f), 0.8f);
        Material white = gm.Mat("Monster Eye", Color.white, 0.7f);
        Material black = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        body = new GameObject("Body").transform;
        body.SetParent(transform, false);
        LevelBuilder.Visual("Core", PrimitiveType.Sphere, body, new Vector3(0f, 2.5f, 0f), new Vector3(4f, 5.5f, 3.5f), drip, false);
        for (int i = 0; i < 9; i++)
        {
            var b = LevelBuilder.Visual("Blob", PrimitiveType.Sphere, body, new Vector3(Random.Range(-1.2f, 1.6f), Random.Range(0.5f, 5f), Random.Range(-1.2f, 1.2f)), Vector3.one * Random.Range(2f, 3.2f), slime, false).transform;
            blobs.Add(b);
        }
        // عيون كثيرة تتحرك
        for (int i = 0; i < 3; i++)
        {
            var e = new GameObject("Eye").transform;
            e.SetParent(body, false);
            e.localPosition = new Vector3(1.3f + (i % 2) * 0.3f, 2.6f + i * 0.9f, -1.6f);
            LevelBuilder.Visual("White", PrimitiveType.Sphere, e, Vector3.zero, Vector3.one * (0.7f - i * 0.12f), white, false);
            LevelBuilder.Visual("Pupil", PrimitiveType.Sphere, e, new Vector3(0.12f, 0f, -0.25f), Vector3.one * (0.3f - i * 0.05f), black, false);
            eyes.Add(e);
        }
        LevelBuilder.Visual("Mouth", PrimitiveType.Sphere, body, new Vector3(1.6f, 1.6f, -1.5f), new Vector3(1.4f, 0.6f, 0.3f), black, false);
        body.localPosition = new Vector3(0f, -1.5f, 0f);
    }

    void OnDestroy() { if (GameManager.Instance != null) GameManager.Instance.Respawned -= OnRespawned; }

    void OnRespawned(Vector3 pos)
    {
        transform.position = new Vector3(pos.x - 13f, 0f, 0f);
        delay = 2.5f;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time;
        // يتموج ويتنفس
        for (int i = 0; i < blobs.Count; i++) blobs[i].localScale = Vector3.one * (2.5f + Mathf.Sin(t * 3f + i * 1.3f) * 0.35f);
        body.localScale = new Vector3(1f + Mathf.Sin(t * 4f) * 0.04f, 1f + Mathf.Cos(t * 4f) * 0.04f, 1f);
        for (int i = 0; i < eyes.Count; i++) eyes[i].localRotation = Quaternion.Euler(Mathf.Sin(t * 2f + i) * 15f, Mathf.Sin(t * 1.5f + i * 2f) * 20f, 0f);

        if (!gm.IsPlaying) return;
        if (delay > 0f) { delay -= Time.deltaTime; return; }
        RowaidaController r = gm.Rowaida;
        float gap = r != null ? r.transform.position.x - Front : 99f;
        float speed = r == null ? RushSpeed : r.Held ? (gap < 6f ? WaitSpeed : RushSpeed) : (gap < 3.5f ? CloseSpeed : RushSpeed);
        // لو رويدا واقفة تستنى عقبة، السلايم يقرب ويوقف وراها (يخوف بس ما ياكلها)
        if (r != null && r.Held && gap < 2.4f) speed = 0f;
        if (Front < endX) transform.position += new Vector3(speed * Time.deltaTime, 0f, 0f);

        // قطرات سلايم تطيح
        if (t >= nextBlob)
        {
            nextBlob = t + 0.05f;
            var d = LevelBuilder.Visual("Slime Drop", PrimitiveType.Sphere, null, new Vector3(Front - Random.Range(0f, 1f), Random.Range(0.5f, 4f), Random.Range(-1.5f, 1.5f)), Vector3.one * Random.Range(0.15f, 0.35f), slime, false);
            var fx = d.AddComponent<FxParticle>();
            fx.velocity = new Vector3(Random.Range(-1f, 2f), Random.Range(0f, 2f), 0f);
            fx.gravity = 9f; fx.life = 0.8f;
        }
        // يبلع الحبر اللي يعديه
        foreach (InkMark mark in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            var mr = mark.GetComponent<MeshRenderer>();
            if (mr != null && mr.bounds.min.x < Front - 0.3f) { Fx.Sparkle(mr.bounds.center, new Color(0.5f, 1f, 0.5f), 6); Destroy(mark.gameObject); }
        }
        if (r == null) return;
        if (gap < 9f) CameraFollow.ShiftRequest = -Mathf.Lerp(3.5f, 1f, gap / 9f);
        if (t >= nextGloop && gap < 12f) { nextGloop = t + 1.1f; LevelSfx.Play("gloop", Mathf.Lerp(0.8f, 0.2f, gap / 12f), 0.6f); }
        if (t >= nextRumble && gap < 5f)
        {
            nextRumble = t + 0.3f;
            var cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null) cam.Shake(Mathf.Lerp(0.08f, 0.02f, gap / 5f), 0.3f);
        }
        if (gap < 0.2f && r.transform.position.y > -3f)
        {
            Fx.Sparkle(r.transform.position, new Color(0.5f, 1f, 0.5f), 20);
            gm.KillRowaida();
        }
    }
}

// منظر المختبر: سبورة عليها معادلات، رفوف دوارق تضوي وتغلي، جزيئات تلف، لولب DNA، جدول دوري، أنابيب سوائل على الجدار،
// مواقد بنسن، ميكروسكوب، معطف مختبر، ودوارق صغيرة على أطراف الصفحات
public class LabScenery : MonoBehaviour
{
    readonly List<Transform> spinners = new List<Transform>();
    readonly List<Vector3> spinAxis = new List<Vector3>();
    readonly List<Material> flow = new List<Material>();
    readonly List<Vector3> bubbleAt = new List<Vector3>();
    readonly List<Color> bubbleCol = new List<Color>();
    Transform dna;

    public static void Create(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        var go = new GameObject("Lab Scenery");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<LabScenery>();
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        float minX = float.MaxValue, maxX = float.MinValue;
        foreach (Block b in data.blocks) { minX = Mathf.Min(minX, b.center.x - b.size.x * 0.5f); maxX = Mathf.Max(maxX, b.center.x + b.size.x * 0.5f); }
        bool night = data.lab == 2;
        Transform p = go.transform;
        float len = maxX - minX + 40f, cx = (minX + maxX) * 0.5f;

        // جدار مختبر (بلاط أبيض صغير) وأرضية بنش طويلة ورا
        var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
                tex.SetPixel(x, y, x == 0 || y == 0 ? (night ? new Color(0.2f, 0.25f, 0.3f) : new Color(0.78f, 0.85f, 0.9f)) : (night ? new Color(0.13f, 0.16f, 0.2f) : new Color(0.95f, 0.97f, 0.99f)));
        tex.Apply();
        var wall = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Lab Wall" };
        wall.SetTexture("_BaseMap", tex);
        wall.SetColor("_BaseColor", Color.white);
        wall.SetTextureScale("_BaseMap", new Vector2(len / 1.2f, 16f / 1.2f));
        LevelBuilder.Visual("Lab Wall", PrimitiveType.Quad, p, new Vector3(cx, 4f, 10f), new Vector3(len, 16f, 1f), wall, false);
        Material bench = gm.Mat("Lab Bench", night ? new Color(0.2f, 0.22f, 0.28f) : new Color(0.25f, 0.3f, 0.38f), 0.6f);
        Material benchTop = gm.Mat("Lab Bench Top", new Color(0.12f, 0.12f, 0.15f), 0.8f);
        LevelBuilder.Visual("Bench", PrimitiveType.Cube, p, new Vector3(cx, -1.5f, 8f), new Vector3(len, 3f, 2f), bench, false);
        LevelBuilder.Visual("Bench Top", PrimitiveType.Cube, p, new Vector3(cx, 0.05f, 8f), new Vector3(len, 0.15f, 2.2f), benchTop, false);

        Material chalk = gm.Mat("Chalkboard", new Color(0.12f, 0.28f, 0.22f), 0.2f);
        Material woodF = gm.Mat("Shelf Wood", new Color(0.75f, 0.55f, 0.35f), 0.3f);
        Material steel = gm.Mat("Spoon Steel", new Color(0.82f, 0.84f, 0.88f), 0.95f, 0.85f);
        Material glass = LabFeatures.Glass(0.3f);
        Color[] potion = { new Color(1f, 0.25f, 0.4f), new Color(0.3f, 0.9f, 0.4f), new Color(0.3f, 0.6f, 1f), new Color(1f, 0.8f, 0.2f), new Color(0.75f, 0.35f, 1f), new Color(0.2f, 0.95f, 0.9f) };
        string[] formulas = { "H2O", "E = mc2", "NaCl", "2H2 + O2 -> 2H2O", "CO2", "C6H12O6", "pH = 7", "F = ma", "O2", "H+ + OH- -> H2O" };
        Font font = ArabicText.Font;

        int k = 0;
        for (float x = minX - 10f; x < maxX + 10f; x += R(10f, 14f), k++)
        {
            // سبورة بمعادلات
            if (k % 2 == 0)
            {
                LevelBuilder.Visual("Chalkboard", PrimitiveType.Cube, p, new Vector3(x, 4.6f, 9.85f), new Vector3(5f, 2.8f, 0.1f), chalk, false);
                LevelBuilder.Visual("Board Frame", PrimitiveType.Cube, p, new Vector3(x, 4.6f, 9.9f), new Vector3(5.3f, 3.1f, 0.08f), woodF, false);
                for (int f = 0; f < 3; f++)
                {
                    var tm = new GameObject("Formula").AddComponent<TextMesh>();
                    tm.transform.SetParent(p, false);
                    tm.transform.position = new Vector3(x - 2.1f, 5.6f - f * 0.8f, 9.78f);
                    tm.font = font;
                    tm.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                    tm.text = formulas[(k * 3 + f) % formulas.Length];
                    tm.fontSize = 64;
                    tm.characterSize = 0.06f;
                    tm.anchor = TextAnchor.UpperLeft;
                    tm.color = new Color(0.95f, 0.95f, 0.9f);
                }
                // رسمة جزيء بالطباشير
                LevelBuilder.Visual("Chalk Atom", PrimitiveType.Sphere, p, new Vector3(x + 1.6f, 4.4f, 9.78f), new Vector3(0.4f, 0.4f, 0.02f), gm.Mat("Chalk", new Color(0.95f, 0.95f, 0.9f), 0.2f), false);
            }
            else
            {
                // جدول دوري (مربعات ملونة)
                for (int row = 0; row < 4; row++)
                    for (int col = 0; col < 9; col++)
                    {
                        if (row == 0 && col > 0 && col < 8) continue;
                        Material m = gm.Mat("Element " + (row + col) % 6, potion[(row + col) % 6] * 0.85f, 0.4f);
                        LevelBuilder.Visual("Element", PrimitiveType.Cube, p, new Vector3(x - 2f + col * 0.5f, 5.8f - row * 0.5f, 9.85f), new Vector3(0.44f, 0.44f, 0.04f), m, false);
                    }
            }
            // رف دوارق تضوي وتغلي
            float sy = R(2.2f, 2.8f);
            LevelBuilder.Visual("Shelf", PrimitiveType.Cube, p, new Vector3(x + 5.5f, sy, 9.3f), new Vector3(3.6f, 0.15f, 1f), woodF, false);
            for (int f = 0; f < 4; f++)
            {
                Color c = potion[(k + f) % potion.Length];
                Vector3 fp = new Vector3(x + 4.2f + f * 0.9f, sy + 0.08f, 9.2f);
                float h = R(0.5f, 0.9f);
                LevelBuilder.Visual("Potion", PrimitiveType.Cylinder, p, fp + Vector3.up * h * 0.4f, new Vector3(0.45f, h * 0.4f, 0.45f), LabFeatures.Glow(gm, c, night ? 1.8f : 1.2f), false);
                LevelBuilder.Visual("Bottle", PrimitiveType.Cylinder, p, fp + Vector3.up * h * 0.5f, new Vector3(0.55f, h * 0.5f, 0.55f), glass, false);
                LevelBuilder.Visual("Cork", PrimitiveType.Cylinder, p, fp + Vector3.up * (h + 0.06f), new Vector3(0.3f, 0.06f, 0.3f), gm.Mat("Cork", new Color(0.75f, 0.55f, 0.35f), 0.2f), false);
                s.bubbleAt.Add(fp + Vector3.up * h * 0.8f);
                s.bubbleCol.Add(c);
            }
            // جزيء (كور وعصي) يلف
            var mol = new GameObject("Molecule").transform;
            mol.SetParent(p, false);
            mol.position = new Vector3(x + R(-2f, 2f), R(7f, 8.5f), R(6f, 8f));
            Color mc = potion[k % potion.Length];
            LevelBuilder.Visual("Atom", PrimitiveType.Sphere, mol, Vector3.zero, Vector3.one * 0.7f, LabFeatures.Glow(gm, mc, 1f), false);
            for (int a = 0; a < 3; a++)
            {
                Vector3 dir = Quaternion.Euler(0f, a * 120f, 35f) * Vector3.right;
                var bond = LevelBuilder.Visual("Bond", PrimitiveType.Cylinder, mol, dir * 0.55f, new Vector3(0.1f, 0.35f, 0.1f), steel, false).transform;
                bond.up = dir;
                LevelBuilder.Visual("Atom", PrimitiveType.Sphere, mol, dir * 1.05f, Vector3.one * 0.42f, LabFeatures.Glow(gm, potion[(k + a + 1) % potion.Length], 1f), false);
            }
            s.spinners.Add(mol);
            s.spinAxis.Add(new Vector3(R(10f, 30f), R(20f, 50f), 0f));
            // أنبوب سوائل على الجدار (يجري)
            var ft = new Texture2D(4, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
            Color pc = potion[(k + 2) % potion.Length];
            for (int y = 0; y < 32; y++) for (int x2 = 0; x2 < 4; x2++) ft.SetPixel(x2, y, Color.Lerp(pc * 0.6f, pc * 1.6f, Mathf.Repeat(y / 8f, 1f) < 0.5f ? 1f : 0f));
            ft.Apply();
            var fm = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Pipe Flow" };
            fm.SetTexture("_BaseMap", ft);
            fm.SetColor("_BaseColor", Color.white);
            fm.SetTextureScale("_BaseMap", new Vector2(1f, 4f));
            s.flow.Add(fm);
            var pipe = LevelBuilder.Visual("Wall Pipe", PrimitiveType.Cylinder, p, new Vector3(x + 2.5f, 1.8f, 9.6f), new Vector3(0.25f, 4f, 0.25f), fm, false).transform;
            pipe.localRotation = Quaternion.Euler(0f, 0f, 90f);
            // موقد بنسن ودورق فوقه على البنش
            Vector3 bb = new Vector3(x + R(-3f, 0f), 0.13f, 7.6f);
            LevelBuilder.Visual("Burner Base", PrimitiveType.Cylinder, p, bb + Vector3.up * 0.05f, new Vector3(0.5f, 0.05f, 0.5f), steel, false);
            LevelBuilder.Visual("Burner Tube", PrimitiveType.Cylinder, p, bb + Vector3.up * 0.4f, new Vector3(0.15f, 0.35f, 0.15f), steel, false);
            LevelBuilder.Visual("Burner Flame", PrimitiveType.Capsule, p, bb + Vector3.up * 0.95f, new Vector3(0.15f, 0.22f, 0.15f), LabFeatures.Glow(gm, new Color(0.4f, 0.6f, 1.2f), 1.6f), false);
            LevelBuilder.Visual("Tripod", PrimitiveType.Cylinder, p, bb + Vector3.up * 1.25f, new Vector3(0.7f, 0.02f, 0.7f), steel, false);
            Color bc = potion[(k + 3) % potion.Length];
            LevelBuilder.Visual("Beaker Liquid", PrimitiveType.Cylinder, p, bb + Vector3.up * 1.5f, new Vector3(0.55f, 0.2f, 0.55f), LabFeatures.Glow(gm, bc, 1.4f), false);
            LevelBuilder.Visual("Beaker", PrimitiveType.Cylinder, p, bb + Vector3.up * 1.65f, new Vector3(0.65f, 0.4f, 0.65f), glass, false);
            s.bubbleAt.Add(bb + Vector3.up * 1.8f);
            s.bubbleCol.Add(bc);
            // ميكروسكوب أحيانًا
            if (k % 3 == 1)
            {
                Vector3 mp = new Vector3(x + 3f, 0.13f, 7.7f);
                Material white = gm.Mat("Fridge White", new Color(0.92f, 0.96f, 0.98f), 0.7f);
                LevelBuilder.Visual("Scope Base", PrimitiveType.Cube, p, mp + new Vector3(0f, 0.1f, 0f), new Vector3(0.9f, 0.2f, 0.7f), white, false);
                var arm = LevelBuilder.Visual("Scope Arm", PrimitiveType.Cube, p, mp + new Vector3(0.2f, 0.8f, 0f), new Vector3(0.2f, 1.2f, 0.25f), white, false).transform;
                arm.localRotation = Quaternion.Euler(0f, 0f, 15f);
                var eye = LevelBuilder.Visual("Scope Tube", PrimitiveType.Cylinder, p, mp + new Vector3(-0.1f, 1.3f, 0f), new Vector3(0.22f, 0.45f, 0.22f), steel, false).transform;
                eye.localRotation = Quaternion.Euler(0f, 0f, -25f);
                LevelBuilder.Visual("Stage", PrimitiveType.Cube, p, mp + new Vector3(-0.1f, 0.6f, 0f), new Vector3(0.6f, 0.05f, 0.5f), white, false);
            }
            // معطف مختبر ونظارة معلقين
            if (k % 3 == 2)
            {
                Vector3 hp = new Vector3(x - 4f, 3.4f, 9.7f);
                Material coat = gm.Mat("Lab Coat", new Color(0.97f, 0.97f, 0.98f), 0.3f);
                LevelBuilder.Visual("Hook", PrimitiveType.Sphere, p, hp + Vector3.up * 0.9f, Vector3.one * 0.15f, steel, false);
                LevelBuilder.Visual("Coat", PrimitiveType.Capsule, p, hp, new Vector3(1f, 1f, 0.2f), coat, false);
                LevelBuilder.Visual("Collar", PrimitiveType.Cube, p, hp + Vector3.up * 0.75f, new Vector3(0.6f, 0.15f, 0.22f), coat, false);
                LevelBuilder.Visual("Goggles", PrimitiveType.Capsule, p, hp + new Vector3(0f, 0.3f, -0.15f), new Vector3(0.18f, 0.35f, 0.1f), LabFeatures.Glow(gm, new Color(0.3f, 0.8f, 1f), 0.9f), false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
        }
        // لولب DNA كبير يلف بالخلفية
        s.dna = new GameObject("DNA").transform;
        s.dna.SetParent(p, false);
        s.dna.position = new Vector3(cx + 4f, 1f, 14f);
        Material dnaA = LabFeatures.Glow(gm, new Color(0.3f, 0.7f, 1f), night ? 1.8f : 1.1f);
        Material dnaB = LabFeatures.Glow(gm, new Color(1f, 0.4f, 0.7f), night ? 1.8f : 1.1f);
        for (int i = 0; i < 26; i++)
        {
            float a = i * 0.5f, y = i * 0.45f;
            Vector3 pa = new Vector3(Mathf.Cos(a) * 1.3f, y, Mathf.Sin(a) * 1.3f), pb = -new Vector3(pa.x, -y, pa.z);
            LevelBuilder.Visual("Strand A", PrimitiveType.Sphere, s.dna, pa, Vector3.one * 0.4f, dnaA, false);
            LevelBuilder.Visual("Strand B", PrimitiveType.Sphere, s.dna, pb, Vector3.one * 0.4f, dnaB, false);
            if (i % 2 == 0)
            {
                var rung = LevelBuilder.Visual("Rung", PrimitiveType.Cylinder, s.dna, (pa + pb) * 0.5f, new Vector3(0.08f, 1.3f, 0.08f), steel, false).transform;
                rung.up = (pa - pb).normalized;
            }
        }
        // دوارق صغيرة وأنابيب اختبار على أطراف الصفحات (ورا)
        foreach (Block b in data.blocks)
        {
            float top = b.center.y + b.size.y * 0.5f, x0 = b.center.x - b.size.x * 0.5f, x1 = b.center.x + b.size.x * 0.5f;
            for (float x = x0 + 0.8f; x < x1 - 0.6f; x += R(1.8f, 3.2f))
            {
                Color c = potion[rng.Next(potion.Length)];
                Vector3 bp = new Vector3(x, top, R(1.3f, 1.8f));
                if (rng.Next(2) == 0)
                {
                    LevelBuilder.Visual("Mini Liquid", PrimitiveType.Sphere, p, bp + Vector3.up * 0.2f, new Vector3(0.4f, 0.3f, 0.4f), LabFeatures.Glow(gm, c, 1.3f), false);
                    LevelBuilder.Visual("Mini Flask", PrimitiveType.Sphere, p, bp + Vector3.up * 0.25f, Vector3.one * 0.5f, glass, false);
                    LevelBuilder.Visual("Mini Neck", PrimitiveType.Cylinder, p, bp + Vector3.up * 0.6f, new Vector3(0.15f, 0.15f, 0.15f), glass, false);
                }
                else
                {
                    LevelBuilder.Visual("Rack", PrimitiveType.Cube, p, bp + Vector3.up * 0.25f, new Vector3(0.9f, 0.08f, 0.3f), woodF, false);
                    for (int tt = 0; tt < 3; tt++)
                    {
                        LevelBuilder.Visual("Tube Liquid", PrimitiveType.Cylinder, p, bp + new Vector3(-0.3f + tt * 0.3f, 0.2f, 0f), new Vector3(0.12f, 0.15f, 0.12f), LabFeatures.Glow(gm, potion[(tt + rng.Next(3)) % potion.Length], 1.3f), false);
                        LevelBuilder.Visual("Test Tube", PrimitiveType.Cylinder, p, bp + new Vector3(-0.3f + tt * 0.3f, 0.35f, 0f), new Vector3(0.15f, 0.35f, 0.15f), glass, false);
                    }
                }
            }
        }
    }

    void Update()
    {
        float t = Time.time, dt = Time.deltaTime;
        for (int i = 0; i < spinners.Count; i++) spinners[i].Rotate(spinAxis[i] * dt, Space.Self);
        if (dna != null) dna.Rotate(0f, 25f * dt, 0f, Space.Self);
        for (int i = 0; i < flow.Count; i++) flow[i].SetTextureOffset("_BaseMap", new Vector2(0f, -t * 0.8f));
        // فقاعات تطلع من الدوارق
        GameManager gm = GameManager.Instance;
        if (gm != null && bubbleAt.Count > 0 && Random.value < 0.35f)
        {
            int i = Random.Range(0, bubbleAt.Count);
            var b = LevelBuilder.Visual("Potion Bubble", PrimitiveType.Sphere, null, bubbleAt[i] + new Vector3(Random.Range(-0.15f, 0.15f), 0f, 0f), Vector3.one * Random.Range(0.06f, 0.14f), LabFeatures.Glow(gm, bubbleCol[i], 1.6f), false);
            var fx = b.AddComponent<FxParticle>();
            fx.velocity = Vector3.up * Random.Range(0.5f, 1.2f); fx.life = 0.9f; fx.flutter = 0.3f;
        }
    }
}
