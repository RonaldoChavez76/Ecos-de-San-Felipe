using UnityEngine;

/// <summary>
/// Interfaz de Barra de Vida y Fases del Jefe Final: La Sombra Guardiana en el Cerro del Fraile.
/// Se muestra automáticamente cuando Ronan entra en la cumbre del Mirador de la Cruz Azul.
/// Incluye sistema de banners de sobrecarga/aturdimiento.
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    public static BossHealthBarUI Instance { get; private set; }

    private SombraGuardianaBossAI bossAI;
    private HealthComponent bossHealth;
    private Transform playerTransform;
    private Texture2D whiteTex;

    // ── Sistema de Banner de Sobrecarga ───────────────────────────────────────
    private string bannerMessage = "";
    private float bannerTimer   = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        whiteTex = new Texture2D(1, 1);
        whiteTex.SetPixel(0, 0, Color.white);
        whiteTex.Apply();
    }

    public static BossHealthBarUI EnsureInstance()
    {
        if (Instance != null) return Instance;
        BossHealthBarUI existing = FindAnyObjectByType<BossHealthBarUI>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }
        GameObject go = new GameObject("BossHealthBarUI");
        Instance = go.AddComponent<BossHealthBarUI>();
        return Instance;
    }

    private void Update()
    {
        if (bossAI == null)
        {
            bossAI = FindAnyObjectByType<SombraGuardianaBossAI>();
            if (bossAI != null) bossHealth = bossAI.GetComponent<HealthComponent>();
        }

        if (playerTransform == null)
        {
            PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
            if (pm != null) playerTransform = pm.transform;
        }

        // Cuenta regresiva del banner
        if (bannerTimer > 0f)
            bannerTimer -= Time.deltaTime;
    }

    /// <summary>
    /// Muestra un banner de aviso prominente (p. ej. "¡JEFE ATURDIDO!") durante 4 segundos.
    /// </summary>
    public void MostrarBannerOvercarga(string msg)
    {
        bannerMessage = msg;
        bannerTimer   = 4f;
    }

    private void OnGUI()
    {
        if (Time.timeScale == 0f || bossAI == null || bossHealth == null || playerTransform == null) return;
        if (bossHealth.currentHealth <= 0f) return;

        float dist = Vector2.Distance(playerTransform.position, bossAI.transform.position);
        if (dist > 17.5f) return;

        float barWidth  = Mathf.Min(UITheme.Px(620f), Screen.width * 0.58f);
        float barHeight = UITheme.Px(24f);
        float titleH    = UITheme.Px(28f);
        float subH      = UITheme.Px(26f);
        float padX      = UITheme.Px(18f);
        float padY      = UITheme.Px(10f);
        float totalH    = padY + titleH + UITheme.Px(4f) + barHeight + UITheme.Px(6f) + subH + padY;
        float x         = (Screen.width - barWidth) * 0.5f;
        float y         = Mathf.Max(UITheme.Px(16f), Screen.height * 0.02f);

        string faseTexto = "FASE I: DESPERTAR DEL OLVIDO";
        Color fillColor = new Color(0.68f, 0.18f, 0.92f, 0.95f);

        if (bossAI.currentPhase == SombraGuardianaBossAI.BossPhase.Phase2_Tempest)
        {
            faseTexto = "FASE II: TEMPESTAD SOMBRÍA";
            fillColor = new Color(0.88f, 0.15f, 0.60f, 0.95f);
        }
        else if (bossAI.currentPhase == SombraGuardianaBossAI.BossPhase.Phase3_AbyssalFury)
        {
            faseTexto = "FASE III: FURIA ABISMAL";
            fillColor = new Color(0.95f, 0.12f, 0.25f, 0.95f);
        }

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = UITheme.Fuente(13),
            fontStyle = FontStyle.Bold,
            wordWrap = false
        };
        titleStyle.normal.textColor = new Color(0.95f, 0.85f, 1.0f);

        GUIStyle hpStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = UITheme.Fuente(12),
            fontStyle = FontStyle.Bold,
            wordWrap = false
        };
        hpStyle.normal.textColor = Color.white;

        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = UITheme.Fuente(12),
            fontStyle = FontStyle.Bold,
            wordWrap = false
        };
        subStyle.normal.textColor = bossAI.isShieldedBySeals
            ? new Color(0.55f, 0.97f, 1.0f)
            : new Color(1.0f, 0.85f, 0.35f);

        // Panel contenedor oscuro con borde dorado/púrpura
        Rect panelRect = new Rect(x - padX, y, barWidth + padX * 2f, totalH);
        UITheme.Panel(panelRect, new Color(0.82f, 0.58f, 0.98f, 0.92f), 1.5f);

        GUI.color = Color.white;
        GUI.Label(
            new Rect(x - padX + 6f, y + padY, barWidth + (padX - 6f) * 2f, titleH),
            $"LA SOMBRA GUARDIANA — {faseTexto}",
            titleStyle);

        // Barra de vida
        float barY = y + padY + titleH + UITheme.Px(4f);
        float ratio = Mathf.Clamp01(bossHealth.currentHealth / Mathf.Max(1f, bossHealth.maxHealth));
        GUI.color = new Color(0.18f, 0.08f, 0.22f, 1f);
        GUI.DrawTexture(new Rect(x, barY, barWidth, barHeight), whiteTex);

        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(x + 2f, barY + 2f, (barWidth - 4f) * ratio, barHeight - 4f), whiteTex);
        UITheme.Borde(new Rect(x, barY, barWidth, barHeight), new Color(1f, 1f, 1f, 0.28f), 1f);

        GUI.color = Color.white;
        GUI.Label(
            new Rect(x, barY, barWidth, barHeight),
            $"{Mathf.CeilToInt(bossHealth.currentHealth)} / {Mathf.CeilToInt(bossHealth.maxHealth)} HP",
            hpStyle);


        float subY = barY + barHeight + UITheme.Px(6f);

        // Texto de estado de sellos dinamico segun fase y escudo
        string estadoSellos;
        if (bossAI.isShieldedBySeals)
            estadoSellos = "ESCUDO ACTIVO: Purifica los 3 Sellos de la Cruz Azul [E / J]";
        else if (bossAI.currentPhase == SombraGuardianaBossAI.BossPhase.Phase3_AbyssalFury)
            estadoSellos = "SOBRECARGA! Contraataca la Gran Esfera con [K] | Modo Asalto [L]";
        else
            estadoSellos = "ESCUDO ROTO! Ataca con Modo Asalto [L]";

        GUI.Label(
            new Rect(x - padX + 6f, subY, barWidth + (padX - 6f) * 2f, subH),
            estadoSellos,
            subStyle);

        // Banner de Sobrecarga / Aturdimiento
        if (bannerTimer > 0f && !string.IsNullOrEmpty(bannerMessage))
        {
            float alpha = Mathf.Clamp01(bannerTimer);
            float bW    = Mathf.Min(UITheme.Px(700f), Screen.width * 0.68f);
            float bH    = UITheme.Px(42f);
            float bX    = (Screen.width - bW) * 0.5f;
            float bY2   = y + totalH + UITheme.Px(8f);

            GUIStyle bannerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = UITheme.Fuente(14),
                fontStyle = FontStyle.Bold,
                wordWrap  = false
            };
            bannerStyle.normal.textColor = new Color(1f, 0.95f, 0.2f, alpha);

            GUI.color = new Color(0.80f, 0.08f, 0.08f, 0.92f * alpha);
            GUI.DrawTexture(new Rect(bX, bY2, bW, bH), whiteTex);
            UITheme.Borde(new Rect(bX, bY2, bW, bH), new Color(1f, 0.85f, 0f, alpha), 2f);
            GUI.color = Color.white;
            GUI.Label(new Rect(bX, bY2, bW, bH), bannerMessage, bannerStyle);
        }
    }
}