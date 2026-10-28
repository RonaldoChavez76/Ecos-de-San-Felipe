using System.Collections;
using UnityEngine;

/// <summary>
/// Controla el comportamiento de proyectiles oscuros lanzados por enemigos
/// y su desvío potenciado al ser contraatacados por el brazal de Ronan.
/// </summary>
public class ProjectileBehaviour : MonoBehaviour
{
    [Header("Parámetros del Proyectil")]
    public float speed = 7.5f;
    public float damage = 18f;
    public float lifeTime = 5f;
    public bool isCounterable = true;
    public string targetTag = "Player";

    [Header("Efectos")]
    public Color normalColor = new Color(0.6f, 0.1f, 0.8f, 1f); // Oscuro / Sombra
    public Color deflectedColor = new Color(0f, 0.9f, 1f, 1f);  // Azul Ecos luminoso

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 currentDirection = Vector2.zero;
    private bool isDeflected = false;
    private GameObject originalShooter;
    private SanFelipeDemoBuilder builderRef;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = normalColor;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void Launch(Vector2 direction, GameObject shooter, float projectileDamage = 18f)
    {
        currentDirection = direction.normalized;
        damage = projectileDamage;
        originalShooter = shooter;

        if (rb != null)
        {
            rb.linearVelocity = currentDirection * speed;
        }

        float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>
    /// Sobrecarga compatible con SanFelipeDemoBuilder.
    /// </summary>
    public void Inicializar(Vector2 dir, Component origen, SanFelipeDemoBuilder builder = null)
    {
        currentDirection = dir.normalized;
        speed = 5.5f;
        damage = 15f;
        builderRef = builder;
        originalShooter = origen != null ? origen.gameObject : null;

        if (rb != null)
        {
            rb.linearVelocity = currentDirection * speed;
        }

        float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        // Movimiento por Transform en caso de que el Rigidbody2D no esté configurado como dinámico
        if (rb == null || rb.bodyType == RigidbodyType2D.Kinematic)
        {
            transform.position += (Vector3)(currentDirection * speed * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
        {
            rb.linearVelocity = currentDirection * speed;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. Impacto con Ronan (cuando aún es proyectil enemigo)
        if (!isDeflected && (collision.CompareTag("Player") || collision.GetComponent<PlayerMovement>() != null))
        {
            PlayerMovement pm = collision.GetComponent<PlayerMovement>();
            CombatStateMachine csm = collision.GetComponent<CombatStateMachine>();

            // Comprobar ventana de contraataque (0.20s)
            bool contraataco = false;
            if (pm != null && pm.IntentarContraataqueProyectil())
            {
                contraataco = true;
            }
            else if (isCounterable && csm != null && csm.IsInCounterWindow)
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

                Deflect(dirRetorno, speed * 1.5f, 2.0f, collision.gameObject);
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

            ImpactAndDestroy();
            return;
        }

        // 2. Impacto con enemigos tras ser devuelto
        if (isDeflected && (collision.CompareTag("Enemy") || collision.gameObject == originalShooter || collision.name.Contains("Sombra")))
        {
            HealthComponent hc = collision.GetComponent<HealthComponent>() ?? collision.transform.root.GetComponent<HealthComponent>();
            if (hc != null)
            {
                hc.TakeDamage(damage, currentDirection, 6f, null);
            }
            else
            {
                collision.SendMessage("RecibirDanio", damage, SendMessageOptions.DontRequireReceiver);
            }

            Debug.Log($"<color=#00E5FF>[Projectile] ¡Proyectil devuelto impactó al enemigo con {damage:F1} de daño crítico!</color>");
            ImpactAndDestroy();
            return;
        }

        // 3. Impacto con muros y obstáculos
        if (collision.CompareTag("Obstacle") || collision.gameObject.layer == LayerMask.NameToLayer("Obstacles"))
        {
            ImpactAndDestroy();
        }
    }

    /// <summary>
    /// Invierte y potencia el proyectil al realizar un Contraataque perfecto.
    /// </summary>
    public void Deflect(Vector2 newDirection, float newSpeed, float damageMultiplier, GameObject newAttacker)
    {
        isDeflected = true;
        currentDirection = newDirection.normalized;
        speed = newSpeed;
        damage *= damageMultiplier;
        targetTag = "Enemy";

        if (sr != null)
        {
            sr.color = deflectedColor;
            sr.transform.localScale *= 1.4f;
        }

        float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
        {
            rb.linearVelocity = currentDirection * speed;
        }

        CrearDestelloAzulProcedural();
    }

    private void ImpactAndDestroy()
    {
        CrearEfectoImpacto();
        Destroy(gameObject);
    }

    private void CrearDestelloAzulProcedural()
    {
        GameObject flash = new GameObject("CounterFlash", typeof(SpriteRenderer));
        flash.transform.position = transform.position;
        SpriteRenderer fsr = flash.GetComponent<SpriteRenderer>();

        Texture2D tex = new Texture2D(32, 32);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16)) / 16f;
                float a = Mathf.Clamp01(1f - dist);
                tex.SetPixel(x, y, new Color(0.2f, 0.9f, 1f, a));
            }
        }
        tex.Apply();
        fsr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16);
        fsr.sortingOrder = 15;
        Destroy(flash, 0.15f);
    }

    private void CrearEfectoImpacto()
    {
        GameObject burst = new GameObject("BurstImpact", typeof(SpriteRenderer));
        burst.transform.position = transform.position;
        burst.transform.localScale = Vector3.one * 0.8f;
        SpriteRenderer bsr = burst.GetComponent<SpriteRenderer>();
        bsr.color = isDeflected ? deflectedColor : normalColor;
        bsr.sortingOrder = 12;

        Texture2D tex = new Texture2D(16, 16);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(8, 8)) / 8f;
                float a = Mathf.Clamp01(1f - dist);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        bsr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        Destroy(burst, 0.12f);
    }
}
