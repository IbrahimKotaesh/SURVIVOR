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
    public int bossCount;
    public Color bossEnvironmentColor;

    public LevelConfig(int index, string name, float dur, float spawn, float minSpawn, float speedMult, int bossHP, float bossSpd, int minCluster, int maxCluster, int damage, int bCount, Color bColor)
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
        bossCount = bCount;
        bossEnvironmentColor = bColor;
    }

    public static LevelConfig GetConfig(int stageIndex)
    {
        switch (stageIndex)
        {
            case 1:
                // Level 1: Fields of Hope (20s, calm spawns, 1 Boss, Dark Night tint, Boss HP = 50)
                return new LevelConfig(1, "FIELDS OF HOPE", 20f, 1.8f, 1.0f, 0.9f, 50, 1.6f, 1, 2, 25, 1, new Color(0.02f, 0.05f, 0.2f, 0.8f));
            case 2:
                // Level 2: Desert Wasteland (30s, medium spawns, 1 Angry Boss, Dark Red Rage tint, Boss HP = 800)
                return new LevelConfig(2, "DESERT WASTELAND", 30f, 1.2f, 0.5f, 1.15f, 800, 2.2f, 1, 3, 34, 1, new Color(0.4f, 0.0f, 0.0f, 0.75f));
            case 3:
                // Level 3: Necropolis Dungeon (40s, hard spawns, 3 Bosses, Deep Red Rage tint, Boss HP = 1200)
                return new LevelConfig(3, "NECROPOLIS DUNGEON", 40f, 1.1f, 0.45f, 1.18f, 1200, 2.3f, 1, 3, 25, 3, new Color(0.5f, 0.0f, 0.0f, 0.85f));
            case 4:
                // Level 4: Crystal Cave (50s, rapid spawns, 2 Crystal Bosses, Dark Violet tint, Boss HP = 2000)
                return new LevelConfig(4, "CRYSTAL CAVE", 50f, 1.0f, 0.4f, 1.25f, 2000, 2.5f, 1, 3, 30, 2, new Color(0.3f, 0.0f, 0.4f, 0.8f));
            case 5:
                // Level 5: The Void Rift (60s, chaotic spawns, 1 Giant Final Boss, Void Black tint, Boss HP = 4000)
                return new LevelConfig(5, "THE VOID RIFT", 60f, 0.9f, 0.35f, 1.3f, 4000, 2.7f, 1, 4, 35, 1, new Color(0.1f, 0.1f, 0.1f, 0.9f));
            default:
                return new LevelConfig(1, "FIELDS OF HOPE", 20f, 1.8f, 1.0f, 0.9f, 50, 1.6f, 1, 2, 25, 1, new Color(0.02f, 0.05f, 0.2f, 0.8f));
        }
    }
}
