using UnityEditor;

// صور الواجهة الكرتونية (Resources/UI): تنقرا كصور واجهة (Sprite) بدون ضغط يخرب الخطوط، وبحجم يكفي الجوال
public class UISpriteImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').Contains("/Resources/UI/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.npotScale = TextureImporterNPOTScale.None;
    }
}
