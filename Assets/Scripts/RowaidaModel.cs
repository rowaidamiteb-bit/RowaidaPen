using UnityEngine;

// شكل رويدا المؤقت (من أشكال بسيطة) بنفس ألوانها: شعر أسود طويل، سويتر أبيض،
// بنطلون أسود واسع، جزمة بيضاء، شنطة سوداء فيها أرنب، وقلم رصاص بيدها.
// ويحرك رجولها ويدينها وهي تمشي.
public class RowaidaModel : MonoBehaviour
{
    Transform model;
    Transform legL, legR, armL, armR;
    Rigidbody rb;
    float phase;
    float modelBaseY;

    public static void Build(GameObject root, GameManager gm)
    {
        var me = root.AddComponent<RowaidaModel>();
        me.rb = root.GetComponent<Rigidbody>();

        Material skin = gm.Mat("Skin", new Color(1f, 0.86f, 0.77f), 0.25f);
        Material hair = gm.Mat("Hair", new Color(0.07f, 0.055f, 0.05f), 0.55f);
        Material sweater = gm.Mat("Sweater", new Color(0.93f, 0.92f, 0.9f), 0.1f);
        Material pants = gm.Mat("Pants", new Color(0.09f, 0.09f, 0.1f), 0.15f);
        Material shoes = gm.Mat("Shoes", new Color(0.97f, 0.97f, 0.97f), 0.3f);
        Material bag = gm.Mat("Backpack", new Color(0.06f, 0.06f, 0.07f), 0.35f);
        Material eye = gm.Mat("Eye", new Color(0.13f, 0.08f, 0.06f), 0.9f);
        Material white = gm.Mat("White", Color.white, 0.6f);
        Material blush = gm.Mat("Blush", new Color(1f, 0.62f, 0.62f), 0.2f);
        Material pencil = gm.Mat("Pencil", new Color(1f, 0.78f, 0.15f), 0.4f);

        // نخليها تطالع لقدام (+x) مع لفة خفيفة للكاميرا عشان يبان وجهها
        me.model = new GameObject("Model").transform;
        me.model.SetParent(root.transform, false);
        me.model.localRotation = Quaternion.LookRotation(new Vector3(1f, 0f, -0.75f));
        me.modelBaseY = 0f;
        Transform m = me.model;

        // الرجول (البنطلون الواسع والجزمة)
        me.legL = Pivot("Leg L", m, new Vector3(-0.09f, -0.2f, 0f));
        me.legR = Pivot("Leg R", m, new Vector3(0.09f, -0.2f, 0f));
        foreach (Transform leg in new[] { me.legL, me.legR })
        {
            Part("Pants", PrimitiveType.Cylinder, leg, new Vector3(0f, -0.2f, 0f), new Vector3(0.18f, 0.2f, 0.18f), pants);
            Part("Shoe", PrimitiveType.Sphere, leg, new Vector3(0f, -0.44f, 0.04f), new Vector3(0.15f, 0.1f, 0.25f), shoes);
        }

        // الجسم (سويتر أبيض واسع)
        Part("Sweater", PrimitiveType.Capsule, m, new Vector3(0f, -0.04f, 0f), new Vector3(0.4f, 0.21f, 0.3f), sweater);

        // اليدين
        me.armL = Pivot("Arm L", m, new Vector3(-0.21f, 0.09f, 0f));
        me.armR = Pivot("Arm R", m, new Vector3(0.21f, 0.09f, 0f));
        foreach (Transform arm in new[] { me.armL, me.armR })
        {
            Part("Sleeve", PrimitiveType.Capsule, arm, new Vector3(0f, -0.14f, 0f), new Vector3(0.12f, 0.15f, 0.12f), sweater);
            Part("Hand", PrimitiveType.Sphere, arm, new Vector3(0f, -0.29f, 0f), Vector3.one * 0.085f, skin);
        }

        // القلم بيدها اليمين
        var pen = Pivot("Pencil", me.armR, new Vector3(0f, -0.3f, 0.02f));
        pen.localRotation = Quaternion.Euler(65f, 0f, 0f);
        Part("Wood", PrimitiveType.Cylinder, pen, new Vector3(0f, 0.02f, 0f), new Vector3(0.028f, 0.13f, 0.028f), pencil);
        Part("Tip", PrimitiveType.Sphere, pen, new Vector3(0f, -0.115f, 0f), new Vector3(0.022f, 0.04f, 0.022f), hair);

        // الراس والوجه
        Part("Head", PrimitiveType.Sphere, m, new Vector3(0f, 0.36f, 0f), Vector3.one * 0.46f, skin);
        for (int s = -1; s <= 1; s += 2)
        {
            Part("Eye", PrimitiveType.Sphere, m, new Vector3(s * 0.08f, 0.34f, 0.2f), new Vector3(0.075f, 0.095f, 0.05f), eye);
            Part("Shine", PrimitiveType.Sphere, m, new Vector3(s * 0.08f - 0.012f, 0.36f, 0.224f), Vector3.one * 0.026f, white);
            Part("Blush", PrimitiveType.Sphere, m, new Vector3(s * 0.135f, 0.28f, 0.172f), new Vector3(0.06f, 0.03f, 0.02f), blush);
        }
        Part("Mouth", PrimitiveType.Sphere, m, new Vector3(0f, 0.255f, 0.205f), new Vector3(0.04f, 0.014f, 0.02f), blush);

        // الشعر: طويل وأسود مع غرة
        Part("Hair Back", PrimitiveType.Sphere, m, new Vector3(0f, 0.41f, -0.08f), Vector3.one * 0.5f, hair);
        Part("Bangs", PrimitiveType.Sphere, m, new Vector3(0f, 0.5f, 0.09f), new Vector3(0.42f, 0.17f, 0.26f), hair);
        Part("Hair Long", PrimitiveType.Capsule, m, new Vector3(0f, 0.12f, -0.13f), new Vector3(0.44f, 0.33f, 0.2f), hair);
        for (int s = -1; s <= 1; s += 2)
            Part("Hair Side", PrimitiveType.Capsule, m, new Vector3(s * 0.2f, 0.2f, -0.01f), new Vector3(0.12f, 0.2f, 0.12f), hair);

        // الشنطة والأرنب
        var pack = Part("Backpack", PrimitiveType.Cube, m, new Vector3(0f, -0.07f, -0.24f), new Vector3(0.3f, 0.32f, 0.13f), bag);
        pack.transform.localRotation = Quaternion.Euler(-5f, 0f, 0f);
        Part("Bunny", PrimitiveType.Sphere, m, new Vector3(0.1f, -0.2f, -0.31f), Vector3.one * 0.06f, white);
        Part("Bunny Ear", PrimitiveType.Capsule, m, new Vector3(0.088f, -0.155f, -0.31f), new Vector3(0.016f, 0.025f, 0.016f), white);
        Part("Bunny Ear", PrimitiveType.Capsule, m, new Vector3(0.112f, -0.155f, -0.31f), new Vector3(0.016f, 0.025f, 0.016f), white);

        // ضوء دافي خفيف يمشي معها
        var glow = new GameObject("Warm Glow").AddComponent<Light>();
        glow.transform.SetParent(root.transform, false);
        glow.transform.localPosition = new Vector3(0.5f, 1.4f, -1.5f);
        glow.type = LightType.Point;
        glow.range = 6f;
        glow.intensity = 1.6f;
        glow.color = new Color(1f, 0.82f, 0.6f);
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

    void Update()
    {
        if (model == null || rb == null) return;

        float speed = Mathf.Abs(rb.linearVelocity.x);
        float amount = Mathf.Clamp01(speed / 2.5f);
        phase += speed * Time.deltaTime * 4.5f;

        float swing = Mathf.Sin(phase) * 35f * amount;
        legL.localRotation = Quaternion.Euler(swing, 0f, 0f);
        legR.localRotation = Quaternion.Euler(-swing, 0f, 0f);
        armL.localRotation = Quaternion.Euler(-swing * 0.8f, 0f, -5f);
        armR.localRotation = Quaternion.Euler(swing * 0.5f - 15f, 0f, 5f);

        Vector3 p = model.localPosition;
        p.y = modelBaseY + Mathf.Abs(Mathf.Sin(phase)) * 0.035f * amount;
        model.localPosition = p;
    }
}
