using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CheckpointSnapshot
{
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
    public List<string> itemsInventario = new List<string>();
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
    private static SaveStateManager _instance;
    private CheckpointSnapshot snapshotActivo;
    private readonly List<string> itemsInventarioGuardados = new List<string>();
    private int llavesInventarioGuardadas;

    public CheckpointSnapshot SnapshotActivo => snapshotActivo;

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
        CargarProgreso();
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
        GuardarProgreso();
        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null) pm.oro = oro;
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

        AplicarSnapshotGlobal(snapshotActivo);
        RestaurarEstadoDelJugador(player, snapshotActivo);
        TeletransportarJugador(player, snapshotActivo.posicion);

        return true;
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
        PlayerPrefs.SetString(ProgressPreferenceKey, JsonUtility.ToJson(progreso));
        PlayerPrefs.Save();
    }

    public void CargarProgreso()
    {
        RestablecerValoresIniciales();
        dificultadActual = (GameDifficulty)PlayerPrefs.GetInt("GameDifficulty", 1);

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
    }

    private CheckpointSnapshot CrearSnapshot(string zonaId, Vector2 posicion, PlayerMovement player)
    {
        InventoryManager inventory = InventoryManager.Instance;
        List<string> items = inventory != null
            ? new List<string>(inventory.GetItems())
            : new List<string>();

        return new CheckpointSnapshot
        {
            zonaId = zonaId,
            posicion = posicion,
            vidaActual = player != null ? player.currentHealth : currentHealth,
            vidaMaxima = player != null ? player.maxHealth : ObtenerVidaMaximaTotal(),
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
            itemsInventario = items
        };
    }

    private void AplicarSnapshotGlobal(CheckpointSnapshot snapshot)
    {
        ecoFeRecuperado = snapshot.ecoFe;
        ecoTradicionRecuperado = snapshot.ecoTradicion;
        ecoMemoriaRecuperado = snapshot.ecoMemoria;
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
        return Enum.TryParse(snapshot.zonaId, false, out LevelManager.ZoneId zona) &&
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

    public float VentanaContraataqueSegundos => dificultadActual switch
    {
        GameDifficulty.Facil => 0.28f,
        GameDifficulty.Dificil => 0.16f,
        _ => 0.20f
    };

    public float MultiplicadorDropCorazones => dificultadActual switch
    {
        GameDifficulty.Facil => 1.50f,
        GameDifficulty.Dificil => 0.60f,
        _ => 1.0f
    };

    public float MultiplicadorDropOro => dificultadActual switch
    {
        GameDifficulty.Dificil => 1.50f,
        _ => 1.0f
    };

    public void ReiniciarProgreso()
    {
        RestablecerValoresIniciales();
        dificultadActual = (GameDifficulty)PlayerPrefs.GetInt("GameDifficulty", 1);
        PlayerPrefs.DeleteKey(CheckpointPreferenceKey);
        PlayerPrefs.DeleteKey(ProgressPreferenceKey);
        PlayerPrefs.Save();

        InventoryManager inventory = InventoryManager.Instance;
        if (inventory != null)
        {
            inventory.RestaurarInventario(new List<string>(), 0);
        }
    }
}
