using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class OrthoZoom_NewInput : MonoBehaviour
{
    public float zoomSpeed = 3f;
    public float minSize = 3f;
    public float maxSize = 20f;
    public float zoomLerpSpeed = 8f; // higher = snappier

    private Camera cam;
    private float targetSize;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        targetSize = cam.orthographicSize;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetSize = Mathf.Clamp(
                targetSize - scroll * zoomSpeed,
                minSize,
                maxSize
            );
        }

        cam.orthographicSize = Mathf.Lerp(
            cam.orthographicSize,
            targetSize,
            Time.deltaTime * zoomLerpSpeed
        );
    }
}
