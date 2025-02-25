using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class EnemyChat : MonoBehaviour
{
    public Text chatDisplayText; // ������� ������ ���������
    public int maxMessages = 10; // �������� ��������� �� ������

    private List<string> messages = new List<string>();

    // ����� ��� ���������� ��������� � ���
    public void AddMessage(string message)
    {
        messages.Add(message);

        // ������������ ���������� ���������
        if (messages.Count > maxMessages)
        {
            messages.RemoveAt(0);
        }

        // ��������� ����� ����
        chatDisplayText.text = string.Join("\n", messages);
    }
}