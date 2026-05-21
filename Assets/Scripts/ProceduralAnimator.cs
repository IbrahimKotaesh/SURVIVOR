using UnityEngine;

public class ProceduralAnimator : MonoBehaviour
{
    [Header("Bobbing Settings")]
    [SerializeField] private float bobSpeed = 14f;
    [SerializeField] private float bobAmount = 0.12f;

    [Header("Tilt Settings")]
    [SerializeField] private float tiltAngle = 8f;

    [Header("Scale Settings")]
    [SerializeField] private float squashStretchAmount = 0.06f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Vector3 originalScale;
    private float timer = 0f;

    private void Start()
    {
        // Find SpriteRenderer in children (common for player setups) or on same GameObject
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer != null)
        {
            originalScale = spriteRenderer.transform.localScale;
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f || spriteRenderer == null) return;

        bool isMoving = false;

        // Check if player is moving via Rigidbody velocity
        if (rb != null)
        {
            isMoving = rb.linearVelocity.magnitude > 0.1f;
        }
        else
        {
            // Fallback: check raw input axes
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");
            isMoving = (moveX != 0f || moveY != 0f);
        }

        if (isMoving)
        {
            timer += Time.deltaTime * bobSpeed;

            // 1. Bobbing Up and Down (Sine absolute wave for jumping step feel)
            float bobOffset = Mathf.Abs(Mathf.Sin(timer)) * bobAmount;
            spriteRenderer.transform.localPosition = new Vector3(0f, bobOffset, 0f);

            // 2. Squash and Stretch (Scales X and Y opposite to each other)
            float scaleY = originalScale.y - (Mathf.Sin(timer) * squashStretchAmount);
            float scaleX = originalScale.x + (Mathf.Sin(timer) * squashStretchAmount * 0.6f);
            spriteRenderer.transform.localScale = new Vector3(scaleX, scaleY, originalScale.z);

            // 3. Dynamic Tilting Left/Right based on steps
            float tilt = Mathf.Sin(timer) * tiltAngle;
            spriteRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
        }
        else
        {
            // Smoothly ease back to original static state (Idle)
            timer = 0f;
            spriteRenderer.transform.localPosition = Vector3.Lerp(spriteRenderer.transform.localPosition, Vector3.zero, Time.deltaTime * 12f);
            spriteRenderer.transform.localScale = Vector3.Lerp(spriteRenderer.transform.localScale, originalScale, Time.deltaTime * 12f);
            spriteRenderer.transform.localRotation = Quaternion.Slerp(spriteRenderer.transform.localRotation, Quaternion.identity, Time.deltaTime * 12f);
        }
    }
}
