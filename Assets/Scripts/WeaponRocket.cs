using UnityEngine;
using System.Collections.Generic;

public class WeaponRocket : MonoBehaviour
{
    private float cooldown = 3.0f;
    private float timer;
    private int level = 1;

    public void LevelUp()
    {
        level++;
        cooldown = Mathf.Max(0.5f, 3.0f - (level * 0.4f));
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            FireRocket();
            timer = cooldown;
        }
    }

    private void FireRocket()
    {
        Transform target = FindNearestEnemy();
        if (target == null) return;

        // Visual: Create a rocket
        GameObject rocket = new GameObject("Rocket");
        rocket.transform.position = transform.position;
        
        SpriteRenderer sr = rocket.AddComponent<SpriteRenderer>();
        sr.sprite = PlayerHealth.GetOrCreateRoundedRectSprite(); // Reusing basic shapes
        sr.color = new Color(1f, 0.4f, 0f, 1f); // Orange
        rocket.transform.localScale = new Vector3(0.4f, 0.2f, 1f);

        TrailRenderer tr = rocket.AddComponent<TrailRenderer>();
        tr.time = 0.4f;
        tr.startWidth = 0.2f;
        tr.endWidth = 0f;
        tr.material = new Material(Shader.Find("Sprites/Default"));
        tr.startColor = Color.yellow;
        tr.endColor = Color.red;
        tr.sortingOrder = 5;

        RocketProjectile rp = rocket.AddComponent<RocketProjectile>();
        rp.target = target;
        rp.damage = 50 + (level * 20); // High damage
        rp.explosionRadius = 2.0f + (level * 0.5f);
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

public class RocketProjectile : MonoBehaviour
{
    public Transform target;
    public int damage;
    public float explosionRadius;
    private float speed = 8f;
    private float lifeTime = 4f;

    private void Update()
    {
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0)
        {
            Explode();
            return;
        }

        if (target != null)
        {
            Vector3 dir = (target.position - transform.position).normalized;
            transform.position += dir * speed * Time.deltaTime;
            
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            if (Vector3.Distance(transform.position, target.position) < 0.5f)
            {
                Explode();
            }
        }
        else
        {
            transform.position += transform.right * speed * Time.deltaTime;
        }
    }

    private void Explode()
    {
        DeathSplashEffect.Create(transform.position, new Color(1f, 0.4f, 0f, 1f), 15);
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX("shield_blast");

        // Damage enemies in radius
        var enemies = EnemyController.ActiveEnemies;
        var enemiesCopy = new List<EnemyController>(enemies);
        foreach (var enemy in enemiesCopy)
        {
            if (enemy != null && Vector3.Distance(transform.position, enemy.transform.position) <= explosionRadius)
            {
                enemy.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }
}
