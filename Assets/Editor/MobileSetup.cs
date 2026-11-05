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

    static void Setup()
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
        AssetDatabase.SaveAssets();
        // يونتي ما يكتب إعدادات المشروع بالملف إلا إذا انحفظ المشروع: نحفظه مرة وحدة لو الملف لسا قديم
        string file = System.IO.File.ReadAllText("ProjectSettings/ProjectSettings.asset");
        if (!file.Contains("iPhone: " + BundleId) && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.ExecuteMenuItem("File/Save Project");
    }
}

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
