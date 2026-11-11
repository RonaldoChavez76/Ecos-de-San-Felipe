using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Modal global de Opciones y Ajustes Audiovisuales para Ecos de San Felipe.
/// Accesible tanto desde la Pantalla de Inicio como desde la pausa en cualquier nivel ([ESC] o botón).
/// Incluye:
///  • Sliders de Audio (Master, Música, Efectos) con persistencia en PlayerPrefs y aplicación en AudioManager.
///  • Ajuste de Brillo / Exposición (-0.4 a +0.4) con overlay procedural a pantalla completa.
///  • Modo de Pantalla (Pantalla Completa vs Ventana).
///  • Sincronización Vertical (V-Sync).
///  • Selector dinámico de Dificultad (Fácil, Normal, Difícil).
///  • Marco colonial con ribetes dorados dobles.
/// </summary>
public class OptionsModalUI : MonoBehaviour
{
    public static OptionsModalUI Instance { get; private set; }

    [Header("Estado")]
    public bool isOptionsOpen = false;

    // Sliders de Audio
    private float volMaster = 1.0f;
    private float volMusic = 0.75f;
    private float volSFX = 0.85f;

    // Control Gráfico
    private float brillo = 0f; // -0.4 a +0.4
    private bool fullscreen = true;
    private bool vsync = true;
    private int dificultadSeleccionada = 1; // 0=Facil, 1=Normal, 2=Dificil

    private const string PREF_BRILLO = "Graficos_Brillo";
    private const string PREF_FULLSCREEN = "Graficos_FullScreen";
    private const string PREF_VSYNC = "Graficos_VSync";

    public static OptionsModalUI EnsureInstance()
    {
        if (Instance != null) return Instance;
        OptionsModalUI existing = FindAnyObjectByType<OptionsModalUI>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }
        GameObject go = new GameObject("OptionsModalUI");
        Instance = go.AddComponent<OptionsModalUI>();
        DontDestroyOnLoad(go);
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CargarPreferencias();
    }

    private void Start()
    {
        AplicarAjustesGraficos();
    }

    public void CargarPreferencias()
    {
        volMaster = PlayerPrefs.GetFloat("VolMaster", 1.0f);
        volMusic = PlayerPrefs.GetFloat("VolMusic", 0.75f);
        volSFX = PlayerPrefs.GetFloat("VolSFX", 0.85f);
        brillo = PlayerPrefs.GetFloat(PREF_BRILLO, 0f);
        fullscreen = PlayerPrefs.GetInt(PREF_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
        vsync = PlayerPrefs.GetInt(PREF_VSYNC, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;

        if (SaveStateManager.Instance != null)
        {
            dificultadSeleccionada = (int)SaveStateManager.Instance.dificultadActual;
        }
        else
        {
            dificultadSeleccionada = PlayerPrefs.GetInt("GameDifficulty", 1);
        }
    }

    public void AbrirModal()
    {
        CargarPreferencias();
        isOptionsOpen = true;
    }

    public void CerrarModal()
    {
        isOptionsOpen = false;
        GuardarPreferencias();
    }

    public void GuardarPreferencias()
    {
        PlayerPrefs.SetFloat("VolMaster", volMaster);
        PlayerPrefs.SetFloat("VolMusic", volMusic);
        PlayerPrefs.SetFloat("VolSFX", volSFX);
        PlayerPrefs.SetFloat(PREF_BRILLO, brillo);
        PlayerPrefs.SetInt(PREF_FULLSCREEN, fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(PREF_VSYNC, vsync ? 1 : 0);
        PlayerPrefs.SetInt("GameDifficulty", dificultadSeleccionada);
        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(volMaster);
            AudioManager.Instance.SetMusicVolume(volMusic);
            AudioManager.Instance.SetSFXVolume(volSFX);
        }

        if (SaveStateManager.Instance != null)
        {
            SaveStateManager.Instance.EstablecerDificultad((SaveStateManager.GameDifficulty)dificultadSeleccionada);
        }

        AplicarAjustesGraficos();
    }

    public void RestablecerPorDefecto()
    {
        volMaster = 1.0f;
        volMusic = 0.75f;
        volSFX = 0.85f;
        brillo = 0f;
        fullscreen = true;
        vsync = true;
        dificultadSeleccionada = 1;

        GuardarPreferencias();
    }

    private void AplicarAjustesGraficos()
    {
        if (Screen.fullScreen != fullscreen)
        {
            Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }
        QualitySettings.vSyncCount = vsync ? 1 : 0;
    }

    private void OnGUI()
    {
        // 1. Overlay de Brillo global (si brillo != 0) siempre activo en pantalla
        if (Mathf.Abs(brillo) > 0.01f)
        {
            Color prev = GUI.color;
            if (brillo > 0f)
            {
                // Brillo positivo: velo blanco aditivo suave
                GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(brillo * 0.75f));
            }
            else
            {
                // Brillo negativo: velo negro atenuante
                GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01(Mathf.Abs(brillo) * 0.85f));
            }
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), UITheme.Blanca);
            GUI.color = prev;
        }

        if (!isOptionsOpen) return;

        // Fondo oscuro bloqueador
        Color prevColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), UITheme.Blanca);
        GUI.color = Color.white;

        float panelW = Mathf.Min(UITheme.Px(720f), Screen.width - UITheme.Px(28f));
        float panelH = Mathf.Min(UITheme.Px(600f), Screen.height - UITheme.Px(24f));
        float px = (Screen.width - panelW) * 0.5f;
        float py = (Screen.height - panelH) * 0.5f;
        float pad = UITheme.Px(24f);
        float innerW = panelW - pad * 2f;

        Rect rPanel = new Rect(px, py, panelW, panelH);
        UITheme.Panel(rPanel, UITheme.BordeDorado, 2.5f);
        // Doble ribete colonial
        UITheme.Borde(new Rect(px + 4f, py + 4f, panelW - 8f, panelH - 8f), new Color(1f, 0.88f, 0.42f, 0.35f), 1f);

        float curY = py + UITheme.Px(16f);

        // Título Principal
        UITheme.EtiquetaConSombra(new Rect(px, curY, panelW, UITheme.Px(32f)),
            "⚜ AJUSTES Y OPCIONES DE SAN FELIPE ⚜",
            UITheme.Estilo(22, FontStyle.Bold, TextAnchor.MiddleCenter, UITheme.Dorado));
        curY += UITheme.Px(38f);

        // SECCIÓN 1: DIFICULTAD DINÁMICA
        GUI.Label(new Rect(px + pad, curY, innerW, UITheme.Px(20f)),
            "<b>DIFICULTAD DE LA AVENTURA:</b>",
            UITheme.Estilo(12, FontStyle.Bold, TextAnchor.MiddleLeft, UITheme.Turquesa));
        curY += UITheme.Px(22f);

        float diffBtnW = (innerW - UITheme.Px(16f)) / 3f;
        float diffBtnH = UITheme.Px(32f);
        string[] diffNames = { "FÁCIL", "NORMAL", "DIFÍCIL" };
        string[] diffDescs = { "Daño x0.7, Ventana 0.28s", "Equilibrado, Ventana 0.20s", "Daño x1.4, Oro +50%, 0.16s" };

        for (int i = 0; i < 3; i++)
        {
            bool sel = (dificultadSeleccionada == i);
            Rect rBtn = new Rect(px + pad + i * (diffBtnW + UITheme.Px(8f)), curY, diffBtnW, diffBtnH);

            Color colBorde = sel ? UITheme.Dorado : new Color(0.4f, 0.4f, 0.5f, 0.6f);
            Color colFondo = sel ? new Color(0.20f, 0.16f, 0.08f, 0.95f) : new Color(0.08f, 0.08f, 0.12f, 0.85f);

            GUI.color = colFondo;
            GUI.DrawTexture(rBtn, UITheme.Blanca);
            GUI.color = Color.white;
            UITheme.Borde(rBtn, colBorde, sel ? 2f : 1f);

            string txt = sel ? $"<b><color=#FFE066>● {diffNames[i]}</color></b>" : diffNames[i];
            if (GUI.Button(rBtn, txt, UITheme.Estilo(12, FontStyle.Bold, TextAnchor.MiddleCenter, sel ? UITheme.Dorado : Color.white)))
            {
                dificultadSeleccionada = i;
                if (SaveStateManager.Instance != null)
                {
                    SaveStateManager.Instance.EstablecerDificultad((SaveStateManager.GameDifficulty)i);
                }
            }
        }
        curY += diffBtnH + UITheme.Px(6f);
        GUI.Label(new Rect(px + pad, curY, innerW, UITheme.Px(16f)),
            $"<i>{diffDescs[dificultadSeleccionada]}</i>",
            UITheme.Estilo(10, FontStyle.Italic, TextAnchor.MiddleCenter, UITheme.TextoSecundario));
        curY += UITheme.Px(22f);

        // SECCIÓN 2: AUDIO (MASTER, MÚSICA, SFX)
        GUI.Label(new Rect(px + pad, curY, innerW, UITheme.Px(20f)),
            "<b>VOLUMEN Y SONIDO:</b>",
            UITheme.Estilo(12, FontStyle.Bold, TextAnchor.MiddleLeft, UITheme.Turquesa));
        curY += UITheme.Px(22f);

        volMaster = DibujarFilaSlider(px + pad, curY, innerW, "Volumen General", volMaster);
        curY += UITheme.Px(26f);
        volMusic = DibujarFilaSlider(px + pad, curY, innerW, "Música de Fondo (BGM)", volMusic);
        curY += UITheme.Px(26f);
        volSFX = DibujarFilaSlider(px + pad, curY, innerW, "Efectos y Combate (SFX)", volSFX);
        curY += UITheme.Px(32f);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(volMaster);
            AudioManager.Instance.SetMusicVolume(volMusic);
            AudioManager.Instance.SetSFXVolume(volSFX);
        }

        // SECCIÓN 3: GRÁFICOS Y PANTALLA
        GUI.Label(new Rect(px + pad, curY, innerW, UITheme.Px(20f)),
            "<b>AJUSTES VISUALES Y MONITOR:</b>",
            UITheme.Estilo(12, FontStyle.Bold, TextAnchor.MiddleLeft, UITheme.Turquesa));
        curY += UITheme.Px(22f);

        // Slider de Brillo
        float labelW = innerW * 0.38f;
        float sliderW = innerW * 0.44f;
        float valueW = innerW * 0.18f;

        GUI.Label(new Rect(px + pad, curY, labelW, UITheme.Px(20f)), "Brillo / Exposición", UITheme.Estilo(11, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white));
        brillo = GUI.HorizontalSlider(new Rect(px + pad + labelW, curY + UITheme.Px(4f), sliderW, UITheme.Px(16f)), brillo, -0.4f, 0.4f);
        string brilloTxt = brillo >= 0f ? $"+{Mathf.RoundToInt(brillo * 100f)}%" : $"{Mathf.RoundToInt(brillo * 100f)}%";
        GUI.Label(new Rect(px + pad + labelW + sliderW + UITheme.Px(6f), curY, valueW, UITheme.Px(20f)), brilloTxt, UITheme.Estilo(11, FontStyle.Bold, TextAnchor.MiddleRight, UITheme.Dorado));
        curY += UITheme.Px(28f);

        // Toggles Pantalla Completa y V-Sync
        float halfW = (innerW - UITheme.Px(12f)) * 0.5f;
        Rect rFs = new Rect(px + pad, curY, halfW, UITheme.Px(30f));
        Rect rVs = new Rect(px + pad + halfW + UITheme.Px(12f), curY, halfW, UITheme.Px(30f));

        string fsLabel = fullscreen ? "▣ Pantalla Completa [ACTIVO]" : "⧈ Modo Ventana";
        if (GUI.Button(rFs, fsLabel, UITheme.Estilo(11, FontStyle.Bold, TextAnchor.MiddleCenter, fullscreen ? UITheme.Verde : Color.white)))
        {
            fullscreen = !fullscreen;
            Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }
        UITheme.Borde(rFs, fullscreen ? UITheme.Verde : new Color(0.4f, 0.4f, 0.5f), 1f);

        string vsLabel = vsync ? "✔ Sincronización V-Sync" : "✖ V-Sync Desactivado";
        if (GUI.Button(rVs, vsLabel, UITheme.Estilo(11, FontStyle.Bold, TextAnchor.MiddleCenter, vsync ? UITheme.Turquesa : Color.white)))
        {
            vsync = !vsync;
            QualitySettings.vSyncCount = vsync ? 1 : 0;
        }
        UITheme.Borde(rVs, vsync ? UITheme.Turquesa : new Color(0.4f, 0.4f, 0.5f), 1f);

        curY += UITheme.Px(44f);

        // BOTONES INFERIORES: RESTABLECER / GUARDAR Y VOLVER
        float btnH = UITheme.Px(42f);
        float bGap = UITheme.Px(14f);
        float bW = (innerW - bGap) * 0.5f;
        float by = py + panelH - btnH - UITheme.Px(16f);

        if (GUI.Button(new Rect(px + pad, by, bW, btnH), "Restablecer Valores", UITheme.Boton(13)))
        {
            RestablecerPorDefecto();
            AudioManager.Instance?.PlayDodgeSound();
        }

        if (GUI.Button(new Rect(px + pad + bW + bGap, by, bW, btnH), "<b>Guardar y Volver</b>", UITheme.Boton(14)))
        {
            CerrarModal();
            AudioManager.Instance?.PlayItemPickupSound();
        }

        GUI.color = prevColor;
    }

    private float DibujarFilaSlider(float x, float y, float totalW, string label, float value)
    {
        float labelW = totalW * 0.38f;
        float sliderW = totalW * 0.44f;
        float valueW = totalW * 0.18f;

        GUI.Label(new Rect(x, y, labelW, UITheme.Px(20f)), label, UITheme.Estilo(11, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white));
        float newVal = GUI.HorizontalSlider(new Rect(x + labelW, y + UITheme.Px(4f), sliderW, UITheme.Px(16f)), value, 0f, 1f);
        int pct = Mathf.RoundToInt(newVal * 100f);
        GUI.Label(new Rect(x + labelW + sliderW + UITheme.Px(6f), y, valueW, UITheme.Px(20f)), $"{pct}%", UITheme.Estilo(11, FontStyle.Bold, TextAnchor.MiddleRight, UITheme.Dorado));

        return newVal;
    }
}

