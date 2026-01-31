using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class CameraControl : MonoBehaviour
{
    public int boundsX = 500;
    public int boundsY = 500;
    public Camera cam;
    private Coroutine currentLerp;
    void Start()
    {
        transform.position += new Vector3(0.0f, 0.0f, 0.0f);
    }
    void Update()
    {
        if (Mouse.current.rightButton.isPressed)
        {
            Vector3 mouseWorldPosition = GetMouseWorldPosition();
            float magnitude = Vector3.Distance(mouseWorldPosition, new Vector3(transform.position.x, 0.0f, transform.position.z));
            if (mouseWorldPosition != Vector3.zero 
                && mouseWorldPosition.x < boundsX 
                && mouseWorldPosition.z < boundsY)
            {
                Debug.Log("Mouse 3D Position: " + mouseWorldPosition);
                StartLerp(new Vector3(mouseWorldPosition.x, 0.0f, mouseWorldPosition.z), 1.0f / Mathf.Clamp(magnitude, 2.0f, 3.0f));
            }
        }
    }
    public Vector3 GetMouseWorldPosition()
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return hit.point;
        }
        else
        {
            return Vector3.zero;
        }
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
