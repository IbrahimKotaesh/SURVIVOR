using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Follow Target")]
    [SerializeField] private Transform target;
    
    [Header("Follow Settings")]
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);

    private float shakeDuration = 0f;
    private float shakeMagnitude = 0.15f;
    private Vector3 shakeOffset = Vector3.zero;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("Player");
            if (player != null) target = player.transform;
        }
    }

    public void TriggerShake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

            // Apply screen shake if active
            if (shakeDuration > 0f)
            {
                Vector2 randomOffset = Random.insideUnitCircle * shakeMagnitude;
                shakeOffset = new Vector3(randomOffset.x, randomOffset.y, 0f);
                shakeDuration -= Time.deltaTime;
            }
            else
            {
                shakeOffset = Vector3.zero;
            }

            transform.position = smoothedPosition + shakeOffset;
        }
    }
}
