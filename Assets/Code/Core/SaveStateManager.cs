using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class SealCheckpointState
{
    public string id;
    public bool purificado;
}

[Serializable]
public class BossCheckpointState
{
    public bool presente;
    public bool derrotado;
    public float vidaActual;
    public float vidaMaxima;
    public int fase;
    public bool escudoActivo;
    public bool ultimoAliento;
    public Vector2 posicion;
}

[Serializable]
public class ChestCheckpointState
{
    public string id;
    public bool abierto;
}

[Serializable]
public class WorldCheckpointState
{
    public string zonaId;
    public bool santuarioPresente;
    public bool santuarioResuelto;
    public List<SealCheckpointState> sellos = new List<SealCheckpointState>();
    public List<ChestCheckpointState> cofres = new List<ChestCheckpointState>();
    public BossCheckpointState jefe;
}

[Serializable]
public class CheckpointSnapshot
{
    public int schemaVersion;
    public string zonaId;
    public Vector2 posicion;
    public float vidaActual;
    public float vidaMaxima;
    public float manaActual;
    public int flechasActuales;
    public int oro;
    public int armaActualIndice;
    public bool arcoComprado;
    public bool baculoComprado;
    public int barrasCobre;
    public int barrasHierro;
    public int barrasOro;
    public int barrasMithril;
    public int gemasVerdes;
    public int gemasPurpuras;
    public int gemasRojas;
    public int gemasAmarillas;
    public int llavesOro;
    public bool ecoFe;
    public bool ecoTradicion;
    public bool ecoMemoria;
    public bool cruzAzulConsagrada;
    public List<string> itemsInventario = new List<string>();
    public WorldCheckpointState estadoMundo;
    public List<WorldCheckpointState> estadosMundo = new List<WorldCheckpointState>();
}

/// <summary>
/// Gestiona la persistencia de datos globales del jugador, Ecos recuperados,
/// estadísticas y estado entre escenas y transiciones de zonas.
/// Auto-instanciable como Singleton persistente para que nunca sea nulo.
/// </summary>
public class SaveStateManager : MonoBehaviour
{
    private const string CheckpointPreferenceKey = "UltimoCheckpointJSON";
    private const string ProgressPreferenceKey = "ProgresoGlobalJSON";
    private const string BlueCrossRitualPreferenceKey = "CruzAzulConsagrada";
    private const int CurrentSaveSchemaVersion = 2;
    private static SaveStateManager _instance;
    private CheckpointSnapshot snapshotActivo;
    private WorldCheckpointState estadoMundoProgreso;
    private readonly List<WorldCheckpointState> estadosMundoProgreso = new List<WorldCheckpointState>();
    private readonly List<string> itemsInventarioGuardados = new List<string>();
    private int llavesInventarioGuardadas;
    private bool respawnPendiente;
    private bool restaurarCheckpointPendiente;

    public CheckpointSnapshot SnapshotActivo => snapshotActivo;
    public bool HayCheckpointGuardado
    {
        get
        {
            CheckpointSnapshot checkpoint = LeerSnapshot(CheckpointPreferenceKey);
            return checkpoint != null && !string.IsNullOrEmpty(ObtenerEscenaDeZona(checkpoint.zonaId));
        }
    }

    public static SaveStateManager Instance
    {
        get
        {
            if (_instance == null)
            {
                EnsureInstance();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Ecos Recuperados")]
    public bool ecoFeRecuperado = false;         // Nivel 1: Parroquia
    public bool ecoTradicionRecuperado = false;  // Nivel 2: Mercado
    public bool ecoMemoriaRecuperado = false;    // Nivel 3: Cerro del Fraile (Cruz Azul)
    public bool cruzAzulConsagrada;

    public enum GameDifficulty
    {
        Facil = 0,
        Normal = 1,
        Dificil = 2
    }

    [Header("Dificultad de Juego")]
    public GameDifficulty dificultadActual = GameDifficulty.Normal;

    [Header("Progreso y Estadísticas de Ronan")]
    public float baseMaxHealth = 100f;
    public float currentHealth = 100f;
    public int goldKeys = 0;
    public int oro = 50;
    public bool arcoComprado = false;
    public bool baculoComprado = false;
    public int copperBars = 0;
    public int ironBars = 0;
    public int goldBars = 0;
    public int mithrilBars = 0;
    public int greenGems = 0;
    public int purpleGems = 0;
    public int redGems = 0;
    public int yellowGems = 0;

    [Header("Métricas de Partida")]
    public float tiempoDeJuego = 0f;
    public int contraataquesExitosos = 0;
    public int enemigosDerrotados = 0;

    [Header("Puntos de Control")]
    public string ultimaZonaId = "ParroquiaExterior";
    public Vector2 ultimoCheckpoint = new Vector2(0f, 0f);

    public event Action<int, string> OnEcoRecuperado;
    public event Action<float> OnVidaMaximaActualizada;
    public event Action<int> OnOroCambiado;

    public static SaveStateManager EnsureInstance()
    {
        if (_instance != null) return _instance;
        SaveStateManager existing = FindAnyObjectByType<SaveStateManager>();
        if (existing != null)
        {
            _instance = existing;
            return existing;
        }
        GameObject go = new GameObject("SaveStateManager");
        _instance = go.AddComponent<SaveStateManager>();
        DontDestroyOnLoad(go);
        return _instance;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += AlCargarEscena;
        CargarProgreso();
    }

    private void Start()
    {
        if (ObtenerEstadoMundo(LevelManager.ObtenerZonaDeEscena(SceneManager.GetActiveScene().name).ToString()) != null)
            StartCoroutine(RutinaRestaurarMundoAlCargar(SceneManager.GetActiveScene().name));
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            _instance = null;
        }
    }

    private void Update()
    {
        tiempoDeJuego += Time.deltaTime;
    }

    /// <summary>
    /// Calcula la vida máxima actual según la base (100) + 25 por cada Eco recuperado.
    /// </summary>
    public float ObtenerVidaMaximaTotal()
    {
        int totalEcos = TotalEcosRecuperados();
        return baseMaxHealth + (totalEcos * 25f);
    }

    public int TotalEcosRecuperados()
    {
        int count = 0;
        if (ecoFeRecuperado) count++;
        if (ecoTradicionRecuperado) count++;
        if (ecoMemoriaRecuperado) count++;
        return count;
    }

    /// <summary>
    /// Registra la recuperación de uno de los 3 Ecos principales y aplica el aumento de vida.
    /// </summary>
    public void RecuperarEco(int indiceEco)
    {
        string nombre = "Eco Desconocido";
        switch (indiceEco)
        {
            case 1:
                if (!ecoFeRecuperado)
                {
                    ecoFeRecuperado = true;
                    nombre = "Eco de la Fe (Parroquia)";
                }
                break;
            case 2:
                if (!ecoTradicionRecuperado)
                {
                    ecoTradicionRecuperado = true;
                    nombre = "Eco de la Tradición (Mercado)";
                }
                break;
            case 3:
                if (!ecoMemoriaRecuperado)
                {
                    ecoMemoriaRecuperado = true;
                    nombre = "Eco de la Memoria (Cruz Azul)";
                }
                break;
        }

        float nuevaVidaMax = ObtenerVidaMaximaTotal();
        currentHealth = nuevaVidaMax;

        GuardarProgreso();
        OnEcoRecuperado?.Invoke(indiceEco, nombre);
        OnVidaMaximaActualizada?.Invoke(nuevaVidaMax);

        Debug.Log($"<color=#00E5FF>[SaveStateManager] ¡{nombre} recuperado! Vida máxima incrementada a: {nuevaVidaMax} HP.</color>");
    }

    public void RegistrarContraataqueExitoso()
    {
        contraataquesExitosos++;
    }

    public void RegistrarEnemigoDerrotado()
    {
        enemigosDerrotados++;
    }

    public void ModificarLlaves(int cantidad)
    {
        goldKeys = Mathf.Max(0, goldKeys + cantidad);
    }

    public bool GastarOro(int cantidad)
    {
        if (oro < cantidad) return false;
        oro -= cantidad;
        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null) pm.oro = oro;
        GuardarProgreso();
        OnOroCambiado?.Invoke(oro);
        return true;
    }

    public void AgregarOro(int cantidad)
    {
        oro += cantidad;
        GuardarProgreso();
        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null) pm.oro = oro;
        OnOroCambiado?.Invoke(oro);
        Debug.Log($"<color=#FFD700>[SaveStateManager] +{cantidad} Oro añadido. Oro total: {oro}</color>");
    }

    public void GuardarCheckpoint(string zonaId, Vector2 posicion)
    {
        ultimaZonaId = zonaId;
        ultimoCheckpoint = posicion;
        GuardarProgreso();
    }

    public void ConsagrarCruzAzul()
    {
        if (cruzAzulConsagrada || !ecoFeRecuperado || !ecoTradicionRecuperado || !ecoMemoriaRecuperado) return;
        cruzAzulConsagrada = true;
        PlayerPrefs.SetInt(BlueCrossRitualPreferenceKey, 1);
        PlayerPrefs.Save();
        GuardarProgreso();
    }

    public void RegistrarCheckpoint(string zonaId, Vector2 pos, PlayerMovement player)
    {
        if (player == null)
        {
            Debug.LogError("[SaveStateManager] No se puede registrar un checkpoint sin un jugador activo.");
            return;
        }

        if (string.IsNullOrWhiteSpace(zonaId))
        {
            Debug.LogError("[SaveStateManager] No se puede registrar un checkpoint sin una zona válida.");
            return;
        }

        snapshotActivo = CrearSnapshot(zonaId, pos, player);
        AplicarSnapshotGlobal(snapshotActivo);
        ultimaZonaId = zonaId;
        ultimoCheckpoint = pos;

        string json = JsonUtility.ToJson(snapshotActivo);
        PlayerPrefs.SetString(CheckpointPreferenceKey, json);
        PlayerPrefs.SetString(ProgressPreferenceKey, json);
        PlayerPrefs.Save();

        Debug.Log($"<color=#FFD700>[SaveStateManager] Checkpoint guardado: {zonaId} en {pos}.</color>");
    }

    public bool HayCheckpointParaZona(string zonaId)
    {
        if (snapshotActivo == null)
        {
            snapshotActivo = LeerSnapshot(CheckpointPreferenceKey);
        }

        return snapshotActivo != null &&
               !string.IsNullOrEmpty(zonaId) &&
               string.Equals(snapshotActivo.zonaId, zonaId, StringComparison.Ordinal);
    }

    public bool RestaurarJugadorDesdeCheckpoint(PlayerMovement player)
    {
        if (player == null)
        {
            Debug.LogError("[SaveStateManager] No se puede restaurar un checkpoint sin un jugador activo.");
            return false;
        }

        if (snapshotActivo == null)
        {
            snapshotActivo = LeerSnapshot(CheckpointPreferenceKey);
        }

        string zonaActual = LevelManager.EnsureInstance().currentZone.ToString();
        if (snapshotActivo == null || !string.Equals(snapshotActivo.zonaId, zonaActual, StringComparison.Ordinal))
        {
            return false;
        }

        if (snapshotActivo.estadoMundo == null)
            snapshotActivo.estadoMundo = CapturarEstadoMundoActual();
        IntegrarProgresoPermanenteEnCheckpoint();
        AplicarSnapshotGlobal(snapshotActivo);
        RestaurarEstadoDelJugador(player, snapshotActivo);
        TeletransportarJugador(player, snapshotActivo.posicion);
        RestaurarEstadoMundo(snapshotActivo.estadoMundo);
        PlayerPrefs.SetString(CheckpointPreferenceKey, JsonUtility.ToJson(snapshotActivo));
        GuardarProgreso();

        return true;
    }

    public void RecargarEscenaParaRespawn(bool desdeCheckpoint)
    {
        Scene escenaActual = SceneManager.GetActiveScene();
        if (!escenaActual.IsValid() || string.IsNullOrEmpty(escenaActual.name))
        {
            Debug.LogError("[SaveStateManager] No se puede recargar una escena inválida para el respawn.");
            return;
        }

        respawnPendiente = true;
        restaurarCheckpointPendiente = desdeCheckpoint;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        SceneManager.LoadScene(escenaActual.name);
    }

    public bool RegresarAlUltimoCheckpoint()
    {
        CheckpointSnapshot checkpoint = LeerSnapshot(CheckpointPreferenceKey);
        if (checkpoint == null)
        {
            Debug.LogWarning("[SaveStateManager] No hay un checkpoint válido al que regresar.");
            return false;
        }

        string escenaDestino = ObtenerEscenaDeZona(checkpoint.zonaId);
        if (string.IsNullOrEmpty(escenaDestino))
        {
            Debug.LogError($"[SaveStateManager] No se reconoce la zona del checkpoint: {checkpoint.zonaId}.");
            return false;
        }

        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
        string escenaActual = SceneManager.GetActiveScene().name;
        if (player != null && escenaActual != "PantallaInicio")
        {
            GuardarProgreso();
        }

        CheckpointSnapshot progreso = LeerSnapshot(ProgressPreferenceKey);
        CheckpointSnapshot continuacion = progreso != null
            ? JsonUtility.FromJson<CheckpointSnapshot>(JsonUtility.ToJson(progreso))
            : JsonUtility.FromJson<CheckpointSnapshot>(JsonUtility.ToJson(checkpoint));
        continuacion.zonaId = checkpoint.zonaId;
        continuacion.posicion = checkpoint.posicion;

        if (continuacion.estadosMundo == null)
            continuacion.estadosMundo = new List<WorldCheckpointState>();
        if (checkpoint.estadoMundo != null &&
            !continuacion.estadosMundo.Exists(estado => estado != null && estado.zonaId == checkpoint.estadoMundo.zonaId))
        {
            continuacion.estadosMundo.Add(ClonarEstadoMundo(checkpoint.estadoMundo));
        }
        WorldCheckpointState estadoDestino = continuacion.estadosMundo.Find(
            estado => estado != null && estado.zonaId == checkpoint.zonaId);
        continuacion.estadoMundo = estadoDestino != null
            ? ClonarEstadoMundo(estadoDestino)
            : ClonarEstadoMundo(checkpoint.estadoMundo);

        snapshotActivo = continuacion;
        AplicarSnapshotGlobal(continuacion);
        ultimaZonaId = continuacion.zonaId;
        ultimoCheckpoint = continuacion.posicion;
        PlayerPrefs.SetString(CheckpointPreferenceKey, JsonUtility.ToJson(continuacion));
        PlayerPrefs.SetString(ProgressPreferenceKey, JsonUtility.ToJson(continuacion));
        PlayerPrefs.Save();

        restaurarCheckpointPendiente = true;
        respawnPendiente = true;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        SceneManager.LoadScene(escenaDestino);
        return true;
    }

    private static string ObtenerEscenaDeZona(string zonaId)
    {
        switch (zonaId)
        {
            case "ParroquiaExterior": return "SampleScene";
            case "ParroquiaInterior": return "ParroquiaInterior";
            case "Mercado": return "Nivel2_Mercado";
            case "CerroDelFraile": return "Nivel3_CerroDelFraile";
            default: return null;
        }
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (respawnPendiente)
        {
            StartCoroutine(RutinaRestaurarRespawn(escena.name));
        }
        else if (ObtenerEstadoMundo(LevelManager.ObtenerZonaDeEscena(escena.name).ToString()) != null)
        {
            StartCoroutine(RutinaRestaurarMundoAlCargar(escena.name));
        }
    }

    private IEnumerator RutinaRestaurarMundoAlCargar(string nombreEscena)
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        WorldCheckpointState estado = ObtenerEstadoMundo(LevelManager.ObtenerZonaDeEscena(nombreEscena).ToString());
        if (estado != null) RestaurarEstadoMundo(estado);
    }

    private IEnumerator RutinaRestaurarRespawn(string nombreEscena)
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        respawnPendiente = false;
        PlayerMovement player = FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        if (player == null)
        {
            Debug.LogError("[SaveStateManager] No se encontró a Ronan después de recargar la escena para el respawn.");
            yield break;
        }

        LevelManager levelManager = LevelManager.EnsureInstance();
        levelManager.currentZone = LevelManager.ObtenerZonaDeEscena(nombreEscena);

        if (restaurarCheckpointPendiente)
        {
            if (!RestaurarJugadorDesdeCheckpoint(player))
            {
                Debug.LogWarning("[SaveStateManager] El checkpoint no coincide con la escena recargada; se usará el inicio del nivel.");
                RestaurarJugadorInicioNivel(player, levelManager.currentZone);
            }
        }
        else
        {
            RestaurarJugadorInicioNivel(player, levelManager.currentZone);
        }
        restaurarCheckpointPendiente = false;

        player.gameObject.SetActive(true);
        player.enabled = true;
        player.RestablecerEstadoTrasDerrota(player.transform.position);

        HealthComponent health = player.GetComponent<HealthComponent>();
        if (health != null)
        {
            health.RestablecerTrasDerrota(player.maxHealth, player.currentHealth);
            health.ActivarInvulnerabilidadTemporal(1.5f);
        }
        player.ActivarInvulnerabilidadTrasRespawn(1.5f);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameManager.GameState.Exploring);
        }
    }

    public void RestaurarJugadorInicioNivel(PlayerMovement player, LevelManager.ZoneId zona)
    {
        if (player == null)
        {
            Debug.LogError("[SaveStateManager] No se puede reiniciar el nivel sin un jugador activo.");
            return;
        }

        LevelManager levelManager = LevelManager.EnsureInstance();
        levelManager.currentZone = zona;
        Vector2 spawn = levelManager.ObtenerSpawnPointPorDefecto(zona);

        if (player.armas == null || player.armas.Count == 0)
        {
            player.armas = WeaponDefinition.CrearArmasPorDefecto();
        }
        AplicarArmasDesbloqueadas(player, arcoComprado, baculoComprado);

        player.maxHealth = ObtenerVidaMaximaTotal();
        player.currentHealth = player.maxHealth;
        player.manaActual = player.manaMaximo;
        player.flechas = player.TieneArmaDesbloqueada(WeaponType.Bow)
            ? Mathf.Max(15, player.flechas)
            : player.flechas;
        player.oro = oro;
        SincronizarComponenteSalud(player);

        TeletransportarJugador(player, spawn);
        currentHealth = player.currentHealth;
        GuardarProgreso();
    }

    private static void TeletransportarJugador(PlayerMovement player, Vector2 posicion)
    {
        player.transform.position = posicion;
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.position = posicion;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.transform.position = new Vector3(posicion.x, posicion.y, camera.transform.position.z);
        }
    }

    public void GuardarProgreso()
    {
        LevelManager levelManager = LevelManager.EnsureInstance();
        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
        string zonaId = levelManager.currentZone.ToString();
        Vector2 posicion = player != null ? (Vector2)player.transform.position : ultimoCheckpoint;

        CheckpointSnapshot progreso = CrearSnapshot(zonaId, posicion, player);
        if (progreso.estadoMundo != null)
            estadoMundoProgreso = ClonarEstadoMundo(progreso.estadoMundo);
        PlayerPrefs.SetString(ProgressPreferenceKey, JsonUtility.ToJson(progreso));
        PlayerPrefs.Save();
    }

    public void CargarProgreso()
    {
        RestablecerValoresIniciales();
        dificultadActual = (GameDifficulty)PlayerPrefs.GetInt("GameDifficulty", 1);
        cruzAzulConsagrada = PlayerPrefs.GetInt(BlueCrossRitualPreferenceKey, 0) == 1;

        CheckpointSnapshot progreso = LeerSnapshot(ProgressPreferenceKey);
        if (progreso != null)
        {
            AplicarSnapshotGlobal(progreso);
            ultimaZonaId = progreso.zonaId;
            ultimoCheckpoint = progreso.posicion;
        }

        snapshotActivo = LeerSnapshot(CheckpointPreferenceKey);
        if (progreso == null && snapshotActivo != null)
        {
            AplicarSnapshotGlobal(snapshotActivo);
            ultimaZonaId = snapshotActivo.zonaId;
            ultimoCheckpoint = snapshotActivo.posicion;
        }
        else if (estadoMundoProgreso == null && snapshotActivo != null)
        {
            estadoMundoProgreso = ClonarEstadoMundo(snapshotActivo.estadoMundo);
        }
    }

    private CheckpointSnapshot CrearSnapshot(string zonaId, Vector2 posicion, PlayerMovement player)
    {
        InventoryManager inventory = InventoryManager.Instance;
        List<string> items = inventory != null
            ? new List<string>(inventory.GetItems())
            : new List<string>();
        WorldCheckpointState estadoMundo = CapturarEstadoMundoActual();
        if (estadoMundo != null)
            GuardarEstadoMundoEnProgreso(estadoMundo);

        return new CheckpointSnapshot
        {
            schemaVersion = CurrentSaveSchemaVersion,
            zonaId = zonaId,
            posicion = posicion,
            vidaActual = player != null ? player.currentHealth : currentHealth,
            vidaMaxima = player != null ? Mathf.Max(player.maxHealth, ObtenerVidaMaximaTotal()) : ObtenerVidaMaximaTotal(),
            manaActual = player != null ? player.manaActual : 60f,
            flechasActuales = player != null ? player.flechas : 12,
            oro = player != null ? player.oro : oro,
            armaActualIndice = player != null ? player.armaActual : 0,
            arcoComprado = arcoComprado || (player != null && player.TieneArmaDesbloqueada(WeaponType.Bow)),
            baculoComprado = baculoComprado || (player != null && player.TieneArmaDesbloqueada(WeaponType.Staff)),
            barrasCobre = copperBars,
            barrasHierro = ironBars,
            barrasOro = goldBars,
            barrasMithril = mithrilBars,
            gemasVerdes = greenGems,
            gemasPurpuras = purpleGems,
            gemasRojas = redGems,
            gemasAmarillas = yellowGems,
            llavesOro = inventory != null ? inventory.goldKeysCount : goldKeys,
            ecoFe = ecoFeRecuperado,
            ecoTradicion = ecoTradicionRecuperado,
            ecoMemoria = ecoMemoriaRecuperado,
            cruzAzulConsagrada = cruzAzulConsagrada,
            itemsInventario = items,
            estadoMundo = ClonarEstadoMundo(estadoMundo),
            estadosMundo = ClonarEstadosMundo(estadosMundoProgreso)
        };
    }

    private void AplicarSnapshotGlobal(CheckpointSnapshot snapshot)
    {
        ecoFeRecuperado = snapshot.ecoFe;
        ecoTradicionRecuperado = snapshot.ecoTradicion;
        ecoMemoriaRecuperado = snapshot.ecoMemoria;
        cruzAzulConsagrada = snapshot.cruzAzulConsagrada || PlayerPrefs.GetInt(BlueCrossRitualPreferenceKey, 0) == 1;
        baseMaxHealth = Mathf.Max(1f, snapshot.vidaMaxima - TotalEcosEnSnapshot(snapshot) * 25f);
        currentHealth = snapshot.vidaActual;
        oro = snapshot.oro;
        arcoComprado = snapshot.arcoComprado;
        baculoComprado = snapshot.baculoComprado;
        copperBars = snapshot.barrasCobre;
        ironBars = snapshot.barrasHierro;
        goldBars = snapshot.barrasOro;
        mithrilBars = snapshot.barrasMithril;
        greenGems = snapshot.gemasVerdes;
        purpleGems = snapshot.gemasPurpuras;
        redGems = snapshot.gemasRojas;
        yellowGems = snapshot.gemasAmarillas;
        goldKeys = snapshot.llavesOro;
        itemsInventarioGuardados.Clear();
        if (snapshot.itemsInventario != null)
        {
            itemsInventarioGuardados.AddRange(snapshot.itemsInventario);
        }
        llavesInventarioGuardadas = snapshot.llavesOro;
        estadosMundoProgreso.Clear();
        if (snapshot.estadosMundo != null)
        {
            foreach (WorldCheckpointState estado in snapshot.estadosMundo)
                GuardarEstadoMundoEnProgreso(estado);
        }
        if (snapshot.estadoMundo != null)
            GuardarEstadoMundoEnProgreso(snapshot.estadoMundo);
        estadoMundoProgreso = ObtenerEstadoMundo(snapshot.zonaId);
        if (estadoMundoProgreso == null && snapshot.estadoMundo != null)
            estadoMundoProgreso = ClonarEstadoMundo(snapshot.estadoMundo);
        ultimaZonaId = snapshot.zonaId;
        ultimoCheckpoint = snapshot.posicion;

        InventoryManager inventory = InventoryManager.Instance;
        if (inventory != null)
        {
            RestaurarInventario(inventory);
        }

        OnOroCambiado?.Invoke(oro);
    }

    public void RestaurarInventario(InventoryManager inventory)
    {
        if (inventory == null) return;
        inventory.RestaurarInventario(itemsInventarioGuardados, llavesInventarioGuardadas);
    }

    private void RestaurarEstadoDelJugador(PlayerMovement player, CheckpointSnapshot snapshot)
    {
        if (player.armas == null || player.armas.Count == 0)
        {
            player.armas = WeaponDefinition.CrearArmasPorDefecto();
        }

        player.maxHealth = snapshot.vidaMaxima;
        player.currentHealth = snapshot.vidaActual;
        player.manaActual = Mathf.Clamp(snapshot.manaActual, 0f, player.manaMaximo);
        player.flechas = Mathf.Clamp(snapshot.flechasActuales, 0, player.maxFlechas);
        player.oro = snapshot.oro;
        AplicarArmasDesbloqueadas(player, snapshot.arcoComprado, snapshot.baculoComprado);

        int arma = Mathf.Clamp(snapshot.armaActualIndice, 0, player.armas.Count - 1);
        if (player.armas[arma].desbloqueada)
        {
            player.armaActual = arma;
        }
        else
        {
            player.armaActual = 0;
        }

        SincronizarComponenteSalud(player);
    }

    private static void SincronizarComponenteSalud(PlayerMovement player)
    {
        HealthComponent health = player.GetComponent<HealthComponent>();
        if (health != null)
            health.RestablecerTrasDerrota(player.maxHealth, player.currentHealth);
    }

    private static void AplicarArmasDesbloqueadas(PlayerMovement player, bool arcoComprado, bool baculoComprado)
    {
        foreach (WeaponDefinition arma in player.armas)
        {
            if (arma.tipo == WeaponType.Sword) arma.desbloqueada = true;
            else if (arma.tipo == WeaponType.Bow) arma.desbloqueada = arcoComprado;
            else if (arma.tipo == WeaponType.Staff) arma.desbloqueada = baculoComprado;
        }
    }

    private CheckpointSnapshot LeerSnapshot(string key)
    {
        if (!PlayerPrefs.HasKey(key)) return null;

        try
        {
            CheckpointSnapshot snapshot = JsonUtility.FromJson<CheckpointSnapshot>(PlayerPrefs.GetString(key));
            if (snapshot == null || !EsSnapshotValido(snapshot))
            {
                Debug.LogError($"[SaveStateManager] El archivo de guardado '{key}' no contiene un checkpoint válido.");
                return null;
            }
            if (snapshot.schemaVersion < CurrentSaveSchemaVersion)
            {
                snapshot.schemaVersion = CurrentSaveSchemaVersion;
                if (snapshot.itemsInventario == null) snapshot.itemsInventario = new List<string>();
                if (snapshot.estadosMundo == null) snapshot.estadosMundo = new List<WorldCheckpointState>();
            }
            return snapshot;
        }
        catch (ArgumentException exception)
        {
            Debug.LogError($"[SaveStateManager] No se pudo leer el guardado '{key}': {exception.Message}");
            return null;
        }
    }

    private static bool EsSnapshotValido(CheckpointSnapshot snapshot)
    {
        return snapshot.schemaVersion >= 0 &&
               snapshot.schemaVersion <= CurrentSaveSchemaVersion &&
               Enum.TryParse(snapshot.zonaId, false, out LevelManager.ZoneId zona) &&
               Enum.IsDefined(typeof(LevelManager.ZoneId), zona) &&
               !float.IsNaN(snapshot.posicion.x) &&
               !float.IsInfinity(snapshot.posicion.x) &&
               !float.IsNaN(snapshot.posicion.y) &&
               !float.IsInfinity(snapshot.posicion.y) &&
               snapshot.vidaMaxima > 0f &&
               !float.IsNaN(snapshot.vidaMaxima) &&
               !float.IsInfinity(snapshot.vidaMaxima) &&
               snapshot.vidaActual >= 0f &&
               snapshot.vidaActual <= snapshot.vidaMaxima;
    }

    private static int TotalEcosEnSnapshot(CheckpointSnapshot snapshot)
    {
        int total = 0;
        if (snapshot.ecoFe) total++;
        if (snapshot.ecoTradicion) total++;
        if (snapshot.ecoMemoria) total++;
        return total;
    }

    private void RestablecerValoresIniciales()
    {
        ecoFeRecuperado = false;
        ecoTradicionRecuperado = false;
        ecoMemoriaRecuperado = false;
        cruzAzulConsagrada = false;
        currentHealth = baseMaxHealth;
        goldKeys = 0;
        oro = 50;
        arcoComprado = false;
        baculoComprado = false;
        copperBars = 0;
        ironBars = 0;
        goldBars = 0;
        mithrilBars = 0;
        greenGems = 0;
        purpleGems = 0;
        redGems = 0;
        yellowGems = 0;
        tiempoDeJuego = 0f;
        contraataquesExitosos = 0;
        enemigosDerrotados = 0;
        ultimaZonaId = "ParroquiaExterior";
        ultimoCheckpoint = Vector2.zero;
        snapshotActivo = null;
        estadoMundoProgreso = null;
        estadosMundoProgreso.Clear();
        itemsInventarioGuardados.Clear();
        llavesInventarioGuardadas = 0;
    }

    public void EstablecerDificultad(GameDifficulty dif)
    {
        dificultadActual = dif;
        PlayerPrefs.SetInt("GameDifficulty", (int)dif);
        PlayerPrefs.Save();
        Debug.Log($"<color=#00E5FF>[SaveStateManager] Dificultad actualizada a: {dif}</color>");
    }

    // Modificadores de Dificultad en Tiempo Real
    public float MultiplicadorDanioRecibidoRonan => dificultadActual switch
    {
        GameDifficulty.Facil => 0.70f,
        GameDifficulty.Dificil => 1.40f,
        _ => 1.0f
    };

    public float MultiplicadorSaludEnemigo => dificultadActual switch
    {
        GameDifficulty.Facil => 0.80f,
        GameDifficulty.Dificil => 1.30f,
        _ => 1.0f
    };

    public float MultiplicadorVelocidadEnemigo => dificultadActual switch
    {
        GameDifficulty.Facil => 0.85f,
        GameDifficulty.Dificil => 1.25f,
        _ => 1.0f
    };

    public float VentanaContraataqueSegundos => (dificultadActual switch
    {
        GameDifficulty.Facil => 0.28f,
        GameDifficulty.Dificil => 0.16f,
        _ => 0.20f
    }) + (MobileInput.IsTouchControlsActive ? 0.06f : 0f);

    public float MultiplicadorDropCorazones => dificultadActual switch
    {
        GameDifficulty.Facil => 0.9f,
        GameDifficulty.Dificil => 0.65f,
        _ => 0.78f
    };

    public float MultiplicadorProbabilidadDrops => dificultadActual switch
    {
        GameDifficulty.Facil => 0.72f,
        GameDifficulty.Dificil => 0.36f,
        _ => 0.54f
    };

    public float MultiplicadorDropOro => dificultadActual switch
    {
        GameDifficulty.Facil => 1.0f,
        GameDifficulty.Dificil => 0.65f,
        _ => 0.82f
    };

    public void ReiniciarProgreso()
    {
        RestablecerValoresIniciales();
        respawnPendiente = false;
        restaurarCheckpointPendiente = false;
        LevelManager.EnsureInstance().currentZone = LevelManager.ZoneId.ParroquiaExterior;
        dificultadActual = (GameDifficulty)PlayerPrefs.GetInt("GameDifficulty", 1);
        PlayerPrefs.DeleteKey(CheckpointPreferenceKey);
        PlayerPrefs.DeleteKey(ProgressPreferenceKey);
        PlayerPrefs.DeleteKey(BlueCrossRitualPreferenceKey);
        PlayerPrefs.Save();

        InventoryManager inventory = InventoryManager.Instance;
        if (inventory != null)
        {
            inventory.RestaurarInventario(new List<string>(), 0);
        }
    }

    public void RegistrarEstadoMundoActual()
    {
        WorldCheckpointState estado = CapturarEstadoMundoActual();
        if (estado == null) return;
        GuardarEstadoMundoEnProgreso(estado);
        GuardarProgreso();
    }

    public void RegistrarDerrotaJefeFinal()
    {
        WorldCheckpointState estado = CapturarEstadoMundoActual();
        if (estado == null) return;
        if (estado.jefe == null)
            estado.jefe = new BossCheckpointState();
        estado.jefe.presente = true;
        estado.jefe.derrotado = true;
        GuardarEstadoMundoEnProgreso(estado);
        GuardarProgreso();
    }

    private WorldCheckpointState CapturarEstadoMundoActual()
    {
        LevelManager levelManager = LevelManager.EnsureInstance();
        string zonaId = levelManager.currentZone.ToString();
        WorldCheckpointState previo = ObtenerEstadoMundo(zonaId);
        WorldCheckpointState estado = previo != null
            ? ClonarEstadoMundo(previo)
            : new WorldCheckpointState { zonaId = zonaId };
        estado.zonaId = zonaId;

        TreasureChestNode[] cofresEnEscena = FindObjectsByType<TreasureChestNode>(FindObjectsInactive.Include);
        if (cofresEnEscena.Length > 0)
        {
            estado.cofres = new List<ChestCheckpointState>(cofresEnEscena.Length);
            foreach (TreasureChestNode cofre in cofresEnEscena)
            {
                if (cofre == null || string.IsNullOrWhiteSpace(cofre.chestId)) continue;
                estado.cofres.Add(new ChestCheckpointState
                {
                    id = cofre.chestId,
                    abierto = cofre.Opened
                });
            }
        }

        if (levelManager.currentZone == LevelManager.ZoneId.CerroDelFraile)
        {
            BlueCrossShrinePuzzle puzzle = FindAnyObjectByType<BlueCrossShrinePuzzle>(FindObjectsInactive.Include);
            if (puzzle != null)
            {
                estado.santuarioPresente = true;
                puzzle.CapturarEstadoCheckpoint(estado);

                SombraGuardianaBossAI jefe = FindAnyObjectByType<SombraGuardianaBossAI>(FindObjectsInactive.Include);
                if (jefe != null)
                {
                    jefe.CapturarEstadoCheckpoint(estado);
                }
            }
        }

        return estado;
    }

    private void RestaurarEstadoMundo(WorldCheckpointState estado)
    {
        if (estado == null) return;

        TreasureChestNode[] cofresEnEscena = FindObjectsByType<TreasureChestNode>(FindObjectsInactive.Include);
        if (estado.cofres != null)
        {
            foreach (TreasureChestNode cofre in cofresEnEscena)
            {
                if (cofre == null) continue;
                ChestCheckpointState guardado = estado.cofres.Find(c => c != null && c.id == cofre.chestId);
                if (guardado != null) cofre.RestaurarEstadoCheckpoint(guardado.abierto);
            }
        }

        if (estado.santuarioPresente)
        {
            BlueCrossShrinePuzzle puzzle = FindAnyObjectByType<BlueCrossShrinePuzzle>(FindObjectsInactive.Include);
            SombraGuardianaBossAI jefe = FindAnyObjectByType<SombraGuardianaBossAI>(FindObjectsInactive.Include);
            if (puzzle == null)
            {
                Debug.LogError("[SaveStateManager] No se encontró el santuario para restaurar el estado del checkpoint.");
                return;
            }

            puzzle.RestaurarEstadoCheckpoint(estado);
            if (jefe != null && estado.jefe != null)
            {
                jefe.RestaurarEstadoCheckpoint(estado.jefe);
            }
            else if (estado.jefe != null && !estado.jefe.derrotado)
            {
                Debug.LogError("[SaveStateManager] No se encontró a La Sombra Guardiana para restaurar el estado del checkpoint.");
                return;
            }
            else if (estado.jefe != null && estado.jefe.derrotado && !ecoMemoriaRecuperado)
            {
                SombraGuardianaBossAI.CrearEcoDeCheckpoint(estado.jefe.posicion, 3);
            }
        }

        GuardarEstadoMundoEnProgreso(estado);
    }

    public bool EstaCofreAbierto(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        foreach (WorldCheckpointState estado in estadosMundoProgreso)
        {
            if (estado == null || estado.cofres == null) continue;
            ChestCheckpointState cofre = estado.cofres.Find(c => c != null && c.id == id);
            if (cofre != null) return cofre.abierto;
        }
        return false;
    }

    private WorldCheckpointState ObtenerEstadoMundo(string zonaId)
    {
        if (string.IsNullOrWhiteSpace(zonaId)) return null;
        return estadosMundoProgreso.Find(estado => estado != null && estado.zonaId == zonaId);
    }

    private void GuardarEstadoMundoEnProgreso(WorldCheckpointState estado)
    {
        if (estado == null || string.IsNullOrWhiteSpace(estado.zonaId)) return;
        WorldCheckpointState clon = ClonarEstadoMundo(estado);
        int indice = estadosMundoProgreso.FindIndex(actual => actual != null && actual.zonaId == clon.zonaId);
        if (indice >= 0)
            estadosMundoProgreso[indice] = clon;
        else
            estadosMundoProgreso.Add(clon);
        estadoMundoProgreso = ClonarEstadoMundo(clon);
    }

    private static List<WorldCheckpointState> ClonarEstadosMundo(List<WorldCheckpointState> estados)
    {
        List<WorldCheckpointState> clones = new List<WorldCheckpointState>();
        if (estados == null) return clones;
        foreach (WorldCheckpointState estado in estados)
        {
            WorldCheckpointState clon = ClonarEstadoMundo(estado);
            if (clon != null) clones.Add(clon);
        }
        return clones;
    }

    private void IntegrarProgresoPermanenteEnCheckpoint()
    {
        CheckpointSnapshot progreso = LeerSnapshot(ProgressPreferenceKey);
        snapshotActivo.estadosMundo = ClonarEstadosMundo(snapshotActivo.estadosMundo);
        if (snapshotActivo.estadoMundo != null)
        {
            int indiceEstadoCheckpoint = snapshotActivo.estadosMundo.FindIndex(
                estado => estado != null && estado.zonaId == snapshotActivo.estadoMundo.zonaId);
            if (indiceEstadoCheckpoint >= 0)
                snapshotActivo.estadosMundo[indiceEstadoCheckpoint] = ClonarEstadoMundo(snapshotActivo.estadoMundo);
            else
                snapshotActivo.estadosMundo.Add(ClonarEstadoMundo(snapshotActivo.estadoMundo));
        }
        if (progreso != null)
        {
            if (progreso.estadosMundo != null)
            {
                foreach (WorldCheckpointState estado in progreso.estadosMundo)
                    IntegrarCofresAbiertosEnSnapshot(estado);
            }
            IntegrarCofresAbiertosEnSnapshot(progreso.estadoMundo);
        }

        WorldCheckpointState estadoZona = snapshotActivo.estadosMundo.Find(
            estado => estado != null && estado.zonaId == snapshotActivo.zonaId);
        if (estadoZona != null)
        {
            snapshotActivo.estadoMundo = ClonarEstadoMundo(estadoZona);
        }

        int totalEcosCheckpoint = TotalEcosEnSnapshot(snapshotActivo);
        float vidaBase = Mathf.Max(1f, snapshotActivo.vidaMaxima - totalEcosCheckpoint * 25f);
        snapshotActivo.ecoFe |= ecoFeRecuperado || (progreso != null && progreso.ecoFe);
        snapshotActivo.ecoTradicion |= ecoTradicionRecuperado || (progreso != null && progreso.ecoTradicion);
        snapshotActivo.ecoMemoria |= ecoMemoriaRecuperado || (progreso != null && progreso.ecoMemoria);
        snapshotActivo.cruzAzulConsagrada |= cruzAzulConsagrada ||
            (progreso != null && progreso.cruzAzulConsagrada) ||
            PlayerPrefs.GetInt(BlueCrossRitualPreferenceKey, 0) == 1;
        snapshotActivo.vidaMaxima = vidaBase + TotalEcosEnSnapshot(snapshotActivo) * 25f;
        snapshotActivo.vidaActual = Mathf.Clamp(snapshotActivo.vidaActual, 0f, snapshotActivo.vidaMaxima);
    }

    private void IntegrarCofresAbiertosEnSnapshot(WorldCheckpointState estadoProgreso)
    {
        if (estadoProgreso == null || string.IsNullOrWhiteSpace(estadoProgreso.zonaId)) return;
        int indice = snapshotActivo.estadosMundo.FindIndex(
            estado => estado != null && estado.zonaId == estadoProgreso.zonaId);
        if (indice < 0)
        {
            snapshotActivo.estadosMundo.Add(ClonarEstadoMundo(estadoProgreso));
            return;
        }

        WorldCheckpointState estadoCheckpoint = snapshotActivo.estadosMundo[indice];
        if (estadoProgreso.cofres == null) return;
        if (estadoCheckpoint.cofres == null)
            estadoCheckpoint.cofres = new List<ChestCheckpointState>();

        foreach (ChestCheckpointState cofreProgreso in estadoProgreso.cofres)
        {
            if (cofreProgreso == null || !cofreProgreso.abierto) continue;
            ChestCheckpointState cofreCheckpoint = estadoCheckpoint.cofres.Find(
                cofre => cofre != null && cofre.id == cofreProgreso.id);
            if (cofreCheckpoint != null)
                cofreCheckpoint.abierto = true;
            else
                estadoCheckpoint.cofres.Add(new ChestCheckpointState { id = cofreProgreso.id, abierto = true });
        }
    }

    private static WorldCheckpointState ClonarEstadoMundo(WorldCheckpointState estado)
    {
        return estado == null ? null : JsonUtility.FromJson<WorldCheckpointState>(JsonUtility.ToJson(estado));
    }
}
