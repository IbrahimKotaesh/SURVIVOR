using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Mobile Input")]
    [SerializeField] private Joystick joystick; // Optional joystick reference

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveInput;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
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

        // 2. Flip sprite based on horizontal movement direction
        if (spriteRenderer != null)
        {
            if (moveInput.x > 0.05f)
            {
                spriteRenderer.flipX = true; // Faces right
            }
            else if (moveInput.x < -0.05f)
            {
                spriteRenderer.flipX = false; // Faces left (original orientation)
            }
        }
    }

    private void FixedUpdate()
    {
        // Apply movement physics-style
        rb.linearVelocity = moveInput * moveSpeed;
    }
}
