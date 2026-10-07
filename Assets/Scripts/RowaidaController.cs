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
        v.x = WaitOnBoat() ? 0f : walkSpeed * (boosted ? 2.2f : 1f);
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
        rb.linearVelocity = new Vector3(dirX * 3.2f, 3.5f, 0f);
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
