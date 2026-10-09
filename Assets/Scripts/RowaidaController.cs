using UnityEngine;

// رويدا تمشي لقدام لحالها، والجاذبية تتحكم بالطيحة
[RequireComponent(typeof(Rigidbody))]
public class RowaidaController : MonoBehaviour
{
    public float walkSpeed = 2.8f;
    public float startDelay = 1.2f;   // ثواني انتظار قبل ما تبدأ تمشي

    Rigidbody rb;
    float timer;
    bool stopped;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (stopped) return;

        timer += Time.fixedDeltaTime;
        if (timer < startDelay || IsKnocked) return;

        // نثبت السرعة الأفقية ونخلي السرعة العمودية للجاذبية
        Vector3 v = rb.linearVelocity;
        v.x = WaitOnBoat() || WaitAtFold() ? 0f : walkSpeed * (boosted ? 2.2f : 1f);
        rb.linearVelocity = v;
    }

    public void Stop()
    {
        stopped = true;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    // ---------- ألوان الحبر ----------

    bool boosted;      // الأزرق يسرّعها لين تلمس أرض ثانية
    float nextBounce;

    // الأحمر: تنط لفوق (عكس الجاذبية). يرجع true لو نطت
    public bool Bounce(float speed)
    {
        if (Time.time < nextBounce) return false;
        nextBounce = Time.time + 0.35f;
        Vector3 v = rb.linearVelocity;
        v.y = -Mathf.Sign(Physics.gravity.y) * speed;
        rb.linearVelocity = v;
        return true;
    }

    float lastBlue;

    public void SetBoost(bool on)
    {
        boosted = on;
        if (on) lastBlue = Time.time;
    }

    // على قارب في الحبر المنسكب وقدامها جدار: توقف وتنتظر لين يرفعها القارب فوقه
    float onBoatUntil;

    bool WaitOnBoat()
    {
        if (Time.time > onBoatUntil) return false;
        var level = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : null;
        if (level == null || !level.Flood) return false;
        Vector3 from = transform.position + Vector3.down * 0.3f;
        foreach (RaycastHit hit in Physics.RaycastAll(from, Vector3.right, 4f))
            if (!hit.collider.isTrigger && hit.collider.GetComponentInParent<InkMark>() == null && hit.collider.GetComponentInParent<RowaidaController>() == null)
                return true;
        return false;
    }

    // صفحة الطيّة: توقف قبل الصفحة اللي بتنطوي (أو المطوية) إذا ما فيه جسر، عشان ما تطيح ظلم
    bool WaitAtFold()
    {
        if (FoldingFlap.All.Count == 0) return false;
        float x = transform.position.x;
        foreach (FoldingFlap f in FoldingFlap.All)
        {
            float edge = f.MinX - x;
            if (edge < 0.3f || edge > 0.8f) continue;

            // الصفحات المتلاصقة اللي تبدأ من هنا لازم كلها تظل مفتوحة لين تعديها
            float end = f.MaxX;
            var chain = new System.Collections.Generic.List<FoldingFlap> { f };
            bool grew = true;
            while (grew)
            {
                grew = false;
                foreach (FoldingFlap g in FoldingFlap.All)
                    if (!chain.Contains(g) && Mathf.Abs(g.MinX - end) < 0.1f) { chain.Add(g); end = g.MaxX; grew = true; }
            }
            float cross = (end - f.MinX + 0.8f) / walkSpeed;
            bool safe = true;
            foreach (FoldingFlap g in chain) if (!g.StaysFlatFor(cross)) safe = false;
            if (safe || BridgeCovers(f.MinX, end, f.Top)) continue;
            return true;
        }
        return false;
    }

    static bool BridgeCovers(float x0, float x1, float top)
    {
        foreach (InkMark m in FindObjectsByType<InkMark>(FindObjectsSortMode.None))
        {
            if (m.closed) continue;
            var r = m.GetComponent<MeshRenderer>();
            if (r == null) continue;
            Bounds b = r.bounds;
            if (b.min.x <= x0 + 0.3f && b.max.x >= x1 - 0.3f && Mathf.Abs(b.center.y - top) < 1.5f) return true;
        }
        return false;
    }

    // نزلت من طيحة: غبار تحت رجولها (وهزة خفيفة لو الطيحة قوية)
    void OnCollisionEnter(Collision c)
    {
        float fall = Mathf.Abs(c.relativeVelocity.y);
        if (fall < 4.5f || c.contactCount == 0) return;
        ContactPoint contact = c.GetContact(0);
        if (Vector3.Dot(contact.normal, -Physics.gravity.normalized) < 0.6f) return;
        Fx.Dust(contact.point, Mathf.InverseLerp(4.5f, 11f, fall));
        if (fall > 8f)
        {
            var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            if (cam != null) cam.Shake(0.08f, 0.2f);
        }
    }

    void OnCollisionStay(Collision c)
    {
        var mark = c.collider.GetComponentInParent<InkMark>();
        if (mark != null && mark.closed) onBoatUntil = Time.time + 0.15f;
        var fx = c.collider.GetComponentInParent<InkColorEffect>();
        if (fx != null) fx.OnRowaida(this);
        // لمست أرض عادية (مو بنفس لحظة لمس الأزرق): يخلص التسريع
        else if (boosted && Time.time - lastBlue > 0.15f) boosted = false;
    }

    // وحش صدمها: تنط لورا شوي وتوقف عن المشي لحظة
    float knockedUntil;

    public void Knockback(float dirX)
    {
        knockedUntil = Time.time + 0.6f;
        rb.linearVelocity = new Vector3(dirX * 3.2f, -Mathf.Sign(Physics.gravity.y) * 3.5f, 0f);
    }

    public bool IsKnocked => Time.time < knockedUntil;

    // ترجع لمكان معين (بعد ما تطيح وباقي لها قلوب) وتنتظر شوي قبل ما تمشي
    public void Respawn(Vector3 position)
    {
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.position = position;
        transform.position = position;
        timer = 0f;
        boosted = false;
        stopped = false;
    }
}
