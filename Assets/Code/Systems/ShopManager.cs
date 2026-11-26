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

        if (MobileInput.GetShopDown(teclaTienda))
        {
            AlternarTienda();
        }
        else if (tiendaAbierta && MobileInput.GetPauseDown())
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

        Rect sa = UITheme.SafeAreaGUI;
        float panelW = Mathf.Min(UITheme.Px(760f), sa.width - UITheme.Px(24f));
        float panelH = Mathf.Min(UITheme.Px(560f), sa.height - UITheme.Px(20f));
        float px = sa.x + (sa.width - panelW) * 0.5f;
        float py = sa.y + (sa.height - panelH) * 0.5f;
        float pad = UITheme.Px(20f);
        float innerW = panelW - pad * 2f;

        AsegurarIconosTienda();
        UITheme.Panel(new Rect(px, py, panelW, panelH), UITheme.BordeDorado, 3f);

        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        int oroActual = pm != null ? pm.oro : (SaveStateManager.Instance != null ? SaveStateManager.Instance.oro : 0);
        bool tieneArco = pm != null && pm.TieneArmaDesbloqueada(WeaponType.Bow);
        bool tieneBaculo = pm != null && pm.TieneArmaDesbloqueada(WeaponType.Staff);

        float y = py + UITheme.Px(14f);
        UITheme.EtiquetaConSombra(new Rect(px, y, panelW, UITheme.Px(34f)),
            "★ TIENDA DE ARMAS Y SUMINISTROS DE SAN FELIPE ★",
            UITheme.Estilo(21, FontStyle.Bold, TextAnchor.MiddleCenter, UITheme.Dorado));
        y += UITheme.Px(38f);

        string exitHint = MobileInput.IsTouchControlsActive ? "Toca Cerrar Tienda para volver" : "Presiona [B] o [ESC] para salir";
        GUI.Label(new Rect(px, y, panelW, UITheme.Px(26f)),
            $"★ TU ORO ACTUAL: <color=#FFE066><b>{oroActual} monedas</b></color>    |    {exitHint}",
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

        float rowH = UITheme.Px(62f);
        float rowStep = UITheme.Px(68f);

        // Fila 1: Arco del Atrio
        DibujarFilaArticulo(px + pad, y, innerW, rowH, texIconoArco,
            "Arco del Atrio (Arma a Distancia)",
            "Dispara flechas a distancia. Daño moderado. Requiere flechas.",
            50, tieneArco ? "EN PROPIEDAD" : "COMPRAR (50 ORO)", !tieneArco, ComprarArco);
        y += rowStep;

        // Fila 2: Báculo del Eco
        DibujarFilaArticulo(px + pad, y, innerW, rowH, texIconoBaculo,
            "Báculo del Eco (Arma Mágica)",
            "Dispara poderosos orbes arcanos. Consume Maná.",
            80, tieneBaculo ? "EN PROPIEDAD" : "COMPRAR (80 ORO)", !tieneBaculo, ComprarBaculo);
        y += rowStep;

        // Fila 3: Carcaj de Flechas (+10)
        string infoFlechas = pm != null ? $" ({pm.flechas}/{pm.maxFlechas})" : "";
        DibujarFilaArticulo(px + pad, y, innerW, rowH, texIconoFlechas,
            $"Carcaj de Flechas (+10 Flechas){infoFlechas}",
            "Munición indispensable para atacar a distancia con el Arco.",
            15, "COMPRAR (15 ORO)", true, ComprarFlechas);
        y += rowStep;

        // Fila 4: Poción de Maná (+40)
        string infoMana = pm != null ? $" ({Mathf.CeilToInt(pm.manaActual)}/{pm.manaMaximo})" : "";
        DibujarFilaArticulo(px + pad, y, innerW, rowH, texIconoMana,
            $"Cristal de Maná (+40 Maná){infoMana}",
            "Restaura tu reserva de maná para continuar conjurando con el báculo.",
            15, "COMPRAR (15 ORO)", true, ComprarMana);

        // Botón Cerrar
        float btnH = UITheme.Px(44f);
        float by = py + panelH - btnH - UITheme.Px(14f);
        if (GUI.Button(new Rect(px + (panelW - UITheme.Px(230f)) * 0.5f, by, UITheme.Px(230f), btnH), "Cerrar Tienda", UITheme.Boton(16)))
        {
            CerrarTienda();
        }
    }

    private void DibujarFilaArticulo(float x, float y, float w, float h, Texture2D icono, string titulo, string desc, int precio, string textoBtn, bool habilitado, System.Action onComprar)
    {
        Rect rFila = new Rect(x, y, w, h);
        GUI.color = new Color(0.08f, 0.11f, 0.18f, 0.90f);
        GUI.DrawTexture(rFila, UITheme.Blanca);
        GUI.color = new Color(0.35f, 0.45f, 0.65f, 0.6f);
        GUI.DrawTexture(new Rect(x, y, w, 1f), UITheme.Blanca);
        GUI.DrawTexture(new Rect(x, y + h - 1f, w, 1f), UITheme.Blanca);
        GUI.color = Color.white;

        // Recuadro e icono pixel-art a la izquierda
        float iconBox = UITheme.Px(42f);
        Rect rIconBox = new Rect(x + UITheme.Px(10f), y + (h - iconBox) * 0.5f, iconBox, iconBox);
        GUI.color = new Color(0.04f, 0.06f, 0.10f, 0.92f);
        GUI.DrawTexture(rIconBox, UITheme.Blanca);
        GUI.color = Color.white;
        UITheme.Borde(rIconBox, new Color(0.85f, 0.68f, 0.28f, 0.70f), 1f);
        if (icono != null)
        {
            float padIcon = UITheme.Px(4f);
            GUI.DrawTexture(new Rect(rIconBox.x + padIcon, rIconBox.y + padIcon, iconBox - padIcon * 2f, iconBox - padIcon * 2f), icono, ScaleMode.ScaleToFit);
        }

        float btnW = UITheme.Px(190f);
        float textX = rIconBox.xMax + UITheme.Px(12f);
        float infoW = (x + w - btnW - UITheme.Px(18f)) - textX;

        GUI.Label(new Rect(textX, y + UITheme.Px(6f), infoW, UITheme.Px(24f)),
            titulo, UITheme.Estilo(15, FontStyle.Bold, TextAnchor.MiddleLeft, UITheme.Dorado));

        GUI.Label(new Rect(textX, y + UITheme.Px(31f), infoW, UITheme.Px(24f)),
            desc, UITheme.Estilo(13, FontStyle.Normal, TextAnchor.MiddleLeft, UITheme.TextoSecundario));

        Rect rBtn = new Rect(x + w - btnW - UITheme.Px(10f), y + (h - UITheme.Px(40f)) * 0.5f, btnW, UITheme.Px(40f));

        if (!habilitado)
        {
            GUI.color = new Color(0.12f, 0.22f, 0.16f, 0.92f);
            GUI.DrawTexture(rBtn, UITheme.Blanca);
            GUI.color = Color.white;
            UITheme.Borde(rBtn, new Color(0.45f, 0.80f, 0.55f, 0.85f), 1.5f);
            GUI.Label(rBtn, textoBtn, UITheme.Estilo(13, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.75f, 0.96f, 0.80f, 1f)));
        }
        else
        {
            if (GUI.Button(rBtn, textoBtn, UITheme.Boton(13)))
            {
                onComprar?.Invoke();
            }
        }
    }

    private static Texture2D texIconoArco;
    private static Texture2D texIconoBaculo;
    private static Texture2D texIconoFlechas;
    private static Texture2D texIconoMana;

    private static void AsegurarIconosTienda()
    {
        if (texIconoArco == null) texIconoArco = CrearIconoTienda(0);
        if (texIconoBaculo == null) texIconoBaculo = CrearIconoTienda(1);
        if (texIconoFlechas == null) texIconoFlechas = CrearIconoTienda(2);
        if (texIconoMana == null) texIconoMana = CrearIconoTienda(3);
    }

    private static Texture2D CrearIconoTienda(int tipo)
    {
        const int s = 24;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
                tex.SetPixel(x, y, Color.clear);

        if (tipo == 0)
        {
            // Arco del Atrio
            for (int t = 4; t <= 19; t++)
            {
                int bx = Mathf.Clamp(Mathf.RoundToInt(16f - Mathf.Pow((t - 11.5f) / 3.8f, 2f) * 2.2f), 2, 21);
                tex.SetPixel(bx, t, new Color(0.95f, 0.72f, 0.28f, 1f));
                tex.SetPixel(bx - 1, t, new Color(0.70f, 0.44f, 0.14f, 1f));
            }
            for (int y = 5; y <= 18; y++) tex.SetPixel(8, y, new Color(0.90f, 0.94f, 1f, 0.85f));
            for (int x = 6; x <= 20; x++) tex.SetPixel(x, 12, new Color(0.95f, 0.88f, 0.55f, 1f));
            tex.SetPixel(21, 12, Color.white);
            tex.SetPixel(19, 13, Color.white);
            tex.SetPixel(19, 11, Color.white);
        }
        else if (tipo == 1)
        {
            // Báculo del Eco
            for (int i = 3; i <= 16; i++)
            {
                tex.SetPixel(i, i, new Color(0.72f, 0.44f, 0.20f, 1f));
                tex.SetPixel(i - 1, i, new Color(0.48f, 0.26f, 0.10f, 1f));
            }
            Vector2 orbCenter = new Vector2(17.5f, 17.5f);
            for (int y = 12; y < 23; y++)
            {
                for (int x = 12; x < 23; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), orbCenter);
                    if (d <= 4.3f) tex.SetPixel(x, y, d < 2.0f ? Color.white : new Color(0f, 0.90f, 1f, 1f));
                }
            }
        }
        else if (tipo == 2)
        {
            // Carcaj de flechas (3 flechas diagonales)
            for (int offset = -3; offset <= 3; offset += 3)
            {
                for (int i = 5; i <= 18; i++)
                {
                    int x = Mathf.Clamp(i + offset, 1, s - 2);
                    int y = Mathf.Clamp(i - offset / 2, 1, s - 2);
                    tex.SetPixel(x, y, new Color(0.92f, 0.78f, 0.42f, 1f));
                }
                int px = Mathf.Clamp(19 + offset, 1, s - 2);
                int py = Mathf.Clamp(19 - offset / 2, 1, s - 2);
                tex.SetPixel(px, py, Color.white);
            }
        }
        else
        {
            // Cristal de Maná
            for (int y = 4; y <= 19; y++)
            {
                int halfW = y <= 12 ? Mathf.RoundToInt(Mathf.Lerp(1f, 6f, (y - 4f) / 8f)) : Mathf.RoundToInt(Mathf.Lerp(6f, 1f, (y - 12f) / 7f));
                for (int dx = -halfW; dx <= halfW; dx++)
                {
                    bool borde = Mathf.Abs(dx) == halfW;
                    Color c = borde ? new Color(0.65f, 0.98f, 1f, 1f) : (dx < 0 ? new Color(0.18f, 0.90f, 1f, 1f) : new Color(0.06f, 0.52f, 0.85f, 1f));
                    tex.SetPixel(12 + dx, y, c);
                }
            }
            for (int y = 9; y <= 14; y++) tex.SetPixel(11, y, Color.white);
        }

        tex.Apply();
        return tex;
    }
}

