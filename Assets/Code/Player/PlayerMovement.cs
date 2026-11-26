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
        Victory = 7,
        ItemGet = 8
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
    private GameObject pickupAuraObj;
    private SpriteRenderer pickupAuraSr;
    private Coroutine pickupAnimationRoutine;
    private bool pendingPickupAnimation;
    private static Sprite pickupAuraSprite;

    private static Sprite swordSlashSprite;
    private static Sprite swordSparkSprite;
    private GameObject swordSlashObj;
    private SpriteRenderer swordSlashSr;
    private readonly SpriteRenderer[] swordSparkRenderers = new SpriteRenderer[6];
    private Coroutine swordVfxRoutine;
    private static Sprite counterRingSprite;
    private static Sprite counterSparkSprite;
    private GameObject counterVfxRoot;
    private readonly SpriteRenderer[] counterRingRenderers = new SpriteRenderer[2];
    private readonly SpriteRenderer[] counterSparkRenderers = new SpriteRenderer[8];
    private Coroutine counterVfxRoutine;

    // Efectos visuales de Victoria con Eco en Manos
    private GameObject ecoEnManosObj;
    private SpriteRenderer ecoEnManosSr;
    private SpriteRenderer ecoHaloSr;
    private readonly GameObject[] ecosOrbitando = new GameObject[3];
    private readonly SpriteRenderer[] ecosOrbitandoRenderers = new SpriteRenderer[3];
    private SpriteRenderer auraVictoriaSr;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingLayerName = "Default";

        combatSM = GetComponent<CombatStateMachine>();
        if (combatSM == null) combatSM = gameObject.AddComponent<CombatStateMachine>();

        if (armas == null || armas.Count == 0) armas = WeaponDefinition.CrearArmasPorDefecto();
        counterWindow = Mathf.Max(counterWindow, 0.55f);

        // Asegurar que solo la espada esté desbloqueada por defecto y ajustar la flecha visible
        foreach (WeaponDefinition w in armas)
        {
            if (w.tipo == WeaponType.Sword) w.desbloqueada = true;
            else w.desbloqueada = false;

            if (w.tipo == WeaponType.Bow)
            {
                w.escalaProyectil = 0.85f;
                w.radioProyectil = 0.20f;
            }
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

        if (pendingPickupAnimation && Time.timeScale > 0f && currentState == CombatState.Idle)
        {
            pendingPickupAnimation = false;
            pickupAnimationRoutine = StartCoroutine(RutinaAnimacionObtencionObjeto());
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
                for (int i = 0; i < ecosOrbitandoRenderers.Length; i++)
                {
                    if (ecosOrbitando[i] == null) continue;
                    float angulo = Time.unscaledTime * 1.8f + i * Mathf.PI * 2f / ecosOrbitando.Length;
                    ecosOrbitando[i].transform.localPosition = new Vector3(
                        Mathf.Cos(angulo) * 0.58f,
                        0.72f + Mathf.Sin(angulo) * 0.26f,
                        0f);
                    float pulso = 0.88f + Mathf.Sin(Time.unscaledTime * 4f + i * 1.7f) * 0.12f;
                    ecosOrbitando[i].transform.localScale = Vector3.one * (0.32f * pulso);
                }
                if (auraVictoriaSr != null)
                {
                    float pulso = 1f + Mathf.Sin(Time.unscaledTime * 3f) * 0.08f;
                    auraVictoriaSr.transform.localScale = Vector3.one * pulso;
                    Color auraColor = auraVictoriaSr.color;
                    auraColor.a = 0.34f + Mathf.Sin(Time.unscaledTime * 3f) * 0.1f;
                    auraVictoriaSr.color = auraColor;
                }
            }
            return;
        }

        if (currentState == CombatState.ItemGet)
        {
            movement = Vector2.zero;
            ActualizarAuraObtencionObjeto();
            return;
        }

        if (currentState == CombatState.Idle)
        {
            LeerMovimientoOchoDirecciones();
            LeerEntradasDeCombate();
        }
        else if (currentState == CombatState.Block)
        {
            if (!MobileInput.GetBlockHeld(blockKey))
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
            if (pickupAuraSr != null)
            {
                pickupAuraSr.sortingLayerName = "Default";
                pickupAuraSr.sortingOrder = sr.sortingOrder + 10;
            }
        }
    }

    private void LeerMovimientoOchoDirecciones()
    {
        movement = MobileInput.GetMovementAxis();

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
        // 0. Cambio de arma (Q = siguiente, 1/2/3 = directa, rueda del ratón, o botón móvil)
        LeerCambioDeArma();

        // 1. Ataque con el arma equipada (espada, arco, báculo...)
        if (MobileInput.GetAttackDown(attackKey) && Time.time >= lastAttackTime + ArmaActual.enfriamiento)
        {
            StartCoroutine(RutinaAtaque());
            return;
        }

        // 2. Bloqueo / Contraataque
        if (MobileInput.GetBlockDown(blockKey))
        {
            EntrarEnBloqueo();
            return;
        }

        // 3. Esquive
        if (MobileInput.GetDodgeDown(dodgeKey) && Time.time >= lastDodgeTime + dodgeCooldown)
        {
            StartCoroutine(RutinaEsquive());
            return;
        }

        // 4. Modo Asalto
        if (MobileInput.GetAssaultDown(assaultKey) && assaultCharge >= maxAssaultCharge && !isAssaultActive)
        {
            StartCoroutine(RutinaModoAsalto());
        }
    }

    private void LeerCambioDeArma()
    {
        if (armas == null || armas.Count < 2) return;

        if (MobileInput.GetNextWeaponDown(nextWeaponKey)) SiguienteArma();

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

    public void RestablecerEstadoTrasDerrota(Vector2 posicion)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        StopAllCoroutines();
        pendingPickupAnimation = false;
        pickupAnimationRoutine = null;
        if (pickupAuraObj != null) Destroy(pickupAuraObj);
        pickupAuraObj = null;
        pickupAuraSr = null;

        transform.position = posicion;
        if (rb != null)
        {
            rb.position = posicion;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        movement = Vector2.zero;
        lookDirection = Vector2.down;
        isInvulnerable = false;
        isAssaultActive = false;
        SetState(CombatState.Idle);

        if (anim != null)
        {
            anim.SetFloat("X", 0f);
            anim.SetFloat("Y", -1f);
            anim.SetFloat("Speed", 0f);
        }
        if (visualEffectObj != null) visualEffectObj.SetActive(false);
        if (sr != null) sr.color = Color.white;
        if (healthComp != null) healthComp.RestablecerTrasDerrota(maxHealth, currentHealth);

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.transform.position = new Vector3(posicion.x, posicion.y, mainCamera.transform.position.z);
        }
    }

    public void ActivarInvulnerabilidadTrasRespawn(float duracion)
    {
        StopCoroutine(nameof(RutinaInvulnerabilidadTrasRespawn));
        StartCoroutine(RutinaInvulnerabilidadTrasRespawn(duracion));
    }

    private IEnumerator RutinaInvulnerabilidadTrasRespawn(float duracion)
    {
        isInvulnerable = true;
        float restante = Mathf.Max(0f, duracion);
        while (restante > 0f)
        {
            restante -= Time.unscaledDeltaTime;
            yield return null;
        }
        isInvulnerable = false;
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
        if (w != null && !w.desbloqueada)
        {
            w.desbloqueada = true;
            if (tipo == WeaponType.Bow && SaveStateManager.Instance != null) SaveStateManager.Instance.arcoComprado = true;
            if (tipo == WeaponType.Staff && SaveStateManager.Instance != null) SaveStateManager.Instance.baculoComprado = true;
            int idx = armas.IndexOf(w);
            if (idx >= 0) CambiarArma(idx);
            CelebrarObtencionObjeto();
        }
    }

    public void CelebrarObtencionObjeto()
    {
        if (currentHealth <= 0f || currentState == CombatState.Victory) return;
        if (Time.timeScale <= 0f || currentState != CombatState.Idle)
        {
            pendingPickupAnimation = true;
            return;
        }

        pendingPickupAnimation = false;
        if (pickupAnimationRoutine != null) StopCoroutine(pickupAnimationRoutine);
        pickupAnimationRoutine = StartCoroutine(RutinaAnimacionObtencionObjeto());
    }

    private IEnumerator RutinaAnimacionObtencionObjeto()
    {
        SetState(CombatState.ItemGet);
        movement = Vector2.zero;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        MostrarAuraObtencionObjeto();
        yield return new WaitForSeconds(0.9f);

        if (pickupAuraObj != null) Destroy(pickupAuraObj);
        pickupAuraObj = null;
        pickupAuraSr = null;
        if (currentState == CombatState.ItemGet) SetState(CombatState.Idle);
        pickupAnimationRoutine = null;
    }

    private void MostrarAuraObtencionObjeto()
    {
        if (pickupAuraObj != null) Destroy(pickupAuraObj);

        pickupAuraObj = new GameObject("ObjetoObtenido_VFX");
        pickupAuraObj.transform.SetParent(transform, false);
        pickupAuraObj.transform.localPosition = new Vector3(0f, 0.72f, 0f);
        pickupAuraSr = pickupAuraObj.AddComponent<SpriteRenderer>();
        if (pickupAuraSprite == null) pickupAuraSprite = CrearSpriteAuraObtencion();
        pickupAuraSr.sprite = pickupAuraSprite;
        pickupAuraSr.color = new Color(1f, 0.78f, 0.2f, 0.5f);
        pickupAuraSr.sortingLayerName = "Default";
        pickupAuraSr.sortingOrder = (sr != null ? sr.sortingOrder : 0) + 10;
    }

    private void ActualizarAuraObtencionObjeto()
    {
        if (pickupAuraSr == null) return;

        float pulso = 0.85f + Mathf.Sin(Time.time * 12f) * 0.12f;
        pickupAuraObj.transform.localScale = Vector3.one * pulso;
        Color color = pickupAuraSr.color;
        color.a = 0.35f + Mathf.Sin(Time.time * 10f) * 0.12f;
        pickupAuraSr.color = color;
    }

    private static Sprite CrearSpriteAuraObtencion()
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f)) / (size * 0.5f);
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2f);
                texture.SetPixel(x, y, new Color(1f, 0.8f, 0.25f, alpha));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
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
        MostrarAvisoRecurso($"+{cant} Oro");
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
            if (arma.tipo == WeaponType.Sword)
            {
                MostrarEfectoTajoEspada(arma.duracionAtaque);
            }

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

    private void MostrarEfectoTajoEspada(float duracionAtaque)
    {
        if (swordSlashSprite == null) CrearSpritesTajoEspada();
        if (swordSlashObj == null) CrearObjetosTajoEspada();
        if (swordVfxRoutine != null) StopCoroutine(swordVfxRoutine);
        swordVfxRoutine = StartCoroutine(AnimarTajoEspada(Mathf.Max(0.16f, duracionAtaque * 0.82f)));
    }

    private void CrearSpritesTajoEspada()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "TajoEspada_Ronan_PixelArt",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 centro = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 delta = new Vector2(x, y) - centro;
                float angulo = Mathf.Abs(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                float distancia = delta.magnitude;
                float apertura = Mathf.Clamp01(1f - angulo / 76f);
                float radio = 22f + Mathf.Sin(angulo * Mathf.Deg2Rad * 1.5f) * 2f;
                float grosor = Mathf.Lerp(2f, 5f, apertura);
                Color pixel = Color.clear;

                if (angulo <= 76f && distancia >= radio - grosor && distancia <= radio + grosor)
                {
                    float borde = Mathf.Abs(distancia - radio) / grosor;
                    if (borde < 0.32f) pixel = new Color(1f, 1f, 0.88f, 1f);
                    else if (borde < 0.72f) pixel = new Color(0.48f, 0.91f, 1f, 0.96f);
                    else pixel = new Color(1f, 0.63f, 0.20f, 0.82f);

                    pixel.a *= apertura;
                }

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        swordSlashSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 48f);

        Texture2D sparkTexture = new Texture2D(9, 9, TextureFormat.RGBA32, false)
        {
            name = "DestelloEspada_Ronan_PixelArt",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                int dx = Mathf.Abs(x - 4);
                int dy = Mathf.Abs(y - 4);
                bool destello = (dx == 0 && dy <= 4) || (dy == 0 && dx <= 4) || (dx <= 1 && dy <= 1);
                sparkTexture.SetPixel(x, y, destello ? Color.white : Color.clear);
            }
        }
        sparkTexture.Apply();
        swordSparkSprite = Sprite.Create(sparkTexture, new Rect(0, 0, 9, 9), new Vector2(0.5f, 0.5f), 48f);
    }

    private void CrearObjetosTajoEspada()
    {
        swordSlashObj = new GameObject("Ronan_Tajo_Espada_VFX");
        swordSlashObj.transform.SetParent(transform, false);
        swordSlashSr = swordSlashObj.AddComponent<SpriteRenderer>();
        swordSlashSr.sprite = swordSlashSprite;
        swordSlashSr.sortingLayerID = sr != null ? sr.sortingLayerID : 0;
        swordSlashSr.enabled = false;

        for (int i = 0; i < swordSparkRenderers.Length; i++)
        {
            GameObject spark = new GameObject($"Ronan_Tajo_Chispa_{i + 1}");
            spark.transform.SetParent(transform, false);
            SpriteRenderer sparkRenderer = spark.AddComponent<SpriteRenderer>();
            sparkRenderer.sprite = swordSparkSprite;
            sparkRenderer.sortingLayerID = sr != null ? sr.sortingLayerID : 0;
            sparkRenderer.enabled = false;
            swordSparkRenderers[i] = sparkRenderer;
        }
    }

    private IEnumerator AnimarTajoEspada(float duracion)
    {
        Vector2 direccion = lookDirection.sqrMagnitude > 0.01f ? lookDirection.normalized : Vector2.down;
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        Quaternion rotacion = Quaternion.Euler(0f, 0f, angulo);
        swordSlashObj.transform.localPosition = direccion * 0.62f;
        swordSlashSr.sortingOrder = sr != null ? sr.sortingOrder + 2 : 2;
        swordSlashSr.enabled = true;

        Vector3[] inicios = new Vector3[swordSparkRenderers.Length];
        Vector3[] finales = new Vector3[swordSparkRenderers.Length];
        for (int i = 0; i < swordSparkRenderers.Length; i++)
        {
            float lateral = Mathf.Lerp(-0.43f, 0.43f, i / (float)(swordSparkRenderers.Length - 1));
            inicios[i] = (Vector3)(direccion * 0.68f + (Vector2)(rotacion * new Vector2(0.05f, lateral)));
            finales[i] = inicios[i] + (Vector3)(rotacion * new Vector2(Random.Range(0.20f, 0.48f), Random.Range(-0.18f, 0.18f)));
            swordSparkRenderers[i].transform.localPosition = inicios[i];
            swordSparkRenderers[i].transform.localScale = Vector3.one * Random.Range(0.75f, 1.3f);
            swordSparkRenderers[i].color = i % 2 == 0
                ? new Color(1f, 0.86f, 0.38f, 1f)
                : new Color(0.52f, 0.91f, 1f, 1f);
            swordSparkRenderers[i].sortingOrder = swordSlashSr.sortingOrder + 1;
            swordSparkRenderers[i].enabled = true;
        }

        float tiempo = 0f;
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float progreso = Mathf.Clamp01(tiempo / duracion);
            float golpe = Mathf.SmoothStep(0f, 1f, progreso);

            swordSlashObj.transform.localRotation = Quaternion.Euler(0f, 0f, angulo + Mathf.Lerp(-20f, 18f, golpe));
            swordSlashObj.transform.localScale = Vector3.one * Mathf.Lerp(0.62f, 1.18f, golpe);
            swordSlashSr.color = new Color(1f, 1f, 1f, 1f - progreso);

            for (int i = 0; i < swordSparkRenderers.Length; i++)
            {
                swordSparkRenderers[i].transform.localPosition = Vector3.Lerp(inicios[i], finales[i], golpe);
                Color color = swordSparkRenderers[i].color;
                color.a = 1f - progreso;
                swordSparkRenderers[i].color = color;
            }

            yield return null;
        }

        swordSlashSr.enabled = false;
        for (int i = 0; i < swordSparkRenderers.Length; i++)
            swordSparkRenderers[i].enabled = false;
        swordVfxRoutine = null;
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

    public bool EstaEnVentanaDeContraataque()
    {
        float ventana = SaveStateManager.Instance != null ? SaveStateManager.Instance.VentanaContraataqueSegundos : counterWindow;
        return currentState == CombatState.Block && (Time.time - blockStartTime) <= ventana;
    }

    public bool IntentarContraataqueProyectil()
    {
        if (currentState == CombatState.Block || currentState == CombatState.Counter)
        {
            StartCoroutine(EfectoContraataqueExitoso());
            AñadirCargaAsalto(35f);
            SaveStateManager.Instance?.RegistrarContraataqueExitoso();
            AudioManager.Instance?.PlayCounterSound();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Desata un tajo/onda de contraataque celeste que inflige daño real al atacante y a los enemigos cercanos.
    /// </summary>
    public void EjecutarContraataque(GameObject atacanteEspecifico = null, float multiplicador = 1.5f)
    {
        StartCoroutine(EfectoContraataqueExitoso());
        AñadirCargaAsalto(35f);
        SaveStateManager.Instance?.RegistrarContraataqueExitoso();
        AudioManager.Instance?.PlayCounterSound();

        float danioContraataque = (isAssaultActive ? baseAttack * 1.60f : baseAttack) * multiplicador;
        int enemigosGolpeados = 0;

        // 1. Dañar directamente al atacante específico si se conoce
        if (atacanteEspecifico != null && atacanteEspecifico != gameObject)
        {
            Collider2D colAtacante = atacanteEspecifico.GetComponent<Collider2D>() ?? atacanteEspecifico.GetComponentInChildren<Collider2D>();
            Vector2 dirEmpuje = ((Vector2)atacanteEspecifico.transform.position - (Vector2)transform.position).normalized;
            if (colAtacante != null && PlayerCombatUtil.AplicarDanio(colAtacante, danioContraataque, dirEmpuje, 7f, gameObject))
            {
                enemigosGolpeados++;
            }
            else
            {
                atacanteEspecifico.SendMessage("RecibirDanio", danioContraataque, SendMessageOptions.DontRequireReceiver);
                enemigosGolpeados++;
            }
        }

        // 2. Onda expansiva de contraataque alrededor de Ronan (radio 2.4m) para golpear enemigos cuerpo a cuerpo cercanos
        Collider2D[] cercanos = Physics2D.OverlapCircleAll(transform.position, 2.4f);
        foreach (Collider2D col in cercanos)
        {
            if (col == null || col.gameObject == gameObject || col.gameObject == atacanteEspecifico) continue;
            if (col.CompareTag("Player")) continue;

            Vector2 dir = ((Vector2)col.transform.position - (Vector2)transform.position).normalized;
            if (PlayerCombatUtil.AplicarDanio(col, danioContraataque, dir, 7f, gameObject))
            {
                enemigosGolpeados++;
            }
        }

        if (enemigosGolpeados > 0)
        {
            MostrarAvisoRecurso($"¡CONTRAATAQUE! -{danioContraataque:0} HP al enemigo");
        }
    }

    private IEnumerator EfectoContraataqueExitoso()
    {
        SetState(CombatState.Counter);
        MostrarEfectoFrenteAlJugador(new Color(0.45f, 0.95f, 1f, 0.85f), new Vector3(0.8f, 0.8f, 1f));
        if (counterVfxRoutine != null) StopCoroutine(counterVfxRoutine);
        counterVfxRoutine = StartCoroutine(AnimarEfectoContraataque());
        yield return new WaitForSeconds(0.20f);
        if (visualEffectObj != null) visualEffectObj.SetActive(false);
        SetState(CombatState.Idle);
    }

    private IEnumerator AnimarEfectoContraataque()
    {
        CrearObjetosContraataque();
        counterVfxRoot.transform.localPosition = Vector3.zero;
        int sortingOrder = sr != null ? sr.sortingOrder + 8 : 8;
        float duracion = 0.34f;
        Vector3[] origenes = new Vector3[counterSparkRenderers.Length];
        Vector3[] destinos = new Vector3[counterSparkRenderers.Length];

        for (int i = 0; i < counterRingRenderers.Length; i++)
        {
            counterRingRenderers[i].enabled = true;
            counterRingRenderers[i].sortingOrder = sortingOrder + i;
            counterRingRenderers[i].color = i == 0
                ? new Color(0.3f, 0.96f, 1f, 1f)
                : new Color(0.68f, 0.82f, 1f, 0.95f);
        }

        for (int i = 0; i < counterSparkRenderers.Length; i++)
        {
            float angulo = i * Mathf.PI * 2f / counterSparkRenderers.Length;
            Vector2 direccion = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
            origenes[i] = direccion * 0.28f;
            destinos[i] = direccion * Random.Range(0.85f, 1.25f);
            counterSparkRenderers[i].transform.localPosition = origenes[i];
            counterSparkRenderers[i].transform.localScale = Vector3.one * Random.Range(0.65f, 1.05f);
            counterSparkRenderers[i].color = i % 2 == 0
                ? new Color(0.2f, 1f, 1f, 1f)
                : new Color(1f, 0.9f, 0.52f, 1f);
            counterSparkRenderers[i].sortingOrder = sortingOrder + 2;
            counterSparkRenderers[i].enabled = true;
        }

        float transcurrido = 0f;
        while (transcurrido < duracion)
        {
            transcurrido += Time.deltaTime;
            float progreso = Mathf.Clamp01(transcurrido / duracion);
            float pulso = Mathf.SmoothStep(0f, 1f, progreso);

            for (int i = 0; i < counterRingRenderers.Length; i++)
            {
                Transform ring = counterRingRenderers[i].transform;
                float escala = Mathf.Lerp(i == 0 ? 0.45f : 0.25f, i == 0 ? 2f : 1.55f, pulso);
                ring.localScale = Vector3.one * escala;
                float giro = i == 0 ? 150f : -210f;
                ring.localRotation = Quaternion.Euler(0f, 0f, giro * pulso);
                Color color = counterRingRenderers[i].color;
                color.a = 1f - progreso;
                counterRingRenderers[i].color = color;
            }

            for (int i = 0; i < counterSparkRenderers.Length; i++)
            {
                counterSparkRenderers[i].transform.localPosition = Vector3.Lerp(origenes[i], destinos[i], pulso);
                Color color = counterSparkRenderers[i].color;
                color.a = 1f - progreso;
                counterSparkRenderers[i].color = color;
            }

            yield return null;
        }

        for (int i = 0; i < counterRingRenderers.Length; i++)
            counterRingRenderers[i].enabled = false;
        for (int i = 0; i < counterSparkRenderers.Length; i++)
            counterSparkRenderers[i].enabled = false;
        counterVfxRoutine = null;
    }

    private void CrearObjetosContraataque()
    {
        if (counterVfxRoot != null) return;
        CrearSpritesContraataque();

        counterVfxRoot = new GameObject("Ronan_Contraataque_VFX");
        counterVfxRoot.transform.SetParent(transform, false);

        for (int i = 0; i < counterRingRenderers.Length; i++)
        {
            GameObject ring = new GameObject($"Anillo_Contraataque_{i + 1}");
            ring.transform.SetParent(counterVfxRoot.transform, false);
            SpriteRenderer ringRenderer = ring.AddComponent<SpriteRenderer>();
            ringRenderer.sprite = counterRingSprite;
            ringRenderer.sortingLayerID = sr != null ? sr.sortingLayerID : 0;
            ringRenderer.enabled = false;
            counterRingRenderers[i] = ringRenderer;
        }

        for (int i = 0; i < counterSparkRenderers.Length; i++)
        {
            GameObject spark = new GameObject($"Chispa_Contraataque_{i + 1}");
            spark.transform.SetParent(counterVfxRoot.transform, false);
            SpriteRenderer sparkRenderer = spark.AddComponent<SpriteRenderer>();
            sparkRenderer.sprite = counterSparkSprite;
            sparkRenderer.sortingLayerID = sr != null ? sr.sortingLayerID : 0;
            sparkRenderer.enabled = false;
            counterSparkRenderers[i] = sparkRenderer;
        }
    }

    private static void CrearSpritesContraataque()
    {
        if (counterRingSprite != null && counterSparkSprite != null) return;

        const int size = 48;
        Texture2D ringTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Anillo_Contraataque_Celestial",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        Vector2 centro = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distancia = Vector2.Distance(new Vector2(x, y), centro);
                Color color = distancia >= 16f && distancia <= 19f
                    ? Color.white
                    : (distancia >= 12f && distancia <= 13f
                        ? new Color(0.65f, 0.9f, 1f, 0.72f)
                        : Color.clear);
                ringTexture.SetPixel(x, y, color);
            }
        }
        ringTexture.Apply();
        counterRingSprite = Sprite.Create(ringTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 48f);

        Texture2D sparkTexture = new Texture2D(9, 9, TextureFormat.RGBA32, false)
        {
            name = "Destello_Contraataque_Celestial",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                int dx = Mathf.Abs(x - 4);
                int dy = Mathf.Abs(y - 4);
                bool visible = (dx == 0 && dy <= 4) || (dy == 0 && dx <= 4) || (dx <= 1 && dy <= 1);
                sparkTexture.SetPixel(x, y, visible ? Color.white : Color.clear);
            }
        }
        sparkTexture.Apply();
        counterSparkSprite = Sprite.Create(sparkTexture, new Rect(0, 0, 9, 9), new Vector2(0.5f, 0.5f), 48f);
    }

    public void RecibirDanio(float cantidad)
    {
        if (isInvulnerable || (healthComp != null && healthComp.IsInvulnerable) ||
            currentState == CombatState.Victory || currentState == CombatState.Counter) return;

        if (currentState == CombatState.Block)
        {
            float ventana = SaveStateManager.Instance != null ? SaveStateManager.Instance.VentanaContraataqueSegundos : counterWindow;
            float tiempoDesdeBloqueo = Time.time - blockStartTime;
            if (tiempoDesdeBloqueo <= ventana)
            {
                // Contraataque perfecto: anula el daño y devuelve un golpe reforzado al enemigo
                EjecutarContraataque(null, 1.5f);
                return;
            }
            // Contraataque en guardia mantenida: mitiga 75% del daño y devuelve 125% de daño al enemigo
            cantidad *= 0.25f;
            EjecutarContraataque(null, 1.0f);
        }

        // Aplicar multiplicador de dificultad al daño recibido por Ronan
        float multDmg = SaveStateManager.Instance != null ? SaveStateManager.Instance.MultiplicadorDanioRecibidoRonan : 1f;
        cantidad *= multDmg;

        currentHealth = Mathf.Clamp(currentHealth - cantidad, 0f, maxHealth);
        if (healthComp != null) healthComp.SincronizarVida(currentHealth);
        if (SaveStateManager.Instance != null) SaveStateManager.Instance.currentHealth = currentHealth;

        if (currentHealth <= 0f)
        {
            if (healthComp != null) healthComp.HandleDeath();
            return;
        }

        StartCoroutine(ParpadeoDanio());
    }

    public void CurarSalud(float cantidad)
    {
        if (currentHealth <= 0f) return;
        currentHealth = Mathf.Clamp(currentHealth + cantidad, 0f, maxHealth);
        if (healthComp != null) healthComp.SincronizarVida(currentHealth);
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
        for (int i = 0; i < ecosOrbitando.Length; i++)
        {
            if (ecosOrbitando[i] != null) Destroy(ecosOrbitando[i]);
            ecosOrbitando[i] = null;
            ecosOrbitandoRenderers[i] = null;
        }
        if (auraVictoriaSr != null)
        {
            Destroy(auraVictoriaSr.gameObject);
            auraVictoriaSr = null;
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
        for (int i = 0; i < ecosOrbitando.Length; i++)
        {
            if (ecosOrbitando[i] != null) Destroy(ecosOrbitando[i]);
            ecosOrbitando[i] = null;
            ecosOrbitandoRenderers[i] = null;
        }

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

        CrearEcosOrbitando(ecoEnManosSr.sprite, indiceEco);
        CrearAuraVictoria();
    }

    private void CrearEcosOrbitando(Sprite spriteEco, int indiceEco)
    {
        SaveStateManager save = SaveStateManager.Instance;
        bool[] obtenidos =
        {
            save != null ? save.ecoFeRecuperado : indiceEco == 1,
            save != null ? save.ecoTradicionRecuperado : indiceEco == 2,
            save != null ? save.ecoMemoriaRecuperado : indiceEco == 3
        };
        Color[] colores =
        {
            new Color(1f, 0.78f, 0.28f, 1f),
            new Color(0.45f, 1f, 0.62f, 1f),
            new Color(0.22f, 0.88f, 1f, 1f)
        };

        for (int i = 0; i < obtenidos.Length; i++)
        {
            if (!obtenidos[i]) continue;

            GameObject eco = new GameObject($"EcoSagrado_Orbitando_{i + 1}");
            eco.transform.SetParent(transform, false);
            SpriteRenderer rendererEco = eco.AddComponent<SpriteRenderer>();
            rendererEco.sprite = spriteEco;
            rendererEco.color = colores[i];
            rendererEco.sortingLayerID = sr != null ? sr.sortingLayerID : 0;
            rendererEco.sortingOrder = (sr != null ? sr.sortingOrder : 10) + 14;
            ecosOrbitando[i] = eco;
            ecosOrbitandoRenderers[i] = rendererEco;
        }
    }

    private void CrearAuraVictoria()
    {
        GameObject aura = new GameObject("Aura_Celebracion_Ronan");
        aura.transform.SetParent(transform, false);
        aura.transform.localPosition = new Vector3(0f, 0.12f, 0f);

        Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false)
        {
            name = "Aura_Celestial_Ronan",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float distancia = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) / 15.5f;
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - distancia), 2f) * 0.38f;
                texture.SetPixel(x, y, new Color(0.12f, 0.78f, 1f, alpha));
            }
        }
        texture.Apply();

        auraVictoriaSr = aura.AddComponent<SpriteRenderer>();
        auraVictoriaSr.sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16f);
        auraVictoriaSr.sortingLayerID = sr != null ? sr.sortingLayerID : 0;
        auraVictoriaSr.sortingOrder = (sr != null ? sr.sortingOrder : 10) - 1;
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

        // 1. Margen seguro (Safe Area) contra bordes cortados y notch móvil
        float safeMarginX = Mathf.Max(UITheme.SafeLeft + UITheme.Px(14f), UITheme.Px(26f));
        float safeMarginY = Mathf.Max(UITheme.SafeTop + UITheme.Px(10f), UITheme.Px(20f));
        bool esTactil = MobileInput.IsTouchControlsActive;

        float panelX = safeMarginX;
        float panelY = safeMarginY;
        float pad = UITheme.Px(14f);
        float panelW = Mathf.Clamp(UITheme.Px(310f), 275f, Screen.width * 0.32f);
        float barW = panelW - pad * 2f;

        float row1H = UITheme.Px(18f);
        float barH = UITheme.Px(15f);
        float row3H = UITheme.Px(14f);
        float row4H = UITheme.Px(18f);
        float gap = UITheme.Px(5f);

        float panelH = pad * 2f + row1H + gap + barH + gap + row3H + gap + row4H;

        Rect panel = new Rect(panelX, panelY, panelW, panelH);
        UITheme.Panel(panel, UITheme.BordeDorado, 1.5f);

        float hpPct = Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));
        bool lowHp = hpPct <= 0.30f;

        // Fila 1: RONAN (en dorado) a la izquierda (en panelX + pad) y vida numérica ♥ 100/100 a la derecha
        float row1Y = panelY + pad;
        string alerta = lowHp ? " <color=#FF6B5E>¡BAJA!</color>" : "";
        GUI.Label(new Rect(panelX + pad, row1Y, barW * 0.56f, row1H),
            "<b>RONAN</b>" + alerta,
            UITheme.Estilo(14, FontStyle.Bold, TextAnchor.MiddleLeft, UITheme.Dorado));

        GUI.Label(new Rect(panelX + pad + barW * 0.48f, row1Y, barW * 0.52f, row1H),
            $"♥ {Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}",
            UITheme.Estilo(12, FontStyle.Bold, TextAnchor.MiddleRight, Color.white));

        // Fila 2: Barra de Vida (verde con fondo oscuro y marco interior)
        float row2Y = row1Y + row1H + gap;
        Rect rHp = new Rect(panelX + pad, row2Y, barW, barH);
        GUI.color = new Color(0.10f, 0.03f, 0.03f, 0.92f);
        GUI.DrawTexture(rHp, UITheme.Blanca);

        GUI.color = hpPct > 0.5f
            ? new Color(0.18f, 0.78f, 0.32f, 0.95f)
            : (hpPct > 0.3f ? new Color(0.92f, 0.68f, 0.12f, 0.95f) : new Color(0.95f, 0.18f, 0.18f, 0.85f + Mathf.PingPong(Time.time * 2.5f, 0.15f)));
        GUI.DrawTexture(new Rect(rHp.x + 1.5f, rHp.y + 1.5f, (rHp.width - 3f) * hpPct, rHp.height - 3f), UITheme.Blanca);
        GUI.color = Color.white;
        UITheme.Borde(rHp, new Color(1f, 1f, 1f, 0.25f), 1f);

        // Fila 3: Barra de Recurso (Maná o Flechas según el arma activa) o Barra de Modo Asalto [L]
        float row3Y = row2Y + barH + gap;
        Rect rRes = new Rect(panelX + pad, row3Y, barW, row3H);
        if (arma.tipo == WeaponType.Staff)
        {
            float manaPct = Mathf.Clamp01(manaActual / Mathf.Max(1f, manaMaximo));
            GUI.color = new Color(0.04f, 0.12f, 0.25f, 0.90f);
            GUI.DrawTexture(rRes, UITheme.Blanca);
            GUI.color = manaActual >= arma.costoMana ? new Color(0f, 0.85f, 1f, 0.95f) : new Color(0.85f, 0.20f, 0.20f, 0.95f);
            GUI.DrawTexture(new Rect(rRes.x + 1.5f, rRes.y + 1.5f, (rRes.width - 3f) * manaPct, rRes.height - 3f), UITheme.Blanca);
            GUI.color = Color.white;
            UITheme.Borde(rRes, new Color(0.45f, 0.88f, 1f, 0.35f), 1f);
            UITheme.EtiquetaConSombra(new Rect(rRes.x + UITheme.Px(6f), rRes.y, rRes.width - UITheme.Px(12f), rRes.height),
                $"Maná: {Mathf.CeilToInt(manaActual)}/{Mathf.CeilToInt(manaMaximo)} (-{arma.costoMana:0})",
                UITheme.Estilo(10, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white));
        }
        else if (arma.tipo == WeaponType.Bow)
        {
            float flechasPct = Mathf.Clamp01((float)flechas / Mathf.Max(1f, maxFlechas));
            GUI.color = new Color(0.20f, 0.15f, 0.05f, 0.90f);
            GUI.DrawTexture(rRes, UITheme.Blanca);
            GUI.color = flechas > 0 ? new Color(1f, 0.80f, 0.25f, 0.95f) : new Color(0.85f, 0.20f, 0.20f, 0.95f);
            GUI.DrawTexture(new Rect(rRes.x + 1.5f, rRes.y + 1.5f, (rRes.width - 3f) * flechasPct, rRes.height - 3f), UITheme.Blanca);
            GUI.color = Color.white;
            UITheme.Borde(rRes, new Color(1f, 0.85f, 0.35f, 0.35f), 1f);
            string txtF = flechas > 0 ? $"Flechas: {flechas}/{maxFlechas} (-1)" : "¡Sin Flechas!";
            UITheme.EtiquetaConSombra(new Rect(rRes.x + UITheme.Px(6f), rRes.y, rRes.width - UITheme.Px(12f), rRes.height),
                txtF, UITheme.Estilo(10, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white));
        }
        else
        {
            float asaltoPct = isAssaultActive ? 1f : Mathf.Clamp01(assaultCharge / Mathf.Max(1f, maxAssaultCharge));
            GUI.color = new Color(0.05f, 0.10f, 0.20f, 0.90f);
            GUI.DrawTexture(rRes, UITheme.Blanca);
            GUI.color = isAssaultActive
                ? new Color(0.20f, 0.92f, 1f, 0.85f + Mathf.PingPong(Time.time * 3f, 0.15f))
                : (asaltoPct >= 1f ? new Color(0.15f, 0.85f, 1f, 1f) : new Color(0.16f, 0.48f, 0.85f, 0.90f));
            GUI.DrawTexture(new Rect(rRes.x + 1.5f, rRes.y + 1.5f, (rRes.width - 3f) * asaltoPct, rRes.height - 3f), UITheme.Blanca);
            GUI.color = Color.white;
            UITheme.Borde(rRes, new Color(0.45f, 0.85f, 1f, 0.35f), 1f);
            string txtAsalto = esTactil
                ? (isAssaultActive ? "Modo Asalto Activo" : (asaltoPct >= 1f ? "Modo Asalto Listo ★" : $"Carga de Asalto: {Mathf.RoundToInt(asaltoPct * 100f)}%"))
                : (isAssaultActive ? "Modo Asalto Activo [L]" : (asaltoPct >= 1f ? "Modo Asalto Listo [L]" : $"Carga de Asalto [L]: {Mathf.RoundToInt(asaltoPct * 100f)}%"));
            UITheme.EtiquetaConSombra(new Rect(rRes.x + UITheme.Px(6f), rRes.y, rRes.width - UITheme.Px(12f), rRes.height),
                txtAsalto, UITheme.Estilo(10, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white));
        }

        // Fila 4: Arma activa en una sola línea limpia
        float row4Y = row3Y + row3H + gap;
        float atkActual = (isAssaultActive ? baseAttack * 1.60f : baseAttack) * arma.multiplicadorDanio;
        string cambiarHint = esTactil ? "Toca arma" : "<color=#00E5FF>[Q]</color>";
        GUI.Label(new Rect(panelX + pad, row4Y, barW, row4H),
            $"<color=#FFE066>{arma.nombre}</color> | ATQ <b>{atkActual:0.#}</b> | {cambiarHint}",
            UITheme.Estilo(11, FontStyle.Bold, TextAnchor.MiddleLeft, UITheme.TextoSecundario));

        // Pequeño Menú Gráfico de Armas (debajo del panel de Ronan, dentro del área segura izquierda)
        float menuY = panel.yMax + UITheme.Px(6f);
        DibujarMenuGraficoArmas(panelX, menuY, panelW);

        // Aviso temporal si falta maná/flechas o confirmación de contraataque (DEBAJO del menú de armas para que jamás se pisen)
        if (avisoRecursoTimer > 0f)
        {
            float avY = menuY + UITheme.Px(66f);
            bool esContraataque = avisoRecurso.Contains("CONTRAATAQUE") || avisoRecurso.Contains("Oro");
            GUI.color = esContraataque ? new Color(0.04f, 0.42f, 0.56f, 0.94f) : new Color(0.85f, 0.20f, 0.20f, 0.92f);
            GUI.DrawTexture(new Rect(panelX, avY, panelW, UITheme.Px(22f)), UITheme.Blanca);
            GUI.color = Color.white;
            UITheme.Borde(new Rect(panelX, avY, panelW, UITheme.Px(22f)), UITheme.BordeDorado, 1f);
            UITheme.EtiquetaConSombra(new Rect(panelX + UITheme.Px(6f), avY, panelW - UITheme.Px(12f), UITheme.Px(22f)),
                avisoRecurso,
                UITheme.Estilo(10, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
        }

        GUI.color = prevColor;
    }

    private static Texture2D texArmaEspada;
    private static Texture2D texArmaArco;
    private static Texture2D texArmaBaculo;

    private static void AsegurarIconosMenuArmas()
    {
        if (texArmaEspada == null) texArmaEspada = CrearIconoArmaPixelArt(WeaponType.Sword);
        if (texArmaArco == null) texArmaArco = CrearIconoArmaPixelArt(WeaponType.Bow);
        if (texArmaBaculo == null) texArmaBaculo = CrearIconoArmaPixelArt(WeaponType.Staff);
    }

    private static Texture2D CrearIconoArmaPixelArt(WeaponType tipo)
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
            // Hoja diagonal de acero reluciente y guarda dorada
            for (int i = 6; i <= 20; i++)
            {
                tex.SetPixel(i, i, new Color(0.94f, 0.98f, 1f, 1f));
                if (i < 20) tex.SetPixel(i - 1, i, new Color(0.68f, 0.80f, 0.94f, 1f));
                if (i < 20) tex.SetPixel(i, i - 1, new Color(0.68f, 0.80f, 0.94f, 1f));
            }
            for (int g = -3; g <= 3; g++)
            {
                tex.SetPixel(7 + g, 7 - g, new Color(1f, 0.86f, 0.28f, 1f));
            }
            for (int h = 3; h <= 6; h++)
            {
                tex.SetPixel(h, h, new Color(0.58f, 0.30f, 0.14f, 1f));
            }
            tex.SetPixel(2, 2, new Color(1f, 0.86f, 0.28f, 1f));
        }
        else if (tipo == WeaponType.Bow)
        {
            // Arco curvo de madera dorada con cuerda tensa y flecha estilizada
            for (int t = 4; t <= 19; t++)
            {
                int bx = Mathf.Clamp(Mathf.RoundToInt(16f - Mathf.Pow((t - 11.5f) / 3.8f, 2f) * 2.2f), 2, 21);
                tex.SetPixel(bx, t, new Color(0.92f, 0.65f, 0.24f, 1f));
                tex.SetPixel(bx - 1, t, new Color(0.68f, 0.42f, 0.14f, 1f));
            }
            // Cuerda del arco
            for (int y = 5; y <= 18; y++)
            {
                tex.SetPixel(8, y, new Color(0.88f, 0.92f, 0.96f, 0.85f));
            }
            // Flecha cruzando el arco
            for (int x = 6; x <= 20; x++)
            {
                tex.SetPixel(x, 12, new Color(0.95f, 0.88f, 0.55f, 1f));
            }
            tex.SetPixel(21, 12, new Color(0.92f, 0.96f, 1f, 1f));
            tex.SetPixel(19, 13, new Color(0.92f, 0.96f, 1f, 1f));
            tex.SetPixel(19, 11, new Color(0.92f, 0.96f, 1f, 1f));
        }
        else
        {
            // Báculo del Eco con vara ancestral y orbe arcano celeste
            for (int i = 3; i <= 16; i++)
            {
                tex.SetPixel(i, i, new Color(0.66f, 0.38f, 0.18f, 1f));
                tex.SetPixel(i - 1, i, new Color(0.46f, 0.25f, 0.10f, 1f));
            }
            Vector2 orbCenter = new Vector2(17.5f, 17.5f);
            for (int y = 12; y < 23; y++)
            {
                for (int x = 12; x < 23; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), orbCenter);
                    if (d <= 4.3f)
                    {
                        Color c = d < 2.0f ? new Color(0.92f, 1f, 1f, 1f) : new Color(0f, 0.88f, 1f, 0.95f);
                        tex.SetPixel(x, y, c);
                    }
                }
            }
        }

        tex.Apply();
        tex.hideFlags = HideFlags.HideAndDontSave;
        return tex;
    }

    private void DibujarMenuGraficoArmas(float x, float y, float width)
    {
        if (armas == null || armas.Count == 0) return;
        AsegurarIconosMenuArmas();

        float pad = UITheme.Px(8f);
        float gap = UITheme.Px(6f);
        int count = Mathf.Min(3, armas.Count);
        float cardW = (width - pad * 2f - gap * (count - 1)) / count;
        float cardH = UITheme.Px(44f);
        float totalH = cardH + pad * 2f;

        Rect rMenu = new Rect(x, y, width, totalH);
        UITheme.Panel(rMenu, UITheme.BordeDorado, 1.5f);

        for (int i = 0; i < count; i++)
        {
            WeaponDefinition w = armas[i];
            bool activa = (i == armaActual);
            bool desbloqueada = w.desbloqueada;

            Rect rCard = new Rect(x + pad + i * (cardW + gap), y + pad, cardW, cardH);

            // Fondo de la casilla de arma
            GUI.color = activa
                ? new Color(0.14f, 0.22f, 0.34f, 0.96f)
                : (desbloqueada ? new Color(0.06f, 0.09f, 0.14f, 0.92f) : new Color(0.04f, 0.05f, 0.08f, 0.85f));
            GUI.DrawTexture(rCard, UITheme.Blanca);

            Color colBorde = activa
                ? new Color(1f, 0.86f, 0.32f, 1f)
                : (desbloqueada ? new Color(0.35f, 0.52f, 0.68f, 0.85f) : new Color(0.28f, 0.30f, 0.35f, 0.65f));
            UITheme.Borde(rCard, colBorde, activa ? 2f : 1f);

            // Icono gráfico del arma a la izquierda de la tarjeta
            float iconSize = cardH - UITheme.Px(10f);
            Rect rIcon = new Rect(rCard.x + UITheme.Px(4f), rCard.y + UITheme.Px(5f), iconSize, iconSize);
            Texture2D tex = w.tipo == WeaponType.Bow ? texArmaArco : (w.tipo == WeaponType.Staff ? texArmaBaculo : texArmaEspada);

            if (tex != null)
            {
                GUI.color = desbloqueada ? Color.white : new Color(0.45f, 0.50f, 0.58f, 0.38f);
                GUI.DrawTexture(rIcon, tex, ScaleMode.ScaleToFit);
            }

            // Textos a la derecha del icono: Tecla [1/2/3], Nombre corto y Estado/Recurso
            float txtX = rIcon.xMax + UITheme.Px(3f);
            float txtW = rCard.xMax - txtX - UITheme.Px(3f);

            string nombreCorto = w.tipo == WeaponType.Sword ? "Espada" : (w.tipo == WeaponType.Bow ? "Arco" : "Báculo");
            Color colTitulo = activa ? UITheme.Dorado : (desbloqueada ? Color.white : new Color(0.55f, 0.60f, 0.68f));
            bool esTactilCard = MobileInput.IsTouchControlsActive;
            string tituloArma = esTactilCard ? $"<b>{nombreCorto}</b>" : $"<b>[{i + 1}] {nombreCorto}</b>";

            GUI.color = Color.white;
            GUI.Label(new Rect(txtX, rCard.y + UITheme.Px(4f), txtW, UITheme.Px(17f)),
                tituloArma,
                UITheme.Estilo(9, FontStyle.Bold, TextAnchor.MiddleLeft, colTitulo));

            string subInfo;
            Color colSub;
            if (!desbloqueada)
            {
                subInfo = esTactilCard ? "En Tienda" : "Tienda [B]";
                colSub = new Color(0.68f, 0.62f, 0.52f, 0.90f);
            }
            else if (w.tipo == WeaponType.Bow)
            {
                subInfo = $"{flechas} flechas";
                colSub = flechas > 0 ? new Color(1f, 0.88f, 0.45f) : UITheme.Alerta;
            }
            else if (w.tipo == WeaponType.Staff)
            {
                subInfo = $"-{w.costoMana:0} maná";
                colSub = manaActual >= w.costoMana ? UITheme.Turquesa : UITheme.Alerta;
            }
            else
            {
                float atk = (isAssaultActive ? baseAttack * 1.60f : baseAttack) * w.multiplicadorDanio;
                subInfo = $"ATQ {atk:0.#}";
                colSub = new Color(0.75f, 0.95f, 0.80f);
            }

            GUI.Label(new Rect(txtX, rCard.y + UITheme.Px(22f), txtW, UITheme.Px(16f)),
                subInfo,
                UITheme.Estilo(8, FontStyle.Bold, TextAnchor.MiddleLeft, colSub));

            // Permitir clic/toque directo sobre la tarjeta para equipar el arma o abrir tienda si está bloqueada
            if (Event.current.type == EventType.MouseDown && rCard.Contains(Event.current.mousePosition))
            {
                if (desbloqueada)
                {
                    CambiarArma(i);
                }
                else if (esTactilCard)
                {
                    MobileInput.ResetHeldStates();
                    ShopManager.EnsureInstance().AbrirTienda();
                }
                Event.current.Use();
            }
        }
    }
}
