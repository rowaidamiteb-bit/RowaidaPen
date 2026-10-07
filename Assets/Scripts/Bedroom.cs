using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// غرفة رويدا بالليل (للمشهد الافتتاحي): سرير، لمبة دافية، شباك يطل على السما،
// رف كتب، رسوماتها على الجدار، أضواء زينة، وسجادة
public class Bedroom
{
    public Transform Root;
    public RowaidaModel Rowaida;
    public Transform Notebook;
    public Light NotebookGlow;
    public Vector3 HipSitting;          // مكان ورك رويدا وهي قاعدة (إحداثيات الغرفة)
    public Vector3 NotebookFloor;       // مكان الدفتر لما يطيح على الأرض
    public Quaternion Facing;           // اتجاه رويدا (تطالع لقدام السرير)

    GameManager gm;
    System.Random rng = new System.Random(77);

    float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

    public static Bedroom Build(GameManager gm, Vector3 origin)
    {
        var room = new Bedroom { gm = gm };
        room.Root = new GameObject("Bedroom").transform;
        room.Root.position = origin;
        room.BuildRoom();
        room.BuildBed();
        room.BuildNightstand();
        room.BuildWindow();
        room.BuildWallDecor();
        room.BuildRowaida();
        room.BuildNotebook();
        return room;
    }

    // ---------- الجدران والأرضية ----------

    void BuildRoom()
    {
        Material floor = TexMat("Wood Floor", WoodTexture(), new Vector2(3f, 3f), 0.35f);
        Material wall = TexMat("Wallpaper", WallpaperTexture(), new Vector2(4f, 2f), 0.05f);
        Material ceiling = gm.Mat("Ceiling", new Color(0.86f, 0.84f, 0.92f), 0.05f);

        Box("Floor", new Vector3(0f, -0.05f, 0.75f), new Vector3(6.4f, 0.1f, 4f), floor);
        Box("Ceiling", new Vector3(0f, 3.05f, 0.75f), new Vector3(6.4f, 0.1f, 4f), ceiling);
        Box("Wall Left", new Vector3(-3.05f, 1.5f, 0.75f), new Vector3(0.1f, 3f, 4f), wall);
        Box("Wall Right", new Vector3(3.05f, 1.5f, 0.75f), new Vector3(0.1f, 3f, 4f), wall);

        // الجدار الخلفي فيه فتحة للشباك (x من 1.1 إلى 2.5، y من 1.2 إلى 2.4)
        Box("Wall Back L", new Vector3(-0.95f, 1.5f, 2.55f), new Vector3(4.1f, 3f, 0.1f), wall);
        Box("Wall Back R", new Vector3(2.8f, 1.5f, 2.55f), new Vector3(0.6f, 3f, 0.1f), wall);
        Box("Wall Back Bottom", new Vector3(1.8f, 0.6f, 2.55f), new Vector3(1.4f, 1.2f, 0.1f), wall);
        Box("Wall Back Top", new Vector3(1.8f, 2.7f, 2.55f), new Vector3(1.4f, 0.6f, 0.1f), wall);

        // نعل الجدار
        Material trim = gm.Mat("Trim", new Color(0.96f, 0.95f, 0.97f), 0.3f);
        Box("Baseboard Back", new Vector3(0f, 0.06f, 2.48f), new Vector3(6f, 0.12f, 0.04f), trim);
        Box("Baseboard Left", new Vector3(-2.98f, 0.06f, 0.75f), new Vector3(0.04f, 0.12f, 4f), trim);

        // سجادة دائرية
        Material rug = gm.Mat("Rug", new Color(0.86f, 0.62f, 0.74f), 0.05f);
        Material rugInner = gm.Mat("Rug Inner", new Color(0.95f, 0.8f, 0.86f), 0.05f);
        Shape("Rug", PrimitiveType.Cylinder, new Vector3(0.3f, 0.005f, 0.2f), new Vector3(2f, 0.005f, 1.6f), rug, false);
        Shape("Rug Inner", PrimitiveType.Cylinder, new Vector3(0.3f, 0.011f, 0.2f), new Vector3(1.4f, 0.004f, 1.1f), rugInner, false);
    }

    // ---------- السرير ----------

    void BuildBed()
    {
        Material frame = gm.Mat("Bed Frame", new Color(0.97f, 0.95f, 0.93f), 0.35f);
        Material mattress = gm.Mat("Mattress", new Color(0.98f, 0.98f, 0.98f), 0.1f);
        Material blanket = gm.Mat("Blanket", new Color(0.72f, 0.64f, 0.9f), 0.08f);
        Material blanketDark = gm.Mat("Blanket Fold", new Color(0.62f, 0.54f, 0.82f), 0.08f);
        Material pillow = gm.Mat("Pillow", new Color(0.99f, 0.97f, 0.97f), 0.1f);

        Box("Bed Base", new Vector3(-0.85f, 0.2f, 1.5f), new Vector3(2.6f, 0.3f, 1.35f), frame);
        Box("Headboard", new Vector3(-2.18f, 0.7f, 1.5f), new Vector3(0.1f, 1.3f, 1.4f), frame);
        Shape("Headboard Top", PrimitiveType.Capsule, new Vector3(-2.18f, 1.35f, 1.5f), new Vector3(0.12f, 0.7f, 0.12f), frame).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Box("Footboard", new Vector3(0.47f, 0.45f, 1.5f), new Vector3(0.08f, 0.6f, 1.4f), frame);
        Box("Mattress", new Vector3(-0.85f, 0.45f, 1.5f), new Vector3(2.5f, 0.2f, 1.28f), mattress);

        // البطانية تغطي نص السرير وتنزل على الجنب
        Box("Blanket", new Vector3(-0.15f, 0.565f, 1.5f), new Vector3(1.15f, 0.05f, 1.36f), blanket);
        Box("Blanket Side", new Vector3(-0.15f, 0.42f, 0.81f), new Vector3(1.15f, 0.32f, 0.04f), blanket);
        Box("Blanket Fold", new Vector3(-0.72f, 0.58f, 1.5f), new Vector3(0.12f, 0.07f, 1.37f), blanketDark);

        // مخدات
        Shape("Pillow", PrimitiveType.Sphere, new Vector3(-1.78f, 0.64f, 1.52f), new Vector3(0.5f, 0.2f, 0.9f), pillow);
        Shape("Cushion", PrimitiveType.Sphere, new Vector3(-1.85f, 0.75f, 1.95f), new Vector3(0.35f, 0.32f, 0.12f), blanket).transform.localRotation = Quaternion.Euler(0f, 0f, -15f);

        // دبدوب أرنب على السرير
        BuildBunny(new Vector3(-1.75f, 0.58f, 1.0f), 1f, Quaternion.Euler(0f, 210f, 0f));
    }

    void BuildBunny(Vector3 pos, float scale, Quaternion rot)
    {
        Material white = gm.Mat("Plush White", new Color(0.98f, 0.96f, 0.95f), 0.05f);
        Material pink = gm.Mat("Plush Pink", new Color(1f, 0.7f, 0.76f), 0.05f);
        Material dark = gm.Mat("Plush Eyes", new Color(0.1f, 0.08f, 0.08f), 0.8f);
        var b = new GameObject("Bunny Plush").transform;
        b.SetParent(Root, false);
        b.localPosition = pos;
        b.localRotation = rot;
        b.localScale = Vector3.one * scale;
        LevelBuilder.Visual("Body", PrimitiveType.Sphere, b, new Vector3(0f, 0.12f, 0f), new Vector3(0.24f, 0.24f, 0.22f), white);
        LevelBuilder.Visual("Head", PrimitiveType.Sphere, b, new Vector3(0f, 0.3f, 0.02f), Vector3.one * 0.2f, white);
        for (int s = -1; s <= 1; s += 2)
        {
            var ear = LevelBuilder.Visual("Ear", PrimitiveType.Capsule, b, new Vector3(s * 0.05f, 0.47f, 0f), new Vector3(0.06f, 0.1f, 0.035f), white);
            ear.transform.localRotation = Quaternion.Euler(0f, 0f, -s * 12f);
            LevelBuilder.Visual("Ear Inner", PrimitiveType.Capsule, ear.transform, new Vector3(0f, 0f, 0.4f), new Vector3(0.6f, 0.8f, 0.3f), pink);
            LevelBuilder.Visual("Eye", PrimitiveType.Sphere, b, new Vector3(s * 0.045f, 0.32f, 0.105f), Vector3.one * 0.025f, dark);
            LevelBuilder.Visual("Foot", PrimitiveType.Sphere, b, new Vector3(s * 0.07f, 0.02f, 0.08f), new Vector3(0.08f, 0.05f, 0.11f), white);
        }
        LevelBuilder.Visual("Nose", PrimitiveType.Sphere, b, new Vector3(0f, 0.29f, 0.115f), Vector3.one * 0.02f, pink);
    }

    // ---------- الكومدينة واللمبة ----------

    void BuildNightstand()
    {
        Material wood = gm.Mat("Nightstand", new Color(0.93f, 0.88f, 0.84f), 0.3f);
        Material knob = gm.Mat("Knob Gold", new Color(0.9f, 0.75f, 0.45f), 0.8f, 0.9f);
        Box("Nightstand", new Vector3(0.95f, 0.3f, 2.15f), new Vector3(0.6f, 0.6f, 0.5f), wood);
        Box("Drawer Line", new Vector3(0.95f, 0.38f, 1.895f), new Vector3(0.5f, 0.01f, 0.01f), gm.Mat("Drawer Line", new Color(0.7f, 0.65f, 0.62f), 0.2f));
        Shape("Drawer Knob", PrimitiveType.Sphere, new Vector3(0.95f, 0.24f, 1.89f), Vector3.one * 0.04f, knob);

        // اللمبة
        Material baseMat = gm.Mat("Lamp Base", new Color(0.95f, 0.85f, 0.9f), 0.5f);
        Shape("Lamp Base", PrimitiveType.Cylinder, new Vector3(0.95f, 0.63f, 2.2f), new Vector3(0.18f, 0.03f, 0.18f), baseMat);
        Shape("Lamp Stem", PrimitiveType.Cylinder, new Vector3(0.95f, 0.78f, 2.2f), new Vector3(0.025f, 0.15f, 0.025f), knob);
        var shadeMesh = CharacterMeshes.Lathe(new[]
        {
            new Vector2(0f, 0f), new Vector2(0.17f, 0f), new Vector2(0.17f, 0f), new Vector2(0.11f, 0.22f), new Vector2(0.11f, 0.22f), new Vector2(0f, 0.22f),
        }, 20);
        var shade = new GameObject("Lamp Shade");
        shade.transform.SetParent(Root, false);
        shade.transform.localPosition = new Vector3(0.95f, 0.88f, 2.2f);
        shade.AddComponent<MeshFilter>().sharedMesh = shadeMesh;
        var r = shade.AddComponent<MeshRenderer>();
        r.sharedMaterial = Glow("Lamp Glow", new Color(2.2f, 1.7f, 1.1f));
        r.shadowCastingMode = ShadowCastingMode.Off;

        var lamp = new GameObject("Lamp Light").AddComponent<Light>();
        lamp.transform.SetParent(Root, false);
        lamp.transform.localPosition = new Vector3(0.95f, 1.0f, 2.05f);
        lamp.type = LightType.Point;
        lamp.color = new Color(1f, 0.76f, 0.5f);
        lamp.intensity = 3.2f;
        lamp.range = 7f;
        lamp.shadows = LightShadows.Soft;
        lamp.shadowStrength = 0.7f;

        // كاسة حليب صغيرة جنب اللمبة
        Shape("Cup", PrimitiveType.Cylinder, new Vector3(0.75f, 0.67f, 2.05f), new Vector3(0.08f, 0.07f, 0.08f), gm.Mat("Cup", new Color(0.75f, 0.85f, 0.98f), 0.6f));
    }

    // ---------- الشباك ----------

    void BuildWindow()
    {
        Material frame = gm.Mat("Window Frame", new Color(0.97f, 0.97f, 0.98f), 0.4f);
        float x0 = 1.1f, x1 = 2.5f, y0 = 1.2f, y1 = 2.4f, z = 2.52f;
        float cx = (x0 + x1) / 2f, cy = (y0 + y1) / 2f;
        Box("Frame Top", new Vector3(cx, y1, z), new Vector3(x1 - x0 + 0.1f, 0.08f, 0.14f), frame);
        Box("Frame Bottom", new Vector3(cx, y0, z), new Vector3(x1 - x0 + 0.1f, 0.08f, 0.14f), frame);
        Box("Frame Left", new Vector3(x0, cy, z), new Vector3(0.08f, y1 - y0, 0.14f), frame);
        Box("Frame Right", new Vector3(x1, cy, z), new Vector3(0.08f, y1 - y0, 0.14f), frame);
        Box("Mullion V", new Vector3(cx, cy, z), new Vector3(0.04f, y1 - y0, 0.06f), frame);
        Box("Mullion H", new Vector3(cx, cy + 0.1f, z), new Vector3(x1 - x0, 0.04f, 0.06f), frame);
        Box("Sill", new Vector3(cx, y0 - 0.05f, 2.42f), new Vector3(x1 - x0 + 0.3f, 0.05f, 0.25f), frame);

        // ستاير بطيات
        Material curtain = gm.Mat("Curtain", new Color(0.95f, 0.72f, 0.82f), 0.05f);
        foreach (float start in new[] { x0 - 0.42f, x1 + 0.08f })
            for (int i = 0; i < 4; i++)
                Shape("Curtain Fold", PrimitiveType.Cylinder, new Vector3(start + 0.05f + i * 0.09f, 1.75f, 2.4f + (i % 2) * 0.03f), new Vector3(0.11f, 0.95f, 0.06f), curtain);
        Shape("Curtain Rod", PrimitiveType.Cylinder, new Vector3(cx, 2.72f, 2.4f), new Vector3(0.025f, 1.2f, 0.025f), gm.Mat("Knob Gold", new Color(0.9f, 0.75f, 0.45f), 0.8f, 0.9f))
            .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
    }

    // ---------- زينة الجدار ----------

    void BuildWallDecor()
    {
        // رف كتب فوق السرير
        Material shelf = gm.Mat("Shelf", new Color(0.93f, 0.88f, 0.84f), 0.3f);
        Box("Shelf", new Vector3(-1.0f, 2.0f, 2.38f), new Vector3(1.3f, 0.05f, 0.25f), shelf);
        Color[] bookColors =
        {
            new Color(0.55f, 0.65f, 0.9f), new Color(0.95f, 0.65f, 0.7f), new Color(0.98f, 0.85f, 0.5f),
            new Color(0.6f, 0.82f, 0.7f), new Color(0.75f, 0.6f, 0.9f), new Color(0.9f, 0.9f, 0.92f),
        };
        float bx = -1.55f;
        for (int i = 0; i < 9; i++)
        {
            float w = R(0.05f, 0.09f), h = R(0.2f, 0.3f);
            Box("Book", new Vector3(bx + w / 2f, 2.025f + h / 2f, 2.38f), new Vector3(w, h, R(0.15f, 0.2f)), gm.Mat("Book " + (i % bookColors.Length), bookColors[i % bookColors.Length], 0.3f));
            bx += w + 0.008f;
        }
        BuildBunny(new Vector3(-0.55f, 2.025f, 2.36f), 0.55f, Quaternion.Euler(0f, 160f, 0f));

        // رسومات رويدا معلقة على الجدار
        PinnedDrawing(new Vector3(-0.15f, 1.55f, 2.495f), new Vector2(0.42f, 0.32f), 4f, Color.white);
        PinnedDrawing(new Vector3(0.38f, 1.85f, 2.495f), new Vector2(0.32f, 0.42f), -6f, new Color(1f, 0.97f, 0.88f));
        PinnedDrawing(new Vector3(0.3f, 1.3f, 2.495f), new Vector2(0.36f, 0.28f), 8f, new Color(0.92f, 0.96f, 1f));
        PinnedDrawing(new Vector3(-2.99f, 1.6f, 0.4f), new Vector2(0.45f, 0.35f), -3f, Color.white, true);
        PinnedDrawing(new Vector3(-2.99f, 1.75f, -0.25f), new Vector2(0.3f, 0.4f), 5f, new Color(1f, 0.95f, 0.95f), true);

        // أضواء زينة على الجدار الخلفي
        var wirePts = new List<Vector3>();
        var bulbs = new List<Vector3>();
        for (int i = 0; i <= 40; i++)
        {
            float t = i / 40f;
            float x = Mathf.Lerp(-2.9f, 0.8f, t);
            float sag = Mathf.Sin(t * Mathf.PI * 3f);
            Vector3 p = new Vector3(x, 2.75f - Mathf.Abs(sag) * 0.18f, 2.47f);
            wirePts.Add(p);
            if (i % 3 == 1) bulbs.Add(p + Vector3.down * 0.04f);
        }
        var wire = new GameObject("Fairy Wire");
        wire.transform.SetParent(Root, false);
        var wm = new Mesh();
        TubeMesh.Build(wm, wirePts, 0.006f, false, 4);
        wire.AddComponent<MeshFilter>().sharedMesh = wm;
        wire.AddComponent<MeshRenderer>().sharedMaterial = gm.Mat("Wire", new Color(0.2f, 0.25f, 0.2f), 0.3f);
        Color[] bulbColors = { new Color(2.4f, 1.9f, 1.1f), new Color(2.3f, 1.3f, 1.7f), new Color(1.6f, 1.9f, 2.4f), new Color(2.4f, 2.2f, 1.2f) };
        for (int i = 0; i < bulbs.Count; i++)
        {
            var bulb = Shape("Fairy Bulb", PrimitiveType.Sphere, bulbs[i], new Vector3(0.045f, 0.06f, 0.045f), Glow("Bulb " + (i % 4), bulbColors[i % 4]), false);
            bulb.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    void PinnedDrawing(Vector3 pos, Vector2 size, float tilt, Color paper, bool leftWall = false)
    {
        var canvas = new PaperCanvas(size, rng.Next(), 500f);
        canvas.RandomDoodle(Vector2.zero, Mathf.Min(size.x, size.y) * 0.85f);
        var mat = new Material(gm.paperMaterial) { name = "Drawing" };
        mat.SetTexture("_BaseMap", canvas.ToTexture("Wall Drawing"));
        mat.SetColor("_BaseColor", paper);

        var go = new GameObject("Wall Drawing");
        go.transform.SetParent(Root, false);
        go.transform.localPosition = pos;
        // الورقة واقفة على الجدار: السطح العلوي للصندوق يطالع الغرفة
        Quaternion face = leftWall ? Quaternion.Euler(0f, 0f, -90f) * Quaternion.Euler(0f, -90f, 0f) : Quaternion.Euler(-90f, 0f, 0f);
        go.transform.localRotation = face * Quaternion.Euler(0f, tilt, 0f);
        go.AddComponent<MeshFilter>().sharedMesh = PaperMeshes.PaperSlab(new Vector3(size.x, 0.004f, size.y));
        go.AddComponent<MeshRenderer>().sharedMaterials = new[] { mat, gm.pageEdgeMaterial };

        // شريط لاصق صغير فوق
        var tape = LevelBuilder.Visual("Tape", PrimitiveType.Cube, go.transform, new Vector3(0f, 0.004f, size.y * 0.5f - 0.01f), new Vector3(0.09f, 0.002f, 0.035f), gm.Mat("Tape", new Color(1f, 0.85f, 0.6f), 0.3f), false);
        tape.transform.localRotation = Quaternion.Euler(0f, 7f, 0f);
    }

    // ---------- رويدا والدفتر ----------

    void BuildRowaida()
    {
        var go = new GameObject("Rowaida (Bedroom)");
        go.transform.SetParent(Root, false);
        RowaidaModel.Build(go, gm);
        Rowaida = go.GetComponent<RowaidaModel>();
        Rowaida.Model.localRotation = Quaternion.identity;
        // ضوء رويدا الدافي قوي على جدران الغرفة القريبة، نطفيه هنا
        var warmGlow = go.transform.Find("Warm Glow");
        if (warmGlow != null) warmGlow.gameObject.SetActive(false);

        Facing = Quaternion.LookRotation(new Vector3(1f, 0f, -0.55f));
        HipSitting = new Vector3(-0.95f, 0.66f, 1.5f);
        NotebookFloor = new Vector3(0.05f, 0.025f, 0.45f);
    }

    void BuildNotebook()
    {
        var nb = new GameObject("Notebook").transform;
        nb.SetParent(Root, false);
        Notebook = nb;

        Material cover = gm.Mat("Notebook Cover", new Color(0.45f, 0.38f, 0.7f), 0.3f);
        LevelBuilder.Visual("Cover", PrimitiveType.Cube, nb, new Vector3(0f, -0.012f, 0f), new Vector3(0.68f, 0.012f, 0.44f), cover);

        for (int side = -1; side <= 1; side += 2)
        {
            var canvas = new PaperCanvas(new Vector2(0.32f, 0.42f), rng.Next(), 500f);
            canvas.RandomDoodle(new Vector2(0f, 0.06f), 0.2f);
            canvas.RandomDoodle(new Vector2(side * 0.05f, -0.1f), 0.14f);
            var mat = new Material(gm.paperMaterial) { name = "Notebook Page" };
            mat.SetTexture("_BaseMap", canvas.ToTexture("Notebook Page"));

            var page = new GameObject("Page");
            page.transform.SetParent(nb, false);
            page.transform.localPosition = new Vector3(side * 0.165f, 0.004f, 0f);
            page.transform.localRotation = Quaternion.Euler(0f, 0f, -side * 5f);
            page.AddComponent<MeshFilter>().sharedMesh = PaperMeshes.PaperSlab(new Vector3(0.32f, 0.018f, 0.42f));
            page.AddComponent<MeshRenderer>().sharedMaterials = new[] { mat, gm.pageEdgeMaterial };
        }

        // سلك حلزوني في النص
        var ring = PaperMeshes.Ring(0.26f, 0.03f);
        for (float z = -0.19f; z <= 0.19f; z += 0.04f)
        {
            var r = new GameObject("Ring");
            r.transform.SetParent(nb, false);
            r.transform.localPosition = new Vector3(0f, 0.01f, z);
            r.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            r.transform.localScale = Vector3.one * 0.07f;
            r.AddComponent<MeshFilter>().sharedMesh = ring;
            r.AddComponent<MeshRenderer>().sharedMaterial = gm.metalMaterial;
        }

        // ضوء سحري يطلع من الصفحات (يبدأ مطفي)
        NotebookGlow = new GameObject("Notebook Glow").AddComponent<Light>();
        NotebookGlow.transform.SetParent(nb, false);
        NotebookGlow.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        NotebookGlow.type = LightType.Point;
        NotebookGlow.color = new Color(1f, 0.92f, 0.75f);
        NotebookGlow.range = 2.2f;
        NotebookGlow.intensity = 0f;
    }

    // ---------- أدوات ----------

    GameObject Box(string name, Vector3 pos, Vector3 size, Material mat)
    {
        return Shape(name, PrimitiveType.Cube, pos, size, mat);
    }

    GameObject Shape(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat, bool shadows = true)
    {
        return LevelBuilder.Visual(name, type, Root, pos, scale, mat, shadows);
    }

    Material TexMat(string name, Texture2D tex, Vector2 tiling, float smooth)
    {
        var m = new Material(gm.Mat(name, Color.white, smooth)) { name = name };
        m.SetTexture("_BaseMap", tex);
        m.SetTextureScale("_BaseMap", tiling);
        return m;
    }

    // مادة تضوي (بدون إضاءة، واللون فوق 1 يعطي توهج)
    static Material Glow(string name, Color hdr)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name };
        m.SetColor("_BaseColor", hdr);
        return m;
    }

    static Texture2D WoodTexture()
    {
        const int w = 256, h = 256;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "Wood", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
        var px = new Color[w * h];
        var rng = new System.Random(5);
        float[] plankTone = new float[8];
        for (int i = 0; i < 8; i++) plankTone[i] = 0.9f + (float)rng.NextDouble() * 0.2f;
        for (int y = 0; y < h; y++)
        {
            int plank = y / 32;
            bool seam = y % 32 == 0;
            for (int x = 0; x < w; x++)
            {
                float grain = Mathf.PerlinNoise(x * 0.02f + plank * 13f, y * 0.35f) * 0.25f + Mathf.PerlinNoise(x * 0.15f, y * 0.9f) * 0.08f;
                bool endSeam = (x + plank * 97) % 256 == 0;
                float v = (seam || endSeam) ? 0.55f : plankTone[plank] * (0.85f + grain);
                px[y * w + x] = new Color(0.72f * v, 0.55f * v, 0.42f * v);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    static Texture2D WallpaperTexture()
    {
        const int s = 128;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, true) { name = "Wallpaper", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
        var px = new Color[s * s];
        Color baseColor = new Color(0.8f, 0.76f, 0.9f);
        Color stripe = new Color(0.84f, 0.8f, 0.93f);
        Color star = new Color(0.97f, 0.93f, 1f);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                Color c = (x / 16) % 2 == 0 ? baseColor : stripe;
                // نجوم صغيرة متفرقة
                foreach (Vector2 p in new[] { new Vector2(32f, 40f), new Vector2(96f, 100f), new Vector2(80f, 20f), new Vector2(20f, 104f) })
                {
                    Vector2 d = new Vector2(Mathf.Abs(x - p.x), Mathf.Abs(y - p.y));
                    float starShape = Mathf.Min(d.x, d.y) < 1.2f && d.x + d.y < 7f ? 1f : (d.magnitude < 2.2f ? 1f : 0f);
                    c = Color.Lerp(c, star, starShape * 0.9f);
                }
                px[y * s + x] = c;
            }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }
}
