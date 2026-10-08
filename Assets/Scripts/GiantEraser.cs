using System.Collections.Generic;
using UnityEngine;

// ممحاة صغيرة تمشي على الصفحة وتفرك:
// - اللي قدام رويدا تمسح أجزاء من الأرض (تصير حفر)
// - اللي ورا (chaser) تلحق رويدا، تمسح الأرض والرسم اللي وراها، وإذا لمستها تخسر قلب
public class GiantEraser : MonoBehaviour
{
    class Hole
    {
        public float from, to;
        public GameObject segment;
        public bool erased;
        public Vector3 pos, scale;
    }

    const float Size = 0.36f;              // أصغر من رويدا
    const float ChaseSpeed = 3f, CloseSpeed = 2.7f, StartGap = 5f;

    readonly List<Hole> holes = new List<Hole>();
    readonly List<Hole> floor = new List<Hole>();
    float speed, endX, delay = 1f;
    bool chaser;
    Transform body;
    float nextCrumb, nextRub;
    bool finished;

    public static GiantEraser Spawn(Transform parent, Vector3 start, float endX, float speed, GameManager gm, bool chaser)
    {
        var go = new GameObject(chaser ? "Chasing Eraser" : "Eraser Ahead");
        go.transform.SetParent(parent, false);
        go.transform.position = start;
        var e = go.AddComponent<GiantEraser>();
        e.speed = speed;
        e.endX = endX;
        e.chaser = chaser;
        if (chaser) e.delay = 0.6f;

        // شكل المساحة: جسم أبيض، طرف وردي، وغلاف (أزرق للي قدام، أحمر للي تلحق)
        e.body = new GameObject("Body").transform;
        e.body.SetParent(go.transform, false);
        var model = new GameObject("Model").transform;
        model.SetParent(e.body, false);
        model.localScale = Vector3.one * Size;
        Material white = gm.Mat("Eraser White", new Color(0.97f, 0.96f, 0.95f), 0.2f);
        Material pink = gm.Mat("Eraser Pink", new Color(0.95f, 0.62f, 0.66f), 0.15f);
        Material sleeve = chaser ? gm.Mat("Sleeve Red", new Color(0.85f, 0.3f, 0.3f), 0.3f) : gm.Mat("Sleeve Blue", new Color(0.35f, 0.45f, 0.75f), 0.3f);
        LevelBuilder.Visual("Eraser Body", PrimitiveType.Cube, model, new Vector3(-0.4f, 0.8f, 0f), new Vector3(2.4f, 1.6f, 2.2f), white);
        LevelBuilder.Visual("Eraser Tip", PrimitiveType.Cube, model, new Vector3(1.25f, 0.75f, 0f), new Vector3(0.9f, 1.5f, 2.1f), pink);
        LevelBuilder.Visual("Sleeve", PrimitiveType.Cube, model, new Vector3(-0.2f, 0.82f, 0f), new Vector3(1.5f, 1.66f, 2.25f), sleeve);
        if (chaser)
        {
            // عيون غاضبة عشان تبان إنها تلحقها
            Material eye = gm.Mat("Eraser Eye", new Color(0.1f, 0.1f, 0.12f), 0.4f);
            for (int s = -1; s <= 1; s += 2)
            {
                var brow = LevelBuilder.Visual("Brow", PrimitiveType.Cube, model, new Vector3(1.72f, 1.2f, s * 0.4f), new Vector3(0.05f, 0.08f, 0.4f), eye);
                brow.transform.localRotation = Quaternion.Euler(s * 25f, 0f, 0f);
                LevelBuilder.Visual("Eye", PrimitiveType.Sphere, model, new Vector3(1.72f, 0.95f, s * 0.4f), new Vector3(0.06f, 0.22f, 0.2f), eye);
            }
        }
        var rng = new System.Random(4);
        LevelBuilder.OutlineBox(model, new Vector3(3.3f, 1.6f, 2.2f), gm, rng);

        if (chaser) gm.Respawned += e.OnRespawned;
        return e;
    }

    void OnDestroy()
    {
        if (chaser && GameManager.Instance != null) GameManager.Instance.Respawned -= OnRespawned;
    }

    public void AddHole(float from, float to, GameObject segment)
    {
        holes.Add(new Hole { from = from, to = to, segment = segment });
    }

    // قطعة أرض تمسحها الممحاة اللي تلحق (وترجع لما رويدا ترجع)
    public void AddFloor(float from, float to, GameObject segment)
    {
        floor.Add(new Hole { from = from, to = to, segment = segment, pos = segment.transform.position, scale = segment.transform.localScale });
    }

    // رويدا خسرت قلب: الممحاة ترجع وراها والأرض ترجع مثل ما كانت
    void OnRespawned(Vector3 pos)
    {
        transform.position = new Vector3(pos.x - StartGap, 0f, 0f);
        delay = 1f;
        finished = false;
        gameObject.SetActive(true);
        foreach (Hole h in floor)
        {
            if (!h.erased || h.segment == null) continue;
            h.erased = false;
            var es = h.segment.GetComponent<ErasedSegment>();
            if (es != null) DestroyImmediate(es);
            h.segment.transform.position = h.pos;
            h.segment.transform.localScale = h.scale;
            h.segment.SetActive(true);
            foreach (Collider c in h.segment.GetComponentsInChildren<Collider>()) c.enabled = true;
        }
    }

    void Update()
    {
        if (finished) return;
        GameManager gm = GameManager.Instance;
        if (chaser && !gm.IsPlaying) return;
        if (delay > 0f) { delay -= Time.deltaTime; return; }

        Vector3 p = transform.position;
        float v = speed;
        RowaidaController rowaida = gm.Rowaida;
        if (chaser && rowaida != null)
        {
            // تلحقها بسرعة، ولما تقرب تبطّي شوي: ما تمسكها إلا إذا وقفت أو علقت
            float gap = rowaida.transform.position.x - p.x;
            v = gap < 3.2f ? CloseSpeed : ChaseSpeed;
        }
        p.x += v * Time.deltaTime;
        transform.position = p;

        // تفرك رايح جاي
        float rub = Mathf.Sin(Time.time * 14f);
        body.localPosition = new Vector3(rub * 0.12f, Mathf.Abs(rub) * 0.03f, 0f);
        body.localRotation = Quaternion.Euler(0f, 0f, rub * 4f);

        if (Time.time >= nextCrumb)
        {
            nextCrumb = Time.time + 0.08f;
            Crumb(p + new Vector3(-0.5f, 0.05f, Random.Range(-0.4f, 0.4f)));
        }
        if (Time.time >= nextRub)
        {
            nextRub = Time.time + 0.3f;
            Camera cam = Camera.main;
            if (cam != null && Mathf.Abs(cam.transform.position.x - p.x) < 16f)
                gm.Sound.PlayChomp();
        }

        if (chaser) Chase(p, rowaida, gm);
        else
        {
            // تمسح الأرض اللي تعدي عليها
            foreach (Hole h in holes)
            {
                if (h.erased || p.x < h.from + 0.4f) continue;
                h.erased = true;
                if (h.segment != null) h.segment.AddComponent<ErasedSegment>();
            }
        }

        // وصلت لآخر الصفحة: تختفي
        if (p.x >= endX)
        {
            finished = true;
            gm.Sound.PlayPoof();
            for (int i = 0; i < 10; i++) Crumb(p + Random.insideUnitSphere * 0.6f + Vector3.up * 0.4f);
            if (chaser) gameObject.SetActive(false);
            else Destroy(gameObject);
        }
    }

    void Chase(Vector3 p, RowaidaController rowaida, GameManager gm)
    {
        // تمسح الأرض اللي عدّتها
        foreach (Hole h in floor)
        {
            if (h.erased || h.segment == null || p.x < h.to + 0.2f) continue;
            h.erased = true;
            h.segment.AddComponent<ErasedSegment>().keep = true;
        }

        // وتمسح الرسم اللي وراها
        if (gm.DrawingsRoot != null)
            foreach (Transform stroke in gm.DrawingsRoot)
            {
                var r = stroke.GetComponentInChildren<Renderer>();
                if (r == null || r.bounds.max.x > p.x - 0.3f) continue;
                for (int i = 0; i < 5; i++) Crumb(r.bounds.center + Random.insideUnitSphere * 0.4f);
                Destroy(stroke.gameObject);
            }

        // لمست رويدا
        if (rowaida != null)
        {
            Vector3 rp = rowaida.transform.position;
            if (rp.x - p.x < 0.75f && Mathf.Abs(rp.y - p.y) < 1.4f) gm.KillRowaida();
        }
    }

    static void Crumb(Vector3 pos)
    {
        bool pinkCrumb = Random.value < 0.4f;
        var mat = pinkCrumb ? GameManager.Instance.Mat("Eraser Pink", new Color(0.95f, 0.62f, 0.66f), 0.15f)
                            : GameManager.Instance.Mat("Eraser White", new Color(0.97f, 0.96f, 0.95f), 0.2f);
        var c = LevelBuilder.Visual("Eraser Crumb", PrimitiveType.Sphere, null, pos, new Vector3(0.08f, 0.05f, 0.06f) * Random.Range(0.7f, 1.4f), mat, false);
        c.AddComponent<PoofPuff>().shrinkOnly = true;
    }
}

// جزء من الأرض انمسح: يختفي التصادم فورًا، والشكل يصغر وينزل لين يختفي.
// keep = نخفيه بس (عشان نرجعه لما رويدا ترجع)
public class ErasedSegment : MonoBehaviour
{
    public bool keep;
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
        if (t < 1f) return;
        if (!keep) { Destroy(gameObject); return; }
        transform.localScale = startScale;
        transform.position = startPos;
        gameObject.SetActive(false);
        Destroy(this);
    }
}
