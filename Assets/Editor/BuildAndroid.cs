using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// نسخة الأندرويد: يبني ملف APK بمجلد Builds (ينزل على الجوال ويتثبت على طول)
public static class BuildAndroid
{
    public const string Output = "Builds/RowaidaPen.apk";

    [MenuItem("Rowaida/نسخة أندرويد (APK)")]
    public static void Build()
    {
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        Directory.CreateDirectory("Builds");
        EditorUserBuildSettings.buildAppBundle = false;
        MobileSetup.Setup();   // الأيقونة ومعرّف الإعلانات وإعدادات الأندرويد
        ResolveDependencies();
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Output,
            target = BuildTarget.Android,
            options = BuildOptions.None,
        });
        Debug.Log("ANDROID BUILD: " + report.summary.result + " size=" + report.summary.totalSize + " errors=" + report.summary.totalErrors);
        if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    // مكتبات إعلانات قوقل والمتجر حق الأندرويد: نضيفها لملفات البناء قبل ما نبني
    static void ResolveDependencies()
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var resolver = asm.GetType("GooglePlayServices.PlayServicesResolver");
            var resolve = resolver?.GetMethod("ResolveSync", new[] { typeof(bool) });
            if (resolve == null) continue;
            Debug.Log("ANDROID RESOLVE: " + resolve.Invoke(null, new object[] { true }));
            return;
        }
    }
}

// نسخة ويندوز سريعة للتجربة: نتأكد إن اللعبة تشتغل برا المحرر قبل ما نبني الجوال
public static class BuildWindowsTest
{
    public static void Build()
    {
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        MobileSetup.Setup();
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/WindowsTest/RowaidaPen.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development,
        });
        Debug.Log("WINDOWS BUILD: " + report.summary.result + " errors=" + report.summary.totalErrors);
        if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
