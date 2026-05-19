using UnityEngine;

public class InfiniteGround : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float tileSizeX = 10.24f; // Matches texture width in world units
    [SerializeField] private float tileSizeY = 5.58f;  // Matches texture height in world units

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
            // Calculate nearest snap position based on tile sizes
            float snapX = Mathf.Round(cameraTransform.position.x / tileSizeX) * tileSizeX;
            float snapY = Mathf.Round(cameraTransform.position.y / tileSizeY) * tileSizeY;
            
            // Move ground to snapped position, keeping Z behind player (Z = 1)
            transform.position = new Vector3(snapX, snapY, 1f);
        }
    }
}
