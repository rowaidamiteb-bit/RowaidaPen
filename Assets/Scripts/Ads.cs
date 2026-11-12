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
        if (Application.isEditor) { EditorAdPreview.Show("إعلان بين المراحل", then); return; }
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
        if (Application.isEditor) { EditorAdPreview.Show("إعلان المكافأة", () => done(true)); return; }
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

// بالمحرر بس: إعلانات يونتي ما تشتغل إلا بالجوال، فنعرض مكانه بطاقة تجريبية ثانيتين عشان نشوف الترتيب
public class EditorAdPreview : MonoBehaviour
{
    System.Action done;
    float age;

    public static void Show(string title, System.Action onDone)
    {
        var go = new GameObject("Ad Preview", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        var root = (RectTransform)go.transform;
        var dim = Box(root, new Color(0.1f, 0.08f, 0.16f, 0.96f));
        dim.rectTransform.anchorMin = Vector2.zero;
        dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;
        dim.raycastTarget = true;
        Text(root, title, 80, new Vector2(0f, 60f), new Color(1f, 0.47f, 0.56f));
        Text(root, "(بالجوال يطلع هنا إعلان حقيقي من يونتي)", 36, new Vector2(0f, -40f), Color.white);
        var preview = go.AddComponent<EditorAdPreview>();
        preview.done = onDone;
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;
        if (age < 2.5f) return;
        System.Action d = done;
        done = null;
        Destroy(gameObject);
        d?.Invoke();
    }

    static UnityEngine.UI.Image Box(RectTransform parent, Color c)
    {
        var img = new GameObject("Dim", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        img.transform.SetParent(parent, false);
        img.color = c;
        return img;
    }

    static void Text(RectTransform parent, string text, int size, Vector2 pos, Color c)
    {
        var t = new GameObject("Text", typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>();
        t.transform.SetParent(parent, false);
        t.font = ArabicText.Font;
        t.text = ArabicText.Fix(text);
        t.fontSize = ArabicText.Size(size);
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = c;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.rectTransform.anchoredPosition = pos;
        t.rectTransform.sizeDelta = new Vector2(1600f, 120f);
    }
}
