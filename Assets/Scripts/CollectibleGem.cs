using System.Collections;
using UnityEngine;

public class CollectibleGem : MonoBehaviour
{
    private bool isCollected = false;

    private void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = GameManager.GetOrCreateDiamondSprite();
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

        float particleSpawnTimer = 0f;
        float particleInterval = 0.04f; // Spawn a trail particle every 40ms

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

            // Spawn trail particles
            particleSpawnTimer += Time.deltaTime;
            if (particleSpawnTimer >= particleInterval)
            {
                particleSpawnTimer = 0f;
                SpawnTrailParticle(transform.position);
            }

            yield return null;
        }

        // Add score to GameManager upon impact
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(1);
        }

        Destroy(gameObject);
    }

    private void SpawnTrailParticle(Vector3 pos)
    {
        GameObject pGo = new GameObject("GemTrailParticle");
        pGo.transform.position = pos;
        
        // Randomize size
        float scale = Random.Range(0.12f, 0.25f);
        pGo.transform.localScale = new Vector3(scale, scale, 1f);

        SpriteRenderer sr = pGo.AddComponent<SpriteRenderer>();
        sr.sprite = DeathSplashEffect.GetOrCreateCircleSprite();
        
        // Cyber blue/cyan glowing trail color
        sr.color = new Color(0.35f, 0.75f, 1f, 0.85f);
        sr.sortingOrder = 4; // Render right behind the gem

        // Add a slight outward expansion drift
        Vector3 drift = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f).normalized * Random.Range(0.3f, 0.8f);

        StartCoroutine(AnimateTrailParticle(pGo, sr, drift));
    }

    private IEnumerator AnimateTrailParticle(GameObject pGo, SpriteRenderer sr, Vector3 drift)
    {
        float duration = 0.35f;
        float elapsed = 0f;
        Vector3 startScale = pGo.transform.localScale;
        Color startColor = sr.color;

        while (elapsed < duration)
        {
            if (pGo == null) yield break;
            elapsed += Time.deltaTime;
            float ratio = Mathf.Clamp01(elapsed / duration);

            // Move the particle along the drift direction (makes it "open up" behind)
            pGo.transform.position += drift * Time.deltaTime;

            // Fade and shrink
            sr.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * (1f - ratio));
            pGo.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, ratio);

            yield return null;
        }

        if (pGo != null)
        {
            Destroy(pGo);
        }
    }
}
