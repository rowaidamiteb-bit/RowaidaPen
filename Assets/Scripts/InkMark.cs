using UnityEngine;

// علامة على كل رسمة: الوحوش تعرف منها إذا هي جسر (تاكله) أو كورة (تلعب فيها)
public class InkMark : MonoBehaviour
{
    public bool closed;     // شكل مغلق = لعبة تلهي الوحوش
    public bool beingEaten; // وحش قاعد ياكلها
    public Vector3[] outline; // نقاط الشكل المسكر (محلية حول مركزه)
    public Vector3[] path;    // نقاط الخط العادي (بالعالم) — للأنابيب والأسلاك والمرايا
}
