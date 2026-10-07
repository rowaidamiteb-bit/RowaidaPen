using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// يبني المرحلة من الكود: أرضية بيضاء بحواف سوداء، باب، ورويدا
public static class LevelBuilder
{
    const float EdgeWidth = 0.06f;

    static readonly Dictionary<PrimitiveType, Mesh> meshCache = new Dictionary<PrimitiveType, Mesh>();
    static PhysicsMaterial slippery;

    public static GameObject Build(LevelData data, GameManager gm)
    {
        var root = new GameObject("Level - " + data.title);
        foreach (Block b in data.blocks) CreateBlock(root.transform, b, gm);
        CreateDoor(root.transform, data.door, gm);
        CreateRowaida(root.transform, data.spawn, gm);
        return root;
    }

    // صفحة دفتر بخطوط خفيفة ورا المرحلة
    public static GameObject BuildBackground(GameManager gm)
    {
        var mat = new Material(gm.paperMaterial) { name = "Notebook Page" };
        Vector2 size = new Vector2(240f, 80f);
        mat.SetTexture("_BaseMap", CreateRuledPaperTexture());
        mat.SetTextureScale("_BaseMap", size / 8f);
        return Visual("Notebook Page", PrimitiveType.Quad, null, new Vector3(30f, -5f, 6f), new Vector3(size.x, size.y, 1f), mat, false);
    }

    static void CreateBlock(Transform parent, Block b, GameManager gm)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Ground";
        go.transform.SetParent(parent, false);
        go.transform.position = b.center;
        go.transform.localScale = b.size;
        go.GetComponent<Renderer>().sharedMaterial = gm.paperMaterial;
        AddOutline(parent, b.center, b.size, gm.inkMaterial);
    }

    // خطوط سوداء على حواف المكعب كأنها مرسومة بالقلم
    public static void AddOutline(Transform parent, Vector3 c, Vector3 s, Material ink)
    {
        Vector3 h = s * 0.5f;
        for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
                Edge(parent, c + new Vector3(0f, y * h.y, z * h.z), new Vector3(s.x + EdgeWidth, EdgeWidth, EdgeWidth), ink);
        for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
                Edge(parent, c + new Vector3(x * h.x, 0f, z * h.z), new Vector3(EdgeWidth, s.y + EdgeWidth, EdgeWidth), ink);
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                Edge(parent, c + new Vector3(x * h.x, y * h.y, 0f), new Vector3(EdgeWidth, EdgeWidth, s.z + EdgeWidth), ink);
    }

    static void Edge(Transform parent, Vector3 pos, Vector3 scale, Material ink)
    {
        var go = Visual("Edge", PrimitiveType.Cube, parent, Vector3.zero, scale, ink, false);
        go.transform.position = pos;
    }

    static void CreateDoor(Transform parent, Vector3 basePos, GameManager gm)
    {
        const float w = 1.4f, h = 2.4f, t = 0.18f, d = 0.5f;

        var door = new GameObject("Door");
        door.transform.SetParent(parent, false);
        door.transform.position = basePos;

        Visual("Post L", PrimitiveType.Cube, door.transform, new Vector3(-w / 2f, h / 2f, 0f), new Vector3(t, h, d), gm.inkMaterial);
        Visual("Post R", PrimitiveType.Cube, door.transform, new Vector3(w / 2f, h / 2f, 0f), new Vector3(t, h, d), gm.inkMaterial);
        Visual("Top", PrimitiveType.Cube, door.transform, new Vector3(0f, h + t / 2f, 0f), new Vector3(w + t, t, d), gm.inkMaterial);
        Visual("Panel", PrimitiveType.Cube, door.transform, new Vector3(0f, h / 2f, 0.1f), new Vector3(w - t, h, 0.1f), gm.paperMaterial);
        Visual("Knob", PrimitiveType.Sphere, door.transform, new Vector3(0.38f, 1.15f, 0f), Vector3.one * 0.14f, gm.inkMaterial);

        var trigger = door.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, h / 2f, 0f);
        trigger.size = new Vector3(w * 0.6f, h, 2f);
        door.AddComponent<Door>();
    }

    static void CreateRowaida(Transform parent, Vector3 pos, GameManager gm)
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

        var col = go.AddComponent<CapsuleCollider>();
        col.height = 1.4f;
        col.radius = 0.35f;
        col.sharedMaterial = slippery;

        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;

        // شكل مؤقت: كبسولة رمادية بعيون تطالع لقدام
        Visual("Body", PrimitiveType.Capsule, go.transform, Vector3.zero, Vector3.one * 0.7f, gm.rowaidaMaterial);
        for (int i = -1; i <= 1; i += 2)
        {
            float z = i * 0.13f;
            Visual("Eye", PrimitiveType.Sphere, go.transform, new Vector3(0.29f, 0.3f, z), Vector3.one * 0.15f, gm.paperMaterial);
            Visual("Pupil", PrimitiveType.Sphere, go.transform, new Vector3(0.355f, 0.3f, z), Vector3.one * 0.07f, gm.inkMaterial);
        }

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

    static Texture2D CreateRuledPaperTexture()
    {
        const int size = 256;
        const int lineEvery = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "Ruled Paper" };
        Color paper = new Color(0.97f, 0.97f, 0.95f);
        Color line = new Color(0.78f, 0.78f, 0.78f);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            Color c = (y % lineEvery) < 2 ? line : paper;
            for (int x = 0; x < size; x++) pixels[y * size + x] = c;
        }
        tex.SetPixels(pixels);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 8;
        tex.Apply();
        return tex;
    }
}
