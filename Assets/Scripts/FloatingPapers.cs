using UnityEngine;

// أوراق صغيرة طايرة ببطء في الخلفية، تعطي إحساس الحلم
public class FloatingPapers : MonoBehaviour
{
    public int count = 45;

    Transform[] papers;
    Vector3[] basePos;
    Vector3[] spin;
    float[] offset;

    public void Build(GameManager gm)
    {
        Material paper = gm.Mat("Floating Paper", new Color(0.9f, 0.89f, 0.92f), 0.1f);
        papers = new Transform[count];
        basePos = new Vector3[count];
        spin = new Vector3[count];
        offset = new float[count];

        var rng = new System.Random(11);
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(R(-15f, 70f), R(-7f, 9f), R(4f, 28f));
            float s = R(0.25f, 0.6f);
            var go = LevelBuilder.Visual("Paper", PrimitiveType.Cube, transform, pos, new Vector3(s, s * 1.3f, 0.01f), paper, false);
            go.transform.rotation = Random.rotation;
            papers[i] = go.transform;
            basePos[i] = pos;
            spin[i] = new Vector3(R(-20f, 20f), R(-30f, 30f), R(-20f, 20f));
            offset[i] = R(0f, 100f);
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
            float o = offset[i];
            // الريح تطيّر الأوراق، ولما تبعد عن الكاميرا ترجع من الجهة الثانية
            basePos[i].x += Weather.Wind * 2.5f * Time.deltaTime;
            if (basePos[i].x < cx - 35f) basePos[i].x += 85f;
            else if (basePos[i].x > cx + 50f) basePos[i].x -= 85f;
            papers[i].position = basePos[i] + new Vector3(Mathf.Sin(t * 0.2f + o) * 0.6f, Mathf.Sin(t * 0.35f + o * 1.7f) * 0.4f, 0f);
            papers[i].Rotate(spin[i] * Time.deltaTime * (1f + Mathf.Abs(Weather.Wind) * 2f), Space.Self);
        }
    }
}
