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
