using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifeTime = 3f;

    private Vector3 moveDirection;

    public void Setup(Vector3 targetPosition)
    {
        moveDirection = (targetPosition - transform.position).normalized;
        
        // Rotate projectile to face direction of travel
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if hit enemy
        if (collision.GetComponent<EnemyController>() != null)
        {
            // Spawn gem drop at enemy position
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SpawnGem(collision.transform.position);
            }

            // Destroy the enemy and the projectile
            Destroy(collision.gameObject);
            Destroy(gameObject);
        }
    }
}
