using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class TransparentBackgroundFilter
{
    [MenuItem("Tools/Make Sprite Background Transparent")]
    public static void CleanCheckerboard()
    {
        string path = "Assets/player_eyeball_sprite.png";
        
        // 1. Make the texture readable first
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("Texture importer not found!");
            return;
        }
        
        bool wasReadable = importer.isReadable;
        
        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        
        // 2. Load texture and apply flood fill transparency
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null)
        {
            Debug.LogError("Failed to load Texture2D!");
            return;
        }
        
        int width = tex.width;
        int height = tex.height;
        Color[] pixels = tex.GetPixels();
        bool[] visited = new bool[pixels.Length];
        
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        
        // Helper to check if color matches checkerboard pattern
        bool IsBackgroundStyle(Color c)
        {
            // Checkerboard is typically grey (e.g. 0.7-0.95) and white (1.0, 1.0, 1.0)
            // Let's check if it's a shade of grey/white (r, g, b are very close to each other, and all are high > 0.5)
            bool isGreyOrWhite = Mathf.Abs(c.r - c.g) < 0.08f && Mathf.Abs(c.g - c.b) < 0.08f && c.r > 0.5f;
            return isGreyOrWhite;
        }

        void AddToQueueIfValid(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            int idx = y * width + x;
            if (visited[idx]) return;
            
            Color c = pixels[idx];
            if (IsBackgroundStyle(c))
            {
                visited[idx] = true;
                queue.Enqueue(new Vector2Int(x, y));
            }
        }
        
        // Start seeds at corners
        AddToQueueIfValid(0, 0);
        AddToQueueIfValid(width - 1, 0);
        AddToQueueIfValid(0, height - 1);
        AddToQueueIfValid(width - 1, height - 1);
        
        // Add borders as seeds just in case
        for (int i = 0; i < width; i++)
        {
            AddToQueueIfValid(i, 0);
            AddToQueueIfValid(i, height - 1);
        }
        for (int j = 0; j < height; j++)
        {
            AddToQueueIfValid(0, j);
            AddToQueueIfValid(width - 1, j);
        }
        
        while (queue.Count > 0)
        {
            Vector2Int curr = queue.Dequeue();
            int currIdx = curr.y * width + curr.x;
            
            pixels[currIdx] = Color.clear;
            
            // Add neighbors
            AddToQueueIfValid(curr.x + 1, curr.y);
            AddToQueueIfValid(curr.x - 1, curr.y);
            AddToQueueIfValid(curr.x, curr.y + 1);
            AddToQueueIfValid(curr.x, curr.y - 1);
        }
        
        // Create new RGBA32 texture to output correct transparency format
        Texture2D rgbaTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        rgbaTex.SetPixels(pixels);
        rgbaTex.Apply();
        
        // Save texture back as PNG
        byte[] bytes = rgbaTex.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
        UnityEngine.Object.DestroyImmediate(rgbaTex);
        
        // 3. Restore importer settings and reimport
        importer.isReadable = wasReadable;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        
        Debug.Log("Sprite background checkerboard removed successfully!");
    }
}
