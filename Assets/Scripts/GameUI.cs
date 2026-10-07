using UnityEngine;
using UnityEngine.UI;

// واجهة اللعبة: شريط الحبر، رقم المرحلة، ورسائل في نص الشاشة
// ملاحظة: النصوص بالإنجليزي لأن نصوص Unity العادية ما تدعم الحروف العربية المتصلة
public class GameUI : MonoBehaviour
{
    RectTransform inkFill;
    Text levelText;
    Text messageText, subtitleText;
    Canvas canvasRef;

    public void SetVisible(bool visible)
    {
        if (canvasRef != null) canvasRef.enabled = visible;
    }
    float messageTimer;
    Font font;

    public void Build()
    {
        font = ArabicText.Font;

        var canvasGo = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 0.5f;
        canvasRef = canvas;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        Transform root = canvasGo.transform;

        // شريط الحبر
        Label(root, ArabicText.Fix("الحبر"), 32, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(100f, 44f));
        Image frame = Panel(root, "Ink Bar", Color.black);
        Place(frame.rectTransform, new Vector2(0f, 1f), new Vector2(120f, -40f), new Vector2(420f, 44f));
        Image inner = Panel(frame.transform, "Inner", Color.white);
        Stretch(inner.rectTransform, 4f);
        Image area = Panel(inner.transform, "Fill Area", Color.clear);
        Stretch(area.rectTransform, 4f);
        Image fill = Panel(area.transform, "Fill", Color.black);
        inkFill = fill.rectTransform;
        inkFill.anchorMin = Vector2.zero;
        inkFill.anchorMax = Vector2.one;
        inkFill.offsetMin = inkFill.offsetMax = Vector2.zero;

        levelText = Label(root, "", 40, TextAnchor.MiddleRight, new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(400f, 50f));
        messageText = Label(root, "", 80, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1600f, 140f));
        subtitleText = Label(root, "", 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1700f, 70f));
        subtitleText.color = Color.white;
        subtitleText.gameObject.AddComponent<Outline>().effectColor = new Color(0.05f, 0.03f, 0.1f, 0.9f);
        messageText.gameObject.AddComponent<Outline>().effectColor = Color.white;
        Label(root, ArabicText.Fix("ارسمي بالماوس   •   زر R يعيد المرحلة"), 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1200f, 40f))
            .color = new Color(0f, 0f, 0f, 0.6f);
    }

    public void SetInk(float fraction)
    {
        if (inkFill != null) inkFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
    }

    public void SetLevel(int number)
    {
        if (levelText != null) levelText.text = ArabicText.Fix("المرحلة " + number);
    }

    public void ShowMessage(string message, float duration, string subtitle = null)
    {
        messageText.text = ArabicText.Fix(message);
        messageText.enabled = true;
        subtitleText.text = ArabicText.Fix(subtitle ?? "");
        subtitleText.enabled = true;
        messageTimer = duration;
    }

    void Update()
    {
        if (messageTimer <= 0f) return;
        messageTimer -= Time.deltaTime;
        if (messageTimer <= 0f) messageText.enabled = subtitleText.enabled = false;
    }

    static Image Panel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    Text Label(Transform parent, string text, int size, TextAnchor align, Vector2 anchor, Vector2 pos, Vector2 box)
    {
        var go = new GameObject(text.Length > 0 ? text : "Message", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.alignment = align;
        t.color = Color.black;
        t.raycastTarget = false;
        Place(t.rectTransform, anchor, pos, box);
        return t;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.one * inset;
        rt.offsetMax = -Vector2.one * inset;
    }
}
