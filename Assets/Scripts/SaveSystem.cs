using UnityEngine;

public static class SaveSystem
{
    private const string GEMS_BANK_KEY = "GemsBank";
    private const string UPGRADE_HP_KEY = "UpgradeHP";
    private const string UPGRADE_SPEED_KEY = "UpgradeSpeed";
    private const string UPGRADE_FIRE_RATE_KEY = "UpgradeFireRate";
    private const string UPGRADE_MAGNET_KEY = "UpgradeMagnet";

    private const string HIGHEST_UNLOCKED_STAGE_KEY = "HighestUnlockedStage";

    public static int GetGemsBank()
    {
        return PlayerPrefs.GetInt(GEMS_BANK_KEY, 0);
    }

    public static void AddGems(int amount)
    {
        int current = GetGemsBank();
        PlayerPrefs.SetInt(GEMS_BANK_KEY, current + amount);
        PlayerPrefs.Save();
    }

    public static bool SpendGems(int amount)
    {
        int current = GetGemsBank();
        if (current >= amount)
        {
            PlayerPrefs.SetInt(GEMS_BANK_KEY, current - amount);
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }

    public static int GetUpgradeLevel(string statKey)
    {
        return PlayerPrefs.GetInt(statKey, 0);
    }

    public static void IncrementUpgradeLevel(string statKey)
    {
        int current = GetUpgradeLevel(statKey);
        PlayerPrefs.SetInt(statKey, current + 1);
        PlayerPrefs.Save();
    }

    public static int GetHighestUnlockedStage()
    {
        int val = PlayerPrefs.GetInt(HIGHEST_UNLOCKED_STAGE_KEY, 5);
        if (val < 5)
        {
            PlayerPrefs.SetInt(HIGHEST_UNLOCKED_STAGE_KEY, 5);
            PlayerPrefs.Save();
            val = 5;
        }
        return val;
    }

    public static void UnlockStage(int stageNumber)
    {
        int current = GetHighestUnlockedStage();
        if (stageNumber > current)
        {
            PlayerPrefs.SetInt(HIGHEST_UNLOCKED_STAGE_KEY, stageNumber);
            PlayerPrefs.Save();
            Debug.Log($"Unlocked Stage {stageNumber}!");
        }
    }

    // Keys helper
    public static string HPKey => UPGRADE_HP_KEY;
    public static string SpeedKey => UPGRADE_SPEED_KEY;
    public static string FireRateKey => UPGRADE_FIRE_RATE_KEY;
    public static string MagnetKey => UPGRADE_MAGNET_KEY;

    public static void ResetSave()
    {
        PlayerPrefs.DeleteKey(GEMS_BANK_KEY);
        PlayerPrefs.DeleteKey(UPGRADE_HP_KEY);
        PlayerPrefs.DeleteKey(UPGRADE_SPEED_KEY);
        PlayerPrefs.DeleteKey(UPGRADE_FIRE_RATE_KEY);
        PlayerPrefs.DeleteKey(UPGRADE_MAGNET_KEY);
        PlayerPrefs.DeleteKey(HIGHEST_UNLOCKED_STAGE_KEY);
        PlayerPrefs.Save();
    }
}
