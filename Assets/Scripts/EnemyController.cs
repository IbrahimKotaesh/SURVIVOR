using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public static readonly System.Collections.Generic.List<EnemyController> ActiveEnemies = new System.Collections.Generic.List<EnemyController>();

    private void OnEnable()
    {
        if (!ActiveEnemies.Contains(this))
        {
            ActiveEnemies.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveEnemies.Remove(this);
    }

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Damage Settings")]
    [SerializeField] private int damageAmount = 10;

    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    private int currentHp = 1;
    private int maxHp = 1;
    private bool isBoss = false;
    private bool isOrange = false;
    private Canvas bossHealthCanvas;
    private UnityEngine.UI.Image bossHealthFill;
    private float baseSpeed = 2f;
    public bool IsBoss => isBoss;
    public bool IsOrange => isOrange;

    private void Awake()
    {
        baseSpeed = moveSpeed;
    }

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        FindPlayer();
    }

    public void SetupEnemy(float speedMultiplier, int hp = 1, bool boss = false, int baseDamage = 10, bool orange = false)
    {
        // Reset speed to base value before applying multiplier
        moveSpeed = baseSpeed;

        // Clean up any leftover boss health canvas from pooling
        Transform existingCanvas = transform.Find("BossHealthCanvas");
        if (existingCanvas != null)
        {
            existingCanvas.name = "TrashCanvas";
            Destroy(existingCanvas.gameObject);
        }

        currentHp = hp;
        maxHp = hp;
        isBoss = boss;
        isOrange = orange;
        damageAmount = baseDamage;

        if (isBoss)
        {
            CreateBossHealthBar();
            // Visual indicators for boss: larger scale, tinted red/pink (reduced from 2.5 to 2.0)
            transform.localScale = new Vector3(2.0f, 2.0f, 1f);
            var sprite = GetComponent<SpriteRenderer>();
            if (sprite != null)
            {
                // Reset sprite to default monster sprite first in case of recycling
                Sprite normalSprite = Resources.Load<Sprite>("monster_sprite");
                if (normalSprite != null) sprite.sprite = normalSprite;
                
                sprite.color = new Color(0.85f, 0.1f, 0.1f, 1f); // Deep Crimson Red
            }
            moveSpeed *= speedMultiplier;
            // Deal double damage to player
            damageAmount = baseDamage * 2;
        }
        else if (isOrange)
        {
            // Orange heavy enemy: 0.78 scale, slower speed, uses orange sprite
            transform.localScale = new Vector3(0.78f, 0.78f, 1f);
            var sprite = GetComponent<SpriteRenderer>();
            if (sprite != null)
            {
                Sprite orangeSprite = Resources.Load<Sprite>("monster_orange_sprite");
                if (orangeSprite != null)
                {
                    sprite.sprite = orangeSprite;
                }
                else
                {
                    sprite.color = new Color(1f, 0.55f, 0.05f, 1f);
                }
            }
            moveSpeed *= speedMultiplier * 0.85f;
        }
        else
        {
            // Regular purple enemy: shrunk from 1.0 to 0.62
            transform.localScale = new Vector3(0.62f, 0.62f, 1f);
            var sprite = GetComponent<SpriteRenderer>();
            if (sprite != null)
            {
                sprite.color = Color.white;
                Sprite normalSprite = Resources.Load<Sprite>("monster_sprite");
                if (normalSprite != null) sprite.sprite = normalSprite;
            }
            moveSpeed *= speedMultiplier;
        }
    }

    private void CreateBossHealthBar()
    {
        GameObject canvasGo = new GameObject("BossHealthCanvas");
        canvasGo.transform.SetParent(transform, false);
        bossHealthCanvas = canvasGo.AddComponent<Canvas>();
        bossHealthCanvas.renderMode = RenderMode.WorldSpace;
        bossHealthCanvas.sortingOrder = 15;
        
        RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1.2f, 0.15f);
        canvasRect.localPosition = new Vector3(0f, 0.7f, 0f); // Position above the boss sprite

        GameObject bgGo = new GameObject("Background");
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgImage = bgGo.AddComponent<UnityEngine.UI.Image>();
        bgImage.sprite = GameManager.GetOrCreateRoundedRectSprite();
        bgImage.type = UnityEngine.UI.Image.Type.Sliced;
        bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        RectTransform bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(bgGo.transform, false);
        bossHealthFill = fillGo.AddComponent<UnityEngine.UI.Image>();
        bossHealthFill.sprite = GameManager.GetOrCreateRoundedRectSprite();
        bossHealthFill.color = new Color(0.9f, 0.15f, 0.15f, 1f);
        bossHealthFill.type = UnityEngine.UI.Image.Type.Filled;
        bossHealthFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        bossHealthFill.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
        
        RectTransform fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        if (currentHp < 0) currentHp = 0;

        if (bossHealthFill != null && maxHp > 0)
        {
            bossHealthFill.fillAmount = (float)currentHp / maxHp;
        }

        if (currentHp <= 0)
        {
            // Spawn procedural death splash particles with custom colors matching the enemy type
            Color splashColor = Color.red;
            if (isBoss)
            {
                splashColor = new Color(0.85f, 0.1f, 0.1f, 1f); // Deep Crimson Red
            }
            else if (isOrange)
            {
                splashColor = new Color(1f, 0.55f, 0.05f, 1f); // Orange splash
            }
            else
            {
                splashColor = new Color(0.6f, 0.1f, 0.8f, 1f); // Purple splash
            }
            
            DeathSplashEffect.Create(transform.position, splashColor, isBoss ? 24 : 10);

            // Spawn floating text (Gold for Boss, Orange for Heavy Orange, Vibrant Green for Normal Purple)
            int scoreValue = isBoss ? 100 : (isOrange ? 30 : 10);
            Color textColor = isBoss ? new Color(1f, 0.82f, 0f, 1f) : (isOrange ? new Color(1f, 0.55f, 0f, 1f) : new Color(0.2f, 0.9f, 0.3f, 1f));
            DeathSplashEffect.CreateFloatingText(transform.position, $"+{scoreValue}", textColor);

            if (GameManager.Instance != null)
            {
                // Award coins immediately upon defeat
                GameManager.Instance.AddCoins(isBoss ? 5 : 1);

                // Spawn normal gem drop
                GameManager.Instance.SpawnGem(transform.position);

                if (isBoss)
                {
                    // Spawn extra gems for defeating boss!
                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 offset = new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(-0.8f, 0.8f), 0f);
                        GameManager.Instance.SpawnGem(transform.position + offset);
                    }

                    // Check if this was the last boss
                    bool otherBossesAlive = false;
                    foreach (var enemy in ActiveEnemies)
                    {
                        if (enemy != this && enemy.IsBoss)
                        {
                            otherBossesAlive = true;
                            break;
                        }
                    }

                    if (!otherBossesAlive)
                    {
                        // Tell GameManager the boss is dead to trigger victory
                        GameManager.Instance.OnBossDefeated();
                    }
                }
            }

            ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
        }
        else
        {
            StartCoroutine(FlashRedRoutine());
            if (isBoss)
            {
                Color splashColor = new Color(0.85f, 0.1f, 0.1f, 1f); // Deep Crimson Red
                DeathSplashEffect.Create(transform.position, splashColor, 5); // Small hit splash
            }
        }
    }

    private System.Collections.IEnumerator FlashRedRoutine()
    {
        if (spriteRenderer != null)
        {
            Color originalColor = isBoss ? new Color(0.85f, 0.1f, 0.1f, 1f) : Color.white;
            spriteRenderer.color = Color.white; // Flash white for boss, red for normal
            if (!isBoss) spriteRenderer.color = Color.red;
            
            yield return new WaitForSeconds(0.1f);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }
    }


    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }

        // Flip sprite to face player
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        if (direction.x > 0.05f)
        {
            spriteRenderer.flipX = true; // Faces right
        }
        else if (direction.x < -0.05f)
        {
            spriteRenderer.flipX = false; // Faces left
        }
    }

    private void FixedUpdate()
    {
        if (playerTransform == null || rb == null) return;

        Vector2 direction = ((Vector2)playerTransform.position - rb.position).normalized;
        rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.name == "Player")
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
            }
        }
    }
}
