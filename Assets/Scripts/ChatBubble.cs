using UnityEngine;
using TMPro;
using System.Collections;

public class ChatBubble : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI textMesh; // Drag the Text object here
    public GameObject bubbleVisuals; // Drag the "BubbleImage" object here

    [Header("Settings")]
    public float typeSpeed = 0.03f;
    public float timeVisible = 4.0f; // How long to stay before vanishing

    private Transform cam;

    void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
        if (bubbleVisuals) bubbleVisuals.SetActive(false); // Start hidden
    }

    void LateUpdate()
    {
        if (cam == null) return;

        // 1. Get the rotation needed to look at the camera
        // (transform.position + cam.forward) creates a target point directly in front of the object
        // aligned with the camera's viewing angle.
        transform.LookAt(transform.position + cam.forward);

        // 2. (Optional) Lock the Z-axis rotation if the bubble starts tilting weirdly
        // This ensures the text stays horizontal and doesn't "roll" like a clock hand.
        Vector3 currentRotation = transform.localEulerAngles;
        currentRotation.z = 0; 
        transform.localEulerAngles = currentRotation;
    }

    public void ShowText(string text)
    {
        // Stop any old typing/hiding so we can start fresh
        StopAllCoroutines();
        StartCoroutine(TypeRoutine(text));
    }

    IEnumerator TypeRoutine(string text)
    {
        if (bubbleVisuals) bubbleVisuals.SetActive(true);
        textMesh.text = ""; // Clear old text

        foreach (char letter in text.ToCharArray())
        {
            textMesh.text += letter;
            yield return new WaitForSeconds(typeSpeed);
        }

        // Wait a few seconds so the player can read it
        yield return new WaitForSeconds(timeVisible);

        // Hide the bubble
        if (bubbleVisuals) bubbleVisuals.SetActive(false);
    }
}