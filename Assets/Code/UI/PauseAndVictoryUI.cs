using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pantallas de victoria de «Ecos de San Felipe»:
///  • Nivel Completado (al recoger cada Eco, tras la animación de recolección).
///  • Victoria Final (cuando Ronan recupera los 3 Ecos Sagrados).
/// Toda la tipografía usa <see cref="UITheme"/> (fuentes escaladas, alto contraste, texto con sombra).
/// </summary>
public class PauseAndVictoryUI : MonoBehaviour
{
    public static PauseAndVictoryUI Instance { get; private set; }

    /// <summary>Mientras sea true (Eco ascendiendo / Ronan celebrando) no se muestra ninguna pantalla de victoria.</summary>
    public static bool SecuenciaEcoEnCurso = false;

    [Header("Pantalla de Victoria de Nivel")]
    public bool mostrarVictoriaNivel = false;
    public int nivelCompletado = 1;
    public string nombreEcoCompletado = "Eco de la Fe";
    private PlayerMovement ronanRef;

    [Header("Pantalla de Victoria Final")]
    public bool mostrarPantallaVictoria = false;
    private bool victoriaYaActivada = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        SecuenciaEcoEnCurso = false;
    }

    public static PauseAndVictoryUI EnsureInstance()
    {
        if (Instance != null) return Instance;
        PauseAndVictoryUI existing = FindAnyObjectByType<PauseAndVictoryUI>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }
        GameObject go = new GameObject("PauseAndVictoryUI");
        Instance = go.AddComponent<PauseAndVictoryUI>();
        return Instance;
    }

    private void Update()
    {
        if (victoriaYaActivada || mostrarVictoriaNivel || SecuenciaEcoEnCurso) return;

        if (SaveStateManager.Instance != null &&
            (SaveStateManager.Instance.TotalEcosRecuperados() >= 3 || SaveStateManager.Instance.ecoMemoriaRecuperado))
        {
            victoriaYaActivada = true;
            mostrarPantallaVictoria = true;
            ronanRef = FindAnyObjectByType<PlayerMovement>();
            Time.timeScale = 0f;
            AudioManager.Instance?.PlayEchoSound();
        }
    }

    /// <summary>
    /// Muestra la pantalla de nivel completado con detalles del Eco recuperado y recompensas.
    /// Si se recupera el 3er Eco (Eco de la Memoria) o ya se tienen los 3 Ecos, activa la pantalla de Victoria Final.
    /// </summary>
    public void MostrarVictoriaDeNivel(int indiceNivel, string nombreEco, PlayerMovement player = null)
    {
        ronanRef = player != null ? player : FindAnyObjectByType<PlayerMovement>();

        if (indiceNivel >= 3 ||
            (SaveStateManager.Instance != null && (SaveStateManager.Instance.TotalEcosRecuperados() >= 3 || SaveStateManager.Instance.ecoMemoriaRecuperado)))
        {
            victoriaYaActivada = true;
            mostrarPantallaVictoria = true;
            Time.timeScale = 0f;
            AudioManager.Instance?.PlayEchoSound();
            return;
        }

        nivelCompletado = indiceNivel;
        nombreEcoCompletado = nombreEco;
        mostrarVictoriaNivel = true;
        Time.timeScale = 0f; // Pausa el juego mientras se muestra el resumen

        AudioManager.Instance?.PlayEchoSound();
    }

    /// <summary>Cierra la pantalla de victoria de nivel, reanuda el tiempo y libera a Ronan de la pose.</summary>
    public void CerrarVictoriaDeNivel()
    {
        mostrarVictoriaNivel = false;
        Time.timeScale = 1f;
        LiberarARonan();
    }

    private void LiberarARonan()
    {
        if (ronanRef == null) ronanRef = FindAnyObjectByType<PlayerMovement>();
        if (ronanRef != null) ronanRef.DesactivarPoseDeVictoria();
    }

    private void OnGUI()
    {
        if (mostrarVictoriaNivel) DibujarPantallaVictoriaNivel();
        else if (mostrarPantallaVictoria) DibujarPantallaVictoriaFinal();
    }

    // ------------------------------------------------------------------------------------------
    private struct DatosNivel
    {
        public string zona, lore, siguiente;
    }

    private static DatosNivel ObtenerDatosNivel(int nivel, string nombreEco)
    {
        switch (nivel)
        {
            case 2:
                return new DatosNivel
                {
                    zona = "MERCADO DE SAN FELIPE",
                    lore = $"Con el <b>{nombreEco}</b> en tus manos, los puestos del mercado recuperan su bullicio y las recetas y oficios de generaciones vuelven a la memoria del pueblo.",
                    siguiente = "El camino hacia la <b>Cruz Azul</b> está despejado."
                };
            default:
                return new DatosNivel
                {
                    zona = "PARROQUIA DE SAN FELIPE APÓSTOL",
                    lore = $"Con el <b>{nombreEco}</b> en tus manos, el repique de las campanas y la devoción de los guardianes disipan la Niebla del Olvido sobre el atrio y el templo. Los cantos de Torres Mochas han sido preservados.",
                    siguiente = "El camino hacia el <b>Mercado de San Felipe</b> está despejado."
                };
        }
    }

    private void DibujarPantallaVictoriaNivel()
    {
        Color previo = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), UITheme.Blanca);
        GUI.color = Color.white;

        float panelW = Mathf.Min(UITheme.Px(780f), Screen.width - 30f);
        float panelH = Mathf.Min(UITheme.Px(560f), Screen.height - 30f);
        float px = (Screen.width - panelW) * 0.5f;
        float py = (Screen.height - panelH) * 0.5f;
        float pad = UITheme.Px(28f);
        float innerW = panelW - pad * 2f;

        UITheme.Panel(new Rect(px, py, panelW, panelH), UITheme.BordeDorado, 3f);

        DatosNivel datos = ObtenerDatosNivel(nivelCompletado, nombreEcoCompletado);
        SaveStateManager ssm = SaveStateManager.Instance;
        float vidaTotal = ssm != null ? ssm.ObtenerVidaMaximaTotal() : 125f;
        int ecos = ssm != null ? ssm.TotalEcosRecuperados() : nivelCompletado;
        int counters = ssm != null ? ssm.contraataquesExitosos : 0;
        int kills = ssm != null ? ssm.enemigosDerrotados : 0;

        float y = py + UITheme.Px(18f);

        UITheme.EtiquetaConSombra(new Rect(px, y, panelW, UITheme.Px(40f)),
            $"★ ¡NIVEL {nivelCompletado} COMPLETADO! ★",
            UITheme.Estilo(30, FontStyle.Bold, TextAnchor.MiddleCenter, UITheme.Dorado));
        y += UITheme.Px(44f);

        GUI.Label(new Rect(px, y, panelW, UITheme.Px(26f)), datos.zona,
            UITheme.Estilo(17, FontStyle.Bold, TextAnchor.MiddleCenter, UITheme.Turquesa));
        y += UITheme.Px(30f);

        GUI.Label(new Rect(px, y, panelW, UITheme.Px(26f)),
            $"✦ {nombreEcoCompletado.ToUpper()} RECUPERADO  ({ecos} / 3) ✦",
            UITheme.Estilo(17, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
        y += UITheme.Px(40f);

        // Narrativa
        Rect rLore = new Rect(px + pad, y, innerW, UITheme.Px(96f));
        GUI.color = new Color(0.12f, 0.16f, 0.26f, 1f);
        GUI.DrawTexture(rLore, UITheme.Blanca);
        GUI.color = Color.white;
        GUI.Label(new Rect(rLore.x + UITheme.Px(12f), rLore.y + UITheme.Px(6f), rLore.width - UITheme.Px(24f), rLore.height - UITheme.Px(12f)),
            datos.lore, UITheme.Estilo(16, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white, true));
        y += rLore.height + UITheme.Px(14f);

        // Recompensas
        Rect rRew = new Rect(px + pad, y, innerW, UITheme.Px(190f));
        GUI.color = new Color(0.07f, 0.20f, 0.14f, 1f);
        GUI.DrawTexture(rRew, UITheme.Blanca);
        GUI.color = Color.white;
        GUI.Label(new Rect(rRew.x + UITheme.Px(14f), rRew.y + UITheme.Px(8f), rRew.width - UITheme.Px(28f), UITheme.Px(26f)),
            "RECOMPENSAS", UITheme.Estilo(17, FontStyle.Bold, TextAnchor.MiddleLeft, UITheme.Verde));

        string rewText =
            $"•  <b>+25 Salud Máxima permanente</b>  →  {vidaTotal:0} HP\n" +
            "•  <b>Vida restaurada por completo</b>\n" +
            $"•  <b>Estadísticas:</b> {kills} sombras vencidas  |  {counters} contraataques perfectos\n" +
            "•  " + datos.siguiente;
        GUI.Label(new Rect(rRew.x + UITheme.Px(14f), rRew.y + UITheme.Px(38f), rRew.width - UITheme.Px(28f), rRew.height - UITheme.Px(44f)),
            rewText, UITheme.Estilo(16, FontStyle.Normal, TextAnchor.UpperLeft, Color.white, true));

        // Botones
        float btnH = UITheme.Px(46f);
        float gap = UITheme.Px(12f);
        float btnW = (innerW - gap * 2f) / 3f;
        float by = py + panelH - btnH - UITheme.Px(18f);
        GUIStyle boton = UITheme.Boton(16);

        string txtSiguienteNivel = nivelCompletado == 1
            ? "Ir al Nivel 2: El Mercado"
            : (nivelCompletado == 2 ? "Ir al Nivel 3: Cerro del Fraile" : "Continuar Aventura");

        if (GUI.Button(new Rect(px + pad, by, btnW, btnH), txtSiguienteNivel, boton))
        {
            Time.timeScale = 1f;
            mostrarVictoriaNivel = false;
            LiberarARonan();
            if (nivelCompletado == 1)
            {
                SceneManager.LoadScene("Nivel2_Mercado");
            }
            else if (nivelCompletado == 2)
            {
                SceneManager.LoadScene("Nivel3_CerroDelFraile");
            }
        }
        if (GUI.Button(new Rect(px + pad + btnW + gap, by, btnW, btnH), "Seguir Explorando", boton))
        {
            CerrarVictoriaDeNivel();
        }
        if (GUI.Button(new Rect(px + pad + (btnW + gap) * 2f, by, btnW, btnH), "Menú Principal", boton))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("PantallaInicio");
        }

        GUI.color = previo;
    }

    private void DibujarPantallaVictoriaFinal()
    {
        Color previo = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), UITheme.Blanca);
        GUI.color = Color.white;

        float panelW = Mathf.Min(UITheme.Px(760f), Screen.width - 30f);
        float panelH = Mathf.Min(UITheme.Px(520f), Screen.height - 30f);
        float px = (Screen.width - panelW) * 0.5f;
        float py = (Screen.height - panelH) * 0.5f;
        float pad = UITheme.Px(28f);
        float innerW = panelW - pad * 2f;

        UITheme.Panel(new Rect(px, py, panelW, panelH), UITheme.BordeDorado, 3f);

        UITheme.EtiquetaConSombra(new Rect(px, py + UITheme.Px(18f), panelW, UITheme.Px(40f)),
            "★ ¡SAN FELIPE HA RECUPERADO SU MEMORIA! ★",
            UITheme.Estilo(26, FontStyle.Bold, TextAnchor.MiddleCenter, UITheme.Dorado));

        SaveStateManager ssm = SaveStateManager.Instance;
        int ecos = ssm != null ? ssm.TotalEcosRecuperados() : 3;
        float maxHp = ssm != null ? ssm.ObtenerVidaMaximaTotal() : 175f;
        int counters = ssm != null ? ssm.contraataquesExitosos : 0;
        int kills = ssm != null ? ssm.enemigosDerrotados : 0;
        float secs = ssm != null ? ssm.tiempoDeJuego : 0f;
        int mins = Mathf.FloorToInt(secs / 60f);
        int remSecs = Mathf.FloorToInt(secs % 60f);

        string resumen =
            "Con la caída de La Sombra Guardiana en el Cerro del Fraile y la reunión de los Tres Ecos Sagrados " +
            "(Fe en la Parroquia, Tradición en el Mercado y Memoria en la Cruz Azul), la Niebla del Olvido se disipa para siempre sobre Torres Mochas.\n\n" +
            "<b>ESTADÍSTICAS FINALES DE RONAN</b>\n" +
            $"•  Ecos Sagrados recuperados: {ecos} / 3\n" +
            $"•  Salud máxima alcanzada: {maxHp:0} HP\n" +
            $"•  Contraataques perfectos (0.20s): {counters}\n" +
            $"•  Criaturas de la Niebla derrotadas: {kills}\n" +
            $"•  Tiempo total de aventura: {mins:00}:{remSecs:00}";

        GUI.Label(new Rect(px + pad, py + UITheme.Px(70f), innerW, panelH - UITheme.Px(160f)), resumen,
            UITheme.Estilo(16, FontStyle.Normal, TextAnchor.UpperLeft, Color.white, true));

        float btnH = UITheme.Px(46f);
        float gap = UITheme.Px(12f);
        float btnW = (innerW - gap * 2f) / 3f;
        float by = py + panelH - btnH - UITheme.Px(18f);
        GUIStyle boton = UITheme.Boton(16);

        if (GUI.Button(new Rect(px + pad, by, btnW, btnH), "Seguir Explorando", boton))
        {
            mostrarPantallaVictoria = false;
            Time.timeScale = 1f;
            LiberarARonan();
        }
        if (GUI.Button(new Rect(px + pad + btnW + gap, by, btnW, btnH), "Nueva Partida", boton))
        {
            Time.timeScale = 1f;
            SaveStateManager.Instance?.ReiniciarProgreso();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        if (GUI.Button(new Rect(px + pad + (btnW + gap) * 2f, by, btnW, btnH), "Menú Principal", boton))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("PantallaInicio");
        }

        GUI.color = previo;
    }
}
