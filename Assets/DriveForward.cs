using UnityEngine;

public class DriveForward : MonoBehaviour
{
    [Header("Movement")]
    public float forwardSpeed = 5f;

    void Start()
    {
    }

    void Update()
    {
        // Forward movement
        transform.position += transform.forward * forwardSpeed * Time.deltaTime;
    }
}
