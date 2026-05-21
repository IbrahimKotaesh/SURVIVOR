using UnityEngine;
using TMPro;

public class DeathSplashEffect : MonoBehaviour
{
    private static Sprite circleSprite;

    public static Sprite GetOrCreateCircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        
        Texture2D texture = new Texture2D(16, 16);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float dx = x - 7.5f;
                float dy = y - 7.5f;
                if (dx * dx + dy * dy <= 7f * 7f)
                {
                    texture.SetPixel(x, y, Color.white);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        texture.Apply();
        circleSprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        return circleSprite;
    }

    public static void Create(Vector3 position, Color enemyColor, int particleCount = 12)
    {
        GameObject effectContainer = new GameObject("DeathSplashEffect");
        effectContainer.transform.position = position;
        
        // Auto destroy the container after 1.2 seconds
        Destroy(effectContainer, 1.2f);

        Sprite sprite = GetOrCreateCircleSprite();

        // Determine particle color (default to enemy tint, or red/purple splash)
        Color splashColor = enemyColor;
        if (splashColor == Color.white)
        {
            // Default to blood red splash
            splashColor = new Color(0.85f, 0.1f, 0.1f, 1f); 
        }

        for (int i = 0; i < particleCount; i++)
        {
            GameObject particle = new GameObject("SplashParticle");
            particle.transform.SetParent(effectContainer.transform);
            particle.transform.localPosition = Vector3.zero;

            // Random initial scale
            float scale = Random.Range(0.15f, 0.35f);
            particle.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            
            // Give slightly randomized tint of the main color
            float colorVariation = Random.Range(-0.1f, 0.1f);
            Color finalColor = new Color(
                Mathf.Clamp01(splashColor.r + colorVariation),
                Mathf.Clamp01(splashColor.g + colorVariation),
                Mathf.Clamp01(splashColor.b + colorVariation),
                1f
            );
            sr.color = finalColor;
            sr.sortingOrder = 5; // Render in front of ground

            // Add the particle behavior script
            SplashParticleBehavior behavior = particle.AddComponent<SplashParticleBehavior>();
            behavior.Initialize();
        }
    }

    public static void CreateFloatingText(Vector3 position, string text, Color color)
    {
        GameObject textObj = new GameObject("FloatingText");
        textObj.transform.position = position + new Vector3(0f, 0.5f, -1.5f);
        
        TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();
        textMesh.text = text;
        textMesh.color = color;
        textMesh.fontSize = 6f; // World-space TMP font size
        
        if (GameFontManager.RetroFont != null)
        {
            textMesh.font = GameFontManager.RetroFont;
        }
        
        textMesh.alignment = TextAlignmentOptions.Center;
        
        MeshRenderer mr = textObj.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingOrder = 10; // Render on top of particles and gems
        }
        
        // Add a floating/rising/fading behavior script
        FloatingTextBehavior behavior = textObj.AddComponent<FloatingTextBehavior>();
        behavior.Initialize();
    }
}

public class SplashParticleBehavior : MonoBehaviour
{
    private Vector3 velocity;
    private float gravity = -12f;
    private float lifeTime = 0.8f;
    private float elapsed = 0f;
    private SpriteRenderer sr;
    private Vector3 initialScale;

    public void Initialize()
    {
        sr = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale;

        // Velocity: throw upward and outward in random directions
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float speed = Random.Range(2f, 6f);
        velocity = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed + Random.Range(1.5f, 3.5f), 0f);
        
        // Randomize lifetime slightly
        lifeTime = Random.Range(0.4f, 0.7f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        // Apply gravity to Y velocity
        velocity.y += gravity * Time.deltaTime;

        // Apply drag (air resistance)
        velocity *= (1f - 2.5f * Time.deltaTime);

        // Update position
        transform.localPosition += velocity * Time.deltaTime;

        // Fade out and shrink
        float ratio = elapsed / lifeTime;
        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Clamp01(1f - ratio);
            sr.color = c;
        }

        transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, ratio);
    }
}

public class FloatingTextBehavior : MonoBehaviour
{
    private float lifeTime = 0.8f;
    private float elapsed = 0f;
    private Vector3 velocity = new Vector3(0f, 1.6f, 0f); // Rise upwards
    private TextMeshPro textMesh;

    public void Initialize()
    {
        textMesh = GetComponent<TextMeshPro>();
        // Add slight random horizontal drift
        velocity.x = Random.Range(-0.4f, 0.4f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += velocity * Time.deltaTime;
        
        // Slowly drift/decelerate
        velocity *= (1f - 1.5f * Time.deltaTime);

        // Fade out
        float ratio = elapsed / lifeTime;
        if (textMesh != null)
        {
            Color c = textMesh.color;
            c.a = Mathf.Clamp01(1f - ratio);
            textMesh.color = c;
        }

        // Pop size at start, then shrink
        if (ratio < 0.2f)
        {
            float popRatio = ratio / 0.2f;
            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, popRatio);
        }
        else
        {
            float shrinkRatio = (ratio - 0.2f) / 0.8f;
            transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.5f, 0.5f, 0.5f), shrinkRatio);
        }
    }
}
