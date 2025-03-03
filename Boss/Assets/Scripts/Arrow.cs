using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float speed = 8f;
    public float damage = 15f;

    void Update()
    {
        transform.Translate(Vector2.up * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            other.GetComponent<EnemyController>().TakeDamage(damage);
            Destroy(gameObject);
        }

        if (other.CompareTag("Boss"))
        {
            other.GetComponent<BossController>().TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}