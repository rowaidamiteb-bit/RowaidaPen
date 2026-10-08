using UnityEngine;

// أشياء صغيرة طايرة ببطء في الخلفية تعطي إحساس الحلم.
// شكلها يتغير حسب المرحلة: أوراق، أوراق شجر، أقلام تلوين، بتلات، لمعات، طيور ورق، فتات مساحة، أو قطرات حبر
public class FloatingPapers : MonoBehaviour
{
    public int count = 45;

    GameManager gm;
    Transform[] papers;
    Vector3[] basePos;
    Vector3[] spin;
    float[] offset;
    string currentTheme;

    public void Build(GameManager manager)
    {
        gm = manager;
        ApplyTheme(LevelTheme.Classic());
    }

    public void ApplyTheme(LevelTheme theme)
    {
        if (theme.name == currentTheme) return;
        currentTheme = theme.name;
        foreach (Transform child in transform) Destroy(child.gameObject);

        papers = new Transform[count];
        basePos = new Vector3[count];
        spin = new Vector3[count];
        offset = new float[count];
        var rng = new System.Random(11);
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        Camera cam = Camera.main;
        float cx = cam != null ? cam.transform.position.x : 0f;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(cx + R(-30f, 50f), R(-7f, 9f), R(4f, 28f));
            Color color = theme.floatColors[rng.Next(theme.floatColors.Length)];
            Material mat = gm.Mat("Float " + ColorUtility.ToHtmlStringRGB(color), color, 0.2f);
            GameObject go = Make(theme.floating, pos, R(0.6f, 1.2f), color, mat);
            go.transform.rotation = Random.rotation;
            papers[i] = go.transform;
            basePos[i] = pos;
            spin[i] = new Vector3(R(-20f, 20f), R(-30f, 30f), R(-20f, 20f));
            offset[i] = R(0f, 100f);
        }
    }

    GameObject Make(FloatKind kind, Vector3 pos, float s, Color color, Material mat)
    {
        switch (kind)
        {
            case FloatKind.Leaves: return LevelBuilder.Visual("Leaf", PrimitiveType.Sphere, transform, pos, new Vector3(0.35f, 0.03f, 0.2f) * s, mat, false);
            case FloatKind.Crayons: return LevelBuilder.Visual("Crayon Bit", PrimitiveType.Cylinder, transform, pos, new Vector3(0.08f, 0.2f, 0.08f) * s, mat, false);
            case FloatKind.Petals: return LevelBuilder.Visual("Petal", PrimitiveType.Sphere, transform, pos, new Vector3(0.2f, 0.02f, 0.13f) * s, mat, false);
            case FloatKind.Sparkles:
                Material glow = gm.Mat("Glow " + ColorUtility.ToHtmlStringRGB(color), color * 1.6f, 0.9f);
                return LevelBuilder.Visual("Sparkle", PrimitiveType.Sphere, transform, pos, Vector3.one * 0.09f * s, glow, false);
            case FloatKind.Cranes: return Props.OrigamiCrane(transform, pos, 0f, color, gm, 0.6f * s);
            case FloatKind.EraserBits: return LevelBuilder.Visual("Eraser Bit", PrimitiveType.Cube, transform, pos, new Vector3(0.14f, 0.09f, 0.11f) * s, mat, false);
            case FloatKind.InkDrops: return LevelBuilder.Visual("Ink Drop", PrimitiveType.Sphere, transform, pos, new Vector3(0.12f, 0.18f, 0.12f) * s, mat, false);
            default: return LevelBuilder.Visual("Paper", PrimitiveType.Cube, transform, pos, new Vector3(0.4f, 0.52f, 0.01f) * s, mat, false);
        }
    }

    void Update()
    {
        if (papers == null) return;
        float t = Time.time;
        Camera cam = Camera.main;
        float cx = cam != null ? cam.transform.position.x : 0f;
        for (int i = 0; i < papers.Length; i++)
        {
            if (papers[i] == null) continue;
            float o = offset[i];
            // الريح تطيّرها، ولما تبعد عن الكاميرا ترجع من الجهة الثانية
            basePos[i].x += Weather.Wind * 2.5f * Time.deltaTime;
            if (basePos[i].x < cx - 35f) basePos[i].x += 85f;
            else if (basePos[i].x > cx + 50f) basePos[i].x -= 85f;
            papers[i].position = basePos[i] + new Vector3(Mathf.Sin(t * 0.2f + o) * 0.6f, Mathf.Sin(t * 0.35f + o * 1.7f) * 0.4f, 0f);
            papers[i].Rotate(spin[i] * Time.deltaTime * (1f + Mathf.Abs(Weather.Wind) * 2f), Space.Self);
        }
    }
}
