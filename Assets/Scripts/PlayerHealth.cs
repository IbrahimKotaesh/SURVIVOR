using UnityEngine;
using System.Collections;

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
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateHealthBar();
    }

    public void TakeDamage(int amount)
    {
        if (isInvincible || currentHealth <= 0) return;

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        UpdateHealthBar();

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
}
