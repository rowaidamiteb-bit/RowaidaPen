using System.Collections.Generic;
using UnityEngine;

// ممحاة عملاقة تمشي على الصفحة قدام رويدا وتفرك، وتمسح أجزاء من الأرض (تصير حفر)
public class GiantEraser : MonoBehaviour
{
    class Hole
    {
        public float from, to;
        public GameObject segment;
        public bool erased;
    }

    readonly List<Hole> holes = new List<Hole>();
    float speed, endX, delay = 1f;
    Transform body;
    float nextCrumb, nextRub;
    bool finished;

    public static GiantEraser Spawn(Transform parent, Vector3 start, float endX, float speed, GameManager gm)
    {
        var go = new GameObject("Giant Eraser");
        go.transform.SetParent(parent, false);
        go.transform.position = start;
        var e = go.AddComponent<GiantEraser>();
        e.speed = speed;
        e.endX = endX;

        // شكل المساحة: جسم أبيض، طرف وردي، وغلاف أزرق (أكبر بكثير من رويدا)
        e.body = new GameObject("Body").transform;
        e.body.SetParent(go.transform, false);
        Material white = gm.Mat("Eraser White", new Color(0.97f, 0.96f, 0.95f), 0.2f);
        Material pink = gm.Mat("Eraser Pink", new Color(0.95f, 0.62f, 0.66f), 0.15f);
        Material sleeve = gm.Mat("Sleeve Blue", new Color(0.35f, 0.45f, 0.75f), 0.3f);
        LevelBuilder.Visual("Eraser Body", PrimitiveType.Cube, e.body, new Vector3(-0.4f, 0.8f, 0f), new Vector3(2.4f, 1.6f, 2.2f), white);
        LevelBuilder.Visual("Eraser Tip", PrimitiveType.Cube, e.body, new Vector3(1.25f, 0.75f, 0f), new Vector3(0.9f, 1.5f, 2.1f), pink);
        LevelBuilder.Visual("Sleeve", PrimitiveType.Cube, e.body, new Vector3(-0.2f, 0.82f, 0f), new Vector3(1.5f, 1.66f, 2.25f), sleeve);
        var rng = new System.Random(4);
        LevelBuilder.OutlineBox(e.body, new Vector3(3.3f, 1.6f, 2.2f), gm, rng);
        return e;
    }

    public void AddHole(float from, float to, GameObject segment)
    {
        holes.Add(new Hole { from = from, to = to, segment = segment });
    }

    void Update()
    {
        if (finished) return;
        if (delay > 0f) { delay -= Time.deltaTime; return; }

        Vector3 p = transform.position;
        p.x += speed * Time.deltaTime;
        transform.position = p;

        // تفرك رايح جاي
        float rub = Mathf.Sin(Time.time * 14f);
        body.localPosition = new Vector3(rub * 0.25f, Mathf.Abs(rub) * 0.05f, 0f);
        body.localRotation = Quaternion.Euler(0f, 0f, rub * 4f);

        if (Time.time >= nextCrumb)
        {
            nextCrumb = Time.time + 0.06f;
            Crumb(p + new Vector3(-1.4f, 0.1f, Random.Range(-1f, 1f)));
        }
        if (Time.time >= nextRub)
        {
            nextRub = Time.time + 0.25f;
            Camera cam = Camera.main;
            if (cam != null && Mathf.Abs(cam.transform.position.x - p.x) < 16f)
                GameManager.Instance.Sound.PlayChomp();
        }

        // تمسح الأرض اللي تعدي عليها
        foreach (Hole h in holes)
        {
            if (h.erased || p.x < h.from + 0.4f) continue;
            h.erased = true;
            if (h.segment != null) h.segment.AddComponent<ErasedSegment>();
        }

        // وصلت لآخر الصفحة: تختفي
        if (p.x >= endX)
        {
            finished = true;
            GameManager.Instance.Sound.PlayPoof();
            for (int i = 0; i < 14; i++) Crumb(p + Random.insideUnitSphere * 1.2f + Vector3.up);
            Destroy(gameObject);
        }
    }

    static void Crumb(Vector3 pos)
    {
        bool pinkCrumb = Random.value < 0.4f;
        var mat = pinkCrumb ? GameManager.Instance.Mat("Eraser Pink", new Color(0.95f, 0.62f, 0.66f), 0.15f)
                            : GameManager.Instance.Mat("Eraser White", new Color(0.97f, 0.96f, 0.95f), 0.2f);
        var c = LevelBuilder.Visual("Eraser Crumb", PrimitiveType.Sphere, null, pos, new Vector3(0.12f, 0.07f, 0.08f) * Random.Range(0.7f, 1.4f), mat, false);
        c.AddComponent<PoofPuff>().shrinkOnly = true;
    }
}

// جزء من الأرض انمسح: يختفي التصادم فورًا، والشكل يصغر وينزل لين يختفي
public class ErasedSegment : MonoBehaviour
{
    float t;
    Vector3 startScale, startPos;

    void Start()
    {
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
        startScale = transform.localScale;
        startPos = transform.position;
        GameManager.Instance.Sound.PlayPoof();
    }

    void Update()
    {
        t += Time.deltaTime / 0.6f;
        transform.localScale = new Vector3(startScale.x, startScale.y * Mathf.Max(0.01f, 1f - t), startScale.z);
        transform.position = startPos + Vector3.down * t * 0.8f;
        if (t >= 1f) Destroy(gameObject);
    }
}
