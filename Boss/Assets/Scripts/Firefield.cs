using UnityEngine;

public class FireField : MonoBehaviour
{
    public float damagePerSecond = 2f; // Урон в секунду
    private float lifetime = 3f;        // Время жизни огненного поля

    void Start()
    {
        Destroy(gameObject, lifetime); // Уничтожение через 3 секунды
    }

    void OnTriggerStay2D(Collider2D other)
    {
        GameManager gm = FindObjectOfType<GameManager>();
        if (other.CompareTag("Enemy"))
        {
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(damagePerSecond * Time.deltaTime);
            }
        }
        else if (other.CompareTag("Boss"))
        {
            BossController boss = other.GetComponent<BossController>();
            if (boss != null)
            {
                boss.TakeDamage(damagePerSecond * Time.deltaTime);
                if (gm != null) gm.AddDamageToEnemies(damagePerSecond * Time.deltaTime);
            }
        }
    }
}