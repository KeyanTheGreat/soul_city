using UnityEngine;
using UnityEngine.InputSystem;

public class Draggable3D : MonoBehaviour
{
    [Header("Drag Settings")]
    public float liftHeight = 1f;
    public float followSpeed = 10f;
    public float momentumDecay = 10f;
    public float dropSpeed = 5f;
    public float dragPlaneOffset = 0f;

    [Header("Camera Zoom")]
    public Transform cameraTransform;
    public float zoomDistance = 2f;
    public float zoomSpeed = 5f;

    private Camera cam;

    private Plane dragPlane;
    private Vector3 velocity;
    private bool isDragging = false;
    private Vector3 targetPosition;
    private float initY;

    void Awake()
    {
        cam = Camera.main;
        initY = transform.position.y; // stash initial Y position
    }

    void Update()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);

        // --- Hover detection + outline ---
        HandleHover(ray);

        // --- Pick up ---
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Draggable3D newDragged = hit.collider.GetComponent<Draggable3D>();
                if (newDragged != null && !newDragged.isDragging)
                {
                    newDragged.StartDrag();
                }
            }
        }

        // --- Dragging ---
        if (isDragging && Mouse.current.leftButton.isPressed)
        {
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                targetPosition = new Vector3(hitPoint.x, initY + liftHeight, hitPoint.z);
                Vector3 newPosition = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);

                velocity = new Vector3(
                    (newPosition.x - transform.position.x) / Time.deltaTime,
                    0f,
                    (newPosition.z - transform.position.z) / Time.deltaTime
                );

                transform.position = newPosition;
            }
        }

        // --- Release ---
        if (isDragging && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }

        // --- Momentum + smooth drop ---
        if (!isDragging && velocity.magnitude > 0f)
        {
            Vector3 pos = transform.position;
            pos += velocity * Time.deltaTime;
            pos.y = Mathf.Lerp(pos.y, initY, Time.deltaTime * dropSpeed);
            transform.position = pos;

            velocity = Vector3.Lerp(velocity, Vector3.zero, momentumDecay * Time.deltaTime);
        }
    }

    private void StartDrag()
    {
        isDragging = true;
        dragPlane = new Plane(Vector3.up, transform.position + Vector3.up * dragPlaneOffset);
        targetPosition = transform.position + Vector3.up * liftHeight;
        velocity = Vector3.zero;
    }

    private void HandleHover(Ray ray)
    {
        Draggable3D hovered = null;

        if (!isDragging && Physics.Raycast(ray, out RaycastHit hit))
        {
            hovered = hit.collider.GetComponent<Draggable3D>();
        }

        Outline outline = GetComponent<Outline>();
        if (outline != null)
            outline.enabled = (hovered == this);
    }
}
