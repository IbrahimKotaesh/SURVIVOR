using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float invincibilityDuration = 0.5f;

    [Header("UI References")]
    [SerializeField] private UnityEngine.UI.Image healthBarFill;

    private int currentHealth;
    private bool isInvincible = false;
    private SpriteRenderer spriteRenderer;
    private Coroutine healthBarCoroutine;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsInvincible => isInvincible;

    private void Start()
    {
        // Load upgraded maxHealth from PlayerStats
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats == null)
        {
            stats = gameObject.AddComponent<PlayerStats>();
        }
        if (stats != null)
        {
            maxHealth = stats.MaxHP;
        }

        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        StyleHealthBarUI();
        UpdateHealthBar();
        RepositionHealthBar();
    }

    public void TakeDamage(int amount)
    {
        if (isInvincible || currentHealth <= 0) return;

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        UpdateHealthBar();

        if (CameraController.Instance != null)
        {
            CameraController.Instance.TriggerShake(0.22f, 0.22f);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(FlashRoutine());
        }
    }

    private void Die()
    {
        var controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;

        var attack = GetComponent<PlayerAttack>();
        if (attack != null) attack.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        StartCoroutine(DeathScaleRoutine());

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDeath();
        }
    }

    private IEnumerator FlashRoutine()
    {
        isInvincible = true;
        float elapsed = 0f;
        bool toggle = false;

        while (elapsed < invincibilityDuration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = toggle ? new Color(1f, 0.3f, 0.3f, 0.9f) : new Color(1f, 1f, 1f, 0.4f);
            }
            toggle = !toggle;
            yield return new WaitForSeconds(0.08f);
            elapsed += 0.08f;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
        isInvincible = false;
    }

    private IEnumerator DeathScaleRoutine()
    {
        Vector3 initialScale = transform.localScale;
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = Vector3.zero;
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            if (healthBarCoroutine != null)
            {
                StopCoroutine(healthBarCoroutine);
            }
            healthBarCoroutine = StartCoroutine(SmoothHealthBarRoutine((float)currentHealth / maxHealth));
        }
    }

    private IEnumerator SmoothHealthBarRoutine(float targetFill)
    {
        float startFill = healthBarFill.fillAmount;
        float elapsed = 0f;
        float duration = 0.25f; // Duration of transition

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            healthBarFill.fillAmount = Mathf.Lerp(startFill, targetFill, elapsed / duration);
            yield return null;
        }

        healthBarFill.fillAmount = targetFill;
        healthBarCoroutine = null;
    }

    public void RepositionHealthBar()
    {
        if (healthBarFill != null)
        {
            Canvas canvas = healthBarFill.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.transform.parent == transform)
            {
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    sr = GetComponentInChildren<SpriteRenderer>();
                }

                if (sr != null && sr.sprite != null)
                {
                    float spriteHeight = sr.sprite.rect.height / sr.sprite.pixelsPerUnit;
                    float playerScaleY = transform.localScale.y;
                    if (playerScaleY == 0f) playerScaleY = 1f;

                    // Place health bar slightly above the player's head, accounting for scale
                    float worldTargetHeight = (spriteHeight * 0.5f) * playerScaleY + 0.28f;
                    float localY = worldTargetHeight / playerScaleY;

                    canvas.transform.localPosition = new Vector3(0f, localY, 0f);
                }
            }
        }
    }

    private static Sprite roundedRectSprite;
    public static Sprite GetOrCreateRoundedRectSprite()
    {
        if (roundedRectSprite != null) return roundedRectSprite;
        
        int size = 32;
        int radius = 8;
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

    private void StyleHealthBarUI()
    {
        if (healthBarFill != null)
        {
            // Set fill image sprite to sliced flat-vector green bar fill
            healthBarFill.sprite = GameSpriteManager.GetSprite("hud_bar_fill_green");
            healthBarFill.type = UnityEngine.UI.Image.Type.Sliced;
            healthBarFill.color = new Color(0.15f, 0.85f, 0.35f, 1f);

            // Get fill RectTransform and make it fill container
            RectTransform fillRect = healthBarFill.GetComponent<RectTransform>();
            if (fillRect != null)
            {
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.sizeDelta = Vector2.zero;
            }

            // Set up background
            Transform parent = healthBarFill.transform.parent;
            if (parent != null)
            {
                UnityEngine.UI.Image bgImg = parent.GetComponent<UnityEngine.UI.Image>();
                if (bgImg == null)
                {
                    bgImg = parent.gameObject.AddComponent<UnityEngine.UI.Image>();
                }
                
                bgImg.sprite = GameSpriteManager.GetSprite("hud_bar_empty");
                bgImg.type = UnityEngine.UI.Image.Type.Sliced;
                bgImg.color = new Color(0.12f, 0.12f, 0.12f, 0.8f);

                // Set parent size to be slightly larger than fill to act as a border
                RectTransform parentRect = parent.GetComponent<RectTransform>();
                if (parentRect != null)
                {
                    parentRect.anchorMin = new Vector2(0.5f, 0.5f);
                    parentRect.anchorMax = new Vector2(0.5f, 0.5f);
                    parentRect.pivot = new Vector2(0.5f, 0.5f);
                    parentRect.anchoredPosition = Vector2.zero;
                    parentRect.sizeDelta = new Vector2(25f, 3.5f);
                }

                // Add nice black outline to the background
                Outline outline = parent.gameObject.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = parent.gameObject.AddComponent<Outline>();
                }
                outline.effectColor = new Color(0f, 0f, 0f, 0.4f);
                outline.effectDistance = new Vector2(1f, 1f);
            }
        }
    }
}
