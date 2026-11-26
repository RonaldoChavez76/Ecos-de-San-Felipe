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
    private static Texture2D medallonTexture;
    private static readonly Color[] coloresEcos =
    {
        new Color(1f, 0.76f, 0.28f, 1f),
        new Color(0.45f, 0.92f, 0.58f, 1f),
        new Color(0.25f, 0.83f, 1f, 1f)
    };

    /// <summary>Mientras sea true (Eco ascendiendo / Ronan celebrando) no se muestra ninguna pantalla de victoria.</summary>
    public static bool SecuenciaEcoEnCurso = false;

    [Header("Menú de Pausa In-Game")]
    public bool menuPausaAbierto = false;

    [Header("Pantalla de Victoria de Nivel")]
    public bool mostrarVictoriaNivel = false;
    public int nivelCompletado = 1;
    public string nombreEcoCompletado = "Eco de la Fe";
    private PlayerMovement ronanRef;

    [Header("Pantalla de Victoria Final")]
    public bool mostrarPantallaVictoria = false;
    private bool victoriaYaActivada = false;
    private bool explorandoTrasVictoriaFinal;
    private bool confirmarNuevaPartida;
    private bool mostrarGuiaExploracion;
    private string textoGuiaExploracion;
    private string escenaGuiaExploracion;

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
        if (mostrarGuiaExploracion &&
            !string.Equals(escenaGuiaExploracion, SceneManager.GetActiveScene().name, System.StringComparison.Ordinal))
        {
            mostrarGuiaExploracion = false;
        }

        // 1. Manejo de Pausa con la tecla ESCAPE o botón móvil
        if (MobileInput.GetPauseDown())
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
        if (!explorandoTrasVictoriaFinal &&
            SaveStateManager.Instance != null &&
            SaveStateManager.Instance.TotalEcosRecuperados() >= 3 &&
            SaveStateManager.Instance.cruzAzulConsagrada)
        {
            MostrarVictoriaFinal();
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
    /// La victoria final solo se activa después de reunir los tres Ecos y consagrarlos en la Cruz Azul.
    /// </summary>
    public void MostrarVictoriaDeNivel(int indiceNivel, string nombreEco, PlayerMovement player = null)
    {
        ronanRef = player != null ? player : FindAnyObjectByType<PlayerMovement>();
        menuPausaAbierto = false;

        if (SaveStateManager.Instance != null &&
            SaveStateManager.Instance.TotalEcosRecuperados() >= 3 &&
            SaveStateManager.Instance.cruzAzulConsagrada)
        {
            MostrarVictoriaFinal(ronanRef);
            return;
        }

        nivelCompletado = indiceNivel;
        nombreEcoCompletado = nombreEco;
        mostrarVictoriaNivel = true;
        Time.timeScale = 0f;

        AudioManager.Instance?.PlayVictoryFanfare();
    }

    public void MostrarVictoriaFinal(PlayerMovement player = null)
    {
        ronanRef = player != null ? player : FindAnyObjectByType<PlayerMovement>();
        explorandoTrasVictoriaFinal = false;
        victoriaYaActivada = true;
        mostrarVictoriaNivel = false;
        mostrarPantallaVictoria = true;
        menuPausaAbierto = false;
        ronanRef?.ActivarPoseDeVictoriaConEco("Los Tres Ecos Sagrados", 3, true);
        Time.timeScale = 0f;
        AudioManager.Instance?.PlayVictoryFanfare();
    }

    public void ReabrirVictoriaFinalDesdeCruz(PlayerMovement player)
    {
        menuPausaAbierto = false;
        mostrarVictoriaNivel = false;
        mostrarPantallaVictoria = false;
        SecuenciaEcoEnCurso = false;
        MostrarVictoriaFinal(player);
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
        // Si el modal de Opciones está abierto, NO dibujar nada (el modal tiene su propio OnGUI encima)
        if (OptionsModalUI.Instance != null && OptionsModalUI.Instance.isOptionsOpen)
            return;

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

        if (mostrarGuiaExploracion && !mostrarVictoriaNivel && !mostrarPantallaVictoria &&
            !menuPausaAbierto && (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive))
        {
            DibujarGuiaExploracion();
        }
        if (confirmarNuevaPartida)
        {
            DibujarConfirmacionNuevaPartida();
        }
    }

    private void DibujarGuiaExploracion()
    {
        float width = Mathf.Min(UITheme.Px(760f), Screen.width - UITheme.Px(32f));
        float height = UITheme.Px(54f);
        Rect panel = new Rect((Screen.width - width) * 0.5f, UITheme.Px(18f), width, height);
        GUI.color = new Color(0.06f, 0.12f, 0.17f, 0.94f);
        GUI.DrawTexture(panel, UITheme.Blanca);
        GUI.color = Color.white;
        UITheme.Borde(panel, UITheme.Turquesa, 2f);
        GUI.Label(panel, textoGuiaExploracion,
            UITheme.Estilo(15, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, true));
    }

    private void ActivarGuiaExploracion()
    {
        switch (nivelCompletado)
        {
            case 1:
                textoGuiaExploracion = "Para avanzar: vuelve al exterior por la salida sur y sigue al Oeste, hacia el Portal del Mercado.";
                break;
            case 2:
                textoGuiaExploracion = "Para avanzar: cruza la plaza hacia el Norte, pasa el Gran Portón y entra al Portal del Cerro del Fraile.";
                break;
            default:
                textoGuiaExploracion = "Para concluir la aventura, reúne los tres Ecos e interactúa con la Cruz Azul.";
                break;
        }

        escenaGuiaExploracion = SceneManager.GetActiveScene().name;
        mostrarGuiaExploracion = true;
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

        Rect sa = UITheme.SafeAreaGUI;
        float panelW = Mathf.Clamp(UITheme.Px(460f), 340f, sa.width - UITheme.Px(24f));
        float panelH = Mathf.Clamp(UITheme.Px(480f), 380f, sa.height - UITheme.Px(24f));
        float px = sa.x + (sa.width - panelW) * 0.5f;
        float py = sa.y + (sa.height - panelH) * 0.5f;

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
        string resumeText = MobileInput.IsTouchControlsActive ? "▶ Reanudar Partida" : "▶ Reanudar Partida [ESC]";
        if (GUI.Button(new Rect(bx, y, btnW, btnH), resumeText, boton))
        {
            ReanudarJuego();
        }
        y += btnH + gap;

        // 2. Ajustes y Opciones
        if (GUI.Button(new Rect(bx, y, btnW, btnH), "★ Ajustes y Opciones", boton))
        {
            OptionsModalUI.EnsureInstance().AbrirModal();
        }
        y += btnH + gap;

        // 3. Volver al último checkpoint guardado
        SaveStateManager save = SaveStateManager.Instance;
        bool hayCheckpoint = save != null && save.HayCheckpointGuardado;
        GUI.enabled = hayCheckpoint;
        string textoCheckpoint = hayCheckpoint ? "« Volver al último checkpoint" : "« No hay checkpoint guardado";
        if (GUI.Button(new Rect(bx, y, btnW, btnH), textoCheckpoint, boton))
        {
            menuPausaAbierto = false;
            save.RegresarAlUltimoCheckpoint();
        }
        GUI.enabled = true;
        y += btnH + gap;

        // 4. Volver al Menú Principal
        if (GUI.Button(new Rect(bx, y, btnW, btnH), "« Salir al Menú Principal", boton))
        {
            SaveStateManager.Instance?.GuardarProgreso();
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
                    siguiente = "Al explorar, sigue al <b>Norte</b>, pasa el Gran Portón y cruza el Portal al Cerro del Fraile."
                };
            default:
                return new DatosNivel
                {
                    zona = nivel == 3 ? "MIRADOR DE LA CRUZ AZUL" : "PARROQUIA DE SAN FELIPE APÓSTOL",
                    lore = nivel == 3
                        ? $"Con el <b>{nombreEco}</b> recuperado, los Tres Ecos ya pueden reunirse. Acércate a la Cruz Azul y pulsa <b>[E]</b> para consagrarlos y restaurar la memoria de San Felipe."
                        : $"Con el <b>{nombreEco}</b> en tus manos, el repique de las campanas disipa la Niebla del Olvido sobre el atrio y el altar colonial. La fe de Torres Mochas ha despertado.",
                    siguiente = nivel == 3
                        ? "El último paso te espera ante la Gran Cruz Azul."
                        : "El camino hacia el <b>Mercado Municipal de San Felipe</b> está despejado."
                };
        }
    }

    private void DibujarPantallaVictoriaNivel()
    {
        Color previo = GUI.color;
        GUI.color = new Color(0.04f, 0.02f, 0.08f, 0.86f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), UITheme.Blanca);
        GUI.color = Color.white;

        Rect sa = UITheme.SafeAreaGUI;
        float margin = UITheme.Px(16f);
        float panelW = Mathf.Min(UITheme.Px(820f), sa.width - margin * 2f);
        float panelH = Mathf.Min(UITheme.Px(620f), sa.height - margin * 2f);
        float px = sa.x + (sa.width - panelW) * 0.5f;
        float py = sa.y + (sa.height - panelH) * 0.5f;
        float pad = UITheme.Px(26f);
        float innerW = panelW - pad * 2f;

        Rect rPanel = new Rect(px, py, panelW, panelH);
        GUI.color = new Color(0.10f, 0.08f, 0.14f, 0.96f);
        GUI.DrawTexture(rPanel, UITheme.Blanca);
        GUI.color = Color.white;
        UITheme.Borde(rPanel, UITheme.BordeDorado, 3f);
        UITheme.Borde(new Rect(px + 4f, py + 4f, panelW - 8f, panelH - 8f), new Color(0.35f, 0.85f, 1f, 0.40f), 1f);

        DatosNivel datos = ObtenerDatosNivel(nivelCompletado, nombreEcoCompletado);
        SaveStateManager ssm = SaveStateManager.Instance;
        float vidaTotal = ssm != null ? ssm.ObtenerVidaMaximaTotal() : 125f;
        int ecos = ssm != null ? ssm.TotalEcosRecuperados() : nivelCompletado;
        int counters = ssm != null ? ssm.contraataquesExitosos : 0;
        int kills = ssm != null ? ssm.enemigosDerrotados : 0;
        int oro = ssm != null ? ssm.oro : 0;
        float secs = ssm != null ? ssm.tiempoDeJuego : 0f;
        int mins = Mathf.FloorToInt(secs / 60f);
        int remSecs = Mathf.FloorToInt(secs % 60f);
        string difStr = ssm != null ? ssm.dificultadActual.ToString() : "Normal";

        UITheme.EtiquetaConSombra(new Rect(px, py + UITheme.Px(14f), panelW, UITheme.Px(38f)),
            $"★  ¡NIVEL {nivelCompletado} COMPLETADO!  ★",
            UITheme.Estilo(26, FontStyle.Bold, TextAnchor.MiddleCenter, UITheme.Dorado));

        GUI.Label(new Rect(px + pad, py + UITheme.Px(52f), innerW, UITheme.Px(24f)), datos.zona,
            UITheme.Estilo(16, FontStyle.Bold, TextAnchor.MiddleCenter, UITheme.Turquesa));
        float pulse = 0.85f + Mathf.Sin(Time.unscaledTime * 4f) * 0.15f;
        Color ecoCol = new Color(0.2f * pulse, 0.9f * pulse, 1.0f * pulse, 1f);
        GUI.Label(new Rect(px + pad, py + UITheme.Px(78f), innerW, UITheme.Px(26f)),
            $"★  {nombreEcoCompletado.ToUpper()} DESPERTADO  ({ecos}/3)  ★",
            UITheme.Estilo(18, FontStyle.Bold, TextAnchor.MiddleCenter, ecoCol));

        Rect rLore = new Rect(px + pad, py + UITheme.Px(108f), innerW, UITheme.Px(42f));
        GUI.Label(rLore, datos.lore,
            UITheme.Estilo(13, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white, true));

        DibujarProgresoDeEcos(new Rect(px + pad, py + UITheme.Px(154f), innerW, UITheme.Px(94f)), ecos);

        Rect rCronica = new Rect(px + pad, py + UITheme.Px(256f), innerW, UITheme.Px(34f));
        GUI.color = new Color(0.12f, 0.21f, 0.23f, 0.9f);
        GUI.DrawTexture(rCronica, UITheme.Blanca);
        GUI.color = Color.white;
        UITheme.Borde(rCronica, new Color(0.25f, 0.83f, 1f, 0.65f), 1f);
        GUI.Label(rCronica, "CRÓNICA Y HAZAÑAS DE RONAN",
            UITheme.Estilo(15, FontStyle.Bold, TextAnchor.MiddleCenter, UITheme.Turquesa));

        float statsGap = UITheme.Px(10f);
        float statsW = (innerW - statsGap * 2f) / 3f;
        float statsH = UITheme.Px(60f);
        float statsY = py + UITheme.Px(300f);
        DibujarTarjetaEstadistica(new Rect(px + pad, statsY, statsW, statsH), "ECOS REUNIDOS", $"{ecos} / 3", coloresEcos[Mathf.Clamp(nivelCompletado - 1, 0, 2)]);
        DibujarTarjetaEstadistica(new Rect(px + pad + statsW + statsGap, statsY, statsW, statsH), "SALUD MÁXIMA", $"{vidaTotal:0} HP", new Color(0.45f, 1f, 0.58f));
        DibujarTarjetaEstadistica(new Rect(px + pad + (statsW + statsGap) * 2f, statsY, statsW, statsH), "ORO ACUMULADO", oro.ToString(), UITheme.Dorado);

        statsY += statsH + UITheme.Px(9f);
        DibujarTarjetaEstadistica(new Rect(px + pad, statsY, statsW, statsH), "SOMBRAS VENCIDAS", kills.ToString(), new Color(0.78f, 0.53f, 1f));
        DibujarTarjetaEstadistica(new Rect(px + pad + statsW + statsGap, statsY, statsW, statsH), "CONTRAATAQUES", counters.ToString(), new Color(0.25f, 0.83f, 1f));
        DibujarTarjetaEstadistica(new Rect(px + pad + (statsW + statsGap) * 2f, statsY, statsW, statsH), "DIFICULTAD", difStr, UITheme.Dorado);

        Rect rSiguiente = new Rect(px + pad, py + UITheme.Px(438f), innerW, UITheme.Px(42f));
        GUI.color = new Color(0.14f, 0.20f, 0.16f, 0.88f);
        GUI.DrawTexture(rSiguiente, UITheme.Blanca);
        GUI.color = Color.white;
        UITheme.Borde(rSiguiente, new Color(0.35f, 0.85f, 0.45f, 0.60f), 1f);
        GUI.Label(rSiguiente, datos.siguiente,
            UITheme.Estilo(13, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white, true));

        float btnH = UITheme.Px(46f);
        float gap = UITheme.Px(12f);
        float btnW = (innerW - gap * 2f) / 3f;
        float by = py + panelH - btnH - UITheme.Px(14f);
        GUIStyle boton = UITheme.Boton(15);

        string txtSiguienteNivel = nivelCompletado == 1
            ? "Avanzar al Nivel 2: El Mercado >>"
            : (nivelCompletado == 2 ? "Avanzar al Nivel 3: Cerro del Fraile >>" : "Continuar Aventura");

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
            ActivarGuiaExploracion();
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

        Rect sa = UITheme.SafeAreaGUI;
        float panelW = Mathf.Clamp(UITheme.Px(820f), 500f, sa.width - UITheme.Px(24f));
        float panelH = Mathf.Clamp(UITheme.Px(620f), 460f, sa.height - UITheme.Px(24f));
        float px = sa.x + (sa.width - panelW) * 0.5f;
        float py = sa.y + (sa.height - panelH) * 0.5f;
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

        GUI.Label(new Rect(px + pad, py + UITheme.Px(66f), innerW, UITheme.Px(42f)),
            "La Cruz Azul ha reunido la Fe, la Tradición y la Memoria. La Niebla del Olvido se disipa sobre Torres Mochas.",
            UITheme.Estilo(14, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white, true));

        DibujarProgresoDeEcos(new Rect(px + pad, py + UITheme.Px(112f), innerW, UITheme.Px(94f)), ecos);

        Rect rCronica = new Rect(px + pad, py + UITheme.Px(214f), innerW, UITheme.Px(34f));
        GUI.color = new Color(0.12f, 0.21f, 0.23f, 0.9f);
        GUI.DrawTexture(rCronica, UITheme.Blanca);
        GUI.color = Color.white;
        UITheme.Borde(rCronica, new Color(0.25f, 0.83f, 1f, 0.65f), 1f);
        GUI.Label(rCronica, "CRÓNICA SAGRADA DE RONAN", UITheme.Estilo(15, FontStyle.Bold, TextAnchor.MiddleCenter, UITheme.Turquesa));

        float statsGap = UITheme.Px(10f);
        float statsW = (innerW - statsGap * 2f) / 3f;
        float statsH = UITheme.Px(60f);
        float statsY = py + UITheme.Px(258f);
        DibujarTarjetaEstadistica(new Rect(px + pad, statsY, statsW, statsH), "ECOS REUNIDOS", $"{ecos} / 3", coloresEcos[2]);
        DibujarTarjetaEstadistica(new Rect(px + pad + statsW + statsGap, statsY, statsW, statsH), "SALUD MÁXIMA", $"{maxHp:0} HP", new Color(0.45f, 1f, 0.58f));
        DibujarTarjetaEstadistica(new Rect(px + pad + (statsW + statsGap) * 2f, statsY, statsW, statsH), "CONTRAATAQUES", counters.ToString(), new Color(0.25f, 0.83f, 1f));

        statsY += statsH + UITheme.Px(9f);
        DibujarTarjetaEstadistica(new Rect(px + pad, statsY, statsW, statsH), "SOMBRAS VENCIDAS", kills.ToString(), new Color(0.78f, 0.53f, 1f));
        DibujarTarjetaEstadistica(new Rect(px + pad + statsW + statsGap, statsY, statsW, statsH), "DIFICULTAD", difStr, UITheme.Dorado);
        DibujarTarjetaEstadistica(new Rect(px + pad + (statsW + statsGap) * 2f, statsY, statsW, statsH), "TRAVESÍA", $"{mins:00}:{remSecs:00}", new Color(1f, 0.72f, 0.4f));

        float btnH = UITheme.Px(46f);
        float gap = UITheme.Px(12f);
        float btnW = (innerW - gap * 2f) / 3f;
        float by = py + panelH - btnH - UITheme.Px(18f);
        GUIStyle boton = UITheme.Boton(15);

        if (GUI.Button(new Rect(px + pad, by, btnW, btnH), "Seguir Explorando", boton))
        {
            mostrarPantallaVictoria = false;
            victoriaYaActivada = false;
            explorandoTrasVictoriaFinal = true;
            Time.timeScale = 1f;
            LiberarARonan();
        }
        if (GUI.Button(new Rect(px + pad + btnW + gap, by, btnW, btnH), "Nueva Partida", boton))
        {
            confirmarNuevaPartida = true;
        }
        if (GUI.Button(new Rect(px + pad + (btnW + gap) * 2f, by, btnW, btnH), "Menú Principal", boton))
        {
            SaveStateManager.Instance?.GuardarProgreso();
            Time.timeScale = 1f;
            SceneManager.LoadScene("PantallaInicio");
        }

        GUI.color = previo;
    }

    private void DibujarConfirmacionNuevaPartida()
    {
        Color colorPrevio = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.80f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), UITheme.Blanca);
        GUI.color = Color.white;

        float width = Mathf.Min(UITheme.Px(560f), Screen.width - UITheme.Px(32f));
        float height = UITheme.Px(220f);
        Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        UITheme.Panel(panel, UITheme.BordeDorado, 3f);
        GUI.Label(new Rect(panel.x + 16f, panel.y + UITheme.Px(24f), panel.width - 32f, UITheme.Px(42f)),
            "¿Reiniciar la aventura?",
            UITheme.Estilo(22, FontStyle.Bold, TextAnchor.MiddleCenter, UITheme.Dorado));
        GUI.Label(new Rect(panel.x + 26f, panel.y + UITheme.Px(72f), panel.width - 52f, UITheme.Px(62f)),
            "Se borrará el progreso guardado. La dificultad y los ajustes de sonido y brillo se conservarán.",
            UITheme.Estilo(14, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white, true));

        float buttonWidth = (panel.width - UITheme.Px(54f)) * 0.5f;
        float buttonHeight = UITheme.Px(44f);
        float buttonY = panel.y + panel.height - buttonHeight - UITheme.Px(20f);
        if (GUI.Button(new Rect(panel.x + UITheme.Px(18f), buttonY, buttonWidth, buttonHeight),
            "Reiniciar progreso", UITheme.Boton(15)))
        {
            confirmarNuevaPartida = false;
            SaveStateManager.Instance?.ReiniciarProgreso();
            Time.timeScale = 1f;
            SceneManager.LoadScene("SampleScene");
        }
        if (GUI.Button(new Rect(panel.x + UITheme.Px(36f) + buttonWidth, buttonY, buttonWidth, buttonHeight),
            "Cancelar", UITheme.Boton(15)))
        {
            confirmarNuevaPartida = false;
        }
        GUI.color = colorPrevio;
    }

    private static void DibujarProgresoDeEcos(Rect rect, int totalRecuperados)
    {
        EnsureMedallonTexture();
        Color panelColor = new Color(0.08f, 0.12f, 0.18f, 0.86f);
        GUI.color = panelColor;
        GUI.DrawTexture(rect, UITheme.Blanca);
        GUI.color = Color.white;
        UITheme.Borde(rect, new Color(0.72f, 0.62f, 0.35f, 0.55f), 1f);

        float medallon = Mathf.Min(UITheme.Px(40f), rect.height - UITheme.Px(8f));
        float espacio = rect.width / 3f;
        string[] nombres = { "FE", "TRADICIÓN", "MEMORIA" };
        SaveStateManager save = SaveStateManager.Instance;
        bool[] obtenidos =
        {
            save != null ? save.ecoFeRecuperado : totalRecuperados >= 1,
            save != null ? save.ecoTradicionRecuperado : totalRecuperados >= 2,
            save != null ? save.ecoMemoriaRecuperado : totalRecuperados >= 3
        };

        for (int i = 0; i < 3; i++)
        {
            float cx = rect.x + espacio * (i + 0.5f);
            Rect badge = new Rect(cx - medallon * 0.5f, rect.y + UITheme.Px(3f), medallon, medallon);
            DibujarMedallon(badge, coloresEcos[i], (i + 1).ToString(), obtenidos[i]);
            GUI.Label(
                new Rect(cx - espacio * 0.5f, badge.yMax + UITheme.Px(1f), espacio, UITheme.Px(16f)),
                nombres[i],
                UITheme.Estilo(9, FontStyle.Bold, TextAnchor.MiddleCenter, obtenidos[i] ? coloresEcos[i] : UITheme.TextoSecundario));
        }
    }

    private static void DibujarMedallon(Rect rect, Color color, string simbolo, bool activo)
    {
        EnsureMedallonTexture();
        Color baseColor = activo ? color : new Color(0.30f, 0.34f, 0.40f, 0.85f);
        GUI.color = new Color(baseColor.r, baseColor.g, baseColor.b, activo ? 0.95f : 0.6f);
        GUI.DrawTexture(rect, medallonTexture);
        GUI.color = new Color(0.07f, 0.09f, 0.15f, 0.95f);
        float inset = rect.width * 0.16f;
        GUI.DrawTexture(new Rect(rect.x + inset, rect.y + inset, rect.width - inset * 2f, rect.height - inset * 2f), medallonTexture);
        GUI.color = Color.white;
        GUI.Label(rect, simbolo, UITheme.Estilo(17, FontStyle.Bold, TextAnchor.MiddleCenter, activo ? baseColor : UITheme.TextoSecundario));
    }

    private static void DibujarTarjetaEstadistica(Rect rect, string titulo, string valor, Color color)
    {
        GUI.color = new Color(0.08f, 0.11f, 0.16f, 0.92f);
        GUI.DrawTexture(rect, UITheme.Blanca);
        GUI.color = Color.white;
        UITheme.Borde(rect, new Color(color.r, color.g, color.b, 0.65f), 1f);
        GUI.Label(new Rect(rect.x + UITheme.Px(4f), rect.y + UITheme.Px(5f), rect.width - UITheme.Px(8f), UITheme.Px(19f)),
            titulo, UITheme.Estilo(9, FontStyle.Bold, TextAnchor.MiddleCenter, UITheme.TextoSecundario));
        GUI.Label(new Rect(rect.x + UITheme.Px(4f), rect.y + UITheme.Px(24f), rect.width - UITheme.Px(8f), UITheme.Px(29f)),
            valor, UITheme.Estilo(17, FontStyle.Bold, TextAnchor.MiddleCenter, color));
    }

    private static void EnsureMedallonTexture()
    {
        if (medallonTexture != null) return;
        const int size = 48;
        medallonTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "UI_Medallon_Eco",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(23.5f, 23.5f));
                medallonTexture.SetPixel(x, y, distance <= 23f ? Color.white : Color.clear);
            }
        }
        medallonTexture.Apply();
    }
}
