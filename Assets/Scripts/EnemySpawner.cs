using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnInterval = 1.2f; // Increased spawn frequency
    [SerializeField] private float spawnRadius = 12f;

    private Transform playerTransform;
    private float spawnTimer;
    private bool bossSpawned = false;

    private void Start()
    {
        FindPlayer();
        spawnTimer = spawnInterval;
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }

        // Reset boss flag if game restarts
        if (GameManager.Instance != null && GameManager.Instance.TimeRemaining > GameManager.Instance.CurrentLevelConfig.duration - 2f)
        {
            bossSpawned = false;
        }

        // Check for Boss fight time
        if (GameManager.Instance != null && GameManager.Instance.IsBossTime)
        {
            if (!bossSpawned)
            {
                SpawnBoss();
                bossSpawned = true;
            }
            return; // Stop normal spawning
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnEnemy();
            
            // Scaled spawn rate based on level progression
            float interval = spawnInterval;
            if (GameManager.Instance != null && GameManager.Instance.CurrentLevelConfig != null)
            {
                var config = GameManager.Instance.CurrentLevelConfig;
                float progress = 1f - (GameManager.Instance.TimeRemaining / config.duration);
                progress = Mathf.Clamp01(progress);
                interval = Mathf.Lerp(config.baseSpawnInterval, config.minSpawnInterval, progress);
            }
            spawnTimer = interval;
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null) return;

        float speedMult = 1.0f;
        int baseDamage = 10;
        if (GameManager.Instance != null && GameManager.Instance.CurrentLevelConfig != null)
        {
            speedMult = GameManager.Instance.CurrentLevelConfig.enemySpeedMultiplier;
            baseDamage = GameManager.Instance.CurrentLevelConfig.enemyDamage;
        }

        // Spawn a cluster of enemies surrounding the player, driven by LevelConfig
        int minCluster = 1;
        int maxCluster = 3;
        if (GameManager.Instance != null && GameManager.Instance.CurrentLevelConfig != null)
        {
            minCluster = GameManager.Instance.CurrentLevelConfig.minClusterSize;
            maxCluster = GameManager.Instance.CurrentLevelConfig.maxClusterSize;
        }

        int count = Random.Range(minCluster, maxCluster + 1); // +1 because Random.Range exclusive max
        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 spawnOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * spawnRadius;
            Vector3 spawnPosition = playerTransform.position + spawnOffset;
            spawnPosition.z = 0f;

            GameObject enemy = ObjectPoolManager.Instance.SpawnObject(enemyPrefab, spawnPosition, Quaternion.identity);
            EnemyController enemyController = enemy.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                // 30% chance to spawn an orange heavy enemy (3 HP), 70% chance to spawn a purple enemy (1 HP)
                bool isOrange = Random.value < 0.30f;
                int hp = isOrange ? 3 : 1;
                enemyController.SetupEnemy(speedMult, hp, false, baseDamage, isOrange);
            }
        }
    }

    private void SpawnBoss()
    {
        if (enemyPrefab == null || playerTransform == null) return;

        // Clear existing normal enemies for clean boss duel
        for (int i = EnemyController.ActiveEnemies.Count - 1; i >= 0; i--)
        {
            var enemy = EnemyController.ActiveEnemies[i];
            if (enemy != null) ObjectPoolManager.Instance.ReturnObjectToPool(enemy.gameObject);
        }

        var config = GameManager.Instance.CurrentLevelConfig;
        int count = config != null ? config.bossCount : 1;

        for (int i = 0; i < count; i++)
        {
            // Spawn bosses distributed in a circle if there are multiple
            float angleOffset = (Mathf.PI * 2f / count) * i;
            float angle = Random.Range(0f, Mathf.PI * 0.5f) + angleOffset;
            Vector3 spawnOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * spawnRadius;
            Vector3 spawnPosition = playerTransform.position + spawnOffset;
            spawnPosition.z = 0f;

            GameObject bossGo = ObjectPoolManager.Instance.SpawnObject(enemyPrefab, spawnPosition, Quaternion.identity);
            EnemyController bossController = bossGo.GetComponent<EnemyController>();
            if (bossController != null)
            {
                bossController.SetupEnemy(config.enemySpeedMultiplier, config.bossHp, true, config.enemyDamage);
                bossGo.name = "BOSS_" + i;
            }
        }
    }
}
