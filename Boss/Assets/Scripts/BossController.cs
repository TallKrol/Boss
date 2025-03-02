using UnityEngine;

public class BossController : MonoBehaviour
{
    private float speed = 5f;
    private float jumpForce = 8f;
    public float health = 130f;
    private float meleeDamage = 10f;
    private float chokeDamage = 15f;
    private float arrowDamage = 8f;
    private float bombDamage = 12f;
    private bool isGrounded = true;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    public GameObject arrowPrefab;
    public GameObject bombPrefab;

    public Sprite firstPhaseSprite;
    public Sprite secondPhaseSprite;

    private bool isSecondPhaseActive = false;
    private bool hasSecondPhase = false;
    private float shieldAmount = 0f;

    public LayerMask groundLayer;

    public AudioClip swordSound;
    public AudioClip chokeSound;
    public AudioClip arrowSound;
    public AudioClip bombSound;
    public AudioClip dashSound;
    private AudioSource audioSource;

    public ParticleSystem attackParticles;
    public ParticleSystem arrowParticles;
    public ParticleSystem bombParticles;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (rb == null || spriteRenderer == null || animator == null || audioSource == null)
        {
            Debug.LogError("Необходимые компоненты отсутствуют на Boss!");
        }
        spriteRenderer.sprite = firstPhaseSprite;
    }

    void Update()
    {
        if (health <= 0 && !isSecondPhaseActive && hasSecondPhase)
        {
            ActivateSecondPhase();
            return;
        }
        if (health <= 0)
        {
            animator.SetTrigger("Death");
            FindObjectOfType<GameManager>()?.BossDied();
            Destroy(gameObject, 0.5f);
            return;
        }

        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 0.6f, groundLayer);

        float moveInput = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(moveInput * speed, rb.velocity.y);

        if (moveInput != 0)
        {
            animator.SetBool("IsMoving", true);
            spriteRenderer.flipX = moveInput < 0;
        }
        else
        {
            animator.SetBool("IsMoving", false);
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            SwordAttack();
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            ChokeAttack();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            ArrowAttack();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            BombAttack();
        }
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            Dash();
        }
    }

    void SwordAttack()
    {
        animator.SetTrigger("Attack");
        if (swordSound != null && audioSource != null) audioSource.PlayOneShot(swordSound);
        if (attackParticles != null) attackParticles.Play();
        float radius = isSecondPhaseActive ? 3f : 2f;
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, radius);
        GameManager gm = FindObjectOfType<GameManager>();
        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.CompareTag("Enemy"))
            {
                EnemyController ec = enemy.GetComponent<EnemyController>();
                if (ec != null)
                {
                    ec.TakeDamage(meleeDamage);
                    gm?.AddDamageToEnemies(meleeDamage);
                }
            }
        }
        Debug.Log("Удар мечом!");
    }

    void ChokeAttack()
    {
        animator.SetTrigger("Attack");
        if (chokeSound != null && audioSource != null) audioSource.PlayOneShot(chokeSound);
        if (attackParticles != null) attackParticles.Play();
        float radius = isSecondPhaseActive ? 2f : 1.5f;
        Collider2D closestEnemy = Physics2D.OverlapCircle(transform.position, radius, LayerMask.GetMask("Enemy"));
        GameManager gm = FindObjectOfType<GameManager>();
        if (closestEnemy != null)
        {
            EnemyController ec = closestEnemy.GetComponent<EnemyController>();
            if (ec != null)
            {
                ec.TakeDamage(chokeDamage);
                gm?.AddDamageToEnemies(chokeDamage);
            }
        }
        Debug.Log("Удушье!");
    }

    void ArrowAttack()
    {
        animator.SetTrigger("Arrow");
        if (arrowSound != null && audioSource != null) audioSource.PlayOneShot(arrowSound);
        if (arrowParticles != null) arrowParticles.Play();
        if (arrowPrefab != null)
        {
            GameObject arrow = Instantiate(arrowPrefab, transform.position, Quaternion.identity);
            Arrow arrowScript = arrow.GetComponent<Arrow>();
            if (arrowScript != null)
            {
                arrowScript.damage = arrowDamage;
                if (isSecondPhaseActive) arrowScript.speed += 5f;
            }
        }
        Debug.Log("Выстрел стрелой!");
    }

    void BombAttack()
    {
        animator.SetTrigger("Bomb");
        if (bombSound != null && audioSource != null) audioSource.PlayOneShot(bombSound);
        if (bombParticles != null) bombParticles.Play();
        if (bombPrefab != null)
        {
            GameObject bomb = Instantiate(bombPrefab, transform.position + transform.up * 2f, Quaternion.identity);
            Bomb bombScript = bomb.GetComponent<Bomb>();
            if (bombScript != null)
            {
                bombScript.damage = bombDamage;
                if (isSecondPhaseActive) bombScript.explosionRadius += 1f;
            }
        }
        Debug.Log("Бросок бомбы!");
    }

    void Dash()
    {
        animator.SetTrigger("Dash");
        if (dashSound != null && audioSource != null) audioSource.PlayOneShot(dashSound);
        rb.velocity = new Vector2(rb.velocity.x * 2f, rb.velocity.y);
        Debug.Log("Рывок!");
    }

    public void TakeDamage(float damage)
    {
        if (shieldAmount > 0)
        {
            shieldAmount -= damage;
            if (shieldAmount < 0)
            {
                health += shieldAmount;
                shieldAmount = 0;
            }
        }
        else
        {
            health -= damage;
        }
        spriteRenderer.color = Color.red;
        Invoke("ResetColor", 0.1f);
    }

    void ResetColor()
    {
        spriteRenderer.color = Color.white;
    }

    void ActivateSecondPhase()
    {
        isSecondPhaseActive = true;
        health = 160f;
        speed += 2f;
        spriteRenderer.sprite = secondPhaseSprite;
        animator.SetTrigger("PhaseChange");
        FindObjectOfType<GameManager>()?.BossPhaseChanged();
        Debug.Log("Босс перешёл во вторую фазу!");
    }

    public void RestoreSecondPhase()
    {
        health = 160f;
        isSecondPhaseActive = true;
        spriteRenderer.sprite = secondPhaseSprite;
    }

    public bool IsInSecondPhase()
    {
        return isSecondPhaseActive;
    }

    public bool HasSecondPhasePurchased()
    {
        return hasSecondPhase;
    }

    public void UpgradeWeapon(string weapon, float amount)
    {
        if (weapon == "Sword") meleeDamage += amount;
        else if (weapon == "Arrow") arrowDamage += amount;
        Debug.Log($"{weapon} улучшен до {amount} урона!");
    }

    public void IncreaseHealth(float amount)
    {
        health += amount;
        Debug.Log($"Здоровье босса увеличено на {amount}!");
    }

    public void ActivateShield(float amount)
    {
        shieldAmount = amount;
        Debug.Log($"Щит активирован на {amount}!");
    }

    public void PurchaseSecondPhase()
        {
            hasSecondPhase = true;
            Debug.Log("Вторая фаза куплена!");
        }

        public void SlowDown(float amount, float duration)
        {
            speed -= amount;
            Invoke("ResetSpeed", duration);
        }

        void ResetSpeed()
        {
            speed = isSecondPhaseActive ? 7f : 5f;
        }
}
