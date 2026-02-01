using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class StartSim : MonoBehaviour
{
    [Header("Transition Settings")]
    public float transitionTime = 5f;    // camera rotation duration
    public float fadeOutTime = 1f;       // button shrink duration

    [Header("Text Motion")]
    public RectTransform flyingText;     // TMP RectTransform
    public Vector2 textFlyOffset = new Vector2(600f, 100f);
    public AnimationCurve textCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Easing")]
    public AnimationCurve easingCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2f),
        new Keyframe(0.6f, 0.85f, 0f, 0f),
        new Keyframe(1f, 1f, 2f, 0f)
    );

    private bool isRunning;
    private Vector3 buttonStartScale;
    private Vector2 textStartPos;

    void Awake()
    {
        buttonStartScale = transform.localScale;

        if (flyingText != null)
            textStartPos = flyingText.anchoredPosition;
    }

    public void TaskOnClick()
    {
        if (isRunning) return;
        isRunning = true;

        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Quaternion startRot = cam.transform.rotation;
        Quaternion targetRot = Quaternion.Euler(
            cam.transform.eulerAngles.x - 50f,
            cam.transform.eulerAngles.y,
            cam.transform.eulerAngles.z
        );

        float elapsed = 0f;

        while (elapsed < transitionTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionTime);
            float easedT = easingCurve.Evaluate(t);

            // Camera rotation
            cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, easedT);

            // Button shrink
            float q = Mathf.Clamp01(elapsed / fadeOutTime);
            float easedQ = easingCurve.Evaluate(q);
            transform.localScale = Vector3.Lerp(buttonStartScale, Vector3.zero, easedQ);

            // Text fly-off
            if (flyingText != null)
            {
                float textT = textCurve.Evaluate(t);
                flyingText.anchoredPosition = textStartPos + textFlyOffset * textT;
            }

            yield return null;
        }

        // Ensure final states
        cam.transform.rotation = targetRot;
        transform.localScale = Vector3.zero;

        if (flyingText != null)
            flyingText.anchoredPosition = textStartPos + textFlyOffset;

        SceneManager.LoadScene("LivelyCity");
    }
}
