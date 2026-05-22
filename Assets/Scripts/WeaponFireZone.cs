using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WeaponFireZone : MonoBehaviour
{
    private float cooldown = 4.0f;
    private float timer;
    private int level = 1;

    public void LevelUp()
    {
        level++;
        cooldown = Mathf.Max(1.5f, 4.0f - (level * 0.4f));
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            LaunchFireZone();
            timer = cooldown;
        }
    }

    private void LaunchFireZone()
    {
        Transform target = FindNearestEnemy();
        Vector3 targetPos = target != null ? target.position : transform.position + new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), 0f);

        // Spawn a Molotov bottle projectile that flies to target
        GameObject bottle = new GameObject("FireBottle");
        bottle.transform.position = transform.position;
        
        SpriteRenderer sr = bottle.AddComponent<SpriteRenderer>();
        sr.sprite = PlayerHealth.GetOrCreateRoundedRectSprite();
        sr.color = new Color(0.9f, 0.4f, 0.1f, 1f); // Dark orange bottle
        bottle.transform.localScale = new Vector3(0.2f, 0.4f, 1f);
        sr.sortingOrder = 5;

        TrailRenderer tr = bottle.AddComponent<TrailRenderer>();
        tr.time = 0.2f;
        tr.startWidth = 0.15f;
        tr.endWidth = 0f;
        tr.material = new Material(Shader.Find("Sprites/Default"));
        tr.startColor = new Color(1f, 0.6f, 0f, 0.8f);
        tr.endColor = new Color(1f, 0.2f, 0f, 0f);

        FireBottleProjectile fbp = bottle.AddComponent<FireBottleProjectile>();
        fbp.targetPosition = targetPos;
        fbp.damage = 12 + (level * 6); // Tick damage (e.g. 18, 24, 30...)
        fbp.fireRadius = 1.6f + (level * 0.3f);
        fbp.duration = 4.0f + (level * 0.5f);
    }

    private Transform FindNearestEnemy()
    {
        var enemies = EnemyController.ActiveEnemies;
        if (enemies == null || enemies.Count == 0) return null;

        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = enemy.transform;
            }
        }
        return nearest;
    }
}

public class FireBottleProjectile : MonoBehaviour
{
    public Vector3 targetPosition;
    public int damage;
    public float fireRadius;
    public float duration;
    
    private float speed = 10f;

    private void Update()
    {
        Vector3 dir = (targetPosition - transform.position);
        float distThisFrame = speed * Time.deltaTime;

        if (dir.magnitude <= distThisFrame)
        {
            transform.position = targetPosition;
            ExplodeAndIgnite();
        }
        else
        {
            transform.position += dir.normalized * distThisFrame;
            // Rotate the bottle for visual juice
            transform.Rotate(0f, 0f, 720f * Time.deltaTime);
        }
    }

    private void ExplodeAndIgnite()
    {
        // Play ignition sound
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("shield_blast");

        // Spawn the Fire Zone on the ground
        GameObject fireZone = new GameObject("FireZone");
        fireZone.transform.position = transform.position;

        FireZoneLogic fzl = fireZone.AddComponent<FireZoneLogic>();
        fzl.damage = damage;
        fzl.radius = fireRadius;
        fzl.duration = duration;

        Destroy(gameObject);
    }
}

public class FireZoneLogic : MonoBehaviour
{
    public int damage;
    public float radius;
    public float duration;

    private SpriteRenderer charredSr;
    private SpriteRenderer outerRingSr;
    private SpriteRenderer innerRingSr;

    private float tickTimer = 0f;
    private float tickInterval = 0.3f; // Tick damage every 0.3 seconds

    private static Sprite charredGroundSprite;
    private static Sprite radialGlowSprite;

    public static Sprite GetOrCreateCharredGroundSprite()
    {
        if (charredGroundSprite != null) return charredGroundSprite;

        int size = 128; // Optimized size
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        // Clear with transparent
        Color[] cols = new Color[size * size];
        for (int i = 0; i < cols.Length; i++) cols[i] = new Color(0f, 0f, 0f, 0f);
        tex.SetPixels(cols);

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxRadius = size / 2f - 6f;

        // 1. Draw charred dark-gray/black ground area
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist < maxRadius)
                {
                    float pct = dist / maxRadius;
                    float edgeNoise = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.2f;
                    if (pct + edgeNoise < 1.0f)
                    {
                        float alpha = (1f - (pct + edgeNoise)) * 0.7f;
                        tex.SetPixel(x, y, new Color(0.08f, 0.06f, 0.06f, alpha));
                    }
                }
            }
        }

        // 2. Draw glowing fire cracks radiating from the center
        int numCracks = 6;
        for (int c = 0; c < numCracks; c++)
        {
            float angle = (c * (Mathf.PI * 2f) / numCracks) + Random.Range(-0.3f, 0.3f);
            Vector2 currentPoint = center;
            float length = Random.Range(maxRadius * 0.4f, maxRadius * 0.85f);
            int steps = 15;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            
            for (int s = 0; s < steps; s++)
            {
                float stepPct = (float)s / steps;
                Vector2 nextPoint = center + dir * (length * stepPct);
                Vector2 perp = new Vector2(-dir.y, dir.x);
                nextPoint += perp * (Mathf.Sin(stepPct * Mathf.PI * 3f) * Random.Range(1.5f, 3.5f));

                DrawJaggedLineOnTexture(tex, currentPoint, nextPoint, new Color(1f, 0.45f, 0f, 1f), new Color(0.85f, 0.1f, 0f, 0.8f), 2.5f * (1f - stepPct));
                currentPoint = nextPoint;
            }
        }

        tex.Apply();
        charredGroundSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return charredGroundSprite;
    }

    private static void DrawJaggedLineOnTexture(Texture2D tex, Vector2 p1, Vector2 p2, Color innerColor, Color outerColor, float width)
    {
        int x0 = Mathf.RoundToInt(p1.x);
        int y0 = Mathf.RoundToInt(p1.y);
        int x1 = Mathf.RoundToInt(p2.x);
        int y1 = Mathf.RoundToInt(p2.y);

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            int r = Mathf.CeilToInt(width);
            for (int wy = -r; wy <= r; wy++)
            {
                for (int wx = -r; wx <= r; wx++)
                {
                    int px = x0 + wx;
                    int py = y0 + wy;
                    if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                    {
                        float d = Vector2.Distance(new Vector2(px, py), new Vector2(x0, y0));
                        if (d <= width)
                        {
                            float pct = d / width;
                            Color c = Color.Lerp(innerColor, outerColor, pct);
                            Color existing = tex.GetPixel(px, py);
                            Color blended = Color.Lerp(existing, c, c.a * (1f - pct));
                            blended.a = Mathf.Max(existing.a, c.a * (1f - pct));
                            tex.SetPixel(px, py, blended);
                        }
                    }
                }
            }

            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    public static Sprite GetOrCreateRadialGlowSprite()
    {
        if (radialGlowSprite != null) return radialGlowSprite;
        
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxRadius = size / 2f;
        
        Color[] cols = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float pct = dist / maxRadius;
                if (pct < 1.0f)
                {
                    float alpha = (1f - pct) * (1f - pct);
                    cols[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    cols[y * size + x] = new Color(1f, 1f, 1f, 0f);
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        
        radialGlowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return radialGlowSprite;
    }

    private void Start()
    {
        // 1. Create Charred Ground Base Layer
        GameObject charredGo = new GameObject("CharredGround");
        charredGo.transform.SetParent(transform, false);
        charredSr = charredGo.AddComponent<SpriteRenderer>();
        charredSr.sprite = GetOrCreateCharredGroundSprite();
        charredSr.sortingOrder = 1;

        // 2. Create Outer Glowing Flame Ring
        GameObject outerGo = new GameObject("OuterGlowRing");
        outerGo.transform.SetParent(transform, false);
        outerRingSr = outerGo.AddComponent<SpriteRenderer>();
        outerRingSr.sprite = GetOrCreateRadialGlowSprite();
        outerRingSr.color = new Color(1f, 0.25f, 0f, 0.35f); // Soft orange-red
        outerRingSr.sortingOrder = 2;

        // 3. Create Inner Glowing Flame Ring
        GameObject innerGo = new GameObject("InnerGlowRing");
        innerGo.transform.SetParent(transform, false);
        innerRingSr = innerGo.AddComponent<SpriteRenderer>();
        innerRingSr.sprite = GetOrCreateRadialGlowSprite();
        innerRingSr.color = new Color(1.0f, 0.75f, 0.1f, 0.45f); // Bright golden-yellow
        innerRingSr.sortingOrder = 3;

        // Start scale from 0 and expand to target radius
        transform.localScale = Vector3.zero;
        StartCoroutine(AnimateFireZone());

        // Spawn visual fire particles over time
        StartCoroutine(SpawnFireParticlesRoutine());
    }

    private IEnumerator AnimateFireZone()
    {
        float elapsed = 0f;
        float expandDuration = 0.5f;
        Vector3 targetScale = new Vector3(radius * 2f, radius * 2f, 1f);

        // Expand
        while (elapsed < expandDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, elapsed / expandDuration);
            yield return null;
        }
        transform.localScale = targetScale;

        // Stay and pulse/rotate slightly
        float remainTime = duration - expandDuration - 0.6f;
        elapsed = 0f;
        while (elapsed < remainTime)
        {
            elapsed += Time.deltaTime;
            
            // Outer ring pulses slowly
            float outerPulse = 1f + Mathf.Sin(elapsed * 4f) * 0.06f;
            if (outerRingSr != null)
            {
                outerRingSr.transform.localScale = new Vector3(outerPulse, outerPulse, 1f);
            }

            // Inner ring pulses faster with phase shift
            float innerPulse = 0.7f + Mathf.Sin(elapsed * 7f + 1f) * 0.08f;
            if (innerRingSr != null)
            {
                innerRingSr.transform.localScale = new Vector3(innerPulse, innerPulse, 1f);
            }

            // Rotate charred ground slightly for kinetic/burning feel
            if (charredSr != null)
            {
                charredSr.transform.Rotate(0f, 0f, 5f * Time.deltaTime);
            }

            yield return null;
        }

        // Fade out
        elapsed = 0f;
        float fadeDuration = 0.6f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float pct = elapsed / fadeDuration;

            if (charredSr != null)
            {
                charredSr.color = new Color(charredSr.color.r, charredSr.color.g, charredSr.color.b, 0.7f * (1f - pct));
            }
            if (outerRingSr != null)
            {
                outerRingSr.color = new Color(outerRingSr.color.r, outerRingSr.color.g, outerRingSr.color.b, 0.35f * (1f - pct));
            }
            if (innerRingSr != null)
            {
                innerRingSr.color = new Color(innerRingSr.color.r, innerRingSr.color.g, innerRingSr.color.b, 0.45f * (1f - pct));
            }

            transform.localScale = Vector3.Lerp(targetScale, Vector3.zero, pct);
            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator SpawnFireParticlesRoutine()
    {
        float elapsed = 0f;
        while (elapsed < duration - 0.6f)
        {
            int particlesToSpawn = Random.Range(1, 3);
            for (int i = 0; i < particlesToSpawn; i++)
            {
                Vector3 offset = Random.insideUnitCircle * (radius * 0.9f);
                GameObject flame = new GameObject("FlameParticle");
                flame.transform.position = transform.position + offset;
                
                SpriteRenderer flameSr = flame.AddComponent<SpriteRenderer>();
                flameSr.sprite = GetOrCreateRadialGlowSprite();
                
                // Random flame colors
                flameSr.color = Color.Lerp(new Color(1f, 0.15f, 0f, 0.8f), new Color(1f, 0.85f, 0.1f, 0.85f), Random.value);
                flame.transform.localScale = new Vector3(Random.Range(0.2f, 0.45f), Random.Range(0.2f, 0.45f), 1f);
                flameSr.sortingOrder = 4;

                StartCoroutine(AnimateFlameParticle(flame, flameSr));
            }

            float waitTime = Random.Range(0.08f, 0.15f);
            elapsed += waitTime;
            yield return new WaitForSeconds(waitTime);
        }
    }

    private IEnumerator AnimateFlameParticle(GameObject flame, SpriteRenderer flameSr)
    {
        float elapsed = 0f;
        float pDuration = Random.Range(0.35f, 0.7f);
        Vector3 startScale = flame.transform.localScale;
        
        Vector3 direction = (flame.transform.position - transform.position).normalized;
        Vector3 moveDir = (Vector3.up * 1.2f + direction * 0.3f).normalized * Random.Range(0.8f, 1.8f);

        while (elapsed < pDuration)
        {
            if (flame == null) yield break;
            elapsed += Time.deltaTime;
            float pct = elapsed / pDuration;

            flame.transform.position += moveDir * Time.deltaTime;
            flame.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, pct);
            if (flameSr != null)
            {
                flameSr.color = new Color(flameSr.color.r, flameSr.color.g, flameSr.color.b, flameSr.color.a * (1f - pct));
            }

            yield return null;
        }

        Destroy(flame);
    }

    private void Update()
    {
        tickTimer -= Time.deltaTime;
        if (tickTimer <= 0f)
        {
            tickTimer = tickInterval;
            DealDamageInZone();
        }
    }

    private void DealDamageInZone()
    {
        var enemies = EnemyController.ActiveEnemies;
        if (enemies == null) return;

        var enemiesCopy = new List<EnemyController>(enemies);
        foreach (var enemy in enemiesCopy)
        {
            if (enemy != null && Vector3.Distance(transform.position, enemy.transform.position) <= radius)
            {
                enemy.TakeDamage(damage);
                DeathSplashEffect.Create(enemy.transform.position, new Color(1f, 0.5f, 0f, 1f), 1);
            }
        }
    }
}
