using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void AddWeapon(string weaponId)
    {
        switch (weaponId)
        {
            case "Rocket":
                var rocket = GetComponent<WeaponRocket>();
                if (rocket == null) gameObject.AddComponent<WeaponRocket>();
                else rocket.LevelUp();
                break;
            case "Bomb":
                var fireZone = GetComponent<WeaponFireZone>();
                if (fireZone == null) gameObject.AddComponent<WeaponFireZone>();
                else fireZone.LevelUp();
                break;
            case "Aura":
                var aura = GetComponent<WeaponAura>();
                if (aura == null) gameObject.AddComponent<WeaponAura>();
                else aura.LevelUp();
                break;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("upgrade");
        }
    }
}
