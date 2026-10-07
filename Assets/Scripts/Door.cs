using UnityEngine;

// باب نهاية المرحلة: لما تدخله رويدا ننتقل للمرحلة اللي بعدها
public class Door : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<RowaidaController>() != null && GameManager.Instance != null)
            GameManager.Instance.OnRowaidaReachedDoor();
    }
}
