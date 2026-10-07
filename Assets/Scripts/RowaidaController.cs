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
        if (timer < startDelay) return;

        // نثبت السرعة الأفقية ونخلي السرعة العمودية للجاذبية
        Vector3 v = rb.linearVelocity;
        v.x = walkSpeed;
        rb.linearVelocity = v;
    }

    public void Stop()
    {
        stopped = true;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    // ترجع لمكان معين (بعد ما تطيح وباقي لها قلوب) وتنتظر شوي قبل ما تمشي
    public void Respawn(Vector3 position)
    {
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.position = position;
        transform.position = position;
        timer = 0f;
        stopped = false;
    }
}
