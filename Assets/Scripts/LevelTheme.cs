using UnityEngine;

// شكل المرحلة: لون الورق ونقشته، السما، الإضاءة، الأغراض، والأشياء الطايرة. كل مرحلة لها عالم مختلف
public enum PaperPattern { Blank, Ruled, Grid, Dots, Kraft, Watercolor, Parchment, Origami, Splats }
public enum FloatKind { Paper, Leaves, Crayons, Petals, Sparkles, Cranes, EraserBits, InkDrops, Snow, Balloons, Embers, Fireflies }
public enum PropSet { Stationery, Crayons, Paint, Erasers, Ink, Origami, Snow, Space }

public class LevelTheme
{
    public string name;

    // الورق
    public Color paper = new Color(0.96f, 0.95f, 0.93f);
    public PaperPattern pattern = PaperPattern.Blank;
    public Color patternColor = new Color(0.7f, 0.7f, 0.75f);
    public Color pageEdge = Color.white;
    public Color[] pageColors;            // للأوريغامي: كل صفحة بلون
    public bool colorfulDoodles;          // شخابيط بألوان الشمع بدل الرصاص
    public Color doodleColor = new Color(0.3f, 0.3f, 0.34f);
    public Color pencil = new Color(0.33f, 0.33f, 0.36f);
    public bool rings = true;
    public bool snowCaps;                 // طبقة ثلج فوق الصفحات
    public bool glitter;                  // الصفحات تلمع بقليتر
    public Color ringColor = new Color(0.2f, 0.2f, 0.22f);
    public Color seaColor = new Color(0.2f, 0.55f, 0.85f);  // لون البحر

    // السما
    public Color skyTop = new Color(0.30f, 0.24f, 0.42f);
    public Color skyMid = new Color(0.16f, 0.15f, 0.30f);
    public Color skyBottom = new Color(0.05f, 0.05f, 0.11f);
    public bool sun;                      // شمس بدل الهلال
    public bool showMoon = true;
    public Color moonColor = new Color(2.4f, 2.2f, 1.75f);
    public float stars = 1f;              // كمية النجوم (0 لـ 1)
    public Color cloudTint = new Color(0.55f, 0.51f, 0.8f);
    public float cloudAlpha = 0.5f;

    // الإضاءة
    public Color sunLight = new Color(1f, 0.9f, 0.78f);
    public float sunIntensity = 1.35f;
    public Vector3 sunAngles = new Vector3(42f, -40f, 0f);
    public Color ambientSky = new Color(0.52f, 0.5f, 0.68f);
    public Color ambientEquator = new Color(0.38f, 0.36f, 0.5f);
    public Color ambientGround = new Color(0.16f, 0.15f, 0.24f);
    public Color fog = new Color(0.07f, 0.07f, 0.14f);

    // الأغراض والأشياء الطايرة
    public PropSet props = PropSet.Stationery;
    public FloatKind floating = FloatKind.Paper;
    public Color[] floatColors = { new Color(0.9f, 0.89f, 0.92f) };

    static readonly Color[] Crayons =
    {
        new Color(0.95f, 0.3f, 0.3f), new Color(1f, 0.65f, 0.2f), new Color(1f, 0.85f, 0.25f),
        new Color(0.35f, 0.8f, 0.4f), new Color(0.3f, 0.6f, 1f), new Color(0.65f, 0.4f, 0.9f),
    };

    // ---------- الأشكال الجاهزة ----------

    public static LevelTheme Classic() => new LevelTheme { name = "Classic" };

    public static LevelTheme RuledRain() => new LevelTheme
    {
        name = "RuledRain",
        paper = new Color(0.95f, 0.96f, 1f), pattern = PaperPattern.Ruled, patternColor = new Color(0.55f, 0.7f, 0.95f),
        pageEdge = new Color(0.9f, 0.93f, 1f), pencil = new Color(0.25f, 0.32f, 0.5f),
        skyTop = new Color(0.32f, 0.38f, 0.5f), skyMid = new Color(0.18f, 0.22f, 0.32f), skyBottom = new Color(0.06f, 0.08f, 0.13f),
        cloudTint = new Color(0.5f, 0.55f, 0.65f), stars = 0.3f, ringColor = new Color(0.55f, 0.6f, 0.7f),
    };

    public static LevelTheme Watercolor() => new LevelTheme
    {
        name = "Watercolor",
        paper = new Color(0.94f, 0.97f, 0.95f), pattern = PaperPattern.Watercolor, patternColor = new Color(0.35f, 0.65f, 0.75f),
        pageEdge = new Color(0.85f, 0.95f, 0.92f), pencil = new Color(0.2f, 0.4f, 0.42f),
        skyTop = new Color(0.2f, 0.42f, 0.45f), skyMid = new Color(0.1f, 0.25f, 0.3f), skyBottom = new Color(0.03f, 0.08f, 0.1f),
        cloudTint = new Color(0.4f, 0.6f, 0.62f), stars = 0.2f, props = PropSet.Paint, ringColor = new Color(0.2f, 0.4f, 0.4f),
    };

    public static LevelTheme KraftDawn() => new LevelTheme
    {
        name = "KraftDawn",
        paper = new Color(0.84f, 0.7f, 0.52f), pattern = PaperPattern.Kraft, patternColor = new Color(0.55f, 0.4f, 0.25f),
        pageEdge = new Color(0.78f, 0.63f, 0.45f), pencil = new Color(0.35f, 0.22f, 0.12f), doodleColor = new Color(0.32f, 0.2f, 0.1f),
        skyTop = new Color(0.98f, 0.62f, 0.5f), skyMid = new Color(1f, 0.8f, 0.62f), skyBottom = new Color(0.55f, 0.45f, 0.7f),
        sun = true, moonColor = new Color(2.6f, 1.9f, 1.2f), stars = 0f, cloudTint = new Color(1f, 0.8f, 0.75f), cloudAlpha = 0.75f,
        sunLight = new Color(1f, 0.78f, 0.6f), sunIntensity = 1.5f, sunAngles = new Vector3(25f, -55f, 0f),
        ambientSky = new Color(0.95f, 0.78f, 0.7f), ambientEquator = new Color(0.8f, 0.62f, 0.55f), ambientGround = new Color(0.4f, 0.3f, 0.35f),
        fog = new Color(0.85f, 0.65f, 0.6f), floating = FloatKind.Leaves,
        floatColors = new[] { new Color(0.95f, 0.55f, 0.2f), new Color(0.85f, 0.35f, 0.2f), new Color(0.95f, 0.75f, 0.3f) },
        ringColor = new Color(0.6f, 0.4f, 0.2f),
    };

    public static LevelTheme GraphDusk() => new LevelTheme
    {
        name = "GraphDusk",
        paper = new Color(0.93f, 0.96f, 0.92f), pattern = PaperPattern.Grid, patternColor = new Color(0.5f, 0.75f, 0.55f),
        pageEdge = new Color(0.88f, 0.95f, 0.88f), pencil = new Color(0.2f, 0.35f, 0.25f),
        skyTop = new Color(0.35f, 0.14f, 0.32f), skyMid = new Color(0.78f, 0.35f, 0.33f), skyBottom = new Color(0.18f, 0.08f, 0.22f),
        stars = 0.4f, cloudTint = new Color(0.7f, 0.35f, 0.45f), cloudAlpha = 0.7f,
        sunLight = new Color(1f, 0.6f, 0.45f), sunIntensity = 1.2f, sunAngles = new Vector3(18f, -60f, 0f),
        ambientSky = new Color(0.7f, 0.42f, 0.5f), ambientEquator = new Color(0.5f, 0.3f, 0.38f), ambientGround = new Color(0.2f, 0.1f, 0.18f),
        fog = new Color(0.3f, 0.12f, 0.22f), ringColor = new Color(0.25f, 0.3f, 0.25f),
    };

    public static LevelTheme CrayonDay() => new LevelTheme
    {
        name = "CrayonDay",
        paper = new Color(1f, 1f, 0.98f), colorfulDoodles = true, pencil = new Color(0.25f, 0.25f, 0.3f),
        skyTop = new Color(0.35f, 0.6f, 0.95f), skyMid = new Color(0.6f, 0.8f, 1f), skyBottom = new Color(0.9f, 0.95f, 1f),
        sun = true, moonColor = new Color(2.8f, 2.4f, 1.4f), stars = 0f, cloudTint = new Color(1.1f, 1.1f, 1.15f), cloudAlpha = 0.9f,
        sunLight = new Color(1f, 0.97f, 0.9f), sunIntensity = 1.6f, sunAngles = new Vector3(50f, -30f, 0f),
        ambientSky = new Color(0.75f, 0.85f, 1f), ambientEquator = new Color(0.65f, 0.7f, 0.8f), ambientGround = new Color(0.35f, 0.38f, 0.45f),
        fog = new Color(0.75f, 0.87f, 1f), props = PropSet.Crayons, floating = FloatKind.Crayons, floatColors = Crayons,
        ringColor = new Color(0.9f, 0.3f, 0.35f),
    };

    public static LevelTheme CandySunset() => new LevelTheme
    {
        name = "CandySunset",
        paper = new Color(1f, 0.9f, 0.94f), pattern = PaperPattern.Dots, patternColor = new Color(0.95f, 0.6f, 0.75f), colorfulDoodles = true,
        pageEdge = new Color(1f, 0.85f, 0.9f), pencil = new Color(0.55f, 0.25f, 0.4f),
        skyTop = new Color(0.5f, 0.3f, 0.65f), skyMid = new Color(0.95f, 0.55f, 0.7f), skyBottom = new Color(0.35f, 0.2f, 0.45f),
        sun = true, moonColor = new Color(2.6f, 1.6f, 1.8f), stars = 0.2f, cloudTint = new Color(1f, 0.7f, 0.85f), cloudAlpha = 0.75f,
        sunLight = new Color(1f, 0.75f, 0.8f), sunIntensity = 1.3f, sunAngles = new Vector3(22f, -50f, 0f),
        ambientSky = new Color(0.9f, 0.65f, 0.8f), ambientEquator = new Color(0.7f, 0.5f, 0.65f), ambientGround = new Color(0.3f, 0.2f, 0.35f),
        fog = new Color(0.55f, 0.3f, 0.5f), props = PropSet.Crayons, floating = FloatKind.Petals,
        floatColors = new[] { new Color(1f, 0.7f, 0.8f), new Color(1f, 0.85f, 0.9f), new Color(0.95f, 0.55f, 0.7f) },
        ringColor = new Color(0.85f, 0.45f, 0.65f),
    };

    public static LevelTheme PastelPaint() => new LevelTheme
    {
        name = "PastelPaint",
        paper = new Color(0.99f, 0.98f, 0.96f), pattern = PaperPattern.Splats, colorfulDoodles = true,
        skyTop = new Color(0.2f, 0.75f, 0.85f), skyMid = new Color(1f, 0.6f, 0.75f), skyBottom = new Color(1f, 0.9f, 0.45f),
        sun = true, moonColor = new Color(2.6f, 2.3f, 1.6f), stars = 0f, cloudTint = new Color(1.1f, 1.05f, 1.1f), cloudAlpha = 0.8f,
        sunLight = new Color(1f, 0.95f, 0.9f), sunIntensity = 1.5f, sunAngles = new Vector3(45f, -35f, 0f),
        ambientSky = new Color(0.85f, 0.82f, 0.95f), ambientEquator = new Color(0.75f, 0.72f, 0.78f), ambientGround = new Color(0.4f, 0.4f, 0.45f),
        fog = new Color(0.95f, 0.75f, 0.7f), props = PropSet.Paint, floating = FloatKind.Sparkles, floatColors = Crayons,
        ringColor = new Color(0.6f, 0.55f, 0.85f),
    };

    public static LevelTheme EraserAfternoon() => new LevelTheme
    {
        name = "EraserAfternoon",
        paper = new Color(0.88f, 0.97f, 0.92f), pattern = PaperPattern.Dots, patternColor = new Color(0.45f, 0.75f, 0.6f), pageEdge = new Color(0.85f, 0.95f, 0.9f), pencil = new Color(0.2f, 0.4f, 0.35f),
        skyTop = new Color(0.25f, 0.6f, 0.6f), skyMid = new Color(0.6f, 0.88f, 0.8f), skyBottom = new Color(0.95f, 0.97f, 0.8f),
        sun = true, moonColor = new Color(2.7f, 2.0f, 1.3f), stars = 0f, cloudTint = new Color(1f, 1f, 0.97f), cloudAlpha = 0.7f,
        sunLight = new Color(1f, 0.95f, 0.8f), sunIntensity = 1.5f, sunAngles = new Vector3(30f, -45f, 0f),
        ambientSky = new Color(0.75f, 0.92f, 0.88f), ambientEquator = new Color(0.62f, 0.75f, 0.7f), ambientGround = new Color(0.28f, 0.38f, 0.35f),
        fog = new Color(0.65f, 0.88f, 0.82f), props = PropSet.Crayons, floating = FloatKind.EraserBits,
        floatColors = new[] { new Color(0.95f, 0.62f, 0.66f), new Color(0.97f, 0.96f, 0.95f) },
        ringColor = new Color(0.3f, 0.55f, 0.5f),
    };

    public static LevelTheme Cosmic() => new LevelTheme
    {
        name = "Cosmic",
        paper = new Color(0.85f, 0.9f, 1f), pattern = PaperPattern.Dots, patternColor = new Color(0.5f, 0.6f, 0.95f),
        pageEdge = new Color(0.8f, 0.85f, 1f), pencil = new Color(0.25f, 0.25f, 0.55f), doodleColor = new Color(0.3f, 0.3f, 0.6f),
        skyTop = new Color(0.25f, 0.08f, 0.4f), skyMid = new Color(0.1f, 0.05f, 0.25f), skyBottom = new Color(0.02f, 0.06f, 0.15f),
        moonColor = new Color(1.6f, 2.0f, 2.6f), stars = 1f, cloudTint = new Color(0.5f, 0.3f, 0.75f), cloudAlpha = 0.35f,
        sunLight = new Color(0.75f, 0.8f, 1f), sunIntensity = 1.2f,
        ambientSky = new Color(0.45f, 0.45f, 0.75f), ambientEquator = new Color(0.32f, 0.3f, 0.55f), ambientGround = new Color(0.1f, 0.1f, 0.25f),
        fog = new Color(0.05f, 0.03f, 0.15f), floating = FloatKind.Sparkles,
        floatColors = new[] { new Color(0.8f, 0.85f, 1f), new Color(1f, 0.9f, 0.6f), new Color(0.75f, 0.6f, 1f) },
        ringColor = new Color(0.75f, 0.78f, 0.9f),
    };

    public static LevelTheme InkParchment() => new LevelTheme
    {
        name = "InkParchment",
        paper = new Color(0.93f, 0.85f, 0.67f), pattern = PaperPattern.Parchment, patternColor = new Color(0.6f, 0.45f, 0.25f),
        pageEdge = new Color(0.85f, 0.75f, 0.55f), pencil = new Color(0.3f, 0.2f, 0.12f), doodleColor = new Color(0.25f, 0.18f, 0.12f),
        skyTop = new Color(0.12f, 0.12f, 0.28f), skyMid = new Color(0.06f, 0.07f, 0.18f), skyBottom = new Color(0.02f, 0.02f, 0.07f),
        moonColor = new Color(2.2f, 2.0f, 1.5f), stars = 0.6f, cloudTint = new Color(0.25f, 0.25f, 0.45f), cloudAlpha = 0.6f,
        sunLight = new Color(1f, 0.85f, 0.65f), sunIntensity = 1.3f,
        ambientSky = new Color(0.5f, 0.45f, 0.55f), ambientEquator = new Color(0.38f, 0.32f, 0.38f), ambientGround = new Color(0.12f, 0.1f, 0.15f),
        fog = new Color(0.04f, 0.04f, 0.1f), props = PropSet.Ink, floating = FloatKind.InkDrops,
        floatColors = new[] { new Color(0.08f, 0.07f, 0.18f) }, ringColor = new Color(0.55f, 0.4f, 0.2f),
    };

    public static LevelTheme Origami() => new LevelTheme
    {
        name = "Origami",
        pattern = PaperPattern.Origami, rings = false, pencil = new Color(0.3f, 0.25f, 0.25f),
        pageColors = new[]
        {
            new Color(0.95f, 0.4f, 0.4f), new Color(1f, 0.8f, 0.3f), new Color(0.35f, 0.75f, 0.75f),
            new Color(0.6f, 0.45f, 0.85f), new Color(1f, 0.6f, 0.35f), new Color(0.45f, 0.75f, 0.45f),
        },
        skyTop = new Color(0.98f, 0.92f, 0.8f), skyMid = new Color(0.96f, 0.85f, 0.75f), skyBottom = new Color(0.85f, 0.72f, 0.7f),
        sun = true, moonColor = new Color(2.6f, 2.2f, 1.6f), stars = 0f, cloudTint = new Color(1.1f, 1f, 0.95f), cloudAlpha = 0.6f,
        sunLight = new Color(1f, 0.93f, 0.85f), sunIntensity = 1.4f, sunAngles = new Vector3(40f, -40f, 0f),
        ambientSky = new Color(0.9f, 0.85f, 0.8f), ambientEquator = new Color(0.78f, 0.7f, 0.65f), ambientGround = new Color(0.4f, 0.35f, 0.35f),
        fog = new Color(0.92f, 0.85f, 0.78f), props = PropSet.Origami, floating = FloatKind.Cranes, floatColors = Crayons,
    };

    // ألوان شمع للشخابيط الملونة
    public Color RandomDoodleColor(System.Random rng)
    {
        return colorfulDoodles ? Crayons[rng.Next(Crayons.Length)] : doodleColor;
    }

    // لون الصفحة رقم i (للأوريغامي كل صفحة بلون)
    // ثلج الصباح: سما زرقا صافية وشمس دافية، ورق أبيض مزرق، ثلج فوق الصفحات وندف تنزل
    public static LevelTheme SnowMorning() => new LevelTheme
    {
        name = "SnowMorning",
        paper = new Color(0.95f, 0.97f, 1f), pattern = PaperPattern.Dots, patternColor = new Color(0.65f, 0.78f, 0.95f),
        pageEdge = new Color(0.9f, 0.95f, 1f), pencil = new Color(0.3f, 0.42f, 0.6f), doodleColor = new Color(0.4f, 0.55f, 0.75f),
        skyTop = new Color(0.35f, 0.62f, 0.95f), skyMid = new Color(0.68f, 0.85f, 1f), skyBottom = new Color(1f, 0.93f, 0.85f),
        sun = true, moonColor = new Color(2.8f, 2.5f, 1.9f), stars = 0f, cloudTint = new Color(1.1f, 1.12f, 1.15f), cloudAlpha = 0.85f,
        sunLight = new Color(1f, 0.95f, 0.86f), sunIntensity = 1.55f, sunAngles = new Vector3(28f, -45f, 0f),
        ambientSky = new Color(0.8f, 0.88f, 1f), ambientEquator = new Color(0.68f, 0.74f, 0.85f), ambientGround = new Color(0.42f, 0.47f, 0.58f),
        fog = new Color(0.82f, 0.9f, 1f), props = PropSet.Snow, floating = FloatKind.Snow,
        floatColors = new[] { Color.white, new Color(0.92f, 0.96f, 1f) },
        ringColor = new Color(0.62f, 0.7f, 0.82f), snowCaps = true,
    };

    // الفضاء: ورق كحلي بنقاط بيضا مثل النجوم، خطوط بيضا مثل الطباشير، سما بنفسجية وسحب سديم وردية
    public static LevelTheme Space() => new LevelTheme
    {
        name = "Space",
        paper = new Color(0.17f, 0.18f, 0.33f), pattern = PaperPattern.Dots, patternColor = new Color(0.85f, 0.88f, 1f),
        pageEdge = new Color(0.35f, 0.37f, 0.58f), pencil = new Color(0.88f, 0.9f, 1f), doodleColor = new Color(0.78f, 0.82f, 1f),
        skyTop = new Color(0.01f, 0.01f, 0.05f), skyMid = new Color(0.07f, 0.03f, 0.15f), skyBottom = new Color(0.2f, 0.06f, 0.28f),
        showMoon = false, stars = 1f, cloudTint = new Color(0.85f, 0.3f, 0.9f), cloudAlpha = 0.45f,
        sunLight = new Color(0.85f, 0.88f, 1f), sunIntensity = 1.35f, sunAngles = new Vector3(35f, -30f, 0f),
        ambientSky = new Color(0.5f, 0.45f, 0.75f), ambientEquator = new Color(0.32f, 0.26f, 0.52f), ambientGround = new Color(0.1f, 0.08f, 0.2f),
        fog = new Color(0.04f, 0.02f, 0.09f), props = PropSet.Space, floating = FloatKind.Sparkles,
        floatColors = new[] { new Color(1f, 0.9f, 0.6f), new Color(0.6f, 0.85f, 1f), new Color(1f, 0.6f, 0.9f), Color.white },
        ringColor = new Color(0.75f, 0.78f, 0.95f),
    };

    // الصفحة الأخيرة: وردي وسعيد، ورق يلمع بقليتر، سما وردية بنفسجية، ولمعات طايرة
    public static LevelTheme LastPage() => new LevelTheme
    {
        name = "LastPage",
        paper = new Color(1f, 0.9f, 0.94f), pattern = PaperPattern.Dots, patternColor = new Color(1f, 0.62f, 0.78f), colorfulDoodles = true,
        pageEdge = new Color(1f, 0.85f, 0.9f), pencil = new Color(0.85f, 0.4f, 0.6f), doodleColor = new Color(0.9f, 0.5f, 0.7f),
        skyTop = new Color(0.88f, 0.62f, 0.92f), skyMid = new Color(1f, 0.78f, 0.9f), skyBottom = new Color(1f, 0.92f, 0.84f),
        sun = true, moonColor = new Color(2.8f, 2.3f, 2.1f), stars = 0.35f, cloudTint = new Color(1.15f, 1.02f, 1.12f), cloudAlpha = 0.8f,
        sunLight = new Color(1f, 0.92f, 0.95f), sunIntensity = 1.5f, sunAngles = new Vector3(30f, -40f, 0f),
        ambientSky = new Color(1f, 0.85f, 0.93f), ambientEquator = new Color(0.86f, 0.7f, 0.8f), ambientGround = new Color(0.45f, 0.35f, 0.42f),
        fog = new Color(1f, 0.86f, 0.93f), props = PropSet.Crayons, floating = FloatKind.Sparkles,
        floatColors = new[] { new Color(1f, 0.6f, 0.8f), new Color(1f, 0.85f, 0.4f), Color.white, new Color(0.8f, 0.65f, 1f) },
        ringColor = new Color(0.95f, 0.75f, 0.4f), glitter = true,
    };

    // البالونات: نهار صافي، ورق كريمي بنقاط ملونة، وبالونات طايرة
    public static LevelTheme BalloonSky() => new LevelTheme
    {
        name = "BalloonSky",
        paper = new Color(1f, 0.97f, 0.88f), pattern = PaperPattern.Dots, patternColor = new Color(0.95f, 0.6f, 0.5f), colorfulDoodles = true,
        pageEdge = new Color(1f, 0.93f, 0.85f), pencil = new Color(0.6f, 0.35f, 0.3f),
        skyTop = new Color(0.35f, 0.7f, 0.95f), skyMid = new Color(0.65f, 0.87f, 1f), skyBottom = new Color(1f, 0.95f, 0.8f),
        sun = true, moonColor = new Color(2.8f, 2.5f, 1.8f), stars = 0f, cloudTint = new Color(1.12f, 1.12f, 1.12f), cloudAlpha = 0.9f,
        sunLight = new Color(1f, 0.96f, 0.88f), sunIntensity = 1.5f, sunAngles = new Vector3(45f, -35f, 0f),
        ambientSky = new Color(0.85f, 0.9f, 1f), ambientEquator = new Color(0.72f, 0.72f, 0.75f), ambientGround = new Color(0.4f, 0.38f, 0.38f),
        fog = new Color(0.82f, 0.9f, 1f), props = PropSet.Crayons, floating = FloatKind.Balloons,
        floatColors = new[] { new Color(1f, 0.4f, 0.45f), new Color(1f, 0.8f, 0.3f), new Color(0.45f, 0.75f, 1f), new Color(0.6f, 0.85f, 0.5f), new Color(0.85f, 0.6f, 1f) },
        ringColor = new Color(0.85f, 0.4f, 0.4f),
    };

    // النار: ورق قديم محروق الأطراف، سما بركان حمرا، وجمر يطلع
    public static LevelTheme Fire() => new LevelTheme
    {
        name = "Fire",
        paper = new Color(0.9f, 0.78f, 0.6f), pattern = PaperPattern.Parchment, patternColor = new Color(0.55f, 0.32f, 0.18f),
        pageEdge = new Color(0.6f, 0.35f, 0.2f), pencil = new Color(0.4f, 0.15f, 0.1f), doodleColor = new Color(0.45f, 0.2f, 0.12f),
        skyTop = new Color(0.15f, 0.03f, 0.05f), skyMid = new Color(0.5f, 0.12f, 0.06f), skyBottom = new Color(0.95f, 0.45f, 0.15f),
        showMoon = false, stars = 0.2f, cloudTint = new Color(0.45f, 0.2f, 0.2f), cloudAlpha = 0.7f,
        sunLight = new Color(1f, 0.6f, 0.35f), sunIntensity = 1.3f, sunAngles = new Vector3(30f, -40f, 0f),
        ambientSky = new Color(0.7f, 0.35f, 0.25f), ambientEquator = new Color(0.5f, 0.25f, 0.18f), ambientGround = new Color(0.2f, 0.08f, 0.05f),
        fog = new Color(0.3f, 0.08f, 0.05f), props = PropSet.Ink, floating = FloatKind.Embers,
        floatColors = new[] { new Color(1f, 0.55f, 0.15f), new Color(1f, 0.8f, 0.3f), new Color(1f, 0.35f, 0.1f) },
        ringColor = new Color(0.35f, 0.25f, 0.2f),
    };

    // البوابات: سحري بنفسجي وفيروزي، ورق شبكة بنفسجي، ولمعات
    public static LevelTheme Mystic() => new LevelTheme
    {
        name = "Mystic",
        paper = new Color(0.93f, 0.9f, 1f), pattern = PaperPattern.Grid, patternColor = new Color(0.7f, 0.6f, 0.95f),
        pageEdge = new Color(0.88f, 0.85f, 1f), pencil = new Color(0.35f, 0.25f, 0.6f), doodleColor = new Color(0.45f, 0.35f, 0.75f),
        skyTop = new Color(0.1f, 0.08f, 0.3f), skyMid = new Color(0.3f, 0.15f, 0.5f), skyBottom = new Color(0.15f, 0.45f, 0.55f),
        moonColor = new Color(1.8f, 2.2f, 2.6f), stars = 0.8f, cloudTint = new Color(0.5f, 0.35f, 0.8f), cloudAlpha = 0.5f,
        sunLight = new Color(0.85f, 0.8f, 1f), sunIntensity = 1.3f,
        ambientSky = new Color(0.6f, 0.55f, 0.85f), ambientEquator = new Color(0.4f, 0.35f, 0.6f), ambientGround = new Color(0.15f, 0.12f, 0.25f),
        fog = new Color(0.12f, 0.08f, 0.25f), props = PropSet.Space, floating = FloatKind.Sparkles,
        floatColors = new[] { new Color(0.7f, 0.5f, 1f), new Color(0.4f, 0.9f, 0.95f), Color.white },
        ringColor = new Color(0.6f, 0.55f, 0.85f),
    };

    // الوادي: ورق رملي، غروب برتقالي، وأوراق شجر تطير
    public static LevelTheme Canyon() => new LevelTheme
    {
        name = "Canyon",
        paper = new Color(0.96f, 0.87f, 0.7f), pattern = PaperPattern.Ruled, patternColor = new Color(0.85f, 0.6f, 0.4f),
        pageEdge = new Color(0.88f, 0.72f, 0.52f), pencil = new Color(0.5f, 0.3f, 0.15f), doodleColor = new Color(0.55f, 0.32f, 0.15f),
        skyTop = new Color(0.4f, 0.55f, 0.85f), skyMid = new Color(0.95f, 0.7f, 0.5f), skyBottom = new Color(1f, 0.85f, 0.6f),
        sun = true, moonColor = new Color(2.8f, 1.9f, 1.1f), stars = 0f, cloudTint = new Color(1f, 0.9f, 0.8f), cloudAlpha = 0.6f,
        sunLight = new Color(1f, 0.8f, 0.6f), sunIntensity = 1.5f, sunAngles = new Vector3(20f, -60f, 0f),
        ambientSky = new Color(0.95f, 0.8f, 0.65f), ambientEquator = new Color(0.75f, 0.6f, 0.5f), ambientGround = new Color(0.35f, 0.25f, 0.2f),
        fog = new Color(0.95f, 0.8f, 0.65f), props = PropSet.Stationery, floating = FloatKind.Leaves,
        floatColors = new[] { new Color(0.85f, 0.55f, 0.3f), new Color(0.7f, 0.4f, 0.2f), new Color(0.9f, 0.7f, 0.35f) },
        ringColor = new Color(0.55f, 0.35f, 0.2f),
    };

    // اليراعات: ظلام غابة، رويدا ماسكة فانوس، وحشرات مضيئة
    public static LevelTheme Fireflies() => new LevelTheme
    {
        name = "Fireflies",
        paper = new Color(0.85f, 0.9f, 0.82f), pattern = PaperPattern.Ruled, patternColor = new Color(0.5f, 0.62f, 0.5f),
        pageEdge = new Color(0.62f, 0.68f, 0.6f), pencil = new Color(0.3f, 0.36f, 0.3f), doodleColor = new Color(0.35f, 0.45f, 0.35f),
        skyTop = new Color(0.01f, 0.02f, 0.04f), skyMid = new Color(0.02f, 0.05f, 0.06f), skyBottom = new Color(0.04f, 0.1f, 0.08f),
        moonColor = new Color(1.2f, 1.35f, 1.2f), stars = 0.6f, cloudTint = new Color(0.1f, 0.15f, 0.15f), cloudAlpha = 0.5f,
        sunLight = new Color(0.4f, 0.5f, 0.6f), sunIntensity = 0.12f,
        ambientSky = new Color(0.06f, 0.07f, 0.09f), ambientEquator = new Color(0.04f, 0.05f, 0.06f), ambientGround = new Color(0.02f, 0.02f, 0.03f),
        fog = new Color(0.01f, 0.02f, 0.03f), props = PropSet.Stationery, floating = FloatKind.Fireflies,
        floatColors = new[] { new Color(0.8f, 1f, 0.4f), new Color(1f, 0.95f, 0.5f) },
        ringColor = new Color(0.3f, 0.32f, 0.3f),
    };

    // البحر: ورق أزرق فاتح، سما صيف صافية، وشمس
    public static LevelTheme Ocean() => new LevelTheme
    {
        name = "Ocean",
        paper = new Color(0.99f, 0.95f, 0.84f), pattern = PaperPattern.Dots, patternColor = new Color(0.55f, 0.75f, 0.9f), colorfulDoodles = true,
        pageEdge = new Color(0.95f, 0.88f, 0.7f), pencil = new Color(0.2f, 0.4f, 0.6f), doodleColor = new Color(0.3f, 0.5f, 0.7f),
        skyTop = new Color(0.2f, 0.55f, 0.95f), skyMid = new Color(0.55f, 0.82f, 1f), skyBottom = new Color(0.9f, 0.97f, 1f),
        sun = true, moonColor = new Color(2.8f, 2.6f, 1.9f), stars = 0f, cloudTint = new Color(1.12f, 1.12f, 1.12f), cloudAlpha = 0.85f,
        sunLight = new Color(1f, 0.97f, 0.9f), sunIntensity = 1.55f, sunAngles = new Vector3(48f, -30f, 0f),
        ambientSky = new Color(0.8f, 0.9f, 1f), ambientEquator = new Color(0.7f, 0.75f, 0.78f), ambientGround = new Color(0.35f, 0.4f, 0.45f),
        fog = new Color(0.75f, 0.88f, 1f), props = PropSet.Crayons, floating = FloatKind.Sparkles,
        floatColors = new[] { Color.white, new Color(0.7f, 0.9f, 1f) },
        ringColor = new Color(0.3f, 0.5f, 0.75f), seaColor = new Color(0.12f, 0.55f, 0.85f),
    };

    public Color PaperFor(int index)
    {
        return pageColors != null && pageColors.Length > 0 ? pageColors[index % pageColors.Length] : paper;
    }
}
