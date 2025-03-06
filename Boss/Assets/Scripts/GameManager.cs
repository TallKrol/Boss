using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public GameObject enemySpawnerPrefab;
    public float globalFatigue = 0f;
    private float waveTime = 0f;
    private int currentWave = 0;
    private bool waveActive = false;
    private int enemiesKilled = 0;
    private int upgradePoints = 0;

    public GameObject upgradePanel;
    public GameObject victoryPanel;
    public GameObject defeatPanel;
    public Slider fatigueSlider;
    public Text waveText;
    public Slider bossHealthSlider;
    public Text upgradePointsText;

    public GameObject statsPanel;
    public Text statsText;

    private List<EnemyController> activeEnemies = new List<EnemyController>();
    private float totalDamageToEnemies = 0f;
    private int upgradesPurchased = 0;
    private Dictionary<string, float> damageByClass = new Dictionary<string, float>();
    private float totalClericHealing = 0f;
    private string bossKiller = "";
    private float lastBossHitTime = -1f;

    public AudioClip backgroundMusic;
    public AudioClip bossPhaseMusic;
    private AudioSource musicSource;

    private bool isUpgradePhase = false;
    private bool gameEnded = false;

    private Dictionary<string, string> classNicknames = new Dictionary<string, string>();

    void Start()
    {
        string[] prefixes = { "Dark", "Light", "Swift", "Rage", "Cool", "Pro", "Silent", "Lone", "Clown", "Nagibator", "TrakTor", "Syn_v_", "genius", "krol", "lork", "ne_", "Daniil", "Kill", "Sliver", "BoRobuSHeK", "DeD", "Divanchik", "Zeus", "ProDaMGaRaZH", "IzYUM" };
        string[] suffixes = { "X", "Y", "Z", "123", "007", "Blade", "Fire", "Storm", "Wolf", "XYZ", "3000", "Parovoz", "Demon", "Chai", "100%", "1000-7", "fire", "erif", "moloko", "2017", "Zadrot", "FoX", "Kr0lik", "NaDivane", "Pirozhok", "Knch", "42" };
        List<string> usedNicknames = new List<string>();

        foreach (string className in new[] { "Berserker", "Sniper", "Pyromancer", "Cleric", "Guardian", "Bard" })
        {
            damageByClass[className] = 0f;
            string nickname;
            do
            {
                nickname = prefixes[UnityEngine.Random.Range(0, prefixes.Length)] +
                           suffixes[UnityEngine.Random.Range(0, suffixes.Length)];
            } while (usedNicknames.Contains(nickname));
            usedNicknames.Add(nickname);
            classNicknames[className] = nickname;
        }

        StartNewWave();
        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);
        statsPanel.SetActive(false);
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
        if (waveText != null) waveText.text = $"Âîëíà {currentWave}";
        if (upgradePointsText != null) upgradePointsText.text = $"Î÷êè: {upgradePoints}";
        BossController boss = FindObjectOfType<BossController>();
        if (bossHealthSlider != null && boss != null)
        {
            bossHealthSlider.maxValue = boss.IsInSecondPhase() ? 160f : 130f;
            bossHealthSlider.value = boss.health;
        }
    }

    public void StartNewWave()
    {
        GameObject spawner = Instantiate(enemySpawnerPrefab, Vector3.zero, Quaternion.identity);
        EnemySpawner enemySpawner = spawner.GetComponent<EnemySpawner>();
        if (enemySpawner != null)
        {
            enemySpawner.SetNicknames(classNicknames);
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
        Debug.Log($"Íà÷àëàñü âîëíà {currentWave}");
    }

    void EndWave()
    {
        globalFatigue += 20f;
        waveActive = false;
        upgradePoints += enemiesKilled * 2;
        if (enemiesKilled >= 6)
        {
            upgradePoints += 5;
            Debug.Log("Áîíóñ +5 î÷êîâ çà óáèéñòâî âñåé ïàòè!");
        }
        enemiesKilled = 0;
        Debug.Log($"Âîëíà {currentWave} ïðîâàëèëàñü. Óñòàëîñòü: {globalFatigue}%. Î÷êè ïðîêà÷êè: {upgradePoints}");
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

    public void BossDied(string killer, float hitTime)
    {
        if (hitTime > lastBossHitTime)
        {
            bossKiller = killer; // Òåïåðü "Íèê [Êëàññ]"
            lastBossHitTime = hitTime;
        }
        EndGame(false);
    }
    public void BossPhaseChanged()
    {
        globalFatigue = Mathf.Max(0, globalFatigue - 10f);
        Debug.Log("Ïåðâàÿ ôàçà áîññà óáèòà! Óñòàëîñòü ñíèæåíà íà 10%. Òåêóùàÿ óñòàëîñòü: " + globalFatigue);
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
            Debug.Log("Áîññ ïîáåäèë — óñòàëîñòü èãðîêîâ äîñòèãëà 100%!");
            victoryPanel.SetActive(true);
            statsPanel.SetActive(true);
            statsText.text = $"Ïîáåäà Áîññà!\n" +
                             $"Íàíåñ¸ííûé óðîí âðàãàì: {totalDamageToEnemies:F1}\n" +
                             $"Êóïëåíî óëó÷øåíèé: {upgradesPurchased}\n" +
                             $"Ïåðåæèòî âîëí: {currentWave}";
        }
        else
        {
            Debug.Log("Èãðîêè ïîáåäèëè — áîññ ì¸ðòâ!");
            defeatPanel.SetActive(true);
            statsPanel.SetActive(true);
            var topDPS = damageByClass.OrderByDescending(x => x.Value).First();
            statsText.text = $"Ïîáåäà Èãðîêîâ!\n" +
                             $"Áîëüøå âñåãî óðîíà: {topDPS.Key} ({topDPS.Value:F1})\n" +
                             $"Ëå÷åíèå Êëåðêà: {totalClericHealing:F1}\n" +
                             $"Óáèéöà áîññà: {bossKiller}\n" +
                             $"Îáùåå êîëè÷åñòâî àòàê: {activeEnemies.Sum(e => e.attackCooldown > 0 ? 1 : 0)}";
        }
        if (musicSource != null) musicSource.Stop();
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Èãðà çàêðûòà!");
    }

    public void UpgradeSword()
    {
        if (upgradePoints >= 5)
        {
            FindObjectOfType<BossController>().UpgradeWeapon("Sword", 5f);
            upgradePoints -= 5;
            upgradesPurchased++;
            StartNewWave();
        }
    }

    public void UpgradeArrow()
    {
        if (upgradePoints >= 5)
        {
            FindObjectOfType<BossController>().UpgradeWeapon("Arrow", 5f);
            upgradePoints -= 5;
            upgradesPurchased++;
            StartNewWave();
        }
    }

    public void IncreaseHealth()
    {
        if (upgradePoints >= 10)
        {
            FindObjectOfType<BossController>().IncreaseHealth(20f);
            upgradePoints -= 10;
            upgradesPurchased++;
            StartNewWave();
        }
    }

    public void BuyShield()
    {
        if (upgradePoints >= 15)
        {
            FindObjectOfType<BossController>().ActivateShield(50f);
            upgradePoints -= 15;
            upgradesPurchased++;
            StartNewWave();
        }
    }

    public void BuySecondPhase()
    {
        if (upgradePoints >= 20 && !FindObjectOfType<BossController>().HasSecondPhasePurchased())
        {
            FindObjectOfType<BossController>().PurchaseSecondPhase();
            upgradePoints -= 20;
            upgradesPurchased++;
            StartNewWave();
        }
    }

    public int GetCurrentWave()
    {
        return currentWave;
    }

    public void AddDamageToEnemies(float damage)
    {
        totalDamageToEnemies += damage;
    }

    public void AddDamageByClass(string className, float damage)
    {
        if (damageByClass.ContainsKey(className))
            damageByClass[className] += damage;
    }

    public void AddClericHealing(float healing)
    {
        totalClericHealing += healing;
    }

    public void BossDied() // Óñòàðåâøèé ìåòîä
    {
        BossDied("Íåèçâåñòíî", Time.time);
    }
}
