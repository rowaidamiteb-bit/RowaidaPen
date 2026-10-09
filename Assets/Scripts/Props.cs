using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// أغراض الرسم الكبيرة على الصفحات (رويدا صغيرة داخل الدفتر): قلم، مساحة، ورق مكرمش، دبابيس، برادة قلم، ومكعبات ورق
// كلها للزينة وما تعيق رويدا
public static class Props
{
    static Mesh pencilBody, pencilWood, pencilLead, pencilFerrule, pencilEraser;

    // قلم رصاص كبير طايح على الصفحة (مسطوح على طول x)
    public static GameObject GiantPencil(Transform parent, Vector3 pos, float yaw, GameManager gm)
    {
        if (pencilBody == null)
        {
            pencilEraser = CharacterMeshes.Lathe(Profile(0f, 0.32f, 0.15f, 0.15f), 16);
            pencilFerrule = CharacterMeshes.Lathe(new[]
            {
                new Vector2(0f, 0.3f), new Vector2(0.168f, 0.3f), new Vector2(0.168f, 0.3f), new Vector2(0.172f, 0.36f),
                new Vector2(0.162f, 0.4f), new Vector2(0.172f, 0.44f), new Vector2(0.162f, 0.48f), new Vector2(0.172f, 0.52f),
                new Vector2(0.168f, 0.56f), new Vector2(0.168f, 0.56f), new Vector2(0f, 0.56f),
            }, 16);
            pencilBody = CharacterMeshes.Lathe(Profile(0.55f, 2.9f, 0.175f, 0.175f), 6);
            pencilWood = CharacterMeshes.Lathe(new[]
            {
                new Vector2(0f, 2.89f), new Vector2(0.17f, 2.89f), new Vector2(0.17f, 2.89f), new Vector2(0.055f, 3.35f), new Vector2(0f, 3.35f),
            }, 12);
            pencilLead = CharacterMeshes.Lathe(new[]
            {
                new Vector2(0f, 3.34f), new Vector2(0.056f, 3.34f), new Vector2(0.056f, 3.34f), new Vector2(0f, 3.55f),
            }, 12);
        }

        var root = new GameObject("Giant Pencil");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos + Vector3.up * 0.15f;
        root.transform.localRotation = Quaternion.Euler(0f, yaw, -90f) * Quaternion.Euler(0f, 30f, 0f);

        Part(root.transform, pencilEraser, gm.Mat("Eraser Pink", new Color(0.95f, 0.62f, 0.66f), 0.15f));
        Part(root.transform, pencilFerrule, gm.Mat("Ferrule", new Color(0.75f, 0.75f, 0.72f), 0.8f, 0.9f));
        Part(root.transform, pencilBody, gm.Mat("Pencil Yellow", new Color(1f, 0.78f, 0.18f), 0.45f));
        Part(root.transform, pencilWood, gm.Mat("Wood", new Color(0.93f, 0.78f, 0.6f), 0.15f));
        Part(root.transform, pencilLead, gm.Mat("Graphite", new Color(0.15f, 0.15f, 0.16f), 0.6f));
        return root;
    }

    // مساحة بيضاء ووردي مع فتات حولها
    public static void Eraser(Transform parent, Vector3 pos, float yaw, GameManager gm, System.Random rng)
    {
        var root = new GameObject("Eraser");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        Material white = gm.Mat("Eraser White", new Color(0.97f, 0.96f, 0.95f), 0.2f);
        Material pink = gm.Mat("Eraser Pink", new Color(0.95f, 0.62f, 0.66f), 0.15f);
        LevelBuilder.Visual("Eraser Body", PrimitiveType.Cube, root.transform, new Vector3(-0.12f, 0.17f, 0f), new Vector3(0.62f, 0.34f, 0.42f), white);
        LevelBuilder.Visual("Eraser Tip", PrimitiveType.Cube, root.transform, new Vector3(0.32f, 0.16f, 0f), new Vector3(0.28f, 0.32f, 0.4f), pink);
        LevelBuilder.Visual("Sleeve", PrimitiveType.Cube, root.transform, new Vector3(-0.05f, 0.17f, 0f), new Vector3(0.4f, 0.355f, 0.435f), gm.Mat("Sleeve Blue", new Color(0.35f, 0.45f, 0.75f), 0.3f));

        for (int i = 0; i < 9; i++)
        {
            Vector3 p = new Vector3(0.6f + (float)rng.NextDouble() * 0.7f, 0.025f, ((float)rng.NextDouble() - 0.5f) * 0.8f);
            float s = 0.04f + (float)rng.NextDouble() * 0.05f;
            var crumb = LevelBuilder.Visual("Crumb", PrimitiveType.Sphere, root.transform, p, new Vector3(s * 1.6f, s, s), (i % 3 == 0) ? pink : white);
            crumb.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 180f, 0f);
        }
    }

    // كورة ورق مكرمشة
    public static void PaperBall(Transform parent, Vector3 pos, float size, GameManager gm, int seed)
    {
        var mesh = CharacterMeshes.Sphere(18, 12, false);
        var verts = mesh.vertices;
        float o = seed * 1.37f;
        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 v = verts[i];
            float n = Mathf.PerlinNoise(v.x * 2.2f + o, v.y * 2.2f + v.z * 1.7f) * 0.55f + Mathf.PerlinNoise(v.z * 5f + o, v.x * 5f) * 0.25f;
            verts[i] = v * (0.75f + n);
        }
        mesh.vertices = verts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var go = new GameObject("Paper Ball");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos + Vector3.up * size * 0.42f;
        go.transform.localScale = Vector3.one * size * 0.5f;
        go.transform.localRotation = Quaternion.Euler(seed * 37f, seed * 91f, seed * 13f);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = gm.Mat("Crumpled Paper", new Color(0.93f, 0.92f, 0.9f), 0.05f);
    }

    // دبوس ورق معدني مسطوح على الصفحة
    public static void PaperClip(Transform parent, Vector3 pos, float yaw, GameManager gm)
    {
        var path = new List<Vector3>();
        void Arc(Vector2 c, float r, float a0, float a1)
        {
            for (int i = 0; i <= 8; i++)
            {
                float a = Mathf.Lerp(a0, a1, i / 8f) * Mathf.Deg2Rad;
                path.Add(new Vector3(c.x + Mathf.Cos(a) * r, 0f, c.y + Mathf.Sin(a) * r));
            }
        }
        path.Add(new Vector3(0.15f, 0f, 0.04f));
        Arc(new Vector2(0.55f, 0.1f), 0.06f, -90f, 90f);
        Arc(new Vector2(0.06f, 0.08f), 0.08f, 90f, 270f);
        Arc(new Vector2(0.62f, 0.07f), 0.07f, -90f, 90f);
        path.Add(new Vector3(0.25f, 0f, 0.14f));

        var go = new GameObject("Paper Clip");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos + Vector3.up * 0.014f;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        var mesh = new Mesh { name = "Paper Clip" };
        TubeMesh.Build(mesh, path, 0.014f, false, 6);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = gm.Mat("Clip Metal", new Color(0.7f, 0.72f, 0.75f), 0.85f, 0.9f);
    }

    // برادة قلم ملفوفة
    public static void Shaving(Transform parent, Vector3 pos, float yaw, GameManager gm, System.Random rng)
    {
        var path = new List<Vector3>();
        float r = 0.08f + (float)rng.NextDouble() * 0.04f;
        for (int i = 0; i <= 20; i++)
        {
            float t = i / 20f;
            float a = t * Mathf.PI * 2.4f;
            path.Add(new Vector3(Mathf.Cos(a) * r * (1f - t * 0.4f), r * 0.6f + Mathf.Sin(a) * r * 0.6f, t * 0.06f));
        }
        var go = new GameObject("Shaving");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        var mesh = new Mesh { name = "Shaving" };
        TubeMesh.Build(mesh, path, 0.018f, false, 5);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = gm.Mat("Wood", new Color(0.93f, 0.78f, 0.6f), 0.15f);
    }

    // مكعب ورق صغير بحواف رصاص
    public static void PaperCube(Transform parent, Vector3 pos, float size, float yaw, GameManager gm, System.Random rng)
    {
        var go = new GameObject("Paper Cube");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos + Vector3.up * size * 0.5f;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        go.AddComponent<MeshFilter>().sharedMesh = PaperMeshes.PaperSlab(Vector3.one * size);
        go.AddComponent<MeshRenderer>().sharedMaterials = new[] { gm.paperMaterial, gm.pageEdgeMaterial };
        LevelBuilder.OutlineBox(go.transform, Vector3.one * size, gm, rng);
    }

    // قلم تلوين شمع: جسم ملون، ورقة ملفوفة عليه، وراس مدبب
    public static void Crayon(Transform parent, Vector3 pos, float yaw, Color color, GameManager gm)
    {
        var root = new GameObject("Crayon");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos + Vector3.up * 0.12f;
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 90f);
        Material wax = gm.Mat("Crayon " + ColorUtility.ToHtmlStringRGB(color), color, 0.45f);
        Material wrap = gm.Mat("Crayon Wrap " + ColorUtility.ToHtmlStringRGB(color), Color.Lerp(color, Color.white, 0.35f), 0.2f);
        LevelBuilder.Visual("Wax", PrimitiveType.Cylinder, root.transform, Vector3.zero, new Vector3(0.24f, 0.7f, 0.24f), wax);
        LevelBuilder.Visual("Wrap", PrimitiveType.Cylinder, root.transform, new Vector3(0f, -0.1f, 0f), new Vector3(0.25f, 0.45f, 0.25f), wrap);
        LevelBuilder.Visual("Tip", PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.72f, 0f), new Vector3(0.2f, 0.22f, 0.2f), wax);
    }

    // أنبوب ألوان مضغوط وبقعة لون جنبه
    public static void PaintTube(Transform parent, Vector3 pos, float yaw, Color color, GameManager gm)
    {
        var root = new GameObject("Paint Tube");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos + Vector3.up * 0.13f;
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 90f);
        Material metal = gm.Mat("Tube Metal", new Color(0.85f, 0.85f, 0.88f), 0.7f, 0.6f);
        Material paint = gm.Mat("Paint " + ColorUtility.ToHtmlStringRGB(color), color, 0.75f);
        LevelBuilder.Visual("Tube", PrimitiveType.Capsule, root.transform, Vector3.zero, new Vector3(0.28f, 0.42f, 0.18f), metal);
        LevelBuilder.Visual("Label", PrimitiveType.Capsule, root.transform, new Vector3(0f, -0.05f, 0f), new Vector3(0.29f, 0.25f, 0.19f), paint);
        LevelBuilder.Visual("Cap", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.12f, 0.08f, 0.12f), gm.Mat("Tube Cap", new Color(0.15f, 0.15f, 0.18f), 0.4f));
        var splat = LevelBuilder.Visual("Paint Splat", PrimitiveType.Cylinder, parent, pos + new Vector3(0.6f, 0.005f, 0.1f), new Vector3(0.5f, 0.004f, 0.35f), paint, false);
        splat.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
    }

    // قنينة حبر وريشة كتابة
    public static void InkBottle(Transform parent, Vector3 pos, float yaw, GameManager gm)
    {
        var root = new GameObject("Ink Bottle");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Material glass = gm.Mat("Ink Glass", new Color(0.1f, 0.1f, 0.22f), 0.95f, 0.1f);
        Material cork = gm.Mat("Cork", new Color(0.7f, 0.52f, 0.32f), 0.2f);
        var bottle = CharacterMeshes.Lathe(new[]
        {
            new Vector2(0f, 0f), new Vector2(0.32f, 0f), new Vector2(0.34f, 0.08f), new Vector2(0.33f, 0.45f),
            new Vector2(0.15f, 0.6f), new Vector2(0.12f, 0.75f), new Vector2(0f, 0.75f),
        }, 18);
        var b = new GameObject("Bottle");
        b.transform.SetParent(root.transform, false);
        b.AddComponent<MeshFilter>().sharedMesh = bottle;
        b.AddComponent<MeshRenderer>().sharedMaterial = glass;
        LevelBuilder.Visual("Cork", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.8f, 0f), new Vector3(0.2f, 0.07f, 0.2f), cork);
        // ريشة طويلة مايلة
        var quill = new GameObject("Quill").transform;
        quill.SetParent(root.transform, false);
        quill.localPosition = new Vector3(0.05f, 0.7f, 0f);
        quill.localRotation = Quaternion.Euler(0f, 0f, -30f);
        LevelBuilder.Visual("Feather", PrimitiveType.Sphere, quill, new Vector3(0f, 0.6f, 0f), new Vector3(0.18f, 0.7f, 0.04f), gm.Mat("Feather", new Color(0.95f, 0.93f, 0.88f), 0.2f));
        LevelBuilder.Visual("Shaft", PrimitiveType.Cylinder, quill, new Vector3(0f, 0.4f, 0f), new Vector3(0.02f, 0.55f, 0.02f), cork);
    }

    // طير ورق (أوريغامي): جسم وجناحين وراس، كلها مثلثات ورق
    public static GameObject OrigamiCrane(Transform parent, Vector3 pos, float yaw, Color color, GameManager gm, float scale = 1f)
    {
        var root = new GameObject("Origami Crane");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        root.transform.localScale = Vector3.one * scale;
        Material paper = gm.Mat("Origami " + ColorUtility.ToHtmlStringRGB(color), color, 0.15f);
        Material shade = gm.Mat("Origami Shade " + ColorUtility.ToHtmlStringRGB(color), color * 0.8f, 0.15f);
        var body = LevelBuilder.Visual("Body", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.2f, 0f), new Vector3(0.5f, 0.22f, 0.16f), paper);
        body.transform.localRotation = Quaternion.Euler(0f, 0f, 45f) * Quaternion.Euler(45f, 0f, 0f);
        for (int s = -1; s <= 1; s += 2)
        {
            var wing = LevelBuilder.Visual("Wing", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.32f, s * 0.25f), new Vector3(0.45f, 0.01f, 0.4f), s < 0 ? paper : shade);
            wing.transform.localRotation = Quaternion.Euler(s * 25f, 0f, 0f);
        }
        var neck = LevelBuilder.Visual("Neck", PrimitiveType.Cube, root.transform, new Vector3(0.32f, 0.38f, 0f), new Vector3(0.05f, 0.35f, 0.05f), paper);
        neck.transform.localRotation = Quaternion.Euler(0f, 0f, -30f);
        var tail = LevelBuilder.Visual("Tail", PrimitiveType.Cube, root.transform, new Vector3(-0.32f, 0.36f, 0f), new Vector3(0.05f, 0.32f, 0.05f), shade);
        tail.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
        return root;
    }

    // قارب ورق
    public static void OrigamiBoat(Transform parent, Vector3 pos, float yaw, Color color, GameManager gm)
    {
        var root = new GameObject("Origami Boat");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Material paper = gm.Mat("Origami " + ColorUtility.ToHtmlStringRGB(color), color, 0.15f);
        for (int s = -1; s <= 1; s += 2)
        {
            var side = LevelBuilder.Visual("Hull", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.12f, s * 0.1f), new Vector3(0.8f, 0.22f, 0.01f), paper);
            side.transform.localRotation = Quaternion.Euler(-s * 25f, 0f, 0f);
        }
        var sail = LevelBuilder.Visual("Sail", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.38f, 0f), new Vector3(0.3f, 0.3f, 0.01f), paper);
        sail.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    static Mesh pineCone;

    // شجرة صنوبر: ثلاث طبقات مخروطية خضرا فوقها ثلج
    public static void PineTree(Transform parent, Vector3 pos, float scale, GameManager gm)
    {
        if (pineCone == null)
            pineCone = CharacterMeshes.Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0.75f) }, 10);
        var root = new GameObject("Pine Tree");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        root.transform.localScale = Vector3.one * scale;
        Material trunk = gm.Mat("Pine Trunk", new Color(0.45f, 0.3f, 0.18f), 0.2f);
        Material green = gm.Mat("Pine Green", new Color(0.18f, 0.48f, 0.35f), 0.25f);
        Material snow = gm.Mat("Snow Body", new Color(0.96f, 0.98f, 1f), 0.35f);
        LevelBuilder.Visual("Trunk", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.14f, 0.15f, 0.14f), trunk);
        for (int i = 0; i < 3; i++)
        {
            float s = 1f - i * 0.25f;
            var layer = new GameObject("Layer");
            layer.transform.SetParent(root.transform, false);
            layer.transform.localPosition = new Vector3(0f, 0.25f + i * 0.38f, 0f);
            layer.transform.localScale = Vector3.one * s;
            Part(layer.transform, pineCone, green);
            var cap = new GameObject("Snow Cap");
            cap.transform.SetParent(layer.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0.38f, 0f);
            cap.transform.localScale = Vector3.one * 0.5f;
            Part(cap.transform, pineCone, snow);
        }
    }

    static Mesh rocketNose;

    // صاروخ لعبة واقف على الصفحة
    public static void Rocket(Transform parent, Vector3 pos, float scale, GameManager gm)
    {
        if (rocketNose == null)
            rocketNose = CharacterMeshes.Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.35f, 0.4f), new Vector2(0f, 0.9f) }, 16);
        var root = new GameObject("Rocket");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        root.transform.localScale = Vector3.one * scale;
        Material body = gm.Mat("Rocket Body", new Color(0.95f, 0.95f, 0.97f), 0.6f);
        Material red = gm.Mat("Rocket Red", new Color(0.95f, 0.3f, 0.35f), 0.5f);
        Material glass = gm.Mat("Saucer Glass", new Color(0.55f, 0.85f, 1f), 0.95f);
        LevelBuilder.Visual("Body", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.36f, 0.4f, 0.36f), body);
        var nose = new GameObject("Nose");
        nose.transform.SetParent(root.transform, false);
        nose.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        nose.transform.localScale = Vector3.one * 0.36f;
        Part(nose.transform, rocketNose, red);
        LevelBuilder.Visual("Window", PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.65f, -0.17f), new Vector3(0.16f, 0.16f, 0.05f), glass);
        for (int i = 0; i < 3; i++)
        {
            var fin = LevelBuilder.Visual("Fin", PrimitiveType.Cube, root.transform, Quaternion.Euler(0f, i * 120f, 0f) * new Vector3(0f, 0.25f, 0.22f), new Vector3(0.04f, 0.3f, 0.2f), red);
            fin.transform.localRotation = Quaternion.Euler(0f, i * 120f, 0f);
        }
    }

    // كوكب صغير بحلقة (زينة)
    public static void MiniPlanet(Transform parent, Vector3 pos, float size, Color color, GameManager gm)
    {
        Material body = gm.Mat("Planet " + ColorUtility.ToHtmlStringRGB(color), color, 0.3f);
        Material ring = gm.Mat("Planet Ring", new Color(0.95f, 0.88f, 0.7f), 0.4f);
        var p = LevelBuilder.Visual("Mini Planet", PrimitiveType.Sphere, parent, pos + Vector3.up * size * 0.5f, Vector3.one * size, body);
        var r = new GameObject("Ring");
        r.transform.SetParent(p.transform, false);
        r.transform.localRotation = Quaternion.Euler(70f, 0f, 20f);
        r.AddComponent<MeshFilter>().sharedMesh = PaperMeshes.Ring(0.85f, 0.05f, 32, 6);
        r.AddComponent<MeshRenderer>().sharedMaterial = ring;
    }

    // كريستالة فضائية تلمع
    public static void Crystal(Transform parent, Vector3 pos, Color color, GameManager gm, System.Random rng)
    {
        Material glow = gm.Mat("Crystal " + ColorUtility.ToHtmlStringRGB(color), color * 1.8f, 0.95f);
        int n = 2 + rng.Next(3);
        for (int i = 0; i < n; i++)
        {
            float h = 0.25f + (float)rng.NextDouble() * 0.35f;
            var c = LevelBuilder.Visual("Crystal", PrimitiveType.Cube, parent, pos + new Vector3(((float)rng.NextDouble() - 0.5f) * 0.3f, h * 0.4f, ((float)rng.NextDouble() - 0.5f) * 0.3f), new Vector3(0.1f, h, 0.1f), glow, false);
            c.transform.localRotation = Quaternion.Euler(((float)rng.NextDouble() - 0.5f) * 40f, 45f, ((float)rng.NextDouble() - 0.5f) * 40f);
        }
    }

    // رجل ثلج صغير (زينة)
    public static void MiniSnowman(Transform parent, Vector3 pos, float yaw, GameManager gm)
    {
        var root = new GameObject("Mini Snowman");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Material snow = gm.Mat("Snow Body", new Color(0.96f, 0.98f, 1f), 0.35f);
        Material coal = gm.Mat("Coal", new Color(0.08f, 0.08f, 0.1f), 0.6f);
        Material carrot = gm.Mat("Carrot", new Color(1f, 0.5f, 0.15f), 0.4f);
        LevelBuilder.Visual("Base", PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.17f, 0f), Vector3.one * 0.36f, snow);
        LevelBuilder.Visual("Head", PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.45f, 0f), Vector3.one * 0.24f, snow);
        for (int s = -1; s <= 1; s += 2)
            LevelBuilder.Visual("Eye", PrimitiveType.Sphere, root.transform, new Vector3(s * 0.045f, 0.49f, -0.1f), Vector3.one * 0.035f, coal);
        var nose = LevelBuilder.Visual("Nose", PrimitiveType.Capsule, root.transform, new Vector3(0f, 0.45f, -0.15f), new Vector3(0.035f, 0.06f, 0.035f), carrot);
        nose.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    // كومة كور ثلج
    public static void Snowballs(Transform parent, Vector3 pos, GameManager gm, System.Random rng)
    {
        Material snow = gm.Mat("Snow Body", new Color(0.96f, 0.98f, 1f), 0.35f);
        int n = 2 + rng.Next(3);
        for (int i = 0; i < n; i++)
        {
            float r = 0.12f + (float)rng.NextDouble() * 0.1f;
            Vector3 p = pos + new Vector3(((float)rng.NextDouble() - 0.5f) * 0.6f, r * 0.8f, ((float)rng.NextDouble() - 0.5f) * 0.4f);
            LevelBuilder.Visual("Snowball", PrimitiveType.Sphere, parent, p, Vector3.one * r * 2f, snow);
        }
    }

    static Vector2[] Profile(float y0, float y1, float r0, float r1)
    {
        return new[] { new Vector2(0f, y0), new Vector2(r0, y0), new Vector2(r0, y0), new Vector2(r1, y1), new Vector2(r1, y1), new Vector2(0f, y1) };
    }

    static void Part(Transform parent, Mesh mesh, Material mat)
    {
        var go = new GameObject(mesh.name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.On;
    }
}
