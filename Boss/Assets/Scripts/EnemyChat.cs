using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class EnemyChat : MonoBehaviour
{
    public Text chatDisplayText; // Область вывода сообщений
    public int maxMessages = 10; // Максимум сообщений на экране

    private List<string> messages = new List<string>();

    // Метод для добавления сообщения в чат
    public void AddMessage(string message)
    {
        messages.Add(message);

        // Ограничиваем количество сообщений
        if (messages.Count > maxMessages)
        {
            messages.RemoveAt(0);
        }

        // Обновляем текст чата
        chatDisplayText.text = string.Join("\n", messages);
    }
}