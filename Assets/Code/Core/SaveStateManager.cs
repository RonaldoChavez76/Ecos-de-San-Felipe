using System;
using UnityEngine;

/// <summary>
/// Gestiona la persistencia de datos globales del jugador, Ecos recuperados,
/// estadísticas y estado entre escenas y transiciones de zonas.
/// Auto-instanciable como Singleton persistente para que nunca sea nulo.
/// </summary>
public class SaveStateManager : MonoBehaviour
{
    private static SaveStateManager _instance;
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

    public void GuardarProgreso()
    {
        // Progreso estrictamente en memoria para la sesión activa (sin persistencia en disco ni PlayerPrefs)
    }

    public void CargarProgreso()
    {
        // Progreso de sesión única en memoria
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

        // Cargar dificultad persistida en PlayerPrefs
        dificultadActual = (GameDifficulty)PlayerPrefs.GetInt("GameDifficulty", 1);
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
        CargarProgreso();
    }
}
