using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnInterval = 1.2f; // Increased spawn frequency
    [SerializeField] private float spawnRadius = 12f;

    private Transform playerTransform;
    private float spawnTimer;

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
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnEnemy();
            spawnTimer = spawnInterval;
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null) return;

        // Spawn a cluster of 1 to 3 enemies surrounding the player
        int count = Random.Range(1, 4);
        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 spawnOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * spawnRadius;
            Vector3 spawnPosition = playerTransform.position + spawnOffset;
            spawnPosition.z = 0f;

            Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        }
    }
}
