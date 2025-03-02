using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;       // �������� �������
    public float damage = 10f;      // ���� �� �������
    public bool friendlyFire = false; // ���� �������������� ����
    public Transform target;        // ���� (���� null, �������� �� �����������)
    public Vector2 direction;       // ����������� �������� (���� ��� ����)
    public bool isAoE = false;      // �������� �� ������ AoE (��� ���������)
    public float explosionRadius = 2f; // ������ ������ ��� AoE
    public bool leaveFireField = false; // ��������� �� �������� ���� (��� ���������)
    public GameObject fireFieldPrefab; // ������ ��������� ����

    private float lifetime = 5f;    // ����� ����� �������

    void Start()
    {
        Destroy(gameObject, lifetime); // ����������� ����� 5 ������, ���� �� �����
    }

    void Update()
    {
        if (target != null)
        {
            // �������� � ����
            direction = (target.position - transform.position).normalized;
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
        }
        else
        {
            // �������� �� ��������� �����������
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
                enemy.TakeDamage(damage, true); // ������������� �����
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