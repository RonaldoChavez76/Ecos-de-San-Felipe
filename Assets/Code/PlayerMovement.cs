using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public enum CombatState { Idle, Attack, Block, Counter, Dodge }

    [Header("Estado Actual (Solo lectura)")]
    public CombatState currentState = CombatState.Idle;

    [Header("Estadísticas Base (Tabla 4 del Documento)")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float baseAttack = 12f;
    public float moveSpeed = 4.5f;
    public float counterWindow = 0.20f;
    public float dodgeCooldown = 0.8f;

    [Header("Configuración de Combate y Esquive")]
    public float attackDuration = 0.25f;
    public float attackRange = 0.9f;
    public float dodgeSpeed = 12f;
    public float dodgeDuration = 0.18f;

    [Header("Modo Asalto (Tabla 3 y 4)")]
    public float assaultCharge = 0f;
    public float maxAssaultCharge = 100f;
    public float assaultDuration = 6f;
    public bool isAssaultActive = false;

    [Header("Controles (Teclado y Mouse)")]
    public KeyCode attackKey = KeyCode.J;      // También Clic Izquierdo
    public KeyCode blockKey = KeyCode.K;       // También Clic Derecho
    public KeyCode dodgeKey = KeyCode.Space;   // Esquivar
    public KeyCode assaultKey = KeyCode.L;     // Activar Modo Asalto

    // Componentes internos
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;

    // Vectores de dirección
    private Vector2 movement;
    private Vector2 lookDirection = Vector2.down; // Mirando hacia abajo por defecto

    // Control de tiempos y estados
    private float lastDodgeTime = -10f;
    private float blockStartTime;
    private bool isInvulnerable = false;

    // Efectos visuales generados por código (Brazal / Corte de espada)
    private GameObject visualEffectObj;
    private SpriteRenderer effectSr;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        // Configuración física ideal para Top-Down estilo Zelda
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        CrearEfectoVisualPorCodigo();
    }

    void Update()
    {
        // Si Ronan cae derrotado, bloquear entradas y apagar efectos visuales
        if (currentHealth <= 0f)
        {
            movement = Vector2.zero;
            if (visualEffectObj != null && visualEffectObj.activeSelf) visualEffectObj.SetActive(false);
            return;
        }

        // Solo leemos movimiento o nuevas acciones cuando estamos en Idle
        if (currentState == CombatState.Idle)
        {
            LeerMovimientoOchoDirecciones();
            LeerEntradasDeCombate();
        }
        else if (currentState == CombatState.Block)
        {
            // Mantener bloqueo hasta soltar la tecla K o Clic Derecho
            if (!Input.GetKey(blockKey) && !Input.GetMouseButton(1))
            {
                SalirDeBloqueo();
            }
            else
            {
                // Cambia el color del escudo: Azul brillante en ventana de Counter (0.20s), azul suave en bloqueo normal
                bool enVentana = (Time.time - blockStartTime) <= counterWindow;
                effectSr.color = enVentana ? new Color(0f, 0.9f, 1f, 0.85f) : new Color(0.2f, 0.5f, 0.8f, 0.45f);
            }
        }
    }

    void FixedUpdate()
    {
        if (currentHealth <= 0f) return;

        if (currentState == CombatState.Idle)
        {
            // Si el Modo Asalto está activo, la velocidad sube un 30%
            float velocidadActual = isAssaultActive ? moveSpeed * 1.30f : moveSpeed;
            rb.MovePosition(rb.position + movement.normalized * velocidadActual * Time.fixedDeltaTime);
        }
        else if (currentState == CombatState.Dodge)
        {
            // Desplazamiento rápido durante el esquive
            rb.MovePosition(rb.position + lookDirection * dodgeSpeed * Time.fixedDeltaTime);
        }
    }

    void LateUpdate()
    {
        // Orden de renderizado dinámico Top-Down: Ronan pasa por delante o detrás de objetos según su Y
        if (sr != null)
        {
            sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 10);
            if (effectSr != null) effectSr.sortingOrder = sr.sortingOrder + 1;
        }
    }

    // ---------------- MOVIMIENTO 8 DIRECCIONES ----------------
    private void LeerMovimientoOchoDirecciones()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // ¡Ya NO bloqueamos las diagonales! Ahora avanza en 8 direcciones como Zelda.
        if (movement != Vector2.zero)
        {
            lookDirection = movement.normalized;

            if (anim != null)
            {
                // Enviamos los valores redondeados (-1, 0, 1) al Blend Tree para las 8 direcciones
                anim.SetFloat("X", movement.x);
                anim.SetFloat("Y", movement.y);
            }
        }

        if (anim != null)
        {
            anim.SetFloat("Speed", movement.sqrMagnitude);
        }
    }

    // ---------------- ENTRADAS DE COMBATE ----------------
    private void LeerEntradasDeCombate()
    {
        // 1. Ataque Cuerpo a Cuerpo (Tecla J o Clic Izquierdo)
        if (Input.GetKeyDown(attackKey) || Input.GetMouseButtonDown(0))
        {
            StartCoroutine(RutinaAtaque());
            return;
        }

        // 2. Bloqueo / Contraataque (Tecla K o Clic Derecho)
        if (Input.GetKeyDown(blockKey) || Input.GetMouseButtonDown(1))
        {
            EntrarEnBloqueo();
            return;
        }

        // 3. Esquive con enfriamiento de 0.8s (Espacio)
        if (Input.GetKeyDown(dodgeKey) && Time.time >= lastDodgeTime + dodgeCooldown)
        {
            StartCoroutine(RutinaEsquive());
            return;
        }

        // 4. Modo Asalto (Tecla L cuando la barra llega a 100)
        if (Input.GetKeyDown(assaultKey) && assaultCharge >= maxAssaultCharge && !isAssaultActive)
        {
            StartCoroutine(RutinaModoAsalto());
        }
    }

    // ---------------- ESTADOS DE LA MÁQUINA DE COMBATE ----------------
    private IEnumerator RutinaAtaque()
    {
        currentState = CombatState.Attack;
        movement = Vector2.zero;
        if (anim != null) anim.SetFloat("Speed", 0f);

        // Nos aseguramos de que el círculo no tape la animación real de la espada
        if (visualEffectObj != null) visualEffectObj.SetActive(false);

        // Pequeño impulso hacia adelante al dar el espadazo para que se sienta con peso
        rb.MovePosition(rb.position + lookDirection * 0.15f);

        // Calcular daño (+60% si está en Modo Asalto)
        float danioActual = isAssaultActive ? baseAttack * 1.60f : baseAttack;

        // Detectar enemigos golpeados frente a Ronan
        Vector2 puntoAtaque = (Vector2)transform.position + lookDirection * (attackRange * 0.65f);
        Collider2D[] impactados = Physics2D.OverlapCircleAll(puntoAtaque, attackRange * 0.55f);

        foreach (Collider2D col in impactados)
        {
            if (col.gameObject != gameObject)
            {
                col.SendMessage("RecibirDanio", danioActual, SendMessageOptions.DontRequireReceiver);
                AgregarCargaAsalto(10f);
            }
        }

        yield return new WaitForSeconds(attackDuration);

        currentState = CombatState.Idle;
    }

    private void EntrarEnBloqueo()
    {
        currentState = CombatState.Block;
        movement = Vector2.zero;
        if (anim != null) anim.SetFloat("Speed", 0f);

        blockStartTime = Time.time;

        // Mostrar el escudo energético del brazal frente a Ronan
        MostrarEfectoFrenteAlJugador(new Color(0f, 0.9f, 1f, 0.85f), new Vector3(0.45f, 0.9f, 1f));
    }

    private void SalirDeBloqueo()
    {
        visualEffectObj.SetActive(false);
        currentState = CombatState.Idle;
    }

    private IEnumerator RutinaEsquive()
    {
        currentState = CombatState.Dodge;
        lastDodgeTime = Time.time;
        isInvulnerable = true;

        // Efecto visual semitransparente al esquivar
        if (sr != null) sr.color = new Color(0.5f, 0.9f, 1f, 0.6f);

        yield return new WaitForSeconds(dodgeDuration);

        isInvulnerable = false;
        if (sr != null) sr.color = isAssaultActive ? new Color(0.4f, 0.85f, 1f, 1f) : Color.white;
        currentState = CombatState.Idle;
    }

    private IEnumerator RutinaModoAsalto()
    {
        isAssaultActive = true;
        assaultCharge = 0f;
        if (sr != null) sr.color = new Color(0.4f, 0.85f, 1f, 1f); // Brillo azul de los ecos

        yield return new WaitForSeconds(assaultDuration);

        isAssaultActive = false;
        if (sr != null) sr.color = Color.white;
    }

    // ---------------- LÓGICA DE CONTRAATAQUE Y DAÑO ----------------
    // Este método lo llaman los proyectiles enemigos al chocar con Ronan
    public bool IntentarContraataqueProyectil()
    {
        if (currentState == CombatState.Block)
        {
            float tiempoDesdeBloqueo = Time.time - blockStartTime;
            if (tiempoDesdeBloqueo <= counterWindow)
            {
                // ¡CONTRAATAQUE EXITOSO DENTRO DE LOS 0.20 SEGUNDOS!
                StartCoroutine(EfectoContraataqueExitoso());
                AgregarCargaAsalto(25f);
                return true;
            }
        }
        return false;
    }

    private IEnumerator EfectoContraataqueExitoso()
    {
        currentState = CombatState.Counter;
        MostrarEfectoFrenteAlJugador(new Color(0f, 1f, 1f, 1f), new Vector3(1.1f, 1.1f, 1f));
        yield return new WaitForSeconds(0.15f);
        visualEffectObj.SetActive(false);
        currentState = CombatState.Idle;
    }

    public void RecibirDanio(float cantidad)
    {
        if (isInvulnerable) return;

        // Si está bloqueando fuera de la ventana de 0.20s, reduce el daño a la mitad
        if (currentState == CombatState.Block)
        {
            cantidad *= 0.5f;
        }

        currentHealth = Mathf.Clamp(currentHealth - cantidad, 0f, maxHealth);
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

    public void AgregarCargaAsalto(float cantidad)
    {
        if (!isAssaultActive)
        {
            assaultCharge = Mathf.Clamp(assaultCharge + cantidad, 0f, maxAssaultCharge);
        }
    }

    // ---------------- GENERADOR DE EFECTOS VISUALES POR CÓDIGO ----------------
    private void CrearEfectoVisualPorCodigo()
    {
        visualEffectObj = new GameObject("EfectoBrazalYEspada");
        visualEffectObj.transform.SetParent(transform);
        effectSr = visualEffectObj.AddComponent<SpriteRenderer>();
        effectSr.sortingOrder = 10;

        // Creamos una textura pixel-art de 16x16 desde código para el escudo/espada
        Texture2D tex = new Texture2D(16, 16);
        tex.filterMode = FilterMode.Point;
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
        visualEffectObj.SetActive(true);
        visualEffectObj.transform.localPosition = (Vector3)(lookDirection * 0.55f);
        float angulo = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
        visualEffectObj.transform.localRotation = Quaternion.Euler(0, 0, angulo);
        visualEffectObj.transform.localScale = escala;
        effectSr.color = color;
    }

    // HUD rápido en pantalla para que veas tu Vida, Modo Asalto y Estado sin tener que armar un Canvas todavía
    void OnGUI()
    {
        GUI.Box(new Rect(15, 15, 250, 95), "ECOS DE SAN FELIPE - RONAN");
        GUI.Label(new Rect(25, 40, 230, 20), $"Vida: {currentHealth:0} / {maxHealth:0}");
        GUI.Label(new Rect(25, 60, 230, 20), $"Carga Asalto: {assaultCharge:0}% {(isAssaultActive ? "[¡ACTIVO!]" : "")}");
        GUI.Label(new Rect(25, 80, 230, 20), $"Estado: {currentState} | [J] Atacar [K] Bloquear [Espacio] Esquivar");
    }
}