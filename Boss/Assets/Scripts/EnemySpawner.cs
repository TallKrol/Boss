using UnityEngine;
using System;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    private string[] classes = { "Berserker", "Sniper", "Pyromancer", "Cleric", "Guardian" };
    public event Action<EnemyController> OnEnemySpawned;

    void Start()
    {
        for (int i = 0; i < 5; i++) SpawnEnemy();
    }

    void SpawnEnemy()
    {
        Vector2 spawnPosition = UnityEngine.Random.insideUnitCircle.normalized * 10f;
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        EnemyController enemyController = enemy.GetComponent<EnemyController>();
        enemyController.SetClass(classes[UnityEngine.Random.Range(0, classes.Length)]);
        OnEnemySpawned?.Invoke(enemyController);
    }
}