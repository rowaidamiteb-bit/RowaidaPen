using System.Collections.Generic;
using UnityEngine;

// المطر يمسح الرسمة: لونها يبهت شوي شوي، وتنقط منها قطرات، وفي الآخر تخف وتختفي
public class InkErosion : MonoBehaviour
{
    // آخر جزء من العمر: الخط يخف لين يختفي (ثانية وحدة أو أقل لو الوقت قصير)
    float ShrinkTime => Mathf.Min(1f, lifetime * 0.4f);

    float lifetime;
    float age;
    List<Vector3> pts;
    float radius;
    bool closed;
    Mesh mesh;
    Material mat;
    Color startColor;
    float nextRebuild, nextDrip;
    bool collidersOff;

    readonly List<Transform> drops = new List<Transform>();
    readonly List<float> dropSpeed = new List<float>();
    static Material dropMat;

    static readonly Color Washed = new Color(0.55f, 0.6f, 0.72f);

    public void Init(Mesh strokeMesh, List<Vector3> localPoints, float tubeRadius, bool isClosed, float seconds)
    {
        mesh = strokeMesh;
        pts = new List<Vector3>(localPoints);
        radius = tubeRadius;
        closed = isClosed;
        lifetime = seconds;
        mat = GetComponent<MeshRenderer>().material;   // نسخة خاصة بهالرسمة
        startColor = mat.GetColor("_BaseColor");
        if (dropMat == null)
            dropMat = GameManager.Instance.Mat("Ink Drop", new Color(0.3f, 0.33f, 0.42f), 0.8f);
    }

    void Update()
    {
        age += Time.deltaTime;
        float k = age / lifetime;

        // اللون يبهت مع الوقت (أسرع في النص الثاني)
        mat.SetColor("_BaseColor", Color.Lerp(startColor, Washed, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 1f, k))));

        // آخر ثانية: الخط يخف ويرتجف شوي كتحذير
        float shrinkK = Mathf.InverseLerp(lifetime - ShrinkTime, lifetime, age);
        if (shrinkK > 0f && Time.time >= nextRebuild)
        {
            nextRebuild = Time.time + 0.05f;
            float wobble = 1f + Mathf.Sin(age * 40f) * 0.06f * shrinkK;
            TubeMesh.Build(mesh, pts, radius * Mathf.Lerp(1f, 0.2f, shrinkK) * wobble, closed);
        }

        // قطرات حبر تنقط من الرسمة
        if (k > 0.2f && Time.time >= nextDrip && pts.Count > 0)
        {
            nextDrip = Time.time + Mathf.Lerp(0.5f, 0.12f, k);
            Vector3 p = transform.TransformPoint(pts[Random.Range(0, pts.Count)]) + Vector3.down * radius;
            var d = LevelBuilder.Visual("Ink Drop", PrimitiveType.Sphere, null, p, new Vector3(0.05f, 0.08f, 0.05f), dropMat, false);
            drops.Add(d.transform);
            dropSpeed.Add(0.5f);
        }
        for (int i = drops.Count - 1; i >= 0; i--)
        {
            dropSpeed[i] += 12f * Time.deltaTime;
            drops[i].position += Vector3.down * dropSpeed[i] * Time.deltaTime;
            if (drops[i].position.y < -6f)
            {
                Destroy(drops[i].gameObject);
                drops.RemoveAt(i);
                dropSpeed.RemoveAt(i);
            }
        }

        // قبل ما تختفي بشوي: ما عاد تشيل رويدا
        if (!collidersOff && age >= lifetime - 0.1f)
        {
            collidersOff = true;
            foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
            var rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }

        if (age >= lifetime)
        {
            foreach (Transform d in drops) if (d != null) Destroy(d.gameObject);
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        foreach (Transform d in drops) if (d != null) Destroy(d.gameObject);
        if (mat != null) Destroy(mat);
    }
}
