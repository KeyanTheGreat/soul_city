using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class SimAgent : MonoBehaviour
{
    [Header("Identity")]
    public string agentName;
    [TextArea(3, 10)] public string persona; // "You are a farmer who is secretly broke..."
    
    [Header("Connections")]
    public ChatBubble myBubble; // Drag your Bubble Canvas here
    public string apiKey = "YOUR_API_KEY_HERE"; // Paste your Gemini Key here
    public string modelId = "gemini-2.0-flash"; // Or "gemini-1.5-flash"

    [Header("Current State (Read Only)")]
    public bool isBusy = false;
    public SimAgent currentPartner;

    // --- PRIVATE MEMORY ---
    private List<string> internalMemory = new List<string>();
    private bool isThinking = false;

    // --- JSON CLASSES FOR GEMINI API ---
    [System.Serializable] public class ResponseRoot { public ResponseCandidate[] candidates; }
    [System.Serializable] public class ResponseCandidate { public ResponseContent content; }
    [System.Serializable] public class ResponseContent { public ResponsePart[] parts; }
    [System.Serializable] public class ResponsePart { public string text; }
    [System.Serializable] public class RequestBody { public RequestContent[] contents; }
    [System.Serializable] public class RequestContent { public RequestPart[] parts; }
    [System.Serializable] public class RequestPart { public string text; }

    // =================================================================================
    // 1. MANUAL TRIGGER (Called by Director)
    // =================================================================================
    public void ForceConversation(SimAgent partner)
    {
        // Reset Logic
        StopAllCoroutines();
        this.isBusy = true;
        this.currentPartner = partner;
        this.internalMemory.Clear();

        partner.StopAllCoroutines();
        partner.isBusy = true;
        partner.currentPartner = this;
        partner.internalMemory.Clear();

        Debug.Log($"<color=orange>[DIRECTOR]</color> {agentName} is approaching {partner.agentName}...");

        // Inject the first thought to kickstart the brain
        internalMemory.Add($"System: You just walked up to {partner.agentName}. Start a conversation based on your persona.");
        
        StartCoroutine(ThinkAndReply());
    }

    // =================================================================================
    // 2. LISTENING LOGIC
    // =================================================================================
    public void HearMessage(string message)
    {
        // If I'm not locked in a chat, ignore it (or you could auto-accept)
        if (!isBusy || currentPartner == null) return;

        // 1. Add what they said to my memory
        internalMemory.Add($"{currentPartner.agentName}: {message}");

        // 2. Check for conversation enders
        if (message.ToLower().Contains("goodbye") || message.ToLower().Contains("bye!"))
        {
            EndConversation();
            return;
        }

        // 3. It's my turn to speak
        StartCoroutine(ThinkAndReply());
    }

    // =================================================================================
    // 3. THE BRAIN (API CALL)
    // =================================================================================
    IEnumerator ThinkAndReply()
    {
        if (isThinking) yield break; // Safety check
        isThinking = true;

        // Simulate "Thinking Time" (makes it feel more natural)
        yield return new WaitForSeconds(Random.Range(1.5f, 3.0f));

        // Format History (Keep last 6 lines to save tokens)
        string historyText = string.Join("\n", internalMemory);
        if (internalMemory.Count > 6) 
            historyText = "..." + string.Join("\n", internalMemory.GetRange(internalMemory.Count - 6, 6));

        // Construct the Prompt
        string finalPrompt = $@"
System: Your name is {agentName}. 
Your Persona: {persona}
Current Context: You are talking to {currentPartner.agentName}.
Conversation History:
{historyText}

INSTRUCTIONS:
- Reply to {currentPartner.agentName}.
- Keep your reply under 20 words.
- If you want to end the chat, say 'Goodbye'.
- Output ONLY your spoken text.
";

        // JSON Serialization
        RequestBody reqBody = new RequestBody { contents = new RequestContent[] { new RequestContent { parts = new RequestPart[] { new RequestPart { text = finalPrompt } } } } };
        string json = JsonUtility.ToJson(reqBody);
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:generateContent?key={apiKey.Trim()}";

        // Web Request
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Parse Response
                ResponseRoot response = JsonUtility.FromJson<ResponseRoot>(request.downloadHandler.text);
                if (response.candidates != null && response.candidates.Length > 0)
                {
                    string reply = response.candidates[0].content.parts[0].text.Trim();
                    
                    // Clean up output (remove quotes or name prefixes if the AI adds them)
                    reply = reply.Replace("\"", "").Replace($"{agentName}:", "").Trim();

                    // Execution
                    Debug.Log($"<color=green>[{agentName}]:</color> {reply}");
                    
                    // Add my own words to my memory
                    internalMemory.Add($"{agentName}: {reply}");
                    
                    // Show Bubble
                    if (myBubble) myBubble.ShowText(reply);
                    
                    // Send to Partner
                    if (currentPartner != null) currentPartner.HearMessage(reply);
                }
            }
            else
            {
                Debug.LogError($"API Error: {request.error}\n{request.downloadHandler.text}");
            }
        }

        isThinking = false;
    }

    // =================================================================================
    // 4. CLEANUP
    // =================================================================================
    public void EndConversation()
    {
        Debug.Log($"<color=red>[{agentName}] Conversation Ended.</color>");
        isBusy = false;
        currentPartner = null;
        if (myBubble) myBubble.ShowText("...");
    }
}