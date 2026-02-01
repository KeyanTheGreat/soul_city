using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class CameraControl : MonoBehaviour
{
    public int boundsX = 500;
    public int boundsY = 500;
    public Camera cam;

    public float moveSpeed = 10f; // WASD speed

    private Coroutine currentLerp;

    void Update()
    {
        HandleMousePan();
        HandleWASD();
    }

    private void HandleMousePan()
    {
        if (Mouse.current.rightButton.isPressed)
        {
            Vector3 mouseWorldPosition = GetMouseWorldPosition();
            float magnitude = Vector3.Distance(mouseWorldPosition, new Vector3(transform.position.x, 0.0f, transform.position.z));
            if (mouseWorldPosition != Vector3.zero)
            {
                Debug.Log("Mouse 3D Position: " + mouseWorldPosition);
                StartLerp(new Vector3(mouseWorldPosition.x, transform.position.y, mouseWorldPosition.z), 1.0f / Mathf.Clamp(magnitude, 2.0f, 3.0f));
            }
        }
    }

    private void HandleWASD()
    {
        Vector3 move = Vector3.zero;

        if (Keyboard.current.sKey.isPressed) move += new Vector3(1, 0, 1);   // forward along isometric diagonal
        if (Keyboard.current.wKey.isPressed) move += new Vector3(-1, 0, -1); // backward
        if (Keyboard.current.dKey.isPressed) move += new Vector3(-1, 0, 1);  // left
        if (Keyboard.current.aKey.isPressed) move += new Vector3(1, 0, -1);  // right

        if (move != Vector3.zero)
        {
            // Normalize so diagonal isn't faster
            move.Normalize();

            Vector3 newPos = transform.position + move * moveSpeed * Time.deltaTime;

            // Clamp within bounds
            newPos.x = Mathf.Clamp(newPos.x, -boundsX, boundsX);
            newPos.z = Mathf.Clamp(newPos.z, -boundsY, boundsY);

            transform.position = new Vector3(newPos.x, transform.position.y, newPos.z);

            // Stop ongoing mouse lerp
            if (currentLerp != null)
            {
                StopCoroutine(currentLerp);
                currentLerp = null;
            }
        }
    }

    public Vector3 GetMouseWorldPosition()
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.point;
        }
        return Vector3.zero;
    }

    public void StartLerp(Vector3 target, float duration)
    {
        if (currentLerp != null)
            StopCoroutine(currentLerp);

        currentLerp = StartCoroutine(LerpRoutine(target, duration));
    }

    IEnumerator LerpRoutine(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        transform.position = target;
    }
}

