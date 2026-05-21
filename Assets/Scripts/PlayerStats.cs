using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Base Values")]
    [SerializeField] private int baseMaxHealth = 100;
    [SerializeField] private float baseMoveSpeed = 5.0f;
    [SerializeField] private float baseFireRate = 0.45f;
    [SerializeField] private float baseMagnetRadius = 2.5f;

    [Header("Upgrade Step Values")]
    [SerializeField] private int hpStep = 20;
    [SerializeField] private float speedStep = 0.5f;
    [SerializeField] private float fireRateStep = 0.05f; // reduction per level
    [SerializeField] private float magnetStep = 1.0f;

    public int MaxHP { get; private set; }
    public float MoveSpeed { get; private set; }
    public float FireRate { get; private set; }
    public float MagnetRadius { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadStats();
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
    }

    public void LoadStats()
    {
        int hpLevel = SaveSystem.GetUpgradeLevel(SaveSystem.HPKey);
        int speedLevel = SaveSystem.GetUpgradeLevel(SaveSystem.SpeedKey);
        int fireRateLevel = SaveSystem.GetUpgradeLevel(SaveSystem.FireRateKey);
        int magnetLevel = SaveSystem.GetUpgradeLevel(SaveSystem.MagnetKey);

        MaxHP = baseMaxHealth + (hpLevel * hpStep);
        MoveSpeed = baseMoveSpeed + (speedLevel * speedStep);
        
        // Ensure fire rate doesn't drop below 0.2 seconds
        FireRate = Mathf.Max(0.2f, baseFireRate - (fireRateLevel * fireRateStep));
        
        MagnetRadius = baseMagnetRadius + (magnetLevel * magnetStep);
        
        Debug.Log($"Loaded Stats: HP={MaxHP} (Lvl {hpLevel}), Speed={MoveSpeed} (Lvl {speedLevel}), FireRate={FireRate:F2}s (Lvl {fireRateLevel}), Magnet={MagnetRadius} (Lvl {magnetLevel})");
    }
}
