using UnityEngine;

public class BossController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float health = 130f;
    private float activeShield = 0f;
    private Rigidbody2D rb;

    public GameObject arrowPrefab;
    public GameObject bombPrefab;

    private float meleeDamage = 18f;
    private float chokeDamage = 28f;
    private float arrowDamage = 14f;
    private float bombDamage = 20f;

    private bool hasSecondPhase = false;
    public bool isSecondPhaseActive = false;
    private bool hasSecondLife = false;
    private float invulnerabilityTime = 0f;

    private float dashDistance = 3f;
    private float dashCooldown = 3.5f;
    private float nextDashTime = 0f;
    private float dashFeedbackTime = 0f;

    private SpriteRenderer spriteRenderer;
    public Sprite firstPhaseSprite;
    public Sprite secondPhaseSprite;
    private Animator animator;

    public AudioClip dashSound;
    public AudioClip swordSound;
    public AudioClip chokeSound;
    public AudioClip arrowSound;
    public AudioClip bombSound;
    public AudioClip phaseChangeSound;
    private AudioSource audioSource;

    public ParticleSystem dashParticles;
    public ParticleSystem attackParticles;
    public ParticleSystem arrowParticles;
    public ParticleSystem bombParticles;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (spriteRenderer == null) Debug.LogError("SpriteRenderer не найден на боссе!");
        if (animator == null) animator = gameObject.AddComponent<Animator>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        spriteRenderer.sprite = firstPhaseSprite;
        animator.SetTrigger("Idle");

        if (dashParticles != null) dashParticles.Stop();
        if (attackParticles != null) attackParticles.Stop();
        if (arrowParticles != null) arrowParticles.Stop();
        if (bombParticles != null) bombParticles.Stop();
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        Vector2 movement = new Vector2(moveX, moveY).normalized;

        if (movement.magnitude > 0)
        {
            rb.velocity = movement * moveSpeed;
            animator.SetBool("IsMoving", true);
        }
        else
        {
            rb.velocity = Vector2.zero;
            animator.SetBool("IsMoving", false);
        }

        if (Input.GetKeyDown(KeyCode.Q)) SwordAttack();
        if (Input.GetKeyDown(KeyCode.W)) ChokeAttack();
        if (Input.GetKeyDown(KeyCode.E)) ArrowAttack();
        if (Input.GetKeyDown(KeyCode.R)) BombAttack();
        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= nextDashTime) Dash(movement);

        if (invulnerabilityTime > 0)
        {
            invulnerabilityTime -= Time.deltaTime;
            spriteRenderer.color = Color.Lerp(Color.white, Color.red, Mathf.PingPong(Time.time * 5, 1));
        }
        else if (dashFeedbackTime > 0)
        {
            dashFeedbackTime -= Time.deltaTime;
            spriteRenderer.color = Color.Lerp(Color.white, Color.blue, Mathf.PingPong(Time.time * 10, 1));
        }
        else
        {
            spriteRenderer.color = Color.white;
            spriteRenderer.sprite = isSecondPhaseActive ? secondPhaseSprite : firstPhaseSprite;
        }
    }

    void SwordAttack()
    {
        animator.SetTrigger("Attack");
        if (swordSound != null) audioSource.PlayOneShot(swordSound);
        if (attackParticles != null) attackParticles.Play();
        float radius = isSecondPhaseActive ? 3f : 2f;
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.CompareTag("Enemy")) enemy.GetComponent<EnemyController>().TakeDamage(meleeDamage);
        }
        Debug.Log("Удар мечом!");
    }

void ChokeAttack()
    {
        animator.SetTrigger("Attack");
        if (chokeSound != null) audioSource.PlayOneShot(chokeSound);
        if (attackParticles != null) attackParticles.Play();
        float radius = isSecondPhaseActive ? 2f : 1.5f;
        Collider2D closestEnemy = Physics2D.OverlapCircle(transform.position, radius, LayerMask.GetMask("Enemy"));
        if (closestEnemy != null) closestEnemy.GetComponent<EnemyController>().TakeDamage(chokeDamage);
        Debug.Log("Удушье!");
    }

    void ArrowAttack()
    {
        animator.SetTrigger("Arrow");
        if (arrowSound != null) audioSource.PlayOneShot(arrowSound);
        if (arrowParticles != null) arrowParticles.Play();
        GameObject arrow = Instantiate(arrowPrefab, transform.position, Quaternion.identity);
        arrow.GetComponent<Arrow>().damage = arrowDamage;
        if (isSecondPhaseActive) arrow.GetComponent<Arrow>().speed += 5f;
        Debug.Log("Выстрел стрелой!");
    }

    void BombAttack()
    {
        animator.SetTrigger("Bomb");
        if (bombSound != null) audioSource.PlayOneShot(bombSound);
        if (bombParticles != null) bombParticles.Play();
        GameObject bomb = Instantiate(bombPrefab, transform.position + transform.up * 2f, Quaternion.identity);
        bomb.GetComponent<Bomb>().damage = bombDamage;
        if (isSecondPhaseActive) bomb.GetComponent<Bomb>().explosionRadius += 1f;
        Debug.Log("Бросок бомбы!");
    }

    void Dash(Vector2 direction)
    {
        if (direction.magnitude > 0)
        {
            animator.SetTrigger("Dash");
            rb.MovePosition(rb.position + direction * dashDistance);
            nextDashTime = Time.time + dashCooldown;
            dashFeedbackTime = 0.3f;
            if (dashSound != null && audioSource != null) audioSource.PlayOneShot(dashSound);
            if (dashParticles != null) dashParticles.Play();
            Debug.Log("Босс сделал рывок!");
        }
    }

    public void TakeDamage(float damage)
    {
        if (invulnerabilityTime > 0) return;

        if (activeShield > 0)
        {
            activeShield -= damage;
            if (activeShield < 0) activeShield = 0;
            return;
        }
        health -= damage;
        if (health <= 0)
        {
            if (hasSecondPhase && hasSecondLife)
            {
                ActivateSecondPhase();
                GameManager gm = FindObjectOfType<GameManager>();
                if (gm != null) gm.BossPhaseChanged();
            }
            else
            {
                animator.SetTrigger("Death");
                GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().BossDied();
                Destroy(gameObject, 0.5f);
            }
        }
    }

    public void UpgradeWeapon(string weapon, float boost)
    {
        if (weapon == "Sword") meleeDamage += boost;
        else if (weapon == "Choke") chokeDamage += boost;
        else if (weapon == "Arrow") arrowDamage += boost;
        else if (weapon == "Bomb") bombDamage += boost;
        Debug.Log($"{weapon} улучшен! Новый урон: {meleeDamage}/{chokeDamage}/{arrowDamage}/{bombDamage}");
    }

    public void IncreaseHealth(float amount)
    {
        health += amount;
        Debug.Log($"Здоровье босса увеличено на {amount}. Текущее здоровье: {health}");
    }

    public void ActivateShield(float amount)
    {
        activeShield = amount;
        Debug.Log($"Щит активирован на {amount}!");
    }

    public void PurchaseSecondPhase()
    {
        hasSecondPhase = true;
        hasSecondLife = true;
        Debug.Log("Вторая фаза куплена! Теперь доступна для каждой волны.");
    }

    public bool HasSecondPhasePurchased()
    {
        return hasSecondPhase;
    }

    public void RestoreSecondPhase()
    {
        if (hasSecondPhase)
        {
            hasSecondLife = true;
            isSecondPhaseActive = false;
            Debug.Log("Вторая фаза восстановлена для следующей волны!");
        }
    }

void ActivateSecondPhase()
    {
        animator.SetTrigger("PhaseChange");
        if (phaseChangeSound != null) audioSource.PlayOneShot(phaseChangeSound);
        isSecondPhaseActive = true;
        hasSecondLife = false;
        health = 160f;
        meleeDamage *= 1.4f;
        chokeDamage *= 1.4f;
        arrowDamage *= 1.4f;
        bombDamage *= 1.4f;
        moveSpeed += 2f;
        invulnerabilityTime = 3f;
        Debug.Log("Босс перешёл во вторую фазу! Урон, скорость и радиус атак увеличены, временная неуязвимость!");
    }

    public void SlowDown(float reduction, float duration)
    {
        if (!isSecondPhaseActive)
        {
            moveSpeed -= reduction;
            Invoke("ResetSpeed", duration);
        }
    }

    void ResetSpeed()
    {
        moveSpeed = isSecondPhaseActive ? 7f : 5f;
    }

    public bool IsInSecondPhase()
    {
        return isSecondPhaseActive;
    }
}
