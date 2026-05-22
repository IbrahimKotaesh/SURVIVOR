using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void SendGameOverToAndroid(int score, int coins);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void SendVictoryToAndroid(int score, int coins, int bonus);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void SendGemsUpdatedToAndroid(int newBalance);
#else
    private static void SendGameOverToAndroid(int score, int coins)
    {
        Debug.Log($"[Mock Android Bridge] Game Over - Score: {score}, Coins: {coins}");
    }

    private static void SendVictoryToAndroid(int score, int coins, int bonus)
    {
        Debug.Log($"[Mock Android Bridge] Victory - Score: {score}, Coins: {coins}, Bonus: {bonus}");
    }

    private static void SendGemsUpdatedToAndroid(int newBalance)
    {
        Debug.Log($"[Mock Android Bridge] Gems Updated: {newBalance}");
    }
#endif

    // Static variable to persist selected stage between scene reloads
    public static int SelectedStage = 1;

    [Header("UI References")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Game Over References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverScoreText;

    [Header("Drop Prefabs")]
    [SerializeField] private GameObject gemPrefab;

    // Level progression stats
    public int SelectedStageIndex { get; set; } = 1;
    public LevelConfig CurrentLevelConfig { get; private set; }
    public float TimeRemaining { get; private set; }
    public bool IsBossTime { get; private set; } = false;

    private int score = 0;
    private int coins = 0;
    private bool isGameOver = false;
    private bool isVictory = false;

    // HUD references
    private TextMeshProUGUI hudTimerText;
    private TextMeshProUGUI hudCoinsText;
    private TextMeshProUGUI hudScoreText;
    private TextMeshProUGUI hudLevelText;
    private Image timerBarFillImage;
    private GameObject warningTextGo;
    private Button hudAbilityButton;
    private TextMeshProUGUI hudAbilityButtonText;

    // Victory Panel programmatic reference
    private GameObject victoryPanel;
    private GameObject bossEnvironmentOverlay;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            SelectedStageIndex = SelectedStage;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Load Selected Stage Config
        CurrentLevelConfig = LevelConfig.GetConfig(SelectedStageIndex);
        TimeRemaining = CurrentLevelConfig.duration;
        IsBossTime = false;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        CreateUnifiedHUD();

        // Start BGM battle fallback if running unpaused (without menu)
        if (SoundManager.Instance != null && Time.timeScale > 0f)
        {
            SoundManager.Instance.PlayBGM("battle");
        }
    }

    private static Sprite roundedRectSprite;
    public static Sprite GetOrCreateRoundedRectSprite()
    {
        if (roundedRectSprite != null) return roundedRectSprite;
        
        int size = 64;
        int radius = 16;
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
        roundedRectSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return roundedRectSprite;
    }

    private static Sprite coinSprite;
    public static Sprite GetOrCreateCoinSprite()
    {
        if (coinSprite != null) return coinSprite;
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        float centerX = size / 2f;
        float centerY = size / 2f;
        float radius = 24f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > radius + 0.5f)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float alpha = 1f;
                    if (dist > radius - 0.5f)
                    {
                        alpha = (radius + 0.5f - dist);
                    }

                    Color pixelColor;
                    if (dist < 6f && Mathf.Abs(dx) < 3.5f && Mathf.Abs(dy) < 3.5f)
                    {
                        pixelColor = new Color(0.50f, 0.30f, 0.02f, alpha); // Dark center relief
                    }
                    else if (dist > radius - 3.5f)
                    {
                        pixelColor = new Color(0.70f, 0.45f, 0.05f, alpha); // Dark gold outline
                    }
                    else if (dist > radius - 6.5f)
                    {
                        pixelColor = new Color(0.95f, 0.70f, 0.10f, alpha); // Medium gold ring
                    }
                    else if (dist > radius - 9.5f)
                    {
                        pixelColor = new Color(1.00f, 0.90f, 0.45f, alpha); // Light highlight ring
                    }
                    else
                    {
                        float shade = (dx - dy) / (2f * radius);
                        float r = 0.90f + shade * 0.10f;
                        float g = 0.68f + shade * 0.10f;
                        float b = 0.08f + shade * 0.05f;
                        pixelColor = new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), alpha);
                    }
                    texture.SetPixel(x, y, pixelColor);
                }
            }
        }
        texture.Apply();
        coinSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return coinSprite;
    }

    public static Sprite LoadSpriteFromResources(string path)
    {
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null) return sprite;

        Sprite[] sprites = Resources.LoadAll<Sprite>(path);
        if (sprites != null && sprites.Length > 0)
        {
            foreach (var s in sprites)
            {
                if (s.name == path + "_0" || s.name == path.ToLower() + "_0" || s.name.EndsWith("_0"))
                {
                    return s;
                }
            }
            return sprites[0];
        }
        return null;
    }

    private static Sprite diamondSprite;
    public static Sprite GetOrCreateDiamondSprite()
    {
        if (diamondSprite != null) return diamondSprite;

        diamondSprite = LoadSpriteFromResources("Diamond");
        if (diamondSprite != null) return diamondSprite;

        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        float centerX = size / 2f;
        float centerY = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;

                float normX = Mathf.Abs(dx) / 20f;
                float normY = Mathf.Abs(dy) / 24f;
                float dist = normX + normY;

                if (dist > 1.05f)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float alpha = 1f;
                    if (dist > 0.95f)
                    {
                        alpha = (1.05f - dist) / 0.1f;
                    }

                    Color pixelColor;
                    bool isRight = dx >= 0;
                    bool isTop = dy >= 0;

                    float innerDist = normX / 0.5f + normY / 0.5f;

                    if (innerDist <= 1.0f)
                    {
                        if (isTop && isRight)
                            pixelColor = new Color(0.70f, 0.95f, 1.00f, alpha);
                        else if (isTop && !isRight)
                            pixelColor = new Color(0.50f, 0.85f, 1.00f, alpha);
                        else if (!isTop && !isRight)
                            pixelColor = new Color(0.25f, 0.55f, 0.95f, alpha);
                        else
                            pixelColor = new Color(0.35f, 0.65f, 0.98f, alpha);
                    }
                    else
                    {
                        if (isTop && isRight)
                            pixelColor = new Color(0.30f, 0.70f, 0.95f, alpha);
                        else if (isTop && !isRight)
                            pixelColor = new Color(0.15f, 0.55f, 0.85f, alpha);
                        else if (!isTop && !isRight)
                            pixelColor = new Color(0.05f, 0.30f, 0.70f, alpha);
                        else
                            pixelColor = new Color(0.10f, 0.40f, 0.80f, alpha);
                    }

                    if (dist > 0.88f && dist <= 0.98f)
                    {
                        pixelColor = new Color(0.85f, 0.95f, 1.00f, alpha);
                    }

                    texture.SetPixel(x, y, pixelColor);
                }
            }
        }
        texture.Apply();
        diamondSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return diamondSprite;
    }

    private static Sprite levelBadgeSprite;
    public static Sprite GetOrCreateLevelBadgeSprite()
    {
        if (levelBadgeSprite != null) return levelBadgeSprite;
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        float centerX = size / 2f;
        float centerY = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;

                // Golden-amber shield shape
                float px = Mathf.Abs(dx);
                float py = dy;

                float widthFactor = 1f;
                if (py < 0f)
                {
                    widthFactor = 1f - Mathf.Pow(py / -24f, 2f);
                }
                else
                {
                    widthFactor = 1f - (py / 35f);
                }

                float shieldWidth = 20f * widthFactor;
                bool inShield = px <= shieldWidth && py >= -22f && py <= 18f;

                if (!inShield)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float distFromEdge = shieldWidth - px;
                    float alpha = 1f;
                    if (distFromEdge < 1f) alpha = distFromEdge;

                    Color pixelColor;
                    if (px < 3f || py > 15f || py < -19f || distFromEdge < 3f)
                    {
                        pixelColor = new Color(0.85f, 0.65f, 0.1f, alpha); // Gold border
                    }
                    else
                    {
                        float highlight = (dx + dy) / 40f;
                        float r = 0.95f + highlight * 0.05f;
                        float g = 0.75f + highlight * 0.10f;
                        float b = 0.20f;
                        pixelColor = new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), alpha);
                    }

                    // Central star/cross detail
                    if (px <= 6f && py >= -4f && py <= 4f)
                    {
                        if (px <= 2f || (py >= -2f && py <= 2f))
                        {
                            pixelColor = new Color(1f, 0.95f, 0.8f, alpha);
                        }
                    }

                    texture.SetPixel(x, y, pixelColor);
                }
            }
        }
        texture.Apply();
        levelBadgeSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return levelBadgeSprite;
    }

    private void CreateUnifiedHUD()
    {
        // Destroy old score UI container if present in the scene to prevent overlapping
        GameObject oldScoreContainer = GameObject.Find("ScoreContainer");
        if (oldScoreContainer != null)
        {
            DestroyImmediate(oldScoreContainer);
        }

        GameObject oldScoreTextGo = GameObject.Find("HUD_ScoreText");
        if (oldScoreTextGo != null)
        {
            DestroyImmediate(oldScoreTextGo);
        }

        GameObject oldTimerCover = GameObject.Find("HUD_TimerCover");
        if (oldTimerCover != null)
        {
            DestroyImmediate(oldTimerCover);
        }

        GameObject oldLevelCover = GameObject.Find("HUD_LevelCover");
        if (oldLevelCover != null)
        {
            DestroyImmediate(oldLevelCover);
        }

        GameObject oldAbilityButton = GameObject.Find("HUD_AbilityButton");
        if (oldAbilityButton != null)
        {
            DestroyImmediate(oldAbilityButton);
        }

        GameObject oldMuteButton = GameObject.Find("HUD_MuteButton");
        if (oldMuteButton != null)
        {
            DestroyImmediate(oldMuteButton);
        }

        Canvas canvas = GetMainCanvas();
        if (canvas == null) return;

        Transform uiParent = canvas.transform;
        GameObject safeArea = GameObject.Find("SafeAreaContainer");
        if (safeArea != null)
        {
            uiParent = safeArea.transform;
        }

        // 1. Create a parent container for the left counters to match timer's top-right layout
        GameObject oldLeftCounters = GameObject.Find("HUD_LeftCounters");
        if (oldLeftCounters != null)
        {
            DestroyImmediate(oldLeftCounters);
        }

        GameObject leftCounters = new GameObject("HUD_LeftCounters");
        leftCounters.transform.SetParent(uiParent, false);
        RectTransform leftCountersRect = leftCounters.AddComponent<RectTransform>();
        leftCountersRect.anchorMin = new Vector2(0f, 1f); // Top Left
        leftCountersRect.anchorMax = new Vector2(0f, 1f);
        leftCountersRect.pivot = new Vector2(0f, 1f);
        leftCountersRect.anchoredPosition = new Vector2(20f, -20f); // Margin from top-left
        leftCountersRect.sizeDelta = new Vector2(341f, 68f); // Height adjusted to fit two rows
        leftCountersRect.localScale = new Vector3(2.15f, 2.15f, 1f); // 215% scale - slightly larger than original 200%, but fits on screen

        // Diamond Capsule (HUD_ScoreCover)
        GameObject scoreCover = new GameObject("HUD_ScoreCover");
        scoreCover.transform.SetParent(leftCounters.transform, false);
        RectTransform scoreCoverRect = scoreCover.AddComponent<RectTransform>();
        scoreCoverRect.anchorMin = new Vector2(0f, 1f);
        scoreCoverRect.anchorMax = new Vector2(0f, 1f);
        scoreCoverRect.pivot = new Vector2(0f, 1f);
        scoreCoverRect.anchoredPosition = new Vector2(0f, 0f);
        scoreCoverRect.sizeDelta = new Vector2(108f, 32f); // Widened to 108 to match custom banner ratio

        Image scoreCoverImg = scoreCover.AddComponent<Image>();
        bool diamondCustomLoaded = false;
        Sprite diamondCounterSprite = Resources.Load<Sprite>("Diamound _counter");
        if (diamondCounterSprite != null)
        {
            scoreCoverImg.sprite = diamondCounterSprite;
            scoreCoverImg.type = Image.Type.Simple;
            scoreCoverImg.color = Color.white;
            diamondCustomLoaded = true;
        }
        else
        {
            scoreCoverImg.sprite = GetOrCreateRoundedRectSprite();
            scoreCoverImg.type = Image.Type.Sliced;
            scoreCoverImg.color = new Color32(22, 28, 43, 220); // Semi-transparent dark capsule fallback
        }

        // Diamond Icon (HUD_DiamondIcon)
        GameObject diamondIconGo = new GameObject("HUD_DiamondIcon");
        diamondIconGo.transform.SetParent(scoreCover.transform, false);
        RectTransform diamondIconRect = diamondIconGo.AddComponent<RectTransform>();
        diamondIconRect.anchorMin = new Vector2(0f, 0.5f);
        diamondIconRect.anchorMax = new Vector2(0f, 0.5f);
        diamondIconRect.pivot = new Vector2(0f, 0.5f);
        diamondIconRect.anchoredPosition = new Vector2(6f, 0f);
        diamondIconRect.sizeDelta = new Vector2(24f, 24f);

        Image diamondIconImg = diamondIconGo.AddComponent<Image>();
        diamondIconImg.sprite = GetOrCreateDiamondSprite();
        diamondIconImg.type = Image.Type.Simple;
        diamondIconImg.color = Color.white;

        if (diamondCustomLoaded)
        {
            // The custom sprite already contains the diamond icon, so hide the procedural one
            diamondIconGo.SetActive(false);
        }

        Debug.Log($"[HUD] CreateUnifiedHUD - Creating fresh HUD_ScoreText");

        GameObject scoreGo = new GameObject("HUD_ScoreText");
        scoreGo.transform.SetParent(scoreCover.transform, false);
        hudScoreText = scoreGo.AddComponent<TextMeshProUGUI>();
        scoreText = hudScoreText;
        Debug.Log($"[HUD] Created new HUD_ScoreText: {hudScoreText}");

        RectTransform scoreRect = scoreText.GetComponent<RectTransform>();
        if (scoreRect == null) scoreRect = scoreText.gameObject.AddComponent<RectTransform>();
        scoreRect.anchorMin = Vector2.zero;
        scoreRect.anchorMax = Vector2.one;
        scoreRect.sizeDelta = Vector2.zero;
        scoreRect.anchoredPosition = Vector2.zero;
        if (diamondCustomLoaded)
        {
            scoreRect.offsetMin = new Vector2(36f, 0f); // Offset past built-in diamond icon
            scoreRect.offsetMax = new Vector2(-6f, 0f);
        }
        else
        {
            scoreRect.offsetMin = new Vector2(28f, 0f); // offset past the icon
            scoreRect.offsetMax = new Vector2(-4f, 0f);
        }

        scoreText.alignment = TextAlignmentOptions.Center;
        scoreText.fontSize = 14;
        scoreText.color = Color.white;
        if (GameFontManager.TitleFont != null)
        {
            scoreText.font = GameFontManager.TitleFont;
        }
        scoreText.fontStyle = FontStyles.Bold;
        scoreText.outlineColor = new Color32(20, 20, 50, 255);
        scoreText.outlineWidth = 0.2f;

        // Coin Capsule (HUD_CoinsCover)
        GameObject coinsCover = new GameObject("HUD_CoinsCover");
        coinsCover.transform.SetParent(leftCounters.transform, false);
        RectTransform coinsCoverRect = coinsCover.AddComponent<RectTransform>();
        coinsCoverRect.anchorMin = new Vector2(0f, 1f);
        coinsCoverRect.anchorMax = new Vector2(0f, 1f);
        coinsCoverRect.pivot = new Vector2(0f, 1f);
        coinsCoverRect.anchoredPosition = new Vector2(0f, -36f); // Underneath Diamonds
        coinsCoverRect.sizeDelta = new Vector2(108f, 32f); // Widened to 108 to match custom banner ratio

        Image coinsCoverImg = coinsCover.AddComponent<Image>();
        bool coinCustomLoaded = false;
        Sprite coinCounterSprite = Resources.Load<Sprite>("Coin_counter");
        if (coinCounterSprite != null)
        {
            coinsCoverImg.sprite = coinCounterSprite;
            coinsCoverImg.type = Image.Type.Simple;
            coinsCoverImg.color = Color.white;
            coinCustomLoaded = true;
        }
        else
        {
            coinsCoverImg.sprite = GetOrCreateRoundedRectSprite();
            coinsCoverImg.type = Image.Type.Sliced;
            coinsCoverImg.color = new Color32(22, 28, 43, 220); // Semi-transparent dark capsule fallback
        }

        // Coin Icon (HUD_CoinIcon)
        GameObject coinIconGo = new GameObject("HUD_CoinIcon");
        coinIconGo.transform.SetParent(coinsCover.transform, false);
        RectTransform coinIconRect = coinIconGo.AddComponent<RectTransform>();
        coinIconRect.anchorMin = new Vector2(0f, 0.5f);
        coinIconRect.anchorMax = new Vector2(0f, 0.5f);
        coinIconRect.pivot = new Vector2(0f, 0.5f);
        coinIconRect.anchoredPosition = new Vector2(6f, 0f);
        coinIconRect.sizeDelta = new Vector2(24f, 24f);

        Image coinIconImg = coinIconGo.AddComponent<Image>();
        coinIconImg.sprite = GetOrCreateCoinSprite();
        coinIconImg.type = Image.Type.Simple;
        coinIconImg.color = Color.white;

        if (coinCustomLoaded)
        {
            // The custom sprite already contains the coin icon, so hide the procedural one
            coinIconGo.SetActive(false);
        }

        // Coins Text
        GameObject coinsTextGo = new GameObject("HUD_CoinsText");
        coinsTextGo.transform.SetParent(coinsCover.transform, false);
        hudCoinsText = coinsTextGo.AddComponent<TextMeshProUGUI>();

        RectTransform coinsTextRect = hudCoinsText.GetComponent<RectTransform>();
        coinsTextRect.anchorMin = Vector2.zero;
        coinsTextRect.anchorMax = Vector2.one;
        coinsTextRect.sizeDelta = Vector2.zero;
        coinsTextRect.anchoredPosition = Vector2.zero;
        if (coinCustomLoaded)
        {
            coinsTextRect.offsetMin = new Vector2(36f, 0f); // Offset past built-in coin icon
            coinsTextRect.offsetMax = new Vector2(-6f, 0f);
        }
        else
        {
            coinsTextRect.offsetMin = new Vector2(28f, 0f); // offset past the icon
            coinsTextRect.offsetMax = new Vector2(-4f, 0f);
        }

        hudCoinsText.alignment = TextAlignmentOptions.Center;
        hudCoinsText.fontSize = 14;
        hudCoinsText.color = Color.white;
        if (GameFontManager.TitleFont != null)
        {
            hudCoinsText.font = GameFontManager.TitleFont;
        }
        hudCoinsText.fontStyle = FontStyles.Bold;
        hudCoinsText.outlineColor = new Color32(20, 20, 50, 255);
        hudCoinsText.outlineWidth = 0.2f;

        // Level Capsule (HUD_LevelCover)
        GameObject levelCover = new GameObject("HUD_LevelCover");
        levelCover.transform.SetParent(leftCounters.transform, false);
        RectTransform levelCoverRect = levelCover.AddComponent<RectTransform>();
        levelCoverRect.anchorMin = new Vector2(0f, 1f);
        levelCoverRect.anchorMax = new Vector2(0f, 1f);
        levelCoverRect.pivot = new Vector2(0f, 1f);
        levelCoverRect.anchoredPosition = new Vector2(120f, 0f); // Right next to Diamond
        levelCoverRect.sizeDelta = new Vector2(101f, 32f);

        Image levelCoverImg = levelCover.AddComponent<Image>();
        levelCoverImg.sprite = GetOrCreateRoundedRectSprite();
        levelCoverImg.type = Image.Type.Sliced;
        levelCoverImg.color = new Color32(22, 28, 43, 220); // Semi-transparent dark capsule

        // Level Badge Icon (HUD_LevelIcon)
        GameObject levelIconGo = new GameObject("HUD_LevelIcon");
        levelIconGo.transform.SetParent(levelCover.transform, false);
        RectTransform levelIconRect = levelIconGo.AddComponent<RectTransform>();
        levelIconRect.anchorMin = new Vector2(0f, 0.5f);
        levelIconRect.anchorMax = new Vector2(0f, 0.5f);
        levelIconRect.pivot = new Vector2(0f, 0.5f);
        levelIconRect.anchoredPosition = new Vector2(6f, 0f);
        levelIconRect.sizeDelta = new Vector2(24f, 24f);

        Image levelIconImg = levelIconGo.AddComponent<Image>();
        levelIconImg.sprite = GetOrCreateLevelBadgeSprite();
        levelIconImg.type = Image.Type.Simple;
        levelIconImg.color = Color.white;

        // Level Text (e.g. LEVEL 1)
        GameObject levelTextGo = new GameObject("HUD_LevelText");
        levelTextGo.transform.SetParent(levelCover.transform, false);
        hudLevelText = levelTextGo.AddComponent<TextMeshProUGUI>();

        RectTransform levelTextRect = hudLevelText.GetComponent<RectTransform>();
        levelTextRect.anchorMin = Vector2.zero;
        levelTextRect.anchorMax = Vector2.one;
        levelTextRect.sizeDelta = Vector2.zero;
        levelTextRect.anchoredPosition = Vector2.zero;
        levelTextRect.offsetMin = new Vector2(28f, 0f); // offset past the badge
        levelTextRect.offsetMax = new Vector2(-4f, 0f);

        hudLevelText.alignment = TextAlignmentOptions.Center;
        hudLevelText.fontSize = 13;
        hudLevelText.color = new Color(0.95f, 0.75f, 0.2f, 1f); // Golden-amber level indicator
        if (GameFontManager.TitleFont != null)
        {
            hudLevelText.font = GameFontManager.TitleFont;
        }
        hudLevelText.fontStyle = FontStyles.Bold;
        hudLevelText.outlineColor = new Color32(20, 20, 50, 255);
        hudLevelText.outlineWidth = 0.2f;
        hudLevelText.text = $"LEVEL {SelectedStageIndex}";


        // 2. Load the custom timer sprite x_0 from resources
        Sprite timerSprite = null;
        Sprite[] timerSprites = Resources.LoadAll<Sprite>("x");
        if (timerSprites != null && timerSprites.Length > 0)
        {
            timerSprite = timerSprites[0];
        }

        // 3. Dynamically slice timerSprite at runtime into clock and bar sprites
        Sprite clockSprite = null;
        Sprite barSprite = null;

        if (timerSprite != null)
        {
            Rect rect = timerSprite.rect;
            float clockWidth = rect.height; // Assuming clock is square (230x230)
            float barWidth = rect.width - clockWidth; // Remaining is bar (766)

            Rect clockRect = new Rect(rect.x, rect.y, clockWidth, rect.height);
            Rect barRect = new Rect(rect.x + clockWidth, rect.y, barWidth, rect.height);

            clockSprite = Sprite.Create(timerSprite.texture, clockRect, new Vector2(0.5f, 0.5f), timerSprite.pixelsPerUnit);
            barSprite = Sprite.Create(timerSprite.texture, barRect, new Vector2(0.5f, 0.5f), timerSprite.pixelsPerUnit);
        }

        Vector2 timerSize = new Vector2(180f, 30f); // Wider timer bar to fill space

        // 4. Timer container anchored to the top-right corner
        GameObject timerCover = new GameObject("HUD_TimerCover");
        timerCover.transform.SetParent(uiParent, false);
        RectTransform timerCoverRect = timerCover.AddComponent<RectTransform>();
        timerCoverRect.anchorMin = new Vector2(1f, 1f); // Top Right
        timerCoverRect.anchorMax = new Vector2(1f, 1f);
        timerCoverRect.pivot = new Vector2(1f, 1f);
        timerCoverRect.anchoredPosition = new Vector2(-20f, -20f); // Position at top-right corner
        timerCoverRect.sizeDelta = timerSize;
        timerCoverRect.localScale = new Vector3(2.15f, 2.15f, 1f); // Match left counters scale

        // 5. Clock Image (left side, width 30)
        GameObject clockGo = new GameObject("HUD_TimerClock");
        clockGo.transform.SetParent(timerCover.transform, false);
        RectTransform clockRectTrans = clockGo.AddComponent<RectTransform>();
        clockRectTrans.anchorMin = new Vector2(0f, 0.5f);
        clockRectTrans.anchorMax = new Vector2(0f, 0.5f);
        clockRectTrans.pivot = new Vector2(0f, 0.5f);
        clockRectTrans.anchoredPosition = Vector2.zero;
        clockRectTrans.sizeDelta = new Vector2(30f, 30f);

        Image clockImg = clockGo.AddComponent<Image>();
        if (clockSprite != null)
        {
            clockImg.sprite = clockSprite;
            clockImg.type = Image.Type.Simple;
            clockImg.color = Color.white;
        }
        else
        {
            clockImg.sprite = GetOrCreateRoundedRectSprite();
            clockImg.type = Image.Type.Sliced;
            clockImg.color = new Color32(230, 80, 80, 255);
        }

        // 6. Empty Shaded Bar Silhouette Background (stretches remaining width)
        GameObject emptyBarGo = new GameObject("HUD_TimerBarEmpty");
        emptyBarGo.transform.SetParent(timerCover.transform, false);
        RectTransform emptyBarRectTrans = emptyBarGo.AddComponent<RectTransform>();
        emptyBarRectTrans.anchorMin = new Vector2(0f, 0.5f);
        emptyBarRectTrans.anchorMax = new Vector2(1f, 0.5f); // Stretch horizontally
        emptyBarRectTrans.pivot = new Vector2(0f, 0.5f);
        emptyBarRectTrans.offsetMin = new Vector2(30f, -15f); // Start after clock
        emptyBarRectTrans.offsetMax = new Vector2(0f, 15f);  // Fill to right edge

        Image emptyBarImg = emptyBarGo.AddComponent<Image>();
        if (barSprite != null)
        {
            emptyBarImg.sprite = barSprite;
            emptyBarImg.type = Image.Type.Simple;
            emptyBarImg.color = new Color32(80, 80, 85, 255);
        }
        else
        {
            emptyBarImg.sprite = GetOrCreateRoundedRectSprite();
            emptyBarImg.type = Image.Type.Sliced;
            emptyBarImg.color = new Color32(40, 45, 60, 255);
        }

        // 7. Active Progress Bar Foreground
        GameObject filledBarGo = new GameObject("HUD_TimerBarFilled");
        filledBarGo.transform.SetParent(timerCover.transform, false);
        RectTransform filledBarRectTrans = filledBarGo.AddComponent<RectTransform>();
        filledBarRectTrans.anchorMin = new Vector2(0f, 0.5f);
        filledBarRectTrans.anchorMax = new Vector2(1f, 0.5f); // Stretch horizontally
        filledBarRectTrans.pivot = new Vector2(0f, 0.5f);
        filledBarRectTrans.offsetMin = new Vector2(30f, -15f); // Start after clock
        filledBarRectTrans.offsetMax = new Vector2(0f, 15f);  // Fill to right edge

        timerBarFillImage = filledBarGo.AddComponent<Image>();
        if (barSprite != null)
        {
            timerBarFillImage.sprite = barSprite;
            timerBarFillImage.type = Image.Type.Filled;
            timerBarFillImage.fillMethod = Image.FillMethod.Horizontal;
            timerBarFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            timerBarFillImage.color = Color.white;
        }
        else
        {
            timerBarFillImage.sprite = GetOrCreateRoundedRectSprite();
            timerBarFillImage.type = Image.Type.Filled;
            timerBarFillImage.fillMethod = Image.FillMethod.Horizontal;
            timerBarFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            timerBarFillImage.color = new Color(0.95f, 0.5f, 0.1f, 1f);
        }

        // 8. Timer Text
        GameObject timerGo = new GameObject("HUD_TimerText");
        timerGo.transform.SetParent(timerCover.transform, false);
        RectTransform timerRect = timerGo.AddComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0f, 0.5f);
        timerRect.anchorMax = new Vector2(1f, 0.5f); // Stretch horizontally
        timerRect.pivot = new Vector2(0.5f, 0.5f);
        timerRect.offsetMin = new Vector2(30f, -15f); // Start after clock
        timerRect.offsetMax = new Vector2(0f, 15f);  // Fill to right edge

        hudTimerText = timerGo.AddComponent<TextMeshProUGUI>();
        hudTimerText.alignment = TextAlignmentOptions.Center;
        hudTimerText.fontSize = 18;
        hudTimerText.color = Color.white;
        if (GameFontManager.BodyFont != null)
        {
            hudTimerText.font = GameFontManager.BodyFont;
        }
        hudTimerText.fontStyle = FontStyles.Bold;
        hudTimerText.outlineColor = new Color32(20, 20, 50, 255);
        hudTimerText.outlineWidth = 0.2f;

        // Big Warning Text (Off by default)
        warningTextGo = new GameObject("HUD_BossWarning");
        warningTextGo.transform.SetParent(uiParent, false);
        RectTransform warnRect = warningTextGo.AddComponent<RectTransform>();
        warnRect.anchoredPosition = new Vector2(0f, 100f);
        warnRect.sizeDelta = new Vector2(500f, 80f);

        var warnTmp = warningTextGo.AddComponent<TextMeshProUGUI>();
        warnTmp.fontSize = 32;
        warnTmp.alignment = TextAlignmentOptions.Center;
        warnTmp.color = Color.red;
        warnTmp.fontStyle = FontStyles.Bold;
        warnTmp.text = "BOSS INCOMING!";
        warningTextGo.SetActive(false);

        // Create Ability Button programmatically
        GameObject abilityBtnGo = new GameObject("HUD_AbilityButton");
        abilityBtnGo.transform.SetParent(uiParent, false);
        RectTransform abilityBtnRect = abilityBtnGo.AddComponent<RectTransform>();
        abilityBtnRect.anchorMin = new Vector2(1f, 0f); // Bottom Right
        abilityBtnRect.anchorMax = new Vector2(1f, 0f);
        abilityBtnRect.pivot = new Vector2(1f, 0f);
        abilityBtnRect.anchoredPosition = new Vector2(-40f, 40f);
        abilityBtnRect.sizeDelta = new Vector2(110f, 110f);

        Image abilityBtnImg = abilityBtnGo.AddComponent<Image>();
        abilityBtnImg.sprite = GetOrCreateRoundedRectSprite(); // Flat rounded rect
        abilityBtnImg.type = Image.Type.Sliced;

        hudAbilityButton = abilityBtnGo.AddComponent<Button>();

        // Wire onClick to player's TriggerAbilityIfAvailable with scale bounce feedback
        hudAbilityButton.onClick.AddListener(() => {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("click");
            StartCoroutine(BounceButtonRoutine(abilityBtnRect));

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("Player");
            if (player != null)
            {
                PlayerController pc = player.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.TriggerAbilityIfAvailable();
                }
            }
        });

        // Add Outline to the button for visual pop
        Outline btnOutline = abilityBtnGo.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0f, 0f, 0f, 0.5f);
        btnOutline.effectDistance = new Vector2(2f, -2f);

        // Add Label text to the button
        GameObject btnTextGo = new GameObject("Text");
        btnTextGo.transform.SetParent(abilityBtnGo.transform, false);
        RectTransform btnTextRect = btnTextGo.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;

        hudAbilityButtonText = btnTextGo.AddComponent<TextMeshProUGUI>();
        hudAbilityButtonText.alignment = TextAlignmentOptions.Center;
        hudAbilityButtonText.fontSize = 15;
        hudAbilityButtonText.color = Color.white;
        hudAbilityButtonText.raycastTarget = false; // Prevent blocking click events
        if (GameFontManager.TitleFont != null)
        {
            hudAbilityButtonText.font = GameFontManager.TitleFont;
        }
        hudAbilityButtonText.fontStyle = FontStyles.Bold;
        hudAbilityButtonText.outlineColor = new Color32(20, 20, 50, 255);
        hudAbilityButtonText.outlineWidth = 0.2f;

        // Apply initial visual settings based on selected player
        int initialUses = 5;
        GameObject initialPlayer = GameObject.FindGameObjectWithTag("Player");
        if (initialPlayer == null) initialPlayer = GameObject.Find("Player");
        if (initialPlayer != null)
        {
            PlayerController pc = initialPlayer.GetComponent<PlayerController>();
            if (pc != null)
            {
                initialUses = pc.AbilityUsesLeft;
            }
        }
        UpdateAbilityButtonVisuals(initialUses);

        // --- HUD MUTE BUTTON ---
        GameObject hudMuteGo = new GameObject("HUD_MuteButton");
        hudMuteGo.transform.SetParent(uiParent, false);
        RectTransform hudMuteRect = hudMuteGo.AddComponent<RectTransform>();
        hudMuteRect.anchorMin = new Vector2(1f, 1f); // Top Right
        hudMuteRect.anchorMax = new Vector2(1f, 1f);
        hudMuteRect.pivot = new Vector2(1f, 1f);
        hudMuteRect.anchoredPosition = new Vector2(-20f, -90f); // Right below timer cover (bottom of timer is -80)
        hudMuteRect.sizeDelta = new Vector2(36f, 36f);
        hudMuteRect.localScale = new Vector3(2f, 2f, 1f); // 200% scale to match other HUD elements

        Image hudMuteImg = hudMuteGo.AddComponent<Image>();
        hudMuteImg.sprite = GetOrCreateRoundedRectSprite();
        hudMuteImg.type = Image.Type.Sliced;

        Button hudMuteBtn = hudMuteGo.AddComponent<Button>();
        
        // Add Outline to the button for visual pop
        Outline hudMuteOutline = hudMuteGo.AddComponent<Outline>();
        hudMuteOutline.effectColor = new Color(0f, 0f, 0f, 0.5f);
        hudMuteOutline.effectDistance = new Vector2(2f, -2f);

        // Add Label text to the button
        GameObject hudMuteTextGo = new GameObject("Text");
        hudMuteTextGo.transform.SetParent(hudMuteGo.transform, false);
        RectTransform hudMuteTextRect = hudMuteTextGo.AddComponent<RectTransform>();
        hudMuteTextRect.anchorMin = Vector2.zero;
        hudMuteTextRect.anchorMax = Vector2.one;
        hudMuteTextRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI hudMuteText = hudMuteTextGo.AddComponent<TextMeshProUGUI>();
        hudMuteText.alignment = TextAlignmentOptions.Center;
        hudMuteText.fontSize = 16;
        hudMuteText.color = Color.white;
        hudMuteText.raycastTarget = false;
        if (GameFontManager.TitleFont != null)
        {
            hudMuteText.font = GameFontManager.TitleFont;
        }
        hudMuteText.fontStyle = FontStyles.Bold;
        hudMuteText.outlineColor = new Color32(20, 20, 50, 255);
        hudMuteText.outlineWidth = 0.2f;

        // Function to update the button display state
        System.Action updateHudMuteVisuals = () =>
        {
            bool isMuted = SoundManager.Instance != null && SoundManager.Instance.IsMuted;
            Sprite soundOnSprite = LoadSpriteFromResources("Sound_on_button");
            Sprite soundOffSprite = LoadSpriteFromResources("sound_off_botton");

            if (soundOnSprite != null && soundOffSprite != null)
            {
                hudMuteImg.sprite = isMuted ? soundOffSprite : soundOnSprite;
                hudMuteImg.type = Image.Type.Simple;
                hudMuteImg.color = Color.white;
                hudMuteText.text = "";
            }
            else
            {
                hudMuteText.text = isMuted ? "🔇" : "🔊";
                hudMuteImg.sprite = GetOrCreateRoundedRectSprite();
                hudMuteImg.type = Image.Type.Sliced;
                hudMuteImg.color = isMuted ? new Color(0.9f, 0.25f, 0.25f, 1.0f) : new Color(0.15f, 0.55f, 0.9f, 1.0f);
            }
        };

        // Initialize state
        updateHudMuteVisuals();

        hudMuteBtn.onClick.AddListener(() =>
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.ToggleMute();
                SoundManager.Instance.PlaySFX("click");
            }
            updateHudMuteVisuals();
        });

        UpdateScoreUI();
        UpdateCoinsUI();
        UpdateHUDTimerText();
    }

    private void Update()
    {
        if (Time.timeScale == 0f || isGameOver || isVictory) return;

        if (!IsBossTime)
        {
            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                IsBossTime = true;
                TriggerBossFight();
            }
            UpdateHUDTimerText();
        }
    }

    private void UpdateHUDTimerText()
    {
        if (hudTimerText == null) return;

        if (IsBossTime)
        {
            hudTimerText.text = "BOSS!";
            if (timerBarFillImage != null)
            {
                timerBarFillImage.fillAmount = 0f;
            }
        }
        else
        {
            // Countdown text in seconds format (e.g. 59s)
            int secondsRemaining = Mathf.CeilToInt(TimeRemaining);
            hudTimerText.text = $"{secondsRemaining}s";
            
            if (timerBarFillImage != null && CurrentLevelConfig != null && CurrentLevelConfig.duration > 0f)
            {
                float ratio = TimeRemaining / CurrentLevelConfig.duration;
                timerBarFillImage.fillAmount = ratio;
            }
        }
    }

    private void TriggerBossFight()
    {
        UpdateHUDTimerText();
        if (warningTextGo != null)
        {
            warningTextGo.SetActive(true);
            Destroy(warningTextGo, 3.0f); // Auto-hide warning after 3 seconds
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("boss_warning");
        }

        StartCoroutine(FadeBossEnvironmentRoutine());
    }

    private System.Collections.IEnumerator FadeBossEnvironmentRoutine()
    {
        Canvas canvas = GetMainCanvas();
        if (canvas == null) yield break;

        Transform uiParent = canvas.transform;
        GameObject safeArea = GameObject.Find("SafeAreaContainer");
        if (safeArea != null) uiParent = safeArea.transform;

        bossEnvironmentOverlay = new GameObject("BossEnvironmentOverlay");
        bossEnvironmentOverlay.transform.SetParent(uiParent, false);
        bossEnvironmentOverlay.transform.SetAsFirstSibling(); // Push to back of UI, covering game but behind HUD

        RectTransform overlayRect = bossEnvironmentOverlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;

        Image overlayBg = bossEnvironmentOverlay.AddComponent<Image>();
        overlayBg.raycastTarget = false; // Don't block clicks

        Color targetColor = CurrentLevelConfig != null ? CurrentLevelConfig.bossEnvironmentColor : new Color(0.4f, 0f, 0f, 0.75f);
        Color startColor = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);
        
        float duration = 2.0f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            overlayBg.color = Color.Lerp(startColor, targetColor, elapsed / duration);
            yield return null;
        }
        overlayBg.color = targetColor;
    }

    public void OnBossDefeated()
    {
        // Check if there are other bosses still alive (the dying one is still active at this exact moment)
        var activeEnemies = EnemyController.ActiveEnemies;
        int bossesAlive = 0;
        foreach (var e in activeEnemies)
        {
            if (e != null && e.IsBoss) bossesAlive++;
        }
        
        // If there is more than 1 boss alive (the dying one + others), don't trigger victory yet!
        if (bossesAlive > 1) return;

        if (isGameOver || isVictory) return;
        isVictory = true;

        if (bossEnvironmentOverlay != null)
        {
            Destroy(bossEnvironmentOverlay);
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
            SoundManager.Instance.PlayBGM("menu"); // Play menu BGM as victory theme
        }

        // Save Gems with 25 bonus gems for clearing!
        int bonusGems = 25;
        SaveSystem.AddGems(score + coins + bonusGems);

        // Unlock next stage in progression
        SaveSystem.UnlockStage(SelectedStageIndex + 1);

        // Freeze game
        Time.timeScale = 0f;

        // Show Weapon Upgrade Selection instead of Victory Panel
        GameObject uiGo = new GameObject("UpgradeSelectionUI");
        UpgradeSelectionUI ui = uiGo.AddComponent<UpgradeSelectionUI>();
        ui.Show();

        // Send stats to Android Bridge
        SendVictoryToAndroid(score, coins, bonusGems);
        SendGemsUpdatedToAndroid(SaveSystem.GetGemsBank());
    }

    private void ShowVictoryPanel(int bonusGems)
    {
        Canvas canvas = GetMainCanvas();
        if (canvas == null) return;

        Transform uiParent = canvas.transform;
        GameObject safeArea = GameObject.Find("SafeAreaContainer");
        if (safeArea != null)
        {
            uiParent = safeArea.transform;
        }

        // Fullscreen dark overlay
        victoryPanel = new GameObject("VictoryPanelOverlay");
        victoryPanel.transform.SetParent(uiParent, false);

        RectTransform overlayRect = victoryPanel.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;

        Image overlayBg = victoryPanel.AddComponent<Image>();
        overlayBg.color = new Color(0f, 0f, 0f, 0.65f); // Semi-transparent black

        // Center Dialog Box
        GameObject dialog = new GameObject("VictoryDialog");
        dialog.transform.SetParent(victoryPanel.transform, false);

        RectTransform dialogRect = dialog.AddComponent<RectTransform>();
        dialogRect.sizeDelta = new Vector2(460f, 400f);
        dialogRect.anchoredPosition = Vector2.zero;

        Image dialogBg = dialog.AddComponent<Image>();
        dialogBg.sprite = GameSpriteManager.GetSprite("panel_purple");
        dialogBg.type = Image.Type.Sliced;
        dialogBg.color = Color.white;

        Outline outline = dialog.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.8f, 0.4f, 0.65f); // Glowing green outline
        outline.effectDistance = new Vector2(2f, 2f);

        // Title text
        CreateText(dialog, "Title", "VICTORY", 42, new Vector2(0f, 130f), Color.yellow, true);
        CreateText(dialog, "Subtitle", $"{CurrentLevelConfig.stageName} CLEARED", 18, new Vector2(0f, 90f), Color.white);

        // Stats Backdrop inside Dialog
        GameObject statsBox = new GameObject("StatsBox");
        statsBox.transform.SetParent(dialog.transform, false);
        RectTransform statsRect = statsBox.AddComponent<RectTransform>();
        statsRect.sizeDelta = new Vector2(380f, 140f);
        statsRect.anchoredPosition = new Vector2(0f, -10f);

        Image statsBg = statsBox.AddComponent<Image>();
        statsBg.sprite = GameSpriteManager.GetSprite("panel_blue_top");
        statsBg.type = Image.Type.Sliced;
        statsBg.color = Color.white;

        string rewardStr = $"Gems Collected: {score}\nCoins Collected: {coins}\nClear Bonus: +{bonusGems} Gems!\n\nTotal Bank: {SaveSystem.GetGemsBank()}";
        CreateText(statsBox, "RewardDetails", rewardStr, 18, Vector2.zero, new Color(0.4f, 1f, 0.7f, 1f));

        // Buttons
        float buttonY = -120f;
        // Button 1: Next Level (only if there is a next level config)
        if (SelectedStageIndex < 5)
        {
            CreateButton(dialog, "NextButton", "NEXT LEVEL", new Vector2(200f, 44f), new Vector2(0f, buttonY), () =>
            {
                SelectedStage = SelectedStageIndex + 1;
                RestartGame();
            }, new Color(0.1f, 0.6f, 0.3f, 1f));
            buttonY -= 55f;
        }

        // Button 2: Menu / Restart
        CreateButton(dialog, "MenuButton", "MAIN MENU", new Vector2(200f, 44f), new Vector2(0f, buttonY), () =>
        {
            SelectedStage = SelectedStageIndex; // keep same stage
            RestartGame();
        }, new Color(0.2f, 0.3f, 0.5f, 1f));
    }

    public static void CreateText(GameObject parent, string name, string content, int fontSize, Vector2 anchoredPosition, Color color, bool bold = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        
        TMP_FontAsset customFont = bold ? GameFontManager.TitleFont : GameFontManager.BodyFont;
        if (customFont != null)
        {
            tmp.font = customFont;
        }

        if (bold) tmp.fontStyle = FontStyles.Bold;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(500, 100);
        rect.anchoredPosition = anchoredPosition;
    }

    private void CreateButton(GameObject parent, string name, string label, Vector2 size, Vector2 anchoredPosition, System.Action onClickAction, Color normalColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        
        Image img = go.AddComponent<Image>();
        Sprite btnSprite = null;
        if (name.Contains("Start") || label.Contains("START") || name.Contains("Play") || label.Contains("PLAY") || name.Contains("Next") || label.Contains("NEXT"))
            btnSprite = GameSpriteManager.GetSprite("button_play_green");
        else if (name.Contains("Back") || label.Contains("BACK") || name.Contains("Retry") || label.Contains("RETRY") || name.Contains("Restart") || label.Contains("RESTART"))
            btnSprite = GameSpriteManager.GetSprite("button_back_red");
        else if (name.Contains("Close") || label.Contains("CLOSE") || name.Contains("Quit") || label.Contains("QUIT"))
            btnSprite = GameSpriteManager.GetSprite("button_close_orange");
        else if (name.Contains("Shop") || label.Contains("SHOP"))
            btnSprite = GameSpriteManager.GetSprite("button_shop_blue");
        else
            btnSprite = GameSpriteManager.GetSprite("button_home_green");

        if (btnSprite != null)
        {
            img.sprite = btnSprite;
            img.color = Color.white;
        }
        else
        {
            img.sprite = GetOrCreateRoundedRectSprite();
            img.color = normalColor;
        }
        img.type = Image.Type.Sliced;
        
        Button btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() => {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("click");
            onClickAction?.Invoke();
        });
        
        GameObject labelGo = new GameObject("Text");
        labelGo.transform.SetParent(go.transform, false);
        RectTransform labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18;
        
        TMP_FontAsset customFont = GameFontManager.TitleFont;
        if (customFont != null)
        {
            tmp.font = customFont;
        }

        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreUI();
    }

    public int GetScore()
    {
        return score;
    }

    public void SpawnGem(Vector3 position)
    {
        if (gemPrefab != null)
        {
            ObjectPoolManager.Instance.SpawnObject(gemPrefab, position, Quaternion.identity);

            // Level 1 Double Gem Drop hook!
            if (SelectedStageIndex == 1)
            {
                // Spawn a second gem with a tiny offset so they don't overlap completely
                Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0f);
                ObjectPoolManager.Instance.SpawnObject(gemPrefab, position + offset, Quaternion.identity);
            }
        }
    }

    private static Sprite heartSprite;
    public static Sprite GetOrCreateHeartSprite()
    {
        if (heartSprite != null) return heartSprite;
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        float centerX = size / 2f;
        float centerY = size * 0.42f; // Offset center for math scaling

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Normalize coordinates relative to center, scaled to fit nice shape
                float dx = (x - centerX) / (size * 0.35f);
                float dy = (y - centerY) / (size * 0.35f);

                float x2 = dx * dx;
                float sqrtAbsX = Mathf.Sqrt(Mathf.Abs(dx));
                
                // Classic heart shape formula: x^2 + (1.2*y - sqrt(|x|))^2 <= 1
                float heartValue = x2 + Mathf.Pow(1.2f * dy - sqrtAbsX, 2f);

                if (heartValue > 1.0f)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float alpha = 1.0f;
                    // Anti-aliasing at the edge
                    if (heartValue > 0.9f)
                    {
                        alpha = Mathf.Clamp01((1.0f - heartValue) / 0.1f);
                    }

                    Color pixelColor;
                    
                    // Highlight shine (top-left inner area)
                    float shineDist = Mathf.Sqrt(Mathf.Pow(dx + 0.25f, 2f) + Mathf.Pow(dy - 0.35f, 2f));

                    if (heartValue > 0.82f)
                    {
                        // Dark red outline/border
                        pixelColor = new Color(0.55f, 0.05f, 0.1f, alpha);
                    }
                    else if (shineDist < 0.22f)
                    {
                        // White highlight shine
                        float shineAlpha = Mathf.Clamp01((0.22f - shineDist) / 0.12f);
                        pixelColor = Color.Lerp(new Color(1f, 0.22f, 0.3f, alpha), new Color(1f, 1f, 1f, alpha), shineAlpha);
                    }
                    else
                    {
                        // Gradient fill
                        float grad = (dy + 1.0f) / 2.0f;
                        float r = Mathf.Lerp(0.85f, 1.0f, grad);
                        float g = Mathf.Lerp(0.1f, 0.22f, grad);
                        float b = Mathf.Lerp(0.2f, 0.3f, grad);
                        pixelColor = new Color(r, g, b, alpha);
                    }
                    texture.SetPixel(x, y, pixelColor);
                }
            }
        }
        texture.Apply();
        heartSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return heartSprite;
    }

    public void SpawnHeart(Vector3 position)
    {
        // Hearts are disabled per user request
    }

    private GameObject proceduralGameOverPanel;

    public void OnPlayerDeath()
    {
        if (isGameOver || isVictory) return;
        isGameOver = true;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
        }

        // Save collected gems and coins to bank
        SaveSystem.AddGems(score + coins);

        // Freeze game
        Time.timeScale = 0f;

        ShowGameOverPanel();

        // Send stats to Android Bridge
        SendGameOverToAndroid(score, coins);
        SendGemsUpdatedToAndroid(SaveSystem.GetGemsBank());
    }

    private void ShowGameOverPanel()
    {
        Canvas canvas = GetMainCanvas();
        if (canvas == null) return;

        // Fullscreen dark overlay
        proceduralGameOverPanel = new GameObject("GameOverPanelOverlay");
        proceduralGameOverPanel.transform.SetParent(canvas.transform, false);

        RectTransform overlayRect = proceduralGameOverPanel.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;

        Image overlayBg = proceduralGameOverPanel.AddComponent<Image>();
        overlayBg.color = new Color(0.1f, 0f, 0f, 0.85f); 

        // Center Dialog Box
        GameObject dialog = new GameObject("GameOverDialog");
        dialog.transform.SetParent(proceduralGameOverPanel.transform, false);

        RectTransform dialogRect = dialog.AddComponent<RectTransform>();
        dialogRect.sizeDelta = new Vector2(400f, 250f);
        dialogRect.anchoredPosition = Vector2.zero;

        Image dialogBg = dialog.AddComponent<Image>();
        dialogBg.sprite = GameSpriteManager.GetSprite("panel_purple");
        dialogBg.type = Image.Type.Sliced;
        dialogBg.color = Color.white;

        Outline outline = dialog.AddComponent<Outline>();
        outline.effectColor = new Color(0.8f, 0.2f, 0.2f, 0.6f); 
        outline.effectDistance = new Vector2(2f, 2f);

        CreateText(dialog, "Title", "YOU DIED", 54, new Vector2(0f, 30f), new Color(1f, 0.3f, 0.3f, 1f), true);

        CreateButton(dialog, "RestartButton", "RETRY", new Vector2(200f, 60f), new Vector2(0f, -50f), () =>
        {
            Time.timeScale = 1f;
            RestartGame();
        }, new Color(0.7f, 0.2f, 0.2f, 1f));
    }

    public void StartNextStageSeamlessly()
    {
        SelectedStageIndex++;
        if (SelectedStageIndex > 5) SelectedStageIndex = 1;

        CurrentLevelConfig = LevelConfig.GetConfig(SelectedStageIndex);
        TimeRemaining = CurrentLevelConfig.duration;

        // Clear scene
        var enemies = GameObject.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var e in enemies) ObjectPoolManager.Instance.ReturnObjectToPool(e.gameObject);

        var projectiles = GameObject.FindObjectsByType<Projectile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var p in projectiles) ObjectPoolManager.Instance.ReturnObjectToPool(p.gameObject);

        var gems = GameObject.FindObjectsByType<CollectibleGem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var g in gems) ObjectPoolManager.Instance.ReturnObjectToPool(g.gameObject);

        var hearts = GameObject.FindObjectsByType<CollectibleHeart>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var h in hearts) ObjectPoolManager.Instance.ReturnObjectToPool(h.gameObject);

        var rockets = GameObject.FindObjectsByType<RocketProjectile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var r in rockets) Destroy(r.gameObject);
        
        var bottles = GameObject.FindObjectsByType<FireBottleProjectile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var b in bottles) Destroy(b.gameObject);

        var fireZones = GameObject.FindObjectsByType<FireZoneLogic>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var fz in fireZones) Destroy(fz.gameObject);

        // isVictory and isGameOver are reset
        isGameOver = false;
        isVictory = false;
        IsBossTime = false; // Reset Boss Time so normal enemies spawn!

        if (hudLevelText != null) hudLevelText.text = $"LEVEL {SelectedStageIndex}";
        
        if (SoundManager.Instance != null) SoundManager.Instance.PlayBGM("battle");
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public Canvas GetMainCanvas()
    {
        Canvas canvas = null;
        GameObject canvasGo = GameObject.Find("Canvas");
        if (canvasGo != null)
        {
            canvas = canvasGo.GetComponent<Canvas>();
        }
        if (canvas == null)
        {
            canvas = FindAnyObjectByType<Canvas>();
        }
        return canvas;
    }

    // StyleScoreUI is now obsolete as we use CreateUnifiedHUD instead.

    private void UpdateScoreUI()
    {
        Debug.Log($"[HUD] UpdateScoreUI - scoreText: {scoreText}, hudScoreText: {hudScoreText}, score: {score}");
        if (hudScoreText != null)
        {
            hudScoreText.text = score.ToString();
            Debug.Log($"[HUD] hudScoreText.text updated to {hudScoreText.text}");
        }
        else if (scoreText != null)
        {
            scoreText.text = score.ToString();
            Debug.Log($"[HUD] scoreText.text updated to {scoreText.text}");
        }
        else
        {
            Debug.LogError("[HUD] UpdateScoreUI - both scoreText and hudScoreText are null!");
        }
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        UpdateCoinsUI();
    }

    private void UpdateCoinsUI()
    {
        Debug.Log($"[HUD] UpdateCoinsUI - hudCoinsText: {hudCoinsText}, coins: {coins}");
        if (hudCoinsText != null)
        {
            hudCoinsText.text = coins.ToString();
            Debug.Log($"[HUD] hudCoinsText.text updated to {hudCoinsText.text}");
        }
        else
        {
            Debug.LogError("[HUD] UpdateCoinsUI - hudCoinsText is null!");
        }
    }

    public void DisableAbilityButton()
    {
        UpdateAbilityButtonVisuals(0);
    }

    public void UpdateAbilityButtonUses(int usesLeft)
    {
        UpdateAbilityButtonVisuals(usesLeft);
    }

    public void UpdateAbilityButtonVisuals(int usesLeft)
    {
        if (hudAbilityButton == null) return;

        Image abilityBtnImg = hudAbilityButton.GetComponent<Image>();
        string activePlayer = PlayerPrefs.GetString("SelectedPlayer", "Virgil");

        if (usesLeft <= 0)
        {
            if (abilityBtnImg != null)
            {
                abilityBtnImg.color = new Color(0.4f, 0.4f, 0.4f, 0.8f); // Gray out
            }
            if (hudAbilityButtonText != null)
            {
                hudAbilityButtonText.text = "❌\nUSED";
            }
            hudAbilityButton.interactable = false;
            return;
        }

        hudAbilityButton.interactable = true;
        if (activePlayer == "Vini")
        {
            if (abilityBtnImg != null)
            {
                abilityBtnImg.color = new Color(0.95f, 0.5f, 0.1f, 0.9f); // Orange tint
            }
            if (hudAbilityButtonText != null)
            {
                hudAbilityButtonText.text = $"⚡\nSAMBA FLARE\n({usesLeft} LEFT)";
            }
        }
        else
        {
            if (abilityBtnImg != null)
            {
                abilityBtnImg.color = new Color(0.15f, 0.45f, 0.9f, 0.9f); // Cyan/blue tint
            }
            if (hudAbilityButtonText != null)
            {
                hudAbilityButtonText.text = $"🛡️\nSHIELD WAVE\n({usesLeft} LEFT)";
            }
        }
    }

    private System.Collections.IEnumerator BounceButtonRoutine(RectTransform buttonRect)
    {
        Vector3 originalScale = Vector3.one;
        float elapsed = 0f;
        float duration = 0.1f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pct = elapsed / duration;
            buttonRect.localScale = Vector3.Lerp(originalScale, originalScale * 0.88f, pct);
            yield return null;
        }
        
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pct = elapsed / duration;
            buttonRect.localScale = Vector3.Lerp(originalScale * 0.88f, originalScale, pct);
            yield return null;
        }
        buttonRect.localScale = originalScale;
    }

    // --- ANDROID BRIDGE INCOMING HANDLERS ---
    
    public void AndroidAddGems(int amount)
    {
        SaveSystem.AddGems(amount);
        UpdateScoreUI();
        UpdateCoinsUI();
        if (MainMenuShopUI.Instance != null)
        {
            MainMenuShopUI.Instance.RefreshShopUI();
        }
        
        // Sync back current gems bank balance to Android
        SendGemsUpdatedToAndroid(SaveSystem.GetGemsBank());
    }

    public void AndroidSelectPlayer(string characterId)
    {
        PlayerPrefs.SetString("SelectedPlayer", characterId);
        PlayerPrefs.Save();

        if (MainMenuShopUI.Instance != null)
        {
            MainMenuShopUI.Instance.UpdateActivePlayerVisuals();
            MainMenuShopUI.Instance.SetupUI();
        }
    }

    public void AndroidRestartGame()
    {
        RestartGame();
    }
}
