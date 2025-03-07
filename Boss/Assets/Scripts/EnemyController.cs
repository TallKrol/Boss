using UnityEngine;
using System;

public class EnemyController : MonoBehaviour
{
    public float speed = 4.4f;
    public float jumpForce = 5f;
    public float health = 63f;
    private Transform target;
    public string enemyClass;
    private string playerNickname;
    private Rigidbody2D rb;
    private bool isGrounded = true;

    public GameObject projectilePrefab;
    public float attackCooldown = 2f;
    private float nextAttackTime = 0f;

    public LayerMask groundLayer;
    public LayerMask obstacleLayer;

    private float preferredDistance = 5f;
    private bool isRangedClass => enemyClass == "Sniper" || enemyClass == "Pyromancer" || enemyClass == "Bard";

    private float dodgeCooldown = 1f;
    private float nextDodgeTime = 0f;

    private float idleTime = 0f;
    private bool isIdling = false;
    private bool recentlyAttacked = false;

    private float stumbleChance = 0.001f;
    private float missChance = 0.015f;

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private int healingPotions = 0;
    private bool hasUsedPotion = false;
    public GameObject potionEffectPrefab;
    private float lastBombCheckTime = 0f;
    private float bombCheckInterval = 0.5f;
    private float deathAnimationTime = 0f;

    public AudioClip potionSound;
    public AudioClip deathSound;
    public AudioClip attackSound;
    private AudioSource audioSource;

    public ParticleSystem deathParticles;
    public ParticleSystem bardBuffParticles;
    public ParticleSystem clericShockParticles;

    private EnemyChat enemyChat;
    private float nextRandomChatTime = 0f;

    [SerializeField]
    private bool friendlyFire = true;

    public event Action<EnemyController> OnDeath;

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Boss").transform;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        enemyChat = FindObjectOfType<EnemyChat>();
        if (spriteRenderer == null) Debug.LogError("SpriteRenderer �� ������ �� " + enemyClass);
        if (animator == null) animator = gameObject.AddComponent<Animator>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        if (playerNickname == null) playerNickname = "Unknown"; // ��������� ��������
        animator.SetTrigger("Idle");

        if (deathParticles != null) deathParticles.Stop();
        if (bardBuffParticles != null && enemyClass != "Bard") bardBuffParticles.Stop();
        if (clericShockParticles != null && enemyClass != "Cleric") clericShockParticles.Stop();

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null && gm.GetCurrentWave() >= 4)
        {
            healingPotions = 1;
        }
        nextRandomChatTime = Time.time + UnityEngine.Random.Range(5f, 15f);
    }

    void Update()
    {
        if (deathAnimationTime > 0)
        {
            deathAnimationTime -= Time.deltaTime;
            spriteRenderer.color = Color.Lerp(Color.white, Color.black, Mathf.PingPong(Time.time * 5, 1));
            if (deathAnimationTime <= 0)
            {
                if (deathParticles != null) deathParticles.Play();
                if (enemyChat != null)
                {
                    string[] deathMessages = {
                        $"{playerNickname} [{enemyClass}]: �� ��, ���������...",
                        $"{playerNickname} [{enemyClass}]: ����, �� #####, �� � ����!",
                        $"{playerNickname} [{enemyClass}]: �����, � �����...",
                        $"{playerNickname} [{enemyClass}]: ��� �����, ������...",
                        $"{playerNickname} [{enemyClass}]: ��, ������ ����...",
                        $"{playerNickname} [{enemyClass}]: ������, �������� ���!",
                        $"{playerNickname} [{enemyClass}]: ��, � � ����...",
                        $"{playerNickname} [{enemyClass}]: ���-�� ���� ��������?",
                        $"{playerNickname} [{enemyClass}]: ���� ##### ������� ����!",
                        $"{playerNickname} [{enemyClass}]: � ��� ��������...",
                        $"{playerNickname} [{enemyClass}]: �� �����, ������ ����!",
                        $"{playerNickname} [{enemyClass}]: ����� ��� �������...",
                        $"{playerNickname} [{enemyClass}]: �� � �����, �������...",
                        $"{playerNickname} [{enemyClass}]: ����, �� �������, #####...",
                        $"{playerNickname} [{enemyClass}]: ��, ��� #####...",
                        $"{playerNickname} [{enemyClass}]: ������ � ����, ����!",
                        $"{playerNickname} [{enemyClass}]: ��������� ������, ����!",
                        $"{playerNickname} [{enemyClass}]: ������ �������, ��..."
                    };
                enemyChat.AddMessage(deathMessages[UnityEngine.Random.Range(0, deathMessages.Length)]);
            }
            Destroy(gameObject);
        }
        return;
    }

        if (!isIdling)
        {
            MoveSmartly();
    TryJumpToPlatform();
    TryDodge();
    CheckForStumble();
    TryUseHealingPotion();
    CheckForBombAvoidance();
}
        else
{
    rb.velocity = new Vector2(rb.velocity.x * 0.9f, rb.velocity.y);
    idleTime -= Time.deltaTime;
    if (idleTime <= 0) isIdling = false;
}

CheckForIdle();

if (Time.time >= nextRandomChatTime && enemyChat != null)
{
    string[] randomMessages = GetRandomMessages();
    enemyChat.AddMessage(randomMessages[UnityEngine.Random.Range(0, randomMessages.Length)]);
    nextRandomChatTime = Time.time + UnityEngine.Random.Range(10f, 20f);
}

if (Time.time >= nextAttackTime)
{
    recentlyAttacked = true;
    if (UnityEngine.Random.value > missChance)
    {
        animator.SetTrigger("Attack");
        if (attackSound != null) audioSource.PlayOneShot(attackSound);
        switch (enemyClass)
        {
            case "Berserker":
                BerserkerRage(false);
                if (enemyChat != null)
                {
                    string[] berserkerAttackMessages = {
                                $"{playerNickname} [{enemyClass}]: ������� �� �����!",
                                $"{playerNickname} [{enemyClass}]: ������� ���� � ����!",
                                $"{playerNickname} [{enemyClass}]: �� �������, �������!",
                                $"{playerNickname} [{enemyClass}]: ��� ��� �������? � �����!",
                                $"{playerNickname} [{enemyClass}]: ��� ��� ����!",
                                $"{playerNickname} [{enemyClass}]: ������� ����, ����!",
                                $"{playerNickname} [{enemyClass}]: ����� ��� � ����!",
                                $"{playerNickname} [{enemyClass}]: �����, � ��� ����!",
                                $"{playerNickname} [{enemyClass}]: �� ����� �������!",
                                $"{playerNickname} [{enemyClass}]: �� ����� ����!",
                                $"{playerNickname} [{enemyClass}]: ������, ��� �� ������!",
                                $"{playerNickname} [{enemyClass}]: ���� ���, ������!",
                                $"{playerNickname} [{enemyClass}]: ������� � ��� �����!",
                                $"{playerNickname} [{enemyClass}]: ������� ��������!",
                                $"{playerNickname} [{enemyClass}]: #####, ����� ����!",
                                $"{playerNickname} [{enemyClass}]: �� ���� � �������!",
                                $"{playerNickname} [{enemyClass}]: �������� �������!",
                                $"{playerNickname} [{enemyClass}]: ����� ������ ���!"
                            };
                enemyChat.AddMessage(berserkerAttackMessages[UnityEngine.Random.Range(0, berserkerAttackMessages.Length)]);
        }
        break;
                    case "Sniper":
                        SniperShot(false);
        if (enemyChat != null)
        {
            string[] sniperAttackMessages = {
                                $"{playerNickname} [{enemyClass}]: �����, ������ �����!",
                                $"{playerNickname} [{enemyClass}]: ������ ���� � ��������!",
                                $"{playerNickname} [{enemyClass}]: ���� �������, ����!",
                                $"{playerNickname} [{enemyClass}]: ����� � ����, ��!",
                                $"{playerNickname} [{enemyClass}]: �� �������, ���������!",
                                $"{playerNickname} [{enemyClass}]: �� ����� ������!",
                                $"{playerNickname} [{enemyClass}]: �� ���� �� ����������!",
                                $"{playerNickname} [{enemyClass}]: ������� �� ���-���!",
                                $"{playerNickname} [{enemyClass}]: ���������, ���!",
                                $"{playerNickname} [{enemyClass}]: ��� �����, �������!",
                                $"{playerNickname} [{enemyClass}]: � �������, �����!",
                                $"{playerNickname} [{enemyClass}]: �� ��������� ����!",
                                $"{playerNickname} [{enemyClass}]: �� ����, ������!",
                                $"{playerNickname} [{enemyClass}]: ������ ��� � ����!",
                                $"{playerNickname} [{enemyClass}]: #####, �����-����!",
                                $"{playerNickname} [{enemyClass}]: �� � ����� ��������!",
                                $"{playerNickname} [{enemyClass}]: ���� ������, ���!",
                                $"{playerNickname} [{enemyClass}]: ������ �������, �����!"
                            };
            enemyChat.AddMessage(sniperAttackMessages[UnityEngine.Random.Range(0, sniperAttackMessages.Length)]);
        }
        break;
                    case "Pyromancer":
                        PyroBlast(false);
        if (enemyChat != null)
        {
            string[] pyroAttackMessages = {
                                $"{playerNickname} [{enemyClass}]: � ���������, ������!",
                                $"{playerNickname} [{enemyClass}]: ����� ���� � �����!",
                                $"{playerNickname} [{enemyClass}]: �� ��������, �������!",
                                $"{playerNickname} [{enemyClass}]: ���, ���� �����!",
                                $"{playerNickname} [{enemyClass}]: ����� ��� �����!",
                                $"{playerNickname} [{enemyClass}]: ����-���� ����!",
                                $"{playerNickname} [{enemyClass}]: ������� ��� ������!",
                                $"{playerNickname} [{enemyClass}]: ��� ���� ���������!",
                                $"{playerNickname} [{enemyClass}]: ����� �����, ����!",
                                $"{playerNickname} [{enemyClass}]: �� ������� �����!",
                                $"{playerNickname} [{enemyClass}]: ����� � ��� �����!",
                                $"{playerNickname} [{enemyClass}]: ����� � �����������!",
                                $"{playerNickname} [{enemyClass}]: �������, ���������!",
                                $"{playerNickname} [{enemyClass}]: �� ��� ������ �� #####!",
                                $"{playerNickname} [{enemyClass}]: #####, ���� ����!",
                                $"{playerNickname} [{enemyClass}]: �� �������� �� ������!",
                                $"{playerNickname} [{enemyClass}]: ����� � ����, �������!",
                                $"{playerNickname} [{enemyClass}]: ����, ���� �� �������!"
                            };
        enemyChat.AddMessage(pyroAttackMessages[UnityEngine.Random.Range(0, pyroAttackMessages.Length)]);
    }
    break;
                    case "Cleric":
                        ClericHeal(false);
    if (enemyChat != null)
    {
        string[] clericAttackMessages = {
                                $"{playerNickname} [{enemyClass}]: ����, �� ������!",
                                $"{playerNickname} [{enemyClass}]: �� ��������, �������!",
                                $"{playerNickname} [{enemyClass}]: ����, �� ���������!",
                                $"{playerNickname} [{enemyClass}]: ����, � ������!",
                                $"{playerNickname} [{enemyClass}]: �� ###, �������!",
                                $"{playerNickname} [{enemyClass}]: ����� ����, ����!",
                                $"{playerNickname} [{enemyClass}]: ���� � ������, ������!",
                                $"{playerNickname} [{enemyClass}]: �� ��� ����� ��� �����!",
                                $"{playerNickname} [{enemyClass}]: �� �����, � �����!",
                                $"{playerNickname} [{enemyClass}]: ������� � ����!",
                                $"{playerNickname} [{enemyClass}]: ������ ����, ����!",
                                $"{playerNickname} [{enemyClass}]: �� ������� ����!",
                                $"{playerNickname} [{enemyClass}]: �� �����, � ���!",
                                $"{playerNickname} [{enemyClass}]: ��������, �������!",
                                $"{playerNickname} [{enemyClass}]: #####, �������!",
                                $"{playerNickname} [{enemyClass}]: �� ���� �������!",
                                $"{playerNickname} [{enemyClass}]: ����, �� ���!",
                                $"{playerNickname} [{enemyClass}]: ���� ������, ������!"
                            };
        enemyChat.AddMessage(clericAttackMessages[UnityEngine.Random.Range(0, clericAttackMessages.Length)]);
    }
    break;
                    case "Guardian":
                        GuardianTaunt(false);
    if (enemyChat != null)
    {
        string[] guardianAttackMessages = {
                                $"{playerNickname} [{enemyClass}]: ��� ������, ���!",
                                $"{playerNickname} [{enemyClass}]: � ���� ����, ������!",
                                $"{playerNickname} [{enemyClass}]: ����, �� ��� �� ��������!",
                                $"{playerNickname} [{enemyClass}]: ���� �� ����, �� ###!",
                                $"{playerNickname} [{enemyClass}]: �� ����� �������!",
                                $"{playerNickname} [{enemyClass}]: ����� �����, ���� ���!",
                                $"{playerNickname} [{enemyClass}]: � �����, ��������!",
                                $"{playerNickname} [{enemyClass}]: ��� ����� ����, �����!",
                                $"{playerNickname} [{enemyClass}]: ��� � ����, �� �����!",
                                $"{playerNickname} [{enemyClass}]: � ��� �����, ����!",
                                $"{playerNickname} [{enemyClass}]: ���������, ���� ���!",
                                $"{playerNickname} [{enemyClass}]: �� ������� ��� �����!",
                                $"{playerNickname} [{enemyClass}]: ��� ��� ����? � ����!",
                                $"{playerNickname} [{enemyClass}]: �� �������, �����!",
                                $"{playerNickname} [{enemyClass}]: #####, � ����������!",
                                $"{playerNickname} [{enemyClass}]: ��� �� �����, ���!",
                                $"{playerNickname} [{enemyClass}]: � �����, ������, �����!",
                                $"{playerNickname} [{enemyClass}]: ����, �� ��� �� ���������!"
                            };
    enemyChat.AddMessage(guardianAttackMessages[UnityEngine.Random.Range(0, guardianAttackMessages.Length)]);
}
break;
                    case "Bard":
                        BardSong(false);
if (enemyChat != null)
{
    string[] bardAttackMessages = {
                                $"{playerNickname} [{enemyClass}]: ����� ��������, �����!",
                                $"{playerNickname} [{enemyClass}]: �� �������, �������!",
                                $"{playerNickname} [{enemyClass}]: ������ � ����, ������!",
                                $"{playerNickname} [{enemyClass}]: ����, ���� ���� � ����!",
                                $"{playerNickname} [{enemyClass}]: �����, �� ###!",
                                $"{playerNickname} [{enemyClass}]: ����� �����, �����!",
                                $"{playerNickname} [{enemyClass}]: �� ����� ����, �����!",
                                $"{playerNickname} [{enemyClass}]: ��� ������ ���!",
                                $"{playerNickname} [{enemyClass}]: ������� � ��� �����!",
                                $"{playerNickname} [{enemyClass}]: �������� ��� ����!",
                                $"{playerNickname} [{enemyClass}]: ������ � ��� ����!",
                                $"{playerNickname} [{enemyClass}]: �� ����� ��� ����!",
                                $"{playerNickname} [{enemyClass}]: ����� ��� ���, �������!",
                                $"{playerNickname} [{enemyClass}]: �����, ����, �����!",
                                $"{playerNickname} [{enemyClass}]: #####, ��� ��� ������!",
                                $"{playerNickname} [{enemyClass}]: �� ��� ��������!",
                                $"{playerNickname} [{enemyClass}]: ������ � ����, ���� ���!",
                                $"{playerNickname} [{enemyClass}]: ��� ��� ��� ���� �����!"
                            };
    enemyChat.AddMessage(bardAttackMessages[UnityEngine.Random.Range(0, bardAttackMessages.Length)]);
}
break;
                }
            }
            else
{
    Debug.Log($"{playerNickname} [{enemyClass}] �����������!");
    if (enemyChat != null)
    {
        string[] missMessages = {
                        $"{playerNickname} [{enemyClass}]: �� � ��� � ��������?",
                        $"{playerNickname} [{enemyClass}]: ׸ �� �����, ����!",
                        $"{playerNickname} [{enemyClass}]: ���� ���� ����, ������!",
                        $"{playerNickname} [{enemyClass}]: �� �����, ����� ����?",
                        $"{playerNickname} [{enemyClass}]: �� �� � �������!",
                        $"{playerNickname} [{enemyClass}]: �� �����, �� � ��� � ���!",
                        $"{playerNickname} [{enemyClass}]: ���� ������, ����!",
                        $"{playerNickname} [{enemyClass}]: �� �� �����, �� ���!",
                        $"{playerNickname} [{enemyClass}]: �����-�� ���� �������!",
                        $"{playerNickname} [{enemyClass}]: �� � #####, �� �����!",
                        $"{playerNickname} [{enemyClass}]: �����, ���� �����!",
                        $"{playerNickname} [{enemyClass}]: ׸ �� ��������?",
                        $"{playerNickname} [{enemyClass}]: ����, �� � �����!",
                        $"{playerNickname} [{enemyClass}]: �� ����������, ���!",
                        $"{playerNickname} [{enemyClass}]: �������� ��� �����!",
                        $"{playerNickname} [{enemyClass}]: �� �� � ����, � �� �����!",
                        $"{playerNickname} [{enemyClass}]: �����������, �� � ���� � ���!"
                    };
    enemyChat.AddMessage(missMessages[UnityEngine.Random.Range(0, missMessages.Length)]);
}
            }
            GameManager gm = FindObjectOfType<GameManager>();
int wave = gm != null ? gm.GetCurrentWave() : 1;
nextAttackTime = Time.time + (wave >= 7 && isRangedClass ? attackCooldown * 0.75f : attackCooldown) + UnityEngine.Random.Range(-0.5f, 0.5f);
Invoke("ResetAttackFlag", 0.5f);
        }
    }

    void MoveSmartly()
{
    Vector2 direction = (target.position - transform.position).normalized;
    float distanceToBoss = Vector2.Distance(transform.position, target.position);
    Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * 0.2f;
    direction += randomOffset;

    isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 0.6f, groundLayer);

    GameManager gm = FindObjectOfType<GameManager>();
    int wave = gm != null ? gm.GetCurrentWave() : 1;
    BossController boss = FindObjectOfType<BossController>();
    bool isBossInSecondPhase = boss != null && boss.IsInSecondPhase();

    if (rb.velocity.magnitude > 0)
    {
        animator.SetBool("IsMoving", true);
    }
    else
    {
        animator.SetBool("IsMoving", false);
    }

    if (isBossInSecondPhase)
    {
        if (enemyClass == "Berserker")
        {
            Vector2 flankDirection = Vector2.Perpendicular(direction) * (UnityEngine.Random.value > 0.5f ? 1 : -1);
            rb.velocity = new Vector2(flankDirection.x * speed, rb.velocity.y);
        }
        else if (enemyClass == "Sniper" || enemyClass == "Bard")
        {
            preferredDistance = 7f;
            if (distanceToBoss < preferredDistance)
                rb.velocity = new Vector2(-direction.x * speed, rb.velocity.y);
            else
                rb.velocity = new Vector2(0, rb.velocity.y);
        }
        else if (enemyClass == "Pyromancer")
        {
            Vector2 midPoint = (transform.position + target.position) / 2;
            direction = (midPoint - (Vector2)transform.position).normalized;
            rb.velocity = new Vector2(direction.x * speed, rb.velocity.y);
        }
        else if (enemyClass == "Cleric")
        {
            EnemyController nearestAlly = FindNearestAlly();
            if (nearestAlly != null && Vector2.Distance(transform.position, nearestAlly.transform.position) > 2f)
                direction = (nearestAlly.transform.position - transform.position).normalized;
            rb.velocity = new Vector2(direction.x * speed, rb.velocity.y);
        }
        else if (enemyClass == "Guardian")
        {
            rb.velocity = new Vector2(direction.x * speed, rb.velocity.y);
}
        }
        else
{
    if (wave >= 2 && enemyClass == "Cleric")
    {
        EnemyController mostInjured = FindMostInjuredAlly();
        if (mostInjured != null)
            direction = (mostInjured.transform.position - transform.position).normalized;
    }
    else if (wave >= 3 && enemyClass == "Guardian")
    {
        EnemyController nearestRanged = FindNearestRanged();
        if (nearestRanged != null && Vector2.Distance(transform.position, nearestRanged.transform.position) > 3f)
            direction = (nearestRanged.transform.position - transform.position).normalized;
    }
    else if (wave >= 6 && enemyClass == "Berserker")
    {
        EnemyController guardian = FindGuardian();
        if (guardian != null)
        {
            Vector2 flankDirection = Vector2.Perpendicular((target.position - guardian.transform.position).normalized);
            direction = flankDirection;
        }
    }
    else if (wave >= 6 && enemyClass == "Guardian")
    {
        direction = (target.position - transform.position).normalized;
    }
    else if (wave >= 8)
    {
        int nearbyAllies = Physics2D.OverlapCircleAll(transform.position, 5f, LayerMask.GetMask("Enemy")).Length - 1;
        if (nearbyAllies >= 3)
        {
            EnemyController nearestAlly = FindNearestAlly();
            if (nearestAlly != null && Vector2.Distance(transform.position, nearestAlly.transform.position) > 1f)
                direction = (nearestAlly.transform.position - transform.position).normalized;
        }
    }

    if (isRangedClass)
    {
        EnemyController guardian = FindGuardian();
        if (guardian != null && distanceToBoss > preferredDistance && wave >= 3)
            direction = (guardian.transform.position - transform.position).normalized;

        if (distanceToBoss < preferredDistance - 1f)
            rb.velocity = new Vector2(-direction.x * speed, rb.velocity.y);
        else if (distanceToBoss > preferredDistance + 1f)
            rb.velocity = new Vector2(direction.x * speed, rb.velocity.y);
        else
            rb.velocity = new Vector2(0, rb.velocity.y);

        if (recentlyAttacked && distanceToBoss > 3f)
            rb.velocity = new Vector2(-direction.x * speed * 0.5f, rb.velocity.y);
    }
    else
    {
        RaycastHit2D obstacleHit = Physics2D.Raycast(transform.position, direction, 1.5f, obstacleLayer);
        if (obstacleHit.collider != null)
        {
            Vector2 detourDirection = Vector2.Perpendicular(direction) * (UnityEngine.Random.value > 0.5f ? 1 : -1);
            rb.velocity = new Vector2(detourDirection.x * speed, rb.velocity.y);
        }
        else
        {
            rb.velocity = new Vector2(direction.x * speed, rb.velocity.y);
        }
    }
}
    }

    void TryJumpToPlatform()
{
    isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 0.6f, groundLayer);

    BossController boss = FindObjectOfType<BossController>();
    bool isBossInSecondPhase = boss != null && boss.IsInSecondPhase();

    if (isBossInSecondPhase && isRangedClass)
    {
        if (isGrounded && UnityEngine.Random.value < 0.3f)
        {
            Collider2D[] nearbyPlatforms = Physics2D.OverlapCircleAll(transform.position, 5f, groundLayer);

foreach (Collider2D platform in nearbyPlatforms)
{
    if (platform.transform.position.y > transform.position.y + 0.5f)
    {
        Vector2 directionToPlatform = (platform.transform.position - transform.position).normalized;
        rb.velocity = new Vector2(directionToPlatform.x * speed, jumpForce);
        if (enemyChat != null && isRangedClass)
        {
            string[] jumpMessages = {
                                $"{playerNickname} [{enemyClass}]: ������ ������, ��!",
                                $"{playerNickname} [{enemyClass}]: ������, ��� ����!",
                                $"{playerNickname} [{enemyClass}]: �� ������, ������!",
                                $"{playerNickname} [{enemyClass}]: ������ ����� �����!",
                                $"{playerNickname} [{enemyClass}]: ������, �� �����!",
                                $"{playerNickname} [{enemyClass}]: ������� �����!",
                                $"{playerNickname} [{enemyClass}]: ����, ������� ���!",
                                $"{playerNickname} [{enemyClass}]: ������ ������ ���!",
                                $"{playerNickname} [{enemyClass}]: ������ �� �����!",
                                $"{playerNickname} [{enemyClass}]: ���� � ��� �����!",
                                $"{playerNickname} [{enemyClass}]: �� ���� ������ ����!",
                                $"{playerNickname} [{enemyClass}]: ��������, �� ������!",
                                $"{playerNickname} [{enemyClass}]: ������ � � ���!",
                                $"{playerNickname} [{enemyClass}]: ������, ������!",
                                $"{playerNickname} [{enemyClass}]: #####, ���� �����!"
                            };
            enemyChat.AddMessage(jumpMessages[UnityEngine.Random.Range(0, jumpMessages.Length)]);
        }
        break;
    }
}
            }
        }
        else if (!isRangedClass)
{
    RaycastHit2D platformCheck = Physics2D.Raycast(transform.position, Vector2.up, 5f, groundLayer);
    if (platformCheck.collider != null && isGrounded)
    {
        float distanceToPlatform = platformCheck.point.y - transform.position.y;
        if (distanceToPlatform > 0.5f && distanceToPlatform < jumpForce)
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }
}
else if (isGrounded && UnityEngine.Random.value < 0.15f)
{
    Collider2D[] nearbyPlatforms = Physics2D.OverlapCircleAll(transform.position, 5f, groundLayer);
    foreach (Collider2D platform in nearbyPlatforms)
    {
        if (platform.transform.position.y > transform.position.y + 0.5f)
        {
            Vector2 directionToPlatform = (platform.transform.position - transform.position).normalized;
            rb.velocity = new Vector2(directionToPlatform.x * speed, jumpForce);
            if (enemyChat != null && isRangedClass)
            {
                string[] jumpMessages = {
                            $"{playerNickname} [{enemyClass}]: ������, �� �� ���!",
                            $"{playerNickname} [{enemyClass}]: ������, ������!",
                            $"{playerNickname} [{enemyClass}]: �� ����� �������!",
                            $"{playerNickname} [{enemyClass}]: ���� � ��� �����!",
                            $"{playerNickname} [{enemyClass}]: ������, � ������!",
                            $"{playerNickname} [{enemyClass}]: ����, �� ����!",
                            $"{playerNickname} [{enemyClass}]: ������ �����!",
                            $"{playerNickname} [{enemyClass}]: ��������, �� ###!",
                            $"{playerNickname} [{enemyClass}]: ������� � ����!",
                            $"{playerNickname} [{enemyClass}]: ������, �������!",
                            $"{playerNickname} [{enemyClass}]: �� � ������ ������!",
                            $"{playerNickname} [{enemyClass}]: ����, ��� �� �������!",
                            $"{playerNickname} [{enemyClass}]: ������ �� ���������!",
                            $"{playerNickname} [{enemyClass}]: ������ ���� �������!",
                            $"{playerNickname} [{enemyClass}]: #####, �������!"
                        };
            enemyChat.AddMessage(jumpMessages[UnityEngine.Random.Range(0, jumpMessages.Length)]);
        }
        break;
    }
}
        }
    }

    void TryDodge()
{
    if (Time.time < nextDodgeTime) return;

    Collider2D[] threats = Physics2D.OverlapCircleAll(transform.position, 3f);
    foreach (Collider2D threat in threats)
    {
        if (threat.CompareTag("Arrow") || threat.CompareTag("Bomb"))
        {
            Vector2 dodgeDirection = Vector2.Perpendicular((threat.transform.position - transform.position).normalized);
            rb.velocity = new Vector2(dodgeDirection.x * speed * 2, jumpForce / 2);
            nextDodgeTime = Time.time + dodgeCooldown;
            if (enemyChat != null)
            {
                string[] dodgeMessages = {
                        $"{playerNickname} [{enemyClass}]: ���� �� �����!",
                        $"{playerNickname} [{enemyClass}]: ��, �� �����, �����!",
                        $"{playerNickname} [{enemyClass}]: ������� � ��������!",
                        $"{playerNickname} [{enemyClass}]: �� ���������, �����!",
                        $"{playerNickname} [{enemyClass}]: ���������, � ��?",
                        $"{playerNickname} [{enemyClass}]: �� �� �����, �� ���!",
                        $"{playerNickname} [{enemyClass}]: ������, ��� ������!",
                        $"{playerNickname} [{enemyClass}]: ��������, ���� ������!",
                        $"{playerNickname} [{enemyClass}]: �� �����, ��-��!",
                        $"{playerNickname} [{enemyClass}]: ���������, �������!",
                        $"{playerNickname} [{enemyClass}]: ������ ����, � ����!",
                        $"{playerNickname} [{enemyClass}]: �� ���������, ����!",
                        $"{playerNickname} [{enemyClass}]: �� ����, ��������!",
                        $"{playerNickname} [{enemyClass}]: �� �� ����, �� �� �����!",
                        $"{playerNickname} [{enemyClass}]: #####, ���������!"
                    };
                enemyChat.AddMessage(dodgeMessages[UnityEngine.Random.Range(0, dodgeMessages.Length)]);
            }
            Debug.Log($"{playerNickname} [{enemyClass}] ������������� �� ����� �����!");
            break;
        }
    }

    GameManager gm = FindObjectOfType<GameManager>();
    int wave = gm != null ? gm.GetCurrentWave() : 1;
    BossController boss = FindObjectOfType<BossController>();
    if (boss != null && (boss.IsInSecondPhase() || wave >= 2) && enemyClass == "Berserker" && UnityEngine.Random.value < (wave >= 2 ? 0.15f : 0.2f))
    {
        Vector2 dodgeDirection = Vector2.Perpendicular((target.position - transform.position).normalized);
        rb.velocity = new Vector2(dodgeDirection.x * speed * 2, jumpForce / 2);
        nextDodgeTime = Time.time + dodgeCooldown * 0.5f;
        if (enemyChat != null)

{
    string[] berserkerDodgeMessages = {
                    $"{playerNickname} [{enemyClass}]: �� ��������, ������!",
                    $"{playerNickname} [{enemyClass}]: �������, � �� �� ���?",
                    $"{playerNickname} [{enemyClass}]: ����, ����� ���������!",
                    $"{playerNickname} [{enemyClass}]: ��, ����, �����!",
                    $"{playerNickname} [{enemyClass}]: �� �� �����, �� � ����!",
                    $"{playerNickname} [{enemyClass}]: ��������, ��� ����!",
                    $"{playerNickname} [{enemyClass}]: �� ���������, ������!",
                    $"{playerNickname} [{enemyClass}]: ���������, ����� ���!",
                    $"{playerNickname} [{enemyClass}]: ������, � �� � ������!",
                    $"{playerNickname} [{enemyClass}]: �� �� ����, �� � �������!",
                    $"{playerNickname} [{enemyClass}]: �� ����, ����!",
                    $"{playerNickname} [{enemyClass}]: ��, ���� ������!",
                    $"{playerNickname} [{enemyClass}]: ���� ����, �������!",
                    $"{playerNickname} [{enemyClass}]: �� ���������, � ���!",
                    $"{playerNickname} [{enemyClass}]: #####, �� �����!"
                };
    enemyChat.AddMessage(berserkerDodgeMessages[UnityEngine.Random.Range(0, berserkerDodgeMessages.Length)]);
}
Debug.Log($"{playerNickname} [{enemyClass}] �������������, ����� �������� �����!");
        }
    }

    void TryUseHealingPotion()
{
    GameManager gm = FindObjectOfType<GameManager>();
    if (gm != null && gm.GetCurrentWave() >= 4 && healingPotions > 0 && !hasUsedPotion && health < 0.3f * GetMaxHealth())
    {
        health += 15f;
        if (health > GetMaxHealth()) health = GetMaxHealth();
        healingPotions--;
        hasUsedPotion = true;
        if (potionEffectPrefab != null)
        {
            GameObject potionEffect = Instantiate(potionEffectPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Destroy(potionEffect, 1f);
        }
        if (potionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(potionSound);
        }
        if (enemyChat != null)
        {
            string[] potionMessages = {
                    $"{playerNickname} [{enemyClass}]: ����� � ������, ����!",
                    $"{playerNickname} [{enemyClass}]: �����, � ����!",
                    $"{playerNickname} [{enemyClass}]: �� �����, ������!",
                    $"{playerNickname} [{enemyClass}]: ��������� �����!",
                    $"{playerNickname} [{enemyClass}]: ��, � ����� � �����!",
                    $"{playerNickname} [{enemyClass}]: ����� � ��� �����!",
                    $"{playerNickname} [{enemyClass}]: ������, �������!",
                    $"{playerNickname} [{enemyClass}]: ������� ����, ���������!",
                    $"{playerNickname} [{enemyClass}]: ���, � �� ��!",
                    $"{playerNickname} [{enemyClass}]: �� ���� ��� �����!",
                    $"{playerNickname} [{enemyClass}]: ������� � ������!",
                    $"{playerNickname} [{enemyClass}]: ����, �� ###!",
                    $"{playerNickname} [{enemyClass}]: �����, � � ���!",
                    $"{playerNickname} [{enemyClass}]: ��, ����� �����!",
                    $"{playerNickname} [{enemyClass}]: #####, � ���!"
                };
            enemyChat.AddMessage(potionMessages[UnityEngine.Random.Range(0, potionMessages.Length)]);
        }
        Debug.Log($"{playerNickname} [{enemyClass}] ����� ����� ������� � ����������� 15 HP!");
    }
}

    void CheckForBombAvoidance()
    {
        GameManager gm = FindObjectOfType<GameManager>();

    if (gm != null && gm.GetCurrentWave() >= 6 && Time.time >= lastBombCheckTime)
    {
        lastBombCheckTime = Time.time + bombCheckInterval;
        Collider2D[] bombs = Physics2D.OverlapCircleAll(transform.position, 5f);
        foreach (Collider2D bomb in bombs)
        {
            if (bomb.CompareTag("Bomb"))
            {
                Vector2 retreatDirection = (transform.position - bomb.transform.position).normalized;
                rb.velocity = new Vector2(retreatDirection.x * speed * 1.5f, rb.velocity.y);
                if (enemyChat != null)
                {
                    string[] bombMessages = {
                                $"{playerNickname} [{enemyClass}]: �����, ����� �� #####!",
                                $"{playerNickname} [{enemyClass}]: ��� ������, �����!",
                                $"{playerNickname} [{enemyClass}]: �� ��������, ����!",
                                $"{playerNickname} [{enemyClass}]: �����, #####, ������!",
                                $"{playerNickname} [{enemyClass}]: ������ �� �������!",
                                $"{playerNickname} [{enemyClass}]: ��� �����, ������!",
                                $"{playerNickname} [{enemyClass}]: ����� �����, ������!",
                                $"{playerNickname} [{enemyClass}]: �� ������, ������!",
                                $"{playerNickname} [{enemyClass}]: �� ���� ������!",
                                $"{playerNickname} [{enemyClass}]: �����, ������� ��������!",
                                $"{playerNickname} [{enemyClass}]: ��� �����, ���� �� ����!",
                                $"{playerNickname} [{enemyClass}]: �� �� � ����!",
                                $"{playerNickname} [{enemyClass}]: �����, ���� �!",
                                $"{playerNickname} [{enemyClass}]: ����� �������!",
                                $"{playerNickname} [{enemyClass}]: #####, ��� ������!"
                            };
                    enemyChat.AddMessage(bombMessages[UnityEngine.Random.Range(0, bombMessages.Length)]);
                }
                Debug.Log($"{playerNickname} [{enemyClass}] ��������� �� �����!");
                break;
            }
        }
    }
        }

    void CheckForIdle()
    {
        if (UnityEngine.Random.value < 0.02f && !isIdling && isGrounded)
        {
            isIdling = true;
            idleTime = UnityEngine.Random.Range(0.5f, 2.5f);
            if (enemyChat != null)
            {
                string[] idleMessages = {
                        $"{playerNickname} [{enemyClass}]: ��� ���� ���?",
                        $"{playerNickname} [{enemyClass}]: ���������� ����� ���!",
                        $"{playerNickname} [{enemyClass}]: ׸, ���� ����?",
                        $"{playerNickname} [{enemyClass}]: �� � ��� �� �������?",
                        $"{playerNickname} [{enemyClass}]: �� �� �����!",
                        $"{playerNickname} [{enemyClass}]: ������, �����!",
                        $"{playerNickname} [{enemyClass}]: �����, ���������!",
                        $"{playerNickname} [{enemyClass}]: ׸ �����, ������?",
                        $"{playerNickname} [{enemyClass}]: ��� ���� ���?",
                        $"{playerNickname} [{enemyClass}]: ���, �� ������!",
                        $"{playerNickname} [{enemyClass}]: �� � ��� �����?",
                        $"{playerNickname} [{enemyClass}]: �� �� ���� ��������!",
                        $"{playerNickname} [{enemyClass}]: ����, �� ���, #####?",
                        $"{playerNickname} [{enemyClass}]: ׸ ������, �����!",
                        $"{playerNickname} [{enemyClass}]: ���, ���� �� ������!"
                    };
                enemyChat.AddMessage(idleMessages[UnityEngine.Random.Range(0, idleMessages.Length)]);
            }
            Debug.Log($"{playerNickname} [{enemyClass}] �������������");
        }

        if (!isRangedClass && Vector2.Distance(transform.position, target.position) > 10f &&
                    Physics2D.OverlapCircle(transform.position, 5f, LayerMask.GetMask("Enemy")) == null)
        {
            Vector2 randomDirection = UnityEngine.Random.insideUnitCircle.normalized;
            rb.velocity = new Vector2(randomDirection.x * speed * 0.5f, rb.velocity.y);
        }
    }

    void CheckForStumble()
{
    if (UnityEngine.Random.value < stumbleChance && !isIdling && isGrounded)
    {
        isIdling = true;
        idleTime = UnityEngine.Random.Range(0.3f, 1f);
        rb.velocity = new Vector2(rb.velocity.x * 0.5f, rb.velocity.y);
        if (enemyChat != null)
        {
            string[] stumbleMessages = {
                    $"{playerNickname} [{enemyClass}]: ׸ �� ����� ��� ������?",
                    $"{playerNickname} [{enemyClass}]: �� � #####, ����������!",
                    $"{playerNickname} [{enemyClass}]: ����� �� ���-��!",
                    $"{playerNickname} [{enemyClass}]: �����, ���� �� ����!",
                    $"{playerNickname} [{enemyClass}]: ��� ��� ����� ��������?",
                    $"{playerNickname} [{enemyClass}]: ��, ���� �� #####!",
                    $"{playerNickname} [{enemyClass}]: �� �� �� ����������!",
                    $"{playerNickname} [{enemyClass}]: �� � ������ ����!",
                    $"{playerNickname} [{enemyClass}]: ����������, ����!",
                    $"{playerNickname} [{enemyClass}]: ׸ �� ��������?",
                    $"{playerNickname} [{enemyClass}]: ���� ��, ���� � �� �!",
                    $"{playerNickname} [{enemyClass}]: �����, ���� ���!",
                    $"{playerNickname} [{enemyClass}]: �� � ������ ���!",
                    $"{playerNickname} [{enemyClass}]: ���� �� ����������!",
                    $"{playerNickname} [{enemyClass}]: �������������, ����� ���!"
                };
            enemyChat.AddMessage(stumbleMessages[UnityEngine.Random.Range(0, stumbleMessages.Length)]);
        }
        Debug.Log($"{playerNickname} [{enemyClass}] ����������!");
    }
}

    public void TakeDamage(float damage, bool fromFriendly = false)
    {
        health -= damage;
        Debug.Log($"{playerNickname} [{enemyClass}] ������� ����: {damage}!");
        spriteRenderer.color = Color.gray;
        Invoke("ResetColor", 0.1f);

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null && !fromFriendly)
        {
            gm.AddDamageByClass(enemyClass, damage);
        }

        if (fromFriendly && enemyChat != null)
        {
            string[] friendlyDamageMessages = {
                    $"{playerNickname} [{enemyClass}]: �� #####, ���� ����!",
                    $"{playerNickname} [{enemyClass}]: ׸ �� #####, ��� �!",
                    $"{playerNickname} [{enemyClass}]: ��, ���� ��, ��������!",
                    $"{playerNickname} [{enemyClass}]: �� ��, ��������?",
                    $"{playerNickname} [{enemyClass}]: ��� �, �� ���, #####!",
                    $"{playerNickname} [{enemyClass}]: ����� �����, �� �� ����?",
                    $"{playerNickname} [{enemyClass}]: �� ##### ��� ���?",
                    $"{playerNickname} [{enemyClass}]: ��, ���� ���� #####!",
                    $"{playerNickname} [{enemyClass}]: �����, ��� �, #####!",
                    $"{playerNickname} [{enemyClass}]: ��������, #####, � ����!",
                    $"{playerNickname} [{enemyClass}]: �� �� �������, ���?",
                    $"{playerNickname} [{enemyClass}]: ����, �� �����, #####!",
                    $"{playerNickname} [{enemyClass}]: ��� �, �� � ����!",
                    $"{playerNickname} [{enemyClass}]: ׸ �� #####, ���� ��!",
                    $"{playerNickname} [{enemyClass}]: �������� �����, #####!"
                };
            enemyChat.AddMessage(friendlyDamageMessages[UnityEngine.Random.Range(0, friendlyDamageMessages.Length)]);
        }

        ReactToBossAttack();

        if (health <= 0)
        {
            deathAnimationTime = 0.5f;
            animator.SetTrigger("Death");
            if (deathSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(deathSound);
            }
            gm?.EnemyDied();

            BossController boss = FindObjectOfType<BossController>();
            if (boss != null && boss.health <= 0 && !fromFriendly)
            {
                gm?.BossDied($"{playerNickname} [{enemyClass}]", Time.time);
            }

            OnDeath?.Invoke(this);
        }
    }

    void ResetColor()
{
    spriteRenderer.color = Color.white;
}

    void ReactToBossAttack()
    {
        BossController boss = FindObjectOfType<BossController>();
        if (boss != null && boss.IsInSecondPhase())
        {
            if (enemyChat != null)
            {
                string[] phaseMessages = {
                        $"{playerNickname} [{enemyClass}]: �� ��, �� �����!",
                        $"{playerNickname} [{enemyClass}]: ������ ����, #####!",
                        $"{playerNickname} [{enemyClass}]: �� ��� ���� �������!",
                        $"{playerNickname} [{enemyClass}]: ��� #####, ������!",
                        $"{playerNickname} [{enemyClass}]: �� ���������, ���!",
                        $"{playerNickname} [{enemyClass}]: ׸ �� #####, �� ����!",
                        $"{playerNickname} [{enemyClass}]: �� � ����� ��������!",
                        $"{playerNickname} [{enemyClass}]: �� ����� �����!",
                        $"{playerNickname} [{enemyClass}]: �� ������ ��� ����!",
                        $"{playerNickname} [{enemyClass}]: ��� �����, ������!",
                        $"{playerNickname} [{enemyClass}]: ����, �� #####, �� ����!",
                        $"{playerNickname} [{enemyClass}]: �� ��� �����!",
                        $"{playerNickname} [{enemyClass}]: ������ ������, �������!",
                        $"{playerNickname} [{enemyClass}]: �� #####, �� ������!",
                        $"{playerNickname} [{enemyClass}]: �� � #####, �� �������!"
                    };
                enemyChat.AddMessage(phaseMessages[UnityEngine.Random.Range(0, phaseMessages.Length)]);
            }

            if (enemyClass == "Berserker")
                TryDodge();
            else if (enemyClass == "Sniper" || enemyClass == "Bard")
                TryJumpToPlatform();
            else if (enemyClass == "Pyromancer")
                PyroBlast(true);
            else if (enemyClass == "Cleric")
                ClericHeal(true);
            else if (enemyClass == "Guardian")
                GuardianTaunt(true);
        }
        else
        {
            if (enemyClass == "Berserker") speed += 0.5f;
            else if (enemyClass == "Cleric") ClericHeal(false);
            else if (isRangedClass && UnityEngine.Random.value < 0.5f) TryJumpToPlatform();
            else if (enemyClass == "Guardian") health += 10f;
        }
    }

    public void SetClass(string newClass)
    {
        enemyClass = newClass;
        switch (enemyClass)
        {
            case "Berserker":
                speed = 4.4f;
                health = 63f;
                attackCooldown = 2f;
                break;
            case "Sniper":
                speed = 2.2f;
                health = 36f;
                attackCooldown = 1.5f;
                break;
            case "Pyromancer":
                speed = 2.75f;
                health = 31.5f;
                attackCooldown = 1.8f;
                break;
            case "Cleric":
                speed = 3.3f;
                health = 45f;
                attackCooldown = 2f;
                break;
            case "Guardian":
                speed = 1.65f;
                health = 90f;
                attackCooldown = 2f;
                break;
            case "Bard":
                speed = 3.0f;
                health = 40f;
                attackCooldown = 2.5f;
                break;
        }
    }

    public void SetNickname(string nickname)
    {
        playerNickname = nickname;
    }

    void BerserkerRage(bool isBossInSecondPhase)
    {
        speed += (Physics2D.OverlapCircle(transform.position, 5f, LayerMask.GetMask("Enemy"))?.GetComponent<EnemyController>()?.enemyClass == "Cleric") ? 2f : 1f;
        foreach (EnemyController ally in FindObjectsOfType<EnemyController>())
        {
            if (Vector2.Distance(transform.position, ally.transform.position) < 5f && !ally.isRangedClass)
                ally.speed += 0.5f;
        }
        Invoke("ResetSpeed", 1f);
        Debug.Log($"{playerNickname} [{enemyClass}] ������� � ������ � ����������� ���������!");
    }

    void ResetSpeed()
    {
        speed -= (Physics2D.OverlapCircle(transform.position, 5f, LayerMask.GetMask("Enemy"))?.GetComponent<EnemyController>()?.enemyClass == "Cleric") ? 2f : 1f;
        foreach (EnemyController ally in FindObjectsOfType<EnemyController>())
        {
            if (Vector2.Distance(transform.position, ally.transform.position) < 5f && !ally.isRangedClass)
                ally.speed -= 0.5f;
        }
    }

    void SniperShot(bool isBossInSecondPhase)
    {
        GameManager gm = FindObjectOfType<GameManager>();
        int wave = gm != null ? gm.GetCurrentWave() : 1;
        for (int i = 0; i < 2; i++)
        {
            GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            projectile.GetComponent<Projectile>().damage = 8f;
            projectile.GetComponent<Projectile>().friendlyFire = friendlyFire;
            if (wave >= 5 && target != null)
            {
                Vector2 bossVelocity = target.GetComponent<Rigidbody2D>().velocity;
                projectile.GetComponent<Projectile>().target = null;
                Vector2 predictedPos = (Vector2)target.position + bossVelocity * 0.5f;
                projectile.GetComponent<Projectile>().SetDirection((predictedPos - (Vector2)transform.position).normalized);
            }
            else
            {
                projectile.GetComponent<Projectile>().target = target;
            }
            projectile.transform.position += (Vector3)UnityEngine.Random.insideUnitCircle * (wave >= 3 ? 0.1f : 0.2f);
        }
        Debug.Log($"{playerNickname} [{enemyClass}] �������� ��������!");
    }

    void PyroBlast(bool isBossInSecondPhase)
    {
        GameManager gm = FindObjectOfType<GameManager>();
        int wave = gm != null ? gm.GetCurrentWave() : 1;
        Vector3 spawnPos = isBossInSecondPhase ? (transform.position + target.position) / 2 : transform.position;
        GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        projectile.GetComponent<Projectile>().damage = 12f;
        projectile.GetComponent<Projectile>().friendlyFire = friendlyFire;
        if (wave >= 5 && target != null)
        {
            Vector2 bossVelocity = target.GetComponent<Rigidbody2D>().velocity;
            Vector2 predictedPos = (Vector2)target.position + bossVelocity * 0.5f;
            projectile.GetComponent<Projectile>().target = null;
            projectile.GetComponent<Projectile>().SetDirection((predictedPos - (Vector2)spawnPos).normalized);
        }
        else
        {
            projectile.GetComponent<Projectile>().target = target;
        }
        projectile.GetComponent<Projectile>().isAoE = true;
        projectile.GetComponent<Projectile>().leaveFireField = true;

        if (wave >= 4)
        {
            Vector3 secondSpawnPos = spawnPos + new Vector3(1f, 0, 0);
            GameObject secondProjectile = Instantiate(projectilePrefab, secondSpawnPos, Quaternion.identity);
            secondProjectile.GetComponent<Projectile>().damage = 12f;
            secondProjectile.GetComponent<Projectile>().friendlyFire = friendlyFire;
            if (wave >= 5 && target != null)

    {
        Vector2 bossVelocity = target.GetComponent<Rigidbody2D>().velocity;
        Vector2 predictedPos = (Vector2)target.position + bossVelocity * 0.5f;
        secondProjectile.GetComponent<Projectile>().target = null;
        secondProjectile.GetComponent<Projectile>().SetDirection((predictedPos - (Vector2)secondSpawnPos).normalized);
    }
                else
    {
        secondProjectile.GetComponent<Projectile>().target = target;
    }
    secondProjectile.GetComponent<Projectile>().isAoE = true;
    secondProjectile.GetComponent<Projectile>().leaveFireField = true;
            }
            Debug.Log($"{playerNickname} [{enemyClass}] ��������� �������� �����!");
        }

    void ClericHeal(bool isBossInSecondPhase)
{
    GameManager gm = FindObjectOfType<GameManager>();
    int wave = gm != null ? gm.GetCurrentWave() : 1;
    EnemyController mostInjured = FindMostInjuredAlly();
    if (mostInjured != null)
    {
        if (wave >= 7)
        {
            EnemyController secondMostInjured = FindSecondMostInjuredAlly(mostInjured);
            if (secondMostInjured != null)
            {
                float healing = Mathf.Min(20f, secondMostInjured.GetMaxHealth() - secondMostInjured.health);
                secondMostInjured.health += healing;
                secondMostInjured.ApplyShield(10f);
                if (gm != null && healing > 0) gm.AddClericHealing(healing);
                Debug.Log($"{playerNickname} [{enemyClass}] ����� � �������� ������� �������� {secondMostInjured.playerNickname} [{secondMostInjured.enemyClass}]!");
            }
        }
        if (UnityEngine.Random.value < 0.05f)
        {
            EnemyController randomAlly = FindObjectsOfType<EnemyController>()[UnityEngine.Random.Range(0, FindObjectsOfType<EnemyController>().Length)];
            if (randomAlly != this)
            {
                float healing = Mathf.Min(20f, randomAlly.GetMaxHealth() - randomAlly.health);
                randomAlly.health += healing;
                randomAlly.ApplyShield(10f);
                if (gm != null && healing > 0) gm.AddClericHealing(healing);
                Debug.Log($"{playerNickname} [{enemyClass}] �������� ����� � �������� {randomAlly.playerNickname} [{randomAlly.enemyClass}]!");
                return;
            }
        }
        float actualHealing = Mathf.Min(20f, mostInjured.GetMaxHealth() - mostInjured.health);
        mostInjured.health += actualHealing;
        mostInjured.ApplyShield(10f);
        if (gm != null && actualHealing > 0) gm.AddClericHealing(actualHealing);

        BossController boss = FindObjectOfType<BossController>();
        if (boss != null && Vector2.Distance(transform.position, boss.transform.position) < 5f)
        {
            float damageToBoss = isBossInSecondPhase ? 10f : 5f;
            boss.TakeDamage(damageToBoss);
            if (clericShockParticles != null)
            {
                clericShockParticles.transform.position = boss.transform.position;
                clericShockParticles.Play();
                Invoke("StopClericShockParticles", 0.5f);
            }
            if (gm != null)
            {
                gm.AddDamageByClass(enemyClass, damageToBoss);
                if (boss.health <= 0) gm.BossDied($"{playerNickname} [{enemyClass}]", Time.time);
            }
            Debug.Log($"{playerNickname} [{enemyClass}] ������� {damageToBoss} ����� ����� ������� �����!");
        }

        Debug.Log($"{playerNickname} [{enemyClass}] ����� � �������� {mostInjured.playerNickname} [{mostInjured.enemyClass}]!");
    }
    if (wave >= 5) attackCooldown = 1.6f;
}

    void GuardianTaunt(bool isBossInSecondPhase)
    {
        health += 5f;
        BossController boss = FindObjectOfType<BossController>();
        if (boss != null) boss.SlowDown(0.5f, 2f);
        Debug.Log($"{playerNickname} [{enemyClass}] ��������� ���� � ��������� �����!");
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null && gm.GetCurrentWave() >= 8) attackCooldown = 1f;
    }

    void BardSong(bool isBossInSecondPhase)
    {
        float radius = isBossInSecondPhase ? 7f : 5f;
        if (bardBuffParticles != null)
        {
            bardBuffParticles.Play();
            Invoke("StopBardParticles", 1f);
        }

        foreach (EnemyController ally in FindObjectsOfType<EnemyController>())
        {
            if (Vector2.Distance(transform.position, ally.transform.position) < radius && ally != this)
            {
                float speedBoost = isBossInSecondPhase ? 1.5f : 1f;
                ally.speed += speedBoost;
                Invoke("ResetBardSpeed", 3f);
                Debug.Log($"{ally.playerNickname} [{ally.enemyClass}] �������� ���� �������� ({speedBoost}) �� {playerNickname} [{enemyClass}]!");
            }
        }

        BossController boss = FindObjectOfType<BossController>();
        if (boss != null && Vector2.Distance(transform.position, boss.transform.position) < radius)
        {
            float slowAmount = isBossInSecondPhase ? 0.75f : 0.5f;
            boss.SlowDown(slowAmount, 3f);
            Debug.Log($"���� �������� {playerNickname} [{enemyClass}] �� {slowAmount}!");
        }
    }

    void StopBardParticles()
    {
        if (bardBuffParticles != null) bardBuffParticles.Stop();
    }

    void StopClericShockParticles()
    {
        if (clericShockParticles != null) clericShockParticles.Stop();
    }

    void ResetBardSpeed()
    {
        foreach (EnemyController ally in FindObjectsOfType<EnemyController>())
        {
            if (Vector2.Distance(transform.position, ally.transform.position) < 7f && ally != this)
            {
                BossController boss = FindObjectOfType<BossController>();
                float speedBoost = (boss != null && boss.IsInSecondPhase()) ? 1.5f : 1f;
                ally.speed -= speedBoost;
            }
        }
    }

    EnemyController FindMostInjuredAlly()
    {
        EnemyController mostInjured = null;
        float lowestHealthPercentage = 1f;

        foreach (EnemyController ally in FindObjectsOfType<EnemyController>())
        {
            if (ally != this && Vector2.Distance(transform.position, ally.transform.position) < 5f)
            {
                float healthPercentage = ally.health / ally.GetMaxHealth();
                if (healthPercentage < 0.5f && healthPercentage < lowestHealthPercentage)
                {
                    mostInjured = ally;
                    lowestHealthPercentage = healthPercentage;
                }
            }
        }
        return mostInjured;
    }

    EnemyController FindSecondMostInjuredAlly(EnemyController exclude)
    {
        EnemyController secondMostInjured = null;
        float secondLowestHealthPercentage = 1f;

        foreach (EnemyController ally in FindObjectsOfType<EnemyController>())
        {
            if (ally != this && ally != exclude && Vector2.Distance(transform.position, ally.transform.position) < 5f)
            {
                float healthPercentage = ally.health / ally.GetMaxHealth();
                if (healthPercentage < 0.5f && healthPercentage < secondLowestHealthPercentage)
                {
                    secondMostInjured = ally;
                    secondLowestHealthPercentage = healthPercentage;
                }
            }
        }
        return secondMostInjured;
    }

    EnemyController FindWeakestAlly()
    {
        EnemyController weakest = null;
        float lowestHealth = float.MaxValue;

    foreach (EnemyController ally in FindObjectsOfType<EnemyController>())
    {
        if (ally != this && ally.health < lowestHealth)
        {
            weakest = ally;
            lowestHealth = ally.health;
        }
    }
    return weakest;
        }

        EnemyController FindGuardian()
    {
        foreach (EnemyController ally in FindObjectsOfType<EnemyController>())
        {
            if (ally.enemyClass == "Guardian" && Vector2.Distance(transform.position, ally.transform.position) < 10f)
                return ally;
        }
        return null;
    }

    EnemyController FindNearestAlly()
    {
        EnemyController nearest = null;
        float minDistance = float.MaxValue;

        foreach (EnemyController ally in FindObjectsOfType<EnemyController>())
        {
            if (ally != this)
            {
                float distance = Vector2.Distance(transform.position, ally.transform.position);
                if (distance < minDistance)
                {
                    nearest = ally;
                    minDistance = distance;
                }
            }
        }
        return nearest;
    }

    EnemyController FindNearestRanged()
    {
        EnemyController nearest = null;
        float minDistance = float.MaxValue;

        foreach (EnemyController ally in FindObjectsOfType<EnemyController>())
        {
            if (ally != this && ally.isRangedClass)
            {
                float distance = Vector2.Distance(transform.position, ally.transform.position);
                if (distance < minDistance)
                {
                    nearest = ally;
                    minDistance = distance;
                }
            }
        }
        return nearest;
    }

    float GetMaxHealth()
    {
        switch (enemyClass)
        {
            case "Berserker": return 63f;
            case "Sniper": return 36f;
            case "Pyromancer": return 31.5f;
            case "Cleric": return 45f;
            case "Guardian": return 90f;
            case "Bard": return 40f;
            default: return 50f;
        }
    }

    public void ApplyShield(float amount)
    {
        health += amount;
        Invoke("RemoveShield", 5f);
    }

    void RemoveShield()
    {
    }

    void ResetAttackFlag() { recentlyAttacked = false; }

    string[] GetRandomMessages()
    {
        switch (enemyClass)
        {
            case "Berserker":
                return new string[] {
                        $"{playerNickname} [{enemyClass}]: ���� ���� � ��� ������ �����!",
                        $"{playerNickname} [{enemyClass}]: �� ����-�� ����� �����!",
                        $"{playerNickname} [{enemyClass}]: ��� ���� ���, � �����!",
                        $"{playerNickname} [{enemyClass}]: ����� ��� �������!",
                        $"{playerNickname} [{enemyClass}]: �����, ������, ������!",
                        $"{playerNickname} [{enemyClass}]: �� ������� ���� �� �����!",
                        $"{playerNickname} [{enemyClass}]: ��� ��� �������? �, ����!",
                        $"{playerNickname} [{enemyClass}]: ����, �� ����� �������?",
                        $"{playerNickname} [{enemyClass}]: ������, ����� ���!",
                        $"{playerNickname} [{enemyClass}]: ������� � ����, �������!",
                        $"{playerNickname} [{enemyClass}]: �� ����� ���������!",
                        $"{playerNickname} [{enemyClass}]: ��� ��� ����, �� ���������?",
                        $"{playerNickname} [{enemyClass}]: ��� ��� ����, �������!",
                        $"{playerNickname} [{enemyClass}]: � ��� ���� �����!",
                        $"{playerNickname} [{enemyClass}]: #####, ��� ���� ���?",
                        $"{playerNickname} [{enemyClass}]: �� ����-�� � ������� ���!",
                        $"{playerNickname} [{enemyClass}]: ����� ����, ������!",
                        $"{playerNickname} [{enemyClass}]: ����, �� ��� �� �����!"
                    };
                case "Sniper":
                    return new string[] {
                        $"{playerNickname} [{enemyClass}]: ������, �����, � �� ����!",
                        $"{playerNickname} [{enemyClass}]: ������ ���� ������ ����!",
                        $"{playerNickname} [{enemyClass}]: ����, �� � �������, �����!",
                        $"{playerNickname} [{enemyClass}]: �� ������ ��������!",
                        $"{playerNickname} [{enemyClass}]: ��� ���, � ���� ����!",
                        $"{playerNickname} [{enemyClass}]: ��� �����, ������� ���!",
                        $"{playerNickname} [{enemyClass}]: ������, � �� � ����!",
                        $"{playerNickname} [{enemyClass}]: �� �������, ���������!",
                        $"{playerNickname} [{enemyClass}]: �� ���� �� ����, ����!",
                        $"{playerNickname} [{enemyClass}]: �������, �� ###!",
                        $"{playerNickname} [{enemyClass}]: �� ����� ������ �������!",
                        $"{playerNickname} [{enemyClass}]: ��� ���� ���, ��� ������!",
                        $"{playerNickname} [{enemyClass}]: ��, �� ���, ��������!",
                        $"{playerNickname} [{enemyClass}]: ������ � ����, ����!",
                        $"{playerNickname} [{enemyClass}]: #####, ������ ��!",
                        $"{playerNickname} [{enemyClass}]: �� � ������ ��������!",
                        $"{playerNickname} [{enemyClass}]: ��� � ����, �������!",
                        $"{playerNickname} [{enemyClass}]: ����, �� ���, �� �������!"
                    };
                case "Pyromancer":
                    return new string[] {
                        $"{playerNickname} [{enemyClass}]: � ���������, ������!",
                        $"{playerNickname} [{enemyClass}]: ����� ���� � ����!",
                        $"{playerNickname} [{enemyClass}]: �� ��������, �� ###!",
                        $"{playerNickname} [{enemyClass}]: ���, ���� �����, �����!",
                        $"{playerNickname} [{enemyClass}]: ����� ��� �����, �������!",
                        $"{playerNickname} [{enemyClass}]: ����-����, ����!",
                        $"{playerNickname} [{enemyClass}]: ������� ��� ������, ��!",
                        $"{playerNickname} [{enemyClass}]: ��� ���� ���������, ����!",
                        $"{playerNickname} [{enemyClass}]: ����� �����, ������!",
                        $"{playerNickname} [{enemyClass}]: �� ������� ��������!",
                        $"{playerNickname} [{enemyClass}]: ����� � ��� �������!",
                        $"{playerNickname} [{enemyClass}]: ����� � �����������!",
                        $"{playerNickname} [{enemyClass}]: �������, ���������!",
                        $"{playerNickname} [{enemyClass}]: �� ��� ������ �� #####!",
                        $"{playerNickname} [{enemyClass}]: #####, ���� ����!",
                        $"{playerNickname} [{enemyClass}]: �� �������� �� �������!",
                        $"{playerNickname} [{enemyClass}]: ����� � ����, ������� ���!",
                        $"{playerNickname} [{enemyClass}]: ����, ���� �� �������!"
                    };
                case "Cleric":
                    return new string[] {
                        $"{playerNickname} [{enemyClass}]: ����, �� ������, �����!",
                        $"{playerNickname} [{enemyClass}]: �� ��������, ������� ���!",
                        $"{playerNickname} [{enemyClass}]: ����, �� ���������, ������!",
                        $"{playerNickname} [{enemyClass}]: ����, � ������, �� ###!",
                        $"{playerNickname} [{enemyClass}]: ������ ����, ������!",
                        $"{playerNickname} [{enemyClass}]: ����� ����, ����, �� ���������!",
                        $"{playerNickname} [{enemyClass}]: ���� � ������, �������!",
                        $"{playerNickname} [{enemyClass}]: �� ��� ����� ��� �����!",
                        $"{playerNickname} [{enemyClass}]: �� �����, � �����, �����!",
                        $"{playerNickname} [{enemyClass}]: ������� � ����, �� �����!",
                        $"{playerNickname} [{enemyClass}]: �������, ������, � ���!",
                        $"{playerNickname} [{enemyClass}]: �� ������� ����, ������!",
                        $"{playerNickname} [{enemyClass}]: �� ����������, � ��� ������!",
                        $"{playerNickname} [{enemyClass}]: ��������, � � ���!",
                        $"{playerNickname} [{enemyClass}]: #####, �������, ����!",
                        $"{playerNickname} [{enemyClass}]: �� ���� ������� � �����!",
                        $"{playerNickname} [{enemyClass}]: ����, �� ���, �����!",
                        $"{playerNickname} [{enemyClass}]: ���� ������, �������!"
                    };
                case "Guardian":
                    return new string[] {
                        $"{playerNickname} [{enemyClass}]: ��� ������, ������!",
                        $"{playerNickname} [{enemyClass}]: � ���� ����, ���� ���!",
                        $"{playerNickname} [{enemyClass}]: ����, �� ��� �� ��������!",
                        $"{playerNickname} [{enemyClass}]: ���� �� ����, �� ###!",
                        $"{playerNickname} [{enemyClass}]: �� ����� ����, �������!",
                        $"{playerNickname} [{enemyClass}]: ����� �����, ��� ������!",
                        $"{playerNickname} [{enemyClass}]: � �����, ��������, �����!",
                        $"{playerNickname} [{enemyClass}]: ��� ����� ����, �����!",
                        $"{playerNickname} [{enemyClass}]: ��� � ����, �� �����!",
                        $"{playerNickname} [{enemyClass}]: � ��� �����, ����!",
                        $"{playerNickname} [{enemyClass}]: ���������, ���� ���!",
                        $"{playerNickname} [{enemyClass}]: �� ������� ��� �����!",
                        $"{playerNickname} [{enemyClass}]: ��� ��� ����? �, ����!",
                        $"{playerNickname} [{enemyClass}]: �� �������, ����� �������!",
                        $"{playerNickname} [{enemyClass}]: #####, � ����������!",
                        $"{playerNickname} [{enemyClass}]: ��� �� �����, ���� ���!",
                        $"{playerNickname} [{enemyClass}]: � �����, ������, �����!",
                        $"{playerNickname} [{enemyClass}]: ����, �� ��� �� ���������!"
                    };
                case "Bard":
                    return new string[] {
                        $"{playerNickname} [{enemyClass}]: �� �������, �������, ����!",
                        $"{playerNickname} [{enemyClass}]: ������ � ����, ������!",
                        $"{playerNickname} [{enemyClass}]: ����� �����, ����� �������!",
                        $"{playerNickname} [{enemyClass}]: ������ ���, ���� ���!",
                        $"{playerNickname} [{enemyClass}]: �������� ��� ����, �����!",
                        $"{playerNickname} [{enemyClass}]: �� ����� ����, �� ###!",
                        $"{playerNickname} [{enemyClass}]: ������� � ��� �����!",
                        $"{playerNickname} [{enemyClass}]: �����, � �� � ����!",
                        $"{playerNickname} [{enemyClass}]: ��� ����� � ���� ������!",
                        $"{playerNickname} [{enemyClass}]: �� ����� ��� ���!",
                        $"{playerNickname} [{enemyClass}]: ������ � ��� �����!",
                        $"{playerNickname} [{enemyClass}]: ��� ��� ������� ������ ����������!",
                        $"{playerNickname} [{enemyClass}]: ����, ���� ���� � ����!",
                        $"{playerNickname} [{enemyClass}]: �����, � �� �������!",
                        $"{playerNickname} [{enemyClass}]: #####, ��� ��� ������!",
                        $"{playerNickname} [{enemyClass}]: �� ��� �������� �� ����!",
                        $"{playerNickname} [{enemyClass}]: ������ � ����, ���� ���!",
                        $"{playerNickname} [{enemyClass}]: ��� ��� ��� ���� �����!",
                        $"{playerNickname} [{enemyClass}]: �� ����, � �� ��������!",
                        $"{playerNickname} [{enemyClass}]: ������ ������, �������!",
                        $"{playerNickname} [{enemyClass}]: ����, ���� ������ � ����!",
                        $"{playerNickname} [{enemyClass}]: ����� �� ������, �����!",
                        $"{playerNickname} [{enemyClass}]: ������� ��� ���, ������!",
                        $"{playerNickname} [{enemyClass}]: �� �������� �����!"
                    };
    default:
                    return new string[] { $"{playerNickname} [{enemyClass}]: �� ��������, �������!" };
            }
        }
}
