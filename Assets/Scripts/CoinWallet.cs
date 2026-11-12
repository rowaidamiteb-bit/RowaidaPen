using System;
using UnityEngine;

// الكوينز: أول عشرين مرحلة مجانية، وكل مرحلة تخلصينها أول مرة تعطيك ٥٠ كوينز،
// ومن المرحلة ٢١ كل مرحلة تنفتح بـ ١٠٠. يعني بعد العشرين الأولى معك نص سعر العشرين الثانية،
// ولو شافت إعلان المكافأة بشاشة الفوز تاخذ ٥٠ زيادة (١٠٠ لكل مرحلة) وتكفي العشرين كلها.
// وبالمتجر حزمة تكفي عشرين مرحلة بـ ٢ دولار
public static class Coins
{
    public const int FreeLevels = 20;
    public const int Reward = 50;
    public const int Price = 100;
    public const int Pack = Price * 20;
    const string BalanceKey = "coins";

    public static event Action Changed;

    public static int Balance => PlayerPrefs.GetInt(BalanceKey, 0);

    public static void Add(int amount)
    {
        if (amount < 0) return;
        PlayerPrefs.SetInt(BalanceKey, Balance + amount);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    static bool Spend(int amount)
    {
        if (Balance < amount) return false;
        PlayerPrefs.SetInt(BalanceKey, Balance - amount);
        PlayerPrefs.Save();
        Changed?.Invoke();
        return true;
    }

    // المرحلة تبي كوينز عشان تنفتح؟
    public static bool NeedsBuying(int index) => index >= FreeLevels && !SaveData.Bought(index);

    public static bool CanAfford => Balance >= Price;

    // نفتح المرحلة بالكوينز (لو ما تكفي ترجع false)
    public static bool Buy(int index)
    {
        if (!NeedsBuying(index)) return true;
        if (!Spend(Price)) return false;
        SaveData.SetBought(index);
        return true;
    }

    // خلصت المرحلة: أول مرة بس تاخذ كوينزها، ويرجع كم أخذت (صفر لو لعبتها قبل)
    public static int LevelDone(int index)
    {
        if (SaveData.Rewarded(index)) return 0;
        SaveData.SetRewarded(index);
        Add(Reward);
        return Reward;
    }

    // مرة وحدة: اللي لعبت قبل نظام الكوينز تاخذ كوينز مراحلها، والمراحل اللي وصلتها تظل مفتوحة
    public static void Init()
    {
        if (PlayerPrefs.GetInt("coinsReady", 0) == 1) return;
        int earned = 0;
        for (int i = 0; i < Levels.All.Count; i++)
        {
            if (SaveData.Stars(i) == 0) continue;
            if (!SaveData.Rewarded(i)) { SaveData.SetRewarded(i); earned += Reward; }
            if (i + 1 < Levels.All.Count && i + 1 >= FreeLevels) SaveData.SetBought(i + 1);
            if (i >= FreeLevels) SaveData.SetBought(i);
        }
        PlayerPrefs.SetInt("coinsReady", 1);
        Add(earned);
        PlayerPrefs.Save();
    }
}
