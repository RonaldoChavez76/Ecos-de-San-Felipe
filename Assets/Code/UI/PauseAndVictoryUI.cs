using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controlador de la Pantalla de Victoria de Nivel (Nivel 1: Parroquia) y de Victoria Final de «Ecos de San Felipe»
/// cuando Ronan recupera los Ecos Sagrados (Fe, Tradición y Memoria) y disipa la Niebla del Olvido,
/// mostrando estadísticas completas de juego, contraataques (0.20s), recompensas y enemigos derrotados.
/// </summary>
public class PauseAndVictoryUI : MonoBehaviour
{
    public static PauseAndVictoryUI Instance { get; private set; }

    [Header("Pantalla de Victoria de Nivel")]
    public bool mostrarVictoriaNivel = false;
    public int nivelCompletado = 1;
    public string nombreEcoCompletado = "Eco de la Fe";
    private PlayerMovement ronanRef;

    [Header("Pantalla de Victoria Final")]
    public bool mostrarPantallaVictoria = false;
    private bool victoriaYaActivada = false;

    private Texture2D darkOverlayTex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        darkOverlayTex = new Texture2D(1, 1);
        darkOverlayTex.SetPixel(0, 0, Color.white);
        darkOverlayTex.Apply();
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
        if (!victoriaYaActivada && !mostrarVictoriaNivel && SaveStateManager.Instance != null)
        {
            if (SaveStateManager.Instance.TotalEcosRecuperados() >= 3)
            {
                victoriaYaActivada = true;
                mostrarPantallaVictoria = true;
                Time.timeScale = 0f;
                AudioManager.Instance?.PlayEchoSound();
            }
        }
    }

    /// <summary>
    /// Muestra la pantalla de nivel completado con detalles del Eco recuperado y recompensas.
    /// </summary>
    public void MostrarVictoriaDeNivel(int indiceNivel, string nombreEco, PlayerMovement player = null)
    {
        if (SaveStateManager.Instance != null && SaveStateManager.Instance.TotalEcosRecuperados() >= 3)
        {
            // Si ya se tienen los 3 ecos, la pantalla de victoria final se encargará
            return;
        }

        nivelCompletado = indiceNivel;
        nombreEcoCompletado = nombreEco;
        ronanRef = player != null ? player : FindAnyObjectByType<PlayerMovement>();
        mostrarVictoriaNivel = true;
        Time.timeScale = 0f; // Pausa el juego mientras se celebra el triunfo del nivel

        AudioManager.Instance?.PlayEchoSound();
        AudioManager.Instance?.PlayBellSound();
    }

    /// <summary>
    /// Cierra la pantalla de victoria de nivel, devuelve el flujo de tiempo y permite continuar explorando.
    /// </summary>
    public void CerrarVictoriaDeNivel()
    {
        mostrarVictoriaNivel = false;
        Time.timeScale = 1f;

        if (ronanRef != null)
        {
            ronanRef.DesactivarPoseDeVictoria();
        }
    }

    private void OnGUI()
    {
        if (mostrarVictoriaNivel)
        {
            DibujarPantallaVictoriaNivel();
            return;
        }

        if (mostrarPantallaVictoria)
        {
            DibujarPantallaVictoriaFinal();
            return;
        }
    }

    private void DibujarPantallaVictoriaNivel()
    {
        // 1. Telón semitransparente oscuro
        GUI.color = new Color(0.03f, 0.05f, 0.10f, 0.94f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkOverlayTex);

        float panelW = 660f;
        float panelH = 460f;
        float px = (Screen.width - panelW) * 0.5f;
        float py = (Screen.height - panelH) * 0.5f;

        // 2. Fondo del panel principal
        GUI.color = new Color(0.08f, 0.10f, 0.16f, 0.98f);
        GUI.DrawTexture(new Rect(px, py, panelW, panelH), darkOverlayTex);

        // 3. Doble marco dorado colonial
        GUI.color = new Color(0.85f, 0.68f, 0.28f, 0.95f);
        GUI.DrawTexture(new Rect(px, py, panelW, 3f), darkOverlayTex);
        GUI.DrawTexture(new Rect(px, py + panelH - 3f, panelW, 3f), darkOverlayTex);
        GUI.DrawTexture(new Rect(px, py, 3f, panelH), darkOverlayTex);
        GUI.DrawTexture(new Rect(px + panelW - 3f, py, 3f, panelH), darkOverlayTex);

        GUI.color = new Color(1f, 0.85f, 0.40f, 0.55f);
        GUI.DrawTexture(new Rect(px + 5f, py + 5f, panelW - 10f, 1f), darkOverlayTex);
        GUI.DrawTexture(new Rect(px + 5f, py + panelH - 6f, panelW - 10f, 1f), darkOverlayTex);
        GUI.DrawTexture(new Rect(px + 5f, py + 5f, 1f, panelH - 10f), darkOverlayTex);
        GUI.DrawTexture(new Rect(px + panelW - 6f, py + 5f, 1f, panelH - 10f), darkOverlayTex);

        GUI.color = Color.white;

        // 4. Encabezados
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = new Color(1f, 0.86f, 0.35f);
        GUI.Label(new Rect(px, py + 14f, panelW, 26f), $"★ ¡NIVEL {nivelCompletado} COMPLETADO! ★", titleStyle);

        GUIStyle subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        subtitleStyle.normal.textColor = new Color(0.38f, 0.90f, 0.92f);
        GUI.Label(new Rect(px, py + 40f, panelW, 20f), "PARROQUIA DE SAN FELIPE APÓSTOL — ATRIO Y TEMPLO", subtitleStyle);

        GUIStyle badgeStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        badgeStyle.normal.textColor = new Color(1f, 0.92f, 0.60f);
        GUI.Label(new Rect(px, py + 62f, panelW, 18f), $"✦ ¡HAS RECUPERADO EL {nombreEcoCompletado.ToUpper()}! (1 / 3) ✦", badgeStyle);

        // 5. Caja de Narrativa Colonial / Lore
        float loreX = px + 22f;
        float loreY = py + 88f;
        float loreW = panelW - 44f;
        float loreH = 92f;

        GUI.color = new Color(0.12f, 0.15f, 0.22f, 0.85f);
        GUI.DrawTexture(new Rect(loreX, loreY, loreW, loreH), darkOverlayTex);
        GUI.color = new Color(0.35f, 0.45f, 0.65f, 0.65f);
        GUI.DrawTexture(new Rect(loreX, loreY, loreW, 1f), darkOverlayTex);
        GUI.DrawTexture(new Rect(loreX, loreY + loreH - 1f, loreW, 1f), darkOverlayTex);

        GUI.color = Color.white;
        GUIStyle loreStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            richText = true
        };
        loreStyle.normal.textColor = new Color(0.92f, 0.94f, 0.98f);

        string loreText =
            "Con el <b>Eco de la Fe</b> en tus manos, el sagrado repique de las campanas y la devoción de los guardianes disipan la densa Niebla del Olvido sobre el atrio y el interior del templo.\n\n" +
            "Los recuerdos y cantos de los habitantes de Torres Mochas han sido preservados para siempre.";
        GUI.Label(new Rect(loreX + 10f, loreY + 8f, loreW - 20f, loreH - 16f), loreText, loreStyle);

        // 6. Tarjeta de Recompensas
        float rewX = px + 22f;
        float rewY = py + 192f;
        float rewW = panelW - 44f;
        float rewH = 195f;

        GUI.color = new Color(0.08f, 0.18f, 0.14f, 0.88f);
        GUI.DrawTexture(new Rect(rewX, rewY, rewW, rewH), darkOverlayTex);
        GUI.color = new Color(0.35f, 0.85f, 0.55f, 0.75f);
        GUI.DrawTexture(new Rect(rewX, rewY, rewW, 1f), darkOverlayTex);
        GUI.DrawTexture(new Rect(rewX, rewY + rewH - 1f, rewW, 1f), darkOverlayTex);

        GUI.color = Color.white;
        GUIStyle rewHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        rewHeaderStyle.normal.textColor = new Color(0.55f, 1f, 0.65f);
        GUI.Label(new Rect(rewX + 16f, rewY + 10f, rewW - 32f, 20f), "✦ RECOMPENSAS Y MEJORAS OBTENIDAS ✦", rewHeaderStyle);

        GUIStyle rewBodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true,
            richText = true
        };
        rewBodyStyle.normal.textColor = new Color(0.92f, 0.96f, 0.92f);

        SaveStateManager ssm = SaveStateManager.Instance;
        float vidaTotal = ssm != null ? ssm.ObtenerVidaMaximaTotal() : 125f;
        int counters = ssm != null ? ssm.contraataquesExitosos : 0;
        int kills = ssm != null ? ssm.enemigosDerrotados : 0;

        string rewText =
            $"• <b>+25 Salud Máxima Permanente:</b> Vitalidad ampliada a <b>{vidaTotal:0} HP</b>.\n" +
            "• <b>Bendición del Altar:</b> Restauración completa de vida y disipación de maldiciones.\n" +
            $"• <b>Eco de la Fe Asegurado:</b> 1 de 3 Ecos Sagrados en el Brazalete de Ronan.\n" +
            $"• <b>Estadísticas de la Zona:</b> {kills} Sombras vencidas | {counters} Contraataques de precisión.\n" +
            "• <b>Siguiente Destino Habilitado:</b> El camino hacia el <b>Mercado de San Felipe</b> está despejado.";

        GUI.Label(new Rect(rewX + 16f, rewY + 34f, rewW - 32f, rewH - 42f), rewText, rewBodyStyle);

        // 7. Botones de Acción
        float btnW = 195f;
        float btnH = 40f;
        float by = py + panelH - 54f;

        if (GUI.Button(new Rect(px + 22f, by, btnW, btnH), "Continuar Aventura"))
        {
            CerrarVictoriaDeNivel();
        }

        if (GUI.Button(new Rect(px + 232f, by, btnW, btnH), "Reiniciar Nivel"))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (GUI.Button(new Rect(px + 443f, by, btnW, btnH), "Menú Principal"))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("PantallaInicio");
        }
    }

    private void DibujarPantallaVictoriaFinal()
    {
        GUI.color = new Color(0.04f, 0.06f, 0.12f, 0.92f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkOverlayTex);
        GUI.color = Color.white;

        float panelW = 620f;
        float panelH = 420f;
        float px = (Screen.width - panelW) * 0.5f;
        float py = (Screen.height - panelH) * 0.5f;

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 15,
            alignment = TextAnchor.UpperCenter,
            padding = new RectOffset(24, 24, 20, 20)
        };
        boxStyle.normal.textColor = new Color(1f, 0.92f, 0.50f);

        GUI.Box(new Rect(px, py, panelW, panelH), "★ ¡SAN FELIPE HA RECUPERADO SU MEMORIA! ★", boxStyle);

        GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            alignment = TextAnchor.UpperCenter,
            wordWrap = true
        };
        bodyStyle.normal.textColor = new Color(0.92f, 0.96f, 1.0f);

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
            $"<b>ESTADÍSTICAS FINALES DE RONAN:</b>\n" +
            $"• Ecos Sagrados Recuperados: {ecos} / 3\n" +
            $"• Salud Máxima Alcanzada: {maxHp} HP\n" +
            $"• Contraataques Perfectos (0.20s): {counters}\n" +
            $"• Criaturas de la Niebla Derrotadas: {kills}\n" +
            $"• Tiempo Total de Aventura: {mins:00}:{remSecs:00}";

        GUI.Label(new Rect(px + 32f, py + 55f, panelW - 64f, 240f), resumen, bodyStyle);

        float btnW = 175f;
        float btnH = 40f;
        float by = py + panelH - 68f;

        if (GUI.Button(new Rect(px + 28f, by, btnW, btnH), "Seguir Explorando"))
        {
            mostrarPantallaVictoria = false;
            Time.timeScale = 1f;
        }

        if (GUI.Button(new Rect(px + 222f, by, btnW, btnH), "Nueva Partida"))
        {
            Time.timeScale = 1f;
            SaveStateManager.Instance?.ReiniciarProgreso();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (GUI.Button(new Rect(px + 416f, by, btnW, btnH), "Menú Principal"))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("PantallaInicio");
        }
    }
}
