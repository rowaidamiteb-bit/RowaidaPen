using UnityEngine;

// الريح تدفع الرسمة: الخط المفتوح يتزحلق مع الريح، والشكل المغلق تدفه وتدحرجه
public class InkDrift : MonoBehaviour
{
    Rigidbody rb;
    bool solid;   // شكل مغلق (يطيح ويتدحرج)

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        solid = rb != null && !rb.isKinematic;
        if (rb == null)
        {
            // الخط المفتوح يصير جسم متحرك (kinematic) عشان رويدا تقدر تمشي عليه وهو يتحرك
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    // لما رويدا واقفة على الخط، وزنها يثبته والريح ما تقدر تحركه
    float anchoredUntil;

    void OnCollisionStay(Collision c)
    {
        if (!solid && c.collider.GetComponentInParent<RowaidaController>() != null)
            anchoredUntil = Time.time + 0.15f;
    }

    void FixedUpdate()
    {
        float wind = Weather.Wind;
        if (solid) rb.AddForce(Vector3.right * wind * 5f, ForceMode.Acceleration);
        else if (rb.isKinematic && Time.time > anchoredUntil) rb.MovePosition(rb.position + Vector3.right * wind * Time.fixedDeltaTime);

        // إذا طارت بعيد مرة نحذفها
        Camera cam = Camera.main;
        if (cam != null && (Mathf.Abs(transform.position.x - cam.transform.position.x) > 45f || transform.position.y < -25f))
            Destroy(gameObject);
    }
}
