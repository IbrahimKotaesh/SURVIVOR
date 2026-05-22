using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Joystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Joystick Settings")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private float handleRange = 1f;

    [Header("Default Position Settings")]
    [SerializeField] private float xOffsetFromRight = 350f; // Offset from the right screen edge
    [SerializeField] private float yPositionFromCenter = 0f; // Y position relative to vertical center (0 is exact center)

    private Vector2 input = Vector2.zero;
    private Canvas canvas;
    private Camera cam;
    private bool isDragging = false;

    public Vector2 Direction => input;

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            cam = canvas.worldCamera;
        }
        
        // Force the joystick to be visible on startup
        if (background != null)
        {
            background.gameObject.SetActive(true);
            
            // Programmatically tint the joystick textures to white to override editor settings
            Image bgImg = background.GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.color = new Color(1f, 1f, 1f, 0.4f); // Semi-transparent white
            }
            
            if (handle != null)
            {
                Image handleImg = handle.GetComponent<Image>();
                if (handleImg != null)
                {
                    handleImg.color = new Color(1f, 1f, 1f, 0.8f); // Solid white
                }
            }
            
            ResetToDefaultPosition();
        }
    }

    private void Update()
    {
        // When not dragging, dynamically keep the joystick positioned in the bottom-right corner
        if (!isDragging)
        {
            ResetToDefaultPosition();
        }
    }

    private void ResetToDefaultPosition()
    {
        if (background != null && background.parent != null)
        {
            RectTransform parentRect = background.parent.GetComponent<RectTransform>();
            if (parentRect != null)
            {
                // Position on the right side, but vertically centered at yPositionFromCenter
                float x = (parentRect.rect.width / 2f) - xOffsetFromRight;
                float y = yPositionFromCenter;
                background.anchoredPosition = new Vector2(x, y);
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;

        if (background != null && background.parent != null)
        {
            background.gameObject.SetActive(true);

            Vector2 localPoint;
            RectTransform parentRect = background.parent.GetComponent<RectTransform>();
            if (parentRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, eventData.position, cam, out localPoint))
            {
                background.anchoredPosition = localPoint;
            }
        }
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null) return;

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
            
            if (handle != null)
            {
                handle.anchoredPosition = input * radius * handleRange;
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        input = Vector2.zero;
        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
        ResetToDefaultPosition();
    }
}
