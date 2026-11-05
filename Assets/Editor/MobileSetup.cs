using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

// إعدادات الجوال (مرة وحدة، ويرجع يضبطها لو تغيرت): أيقونة اللعبة، الشاشة بالعرض بس، ومعرّف اللعبة
[InitializeOnLoad]
public static class MobileSetup
{
    const string IconPath = "Assets/Art/AppIcon.png";
    const string BundleId = "com.rowaida.pen";

    static MobileSetup()
    {
        EditorApplication.delayCall += Setup;
    }

    public static void Setup()
    {
        // الأيقونة: صورة كاملة بدون ضغط (الجوال يصغرها لكل حجم)
        var importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
        if (importer != null && (importer.textureCompression != TextureImporterCompression.Uncompressed || importer.maxTextureSize != 1024 || importer.alphaSource != TextureImporterAlphaSource.None))
        {
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
        }
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (icon != null)
        {
            var icons = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any);
            if (icons.Length == 0 || icons[0] != icon) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }

        // الشاشة بالعرض بس (تلف مع الجوال يمين ويسار)
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        // معرّف اللعبة بالآيفون والأندرويد، ورقم النسخة
        if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS) != BundleId) PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
        if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != BundleId) PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
        if (PlayerSettings.bundleVersion == "0.1.0") PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.statusBarHidden = true;

        // أندرويد: IL2CPP و64 بت (قوقل بلاي يطلبها)
        if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        SetAdMobIds();
        AssetDatabase.SaveAssets();
        // يونتي ما يكتب إعدادات المشروع بالملف إلا إذا انحفظ المشروع: نحفظه مرة وحدة لو الملف لسا قديم
        string file = System.IO.File.ReadAllText("ProjectSettings/ProjectSettings.asset");
        if (!file.Contains("iPhone: " + BundleId) && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.ExecuteMenuItem("File/Save Project");
    }

    // إعلانات قوقل تحتاج معرّف التطبيق بإعداداتها (بدونه اللعبة تطفي على الجوال)
    static void SetAdMobIds()
    {
        System.Type type = null;
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            if ((type = asm.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings")) != null) break;
        if (type == null) return;
        var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
        var settings = type.GetMethod("LoadInstance", flags)?.Invoke(null, null) as ScriptableObject;
        if (settings == null) return;
        var android = type.GetProperty("GoogleMobileAdsAndroidAppId");
        var ios = type.GetProperty("GoogleMobileAdsIOSAppId");
        if ((string)android.GetValue(settings) == Ads.AndroidAppId && (string)ios.GetValue(settings) == Ads.IosAppId) return;
        android.SetValue(settings, Ads.AndroidAppId);
        ios.SetValue(settings, Ads.IosAppId);
        EditorUtility.SetDirty(settings);
    }
}

#if UNITY_ANDROID
// اسم اللعبة تحت الأيقونة بالأندرويد: "قلم رويدا"
public class AndroidDisplayName : UnityEditor.Android.IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 999;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string file = System.IO.Path.Combine(path, "..", "launcher", "src", "main", "res", "values", "strings.xml");
        if (!System.IO.File.Exists(file)) return;
        string xml = System.IO.File.ReadAllText(file);
        xml = System.Text.RegularExpressions.Regex.Replace(xml, "<string name=\"app_name\">[^<]*</string>", "<string name=\"app_name\">قلم رويدا</string>");
        System.IO.File.WriteAllText(file, xml);
    }
}
#endif

#if UNITY_IOS
// اسم اللعبة تحت الأيقونة بالآيفون: "قلم رويدا" (اسم المشروع يبقى بالإنجليزي عشان ما تضيع الحفظات)
public static class IosDisplayName
{
    [UnityEditor.Callbacks.PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS) return;
        string plistPath = System.IO.Path.Combine(path, "Info.plist");
        var plist = new UnityEditor.iOS.Xcode.PlistDocument();
        plist.ReadFromFile(plistPath);
        plist.root.SetString("CFBundleDisplayName", "قلم رويدا");
        plist.WriteToFile(plistPath);
    }
}
#endif
