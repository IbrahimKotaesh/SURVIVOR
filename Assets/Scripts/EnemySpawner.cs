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

            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            EnemyController enemyController = enemy.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                enemyController.SetupEnemy(speedMult, 1, false, baseDamage);
            }
        }
    }

    private void SpawnBoss()
    {
        if (enemyPrefab == null || playerTransform == null) return;

        // Clear existing normal enemies for clean boss duel
        var activeEnemies = GameObject.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }

        // Spawn boss at spawn radius
        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 spawnOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * spawnRadius;
        Vector3 spawnPosition = playerTransform.position + spawnOffset;
        spawnPosition.z = 0f;

        GameObject bossGo = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        EnemyController bossController = bossGo.GetComponent<EnemyController>();
        if (bossController != null)
        {
            var config = GameManager.Instance.CurrentLevelConfig;
            bossController.SetupEnemy(config.enemySpeedMultiplier, config.bossHp, true, config.enemyDamage);
            bossGo.name = "BOSS";
        }
    }
}
