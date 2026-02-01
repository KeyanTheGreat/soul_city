using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;

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

    [Header("Blip-Blip Voice")]
    public AudioClip voiceClip; // LEAVE EMPTY to generate automatically
    [Range(0.5f, 2.0f)] public float basePitch = 1.0f; // 0.8 for Deep, 1.2 for High

    // --- STATE ---
    public bool isBusy = false;
    public SimAgent currentPartner;
    private float cooldownTimer = 0f;
    private List<string> internalMemory = new List<string>();
    private bool isThinking = false;
    private AudioSource audioSource;
    
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
        audioSource.spatialBlend = 1.0f; // 3D Sound

        // --- SYNTH GENERATOR ---
        // If you didn't drag a file, we make one with math!
        if (voiceClip == null)
        {
            voiceClip = CreateProceduralBeep();
        }
    }

    void Update()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            return; 
        }

        if (!isBusy)
        {
            if (Time.frameCount % 30 == 0) ScanForPartner();
        }
    }

    // --- PROCEDURAL AUDIO GENERATOR ---
    AudioClip CreateProceduralBeep()
    {
        int sampleRate = 44100;
        float frequency = 800f; // Base tone (Hz)
        float length = 0.08f;   // Very short (80ms)
        int sampleCount = (int)(sampleRate * length);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            // Generate a Sine Wave
            float wave = Mathf.Sin(2 * Mathf.PI * frequency * t);
            
            // Apply a "Fade Out" so it doesn't click at the end
            float volume = Mathf.Lerp(1f, 0f, t / length); 
            
            samples[i] = wave * volume * 0.25f; // 0.25f is master volume
        }

        AudioClip clip = AudioClip.Create("SynthBeep", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // =================================================================================
    // RADAR LOGIC
    // =================================================================================
    void ScanForPartner()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, characterLayer);

        foreach (var hit in hits)
        {
            SimAgent potentialPartner = hit.GetComponentInParent<SimAgent>();

            if (potentialPartner != null && 
                potentialPartner != this && 
                !potentialPartner.isBusy && 
                potentialPartner.cooldownTimer <= 0)
            {
                ForceConversation(potentialPartner);
                break; 
            }
        }
    }

    // =================================================================================
    // CONVERSATION MANAGEMENT
    // =================================================================================
    public void ForceConversation(SimAgent partner)
    {
        this.isBusy = true;
        this.currentPartner = partner;
        this.internalMemory.Clear();
        
        partner.JoinConversation(this);

        internalMemory.Add($"System: You just saw {partner.agentName}. Start a conversation.");
        StartCoroutine(ThinkAndReply());
    }

    public void JoinConversation(SimAgent initiator)
    {
        StopAllCoroutines(); 
        this.isBusy = true;
        this.currentPartner = initiator;
        this.internalMemory.Clear();
    }

    public void HearMessage(string message)
    {
        if (!isBusy || currentPartner == null) return;

        internalMemory.Add($"{currentPartner.agentName}: {message}");

        if (message.ToLower().Contains("goodbye") || message.ToLower().Contains("bye"))
        {
            EndConversation();
            if(currentPartner) currentPartner.EndConversation();
            return;
        }

        StartCoroutine(ThinkAndReply());
    }

    public void EndConversation()
    {
        isBusy = false;
        currentPartner = null;
        cooldownTimer = postChatCooldown; 
        if (myBubble) myBubble.ShowText("...");
    }

    // =================================================================================
    // THE BRAIN (Gemini API)
    // =================================================================================
    IEnumerator ThinkAndReply()
    {
        if (isThinking) yield break;
        isThinking = true;

        yield return new WaitForSeconds(Random.Range(1.0f, 2.5f));

        string historyText = string.Join("\n", internalMemory);
        if (internalMemory.Count > 6) historyText = "..." + string.Join("\n", internalMemory.GetRange(internalMemory.Count - 6, 6));

        string finalPrompt = $@"
System: Your name is {agentName}. Persona: {persona}
Context: Talking to {currentPartner.agentName}.
History: {historyText}
INSTRUCTION: Reply under 20 words. If you want to stop, say 'Goodbye'. Output only speech.";

        // Construct JSON
        RequestBody reqBody = new RequestBody { contents = new RequestContent[] { new RequestContent { parts = new RequestPart[] { new RequestPart { text = finalPrompt } } } } };
        string json = JsonUtility.ToJson(reqBody);
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:generateContent?key={apiKey.Trim()}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                ResponseRoot response = JsonUtility.FromJson<ResponseRoot>(request.downloadHandler.text);
                if (response.candidates != null && response.candidates.Length > 0)
                {
                    string reply = response.candidates[0].content.parts[0].text.Trim().Replace("\"", "").Replace($"{agentName}:", "").Trim();
                    
                    Debug.Log($"[{agentName}]: {reply}");

                    internalMemory.Add($"{agentName}: {reply}");
                    
                    if (myBubble) myBubble.ShowText(reply);
                    
                    // PLAY PROCEDURAL BLIPS (This will now run longer)
                    StartCoroutine(PlayBlipSound(reply.Length));

                    if (currentPartner) currentPartner.HearMessage(reply);
                }
            }
        }
        isThinking = false;
    }

    // =================================================================================
    // THE AUDIO PLAYER (Updated Logic)
    // =================================================================================
    IEnumerator PlayBlipSound(int charCount)
    {
        if (voiceClip == null) yield break;

        // NEW LOGIC:
        // We calculate 1 blip for every single character (roughly).
        // We set the minimum to 3 blips.
        // We set the maximum to 30 blips (so it doesn't drone on forever if they write an essay).
        int blips = Mathf.Clamp(charCount / 3, 3, 30);

        for (int i = 0; i < blips; i++)
        {
            // Randomize pitch for "talking" effect
            audioSource.pitch = basePitch + Random.Range(-0.15f, 0.15f);
            audioSource.PlayOneShot(voiceClip);
            
            // Wait based on talking speed (0.08 is standard "Animal Crossing" speed)
            yield return new WaitForSeconds(0.08f);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isBusy ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}