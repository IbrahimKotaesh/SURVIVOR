using UnityEngine;

public class CollectibleGem : MonoBehaviour
{
    [Header("Magnet Settings")]
    [SerializeField] private float magnetRadius = 2.5f;
    [SerializeField] private float magnetSpeed = 6.0f;

    [Header("Bobbing Settings")]
    [SerializeField] private float bobSpeed = 2.0f;
    [SerializeField] private float bobAmount = 0.1f;

    private Transform playerTransform;
    private Vector3 basePosition;
    private bool isBeingPulled = false;

    private void Start()
    {
        basePosition = transform.position;
        FindPlayer();
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Update()
    {
        ApplyBobbing();
    }

    private void ApplyBobbing()
    {
        // Simple vertical bobbing offset from base position
        float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.position = basePosition + new Vector3(0f, bobOffset, 0f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.GetComponent<PlayerController>() != null)
        {
            Collect();
        }
    }

    private void Collect()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(1);
        }
        
        // Spawn a tiny flash or effect here if desired, then destroy
        Destroy(gameObject);
    }
}
