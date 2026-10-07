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
    public string subtitle;        // جملة تطلع تحت عنوان المرحلة
    public float rainEraseTime;    // المطر يمسح الرسمة بعد هالكم ثانية (0 = ما فيه مطر)
    public bool Rain => rainEraseTime > 0f;
    public List<Block> blocks = new List<Block>();
}

// كل مراحل اللعبة، نضيف المراحل الجديدة هنا
public static class Levels
{
    public static readonly List<LevelData> All = new List<LevelData>
    {
        Level1(),
        RainLevel1(),
        RainLevel2(),
    };

    // صندوق صفحة من أول x لآخر x، وسطحها العلوي على ارتفاع top
    static Block Page(float fromX, float toX, float top = 0f)
    {
        const float thickness = 1.2f;
        return new Block(new Vector3((fromX + toX) * 0.5f, top - thickness * 0.5f, 0f), new Vector3(toX - fromX, thickness, 4f));
    }

    // صفحات المطر 1: أربع صفحات بينها ثلاث حفر، والمطر يمسح الرسم بعد 2.5 ثانية
    static LevelData RainLevel1()
    {
        return new LevelData
        {
            title = "صفحات المطر",
            subtitle = "المطر يمسح الرسم بعد شوي... لازم تكونين سريعة!",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(35f, 0f, 0f),
            ink = 16f,
            rainEraseTime = 2.5f,
            blocks =
            {
                Page(-7f, 5f),       // حفرة 3.5
                Page(8.5f, 15f),     // حفرة 3
                Page(18f, 24f),      // حفرة 4
                Page(28f, 38f),
            },
        };
    }

    // تحت المطر: صفحات على ارتفاعات مختلفة، وفيها صفحة أعلى تحتاج طريق مايل
    static LevelData RainLevel2()
    {
        return new LevelData
        {
            title = "تحت المطر",
            subtitle = "الصفحة اللي قدام أعلى... ارسمي لها طريق مايل يطلّعها فوق!",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(34f, 0f, 0f),
            ink = 18f,
            rainEraseTime = 2.2f,
            blocks =
            {
                Page(-7f, 4f),             // حفرة 3 وبعدها صفحة أوطى
                Page(7f, 14f, -1f),        // حفرة 3 وبعدها صفحة أعلى بـ 1.6
                Page(17f, 24f, 0.6f),      // حفرة 3
                Page(27f, 37f),
            },
        };
    }

    // المرحلة الأولى: ثلاث صفحات بينها حفرتين، والباب في آخر صفحة
    static LevelData Level1()
    {
        return new LevelData
        {
            title = "الصفحات الأولى",
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
