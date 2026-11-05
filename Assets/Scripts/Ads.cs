using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;
using UnityEngine.UI;

// الإعلانات (قوقل AdMob): بعد كل خمس مراحل يطلع إعلان قبل المرحلة الجاية (ما يقاطع اللعب أبدًا).
// لو شافته اللاعبة للآخر تتدبل الكوينز اللي جمعتها من آخر خمس مراحل.
// المعرفات الحين معرفات قوقل التجريبية (تطلع إعلانات تجريبية حقيقية)، ولما تسوين حساب AdMob نحط معرفاتك هنا
public static class Ads
{
    public const int Every = 5;
    public const string AndroidAppId = "ca-app-pub-3940256099942544~3347511713";
    public const string IosAppId = "ca-app-pub-3940256099942544~1458002511";
#if UNITY_IOS
    const string RewardedId = "ca-app-pub-3940256099942544/6978759866";
    const string InterstitialId = "ca-app-pub-3940256099942544/4411468910";
#else
    const string RewardedId = "ca-app-pub-3940256099942544/5354046379";
    const string InterstitialId = "ca-app-pub-3940256099942544/1033173712";
#endif
    const string CountKey = "ads_levels_done";
    const string BonusKey = "ads_bonus";   // كوينز آخر خمس مراحل (تنضاف مرة ثانية لو شافت الإعلان كامل)
    static bool pending, started;
    static RewardedInterstitialAd rewarded;
    static InterstitialAd interstitial;

    public static void Init()
    {
        if (started) return;
        started = true;
        MobileAds.SetiOSAppPauseOnBackground(true);
        MobileAds.Initialize(status => MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
            LoadRewarded();
            LoadInterstitial();
        }));
    }

    // نجهز الإعلان من قبل عشان يطلع على طول وقت ما نحتاجه
    static void LoadRewarded()
    {
        RewardedInterstitialAd.Load(RewardedId, new AdRequest(), (ad, error) =>
            MobileAdsEventExecutor.ExecuteInUpdate(() => { if (error == null && ad != null) rewarded = ad; }));
    }

    static void LoadInterstitial()
    {
        InterstitialAd.Load(InterstitialId, new AdRequest(), (ad, error) =>
            MobileAdsEventExecutor.ExecuteInUpdate(() => { if (error == null && ad != null) interstitial = ad; }));
    }

    // خلصت مرحلة: نحسب كوينزها للمضاعفة، وكل خمس مراحل يصير فيه إعلان ينتظر
    public static void LevelCompleted(int coins)
    {
        int n = PlayerPrefs.GetInt(CountKey, 0) + 1;
        PlayerPrefs.SetInt(CountKey, n);
        PlayerPrefs.SetInt(BonusKey, PlayerPrefs.GetInt(BonusKey, 0) + coins);
        PlayerPrefs.Save();
        if (n % Every == 0) pending = true;
    }

    // قبل ما نبدأ المرحلة الجاية: لو فيه إعلان ينتظر نعرضه، وبعدين نكمل
    public static void ShowIfDue(Action then)
    {
        if (!pending) { then(); return; }
        pending = false;
        int bonus = PlayerPrefs.GetInt(BonusKey, 0);
        PlayerPrefs.SetInt(BonusKey, 0);
        PlayerPrefs.Save();
        // فيه كوينز تتدبل: إعلان بمكافأة (قبله شاشة صغيرة تقول المكافأة، وتقدر تتخطاه)
        if (bonus > 0 && rewarded != null && rewarded.CanShowAd())
            AdIntro.Open(bonus, watch => { if (watch) ShowRewarded(bonus, then); else then(); });
        else if (interstitial != null && interstitial.CanShowAd()) ShowInterstitial(then);
        else then();   // ما فيه نت أو الإعلان ما جهز: نكمل عادي
    }

    static void ShowRewarded(int bonus, Action then)
    {
        RewardedInterstitialAd ad = rewarded;
        rewarded = null;
        bool done = false;
        void Finish()
        {
            if (done) return;
            done = true;
            ad.Destroy();
            LoadRewarded();
            then();
        }
        ad.OnAdFullScreenContentClosed += () => MobileAdsEventExecutor.ExecuteInUpdate(Finish);
        ad.OnAdFullScreenContentFailed += error => MobileAdsEventExecutor.ExecuteInUpdate(Finish);
        // شافته للآخر: الكوينز تتدبل
        ad.Show(reward => MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
            Coins.Add(bonus);
            GameManager.Instance.Menu.Toast("تضاعفت الكوينز! حصلنا على " + bonus + " إضافية");
        }));
    }

    static void ShowInterstitial(Action then)
    {
        InterstitialAd ad = interstitial;
        interstitial = null;
        bool done = false;
        void Finish()
        {
            if (done) return;
            done = true;
            ad.Destroy();
            LoadInterstitial();
            then();
        }
        ad.OnAdFullScreenContentClosed += () => MobileAdsEventExecutor.ExecuteInUpdate(Finish);
        ad.OnAdFullScreenContentFailed += error => MobileAdsEventExecutor.ExecuteInUpdate(Finish);
        ad.Show();
    }
}

// قبل الإعلان بمكافأة: بطاقة تقول كم كوينز تتدبل، وعد تنازلي ثلاث ثواني، وزر تخطي
public class AdIntro : MonoBehaviour
{
    const float Wait = 3f;
    Action<bool> done;
    float age;
    Text countdown;

    public static void Open(int bonus, Action<bool> onDone)
    {
        var go = new GameObject("Ad Intro", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        var intro = go.AddComponent<AdIntro>();
        intro.done = onDone;
        intro.Build((RectTransform)go.transform, bonus);
    }

    void Build(RectTransform root, int bonus)
    {
        Sprite round = MenuUI.RoundedSprite();
        var dim = Box(root, "Dim", new Color(0.08f, 0.06f, 0.12f, 0.85f), null, Vector2.zero, Vector2.zero);
        dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = Vector2.one;
        dim.raycastTarget = true;   // ما تقدر ترسم ورا البطاقة
        var card = Box(root, "Card", new Color(1f, 0.97f, 0.9f), round, Vector2.zero, new Vector2(1000f, 600f));
        Label(card.rectTransform, "إعلان قصير", 64, new Vector2(0f, 200f), new Color(1f, 0.47f, 0.56f));
        Label(card.rectTransform, "نشاهده حتى النهاية فتتضاعف كوينز آخر خمس مراحل", 34, new Vector2(0f, 105f), new Color(0.17f, 0.24f, 0.31f));
        var coin = Box(card.rectTransform, "Coin", Color.white, MenuUI.CoinSprite(), new Vector2(150f, 5f), new Vector2(110f, 110f));
        coin.type = Image.Type.Simple;
        Label(card.rectTransform, "", 80, new Vector2(-55f, 5f), new Color(0.93f, 0.6f, 0.15f)).text = "+" + bonus;   // رقم بس: بدون قلب الحروف
        countdown = Label(card.rectTransform, "", 32, new Vector2(0f, -100f), new Color(0.45f, 0.47f, 0.5f));
        // زر التخطي (بدون مضاعفة)
        var skip = Box(card.rectTransform, "Skip", new Color(0.6f, 0.59f, 0.57f), round, new Vector2(0f, -205f), new Vector2(260f, 84f));
        skip.raycastTarget = true;
        Label(skip.rectTransform, "تخطي", 36, new Vector2(0f, 2f), Color.white);
        skip.gameObject.AddComponent<Button>().onClick.AddListener(() => Finish(false));
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;
        int left = Mathf.CeilToInt(Wait - age);
        countdown.text = ArabicText.Fix("يبدأ الإعلان بعد " + Mathf.Max(left, 1));
        if (age >= Wait) Finish(true);
    }

    void Finish(bool watch)
    {
        if (done == null) return;
        Action<bool> d = done;
        done = null;
        Destroy(gameObject);
        d(watch);
    }

    static Image Box(RectTransform parent, string name, Color c, Sprite sprite, Vector2 pos, Vector2 size)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        img.color = c;
        img.raycastTarget = false;
        if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f; }
        img.rectTransform.anchoredPosition = pos;
        img.rectTransform.sizeDelta = size;
        return img;
    }

    static Text Label(RectTransform parent, string text, int size, Vector2 pos, Color c)
    {
        var t = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        t.transform.SetParent(parent, false);
        t.font = ArabicText.Font;
        t.text = ArabicText.Fix(text);
        t.fontSize = ArabicText.Size(size);
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = c;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.rectTransform.anchoredPosition = pos;
        t.rectTransform.sizeDelta = new Vector2(900f, 100f);
        return t;
    }
}
