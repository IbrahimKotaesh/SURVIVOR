using UnityEngine;
using System.Collections.Generic;

public class WeaponAura : MonoBehaviour
{
    private int level = 1;
    private int auraDamage = 25;
    private float orbitSpeed = 180f; 
    private float orbitRadius = 1.5f;

    private List<GameObject> auras = new List<GameObject>();
    private List<float> angles = new List<float>();
    private float hitTimer = 0f;

    private void Start()
    {
        CreateAuras();
    }

    public void LevelUp()
    {
        level++;
        auraDamage += 15;
        orbitRadius += 0.2f;
        orbitSpeed += 20f;
        CreateAuras(); 
    }

    private void CreateAuras()
    {
        foreach (var a in auras)
        {
            if (a != null) Destroy(a);
        }
        auras.Clear();
        angles.Clear();

        int count = 1 + level; // level 1 = 2 auras

        for (int i = 0; i < count; i++)
        {
            GameObject aura = new GameObject("Aura");
            aura.transform.SetParent(transform); 
            
            SpriteRenderer sr = aura.AddComponent<SpriteRenderer>();
            sr.sprite = PlayerHealth.GetOrCreateRoundedRectSprite(); 
            sr.color = new Color(0.2f, 0.9f, 0.4f, 0.8f); 
            aura.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
            sr.sortingOrder = 6;

            TrailRenderer tr = aura.AddComponent<TrailRenderer>();
            tr.time = 0.2f;
            tr.startWidth = 0.15f;
            tr.endWidth = 0f;
            tr.material = new Material(Shader.Find("Sprites/Default"));
            tr.startColor = new Color(0.2f, 0.9f, 0.4f, 0.5f);
            tr.endColor = new Color(0.2f, 0.9f, 0.4f, 0f);

            auras.Add(aura);
            angles.Add((360f / count) * i);
        }
    }

    private void Update()
    {
        if (auras.Count == 0) return;

        for (int i = 0; i < auras.Count; i++)
        {
            if (auras[i] == null) continue;
            
            angles[i] += orbitSpeed * Time.deltaTime;
            if (angles[i] >= 360f) angles[i] -= 360f;

            float rad = angles[i] * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * orbitRadius;
            auras[i].transform.position = transform.position + offset;
            auras[i].transform.rotation = Quaternion.Euler(0f, 0f, angles[i] + 90f);
        }

        hitTimer -= Time.deltaTime;
        if (hitTimer <= 0f)
        {
            hitTimer = 0.2f;
            bool hitAnything = false;

            var enemies = EnemyController.ActiveEnemies;
            if (enemies != null && enemies.Count > 0)
            {
                var enemiesCopy = new List<EnemyController>(enemies);
                foreach (var enemy in enemiesCopy)
                {
                    if (enemy == null) continue;
                    
                    // Check against all auras
                    foreach (var aura in auras)
                    {
                        if (aura != null && Vector3.Distance(aura.transform.position, enemy.transform.position) <= 0.7f)
                        {
                            enemy.TakeDamage(auraDamage);
                            DeathSplashEffect.Create(enemy.transform.position, new Color(0.2f, 0.9f, 0.4f, 1f), 3);
                            hitAnything = true;
                            break; 
                        }
                    }
                }
            }

            if (hitAnything)
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("shield_blast");
            }
        }
    }
}
