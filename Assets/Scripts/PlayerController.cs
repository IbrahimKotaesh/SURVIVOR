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
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;

        // Load upgraded moveSpeed from PlayerStats
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats == null)
        {
            stats = gameObject.AddComponent<PlayerStats>();
        }
        if (stats != null)
        {
            moveSpeed = stats.MoveSpeed;
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

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

    private void FixedUpdate()
    {
        // Apply movement physics-style
        rb.linearVelocity = moveInput * moveSpeed;
    }
}
