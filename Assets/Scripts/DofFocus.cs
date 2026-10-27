using UnityEngine;
using UnityEngine.Rendering.Universal;

// غبشة الخلفية تبدأ بعد رويدا بشوي: لما الكاميرا ترجع لورا (عشان تبين عقبة كبيرة) الغبشة تبعد معها
public class DofFocus : MonoBehaviour
{
    public DepthOfField dof;

    void LateUpdate()
    {
        if (dof == null) return;
        GameManager gm = GameManager.Instance;
        float d = 9f;
        if (gm != null && gm.Rowaida != null) d = Vector3.Distance(transform.position, gm.Rowaida.transform.position);
        dof.gaussianStart.value = d + 6f;
        dof.gaussianEnd.value = d + 26f;
    }
}
