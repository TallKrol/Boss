using UnityEngine;

public class Bomb : MonoBehaviour
{
    public float damage;
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
    }
}