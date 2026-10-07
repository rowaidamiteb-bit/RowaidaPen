using UnityEngine;

// شكل رويدا المؤقت (مبني بالكود) بنفس شكلها: شعر أسود طويل بخصل، وجه بعيون كبيرة،
// سويتر أبيض واسع، بنطلون أسود واسع، جزمة بيضاء، شنطة سوداء فيها أرنب، وقلم رصاص بيدها.
// ويحرك رجولها ويدينها وشعرها وهي تمشي.
public class RowaidaModel : MonoBehaviour
{
    static readonly Vector3 HeadCenter = new Vector3(0f, 0.36f, 0f);
    static readonly Vector3 HeadRadius = new Vector3(0.235f, 0.225f, 0.22f);

    static Mesh headMesh, hairCapMesh, hairMesh, sweaterMesh, sleeveMesh, pantsMesh;
    static Texture2D faceTex, faceClosedTex;

    Transform model, hair;
    Material faceMat;

    // مفاصل الشخصية (للمشهد الافتتاحي)
    public Transform Model => model;
    public Transform LegL => legL;
    public Transform LegR => legR;
    public Transform ArmL => armL;
    public Transform ArmR => armR;

    // يسكر عيونها أو يفتحها
    public void SetEyesClosed(bool closed)
    {
        faceMat.SetTexture("_BaseMap", closed ? faceClosedTex : faceTex);
    }

    Transform legL, legR, armL, armR;
    Rigidbody rb;
    float phase;
    int lastStep;

    public static void Build(GameObject root, GameManager gm)
    {
        var me = root.AddComponent<RowaidaModel>();
        me.rb = root.GetComponent<Rigidbody>();

        Color skinColor = new Color(1f, 0.87f, 0.79f);
        if (faceTex == null)
        {
            faceTex = CharacterMeshes.FaceTexture(skinColor);
            faceClosedTex = CharacterMeshes.FaceTexture(skinColor, true);
        }
        Material face = new Material(gm.Mat("Face", Color.white, 0.3f));
        me.faceMat = face;
        face.SetTexture("_BaseMap", faceTex);
        Material skin = gm.Mat("Skin", skinColor, 0.3f);
        Material hairMat = gm.Mat("Hair", new Color(0.075f, 0.055f, 0.05f), 0.62f);
        Material sweater = gm.Mat("Sweater", new Color(0.94f, 0.93f, 0.91f), 0.12f);
        Material pants = gm.Mat("Pants", new Color(0.1f, 0.1f, 0.11f), 0.2f);
        Material shoes = gm.Mat("Shoes", new Color(0.97f, 0.97f, 0.97f), 0.35f);
        Material sole = gm.Mat("Sole", new Color(0.78f, 0.78f, 0.8f), 0.2f);
        Material bag = gm.Mat("Backpack", new Color(0.07f, 0.07f, 0.08f), 0.4f);
        Material white = gm.Mat("White", Color.white, 0.5f);
        Material pencil = gm.Mat("Pencil", new Color(1f, 0.78f, 0.15f), 0.45f);
        Material graphite = gm.Mat("Graphite", new Color(0.15f, 0.15f, 0.16f), 0.6f);

        if (headMesh == null)
        {
            headMesh = CharacterMeshes.Sphere(48, 32, false);
            hairCapMesh = CharacterMeshes.Sphere(40, 24, true);
            hairMesh = CharacterMeshes.Hair(HeadCenter, HeadRadius);
            sweaterMesh = CharacterMeshes.Lathe(new[]
            {
                new Vector2(0f, -0.27f), new Vector2(0.2f, -0.27f), new Vector2(0.218f, -0.23f), new Vector2(0.215f, -0.12f),
                new Vector2(0.205f, -0.02f), new Vector2(0.195f, 0.05f), new Vector2(0.165f, 0.11f), new Vector2(0.11f, 0.145f),
                new Vector2(0.08f, 0.16f), new Vector2(0.078f, 0.2f), new Vector2(0f, 0.2f),
            }, 24, 0.78f);
            sleeveMesh = CharacterMeshes.Lathe(new[]
            {
                new Vector2(0f, -0.27f), new Vector2(0.062f, -0.265f), new Vector2(0.066f, -0.21f), new Vector2(0.058f, -0.08f),
                new Vector2(0.055f, 0f), new Vector2(0.04f, 0.04f), new Vector2(0f, 0.05f),
            }, 14);
            pantsMesh = CharacterMeshes.Lathe(new[]
            {
                new Vector2(0f, -0.43f), new Vector2(0.098f, -0.43f), new Vector2(0.096f, -0.3f), new Vector2(0.085f, -0.12f),
                new Vector2(0.088f, 0f), new Vector2(0.07f, 0.04f), new Vector2(0f, 0.05f),
            }, 16);
        }

        // نخليها تطالع لقدام (+x) مع لفة خفيفة للكاميرا عشان يبان وجهها
        me.model = new GameObject("Model").transform;
        me.model.SetParent(root.transform, false);
        me.model.localRotation = Quaternion.LookRotation(new Vector3(1f, 0f, -0.9f));
        Transform m = me.model;

        // الرجول: بنطلون واسع وجزمة بيضاء
        me.legL = Pivot("Leg L", m, new Vector3(-0.095f, -0.2f, 0f));
        me.legR = Pivot("Leg R", m, new Vector3(0.095f, -0.2f, 0f));
        foreach (Transform leg in new[] { me.legL, me.legR })
        {
            MeshPart("Pants", pantsMesh, leg, Vector3.zero, Vector3.one, pants);
            Part("Shoe", PrimitiveType.Sphere, leg, new Vector3(0f, -0.445f, 0.045f), new Vector3(0.14f, 0.09f, 0.25f), shoes);
            Part("Sole", PrimitiveType.Sphere, leg, new Vector3(0f, -0.47f, 0.045f), new Vector3(0.145f, 0.035f, 0.255f), sole);
        }

        // سويتر أبيض واسع
        MeshPart("Sweater", sweaterMesh, m, Vector3.zero, Vector3.one, sweater);

        // اليدين
        me.armL = Pivot("Arm L", m, new Vector3(-0.2f, 0.1f, 0f));
        me.armR = Pivot("Arm R", m, new Vector3(0.2f, 0.1f, 0f));
        foreach (Transform arm in new[] { me.armL, me.armR })
        {
            MeshPart("Sleeve", sleeveMesh, arm, Vector3.zero, Vector3.one, sweater);
            Part("Hand", PrimitiveType.Sphere, arm, new Vector3(0f, -0.3f, 0f), new Vector3(0.075f, 0.085f, 0.075f), skin);
        }

        // القلم بيدها اليمين
        var pen = Pivot("Pencil", me.armR, new Vector3(0f, -0.31f, 0.02f));
        pen.localRotation = Quaternion.Euler(70f, 0f, 0f);
        Part("Wood", PrimitiveType.Cylinder, pen, new Vector3(0f, 0.03f, 0f), new Vector3(0.026f, 0.13f, 0.026f), pencil);
        Part("Tip", PrimitiveType.Sphere, pen, new Vector3(0f, -0.105f, 0f), new Vector3(0.02f, 0.04f, 0.02f), graphite);

        // الراس بالوجه المرسوم
        MeshPart("Head", headMesh, m, HeadCenter, HeadRadius, face);

        // الشعر: طاقية شعر + خصل طويلة وغرة
        me.hair = Pivot("Hair", m, HeadCenter);
        MeshPart("Hair Cap", hairCapMesh, me.hair, new Vector3(0f, 0.012f, -0.008f), HeadRadius * 1.05f, hairMat);
        MeshPart("Hair Strands", hairMesh, me.hair, -HeadCenter, Vector3.one, hairMat);
        Part("Hair Mass", PrimitiveType.Capsule, me.hair, new Vector3(0f, -0.22f, -0.085f), new Vector3(0.45f, 0.29f, 0.26f), hairMat);

        // الشنطة والأرنب
        Part("Backpack", PrimitiveType.Capsule, m, new Vector3(0f, -0.06f, -0.235f), new Vector3(0.3f, 0.17f, 0.13f), bag);
        Part("Pocket", PrimitiveType.Capsule, m, new Vector3(0f, -0.13f, -0.3f), new Vector3(0.2f, 0.07f, 0.05f), bag);
        Part("Bunny", PrimitiveType.Sphere, m, new Vector3(0.11f, -0.2f, -0.33f), Vector3.one * 0.06f, white);
        Part("Bunny Ear", PrimitiveType.Capsule, m, new Vector3(0.098f, -0.155f, -0.33f), new Vector3(0.016f, 0.025f, 0.016f), white);
        Part("Bunny Ear", PrimitiveType.Capsule, m, new Vector3(0.122f, -0.155f, -0.33f), new Vector3(0.016f, 0.025f, 0.016f), white);

        // ضوء دافي خفيف يمشي معها
        var glow = new GameObject("Warm Glow").AddComponent<Light>();
        glow.transform.SetParent(root.transform, false);
        glow.transform.localPosition = new Vector3(0.8f, 1.2f, -1.6f);
        glow.type = LightType.Point;
        glow.range = 5f;
        glow.intensity = 1.4f;
        glow.color = new Color(1f, 0.82f, 0.62f);
        glow.shadows = LightShadows.None;
    }

    static Transform Pivot(string name, Transform parent, Vector3 pos)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        return t;
    }

    static GameObject Part(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        return LevelBuilder.Visual(name, type, parent, pos, scale, mat);
    }

    static GameObject MeshPart(string name, Mesh mesh, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    void Update()
    {
        if (model == null || rb == null) return;

        float speed = Mathf.Abs(rb.linearVelocity.x);
        float amount = Mathf.Clamp01(speed / 2.5f);
        phase += speed * Time.deltaTime * 4.5f;

        // صوت خطوة مع كل نزلة رجل (إذا كانت على الأرض)
        int step = Mathf.FloorToInt(phase / Mathf.PI);
        if (step != lastStep)
        {
            lastStep = step;
            bool grounded = Physics.Raycast(transform.position, Vector3.down, 0.8f);
            if (amount > 0.4f && grounded && GameManager.Instance != null && GameManager.Instance.Sound != null)
                GameManager.Instance.Sound.PlayFootstep();
        }

        float swing = Mathf.Sin(phase) * 32f * amount;
        legL.localRotation = Quaternion.Euler(swing, 0f, 0f);
        legR.localRotation = Quaternion.Euler(-swing, 0f, 0f);
        armL.localRotation = Quaternion.Euler(-swing * 0.8f, 0f, -6f);
        armR.localRotation = Quaternion.Euler(swing * 0.5f - 20f, 0f, 6f);

        // هزة خفيفة للجسم والشعر مع المشي
        Vector3 p = model.localPosition;
        p.y = Mathf.Abs(Mathf.Sin(phase)) * 0.03f * amount;
        model.localPosition = p;
        // الشعر يطير مع الريح (ريح من قدام = الشعر يروح لورا)
        float windPush = Mathf.Clamp(-Weather.Wind * 9f, -14f, 14f) * (0.8f + 0.2f * Mathf.Sin(Time.time * 7f));
        hair.localRotation = Quaternion.Euler(-4f * amount + Mathf.Sin(phase * 2f) * 1.5f * amount + windPush, 0f, 0f);
    }
}
