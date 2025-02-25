using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 8f;
    public float damage;
    public bool isAoE = false;
    public bool leaveFireField = false;
    public float fireFieldRadius = 1f;
    public Transform target;
    private Vector2 direction;

    void Start()
    {
        if (target == null)
        {
            direction = transform.up;
        }
    }

    void Update()
    {
        if (target != null)
        {
            transform.position += (target.position - transform.position).normalized * speed * Time.deltaTime;
        }
        else
        {
            transform.Translate(direction * speed * Time.deltaTime);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Boss"))
        {
            if (isAoE)
            {
                Collider2D[] hit = Physics2D.OverlapCircleAll(transform.position, 2f);
                foreach (Collider2D obj in hit)
                {
                    if (obj.CompareTag("Boss")) obj.GetComponent<BossController>().TakeDamage(damage);
                }
            }
            else
            {
                other.GetComponent<BossController>().TakeDamage(damage);
            }
            if (leaveFireField)
            {
                GameObject fireField = new GameObject("FireField");
                fireField.transform.position = transform.position;
                fireField.AddComponent<FireField>().damage = 5f;
                fireField.GetComponent<FireField>().radius = fireFieldRadius;
            }
            Destroy(gameObject);
        }
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir;
    }
}