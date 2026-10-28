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
            rb.linearDamping = 15f;
            rb.angularDamping = 15f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
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
        if (rb == null) return;

        if (currentHealth <= 0f)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            return;
        }

        if (currentState == CombatState.Idle)
        {
            if (movement.sqrMagnitude > 0.001f)
            {
                float velocidadActual = isAssaultActive ? moveSpeed * 1.30f : moveSpeed;
                rb.MovePosition(rb.position + movement.normalized * velocidadActual * Time.fixedDeltaTime);
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
        else if (currentState == CombatState.Dodge)
        {
            rb.MovePosition(rb.position + lookDirection * dodgeSpeed * Time.fixedDeltaTime);
        }
        else
        {
            // En Attack, Block y Counter Ronan debe mantenerse firme sin deslizarse hacia atrás ni en diagonal
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
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
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        if (anim != null) anim.SetFloat("Speed", 0f);
        if (visualEffectObj != null) visualEffectObj.SetActive(false);

        AudioManager.Instance?.PlaySlashSound();

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
        if (rb != null) rb.linearVelocity = Vector2.zero;
        SetState(CombatState.Idle);
    }

    private void EntrarEnBloqueo()
    {
        SetState(CombatState.Block);
        movement = Vector2.zero;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
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

    public void CurarSalud(float cantidad)
    {
        if (currentHealth <= 0f) return;
        currentHealth = Mathf.Clamp(currentHealth + cantidad, 0f, maxHealth);
        if (healthComp != null) healthComp.currentHealth = currentHealth;
        if (SaveStateManager.Instance != null) SaveStateManager.Instance.currentHealth = currentHealth;

        StartCoroutine(ParpadeoCuracion());
    }

    private IEnumerator ParpadeoCuracion()
    {
        if (sr != null)
        {
            sr.color = new Color(0.35f, 1f, 0.45f, 1f);
            yield return new WaitForSeconds(0.18f);
            sr.color = isAssaultActive ? new Color(0.4f, 0.85f, 1f, 1f) : Color.white;
        }
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

    private static Texture2D whiteBarTex;

    private void OnGUI()
    {
        if (Time.timeScale == 0f) return;

        // Sincronizar con HealthComponent por si recibió daño directo desde HealthComponent.TakeDamage
        if (healthComp != null && Mathf.Abs(healthComp.currentHealth - currentHealth) > 0.01f)
        {
            currentHealth = Mathf.Clamp(healthComp.currentHealth, 0f, maxHealth);
        }

        if (whiteBarTex == null)
        {
            whiteBarTex = new Texture2D(1, 1);
            whiteBarTex.SetPixel(0, 0, Color.white);
            whiteBarTex.Apply();
        }

        float panelX = 16f;
        float panelY = 16f;
        float panelW = 350f;
        float panelH = 112f;

        Color prevColor = GUI.color;

        // Fondo oscuro semitransparente del HUD de Ronan
        GUI.color = new Color(0.08f, 0.07f, 0.11f, 0.88f);
        GUI.DrawTexture(new Rect(panelX, panelY, panelW, panelH), whiteBarTex);

        // Borde dorado colonial
        GUI.color = new Color(0.85f, 0.68f, 0.28f, 0.95f);
        GUI.DrawTexture(new Rect(panelX, panelY, panelW, 2f), whiteBarTex);
        GUI.DrawTexture(new Rect(panelX, panelY + panelH - 2f, panelW, 2f), whiteBarTex);
        GUI.DrawTexture(new Rect(panelX, panelY, 2f, panelH), whiteBarTex);
        GUI.DrawTexture(new Rect(panelX + panelW - 2f, panelY, 2f, panelH), whiteBarTex);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = false,
            clipping = TextClipping.Overflow,
            padding = new RectOffset(0, 0, 0, 0)
        };
        titleStyle.normal.textColor = new Color(1f, 0.90f, 0.55f);

        GUIStyle textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = false,
            clipping = TextClipping.Overflow,
            padding = new RectOffset(0, 0, 0, 0)
        };
        textStyle.normal.textColor = new Color(0.95f, 0.95f, 0.95f);

        GUI.color = Color.white;
        float hpPct = Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));
        bool lowHp = hpPct <= 0.30f;
        string headerAlert = lowHp ? "  <color=#FF4444>[¡VIDA BAJA!]</color>" : "";
        GUI.Label(new Rect(panelX + 12f, panelY + 6f, panelW - 20f, 22f), $"⚔ RONAN — GUARDIÁN DE SAN FELIPE{headerAlert}", titleStyle);

        // 1. Barra de Vida (HP)
        float barX = panelX + 12f;
        float hpBarY = panelY + 32f;
        float barW = panelW - 24f;
        float barH = 22f;

        GUI.color = new Color(0.22f, 0.08f, 0.08f, 0.95f);
        GUI.DrawTexture(new Rect(barX, hpBarY, barW, barH), whiteBarTex);

        Color hpColor = hpPct > 0.5f
            ? new Color(0.22f, 0.82f, 0.35f, 0.95f)
            : (hpPct > 0.3f ? new Color(0.95f, 0.72f, 0.15f, 0.95f) : new Color(0.95f, 0.18f, 0.18f, 0.75f + Mathf.PingPong(Time.time * 2.5f, 0.25f)));
        GUI.color = hpColor;
        GUI.DrawTexture(new Rect(barX + 2f, hpBarY + 2f, (barW - 4f) * hpPct, barH - 4f), whiteBarTex);

        GUI.color = Color.white;
        GUI.Label(new Rect(barX + 8f, hpBarY, barW - 12f, barH), $"♥ VIDA: {Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)} HP", textStyle);

        // 2. Barra de Modo Asalto
        float assaultBarY = panelY + 58f;
        float assaultPct = isAssaultActive ? 1f : Mathf.Clamp01(assaultCharge / Mathf.Max(1f, maxAssaultCharge));

        GUI.color = new Color(0.08f, 0.14f, 0.24f, 0.95f);
        GUI.DrawTexture(new Rect(barX, assaultBarY, barW, 20f), whiteBarTex);

        GUI.color = isAssaultActive
            ? new Color(0.20f, 0.95f, 1f, 0.8f + Mathf.PingPong(Time.time * 3f, 0.2f))
            : (assaultPct >= 1f ? new Color(0.15f, 0.85f, 1f, 0.95f) : new Color(0.18f, 0.52f, 0.88f, 0.95f));
        GUI.DrawTexture(new Rect(barX + 2f, assaultBarY + 2f, (barW - 4f) * assaultPct, 16f), whiteBarTex);

        GUI.color = Color.white;
        string estadoAsalto = isAssaultActive
            ? "¡MODO ASALTO ACTIVO! (+60% ATQ / +30% VEL)"
            : (assaultPct >= 1f ? "⚡ MODO ASALTO: 100% (¡LISTO!)" : $"⚡ MODO ASALTO: {assaultCharge:0}%");
        GUI.Label(new Rect(barX + 8f, assaultBarY, barW - 12f, 20f), estadoAsalto, textStyle);

        // 3. Estadísticas de Ronan (Ataque, Velocidad, Ventana Parry y Estado) sin cortarse
        float atkActual = isAssaultActive ? baseAttack * 1.60f : baseAttack;
        float spdActual = isAssaultActive ? moveSpeed * 1.30f : moveSpeed;

        GUIStyle statStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft,
            richText = true,
            wordWrap = false,
            clipping = TextClipping.Overflow,
            padding = new RectOffset(0, 0, 0, 0)
        };
        statStyle.normal.textColor = new Color(0.92f, 0.94f, 0.98f);

        GUI.Label(new Rect(barX + 2f, panelY + 82f, barW, 24f),
            $"<b>ATQ:</b> {atkActual:0.#}   |   <b>VEL:</b> {spdActual:0.#} u/s   |   <b>PARRY:</b> {counterWindow:0.00}s   |   <b>ESTADO:</b> {currentState}",
            statStyle);

        GUI.color = prevColor;
    }
}
