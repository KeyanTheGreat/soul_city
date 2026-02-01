using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class Draggable3D : MonoBehaviour
{
    [Header("Drag Settings")]
    public float liftHeight = 1f;
    public float followSpeed = 15f;
    public float momentumDecay = 8f;
    public float dropSpeed = 8f;

    private static Draggable3D active;   // single drag owner

    private Camera cam;
    private Plane dragPlane;

    private bool isDragging;
    private Vector3 velocity;
    private Vector3 targetPosition;
    private float initY;

    void Awake()
    {
        cam = Camera.main;
        initY = transform.position.y;
    }

    void Update()
    {
        if (cam == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);

        // ---------------- PICK UP ----------------
        if (Mouse.current.leftButton.wasPressedThisFrame && active == null)
        {
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider == GetComponent<Collider>())
                {
                    BeginDrag();
                }
            }
        }

        // ---------------- DRAG ----------------
        if (isDragging && Mouse.current.leftButton.isPressed)
        {
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                float targetY = initY + liftHeight;

                targetPosition = new Vector3(hitPoint.x, targetY, hitPoint.z);

                Vector3 newPos = Vector3.Lerp(
                    transform.position,
                    targetPosition,
                    Time.deltaTime * followSpeed
                );

                velocity = (newPos - transform.position) / Time.deltaTime;
                velocity.y = 0f;

                transform.position = newPos;
            }
        }

        // ---------------- RELEASE ----------------
        if (isDragging && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
            active = null;
        }

        // ---------------- DROP + MOMENTUM ----------------
        if (!isDragging && velocity.sqrMagnitude > 0.0001f)
        {
            Vector3 pos = transform.position;
            pos += velocity * Time.deltaTime;
            pos.y = Mathf.Lerp(pos.y, initY, Time.deltaTime * dropSpeed);

            transform.position = pos;

            velocity = Vector3.Lerp(velocity, Vector3.zero, momentumDecay * Time.deltaTime);

            if (velocity.magnitude < 0.05f && Mathf.Abs(pos.y - initY) < 0.01f)
            {
                transform.position = new Vector3(pos.x, initY, pos.z);
                velocity = Vector3.zero;
            }
        }
    }

    private void BeginDrag()
    {
        active = this;
        isDragging = true;
        velocity = Vector3.zero;

        float planeY = initY + liftHeight;
        dragPlane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
    }
}
