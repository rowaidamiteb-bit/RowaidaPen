using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// يبني مشهد المرحلة تلقائيًا أول مرة يفتح فيها المشروع
// ولو احتجتيه مرة ثانية: من القائمة فوق RowaidaPen > Rebuild Game Scene
[InitializeOnLoad]
public static class RowaidaPenSetup
{
    const string ScenePath = "Assets/Scenes/Game.unity";
    const string MaterialsFolder = "Assets/Materials";

    static RowaidaPenSetup()
    {
        EditorApplication.delayCall += AutoSetup;
    }

    static void AutoSetup()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists(ScenePath)) return;
        BuildScene();
    }

    [MenuItem("RowaidaPen/Rebuild Game Scene")]
    public static void BuildScene()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var game = new GameObject("Game");
        var gm = game.AddComponent<GameManager>();
        gm.paperMaterial = GetOrCreateMaterial("Paper", new Color(0.96f, 0.96f, 0.94f), 0.05f);
        gm.inkMaterial = GetOrCreateMaterial("Ink", new Color(0.07f, 0.07f, 0.08f), 0.35f);
        gm.rowaidaMaterial = GetOrCreateMaterial("Rowaida", new Color(0.32f, 0.32f, 0.33f), 0.25f);

        GameManager.CreateCameraAndLight();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("RowaidaPen: Game scene ready at " + ScenePath + " - press Play!");
    }

    static Material GetOrCreateMaterial(string name, Color color, float smoothness)
    {
        string path = MaterialsFolder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            AssetDatabase.CreateFolder("Assets", "Materials");
        mat = GameManager.MakeMaterial(color, smoothness);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
