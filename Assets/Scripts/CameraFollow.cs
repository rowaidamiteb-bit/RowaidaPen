using UnityEngine;

// كاميرا جانبية تتابع رويدا، بزاوية خفيفة عشان يبان العمق الثلاثي الأبعاد
public class CameraFollow : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 3.5f, -14f);
    public Vector3 lookOffset = new Vector3(2.5f, 0.5f, 0f);
    public float smoothTime = 0.35f;
    public float minY = -3f;   // ما تنزل الكاميرا أكثر من كذا لما تطيح رويدا

    Transform target;
    Vector3 focus;
    Vector3 velocity;

    public void SetTarget(Transform newTarget, bool snap)
    {
        target = newTarget;
        if (target == null) return;
        if (snap)
        {
            focus = Focus();
            velocity = Vector3.zero;
            Apply();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        focus = Vector3.SmoothDamp(focus, Focus(), ref velocity, smoothTime);
        Apply();
    }

    Vector3 Focus()
    {
        Vector3 p = target.position;
        p.y = Mathf.Max(p.y, minY);
        p.z = 0f;
        return p;
    }

    void Apply()
    {
        transform.position = focus + offset;
        transform.LookAt(focus + lookOffset);
    }
}
