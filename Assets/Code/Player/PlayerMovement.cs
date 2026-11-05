using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador principal de Ronan para movimiento Top-Down 8 direcciones,
/// gestión de combate (Ataque, Bloqueo, Contraataque, Esquive, Modo Asalto y Victoria con Eco).
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    public enum CombatState
    {
        Idle = 0,
        Attack = 1,
        Block = 2,
        Counter = 3,
        Dodge = 4,
        Victory = 7
    }

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
    public KeyCode nextWeaponKey = KeyCode.Q;  // Cambiar al siguiente arma (también 1/2/3 y rueda del ratón)

    [Header("Armas intercambiables")]
    public List<WeaponDefinition> armas = new List<WeaponDefinition>();
    public int armaActual = 0;
    private float lastAttackTime = -10f;

    [Header("Recursos de Combate (Maná y Flechas)")]
    public int flechas = 12;
    public int maxFlechas = 50;
    public float manaActual = 60f;
    public float manaMaximo = 100f;
    public int oro = 50;

    private string avisoRecurso = "";
    private float avisoRecursoTimer = 0f;

    /// <summary>Arma equipada actualmente (nunca nula).</summary>
    public WeaponDefinition ArmaActual
    {
        get
        {
            if (armas == null || armas.Count == 0) armas = WeaponDefinition.CrearArmasPorDefecto();
            armaActual = Mathf.Clamp(armaActual, 0, armas.Count - 1);
            return armas[armaActual];
        }
    }

    /// <summary>Se dispara cuando Ronan cambia de arma (para HUD, audio, etc.).</summary>
    public event System.Action<WeaponDefinition> OnArmaCambiada;

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

    // Efectos visuales de Victoria con Eco en Manos
    private GameObject ecoEnManosObj;
    private SpriteRenderer ecoEnManosSr;
    private SpriteRenderer ecoHaloSr;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingLayerName = "Default";

        combatSM = GetComponent<CombatStateMachine>();
        if (combatSM == null) combatSM = gameObject.AddComponent<CombatStateMachine>();

        if (armas == null || armas.Count == 0) armas = WeaponDefinition.CrearArmasPorDefecto();

        // Asegurar que solo la espada esté desbloqueada por defecto al comenzar
        foreach (WeaponDefinition w in armas)
        {
            if (w.tipo == WeaponType.Sword) w.desbloqueada = true;
            else w.desbloqueada = false;
        }

        healthComp = GetComponent<HealthComponent>();
        if (healthComp == null)
        {
            healthComp = gameObject.AddComponent<HealthComponent>();
            healthComp.isPlayer = true;
        }

        if (!gameObject.CompareTag("Player"))
        {
            gameObject.tag = "Player";
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

        // Sincronizar vida y oro con el SaveStateManager (100 base + 25 por cada eco recuperado)
        if (SaveStateManager.Instance != null)
        {
            oro = SaveStateManager.Instance.oro;
            maxHealth = SaveStateManager.Instance.ObtenerVidaMaximaTotal();
            currentHealth = Mathf.Clamp(SaveStateManager.Instance.currentHealth, 1f, maxHealth);
            if (healthComp != null)
            {
                healthComp.maxHealth = maxHealth;
                healthComp.currentHealth = currentHealth;
            }

            // Sincronizar armas compradas en la tienda
            if (SaveStateManager.Instance.arcoComprado)
            {
                WeaponDefinition bow = armas.Find(x => x.tipo == WeaponType.Bow);
                if (bow != null) bow.desbloqueada = true;
            }
            if (SaveStateManager.Instance.baculoComprado)
            {
                WeaponDefinition staff = armas.Find(x => x.tipo == WeaponType.Staff);
                if (staff != null) staff.desbloqueada = true;
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
        if (avisoRecursoTimer > 0f) avisoRecursoTimer -= Time.deltaTime;

        if (currentHealth <= 0f)
        {
            movement = Vector2.zero;
            if (visualEffectObj != null && visualEffectObj.activeSelf) visualEffectObj.SetActive(false);
            return;
        }

        // Durante la animación de victoria con el Eco, Ronan mantiene la pose triunfal con pulso luminoso
        if (currentState == CombatState.Victory)
        {
            movement = Vector2.zero;
            if (ecoEnManosObj != null)
            {
                float bob = Mathf.Sin(Time.unscaledTime * 4.5f) * 0.05f;
                ecoEnManosObj.transform.localPosition = new Vector3(0f, 0.78f + bob, 0f);
                if (ecoHaloSr != null)
                {
                    ecoHaloSr.transform.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * 70f);
                }
                if (ecoEnManosSr != null)
                {
                    float t = (Mathf.Sin(Time.unscaledTime * 3.5f) + 1f) * 0.5f;
                    ecoEnManosSr.color = Color.Lerp(new Color(0.2f, 0.95f, 1f, 1f), new Color(1f, 0.95f, 0.45f, 1f), t);
                }
            }
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
            // En Attack, Block, Counter y Victory Ronan debe mantenerse firme sin deslizarse hacia atrás ni en diagonal
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void LateUpdate()
    {
        if (sr != null)
        {
            // Orden de renderizado dinámico Top-Down según el eje Y en capa Characters (por encima de puentes y suelo)
            sr.sortingLayerName = "Default";
            sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 10f);
            if (effectSr != null)
            {
                effectSr.sortingLayerName = "Default";
                effectSr.sortingOrder = sr.sortingOrder + 1;
            }
            if (ecoEnManosSr != null)
            {
                ecoEnManosSr.sortingLayerName = "Default";
                ecoEnManosSr.sortingOrder = sr.sortingOrder + 12;
            }
            if (ecoHaloSr != null)
            {
                ecoHaloSr.sortingLayerName = "Default";
                ecoHaloSr.sortingOrder = sr.sortingOrder + 11;
            }
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
        // 0. Cambio de arma (Q = siguiente, 1/2/3 = directa, rueda del ratón)
        LeerCambioDeArma();

        // 1. Ataque con el arma equipada (espada, arco, báculo...)
        if ((Input.GetKeyDown(attackKey) || Input.GetMouseButtonDown(0)) && Time.time >= lastAttackTime + ArmaActual.enfriamiento)
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

    private void LeerCambioDeArma()
    {
        if (armas == null || armas.Count < 2) return;

        if (Input.GetKeyDown(nextWeaponKey)) SiguienteArma();

        if (Input.GetKeyDown(KeyCode.Alpha1)) CambiarArma(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) CambiarArma(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) CambiarArma(2);

        float rueda = Input.mouseScrollDelta.y;
        if (rueda > 0.1f) SiguienteArma(-1);
        else if (rueda < -0.1f) SiguienteArma(1);
    }

    /// <summary>Equipa la siguiente (dir = 1) o anterior (dir = -1) arma desbloqueada.</summary>
    public void SiguienteArma(int dir = 1)
    {
        if (armas == null || armas.Count == 0) return;
        for (int i = 1; i <= armas.Count; i++)
        {
            int idx = ((armaActual + dir * i) % armas.Count + armas.Count) % armas.Count;
            if (armas[idx].desbloqueada)
            {
                CambiarArma(idx);
                return;
            }
        }
    }

    /// <summary>Equipa el arma por índice (solo si existe y está desbloqueada).</summary>
    public void CambiarArma(int indice)
    {
        if (armas == null || indice < 0 || indice >= armas.Count) return;
        if (!armas[indice].desbloqueada || indice == armaActual) return;
        if (currentState != CombatState.Idle) return;

        armaActual = indice;
        if (combatSM != null) combatSM.attackDuration = armas[indice].duracionAtaque;
        AudioManager.Instance?.PlayDodgeSound();
        OnArmaCambiada?.Invoke(armas[indice]);
    }

    public bool TieneArmaDesbloqueada(WeaponType tipo)
    {
        if (armas == null) return false;
        WeaponDefinition w = armas.Find(x => x.tipo == tipo);
        return w != null && w.desbloqueada;
    }

    public void DesbloquearArma(WeaponType tipo)
    {
        if (armas == null) return;
        WeaponDefinition w = armas.Find(x => x.tipo == tipo);
        if (w != null)
        {
            w.desbloqueada = true;
            if (tipo == WeaponType.Bow && SaveStateManager.Instance != null) SaveStateManager.Instance.arcoComprado = true;
            if (tipo == WeaponType.Staff && SaveStateManager.Instance != null) SaveStateManager.Instance.baculoComprado = true;
            int idx = armas.IndexOf(w);
            if (idx >= 0) CambiarArma(idx);
        }
    }

    public void AgregarFlechas(int cant)
    {
        flechas = Mathf.Clamp(flechas + cant, 0, maxFlechas);
    }

    public void RestaurarMana(float cant)
    {
        manaActual = Mathf.Clamp(manaActual + cant, 0f, manaMaximo);
    }

    public void AgregarOro(int cant)
    {
        oro += cant;
        if (SaveStateManager.Instance != null)
        {
            SaveStateManager.Instance.oro = oro;
            SaveStateManager.Instance.GuardarProgreso();
        }
        MostrarAvisoRecurso($"💰 +{cant} Oro");
        Debug.Log($"<color=#FFD700>[PlayerMovement] +{cant} Oro recogido! Total: {oro}</color>");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ProcesarTriggerRecurso(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        ProcesarTriggerRecurso(other);
    }

    private void ProcesarTriggerRecurso(Collider2D other)
    {
        if (other == null) return;
        ResourcePickup rp = other.GetComponent<ResourcePickup>() ?? other.GetComponentInParent<ResourcePickup>();
        if (rp != null)
        {
            rp.Recolectar(this);
        }
    }

    public void MostrarAvisoRecurso(string msg)
    {
        avisoRecurso = msg;
        avisoRecursoTimer = 2.5f;
    }

    private IEnumerator RutinaAtaque()
    {
        WeaponDefinition arma = ArmaActual;

        // Comprobación de recursos para armas a distancia
        if (arma.tipo == WeaponType.Bow)
        {
            if (flechas < arma.costoFlechas)
            {
                AudioManager.Instance?.PlayDodgeSound();
                MostrarAvisoRecurso("¡SIN FLECHAS! Compra en la Tienda [B] o vence enemigos.");
                yield break;
            }
            flechas -= arma.costoFlechas;
        }
        else if (arma.tipo == WeaponType.Staff)
        {
            if (manaActual < arma.costoMana)
            {
                AudioManager.Instance?.PlayDodgeSound();
                MostrarAvisoRecurso("¡MANÁ INSUFICIENTE! Recoge cristales o visita la Tienda [B].");
                yield break;
            }
            manaActual = Mathf.Max(0f, manaActual - arma.costoMana);
        }

        SetState(CombatState.Attack);
        movement = Vector2.zero;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        if (anim != null) anim.SetFloat("Speed", 0f);
        if (visualEffectObj != null) visualEffectObj.SetActive(false);

        lastAttackTime = Time.time;
        if (combatSM != null) combatSM.attackDuration = arma.duracionAtaque;

        AudioManager.Instance?.PlaySlashSound();

        float danioActual = (isAssaultActive ? baseAttack * 1.60f : baseAttack) * arma.multiplicadorDanio;

        if (!arma.esADistancia)
        {
            // Arma cuerpo a cuerpo: golpe inmediato en arco frente a Ronan
            Vector2 puntoAtaque = (Vector2)transform.position + lookDirection * (arma.alcance * 0.65f);
            Collider2D[] impactados = Physics2D.OverlapCircleAll(puntoAtaque, arma.alcance * 0.60f);

            bool conectoGolpe = false;
            foreach (Collider2D col in impactados)
            {
                if (col.gameObject != gameObject && !col.isTrigger)
                {
                    if (PlayerCombatUtil.AplicarDanio(col, danioActual, lookDirection, arma.empuje, gameObject))
                    {
                        conectoGolpe = true;
                    }
                }
            }

            if (conectoGolpe)
            {
                AñadirCargaAsalto(10f);
            }

            yield return new WaitForSeconds(arma.duracionAtaque);
        }
        else
        {
            // Arma a distancia: el proyectil sale en el cuadro de "suelta" (~55 % de la animación)
            yield return new WaitForSeconds(arma.duracionAtaque * 0.55f);
            if (currentState == CombatState.Attack && currentHealth > 0f)
            {
                Vector2 origen = (Vector2)transform.position + lookDirection * 0.55f + Vector2.up * 0.05f;
                PlayerProjectile.Disparar(arma, origen, lookDirection, danioActual, gameObject, sr != null ? sr.sortingOrder : 0);
            }
            yield return new WaitForSeconds(arma.duracionAtaque * 0.45f);
        }
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
        if (isInvulnerable || currentState == CombatState.Victory) return;

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

    /// <summary>
    /// Activa la animación heroica de victoria levantando el Eco Sagrado por encima de su cabeza.
    /// </summary>
    public void ActivarPoseDeVictoriaConEco(string nombreEco, int indiceEco, bool mostrarEcoInmediato = true)
    {
        SetState(CombatState.Victory);
        movement = Vector2.zero;
        lookDirection = Vector2.down; // Ronan mira al frente / hacia la cámara
        isInvulnerable = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        if (anim != null) anim.SetFloat("Speed", 0f);
        if (visualEffectObj != null) visualEffectObj.SetActive(false);

        if (mostrarEcoInmediato) CrearOMostrarEcoEnManos(indiceEco);
    }

    /// <summary>
    /// Desactiva la pose de victoria, absorbiendo el eco y permitiendo a Ronan reanudar la marcha.
    /// </summary>
    public void DesactivarPoseDeVictoria()
    {
        if (ecoEnManosObj != null)
        {
            Destroy(ecoEnManosObj);
            ecoEnManosObj = null;
            ecoEnManosSr = null;
            ecoHaloSr = null;
        }
        isInvulnerable = false;
        SetState(CombatState.Idle);
    }

    /// <summary>Posición en el mundo sobre la cabeza/manos de Ronan donde se sostiene el Eco.</summary>
    public Vector3 PosicionEcoEnManos => transform.position + new Vector3(0f, 0.78f, 0f);

    /// <summary>Muestra el Eco brillante sostenido en alto (se llama al terminar de recogerlo).</summary>
    public void MostrarEcoEnManos(int indiceEco)
    {
        if (currentState == CombatState.Victory) CrearOMostrarEcoEnManos(indiceEco);
    }

    private void CrearOMostrarEcoEnManos(int indiceEco)
    {
        if (ecoEnManosObj != null) Destroy(ecoEnManosObj);

        ecoEnManosObj = new GameObject("EcoEnManos_VFX");
        ecoEnManosObj.transform.SetParent(transform);
        ecoEnManosObj.transform.localPosition = new Vector3(0f, 0.78f, 0f);

        // Orbe sagrado central del Eco
        ecoEnManosSr = ecoEnManosObj.AddComponent<SpriteRenderer>();
        Texture2D orbTex = new Texture2D(28, 28);
        for (int y = 0; y < 28; y++)
        {
            for (int x = 0; x < 28; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(13.5f, 13.5f)) / 14f;
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = Mathf.Pow(alpha, 1.3f);
                orbTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        orbTex.Apply();
        ecoEnManosSr.sprite = Sprite.Create(orbTex, new Rect(0, 0, 28, 28), new Vector2(0.5f, 0.5f), 16f);
        ecoEnManosSr.sortingOrder = (sr != null ? sr.sortingOrder : 10) + 12;
        ecoEnManosSr.color = new Color(0.2f, 0.95f, 1f, 1f);

        // Halo místico giratorio
        GameObject haloObj = new GameObject("EcoHalo");
        haloObj.transform.SetParent(ecoEnManosObj.transform);
        haloObj.transform.localPosition = Vector3.zero;
        ecoHaloSr = haloObj.AddComponent<SpriteRenderer>();

        Texture2D haloTex = new Texture2D(36, 36);
        for (int y = 0; y < 36; y++)
        {
            for (int x = 0; x < 36; x++)
            {
                Vector2 c = new Vector2(x - 17.5f, y - 17.5f);
                float dist = c.magnitude / 18f;
                float angle = Mathf.Atan2(c.y, c.x) * Mathf.Rad2Deg;
                float rayMod = Mathf.Abs(Mathf.Sin(angle * 4f * Mathf.Deg2Rad));
                float alpha = Mathf.Clamp01((1f - dist) * (0.35f + 0.65f * rayMod));
                haloTex.SetPixel(x, y, new Color(1f, 0.95f, 0.5f, alpha * 0.8f));
            }
        }
        haloTex.Apply();
        ecoHaloSr.sprite = Sprite.Create(haloTex, new Rect(0, 0, 36, 36), new Vector2(0.5f, 0.5f), 16f);
        ecoHaloSr.sortingOrder = ecoEnManosSr.sortingOrder - 1;
        ecoHaloSr.color = new Color(1f, 0.95f, 0.6f, 0.85f);
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
        if (Time.timeScale == 0f) return;

        // Sincronizar con HealthComponent por si recibió daño directo desde HealthComponent.TakeDamage
        if (healthComp != null && Mathf.Abs(healthComp.currentHealth - currentHealth) > 0.01f)
        {
            currentHealth = Mathf.Clamp(healthComp.currentHealth, 0f, maxHealth);
        }

        Color prevColor = GUI.color;

        WeaponDefinition arma = ArmaActual;
        bool muestraRecurso = (arma.tipo == WeaponType.Staff || arma.tipo == WeaponType.Bow);

        // Anclado con margen seguro a la esquina superior izquierda
        float panelX = UITheme.Px(18f);
        float panelY = UITheme.Px(16f);
        float panelW = Mathf.Clamp(UITheme.Px(320f), 290f, 380f);
        float pad = UITheme.Px(12f);
        float gap = UITheme.Px(4f);
        float barW = panelW - pad * 2f;
        float barH = UITheme.Px(16f);
        float panelH = UITheme.Px(muestraRecurso ? 116f : 96f);
        if (avisoRecursoTimer > 0f) panelH += UITheme.Px(24f);

        Rect panel = new Rect(panelX, panelY, panelW, panelH);
        UITheme.Panel(panel, UITheme.BordeDorado, 1.5f);

        float hpPct = Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));
        bool lowHp = hpPct <= 0.30f;

        // Línea 1: Encabezado con margen interior garantizado
        float headerY = panelY + UITheme.Px(8f);
        string alerta = lowHp ? " <color=#FF6B5E>¡BAJA!</color>" : "";
        GUI.Label(new Rect(panelX + pad, headerY, barW * 0.55f, UITheme.Px(18f)),
            "<b>RONAN</b>" + alerta, UITheme.Estilo(14, FontStyle.Bold, TextAnchor.MiddleLeft, UITheme.Dorado));

        GUI.Label(new Rect(panelX + pad + barW * 0.48f, headerY, barW * 0.52f, UITheme.Px(18f)),
            $"♥ {Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}",
            UITheme.Estilo(12, FontStyle.Bold, TextAnchor.MiddleRight, Color.white));

        // 1. Barra de Vida con marco interior y fondo oscuro
        float hpY = headerY + UITheme.Px(20f);
        GUI.color = new Color(0.12f, 0.03f, 0.03f, 0.90f);
        GUI.DrawTexture(new Rect(panelX + pad, hpY, barW, barH), UITheme.Blanca);
        GUI.color = hpPct > 0.5f
            ? new Color(0.18f, 0.78f, 0.32f, 0.95f)
            : (hpPct > 0.3f ? new Color(0.92f, 0.68f, 0.12f, 0.95f) : new Color(0.95f, 0.18f, 0.18f, 0.85f + Mathf.PingPong(Time.time * 2.5f, 0.15f)));
        GUI.DrawTexture(new Rect(panelX + pad + 1.5f, hpY + 1.5f, (barW - 3f) * hpPct, barH - 3f), UITheme.Blanca);
        GUI.color = Color.white;

        float currY = hpY + barH + gap;

        // 2. Barra de Recurso (Maná si es báculo, Flechas si es arco)
        if (arma.tipo == WeaponType.Staff)
        {
            float manaPct = Mathf.Clamp01(manaActual / Mathf.Max(1f, manaMaximo));
            float rH = UITheme.Px(14f);
            GUI.color = new Color(0.04f, 0.12f, 0.25f, 0.88f);
            GUI.DrawTexture(new Rect(panelX + pad, currY, barW, rH), UITheme.Blanca);
            GUI.color = manaActual >= arma.costoMana ? new Color(0f, 0.85f, 1f, 0.95f) : new Color(0.85f, 0.20f, 0.20f, 0.95f);
            GUI.DrawTexture(new Rect(panelX + pad + 1.5f, currY + 1.5f, (barW - 3f) * manaPct, rH - 3f), UITheme.Blanca);
            GUI.color = Color.white;
            UITheme.EtiquetaConSombra(new Rect(panelX + pad + UITheme.Px(6f), currY, barW - UITheme.Px(12f), rH),
                $"💧 Maná: {Mathf.CeilToInt(manaActual)}/{Mathf.CeilToInt(manaMaximo)} (-{arma.costoMana:0})",
                UITheme.Estilo(11, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white));
            currY += rH + gap;
        }
        else if (arma.tipo == WeaponType.Bow)
        {
            float flechasPct = Mathf.Clamp01((float)flechas / Mathf.Max(1f, maxFlechas));
            float rH = UITheme.Px(14f);
            GUI.color = new Color(0.20f, 0.15f, 0.05f, 0.88f);
            GUI.DrawTexture(new Rect(panelX + pad, currY, barW, rH), UITheme.Blanca);
            GUI.color = flechas > 0 ? new Color(1f, 0.80f, 0.25f, 0.95f) : new Color(0.85f, 0.20f, 0.20f, 0.95f);
            GUI.DrawTexture(new Rect(panelX + pad + 1.5f, currY + 1.5f, (barW - 3f) * flechasPct, rH - 3f), UITheme.Blanca);
            GUI.color = Color.white;
            string txtF = flechas > 0 ? $"🏹 Flechas: {flechas}/{maxFlechas} (-1)" : "🏹 ¡Sin Flechas!";
            UITheme.EtiquetaConSombra(new Rect(panelX + pad + UITheme.Px(6f), currY, barW - UITheme.Px(12f), rH),
                txtF, UITheme.Estilo(11, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white));
            currY += rH + gap;
        }

        // 3. Barra de Modo Asalto (8px)
        float asH = UITheme.Px(8f);
        float asaltoPct = isAssaultActive ? 1f : Mathf.Clamp01(assaultCharge / Mathf.Max(1f, maxAssaultCharge));
        GUI.color = new Color(0.05f, 0.10f, 0.20f, 0.88f);
        GUI.DrawTexture(new Rect(panelX + pad, currY, barW, asH), UITheme.Blanca);
        GUI.color = isAssaultActive
            ? new Color(0.20f, 0.92f, 1f, 0.85f + Mathf.PingPong(Time.time * 3f, 0.15f))
            : (asaltoPct >= 1f ? new Color(0.15f, 0.85f, 1f, 1f) : new Color(0.16f, 0.48f, 0.85f, 0.9f));
        GUI.DrawTexture(new Rect(panelX + pad + 1.5f, currY + 1.5f, (barW - 3f) * asaltoPct, asH - 3f), UITheme.Blanca);
        GUI.color = Color.white;
        currY += asH + gap;

        // 4. Arma y estadísticas en línea con suficiente margen y ancho completo
        float atkActual = (isAssaultActive ? baseAttack * 1.60f : baseAttack) * arma.multiplicadorDanio;
        GUI.Label(new Rect(panelX + pad, currY, barW, UITheme.Px(18f)),
            $"⚔ <color=#FFE066>{arma.nombre}</color>  |  ATQ <b>{atkActual:0.#}</b>  <color=#9AA6B8>[Q]</color>",
            UITheme.Estilo(12, FontStyle.Normal, TextAnchor.MiddleLeft, UITheme.TextoSecundario));

        // 5. Aviso flotante de recurso insuficiente
        if (avisoRecursoTimer > 0f)
        {
            float avY = currY + UITheme.Px(20f);
            GUI.color = new Color(0.85f, 0.20f, 0.20f, 0.92f);
            GUI.DrawTexture(new Rect(panelX + pad, avY, barW, UITheme.Px(19f)), UITheme.Blanca);
            GUI.color = Color.white;
            UITheme.EtiquetaConSombra(new Rect(panelX + pad, avY, barW, UITheme.Px(19f)),
                avisoRecurso,
                UITheme.Estilo(11, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
        }

        // 6. Selector Visual del Arma Equipada (Esquina Inferior Izquierda)
        DibujarSelectorVisualArma(arma, atkActual);

        GUI.color = prevColor;
    }

    private static Texture2D texIconoEspada;
    private static Texture2D texIconoArco;
    private static Texture2D texIconoBaculo;

    private static void AsegurarIconosArmas()
    {
        if (texIconoArco == null)
        {
            texIconoArco = ZoneWaveSpawner.LoadSpriteTexture("Arrow");
        }
        if (texIconoEspada == null)
        {
            texIconoEspada = CrearIconoArmaProcedural(WeaponType.Sword);
        }
        if (texIconoBaculo == null)
        {
            texIconoBaculo = CrearIconoArmaProcedural(WeaponType.Staff);
        }
        if (texIconoArco == null)
        {
            texIconoArco = CrearIconoArmaProcedural(WeaponType.Bow);
        }
    }

    private static Texture2D CrearIconoArmaProcedural(WeaponType tipo)
    {
        int s = 24;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
                tex.SetPixel(x, y, Color.clear);

        if (tipo == WeaponType.Sword)
        {
            // Hoja diagonal de acero reluciente y empuñadura dorada
            for (int i = 6; i <= 20; i++)
            {
                tex.SetPixel(i, i, new Color(0.92f, 0.96f, 1f, 1f));
                if (i < 20) tex.SetPixel(i - 1, i, new Color(0.68f, 0.78f, 0.90f, 1f));
                if (i < 20) tex.SetPixel(i, i - 1, new Color(0.68f, 0.78f, 0.90f, 1f));
            }
            // Guarda dorada
            for (int g = -3; g <= 3; g++)
            {
                tex.SetPixel(7 + g, 7 - g, new Color(1f, 0.85f, 0.25f, 1f));
            }
            // Empuñadura y pomo
            for (int h = 3; h <= 6; h++)
            {
                tex.SetPixel(h, h, new Color(0.55f, 0.28f, 0.12f, 1f));
            }
            tex.SetPixel(2, 2, new Color(1f, 0.85f, 0.25f, 1f));
        }
        else if (tipo == WeaponType.Staff)
        {
            // Vara de madera ancestral con orbe arcano celeste en la punta
            for (int i = 3; i <= 16; i++)
            {
                tex.SetPixel(i, i, new Color(0.62f, 0.36f, 0.16f, 1f));
                tex.SetPixel(i - 1, i, new Color(0.45f, 0.24f, 0.10f, 1f));
            }
            Vector2 orbCenter = new Vector2(18f, 18f);
            for (int y = 13; y < 23; y++)
            {
                for (int x = 13; x < 23; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), orbCenter);
                    if (d <= 4.2f)
                    {
                        Color c = d < 2.0f ? new Color(0.90f, 1f, 1f, 1f) : new Color(0f, 0.88f, 1f, 0.95f);
                        tex.SetPixel(x, y, c);
                    }
                }
            }
        }
        else
        {
            // Arco curvo con cuerda y flecha
            for (int i = 4; i <= 19; i++)
            {
                tex.SetPixel(i, i, new Color(0.95f, 0.85f, 0.45f, 1f));
            }
        }

        tex.Apply();
        tex.hideFlags = HideFlags.HideAndDontSave;
        return tex;
    }

    private void DibujarSelectorVisualArma(WeaponDefinition arma, float atkActual)
    {
        AsegurarIconosArmas();

        float w = Mathf.Clamp(UITheme.Px(310f), 280f, 360f);
        float h = UITheme.Px(68f);
        float x = UITheme.Px(18f);
        float y = Screen.height - h - UITheme.Px(16f);
        float pad = UITheme.Px(8f);

        Rect rPanel = new Rect(x, y, w, h);
        UITheme.Panel(rPanel, UITheme.BordeDorado, 1.5f);

        // Casilla cuadrada del icono del arma activa
        float iconBoxSize = h - pad * 2f;
        Rect rIconBox = new Rect(x + pad, y + pad, iconBoxSize, iconBoxSize);
        GUI.color = new Color(0.05f, 0.07f, 0.12f, 0.95f);
        GUI.DrawTexture(rIconBox, UITheme.Blanca);
        UITheme.Borde(rIconBox, new Color(1f, 0.86f, 0.35f, 0.98f), 1.5f);

        Texture2D texArma = arma.tipo == WeaponType.Bow
            ? texIconoArco
            : (arma.tipo == WeaponType.Staff ? texIconoBaculo : texIconoEspada);

        if (texArma != null)
        {
            GUI.color = new Color(1f, 0.88f, 0.35f, 0.18f);
            GUI.DrawTexture(new Rect(rIconBox.x + 2f, rIconBox.y + 2f, rIconBox.width - 4f, rIconBox.height - 4f), UITheme.Blanca);
            GUI.color = Color.white;
            float p = UITheme.Px(6f);
            GUI.DrawTexture(new Rect(rIconBox.x + p, rIconBox.y + p, rIconBox.width - p * 2f, rIconBox.height - p * 2f), texArma, ScaleMode.ScaleToFit);
        }

        // Etiqueta [Q] en esquina de la casilla del icono
        Rect rBadgeQ = new Rect(rIconBox.xMax - UITheme.Px(20f), rIconBox.yMax - UITheme.Px(15f), UITheme.Px(19f), UITheme.Px(14f));
        GUI.color = new Color(0.08f, 0.12f, 0.18f, 0.95f);
        GUI.DrawTexture(rBadgeQ, UITheme.Blanca);
        GUI.color = Color.white;
        GUI.Label(rBadgeQ, "[Q]", UITheme.Estilo(9, FontStyle.Bold, TextAnchor.MiddleCenter, UITheme.Dorado));

        // Información derecha: Nombre, Daño y Munición/Maná gráfico
        float infoX = rIconBox.xMax + UITheme.Px(10f);
        float infoW = w - (infoX - x) - pad;

        GUI.Label(new Rect(infoX, y + UITheme.Px(6f), infoW, UITheme.Px(18f)),
            $"<b><color=#FFE066>{arma.nombre}</color></b>",
            UITheme.Estilo(12, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white));

        GUI.Label(new Rect(infoX, y + UITheme.Px(24f), infoW, UITheme.Px(16f)),
            $"DAÑO: <b>{atkActual:0.#}</b>   |   Cambiar: <b><color=#00E5FF>[Q]</color></b>   Tienda: <b>[B]</b>",
            UITheme.Estilo(10, FontStyle.Normal, TextAnchor.MiddleLeft, UITheme.TextoSecundario));

        // Barra / Indicador gráfico de munición o maná en la parte inferior del selector
        Rect rBar = new Rect(infoX, y + UITheme.Px(44f), infoW, UITheme.Px(14f));
        if (arma.tipo == WeaponType.Bow)
        {
            float pct = Mathf.Clamp01((float)flechas / Mathf.Max(1f, maxFlechas));
            GUI.color = new Color(0.18f, 0.12f, 0.04f, 0.92f);
            GUI.DrawTexture(rBar, UITheme.Blanca);
            GUI.color = flechas > 0 ? new Color(1f, 0.80f, 0.25f, 0.95f) : new Color(0.85f, 0.20f, 0.20f, 0.95f);
            GUI.DrawTexture(new Rect(rBar.x + 1.5f, rBar.y + 1.5f, (rBar.width - 3f) * pct, rBar.height - 3f), UITheme.Blanca);
            GUI.color = Color.white;
            UITheme.EtiquetaConSombra(rBar, $"🏹 Munición: {flechas}/{maxFlechas} Flechas", UITheme.Estilo(10, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
        }
        else if (arma.tipo == WeaponType.Staff)
        {
            float pct = Mathf.Clamp01(manaActual / Mathf.Max(1f, manaMaximo));
            GUI.color = new Color(0.04f, 0.12f, 0.24f, 0.92f);
            GUI.DrawTexture(rBar, UITheme.Blanca);
            GUI.color = manaActual >= arma.costoMana ? new Color(0f, 0.85f, 1f, 0.95f) : new Color(0.85f, 0.20f, 0.20f, 0.95f);
            GUI.DrawTexture(new Rect(rBar.x + 1.5f, rBar.y + 1.5f, (rBar.width - 3f) * pct, rBar.height - 3f), UITheme.Blanca);
            GUI.color = Color.white;
            UITheme.EtiquetaConSombra(rBar, $"💧 Maná: {Mathf.CeilToInt(manaActual)}/{Mathf.CeilToInt(manaMaximo)}", UITheme.Estilo(10, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
        }
        else
        {
            bool tieneArco = TieneArmaDesbloqueada(WeaponType.Bow);
            bool tieneBaculo = TieneArmaDesbloqueada(WeaponType.Staff);
            string slots = $"<color=#7CFF8E>[1] Espada</color>  {(tieneArco ? "<color=#FFE066>[2] Arco</color>" : "<color=#667080>[2] Arco🔒</color>")}  {(tieneBaculo ? "<color=#00E5FF>[3] Báculo</color>" : "<color=#667080>[3] Báculo🔒</color>")}";
            GUI.Label(rBar, slots, UITheme.Estilo(10, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white));
        }
    }
}
