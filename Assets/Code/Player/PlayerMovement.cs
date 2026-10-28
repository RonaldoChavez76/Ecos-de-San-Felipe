using System.Collections;
using UnityEngine;

/// <summary>
/// Controlador principal de Ronan para movimiento Top-Down 8 direcciones,
/// gestión de combate (Ataque, Bloqueo, Contraataque, Esquive) y Modo Asalto.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    public enum CombatState { Idle, Attack, Block, Counter, Dodge }

    [Header("Estado Actual")]
    public CombatState currentState = CombatState.Idle;

    [Header("Estadísticas Base (GDD Secciones 2.3 - 2.5)")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float baseAttack = 12f;
    public float moveSpeed = 4.5f;
    public float counterWindow = 0.20f;
    public float dodgeCooldown = 0.8f;

    [Header("Configuración de Combate")]
    public float attackDuration = 0.25f;
    public float attackRange = 1.1f;
    public float dodgeSpeed = 12f;
    public float dodgeDuration = 0.18f;

    [Header("Modo Asalto (Tecla L)")]
    public float assaultCharge = 0f;
    public float maxAssaultCharge = 100f;
    public float assaultDuration = 6f;
    public bool isAssaultActive = false;

    [Header("Controles")]
    public KeyCode attackKey = KeyCode.J;      // O Clic Izquierdo
    public KeyCode blockKey = KeyCode.K;       // O Clic Derecho
    public KeyCode dodgeKey = KeyCode.Space;   // Esquivar
    public KeyCode assaultKey = KeyCode.L;     // Activar Modo Asalto

    // Componentes internos
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private CombatStateMachine combatSM;
    private HealthComponent healthComp;

    // Vectores de dirección
    private Vector2 movement;
    private Vector2 lookDirection = Vector2.down; // Abajo por defecto
    public Vector2 LookDirection => lookDirection;

    // Tiempos y estados
    private float lastDodgeTime = -10f;
    private float blockStartTime = -10f;
    private bool isInvulnerable = false;

    // Efectos visuales de brazal / escudo
    private GameObject visualEffectObj;
    private SpriteRenderer effectSr;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        combatSM = GetComponent<CombatStateMachine>();
        if (combatSM == null) combatSM = gameObject.AddComponent<CombatStateMachine>();

        healthComp = GetComponent<HealthComponent>();
        if (healthComp == null)
        {
            healthComp = gameObject.AddComponent<HealthComponent>();
            healthComp.isPlayer = true;
        }
    }

    private void Start()
    {
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        // Sincronizar vida con el SaveStateManager (100 base + 25 por cada eco recuperado)
        if (SaveStateManager.Instance != null)
        {
            maxHealth = SaveStateManager.Instance.ObtenerVidaMaximaTotal();
            currentHealth = Mathf.Clamp(SaveStateManager.Instance.currentHealth, 1f, maxHealth);
            if (healthComp != null)
            {
                healthComp.maxHealth = maxHealth;
                healthComp.currentHealth = currentHealth;
            }
        }

        CrearEfectoVisualPorCodigo();

        if (combatSM != null)
        {
            combatSM.counterWindow = counterWindow;
            combatSM.attackDuration = attackDuration;
            combatSM.dodgeDuration = dodgeDuration;
            combatSM.dodgeCooldown = dodgeCooldown;
        }
    }

    private void Update()
    {
        if (currentHealth <= 0f)
        {
            movement = Vector2.zero;
            if (visualEffectObj != null && visualEffectObj.activeSelf) visualEffectObj.SetActive(false);
            return;
        }

        if (currentState == CombatState.Idle)
        {
            LeerMovimientoOchoDirecciones();
            LeerEntradasDeCombate();
        }
        else if (currentState == CombatState.Block)
        {
            if (!Input.GetKey(blockKey) && !Input.GetMouseButton(1))
            {
                SalirDeBloqueo();
            }
            else
            {
                bool enVentana = (Time.time - blockStartTime) <= counterWindow;
                if (effectSr != null)
                {
                    effectSr.color = enVentana ? new Color(0f, 0.9f, 1f, 0.9f) : new Color(0.2f, 0.5f, 0.8f, 0.45f);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (currentHealth <= 0f) return;

        if (currentState == CombatState.Idle)
        {
            float velocidadActual = isAssaultActive ? moveSpeed * 1.30f : moveSpeed;
            rb.MovePosition(rb.position + movement.normalized * velocidadActual * Time.fixedDeltaTime);
        }
        else if (currentState == CombatState.Dodge)
        {
            rb.MovePosition(rb.position + lookDirection * dodgeSpeed * Time.fixedDeltaTime);
        }
    }

    private void LateUpdate()
    {
        if (sr != null)
        {
            // Orden de renderizado dinámico Top-Down según el eje Y
            sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 10f);
            if (effectSr != null) effectSr.sortingOrder = sr.sortingOrder + 1;
        }
    }

    private void LeerMovimientoOchoDirecciones()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        if (movement != Vector2.zero)
        {
            lookDirection = movement.normalized;
            if (anim != null)
            {
                anim.SetFloat("X", movement.x);
                anim.SetFloat("Y", movement.y);
            }
        }

        if (anim != null)
        {
            anim.SetFloat("Speed", movement.sqrMagnitude);
        }
    }

    private void LeerEntradasDeCombate()
    {
        // 1. Ataque cuerpo a cuerpo
        if (Input.GetKeyDown(attackKey) || Input.GetMouseButtonDown(0))
        {
            StartCoroutine(RutinaAtaque());
            return;
        }

        // 2. Bloqueo / Contraataque
        if (Input.GetKeyDown(blockKey) || Input.GetMouseButtonDown(1))
        {
            EntrarEnBloqueo();
            return;
        }

        // 3. Esquive
        if (Input.GetKeyDown(dodgeKey) && Time.time >= lastDodgeTime + dodgeCooldown)
        {
            StartCoroutine(RutinaEsquive());
            return;
        }

        // 4. Modo Asalto
        if (Input.GetKeyDown(assaultKey) && assaultCharge >= maxAssaultCharge && !isAssaultActive)
        {
            StartCoroutine(RutinaModoAsalto());
        }
    }

    private IEnumerator RutinaAtaque()
    {
        SetState(CombatState.Attack);
        movement = Vector2.zero;
        if (anim != null) anim.SetFloat("Speed", 0f);
        if (visualEffectObj != null) visualEffectObj.SetActive(false);

        // Impulso hacia adelante
        rb.MovePosition(rb.position + lookDirection * 0.15f);

        float danioActual = isAssaultActive ? baseAttack * 1.60f : baseAttack;
        Vector2 puntoAtaque = (Vector2)transform.position + lookDirection * (attackRange * 0.65f);
        Collider2D[] impactados = Physics2D.OverlapCircleAll(puntoAtaque, attackRange * 0.60f);

        bool conectoGolpe = false;
        foreach (Collider2D col in impactados)
        {
            if (col.gameObject != gameObject && !col.isTrigger)
            {
                HealthComponent hc = col.GetComponent<HealthComponent>() ?? col.transform.root.GetComponent<HealthComponent>();
                if (hc != null && !col.CompareTag("Player"))
                {
                    hc.TakeDamage(danioActual, lookDirection, 5f, gameObject);
                    conectoGolpe = true;
                }
                else if (col.CompareTag("Enemy") || col.name.Contains("Sombra"))
                {
                    col.SendMessage("RecibirDanio", danioActual, SendMessageOptions.DontRequireReceiver);
                    conectoGolpe = true;
                }
            }
        }

        if (conectoGolpe)
        {
            AñadirCargaAsalto(10f);
        }

        yield return new WaitForSeconds(attackDuration);
        SetState(CombatState.Idle);
    }

    private void EntrarEnBloqueo()
    {
        SetState(CombatState.Block);
        movement = Vector2.zero;
        if (anim != null) anim.SetFloat("Speed", 0f);

        blockStartTime = Time.time;
        MostrarEfectoFrenteAlJugador(new Color(0f, 0.9f, 1f, 0.85f), new Vector3(0.45f, 0.9f, 1f));
    }

    private void SalirDeBloqueo()
    {
        if (visualEffectObj != null) visualEffectObj.SetActive(false);
        SetState(CombatState.Idle);
    }

    private IEnumerator RutinaEsquive()
    {
        SetState(CombatState.Dodge);
        lastDodgeTime = Time.time;
        isInvulnerable = true;

        if (sr != null) sr.color = new Color(0.5f, 0.9f, 1f, 0.6f);

        yield return new WaitForSeconds(dodgeDuration);

        isInvulnerable = false;
        if (sr != null) sr.color = isAssaultActive ? new Color(0.4f, 0.85f, 1f, 1f) : Color.white;
        SetState(CombatState.Idle);
    }

    private IEnumerator RutinaModoAsalto()
    {
        isAssaultActive = true;
        assaultCharge = 0f;
        AudioManager.Instance?.PlayAssaultSound();

        if (sr != null) sr.color = new Color(0.4f, 0.85f, 1f, 1f);

        yield return new WaitForSeconds(assaultDuration);

        isAssaultActive = false;
        if (sr != null) sr.color = Color.white;
    }

    public bool IntentarContraataqueProyectil()
    {
        if (currentState == CombatState.Block)
        {
            float tiempoDesdeBloqueo = Time.time - blockStartTime;
            if (tiempoDesdeBloqueo <= counterWindow)
            {
                StartCoroutine(EfectoContraataqueExitoso());
                AñadirCargaAsalto(35f);
                SaveStateManager.Instance?.RegistrarContraataqueExitoso();
                AudioManager.Instance?.PlayCounterSound();
                return true;
            }
        }
        return false;
    }

    private IEnumerator EfectoContraataqueExitoso()
    {
        SetState(CombatState.Counter);
        MostrarEfectoFrenteAlJugador(new Color(0f, 1f, 1f, 1f), new Vector3(1.2f, 1.2f, 1f));
        yield return new WaitForSeconds(0.18f);
        if (visualEffectObj != null) visualEffectObj.SetActive(false);
        SetState(CombatState.Idle);
    }

    public void RecibirDanio(float cantidad)
    {
        if (isInvulnerable) return;

        if (currentState == CombatState.Block)
        {
            float tiempoDesdeBloqueo = Time.time - blockStartTime;
            if (tiempoDesdeBloqueo <= counterWindow)
            {
                // Contraataque perfecto
                StartCoroutine(EfectoContraataqueExitoso());
                AñadirCargaAsalto(35f);
                return;
            }
            cantidad *= 0.30f; // Mitigación
        }

        currentHealth = Mathf.Clamp(currentHealth - cantidad, 0f, maxHealth);
        if (healthComp != null) healthComp.currentHealth = currentHealth;
        if (SaveStateManager.Instance != null) SaveStateManager.Instance.currentHealth = currentHealth;

        StartCoroutine(ParpadeoDanio());
    }

    private IEnumerator ParpadeoDanio()
    {
        if (sr != null)
        {
            sr.color = Color.red;
            yield return new WaitForSeconds(0.12f);
            sr.color = isAssaultActive ? new Color(0.4f, 0.85f, 1f, 1f) : Color.white;
        }
    }

    public void AgregarCargaAsalto(float cantidad) => AñadirCargaAsalto(cantidad);

    public void AñadirCargaAsalto(float cantidad)
    {
        if (!isAssaultActive)
        {
            assaultCharge = Mathf.Clamp(assaultCharge + cantidad, 0f, maxAssaultCharge);
        }
    }

    private void SetState(CombatState nuevoEstado)
    {
        currentState = nuevoEstado;
        if (combatSM != null)
        {
            combatSM.ChangeState((CombatStateMachine.State)nuevoEstado);
        }
    }

    private void CrearEfectoVisualPorCodigo()
    {
        visualEffectObj = new GameObject("Brazal_Shield_VFX");
        visualEffectObj.transform.SetParent(transform);
        visualEffectObj.transform.localPosition = Vector3.zero;

        effectSr = visualEffectObj.AddComponent<SpriteRenderer>();

        Texture2D tex = new Texture2D(16, 16);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                tex.SetPixel(x, y, dist <= 7.5f ? Color.white : Color.clear);
            }
        }
        tex.Apply();

        effectSr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        visualEffectObj.SetActive(false);
    }

    private void MostrarEfectoFrenteAlJugador(Color color, Vector3 escala)
    {
        if (visualEffectObj == null) return;
        visualEffectObj.SetActive(true);
        visualEffectObj.transform.localPosition = (Vector3)(lookDirection * 0.55f);
        float angulo = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
        visualEffectObj.transform.localRotation = Quaternion.Euler(0, 0, angulo);
        visualEffectObj.transform.localScale = escala;
        if (effectSr != null) effectSr.color = color;
    }

    private void OnGUI()
    {
        // HUD rápido de depuración cuando no hay canvas activo
        if (FindAnyObjectByType<Canvas>() == null)
        {
            GUI.Box(new Rect(15, 15, 260, 95), "ECOS DE SAN FELIPE - RONAN");
            GUI.Label(new Rect(25, 40, 240, 20), $"Vida: {currentHealth:0} / {maxHealth:0}");
            GUI.Label(new Rect(25, 60, 240, 20), $"Modo Asalto: {assaultCharge:0}% {(isAssaultActive ? "[¡ACTIVO!]" : "")}");
            GUI.Label(new Rect(25, 80, 240, 20), $"Estado: {currentState} | [J] [K] [Espacio] [L]");
        }
    }
}
