using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifeTime = 3f;

    private Vector3 moveDirection;
    private int damage = 1;
    private bool isPiercing = false;
    private float curveIntensity = 0f;

    public void Setup(Vector3 targetPosition)
    {
        moveDirection = (targetPosition - transform.position).normalized;
        
        // Rotate projectile to face direction of travel
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        // Start lifetime coroutine
        StartCoroutine(LifeTimeRoutine());
    }

    public void SetupCustom(Vector3 direction, float customSpeed, int customDamage, bool piercing, float curve = 0f)
    {
        moveDirection = direction.normalized;
        speed = customSpeed;
        damage = customDamage;
        isPiercing = piercing;
        curveIntensity = curve;

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Start lifetime coroutine
        StartCoroutine(LifeTimeRoutine());
    }

    private System.Collections.IEnumerator LifeTimeRoutine()
    {
        yield return new WaitForSeconds(lifeTime);
        ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
    }

    private void Update()
    {
        if (curveIntensity != 0f)
        {
            // Rotate the move direction over time to create a curve path
            float angleOffset = curveIntensity * Time.deltaTime;
            float cos = Mathf.Cos(angleOffset);
            float sin = Mathf.Sin(angleOffset);
            float rx = moveDirection.x * cos - moveDirection.y * sin;
            float ry = moveDirection.x * sin + moveDirection.y * cos;
            moveDirection = new Vector3(rx, ry, 0f).normalized;

            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if hit enemy
        EnemyController enemy = collision.GetComponent<EnemyController>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            if (!isPiercing)
            {
                ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
            }
        }
    }
}
