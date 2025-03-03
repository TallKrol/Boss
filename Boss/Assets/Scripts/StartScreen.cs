using UnityEngine;
using UnityEngine.UI;

public class StartScreen : MonoBehaviour
{
    public GameObject startPanel;
    public Button startButton;

    void Start()
    {
        if (startPanel == null || startButton == null)
        {
            Debug.LogError("StartPanel ��� StartButton �� ���������!");
            return;
        }
        startPanel.SetActive(true);
        startButton.onClick.AddListener(StartGame);
    }

    void StartGame()
    {
        startPanel.SetActive(false);
        FindObjectOfType<GameManager>()?.StartNewWave();
    }
}