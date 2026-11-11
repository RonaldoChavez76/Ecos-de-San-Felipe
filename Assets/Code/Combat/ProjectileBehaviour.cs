using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla el comportamiento de proyectiles enemigos diferenciados por tipo:
///  • Fireball_Curse: Trayectoria recta con halo ígneo ardiente.
///  • Abyssal_Orb: Trayectoria oscilatoria senoidal con halo místico turquesa.
///  • Shadow_Arrow: Proyectil recto veloz en forma de saeta oscura.
///  • Spore_Ball: Orbe vegetal que al impactar genera un área de esporas venenosas por 1.5s.
///  • Boss_Mega_Orb: Gran Esfera Cósmica del Jefe Final. Obliga a contraataque [K],
///                   rebotando para infligir 80 de daño y aturdir a La Sombra Guardiana durante 3.5s.
/// </summary>
public class ProjectileBehaviour : MonoBehaviour
{
    public enum ProjectileType
    {
        Fireball_Curse,
        Abyssal_Orb,
        Shadow_Arrow,
        Spore_Ball,
        Boss_Mega_Orb
    }

    [Header("Tipo y Parámetros del Proyectil")]
    public ProjectileType projectileType = ProjectileType.Fireball_Curse;
    public float speed = 7.5f;
    public float damage = 18f;
    public float lifeTime = 5f;
    public bool isCounterable = true;
    public string targetTag = "Player";

    [Header("Efectos")]
    public Color normalColor = new Color(0.6f, 0.1f, 0.8f, 1f);
    public Color deflectedColor = new Color(0f, 0.9f, 1f, 1f);
    public Transform haloFuego;

    // Movimiento senoidal para Abyssal_Orb
    private float spawnTime;
    private Vector2 startPosition;
    private Vector2 perpendicularDir;

    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    #region Generación Procedural de Sprites de Proyectiles
    public static Sprite ObtenerSpriteBolaDeFuegoSombra(Color colorExterior, Color colorNucleo)
    {
        string key = $"fire_{colorExterior.r:F2}_{colorExterior.g:F2}_{colorExterior.b:F2}_{colorExterior.a:F2}__{colorNucleo.r:F2}_{colorNucleo.g:F2}_{colorNucleo.b:F2}_{colorNucleo.a:F2}";
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        int size = 24;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2(11.5f, 11.5f);
        float radius = 11.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / radius;
                if (dist > 1.0f)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    Color c;
                    if (dist < 0.38f)
                    {
                        float t = dist / 0.38f;
                        c = Color.Lerp(colorNucleo, colorExterior, t);
                    }
                    else
                    {
                        float t = (dist - 0.38f) / 0.62f;
                        c = Color.Lerp(colorExterior, new Color(colorExterior.r * 0.7f, colorExterior.g * 0.15f, colorExterior.b * 0.1f, 0f), t);
                    }
                    tex.SetPixel(x, y, c);
                }
            }
        }
        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        spriteCache[key] = sp;
        return sp;
    }

    public static Sprite ObtenerSpriteOrbeAbisal()
    {
        string key = "abyssal_orb_violet";
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        int size = 24;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2(11.5f, 11.5f);
        float radius = 11.5f;

        Color violetCore = new Color(0.95f, 0.25f, 1.0f, 1f);
        Color abyssalEdge = new Color(0.25f, 0.05f, 0.45f, 1f);
        Color turquoiseRim = new Color(0.10f, 0.85f, 0.95f, 0.85f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / radius;
                if (dist > 1.0f)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else if (dist < 0.35f)
                {
                    tex.SetPixel(x, y, Color.Lerp(Color.white, violetCore, dist / 0.35f));
                }
                else if (dist < 0.75f)
                {
                    tex.SetPixel(x, y, Color.Lerp(violetCore, abyssalEdge, (dist - 0.35f) / 0.40f));
                }
                else
                {
                    tex.SetPixel(x, y, Color.Lerp(turquoiseRim, Color.clear, (dist - 0.75f) / 0.25f));
                }
            }
        }
        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        spriteCache[key] = sp;
        return sp;
    }

    public static Sprite ObtenerSpriteFlechaSombra()
    {
        string key = "shadow_arrow_sharp";
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        int size = 24;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                tex.SetPixel(x, y, Color.clear);
            }
        }

        // Astil de la saeta
        Color darkWood = new Color(0.18f, 0.12f, 0.22f, 1f);
        Color shadowTip = new Color(0.95f, 0.25f, 0.35f, 1f);
        for (int x = 4; x <= 18; x++)
        {
            tex.SetPixel(x, 11, darkWood);
            tex.SetPixel(x, 12, darkWood);
        }
        // Punta metálica afilada
        tex.SetPixel(19, 11, shadowTip);
        tex.SetPixel(19, 12, shadowTip);
        tex.SetPixel(20, 11, Color.white);
        tex.SetPixel(20, 12, Color.white);
        tex.SetPixel(18, 10, shadowTip);
        tex.SetPixel(18, 13, shadowTip);
        // Plumas traseras
        Color feather = new Color(0.65f, 0.15f, 0.85f, 0.95f);
        tex.SetPixel(3, 9, feather);
        tex.SetPixel(4, 10, feather);
        tex.SetPixel(3, 14, feather);
        tex.SetPixel(4, 13, feather);

        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        spriteCache[key] = sp;
        return sp;
    }

    public static Sprite ObtenerSpriteEsporaVeneno()
    {
        string key = "spore_ball_green";
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        int size = 24;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2(11.5f, 11.5f);
        float radius = 11.5f;

        Color limeCore = new Color(0.85f, 0.98f, 0.25f, 1f);
        Color toxicGreen = new Color(0.20f, 0.75f, 0.22f, 0.95f);
        Color darkSpore = new Color(0.08f, 0.35f, 0.12f, 0.70f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / radius;
                if (dist > 1.0f)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else if (dist < 0.40f)
                {
                    tex.SetPixel(x, y, Color.Lerp(Color.white, limeCore, dist / 0.40f));
                }
                else
                {
                    float t = (dist - 0.40f) / 0.60f;
                    Color c = Color.Lerp(toxicGreen, darkSpore, t);
                    tex.SetPixel(x, y, c);
                }
            }
        }
        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        spriteCache[key] = sp;
        return sp;
    }

    public static Sprite ObtenerSpriteMegaEsfera()
    {
        string key = "boss_mega_orb_cosmic";
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2(15.5f, 15.5f);
        float radius = 15.5f;

        Color cosmicCyan = new Color(0.35f, 0.95f, 1.0f, 1f);
        Color deepViolet = new Color(0.30f, 0.05f, 0.55f, 1f);
        Color darkCrimson = new Color(0.85f, 0.10f, 0.35f, 0.9f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / radius;
                if (dist > 1.0f)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else if (dist < 0.30f)
                {
                    tex.SetPixel(x, y, Color.Lerp(Color.white, cosmicCyan, dist / 0.30f));
                }
                else if (dist < 0.70f)
                {
                    tex.SetPixel(x, y, Color.Lerp(cosmicCyan, deepViolet, (dist - 0.30f) / 0.40f));
                }
                else
                {
                    tex.SetPixel(x, y, Color.Lerp(deepViolet, darkCrimson, (dist - 0.70f) / 0.30f));
                }
            }
        }
        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        spriteCache[key] = sp;
        return sp;
    }
    #endregion

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 currentDirection = Vector2.zero;
    private bool isDeflected = false;
    private bool hasImpacted = false;
    private GameObject originalShooter;
    private GameObject deflectedBy;
    private SanFelipeDemoBuilder builderRef;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = normalColor;
        spawnTime = Time.time;
        startPosition = transform.position;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void Launch(Vector2 direction, GameObject shooter, float projectileDamage = 18f, ProjectileType pType = ProjectileType.Fireball_Curse)
    {
        currentDirection = direction.normalized;
        damage = projectileDamage;
        originalShooter = shooter;
        projectileType = pType;
        spawnTime = Time.time;
        startPosition = transform.position;
        perpendicularDir = new Vector2(-currentDirection.y, currentDirection.x);

        // Configuración específica según el tipo
        ConfigurarParametrosPorTipo();

        if (rb != null)
        {
            rb.linearVelocity = currentDirection * speed;
        }

        float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void ConfigurarParametrosPorTipo()
    {
        switch (projectileType)
        {
            case ProjectileType.Fireball_Curse:
                speed = 8.0f;
                break;
            case ProjectileType.Abyssal_Orb:
                speed = 6.5f;
                break;
            case ProjectileType.Shadow_Arrow:
                speed = 11.5f;
                break;
            case ProjectileType.Spore_Ball:
                speed = 4.5f;
                break;
            case ProjectileType.Boss_Mega_Orb:
                speed = 5.0f;
                damage = 45f;
                lifeTime = 8f;
                isCounterable = true;
                break;
        }
    }

    public void Inicializar(Vector2 dir, Component origen, SanFelipeDemoBuilder builder = null)
    {
        currentDirection = dir.normalized;
        speed = 5.5f;
        damage = 15f;
        builderRef = builder;
        originalShooter = origen != null ? origen.gameObject : null;
        spawnTime = Time.time;
        startPosition = transform.position;
        perpendicularDir = new Vector2(-currentDirection.y, currentDirection.x);

        if (rb != null)
        {
            rb.linearVelocity = currentDirection * speed;
        }

        float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        if (hasImpacted) return;

        // Comportamiento de trayectoria según tipo
        if (!isDeflected && projectileType == ProjectileType.Abyssal_Orb)
        {
            float elapsed = Time.time - spawnTime;
            Vector2 forwardStep = startPosition + currentDirection * (speed * elapsed);
            float osc = Mathf.Sin(elapsed * 7.5f) * 0.45f;
            transform.position = (Vector3)(forwardStep + perpendicularDir * osc);
        }
        else
        {
            // Movimiento recto estándar
            if (rb == null || rb.bodyType == RigidbodyType2D.Kinematic)
            {
                transform.position += (Vector3)(currentDirection * speed * Time.deltaTime);
            }
        }

        // Pulso visual del halo de fuego
        if (haloFuego != null)
        {
            float pulseRate = projectileType == ProjectileType.Boss_Mega_Orb ? 12f : 8f;
            float pulseAmp = projectileType == ProjectileType.Boss_Mega_Orb ? 0.35f : 0.20f;
            float pulse = 1.25f + Mathf.PingPong(Time.time * pulseRate, pulseAmp);
            haloFuego.localScale = new Vector3(pulse, pulse, 1f);
        }

        // Detección activa de impactos
        float hitRadius = projectileType == ProjectileType.Boss_Mega_Orb ? 0.75f : 0.45f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null && hits[i].gameObject != gameObject)
            {
                ProcesarColision(hits[i]);
                if (hasImpacted) return;
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic && (!isDeflected || projectileType != ProjectileType.Abyssal_Orb))
        {
            rb.linearVelocity = currentDirection * speed;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!hasImpacted && collision != null)
        {
            ProcesarColision(collision);
        }
    }

    private void ProcesarColision(Collider2D collision)
    {
        if (hasImpacted || collision == null) return;

        // 1. Impacto con Ronan (cuando es proyectil enemigo)
        if (!isDeflected && (collision.CompareTag("Player") || collision.GetComponent<PlayerMovement>() != null))
        {
            PlayerMovement pm = collision.GetComponent<PlayerMovement>() ?? collision.GetComponentInParent<PlayerMovement>();
            CombatStateMachine csm = collision.GetComponent<CombatStateMachine>() ?? collision.GetComponentInParent<CombatStateMachine>();

            bool contraataco = false;
            if (pm != null && pm.IntentarContraataqueProyectil())
            {
                contraataco = true;
            }
            else if (isCounterable && csm != null && (csm.IsInCounterWindow || csm.CurrentState == CombatStateMachine.State.Block))
            {
                csm.TriggerCounter();
                if (pm != null) pm.AñadirCargaAsalto(35f);
                SaveStateManager.Instance?.RegistrarContraataqueExitoso();
                AudioManager.Instance?.PlayCounterSound();
                contraataco = true;
            }

            if (contraataco)
            {
                if (builderRef != null) builderRef.ReproducirSonidoContraataque();

                Vector2 dirRetorno = -currentDirection;
                if (originalShooter != null)
                {
                    dirRetorno = ((Vector2)originalShooter.transform.position - (Vector2)transform.position).normalized;
                }

                // Si es la mega esfera del jefe, multiplicamos el daño de retorno a 80 y aceleramos
                float speedMultiplier = projectileType == ProjectileType.Boss_Mega_Orb ? 2.0f : 1.6f;
                float dmgMultiplier = projectileType == ProjectileType.Boss_Mega_Orb ? 2.5f : 2.2f;

                Deflect(dirRetorno, speed * speedMultiplier, dmgMultiplier, collision.gameObject);

                if (pm != null)
                {
                    if (projectileType == ProjectileType.Boss_Mega_Orb)
                    {
                        pm.MostrarAvisoRecurso("⚡ ¡GRAN CONTRAATAQUE CÓSMICO! ¡JEFE ATURDIDO!");
                    }
                    else
                    {
                        pm.MostrarAvisoRecurso("⚡ ¡CONTRAATAQUE! Proyectil devuelto");
                    }
                }
                return;
            }

            // Daño a Ronan si no hubo contraataque
            HealthComponent hc = collision.GetComponent<HealthComponent>();
            if (hc != null)
            {
                hc.TakeDamage(damage, currentDirection, 4f, originalShooter);
            }
            else if (pm != null)
            {
                pm.RecibirDanio(damage);
            }

            if (projectileType == ProjectileType.Spore_Ball)
            {
                GenerarNubeEsporas(transform.position);
            }

            Impactar(collision.gameObject);
            return;
        }

        // 2. Impacto con enemigos (cuando el proyectil fue contraatacado y devuelto)
        if (isDeflected && (collision.CompareTag("Enemy") || collision.GetComponent<HealthComponent>() != null || collision.GetComponent<BaseEnemyAI>() != null))
        {
            if (collision.gameObject == deflectedBy) return;

            BaseEnemyAI enemyAI = collision.GetComponent<BaseEnemyAI>() ?? collision.GetComponentInParent<BaseEnemyAI>();
            SombraGuardianaBossAI boss = collision.GetComponent<SombraGuardianaBossAI>() ?? collision.GetComponentInParent<SombraGuardianaBossAI>();

            if (boss != null && projectileType == ProjectileType.Boss_Mega_Orb)
            {
                boss.AturdirPorContraataqueMegaEsfera(80f);
            }
            else if (enemyAI != null)
            {
                enemyAI.TakeCounterDamage(damage);
            }
            else
            {
                HealthComponent hc = collision.GetComponent<HealthComponent>();
                if (hc != null)
                {
                    hc.TakeDamage(damage, currentDirection, 6f, deflectedBy);
                }
            }

            if (projectileType == ProjectileType.Spore_Ball)
            {
                GenerarNubeEsporas(transform.position);
            }

            Impactar(collision.gameObject);
            return;
        }

        // 3. Impacto con obstáculos sólidos del entorno
        if (!collision.isTrigger && !collision.CompareTag("Player") && !collision.CompareTag("Enemy"))
        {
            if (projectileType == ProjectileType.Spore_Ball)
            {
                GenerarNubeEsporas(transform.position);
            }
            Impactar(collision.gameObject);
        }
    }

    public void Deflect(Vector2 newDirection, float newSpeed, float damageMultiplier, GameObject deflector)
    {
        isDeflected = true;
        currentDirection = newDirection.normalized;
        speed = newSpeed;
        damage *= damageMultiplier;
        deflectedBy = deflector;
        targetTag = "Enemy";

        if (rb != null)
        {
            rb.linearVelocity = currentDirection * speed;
        }

        float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (sr != null)
        {
            sr.color = deflectedColor;
        }

        if (haloFuego != null)
        {
            SpriteRenderer haloSr = haloFuego.GetComponent<SpriteRenderer>();
            if (haloSr != null)
            {
                haloSr.color = new Color(0.2f, 0.95f, 1.0f, 0.95f);
            }
            haloFuego.localScale = Vector3.one * 1.6f;
        }

        // Renovar tiempo de vida para que alcance a impactar al enemigo
        CancelInvoke();
        Destroy(gameObject, 4.5f);
    }

    private void GenerarNubeEsporas(Vector2 centro)
    {
        GameObject nube = new GameObject("Nube_Esporas_Venenosas");
        nube.transform.position = centro;
        nube.transform.localScale = Vector3.one * 1.5f;

        SpriteRenderer nSr = nube.AddComponent<SpriteRenderer>();
        nSr.sprite = ObtenerSpriteEsporaVeneno();
        nSr.color = new Color(0.35f, 0.85f, 0.20f, 0.45f);
        nSr.sortingOrder = 14;

        CircleCollider2D col = nube.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.70f;

        PoisonCloudEffect pce = nube.AddComponent<PoisonCloudEffect>();
        pce.duracion = 1.5f;
        pce.dps = 6f;
    }

    private void Impactar(GameObject target)
    {
        if (hasImpacted) return;
        hasImpacted = true;

        if (rb != null) rb.linearVelocity = Vector2.zero;
        Destroy(gameObject, 0.05f);
    }
}

/// <summary>
/// Efecto de nube tóxica temporal dejada por las esporas al impactar.
/// </summary>
public class PoisonCloudEffect : MonoBehaviour
{
    public float duracion = 1.5f;
    public float dps = 6f;
    private float tiempoInicio;

    private void Start()
    {
        tiempoInicio = Time.time;
        Destroy(gameObject, duracion);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement pm = other.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                pm.RecibirDanio(dps * Time.deltaTime);
            }
        }
    }
}
