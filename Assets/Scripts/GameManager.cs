using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

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
    private bool isGameOver = false;
    private bool isVictory = false;

    // HUD references
    private TextMeshProUGUI hudTimerText;
    private Image timerProgressFill;
    private GameObject warningTextGo;

    // Victory Panel programmatic reference
    private GameObject victoryPanel;

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

    private void CreateUnifiedHUD()
    {
        // Deactivate old score UI container if present in the scene to prevent overlapping
        GameObject oldScoreContainer = GameObject.Find("ScoreContainer");
        if (oldScoreContainer != null)
        {
            oldScoreContainer.SetActive(false);
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        Transform uiParent = canvas.transform;
        GameObject safeArea = GameObject.Find("SafeAreaContainer");
        if (safeArea != null)
        {
            uiParent = safeArea.transform;
        }

        // 1. Load the bar_game_top sprite
        Sprite[] topBarSprites = Resources.LoadAll<Sprite>("bar_game_top");
        Sprite topBarSprite = (topBarSprites != null && topBarSprites.Length > 0) ? topBarSprites[0] : null;

        // 2. Create the unified top-HUD background panel
        GameObject hudBarGo = new GameObject("HUD_Bar_Game_Top");
        hudBarGo.transform.SetParent(uiParent, false);

        RectTransform barRect = hudBarGo.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 1f); // Top Center
        barRect.anchorMax = new Vector2(0.5f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.anchoredPosition = new Vector2(0f, -15f); // Positioned slightly lower for 2x scale
        barRect.sizeDelta = new Vector2(405f, 80f); // Scaled based on 324x64 aspect ratio
        barRect.localScale = new Vector3(2f, 2f, 1f); // Scale UI by 200%

        Image barImage = hudBarGo.AddComponent<Image>();
        if (topBarSprite != null)
        {
            barImage.sprite = topBarSprite;
        }
        else
        {
            // Fallback to blue panel if asset not loaded
            barImage.sprite = GameSpriteManager.GetSprite("panel_blue_top");
            barImage.type = Image.Type.Sliced;
        }
        barImage.color = Color.white;

        // Add a clean outline to stand out from background
        Outline barOutline = hudBarGo.AddComponent<Outline>();
        barOutline.effectColor = new Color(0.1f, 0.1f, 0.2f, 0.4f);
        barOutline.effectDistance = new Vector2(1.5f, 1.5f);

        // 3. Score slot cover & text container (Gold Coin capsule, left side)
        GameObject coinCover = new GameObject("HUD_CoinCover");
        coinCover.transform.SetParent(hudBarGo.transform, false);
        RectTransform coinCoverRect = coinCover.AddComponent<RectTransform>();
        coinCoverRect.anchorMin = new Vector2(0f, 0.5f);
        coinCoverRect.anchorMax = new Vector2(0f, 0.5f);
        coinCoverRect.pivot = new Vector2(0.5f, 0.5f);
        coinCoverRect.anchoredPosition = new Vector2(90f, -1.25f);
        coinCoverRect.sizeDelta = new Vector2(65f, 32f);
        Image coinCoverImg = coinCover.AddComponent<Image>();
        coinCoverImg.sprite = GetOrCreateRoundedRectSprite();
        coinCoverImg.type = Image.Type.Sliced;
        coinCoverImg.color = new Color32(22, 28, 43, 255);

        if (scoreText == null)
        {
            GameObject scoreGo = new GameObject("HUD_ScoreText");
            scoreGo.transform.SetParent(hudBarGo.transform, false);
            scoreText = scoreGo.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            scoreText.transform.SetParent(hudBarGo.transform, false);
        }

        RectTransform scoreRect = scoreText.GetComponent<RectTransform>();
        if (scoreRect == null) scoreRect = scoreText.gameObject.AddComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0f, 0.5f);
        scoreRect.anchorMax = new Vector2(0f, 0.5f);
        scoreRect.pivot = new Vector2(0.5f, 0.5f);
        scoreRect.anchoredPosition = new Vector2(90f, -1.25f);
        scoreRect.sizeDelta = new Vector2(65f, 35f);

        scoreText.alignment = TextAlignmentOptions.Center;
        scoreText.fontSize = 22;
        scoreText.color = Color.white;
        if (GameFontManager.TitleFont != null)
        {
            scoreText.font = GameFontManager.TitleFont;
        }
        scoreText.fontStyle = FontStyles.Bold;
        scoreText.outlineColor = new Color32(20, 20, 50, 255);
        scoreText.outlineWidth = 0.2f;

        // 4. Gems Bank slot cover & text container (Blue Diamond capsule, middle-left side)
        GameObject bankCover = new GameObject("HUD_BankCover");
        bankCover.transform.SetParent(hudBarGo.transform, false);
        RectTransform bankCoverRect = bankCover.AddComponent<RectTransform>();
        bankCoverRect.anchorMin = new Vector2(0f, 0.5f);
        bankCoverRect.anchorMax = new Vector2(0f, 0.5f);
        bankCoverRect.pivot = new Vector2(0.5f, 0.5f);
        bankCoverRect.anchoredPosition = new Vector2(162.5f, -1.25f);
        bankCoverRect.sizeDelta = new Vector2(65f, 32f);
        Image bankCoverImg = bankCover.AddComponent<Image>();
        bankCoverImg.sprite = GetOrCreateRoundedRectSprite();
        bankCoverImg.type = Image.Type.Sliced;
        bankCoverImg.color = new Color32(22, 28, 43, 255);

        GameObject bankGo = new GameObject("HUD_BankText");
        bankGo.transform.SetParent(hudBarGo.transform, false);
        RectTransform bankRect = bankGo.AddComponent<RectTransform>();
        bankRect.anchorMin = new Vector2(0f, 0.5f);
        bankRect.anchorMax = new Vector2(0f, 0.5f);
        bankRect.pivot = new Vector2(0.5f, 0.5f);
        bankRect.anchoredPosition = new Vector2(162.5f, -1.25f);
        bankRect.sizeDelta = new Vector2(65f, 35f);

        TextMeshProUGUI bankText = bankGo.AddComponent<TextMeshProUGUI>();
        bankText.alignment = TextAlignmentOptions.Center;
        bankText.fontSize = 22;
        bankText.color = Color.white;
        if (GameFontManager.TitleFont != null)
        {
            bankText.font = GameFontManager.TitleFont;
        }
        bankText.fontStyle = FontStyles.Bold;
        bankText.outlineColor = new Color32(20, 20, 50, 255);
        bankText.outlineWidth = 0.2f;
        bankText.text = SaveSystem.GetGemsBank().ToString();

        // 5. Avatar (Eyeball) Image inside Center Circle
        GameObject avatarGo = new GameObject("HUD_Avatar");
        avatarGo.transform.SetParent(hudBarGo.transform, false);
        RectTransform avatarRect = avatarGo.AddComponent<RectTransform>();
        avatarRect.anchorMin = new Vector2(0.5f, 0.5f);
        avatarRect.anchorMax = new Vector2(0.5f, 0.5f);
        avatarRect.pivot = new Vector2(0.5f, 0.5f);
        avatarRect.anchoredPosition = new Vector2(0f, 1.5f);
        avatarRect.sizeDelta = new Vector2(52f, 52f);

        Image avatarImg = avatarGo.AddComponent<Image>();
        Sprite eyeballSprite = Resources.Load<Sprite>("player_eyeball_sprite");
        if (eyeballSprite != null)
        {
            avatarImg.sprite = eyeballSprite;
            avatarImg.color = Color.white;
        }
        else
        {
            avatarImg.color = Color.clear;
        }

        // 6. Level Progress Bar and cover (Right side)
        GameObject timerCover = new GameObject("HUD_TimerCover");
        timerCover.transform.SetParent(hudBarGo.transform, false);
        RectTransform timerCoverRect = timerCover.AddComponent<RectTransform>();
        timerCoverRect.anchorMin = new Vector2(1f, 0.5f);
        timerCoverRect.anchorMax = new Vector2(1f, 0.5f);
        timerCoverRect.pivot = new Vector2(0.5f, 0.5f);
        timerCoverRect.anchoredPosition = new Vector2(-72.5f, -1.25f);
        timerCoverRect.sizeDelta = new Vector2(115f, 32f);
        Image timerCoverImg = timerCover.AddComponent<Image>();
        timerCoverImg.sprite = GetOrCreateRoundedRectSprite();
        timerCoverImg.type = Image.Type.Sliced;
        timerCoverImg.color = new Color32(22, 28, 43, 255);

        GameObject progressContainer = new GameObject("HUD_TimerProgress");
        progressContainer.transform.SetParent(hudBarGo.transform, false);
        RectTransform progressRect = progressContainer.AddComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(1f, 0.5f);
        progressRect.anchorMax = new Vector2(1f, 0.5f);
        progressRect.pivot = new Vector2(0.5f, 0.5f);
        progressRect.anchoredPosition = new Vector2(-72.5f, -1.25f);
        progressRect.sizeDelta = new Vector2(115f, 32f);

        timerProgressFill = progressContainer.AddComponent<Image>();
        timerProgressFill.sprite = GetOrCreateRoundedRectSprite();
        timerProgressFill.type = Image.Type.Filled;
        timerProgressFill.fillMethod = Image.FillMethod.Horizontal;
        timerProgressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        timerProgressFill.color = new Color(0.12f, 0.65f, 0.95f, 0.85f);
        timerProgressFill.fillAmount = 0f;

        // 7. Timer text centered on the right progress bar
        GameObject timerGo = new GameObject("HUD_TimerText");
        timerGo.transform.SetParent(hudBarGo.transform, false);
        RectTransform timerRect = timerGo.AddComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(1f, 0.5f);
        timerRect.anchorMax = new Vector2(1f, 0.5f);
        timerRect.pivot = new Vector2(0.5f, 0.5f);
        timerRect.anchoredPosition = new Vector2(-72.5f, -1.25f);
        timerRect.sizeDelta = new Vector2(115f, 35f);

        hudTimerText = timerGo.AddComponent<TextMeshProUGUI>();
        hudTimerText.alignment = TextAlignmentOptions.Center;
        hudTimerText.fontSize = 20;
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

        UpdateScoreUI();
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
            if (timerProgressFill != null)
            {
                timerProgressFill.fillAmount = 1f;
            }
        }
        else
        {
            int minutes = Mathf.FloorToInt(TimeRemaining / 60f);
            int seconds = Mathf.FloorToInt(TimeRemaining % 60f);
            hudTimerText.text = $"{minutes:D2}:{seconds:D2}";
            if (timerProgressFill != null && CurrentLevelConfig != null && CurrentLevelConfig.duration > 0f)
            {
                timerProgressFill.fillAmount = 1f - (TimeRemaining / CurrentLevelConfig.duration);
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
    }

    public void OnBossDefeated()
    {
        if (isGameOver || isVictory) return;
        isVictory = true;

        // Save Gems with 25 bonus gems for clearing!
        int bonusGems = 25;
        SaveSystem.AddGems(score + bonusGems);

        // Unlock next stage in progression
        SaveSystem.UnlockStage(SelectedStageIndex + 1);

        // Freeze game
        Time.timeScale = 0f;

        ShowVictoryPanel(bonusGems);
    }

    private void ShowVictoryPanel(int bonusGems)
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
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

        string rewardStr = $"Gems Collected: {score}\nClear Bonus: +{bonusGems} Gems!\n\nTotal Bank: {SaveSystem.GetGemsBank()}";
        CreateText(statsBox, "RewardDetails", rewardStr, 18, Vector2.zero, new Color(0.4f, 1f, 0.7f, 1f));

        // Buttons
        float buttonY = -120f;
        // Button 1: Next Stage (only if there is a next stage config)
        if (SelectedStageIndex < 3)
        {
            CreateButton(dialog, "NextButton", "NEXT STAGE", new Vector2(200f, 44f), new Vector2(0f, buttonY), () =>
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

    private void CreateText(GameObject parent, string name, string content, int fontSize, Vector2 anchoredPosition, Color color, bool bold = false)
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
        btn.onClick.AddListener(() => onClickAction?.Invoke());
        
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
            Instantiate(gemPrefab, position, Quaternion.identity);
        }
    }

    private GameObject proceduralGameOverPanel;

    public void OnPlayerDeath()
    {
        if (isGameOver || isVictory) return;
        isGameOver = true;

        // Save collected gems to bank
        SaveSystem.AddGems(score);

        // Freeze game
        Time.timeScale = 0f;

        ShowGameOverPanel();
    }

    private void ShowGameOverPanel()
    {
        // Disable scene's default panel if it exists
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        Transform uiParent = canvas.transform;
        GameObject safeArea = GameObject.Find("SafeAreaContainer");
        if (safeArea != null)
        {
            uiParent = safeArea.transform;
        }

        // Fullscreen dark overlay
        proceduralGameOverPanel = new GameObject("GameOverPanelOverlay");
        proceduralGameOverPanel.transform.SetParent(uiParent, false);

        RectTransform overlayRect = proceduralGameOverPanel.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;

        Image overlayBg = proceduralGameOverPanel.AddComponent<Image>();
        overlayBg.color = new Color(0f, 0f, 0f, 0.7f); // Darker semi-transparent overlay

        // Center Dialog Box
        GameObject dialog = new GameObject("GameOverDialog");
        dialog.transform.SetParent(proceduralGameOverPanel.transform, false);

        RectTransform dialogRect = dialog.AddComponent<RectTransform>();
        dialogRect.sizeDelta = new Vector2(460f, 400f);
        dialogRect.anchoredPosition = Vector2.zero;

        Image dialogBg = dialog.AddComponent<Image>();
        dialogBg.sprite = GameSpriteManager.GetSprite("panel_purple");
        dialogBg.type = Image.Type.Sliced;
        dialogBg.color = Color.white;

        Outline outline = dialog.AddComponent<Outline>();
        outline.effectColor = new Color(0.8f, 0.2f, 0.2f, 0.6f); // Glowing red outline
        outline.effectDistance = new Vector2(2f, 2f);

        // Title text
        CreateText(dialog, "Title", "DEFEATED", 42, new Vector2(0f, 130f), new Color(1f, 0.3f, 0.3f, 1f), true);
        CreateText(dialog, "Subtitle", "RUN ENDED", 18, new Vector2(0f, 90f), Color.white);

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

        string rewardStr = $"Gems Collected: {score}\n\nTotal Bank: {SaveSystem.GetGemsBank()}";
        CreateText(statsBox, "RewardDetails", rewardStr, 18, Vector2.zero, new Color(1f, 0.7f, 0.7f, 1f));

        // Buttons
        // Button 1: Restart Run
        CreateButton(dialog, "RestartButton", "RETRY RUN", new Vector2(200f, 44f), new Vector2(0f, -120f), () =>
        {
            RestartGame();
        }, new Color(0.7f, 0.2f, 0.2f, 1f));

        // Button 2: Menu
        CreateButton(dialog, "MenuButton", "MAIN MENU", new Vector2(200f, 44f), new Vector2(0f, -175f), () =>
        {
            Time.timeScale = 1f;
            RestartGame();
        }, new Color(0.2f, 0.3f, 0.5f, 1f));
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    // StyleScoreUI is now obsolete as we use CreateUnifiedHUD instead.

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }
}
