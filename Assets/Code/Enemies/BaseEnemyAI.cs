using System.Collections;
using UnityEngine;

/// <summary>
/// Máquina de estados base para todos los enemigos de Ecos de San Felipe.
/// Gestiona patrullaje, persecución, anticipación/carga de ataque, ejecución, daño,
/// animación de muerte, caída de corazones curativos y limpieza del mapa.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public abstract class BaseEnemyAI : MonoBehaviour
{
    public enum AIState
    {
        Patrol,
        Chase,
        ChargeAttack,
        ExecuteAttack,
        Recover,
        Hurt,
        Dead
    }

    [Header("Estado de IA")]
    public AIState currentState = AIState.Patrol;
    public string enemyDisplayName = "Enemigo Sombrío";

    [Header("Estadísticas de Combate")]
    public float maxHealth = 35f;
    public float moveSpeed = 2.6f;
    public float detectionRange = 8.5f;
    public float attackRange = 1.35f;
    public float attackDamage = 12f;
    public float chargeTime = 0.28f;
    public float attackCooldown = 1.4f;
    public float patrolRadius = 2.0f;

    [Header("Recompensa de Curación")]
    [Range(0f, 1f)] public float heartDropChance = 0.75f;
    public float heartHealAmount = 25f;

    protected Transform playerTarget;
    protected PlayerMovement playerMovement;
    protected SpriteRenderer sr;
    protected HealthComponent healthComp;
    protected EnemySpriteSheetAnimator sheetAnimator;
    protected Vector3 spawnOrigin;
    protected Vector3 currentPatrolPoint;
    protected float cooldownTimer = 0f;
    protected float stateTimer = 0f;
    protected Color baseColor = Color.white;
    private bool deathSequenceStarted = false;

    public Transform PlayerTarget => playerTarget;
    public float StateTimer => stateTimer;

    protected virtual void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
        sheetAnimator = GetComponent<EnemySpriteSheetAnimator>();

        healthComp = GetComponent<HealthComponent>();
        if (healthComp == null)
        {
            healthComp = gameObject.AddComponent<HealthComponent>();
        }
        healthComp.isPlayer = false;
        healthComp.maxHealth = maxHealth;
        healthComp.currentHealth = maxHealth;
        if (healthComp.onDamaged == null) healthComp.onDamaged = new UnityEngine.Events.UnityEvent<float>();
        if (healthComp.onDeath == null) healthComp.onDeath = new UnityEngine.Events.UnityEvent();
        healthComp.onDamaged.AddListener(dmg => HandleDamaged(dmg, Vector2.zero));
        healthComp.onDeath.AddListener(HandleDeath);

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null && GetComponent<Collider2D>() == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.45f;
        }
    }

    protected virtual void Start()
    {
        spawnOrigin = transform.position;
        ChooseNewPatrolPoint();
        LocatePlayer();
    }

    protected void LocatePlayer()
    {
        if (playerMovement == null)
        {
            playerMovement = FindAnyObjectByType<PlayerMovement>();
            if (playerMovement != null) playerTarget = playerMovement.transform;
        }
    }

    protected virtual void Update()
    {
        stateTimer += Time.deltaTime;
        if (currentState == AIState.Dead) return;

        if (playerTarget == null) LocatePlayer();
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        // Orden de renderizado dinámico Top-Down según eje Y
        if (sr != null)
        {
            sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 10f);
        }

        if (playerTarget == null || (playerMovement != null && playerMovement.currentHealth <= 0f))
        {
            UpdatePatrol();
            return;
        }

        float distToPlayer = Vector2.Distance(transform.position, playerTarget.position);
        OrientTowards(playerTarget.position);

        switch (currentState)
        {
            case AIState.Patrol:
                if (distToPlayer <= detectionRange)
                {
                    ChangeState(AIState.Chase);
                }
                else
                {
                    UpdatePatrol();
                }
                break;

            case AIState.Chase:
                if (distToPlayer > detectionRange * 1.35f)
                {
                    ChangeState(AIState.Patrol);
                }
                else if (distToPlayer <= attackRange && cooldownTimer <= 0f)
                {
                    StartCoroutine(AttackSequenceRoutine());
                }
                else
                {
                    ChaseBehavior(distToPlayer);
                }
                break;

            case AIState.Recover:
                if (stateTimer >= 0.35f)
                {
                    ChangeState(AIState.Chase);
                }
                break;

            case AIState.Hurt:
                if (stateTimer >= 0.24f)
                {
                    ChangeState(AIState.Chase);
                }
                break;
        }
    }

    protected virtual void UpdatePatrol()
    {
        transform.position = Vector3.MoveTowards(transform.position, currentPatrolPoint, (moveSpeed * 0.55f) * Time.deltaTime);
        if (Vector2.Distance(transform.position, currentPatrolPoint) < 0.2f || stateTimer > 3.5f)
        {
            ChooseNewPatrolPoint();
            stateTimer = 0f;
        }
    }

    protected void ChooseNewPatrolPoint()
    {
        Vector2 offset = Random.insideUnitCircle * patrolRadius;
        currentPatrolPoint = spawnOrigin + new Vector3(offset.x, offset.y, 0f);
    }

    protected virtual void ChaseBehavior(float distToPlayer)
    {
        if (distToPlayer > attackRange * 0.88f)
        {
            Vector2 dir = (playerTarget.position - transform.position).normalized;
            transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);
        }
    }

    protected virtual void OrientTowards(Vector3 targetPos)
    {
        if (sr == null) return;
        if (sheetAnimator == null) sheetAnimator = GetComponent<EnemySpriteSheetAnimator>();

        // Las hojas Puny 24x8 ya incluyen las 8 direcciones dibujadas en sus 8 filas; no deben voltearse con flipX
        if (sheetAnimator != null && sheetAnimator.isPuny24x8Format)
        {
            sr.flipX = false;
            return;
        }

        float dx = targetPos.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.15f)
        {
            sr.flipX = dx < 0f;
        }
    }

    protected virtual IEnumerator AttackSequenceRoutine()
    {
        ChangeState(AIState.ChargeAttack);

        // Fase 1: Anticipación / Carga visible
        OnStartAttackCharge();
        Vector3 startScale = transform.localScale;
        float t = 0f;
        while (t < chargeTime && currentState == AIState.ChargeAttack)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / Mathf.Max(0.05f, chargeTime));
            if (sr != null)
            {
                sr.color = Color.Lerp(baseColor, new Color(1f, 0.45f, 0.25f, 1f), Mathf.PingPong(p * 4f, 1f));
            }
            transform.localScale = new Vector3(startScale.x * (1f + 0.12f * p), startScale.y * (1f + 0.12f * p), startScale.z);
            yield return null;
        }

        transform.localScale = startScale;
        if (sr != null && currentState != AIState.Dead) sr.color = baseColor;

        if (currentState != AIState.ChargeAttack) yield break;

        // Fase 2: Ejecutar el ataque (cuerpo a cuerpo o proyectil)
        ChangeState(AIState.ExecuteAttack);
        yield return StartCoroutine(PerformAttackAction());

        cooldownTimer = attackCooldown;
        if (currentState == AIState.ExecuteAttack)
        {
            ChangeState(AIState.Recover);
        }
    }

    protected virtual void OnStartAttackCharge()
    {
        BlackCloverDemonRenderer demonRend = GetComponent<BlackCloverDemonRenderer>();
        if (demonRend != null)
        {
            demonRend.ActivarPoseDeAtaque(chargeTime + 0.30f);
        }
    }

    protected abstract IEnumerator PerformAttackAction();

    public virtual void RecibirDanio(float cantidad)
    {
        if (healthComp != null)
        {
            Vector2 dir = playerTarget != null ? (Vector2)(transform.position - playerTarget.position).normalized : Vector2.up;
            healthComp.TakeDamage(cantidad, dir, 3.5f);
        }
    }

    protected virtual void HandleDamaged(float dmg, Vector2 knockbackDir)
    {
        if (currentState == AIState.Dead) return;
        ChangeState(AIState.Hurt);
    }

    protected virtual void HandleDeath()
    {
        if (deathSequenceStarted) return;
        deathSequenceStarted = true;
        ChangeState(AIState.Dead);
        StopAllCoroutines();
        StartCoroutine(DeathAnimationAndCleanupRoutine());
    }

    private IEnumerator DeathAnimationAndCleanupRoutine()
    {
        // Desactivar colisionadores inmediatamente para que el cadáver no estorbe a Ronan
        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D c in cols)
        {
            if (c != null) c.enabled = false;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        // Soltar corazón curativo con probabilidad configurada
        if (Random.value <= heartDropChance)
        {
            HeartPickup.SpawnHeart(transform.position, heartHealAmount);
        }

        if (sr != null) sr.color = baseColor;

        // Permitir que EnemySpriteSheetAnimator reproduzca la animación completa de muerte (caída / desintegración)
        yield return new WaitForSeconds(0.65f);

        // Desvanecimiento suave antes de eliminar del mapa
        float fadeDuration = 0.35f;
        float elapsed = 0f;
        Color startCol = sr != null ? sr.color : Color.white;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (sr != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                sr.color = new Color(startCol.r, startCol.g, startCol.b, alpha);
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    protected void ChangeState(AIState next)
    {
        currentState = next;
        stateTimer = 0f;
    }

    protected GameObject SpawnReflectableProjectile(Vector2 direction, float speed, float damage, Color outerColor, Color coreColor, float scale = 0.52f)
    {
        GameObject proj = new GameObject("Proyectil_Enemigo");
        proj.transform.position = transform.position + (Vector3)(direction.normalized * 0.45f);
        proj.transform.localScale = new Vector3(scale, scale, 1f);

        SpriteRenderer projSr = proj.AddComponent<SpriteRenderer>();
        projSr.sprite = CreateCircleSprite(outerColor);
        projSr.sortingOrder = (sr != null ? sr.sortingOrder : 4) + 2;

        GameObject core = new GameObject("Nucleo");
        core.transform.SetParent(proj.transform);
        core.transform.localPosition = Vector3.zero;
        core.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
        SpriteRenderer coreSr = core.AddComponent<SpriteRenderer>();
        coreSr.sprite = CreateCircleSprite(coreColor);
        coreSr.sortingOrder = projSr.sortingOrder + 1;

        CircleCollider2D col = proj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.38f;

        ProjectileBehaviour pb = proj.AddComponent<ProjectileBehaviour>();
        pb.speed = speed;
        pb.Launch(direction.normalized, gameObject, damage);
        return proj;
    }

    public static Sprite CreateCircleSprite(Color col)
    {
        Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                tex.SetPixel(x, y, d <= 7f ? col : Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
    }
}

/// <summary>
/// Corazón curativo recolectable que usa el sprite real Assets/Sprites/Heart.png (celda 0,0 de 16x16)
/// para restaurar la salud de Ronan durante la exploración y los combates por oleadas.
/// </summary>
public class HeartPickup : MonoBehaviour
{
    public float healAmount = 25f;
    private Vector3 basePos;
    private float spawnTime;
    private static Sprite cachedHeartSprite;

    public static GameObject SpawnHeart(Vector3 position, float heal = 25f, Transform parent = null)
    {
        GameObject go = new GameObject("Corazon_Curativo");
        if (parent != null) go.transform.SetParent(parent);
        go.transform.position = position;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetHeartSprite();
        sr.sortingOrder = Mathf.RoundToInt(-position.y * 10f) + 2;
        go.transform.localScale = new Vector3(1.25f, 1.25f, 1f);

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.48f;

        HeartPickup hp = go.AddComponent<HeartPickup>();
        hp.healAmount = heal;
        return go;
    }

    public static Sprite GetHeartSprite()
    {
        if (cachedHeartSprite != null) return cachedHeartSprite;

        Texture2D tex = ZoneWaveSpawner.LoadSpriteTexture("Heart");
        if (tex != null && tex.width >= 16 && tex.height >= 16)
        {
            // Heart.png es de 80x16 (5 cuadros de 16x16: col=4 en x=64 es el corazón lleno rojo brillante)
            int w = 16;
            int h = 16;
            Texture2D sub = new Texture2D(w, h, TextureFormat.RGBA32, false);
            sub.filterMode = FilterMode.Point;
            sub.wrapMode = TextureWrapMode.Clamp;
            if (tex.isReadable)
            {
                sub.SetPixels(tex.GetPixels(64, 0, w, h));
                sub.Apply();
                cachedHeartSprite = Sprite.Create(sub, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
                return cachedHeartSprite;
            }
        }

        cachedHeartSprite = BaseEnemyAI.CreateCircleSprite(new Color(0.95f, 0.18f, 0.25f, 1f));
        return cachedHeartSprite;
    }

    private void Start()
    {
        basePos = transform.position;
        spawnTime = Time.time;
    }

    private void Update()
    {
        float bob = Mathf.Sin((Time.time - spawnTime) * 4.2f) * 0.10f;
        float pulse = 1.20f + Mathf.Sin((Time.time - spawnTime) * 5.5f) * 0.10f;
        transform.position = basePos + new Vector3(0f, bob, 0f);
        transform.localScale = new Vector3(pulse, pulse, 1f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement pm = other.GetComponent<PlayerMovement>();
        if (pm == null) return;

        // Si Ronan ya tiene la vida al máximo, aún puede recogerlo si le falta aunque sea 1 HP
        if (pm.currentHealth >= pm.maxHealth) return;

        pm.CurarSalud(healAmount);
        AudioManager.Instance?.PlayEchoSound();
        Destroy(gameObject);
    }
}
