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

        // أندرويد: IL2CPP، ويشتغل على الجوالات الجديدة (64 بت) والقديمة (32 بت)، ومن أندرويد 7.1 وفوق
        if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

        IncludeShaders();
        AssetDatabase.SaveAssets();
        // يونتي ما يكتب إعدادات المشروع بالملف إلا إذا انحفظ المشروع: نحفظه مرة وحدة لو الملف لسا قديم
        string file = System.IO.File.ReadAllText("ProjectSettings/ProjectSettings.asset");
        if (!file.Contains("iPhone: " + BundleId) && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.ExecuteMenuItem("File/Save Project");
    }

    // شيدرات ينطلبون بالاسم من الكود: يونتي ما يحطهم بالبناء لحاله، وبدونهم الجوال يعلق أول ما تبدأ المرحلة.
    // ونحط مع البناء كل أشكال الشيدرات اللي استخدمتها المراحل (انسجلت وإحنا نلعب كل المراحل بالمحرر)
    static readonly string[] CodeShaders = { "Universal Render Pipeline/Unlit", "Universal Render Pipeline/Particles/Unlit" };
    public const string VariantsPath = "Assets/Settings/GameShaders.shadervariants";

    static void IncludeShaders()
    {
        var graphics = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
        if (graphics == null) return;
        var so = new SerializedObject(graphics);
        bool changed = false;
        var always = so.FindProperty("m_AlwaysIncludedShaders");
        foreach (string name in CodeShaders)
            changed |= AddOnce(always, Shader.Find(name));
        changed |= AddOnce(so.FindProperty("m_PreloadedShaders"), AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(VariantsPath));
        if (changed) so.ApplyModifiedPropertiesWithoutUndo();
    }

    static bool AddOnce(SerializedProperty list, Object item)
    {
        if (list == null || item == null) return false;
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == item) return false;
        list.InsertArrayElementAtIndex(list.arraySize);
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
        return true;
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
