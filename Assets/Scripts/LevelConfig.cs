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

    public LevelConfig(int index, string name, float dur, float spawn, float minSpawn, float speedMult, int bossHP, float bossSpd)
    {
        stageIndex = index;
        stageName = name;
        duration = dur;
        baseSpawnInterval = spawn;
        minSpawnInterval = minSpawn;
        enemySpeedMultiplier = speedMult;
        bossHp = bossHP;
        bossSpeed = bossSpd;
    }

    public static LevelConfig GetConfig(int stageIndex)
    {
        switch (stageIndex)
        {
            case 1:
                // Stage 1: Fields (60s, base spawn 1.2s -> min 0.6s, speed x1.0, Boss HP: 5, Boss Speed: 1.8f)
                return new LevelConfig(1, "FIELDS OF HOPE", 60f, 1.2f, 0.6f, 1.0f, 5, 1.8f);
            case 2:
                // Stage 2: Desert (90s, base spawn 0.9s -> min 0.4s, speed x1.25, Boss HP: 12, Boss Speed: 2.2f)
                return new LevelConfig(2, "DESERT WASTELAND", 90f, 0.9f, 0.4f, 1.25f, 12, 2.2f);
            case 3:
                // Stage 3: Dungeon (120s, base spawn 0.6s -> min 0.25s, speed x1.50, Boss HP: 30, Boss Speed: 2.6f)
                return new LevelConfig(3, "NECROPOLIS DUNGEON", 120f, 0.6f, 0.25f, 1.5f, 30, 2.6f);
            default:
                return new LevelConfig(1, "FIELDS OF HOPE", 60f, 1.2f, 0.6f, 1.0f, 5, 1.8f);
        }
    }
}
