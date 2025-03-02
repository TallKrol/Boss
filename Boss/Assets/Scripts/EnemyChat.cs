using UnityEngine;
using UnityEngine.UI;

public class EnemyChat : MonoBehaviour
{
    public Text chatDisplayText;
    private string chatLog = "";

    void Start()
    {
        if (chatDisplayText == null)
        {
            Debug.LogError("ChatDisplayText не привязан в инспекторе!");
        }
    }

    public void AddMessage(string message)
    {
        chatLog = message + "\n" + chatLog;
        if (chatDisplayText != null)
        {
            chatDisplayText.text = chatLog;
        }
    }
}