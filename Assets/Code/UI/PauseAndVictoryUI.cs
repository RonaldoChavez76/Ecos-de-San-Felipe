using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controlador de la Pantalla de Victoria Final de «Ecos de San Felipe»
/// cuando Ronan recupera los 3 Ecos Sagrados (Fe, Tradición y Memoria) y disipa la Niebla del Olvido,
/// mostrando estadísticas completas de juego, contraataques (0.20s) y enemigos derrotados.
/// </summary>
public class PauseAndVictoryUI : MonoBehaviour
{
    public static PauseAndVictoryUI Instance { get; private set; }

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
        if (!victoriaYaActivada && SaveStateManager.Instance != null)
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

    private void OnGUI()
    {
        if (!mostrarPantallaVictoria) return;

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

