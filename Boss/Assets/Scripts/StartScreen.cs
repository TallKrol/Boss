
using UnityEngine;
using UnityEngine.UI;

public class StartScreen : MonoBehaviour
{
    public GameObject startPanel;
    public Button startButton;

    void Start()
    {
        startPanel.SetActive(true);
        Time.timeScale = 0f;
        startButton.onClick.AddListener(StartGame);
    }

    void StartGame()
    {
        startPanel.SetActive(false);
        Time.timeScale = 1f;
    }
}