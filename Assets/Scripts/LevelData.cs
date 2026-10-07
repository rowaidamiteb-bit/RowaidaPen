using System.Collections.Generic;
using UnityEngine;

// مكعب واحد من أرضية المرحلة (المركز والحجم)
[System.Serializable]
public struct Block
{
    public Vector3 center;
    public Vector3 size;

    public Block(Vector3 center, Vector3 size)
    {
        this.center = center;
        this.size = size;
    }
}

// وصف مرحلة كاملة
public class LevelData
{
    public string title;
    public Vector3 spawn;          // مكان بداية رويدا
    public Vector3 door;           // مكان الباب (أسفل الباب)
    public float ink = 12f;        // كمية الحبر (بطول الخطوط)
    public float deathY = -8f;     // إذا نزلت رويدا تحت هذا الارتفاع تعيد المرحلة
    public bool bridgeHint;        // خط منقط فوق أول حفرة يعلّم اللاعب وين يرسم
    public List<Block> blocks = new List<Block>();
}

// كل مراحل اللعبة، نضيف المراحل الجديدة هنا
public static class Levels
{
    public static readonly List<LevelData> All = new List<LevelData>
    {
        Level1(),
    };

    // المرحلة الأولى: ثلاث صفحات بينها حفرتين، والباب في آخر صفحة
    static LevelData Level1()
    {
        return new LevelData
        {
            title = "Level 1",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(33f, 0f, 0f),
            ink = 16f,
            bridgeHint = true,
            blocks =
            {
                new Block(new Vector3(0f, -0.6f, 0f), new Vector3(14f, 1.2f, 4f)),        // من -7 إلى 7، بعدها حفرة 4
                new Block(new Vector3(16f, -0.6f, 0f), new Vector3(10f, 1.2f, 4f)),       // من 11 إلى 21، بعدها حفرة 3.5
                new Block(new Vector3(30.25f, -0.6f, 0f), new Vector3(11.5f, 1.2f, 4f)),  // من 24.5 إلى 36
            },
        };
    }
}
