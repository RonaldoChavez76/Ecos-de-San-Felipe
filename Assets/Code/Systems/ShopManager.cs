using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor de la Tienda de Armas y Suministros de San Felipe.
/// Permite al jugador comprar el Arco del Atrio, el Báculo del Eco,
/// flechas, pociones de maná y bendiciones curativas usando el oro obtenido de enemigos y cofres.
/// Se puede abrir interactuando con mercaderes o presionando la tecla [B].
/// </summary>
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Estado de la Tienda")]
    public bool tiendaAbierta = false;
    public KeyCode teclaTienda = KeyCode.B;

    private string mensajeAviso = "";
    private float avisoTimer = 0f;
    private Color avisoColor = Color.white;

    public static ShopManager EnsureInstance()
    {
        if (Instance != null) return Instance;
        ShopManager existing = FindAnyObjectByType<ShopManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }
        GameObject go = new GameObject("ShopManager");
        Instance = go.AddComponent<ShopManager>();
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
    }

    private void Update()
    {
        if (avisoTimer > 0f) avisoTimer -= Time.unscaledDeltaTime;

        if (Input.GetKeyDown(teclaTienda))
        {
            AlternarTienda();
        }
        else if (tiendaAbierta && Input.GetKeyDown(KeyCode.Escape))
        {
            CerrarTienda();
        }
    }

    public void AlternarTienda()
    {
        if (tiendaAbierta) CerrarTienda();
        else AbrirTienda();
    }

    public void AbrirTienda()
    {
        tiendaAbierta = true;
        Time.timeScale = 0f;
        AudioManager.Instance?.PlaySecretSound();
    }

    public void CerrarTienda()
    {
        tiendaAbierta = false;
        Time.timeScale = 1f;
    }

    private void MostrarAviso(string msg, Color col)
    {
        mensajeAviso = msg;
        avisoTimer = 2.5f;
        avisoColor = col;
    }

    private bool DescontarOro(int precio, PlayerMovement pm)
    {
        int disponible = pm != null ? pm.oro : (SaveStateManager.Instance != null ? SaveStateManager.Instance.oro : 0);
        if (disponible < precio) return false;

        if (pm != null) pm.oro -= precio;
        if (SaveStateManager.Instance != null)
        {
            SaveStateManager.Instance.oro = pm != null ? pm.oro : (SaveStateManager.Instance.oro - precio);
            SaveStateManager.Instance.GuardarProgreso();
        }
        return true;
    }

    public void ComprarArco()
    {
        int precio = 50;
        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null && pm.TieneArmaDesbloqueada(WeaponType.Bow))
        {
            MostrarAviso("¡Ya posees el Arco del Atrio!", UITheme.Dorado);
            return;
        }

        if (DescontarOro(precio, pm))
        {
            if (pm != null) pm.DesbloquearArma(WeaponType.Bow);
            AudioManager.Instance?.PlayEchoSound();
            MostrarAviso("¡Has adquirido el Arco del Atrio! Pulsa [Q] para equiparlo.", UITheme.Verde);
        }
        else
        {
            AudioManager.Instance?.PlayDodgeSound();
            MostrarAviso($"Oro insuficiente. Necesitas {precio} monedas de oro.", UITheme.Alerta);
        }
    }

    public void ComprarBaculo()
    {
        int precio = 80;
        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null && pm.TieneArmaDesbloqueada(WeaponType.Staff))
        {
            MostrarAviso("¡Ya posees el Báculo del Eco!", UITheme.Dorado);
            return;
        }

        if (DescontarOro(precio, pm))
        {
            if (pm != null) pm.DesbloquearArma(WeaponType.Staff);
            AudioManager.Instance?.PlayEchoSound();
            MostrarAviso("¡Has adquirido el Báculo del Eco! Usa tu reserva de Maná para conjurar orbes.", UITheme.Verde);
        }
        else
        {
            AudioManager.Instance?.PlayDodgeSound();
            MostrarAviso($"Oro insuficiente. Necesitas {precio} monedas de oro.", UITheme.Alerta);
        }
    }

    public void ComprarFlechas()
    {
        int precio = 15;
        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null && pm.flechas >= pm.maxFlechas)
        {
            MostrarAviso("¡Tu carcaj de flechas ya está al máximo!", UITheme.Dorado);
            return;
        }

        if (DescontarOro(precio, pm))
        {
            pm?.AgregarFlechas(10);
            AudioManager.Instance?.PlaySecretSound();
            MostrarAviso("¡+10 Flechas añadidas a tu inventario!", UITheme.Verde);
        }
        else
        {
            AudioManager.Instance?.PlayDodgeSound();
            MostrarAviso($"Oro insuficiente. Necesitas {precio} monedas de oro.", UITheme.Alerta);
        }
    }

    public void ComprarMana()
    {
        int precio = 15;
        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null && pm.manaActual >= pm.manaMaximo)
        {
            MostrarAviso("¡Tu maná ya está al máximo!", UITheme.Dorado);
            return;
        }

        if (DescontarOro(precio, pm))
        {
            pm?.RestaurarMana(40f);
            AudioManager.Instance?.PlaySecretSound();
            MostrarAviso("¡+40 de Maná restaurado a tu reserva!", UITheme.Turquesa);
        }
        else
        {
            AudioManager.Instance?.PlayDodgeSound();
            MostrarAviso($"Oro insuficiente. Necesitas {precio} monedas de oro.", UITheme.Alerta);
        }
    }

    public void ComprarCuracion()
    {
        int precio = 15;
        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null && pm.currentHealth >= pm.maxHealth)
        {
            MostrarAviso("¡Tu vida ya está al máximo!", UITheme.Dorado);
            return;
        }

        if (DescontarOro(precio, pm))
        {
            pm?.CurarSalud(30f);
            AudioManager.Instance?.PlaySecretSound();
            MostrarAviso("¡+30 HP restaurados con la bendición!", UITheme.Verde);
        }
        else
        {
            AudioManager.Instance?.PlayDodgeSound();
            MostrarAviso($"Oro insuficiente. Necesitas {precio} monedas de oro.", UITheme.Alerta);
        }
    }

    private void OnGUI()
    {
        if (!tiendaAbierta) return;

        // Fondo oscuro
        GUI.color = new Color(0f, 0f, 0f, 0.88f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), UITheme.Blanca);
        GUI.color = Color.white;

        float panelW = Mathf.Min(UITheme.Px(720f), Screen.width - 30f);
        float panelH = Mathf.Min(UITheme.Px(540f), Screen.height - 30f);
        float px = (Screen.width - panelW) * 0.5f;
        float py = (Screen.height - panelH) * 0.5f;
        float pad = UITheme.Px(20f);
        float innerW = panelW - pad * 2f;

        UITheme.Panel(new Rect(px, py, panelW, panelH), UITheme.BordeDorado, 3f);

        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        int oroActual = pm != null ? pm.oro : (SaveStateManager.Instance != null ? SaveStateManager.Instance.oro : 0);
        bool tieneArco = pm != null && pm.TieneArmaDesbloqueada(WeaponType.Bow);
        bool tieneBaculo = pm != null && pm.TieneArmaDesbloqueada(WeaponType.Staff);

        float y = py + UITheme.Px(16f);
        UITheme.EtiquetaConSombra(new Rect(px, y, panelW, UITheme.Px(32f)),
            "★ TIENDA DE ARMAS Y SUMINISTROS DE SAN FELIPE ★",
            UITheme.Estilo(22, FontStyle.Bold, TextAnchor.MiddleCenter, UITheme.Dorado));
        y += UITheme.Px(36f);

        GUI.Label(new Rect(px, y, panelW, UITheme.Px(24f)),
            $"💰 TU ORO ACTUAL: <color=#FFE066><b>{oroActual} monedas</b></color>    |    Presiona [B] o [ESC] para salir",
            UITheme.Estilo(15, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
        y += UITheme.Px(32f);

        // Aviso si compró o faltó oro
        if (avisoTimer > 0f && !string.IsNullOrEmpty(mensajeAviso))
        {
            Rect rAviso = new Rect(px + pad, y, innerW, UITheme.Px(28f));
            GUI.color = new Color(0.1f, 0.1f, 0.18f, 0.95f);
            GUI.DrawTexture(rAviso, UITheme.Blanca);
            GUI.color = Color.white;
            GUI.Label(rAviso, mensajeAviso, UITheme.Estilo(14, FontStyle.Bold, TextAnchor.MiddleCenter, avisoColor));
            y += UITheme.Px(32f);
        }

        // Fila 1: Arco del Atrio
        DibujarFilaArticulo(px + pad, y, innerW, UITheme.Px(58f),
            "🏹 Arco del Atrio (Arma a Distancia)",
            "Dispara flechas a distancia. Daño moderado. Requiere flechas.",
            50, tieneArco ? "EN PROPIEDAD" : "COMPRAR (50 ORO)", !tieneArco, ComprarArco);
        y += UITheme.Px(64f);

        // Fila 2: Báculo del Eco
        DibujarFilaArticulo(px + pad, y, innerW, UITheme.Px(58f),
            "🔮 Báculo del Eco (Arma Mágica)",
            "Dispara poderosos orbes arcanos. Consume Maná.",
            80, tieneBaculo ? "EN PROPIEDAD" : "COMPRAR (80 ORO)", !tieneBaculo, ComprarBaculo);
        y += UITheme.Px(64f);

        // Fila 3: Carcaj de Flechas (+10)
        string infoFlechas = pm != null ? $" ({pm.flechas}/{pm.maxFlechas})" : "";
        DibujarFilaArticulo(px + pad, y, innerW, UITheme.Px(58f),
            $"🏹 Carcaj de Flechas (+10 Flechas){infoFlechas}",
            "Munición indispensable para atacar a distancia con el Arco.",
            15, "COMPRAR (15 ORO)", true, ComprarFlechas);
        y += UITheme.Px(64f);

        // Fila 4: Poción de Maná (+40)
        string infoMana = pm != null ? $" ({Mathf.CeilToInt(pm.manaActual)}/{pm.manaMaximo})" : "";
        DibujarFilaArticulo(px + pad, y, innerW, UITheme.Px(58f),
            $"💧 Cristal de Maná (+40 Maná){infoMana}",
            "Restaura tu reserva de maná para continuar conjurando con el báculo.",
            15, "COMPRAR (15 ORO)", true, ComprarMana);
        y += UITheme.Px(64f);

        // Botón Cerrar
        float btnH = UITheme.Px(42f);
        float by = py + panelH - btnH - UITheme.Px(16f);
        if (GUI.Button(new Rect(px + (panelW - UITheme.Px(220f)) * 0.5f, by, UITheme.Px(220f), btnH), "Cerrar Tienda", UITheme.Boton(16)))
        {
            CerrarTienda();
        }
    }

    private void DibujarFilaArticulo(float x, float y, float w, float h, string titulo, string desc, int precio, string textoBtn, bool habilitado, System.Action onComprar)
    {
        Rect rFila = new Rect(x, y, w, h);
        GUI.color = new Color(0.08f, 0.11f, 0.18f, 0.90f);
        GUI.DrawTexture(rFila, UITheme.Blanca);
        GUI.color = new Color(0.35f, 0.45f, 0.65f, 0.6f);
        GUI.DrawTexture(new Rect(x, y, w, 1f), UITheme.Blanca);
        GUI.DrawTexture(new Rect(x, y + h - 1f, w, 1f), UITheme.Blanca);
        GUI.color = Color.white;

        float btnW = UITheme.Px(180f);
        float infoW = w - btnW - UITheme.Px(20f);

        GUI.Label(new Rect(x + UITheme.Px(10f), y + UITheme.Px(4f), infoW, UITheme.Px(24f)),
            titulo, UITheme.Estilo(15, FontStyle.Bold, TextAnchor.MiddleLeft, UITheme.Dorado));

        GUI.Label(new Rect(x + UITheme.Px(10f), y + UITheme.Px(28f), infoW, UITheme.Px(24f)),
            desc, UITheme.Estilo(13, FontStyle.Normal, TextAnchor.MiddleLeft, UITheme.TextoSecundario));

        Rect rBtn = new Rect(x + w - btnW - UITheme.Px(10f), y + (h - UITheme.Px(38f)) * 0.5f, btnW, UITheme.Px(38f));

        if (!habilitado)
        {
            GUI.color = new Color(0.4f, 0.4f, 0.4f, 0.7f);
            GUI.Box(rBtn, textoBtn, UITheme.Boton(13));
            GUI.color = Color.white;
        }
        else
        {
            if (GUI.Button(rBtn, textoBtn, UITheme.Boton(13)))
            {
                onComprar?.Invoke();
            }
        }
    }
}

