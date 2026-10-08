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

// خط تلميح منقط من نقطة لنقطة بلون معين
public struct HintLine
{
    public Vector3 a, b;
    public Color color;

    public HintLine(Vector3 a, Vector3 b, Color color)
    {
        this.a = a;
        this.b = b;
        this.color = color;
    }
}

// صفحة تنطوي: تلف حول مفصل (hinge) لتحت (مثل جسر متحرك) أو لفوق (تصير جدار)
public struct FoldSpec
{
    public float hingeX, length, top;
    public bool foldUp;          // true = توقف مثل الجدار، false = تنزل لتحت
    public bool foldOver;        // تنطوي فوق الصفحة اللي جنبها (مثل قلب ورقة)
    public bool hingeOnRight;    // المفصل على يمين الصفحة بدل يسارها
    public float period, phase;  // طول الدورة كاملة، ووين تبدأ فيها

    public FoldSpec(float hingeX, float length, float top, bool foldUp, bool hingeOnRight, float period, float phase, bool foldOver = false)
    {
        this.hingeX = hingeX;
        this.length = length;
        this.top = top;
        this.foldUp = foldUp;
        this.foldOver = foldOver;
        this.hingeOnRight = hingeOnRight;
        this.period = period;
        this.phase = phase;
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

    public bool colors;                                   // صفحة الألوان: أحمر ينطط، أزرق يزلّق، أخضر يطول
    public List<HintLine> hints = new List<HintLine>();   // خطوط تلميح منقطة (ملونة)

    public float eraserSpeed;                             // صفحة الممحاة: سرعة الممحاة العملاقة (0 = ما فيه)
    public List<Vector2> eraserHoles = new List<Vector2>(); // الأماكن اللي تمسحها من الأرض (من x لـ x)

    public bool flipped;                                  // الصفحة المقلوبة: الجاذبية لفوق

    public float floodStart, floodSpeed, floodMax;        // صفحة الحبر المنسكب: الحبر يرتفع
    public bool Flood => floodSpeed > 0f;

    public List<FoldSpec> folds = new List<FoldSpec>();   // صفحة الطيّة: صفحات تنطوي وتنفتح
    public float timeLimit;                               // وقت محدود للمرحلة بالثواني (0 = بدون وقت)
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
        ColorsPage(),
        EraserPage(),
        UpsideDownPage(),
        SpilledInkPage(),
        FoldingPage(),
    };


    // صفحة سميكة من فوق (top) لتحت (bottom)، مثل جدار عالي
    static Block Wall(float fromX, float toX, float top, float bottom = -1.2f)
    {
        return new Block(new Vector3((fromX + toX) * 0.5f, (top + bottom) * 0.5f, 0f), new Vector3(toX - fromX, top - bottom, 4f));
    }

    // صفحة الألوان: جدار عالي (أحمر ينطط)، حفرة طويلة (أخضر يطول)، وحفرة عريضة (أزرق يطيّرها)
    static LevelData ColorsPage()
    {
        return new LevelData
        {
            title = "صفحة الألوان",
            subtitle = "٢ أحمر ينطط  •  ٣ أزرق يزلّق  •  ٤ أخضر يطول",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(42f, 0f, 0f),
            ink = 9f,
            colors = true,
            blocks =
            {
                Page(-7f, 6f),              // جدار عالي بعده
                Wall(6f, 14f, 2.5f),        // حفرة طويلة 8
                Wall(22f, 28f, 2.5f),       // حفرة 5.5 وصفحة أوطى
                Page(33.5f, 46f),
            },
            hints =
            {
                new HintLine(new Vector3(4f, 0.18f, 0f), new Vector3(5.3f, 0.18f, 0f), InkColorEffect.ColorOf(InkColor.Red)),
                new HintLine(new Vector3(13.6f, 2.7f, 0f), new Vector3(15.8f, 2.7f, 0f), InkColorEffect.ColorOf(InkColor.Green)),
                new HintLine(new Vector3(25.5f, 2.68f, 0f), new Vector3(27.8f, 3.4f, 0f), InkColorEffect.ColorOf(InkColor.Blue)),
            },
        };
    }

    // صفحة الممحاة: ممحاة عملاقة تمشي قدام رويدا وتمسح الأرض
    static LevelData EraserPage()
    {
        return new LevelData
        {
            title = "صفحة الممحاة",
            subtitle = "ممحاة عملاقة تمسح الأرض قدامك... ارسمي قبل ما توصلين!",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(50f, 0f, 0f),
            ink = 24f,
            eraserSpeed = 3.3f,
            blocks = { Page(-7f, 54f) },
            eraserHoles =
            {
                new Vector2(8f, 11f), new Vector2(15f, 18.5f), new Vector2(23f, 26f),
                new Vector2(31f, 34.5f), new Vector2(39f, 42f),
            },
        };
    }

    // صفحة سقف: سطحها اللي تمشي عليه رويدا تحت (surface)
    static Block Ceiling(float fromX, float toX, float surface)
    {
        const float thickness = 1.2f;
        return new Block(new Vector3((fromX + toX) * 0.5f, surface + thickness * 0.5f, 0f), new Vector3(toX - fromX, thickness, 4f));
    }

    // الصفحة المقلوبة: الجاذبية لفوق ورويدا تمشي على السقف.
    // فيها حفر، سقوف على ارتفاعات مختلفة، بروزات تسكر الطريق، ووحوش تمشي مقلوبة
    static LevelData UpsideDownPage()
    {
        return new LevelData
        {
            title = "الصفحة المقلوبة",
            subtitle = "الجاذبية انقلبت! ارسمي طرق تعدي البروزات... وامسحي الوحوش",
            spawn = new Vector3(-5f, -1f, 0f),
            door = new Vector3(43f, 0f, 0f),
            ink = 28f,
            flipped = true,
            deathY = 9f,
            blocks =
            {
                Ceiling(-7f, 4f, 0f),           // حفرة 3
                Ceiling(7f, 14f, 0f),           // فيها بروز
                Ceiling(9.8f, 10.8f, -0.9f),    // بروز يسكر الطريق (يحتاج طريق يطلع فوقه)
                Ceiling(17f, 24f, 0.8f),        // حفرة 3 (سقف أوطى)، فيها وحش
                Ceiling(27f, 32f, -0.6f),       // حفرة 3 (سقف أعلى)، فيها وحش
                Ceiling(35.5f, 47f, 0f),        // حفرة 3.5
                Ceiling(38.5f, 39.5f, -0.8f),   // بروز ثاني
            },
            monsters =
            {
                Monster(ScribbleMonster.Kind.GreenEye, 21f, 18.5f, 23f),
                Monster(ScribbleMonster.Kind.PurpleBlob, 30f, 28f, 31.5f),
            },
        };
    }

    static MonsterSpawn Monster(ScribbleMonster.Kind kind, float x, float from, float to) => new MonsterSpawn(kind, x, from, to);

    // صفحة الحبر المنسكب: الحبر يرتفع، سلم يطلّعها، وقارب يطفو ويرفعها
    static LevelData SpilledInkPage()
    {
        var level = new LevelData
        {
            title = "صفحة الحبر المنسكب",
            subtitle = "الحبر يرتفع! ارسمي سلم تطلع عليه... وقارب كبير يطفو فيها",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(23f, 2.3f, 0f),
            ink = 17f,
            floodStart = -2.5f,
            floodSpeed = 0.33f,
            floodMax = 2f,
            blocks =
            {
                Page(-7f, 3f),                 // حفرة 3 وصفحة أعلى (سلم)
                Wall(6f, 11f, 1.3f, -3.6f),    // حوض الحبر بعده
                Page(11f, 16f, -3f),           // أرضية الحوض
                Wall(16f, 27f, 2.3f, -3.6f),   // جدار عالي (القارب يرفعها له)
            },
            hints =
            {
                new HintLine(new Vector3(2.7f, 0.2f, 0f), new Vector3(6.4f, 1.5f, 0f), InkColorEffect.ColorOf(InkColor.Black)),
            },
        };
        // قارب منقط في الحوض
        const int n = 16;
        for (int i = 0; i < n; i += 2)
        {
            float a0 = i / (float)n * Mathf.PI * 2f, a1 = (i + 1) / (float)n * Mathf.PI * 2f;
            Vector3 c = new Vector3(13.5f, -1.2f, 0f);
            level.hints.Add(new HintLine(c + new Vector3(Mathf.Cos(a0) * 2f, Mathf.Sin(a0) * 0.6f, 0f), c + new Vector3(Mathf.Cos(a1) * 2f, Mathf.Sin(a1) * 0.6f, 0f), InkColorEffect.ColorOf(InkColor.Black)));
        }
        return level;
    }

    // صفحة الطيّة: صفحات تنطوي فوق اللي جنبها (مثل قلب ورقة) وصفحات توقف جدار.
    // الطيّة تمسح أي رسمة تعترضها، والحبر ما يكفي تجسرين كل شي، فلازم توقيت
    static LevelData FoldingPage()
    {
        return new LevelData
        {
            title = "صفحة الطيّة",
            subtitle = "الصفحات تنطوي وتمسح الرسم اللي يعترضها... انتبهي للتوقيت!",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(37f, 0f, 0f),
            ink = 18f,
            timeLimit = 26f,
            blocks =
            {
                Page(-7f, 4f),
                Page(7f, 11f),
                Page(14f, 17f),
                Page(23f, 27f),
                Page(30.5f, 40f),
            },
            folds =
            {
                new FoldSpec(4f, 3f, 0f, false, false, 5f, 1f, true),       // تنطوي فوق الصفحة الأولى (أول مرة تكون مفتوحة)
                new FoldSpec(11f, 3f, 0f, true, false, 4.5f, 1.5f),         // توقف جدار
                new FoldSpec(17f, 3f, 0f, false, false, 4f, 2.5f, true),    // زوج يلتقي في النص
                new FoldSpec(23f, 3f, 0f, false, true, 4f, 2.5f, true),     // (الزوج ينطوي مع بعض)
                new FoldSpec(27f, 3.5f, 0f, false, false, 3.5f, 1f, true),  // سريعة
            },
        };
    }

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
