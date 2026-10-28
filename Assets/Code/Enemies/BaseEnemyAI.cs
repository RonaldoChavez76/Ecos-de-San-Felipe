using System.Collections;
using UnityEngine;

/// <summary>
/// Máquina de estados base para todos los enemigos de Ecos de San Felipe.
/// Gestiona patrullaje, persecución, anticipación/carga de ataque, ejecución, daño, retroceso y muerte.
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

    protected Transform playerTarget;
    protected PlayerMovement playerMovement;
    protected SpriteRenderer sr;
    protected HealthComponent healthComp;
    protected Vector3 spawnOrigin;
    protected Vector3 currentPatrolPoint;
    protected float cooldownTimer = 0f;
    protected float stateTimer = 0f;
    protected Color baseColor = Color.white;

    protected virtual void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;

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
            playerMovement = FindFirstObjectByType<PlayerMovement>();
            if (playerMovement != null) playerTarget = playerMovement.transform;
        }
    }

    protected virtual void Update()
    {
        if (currentState == AIState.Dead) return;
        if (playerTarget == null) LocatePlayer();

        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        stateTimer += Time.deltaTime;

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
                if (stateTimer >= 0.22f)
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
            transform.localScale = new Vector3(startScale.x * (1f + 0.15f * p), startScale.y * (1f + 0.15f * p), startScale.z);
            yield return null;
        }

        transform.localScale = startScale;
        if (sr != null) sr.color = baseColor;

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
        ChangeState(AIState.Dead);
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
