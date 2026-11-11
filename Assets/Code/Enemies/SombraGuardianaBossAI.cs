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
    private static Material attackLineMaterial;
    private static Sprite dashSmokeSprite;
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
    private float bobOffset;
    private Vector3 bossBaseScale;
    private float stunnedUntil;

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
        bossBaseScale = new Vector3(2.4f, 2.4f, 1f);
        transform.localScale = bossBaseScale;
        OnBossHealthChanged?.Invoke(healthComp.currentHealth, healthComp.maxHealth, currentPhase);
    }

    protected override void Update()
    {
        base.Update();
        if (currentState != AIState.Dead && Time.time < stunnedUntil)
        {
            ChangeState(AIState.Hurt);
        }
    }

    private void LateUpdate()
    {
        if (currentState == AIState.Dead) return;

        float nextBob = currentState == AIState.ChargeAttack || currentState == AIState.ExecuteAttack
            ? 0f
            : Mathf.Sin(Time.time * 2.5f) * 0.08f;
        transform.position += Vector3.up * (nextBob - bobOffset);
        bobOffset = nextBob;

        if (currentState != AIState.ChargeAttack && currentState != AIState.ExecuteAttack)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 2.5f) * 0.025f;
            transform.localScale = bossBaseScale * pulse;
        }
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
        BlueCrossSealNode[] sellos = FindObjectsByType<BlueCrossSealNode>();
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

    // ─── Fase 3: Bersérker — corrompe los sellos que aún no estén purificados ─
    private IEnumerator IniciarFase3Berserker()
    {
        foreach (BlueCrossSealNode s in FindObjectsByType<BlueCrossSealNode>())
            if (!s.purificado) s.Corromper();
        yield return null;
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
        if (currentState == AIState.Dead) return;
        stunnedUntil = Time.time + 3.5f;
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
        if (attackCycleCounter % 4 == 0)
        {
            yield return StartCoroutine(LanzarJuicioAbismal());
            yield break;
        }

        float distancia = Vector2.Distance(transform.position, playerTarget.position);
        if (distancia < 3.5f)
        {
            switch (attackCycleCounter % 3)
            {
                case 0:
                    yield return StartCoroutine(PisotonProfano());
                    break;
                case 1:
                    yield return StartCoroutine(ZarpazoDoble());
                    break;
                default:
                    yield return StartCoroutine(EmbestidaDeSombras());
                    break;
            }
        }
        else if (attackCycleCounter % 3 == 1)
        {
            yield return StartCoroutine(RafagaMeteoros());
        }
        else if (attackCycleCounter % 3 == 2)
        {
            yield return StartCoroutine(RayoDeVacio());
        }
        else
        {
            yield return StartCoroutine(EmbestidaDeSombras());
        }
    }

    private IEnumerator ZarpazoDoble()
    {
        Color colorBase = sr != null ? sr.color : Color.white;
        float carga = 0f;
        while (carga < 0.4f)
        {
            carga += Time.deltaTime;
            if (sr != null) sr.color = Mathf.PingPong(carga * 8f, 1f) > 0.5f
                ? new Color(0.75f, 0.2f, 1f)
                : colorBase;
            yield return null;
        }

        if (playerTarget == null) yield break;
        Vector2 haciaRonan = (playerTarget.position - transform.position).normalized;
        AplicarGolpe(haciaRonan, 2.1f, attackDamage);
        yield return new WaitForSeconds(0.22f);
        AplicarGolpe(Quaternion.Euler(0f, 0f, -55f) * haciaRonan, 2.2f, attackDamage * 1.15f);
        if (sr != null) sr.color = colorBase;
    }

    private IEnumerator EmbestidaDeSombras()
    {
        if (playerTarget == null) yield break;
        Vector2 inicio = transform.position;
        Vector2 direccion = ((Vector2)playerTarget.position - inicio).normalized;
        float distanciaObjetivo = Mathf.Min(7f, Vector2.Distance(inicio, playerTarget.position));
        float carga = 0f;
        Vector3 escalaInicial = transform.localScale;

        while (carga < 0.35f)
        {
            carga += Time.deltaTime;
            transform.localScale = new Vector3(escalaInicial.x * 0.82f, escalaInicial.y * 0.88f, escalaInicial.z);
            yield return null;
        }

        transform.localScale = escalaInicial;
        Vector2 origenRay = inicio + direccion * 1.2f;
        RaycastHit2D obstaculo = Physics2D.Raycast(origenRay, direccion, Mathf.Max(0f, distanciaObjetivo - 1.2f), LayerMask.GetMask("Default"));
        bool chocoConBorde = obstaculo.collider != null && !obstaculo.collider.isTrigger &&
                             obstaculo.collider.GetComponent<PlayerMovement>() == null &&
                             obstaculo.collider.GetComponent<BaseEnemyAI>() == null;
        if (chocoConBorde)
        {
            distanciaObjetivo = Mathf.Min(distanciaObjetivo, Mathf.Max(0.25f, obstaculo.distance - 0.8f));
        }

        Vector2 destino = inicio + direccion * distanciaObjetivo;
        float duracion = Mathf.Max(0.15f, distanciaObjetivo / 18f);
        float transcurrido = 0f;
        bool impactoAplicado = false;
        while (transcurrido < duracion)
        {
            transcurrido += Time.deltaTime;
            transform.position = Vector2.Lerp(inicio, destino, Mathf.Clamp01(transcurrido / duracion));
            if (!impactoAplicado && playerTarget != null &&
                Vector2.Distance(transform.position, playerTarget.position) < 1.8f)
            {
                AplicarDanioDirecto(attackDamage * 1.4f, (Vector2)playerTarget.position - inicio, 9f);
                impactoAplicado = true;
            }
            if (Time.frameCount % 3 == 0) CrearHumoDeEmbestida(transform.position);
            yield return null;
        }

        if (chocoConBorde)
        {
            stunnedUntil = Time.time + 1.2f;
            ChangeState(AIState.Hurt);
            if (sr != null) sr.color = new Color(0.45f, 0.85f, 1f);
            yield return new WaitForSeconds(1.2f);
            if (sr != null) sr.color = baseColor;
        }
    }

    private IEnumerator RafagaMeteoros()
    {
        if (playerTarget == null) yield break;
        Vector2 haciaRonan = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
        float angulo = Mathf.Atan2(haciaRonan.y, haciaRonan.x) * Mathf.Rad2Deg;
        for (int i = 0; i < 5; i++)
        {
            float offset = -30f + i * 15f;
            float rad = (angulo + offset) * Mathf.Deg2Rad;
            Vector2 direccion = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            SpawnReflectableProjectile(direccion, 4.2f, attackDamage * 0.85f,
                new Color(0.06f, 0.015f, 0.12f), new Color(0.75f, 0.2f, 1f), 0.85f,
                ProjectileBehaviour.ProjectileType.Boss_Shadow_Meteor);
        }
        yield return new WaitForSeconds(0.3f);
    }

    private IEnumerator RayoDeVacio()
    {
        if (playerTarget == null) yield break;
        Vector2 origen = transform.position;
        Vector2 objetivo = playerTarget.position;
        Vector2 direccion = (objetivo - origen).normalized;
        float anguloInicial = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        GameObject aviso = CrearLineaJefe("Aviso_Rayo_Vacio", new Color(0.95f, 0.08f, 0.28f, 0.85f), 0.12f);
        LineRenderer linea = aviso.GetComponent<LineRenderer>();
        float longitud = Mathf.Min(14f, Vector2.Distance(origen, objetivo) + 2f);

        float carga = 0f;
        while (carga < 0.8f)
        {
            carga += Time.deltaTime;
            ActualizarLinea(linea, origen, anguloInicial, longitud);
            yield return null;
        }

        linea.startColor = new Color(0.75f, 0.1f, 1f, 0.95f);
        linea.endColor = Color.cyan;
        linea.startWidth = 0.48f;
        linea.endWidth = 0.18f;
        HashSet<PlayerMovement> impactados = new HashSet<PlayerMovement>();
        float tiempo = 0f;
        while (tiempo < 0.6f)
        {
            tiempo += Time.deltaTime;
            float angulo = anguloInicial + Mathf.Lerp(-7.5f, 7.5f, tiempo / 0.6f);
            ActualizarLinea(linea, origen, angulo, longitud);
            Vector2 dir = new Vector2(Mathf.Cos(angulo * Mathf.Deg2Rad), Mathf.Sin(angulo * Mathf.Deg2Rad));
            Vector2 centro = origen + dir * (longitud * 0.5f);
            foreach (Collider2D col in Physics2D.OverlapBoxAll(centro, new Vector2(longitud, 0.6f), angulo))
            {
                PlayerMovement target = col.GetComponent<PlayerMovement>() ?? col.GetComponentInParent<PlayerMovement>();
                if (target != null && impactados.Add(target))
                    target.RecibirDanio(attackDamage * 1.4f);
            }
            yield return null;
        }
        Destroy(aviso);
    }

    private IEnumerator PisotonProfano()
    {
        Vector3 suelo = transform.position;
        Vector3 escala = transform.localScale;
        float carga = 0f;
        while (carga < 0.45f)
        {
            carga += Time.deltaTime;
            transform.position = suelo + Vector3.up * Mathf.Sin(Mathf.Clamp01(carga / 0.45f) * Mathf.PI) * 1f;
            yield return null;
        }
        transform.position = suelo;
        CrearOndaExpansiva(suelo, attackDamage, 5.5f);
        AplicarGolpe(Vector2.zero, 1.1f, attackDamage * 0.8f);
        yield return new WaitForSeconds(0.45f);
        transform.localScale = escala;
    }

    private IEnumerator LanzarJuicioAbismal()
    {
        if (playerTarget == null) yield break;
        BlackCloverDemonRenderer render = GetComponent<BlackCloverDemonRenderer>();
        render?.ActivarPoseDeAtaque(1.1f);
        BossHealthBarUI.Instance?.MostrarBannerOvercarga("¡JUICIO ABISMAL! Devuelve la esfera con [K]");
        yield return new WaitForSeconds(0.65f);

        if (playerTarget == null) yield break;
        Vector2 haciaRonan = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
        SpawnReflectableProjectile(haciaRonan, 4.2f, attackDamage * 2f,
            new Color(0.20f, 0.04f, 0.30f), new Color(1f, 0.78f, 0.25f), 2f,
            ProjectileBehaviour.ProjectileType.Boss_Mega_Orb);
        yield return new WaitForSeconds(0.25f);
    }

    private void AplicarGolpe(Vector2 direccion, float distancia, float dano)
    {
        Vector2 centro = (Vector2)transform.position + direccion.normalized * distancia;
        foreach (Collider2D col in Physics2D.OverlapCircleAll(centro, 1.25f))
        {
            PlayerMovement target = col.GetComponent<PlayerMovement>() ?? col.GetComponentInParent<PlayerMovement>();
            if (target != null)
                AplicarDanioDirecto(dano, (Vector2)target.transform.position - (Vector2)transform.position, 7f);
        }
    }

    private void AplicarDanioDirecto(float dano, Vector2 direccion, float empuje)
    {
        if (playerMovement == null) LocatePlayer();
        if (playerMovement == null) return;
        playerMovement.RecibirDanio(dano);
        Rigidbody2D rbJugador = playerMovement.GetComponent<Rigidbody2D>();
        if (rbJugador != null && direccion.sqrMagnitude > 0.01f)
            rbJugador.AddForce(direccion.normalized * empuje, ForceMode2D.Impulse);
    }

    private void CrearHumoDeEmbestida(Vector2 posicion)
    {
        GameObject humo = new GameObject("Humo_Embestida_Abismal");
        humo.transform.position = posicion;
        humo.transform.localScale = Vector3.one * 0.48f;
        SpriteRenderer rendererHumo = humo.AddComponent<SpriteRenderer>();
        if (dashSmokeSprite == null)
            dashSmokeSprite = CreateCircleSprite(new Color(0.20f, 0.04f, 0.32f, 0.55f));
        rendererHumo.sprite = dashSmokeSprite;
        rendererHumo.sortingOrder = 4;
        Destroy(humo, 0.45f);
    }

    private GameObject CrearLineaJefe(string nombre, Color color, float ancho)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(transform, true);
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = ancho;
        line.endWidth = ancho * 0.6f;
        line.startColor = color;
        line.endColor = new Color(color.r, color.g, color.b, 0.2f);
        line.sharedMaterial = ObtenerMaterialDeAtaque();
        line.sortingOrder = 30;
        return go;
    }

    private static void ActualizarLinea(LineRenderer linea, Vector2 origen, float angulo, float longitud)
    {
        float rad = angulo * Mathf.Deg2Rad;
        Vector3 fin = origen + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * longitud;
        linea.SetPosition(0, origen);
        linea.SetPosition(1, fin);
    }

    private void CrearOndaExpansiva(Vector2 centro, float dano, float radio)
    {
        GameObject onda = new GameObject("Onda_Profana");
        LineRenderer linea = onda.AddComponent<LineRenderer>();
        linea.positionCount = 49;
        linea.useWorldSpace = true;
        linea.loop = true;
        linea.startWidth = 0.18f;
        linea.endWidth = 0.18f;
        linea.startColor = new Color(1f, 0.35f, 0.85f, 0.95f);
        linea.endColor = new Color(0.25f, 0.8f, 1f, 0.85f);
        linea.sharedMaterial = ObtenerMaterialDeAtaque();
        linea.sortingOrder = 18;
        BossShockwaveEffect effect = onda.AddComponent<BossShockwaveEffect>();
        effect.Iniciar(centro, dano, radio, linea);
    }

    private static Material ObtenerMaterialDeAtaque()
    {
        if (attackLineMaterial == null)
            attackLineMaterial = new Material(Shader.Find("Sprites/Default"));
        return attackLineMaterial;
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

public class BossShockwaveEffect : MonoBehaviour
{
    private readonly HashSet<PlayerMovement> impactados = new HashSet<PlayerMovement>();
    private Vector2 centro;
    private float dano;
    private float radioMaximo;
    private float tiempo;
    private float radioAnterior;
    private LineRenderer linea;

    public void Iniciar(Vector2 posicion, float cantidadDanio, float radio, LineRenderer rendererLinea)
    {
        centro = posicion;
        dano = cantidadDanio;
        radioMaximo = radio;
        linea = rendererLinea;
    }

    private void Update()
    {
        tiempo += Time.deltaTime;
        float progreso = Mathf.Clamp01(tiempo / 1.15f);
        float radioActual = Mathf.Lerp(0.35f, radioMaximo, progreso);

        for (int i = 0; i < linea.positionCount; i++)
        {
            float angulo = i / (float)(linea.positionCount - 1) * Mathf.PI * 2f;
            linea.SetPosition(i, centro + new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * radioActual);
        }

        foreach (Collider2D collider in Physics2D.OverlapCircleAll(centro, radioActual))
        {
            PlayerMovement player = collider.GetComponent<PlayerMovement>() ?? collider.GetComponentInParent<PlayerMovement>();
            if (player == null || impactados.Contains(player)) continue;

            float distancia = Vector2.Distance(centro, player.transform.position);
            if (distancia <= radioActual && distancia >= radioAnterior - 0.45f)
            {
                impactados.Add(player);
                player.RecibirDanio(dano);
                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.AddForce(((Vector2)player.transform.position - centro).normalized * 7f, ForceMode2D.Impulse);
            }
        }

        radioAnterior = radioActual;
        if (progreso >= 1f)
        {
            enabled = false;
            Destroy(gameObject, 0.15f);
        }
    }
}
