using UnityEngine;

// حفظ التقدم: آخر مرحلة مفتوحة، ونجوم كل مرحلة، وهل شافت البداية (يبقى حتى لو سكرتي اللعبة)
public static class SaveData
{
    public const int MaxStars = 3;

    // أبعد مرحلة مفتوحة (رقمها من 0)
    // (أي مرحلة خلصتيها تفتح اللي بعدها، حتى لو انضافت مراحل جديدة بعدين)
    public static int Unlocked
    {
        get
        {
            int unlocked = PlayerPrefs.GetInt("unlocked", 0);
            for (int i = 0; i < Levels.All.Count; i++)
                if (Stars(i) > 0) unlocked = Mathf.Max(unlocked, i + 1);
            return Mathf.Clamp(unlocked, 0, Levels.All.Count - 1);
        }
    }

    public static void Unlock(int index)
    {
        if (index <= PlayerPrefs.GetInt("unlocked", 0)) return;
        PlayerPrefs.SetInt("unlocked", index);
        PlayerPrefs.Save();
    }

    public static int Stars(int index) => PlayerPrefs.GetInt("stars" + index, 0);

    // نحفظ أحسن نتيجة بس
    public static void SetStars(int index, int stars)
    {
        if (stars <= Stars(index)) return;
        PlayerPrefs.SetInt("stars" + index, stars);
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

    public static bool HasProgress => IntroSeen || PlayerPrefs.GetInt("unlocked", 0) > 0;
}
