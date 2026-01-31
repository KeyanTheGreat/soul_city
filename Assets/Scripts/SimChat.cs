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
    public float typeSpeed = 0.03f; // Seconds between letters

    [Header("UI")]
    public TextMeshProUGUI textDisplay;

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

        string chatHistory = "Context: Two Sims (Red and Blue) are meeting for the first time.";
        bool isRedTurn = true;

        while (true)
        {
            string speaker = isRedTurn ? "Red Sim" : "Blue Sim";
            string prompt = $"{chatHistory}\n\n(INSTRUCTION: You are {speaker}. Write exactly ONE short sentence.)";
            
            RequestBody reqBody = new RequestBody();
            reqBody.contents = new RequestContent[1];
            reqBody.contents[0] = new RequestContent();
            reqBody.contents[0].parts = new RequestPart[1];
            reqBody.contents[0].parts[0] = new RequestPart();
            reqBody.contents[0].parts[0].text = prompt;

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
                    string fullReply = response.candidates[0].content.parts[0].text;
                    
                    // Clean the reply
                    string cleanReply = fullReply.Split('\n')[0].Trim();
                    cleanReply = cleanReply.Replace("Red Sim:", "").Replace("Blue Sim:", "").Trim();

                    // --- SCROLLING TEXT START ---
                    string color = isRedTurn ? "#FF5555" : "#5555FF";
                    string header = $"\n\n<b><color={color}>{speaker}:</color></b> ";
                    
                    // Add the header immediately
                    textDisplay.text += header;

                    // Type the message out character by character
                    yield return StartCoroutine(TypeText(cleanReply));
                    // --- SCROLLING TEXT END ---
                    
                    chatHistory += $" {speaker}: {cleanReply}";
                    isRedTurn = !isRedTurn;
                }
            } 

            yield return new WaitForSeconds(3f); // Wait after typing finishes
        }
    }

    // Helper function to print text letter by letter
    IEnumerator TypeText(string message)
    {
        foreach (char letter in message.ToCharArray())
        {
            textDisplay.text += letter;
            // Scroll to bottom every letter (useful for long chats)
            Canvas.ForceUpdateCanvases(); 
            yield return new WaitForSeconds(typeSpeed);
        }
    }
}