using UnityEngine;

/// <summary>
/// Interfaz de Barra de Vida y Fases del Jefe Final: La Sombra Guardiana en el Cerro del Fraile.
/// Se muestra automáticamente cuando Ronan entra en la cumbre del Mirador de la Cruz Azul.
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    public static BossHealthBarUI Instance { get; private set; }

    private SombraGuardianaBossAI bossAI;
    private HealthComponent bossHealth;
    private Transform playerTransform;
    private Texture2D whiteTex;

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
    }

    private void OnGUI()
    {
        if (Time.timeScale == 0f || bossAI == null || bossHealth == null || playerTransform == null) return;
        if (bossHealth.currentHealth <= 0f) return;

        float dist = Vector2.Distance(playerTransform.position, bossAI.transform.position);
        if (dist > 17.5f) return;

        float barWidth = 480f;
        float barHeight = 22f;
        float x = (Screen.width - barWidth) * 0.5f;
        float y = 22f;

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
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = new Color(0.95f, 0.85f, 1.0f);

        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };
        subStyle.normal.textColor = bossAI.isShieldedBySeals
            ? new Color(0.35f, 0.92f, 1.0f)
            : new Color(1.0f, 0.85f, 0.35f);

        // Fondo oscuro del panel del Jefe
        GUI.color = new Color(0.06f, 0.03f, 0.10f, 0.90f);
        GUI.DrawTexture(new Rect(x - 14f, y - 6f, barWidth + 28f, 76f), whiteTex);

        GUI.color = Color.white;
        GUI.Label(new Rect(x, y - 2f, barWidth, 20f), $"LA SOMBRA GUARDIANA — {faseTexto}", titleStyle);

        // Barra de vida
        float ratio = Mathf.Clamp01(bossHealth.currentHealth / Mathf.Max(1f, bossHealth.maxHealth));
        GUI.color = new Color(0.18f, 0.08f, 0.22f, 1f);
        GUI.DrawTexture(new Rect(x, y + 22f, barWidth, barHeight), whiteTex);

        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(x + 2f, y + 24f, (barWidth - 4f) * ratio, barHeight - 4f), whiteTex);

        GUI.color = Color.white;
        GUI.Label(new Rect(x, y + 22f, barWidth, barHeight), $"{Mathf.CeilToInt(bossHealth.currentHealth)} / {Mathf.CeilToInt(bossHealth.maxHealth)} HP", titleStyle);

        string estadoSellos = bossAI.isShieldedBySeals
            ? "ESCUDO ACTIVO: Purifica los 3 Sellos de la Cruz Azul [E / J]"
            : "¡ESCUDO DESTRUIDO! Contraataca sus orbes con [K] (0.20s)";
        GUI.Label(new Rect(x, y + 46f, barWidth, 20f), estadoSellos, subStyle);
    }
}

