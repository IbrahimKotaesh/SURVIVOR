using UnityEngine;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Joystick Settings")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private float handleRange = 1f;

    private Vector2 input = Vector2.zero;
    private Canvas canvas;
    private Camera cam;

    public Vector2 Direction => input;

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            cam = canvas.worldCamera;
        }
        
        // Hide joystick initially
        if (background != null)
        {
            background.gameObject.SetActive(false);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (background != null)
        {
            background.gameObject.SetActive(true);
        }

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform, eventData.position, cam, out localPoint))
        {
            background.anchoredPosition = localPoint;
        }
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, cam, out localPoint))
        {
            Vector2 radius = background.sizeDelta / 2f;
            input = localPoint / (radius * handleRange);
            
            if (input.magnitude > 1f)
            {
                input = input.normalized;
            }
            
            handle.anchoredPosition = input * radius * handleRange;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;
        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
        if (background != null)
        {
            background.gameObject.SetActive(false);
        }
    }
}
