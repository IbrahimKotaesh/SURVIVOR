using UnityEngine;
using System.Collections.Generic;

public static class GameSpriteManager
{
    private static Dictionary<string, Sprite> cachedSprites = new Dictionary<string, Sprite>();

    public static void ForceReload()
    {
        cachedSprites.Clear();
    }

    public static Sprite GetSprite(string spriteName)
    {
        if (cachedSprites.TryGetValue(spriteName, out Sprite cached))
        {
            return cached;
        }

        // Procedurally generate a clean rounded rectangle sprite
        int size = 64;
        int radius = 16;

        if (spriteName.Contains("bar") || spriteName.Contains("fill"))
        {
            size = 32;
            radius = 8;
        }

        Texture2D texture = new Texture2D(size, size);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Min(x, size - 1 - x);
                int dy = Mathf.Min(y, size - 1 - y);
                if (dx < radius && dy < radius)
                {
                    float dist = Mathf.Sqrt((radius - dx) * (radius - dx) + (radius - dy) * (radius - dy));
                    if (dist > radius)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }
                }
                texture.SetPixel(x, y, Color.white);
            }
        }
        texture.Apply();

        // 9-slice borders so it stretches perfectly without corner distortion
        Vector4 border = new Vector4(radius, radius, radius, radius);
        Sprite newSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        newSprite.name = spriteName;

        cachedSprites[spriteName] = newSprite;
        return newSprite;
    }
}
