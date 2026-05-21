using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Damage Settings")]
    [SerializeField] private int damageAmount = 10;

    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    private int currentHp = 1;
    private bool isBoss = false;
    public bool IsBoss => isBoss;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        FindPlayer();
    }

    public void SetupEnemy(float speedMultiplier, int hp = 1, bool boss = false, int baseDamage = 10)
    {
        currentHp = hp;
        isBoss = boss;
        moveSpeed *= speedMultiplier;
        damageAmount = baseDamage;

        if (isBoss)
        {
            // Visual indicators for boss: larger scale, tinted red/pink
            transform.localScale = new Vector3(2.5f, 2.5f, 1f);
            var sprite = GetComponent<SpriteRenderer>();
            if (sprite != null)
            {
                sprite.color = new Color(1f, 0.4f, 0.4f, 1f);
            }
            // Deal double damage to player
            damageAmount = baseDamage * 2;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        if (currentHp <= 0)
        {
            // Spawn procedural death splash particles
            Color splashColor = spriteRenderer != null ? spriteRenderer.color : Color.red;
            DeathSplashEffect.Create(transform.position, splashColor, isBoss ? 24 : 10);

            // Spawn floating text (Gold for Boss, Vibrant Green for Normal)
            int scoreValue = isBoss ? 100 : 10;
            Color textColor = isBoss ? new Color(1f, 0.82f, 0f, 1f) : new Color(0.2f, 0.9f, 0.3f, 1f);
            DeathSplashEffect.CreateFloatingText(transform.position, $"+{scoreValue}", textColor);

            // Screen shake juice!
            if (CameraController.Instance != null)
            {
                CameraController.Instance.TriggerShake(isBoss ? 0.15f : 0.05f, isBoss ? 0.1f : 0.02f);
            }

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

                    // Tell GameManager the boss is dead to trigger victory
                    GameManager.Instance.OnBossDefeated();
                }
            }

            Destroy(gameObject);
        }
        else
        {
            StartCoroutine(FlashRedRoutine());
        }
    }

    private System.Collections.IEnumerator FlashRedRoutine()
    {
        if (spriteRenderer != null)
        {
            Color originalColor = isBoss ? new Color(1f, 0.4f, 0.4f, 1f) : Color.white;
            spriteRenderer.color = Color.red;
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
