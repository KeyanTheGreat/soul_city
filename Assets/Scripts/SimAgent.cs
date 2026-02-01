using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;
using TMPro; // Added for TextMeshPro access

[RequireComponent(typeof(AudioSource))]
public class SimAgent : MonoBehaviour
{
    [Header("Identity")]
    public string agentName;
    [TextArea(3, 10)] public string persona;

    [Header("Proximity Radar")]
    public float detectionRadius = 4.0f;
    public LayerMask characterLayer;      
    public float postChatCooldown = 5.0f; 

    [Header("Connections")]
    public ChatBubble myBubble;
    public string apiKey = "YOUR_API_KEY_HERE";
    public string modelId = "gemini-2.0-flash";

    [Header("Isometric Correction")]
    public Vector3 manualOffsetCorrection; 

    [Header("Blip-Blip Voice")]
    public AudioClip voiceClip; 
    [Range(0.5f, 2.0f)] public float basePitch = 1.0f; 

    private Vector3 capturedOffset; 
    private Transform bubbleTransform;
    private AudioSource audioSource;
    public bool isBusy = false;
    public SimAgent currentPartner;
    private float cooldownTimer = 0f;
    private List<string> internalMemory = new List<string>();
    private bool isThinking = false;

    // --- JSON CLASSES ---
    [System.Serializable] public class ResponseRoot { public ResponseCandidate[] candidates; }
    [System.Serializable] public class ResponseCandidate { public ResponseContent content; }
    [System.Serializable] public class ResponseContent { public ResponsePart[] parts; }
    [System.Serializable] public class ResponsePart { public string text; }
    [System.Serializable] public class RequestBody { public RequestContent[] contents; }
    [System.Serializable] public class RequestContent { public RequestPart[] parts; }
    [System.Serializable] public class RequestPart { public string text; }

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f; 

        if (myBubble == null) myBubble = GetComponentInChildren<ChatBubble>();

        if (myBubble != null)
        {
            bubbleTransform = myBubble.transform;
            capturedOffset = bubbleTransform.position - transform.position;

            // --- 1. FORCE CANVAS SORTING ---
            Canvas bubbleCanvas = myBubble.GetComponent<Canvas>();
            if (bubbleCanvas != null)
            {
                bubbleCanvas.sortingOrder = 999;
            }

            // --- 2. FORCE MATERIALS TO ALWAYS SHOW (Z-TEST FIX) ---
            ApplyAlwaysOnTop(myBubble.gameObject);

            // DETACH to keep motion smooth
            bubbleTransform.SetParent(null);
        }

        if (voiceClip == null) voiceClip = CreateProceduralBeep();
    }

    // Logic to force UI materials to ignore depth
    void ApplyAlwaysOnTop(GameObject root)
    {
        Graphic[] graphics = root.GetComponentsInChildren<Graphic>();
        foreach (Graphic g in graphics)
        {
            // Create a unique instance of the material so we don't break every other UI in the game
            Material overlayMat = new Material(g.defaultMaterial);
            // 8 = "Always" ZTest mode
            overlayMat.SetInt("unity_GUIZTestMode", 8); 
            g.material = overlayMat;
        }
    }

    void LateUpdate()
    {
        if (bubbleTransform != null)
        {
            // Keep the bubble following the agent + your manual slider
            bubbleTransform.position = transform.position + capturedOffset + manualOffsetCorrection;

            // Keep it facing the Ortho camera
            if (Camera.main != null)
            {
                bubbleTransform.rotation = Camera.main.transform.rotation;
            }
        }
    }

    // --- AI BRAIN & AUDIO ---
    void Update()
    {
        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;
        else if (!isBusy && Time.frameCount % 30 == 0) ScanForPartner();
    }

    AudioClip CreateProceduralBeep()
    {
        int sampleRate = 44100; float frequency = 800f; float length = 0.08f;   
        int sampleCount = (int)(sampleRate * length); float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++) {
            float t = i / (float)sampleRate;
            float wave = Mathf.Sin(2 * Mathf.PI * frequency * t);
            float volume = Mathf.Lerp(1f, 0f, t / length); 
            samples[i] = wave * volume * 0.25f; 
        }
        AudioClip clip = AudioClip.Create("SynthBeep", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0); return clip;
    }

    void ScanForPartner()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, characterLayer);
        foreach (var hit in hits) {
            SimAgent potentialPartner = hit.GetComponentInParent<SimAgent>();
            if (potentialPartner != null && potentialPartner != this && !potentialPartner.isBusy && potentialPartner.cooldownTimer <= 0) {
                ForceConversation(potentialPartner); break; 
            }
        }
    }

    public void ForceConversation(SimAgent partner) {
        isBusy = true; currentPartner = partner; internalMemory.Clear();
        partner.JoinConversation(this);
        internalMemory.Add($"System: You just saw {partner.agentName}. Start a conversation.");
        StartCoroutine(ThinkAndReply());
    }

    public void JoinConversation(SimAgent initiator) {
        StopAllCoroutines(); isBusy = true; currentPartner = initiator; internalMemory.Clear();
    }

    public void HearMessage(string message) {
        if (!isBusy || currentPartner == null) return;
        internalMemory.Add($"{currentPartner.agentName}: {message}");
        if (message.ToLower().Contains("goodbye") || message.ToLower().Contains("bye")) {
            EndConversation(); if(currentPartner) currentPartner.EndConversation(); return;
        }
        StartCoroutine(ThinkAndReply());
    }

    public void EndConversation() {
        isBusy = false; currentPartner = null; cooldownTimer = postChatCooldown; 
        if (myBubble) myBubble.ShowText("...");
    }

    IEnumerator ThinkAndReply() {
        if (isThinking) yield break;
        isThinking = true;
        yield return new WaitForSeconds(Random.Range(1.0f, 2.5f));
        string historyText = string.Join("\n", internalMemory);
        if (internalMemory.Count > 6) historyText = "..." + string.Join("\n", internalMemory.GetRange(internalMemory.Count - 6, 6));
        string finalPrompt = $"System: Your name is {agentName}. Persona: {persona}\nContext: Talking to {currentPartner.agentName}.\nHistory: {historyText}\nINSTRUCTION: Reply under 20 words. Output only speech.";
        RequestBody reqBody = new RequestBody { contents = new RequestContent[] { new RequestContent { parts = new RequestPart[] { new RequestPart { text = finalPrompt } } } } };
        string json = JsonUtility.ToJson(reqBody);
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:generateContent?key={apiKey.Trim()}";
        using (UnityWebRequest request = new UnityWebRequest(url, "POST")) {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success) {
                ResponseRoot response = JsonUtility.FromJson<ResponseRoot>(request.downloadHandler.text);
                if (response.candidates != null && response.candidates.Length > 0) {
                    string reply = response.candidates[0].content.parts[0].text.Trim().Replace("\"", "").Replace($"{agentName}:", "").Trim();
                    internalMemory.Add($"{agentName}: {reply}");
                    if (myBubble) myBubble.ShowText(reply);
                    StartCoroutine(PlayBlipSound(reply.Length));
                    if (currentPartner) currentPartner.HearMessage(reply);
                }
            }
        }
        isThinking = false;
    }

    IEnumerator PlayBlipSound(int charCount) {
        if (voiceClip == null) yield break;
        int blips = Mathf.Clamp(charCount / 2, 3, 60);
        for (int i = 0; i < blips; i++) {
            audioSource.pitch = basePitch + Random.Range(-0.15f, 0.15f);
            audioSource.PlayOneShot(voiceClip);
            yield return new WaitForSeconds(0.08f);
        }
    }

    void OnDrawGizmos() { Gizmos.color = isBusy ? Color.red : Color.yellow; Gizmos.DrawWireSphere(transform.position, detectionRadius); }
}