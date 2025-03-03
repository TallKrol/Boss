using UnityEngine;

public class Bomb : MonoBehaviour
{
    public float damage = 10f;
    public float explosionRadius = 3f;
    private float lifetime = 2f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            foreach (Collider2D enemy in hitEnemies)
            {
                if (enemy.CompareTag("Enemy")) enemy.GetComponent<EnemyController>().TakeDamage(damage);
            }
            Destroy(gameObject);
        }

        if (other.CompareTag("Boss"))
        {
            Collider2D[] hitBoss = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            foreach (Collider2D boss in hitBoss)
            {
                if (boss.CompareTag("Boss")) boss.GetComponent<BossController>().TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
}