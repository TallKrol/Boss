using UnityEngine;

public class FireField : MonoBehaviour
{
    public float damage = 5f;
    public float radius = 1f;
    private float lifetime = 5f;
    private float nextDamageTime = 0f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (Time.time >= nextDamageTime)
        {
            Collider2D[] hit = Physics2D.OverlapCircleAll(transform.position, radius);
            foreach (Collider2D obj in hit)
            {
                if (obj.CompareTag("Boss")) obj.GetComponent<BossController>().TakeDamage(damage);
            }
            nextDamageTime = Time.time + 1f; // ”рон раз в секунду
        }
    }
}