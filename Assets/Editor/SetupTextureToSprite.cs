using UnityEditor;
using UnityEngine;

public static class SetupTextureToSprite
{
    [MenuItem("Tools/Convert Player Sprite")]
    public static void Convert()
    {
        string path = "Assets/player_eyeball_sprite.png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            Debug.Log("Converted " + path + " to Sprite successfully.");
        }
        else
        {
            Debug.LogError("Failed to find importer for: " + path);
        }
    }
}
