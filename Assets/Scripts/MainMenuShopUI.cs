using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuShopUI : MonoBehaviour
{
    private static MainMenuShopUI instance;
    public static MainMenuShopUI Instance => instance;

#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void SendGemsUpdatedToAndroid(int newBalance);
#else
    private static void SendGemsUpdatedToAndroid(int newBalance)
    {
        Debug.Log($"[Mock Android Bridge] Gems Updated: {newBalance}");
    }
#endif

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

    private string tempSelectedPlayer = "Virgil";
    private int onboardingPage = 1;
    private Coroutine loadingCoroutine;
    private Image onboardingLoadingFill;
    private TextMeshProUGUI onboardingLoadingText;

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
        
        EnsureEventSystemInputModule();
        
        // Force reset onboarding for testing purposes so the user always sees it on start
        PlayerPrefs.SetInt("OnboardingCompleted", 0);
        PlayerPrefs.Save();
        onboardingPage = 1;

        GameSpriteManager.ForceReload();
        SetupUI();
        
        // Play Menu BGM
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM("menu");
        }

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
        EnsureEventSystemInputModule();
        SetupUI();
        // Play Menu BGM
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM("menu");
        }
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

    public void SetupUI()
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

        // Find existing canvas (prioritize the main Screen Space Canvas over player health bar canvas)
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

        if (canvas == null)
        {
            Debug.LogError("MainMenuShopUI: No Canvas found in the scene! Cannot build Main Menu UI.");
            return;
        }
        Debug.Log("MainMenuShopUI: Found Canvas: " + canvas.name + " (" + canvas.gameObject.GetInstanceID() + ")");

        // Check if onboarding is completed
        bool isOnboarding = (PlayerPrefs.GetInt("OnboardingCompleted", 0) == 0);

        // Try to attach to SafeAreaContainer if it exists, otherwise Canvas directly
        Transform uiParent = canvas.transform;
        GameObject safeArea = GameObject.Find("SafeAreaContainer");
        if (safeArea != null && !isOnboarding)
        {
            uiParent = safeArea.transform;
            Debug.Log("MainMenuShopUI: Found SafeAreaContainer to use as parent.");
        }
        else
        {
            Debug.Log("MainMenuShopUI: Onboarding active or SafeAreaContainer not found, using Canvas directly.");
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
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image bgImage = menuPanel.AddComponent<Image>();
        bgImage.color = new Color(0.02f, 0.03f, 0.08f, 0.85f); // Deep dark space background overlay

        // Check if onboarding is completed
        if (PlayerPrefs.GetInt("OnboardingCompleted", 0) == 0)
        {
            SetupOnboardingUI(menuPanel);
            return;
        }

        // --- LOBBY CARD (Central Box) ---
        GameObject lobbyCard = new GameObject("LobbyCard");
        lobbyCard.transform.SetParent(menuPanel.transform, false);
        RectTransform lobbyRect = lobbyCard.AddComponent<RectTransform>();
        lobbyRect.sizeDelta = new Vector2(660f, 620f); // Height increased to 620 to fit character selection
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
        CreateText(lobbyCard, "TitleText", "EYES OF THE ECLIPSE", 36, new Vector2(0, 255), TextAlignmentOptions.Center, true);

        // --- MUTE TOGGLE BUTTON ---
        bool soundMuted = SoundManager.Instance != null && SoundManager.Instance.IsMuted;
        string muteText = soundMuted ? "🔇" : "🔊";
        Color muteBtnColor = soundMuted ? new Color(0.9f, 0.25f, 0.25f, 1f) : new Color(0.15f, 0.55f, 0.9f, 1f);
        CreateButton(lobbyCard, "MuteButton", muteText, new Vector2(44f, 44f), new Vector2(285f, 260f), () =>
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.ToggleMute();
            }
            SetupUI(); // Refresh UI to update the speaker icon
        }, muteBtnColor);

        // --- GEMS BALANCE ---
        gemsText = CreateText(lobbyCard, "GemsBankText", "Gems Bank: " + SaveSystem.GetGemsBank(), 20, new Vector2(0, 205));
        gemsText.color = new Color(0.12f, 0.58f, 0.92f, 1f); // Rich sapphire blue
        gemsText.outlineColor = new Color32(20, 20, 50, 255);
        gemsText.outlineWidth = 0.2f;

        // --- SHOP TABLET BACKDROP ---
        GameObject shopBackdrop = new GameObject("ShopBackdrop");
        shopBackdrop.transform.SetParent(lobbyCard.transform, false);
        RectTransform backdropRect = shopBackdrop.AddComponent<RectTransform>();
        backdropRect.sizeDelta = new Vector2(580, 180);
        backdropRect.anchoredPosition = new Vector2(0, 75);
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

        // --- SELECT PLAYER ---
        CreateText(lobbyCard, "CharacterSelectLabel", "SELECT PLAYER:", 16, new Vector2(0, -45), TextAlignmentOptions.Center, true);
        
        string selectedPlayer = PlayerPrefs.GetString("SelectedPlayer", "Virgil");
        
        // Virgil Button
        CreatePlayerButton(lobbyCard, "Player_Virgil_Btn", "VIRGIL VAN DIJK", new Vector2(170, 38), new Vector2(-190, -85), "Virgil", selectedPlayer == "Virgil");
        
        // Vini Button
        CreatePlayerButton(lobbyCard, "Player_Vini_Btn", "VINICIUS JR.", new Vector2(170, 38), new Vector2(0, -85), "Vini", selectedPlayer == "Vini");

        // Yamal Button
        CreatePlayerButton(lobbyCard, "Player_Yamal_Btn", "LAMINE YAMAL", new Vector2(170, 38), new Vector2(190, -85), "Yamal", selectedPlayer == "Yamal");

        // --- LEVEL SELECTION LABEL ---
        CreateText(lobbyCard, "LevelSelectLabel", "SELECT LEVEL:", 16, new Vector2(0, -145), TextAlignmentOptions.Center, true);

        // Create Stage Selection Buttons
        CreateStageButton(lobbyCard, "Stage1_Btn", "LEVEL 1", new Vector2(130, 36), new Vector2(-150, -185), 1);
        CreateStageButton(lobbyCard, "Stage2_Btn", "LEVEL 2", new Vector2(130, 36), new Vector2(0, -185), 2);
        CreateStageButton(lobbyCard, "Stage3_Btn", "LEVEL 3", new Vector2(130, 36), new Vector2(150, -185), 3);

        // --- START GAME BUTTON ---
        CreateButton(lobbyCard, "StartButton", "START RUN", new Vector2(250, 52), new Vector2(0, -255), StartGame, new Color(0.1f, 0.7f, 0.3f, 1f));

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
        button.onClick.AddListener(() => {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("click");
            onUpgrade?.Invoke();
        });

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

            // Sync with Android Bridge
            SendGemsUpdatedToAndroid(SaveSystem.GetGemsBank());
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

    public void RefreshShopUI()
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

        // Play Battle BGM
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM("battle");
        }

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
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("click");
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

    private void CreatePlayerButton(GameObject parent, string name, string label, Vector2 size, Vector2 anchoredPosition, string characterId, bool isSelected)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        Image img = go.AddComponent<Image>();
        img.type = Image.Type.Sliced;
        img.sprite = GameSpriteManager.GetSprite("button_generic");
        
        // Highlight active selection
        if (isSelected)
        {
            img.color = new Color(0.12f, 0.74f, 0.35f, 1f); // Bright emerald (Selected)
        }
        else
        {
            img.color = new Color(0.15f, 0.55f, 0.9f, 1f); // Steel blue (Inactive)
        }

        Button btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("click");
            PlayerPrefs.SetString("SelectedPlayer", characterId);
            PlayerPrefs.Save();
            
            // Dynamically refresh player's visual in the active scene if one exists
            UpdateActivePlayerVisuals();
            
            SetupUI(); // Rebuild UI to reflect the selected player immediately
        });

        // Label Text
        GameObject labelGo = new GameObject("Text");
        labelGo.transform.SetParent(go.transform, false);
        RectTransform labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 13;
        
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

    public void UpdateActivePlayerVisuals()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player != null)
        {
            var animator = player.GetComponent<SpriteSheetAnimator>();
            if (animator != null)
            {
                animator.Start(); // Re-run Start to reload the sprites sheet and reinitialize
            }

            var controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.SetFacingDirection(true); // Both Virgil and Vini face right

                // Update the ability button visual in GameManager to match the selected character
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.UpdateAbilityButtonVisuals(controller.AbilityUsesLeft);
                }
            }
        }

        // Also update the HUD avatar if it exists in the scene
        GameObject hudAvatar = GameObject.Find("HUD_Avatar");
        if (hudAvatar != null)
        {
            Image avatarImg = hudAvatar.GetComponent<Image>();
            if (avatarImg != null)
            {
                string selectedPlayer = PlayerPrefs.GetString("SelectedPlayer", "Virgil");
                string resourcePath = "van_dyk_cha";
                if (selectedPlayer == "Yamal") resourcePath = "yamal_cha";
                else if (selectedPlayer == "Vini") resourcePath = "vini_cha";
                Sprite avatarSprite = GameSpriteManager.GetCharacterPreviewSprite(resourcePath);
                if (avatarSprite != null)
                {
                    avatarImg.sprite = avatarSprite;
                    avatarImg.enabled = true;
                }
            }
        }
    }

    private void SetupOnboardingUI(GameObject parent)
    {
        if (onboardingPage == 1)
        {
            SetupOnboardingPage1(parent);
        }
        else
        {
            SetupOnboardingPage2(parent);
        }
    }

    private void SetupOnboardingPage1(GameObject parent)
    {
        // 1. Onboarding Background (Full Screen Background using Splash Image)
        GameObject onboardingBg = new GameObject("OnboardingBg");
        onboardingBg.transform.SetParent(parent.transform, false);
        RectTransform bgRect = onboardingBg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image cardImg = onboardingBg.AddComponent<Image>();
        Sprite splashSprite = Resources.Load<Sprite>("Splash_1");
        if (splashSprite != null)
        {
            cardImg.sprite = splashSprite;
            cardImg.color = Color.white;
            cardImg.preserveAspect = false; // Scaled by AspectRatioFitter

            AspectRatioFitter fitter = onboardingBg.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = (float)splashSprite.rect.width / splashSprite.rect.height;
        }
        else
        {
            Debug.LogError("MainMenuShopUI: onboarding_splash sprite not found in Resources!");
            cardImg.color = new Color(0.02f, 0.03f, 0.08f, 1f); // Deep dark space/navy instead of purple
        }

        // 2. Onboarding UI Container (Stretched to match screen exactly, no aspect ratio scaling to prevent UI cutoff)
        GameObject onboardingUI = new GameObject("OnboardingUI");
        onboardingUI.transform.SetParent(parent.transform, false);
        RectTransform uiRect = onboardingUI.AddComponent<RectTransform>();
        uiRect.anchorMin = Vector2.zero;
        uiRect.anchorMax = Vector2.one;
        uiRect.offsetMin = Vector2.zero;
        uiRect.offsetMax = Vector2.zero;

        // --- ONBOARDING MUTE BUTTON ---
        bool soundMuted = SoundManager.Instance != null && SoundManager.Instance.IsMuted;
        string muteText = soundMuted ? "🔇" : "🔊";
        Color muteBtnColor = soundMuted ? new Color(0.9f, 0.25f, 0.25f, 1f) : new Color(0.15f, 0.55f, 0.9f, 1f);
        Button onboardingMuteBtn = CreateButton(onboardingUI, "OnboardingMuteBtn", muteText, new Vector2(44f, 44f), Vector2.zero, () =>
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.ToggleMute();
            }
            SetupUI(); // Refresh UI to update the speaker icon
        }, muteBtnColor);
        RectTransform onboardingMuteRect = onboardingMuteBtn.GetComponent<RectTransform>();
        onboardingMuteRect.anchorMin = new Vector2(1f, 1f); // Top Right
        onboardingMuteRect.anchorMax = new Vector2(1f, 1f);
        onboardingMuteRect.pivot = new Vector2(1f, 1f);
        onboardingMuteRect.anchoredPosition = new Vector2(-20f, -20f);

        // --- TITLE ---
        var titleText = CreateText(onboardingUI, "OnboardingTitle", "EYES OF THE ECLIPSE", 38, Vector2.zero, TextAlignmentOptions.Center, true);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1.0f);
        titleRect.anchorMax = new Vector2(0.5f, 1.0f);
        titleRect.pivot = new Vector2(0.5f, 1.0f);
        titleRect.anchoredPosition = new Vector2(0f, -80f);
        titleRect.sizeDelta = new Vector2(600, 60);

        var subtitleText = CreateText(onboardingUI, "OnboardingSubtitle", "The monsters are chasing you! Get ready to run!", 16, Vector2.zero, TextAlignmentOptions.Center, false);
        RectTransform subRect = subtitleText.rectTransform;
        subRect.anchorMin = new Vector2(0.5f, 1.0f);
        subRect.anchorMax = new Vector2(0.5f, 1.0f);
        subRect.pivot = new Vector2(0.5f, 1.0f);
        subRect.anchoredPosition = new Vector2(0f, -145f);
        subRect.sizeDelta = new Vector2(600, 50);
        subtitleText.color = Color.white;
        subtitleText.outlineColor = new Color32(10, 10, 30, 255);
        subtitleText.outlineWidth = 0.2f;

        // --- LOADING BAR CONTAINER ---
        GameObject progressContainer = new GameObject("LoadingProgressContainer");
        progressContainer.transform.SetParent(onboardingUI.transform, false);
        RectTransform containerRect = progressContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.0f);
        containerRect.anchorMax = new Vector2(0.5f, 0.0f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(0f, 120f); // 120px above bottom
        containerRect.sizeDelta = new Vector2(500f, 28f);
        
        Image containerImg = progressContainer.AddComponent<Image>();
        containerImg.sprite = GameSpriteManager.GetSprite("hud_bar_empty");
        containerImg.type = Image.Type.Sliced;
        containerImg.color = new Color(0.12f, 0.14f, 0.22f, 1f); // Dark navy outline

        // Fill
        GameObject progressFillGo = new GameObject("Fill");
        progressFillGo.transform.SetParent(progressContainer.transform, false);
        RectTransform fillRect = progressFillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0.5f);
        fillRect.anchorMax = new Vector2(0f, 0.5f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(0f, 22f); // slightly smaller than container height

        onboardingLoadingFill = progressFillGo.AddComponent<Image>();
        onboardingLoadingFill.sprite = GameSpriteManager.GetSprite("hud_bar_fill_green");
        onboardingLoadingFill.type = Image.Type.Sliced;
        onboardingLoadingFill.color = new Color(0.12f, 0.74f, 0.35f, 1f); // Emerald green fill

        // --- LOADING TEXT ---
        onboardingLoadingText = CreateText(onboardingUI, "LoadingText", "LOADING... 0%", 18, Vector2.zero);
        RectTransform loadingTextRect = onboardingLoadingText.rectTransform;
        loadingTextRect.anchorMin = new Vector2(0.5f, 0.0f);
        loadingTextRect.anchorMax = new Vector2(0.5f, 0.0f);
        loadingTextRect.pivot = new Vector2(0.5f, 0.5f);
        loadingTextRect.anchoredPosition = new Vector2(0f, 75f); // 75px above bottom
        loadingTextRect.sizeDelta = new Vector2(500f, 40f);
        onboardingLoadingText.color = Color.white;
        onboardingLoadingText.outlineColor = new Color32(10, 10, 30, 255);
        onboardingLoadingText.outlineWidth = 0.25f;
        if (GameFontManager.BodyFont != null) onboardingLoadingText.font = GameFontManager.BodyFont;

        // Start loading coroutine
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }
        loadingCoroutine = StartCoroutine(AnimateOnboardingLoading());
    }

    private void SetupOnboardingPage2(GameObject parent)
    {
        // 1. Onboarding Background (Full Screen Background)
        GameObject onboardingBg = new GameObject("OnboardingBg");
        onboardingBg.transform.SetParent(parent.transform, false);
        RectTransform bgRect = onboardingBg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image cardImg = onboardingBg.AddComponent<Image>();
        Sprite splashSprite = Resources.Load<Sprite>("Splash_1");
        if (splashSprite != null)
        {
            cardImg.sprite = splashSprite;
            cardImg.color = new Color(0.12f, 0.15f, 0.22f, 1f); // Dark navy tint over splash art (no purple)
            cardImg.preserveAspect = false; // Scaled by AspectRatioFitter

            AspectRatioFitter fitter = onboardingBg.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = (float)splashSprite.rect.width / splashSprite.rect.height;
        }
        else
        {
            cardImg.color = new Color(0.02f, 0.03f, 0.08f, 1f); // Deep dark space/navy instead of purple
        }

        // 2. Onboarding UI Container (Stretched to match screen exactly, no aspect ratio scaling to prevent UI cutoff)
        GameObject onboardingUI = new GameObject("OnboardingUI");
        onboardingUI.transform.SetParent(parent.transform, false);
        RectTransform uiRect = onboardingUI.AddComponent<RectTransform>();
        uiRect.anchorMin = Vector2.zero;
        uiRect.anchorMax = Vector2.one;
        uiRect.offsetMin = Vector2.zero;
        uiRect.offsetMax = Vector2.zero;

        // --- ONBOARDING MUTE BUTTON ---
        bool soundMuted = SoundManager.Instance != null && SoundManager.Instance.IsMuted;
        string muteText = soundMuted ? "🔇" : "🔊";
        Color muteBtnColor = soundMuted ? new Color(0.9f, 0.25f, 0.25f, 1f) : new Color(0.15f, 0.55f, 0.9f, 1f);
        Button onboardingMuteBtn = CreateButton(onboardingUI, "OnboardingMuteBtn", muteText, new Vector2(44f, 44f), Vector2.zero, () =>
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.ToggleMute();
            }
            SetupUI(); // Refresh UI to update the speaker icon
        }, muteBtnColor);
        RectTransform onboardingMuteRect = onboardingMuteBtn.GetComponent<RectTransform>();
        onboardingMuteRect.anchorMin = new Vector2(1f, 1f); // Top Right
        onboardingMuteRect.anchorMax = new Vector2(1f, 1f);
        onboardingMuteRect.pivot = new Vector2(1f, 1f);
        onboardingMuteRect.anchoredPosition = new Vector2(-20f, -20f);

        // --- TITLE ---
        var titleText = CreateText(onboardingUI, "OnboardingTitle", "CHOOSE YOUR ENDURANCE.\nCHOOSE YOUR PLAYER.", 28, Vector2.zero, TextAlignmentOptions.Center, true);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1.0f);
        titleRect.anchorMax = new Vector2(0.5f, 1.0f);
        titleRect.pivot = new Vector2(0.5f, 1.0f);
        titleRect.anchoredPosition = new Vector2(0f, -60f); // Moved up slightly to fit two lines
        titleRect.sizeDelta = new Vector2(700, 80);

        var subtitleText = CreateText(onboardingUI, "OnboardingSubtitle", "Select your initial hero to enter the arena", 16, Vector2.zero, TextAlignmentOptions.Center, false);
        RectTransform subRect = subtitleText.rectTransform;
        subRect.anchorMin = new Vector2(0.5f, 1.0f);
        subRect.anchorMax = new Vector2(0.5f, 1.0f);
        subRect.pivot = new Vector2(0.5f, 1.0f);
        subRect.anchoredPosition = new Vector2(0f, -150f);
        subRect.sizeDelta = new Vector2(600, 50);
        subtitleText.color = Color.white;
        subtitleText.outlineColor = new Color32(10, 10, 30, 255);
        subtitleText.outlineWidth = 0.2f;

        // --- CARDS CONTAINER ---
        CreateOnboardingCharacterCard(onboardingUI, "VirgilCard", "VIRGIL VAN DIJK", "A legendary wall. Unmatched strength and presence.", "van_dyk_cha", new Vector2(-220, 30), "Virgil");

        CreateOnboardingCharacterCard(onboardingUI, "ViniCard", "VINICIUS JR.", "Lightning fast speed. Master of agility and dribbles.", "vini_cha", new Vector2(0, 30), "Vini");

        CreateOnboardingCharacterCard(onboardingUI, "YamalCard", "LAMINE YAMAL", "Yamal Chaos. Young sensation with dazzling speed and clinical finishes.", "yamal_cha", new Vector2(220, 30), "Yamal");

        // --- CONFIRM BUTTON ---
        // Positioned lower down (50px from bottom instead of 75px) to be clearly below the player cards
        Button confirmBtn = CreateButton(onboardingUI, "ConfirmOnboardingButton", "ENTER THE AREA", new Vector2(300, 52), Vector2.zero, ConfirmOnboardingSelection, new Color(0.12f, 0.74f, 0.35f, 1f));
        RectTransform btnRect = confirmBtn.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.0f);
        btnRect.anchorMax = new Vector2(0.5f, 0.0f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(0f, 50f); // 50px above bottom
    }

    private void CreateOnboardingCharacterCard(GameObject parent, string cardName, string nameLabel, string descText, string resourcePath, Vector2 pos, string characterId)
    {
        bool isSelected = (tempSelectedPlayer == characterId);

        GameObject cardGo = new GameObject(cardName);
        cardGo.transform.SetParent(parent.transform, false);
        RectTransform rect = cardGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(210f, 360f);
        rect.anchoredPosition = new Vector2(pos.x, pos.y + 20f);

        Image cardImg = cardGo.AddComponent<Image>();
        cardImg.sprite = GameSpriteManager.GetSprite("panel_blue_top");
        cardImg.type = Image.Type.Sliced;
        cardImg.color = isSelected ? new Color(0.9f, 0.95f, 1f, 1f) : new Color(0.94f, 0.96f, 0.99f, 1f);

        Outline cardOutline = cardGo.AddComponent<Outline>();
        if (isSelected)
        {
            cardOutline.effectColor = new Color(0.12f, 0.74f, 0.35f, 1f); // Bright green border if selected
            cardOutline.effectDistance = new Vector2(5f, 5f); // Thicker outline for better selection visibility

            // Add Checkmark badge at the top-right
            GameObject checkBadge = new GameObject("CheckmarkBadge");
            checkBadge.transform.SetParent(cardGo.transform, false);
            RectTransform badgeRect = checkBadge.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1f, 1f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(-15f, -15f);
            badgeRect.sizeDelta = new Vector2(36f, 36f);

            Image badgeImg = checkBadge.AddComponent<Image>();
            badgeImg.sprite = GetOrCreateRoundedRectSprite();
            badgeImg.color = new Color(0.12f, 0.74f, 0.35f, 1f); // Green circle

            Outline badgeOutline = checkBadge.AddComponent<Outline>();
            badgeOutline.effectColor = Color.white;
            badgeOutline.effectDistance = new Vector2(1.5f, 1.5f);

            GameObject checkTextGo = new GameObject("Text");
            checkTextGo.transform.SetParent(checkBadge.transform, false);
            RectTransform checkTextRect = checkTextGo.AddComponent<RectTransform>();
            checkTextRect.anchorMin = Vector2.zero;
            checkTextRect.anchorMax = Vector2.one;
            checkTextRect.offsetMin = Vector2.zero;
            checkTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI checkTmp = checkTextGo.AddComponent<TextMeshProUGUI>();
            checkTmp.text = "✓";
            checkTmp.fontSize = 22;
            checkTmp.alignment = TextAlignmentOptions.Center;
            checkTmp.fontStyle = FontStyles.Bold;
            checkTmp.color = Color.white;
            if (GameFontManager.TitleFont != null) checkTmp.font = GameFontManager.TitleFont;
        }
        else
        {
            cardOutline.effectColor = new Color(0.15f, 0.55f, 0.9f, 0.2f); // Faint blue border
            cardOutline.effectDistance = new Vector2(1f, 1f);
        }

        // Add Button component to the card to make it clickable
        Button cardBtn = cardGo.AddComponent<Button>();
        cardBtn.onClick.AddListener(() =>
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("click");
            tempSelectedPlayer = characterId;
            // Re-setup the Onboarding UI to refresh the highlights
            SetupUI();
        });

        // --- CHARACTER IMAGE PREVIEW ---
        GameObject imgGo = new GameObject("PreviewImage");
        imgGo.transform.SetParent(cardGo.transform, false);
        RectTransform imgRect = imgGo.AddComponent<RectTransform>();
        imgRect.anchorMin = new Vector2(0.5f, 1f);
        imgRect.anchorMax = new Vector2(0.5f, 1f);
        
        // Use center pivot so scaling happens from the center of the image
        imgRect.pivot = new Vector2(0.5f, 0.5f);
        
        Image previewImg = imgGo.AddComponent<Image>();
        previewImg.raycastTarget = false;

        // Base center position (equivalent to top-pivot at -20 with 180 height)
        imgRect.anchoredPosition = new Vector2(0f, -110f);
        imgRect.sizeDelta = new Vector2(140f, 180f);

        if (characterId == "Virgil" || characterId == "Vini")
        {
            // Virgil's and Vini's sprites have huge transparent margins.
            // preserveAspect makes them tiny. We use localScale to scale them up uniformly from the center!
            imgRect.localScale = new Vector3(2.5f, 2.5f, 1f);
        }
        else
        {
            imgRect.localScale = Vector3.one;
        }

        Sprite previewSprite = GameSpriteManager.GetCharacterPreviewSprite(resourcePath);
        if (previewSprite != null)
        {
            previewImg.sprite = previewSprite;
            previewImg.color = Color.white;
            previewImg.preserveAspect = true;
        }

        // --- CHARACTER NAME ---
        GameObject nameGo = new GameObject("NameText");
        nameGo.transform.SetParent(cardGo.transform, false);
        RectTransform nameRect = nameGo.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 0f);
        nameRect.anchorMax = new Vector2(0.5f, 0f);
        nameRect.pivot = new Vector2(0.5f, 0f);
        nameRect.anchoredPosition = new Vector2(0f, 80f);
        nameRect.sizeDelta = new Vector2(180f, 40f);

        TextMeshProUGUI nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
        nameTmp.text = nameLabel;
        nameTmp.fontSize = 20;
        nameTmp.alignment = TextAlignmentOptions.Center;
        if (GameFontManager.TitleFont != null) nameTmp.font = GameFontManager.TitleFont;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = new Color(0.1f, 0.12f, 0.2f, 1f);

        // --- DESCRIPTION ---
        GameObject descGo = new GameObject("DescText");
        descGo.transform.SetParent(cardGo.transform, false);
        RectTransform descRect = descGo.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.5f, 0f);
        descRect.anchorMax = new Vector2(0.5f, 0f);
        descRect.pivot = new Vector2(0.5f, 0f);
        descRect.anchoredPosition = new Vector2(0f, 15f);
        descRect.sizeDelta = new Vector2(180f, 65f);

        TextMeshProUGUI descTmp = descGo.AddComponent<TextMeshProUGUI>();
        descTmp.text = descText;
        descTmp.fontSize = 12;
        descTmp.alignment = TextAlignmentOptions.Center;
        if (GameFontManager.BodyFont != null) descTmp.font = GameFontManager.BodyFont;
        descTmp.color = new Color(0.3f, 0.35f, 0.45f, 1f);
    }

    private void ConfirmOnboardingSelection()
    {
        PlayerPrefs.SetString("SelectedPlayer", tempSelectedPlayer);
        PlayerPrefs.SetInt("OnboardingCompleted", 1);
        PlayerPrefs.Save();

        // Re-initialize active player's visuals and orientation if they exist
        UpdateActivePlayerVisuals();

        // Start the game immediately!
        StartGame();
    }

    private System.Collections.IEnumerator AnimateOnboardingLoading()
    {
        float duration = 5.0f; // Slower loading (5.0 seconds)
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float pct = Mathf.Clamp01(elapsed / duration);

            if (onboardingLoadingFill != null)
            {
                onboardingLoadingFill.rectTransform.sizeDelta = new Vector2(pct * 494f, 22f);
            }
            if (onboardingLoadingText != null)
            {
                onboardingLoadingText.text = $"LOADING... {Mathf.RoundToInt(pct * 100f)}%";
            }

            yield return null;
        }

        onboardingPage = 2;
        loadingCoroutine = null;
        SetupUI();
    }

    private void EnsureEventSystemInputModule()
    {
        var eventSystem = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            var go = new GameObject("EventSystem");
            eventSystem = go.AddComponent<UnityEngine.EventSystems.EventSystem>();
        }

        var inputModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        if (inputModule == null)
        {
            inputModule = eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Try to load the input actions asset from Resources
        var asset = Resources.Load<UnityEngine.InputSystem.InputActionAsset>("InputSystem_Actions");
        if (asset != null)
        {
            inputModule.actionsAsset = asset;
            // Bind the UI actions to the input module using string paths or actions
            inputModule.point = UnityEngine.InputSystem.InputActionReference.Create(asset.FindAction("UI/Point"));
            inputModule.leftClick = UnityEngine.InputSystem.InputActionReference.Create(asset.FindAction("UI/Click"));
            inputModule.middleClick = UnityEngine.InputSystem.InputActionReference.Create(asset.FindAction("UI/MiddleClick"));
            inputModule.rightClick = UnityEngine.InputSystem.InputActionReference.Create(asset.FindAction("UI/RightClick"));
            inputModule.scrollWheel = UnityEngine.InputSystem.InputActionReference.Create(asset.FindAction("UI/ScrollWheel"));
            inputModule.move = UnityEngine.InputSystem.InputActionReference.Create(asset.FindAction("UI/Navigate"));
            inputModule.submit = UnityEngine.InputSystem.InputActionReference.Create(asset.FindAction("UI/Submit"));
            inputModule.cancel = UnityEngine.InputSystem.InputActionReference.Create(asset.FindAction("UI/Cancel"));
            inputModule.trackedDeviceOrientation = UnityEngine.InputSystem.InputActionReference.Create(asset.FindAction("UI/TrackedDeviceOrientation"));
            inputModule.trackedDevicePosition = UnityEngine.InputSystem.InputActionReference.Create(asset.FindAction("UI/TrackedDevicePosition"));
            
            Debug.Log("EnsureEventSystemInputModule: Successfully configured InputSystemUIInputModule with Resources/InputSystem_Actions.");
        }
        else
        {
            Debug.LogError("EnsureEventSystemInputModule: Could not find InputSystem_Actions in Resources!");
        }
    }
}
