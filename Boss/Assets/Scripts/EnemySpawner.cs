using UnityEngine;
using System;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public GameObject[] enemyPrefabs; // 0 - Berserker, 1 - Sniper, 2 - Pyromancer, 3 - Cleric, 4 - Guardian, 5 - Bard
    private string[] classes = { "Berserker", "Sniper", "Pyromancer", "Cleric", "Guardian", "Bard" };
    public event Action<EnemyController> OnEnemySpawned;
    private Dictionary<string, string> classNicknames;

    void Start()
    {
        if (classNicknames == null)
        {
            Debug.LogError("Никнеймы не установлены перед спавном!");
            return;
        }
        for (int i = 0; i < 6; i++) SpawnEnemy(i);
    }

    public void SetNicknames(Dictionary<string, string> nicknames)
    {
        classNicknames = nicknames;
    }

    void SpawnEnemy(int classIndex)
    {
        Vector2 spawnPosition = UnityEngine.Random.insideUnitCircle.normalized * 10f;
        GameObject enemy = Instantiate(enemyPrefabs[classIndex], spawnPosition, Quaternion.identity);
        EnemyController enemyController = enemy.GetComponent<EnemyController>();
        enemyController.SetClass(classes[classIndex]);
        enemyController.SetNickname(classNicknames[classes[classIndex]]); // Ник теперь задаётся первым
        OnEnemySpawned?.Invoke(enemyController);
    }
}