using System;
using System.Collections;
using Unity.Services.LevelPlay;
using UnityEngine;

// الإعلانات (يونتي LevelPlay):
// ١. إعلان بين المراحل: أول ما رويدا توصل الباب، وبعده تطلع شاشة الفوز
// ٢. إعلان بمكافأة: زر "شاهد إعلان +50" بشاشة الفوز، لو انشاف للآخر تاخذ ٥٠ كوينز زيادة (١٠٠ بدل ٥٠)
// المفتاح ورموز الإعلانين من لوحة LevelPlay (لو فاضية: ما فيه إعلانات واللعبة تكمل عادي)
public static class Ads
{
#if UNITY_IOS
    const string AppKey = "";
    const string RewardedId = "";
    const string InterstitialId = "";
#else
    // أندرويد: من حساب LevelPlay (تطبيق "قلم رويدا")
    const string AppKey = "296e24f65";
    const string RewardedId = "jglhr81kmtpagg0a";       // Double Coins Rewarded
    const string InterstitialId = "bngenpt4djx3l4ci";   // Between Levels Interstitial
#endif
    static bool started;
    static LevelPlayRewardedAd rewarded;
    static LevelPlayInterstitialAd interstitial;
    static Action afterInterstitial;      // نكمل بعد ما يتسكر الإعلان
    static Action<bool> afterReward;      // نقول شافته للآخر أو لا
    static bool rewardEarned;

    public static void Init()
    {
        if (started || string.IsNullOrEmpty(AppKey)) return;
        started = true;
        LevelPlay.OnInitSuccess += config => CreateAds();
        LevelPlay.OnInitFailed += error => Debug.LogWarning("الإعلانات ما اشتغلت: " + error);
        LevelPlay.Init(AppKey);
    }

    // نجهز الإعلانين من قبل عشان يطلعون على طول وقت ما نحتاجهم
    static void CreateAds()
    {
        rewarded = new LevelPlayRewardedAd(RewardedId);
        rewarded.OnAdRewarded += (info, reward) => rewardEarned = true;
        rewarded.OnAdClosed += info => { rewarded.LoadAd(); FinishReward(); };
        rewarded.OnAdDisplayFailed += (info, error) => { rewarded.LoadAd(); FinishReward(); };
        rewarded.LoadAd();

        interstitial = new LevelPlayInterstitialAd(InterstitialId);
        interstitial.OnAdClosed += info => { interstitial.LoadAd(); FinishInterstitial(); };
        interstitial.OnAdDisplayFailed += (info, error) => { interstitial.LoadAd(); FinishInterstitial(); };
        interstitial.LoadAd();
    }

    // ---------- إعلان آخر المرحلة ----------

    // وصلت الباب: إعلان (لو جاهز)، وبعده نكمل لشاشة الفوز
    public static void ShowLevelEnd(Action then)
    {
        try
        {
            if (interstitial != null && interstitial.IsAdReady())
            {
                afterInterstitial = then;
                interstitial.ShowAd();
                return;
            }
            if (interstitial != null) interstitial.LoadAd();   // ما جهز (ما فيه نت مثلًا): نطلب واحد للمرة الجاية
        }
        catch (Exception e) { Debug.LogWarning(e); afterInterstitial = null; }
        then();
    }

    static void FinishInterstitial()
    {
        Action next = afterInterstitial;
        afterInterstitial = null;
        next?.Invoke();
    }

    // ---------- إعلان المكافأة ----------

    // فيه إعلان مكافأة جاهز؟ (بالمحرر دايم نعم، عشان نجرب الزر)
    public static bool RewardReady => Application.isEditor || (rewarded != null && rewarded.IsAdReady());

    // نعرض إعلان المكافأة، و done(true) لو انشاف للآخر
    public static void ShowReward(Action<bool> done)
    {
        if (Application.isEditor) { done(true); return; }
        try
        {
            if (rewarded == null || !rewarded.IsAdReady()) { rewarded?.LoadAd(); done(false); return; }
            afterReward = done;
            rewardEarned = false;
            rewarded.ShowAd();
        }
        catch (Exception e) { Debug.LogWarning(e); afterReward = null; done(false); }
    }

    // المكافأة أحيانًا توصل بعد ما يتسكر الإعلان بشوي: نستنى نص ثانية قبل ما نقرر
    static void FinishReward()
    {
        if (afterReward == null) return;
        if (GameManager.Instance != null) GameManager.Instance.StartCoroutine(FinishRewardLater());
        else EndReward();
    }

    static IEnumerator FinishRewardLater()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        EndReward();
    }

    static void EndReward()
    {
        Action<bool> done = afterReward;
        afterReward = null;
        done?.Invoke(rewardEarned);
    }
}
