using UnityEditor;

// صور الواجهة الكرتونية (Resources/UI): تنقرا كصور واجهة (Sprite) بدون ضغط يخرب الخطوط، وبحجم يكفي الجوال
public class UISpriteImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string path = assetPath.Replace('\\', '/');
        bool splash = path.Contains("/Resources/Splash/");   // شعارات قبل اللعبة
        if (!path.Contains("/Resources/UI/") && !splash) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = splash ? 2048 : 4096;   // الشعار كبير جدا: نصغره للجوال
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.npotScale = TextureImporterNPOTScale.None;
    }
}
