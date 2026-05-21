using UnityEngine;

public class CollectibleGem : MonoBehaviour
{
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
        
        Destroy(gameObject);
    }
}
