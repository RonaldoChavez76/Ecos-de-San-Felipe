using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestiona la interfaz de Pausa [ESC], Opciones in-game y Pantallas de Victoria:
///  • Menú de Pausa In-Game con acceso directo al modal de Ajustes y Opciones audiovisuales.
///  • Nivel Completado (pergamino sacro con doble marco dorado, emblema del Eco y fanfarria).
///  • Victoria Final (tras derrotar a La Sombra Guardiana y reunir los 3 Ecos Sagrados).
/// </summary>
public class PauseAndVictoryUI : MonoBehaviour
{
    public static PauseAndVictoryUI Instance { get; private set; }

    /// <summary>Mientras sea true (Eco ascendiendo / Ronan celebrando) no se muestra ninguna pantalla de victoria.</summary>
    public static bool SecuenciaEcoEnCurso = false;

    [Header("Menú de Pausa In-Game")]
    public bool menuPausaAbierto = false;
    private bool estabaOpcionesAbierto = false;

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
        // 1. Manejo de Pausa con la tecla ESCAPE
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Si el modal de opciones está abierto, OptionsModalUI maneja su propio cierre con ESC
            if (OptionsModalUI.Instance != null && OptionsModalUI.Instance.isOptionsOpen)
            {
                return;
            }

            if (!mostrarVictoriaNivel && !mostrarPantallaVictoria && !SecuenciaEcoEnCurso)
            {
                if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
                {
                    // No pausar sobre diálogo activo
                }
                else
                {
                    AlternarPausa();
                }
            }
        }

        if (victoriaYaActivada || mostrarVictoriaNivel || SecuenciaEcoEnCurso) return;

        // 2. Detección de Victoria Final
        if (SaveStateManager.Instance != null &&
            (SaveStateManager.Instance.TotalEcosRecuperados() >= 3 || SaveStateManager.Instance.ecoMemoriaRecuperado))
        {
            victoriaYaActivada = true;
            mostrarPantallaVictoria = true;
            menuPausaAbierto = false;
            ronanRef = FindAnyObjectByType<PlayerMovement>();
            Time.timeScale = 0f;
            AudioManager.Instance?.PlayVictoryFanfare();
        }
    }

    public void AlternarPausa()
    {
        menuPausaAbierto = !menuPausaAbierto;
        Time.timeScale = menuPausaAbierto ? 0f : 1f;

        if (menuPausaAbierto)
        {
            AudioManager.Instance?.PlayButtonHover();
        }
    }

    public void ReanudarJuego()
    {
        menuPausaAbierto = false;
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Muestra la pantalla de nivel completado con detalles del Eco recuperado y recompensas.
    /// Si se recupera el 3er Eco (Eco de la Memoria) o ya se tienen los 3 Ecos, activa la pantalla de Victoria Final.
    /// </summary>
    public void MostrarVictoriaDeNivel(int indiceNivel, string nombreEco, PlayerMovement player = null)
    {
        ronanRef = player != null ? player : FindAnyObjectByType<PlayerMovement>();
        menuPausaAbierto = false;

        if (indiceNivel >= 3 ||
            (SaveStateManager.Instance != null && (SaveStateManager.Instance.TotalEcosRecuperados() >= 3 || SaveStateManager.Instance.ecoMemoriaRecuperado)))
        {
            victoriaYaActivada = true;
            mostrarPantallaVictoria = true;
            Time.timeScale = 0f;
            AudioManager.Instance?.PlayVictoryFanfare();
            return;
        }

        nivelCompletado = indiceNivel;
        nombreEcoCompletado = nombreEco;
        mostrarVictoriaNivel = true;
        Time.timeScale = 0f;

        AudioManager.Instance?.PlayVictoryFanfare();
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
        if (menuPausaAbierto)
        {
            DibujarMenuPausa();
        }
        else if (mostrarVictoriaNivel)
        {
            DibujarPantallaVictoriaNivel();
        }
        else if (mostrarPantallaVictoria)
        {
            DibujarPantallaVictoriaFinal();
        }
    }

    // ------------------------------------------------------------------------------------------
    // MENÚ DE PAUSA IN-GAME
    // ------------------------------------------------------------------------------------------
    private void DibujarMenuPausa()
    {
        Color previo = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.78f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), UITheme.Blanca);
        GUI.color = Color.white;

        float panelW = Mathf.Clamp(UITheme.Px(460f), 340f, Screen.width - 40f);
        float panelH = Mathf.Clamp(UITheme.Px(480f), 400f, Screen.height - 40f);
        float px = (Screen.width - panelW) * 0.5f;
        float py = (Screen.height - panelH) * 0.5f;

        // Marco colonial
        UITheme.Panel(new Rect(px, py, panelW, panelH), UITheme.BordeDorado, 3f);

        float y = py + UITheme.Px(24f);

        // Título de Pausa
        UITheme.EtiquetaConSombra(
            new Rect(px, y, panelW, UITheme.Px(36f)),
            "★  PARTIDA EN PAUSA  ★",
            UITheme.Estilo(22, FontStyle.Bold, TextAnchor.MiddleCenter, UITheme.Dorado));
        y += UITheme.Px(42f);

        // Subtítulo con la zona actual
        string zonaActual = "Torres Mochas — San Felipe";
        if (LevelManager.Instance != null)
        {
            zonaActual = LevelManager.Instance.currentZone.ToString();
        }
        GUI.Label(
            new Rect(px, y, panelW, UITheme.Px(22f)),
            zonaActual,
            UITheme.Estilo(14, FontStyle.Italic, TextAnchor.MiddleCenter, UITheme.Turquesa));
        y += UITheme.Px(34f);

        // Dificultad actual en texto discreto
        string difTxt = "Dificultad: Normal";
        if (SaveStateManager.Instance != null)
        {
            difTxt = $"Dificultad: <b>{SaveStateManager.Instance.dificultadActual}</b>";
        }
        GUI.Label(
            new Rect(px, y, panelW, UITheme.Px(22f)),
            difTxt,
            UITheme.Estilo(13, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white));
        y += UITheme.Px(32f);

        // Botones del Menú de Pausa
        float btnW = panelW - UITheme.Px(56f);
        float btnH = UITheme.Px(44f);
        float gap = UITheme.Px(14f);
        float bx = px + UITheme.Px(28f);
        GUIStyle boton = UITheme.Boton(15);

        // 1. Reanudar
        if (GUI.Button(new Rect(bx, y, btnW, btnH), "▶ Reanudar Partida [ESC]", boton))
        {
            ReanudarJuego();
        }
        y += btnH + gap;

        // 2. Ajustes y Opciones
        if (GUI.Button(new Rect(bx, y, btnW, btnH), "⚙ Ajustes y Opciones", boton))
        {
            OptionsModalUI.EnsureInstance().AbrirModal();
        }
        y += btnH + gap;

        // 3. Reiniciar Nivel / Checkpoint
        if (GUI.Button(new Rect(bx, y, btnW, btnH), "↺ Reiniciar Desde Checkpoint", boton))
        {
            Time.timeScale = 1f;
            menuPausaAbierto = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        y += btnH + gap;

        // 4. Volver al Menú Principal
        if (GUI.Button(new Rect(bx, y, btnW, btnH), "⌂ Salir al Menú Principal", boton))
        {
            Time.timeScale = 1f;
            menuPausaAbierto = false;
            SceneManager.LoadScene("PantallaInicio");
        }

        GUI.color = previo;
    }

    // ------------------------------------------------------------------------------------------
    // PANTALLA DE FIN DE NIVEL REDISEÑADA (PERGAMINO SACRO Y CELEBRACIÓN DE ECOS)
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
                    zona = "MERCADO MUNICIPAL DE SAN FELIPE",
                    lore = $"Con el <b>{nombreEco}</b> rescatado del Cofre Sagrado del Gremio, los puestos alfareros y plateros resucitan su historia. Las campanas anuncian que el ascenso sagrado está abierto.",
                    siguiente = "El sendero hacia el <b>Cerro del Fraile y la Cruz Azul</b> está despejado."
                };
            default:
                return new DatosNivel
                {
                    zona = "PARROQUIA DE SAN FELIPE APÓSTOL",
                    lore = $"Con el <b>{nombreEco}</b> en tus manos, el repique de las campanas disipa la Niebla del Olvido sobre el atrio y el altar colonial. La fe de Torres Mochas ha despertado.",
                    siguiente = "El camino hacia el <b>Mercado Municipal de San Felipe</b> está despejado."
                };
        }
    }

    private void DibujarPantallaVictoriaNivel()
    {
        Color previo = GUI.color;
        // Fondo oscurecido con halo místico
        GUI.color = new Color(0.04f, 0.02f, 0.08f, 0.86f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), UITheme.Blanca);
        GUI.color = Color.white;

        float panelW = Mathf.Clamp(UITheme.Px(820f), 520f, Screen.width - 40f);
        float panelH = Mathf.Clamp(UITheme.Px(580f), 480f, Screen.height - 40f);
        float px = (Screen.width - panelW) * 0.5f;
        float py = (Screen.height - panelH) * 0.5f;
        float pad = UITheme.Px(26f);
        float innerW = panelW - pad * 2f;

        // Marco de pergamino sacro con doble borde dorado
        Rect rPanel = new Rect(px, py, panelW, panelH);
        GUI.color = new Color(0.12f, 0.09f, 0.06f, 0.96f);
        GUI.DrawTexture(rPanel, UITheme.Blanca);
        GUI.color = Color.white;
        UITheme.Borde(rPanel, UITheme.BordeDorado, 3f);
        UITheme.Borde(new Rect(px + 4f, py + 4f, panelW - 8f, panelH - 8f), new Color(1f, 0.85f, 0.35f, 0.40f), 1f);

        DatosNivel datos = ObtenerDatosNivel(nivelCompletado, nombreEcoCompletado);
        SaveStateManager ssm = SaveStateManager.Instance;
        float vidaTotal = ssm != null ? ssm.ObtenerVidaMaximaTotal() : 125f;
        int ecos = ssm != null ? ssm.TotalEcosRecuperados() : nivelCompletado;
        int counters = ssm != null ? ssm.contraataquesExitosos : 0;
        int kills = ssm != null ? ssm.enemigosDerrotados : 0;
        int oro = ssm != null ? ssm.oro : 0;

        float y = py + UITheme.Px(20f);

        // Título de Nivel
        UITheme.EtiquetaConSombra(new Rect(px, y, panelW, UITheme.Px(38f)),
            $"★  ¡NIVEL {nivelCompletado} COMPLETADO!  ★",
            UITheme.Estilo(26, FontStyle.Bold, TextAnchor.MiddleCenter, UITheme.Dorado));
        y += UITheme.Px(40f);

        GUI.Label(new Rect(px, y, panelW, UITheme.Px(24f)), datos.zona,
            UITheme.Estilo(16, FontStyle.Bold, TextAnchor.MiddleCenter, UITheme.Turquesa));
        y += UITheme.Px(30f);

        // Emblema palpitante del Eco
        float pulse = 0.85f + Mathf.Sin(Time.unscaledTime * 4f) * 0.15f;
        Color ecoCol = new Color(0.2f * pulse, 0.9f * pulse, 1.0f * pulse, 1f);
        GUI.Label(new Rect(px, y, panelW, UITheme.Px(28f)),
            $"✦  {nombreEcoCompletado.ToUpper()} DESPERTADO  ({ecos}/3)  ✦",
            UITheme.Estilo(18, FontStyle.Bold, TextAnchor.MiddleCenter, ecoCol));
        y += UITheme.Px(38f);

        // Caja de Lore / Narrativa
        Rect rLore = new Rect(px + pad, y, innerW, UITheme.Px(92f));
        GUI.color = new Color(0.18f, 0.14f, 0.10f, 0.85f);
        GUI.DrawTexture(rLore, UITheme.Blanca);
        GUI.color = Color.white;
        UITheme.Borde(rLore, new Color(0.85f, 0.70f, 0.35f, 0.60f), 1f);
        GUI.Label(new Rect(rLore.x + UITheme.Px(14f), rLore.y + UITheme.Px(8f), rLore.width - UITheme.Px(28f), rLore.height - UITheme.Px(16f)),
            datos.lore, UITheme.Estilo(15, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white, true));
        y += rLore.height + UITheme.Px(14f);

        // Caja de Estadísticas y Recompensas
        Rect rRew = new Rect(px + pad, y, innerW, UITheme.Px(175f));
        GUI.color = new Color(0.14f, 0.20f, 0.16f, 0.88f);
        GUI.DrawTexture(rRew, UITheme.Blanca);
        GUI.color = Color.white;
        UITheme.Borde(rRew, new Color(0.35f, 0.85f, 0.45f, 0.60f), 1f);

        GUI.Label(new Rect(rRew.x + UITheme.Px(14f), rRew.y + UITheme.Px(8f), rRew.width - UITheme.Px(28f), UITheme.Px(24f)),
            "RECOMPENSAS Y HAZAÑAS DE RONAN", UITheme.Estilo(15, FontStyle.Bold, TextAnchor.MiddleLeft, UITheme.Verde));

        string difStr = ssm != null ? ssm.dificultadActual.ToString() : "Normal";
        string rewText =
            $"•  <b>+25 Salud Máxima Permanente:</b> Ronan ahora posee {vidaTotal:0} HP\n" +
            "•  <b>Vitalidad Restaurada:</b> Salud restaurada por completo\n" +
            $"•  <b>Hazañas en combate:</b> {kills} Sombras derrotadas  |  {counters} Contraataques perfectos\n" +
            $"•  <b>Riqueza acumulada:</b> {oro} Monedas de Oro  |  Dificultad: {difStr}\n" +
            "•  " + datos.siguiente;
        GUI.Label(new Rect(rRew.x + UITheme.Px(14f), rRew.y + UITheme.Px(34f), rRew.width - UITheme.Px(28f), rRew.height - UITheme.Px(40f)),
            rewText, UITheme.Estilo(14, FontStyle.Normal, TextAnchor.UpperLeft, Color.white, true));

        // Botones de Avance
        float btnH = UITheme.Px(46f);
        float gap = UITheme.Px(12f);
        float btnW = (innerW - gap * 2f) / 3f;
        float by = py + panelH - btnH - UITheme.Px(18f);
        GUIStyle boton = UITheme.Boton(15);

        string txtSiguienteNivel = nivelCompletado == 1
            ? "Avanzar al Nivel 2: El Mercado ➔"
            : (nivelCompletado == 2 ? "Avanzar al Nivel 3: Cerro del Fraile ➔" : "Continuar Aventura");

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

    // ------------------------------------------------------------------------------------------
    // PANTALLA DE VICTORIA FINAL (LOS TRES ECOS REUNIDOS)
    // ------------------------------------------------------------------------------------------
    private void DibujarPantallaVictoriaFinal()
    {
        Color previo = GUI.color;
        GUI.color = new Color(0.04f, 0.02f, 0.08f, 0.90f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), UITheme.Blanca);
        GUI.color = Color.white;

        float panelW = Mathf.Clamp(UITheme.Px(820f), 520f, Screen.width - 40f);
        float panelH = Mathf.Clamp(UITheme.Px(580f), 480f, Screen.height - 40f);
        float px = (Screen.width - panelW) * 0.5f;
        float py = (Screen.height - panelH) * 0.5f;
        float pad = UITheme.Px(26f);
        float innerW = panelW - pad * 2f;

        Rect rPanel = new Rect(px, py, panelW, panelH);
        GUI.color = new Color(0.10f, 0.08f, 0.14f, 0.96f);
        GUI.DrawTexture(rPanel, UITheme.Blanca);
        GUI.color = Color.white;
        UITheme.Borde(rPanel, UITheme.BordeDorado, 3f);
        UITheme.Borde(new Rect(px + 4f, py + 4f, panelW - 8f, panelH - 8f), new Color(0.35f, 0.85f, 1.0f, 0.40f), 1f);

        UITheme.EtiquetaConSombra(new Rect(px, py + UITheme.Px(20f), panelW, UITheme.Px(38f)),
            "★  ¡SAN FELIPE HA RECUPERADO SU MEMORIA!  ★",
            UITheme.Estilo(25, FontStyle.Bold, TextAnchor.MiddleCenter, UITheme.Dorado));

        SaveStateManager ssm = SaveStateManager.Instance;
        int ecos = ssm != null ? ssm.TotalEcosRecuperados() : 3;
        float maxHp = ssm != null ? ssm.ObtenerVidaMaximaTotal() : 175f;
        int counters = ssm != null ? ssm.contraataquesExitosos : 0;
        int kills = ssm != null ? ssm.enemigosDerrotados : 0;
        float secs = ssm != null ? ssm.tiempoDeJuego : 0f;
        int mins = Mathf.FloorToInt(secs / 60f);
        int remSecs = Mathf.FloorToInt(secs % 60f);
        string difStr = ssm != null ? ssm.dificultadActual.ToString() : "Normal";

        string resumen =
            "Con la caída de La Sombra Guardiana en la cumbre del Cerro del Fraile y la reunión de los Tres Ecos Sagrados " +
            "(Fe en la Parroquia, Tradición en el Mercado y Memoria en la Cruz Azul), la Niebla del Olvido se disipa para siempre sobre Torres Mochas.\n\n" +
            "<b>CRÓNICA SAGRADA DE RONAN:</b>\n" +
            $"•  Ecos Sagrados recuperados: <b>{ecos} / 3</b>\n" +
            $"•  Salud máxima definitiva: <b>{maxHp:0} HP</b>\n" +
            $"•  Contraataques perfectos ejecutados: <b>{counters}</b>\n" +
            $"•  Criaturas de la Niebla vencidas: <b>{kills}</b>\n" +
            $"•  Dificultad superada: <b>{difStr}</b>\n" +
            $"•  Tiempo total de travesía: <b>{mins:00}:{remSecs:00}</b>";

        GUI.Label(new Rect(px + pad, py + UITheme.Px(72f), innerW, panelH - UITheme.Px(160f)), resumen,
            UITheme.Estilo(15, FontStyle.Normal, TextAnchor.UpperLeft, Color.white, true));

        float btnH = UITheme.Px(46f);
        float gap = UITheme.Px(12f);
        float btnW = (innerW - gap * 2f) / 3f;
        float by = py + panelH - btnH - UITheme.Px(18f);
        GUIStyle boton = UITheme.Boton(15);

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
