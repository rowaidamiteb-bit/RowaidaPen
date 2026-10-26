using System.Collections.Generic;
using UnityEngine;

// حياة زيادة للمطبخ وأرض الحلويات: بوتجاز ورا بقدور تغلي، لمبات معلقة تتمايل، قطوة نايمة على رف،
// رسومات أطفال على الجدار، علاقة فلفل وثوم، رف بهارات... وبأرض الحلويات: شلال شوكولاتة، بيوت زنجبيل،
// سور عصي حلاوة، شجر آيس كريم، قوس قزح حلاوة، وبالونات قلوب
public class KitchenExtras : MonoBehaviour
{
    readonly List<Transform> lamps = new List<Transform>();
    readonly List<Transform> lids = new List<Transform>();
    readonly List<Vector3> steamAt = new List<Vector3>();
    readonly List<Transform> balloons = new List<Transform>();
    readonly List<Vector3> balloonHome = new List<Vector3>();
    readonly List<Transform> sway = new List<Transform>();
    Transform cat, catTail, catHead;
    Vector3 catPos;
    float nextMeow;
    Material fall;
    bool kitchen;

    public static void Build(Transform root, LevelData data, float minX, float maxX, GameManager gm, System.Random rng)
    {
        var go = new GameObject("Kitchen Extras");
        go.transform.SetParent(root, false);
        var e = go.AddComponent<KitchenExtras>();
        e.kitchen = data.kitchenScenery == 1;
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        if (e.kitchen) e.BuildKitchen(go.transform, minX, maxX, gm, rng, R);
        else e.BuildCandy(go.transform, minX, maxX, gm, rng, R);
    }

    // ---------- المطبخ ----------

    void BuildKitchen(Transform p, float minX, float maxX, GameManager gm, System.Random rng, System.Func<float, float, float> R)
    {
        Material steel = gm.Mat("Sharpener Steel", new Color(0.8f, 0.82f, 0.86f), 0.9f, 0.8f);
        Material black = gm.Mat("Stove Black", new Color(0.15f, 0.14f, 0.16f), 0.6f, 0.3f);
        Material white = gm.Mat("Fridge White", new Color(0.92f, 0.96f, 0.98f), 0.7f);
        Material counterTop = gm.Mat("Counter Top", new Color(0.55f, 0.75f, 0.78f), 0.6f);
        Material wood = gm.Mat("Shelf Wood", new Color(0.75f, 0.55f, 0.35f), 0.3f);
        Material flame = gm.Mat("Stove Flame", new Color(0.4f, 0.6f, 1.6f), 0.5f);
        Color[] potCols = { new Color(0.9f, 0.35f, 0.3f), new Color(0.3f, 0.6f, 0.85f), new Color(0.95f, 0.75f, 0.25f) };

        // كاونتر طويل ورا تحت الجدار
        LevelBuilder.Visual("Back Counter", PrimitiveType.Cube, p, new Vector3((minX + maxX) * 0.5f, -1.4f, 7.6f), new Vector3(maxX - minX + 40f, 2.6f, 1.6f), white, false);
        LevelBuilder.Visual("Back Counter Top", PrimitiveType.Cube, p, new Vector3((minX + maxX) * 0.5f, -0.05f, 7.5f), new Vector3(maxX - minX + 40f, 0.15f, 1.8f), counterTop, false);

        int k = 0;
        for (float x = minX - 6f; x < maxX + 6f; x += R(11f, 15f), k++)
        {
            // بوتجاز بقدرين يغلون
            LevelBuilder.Visual("Back Stove", PrimitiveType.Cube, p, new Vector3(x, 0.08f, 7.4f), new Vector3(3.4f, 0.12f, 1.6f), black, false);
            for (int i = 0; i < 2; i++)
            {
                float px = x - 0.8f + i * 1.6f;
                Material pot = gm.Mat("Back Pot " + (k + i) % 3, potCols[(k + i) % 3], 0.7f, 0.2f);
                LevelBuilder.Visual("Back Pot", PrimitiveType.Cylinder, p, new Vector3(px, 0.6f, 7.4f), new Vector3(1.1f, 0.45f, 1.1f), pot, false);
                for (int s = -1; s <= 1; s += 2) LevelBuilder.Visual("Pot Handle", PrimitiveType.Cube, p, new Vector3(px + s * 0.62f, 0.85f, 7.4f), new Vector3(0.2f, 0.08f, 0.2f), black, false);
                var lid = new GameObject("Pot Lid").transform;
                lid.SetParent(p, false);
                lid.position = new Vector3(px, 1.08f, 7.4f);
                LevelBuilder.Visual("Lid", PrimitiveType.Cylinder, lid, Vector3.zero, new Vector3(1.15f, 0.04f, 1.15f), steel, false);
                LevelBuilder.Visual("Knob", PrimitiveType.Sphere, lid, new Vector3(0f, 0.08f, 0f), Vector3.one * 0.16f, black, false);
                lids.Add(lid);
                steamAt.Add(new Vector3(px, 1.3f, 7.4f));
                for (int f = 0; f < 5; f++)
                {
                    float a = f / 5f * Mathf.PI * 2f;
                    LevelBuilder.Visual("Flame", PrimitiveType.Capsule, p, new Vector3(px + Mathf.Cos(a) * 0.35f, 0.18f, 7.4f + Mathf.Sin(a) * 0.35f), new Vector3(0.08f, 0.08f, 0.08f), flame, false);
                }
            }
            // لمبة معلقة بضو دافي
            var lamp = new GameObject("Hanging Lamp").transform;
            lamp.SetParent(p, false);
            lamp.position = new Vector3(x + R(4f, 6f), 9.5f, 4f);
            LevelBuilder.Visual("Cord", PrimitiveType.Cube, lamp, new Vector3(0f, -1.2f, 0f), new Vector3(0.04f, 2.4f, 0.04f), black, false);
            LevelBuilder.Visual("Shade", PrimitiveType.Sphere, lamp, new Vector3(0f, -2.5f, 0f), new Vector3(1.2f, 0.7f, 1.2f), gm.Mat("Lamp Shade", new Color(1f, 0.6f, 0.35f), 0.4f), false);
            LevelBuilder.Visual("Bulb", PrimitiveType.Sphere, lamp, new Vector3(0f, -2.85f, 0f), Vector3.one * 0.35f, gm.Mat("Bulb Glow", new Color(2.5f, 2f, 1.2f), 0.5f), false);
            var l = new GameObject("Lamp Light").AddComponent<Light>();
            l.transform.SetParent(lamp, false);
            l.transform.localPosition = new Vector3(0f, -3.2f, -0.5f);
            l.type = LightType.Point;
            l.color = new Color(1f, 0.8f, 0.55f);
            l.range = 6f;
            l.intensity = 1.6f;
            lamps.Add(lamp);
            // رسمة طفل معلقة على الجدار (ورق ملون وشخابيط)
            var drawing = new GameObject("Kid Drawing").transform;
            drawing.SetParent(p, false);
            drawing.position = new Vector3(x + R(-4f, -2f), R(3f, 4f), 8.85f);
            drawing.localRotation = Quaternion.Euler(0f, 0f, R(-8f, 8f));
            LevelBuilder.Visual("Paper", PrimitiveType.Cube, drawing, Vector3.zero, new Vector3(1.4f, 1.1f, 0.02f), gm.Mat("Drawing Paper", new Color(1f, 0.99f, 0.95f), 0.2f), false);
            LevelBuilder.Visual("Sun", PrimitiveType.Sphere, drawing, new Vector3(0.38f, 0.28f, -0.02f), new Vector3(0.28f, 0.28f, 0.01f), gm.Mat("Drawing Sun", new Color(1f, 0.8f, 0.2f), 0.3f), false);
            LevelBuilder.Visual("House", PrimitiveType.Cube, drawing, new Vector3(-0.2f, -0.2f, -0.02f), new Vector3(0.45f, 0.35f, 0.01f), gm.Mat("Drawing Red", new Color(0.95f, 0.35f, 0.3f), 0.3f), false);
            var roof = LevelBuilder.Visual("Roof", PrimitiveType.Cube, drawing, new Vector3(-0.2f, 0.05f, -0.02f), new Vector3(0.32f, 0.32f, 0.01f), gm.Mat("Drawing Blue", new Color(0.35f, 0.55f, 0.95f), 0.3f), false).transform;
            roof.localRotation = Quaternion.Euler(0f, 0f, 45f);
            LevelBuilder.Visual("Grass", PrimitiveType.Cube, drawing, new Vector3(0f, -0.42f, -0.02f), new Vector3(1.3f, 0.08f, 0.01f), gm.Mat("Drawing Green", new Color(0.4f, 0.8f, 0.35f), 0.3f), false);
            LevelBuilder.Visual("Tape", PrimitiveType.Cube, drawing, new Vector3(0f, 0.55f, -0.03f), new Vector3(0.4f, 0.12f, 0.01f), gm.Mat("Tape", new Color(1f, 0.95f, 0.7f), 0.3f), false);
            // علاقة فلفل وثوم
            if (k % 2 == 0)
            {
                var string2 = new GameObject("Chili String").transform;
                string2.SetParent(p, false);
                string2.position = new Vector3(x + R(1f, 2f), 6.2f, 8.6f);
                Material chili = gm.Mat("Chili", new Color(0.9f, 0.15f, 0.12f), 0.7f);
                Material garlic = gm.Mat("Garlic", new Color(0.98f, 0.95f, 0.88f), 0.4f);
                for (int c = 0; c < 6; c++)
                {
                    var piece = LevelBuilder.Visual(c % 3 == 2 ? "Garlic" : "Chili", c % 3 == 2 ? PrimitiveType.Sphere : PrimitiveType.Capsule, string2, new Vector3(Mathf.Sin(c) * 0.12f, -c * 0.3f, 0f), c % 3 == 2 ? Vector3.one * 0.28f : new Vector3(0.12f, 0.2f, 0.12f), c % 3 == 2 ? garlic : chili, false).transform;
                    piece.localRotation = Quaternion.Euler(0f, 0f, c % 2 == 0 ? 25f : -25f);
                }
                sway.Add(string2);
            }
            // رف بهارات صغيرة
            if (k % 2 == 1)
            {
                float sy = R(2.8f, 3.4f);
                LevelBuilder.Visual("Spice Rack", PrimitiveType.Cube, p, new Vector3(x + 2f, sy, 8.5f), new Vector3(2.4f, 0.1f, 0.5f), wood, false);
                Color[] sp = { new Color(0.85f, 0.35f, 0.2f), new Color(0.95f, 0.8f, 0.3f), new Color(0.4f, 0.6f, 0.3f), new Color(0.6f, 0.3f, 0.2f) };
                for (int s = 0; s < 4; s++)
                {
                    LevelBuilder.Visual("Spice Jar", PrimitiveType.Cylinder, p, new Vector3(x + 1.1f + s * 0.6f, sy + 0.3f, 8.45f), new Vector3(0.3f, 0.25f, 0.3f), gm.Mat("Spice " + s, sp[s], 0.6f), false);
                    LevelBuilder.Visual("Spice Cap", PrimitiveType.Cylinder, p, new Vector3(x + 1.1f + s * 0.6f, sy + 0.58f, 8.45f), new Vector3(0.32f, 0.04f, 0.32f), black, false);
                }
            }
        }

        // قطوة نايمة على رف (تصحى إذا قربت رويدا)
        float cx = (minX + maxX) * 0.5f + 6f;
        LevelBuilder.Visual("Cat Shelf", PrimitiveType.Cube, p, new Vector3(cx, 5.6f, 7.6f), new Vector3(3f, 0.18f, 1.2f), wood, false);
        catPos = new Vector3(cx, 5.69f, 7.5f);
        cat = new GameObject("Kitchen Cat").transform;
        cat.SetParent(p, false);
        cat.position = catPos;
        Material fur = gm.Mat("Cat Fur", new Color(1f, 0.72f, 0.4f), 0.35f);
        Material stripe = gm.Mat("Cat Stripe", new Color(0.85f, 0.5f, 0.22f), 0.35f);
        Material pink = gm.Mat("Bunny Pink", new Color(1f, 0.7f, 0.75f), 0.3f);
        Material dark = gm.Mat("Monster Pupil", new Color(0.05f, 0.04f, 0.06f), 0.9f);
        LevelBuilder.Visual("Body", PrimitiveType.Sphere, cat, new Vector3(0f, 0.3f, 0f), new Vector3(1.3f, 0.6f, 0.8f), fur, false);
        for (int s = 0; s < 3; s++) LevelBuilder.Visual("Stripe", PrimitiveType.Cube, cat, new Vector3(-0.3f + s * 0.3f, 0.58f, 0f), new Vector3(0.1f, 0.05f, 0.7f), stripe, false);
        catHead = new GameObject("Head").transform;
        catHead.SetParent(cat, false);
        catHead.localPosition = new Vector3(0.65f, 0.35f, -0.05f);
        LevelBuilder.Visual("Head", PrimitiveType.Sphere, catHead, Vector3.zero, new Vector3(0.6f, 0.5f, 0.55f), fur, false);
        for (int s = -1; s <= 1; s += 2)
        {
            var ear = LevelBuilder.Visual("Ear", PrimitiveType.Cube, catHead, new Vector3(0.05f, 0.25f, s * 0.15f), new Vector3(0.15f, 0.18f, 0.06f), fur, false).transform;
            ear.localRotation = Quaternion.Euler(0f, 0f, 45f);
            LevelBuilder.Visual("Eye Closed", PrimitiveType.Cube, catHead, new Vector3(0.22f, 0.04f, s * 0.12f), new Vector3(0.02f, 0.02f, 0.1f), dark, false);
        }
        LevelBuilder.Visual("Nose", PrimitiveType.Sphere, catHead, new Vector3(0.28f, -0.04f, 0f), Vector3.one * 0.06f, pink, false);
        catTail = new GameObject("Tail").transform;
        catTail.SetParent(cat, false);
        catTail.localPosition = new Vector3(-0.6f, 0.25f, 0f);
        for (int i = 0; i < 5; i++) LevelBuilder.Visual("Tail Seg", PrimitiveType.Sphere, catTail, new Vector3(-i * 0.15f, -i * 0.12f, 0f), Vector3.one * 0.16f, i % 2 == 0 ? fur : stripe, false);
    }

    // ---------- أرض الحلويات ----------

    void BuildCandy(Transform p, float minX, float maxX, GameManager gm, System.Random rng, System.Func<float, float, float> R)
    {
        Color[] cols = { new Color(1f, 0.55f, 0.7f), new Color(0.55f, 0.8f, 1f), new Color(1f, 0.85f, 0.4f), new Color(0.6f, 0.9f, 0.6f), new Color(0.75f, 0.6f, 1f) };
        // شلال شوكولاتة كبير ورا
        float fx = (minX + maxX) * 0.5f;
        var tex = new Texture2D(8, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < 64; y++) for (int x = 0; x < 8; x++)
                tex.SetPixel(x, y, Color.Lerp(new Color(0.38f, 0.21f, 0.12f), new Color(0.6f, 0.36f, 0.2f), Mathf.Clamp01(Mathf.Sin(y / 64f * Mathf.PI * 6f + x) * 1.4f - 0.4f)));
        tex.Apply();
        fall = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Choco Fall" };
        fall.SetTexture("_BaseMap", tex);
        fall.SetColor("_BaseColor", Color.white);
        fall.SetTextureScale("_BaseMap", new Vector2(1f, 3f));
        LevelBuilder.Visual("Choco Cliff", PrimitiveType.Cube, p, new Vector3(fx, 4f, 17f), new Vector3(12f, 14f, 3f), gm.Mat("Choco Cliff", new Color(0.45f, 0.26f, 0.15f), 0.4f), false);
        LevelBuilder.Visual("Choco Fall", PrimitiveType.Quad, p, new Vector3(fx, 3f, 15.4f), new Vector3(4f, 13f, 1f), fall, false);
        LevelBuilder.Visual("Choco Pool", PrimitiveType.Cylinder, p, new Vector3(fx, -3.3f, 14f), new Vector3(9f, 0.2f, 4f), gm.Mat("Choco Drip", new Color(0.4f, 0.22f, 0.13f), 0.7f), false);
        // بيوت زنجبيل بشبابيك تضوي
        Material ginger = gm.Mat("Gingerbread", new Color(0.72f, 0.45f, 0.25f), 0.3f);
        Material icing = gm.Mat("Icing White", new Color(1f, 0.98f, 0.96f), 0.5f);
        Material window = gm.Mat("Ginger Window", new Color(2f, 1.6f, 0.6f), 0.5f);
        for (float x = minX - 4f; x < maxX + 4f; x += R(12f, 18f))
        {
            if (Mathf.Abs(x - fx) < 7f) continue;
            var h = new GameObject("Gingerbread House").transform;
            h.SetParent(p, false);
            h.position = new Vector3(x, -2.2f, R(9f, 11f));
            LevelBuilder.Visual("Walls", PrimitiveType.Cube, h, new Vector3(0f, 1.2f, 0f), new Vector3(3f, 2.4f, 2.4f), ginger, false);
            for (int s = -1; s <= 1; s += 2)
            {
                var roof = LevelBuilder.Visual("Roof", PrimitiveType.Cube, h, new Vector3(s * 0.85f, 3f, 0f), new Vector3(2.2f, 0.25f, 2.7f), ginger, false).transform;
                roof.localRotation = Quaternion.Euler(0f, 0f, -s * 35f);
                LevelBuilder.Visual("Icing Edge", PrimitiveType.Cube, roof, new Vector3(0f, -0.6f, 0f), new Vector3(1.02f, 0.4f, 1.02f), icing, false);
                LevelBuilder.Visual("Window", PrimitiveType.Cube, h, new Vector3(s * 0.8f, 1.4f, -1.21f), new Vector3(0.6f, 0.6f, 0.02f), window, false);
            }
            LevelBuilder.Visual("Door", PrimitiveType.Cube, h, new Vector3(0f, 0.6f, -1.21f), new Vector3(0.6f, 1.1f, 0.02f), gm.Mat("Cane Red", new Color(0.95f, 0.2f, 0.28f), 0.7f), false);
            for (int g = 0; g < 4; g++)
                LevelBuilder.Visual("Gumdrop", PrimitiveType.Sphere, h, new Vector3(-1.2f + g * 0.8f, 2.45f, -1.25f), Vector3.one * 0.3f, gm.Mat("Sweet " + g, cols[g], 0.8f), false);
        }
        // شجر آيس كريم
        Material cone = gm.Mat("Waffle Cone", new Color(0.88f, 0.65f, 0.35f), 0.3f);
        for (float x = minX - 6f; x < maxX + 6f; x += R(7f, 11f))
        {
            var ic = new GameObject("Ice Cream Tree").transform;
            ic.SetParent(p, false);
            ic.position = new Vector3(x, -2f, R(5.5f, 7f));
            float h = R(2.5f, 4f);
            LevelBuilder.Visual("Cone", PrimitiveType.Cylinder, ic, new Vector3(0f, h * 0.5f, 0f), new Vector3(0.6f, h * 0.5f, 0.6f), cone, false);
            for (int s = 0; s < 2; s++)
                LevelBuilder.Visual("Scoop", PrimitiveType.Sphere, ic, new Vector3(0f, h + 0.35f + s * 0.7f, 0f), Vector3.one * (1.3f - s * 0.2f), gm.Mat("Scoop " + rng.Next(5), cols[rng.Next(cols.Length)], 0.5f), false);
            LevelBuilder.Visual("Cherry", PrimitiveType.Sphere, ic, new Vector3(0f, h + 1.65f, 0f), Vector3.one * 0.3f, gm.Mat("Apple", new Color(0.95f, 0.25f, 0.25f), 0.6f), false);
            sway.Add(ic);
        }
        // قوس قزح حلاوة
        for (int band = 0; band < 5; band++)
        {
            Material m = gm.Mat("Rainbow Candy " + band, cols[band], 0.6f);
            float rad = 10f - band * 0.7f;
            for (int i = 0; i <= 16; i++)
            {
                float a = i / 16f * Mathf.PI;
                LevelBuilder.Visual("Rainbow Bit", PrimitiveType.Sphere, p, new Vector3(minX + 12f + Mathf.Cos(a) * rad, -2f + Mathf.Sin(a) * rad, 22f), Vector3.one * 0.85f, m, false);
            }
        }
        // سور عصي حلاوة ورا الصفحات
        Material red = gm.Mat("Cane Red", new Color(0.95f, 0.2f, 0.28f), 0.7f);
        Material whiteC = gm.Mat("Cane White", new Color(1f, 0.98f, 0.97f), 0.7f);
        for (float x = minX - 2f; x < maxX + 2f; x += 1.2f)
            for (int s = 0; s < 3; s++)
                LevelBuilder.Visual("Fence Cane", PrimitiveType.Cylinder, p, new Vector3(x, -1.2f + s * 0.4f, 3.4f), new Vector3(0.18f, 0.2f, 0.18f), s % 2 == 0 ? red : whiteC, false);
        // بالونات قلوب تطير
        for (int i = 0; i < 7; i++)
        {
            var b = new GameObject("Heart Balloon").transform;
            b.SetParent(p, false);
            Vector3 home = new Vector3(R(minX, maxX), R(3f, 7f), R(5f, 9f));
            b.position = home;
            Material m = gm.Mat("Balloon " + i % 5, cols[i % 5] * 1.1f, 0.85f);
            for (int s = -1; s <= 1; s += 2) LevelBuilder.Visual("Lobe", PrimitiveType.Sphere, b, new Vector3(s * 0.22f, 0.15f, 0f), Vector3.one * 0.55f, m, false);
            var tip = LevelBuilder.Visual("Tip", PrimitiveType.Cube, b, new Vector3(0f, -0.12f, 0f), new Vector3(0.45f, 0.45f, 0.4f), m, false).transform;
            tip.localRotation = Quaternion.Euler(0f, 0f, 45f);
            LevelBuilder.Visual("String", PrimitiveType.Cube, b, new Vector3(0f, -0.9f, 0f), new Vector3(0.02f, 1.2f, 0.02f), whiteC, false);
            balloons.Add(b);
            balloonHome.Add(home);
        }
    }

    void Update()
    {
        float t = Time.time, dt = Time.deltaTime;
        for (int i = 0; i < lamps.Count; i++) lamps[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.9f + i) * 4f);
        for (int i = 0; i < sway.Count; i++) sway[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.2f + i) * 3f);
        // أغطية القدور تنط من البخار
        for (int i = 0; i < lids.Count; i++)
        {
            float jig = Mathf.Max(0f, Mathf.Sin(t * 9f + i * 2.3f)) * Mathf.Max(0f, Mathf.Sin(t * 0.7f + i));
            lids[i].localPosition = new Vector3(lids[i].localPosition.x, 1.08f + jig * 0.08f, lids[i].localPosition.z);
            lids[i].localRotation = Quaternion.Euler(0f, 0f, jig * 6f);
            if (Random.value < 0.03f) SmokeCloud.Puff(steamAt[i] + new Vector3(Random.Range(-0.3f, 0.3f), 0f, 0f), Random.Range(0.6f, 1f));
        }
        if (fall != null) fall.SetTextureOffset("_BaseMap", new Vector2(0f, t * 0.4f));
        for (int i = 0; i < balloons.Count; i++)
        {
            balloons[i].position = balloonHome[i] + new Vector3(Mathf.Sin(t * 0.3f + i) * 1.5f, Mathf.Sin(t * 0.8f + i * 2f) * 0.4f, 0f);
            balloons[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.1f + i) * 8f);
        }
        // القطوة: تتنفس ويتحرك ذيلها، وإذا قربت رويدا ترفع راسها وتماوي
        if (cat != null)
        {
            cat.localScale = new Vector3(1f, 1f + Mathf.Sin(t * 1.6f) * 0.05f, 1f);
            catTail.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.2f) * 20f);
            GameManager gm = GameManager.Instance;
            bool near = gm != null && gm.Rowaida != null && Mathf.Abs(gm.Rowaida.transform.position.x - catPos.x) < 3f;
            catHead.localRotation = Quaternion.Slerp(catHead.localRotation, Quaternion.Euler(0f, near ? -30f : 0f, near ? 15f : -10f), dt * 3f);
            if (near && t > nextMeow) { nextMeow = t + 6f; LevelSfx.Play("meow", 0.5f, 1.1f); }
        }
    }
}
