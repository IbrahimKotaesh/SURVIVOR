using UnityEngine;
using TMPro;

public static class GameFontManager
{
    private static TMP_FontAsset titleFontAsset;
    private static TMP_FontAsset bodyFontAsset;
    private static TMP_FontAsset retroFontAsset;

    public static TMP_FontAsset TitleFont
    {
        get
        {
            if (titleFontAsset == null)
            {
                titleFontAsset = CreateFontAssetFromResource("LilitaOne-Regular");
            }
            return titleFontAsset;
        }
    }

    public static TMP_FontAsset BodyFont
    {
        get
        {
            if (bodyFontAsset == null)
            {
                bodyFontAsset = CreateFontAssetFromResource("LilitaOne-Regular");
            }
            return bodyFontAsset;
        }
    }

    public static TMP_FontAsset RetroFont
    {
        get
        {
            if (retroFontAsset == null)
            {
                retroFontAsset = CreateFontAssetFromResource("PressStart2P-Regular");
            }
            return retroFontAsset;
        }
    }

    private static TMP_FontAsset CreateFontAssetFromResource(string resourceName)
    {
        Font font = Resources.Load<Font>(resourceName);
        if (font == null)
        {
            Debug.LogWarning($"GameFontManager: Could not load TTF font from Resources: {resourceName}");
            return null;
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font);
        if (fontAsset != null)
        {
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        }
        else
        {
            Debug.LogWarning($"GameFontManager: Failed to generate TMP_FontAsset for: {resourceName}");
        }
        return fontAsset;
    }
}
