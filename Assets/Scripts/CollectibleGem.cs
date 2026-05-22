using System.Collections;
using UnityEngine;

public class CollectibleGem : MonoBehaviour
{
    private bool isCollected = false;

    private TrailRenderer trail;

    private void Awake()
    {
        // Setup TrailRenderer once for pooling
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.3f;
        trail.startWidth = 0.3f;
        trail.endWidth = 0f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = new Color(0.35f, 0.75f, 1f, 0.85f);
        trail.endColor = new Color(0.35f, 0.75f, 1f, 0f);
        trail.enabled = false; // Only enabled when flying to player
        trail.sortingOrder = 4;
    }

    private void OnEnable()
    {
        isCollected = false;
        transform.rotation = Quaternion.identity;
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = true;

        if (trail != null)
        {
            trail.Clear();
            trail.enabled = false;
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = GameManager.GetOrCreateDiamondSprite();
            if (sr.sprite != null)
            {
                float currentWorldWidth = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
                if (currentWorldWidth > 0f)
                {
                    float targetWorldSize = 0.5f;
                    float scaleFactor = targetWorldSize / currentWorldWidth;
                    transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
                }
                else
                {
                    transform.localScale = Vector3.one;
                }
            }
            else
            {
                transform.localScale = Vector3.one;
            }
        }
        else
        {
            transform.localScale = Vector3.one;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;

        // Check if collision is player (by tag or script)
        if (collision.CompareTag("Player") || collision.GetComponent<PlayerController>() != null)
        {
            isCollected = true;
            
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX("gem_collect");
            }
            
            // Disable trigger/colliders immediately so it can't be collected again during animation
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = false;
            }

            // Disable physics simulation to allow smooth transform animation without physics engine overrides
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.simulated = false;
            }

            StartCoroutine(AnimateGemCollection(collision.transform));
        }
    }

    private IEnumerator AnimateGemCollection(Transform playerTransform)
    {
        Vector3 startPos = transform.position;
        Vector3 originalScale = transform.localScale;
        
        // Phase 1: Small Circular Loop and Pop (0.3 seconds)
        float loopDuration = 0.3f;
        float elapsed = 0f;
        float radius = 0.4f;

        while (elapsed < loopDuration)
        {
            elapsed += Time.deltaTime;
            float ratio = Mathf.Clamp01(elapsed / loopDuration);
            
            // Circular offset starting and ending at (0,0)
            float angle = ratio * 2f * Mathf.PI;
            Vector3 offset = new Vector3(Mathf.Sin(angle), 1f - Mathf.Cos(angle), 0f) * radius;
            
            transform.position = startPos + offset;
            
            // Pop scale
            float scaleMultiplier = 1f + Mathf.Sin(ratio * Mathf.PI) * 0.4f;
            transform.localScale = originalScale * scaleMultiplier;

            // Spin rotation
            transform.rotation = Quaternion.Euler(0f, 0f, ratio * 360f);

            yield return null;
        }

        // Phase 2: Fly to Player with expanding trail (0.4 seconds)
        Vector3 loopEndPos = transform.position;
        Vector3 loopEndScale = transform.localScale;
        float flyDuration = 0.4f;
        elapsed = 0f;

        while (elapsed < flyDuration)
        {
            if (playerTransform == null) break; // Player died or became null

            elapsed += Time.deltaTime;
            float ratio = Mathf.Clamp01(elapsed / flyDuration);
            
            // Accelerating lerp (Ease In)
            float easeT = ratio * ratio; 
            
            transform.position = Vector3.Lerp(loopEndPos, playerTransform.position, easeT);
            transform.localScale = Vector3.Lerp(loopEndScale, Vector3.zero, ratio);

            // Spin even faster during fly
            transform.rotation = Quaternion.Euler(0f, 0f, 360f + ratio * 720f);

            // Enable TrailRenderer instead of spawning particles manually
            if (trail != null) trail.enabled = true;

            yield return null;
        }

        // Add score to GameManager upon impact
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(1);
        }

        ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
    }
}
