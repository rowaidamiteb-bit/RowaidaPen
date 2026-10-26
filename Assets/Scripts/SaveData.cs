using UnityEngine;

// حفظ التقدم: آخر مرحلة مفتوحة، ونجوم كل مرحلة، وهل شافت البداية (يبقى حتى لو سكرتي اللعبة)
// النجوم محفوظة باسم المرحلة (مو برقمها) عشان لو انضافت مراحل بالنص ما تتلخبط
public static class SaveData
{
    public const int MaxStars = 3;

    // ترتيب المراحل القديم (لما كانت النجوم محفوظة بالرقم): ننقلها مرة وحدة للحفظ الجديد
    static readonly string[] oldOrder =
    {
        "الصفحات الأولى", "صفحات المطر", "تحت المطر", "صفحات الريح", "العاصفة",
        "صفحات الشخابيط", "شخابيط أكثر", "صفحة الألوان", "الأرض الطايرة", "الصفحة المقلوبة",
        "صفحة الحبر المنسكب", "الصفحة المتلاشية", "زيزو", "صفحة الفضاء", "صفحة البالونات",
        "صفحة النار", "صفحة البوابات", "صفحة الوادي", "صفحة اليراعات", "الصفحة الأخيرة",
    };

    static void Migrate()
    {
        if (PlayerPrefs.GetInt("starsByName", 0) == 1) return;
        for (int i = 0; i < oldOrder.Length; i++)
        {
            int s = PlayerPrefs.GetInt("stars" + i, 0);
            if (s > 0) PlayerPrefs.SetInt(Key(oldOrder[i]), s);
            PlayerPrefs.DeleteKey("stars" + i);
        }
        PlayerPrefs.DeleteKey("unlocked");   // كان رقم بالترتيب القديم، والحين نحسبه من النجوم
        PlayerPrefs.SetInt("starsByName", 1);
        PlayerPrefs.Save();
    }

    // مفتاح ثابت من اسم المرحلة (أرقام وحروف إنجليزي بس)
    static string Key(string title)
    {
        uint h = 2166136261;
        foreach (char c in title) { h ^= c; h *= 16777619; }
        return "stars_" + h.ToString("x8");
    }

    // أبعد مرحلة مفتوحة (رقمها من 0): المرحلة اللي بعد آخر وحدة خلصتيها من البداية ورا بعض
    public static int Unlocked
    {
        get
        {
            int unlocked = 0;
            while (unlocked < Levels.All.Count - 1 && Stars(unlocked) > 0) unlocked++;
            return unlocked;
        }
    }

    public static void Unlock(int index)
    {
        if (index <= PlayerPrefs.GetInt("unlocked", 0)) return;
        PlayerPrefs.SetInt("unlocked", index);
        PlayerPrefs.Save();
    }

    public static int Stars(int index)
    {
        Migrate();
        return PlayerPrefs.GetInt(Key(Levels.All[index].title), 0);
    }

    // نحفظ أحسن نتيجة بس
    public static void SetStars(int index, int stars)
    {
        if (stars <= Stars(index)) return;
        PlayerPrefs.SetInt(Key(Levels.All[index].title), stars);
        PlayerPrefs.Save();
    }

    // للتجربة: نحط نجوم لمرحلة بدون ما نلعبها (أو نمسحها بصفر)
    public static void ForceStars(int index, int stars)
    {
        Migrate();
        PlayerPrefs.SetInt(Key(Levels.All[index].title), stars);
        PlayerPrefs.Save();
    }

    // الملصقات المخفية (الجزء الثالث): ملصق واحد بكل مرحلة
    public static bool HasSticker(int index) => PlayerPrefs.GetInt("sticker" + Key(Levels.All[index].title).Substring(5), 0) == 1;

    public static void SetSticker(int index)
    {
        PlayerPrefs.SetInt("sticker" + Key(Levels.All[index].title).Substring(5), 1);
        PlayerPrefs.Save();
    }

    public static int TotalStars
    {
        get
        {
            int total = 0;
            for (int i = 0; i < Levels.All.Count; i++) total += Stars(i);
            return total;
        }
    }

    public static bool IntroSeen
    {
        get => PlayerPrefs.GetInt("introSeen", 0) == 1;
        set { PlayerPrefs.SetInt("introSeen", value ? 1 : 0); PlayerPrefs.Save(); }
    }

    // من جديد: نمسح المراحل المفتوحة والنجوم والبداية (لون وشكل القلم يظلون)
    public static void ResetProgress()
    {
        Migrate();
        PlayerPrefs.DeleteKey("unlocked");
        PlayerPrefs.DeleteKey("introSeen");
        foreach (LevelData level in Levels.All) PlayerPrefs.DeleteKey(Key(level.title));
        PlayerPrefs.Save();
    }

    public static bool HasProgress => IntroSeen || Unlocked > 0;
}
