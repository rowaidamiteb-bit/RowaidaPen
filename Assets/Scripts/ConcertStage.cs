using System.Collections.Generic;
using UnityEngine;

// اللحن الأخير: المرحلة كلها مسرح حفلة.
// ستارة ترتفع بالبداية، جمهور قدام يتمايل ويوقف يصفق لما يكتمل لحن، أضواء تتحرك (وحدة تلحق رويدا)،
// آلات ورا الطريق (هارب، تشيلو، ترمبيت، حوامل نوتات)، نجوم ورق معلقة، وقائد أوركسترا جنب الباب
public class ConcertStage : MonoBehaviour
{
    class Fan { public Transform t; public float baseY, phase; }

    readonly List<Fan> audience = new List<Fan>();
    readonly List<Transform> beams = new List<Transform>();
    readonly List<Vector3> beamTops = new List<Vector3>();
    readonly List<float> beamX = new List<float>();
    readonly List<Transform> stars = new List<Transform>();
    readonly List<Transform> pleats = new List<Transform>();
    readonly List<Transform> harpStrings = new List<Transform>();
    Transform frontCurtain, armL, armR, baton, conductor;
    Light follow;
    float start, cheerUntil, minX, maxX;
    LevelData data;

    public static void Build(Transform root, LevelData data, GameManager gm)
    {
        if (!data.concertStage) return;
        var go = new GameObject("Concert Stage");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<ConcertStage>();
        s.data = data;
        s.start = Time.time;
        s.minX = data.blocks[0].center.x - data.blocks[0].size.x * 0.5f;
        Block last = data.blocks[data.blocks.Count - 1];
        s.maxX = last.center.x + last.size.x * 0.5f;
        s.BuildCurtains(gm);
        s.BuildAudience(gm);
        s.BuildLights(gm);
        s.BuildInstruments(gm);
        s.BuildStars(gm);
        s.BuildConductor(gm);
        StaffBridge.Completed += s.Cheer;
    }

    void OnDestroy() { StaffBridge.Completed -= Cheer; }

    // ---------- البناء ----------

    void BuildCurtains(GameManager gm)
    {
        Material velvet = gm.Mat("Curtain Velvet", new Color(0.62f, 0.08f, 0.14f), 0.35f);
        Material dark = gm.Mat("Curtain Dark", new Color(0.42f, 0.04f, 0.09f), 0.3f);
        Material gold = gm.Mat("Gear Brass", new Color(0.85f, 0.65f, 0.3f), 0.7f, 0.5f);

        // ستارة قدام بالبداية: ترتفع أول ما تبدا المرحلة
        frontCurtain = new GameObject("Front Curtain").transform;
        frontCurtain.SetParent(transform, false);
        frontCurtain.position = new Vector3(data.spawn.x + 3f, 3.2f, -2.4f);
        for (int i = 0; i < 22; i++)
        {
            var p = LevelBuilder.Visual("Pleat", PrimitiveType.Capsule, frontCurtain, new Vector3(-6.5f + i * 0.62f, 0f, (i % 2) * 0.12f), new Vector3(0.7f, 4.6f, 0.4f), i % 2 == 0 ? velvet : dark, false).transform;
            pleats.Add(p);
        }
        LevelBuilder.Visual("Fringe", PrimitiveType.Cube, frontCurtain, new Vector3(0f, -4.5f, -0.2f), new Vector3(14f, 0.2f, 0.2f), gold, false);

        // ستارة حول الباب (مسرح النهاية)
        float dx = data.door.x;
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 5; i++)
            {
                var p = LevelBuilder.Visual("Side Pleat", PrimitiveType.Capsule, transform, new Vector3(dx + side * (2.6f + i * 0.45f), 3.6f, 1.2f + i * 0.08f), new Vector3(0.55f, 4f, 0.35f), i % 2 == 0 ? velvet : dark, false).transform;
                pleats.Add(p);
            }
        LevelBuilder.Visual("Valance", PrimitiveType.Cube, transform, new Vector3(dx, 7.3f, 1.1f), new Vector3(9f, 0.9f, 0.5f), velvet);
        LevelBuilder.Visual("Valance Trim", PrimitiveType.Cube, transform, new Vector3(dx, 6.82f, 0.85f), new Vector3(9f, 0.12f, 0.1f), gold, false);
        for (int side = -1; side <= 1; side += 2)
            LevelBuilder.Visual("Tassel", PrimitiveType.Sphere, transform, new Vector3(dx + side * 2.7f, 2.4f, 0.9f), new Vector3(0.25f, 0.4f, 0.25f), gold, false);
    }

    void BuildAudience(GameManager gm)
    {
        Color[] shirts = { new Color(0.3f, 0.45f, 0.85f), new Color(0.9f, 0.45f, 0.4f), new Color(0.4f, 0.75f, 0.5f), new Color(0.95f, 0.8f, 0.35f), new Color(0.6f, 0.45f, 0.8f) };
        Color[] hair = { new Color(0.12f, 0.08f, 0.06f), new Color(0.45f, 0.28f, 0.15f), new Color(0.9f, 0.75f, 0.4f), new Color(0.2f, 0.2f, 0.22f) };
        Material skin = gm.Mat("Fan Skin", new Color(0.98f, 0.85f, 0.72f), 0.3f);
        var rng = new System.Random(7);
        for (float x = minX - 2f; x < maxX + 2f; x += 0.95f + (float)rng.NextDouble() * 0.4f)
        {
            var f = new GameObject("Fan").transform;
            f.SetParent(transform, false);
            float row = rng.Next(2);
            f.position = new Vector3(x + row * 0.45f, -1.25f - row * 0.3f, -2.9f - row * 0.6f);
            Material shirt = gm.Mat("Fan Shirt " + rng.Next(5), shirts[rng.Next(shirts.Length)], 0.3f);
            LevelBuilder.Visual("Body", PrimitiveType.Capsule, f, new Vector3(0f, 0f, 0f), new Vector3(0.7f, 0.55f, 0.5f), shirt, false);
            LevelBuilder.Visual("Head", PrimitiveType.Sphere, f, new Vector3(0f, 0.75f, 0f), Vector3.one * 0.5f, skin, false);
            LevelBuilder.Visual("Hair", PrimitiveType.Sphere, f, new Vector3(0f, 0.86f, 0.06f), new Vector3(0.54f, 0.38f, 0.52f), gm.Mat("Fan Hair " + rng.Next(4), hair[rng.Next(hair.Length)], 0.3f), false);
            // بعضهم ماسكين عصي ضوئية
            if (rng.Next(4) == 0)
            {
                var stick = LevelBuilder.Visual("Glow Stick", PrimitiveType.Cylinder, f, new Vector3(0.35f, 0.9f, -0.1f), new Vector3(0.08f, 0.35f, 0.08f), gm.Mat("Glow " + rng.Next(5), MusicFeatures.KeyColors[rng.Next(5)] * 1.8f, 0.5f), false).transform;
                stick.localRotation = Quaternion.Euler(0f, 0f, -20f);
            }
            audience.Add(new Fan { t = f, baseY = f.position.y, phase = (float)rng.NextDouble() * 6.28f });
        }
    }

    void BuildLights(GameManager gm)
    {
        var tex = new Texture2D(16, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 16; x++)
            {
                float across = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(y / 31f - 0.5f) * 2f);
                float along = Mathf.Lerp(0.35f, 1f, x / 15f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, across * along));
            }
        tex.Apply();
        Color[] cols = { new Color(1.5f, 1.35f, 0.8f, 0.22f), new Color(0.9f, 0.6f, 1.5f, 0.18f), new Color(0.6f, 1.1f, 1.5f, 0.18f) };
        for (int i = 0; i < 3; i++)
        {
            var mat = NightSky.Transparent("Stage Beam " + i, tex, cols[i]);
            beams.Add(LevelBuilder.Visual("Stage Beam", PrimitiveType.Quad, transform, Vector3.zero, Vector3.one, mat, false).transform);
            beamTops.Add(new Vector3(0f, 11f, 1.5f));
            beamX.Add(data.spawn.x);
        }
        follow = new GameObject("Spot Light").AddComponent<Light>();
        follow.transform.SetParent(transform, false);
        follow.type = LightType.Point;
        follow.color = new Color(1f, 0.92f, 0.7f);
        follow.range = 5f;
        follow.intensity = 2.2f;
    }

    void BuildInstruments(GameManager gm)
    {
        Material wood = gm.Mat("Guitar Wood", new Color(0.55f, 0.3f, 0.15f), 0.5f);
        Material gold = gm.Mat("Gear Brass", new Color(0.85f, 0.65f, 0.3f), 0.7f, 0.5f);
        Material black = gm.Mat("Piano Wood", new Color(0.15f, 0.12f, 0.14f), 0.7f);
        Material paper = gm.Mat("Sheet Paper", new Color(0.98f, 0.97f, 0.92f), 0.2f);
        Material str = gm.Mat("Harp String", new Color(0.95f, 0.9f, 0.7f), 0.6f);
        var pages = data.blocks;

        // هارب على أول صفحة
        Vector3 h = Top(pages[0], 0.25f, 1.5f);
        var harp = new GameObject("Harp").transform;
        harp.SetParent(transform, false);
        harp.position = h;
        LevelBuilder.Visual("Pillar", PrimitiveType.Cylinder, harp, new Vector3(-0.6f, 1.3f, 0f), new Vector3(0.18f, 1.3f, 0.18f), gold);
        var neck = LevelBuilder.Visual("Neck", PrimitiveType.Cube, harp, new Vector3(0f, 2.55f, 0f), new Vector3(1.3f, 0.18f, 0.14f), gold).transform;
        neck.localRotation = Quaternion.Euler(0f, 0f, -12f);
        var body = LevelBuilder.Visual("Soundboard", PrimitiveType.Cube, harp, new Vector3(0.15f, 1.2f, 0f), new Vector3(0.22f, 2.5f, 0.22f), wood).transform;
        body.localRotation = Quaternion.Euler(0f, 0f, 28f);
        for (int i = 0; i < 7; i++)
        {
            float x = -0.45f + i * 0.14f;
            float topY = 2.45f - (x + 0.6f) * 0.2f, botY = 0.25f + (x + 0.6f) * 1.6f;
            var s = LevelBuilder.Visual("String", PrimitiveType.Cube, harp, new Vector3(x, (topY + botY) * 0.5f, 0f), new Vector3(0.02f, topY - botY, 0.02f), str, false).transform;
            harpStrings.Add(s);
        }

        // تشيلو على الصفحة الثانية
        if (pages.Count > 1)
        {
            var cello = new GameObject("Cello").transform;
            cello.SetParent(transform, false);
            cello.position = Top(pages[1], 0.7f, 1.6f);
            cello.localRotation = Quaternion.Euler(0f, 0f, 8f);
            LevelBuilder.Visual("Lower", PrimitiveType.Sphere, cello, new Vector3(0f, 0.55f, 0f), new Vector3(1f, 0.95f, 0.35f), wood);
            LevelBuilder.Visual("Upper", PrimitiveType.Sphere, cello, new Vector3(0f, 1.3f, 0f), new Vector3(0.8f, 0.8f, 0.33f), wood);
            LevelBuilder.Visual("Neck", PrimitiveType.Cube, cello, new Vector3(0f, 2.2f, 0f), new Vector3(0.12f, 1.3f, 0.1f), black);
            LevelBuilder.Visual("Scroll", PrimitiveType.Sphere, cello, new Vector3(0f, 2.9f, 0f), Vector3.one * 0.2f, black);
            for (int i = -1; i <= 1; i += 2)
                LevelBuilder.Visual("F Hole", PrimitiveType.Cube, cello, new Vector3(i * 0.22f, 0.8f, -0.17f), new Vector3(0.04f, 0.3f, 0.01f), black, false);
            var bow = LevelBuilder.Visual("Bow", PrimitiveType.Cube, cello, new Vector3(0.3f, 0.9f, -0.25f), new Vector3(1.6f, 0.04f, 0.04f), wood, false).transform;
            bow.localRotation = Quaternion.Euler(0f, 0f, -18f);
        }

        // ترمبيت على حامل بالصفحة الثالثة
        if (pages.Count > 2)
        {
            var tr = new GameObject("Trumpet").transform;
            tr.SetParent(transform, false);
            tr.position = Top(pages[2], 0.65f, 1.4f) + Vector3.up * 1.1f;
            LevelBuilder.Visual("Stand", PrimitiveType.Cylinder, tr, new Vector3(0f, -0.55f, 0f), new Vector3(0.06f, 0.55f, 0.06f), black);
            var tube = LevelBuilder.Visual("Tube", PrimitiveType.Cylinder, tr, Vector3.zero, new Vector3(0.12f, 0.6f, 0.12f), gold).transform;
            tube.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var bell = LevelBuilder.Visual("Bell", PrimitiveType.Cylinder, tr, new Vector3(0.7f, 0f, 0f), new Vector3(0.45f, 0.12f, 0.45f), gold).transform;
            bell.localRotation = Quaternion.Euler(0f, 0f, 90f);
            for (int i = 0; i < 3; i++)
                LevelBuilder.Visual("Valve", PrimitiveType.Cylinder, tr, new Vector3(-0.15f + i * 0.15f, 0.15f, 0f), new Vector3(0.07f, 0.12f, 0.07f), gold, false);
        }

        // حوامل نوتات على كل صفحة
        foreach (Block b in pages)
        {
            Vector3 p = Top(b, 0.45f, 0.9f);
            if (Mathf.Abs(p.x - data.door.x) < 3f) continue;
            var stand = new GameObject("Music Stand").transform;
            stand.SetParent(transform, false);
            stand.position = p;
            LevelBuilder.Visual("Pole", PrimitiveType.Cylinder, stand, new Vector3(0f, 0.55f, 0f), new Vector3(0.05f, 0.55f, 0.05f), black);
            var desk = LevelBuilder.Visual("Desk", PrimitiveType.Cube, stand, new Vector3(0f, 1.2f, 0f), new Vector3(0.8f, 0.55f, 0.04f), black).transform;
            desk.localRotation = Quaternion.Euler(-15f, 0f, 0f);
            var sheet = LevelBuilder.Visual("Sheet", PrimitiveType.Cube, desk, new Vector3(0f, 0.05f, -0.8f), new Vector3(0.9f, 0.9f, 0.5f), paper, false).transform;
            for (int i = 0; i < 3; i++)
                LevelBuilder.Visual("Line", PrimitiveType.Cube, sheet, new Vector3(0f, 0.3f - i * 0.25f, -0.6f), new Vector3(0.85f, 0.03f, 0.2f), black, false);
        }
    }

    void BuildStars(GameManager gm)
    {
        Material star = gm.Mat("Paper Star", new Color(1.3f, 1.1f, 0.4f), 0.4f);
        Material thread = gm.Mat("Thread", new Color(0.8f, 0.8f, 0.85f), 0.2f);
        var rng = new System.Random(11);
        for (float x = minX; x < maxX; x += 3.5f + (float)rng.NextDouble() * 2f)
        {
            float y = 6.5f + (float)rng.NextDouble() * 2.5f;
            var s = new GameObject("Hanging Star").transform;
            s.SetParent(transform, false);
            s.position = new Vector3(x, y, 2.2f);
            LevelBuilder.Visual("Thread", PrimitiveType.Cube, s, new Vector3(0f, (12f - y) * 0.5f, 0f), new Vector3(0.02f, 12f - y, 0.02f), thread, false);
            LevelBuilder.Visual("Point A", PrimitiveType.Cube, s, Vector3.zero, new Vector3(0.45f, 0.45f, 0.05f), star, false).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            LevelBuilder.Visual("Point B", PrimitiveType.Cube, s, Vector3.zero, new Vector3(0.45f, 0.45f, 0.05f), star, false);
            stars.Add(s);
        }
    }

    void BuildConductor(GameManager gm)
    {
        Material suit = gm.Mat("Piano Wood", new Color(0.15f, 0.12f, 0.14f), 0.7f);
        Material skin = gm.Mat("Fan Skin", new Color(0.98f, 0.85f, 0.72f), 0.3f);
        Material white = gm.Mat("Monster Eye", Color.white, 0.7f);
        Material podium = gm.Mat("Curtain Velvet", new Color(0.62f, 0.08f, 0.14f), 0.35f);
        conductor = new GameObject("Conductor").transform;
        conductor.SetParent(transform, false);
        Block last = data.blocks[data.blocks.Count - 1];
        float top = last.center.y + last.size.y * 0.5f;
        conductor.position = new Vector3(data.door.x + 1.8f, top, 0.7f);
        LevelBuilder.Visual("Podium", PrimitiveType.Cylinder, conductor, new Vector3(0f, 0.2f, 0f), new Vector3(0.9f, 0.2f, 0.9f), podium);
        LevelBuilder.Visual("Legs", PrimitiveType.Capsule, conductor, new Vector3(0f, 0.75f, 0f), new Vector3(0.35f, 0.4f, 0.3f), suit);
        LevelBuilder.Visual("Coat", PrimitiveType.Capsule, conductor, new Vector3(0f, 1.25f, 0f), new Vector3(0.5f, 0.45f, 0.4f), suit);
        LevelBuilder.Visual("Shirt", PrimitiveType.Cube, conductor, new Vector3(0f, 1.4f, -0.2f), new Vector3(0.14f, 0.3f, 0.02f), white, false);
        LevelBuilder.Visual("Bow Tie", PrimitiveType.Cube, conductor, new Vector3(0f, 1.55f, -0.22f), new Vector3(0.2f, 0.07f, 0.02f), suit, false);
        LevelBuilder.Visual("Head", PrimitiveType.Sphere, conductor, new Vector3(0f, 1.9f, 0f), Vector3.one * 0.42f, skin);
        LevelBuilder.Visual("Hair", PrimitiveType.Sphere, conductor, new Vector3(0f, 2.05f, 0.05f), new Vector3(0.48f, 0.3f, 0.45f), gm.Mat("Fan Hair 3", new Color(0.85f, 0.85f, 0.88f), 0.3f), false);
        for (int s = -1; s <= 1; s += 2)
        {
            var arm = new GameObject("Arm").transform;
            arm.SetParent(conductor, false);
            arm.localPosition = new Vector3(s * 0.25f, 1.45f, 0f);
            LevelBuilder.Visual("Sleeve", PrimitiveType.Cube, arm, new Vector3(s * 0.25f, 0f, 0f), new Vector3(0.5f, 0.12f, 0.12f), suit, false);
            LevelBuilder.Visual("Hand", PrimitiveType.Sphere, arm, new Vector3(s * 0.52f, 0f, 0f), Vector3.one * 0.13f, skin, false);
            if (s < 0) armL = arm; else armR = arm;
        }
        baton = LevelBuilder.Visual("Baton", PrimitiveType.Cube, armR, new Vector3(0.75f, 0.1f, 0f), new Vector3(0.45f, 0.03f, 0.03f), white, false).transform;
        baton.localRotation = Quaternion.Euler(0f, 0f, 25f);
    }

    Vector3 Top(Block b, float along, float z)
    {
        float x0 = b.center.x - b.size.x * 0.5f, x1 = b.center.x + b.size.x * 0.5f;
        return new Vector3(Mathf.Lerp(x0 + 0.8f, x1 - 0.8f, along), b.center.y + b.size.y * 0.5f, z);
    }

    // ---------- الحركة ----------

    void Cheer(Vector3 at)
    {
        cheerUntil = Time.time + 2.6f;
        LevelSfx.Play("applause", 0.9f);
        GameManager gm = GameManager.Instance;
        for (int i = 0; i < 40; i++)
        {
            var bit = LevelBuilder.Visual("Confetti", PrimitiveType.Cube, null, at + new Vector3(Random.Range(-2f, 2f), Random.Range(0f, 2f), -0.5f), Vector3.one * 0.14f,
                gm.Mat("Crayon Fall " + i % 4, MusicFeatures.KeyColors[i % 4], 0.4f), false);
            bit.transform.rotation = Random.rotation;
            var fx = bit.AddComponent<FxParticle>();
            fx.velocity = new Vector3(Random.Range(-3f, 3f), Random.Range(3f, 7f), Random.Range(-1f, 1f));
            fx.gravity = 5f; fx.drag = 0.8f; fx.flutter = 1f;
            fx.spin = Random.insideUnitSphere * 400f;
            fx.life = 2f;
        }
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        float t = Time.time, age = t - start;
        bool cheering = t < cheerUntil;
        Vector3 rp = gm.Rowaida != null ? gm.Rowaida.transform.position : data.spawn;
        float beat = t * Mathf.PI * 2f / 0.8f;   // نبضة الموسيقى

        // الستارة ترتفع بأول المرحلة
        float lift = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - 0.3f) / 1.6f));
        frontCurtain.position = new Vector3(frontCurtain.position.x, 3.2f + lift * 10f, frontCurtain.position.z);
        frontCurtain.gameObject.SetActive(lift < 0.999f);
        for (int i = 0; i < pleats.Count; i++)
            pleats[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.2f + i * 0.7f) * 1.5f);

        // الجمهور: يتمايل مع النبضة، ولما يكتمل لحن ينط ويصفق
        foreach (Fan f in audience)
        {
            float bob = cheering ? Mathf.Abs(Mathf.Sin(t * 9f + f.phase)) * 0.45f : Mathf.Max(0f, Mathf.Sin(beat + f.phase * 0.3f)) * 0.07f;
            f.t.position = new Vector3(f.t.position.x, f.baseY + bob, f.t.position.z);
            f.t.localRotation = Quaternion.Euler(0f, 0f, cheering ? Mathf.Sin(t * 12f + f.phase) * 8f : Mathf.Sin(t * 1.3f + f.phase) * 6f);
        }

        // الأضواء: وحدة تلحق رويدا، والثنتين يتحركون يمين ويسار
        for (int i = 0; i < beams.Count; i++)
        {
            float target = i == 0 ? rp.x : rp.x + Mathf.Sin(t * (0.5f + i * 0.2f) + i * 2f) * 7f;
            beamX[i] = Mathf.Lerp(beamX[i], target, Time.deltaTime * (i == 0 ? 2.5f : 1f));
            Vector3 topP = new Vector3(beamX[i] + (i - 1) * 4f, 11f, 1.5f);
            Vector3 hit = new Vector3(beamX[i], 0f, 0f);
            Vector3 d = hit - topP;
            beams[i].position = (topP + hit) * 0.5f + new Vector3(0f, 0f, -0.3f);
            beams[i].rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + 180f);
            beams[i].localScale = new Vector3(d.magnitude * 1.05f, (cheering ? 2.4f : 1.6f) + Mathf.Sin(t * 3f + i) * 0.1f, 1f);
        }
        follow.transform.position = new Vector3(beamX[0], 1.6f, -0.8f);
        follow.intensity = cheering ? 3.5f : 2.2f;

        // النجوم تلف شوي وتلمع
        for (int i = 0; i < stars.Count; i++)
        {
            stars[i].localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.8f + i) * 40f, 0f);
            stars[i].localScale = Vector3.one * (1f + Mathf.Max(0f, Mathf.Sin(t * 2f + i * 1.7f)) * 0.15f);
        }

        // أوتار الهارب تهتز
        for (int i = 0; i < harpStrings.Count; i++)
            harpStrings[i].localPosition = new Vector3(harpStrings[i].localPosition.x, harpStrings[i].localPosition.y, Mathf.Sin(t * 30f + i * 1.3f) * 0.015f);

        // قائد الأوركسترا: يرسم الإيقاع بالعصا، ويرفع يدينه لما يصفقون
        float swing = Mathf.Sin(beat);
        armR.localRotation = Quaternion.Euler(0f, 0f, cheering ? 70f : 20f + swing * 35f);
        armL.localRotation = Quaternion.Euler(0f, 0f, cheering ? -70f : -10f - Mathf.Sin(beat * 0.5f) * 15f);
        conductor.localRotation = Quaternion.Euler(0f, 180f + Mathf.Sin(beat * 0.5f) * 10f, 0f);
    }
}
