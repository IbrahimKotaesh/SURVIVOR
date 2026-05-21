using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuShopUI : MonoBehaviour
{
    private static MainMenuShopUI instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeOnLoad()
    {
        Debug.Log("MainMenuShopUI: InitializeOnLoad running!");
        // Check if there is already a MainMenuShopUI in the scene
        if (FindAnyObjectByType<MainMenuShopUI>() != null)
        {
            Debug.Log("MainMenuShopUI: Already exists in scene, skipping loader creation.");
            return;
        }

        GameObject go = new GameObject("MainMenuShopUI_Loader");
        go.AddComponent<MainMenuShopUI>();
        Debug.Log("MainMenuShopUI: Created MainMenuShopUI_Loader GameObject.");
    }

    private GameObject menuPanel;
    private TextMeshProUGUI gemsText;
    
    // UI References for upgrade rows
    private TextMeshProUGUI hpText;
    private Image hpProgressFill;
    private Button hpButton;
    private TextMeshProUGUI hpButtonText;

    private TextMeshProUGUI speedText;
    private Image speedProgressFill;
    private Button speedButton;
    private TextMeshProUGUI speedButtonText;

    private TextMeshProUGUI fireRateText;
    private Image fireRateProgressFill;
    private Button fireRateButton;
    private TextMeshProUGUI fireRateButtonText;

    private readonly int[] upgradeCosts = new int[] { 15, 30, 60, 120, 250 };
    private const int MAX_LEVEL = 5;

    private void Awake()
    {
        Debug.Log("MainMenuShopUI: Awake called!");
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("MainMenuShopUI: Set instance and DontDestroyOnLoad.");
        }
        else
        {
            Debug.Log("MainMenuShopUI: Instance already exists, destroying this duplicate.");
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        Debug.Log("MainMenuShopUI: Start called!");
        GameSpriteManager.ForceReload();
        SetupUI();
        
        // Pause the game on start to show menu
        Time.timeScale = 0f;
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        SetupUI();
        // Pause game to show menu
        Time.timeScale = 0f;
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

    private void SetupUI()
    {
        Debug.Log("MainMenuShopUI: SetupUI started!");
        // Force reload sprites from the sheet to guarantee sliced metadata changes are updated
        GameSpriteManager.ForceReload();

        // Deactivate player's health bar during lobby menu to prevent overlap
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player != null)
        {
            Transform healthCanvas = player.transform.Find("HealthBarCanvas");
            if (healthCanvas != null)
            {
                healthCanvas.gameObject.SetActive(false);
                Debug.Log("MainMenuShopUI: Deactivated player's HealthBarCanvas.");
            }
        }

        // Find existing canvas
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("MainMenuShopUI: No Canvas found in the scene! Cannot build Main Menu UI.");
            return;
        }
        Debug.Log("MainMenuShopUI: Found Canvas: " + canvas.name + " (" + canvas.gameObject.GetInstanceID() + ")");

        // Try to attach to SafeAreaContainer if it exists, otherwise Canvas directly
        Transform uiParent = canvas.transform;
        GameObject safeArea = GameObject.Find("SafeAreaContainer");
        if (safeArea != null)
        {
            uiParent = safeArea.transform;
            Debug.Log("MainMenuShopUI: Found SafeAreaContainer to use as parent.");
        }
        else
        {
            Debug.Log("MainMenuShopUI: SafeAreaContainer not found, using Canvas directly.");
        }

        // Clean up old menu panel if reloading scene
        if (menuPanel != null)
        {
            Debug.Log("MainMenuShopUI: Cleaning up existing menuPanel.");
            Destroy(menuPanel);
        }

        // Create Main Menu Panel (dimmed backdrop overlay)
        menuPanel = new GameObject("MainMenuPanel");
        menuPanel.transform.SetParent(uiParent, false);
        
        RectTransform rect = menuPanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        Image bgImage = menuPanel.AddComponent<Image>();
        bgImage.color = new Color(0.02f, 0.03f, 0.08f, 0.85f); // Deep dark space background overlay

        // --- LOBBY CARD (Central Box) ---
        GameObject lobbyCard = new GameObject("LobbyCard");
        lobbyCard.transform.SetParent(menuPanel.transform, false);
        RectTransform lobbyRect = lobbyCard.AddComponent<RectTransform>();
        lobbyRect.sizeDelta = new Vector2(660f, 520f);
        lobbyRect.anchoredPosition = Vector2.zero;

        Image lobbyImg = lobbyCard.AddComponent<Image>();
        lobbyImg.sprite = GameSpriteManager.GetSprite("panel_purple");
        lobbyImg.type = Image.Type.Sliced;
        lobbyImg.color = new Color(0.65f, 0.55f, 0.93f, 1f); // Soft purple panel color

        // Glowing cyan outline for the card
        Outline cardOutline = lobbyCard.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.7f, 0.4f, 0.9f, 0.3f);
        cardOutline.effectDistance = new Vector2(2f, 2f);

        // --- TITLE ---
        CreateText(lobbyCard, "TitleText", "SURVIVOR SOULS", 44, new Vector2(0, 205), TextAlignmentOptions.Center, true);

        // --- GEMS BALANCE ---
        gemsText = CreateText(lobbyCard, "GemsBankText", "Gems Bank: " + SaveSystem.GetGemsBank(), 20, new Vector2(0, 155));
        gemsText.color = new Color(0.12f, 0.58f, 0.92f, 1f); // Rich sapphire blue
        gemsText.outlineColor = new Color32(20, 20, 50, 255);
        gemsText.outlineWidth = 0.2f;

        // --- SHOP TABLET BACKDROP ---
        GameObject shopBackdrop = new GameObject("ShopBackdrop");
        shopBackdrop.transform.SetParent(lobbyCard.transform, false);
        RectTransform backdropRect = shopBackdrop.AddComponent<RectTransform>();
        backdropRect.sizeDelta = new Vector2(580, 180);
        backdropRect.anchoredPosition = new Vector2(0, 25);
        Image backdropImg = shopBackdrop.AddComponent<Image>();
        backdropImg.sprite = GameSpriteManager.GetSprite("panel_blue_top");
        backdropImg.type = Image.Type.Sliced;
        backdropImg.color = new Color(0.94f, 0.96f, 0.99f, 1f); // Off-white blue backdrop

        Outline backdropOutline = shopBackdrop.AddComponent<Outline>();
        backdropOutline.effectColor = new Color(0.2f, 0.6f, 0.9f, 0.2f);
        backdropOutline.effectDistance = new Vector2(1f, 1f);

        // --- UPGRADE ROWS ---
        float startY = 50;
        float spacingY = 50;

        // HP Row
        CreateUpgradeRow("Max HP", SaveSystem.HPKey, 0, startY - (0 * spacingY), shopBackdrop,
            out hpText, out hpProgressFill, out hpButton, out hpButtonText, () => TryUpgrade(SaveSystem.HPKey));

        // Speed Row
        CreateUpgradeRow("Move Speed", SaveSystem.SpeedKey, 1, startY - (1 * spacingY), shopBackdrop,
            out speedText, out speedProgressFill, out speedButton, out speedButtonText, () => TryUpgrade(SaveSystem.SpeedKey));

        // Fire Rate Row
        CreateUpgradeRow("Fire Rate", SaveSystem.FireRateKey, 2, startY - (2 * spacingY), shopBackdrop,
            out fireRateText, out fireRateProgressFill, out fireRateButton, out fireRateButtonText, () => TryUpgrade(SaveSystem.FireRateKey));

        // --- LEVEL SELECTION LABEL ---
        CreateText(lobbyCard, "LevelSelectLabel", "SELECT STAGE:", 16, new Vector2(0, -95), TextAlignmentOptions.Center, true);

        // Create Stage Selection Buttons
        CreateStageButton(lobbyCard, "Stage1_Btn", "STAGE 1", new Vector2(130, 36), new Vector2(-150, -135), 1);
        CreateStageButton(lobbyCard, "Stage2_Btn", "STAGE 2", new Vector2(130, 36), new Vector2(0, -135), 2);
        CreateStageButton(lobbyCard, "Stage3_Btn", "STAGE 3", new Vector2(130, 36), new Vector2(150, -135), 3);

        // --- START GAME BUTTON ---
        CreateButton(lobbyCard, "StartButton", "START RUN", new Vector2(250, 52), new Vector2(0, -205), StartGame, new Color(0.1f, 0.7f, 0.3f, 1f));

        RefreshShopUI();
    }

    private void CreateUpgradeRow(string statName, string statKey, int rowIndex, float yPos, GameObject parent,
        out TextMeshProUGUI labelText, out Image progressFill, out Button button, out TextMeshProUGUI btnText, System.Action onUpgrade)
    {

        // 1. Label
        labelText = CreateText(parent, statName + "_Label", statName, 18, new Vector2(-110f, yPos), TextAlignmentOptions.Left);
        labelText.rectTransform.sizeDelta = new Vector2(180, 40);

        // 2. Progress bar (GRAPHICAL BAR!)
        // Container
        GameObject progressContainer = new GameObject(statName + "_ProgressContainer");
        progressContainer.transform.SetParent(parent.transform, false);
        RectTransform containerRect = progressContainer.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(120f, 18f);
        containerRect.anchoredPosition = new Vector2(40f, yPos);
        
        Image containerImg = progressContainer.AddComponent<Image>();
        containerImg.sprite = GameSpriteManager.GetSprite("hud_bar_empty");
        containerImg.type = Image.Type.Sliced;
        containerImg.color = new Color(0.15f, 0.18f, 0.25f, 1f); // Dark gray/navy outline

        // Fill
        GameObject progressFillGo = new GameObject("Fill");
        progressFillGo.transform.SetParent(progressContainer.transform, false);
        RectTransform fillRect = progressFillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0.5f);
        fillRect.anchorMax = new Vector2(0f, 0.5f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(0f, 14f); // slightly smaller than container height

        progressFill = progressFillGo.AddComponent<Image>();
        progressFill.sprite = GameSpriteManager.GetSprite("hud_bar_fill_green");
        progressFill.type = Image.Type.Sliced;
        progressFill.color = new Color(0.12f, 0.74f, 0.35f, 1f); // Emerald green fill

        // 3. Button GameObject
        GameObject btnGo = new GameObject(statName + "_Button");
        btnGo.transform.SetParent(parent.transform, false);
        RectTransform btnRect = btnGo.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(130, 36);
        btnRect.anchoredPosition = new Vector2(200, yPos);

        Image btnImg = btnGo.AddComponent<Image>();
        btnImg.sprite = GameSpriteManager.GetSprite("button_shop_blue");
        btnImg.type = Image.Type.Sliced;
        btnImg.color = new Color(0.15f, 0.55f, 0.9f, 1f); // Royal blue button

        button = btnGo.AddComponent<Button>();
        button.onClick.AddListener(() => onUpgrade?.Invoke());

        // 4. Button Text Label
        GameObject btnLabelGo = new GameObject("Text");
        btnLabelGo.transform.SetParent(btnGo.transform, false);
        RectTransform btnLabelRect = btnLabelGo.AddComponent<RectTransform>();
        btnLabelRect.anchorMin = Vector2.zero;
        btnLabelRect.anchorMax = Vector2.one;
        btnLabelRect.sizeDelta = Vector2.zero;

        btnText = btnLabelGo.AddComponent<TextMeshProUGUI>();
        btnText.text = "+ 15 Gems";
        btnText.fontSize = 15;
        TMP_FontAsset customFont = GameFontManager.BodyFont;
        if (customFont != null)
        {
            btnText.font = customFont;
        }
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.fontStyle = FontStyles.Bold;
        btnText.color = Color.white;
        btnText.outlineColor = new Color32(20, 20, 40, 255);
        btnText.outlineWidth = 0.2f;
    }

    private void TryUpgrade(string statKey)
    {
        int currentLevel = SaveSystem.GetUpgradeLevel(statKey);
        if (currentLevel >= MAX_LEVEL) return;

        int cost = upgradeCosts[currentLevel];
        if (SaveSystem.SpendGems(cost))
        {
            SaveSystem.IncrementUpgradeLevel(statKey);
            
            // Reload stat values on the player
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.LoadStats();
            }

            // Sync with other components
            UpdateActivePlayerStats();

            RefreshShopUI();
        }
    }

    private void UpdateActivePlayerStats()
    {
        // Find player and force updates if player is alive
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player != null)
        {
            // Update Max HP & Speed & Attack rate dynamically
            var playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null) playerStats.LoadStats();

            var playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // Re-initialize health bar to new Max HP
                playerHealth.TakeDamage(0); // Safely triggers health bar refresh
            }
        }
    }

    private void RefreshShopUI()
    {
        gemsText.text = "Gems Bank: " + SaveSystem.GetGemsBank();

        RefreshRow(SaveSystem.HPKey, hpText, hpProgressFill, hpButton, hpButtonText, "Max HP");
        RefreshRow(SaveSystem.SpeedKey, speedText, speedProgressFill, speedButton, speedButtonText, "Move Speed");
        RefreshRow(SaveSystem.FireRateKey, fireRateText, fireRateProgressFill, fireRateButton, fireRateButtonText, "Fire Rate");
    }

    private void RefreshRow(string statKey, TextMeshProUGUI label, Image progressFill, Button button, TextMeshProUGUI btnText, string statName)
    {
        int level = SaveSystem.GetUpgradeLevel(statKey);
        label.text = $"{statName} (Lvl {level}/{MAX_LEVEL})";

        // Progress bar build - graphically adjust size delta width!
        float percent = level / (float)MAX_LEVEL;
        progressFill.rectTransform.sizeDelta = new Vector2(percent * 120f, 14f);
        progressFill.gameObject.SetActive(level > 0);

        Image btnImg = button.GetComponent<Image>();
        btnImg.sprite = GameSpriteManager.GetSprite("button_generic");
        if (level >= MAX_LEVEL)
        {
            btnText.text = "MAX";
            button.interactable = false;
            btnImg.color = new Color(0.9f, 0.25f, 0.25f, 0.5f);
        }
        else
        {
            int cost = upgradeCosts[level];
            btnText.text = $"+ {cost} Gems";
            
            bool canAfford = SaveSystem.GetGemsBank() >= cost;
            button.interactable = canAfford;
            if (canAfford)
            {
                btnImg.color = new Color(0.12f, 0.74f, 0.35f, 1f);
            }
            else
            {
                btnImg.color = new Color(0.9f, 0.25f, 0.25f, 0.6f);
            }
        }
    }

    private void StartGame()
    {
        Time.timeScale = 1f;
        menuPanel.SetActive(false);

        // Reactivate player's health bar when the run starts
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player != null)
        {
            Transform healthCanvas = player.transform.Find("HealthBarCanvas");
            if (healthCanvas != null)
            {
                healthCanvas.gameObject.SetActive(true);
            }
        }
    }

    // Text Creation Helper
    private TextMeshProUGUI CreateText(GameObject parent, string name, string content, int fontSize, Vector2 anchoredPosition, TextAlignmentOptions alignment = TextAlignmentOptions.Center, bool isTitle = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        
        TMP_FontAsset customFont = isTitle ? GameFontManager.TitleFont : GameFontManager.BodyFont;
        if (customFont != null)
        {
            tmp.font = customFont;
        }
        
        if (isTitle)
        {
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(1f, 0.72f, 0.15f, 1f); // Bouncy golden orange
            tmp.outlineColor = new Color32(45, 20, 60, 255); // Deep dark purple outline
            tmp.outlineWidth = 0.25f;
        }
        else
        {
            tmp.color = new Color(0.18f, 0.12f, 0.25f, 1f); // Dark slate-purple (high contrast on white card)
        }
        
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(500, 80);
        rect.anchoredPosition = anchoredPosition;
        
        return tmp;
    }

    // Button Creation Helper
    private Button CreateButton(GameObject parent, string name, string label, Vector2 size, Vector2 anchoredPosition, System.Action onClickAction, Color normalColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        
        Image img = go.AddComponent<Image>();
        img.sprite = GameSpriteManager.GetSprite(name);
        img.type = Image.Type.Sliced;
        
        Color btnColor = normalColor;
        if (name.Contains("Start") || label.Contains("START") || name.Contains("Play") || label.Contains("PLAY"))
            btnColor = new Color(0.12f, 0.74f, 0.35f, 1f); // Emerald green
        else if (name.Contains("Back") || label.Contains("BACK"))
            btnColor = new Color(0.9f, 0.25f, 0.25f, 1f); // Crimson red
        else if (name.Contains("Close") || label.Contains("CLOSE"))
            btnColor = new Color(0.95f, 0.57f, 0.11f, 1f); // Orange
        else if (name.Contains("Shop") || label.Contains("SHOP") || name.Contains("Next") || label.Contains("NEXT"))
            btnColor = new Color(0.15f, 0.55f, 0.9f, 1f); // Blue
        
        img.color = btnColor;
        
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
        tmp.fontSize = 22;
        
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
        
        return btn;
    }

    private void CreateStageButton(GameObject parent, string name, string label, Vector2 size, Vector2 anchoredPosition, int stageIndex)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        Image img = go.AddComponent<Image>();
        img.type = Image.Type.Sliced;
        
        Button btn = go.AddComponent<Button>();
        
        int highestUnlocked = SaveSystem.GetHighestUnlockedStage();
        bool isUnlocked = stageIndex <= highestUnlocked;

        btn.interactable = isUnlocked;

        img.sprite = GameSpriteManager.GetSprite("button_generic");
        if (!isUnlocked)
        {
            img.color = new Color(0.9f, 0.25f, 0.25f, 0.4f); // semi-transparent red (Locked)
        }
        else if (stageIndex == GameManager.SelectedStage)
        {
            img.color = new Color(0.12f, 0.74f, 0.35f, 1f); // Bright emerald (Selected)
        }
        else
        {
            img.color = new Color(0.15f, 0.55f, 0.9f, 1f); // Unlocked selection (steel blue)
        }

        btn.onClick.AddListener(() =>
        {
            GameManager.SelectedStage = stageIndex;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SelectedStageIndex = stageIndex;
            }
            SetupUI(); // Rebuild UI to reflect new stage selection instantly
        });

        // Label Text
        GameObject labelGo = new GameObject("Text");
        labelGo.transform.SetParent(go.transform, false);
        RectTransform labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = isUnlocked ? label : "LOCKED";
        tmp.fontSize = 14;
        
        TMP_FontAsset customFont = GameFontManager.TitleFont;
        if (customFont != null)
        {
            tmp.font = customFont;
        }
        
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = isUnlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
    }
}
