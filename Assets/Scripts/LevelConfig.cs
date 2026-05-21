using UnityEngine;

[System.Serializable]
public class LevelConfig
{
    public int stageIndex;
    public string stageName;
    public float duration; // survival duration in seconds
    public float baseSpawnInterval;
    public float minSpawnInterval;
    public float enemySpeedMultiplier;
    public int bossHp;
    public float bossSpeed;
    public int minClusterSize;
    public int maxClusterSize;
    public int enemyDamage;

    public LevelConfig(int index, string name, float dur, float spawn, float minSpawn, float speedMult, int bossHP, float bossSpd, int minCluster = 1, int maxCluster = 3, int damage = 10)
    {
        stageIndex = index;
        stageName = name;
        duration = dur;
        baseSpawnInterval = spawn;
        minSpawnInterval = minSpawn;
        enemySpeedMultiplier = speedMult;
        bossHp = bossHP;
        bossSpeed = bossSpd;
        minClusterSize = minCluster;
        maxClusterSize = maxCluster;
        enemyDamage = damage;
    }

    public static LevelConfig GetConfig(int stageIndex)
    {
        switch (stageIndex)
        {
            case 1:
                // Level 1: Fields of Hope (20s, calm spawns 1.8s -> 1.0s, cluster size 1-2, speed x0.9, Boss HP: 8, Boss Speed: 1.6f, Damage: 25, Double Gem Drops handled in GameManager)
                return new LevelConfig(1, "FIELDS OF HOPE", 20f, 1.8f, 1.0f, 0.9f, 8, 1.6f, 1, 2, 25);
            case 2:
                // Level 2: Desert Wasteland (30s, medium spawns 1.2s -> 0.5s, cluster size 1-3, speed x1.15, Boss HP: 15, Boss Speed: 2.2f, Damage: 34)
                return new LevelConfig(2, "DESERT WASTELAND", 30f, 1.2f, 0.5f, 1.15f, 15, 2.2f, 1, 3, 34);
            case 3:
                // Level 3: Necropolis Dungeon (50s, intense spawns 0.8s -> 0.3s, cluster size 2-4, speed x1.35, Boss HP: 25, Boss Speed: 2.8f, Damage: 50)
                return new LevelConfig(3, "NECROPOLIS DUNGEON", 50f, 0.8f, 0.3f, 1.35f, 25, 2.8f, 2, 4, 50);
            default:
                return new LevelConfig(1, "FIELDS OF HOPE", 20f, 1.8f, 1.0f, 0.9f, 8, 1.6f, 1, 2, 25);
        }
    }
}
