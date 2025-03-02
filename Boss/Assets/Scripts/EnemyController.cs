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
    private float attackCooldown = 2f;
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

    private float stumbleChance = 0.01f;
    private float missChance = 0.15f;

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
    private bool friendlyFire = false;

    public event Action<EnemyController> OnDeath;

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Boss").transform;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        enemyChat = FindObjectOfType<EnemyChat>();
        if (spriteRenderer == null) Debug.LogError("SpriteRenderer не найден на " + enemyClass);
        if (animator == null) animator = gameObject.AddComponent<Animator>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        if (playerNickname == null) playerNickname = "Unknown"; // Дефолтное значение
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
                        $"{playerNickname} [{enemyClass}]: Ну всё, откинулся...",
                        $"{playerNickname} [{enemyClass}]: Босс, ты #####, но я сдох!",
                        $"{playerNickname} [{enemyClass}]: Ладно, я выбыл...",
                        $"{playerNickname} [{enemyClass}]: Это конец, пацаны...",
                        $"{playerNickname} [{enemyClass}]: Ух, добили меня...",
                        $"{playerNickname} [{enemyClass}]: Прощай, жестокий мир!",
                        $"{playerNickname} [{enemyClass}]: Всё, я в ауте...",
                        $"{playerNickname} [{enemyClass}]: Кто-то меня прикроет?",
                        $"{playerNickname} [{enemyClass}]: Этот ##### слишком силён!",
                        $"{playerNickname} [{enemyClass}]: Я своё отвоевал...",
                        $"{playerNickname} [{enemyClass}]: Не вышло, гасите свет!",
                        $"{playerNickname} [{enemyClass}]: Попал под раздачу...",
                        $"{playerNickname} [{enemyClass}]: Ну и ладно, отдохну...",
                        $"{playerNickname} [{enemyClass}]: Босс, ты выиграл, #####...",
                        $"{playerNickname} [{enemyClass}]: Всё, мне #####...",
                        $"{playerNickname} [{enemyClass}]: Сыграл и сдох, норм!",
                        $"{playerNickname} [{enemyClass}]: Последний аккорд, пока!",
                        $"{playerNickname} [{enemyClass}]: Музыка затихла, всё..."
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
                                $"{playerNickname} [{enemyClass}]: Получай по башке!",
                                $"{playerNickname} [{enemyClass}]: Топором тебе в рыло!",
                                $"{playerNickname} [{enemyClass}]: Ща развалю, держись!",
                                $"{playerNickname} [{enemyClass}]: Кто тут главный? Я решаю!",
                                $"{playerNickname} [{enemyClass}]: Бей или вали!",
                                $"{playerNickname} [{enemyClass}]: Размажу тебя, босс!",
                                $"{playerNickname} [{enemyClass}]: Топор уже в деле!",
                                $"{playerNickname} [{enemyClass}]: Хрясь, и нет тебя!",
                                $"{playerNickname} [{enemyClass}]: На куски порублю!",
                                $"{playerNickname} [{enemyClass}]: Ща будет мясо!",
                                $"{playerNickname} [{enemyClass}]: Докажи, что не слабак!",
                                $"{playerNickname} [{enemyClass}]: Вали его, ребята!",
                                $"{playerNickname} [{enemyClass}]: Топорик в ход пошёл!",
                                $"{playerNickname} [{enemyClass}]: Нарублю дровишек!",
                                $"{playerNickname} [{enemyClass}]: #####, держи удар!",
                                $"{playerNickname} [{enemyClass}]: Ща тебе в челюсть!",
                                $"{playerNickname} [{enemyClass}]: Разрублю пополам!",
                                $"{playerNickname} [{enemyClass}]: Топор жаждет боя!"
                            };
                enemyChat.AddMessage(berserkerAttackMessages[UnityEngine.Random.Range(0, berserkerAttackMessages.Length)]);
        }
        break;
                    case "Sniper":
                        SniperShot(false);
        if (enemyChat != null)
        {
            string[] sniperAttackMessages = {
                                $"{playerNickname} [{enemyClass}]: Попал, теперь кричи!",
                                $"{playerNickname} [{enemyClass}]: Стрела тебе в лобешник!",
                                $"{playerNickname} [{enemyClass}]: Лови подарок, босс!",
                                $"{playerNickname} [{enemyClass}]: Точно в глаз, ха!",
                                $"{playerNickname} [{enemyClass}]: Не дёргайся, прицелюсь!",
                                $"{playerNickname} [{enemyClass}]: Ща будет больно!",
                                $"{playerNickname} [{enemyClass}]: От меня не спрячешься!",
                                $"{playerNickname} [{enemyClass}]: Стреляю на раз-два!",
                                $"{playerNickname} [{enemyClass}]: Попадание, ура!",
                                $"{playerNickname} [{enemyClass}]: Лук готов, держись!",
                                $"{playerNickname} [{enemyClass}]: В яблочко, чувак!",
                                $"{playerNickname} [{enemyClass}]: Ща прострелю тебя!",
                                $"{playerNickname} [{enemyClass}]: Не стой, двигай!",
                                $"{playerNickname} [{enemyClass}]: Стрела уже в пути!",
                                $"{playerNickname} [{enemyClass}]: #####, попал-таки!",
                                $"{playerNickname} [{enemyClass}]: Ща в череп прилетит!",
                                $"{playerNickname} [{enemyClass}]: Лови стрелу, гад!",
                                $"{playerNickname} [{enemyClass}]: Точный выстрел, держи!"
                            };
            enemyChat.AddMessage(sniperAttackMessages[UnityEngine.Random.Range(0, sniperAttackMessages.Length)]);
        }
        break;
                    case "Pyromancer":
                        PyroBlast(false);
        if (enemyChat != null)
        {
            string[] pyroAttackMessages = {
                                $"{playerNickname} [{enemyClass}]: Я поехавший, бегите!",
                                $"{playerNickname} [{enemyClass}]: Огонь тебе в морду!",
                                $"{playerNickname} [{enemyClass}]: Ща поджарим, держись!",
                                $"{playerNickname} [{enemyClass}]: Жги, пока живой!",
                                $"{playerNickname} [{enemyClass}]: Пламя уже летит!",
                                $"{playerNickname} [{enemyClass}]: Гори-гори ясно!",
                                $"{playerNickname} [{enemyClass}]: Подгони мне спички!",
                                $"{playerNickname} [{enemyClass}]: Это тебе фейерверк!",
                                $"{playerNickname} [{enemyClass}]: Жарко будет, босс!",
                                $"{playerNickname} [{enemyClass}]: Ща устроим пожар!",
                                $"{playerNickname} [{enemyClass}]: Огонь — мой кореш!",
                                $"{playerNickname} [{enemyClass}]: Поджёг и расслабился!",
                                $"{playerNickname} [{enemyClass}]: Полыхай, красавчик!",
                                $"{playerNickname} [{enemyClass}]: Ща все сгорит на #####!",
                                $"{playerNickname} [{enemyClass}]: #####, жарю тебя!",
                                $"{playerNickname} [{enemyClass}]: Ща поджарим до хруста!",
                                $"{playerNickname} [{enemyClass}]: Огонь в деле, держись!",
                                $"{playerNickname} [{enemyClass}]: Гори, пока не сгоришь!"
                            };
        enemyChat.AddMessage(pyroAttackMessages[UnityEngine.Random.Range(0, pyroAttackMessages.Length)]);
    }
    break;
                    case "Cleric":
                        ClericHeal(false);
    if (enemyChat != null)
    {
        string[] clericAttackMessages = {
                                $"{playerNickname} [{enemyClass}]: Живи, не кашляй!",
                                $"{playerNickname} [{enemyClass}]: Ща подлатаю, держись!",
                                $"{playerNickname} [{enemyClass}]: Лечу, не благодари!",
                                $"{playerNickname} [{enemyClass}]: Стой, я помогу!",
                                $"{playerNickname} [{enemyClass}]: Не ###, вылечим!",
                                $"{playerNickname} [{enemyClass}]: Жизнь тебе, бери!",
                                $"{playerNickname} [{enemyClass}]: Свет в помощь, пацаны!",
                                $"{playerNickname} [{enemyClass}]: Ща все будут как новые!",
                                $"{playerNickname} [{enemyClass}]: Не дохни, я рядом!",
                                $"{playerNickname} [{enemyClass}]: Лечение в деле!",
                                $"{playerNickname} [{enemyClass}]: Вытяну тебя, брат!",
                                $"{playerNickname} [{enemyClass}]: Ща заживим раны!",
                                $"{playerNickname} [{enemyClass}]: Не падай, я тут!",
                                $"{playerNickname} [{enemyClass}]: Подлатаю, держись!",
                                $"{playerNickname} [{enemyClass}]: #####, выживай!",
                                $"{playerNickname} [{enemyClass}]: Ща всех подниму!",
                                $"{playerNickname} [{enemyClass}]: Лечу, не ной!",
                                $"{playerNickname} [{enemyClass}]: Живём дальше, пацаны!"
                            };
        enemyChat.AddMessage(clericAttackMessages[UnityEngine.Random.Range(0, clericAttackMessages.Length)]);
    }
    break;
                    case "Guardian":
                        GuardianTaunt(false);
    if (enemyChat != null)
    {
        string[] guardianAttackMessages = {
                                $"{playerNickname} [{enemyClass}]: Щит держит, бей!",
                                $"{playerNickname} [{enemyClass}]: Я твой танк, пацаны!",
                                $"{playerNickname} [{enemyClass}]: Босс, ты мне не пробьёшь!",
                                $"{playerNickname} [{enemyClass}]: Стой за мной, не ###!",
                                $"{playerNickname} [{enemyClass}]: Ща приму ударчик!",
                                $"{playerNickname} [{enemyClass}]: Держу фронт, вали его!",
                                $"{playerNickname} [{enemyClass}]: Я стена, попробуй!",
                                $"{playerNickname} [{enemyClass}]: Бей через меня, давай!",
                                $"{playerNickname} [{enemyClass}]: Щит в деле, не бойся!",
                                $"{playerNickname} [{enemyClass}]: Я как скала, босс!",
                                $"{playerNickname} [{enemyClass}]: Прикрываю, мочи его!",
                                $"{playerNickname} [{enemyClass}]: Ща замедлю эту тварь!",
                                $"{playerNickname} [{enemyClass}]: Кто тут танк? Я танк!",
                                $"{playerNickname} [{enemyClass}]: Не пройдёшь, держу!",
                                $"{playerNickname} [{enemyClass}]: #####, я несокрушим!",
                                $"{playerNickname} [{enemyClass}]: Щит на месте, бей!",
                                $"{playerNickname} [{enemyClass}]: Я держу, пацаны, давай!",
                                $"{playerNickname} [{enemyClass}]: Босс, ты мне не конкурент!"
                            };
    enemyChat.AddMessage(guardianAttackMessages[UnityEngine.Random.Range(0, guardianAttackMessages.Length)]);
}
break;
                    case "Bard":
                        BardSong(false);
if (enemyChat != null)
{
    string[] bardAttackMessages = {
                                $"{playerNickname} [{enemyClass}]: Песня врублена, валим!",
                                $"{playerNickname} [{enemyClass}]: Ща заиграю, держись!",
                                $"{playerNickname} [{enemyClass}]: Музыка в деле, пацаны!",
                                $"{playerNickname} [{enemyClass}]: Босс, тебе ноты в рыло!",
                                $"{playerNickname} [{enemyClass}]: Играю, не ###!",
                                $"{playerNickname} [{enemyClass}]: Песня тащит, бегай!",
                                $"{playerNickname} [{enemyClass}]: Ща будет ритм, чувак!",
                                $"{playerNickname} [{enemyClass}]: Моя гитара рвёт!",
                                $"{playerNickname} [{enemyClass}]: Баллада в ход пошла!",
                                $"{playerNickname} [{enemyClass}]: Подпевай или вали!",
                                $"{playerNickname} [{enemyClass}]: Музыка — мой удар!",
                                $"{playerNickname} [{enemyClass}]: Ща зажгу под ритм!",
                                $"{playerNickname} [{enemyClass}]: Песня для боя, держись!",
                                $"{playerNickname} [{enemyClass}]: Играю, босс, пляши!",
                                $"{playerNickname} [{enemyClass}]: #####, под мою музыку!",
                                $"{playerNickname} [{enemyClass}]: Ща все забегают!",
                                $"{playerNickname} [{enemyClass}]: Гитара в деле, вали его!",
                                $"{playerNickname} [{enemyClass}]: Под мой бит всех порвём!"
                            };
    enemyChat.AddMessage(bardAttackMessages[UnityEngine.Random.Range(0, bardAttackMessages.Length)]);
}
break;
                }
            }
            else
{
    Debug.Log($"{playerNickname} [{enemyClass}] промахнулся!");
    if (enemyChat != null)
    {
        string[] missMessages = {
                        $"{playerNickname} [{enemyClass}]: Ну и как я промазал?",
                        $"{playerNickname} [{enemyClass}]: Чё за фигня, мимо!",
                        $"{playerNickname} [{enemyClass}]: Этот босс юлит, зараза!",
                        $"{playerNickname} [{enemyClass}]: Да ладно, опять мимо?",
                        $"{playerNickname} [{enemyClass}]: Ну ты и шустрый!",
                        $"{playerNickname} [{enemyClass}]: Не попал, ну и фиг с ним!",
                        $"{playerNickname} [{enemyClass}]: Руки кривые, блин!",
                        $"{playerNickname} [{enemyClass}]: Ща бы попал, но нет!",
                        $"{playerNickname} [{enemyClass}]: Какой-то босс шустрый!",
                        $"{playerNickname} [{enemyClass}]: Ну и #####, не попал!",
                        $"{playerNickname} [{enemyClass}]: Давай, стой ровно!",
                        $"{playerNickname} [{enemyClass}]: Чё за подстава?",
                        $"{playerNickname} [{enemyClass}]: Мимо, ну и ладно!",
                        $"{playerNickname} [{enemyClass}]: Ты двигаешься, гад!",
                        $"{playerNickname} [{enemyClass}]: Попробую ещё разок!",
                        $"{playerNickname} [{enemyClass}]: Ща бы в ноту, а не вышло!",
                        $"{playerNickname} [{enemyClass}]: Промахнулся, ну и чёрт с ним!"
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
                                $"{playerNickname} [{enemyClass}]: Прыгну повыше, чё!",
                                $"{playerNickname} [{enemyClass}]: Наверх, там кайф!",
                                $"{playerNickname} [{enemyClass}]: Ща залезу, погоди!",
                                $"{playerNickname} [{enemyClass}]: Сверху видак лучше!",
                                $"{playerNickname} [{enemyClass}]: Прыгаю, не мешай!",
                                $"{playerNickname} [{enemyClass}]: Наверху зажгу!",
                                $"{playerNickname} [{enemyClass}]: Лезу, держись там!",
                                $"{playerNickname} [{enemyClass}]: Оттуда завалю его!",
                                $"{playerNickname} [{enemyClass}]: Прыжок на точку!",
                                $"{playerNickname} [{enemyClass}]: Выше — мой стиль!",
                                $"{playerNickname} [{enemyClass}]: Ща буду король горы!",
                                $"{playerNickname} [{enemyClass}]: Подскочу, не вопрос!",
                                $"{playerNickname} [{enemyClass}]: Наверх и в бой!",
                                $"{playerNickname} [{enemyClass}]: Прыгаю, пацаны!",
                                $"{playerNickname} [{enemyClass}]: #####, лезу вверх!"
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
                            $"{playerNickname} [{enemyClass}]: Прыгаю, чё уж там!",
                            $"{playerNickname} [{enemyClass}]: Наверх, пацаны!",
                            $"{playerNickname} [{enemyClass}]: Ща займу позицию!",
                            $"{playerNickname} [{enemyClass}]: Выше — мой конёк!",
                            $"{playerNickname} [{enemyClass}]: Прыжок, и готово!",
                            $"{playerNickname} [{enemyClass}]: Лезу, не стой!",
                            $"{playerNickname} [{enemyClass}]: Сверху зажгу!",
                            $"{playerNickname} [{enemyClass}]: Подскочу, не ###!",
                            $"{playerNickname} [{enemyClass}]: Наверху я царь!",
                            $"{playerNickname} [{enemyClass}]: Прыгаю, держись!",
                            $"{playerNickname} [{enemyClass}]: Ща с высоты завалю!",
                            $"{playerNickname} [{enemyClass}]: Выше, чем ты думаешь!",
                            $"{playerNickname} [{enemyClass}]: Прыжок на верхотуру!",
                            $"{playerNickname} [{enemyClass}]: Оттуда всех достану!",
                            $"{playerNickname} [{enemyClass}]: #####, прыгнул!"
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
                        $"{playerNickname} [{enemyClass}]: Ушёл от хрени!",
                        $"{playerNickname} [{enemyClass}]: Ха, не попал, лошок!",
                        $"{playerNickname} [{enemyClass}]: Прыгнул в сторонку!",
                        $"{playerNickname} [{enemyClass}]: Не достанешь, чувак!",
                        $"{playerNickname} [{enemyClass}]: Уклонился, и чё?",
                        $"{playerNickname} [{enemyClass}]: Ща бы попал, но нет!",
                        $"{playerNickname} [{enemyClass}]: Докажи, что можешь!",
                        $"{playerNickname} [{enemyClass}]: Отскочил, лови дальше!",
                        $"{playerNickname} [{enemyClass}]: Не попал, ха-ха!",
                        $"{playerNickname} [{enemyClass}]: Увернулся, держись!",
                        $"{playerNickname} [{enemyClass}]: Прыжок вбок, и норм!",
                        $"{playerNickname} [{enemyClass}]: Ты медленный, босс!",
                        $"{playerNickname} [{enemyClass}]: Не туда, придурок!",
                        $"{playerNickname} [{enemyClass}]: Ща бы меня, да не вышло!",
                        $"{playerNickname} [{enemyClass}]: #####, увернулся!"
                    };
                enemyChat.AddMessage(dodgeMessages[UnityEngine.Random.Range(0, dodgeMessages.Length)]);
            }
            Debug.Log($"{playerNickname} [{enemyClass}] уворачивается от атаки босса!");
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
                    $"{playerNickname} [{enemyClass}]: Не поймаешь, лошара!",
                    $"{playerNickname} [{enemyClass}]: Прыгнул, и чё ты мне?",
                    $"{playerNickname} [{enemyClass}]: Ушёл, держи дистанцию!",
                    $"{playerNickname} [{enemyClass}]: Ха, мимо, чувак!",
                    $"{playerNickname} [{enemyClass}]: Ща бы попал, но я ушёл!",
                    $"{playerNickname} [{enemyClass}]: Отскочил, иди сюда!",
                    $"{playerNickname} [{enemyClass}]: Не достанешь, слабак!",
                    $"{playerNickname} [{enemyClass}]: Увернулся, давай ещё!",
                    $"{playerNickname} [{enemyClass}]: Прыжок, и ты в пролёте!",
                    $"{playerNickname} [{enemyClass}]: Ща бы меня, но я быстрый!",
                    $"{playerNickname} [{enemyClass}]: Не туда, босс!",
                    $"{playerNickname} [{enemyClass}]: Ха, лови воздух!",
                    $"{playerNickname} [{enemyClass}]: Ушёл вбок, держись!",
                    $"{playerNickname} [{enemyClass}]: Ты медленный, я нет!",
                    $"{playerNickname} [{enemyClass}]: #####, не попал!"
                };
    enemyChat.AddMessage(berserkerDodgeMessages[UnityEngine.Random.Range(0, berserkerDodgeMessages.Length)]);
}
Debug.Log($"{playerNickname} [{enemyClass}] уворачивается, чтобы окружить босса!");
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
                    $"{playerNickname} [{enemyClass}]: Зелье в глотку, живём!",
                    $"{playerNickname} [{enemyClass}]: Выпил, и норм!",
                    $"{playerNickname} [{enemyClass}]: Ща оживу, погоди!",
                    $"{playerNickname} [{enemyClass}]: Лекарство пошло!",
                    $"{playerNickname} [{enemyClass}]: Ха, я снова в строю!",
                    $"{playerNickname} [{enemyClass}]: Зелье — мой кореш!",
                    $"{playerNickname} [{enemyClass}]: Живчик, держись!",
                    $"{playerNickname} [{enemyClass}]: Вытянул себя, красавчик!",
                    $"{playerNickname} [{enemyClass}]: Пью, и всё ок!",
                    $"{playerNickname} [{enemyClass}]: Ща буду как новый!",
                    $"{playerNickname} [{enemyClass}]: Зельеце в помощь!",
                    $"{playerNickname} [{enemyClass}]: Ожил, не ###!",
                    $"{playerNickname} [{enemyClass}]: Выпил, и в бой!",
                    $"{playerNickname} [{enemyClass}]: Ха, зелье тащит!",
                    $"{playerNickname} [{enemyClass}]: #####, я жив!"
                };
            enemyChat.AddMessage(potionMessages[UnityEngine.Random.Range(0, potionMessages.Length)]);
        }
        Debug.Log($"{playerNickname} [{enemyClass}] выпил зелье лечения и восстановил 15 HP!");
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
                                $"{playerNickname} [{enemyClass}]: Бомба, валим на #####!",
                                $"{playerNickname} [{enemyClass}]: Это рванёт, бегом!",
                                $"{playerNickname} [{enemyClass}]: Ща бабахнет, ноги!",
                                $"{playerNickname} [{enemyClass}]: Бомба, #####, уходим!",
                                $"{playerNickname} [{enemyClass}]: Успеть бы свалить!",
                                $"{playerNickname} [{enemyClass}]: Это взрыв, отскок!",
                                $"{playerNickname} [{enemyClass}]: Бомба рядом, паника!",
                                $"{playerNickname} [{enemyClass}]: Ща рванёт, бегите!",
                                $"{playerNickname} [{enemyClass}]: Не хочу гореть!",
                                $"{playerNickname} [{enemyClass}]: Бомба, держись подальше!",
                                $"{playerNickname} [{enemyClass}]: Это конец, если не уйти!",
                                $"{playerNickname} [{enemyClass}]: Ща всё в хлам!",
                                $"{playerNickname} [{enemyClass}]: Бомба, мать её!",
                                $"{playerNickname} [{enemyClass}]: Легче свалить!",
                                $"{playerNickname} [{enemyClass}]: #####, это рванёт!"
                            };
                    enemyChat.AddMessage(bombMessages[UnityEngine.Random.Range(0, bombMessages.Length)]);
                }
                Debug.Log($"{playerNickname} [{enemyClass}] отступает от бомбы!");
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
                        $"{playerNickname} [{enemyClass}]: Где этот тип?",
                        $"{playerNickname} [{enemyClass}]: Задолбался ждать уже!",
                        $"{playerNickname} [{enemyClass}]: Чё, босс спит?",
                        $"{playerNickname} [{enemyClass}]: Ну и где он шляется?",
                        $"{playerNickname} [{enemyClass}]: Ща бы драку!",
                        $"{playerNickname} [{enemyClass}]: Скучно, чувак!",
                        $"{playerNickname} [{enemyClass}]: Давай, появляйся!",
                        $"{playerNickname} [{enemyClass}]: Чё стоим, пацаны?",
                        $"{playerNickname} [{enemyClass}]: Где этот гад?",
                        $"{playerNickname} [{enemyClass}]: Жду, аж тошнит!",
                        $"{playerNickname} [{enemyClass}]: Ну и где бойня?",
                        $"{playerNickname} [{enemyClass}]: Ща бы кого завалить!",
                        $"{playerNickname} [{enemyClass}]: Босс, ты где, #####?",
                        $"{playerNickname} [{enemyClass}]: Чё молчим, давай!",
                        $"{playerNickname} [{enemyClass}]: Жду, пока не сдохну!"
                    };
                enemyChat.AddMessage(idleMessages[UnityEngine.Random.Range(0, idleMessages.Length)]);
            }
            Debug.Log($"{playerNickname} [{enemyClass}] осматривается");
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
                    $"{playerNickname} [{enemyClass}]: Чё за хрень под ногами?",
                    $"{playerNickname} [{enemyClass}]: Ну и #####, споткнулся!",
                    $"{playerNickname} [{enemyClass}]: Задел за что-то!",
                    $"{playerNickname} [{enemyClass}]: Ладно, чуть не упал!",
                    $"{playerNickname} [{enemyClass}]: Кто тут камни раскидал?",
                    $"{playerNickname} [{enemyClass}]: Ой, чуть не #####!",
                    $"{playerNickname} [{enemyClass}]: Ща бы не грохнуться!",
                    $"{playerNickname} [{enemyClass}]: Ну и кривые ноги!",
                    $"{playerNickname} [{enemyClass}]: Споткнулся, чёрт!",
                    $"{playerNickname} [{enemyClass}]: Чё за подстава?",
                    $"{playerNickname} [{enemyClass}]: Упал бы, если б не я!",
                    $"{playerNickname} [{enemyClass}]: Ладно, стою ещё!",
                    $"{playerNickname} [{enemyClass}]: Ну и дорога тут!",
                    $"{playerNickname} [{enemyClass}]: Чуть не растянулся!",
                    $"{playerNickname} [{enemyClass}]: Поскользнулся, ешкин кот!"
                };
            enemyChat.AddMessage(stumbleMessages[UnityEngine.Random.Range(0, stumbleMessages.Length)]);
        }
        Debug.Log($"{playerNickname} [{enemyClass}] споткнулся!");
    }
}

    public void TakeDamage(float damage, bool fromFriendly = false)
    {
        health -= damage;
        Debug.Log($"{playerNickname} [{enemyClass}] получил урон: {damage}!");
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
                    $"{playerNickname} [{enemyClass}]: Ты #####, свои бьют!",
                    $"{playerNickname} [{enemyClass}]: Чё за #####, это я!",
                    $"{playerNickname} [{enemyClass}]: Эй, свои же, придурок!",
                    $"{playerNickname} [{enemyClass}]: Вы чё, охренели?",
                    $"{playerNickname} [{enemyClass}]: Это я, не бей, #####!",
                    $"{playerNickname} [{enemyClass}]: Своих мочим, чё за дела?",
                    $"{playerNickname} [{enemyClass}]: Ты ##### или кто?",
                    $"{playerNickname} [{enemyClass}]: Ой, свои меня #####!",
                    $"{playerNickname} [{enemyClass}]: Чувак, это я, #####!",
                    $"{playerNickname} [{enemyClass}]: Прекрати, #####, я свой!",
                    $"{playerNickname} [{enemyClass}]: Ты чё творишь, гад?",
                    $"{playerNickname} [{enemyClass}]: Свои, не бейте, #####!",
                    $"{playerNickname} [{enemyClass}]: Это я, не в меня!",
                    $"{playerNickname} [{enemyClass}]: Чё за #####, свои же!",
                    $"{playerNickname} [{enemyClass}]: Ошибочка вышла, #####!"
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
                        $"{playerNickname} [{enemyClass}]: Ну всё, он лютый!",
                        $"{playerNickname} [{enemyClass}]: Вторая фаза, #####!",
                        $"{playerNickname} [{enemyClass}]: Ща нас всех завалит!",
                        $"{playerNickname} [{enemyClass}]: Это #####, пацаны!",
                        $"{playerNickname} [{enemyClass}]: Он разошёлся, гад!",
                        $"{playerNickname} [{enemyClass}]: Чё за #####, он силён!",
                        $"{playerNickname} [{enemyClass}]: Ну и хрень началась!",
                        $"{playerNickname} [{enemyClass}]: Ща будет жарко!",
                        $"{playerNickname} [{enemyClass}]: Он второй раз ожил!",
                        $"{playerNickname} [{enemyClass}]: Это конец, похоже!",
                        $"{playerNickname} [{enemyClass}]: Босс, ты #####, но крут!",
                        $"{playerNickname} [{enemyClass}]: Ща нас порвёт!",
                        $"{playerNickname} [{enemyClass}]: Вторая стадия, держись!",
                        $"{playerNickname} [{enemyClass}]: Он #####, не сдаётся!",
                        $"{playerNickname} [{enemyClass}]: Ну и #####, он живучий!"
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
        Debug.Log($"{playerNickname} [{enemyClass}] впадает в ярость и вдохновляет союзников!");
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
        Debug.Log($"{playerNickname} [{enemyClass}] стреляет очередью!");
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
            Debug.Log($"{playerNickname} [{enemyClass}] выпускает огненный взрыв!");
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
                Debug.Log($"{playerNickname} [{enemyClass}] лечит и защищает второго союзника {secondMostInjured.playerNickname} [{secondMostInjured.enemyClass}]!");
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
                Debug.Log($"{playerNickname} [{enemyClass}] случайно лечит и защищает {randomAlly.playerNickname} [{randomAlly.enemyClass}]!");
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
            Debug.Log($"{playerNickname} [{enemyClass}] наносит {damageToBoss} урона боссу Светлым шоком!");
        }

        Debug.Log($"{playerNickname} [{enemyClass}] лечит и защищает {mostInjured.playerNickname} [{mostInjured.enemyClass}]!");
    }
    if (wave >= 5) attackCooldown = 1.6f;
}

    void GuardianTaunt(bool isBossInSecondPhase)
    {
        health += 5f;
        BossController boss = FindObjectOfType<BossController>();
        if (boss != null) boss.SlowDown(0.5f, 2f);
        Debug.Log($"{playerNickname} [{enemyClass}] принимает удар и замедляет босса!");
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
                Debug.Log($"{ally.playerNickname} [{ally.enemyClass}] получает бафф скорости ({speedBoost}) от {playerNickname} [{enemyClass}]!");
            }
        }

        BossController boss = FindObjectOfType<BossController>();
        if (boss != null && Vector2.Distance(transform.position, boss.transform.position) < radius)
        {
            float slowAmount = isBossInSecondPhase ? 0.75f : 0.5f;
            boss.SlowDown(slowAmount, 3f);
            Debug.Log($"Босс замедлен {playerNickname} [{enemyClass}] на {slowAmount}!");
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
                        $"{playerNickname} [{enemyClass}]: Этот босс — мой личный мешок!",
                        $"{playerNickname} [{enemyClass}]: Ща кому-то башку снесу!",
                        $"{playerNickname} [{enemyClass}]: Где этот тип, я готов!",
                        $"{playerNickname} [{enemyClass}]: Топор уже чешется!",
                        $"{playerNickname} [{enemyClass}]: Давай, выходи, слабак!",
                        $"{playerNickname} [{enemyClass}]: Ща порублю всех на куски!",
                        $"{playerNickname} [{enemyClass}]: Кто тут главный? Я, ясно!",
                        $"{playerNickname} [{enemyClass}]: Босс, ты готов умыться?",
                        $"{playerNickname} [{enemyClass}]: Пацаны, валим его!",
                        $"{playerNickname} [{enemyClass}]: Топорик в деле, держись!",
                        $"{playerNickname} [{enemyClass}]: Ща будет мясорубка!",
                        $"{playerNickname} [{enemyClass}]: Где мой враг, чё прячешься?",
                        $"{playerNickname} [{enemyClass}]: Бей или вали, выбирай!",
                        $"{playerNickname} [{enemyClass}]: Я тут всех порву!",
                        $"{playerNickname} [{enemyClass}]: #####, где этот гад?",
                        $"{playerNickname} [{enemyClass}]: Ща кому-то в челюсть дам!",
                        $"{playerNickname} [{enemyClass}]: Топор зовёт, пацаны!",
                        $"{playerNickname} [{enemyClass}]: Босс, ты мне не ровня!"
                    };
                case "Sniper":
                    return new string[] {
                        $"{playerNickname} [{enemyClass}]: Целься, дурак, а не стой!",
                        $"{playerNickname} [{enemyClass}]: Стрелы сами найдут цель!",
                        $"{playerNickname} [{enemyClass}]: Босс, ты в прицеле, чувак!",
                        $"{playerNickname} [{enemyClass}]: Ща завалю издалека!",
                        $"{playerNickname} [{enemyClass}]: Кто тут, я всех вижу!",
                        $"{playerNickname} [{enemyClass}]: Лук готов, держись там!",
                        $"{playerNickname} [{enemyClass}]: Попаду, и ты в ауте!",
                        $"{playerNickname} [{enemyClass}]: Не дёргайся, прицелюсь!",
                        $"{playerNickname} [{enemyClass}]: От меня не уйти, босс!",
                        $"{playerNickname} [{enemyClass}]: Стреляю, не ###!",
                        $"{playerNickname} [{enemyClass}]: Ща будет точный выстрел!",
                        $"{playerNickname} [{enemyClass}]: Где этот тип, дай прицел!",
                        $"{playerNickname} [{enemyClass}]: Ха, ты мой, готовься!",
                        $"{playerNickname} [{enemyClass}]: Стрела в пути, лови!",
                        $"{playerNickname} [{enemyClass}]: #####, попаду ща!",
                        $"{playerNickname} [{enemyClass}]: Ща в глазик прилетит!",
                        $"{playerNickname} [{enemyClass}]: Лук в деле, держись!",
                        $"{playerNickname} [{enemyClass}]: Босс, ты мой, не дёргайся!"
                    };
                case "Pyromancer":
                    return new string[] {
                        $"{playerNickname} [{enemyClass}]: Я поехавший, бегите!",
                        $"{playerNickname} [{enemyClass}]: Огонь тебе в харю!",
                        $"{playerNickname} [{enemyClass}]: Ща поджарим, не ###!",
                        $"{playerNickname} [{enemyClass}]: Жги, пока живой, чувак!",
                        $"{playerNickname} [{enemyClass}]: Пламя уже летит, держись!",
                        $"{playerNickname} [{enemyClass}]: Гори-гори, босс!",
                        $"{playerNickname} [{enemyClass}]: Подгони мне спички, ха!",
                        $"{playerNickname} [{enemyClass}]: Это тебе фейерверк, лови!",
                        $"{playerNickname} [{enemyClass}]: Жарко будет, пацаны!",
                        $"{playerNickname} [{enemyClass}]: Ща устроим пожарчик!",
                        $"{playerNickname} [{enemyClass}]: Огонь — мой корешок!",
                        $"{playerNickname} [{enemyClass}]: Поджёг и расслабился!",
                        $"{playerNickname} [{enemyClass}]: Полыхай, красавчик!",
                        $"{playerNickname} [{enemyClass}]: Ща все сгорит на #####!",
                        $"{playerNickname} [{enemyClass}]: #####, жарю всех!",
                        $"{playerNickname} [{enemyClass}]: Ща поджарим до корочки!",
                        $"{playerNickname} [{enemyClass}]: Огонь в деле, держись там!",
                        $"{playerNickname} [{enemyClass}]: Гори, пока не сгоришь!"
                    };
                case "Cleric":
                    return new string[] {
                        $"{playerNickname} [{enemyClass}]: Живи, не кашляй, чувак!",
                        $"{playerNickname} [{enemyClass}]: Ща подлатаю, держись там!",
                        $"{playerNickname} [{enemyClass}]: Лечу, не благодари, пацаны!",
                        $"{playerNickname} [{enemyClass}]: Стой, я помогу, не ###!",
                        $"{playerNickname} [{enemyClass}]: Вытяну тебя, браток!",
                        $"{playerNickname} [{enemyClass}]: Жизнь тебе, бери, не стесняйся!",
                        $"{playerNickname} [{enemyClass}]: Свет в помощь, держись!",
                        $"{playerNickname} [{enemyClass}]: Ща все будут как новые!",
                        $"{playerNickname} [{enemyClass}]: Не дохни, я рядом, чувак!",
                        $"{playerNickname} [{enemyClass}]: Лечение в деле, не падай!",
                        $"{playerNickname} [{enemyClass}]: Выживем, пацаны, я тут!",
                        $"{playerNickname} [{enemyClass}]: Ща заживим раны, погоди!",
                        $"{playerNickname} [{enemyClass}]: Не сдавайтесь, я вас вытяну!",
                        $"{playerNickname} [{enemyClass}]: Подлатаю, и в бой!",
                        $"{playerNickname} [{enemyClass}]: #####, выживай, брат!",
                        $"{playerNickname} [{enemyClass}]: Ща всех подниму с колен!",
                        $"{playerNickname} [{enemyClass}]: Лечу, не ной, пацан!",
                        $"{playerNickname} [{enemyClass}]: Живём дальше, держись!"
                    };
                case "Guardian":
                    return new string[] {
                        $"{playerNickname} [{enemyClass}]: Щит держит, пацаны!",
                        $"{playerNickname} [{enemyClass}]: Я твой танк, вали его!",
                        $"{playerNickname} [{enemyClass}]: Босс, ты мне не пробьёшь!",
                        $"{playerNickname} [{enemyClass}]: Стой за мной, не ###!",
                        $"{playerNickname} [{enemyClass}]: Ща приму удар, держись!",
                        $"{playerNickname} [{enemyClass}]: Держу фронт, бей дальше!",
                        $"{playerNickname} [{enemyClass}]: Я стена, попробуй, чувак!",
                        $"{playerNickname} [{enemyClass}]: Бей через меня, давай!",
                        $"{playerNickname} [{enemyClass}]: Щит в деле, не бойся!",
                        $"{playerNickname} [{enemyClass}]: Я как скала, босс!",
                        $"{playerNickname} [{enemyClass}]: Прикрываю, мочи его!",
                        $"{playerNickname} [{enemyClass}]: Ща замедлю эту тварь!",
                        $"{playerNickname} [{enemyClass}]: Кто тут танк? Я, ясно!",
                        $"{playerNickname} [{enemyClass}]: Не пройдёшь, держу позицию!",
                        $"{playerNickname} [{enemyClass}]: #####, я несокрушим!",
                        $"{playerNickname} [{enemyClass}]: Щит на месте, вали его!",
                        $"{playerNickname} [{enemyClass}]: Я держу, пацаны, давай!",
                        $"{playerNickname} [{enemyClass}]: Босс, ты мне не конкурент!"
                    };
                case "Bard":
                    return new string[] {
                        $"{playerNickname} [{enemyClass}]: Ща заиграю, держись, босс!",
                        $"{playerNickname} [{enemyClass}]: Музыка в деле, пацаны!",
                        $"{playerNickname} [{enemyClass}]: Песня тащит, бегай быстрее!",
                        $"{playerNickname} [{enemyClass}]: Гитара рвёт, вали его!",
                        $"{playerNickname} [{enemyClass}]: Подпевай или вали, чувак!",
                        $"{playerNickname} [{enemyClass}]: Ща будет ритм, не ###!",
                        $"{playerNickname} [{enemyClass}]: Баллада в ход пошла!",
                        $"{playerNickname} [{enemyClass}]: Играю, и ты в ауте!",
                        $"{playerNickname} [{enemyClass}]: Моя песня — твой кошмар!",
                        $"{playerNickname} [{enemyClass}]: Ща зажгу под бит!",
                        $"{playerNickname} [{enemyClass}]: Музыка — мой кореш!",
                        $"{playerNickname} [{enemyClass}]: Под мою мелодию пляши!",
                        $"{playerNickname} [{enemyClass}]: Босс, тебе ноты в рыло!",
                        $"{playerNickname} [{enemyClass}]: Играю, и мы победим!",
                        $"{playerNickname} [{enemyClass}]: #####, под мою музыку!",
                        $"{playerNickname} [{enemyClass}]: Ща все забегают от бита!",
                        $"{playerNickname} [{enemyClass}]: Гитара в деле, мочи его!",
                        $"{playerNickname} [{enemyClass}]: Под мой бит всех порвём!",
                        $"{playerNickname} [{enemyClass}]: Ща спою, и ты сдохнешь!",
                        $"{playerNickname} [{enemyClass}]: Музыка качает, держись!",
                        $"{playerNickname} [{enemyClass}]: Босс, тебе припев в харю!",
                        $"{playerNickname} [{enemyClass}]: Играю на нервах, чувак!",
                        $"{playerNickname} [{enemyClass}]: Баллада для боя, врубай!",
                        $"{playerNickname} [{enemyClass}]: Ща мотивчик зажгу!"
                    };
    default:
                    return new string[] { $"{playerNickname} [{enemyClass}]: Ща начнётся, держись!" };
            }
        }
}
