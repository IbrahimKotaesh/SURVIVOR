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
    private bool isSprintInvincible = false;
    private SpriteRenderer spriteRenderer;
    private Coroutine healthBarCoroutine;
    private UnityEngine.UI.Image catchUpFill;
    private Coroutine catchUpCoroutine;
    private Coroutine pulseCoroutine;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsInvincible => isInvincible;
    public bool IsSprintInvincible => isSprintInvincible;

    private void Start()
    {
        // Load upgraded maxHealth from PlayerStats
        PlayerStats stats = PlayerStats.Instance;
        if (stats == null)
        {
            stats = GetComponent<PlayerStats>();
            if (stats == null)
            {
                stats = gameObject.AddComponent<PlayerStats>();
            }
        }
        if (stats != null)
        {
            maxHealth = stats.MaxHP;
        }

        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        StyleHealthBarUI();
        UpdateHealthBar(false);
        RepositionHealthBar();
    }

    public void SetSprintInvincible(bool val)
    {
        isSprintInvincible = val;
    }

    public void TakeDamage(int amount)
    {
        if (isInvincible || isSprintInvincible || currentHealth <= 0) return;

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        UpdateHealthBar();

        if (amount > 0)
        {
            if (CameraController.Instance != null)
            {
                // Trigger screen shake on damage
                CameraController.Instance.TriggerShake(0.15f, 0.08f);
            }
            #if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
            #endif
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX("player_hurt");
            }
            StartCoroutine(FlashRoutine());
        }
    }

    public void Heal(int amount)
    {
        if (currentHealth <= 0) return;

        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        UpdateHealthBar();

        // Create floating green text above player
        DeathSplashEffect.CreateFloatingText(transform.position + Vector3.up * 0.5f, $"+{amount} HP", new Color(0.2f, 1.0f, 0.4f, 1f));

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("heal");
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

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("player_death");
        }

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

    private void UpdateHealthBar(bool animate = true)
    {
        if (healthBarFill != null)
        {
            float targetFill = (float)currentHealth / maxHealth;
            
            if (healthBarCoroutine != null)
            {
                StopCoroutine(healthBarCoroutine);
            }
            if (catchUpCoroutine != null)
            {
                StopCoroutine(catchUpCoroutine);
            }

            // If we are healing or at start, move both together
            float currentFill = healthBarFill.fillAmount;
            if (targetFill >= currentFill || !animate)
            {
                healthBarFill.fillAmount = targetFill;
                if (catchUpFill != null)
                {
                    catchUpFill.fillAmount = targetFill;
                }
            }
            else
            {
                // Taking damage: green bar snaps instantly, catch-up bar slides down
                healthBarFill.fillAmount = targetFill;
                catchUpCoroutine = StartCoroutine(SmoothCatchUpRoutine(targetFill));
            }
            
            if (animate)
            {
                // Trigger health bar pop/shake animation
                if (pulseCoroutine != null)
                {
                    StopCoroutine(pulseCoroutine);
                }
                pulseCoroutine = StartCoroutine(PulseHealthBarRoutine());
            }
        }
    }

    private IEnumerator SmoothCatchUpRoutine(float targetFill)
    {
        if (catchUpFill == null) yield break;

        // Pause briefly before draining
        yield return new WaitForSeconds(0.22f);

        float startFill = catchUpFill.fillAmount;
        float elapsed = 0f;
        float duration = 0.35f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            catchUpFill.fillAmount = Mathf.Lerp(startFill, targetFill, elapsed / duration);
            yield return null;
        }

        catchUpFill.fillAmount = targetFill;
        catchUpCoroutine = null;
    }

    private IEnumerator PulseHealthBarRoutine()
    {
        if (healthBarFill != null && healthBarFill.transform.parent != null)
        {
            Transform barParent = healthBarFill.transform.parent;
            Vector3 originalScale = Vector3.one;
            
            // Pop out (scale up)
            float popDuration = 0.07f;
            float elapsed = 0f;
            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / popDuration;
                barParent.localScale = Vector3.Lerp(originalScale, originalScale * 1.35f, t);
                yield return null;
            }

            // Return to original size
            elapsed = 0f;
            float returnDuration = 0.12f;
            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / returnDuration;
                barParent.localScale = Vector3.Lerp(originalScale * 1.35f, originalScale, t);
                yield return null;
            }

            barParent.localScale = originalScale;
            pulseCoroutine = null;
        }
    }


    private float cachedHealthBarOffset = -1f;
    private Sprite lastCachedSprite;
    private Vector3 initialCanvasScale;
    private bool initializedCanvasScale = false;

    public void RepositionHealthBar()
    {
        if (healthBarFill != null)
        {
            Canvas canvas = healthBarFill.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.transform.parent == transform)
            {
                if (!initializedCanvasScale)
                {
                    initialCanvasScale = canvas.transform.localScale;
                    initializedCanvasScale = true;
                }

                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    sr = GetComponentInChildren<SpriteRenderer>();
                }

                if (sr != null && sr.sprite != null)
                {
                    if (sr.sprite != lastCachedSprite)
                    {
                        lastCachedSprite = sr.sprite;
                        float spriteHeight = sr.sprite.rect.height / sr.sprite.pixelsPerUnit;
                        float pivotY = sr.sprite.pivot.y / sr.sprite.rect.height;
                        float playerScaleY = transform.localScale.y;
                        if (playerScaleY == 0f) playerScaleY = 1f;

                        // Calculate offset dynamically based on the sprite's height and pivot Y
                        float distanceToTop = spriteHeight * (1.0f - pivotY);
                        cachedHealthBarOffset = distanceToTop * playerScaleY + 0.28f;
                    }
                }
                else if (cachedHealthBarOffset < 0f)
                {
                    cachedHealthBarOffset = 1.0f; // Fallback
                }

                // Inverse scale the canvas so it doesn't squash/stretch with the player,
                // but preserve its original small scale used for World Space Canvas!
                canvas.transform.localScale = new Vector3(
                    initialCanvasScale.x / transform.localScale.x,
                    initialCanvasScale.y / transform.localScale.y,
                    initialCanvasScale.z / transform.localScale.z
                );

                // Set world position directly using the fixed offset
                canvas.transform.position = transform.position + new Vector3(0f, cachedHealthBarOffset, 0f);
            }
        }
    }

    private void LateUpdate()
    {
        RepositionHealthBar();
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
            Transform parent = healthBarFill.transform.parent;
            if (parent != null)
            {
                // Create catch-up fill if it doesn't exist
                if (catchUpFill == null)
                {
                    GameObject catchUpGo = new GameObject("HUD_HealthBarCatchUp");
                    catchUpGo.transform.SetParent(parent, false);
                    catchUpFill = catchUpGo.AddComponent<UnityEngine.UI.Image>();
                    
                    // Render it behind healthBarFill
                    catchUpGo.transform.SetSiblingIndex(healthBarFill.transform.GetSiblingIndex());
                }

                UnityEngine.UI.Image bgImg = parent.GetComponent<UnityEngine.UI.Image>();
                if (bgImg == null)
                {
                    bgImg = parent.gameObject.AddComponent<UnityEngine.UI.Image>();
                }
                
                bgImg.sprite = GameSpriteManager.GetSprite("hud_bar_empty");
                bgImg.type = UnityEngine.UI.Image.Type.Sliced;
                bgImg.color = new Color(0.12f, 0.12f, 0.12f, 0.8f);

                // Set parent size to be even larger (60f x 8f, up from 25f x 3.5f)
                RectTransform parentRect = parent.GetComponent<RectTransform>();
                if (parentRect != null)
                {
                    parentRect.anchorMin = new Vector2(0.5f, 0.5f);
                    parentRect.anchorMax = new Vector2(0.5f, 0.5f);
                    parentRect.pivot = new Vector2(0.5f, 0.5f);
                    parentRect.anchoredPosition = Vector2.zero;
                    parentRect.sizeDelta = new Vector2(60f, 8.0f);
                }

                // Add nice black outline to the background
                Outline outline = parent.gameObject.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = parent.gameObject.AddComponent<Outline>();
                }
                outline.effectColor = new Color(0f, 0f, 0f, 0.65f); // Thicker, darker outline
                outline.effectDistance = new Vector2(1.5f, 1.5f);
            }

            // Set fill image sprite to flat-vector green bar fill (Filled type for draining animation)
            healthBarFill.sprite = GameSpriteManager.GetSprite("hud_bar_fill_green");
            healthBarFill.type = UnityEngine.UI.Image.Type.Filled;
            healthBarFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            healthBarFill.fillOrigin = 0; // Left
            healthBarFill.color = new Color(0.15f, 0.85f, 0.35f, 1f);

            // Get fill RectTransform and make it fill container
            RectTransform fillRect = healthBarFill.GetComponent<RectTransform>();
            if (fillRect != null)
            {
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.sizeDelta = Vector2.zero;
            }

            // Style catch-up fill to match layout (Filled type for draining animation)
            if (catchUpFill != null)
            {
                catchUpFill.sprite = GameSpriteManager.GetSprite("hud_bar_fill_green");
                catchUpFill.type = UnityEngine.UI.Image.Type.Filled;
                catchUpFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
                catchUpFill.fillOrigin = 0; // Left
                catchUpFill.color = new Color(0.85f, 0.15f, 0.15f, 1f); // Red catch-up
                catchUpFill.fillAmount = healthBarFill.fillAmount;

                RectTransform catchUpRect = catchUpFill.GetComponent<RectTransform>();
                if (catchUpRect != null)
                {
                    catchUpRect.anchorMin = Vector2.zero;
                    catchUpRect.anchorMax = Vector2.one;
                    catchUpRect.sizeDelta = Vector2.zero;
                }
            }
        }
    }
}
