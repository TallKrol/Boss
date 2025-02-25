using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public GameObject enemySpawnerPrefab;
    public float globalFatigue = 0f;
    private float waveTime = 0f;
    private int currentWave = 0;
    private bool waveActive = false;
    private int enemiesKilled = 0;
    private int upgradePoints = 0;

    // UI элементы
    public GameObject upgradePanel;
    public GameObject victoryPanel;
    public GameObject defeatPanel;
    public Slider fatigueSlider;
    public Text waveText;
    public Slider bossHealthSlider;
    public Text upgradePointsText;

    // Кэширование врагов
    private List<EnemyController> activeEnemies = new List<EnemyController>();

    // Музыка
    public AudioClip backgroundMusic;
    public AudioClip bossPhaseMusic;
    private AudioSource musicSource;

    private bool isUpgradePhase = false;
    private bool gameEnded = false;

    void Start()
    {
        StartNewWave();
        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);
        UpdateUI();

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    void Update()
    {
        if (gameEnded) return;

        if (waveActive)
        {
            waveTime += Time.deltaTime;

            if (waveTime >= 300f)
            {
                globalFatigue += 1f * Time.deltaTime / 5f;
            }

            if (activeEnemies.Count == 0)
            {
                EndWave();
            }
        }

        if (globalFatigue >= 100f)
        {
            EndGame(true);
        }

        UpdateUI();
        upgradePanel.SetActive(isUpgradePhase);
    }

    void UpdateUI()
    {
        if (fatigueSlider != null) fatigueSlider.value = globalFatigue / 100f;
        if (waveText != null) waveText.text = $"Волна {currentWave}";
        if (upgradePointsText != null) upgradePointsText.text = $"Очки: {upgradePoints}";
        BossController boss = FindObjectOfType<BossController>();
        if (bossHealthSlider != null && boss != null)
        {
            bossHealthSlider.maxValue = boss.IsInSecondPhase() ? 160f : 130f;
            bossHealthSlider.value = boss.health;
        }
    }

    void StartNewWave()
    {
        GameObject spawner = Instantiate(enemySpawnerPrefab, Vector3.zero, Quaternion.identity);
        EnemySpawner enemySpawner = spawner.GetComponent<EnemySpawner>();
        if (enemySpawner != null)
        {
            enemySpawner.OnEnemySpawned += AddEnemy;
        }
        currentWave++;
        waveTime = 0f;
        waveActive = true;
        isUpgradePhase = false;

        BossController boss = FindObjectOfType<BossController>();
        if (boss != null && boss.HasSecondPhasePurchased())
        {
            boss.RestoreSecondPhase();
        }
        Debug.Log($"Началась волна {currentWave}");
    }

    void EndWave()
    {
        globalFatigue += 20f;
        waveActive = false;
        upgradePoints += enemiesKilled * 2;
        if (enemiesKilled >= 5)
        {
            upgradePoints += 5;
            Debug.Log("Бонус +5 очков за убийство всей пати!");
        }
        enemiesKilled = 0;
        Debug.Log($"Волна {currentWave} провалилась. Усталость: {globalFatigue}%. Очки прокачки: {upgradePoints}");
        isUpgradePhase = true;
    }

    public void EnemyDied()
    {
        enemiesKilled++;
    }

    public void AddEnemy(EnemyController enemy)
    {
        activeEnemies.Add(enemy);
        enemy.OnDeath += RemoveEnemy;
    }

    public void RemoveEnemy(EnemyController enemy)
    {
        activeEnemies.Remove(enemy);
    }

    public void BossDied()
    {
        EndGame(false);
    }

public void BossPhaseChanged()
    {
        globalFatigue = Mathf.Max(0, globalFatigue - 10f);
        Debug.Log("Первая фаза босса убита! Усталость снижена на 10%. Текущая усталость: " + globalFatigue);
        if (bossPhaseMusic != null && musicSource != null)
        {
            musicSource.clip = bossPhaseMusic;
            musicSource.Play();
        }
    }

    void EndGame(bool bossWon)
    {
        gameEnded = true;
        waveActive = false;
        isUpgradePhase = false;
        upgradePanel.SetActive(false);

        if (bossWon)
        {
            Debug.Log("Босс победил — усталость игроков достигла 100%!");
            victoryPanel.SetActive(true);
        }
        else
        {
            Debug.Log("Игроки победили — босс мёртв!");
            defeatPanel.SetActive(true);
        }
        if (musicSource != null) musicSource.Stop();
    }

    public int GetCurrentWave()
    {
        return currentWave;
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Игра закрыта!");
    }

    public void UpgradeSword()
    {
        if (upgradePoints >= 5)
        {
            FindObjectOfType<BossController>().UpgradeWeapon("Sword", 5f);
            upgradePoints -= 5;
            StartNewWave();
        }
    }

    public void UpgradeArrow()
    {
        if (upgradePoints >= 5)
        {
            FindObjectOfType<BossController>().UpgradeWeapon("Arrow", 5f);
            upgradePoints -= 5;
            StartNewWave();
        }
    }

    public void IncreaseHealth()
    {
        if (upgradePoints >= 10)
        {
            FindObjectOfType<BossController>().IncreaseHealth(20f);
            upgradePoints -= 10;
            StartNewWave();
        }
    }

    public void BuyShield()
    {
        if (upgradePoints >= 15)
        {
            FindObjectOfType<BossController>().ActivateShield(50f);
            upgradePoints -= 15;
            StartNewWave();
        }
    }

    public void BuySecondPhase()
    {
        if (upgradePoints >= 20 && !FindObjectOfType<BossController>().HasSecondPhasePurchased())
        {
            FindObjectOfType<BossController>().PurchaseSecondPhase();
            upgradePoints -= 20;
            StartNewWave();
        }
    }
}
