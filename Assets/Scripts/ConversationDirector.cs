using UnityEngine;
using UnityEngine.InputSystem; // This is the required line

public class ConversationDirector : MonoBehaviour
{
    public SimAgent initiator; // Drag RED here
    public SimAgent receiver;  // Drag BLUE here

    void Update()
    {
        // Safety check: Make sure a keyboard is connected
        if (Keyboard.current == null) return;

        // Check for the "Enter" (Return) key
        if (Keyboard.current.enterKey.wasPressedThisFrame) 
        {
            if (initiator != null && receiver != null)
            {
                initiator.ForceConversation(receiver);
            }
            else
            {
                Debug.LogWarning("Please assign both agents (Initiator and Receiver) in the Inspector!");
            }
        }
    }
}