using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float attackRange = 6f;
    [SerializeField] private float fireRate = 0.45f; // Fast fire rate to keep enemies away

    private float fireTimer;

    private void Start()
    {
        // Load upgraded fireRate from PlayerStats
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats == null)
        {
            stats = gameObject.AddComponent<PlayerStats>();
        }
        if (stats != null)
        {
            fireRate = stats.FireRate;
        }
        fireTimer = fireRate;
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            Transform target = FindNearestEnemy();
            if (target != null)
            {
                Shoot(target);
                fireTimer = fireRate;
            }
        }
    }

    private Transform FindNearestEnemy()
    {
        // Try finding by EnemyController instances
        var clones = GameObject.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (clones == null || clones.Length == 0) return null;

        Transform nearest = null;
        float minDistance = attackRange;

        foreach (var enemy in clones)
        {
            if (enemy == null) continue;
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = enemy.transform;
            }
        }

        return nearest;
    }

    private void Shoot(Transform target)
    {
        if (projectilePrefab == null) return;

        // Instantiate projectile at player's position
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        Projectile projectileScript = proj.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.Setup(target.position);
        }
    }
}
