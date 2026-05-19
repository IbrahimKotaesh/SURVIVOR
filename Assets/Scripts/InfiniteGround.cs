using UnityEngine;

public class InfiniteGround : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float tileSize = 5.12f; // Matches texture size in world units

    private void Start()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform != null)
        {
            // Calculate nearest snap position based on tileSize
            float snapX = Mathf.Round(cameraTransform.position.x / tileSize) * tileSize;
            float snapY = Mathf.Round(cameraTransform.position.y / tileSize) * tileSize;
            
            // Move ground to snapped position, keeping Z behind player (Z = 1)
            transform.position = new Vector3(snapX, snapY, 1f);
        }
    }
}
