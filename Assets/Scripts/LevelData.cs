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

// وحش شخابيط: نوعه، مكانه، والمنطقة اللي يتمشى فيها
public struct MonsterSpawn
{
    public ScribbleMonster.Kind kind;
    public float x, patrolMin, patrolMax;

    public MonsterSpawn(ScribbleMonster.Kind kind, float x, float patrolMin, float patrolMax)
    {
        this.kind = kind;
        this.x = x;
        this.patrolMin = patrolMin;
        this.patrolMax = patrolMax;
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
    public float windSpeed;        // سرعة الريح (موجب = لليمين، سالب = لليسار، 0 = ما فيه ريح)
    public float windSwitchPeriod; // كل كم ثانية تعكس الريح اتجاهها (0 = ثابتة)
    public bool Windy => windSpeed != 0f;
    public List<MonsterSpawn> monsters = new List<MonsterSpawn>();   // وحوش الشخابيط
    public bool Scribbly => monsters.Count > 0;
    public bool monsterHint;       // أول وحش فوقه تلميح شخبطة
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
        WindLevel1(),
        WindLevel2(),
        ScribbleLevel1(),
        ScribbleLevel2(),
    };

    // صفحات الشخابيط: وحش واحد، وبعدين وحشين قبل الباب
    static LevelData ScribbleLevel1()
    {
        return new LevelData
        {
            title = "صفحات الشخابيط",
            subtitle = "رسوماتي القديمة صحت! شخبطي عليها عشان تنمسح",
            monsterHint = true,
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(32f, 0f, 0f),
            ink = 30f,
            blocks =
            {
                Page(-7f, 6f),        // حفرة 3
                Page(9f, 20f),        // حفرة 3
                Page(23f, 35f),
            },
            monsters =
            {
                new MonsterSpawn(ScribbleMonster.Kind.PurpleBlob, 15f, 12f, 19f),
                new MonsterSpawn(ScribbleMonster.Kind.FuzzyBall, 26f, 24.5f, 29f),
                new MonsterSpawn(ScribbleMonster.Kind.GreenEye, 29f, 27f, 30.5f),
            },
        };
    }

    // شخابيط أكثر: وحوش جنب الحفر تاكل الجسور
    static LevelData ScribbleLevel2()
    {
        return new LevelData
        {
            title = "شخابيط أكثر",
            subtitle = "الوحوش تاكل الجسور... امسحيها قبل ما ترسمين!",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(36f, 0f, 0f),
            ink = 34f,
            blocks =
            {
                Page(-7f, 4f),             // حفرة 3
                Page(7f, 15f),             // حفرة 3.5
                Page(18.5f, 26f, 0.5f),    // حفرة 3
                Page(29f, 39f),
            },
            monsters =
            {
                new MonsterSpawn(ScribbleMonster.Kind.FuzzyBall, 11f, 9f, 14f),
                new MonsterSpawn(ScribbleMonster.Kind.PurpleBlob, 21f, 19.5f, 25f),
                new MonsterSpawn(ScribbleMonster.Kind.GreenEye, 24f, 20f, 25.5f),
                new MonsterSpawn(ScribbleMonster.Kind.GreenEye, 33f, 31f, 37.5f),
            },
        };
    }

    // صفحات الريح: ريح ثابتة جاية من قدام تدفع الرسمات لورا
    static LevelData WindLevel1()
    {
        return new LevelData
        {
            title = "صفحات الريح",
            subtitle = "الريح تدفع الرسمات لورا... ارسمي الجسر أطول من قدام!",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(35f, 0f, 0f),
            ink = 24f,
            windSpeed = -1f,
            blocks =
            {
                Page(-7f, 5f),        // حفرة 3
                Page(8f, 15f),        // حفرة 3.5
                Page(18.5f, 25f),     // حفرة 3
                Page(28f, 38f),
            },
        };
    }

    // العاصفة: الريح تغيّر اتجاهها كل كم ثانية
    static LevelData WindLevel2()
    {
        return new LevelData
        {
            title = "العاصفة",
            subtitle = "الريح تغيّر اتجاهها... انتبهي للسهم فوق!",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(33f, 0f, 0f),
            ink = 26f,
            windSpeed = 1.4f,
            windSwitchPeriod = 7f,
            blocks =
            {
                Page(-7f, 4f),             // حفرة 3.5
                Page(7.5f, 13f),           // حفرة 3 وبعدها صفحة أعلى شوي
                Page(16f, 22f, 0.5f),      // حفرة 3.5
                Page(25.5f, 36f),
            },
        };
    }

    // صندوق صفحة من أول x لآخر x، وسطحها العلوي على ارتفاع top
    static Block Page(float fromX, float toX, float top = 0f)
    {
        const float thickness = 1.2f;
        return new Block(new Vector3((fromX + toX) * 0.5f, top - thickness * 0.5f, 0f), new Vector3(toX - fromX, thickness, 4f));
    }

    // صفحات المطر 1: أربع صفحات بينها ثلاث حفر، والمطر يمسح الرسم بعد ثانيتين
    static LevelData RainLevel1()
    {
        return new LevelData
        {
            title = "صفحات المطر",
            subtitle = "المطر يمسح الرسم بعد شوي... لازم تكونين سريعة!",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(35f, 0f, 0f),
            ink = 16f,
            rainEraseTime = 2f,
            blocks =
            {
                Page(-7f, 5f),       // حفرة 3.5
                Page(8.5f, 15f),     // حفرة 3
                Page(18f, 24.5f),    // حفرة 3.5
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
            rainEraseTime = 2f,
            blocks =
            {
                Page(-7f, 4f),             // حفرة 3 وبعدها صفحة أوطى
                Page(7f, 14f, -1f),        // حفرة 2.5 وبعدها صفحة أعلى بـ 1.4
                Page(16.5f, 24f, 0.4f),    // حفرة 3
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
