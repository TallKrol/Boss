using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;       // Скорость снаряда
    public float damage = 10f;      // Урон от снаряда
    public bool friendlyFire = false; // Флаг дружественного огня
    public Transform target;        // Цель (если null, движется по направлению)
    public Vector2 direction;       // Направление движения (если нет цели)
    public bool isAoE = false;      // Является ли снаряд AoE (для Пироманта)
    public float explosionRadius = 2f; // Радиус взрыва для AoE
    public bool leaveFireField = false; // Оставляет ли огненное поле (для Пироманта)
    public GameObject fireFieldPrefab; // Префаб огненного поля

    private float lifetime = 5f;    // Время жизни снаряда

    void Start()
    {
        Destroy(gameObject, lifetime); // Уничтожение через 5 секунд, если не попал
    }

    void Update()
    {
        if (target != null)
        {
            // Движение к цели
            direction = (target.position - transform.position).normalized;
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
        }
        else
        {
            // Движение по заданному направлению
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        GameManager gm = FindObjectOfType<GameManager>();
        if (other.CompareTag("Boss") && !friendlyFire)
        {
            BossController boss = other.GetComponent<BossController>();
            if (boss != null)
            {
                boss.TakeDamage(damage);
                if (gm != null) gm.AddDamageToEnemies(damage);

                if (isAoE)
                {
                    Explode();
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
        else if (other.CompareTag("Enemy") && friendlyFire)
        {
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage, true); // Дружественный огонь
                if (gm != null) gm.AddDamageToEnemies(damage);

                if (isAoE)
                {
                    Explode();
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    void Explode()
    {
        Collider2D[] hitTargets = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        GameManager gm = FindObjectOfType<GameManager>();
        foreach (Collider2D hit in hitTargets)
        {
            if (hit.CompareTag("Boss") && !friendlyFire)
            {
                BossController boss = hit.GetComponent<BossController>();
                if (boss != null)
                {
                    boss.TakeDamage(damage);
                    if (gm != null) gm.AddDamageToEnemies(damage);
                }
            }
            else if (hit.CompareTag("Enemy") && friendlyFire)
            {
                EnemyController enemy = hit.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage, true);
                    if (gm != null) gm.AddDamageToEnemies(damage);
                }
            }
        }

        if (leaveFireField && fireFieldPrefab != null)
        {
            Instantiate(fireFieldPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
    }
}