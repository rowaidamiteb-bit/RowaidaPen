using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// يركب تأثير "رسم الطفلة" (خطوط رصاص وتظليل شمع وحبيبات ورق) على الرندر مرة وحدة:
// يسوي مادة من الشيدر ويضيف خطوة رسم على كامل الشاشة قبل الأشياء الشفافة (عشان الواجهة والمطر ما ينرسم حولها)
[InitializeOnLoad]
public static class KidDrawingSetup
{
    const string MatPath = "Assets/Resources/KidDrawing.mat";

    static KidDrawingSetup()
    {
        EditorApplication.delayCall += Setup;
    }

    static void Setup()
    {
        Shader shader = Shader.Find("Rowaida/KidDrawing");
        if (shader == null) return;
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            mat = new Material(shader) { name = "KidDrawing" };
            AssetDatabase.CreateAsset(mat, MatPath);
        }
        foreach (string path in new[] { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" })
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (data == null) continue;
            bool has = false;
            foreach (ScriptableRendererFeature f in data.rendererFeatures)
                if (f != null && f.name == "Kid Drawing") has = true;
            if (has) continue;
            var feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            feature.name = "Kid Drawing";
            feature.passMaterial = mat;
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingTransparents;
            feature.requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
            feature.fetchColorBuffer = true;
            AssetDatabase.AddObjectToAsset(feature, data);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);
            var so = new SerializedObject(data);
            var list = so.FindProperty("m_RendererFeatures");
            var map = so.FindProperty("m_RendererFeatureMap");
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log("[KidDrawing] added to " + path);
        }
    }
}
