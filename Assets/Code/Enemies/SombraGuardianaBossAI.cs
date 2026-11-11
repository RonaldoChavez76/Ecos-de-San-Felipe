using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IA del Jefe Final: La Sombra Guardiana en la cima del Cerro del Fraile.
/// Combate multi-fase (100%, 60%, 25% HP) que evalúa dominio de Contraataque (0.20s), Esquive y Modo Asalto.
/// </summary>
public class SombraGuardianaBossAI : BaseEnemyAI
{
    public enum BossPhase { Phase1_Awakened = 1, Phase2_Tempest = 2, Phase3_AbyssalFury = 3 }

    [Header("Configuración del Jefe Final")]
    public BossPhase currentPhase = BossPhase.Phase1_Awakened;
    public bool isShieldedBySeals = false;
    public int echoIndexOnDefeat = 3; // Eco de la Memoria (3er Eco principal)

    public event Action<float, float, BossPhase> OnBossHealthChanged;
    public event Action OnBossDefeated;

    private int attackCycleCounter = 0;
    private bool phase2Announced = false;
    private bool phase3Announced = false;

    protected override void Awake()
    {
        enemyDisplayName = "La Sombra Guardiana — Custodia del Olvido";
        maxHealth = 240f;
        moveSpeed = 2.8f;
        detectionRange = 14.0f;
        attackRange = 6.5f;
        attackDamage = 18f;
        chargeTime = 0.34f;
        attackCooldown = 1.85f;
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        transform.localScale = new Vector3(1.35f, 1.35f, 1f);
        OnBossHealthChanged?.Invoke(healthComp.currentHealth, healthComp.maxHealth, currentPhase);
    }

    protected override void HandleDamaged(float dmg, Vector2 knockbackDir)
    {
        if (isShieldedBySeals && healthComp != null)
        {
            // Si los sellos de la Cruz Azul aún no han sido purificados, mitiga el daño
            healthComp.Heal(dmg * 0.85f);
        }

        base.HandleDamaged(dmg, knockbackDir);
        EvaluateBossPhase();
        if (healthComp != null)
        {
            OnBossHealthChanged?.Invoke(healthComp.currentHealth, healthComp.maxHealth, currentPhase);
        }
    }

    private void EvaluateBossPhase()
    {
        if (healthComp == null) return;
        float ratio = healthComp.currentHealth / Mathf.Max(1f, healthComp.maxHealth);

        if (ratio <= 0.30f && currentPhase != BossPhase.Phase3_AbyssalFury)
        {
            currentPhase = BossPhase.Phase3_AbyssalFury;
            moveSpeed = 3.9f;
            attackCooldown = 1.15f;
            chargeTime = 0.24f;
            if (!phase3Announced)
            {
                phase3Announced = true;
                StartCoroutine(PhaseTransitionBurst(new Color(1f, 0.15f, 0.45f, 1f)));
                // Fase 3: despliega onda de choque + corrompe todos los sellos
                StartCoroutine(IniciarFase3Berserker());
            }
        }
        else if (ratio <= 0.65f && ratio > 0.30f && currentPhase == BossPhase.Phase1_Awakened)
        {
            currentPhase = BossPhase.Phase2_Tempest;
            moveSpeed = 3.3f;
            attackCooldown = 1.45f;
            chargeTime = 0.28f;
            if (!phase2Announced)
            {
                phase2Announced = true;
                StartCoroutine(PhaseTransitionBurst(new Color(0.75f, 0.25f, 1f, 1f)));
                // Fase 2: corrompe 2 sellos aleatorios y activa escudo de regeneración
                StartCoroutine(IniciarFase2Escudo());
            }
        }
    }

    // ─── Fase 2: Escudo de regeneración + sellos corrompidos ──────────────────
    private IEnumerator IniciarFase2Escudo()
    {
        // Corromper 2 sellos aleatorios de la Cruz Azul
        BlueCrossSealNode[] sellos = FindObjectsByType<BlueCrossSealNode>(FindObjectsSortMode.None);
        List<BlueCrossSealNode> disponibles = new List<BlueCrossSealNode>(sellos);
        int corrompidos = 0;
        while (corrompidos < 2 && disponibles.Count > 0)
        {
            int idx = UnityEngine.Random.Range(0, disponibles.Count);
            if (!disponibles[idx].purificado)
            {
                disponibles[idx].Corromper();
                corrompidos++;
            }
            disponibles.RemoveAt(idx);
        }

        isShieldedBySeals = true;
        yield return null;
    }

    // ─── Fase 3: Bersérker — cada 12 s lanza la Gran Esfera del Juicio ───────
    private IEnumerator IniciarFase3Berserker()
    {
        // Corromper todos los sellos restantes
        foreach (BlueCrossSealNode s in FindObjectsByType<BlueCrossSealNode>(FindObjectsSortMode.None))
            if (!s.purificado) s.Corromper();

        while (currentState != AIState.Dead)
        {
            yield return new WaitForSeconds(12f);
            if (currentState == AIState.Dead) yield break;
            if (playerTarget == null) continue;

            // Spawnear la Gran Esfera del Juicio de la Sombra (Boss_Mega_Orb)
            Vector2 toPlayer = (playerTarget.position - transform.position).normalized;
            SpawnReflectableProjectile(toPlayer, 4.5f, attackDamage * 2.5f,
                new Color(0.10f, 0f, 0.20f), new Color(1f, 0.1f, 0.3f), 1.1f,
                ProjectileBehaviour.ProjectileType.Boss_Mega_Orb);

            BossHealthBarUI.Instance?.MostrarBannerOvercarga("¡JUICIO DE LA SOMBRA! Contraataca la Gran Esfera con [K]");
        }
    }

    // ─── Aturdimiento por contraataque de la Gran Esfera ─────────────────────
    /// <summary>
    /// Llamado por ProjectileBehaviour cuando Ronan devuelve la Boss_Mega_Orb.
    /// Aplica daño directo y aturde al jefe durante 3.5 s.
    /// </summary>
    public void AturdirPorContraataqueMegaEsfera(float dmg)
    {
        if (healthComp != null)
            healthComp.TakeDamage(dmg, Vector2.zero, 8f, null);
        StartCoroutine(EstadoAturdido(3.5f));
        BossHealthBarUI.Instance?.MostrarBannerOvercarga("¡JEFE ATURDIDO! ¡Ataca con Modo Asalto [L]!");
    }

    private IEnumerator EstadoAturdido(float duracion)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color colorBase = sr != null ? sr.color : Color.white;
        ChangeState(AIState.Hurt);
        if (sr != null) sr.color = new Color(0.4f, 0.9f, 1f, 1f);
        yield return new WaitForSeconds(duracion);
        if (sr != null) sr.color = colorBase;
        if (currentState != AIState.Dead) ChangeState(AIState.Chase);
    }

    private IEnumerator PhaseTransitionBurst(Color burstColor)
    {
        for (int i = 0; i < 8; i++)
        {
            float ang = i * 45f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            SpawnReflectableProjectile(dir, 5.8f, attackDamage * 0.8f, new Color(0.18f, 0.04f, 0.28f), burstColor, 0.55f);
        }
        yield return null;
    }

    protected override IEnumerator PerformAttackAction()
    {
        if (playerTarget == null) yield break;

        attackCycleCounter++;
        Vector2 toPlayer = (playerTarget.position - transform.position).normalized;
        float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

        BlackCloverDemonRenderer demonRend = GetComponent<BlackCloverDemonRenderer>();
        if (demonRend != null) demonRend.ActivarPoseDeAtaque(0.65f);

        if (currentPhase == BossPhase.Phase1_Awakened)
        {
            if (attackCycleCounter % 2 == 1)
            {
                // Ataque 1: Orbe abismal directo (ideal para Contraataque de Ronan en 0.20s)
                SpawnReflectableProjectile(toPlayer, 6.8f, attackDamage, new Color(0.15f, 0.02f, 0.25f), new Color(0.85f, 0.25f, 1f), 0.65f);
            }
            else
            {
                // Ataque 2: Estocada sombría cuerpo a cuerpo
                yield return StartCoroutine(BossLungeSlash(toPlayer, 1.8f));
            }
        }
        else if (currentPhase == BossPhase.Phase2_Tempest)
        {
            if (attackCycleCounter % 2 == 1)
            {
                // Ráfaga triple en abanico
                float[] fan = { -18f, 0f, 18f };
                foreach (float a in fan)
                {
                    float rad = (baseAngle + a) * Mathf.Deg2Rad;
                    Vector2 d = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                    SpawnReflectableProjectile(d, 7.2f, attackDamage, new Color(0.18f, 0.02f, 0.28f), new Color(1f, 0.20f, 0.65f), 0.60f);
                }
            }
            else
            {
                yield return StartCoroutine(BossLungeSlash(toPlayer, 2.3f));
            }
        }
        else
        {
            // Fase 3: Furia Abismal (abanico de 5 orbes + estocada veloz)
            float[] fan5 = { -28f, -14f, 0f, 14f, 28f };
            foreach (float a in fan5)
            {
                float rad = (baseAngle + a) * Mathf.Deg2Rad;
                Vector2 d = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                SpawnReflectableProjectile(d, 7.6f, attackDamage * 1.1f, new Color(0.22f, 0.02f, 0.15f), new Color(1f, 0.15f, 0.35f), 0.62f);
            }
            yield return new WaitForSeconds(0.18f);
            yield return StartCoroutine(BossLungeSlash(toPlayer, 2.6f));
        }

        yield return new WaitForSeconds(0.25f);
    }

    private IEnumerator BossLungeSlash(Vector2 dir, float lungeDistance)
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + (Vector3)(dir * lungeDistance);
        float t = 0f;
        while (t < 0.20f)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, targetPos, t / 0.20f);
            yield return null;
        }

        if (playerTarget != null && Vector2.Distance(transform.position, playerTarget.position) <= 1.85f)
        {
            playerMovement?.RecibirDanio(attackDamage * 1.15f);
        }
    }

    protected override void HandleDeath()
    {
        base.HandleDeath();
        ResourcePickup.SpawnDrops(transform.position, 1.0f, 60, 100, 1.0f, 10, 1.0f, 60f, 1.0f, 50f);
        OnBossDefeated?.Invoke();

        // Liberar el Eco de la Memoria al derrotar a la Sombra Guardiana
        GameObject ecoObj = new GameObject("Eco_De_La_Memoria_Final");
        ecoObj.transform.position = transform.position;
        CircleCollider2D colEco = ecoObj.AddComponent<CircleCollider2D>();
        colEco.isTrigger = true;
        colEco.radius = 0.75f;
        EchoCollectible echo = ecoObj.AddComponent<EchoCollectible>();
        echo.indiceEco = echoIndexOnDefeat;
        echo.nombreEco = "Eco de la Memoria";
        Destroy(gameObject, 0.25f);
    }
}
