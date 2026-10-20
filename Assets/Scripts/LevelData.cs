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

// الزلاجة: رويدا توقف عند spotX لين ترسمين زلاجة، بعدين تنزل المنحدر وتطير فوق الحفرة
public class SledSpec
{
    public float spotX;          // وين توقف رويدا تستنى الزلاجة
    public float hillStart;      // بداية المنحدر (آخر الصفحة العالية)
    public float hillEnd;        // نهاية المنحدر
    public float drop;           // كم ينزل المنحدر
    public float rampEnd;        // نهاية القفزة (الطلعة الصغيرة)
    public float rampRise;       // كم تطلع القفزة
    public float landingTop;     // سطح الصفحة اللي تنزل عليها
}

// أرضية طايرة: مكانها، عرضها، وكم تتحرك يمين ويسار (وكل كم ثانية)
public struct FloaterSpec
{
    public float centerX, width, amplitude, period, phase;

    public FloaterSpec(float centerX, float width, float amplitude, float period, float phase)
    {
        this.centerX = centerX;
        this.width = width;
        this.amplitude = amplitude;
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
    public LevelTheme theme = LevelTheme.Classic();       // شكل المرحلة (ورق، سما، إضاءة، أغراض)
    public bool Scribbly => monsters.Count > 0;
    public bool monsterHint;       // أول وحش فوقه تلميح شخبطة

    public bool colors;                                   // صفحة الألوان: أحمر ينطط، أزرق يزلّق، أخضر يطول
    public List<HintLine> hints = new List<HintLine>();   // خطوط تلميح منقطة (ملونة)


    public bool flipped;                                  // الصفحة المقلوبة: الجاذبية لفوق

    public float floodStart, floodSpeed, floodMax;        // صفحة الحبر المنسكب: الحبر يرتفع
    public bool Flood => floodSpeed > 0f;

    public List<FoldSpec> folds = new List<FoldSpec>();   // صفحة الطيّة: صفحات تنطوي وتنفتح
    public List<Vector3> vanishTiles = new List<Vector3>(); // الصفحة المتلاشية: (من x، لـ x، مسافة البداية)
    public List<FloaterSpec> floaters = new List<FloaterSpec>(); // الأرض الطايرة: صفحات تتحرك في السما
    public List<Vector3> icePlanks = new List<Vector3>();   // زيزو: ألواح جليد طايرة تتكسر بعد ما تنطين عليها (من x، لـ x، ارتفاع السطح)
    public bool allowJump;                                  // تقدر رويدا تنط (زر المسافة)
    public float gravityScale = 1f;                         // الفضاء: جاذبية خفيفة (أقل من 1)
    public bool planets;                                    // الفضاء: كواكب كبيرة بالخلفية
    public bool drawDoor;                                   // الصفحة الأخيرة: ما فيه باب، اللاعبة ترسمه
    public bool memoryClouds;                               // الصفحة الأخيرة: غيوم فيها ذكريات (صور ورسومات)
    public bool balloonInk;                                 // البالونات: الأشكال المغلقة تصير بالونات تطير برويدا
    public List<Vector4> firePits = new List<Vector4>();    // النار: حفر نار (من x، لـ x، كل كم ثانية، تأخير البداية)
    public List<Vector4> dragons = new List<Vector4>();     // النار: تنين على صخرة (x، y، وين ينفخ x، تأخير)
    public List<Vector4> portals = new List<Vector4>();     // البوابات: (دخول x، دخول y، خروج x، خروج y) على سطح الأرض
    public List<Vector3> portalMoves = new List<Vector3>(); // البوابات: البرتقالية تتحرك (رقم البوابة، كم متر، كل كم ثانية)
    public List<Vector3> gates = new List<Vector3>();       // البوابات: باب مقفل (x الباب، x الزر، ارتفاع الرف)
    public float maxSpan;                                   // الوادي: الخط أطول من كذا ينكسر تحت رويدا (0 = ما فيه)
    public List<Vector3> pegs = new List<Vector3>();        // الوادي: حجارة طايرة (x، سطحها، عرضها)
    public List<Vector4> birds = new List<Vector4>();       // الوادي: طير ورق ينقض على حفرة (من x، لـ x، أوطى نقطة، تأخير)
    public List<Vector3> pins = new List<Vector3>();        // البالونات: دبابيس معلقة تفجر البالونة (من x، لـ x، ارتفاعها)
    public List<Vector3> fans = new List<Vector3>();        // البالونات: مروحة تدف البالونة لليمين (من x، لـ x، ارتفاع الطيران)
    public bool darkness;                                   // اليراعات: ظلام، فانوس مع رويدا، وخطوط تضوي
    public bool eraserChase;                                // نهاية الجزء الأول: ممحاة عملاقة تلحق رويدا

    // الجزء الثاني: البحر
    public List<Vector4> slopes = new List<Vector4>();      // صفحات مايلة (من x، من y، لـ x، لـ y) على السطح
    public bool sea;                                        // فيه بحر (يطلع وينزل)
    public float seaLevel, tideAmp, tidePeriod;             // مستوى البحر، كم يطلع وينزل، وكل كم ثانية
    public List<Vector3> crabs = new List<Vector3>();       // سرطانات (من x، لـ x، سطح الرمل)
    public bool underwater;                                 // تحت المويه: هوا ينقص، وجاذبية خفيفة
    public bool waitForBridge;                              // رويدا تستنى عند كل حفرة لين ينرسم جسر
    public List<Vector3> tideBars = new List<Vector3>();    // المد والجزر: رمل يغرق (من x فوق، لـ x فوق، سطح الرمل)
    public List<Vector3> vents = new List<Vector3>();       // فتحات فقاعات (x، y، كل كم ثانية)
    public List<Vector3> jellies = new List<Vector3>();     // قناديل بحر (x، y، كم تطلع وتنزل)
    public List<Vector3> tentacles = new List<Vector3>();   // أذرع أخطبوط (من x، لـ x، تأخير)
    public List<Vector3> whirlpools = new List<Vector3>();  // دوامات (x، نص القطر اللي تسحب منه)
    public bool boatRide;                                   // المنارة: الشكل المغلق بالبحر يصير قارب يشيل رويدا
    public List<Vector3> sharks = new List<Vector3>();      // قروش (من x، لـ x)
    public Vector3? lighthouse;                             // المنارة (x، سطح الأرض، أبعد x يوصله الضو)

    // الجزء الثاني: أدوات المدرسة
    public List<Vector4> rulers = new List<Vector4>();      // مسطرة ميزان (من x، لـ x، مكان القلم اللي تحتها، ثقل الممحاة)
    public List<Vector3> colorGates = new List<Vector3>();  // أبواب ملونة (x، اللون، اللون الثاني أو -1). الألوان: 1 أحمر، 2 أزرق، 3 أخضر
    public List<CalcSpec> calcs = new List<CalcSpec>();     // آلة حاسبة: أزرار أرقام وباب ينفتح إذا المجموع صح
    public List<Vector3> scissors = new List<Vector3>();    // مقص كبير على حفرة (من x، لـ x)

    // الجزء الثاني: الوقت
    public List<Vector3> clocks = new List<Vector3>();      // ساعة على حفرة (من x، لـ x، سرعة العقرب درجة بالثانية)
    public List<Vector4> hourglasses = new List<Vector4>(); // ساعة رملية فوق حفرة (من x، لـ x، مكانها x، y)
    public List<GearSpec> gearBridges = new List<GearSpec>(); // تروس وجسر متحرك
    public List<Vector4> alarms = new List<Vector4>();      // منبه وقطوة نايمة (x القطوة، x المنبه، y المنبه، سقف فوقه 1/0)
    public List<Vector4> dayPads = new List<Vector4>();     // الليل والنهار: صفحات (من x، لـ x، سطحها، 0 نهار / 1 ليل)
    public List<Vector3> swarms = new List<Vector3>();      // اليراعات: سرب يعبي الفانوس (لمسة رويدا أو خط)
    public List<Vector3> shadows = new List<Vector3>();     // اليراعات: ظلال تخاف من الضو
    public Vector2 meteorZone;                              // الفضاء: مطر نيازك بين x و x (صفر = ما فيه)
    public bool HasMeteors => meteorZone.y > meteorZone.x;
    public Vector3? blackHole;                              // الفضاء: مكان الثقب الأسود (null = ما فيه)
    public bool avalanche;                                  // زيزو: انهيار ثلجي يلحقها من ورا
    public SledSpec sled;                                   // زيزو: محطة الزلاجة والمنحدر (null = ما فيه)
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
        FloatingPagesLevel(),
        UpsideDownPage(),
        SpilledInkPage(),
        FoldingPage(),
        SnowLevel(),
        SpaceLevel(),
        BalloonLevel(),
        FireLevel(),
        PortalLevel(),
        CanyonLevel(),
        FirefliesLevel(),
        EraserFinale(),
        TideLevel(),
        UnderwaterLevel(),
        OctopusLevel(),
        WhirlpoolLevel(),
        LighthouseLevel(),
        RulerLevel(),
        CrayonBoxLevel(),
        CalculatorLevel(),
        ScissorsLevel(),
        PencilCaseLevel(),
        ClockLevel(),
        HourglassLevel(),
        GearsLevel(),
        AlarmLevel(),
        DayNightLevel(),
        LastPage(),
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
            theme = LevelTheme.PastelPaint(),
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

    // الأرض الطايرة: أربع أرضيات تتحرك في سما الدفتر، ارسمي جسر بينها في الوقت الصح.
    // الحبر ما يكفي لخط واحد طويل، والأرضية اللي تعدينها تختفي وراك
    static LevelData FloatingPagesLevel()
    {
        return new LevelData
        {
            title = "الأرض الطايرة",
            theme = LevelTheme.EraserAfternoon(),
            subtitle = "أربع أرضيات تطير في سما الدفتر... ارسمي جسر بينها!",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(37f, 0f, 0f),
            ink = 24f,
            blocks = { Page(-7f, 4f), Page(30f, 41f) },
            floaters =
            {
                new FloaterSpec(7.5f, 3f, 1.3f, 4f, 0f),
                new FloaterSpec(13.5f, 3f, 1.6f, 5f, 1.5f),
                new FloaterSpec(19.5f, 2.6f, 1.4f, 3.6f, 3f),
                new FloaterSpec(25.5f, 3f, 1.6f, 4.6f, 4.5f),
            },
            hints = { new HintLine(new Vector3(3.6f, 0.12f, 0f), new Vector3(6.6f, 0.12f, 0f), new Color(0.3f, 0.3f, 0.35f)) },
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
            theme = LevelTheme.Cosmic(),
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
            theme = LevelTheme.InkParchment(),
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

    // الصفحة المتلاشية: أجزاء من الأرض تهتز وتختفي لما تقربين منها، ارسمي خط فوقها عشان تعدين.
    // (تقدرين ترسمين الخط قبل لا تختفي، والخط يبقى)
    // زيزو: صباح ثلجي، والمرحلة الوحيدة اللي فيها نط (زر المسافة). انهيار ثلجي يلحق رويدا من البداية:
    // 1) ألواح جليد طايرة فوق وتحت، كل لوح يتكسر بعد ما تنطين عليه  2) رجال ثلج يرمون كور كثير (ارسمي درع)
    // 3) فوق التلة ترسمين زلاجة وتنزلين المنحدر وتطيرين فوق حفرة كبيرة
    static LevelData SnowLevel()
    {
        return new LevelData
        {
            title = "زيزو",
            theme = LevelTheme.SnowMorning(),
            subtitle = "نطّي بزر المسافة! الجليد يتكسر، الكور تطير، والانهيار وراك",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(66f, -3.5f, 0f),
            ink = 18f,
            allowJump = true,
            avalanche = true,
            blocks =
            {
                Page(-7f, 4f),                  // بعدها ألواح الجليد
                Page(23.4f, 36f),               // التلة: رجال الثلج فوقها ومحطة الزلاجة في آخرها
                Page(56f, 70f, -3.5f),          // تحت بعد الطيران
                Wall(12.9f, 14.1f, 5f, 4.4f),   // رف عالي فوق الجليد (رجل ثلج يرمي وأنتِ تنطين)
                Wall(27.6f, 29.2f, 4.2f, 3.6f), // رفوف رجال الثلج فوق التلة (عالية عشان ما توصلها بالنط)
                Wall(31.4f, 33f, 4.2f, 3.6f),
            },
            // ألواح جليد: من x، لـ x، ارتفاع السطح (فوق وتحت وبينها فراغ)
            icePlanks =
            {
                new Vector3(5.8f, 8.4f, 0.5f),
                new Vector3(10.2f, 12.8f, 1.4f),
                new Vector3(14.6f, 17.2f, 0.5f),
                new Vector3(19f, 21.6f, 1.3f),
            },
            sled = new SledSpec { spotX = 32.5f, hillStart = 36f, hillEnd = 46f, drop = 4f, rampEnd = 48f, rampRise = 1f, landingTop = -3.5f },
            monsters =
            {
                Monster(ScribbleMonster.Kind.SnowThrower, 13.5f, 13.5f, 13.5f),
                Monster(ScribbleMonster.Kind.SnowThrower, 28.4f, 28.4f, 28.4f),
                Monster(ScribbleMonster.Kind.SnowThrower, 32.2f, 32.2f, 32.2f),
            },
            hints =
            {
                new HintLine(new Vector3(33.2f, 0.25f, 0f), new Vector3(35.1f, 0.25f, 0f), new Color(0.85f, 0.3f, 0.35f)),
                new HintLine(new Vector3(35.1f, 0.25f, 0f), new Vector3(35.45f, 0.65f, 0f), new Color(0.85f, 0.3f, 0.35f)),
            },
        };
    }

    // صفحة الفضاء: مطر نيازك (دوائر حمرا توري وين بتطيح، تكسر الصفحة وتصيبك: ارسمي مظلة)،
    // بعدين ثقب أسود يسحبك لفوق ويبلعك (ارسمي سقف يمسكك)، وفضائيين يطلقون ليزر
    static LevelData SpaceLevel()
    {
        return new LevelData
        {
            title = "صفحة الفضاء",
            theme = LevelTheme.Space(),
            subtitle = "انتبهي للدوائر الحمرا! ارسمي مظلة من النيازك، وسقف يحميك من الثقب الأسود",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(57f, 0f, 0f),
            ink = 30f,
            gravityScale = 0.75f,
            planets = true,
            blocks =
            {
                Page(-7f, 3f),          // حفرة 3
                Page(6f, 26f),          // مطر النيازك: الصفحة تتكسر قطع
                Page(29f, 47f),         // الثقب الأسود فوقها
                Page(50f, 60f),
            },
            meteorZone = new Vector2(6f, 26f),
            blackHole = new Vector3(38f, 3.4f, 0f),
            monsters =
            {
                Monster(ScribbleMonster.Kind.Alien, 27.5f, 27.5f, 27.5f),
                Monster(ScribbleMonster.Kind.Alien, 53f, 53f, 53f),
            },
            bridgeHint = true,
            // تلميح: سقف تحت الثقب الأسود
            hints = { new HintLine(new Vector3(35.6f, 1.8f, 0f), new Vector3(40.6f, 1.8f, 0f), new Color(0.75f, 0.45f, 1f)) },
        };
    }

    // الصفحة الأخيرة: وردية وتلمع بقليتر، وفوقها غيوم فيها ذكريات رويدا (صور المراحل ورسوماتك).
    // فيها شوي من كل شي: حفرة، أرض تختفي، صفحات طايرة تتحرك، طلعة، وفي الآخر ترسمين باب البيت
    static LevelData LastPage()
    {
        return new LevelData
        {
            title = "الصفحة الأخيرة",
            theme = LevelTheme.LastPage(),
            subtitle = "آخر صفحة في الدفتر... عدّي كل شي وارسمي باب البيت!",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(48f, 0f, 0f),
            ink = 34f,
            drawDoor = true,
            memoryClouds = true,
            bridgeHint = true,
            blocks =
            {
                Page(-7f, 6f),              // حفرة 3
                Page(9f, 16f),              // فيها قطعة تختفي، وبعدها صفحات طايرة
                Page(30f, 36f, 0.8f),       // طلعة (ارسمي منحدر)، وبعدها حفرة 3
                Page(39f, 53f),
            },
            vanishTiles = { new Vector3(12f, 14.5f, 4f) },
            floaters =
            {
                new FloaterSpec(19.5f, 3f, 1.2f, 4f, 0f),
                new FloaterSpec(25.5f, 3f, 1.3f, 4.4f, 1.5f),
            },
        };
    }

    // ========== الجزء الثاني ==========

    // المد والجزر: البحر يطلع وينزل، ومسطرة بالبحر تبين وين وصل المويه. رويدا تستنى الجزر قبل الرمل،
    // والرمل الطويل ما تلحق تعديه... ارسمي له ممشى فوق المويه
    static LevelData TideLevel()
    {
        return new LevelData
        {
            title = "المد والجزر",
            theme = LevelTheme.Ocean(),
            subtitle = "رويدا تستنى الجزر عشان تعدي الرمل... والرمل الطويل ارسمي له ممشى",
            spawn = new Vector3(-5f, 1.8f, 0f),
            door = new Vector3(44f, 0.8f, 0f),
            ink = 28f,
            sea = true, seaLevel = -0.3f, tideAmp = 1f, tidePeriod = 8f,
            blocks =
            {
                Page(-7f, 3f, 0.8f),
                Page(5f, 8f, -0.3f),        // رمل قصير: تستنى الجزر وتعدي
                Page(10f, 14f, 0.8f),
                Page(16f, 26f, -0.3f),      // رمل طويل: ما تلحق، تبي ممشى
                Page(28f, 31f, 0.8f),
                Page(33f, 36f, -0.3f),      // رمل فيه سرطان
                Page(38f, 48f, 0.8f),
            },
            slopes =
            {
                new Vector4(3f, 0.8f, 5f, -0.3f), new Vector4(8f, -0.3f, 10f, 0.8f),
                new Vector4(14f, 0.8f, 16f, -0.3f), new Vector4(26f, -0.3f, 28f, 0.8f),
                new Vector4(31f, 0.8f, 33f, -0.3f), new Vector4(36f, -0.3f, 38f, 0.8f),
            },
            // (من x، لـ x، سطح الرمل): من فوق المنحدر لين فوق المنحدر الثاني
            tideBars =
            {
                new Vector3(3f, 10f, -0.3f),
                new Vector3(14f, 28f, -0.3f),
                new Vector3(31f, 38f, -0.3f),
            },
            crabs = { new Vector3(33.3f, 35.7f, -0.3f) },
            hints = { new HintLine(new Vector3(14.2f, 0.85f, 0f), new Vector3(27.8f, 0.85f, 0f), new Color(0.3f, 0.55f, 0.85f)) },
        };
    }

    // أعماق البحر: الهوا ينقص (شريط الهوا فوق). فتحات بقاع الحفر تطلع فقاعات هوا،
    // والجسر فوق الحفرة يخلي الفقاعات تتزحلق تحته وتطلع عند طرفه... بطريق رويدا. وقناديل البحر تخاف من الحبر
    static LevelData UnderwaterLevel()
    {
        var level = new LevelData
        {
            title = "أعماق البحر",
            theme = LevelTheme.Underwater(),
            subtitle = "ارسمي جسور فوق الحفر... الفقاعات تتزحلق تحتها وتعبي هوا رويدا",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(51f, 0f, 0f),
            ink = 28f,
            underwater = true,
            waitForBridge = true,
            gravityScale = 0.6f,
            blocks =
            {
                Wall(-7f, 4f, 0f, -8f),
                Wall(7.5f, 16f, 0f, -8f),
                Wall(20f, 27f, 0f, -8f),
                Wall(31f, 40f, 0f, -8f),
                Wall(44f, 54f, 0f, -8f),
            },
            vents =
            {
                new Vector3(5.75f, -2.6f, 1f),
                new Vector3(18f, -2.6f, 1f),
                new Vector3(29f, -2.6f, 1f),
                new Vector3(42f, -2.6f, 1f),
            },
            jellies =
            {
                new Vector3(12f, 1.7f, 0.9f),
                new Vector3(35f, 1.6f, 1f),
            },
        };
        Color c = new Color(0.3f, 0.6f, 0.8f);
        foreach (Vector2 gap in new[] { new Vector2(4f, 7.5f), new Vector2(16f, 20f), new Vector2(27f, 31f), new Vector2(40f, 44f) })
            level.hints.Add(new HintLine(new Vector3(gap.x - 0.2f, 0.1f, 0f), new Vector3(gap.y + 0.2f, 0.1f, 0f), c));
        // خط قصير فوق أول قنديل (يخوفه)
        level.hints.Add(new HintLine(new Vector3(11f, 3.4f, 0f), new Vector3(13f, 3.4f, 0f), new Color(1f, 0.55f, 0.85f)));
        return level;
    }

    // الأخطبوط العملاق: في كل حفرة ذراع جوعانة تسحب أي جسر تحت المويه.
    // ارمي لها كورة تاكلها وتنام، وبعدين ارسمي الجسر
    static LevelData OctopusLevel()
    {
        var level = new LevelData
        {
            title = "الأخطبوط العملاق",
            theme = LevelTheme.SunsetSea(),
            subtitle = "الأخطبوط جوعان! ارمي له كورة ياكلها وينام... وبعدين ارسمي الجسر",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(43f, 0f, 0f),
            ink = 32f,
            sea = true, seaLevel = -0.7f, tidePeriod = 1f,
            blocks =
            {
                Page(-7f, 4f),
                Page(9f, 17f),
                Page(22f, 30f),
                Page(35f, 46f),
            },
            tentacles =
            {
                new Vector3(4f, 9f, 0f),
                new Vector3(17f, 22f, 0f),
                new Vector3(30f, 35f, 0f),
            },
        };
        // أول حفرة: كورة منقطة فوق المويه، وجسر منقط
        Oval(level, 6.5f, 1.4f, new Color(1f, 0.5f, 0.75f));
        level.hints.Add(new HintLine(new Vector3(3.8f, 0.1f, 0f), new Vector3(9.2f, 0.1f, 0f), new Color(0.55f, 0.3f, 0.5f)));
        return level;
    }

    // الدوامة: كل خط يقرب منها تبلعه. الجسر المستقيم ما ينفع، ارسمي جسر مقوّس عالي فوقها
    static LevelData WhirlpoolLevel()
    {
        var level = new LevelData
        {
            title = "الدوامة",
            theme = LevelTheme.StormSea(),
            subtitle = "الدوامة تبلع أي خط يقرب منها... ارسمي الجسر مقوّس فوقها!",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(37f, 0f, 0f),
            ink = 26f,
            sea = true, seaLevel = -0.7f, tidePeriod = 1f,
            blocks =
            {
                Page(-7f, 4f),
                Page(12f, 20f),
                Page(30f, 40f),
            },
            whirlpools =
            {
                new Vector3(8f, 2.4f, 0f),
                new Vector3(25f, 3f, 0f),
            },
        };
        // قوس منقط فوق أول دوامة
        Vector3 prev = new Vector3(3.8f, 0.1f, 0f);
        for (int i = 1; i <= 8; i++)
        {
            float x = 3.8f + i * (12.2f - 3.8f) / 8f;
            var p = new Vector3(x, 0.1f + 2.1f * Mathf.Sin(Mathf.PI * i / 8f), 0f);
            level.hints.Add(new HintLine(prev, p, new Color(0.3f, 0.5f, 0.6f)));
            prev = p;
        }
        return level;
    }

    // المنارة (نهاية فصل البحر): ليل. ارسمي كورة بالبحر تصير قارب بشراع يشيل رويدا لين أول صخرة،
    // وبعدين قارب ثاني... والقرش يعض القوارب إلا لو ضو المنارة عليه (يخاف ويغطس)
    static LevelData LighthouseLevel()
    {
        var level = new LevelData
        {
            title = "المنارة",
            theme = LevelTheme.LighthouseNight(),
            subtitle = "ارسمي قارب بالبحر يوصلك للمنارة... والقرش يخاف من ضوها!",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(49f, -0.4f, 0f),
            ink = 28f,
            sea = true, seaLevel = -0.7f, tidePeriod = 1f,
            boatRide = true,
            blocks =
            {
                Page(-7f, 4f),
                Page(44f, 58f, -0.4f),
            },
            pegs =
            {
                new Vector3(13f, -0.45f, 1.8f),
                new Vector3(24f, -0.45f, 1.8f),
                new Vector3(35f, -0.45f, 1.8f),
            },
            sharks = { new Vector3(25.5f, 33.5f, 0f) },
            lighthouse = new Vector3(54f, -0.4f, 40f),
        };
        Oval(level, 5.4f, 0.4f, new Color(0.6f, 0.75f, 1f));
        return level;
    }

    // ===== أدوات المدرسة =====

    // المسطرة: مسطرة فوق كل حفرة، مايلة لأن على طرفها ممحاة ثقيلة.
    // ارمي كورة على طرفها الثاني تعدلها وتصير جسر (كل ما كانت الكورة أبعد كانت أقوى)
    static LevelData RulerLevel()
    {
        var level = new LevelData
        {
            title = "المسطرة",
            theme = LevelTheme.CrayonDay(),
            subtitle = "ارمي كورة على طرف المسطرة عشان تتعدل... كل ما كانت أبعد كانت أقوى!",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(53f, 0f, 0f),
            ink = 20f,
            blocks =
            {
                Page(-7f, 4f),
                Page(12f, 20f),
                Page(29f, 37f),
                Page(46f, 56f),
            },
            // من x، لـ x، القلم اللي تحتها، ثقل الممحاة
            rulers =
            {
                new Vector4(4.1f, 12.9f, 6.6f, 1.5f),
                new Vector4(20.1f, 29.9f, 22.5f, 2.2f),
                new Vector4(37.1f, 46.9f, 39.4f, 3f),
            },
        };
        Oval(level, 11.2f, 4f, new Color(1f, 0.55f, 0.3f));
        return level;
    }

    // صندوق الألوان: كل باب ما ينفتح إلا إذا رسمتي عليه بلونه (الأرقام تختار اللون).
    // الباب البنفسجي يبي الأحمر والأزرق مع بعض
    static LevelData CrayonBoxLevel()
    {
        return new LevelData
        {
            title = "صندوق الألوان",
            theme = LevelTheme.PastelPaint(),
            subtitle = "كل باب يبي لونه! اختاري اللون بالأرقام وارسمي على الباب",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(45f, 0f, 0f),
            ink = 30f,
            colors = true,
            waitForBridge = true,
            blocks =
            {
                Page(-7f, 8f),
                Page(11f, 21f),
                Page(24.5f, 35f),
                Page(38f, 48f),
            },
            // x، اللون (1 أحمر، 2 أزرق، 3 أخضر)، اللون الثاني (-1 = ما فيه)
            colorGates =
            {
                new Vector3(5f, 1f, -1f),
                new Vector3(17f, 2f, -1f),
                new Vector3(30f, 1f, 2f),
                new Vector3(41.5f, 3f, -1f),
            },
            hints = { new HintLine(new Vector3(4.6f, 0.8f, 0f), new Vector3(4.6f, 2.6f, 0f), InkColorEffect.ColorOf(InkColor.Red)) },
        };
    }

    // الآلة الحاسبة: ارمي كور على أزرار الأرقام (فوق الطريق) لين يصير مجموعها نفس الرقم اللي بالشاشة
    static LevelData CalculatorLevel()
    {
        return new LevelData
        {
            title = "الآلة الحاسبة",
            theme = LevelTheme.GraphDusk(),
            subtitle = "ارمي كور على الأرقام... إذا صار مجموعها نفس رقم الشاشة ينفتح الباب",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(60f, 0f, 0f),
            ink = 36f,
            waitForBridge = true,
            blocks =
            {
                Page(-7f, 18f),
                Page(21f, 40f),
                Page(43f, 64f),
            },
            calcs =
            {
                new CalcSpec(9f, 10.6f, 5, 1, 2, 3, 4),
                new CalcSpec(29f, 30.6f, 9, 2, 3, 4, 5, 6),
                new CalcSpec(50f, 51.6f, 12, 3, 4, 5, 7, 8),
            },
        };
    }

    // المقص: على كل حفرة مقص كبير يفتح ويسكر ويقص الجسور. ارمي كورة بين شفراته تعلقه، وبعدين ارسمي الجسر
    static LevelData ScissorsLevel()
    {
        var level = new LevelData
        {
            title = "المقص",
            theme = LevelTheme.Origami(),
            subtitle = "المقص يقص الجسور! ارمي كورة بين شفراته تعلقه... وبعدين ارسمي الجسر",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(45f, 0f, 0f),
            ink = 40f,
            blocks =
            {
                Page(-7f, 4f),
                Page(9.5f, 18f),
                Page(24f, 32f),
                Page(38f, 48f),
            },
            scissors =
            {
                new Vector3(4f, 9.5f, 0f),
                new Vector3(18f, 24f, 0f),
                new Vector3(32f, 38f, 0f),
            },
        };
        Oval(level, 6.5f, 1.6f, new Color(1f, 0.45f, 0.55f));
        level.hints.Add(new HintLine(new Vector3(3.8f, 0.1f, 0f), new Vector3(9.7f, 0.1f, 0f), new Color(0.5f, 0.5f, 0.55f)));
        return level;
    }

    // المقلمة (نهاية فصل أدوات المدرسة): كل الأدوات مع بعض: مسطرة، باب ملون، مقص، آلة حاسبة، ومسطرة أثقل
    static LevelData PencilCaseLevel()
    {
        return new LevelData
        {
            title = "المقلمة",
            theme = LevelTheme.KraftDawn(),
            subtitle = "كل أدوات المدرسة مع بعض... وريها وش تعلمتي!",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(59f, 0f, 0f),
            ink = 46f,
            colors = true,
            blocks =
            {
                Page(-7f, 4f),
                Page(12f, 22f),
                Page(27.5f, 44f),
                Page(52f, 62f),
            },
            rulers =
            {
                new Vector4(4.1f, 12.9f, 6.6f, 1.5f),
                new Vector4(44.1f, 52.9f, 46.4f, 2.6f),
            },
            colorGates = { new Vector3(17f, 2f, -1f) },
            scissors = { new Vector3(22f, 27.5f, 0f) },
            calcs = { new CalcSpec(33f, 34.6f, 6, 1, 2, 4, 5) },
        };
    }

    // ===== الوقت =====

    // الساعة: عقرب الدقايق هو الجسر بس يلف. ارسمي خط صغير تحت مكانه لما يصير مستوي، يوقف عليه وتعدين
    static LevelData ClockLevel()
    {
        var level = new LevelData
        {
            title = "الساعة",
            theme = LevelTheme.Watercolor(),
            subtitle = "عقرب الساعة يلف! ارسمي خط تحت مكانه لما يصير مستوي... يوقف عليه وتعدين",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(48f, 0f, 0f),
            ink = 18f,
            blocks =
            {
                Page(-7f, 4f),
                Page(10f, 19f),
                Page(26f, 34f),
                Page(42f, 52f),
            },
            // من x، لـ x، سرعة العقرب
            clocks =
            {
                new Vector3(4f, 10f, 60f),
                new Vector3(19f, 26f, 90f),
                new Vector3(34f, 42f, 130f),
            },
        };
        level.hints.Add(new HintLine(new Vector3(7.6f, -0.5f, 0f), new Vector3(9.4f, -0.5f, 0f), new Color(0.35f, 0.3f, 0.45f)));
        return level;
    }

    // الساعة الرملية: فوق كل حفرة ساعة رملية مقلوبة. المسيها بخط تنقلب، والرمل يملي الحفرة لين تقدر رويدا تمشي عليه
    static LevelData HourglassLevel()
    {
        return new LevelData
        {
            title = "الساعة الرملية",
            theme = LevelTheme.Canyon(),
            subtitle = "المسي الساعة الرملية بخط... تنقلب والرمل يملي الحفرة",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(46f, 0f, 0f),
            ink = 22f,
            blocks =
            {
                Wall(-7f, 4f, 0f, -6f),
                Wall(10f, 18f, 0f, -6f),
                Wall(25f, 33f, 0f, -6f),
                Wall(39f, 50f, 0f, -6f),
            },
            // الحفرة (من x، لـ x)، ومكان الساعة الرملية
            hourglasses =
            {
                new Vector4(4f, 10f, 7f, 3.9f),
                new Vector4(18f, 25f, 20f, 4f),
                new Vector4(18f, 25f, 23f, 4f),
                new Vector4(33f, 39f, 36f, 5.6f),
            },
            hints = { new HintLine(new Vector3(5.3f, 2.1f, 0f), new Vector3(6.3f, 3.4f, 0f), new Color(0.6f, 0.4f, 0.25f)) },
        };
    }

    // التروس: الترس الأزرق موتور يلف. ارسمي خط يلمس ترسين ورا بعض (مثل السير)، ولما توصل الحركة لترس الجسر ينزل الجسر
    static LevelData GearsLevel()
    {
        var level = new LevelData
        {
            title = "التروس",
            theme = LevelTheme.InkParchment(),
            subtitle = "الترس الأزرق يلف... ارسمي خط يوصله بالترس الثاني عشان ينزل الجسر",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(42f, 0f, 0f),
            ink = 30f,
            blocks =
            {
                Page(-7f, 4f),
                Page(10f, 16f),
                Page(22f, 28f),
                Page(35f, 45f),
            },
            gearBridges =
            {
                new GearSpec(4f, 10f, new Vector3(6.2f, 3.8f, 0.9f), new Vector3(10.9f, 2.7f, 0.8f)),
                new GearSpec(16f, 22f, new Vector3(17f, 4.4f, 0.8f), new Vector3(19.6f, 3f, 0.7f), new Vector3(22.9f, 2.7f, 0.8f)),
                new GearSpec(28f, 35f, new Vector3(29.6f, 3f, 1f), new Vector3(32.2f, 5.2f, 0.8f), new Vector3(35.9f, 2.7f, 0.8f)),
            },
        };
        level.hints.Add(new HintLine(new Vector3(7f, 3.6f, 0f), new Vector3(10.2f, 2.9f, 0f), new Color(0.35f, 0.6f, 0.85f)));
        return level;
    }

    // المنبه: قطاوة نايمة تسد الطريق. ارمي كورة على زر المنبه يرن وتصحى وتروح.
    // آخر منبه فوقه سقف: ارسمي طريق مايل تتدحرج عليه الكورة لين توصل له
    static LevelData AlarmLevel()
    {
        var level = new LevelData
        {
            title = "المنبه",
            theme = LevelTheme.CandySunset(),
            subtitle = "القطاوة نايمة بالطريق! ارمي كورة على زر المنبه عشان يرن وتصحى",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(45f, 0f, 0f),
            ink = 30f,
            waitForBridge = true,
            blocks =
            {
                Page(-7f, 24f),
                Page(27f, 48f),
            },
            alarms =
            {
                new Vector4(6f, 10f, 3f, 0f),
                new Vector4(16f, 20.5f, 4.4f, 0f),
                new Vector4(34f, 38.5f, 3f, 1f),
            },
        };
        Oval(level, 10f, 5f, new Color(0.95f, 0.3f, 0.3f));
        // آخر منبه: طريق مايل منقط من اليمين للمنبه
        level.hints.Add(new HintLine(new Vector3(43f, 4.4f, 0f), new Vector3(39.7f, 3.3f, 0f), new Color(0.95f, 0.3f, 0.3f)));
        return level;
    }

    // الليل والنهار (نهاية فصل الوقت): المسي قرص الشمس والقمر بخط يتغير الوقت.
    // الصفحات الصفرا بالنهار بس، والزرقا بالليل بس، والحجارة دايم. والخطوط تختفي إذا تغير الوقت
    static LevelData DayNightLevel()
    {
        return new LevelData
        {
            title = "الليل والنهار",
            theme = LevelTheme.BalloonSky(),
            subtitle = "المسي الشمس والقمر بخط عشان يتغير الوقت... كل صفحة لها وقتها",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(40f, 0f, 0f),
            ink = 16f,
            blocks =
            {
                Page(-7f, 4.5f),
                Page(14f, 20f),
                Page(32f, 44f),
            },
            // من x، لـ x، سطحها، 0 نهار / 1 ليل
            dayPads =
            {
                new Vector4(4.6f, 8.1f, 0f, 0f),
                new Vector4(9.9f, 13.9f, 0f, 1f),
                new Vector4(20.1f, 22.6f, 0f, 1f),
                new Vector4(24.4f, 26.6f, 0f, 0f),
                new Vector4(28.4f, 31.9f, 0f, 1f),
            },
            // حجارة بين الصفحات (دايم موجودة)
            pegs =
            {
                new Vector3(9f, 0f, 1.8f),
                new Vector3(23.5f, 0f, 1.8f),
                new Vector3(27.5f, 0f, 1.8f),
            },
        };
    }

    // صفحة البالونات: الشكل المغلق يصير سلة تحت بالونة.
    // 1) بالونة بسيطة فوق جدار  2) دبابيس معلقة: جسر صغير أول، وبعدين البالونة بعيد عن الدبابيس
    // 3) مروحة كبيرة تدف البالونة فوق حفرة طويلة
    static LevelData BalloonLevel()
    {
        var level = new LevelData
        {
            title = "صفحة البالونات",
            theme = LevelTheme.BalloonSky(),
            subtitle = "ارسمي دايرة تصير بالونة... وانتبهي من الدبابيس!",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(51f, 0f, 0f),
            ink = 34f,
            balloonInk = true,
            blocks =
            {
                Page(-7f, 3f),              // حفرة: بالونة لفوق
                Wall(6f, 13f, 3f),          // حفرة عريضة تحت دبابيس
                Wall(18.5f, 24f, 6.5f),     // حفرة طويلة: المروحة تدفك
                Page(33f, 40f, 6.5f),       // حفرة: جسر لتحت
                Page(43f, 54f),
            },
            pins = { new Vector3(13f, 15.3f, 7.3f) },
            fans = { new Vector3(24f, 33f, 6.5f) },
        };
        Oval(level, 4.5f, -0.3f, new Color(1f, 0.45f, 0.5f));
        level.hints.Add(new HintLine(new Vector3(12.8f, 3.2f, 0f), new Vector3(15.5f, 3.2f, 0f), new Color(0.4f, 0.6f, 0.95f)));
        Oval(level, 16.95f, 2.75f, new Color(0.4f, 0.6f, 0.95f));
        Oval(level, 25.3f, 6.15f, new Color(1f, 0.7f, 0.2f));
        return level;
    }

    // بالونة منقطة (تلميح وين ترسمين)
    static void Oval(LevelData level, float cx, float cy, Color c)
    {
        for (int i = 0; i < 10; i++)
        {
            float a0 = i / 10f * Mathf.PI * 2f, a1 = (i + 1) / 10f * Mathf.PI * 2f;
            level.hints.Add(new HintLine(new Vector3(cx + Mathf.Cos(a0) * 1.15f, cy + Mathf.Sin(a0) * 0.42f, 0f),
                                         new Vector3(cx + Mathf.Cos(a1) * 1.15f, cy + Mathf.Sin(a1) * 0.42f, 0f), c));
        }
    }

    // صفحة النار: حفر نار تشتغل وتطفي، وتنينين على صخور طايرة ينفخون نار مايلة.
    // الجسر فوق الحفرة أول ما تنطفي، والدرع (خط فوق راسها) يصد نفخة التنين
    static LevelData FireLevel()
    {
        var level = new LevelData
        {
            title = "صفحة النار",
            theme = LevelTheme.Fire(),
            subtitle = "التنين ينفخ نار! ارسمي فوق راسك درع يصدها",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(49f, 0f, 0f),
            ink = 32f,
            blocks =
            {
                Page(-7f, 4f),
                Page(7f, 24f),
                Page(27.5f, 38f),
                Page(41.5f, 52f),
            },
            // من x، لـ x، كل كم ثانية تشتعل، تأخير
            firePits =
            {
                new Vector4(4f, 7f, 4.6f, 0f),
                new Vector4(24f, 27.5f, 4.4f, 1.5f),
                new Vector4(38f, 41.5f, 5f, 0.8f),
            },
            dragons =
            {
                new Vector4(21f, 4.8f, 13f, 0f),
                new Vector4(37.5f, 5.2f, 31f, 2.1f),
            },
        };
        level.hints.Add(new HintLine(new Vector3(14f, 2.6f, 0f), new Vector3(19.6f, 2.6f, 0f), new Color(1f, 0.6f, 0.2f)));
        level.hints.Add(new HintLine(new Vector3(31.2f, 2.6f, 0f), new Vector3(36f, 2.6f, 0f), new Color(1f, 0.6f, 0.2f)));
        return level;
    }

    // صفحة البوابات: 1) البوابة تطلعك فوق جدار  2) ارسمي كورة فوق بوابة طايرة: تطلع ورا الباب وتطيح على الزر
    // 3) بوابة تطلعك فوق حفرة (جهزي جسر)  4) نفس الكورة، بس البوابة البرتقالية تمشي: انتبهي للوقت
    static LevelData PortalLevel()
    {
        var level = new LevelData
        {
            title = "صفحة البوابات",
            theme = LevelTheme.Mystic(),
            subtitle = "ارسمي كورة فوق البوابة الزرقا... تطيح على الزر وتفتح الباب!",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(58f, 0f, 0f),
            ink = 26f,
            blocks =
            {
                Page(-7f, 8f),
                Wall(10f, 18f, 4.5f),       // البوابة الأولى تطلعك فوقه
                Page(18f, 35f),
                Page(41f, 60f),             // البوابة الثالثة تطلعك فوق الحفرة اللي قبلها
            },
            // (دخول x، دخول y، خروج x، خروج y)
            portals =
            {
                new Vector4(6.5f, 0f, 11.5f, 4.5f),
                new Vector4(23f, 2f, 31.7f, 3.6f),      // للكورة (طايرة فوق راس رويدا)
                new Vector4(34.2f, 0f, 38f, 1f),
                new Vector4(44f, 2f, 49.5f, 3.6f),      // للكورة، والخروج يمشي
            },
            portalMoves = { new Vector3(3f, 5f, 4f) },
            gates =
            {
                new Vector3(30f, 32.3f, 3f),
                new Vector3(48.5f, 54.2f, 3f),
            },
            hints = { new HintLine(new Vector3(37.2f, 0.9f, 0f), new Vector3(41.4f, 0.1f, 0f), new Color(1f, 0.6f, 0.25f)) },
        };
        Oval(level, 23f, 4.7f, new Color(0.4f, 0.8f, 1f));
        Oval(level, 44f, 4.7f, new Color(0.4f, 0.8f, 1f));
        return level;
    }

    // صفحة الوادي: أعمدة حجر بين الحفر، والخط الطويل ينكسر. وطير ورق كبير يصيح وينقض ويخطف الخطوط:
    // رويدا تستنى لين يعدي، وأنتِ ترسمين الجسور وراه بسرعة
    static LevelData CanyonLevel()
    {
        return new LevelData
        {
            title = "صفحة الوادي",
            theme = LevelTheme.Canyon(),
            subtitle = "طير الورق يخطف الخطوط! خليه يعدي وارسمي وراه",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(41f, 0f, 0f),
            ink = 32f,
            maxSpan = 3.4f,
            blocks =
            {
                Page(-7f, 4f),
                Page(10f, 15f),
                Page(23f, 28f),
                Page(34f, 44f),
            },
            // أعمدة حجر: x، سطحها، عرضها
            pegs =
            {
                new Vector3(7f, -0.2f, 1.2f),
                new Vector3(17.6f, 0.5f, 1.2f),
                new Vector3(20.4f, 0.1f, 1.2f),
                new Vector3(31f, -0.5f, 1.2f),
            },
            birds =
            {
                new Vector4(15f, 23f, -0.8f, 0f),
                new Vector4(28f, 34f, -0.8f, 4f),
            },
        };
    }

    // نهاية الجزء الأول: ممحاة عملاقة تلحقك وتمسح كل شي وراك، وفيها شوي من كل شي تعلمتيه:
    // جسر فوق حفرة، وحش شخابيط، جدار عالي (أحمر ينطط)، جسر مايل، وأرض تختفي
    static LevelData EraserFinale()
    {
        return new LevelData
        {
            title = "الممحاة الكبيرة",
            theme = LevelTheme.EraserAfternoon(),
            subtitle = "الممحاة تلحقك وتمسح كل شي وراك... لا توقفين!",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(47f, 0f, 0f),
            ink = 30f,
            colors = true,
            eraserChase = true,
            blocks =
            {
                Page(-7f, 4f),              // حفرة 3
                Page(7f, 17f),              // وحش شخابيط
                Wall(17f, 23f, 2.5f),       // جدار عالي: أحمر ينطط
                Page(26.5f, 50f),           // حفرة مايلة لتحت، وبعدين أرض تختفي
            },
            monsters = { new MonsterSpawn(ScribbleMonster.Kind.FuzzyBall, 11f, 8.5f, 13f) },
            vanishTiles =
            {
                new Vector3(31f, 34f, 4.5f),
                new Vector3(37f, 40f, 4f),
            },
            hints =
            {
                new HintLine(new Vector3(3.6f, 0.12f, 0f), new Vector3(7.4f, 0.12f, 0f), new Color(0.3f, 0.3f, 0.35f)),
                new HintLine(new Vector3(15.4f, 0.18f, 0f), new Vector3(16.7f, 0.18f, 0f), InkColorEffect.ColorOf(InkColor.Red)),
            },
        };
    }

    // صفحة اليراعات: ظلام، وزيت الفانوس يخلص شوي شوي. الظلال تقرب لما يضعف الضو.
    // اليراعات تعبي الفانوس: اللي على الطريق تاخذها رويدا، واللي فوق ارسمي لها خط يوصلها
    static LevelData FirefliesLevel()
    {
        return new LevelData
        {
            title = "صفحة اليراعات",
            theme = LevelTheme.Fireflies(),
            subtitle = "الفانوس يخلص! ارسمي خط يلمس اليراعات تعبيه، والظلال تخاف من الضو",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(43f, 0f, 0f),
            ink = 26f,
            darkness = true,
            blocks =
            {
                Page(-7f, 5f),
                Page(8f, 14f, 0.6f),
                Page(17.5f, 26f),
                Page(29f, 33f, -0.5f),
                Page(36f, 46f),
            },
            swarms =
            {
                new Vector3(9.5f, 1.9f, 0f),    // على الطريق
                new Vector3(21f, 3.6f, 0f),     // فوق: يبي خط
                new Vector3(31f, 3.1f, 0f),     // فوق: يبي خط
            },
            shadows =
            {
                new Vector3(22.5f, 1.5f, 0f),
                new Vector3(31.5f, 1.2f, 0f),
                new Vector3(40.5f, 1.6f, 0f),
            },
            hints =
            {
                new HintLine(new Vector3(20.3f, 2.9f, 0f), new Vector3(21.7f, 2.9f, 0f), new Color(0.8f, 1f, 0.4f)),
                new HintLine(new Vector3(30.3f, 2.4f, 0f), new Vector3(31.7f, 2.4f, 0f), new Color(0.8f, 1f, 0.4f)),
            },
        };
    }

    static LevelData FoldingPage()
    {
        return new LevelData
        {
            title = "الصفحة المتلاشية",
            theme = LevelTheme.Origami(),
            subtitle = "الأرض تختفي قدامك... ارسمي خط وعدّي!",
            spawn = new Vector3(-5f, 1f, 0f),
            door = new Vector3(51f, 0f, 0f),
            ink = 34f,
            blocks =
            {
                Page(-7f, 18f),
                Page(21f, 36f),
                Page(38.5f, 54f),
            },
            // من x، لـ x، وتبدا تختفي إذا صارت رويدا على بعد كذا منها
            vanishTiles =
            {
                new Vector3(3f, 6f, 5f),
                new Vector3(10f, 13.5f, 4.5f),
                new Vector3(27f, 30f, 4.3f),
                new Vector3(31.5f, 34.5f, 3.8f),
                new Vector3(44.5f, 47.5f, 3.6f),
            },
            hints = { new HintLine(new Vector3(2.4f, 0.12f, 0f), new Vector3(6.6f, 0.12f, 0f), new Color(0.3f, 0.3f, 0.35f)) },
        };
    }

    // صفحات الشخابيط: وحش واحد، وبعدين وحشين قبل الباب
    static LevelData ScribbleLevel1()
    {
        return new LevelData
        {
            title = "صفحات الشخابيط",
            theme = LevelTheme.CrayonDay(),
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
            theme = LevelTheme.CandySunset(),
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
            theme = LevelTheme.KraftDawn(),
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
            theme = LevelTheme.GraphDusk(),
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
            theme = LevelTheme.RuledRain(),
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
            theme = LevelTheme.Watercolor(),
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
