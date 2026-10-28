using System;
using UnityEngine;

/// <summary>
/// Gestiona la persistencia de datos globales del jugador, Ecos recuperados,
/// estadísticas y estado entre escenas y transiciones de zonas.
/// </summary>
public class SaveStateManager : MonoBehaviour
{
    public static SaveStateManager Instance { get; private set; }

    [Header("Ecos Recuperados")]
    public bool ecoFeRecuperado = false;         // Nivel 1: Parroquia
    public bool ecoTradicionRecuperado = false;  // Nivel 2: Mercado
    public bool ecoMemoriaRecuperado = false;    // Nivel 3: Cerro del Fraile (Cruz Azul)

    [Header("Progreso y Estadísticas de Ronan")]
    public float baseMaxHealth = 100f;
    public float currentHealth = 100f;
    public int goldKeys = 0;
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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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
        currentHealth = nuevaVidaMax; // Al recuperar un eco se restaura la vida al 100%

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

    public void GuardarCheckpoint(string zonaId, Vector2 posicion)
    {
        ultimaZonaId = zonaId;
        ultimoCheckpoint = posicion;
        GuardarProgreso();
    }

    public void GuardarProgreso()
    {
        PlayerPrefs.SetInt("Save_EcoFe", ecoFeRecuperado ? 1 : 0);
        PlayerPrefs.SetInt("Save_EcoTradicion", ecoTradicionRecuperado ? 1 : 0);
        PlayerPrefs.SetInt("Save_EcoMemoria", ecoMemoriaRecuperado ? 1 : 0);
        PlayerPrefs.SetFloat("Save_Health", currentHealth);
        PlayerPrefs.SetInt("Save_Keys", goldKeys);
        PlayerPrefs.SetFloat("Save_Time", tiempoDeJuego);
        PlayerPrefs.SetInt("Save_Counters", contraataquesExitosos);
        PlayerPrefs.SetInt("Save_Kills", enemigosDerrotados);
        PlayerPrefs.SetString("Save_Zone", ultimaZonaId);
        PlayerPrefs.SetFloat("Save_PosX", ultimoCheckpoint.x);
        PlayerPrefs.SetFloat("Save_PosY", ultimoCheckpoint.y);
        PlayerPrefs.Save();
    }

    public void CargarProgreso()
    {
        ecoFeRecuperado = PlayerPrefs.GetInt("Save_EcoFe", 0) == 1;
        ecoTradicionRecuperado = PlayerPrefs.GetInt("Save_EcoTradicion", 0) == 1;
        ecoMemoriaRecuperado = PlayerPrefs.GetInt("Save_EcoMemoria", 0) == 1;
        currentHealth = PlayerPrefs.GetFloat("Save_Health", ObtenerVidaMaximaTotal());
        goldKeys = PlayerPrefs.GetInt("Save_Keys", 0);
        tiempoDeJuego = PlayerPrefs.GetFloat("Save_Time", 0f);
        contraataquesExitosos = PlayerPrefs.GetInt("Save_Counters", 0);
        enemigosDerrotados = PlayerPrefs.GetInt("Save_Kills", 0);
        ultimaZonaId = PlayerPrefs.GetString("Save_Zone", "ParroquiaExterior");
        ultimoCheckpoint = new Vector2(
            PlayerPrefs.GetFloat("Save_PosX", 0f),
            PlayerPrefs.GetFloat("Save_PosY", 0f)
        );
    }

    public void ReiniciarProgreso()
    {
        ecoFeRecuperado = false;
        ecoTradicionRecuperado = false;
        ecoMemoriaRecuperado = false;
        currentHealth = baseMaxHealth;
        goldKeys = 0;
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
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}
