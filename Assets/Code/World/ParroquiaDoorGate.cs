using UnityEngine;

/// <summary>
/// Cerrojo de la puerta principal de la Parroquia.
/// La puerta permanece BLOQUEADA (barrera sólida + portal de cambio de escena desactivado) mientras:
///   1) existan encuentros requeridos del Atrio sin completar, o
///   2) haya enemigos vivos en la zona exterior (contador de enemigos activos &gt; 0).
/// Solo cuando ambas condiciones se cumplen (contador = 0) se abre el paso.
/// </summary>
public class ParroquiaDoorGate : MonoBehaviour
{
    [Header("Elementos de la puerta")]
    public GameObject barrera;
    public Collider2D portalTrigger;
    public SpriteRenderer portalVisual;

    [Header("Condiciones de apertura")]
    public ZoneWaveSpawner[] encuentrosRequeridos;
    [Tooltip("Área del mapa exterior en la que se cuentan los enemigos vivos.")]
    public Rect zonaExterior = new Rect(-40f, -26f, 80f, 64f);
    public float intervaloComprobacion = 0.2f;

    [Header("Estado en tiempo de ejecución")]
    public int enemigosActivos = 0;
    public bool puertaAbierta = false;
    public bool abiertaPermanentemente = false;

    private float timer;
    private Transform player;
    private bool anteriorAbierta = true;
    private float avisoTimer = 0f;

    private void Start()
    {
        EnemyManager.EnsureInstance();
        Refrescar(true);
    }

    private void Update()
    {
        if (avisoTimer > 0f) avisoTimer -= Time.deltaTime;
        if (abiertaPermanentemente) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = intervaloComprobacion;
        Refrescar(false);
    }

    /// <summary>
    /// Escaneo exhaustivo a nivel de escena completa de todos los enemigos activos.
    /// La puerta solo se abre cuando no queda ni un solo enemigo con vida en todo el mapa.
    /// </summary>
    public int ContarEnemigosActivos()
    {
        System.Collections.Generic.HashSet<GameObject> enemigosVivos = new System.Collections.Generic.HashSet<GameObject>();

        // 1. Escaneo por tag "Enemy" en toda la jerarquía de la escena
        GameObject[] porTag = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject go in porTag)
        {
            if (go == null || !go.activeInHierarchy || go.CompareTag("Player")) continue;
            HealthComponent hc = go.GetComponent<HealthComponent>();
            if (hc != null && hc.currentHealth <= 0f) continue;
            BaseEnemyAI ai = go.GetComponent<BaseEnemyAI>();
            if (ai != null && ai.currentState == BaseEnemyAI.AIState.Dead) continue;
            enemigosVivos.Add(go);
        }

        // 2. Registro unificado de EnemyManager
        if (EnemyManager.Instance != null)
        {
            foreach (GameObject reg in EnemyManager.Instance.ObtenerListaEnemigosVivos())
            {
                if (reg != null && reg.activeInHierarchy && !reg.CompareTag("Player"))
                {
                    enemigosVivos.Add(reg);
                }
            }
        }

        // 3. Chequeo global por componentes HealthComponent vivos de enemigos
        HealthComponent[] todosHC = FindObjectsByType<HealthComponent>(FindObjectsInactive.Exclude);
        foreach (HealthComponent hc in todosHC)
        {
            if (hc != null && !hc.isPlayer && hc.currentHealth > 0f && hc.gameObject.activeInHierarchy)
            {
                enemigosVivos.Add(hc.gameObject);
            }
        }

        return enemigosVivos.Count;
    }

    private bool EncuentrosRequeridosCompletos()
    {
        if (encuentrosRequeridos == null) return true;
        foreach (ZoneWaveSpawner z in encuentrosRequeridos)
        {
            if (z != null && !z.encounterCompleted) return false;
        }
        return true;
    }

    private void Refrescar(bool forzar)
    {
        enemigosActivos = ContarEnemigosActivos();
        bool abrir = EncuentrosRequeridosCompletos() && enemigosActivos == 0;
        puertaAbierta = abrir;

        if (!forzar && abrir == anteriorAbierta) return;
        anteriorAbierta = abrir;

        if (barrera != null) barrera.SetActive(!abrir);
        if (portalTrigger != null) portalTrigger.enabled = abrir;

        if (abrir && !forzar)
        {
            AudioManager.Instance?.PlaySecretSound();
            avisoTimer = 4f;
        }
    }

    private void OnGUI()
    {
        if (Time.timeScale == 0f) return;

        if (player == null)
        {
            PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
            if (pm != null) player = pm.transform;
            return;
        }

        float dist = Vector2.Distance(player.position, transform.position);
        float y = Screen.height - UITheme.Px(110f);

        if (!puertaAbierta && dist <= 9f)
        {
            string motivo = enemigosActivos > 0
                ? $"PUERTA SELLADA — Derrota a los enemigos restantes: {enemigosActivos}"
                : "PUERTA SELLADA — Purifica el Atrio de la Parroquia";
            UITheme.Banner(y, "🔒 " + motivo, UITheme.Alerta, 680f, 18);
        }
        else if (puertaAbierta && avisoTimer > 0f)
        {
            UITheme.Banner(y, "✔ ¡Puerta abierta! Entra a la Parroquia", UITheme.Verde, 560f, 18);
        }
    }
}
