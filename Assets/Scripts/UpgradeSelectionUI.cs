using UnityEngine;
using UnityEngine.UI;

public class UpgradeSelectionUI : MonoBehaviour
{
    private GameObject panel;

    public void Show()
    {
        Canvas canvas = GameManager.Instance.GetMainCanvas();
        if (canvas == null) return;

        // Overlay
        panel = new GameObject("UpgradeSelectionUI");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform bgRect = panel.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        Image bgImage = panel.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.85f); // Dark background to focus on cards

        // Title
        GameManager.CreateText(panel, "Title", "CHOOSE YOUR WEAPON", 46, new Vector2(0f, 250f), Color.yellow, true);

        // Container Parent
        GameObject containerParent = new GameObject("ContainerParent");
        containerParent.transform.SetParent(panel.transform, false);
        RectTransform cpRect = containerParent.AddComponent<RectTransform>();
        cpRect.anchoredPosition = new Vector2(0f, -20f);
        cpRect.sizeDelta = new Vector2(800f, 400f);

        // Create 3 Cards with explicit positioning
        CreateWeaponCard(containerParent, "HOMING ROCKET", "Fires a deadly rocket that seeks enemies.", "Rocket", new Color(1f, 0.4f, 0f, 1f), new Vector2(-280f, 0f));
        CreateWeaponCard(containerParent, "FLAME ZONE", "Throws a fire potion that ignites the ground, burning enemies.", "Bomb", new Color(1f, 0.35f, 0.05f, 1f), new Vector2(0f, 0f));
        CreateWeaponCard(containerParent, "ORBITING AURA", "Spins deadly shields around you continuously.", "Aura", new Color(0.2f, 0.9f, 0.4f, 1f), new Vector2(280f, 0f));
    }

    private void CreateWeaponCard(GameObject parent, string nameStr, string descStr, string weaponId, Color themeColor, Vector2 position)
    {
        GameObject card = new GameObject("WeaponCard_" + weaponId);
        card.transform.SetParent(parent.transform, false);
        
        RectTransform cardRect = card.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(240f, 320f);
        cardRect.anchoredPosition = position;

        Image cardImg = card.AddComponent<Image>();
        cardImg.sprite = GameSpriteManager.GetSprite("panel_blue_top");
        cardImg.type = Image.Type.Sliced;
        cardImg.color = Color.white;

        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = themeColor;
        outline.effectDistance = new Vector2(4f, -4f);

        // Icon Graphic
        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(card.transform, false);
        RectTransform iconRect = icon.AddComponent<RectTransform>();
        iconRect.anchoredPosition = new Vector2(0f, 70f);
        iconRect.sizeDelta = new Vector2(80f, 80f);
        Image iconImg = icon.AddComponent<Image>();

        Sprite iconSprite = null;
        if (weaponId == "Rocket")
        {
            iconSprite = GameManager.LoadSpriteFromResources("roket");
        }
        else if (weaponId == "Bomb")
        {
            iconSprite = GameManager.LoadSpriteFromResources("fire");
        }
        else if (weaponId == "Aura")
        {
            iconSprite = GameManager.LoadSpriteFromResources("shield");
        }

        if (iconSprite != null)
        {
            iconImg.sprite = iconSprite;
            iconImg.color = Color.white;
        }
        else
        {
            iconImg.sprite = PlayerHealth.GetOrCreateRoundedRectSprite(); // Nice rounded rect for icon
            iconImg.color = themeColor;
        }

        // Name
        GameManager.CreateText(card, "Name", nameStr, 22, new Vector2(0f, 0f), themeColor, true);

        // Description
        GameManager.CreateText(card, "Desc", descStr, 15, new Vector2(0f, -50f), Color.white);

        // Make the ENTIRE CARD a Button
        Button btn = card.AddComponent<Button>();
        btn.targetGraphic = cardImg;
        btn.onClick.AddListener(() => OnWeaponSelected(weaponId));
        
        // Add a pulsing visual effect or highlight to make it obvious it's a button
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        btn.colors = cb;
    }

    private void OnWeaponSelected(string weaponId)
    {
        if (WeaponManager.Instance != null)
        {
            WeaponManager.Instance.AddWeapon(weaponId);
        }
        else
        {
            // If instance doesn't exist, try to find player and add it
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("Player");
            if (player != null)
            {
                WeaponManager wm = player.GetComponent<WeaponManager>();
                if (wm == null) wm = player.AddComponent<WeaponManager>();
                wm.AddWeapon(weaponId);
            }
        }

        Destroy(panel);
        
        // Resume game and seamlessly load next stage logic
        Time.timeScale = 1f;
        GameManager.Instance.StartNextStageSeamlessly();
    }
}
