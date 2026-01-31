using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Text;

public class SimChat : MonoBehaviour
{
    [Header("Settings")]
    public string apiKey = "PASTE_API_KEY_HERE";
    public string modelId = "gemma-3-27b-it"; 
    public float typeSpeed = 0.02f; 
    public float minTimeBetweenPrompts = 3.0f; // Minimum cooldown

    [Header("UI")]
    public TextMeshProUGUI textDisplay;
    public TextMeshProUGUI timerDisplay; // Optional: Drag a second TMP here to see the clock

    private float lastTurnEndTime;
    private bool isProcessing = false;

    // --- DATA STRUCTURES ---
    [System.Serializable] public class ResponseRoot { public ResponseCandidate[] candidates; }
    [System.Serializable] public class ResponseCandidate { public ResponseContent content; }
    [System.Serializable] public class ResponseContent { public ResponsePart[] parts; }
    [System.Serializable] public class ResponsePart { public string text; }
    [System.Serializable] public class RequestBody { public RequestContent[] contents; }
    [System.Serializable] public class RequestContent { public RequestPart[] parts; }
    [System.Serializable] public class RequestPart { public string text; }

    IEnumerator Start()
    {
        textDisplay.text = ""; 
        string chatHistory = "Context: Two Sims (Red and Blue) are meeting.";
        bool isRedTurn = true;
        lastTurnEndTime = -minTimeBetweenPrompts; // Allow first turn immediately

        while (true)
        {
            // 1. CALCULATE COOLDOWN
            float timeSinceLast = Time.time - lastTurnEndTime;
            
            // 2. WAIT ONLY IF NECESSARY
            while (timeSinceLast < minTimeBetweenPrompts)
            {
                timeSinceLast = Time.time - lastTurnEndTime;
                if (timerDisplay != null) 
                    timerDisplay.text = $"Next prompt in: {(minTimeBetweenPrompts - timeSinceLast):F1}s";
                
                yield return null; // Wait for next frame
            }

            if (timerDisplay != null) timerDisplay.text = "Status: Thinking...";

            // 3. START THE API TURN
            yield return StartCoroutine(RunChatTurn(chatHistory, isRedTurn, (newHistory, nextTurn) => {
                chatHistory = newHistory;
                isRedTurn = nextTurn;
            }));

            // 4. MARK THE END TIME
            lastTurnEndTime = Time.time;
        }
    }

    IEnumerator RunChatTurn(string currentHistory, bool isRed, System.Action<string, bool> callback)
    {
        string speaker = isRed ? "Red Sim" : "Blue Sim";
        string prompt = $"{currentHistory}\n\n(INSTRUCTION: You are {speaker}. One short sentence.)";
        
        RequestBody reqBody = new RequestBody { contents = new RequestContent[] { new RequestContent { parts = new RequestPart[] { new RequestPart { text = prompt } } } } };
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
                string cleanReply = response.candidates[0].content.parts[0].text.Split('\n')[0].Trim();
                cleanReply = cleanReply.Replace("Red Sim:", "").Replace("Blue Sim:", "").Trim();

                // UI Typewriter
                string color = isRed ? "#FF5555" : "#5555FF";
                textDisplay.text += $"\n\n<b><color={color}>{speaker}:</color></b> ";
                yield return StartCoroutine(TypeText(cleanReply));
                
                callback(currentHistory + $" {speaker}: {cleanReply}", !isRed);
            }
        }
    }

    IEnumerator TypeText(string message)
    {
        foreach (char letter in message.ToCharArray())
        {
            textDisplay.text += letter;
            yield return new WaitForSeconds(typeSpeed);
        }
    }
}