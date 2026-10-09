using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// يبني المرحلة من الكود: صفحات ورق سميكة بخطوط رصاص وشقوق، سلك الدفتر، الباب، ورويدا
public static class LevelBuilder
{
    const float PencilRadius = 0.016f;

    static readonly Dictionary<PrimitiveType, Mesh> meshCache = new Dictionary<PrimitiveType, Mesh>();
    static PhysicsMaterial slippery;
    static Mesh ringMesh;

    public static GameObject Build(LevelData data, GameManager gm)
    {
        var root = new GameObject("Level - " + data.title);
        var rng = new System.Random(data.title.GetHashCode());
        if (data.vanishTiles.Count > 0) BuildVanishingPages(root.transform, data, gm, rng);
        else
            for (int i = 0; i < data.blocks.Count; i++)
                CreatePaperBlock(root.transform, data.blocks[i], gm, rng, data, i == 0, i);
        if (data.bridgeHint && data.blocks.Count > 1) BridgeHint(root.transform, data.blocks[0], data.blocks[1], gm);
        foreach (HintLine h in data.hints) DashedHint(root.transform, h, gm);
        CreateDoor(root.transform, data.door, gm, data.flipped);
        CreateRowaida(root.transform, data.spawn, gm, data.flipped);
        SpawnMonsters(root.transform, data, gm);
        if (data.Flood) InkFlood.Spawn(root.transform, data, gm);
        foreach (FoldSpec f in data.folds) FoldingFlap.Spawn(root.transform, f, gm);
        BuildFloaters(root.transform, data, gm, rng);
        return root;
    }

    // الصفحة المتلاشية: الأجزاء اللي بتختفي تصير قطع لحالها
    static void BuildVanishingPages(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        bool first = true;
        for (int bi = 0; bi < data.blocks.Count; bi++)
        {
            Block b = data.blocks[bi];
            float left = b.center.x - b.size.x * 0.5f, right = b.center.x + b.size.x * 0.5f;
            float top = b.center.y + b.size.y * 0.5f, bottom = b.center.y - b.size.y * 0.5f;
            float x = left;
            foreach (Vector3 v in data.vanishTiles)
            {
                if (v.y <= left || v.x >= right) continue;
                float h0 = Mathf.Max(v.x, left), h1 = Mathf.Min(v.y, right);
                if (h0 > x) CreatePaperBlock(root, Segment(x, h0, top, bottom, b.size.z), gm, rng, data, first, bi);
                first = false;
                GameObject seg = CreatePaperBlock(root, Segment(h0, h1, top, bottom, b.size.z), gm, rng, data, false);
                VanishingTile.Attach(seg, h0, v.z);
                x = h1;
            }
            if (right > x) CreatePaperBlock(root, Segment(x, right, top, bottom, b.size.z), gm, rng, data, first, bi);
            first = false;
        }
    }

    // الأرض الطايرة: صفحات صغيرة تتحرك في سما الدفتر
    static void BuildFloaters(Transform root, LevelData data, GameManager gm, System.Random rng)
    {
        foreach (FloaterSpec f in data.floaters)
        {
            Transform mover = FloatingPlatform.Create(root, f);
            CreatePaperBlock(mover, new Block(new Vector3(f.centerX, -0.5f, 0f), new Vector3(f.width, 1f, 3.2f)), gm, rng, data, false);
        }
    }

    static Block Segment(float from, float to, float top, float bottom, float depth)
    {
        return new Block(new Vector3((from + to) * 0.5f, (top + bottom) * 0.5f, 0f), new Vector3(to - from, top - bottom, depth));
    }

    // وحوش الشخابيط: كل وحش يطلع فوق الصفحة اللي تحته
    static void SpawnMonsters(Transform root, LevelData data, GameManager gm)
    {
        foreach (MonsterSpawn m in data.monsters)
        {
            float top = 0f;
            foreach (Block b in data.blocks)
                if (m.x >= b.center.x - b.size.x * 0.5f && m.x <= b.center.x + b.size.x * 0.5f)
                    top = b.center.y + (data.flipped ? -0.5f : 0.5f) * b.size.y;
            bool hint = data.monsterHint && m.Equals(data.monsters[0]);
            ScribbleMonster.Spawn(root, m.kind, new Vector3(m.x, top, 0f), m.patrolMin, m.patrolMax, gm, hint);
        }
    }

    // خلفية حالمة: سما ليلية (نجوم وغيوم وهلال) + أوراق صغيرة طايرة
    public static void BuildBackdrop(GameManager gm)
    {
        new GameObject("Night Sky").AddComponent<NightSky>().Build();

        var dust = new GameObject("Floating Papers").AddComponent<FloatingPapers>();
        dust.Build(gm);
    }

    // صفحة ورق سميكة: سطح ورق، جوانب صفحات مكدسة، خطوط رصاص على الحواف، شقوق، وسلك الدفتر
    static int pageCounter;

    static GameObject CreatePaperBlock(Transform parent, Block b, GameManager gm, System.Random rng, LevelData data, bool first, int index = -1)
    {
        var go = new GameObject("Paper Page");
        go.transform.SetParent(parent, false);
        go.transform.position = b.center;
        // الصفحة المقلوبة: نقلب الصفحة عشان الرسومات والأغراض تكون على السطح اللي تحت
        if (data.flipped) go.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
        go.AddComponent<MeshFilter>().sharedMesh = PaperMeshes.PaperSlab(b.size);
        var top = new Material(gm.paperMaterial) { name = "Page Drawing" };
        if (index < 0) index = pageCounter++;
        top.SetTexture("_BaseMap", DrawPageArt(b, rng, data, first, index));
        if (data.Rain)
        {
            // ورق مبلول: أغمق شوي ولامع
            top.SetColor("_BaseColor", new Color(0.84f, 0.87f, 0.95f));
            top.SetFloat("_Smoothness", 0.6f);
        }
        go.AddComponent<MeshRenderer>().sharedMaterials = new[] { top, gm.pageEdgeMaterial };
        go.AddComponent<BoxCollider>().size = b.size;

        Vector3 h = b.size * 0.5f;
        Transform t = go.transform;

        OutlineBox(t, b.size, gm, rng);
        PlaceProps(t, b, gm, rng, data);
        if (data.Rain) Puddles(t, b, gm, rng);

        // شقوق عند أطراف الصفحة (جنب الحفر)
        for (int side = -1; side <= 1; side += 2)
        {
            int cracks = 2 + rng.Next(2);
            for (int i = 0; i < cracks; i++)
            {
                float z = Mathf.Lerp(-h.z * 0.8f, h.z * 0.8f, (float)rng.NextDouble());
                Crack(t, new Vector3(side * h.x, h.y, z), -side, gm, rng);
            }
        }

        if (data.theme.snowCaps) SnowCap(t, b.size, gm, rng);

        // سلك الدفتر الحلزوني على الحافة الخلفية
        if (!data.theme.rings) return go;
        if (ringMesh == null) ringMesh = PaperMeshes.Ring(0.26f, 0.03f);
        for (float x = -h.x + 0.5f; x <= h.x - 0.4f; x += 0.55f)
        {
            var ring = new GameObject("Binding Ring");
            ring.transform.SetParent(t, false);
            ring.transform.localPosition = new Vector3(x, h.y - 0.05f, h.z + 0.02f);
            ring.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            ring.AddComponent<MeshFilter>().sharedMesh = ringMesh;
            ring.AddComponent<MeshRenderer>().sharedMaterial = gm.metalMaterial;
        }
        return go;
    }

    // خطوط رصاص على حواف صندوق (مهزوزة شوي كأنها بالقلم)
    public static void OutlineBox(Transform t, Vector3 size, GameManager gm, System.Random rng)
    {
        Vector3 h = size * 0.5f;
        for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
                PencilLine(t, new Vector3(-h.x, y * h.y, z * h.z), new Vector3(h.x, y * h.y, z * h.z), gm, rng);
        for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
                PencilLine(t, new Vector3(x * h.x, -h.y, z * h.z), new Vector3(x * h.x, h.y, z * h.z), gm, rng);
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                PencilLine(t, new Vector3(x * h.x, y * h.y, -h.z), new Vector3(x * h.x, y * h.y, h.z), gm, rng);
    }

    // رسومات الرصاص على سطح الصفحة: رسومات صغيرة، تظليل عند الحفر، لطخات، وآثار مساحة
    static Texture2D DrawPageArt(Block b, System.Random rng, LevelData data, bool first, int index)
    {
        Vector2 size = new Vector2(b.size.x, b.size.z);
        var canvas = new PaperCanvas(size, rng.Next(), 80f, data.theme, index);
        float hx = size.x * 0.5f, hz = size.y * 0.5f;
        float R(float a, float c) => a + (float)rng.NextDouble() * (c - a);

        // لطخات رصاص وآثار مساحة
        for (int i = 0; i < Mathf.CeilToInt(size.x / 3f); i++)
            canvas.Smudge(new Vector2(R(-hx, hx), R(-hz, hz)), R(0.3f, 0.8f), R(0.04f, 0.1f));
        for (int i = 0; i < Mathf.CeilToInt(size.x / 5f); i++)
        {
            Vector2 c = new Vector2(R(-hx + 1f, hx - 1f), R(-hz * 0.8f, hz * 0.8f));
            Vector2 d = new Vector2(R(0.3f, 0.7f), R(-0.15f, 0.15f));
            canvas.Erased(c - d, c + d, R(0.08f, 0.14f));
        }

        // تظليل بالرصاص عند أطراف الصفحة (جنب الحفر)
        canvas.Hatch(new Vector2(-hx, -hz), new Vector2(-hx + R(0.6f, 0.9f), hz), 0.07f, R(50f, 65f), false);
        canvas.Hatch(new Vector2(hx - R(0.6f, 0.9f), -hz), new Vector2(hx, hz), 0.07f, R(50f, 65f), true);

        // رسومات صغيرة متفرقة (بعيد عن طريق رويدا شوي)
        // صفحات الشخابيط فيها رسومات أكثر (وحوش ما كملت وشخابيط)
        float doodleEvery = data.Scribbly ? 0.9f : 1.6f;
        for (float x = -hx + 1.2f; x < hx - 1f; x += doodleEvery * R(0.8f, 1.4f))
        {
            if (Mathf.Abs(b.center.x + x - data.door.x) < 1.2f) continue;
            bool back = rng.Next(3) != 0;
            float z = back ? R(0.55f, hz - 0.45f) : R(-hz + 0.4f, -0.55f);
            canvas.RandomDoodle(new Vector2(x, z), R(0.6f, 1.05f), data.Scribbly);
        }

        // أسهم تعلّم اللاعب إن رويدا تمشي لقدام
        if (first)
        {
            canvas.Arrow(new Vector2(-hx + 2.4f, -0.9f), 0.7f);
            canvas.Arrow(new Vector2(-hx + 3.3f, -0.9f), 0.7f);
        }
        return canvas.ToTexture("Page Art");
    }

    // أغراض رسم على الصفحة: قلم كبير، مساحة، كور ورق، دبابيس، برادة، ومكعبات ورق
    static void PlaceProps(Transform t, Block b, GameManager gm, System.Random rng, LevelData data)
    {
        float hx = b.size.x * 0.5f, hz = b.size.z * 0.5f, top = b.size.y * 0.5f;
        float R(float a, float c) => a + (float)rng.NextDouble() * (c - a);
        bool NearDoor(float x) => Mathf.Abs(b.center.x + x - data.door.x) < 2f || Mathf.Abs(b.center.x + x - data.spawn.x) < 1.5f;
        LevelTheme theme = data.theme;
        Color RandomColor() => theme.floatColors[rng.Next(theme.floatColors.Length)];

        // غرض كبير ورا على الصفحة حسب شكل المرحلة
        if (b.size.x > 8f && rng.Next(2) == 0)
        {
            float x = R(-hx + 1.5f, hx - 5f);
            if (!NearDoor(x) && !NearDoor(x + 3.5f))
            {
                Vector3 p = new Vector3(x, top, R(1.1f, 1.4f));
                switch (theme.props)
                {
                    case PropSet.Ink: Props.InkBottle(t, p, R(0f, 360f), gm); break;
                    case PropSet.Snow: Props.PineTree(t, p + new Vector3(0f, 0f, 0.4f), R(1.5f, 2.1f), gm); break;
                    case PropSet.Origami: Props.OrigamiCrane(t, p + new Vector3(0f, 0f, 0.8f), R(-30f, 30f), RandomColor(), gm, 2.2f); break;
                    case PropSet.Crayons: Props.Crayon(t, p, R(-15f, 15f), RandomColor(), gm); break;
                    case PropSet.Paint: Props.PaintTube(t, p, R(-20f, 20f), RandomColor(), gm); break;
                    case PropSet.Erasers: Props.Eraser(t, p, R(-30f, 30f), gm, rng); break;
                    default: Props.GiantPencil(t, p, R(-8f, 8f), gm); break;
                }
            }
        }

        // أغراض صغيرة متفرقة
        for (float x = -hx + 1f; x < hx - 0.8f; x += R(1.6f, 2.8f))
        {
            if (NearDoor(x)) continue;
            int kind = rng.Next(6);
            bool back = kind != 3 && kind != 4 || rng.Next(2) == 0;
            float z = back ? R(0.9f, hz - 0.35f) : R(-hz + 0.3f, -0.8f);
            Vector3 p = new Vector3(x, top, z);
            switch (theme.props)
            {
                case PropSet.Crayons:
                    if (kind < 4) Props.Crayon(t, p, R(0f, 360f), RandomColor(), gm);
                    else Props.PaperBall(t, p, R(0.35f, 0.55f), gm, rng.Next(1000));
                    break;
                case PropSet.Paint:
                    if (kind < 3) Props.PaintTube(t, p, R(0f, 360f), RandomColor(), gm);
                    else Props.Crayon(t, p, R(0f, 360f), RandomColor(), gm);
                    break;
                case PropSet.Erasers:
                    if (kind < 4) Props.Eraser(t, p, R(-40f, 40f), gm, rng);
                    else Props.PaperBall(t, p, R(0.35f, 0.55f), gm, rng.Next(1000));
                    break;
                case PropSet.Snow:
                    if (kind < 2) Props.PineTree(t, new Vector3(p.x, p.y, Mathf.Abs(p.z) + 0.3f), R(0.6f, 0.95f), gm);
                    else if (kind < 4) Props.Snowballs(t, p, gm, rng);
                    else Props.Snowballs(t, p, gm, rng);
                    break;
                case PropSet.Ink:
                    if (kind < 2) Props.InkBottle(t, p, R(0f, 360f), gm);
                    else if (kind < 4) Props.PaperBall(t, p, R(0.35f, 0.5f), gm, rng.Next(1000));
                    else Props.PaperClip(t, p, R(0f, 360f), gm);
                    break;
                case PropSet.Origami:
                    if (kind < 3) Props.OrigamiCrane(t, new Vector3(p.x, p.y, Mathf.Abs(p.z) + 0.4f), R(0f, 360f), RandomColor(), gm, R(0.9f, 1.3f));
                    else Props.OrigamiBoat(t, p, R(0f, 360f), RandomColor(), gm);
                    break;
                default:
                    switch (kind)
                    {
                        case 0: Props.PaperBall(t, p, R(0.35f, 0.55f), gm, rng.Next(1000)); break;
                        case 1: Props.Eraser(t, p, R(-40f, 40f), gm, rng); break;
                        case 2: Props.PaperCube(t, p, R(0.35f, 0.6f), R(-30f, 30f), gm, rng); break;
                        case 3: Props.PaperClip(t, p, R(0f, 360f), gm); break;
                        case 4:
                            for (int i = 0; i < 3; i++)
                                Props.Shaving(t, p + new Vector3(R(-0.3f, 0.3f), 0f, R(-0.2f, 0.2f)), R(0f, 360f), gm, rng);
                            break;
                        default:
                            Props.PaperCube(t, p, R(0.4f, 0.55f), R(-20f, 20f), gm, rng);
                            Props.PaperCube(t, p + new Vector3(0.55f, 0f, R(-0.1f, 0.1f)), R(0.3f, 0.4f), R(-30f, 30f), gm, rng);
                            break;
                    }
                    break;
            }
        }
    }

    // ثلج متراكم على أطراف الصفحة ونازل من الحافة، وبقع ثلج صغيرة فوقها (النص يظل باين عشان الرسومات)
    static void SnowCap(Transform t, Vector3 size, GameManager gm, System.Random rng)
    {
        Material snow = gm.Mat("Snow Body", new Color(0.96f, 0.98f, 1f), 0.35f);
        Vector3 h = size * 0.5f;
        float R(float a, float c) => a + (float)rng.NextDouble() * (c - a);
        for (int side = -1; side <= 1; side += 2)
        {
            float z = side * (h.z - 0.22f);
            Visual("Snow Bank", PrimitiveType.Cube, t, new Vector3(0f, h.y + 0.04f, z), new Vector3(size.x + 0.06f, 0.1f, 0.48f), snow);
            for (float x = -h.x + 0.2f; x <= h.x - 0.1f; x += R(0.35f, 0.6f))
            {
                // كتل ثلج على الطرف، اللي قدام تنزل شوي على جنب الصفحة
                var lump = Visual("Snow Lump", PrimitiveType.Sphere, t, new Vector3(x, h.y + (side < 0 ? 0f : 0.05f), side * (h.z - 0.02f)),
                    new Vector3(R(0.4f, 0.65f), R(0.16f, 0.26f), R(0.25f, 0.4f)), snow);
                lump.transform.localRotation = Quaternion.Euler(0f, R(-15f, 15f), 0f);
            }
        }
        // كتل على الجوانب (عند الحفر)
        for (int side = -1; side <= 1; side += 2)
            for (float z = -h.z + 0.3f; z < h.z - 0.2f; z += 0.5f)
                Visual("Snow Lump", PrimitiveType.Sphere, t, new Vector3(side * (h.x - 0.02f), h.y, z), new Vector3(0.3f, R(0.14f, 0.22f), R(0.4f, 0.55f)), snow);
        // بقع ثلج متفرقة فوق الصفحة
        int patches = Mathf.RoundToInt(size.x / 2.5f);
        for (int i = 0; i < patches; i++)
            Visual("Snow Patch", PrimitiveType.Sphere, t, new Vector3(R(-h.x + 0.5f, h.x - 0.5f), h.y, R(-h.z + 0.8f, h.z - 0.8f)),
                new Vector3(R(0.5f, 1.1f), 0.05f, R(0.3f, 0.6f)), snow, false);
    }

    // برك مويه صغيرة لامعة على الصفحة (للزينة)
    static void Puddles(Transform t, Block b, GameManager gm, System.Random rng)
    {
        float hx = b.size.x * 0.5f, hz = b.size.z * 0.5f, top = b.size.y * 0.5f;
        Material water = gm.Mat("Puddle", new Color(0.66f, 0.72f, 0.86f), 0.92f);
        int count = 2 + Mathf.RoundToInt(b.size.x / 4f);
        for (int i = 0; i < count; i++)
        {
            float w = 0.4f + (float)rng.NextDouble() * 0.8f;
            Vector3 p = new Vector3(Mathf.Lerp(-hx + 0.6f, hx - 0.6f, (float)rng.NextDouble()), top + 0.004f, Mathf.Lerp(-hz + 0.4f, hz - 0.4f, (float)rng.NextDouble()));
            var puddle = Visual("Puddle", PrimitiveType.Cylinder, t, p, new Vector3(w, 0.003f, w * (0.5f + (float)rng.NextDouble() * 0.4f)), water, false);
            puddle.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 180f, 0f);
        }
    }

    // خط رصاص مهزوز بين نقطتين، مع امتداد بسيط عند الزوايا مثل الرسم باليد
    static void PencilLine(Transform parent, Vector3 a, Vector3 b, GameManager gm, System.Random rng)
    {
        Vector3 dir = (b - a).normalized;
        float overshoot = 0.04f + (float)rng.NextDouble() * 0.06f;
        a -= dir * overshoot;
        b += dir * overshoot;

        float len = Vector3.Distance(a, b);
        int count = Mathf.Max(2, Mathf.CeilToInt(len / 0.3f) + 1);
        Vector3 perpA = Vector3.Cross(dir, Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
        Vector3 perpB = Vector3.Cross(dir, perpA);
        float seed = (float)rng.NextDouble() * 100f;

        var pts = new List<Vector3>(count);
        for (int i = 0; i < count; i++)
        {
            float k = i / (count - 1f);
            float wobble = (Mathf.PerlinNoise(seed, k * len * 1.3f) - 0.5f) * 0.03f;
            float wobble2 = (Mathf.PerlinNoise(seed + 7f, k * len * 1.3f) - 0.5f) * 0.03f;
            pts.Add(Vector3.Lerp(a, b, k) + perpA * wobble + perpB * wobble2);
        }
        Line("Pencil Line", parent, pts, PencilRadius, gm.pencilMaterial);
    }

    // شق متعرج على سطح الورقة يبدأ من الحافة ويدخل لداخل
    static void Crack(Transform parent, Vector3 start, int inward, GameManager gm, System.Random rng)
    {
        var pts = new List<Vector3> { start };
        Vector3 p = start;
        float angle = 0f;
        int segments = 3 + rng.Next(3);
        for (int i = 0; i < segments; i++)
        {
            angle += ((float)rng.NextDouble() - 0.5f) * 1.6f;
            float step = 0.15f + (float)rng.NextDouble() * 0.25f;
            p += new Vector3(Mathf.Cos(angle) * inward, 0f, Mathf.Sin(angle)) * step;
            pts.Add(p);
            if (i == 1 && rng.Next(2) == 0)
            {
                Vector3 branchEnd = p + new Vector3(inward * 0.2f, 0f, ((float)rng.NextDouble() - 0.5f) * 0.5f);
                Line("Crack", parent, new List<Vector3> { p, branchEnd }, PencilRadius * 0.7f, gm.pencilMaterial);
            }
        }
        Line("Crack", parent, pts, PencilRadius * 0.8f, gm.pencilMaterial);
    }

    // قوس منقط خفيف بين صفحتين: تلميح إن اللاعب يرسم جسر هنا
    static void BridgeHint(Transform parent, Block a, Block b, GameManager gm)
    {
        Vector3 from = new Vector3(a.center.x + a.size.x * 0.5f - 0.4f, a.center.y + a.size.y * 0.5f + 0.05f, 0f);
        Vector3 to = new Vector3(b.center.x - b.size.x * 0.5f + 0.4f, b.center.y + b.size.y * 0.5f + 0.05f, 0f);
        Material hint = gm.Mat("Hint", new Color(0.55f, 0.55f, 0.62f), 0.2f);
        const int dashes = 14;
        for (int i = 0; i < dashes; i++)
        {
            float t0 = i / (float)dashes, t1 = t0 + 0.55f / dashes;
            var pts = new List<Vector3>();
            for (int k = 0; k <= 3; k++)
            {
                float t = Mathf.Lerp(t0, t1, k / 3f);
                pts.Add(Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.5f);
            }
            Line("Hint", parent, pts, 0.03f, hint);
        }
    }

    // خط تلميح منقط مستقيم بلون معين (فاتح شوي)
    static void DashedHint(Transform parent, HintLine h, GameManager gm)
    {
        Color c = Color.Lerp(h.color, Color.white, 0.35f);
        Material mat = gm.Mat("Hint " + ColorUtility.ToHtmlStringRGB(c), c, 0.2f);
        float len = Vector3.Distance(h.a, h.b);
        int dashes = Mathf.Max(2, Mathf.RoundToInt(len / 0.4f));
        for (int i = 0; i < dashes; i++)
        {
            float t0 = i / (float)dashes, t1 = t0 + 0.55f / dashes;
            Line("Hint", parent, new List<Vector3> { Vector3.Lerp(h.a, h.b, t0), Vector3.Lerp(h.a, h.b, t1) }, 0.035f, mat);
        }
    }

    static void Line(string name, Transform parent, List<Vector3> localPts, float radius, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var mesh = new Mesh { name = name };
        TubeMesh.Build(mesh, localPts, radius, false, 5);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
    }

    // باب مرسوم: إطار رصاص ولوح ورق
    static void CreateDoor(Transform parent, Vector3 basePos, GameManager gm, bool flipped = false)
    {
        const float w = 1.4f, h = 2.4f, t = 0.14f, d = 0.3f;

        var door = new GameObject("Door");
        door.transform.SetParent(parent, false);
        door.transform.position = basePos;
        if (flipped) door.transform.rotation = Quaternion.Euler(180f, 0f, 0f);

        Material wood = gm.Mat("Door", new Color(0.95f, 0.93f, 0.9f), 0.15f);
        Material frame = gm.Mat("Door Frame", new Color(0.85f, 0.82f, 0.78f), 0.15f);
        Visual("Post L", PrimitiveType.Cube, door.transform, new Vector3(-w / 2f, h / 2f, 0f), new Vector3(t, h, d), frame);
        Visual("Post R", PrimitiveType.Cube, door.transform, new Vector3(w / 2f, h / 2f, 0f), new Vector3(t, h, d), frame);
        Visual("Top", PrimitiveType.Cube, door.transform, new Vector3(0f, h + t / 2f, 0f), new Vector3(w + t, t, d), frame);
        Visual("Panel", PrimitiveType.Cube, door.transform, new Vector3(0f, h / 2f, 0.05f), new Vector3(w - t, h, 0.08f), wood);
        Visual("Knob", PrimitiveType.Sphere, door.transform, new Vector3(0.42f, 1.15f, -0.03f), Vector3.one * 0.11f, gm.metalMaterial);

        // خطوط رصاص على الباب
        var rng = new System.Random(7);
        float l = -w / 2f - t / 2f, r = w / 2f + t / 2f, top = h + t, z = -d / 2f - 0.01f;
        PencilLine(door.transform, new Vector3(l, 0f, z), new Vector3(l, top, z), gm, rng);
        PencilLine(door.transform, new Vector3(r, 0f, z), new Vector3(r, top, z), gm, rng);
        PencilLine(door.transform, new Vector3(l, top, z), new Vector3(r, top, z), gm, rng);
        float pl = -w / 2f + 0.25f, pr = w / 2f - 0.25f, pz = 0.05f - 0.045f;
        PencilLine(door.transform, new Vector3(pl, 1.4f, pz), new Vector3(pr, 1.4f, pz), gm, rng);
        PencilLine(door.transform, new Vector3(pl, 2.15f, pz), new Vector3(pr, 2.15f, pz), gm, rng);
        PencilLine(door.transform, new Vector3(pl, 1.4f, pz), new Vector3(pl, 2.15f, pz), gm, rng);
        PencilLine(door.transform, new Vector3(pr, 1.4f, pz), new Vector3(pr, 2.15f, pz), gm, rng);

        var glow = new GameObject("Door Glow").AddComponent<Light>();
        glow.transform.SetParent(door.transform, false);
        glow.transform.localPosition = new Vector3(0f, 1.5f, -1.2f);
        glow.type = LightType.Point;
        glow.range = 4f;
        glow.intensity = 1.2f;
        glow.color = new Color(1f, 0.85f, 0.6f);

        var trigger = door.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, h / 2f, 0f);
        trigger.size = new Vector3(w * 0.6f, h, 2f);
        door.AddComponent<Door>();
    }

    static void CreateRowaida(Transform parent, Vector3 pos, GameManager gm, bool flipped = false)
    {
        if (slippery == null)
            slippery = new PhysicsMaterial("Rowaida")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0f,
            };

        var go = new GameObject("Rowaida");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        if (flipped) go.transform.rotation = Quaternion.Euler(180f, 0f, 0f);

        var col = go.AddComponent<CapsuleCollider>();
        col.height = 1.4f;
        col.radius = 0.32f;
        col.sharedMaterial = slippery;

        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;

        RowaidaModel.Build(go, gm);
        // مقلوبة: نلف الشكل عشان وجهها يظل يطالع الكاميرا
        if (flipped) go.GetComponent<RowaidaModel>().Model.localRotation = Quaternion.LookRotation(new Vector3(1f, 0f, 0.9f));
        go.AddComponent<RowaidaController>();
    }

    // جسم للعرض فقط (بدون تصادم)
    public static GameObject Visual(string name, PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool castShadows = true)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh(type);
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        return go;
    }

    static Mesh PrimitiveMesh(PrimitiveType type)
    {
        if (meshCache.TryGetValue(type, out Mesh m) && m != null) return m;
        var temp = GameObject.CreatePrimitive(type);
        m = temp.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(temp);
        meshCache[type] = m;
        return m;
    }
}
