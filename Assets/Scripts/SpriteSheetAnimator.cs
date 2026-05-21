using UnityEngine;

public class SpriteSheetAnimator : MonoBehaviour
{
    [Header("Sprite Sheet Settings")]
    [SerializeField] private string resourcePath = "player_eyeball_sprite";
    [SerializeField] private float frameRate = 30f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Sprite[] frames;
    private int currentFrame = 0;
    private float timer = 0f;
    private bool isMoving = false;

    private void Start()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        rb = GetComponent<Rigidbody2D>();

        // Load all sub-sprites dynamically from Resources folder
        resourcePath = "vergil_van_dijk_2";
        frames = Resources.LoadAll<Sprite>(resourcePath);
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning($"SpriteSheetAnimator: No sprites found at Resources/{resourcePath}. Trying fallback 'player_eyeball_sprite'...");
            resourcePath = "player_eyeball_sprite";
            frames = Resources.LoadAll<Sprite>(resourcePath);
        }

        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning($"SpriteSheetAnimator: Fallback failed. No sprites found.");
        }
        else
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = frames[0];
            }

            PlayerHealth ph = GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.RepositionHealthBar();
            }
        }
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0 || spriteRenderer == null) return;
        if (Time.timeScale == 0f) return; // Pause animation if game is paused

        // Determine if player is moving
        if (rb != null)
        {
            isMoving = rb.linearVelocity.magnitude > 0.1f;
        }
        else
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");
            isMoving = (moveX != 0f || moveY != 0f);
        }

        if (isMoving)
        {
            timer += Time.deltaTime;
            float timePerFrame = 1f / frameRate;

            if (timer >= timePerFrame)
            {
                timer -= timePerFrame;
                currentFrame++;

                if (currentFrame >= frames.Length)
                {
                    currentFrame = 0; // Loop running animation
                }

                spriteRenderer.sprite = frames[currentFrame];
            }
        }
        else
        {
            // Idle: Show the first frame (standing stance)
            currentFrame = 0;
            if (frames != null && frames.Length > 0)
            {
                spriteRenderer.sprite = frames[0];
            }
            timer = 0f;
        }
    }

    public int GetFramesCount() => frames != null ? frames.Length : -1;
    public string GetResourcePath() => resourcePath;
}
