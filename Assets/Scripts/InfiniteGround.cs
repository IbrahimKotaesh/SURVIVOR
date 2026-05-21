using UnityEngine;

public class InfiniteGround : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float tileSizeX = 20.48f; // Matches texture width in world units (1024 / 50 PPU)
    [SerializeField] private float tileSizeY = 20.48f; // Matches texture height in world units (1024 / 50 PPU)

    private void Start()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = Resources.Load<Sprite>("city_street_background");
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(100f, 100f);
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform != null)
        {
            // Calculate nearest snap position based on tile sizes
            float snapX = Mathf.Round(cameraTransform.position.x / tileSizeX) * tileSizeX;
            float snapY = Mathf.Round(cameraTransform.position.y / tileSizeY) * tileSizeY;
            
            // Move ground to snapped position, keeping Z behind player (Z = 1)
            transform.position = new Vector3(snapX, snapY, 1f);
        }
    }
}
