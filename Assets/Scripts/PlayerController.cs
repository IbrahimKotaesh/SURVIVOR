using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Mobile Input")]
    [SerializeField] private Joystick joystick; // Optional joystick reference

    [Header("Sprite Direction Settings")]
    [SerializeField] private bool originalSpriteFacesRight = false; // Default eyeball sprite faces left

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveInput;
    private float speedBoostMultiplier = 1f;
    private int abilityUsesLeft = 5;
    public int AbilityUsesLeft => abilityUsesLeft;

    [Header("Juice Settings (Squash & Stretch)")]
    [SerializeField] private float bounceSpeed = 14f;
    [SerializeField] private float bounceScaleRange = 0.08f;
    private Vector3 originalScale;
    private float bounceTimer = 0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        originalScale = transform.localScale;

        // Configure originalSpriteFacesRight based on selected character (Virgil, Vini, and Yamal all face right)
        string selectedPlayer = PlayerPrefs.GetString("SelectedPlayer", "Virgil");
        if (selectedPlayer == "Vini" || selectedPlayer == "Virgil" || selectedPlayer == "Yamal")
        {
            originalSpriteFacesRight = true;
        }
        else
        {
            originalSpriteFacesRight = false;
        }

        // Load upgraded moveSpeed from PlayerStats
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
            moveSpeed = stats.MoveSpeed;
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        // Spacebar shortcut to activate signature ability
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TriggerAbilityIfAvailable();
        }

        // 1. Get input from joystick if available
        if (joystick != null && joystick.Direction.sqrMagnitude > 0.01f)
        {
            moveInput = joystick.Direction;
        }
        else
        {
            // Fallback to keyboard/WASD using New Input System
            float moveX = 0f;
            float moveY = 0f;
            
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveY = 1f;
                else if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveY = -1f;
                
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveX = -1f;
                else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveX = 1f;
            }
            
            moveInput = new Vector2(moveX, moveY).normalized;
        }

        // 2. Flip sprite based on horizontal movement direction and original sprite orientation
        if (spriteRenderer != null)
        {
            if (moveInput.x > 0.05f)
            {
                spriteRenderer.flipX = !originalSpriteFacesRight;
            }
            else if (moveInput.x < -0.05f)
            {
                spriteRenderer.flipX = originalSpriteFacesRight;
            }
        }

        // 3. Squash & Stretch animation juice when moving
        if (moveInput.sqrMagnitude > 0.01f)
        {
            bounceTimer += Time.deltaTime * bounceSpeed;
            // Sine wave to swap squash & stretch on X and Y
            float scaleY = 1f + Mathf.Sin(bounceTimer) * bounceScaleRange;
            float scaleX = 1f - Mathf.Sin(bounceTimer) * bounceScaleRange;
            transform.localScale = new Vector3(originalScale.x * scaleX, originalScale.y * scaleY, originalScale.z);
        }
        else
        {
            bounceTimer = 0f;
            // Return smoothly to normal scale when idle
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * 10f);
        }
    }

    public void SetFacingDirection(bool facesRight)
    {
        originalSpriteFacesRight = facesRight;
    }

    private void FixedUpdate()
    {
        // Apply movement physics-style
        rb.linearVelocity = moveInput * moveSpeed * speedBoostMultiplier;
    }

    // --- SIGNATURE PLAYER ABILITIES ---

    public void TriggerAbilityIfAvailable()
    {
        if (abilityUsesLeft <= 0) return;

        string selectedPlayer = PlayerPrefs.GetString("SelectedPlayer", "Virgil");
        if (selectedPlayer == "Vini")
        {
            TriggerSambaSprint();
        }
        else if (selectedPlayer == "Yamal")
        {
            TriggerYamalStrike();
        }
        else
        {
            TriggerShieldWave();
        }

        abilityUsesLeft--;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateAbilityButtonVisuals(abilityUsesLeft);
        }
    }

    private void TriggerShieldWave()
    {
        // 1. Shake screen violently
        if (CameraController.Instance != null)
        {
            CameraController.Instance.TriggerShake(0.55f, 0.45f);
        }

        // Play SFX
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("shield_blast");
        }

        // 2. Spawn expanding wave visual effect that applies dynamic propagation damage
        StartCoroutine(SpawnWaveVisualEffect());
    }

    private System.Collections.IEnumerator SpawnWaveVisualEffect()
    {
        GameObject wave = new GameObject("ShieldWaveVisual");
        wave.transform.position = transform.position;
        wave.transform.localScale = Vector3.zero;

        SpriteRenderer sr = wave.AddComponent<SpriteRenderer>();
        sr.sprite = PlayerHealth.GetOrCreateRoundedRectSprite(); // Reuse flat green rounded rect bar sprite
        sr.color = new Color(0.15f, 0.65f, 0.95f, 0.45f); // Rich sapphire semi-transparent blue
        sr.sortingOrder = 10; // Overlay

        float elapsed = 0f;
        float duration = 0.7f; // 0.7 seconds duration for a highly visible propagation sweep
        Vector3 targetScale = new Vector3(30f, 30f, 1f);

        System.Collections.Generic.HashSet<EnemyController> hitEnemies = new System.Collections.Generic.HashSet<EnemyController>();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pct = elapsed / duration;
            
            float scaleFactor = Mathf.Lerp(0f, 1f, pct);
            wave.transform.localScale = targetScale * scaleFactor;

            // Calculate exact physical radius based on sprite dimensions:
            // The rounded rect sprite is 64x64 pixels with a pixelsPerUnit of 100f.
            // This means its base radius at scale 1 is 32 / 100 = 0.32 units in world space.
            float currentRadius = wave.transform.localScale.x * 0.32f;

            // Deal damage to any enemy that has been touched by the expanding wave
            var enemies = GameObject.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (enemies != null)
            {
                Vector3 centerPosition = wave.transform.position;
                foreach (var enemy in enemies)
                {
                    if (enemy == null || hitEnemies.Contains(enemy)) continue;

                    float dist = Vector3.Distance(centerPosition, enemy.transform.position);
                    if (dist <= currentRadius)
                    {
                        if (Camera.main != null)
                        {
                            Vector3 screenPoint = Camera.main.WorldToViewportPoint(enemy.transform.position);
                            bool onScreen = screenPoint.z > 0 && screenPoint.x >= 0f && screenPoint.x <= 1f && screenPoint.y >= 0f && screenPoint.y <= 1f;
                            
                            if (onScreen)
                            {
                                hitEnemies.Add(enemy);
                                if (enemy.IsBoss)
                                {
                                    enemy.TakeDamage(5); // Substantial boss damage
                                }
                                else
                                {
                                    enemy.TakeDamage(100); // Vaporize normal enemy
                                }
                            }
                        }
                    }
                }
            }

            sr.color = new Color(0.15f, 0.65f, 0.95f, 0.45f * (1f - pct));

            yield return null;
        }

        Destroy(wave);
    }

    private void TriggerSambaSprint()
    {
        StartCoroutine(SambaSprintRoutine());
    }

    private System.Collections.IEnumerator SambaSprintRoutine()
    {
        speedBoostMultiplier = 1.6f; // Faster speed boost

        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.SetSprintInvincible(true);
        }

        PlayerAttack attack = GetComponent<PlayerAttack>();

        // Play SFX & switch BGM to samba
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("samba_sprint");
            SoundManager.Instance.PlayBGM("samba");
        }

        float duration = 4.0f; // 4 seconds of absolute machine gun carnage
        float elapsed = 0f;
        float trailTimer = 0f;
        float shootTimer = 0f;
        float shootInterval = 0.04f; // 25 shots per second!
        float spiralAngle = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            trailTimer += Time.deltaTime;
            shootTimer += Time.deltaTime;

            if (trailTimer >= 0.08f)
            {
                trailTimer = 0f;
                SpawnSpeedFlareGhost();
            }

            // Shoot machine-gun projectiles!
            if (shootTimer >= shootInterval)
            {
                shootTimer = 0f;

                if (attack != null && attack.ProjectilePrefab != null)
                {
                    // 1. Spiral Stream (Vaporizes crowd in a spinning circle)
                    float rad = spiralAngle * Mathf.Deg2Rad;
                    Vector3 spiralDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
                    spiralAngle += 24f; // Spiraling rotation step

                    GameObject proj1 = Instantiate(attack.ProjectilePrefab, transform.position, Quaternion.identity);
                    Projectile pScript1 = proj1.GetComponent<Projectile>();
                    if (pScript1 != null)
                    {
                        pScript1.Setup(transform.position + spiralDir);
                    }

                    // 2. Focused Stream (Targets nearest enemy for boss/focused damage)
                    Transform nearest = FindNearestEnemyForMachineGun();
                    if (nearest != null)
                    {
                        GameObject proj2 = Instantiate(attack.ProjectilePrefab, transform.position, Quaternion.identity);
                        Projectile pScript2 = proj2.GetComponent<Projectile>();
                        if (pScript2 != null)
                        {
                            pScript2.Setup(nearest.position);
                        }
                    }
                }
            }

            yield return null;
        }

        if (health != null)
        {
            health.SetSprintInvincible(false);
        }
        speedBoostMultiplier = 1f;

        // Switch BGM back to battle
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM("battle");
        }
    }

    private Transform FindNearestEnemyForMachineGun()
    {
        var clones = GameObject.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (clones == null || clones.Length == 0) return null;

        Transform nearest = null;
        float minDistance = 12f; // Large search range for machine gun targeting

        foreach (var enemy in clones)
        {
            if (enemy == null) continue;
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = enemy.transform;
            }
        }
        return nearest;
    }

    private void SpawnSpeedFlareGhost()
    {
        GameObject ghost = new GameObject("SpeedFlareGhost");
        ghost.transform.position = transform.position;
        ghost.transform.localScale = transform.localScale;

        SpriteRenderer playerSr = GetComponentInChildren<SpriteRenderer>();
        if (playerSr == null) playerSr = GetComponent<SpriteRenderer>();

        if (playerSr != null && playerSr.sprite != null)
        {
            SpriteRenderer ghostSr = ghost.AddComponent<SpriteRenderer>();
            ghostSr.sprite = playerSr.sprite;
            ghostSr.flipX = playerSr.flipX;
            ghostSr.sortingOrder = playerSr.sortingOrder - 1; // Render behind player

            // Golden-orange speed flare tint
            ghostSr.color = new Color(1f, Random.Range(0.2f, 0.5f), 0.05f, 0.6f);

            StartCoroutine(FadeGhostRoutine(ghost, ghostSr, 0.4f));
        }
        else
        {
            Destroy(ghost);
        }
    }

    private System.Collections.IEnumerator FadeGhostRoutine(GameObject ghost, SpriteRenderer sr, float fadeDuration)
    {
        float elapsed = 0f;
        Color startCol = sr.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float pct = elapsed / fadeDuration;

            if (sr != null)
            {
                sr.color = new Color(startCol.r, startCol.g, startCol.b, startCol.a * (1f - pct));
            }
            yield return null;
        }

        Destroy(ghost);
    }

    private void TriggerYamalStrike()
    {
        StartCoroutine(YamalStrikeRoutine());
    }

    private System.Collections.IEnumerator YamalStrikeRoutine()
    {
        speedBoostMultiplier = 1.5f; // Golden speed burst

        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.SetSprintInvincible(true); // Invincible during finesse sweep
        }

        PlayerAttack attack = GetComponent<PlayerAttack>();

        // Play SFX
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("samba_sprint"); // Re-use fast chiptune audio
        }

        // Spawn a circular ring of 16 spiraling golden projectiles
        if (attack != null && attack.ProjectilePrefab != null)
        {
            int projectileCount = 16;
            float baseAngleOffset = Random.Range(0f, 360f); // Randomized start angle
            for (int i = 0; i < projectileCount; i++)
            {
                float angle = baseAngleOffset + (i * (360f / projectileCount));
                float rad = angle * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

                GameObject proj = Instantiate(attack.ProjectilePrefab, transform.position, Quaternion.identity);
                
                // Color the projectile golden/yellow
                SpriteRenderer projSr = proj.GetComponent<SpriteRenderer>();
                if (projSr != null)
                {
                    projSr.color = new Color(1f, 0.8f, 0f, 1f); // Vibrant gold
                }

                // Add TrailRenderer for a beautiful trace effect
                TrailRenderer tr = proj.AddComponent<TrailRenderer>();
                tr.time = 0.25f;
                tr.startWidth = 0.15f;
                tr.endWidth = 0f;
                tr.material = new Material(Shader.Find("Sprites/Default"));
                tr.startColor = new Color(1f, 0.8f, 0f, 0.8f);
                tr.endColor = new Color(1f, 0.4f, 0f, 0f);

                Projectile pScript = proj.GetComponent<Projectile>();
                if (pScript != null)
                {
                    // speed=12f, damage=5, piercing=true, curveIntensity=140f (creates a nice spiral out)
                    pScript.SetupCustom(dir, 12f, 5, true, 140f);
                }
            }
        }

        float duration = 3.0f;
        float elapsed = 0f;
        float trailTimer = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            trailTimer += Time.deltaTime;

            if (trailTimer >= 0.08f)
            {
                trailTimer = 0f;
                SpawnSpeedFlareGhost(); // Spawn flashy golden trail behind the player
            }

            yield return null;
        }

        if (health != null)
        {
            health.SetSprintInvincible(false);
        }
        speedBoostMultiplier = 1f;
    }
}
