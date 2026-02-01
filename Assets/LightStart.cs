using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightStart : MonoBehaviour
{
    [Header("Light Settings")]
    public float targetIntensity = 1.2f; // final light intensity
    public float lerpSpeed = 2f;         // how fast it ramps up

    private Light dirLight;

    void Awake()
    {
        dirLight = GetComponent<Light>();

        // Set initial intensity to 0
        dirLight.intensity = 0f;
    }

    void Update()
    {
        // Smoothly increase light intensity toward target
        dirLight.intensity = Mathf.Lerp(dirLight.intensity, targetIntensity, Time.deltaTime * lerpSpeed);
    }
}
