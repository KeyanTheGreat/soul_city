using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public int boundsX = 500;
    public int boundsY = 500;
    void Start()
    {
        transform.position += new Vector3(10.0f, 0.0f, 0.0f);
    }

    void Update()
    {
        
    }
}
