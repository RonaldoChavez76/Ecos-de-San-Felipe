using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HUD de Inventario, Zona Activa, Exhibidor Visual de los 3 Ecos Sagrados y Mochila Gráfica de Objetos Clave.
/// Presenta:
///  • Anclaje seguro en esquina superior derecha: Rect(Screen.width - width - UITheme.Px(18f), UITheme.Px(14f), width, height).
///  • Exhibidor Visual de los 3 Ecos Sagrados ([1. Fe] | [2. Tradición] | [3. Memoria]) con orbes celestes (#00E5FF)
///    y destellos dorados al estar recuperados, o silueta apagada (#445566) con número cuando faltan.
///  • Mochila Gráfica estilo Zelda con 4 ranuras cuadradas de borde de cantera/oro y texturas reales:
///      Slot 1: Lingote de Cobre (BarCopper.png) / Lingote Templado (BarMithril.png)
///      Slot 2: Esmeralda de San Felipe (GemGreen.png)
///      Slot 3: Llave Dorada del Gremio (GoldKey.png)
///      Slot 4: Reserva de Oro / Corazón Sagrado (Heart.png / BarGold.png)
///  • Notificaciones flotantes en pantalla al intercambiar o recolectar objetos clave.
/// </summary>
public class InventoryHUD : MonoBehaviour
{
    public static InventoryHUD Instance { get; private set; }

    private static string notificacionTexto = "";
    private static float notificacionTimer = 0f;

    // Cache de texturas reales de ítems
    private Texture2D texCopper;
    private Texture2D texMithril;
    private Texture2D texGreenGem;
    private Texture2D texGoldKey;
    private Texture2D texHeart;
    private Texture2D texGoldBar;

    // Texturas procedurales de los medallones circulares de los 3 Ecos
    private Texture2D texEcoActivo;
    private Texture2D texEcoInactivo;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public static InventoryHUD EnsureInstance()
    {
        if (Instance != null) return Instance;
        InventoryHUD existing = FindAnyObjectByType<InventoryHUD>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }
        GameObject go = new GameObject("InventoryHUD");
        Instance = go.AddComponent<InventoryHUD>();
        return Instance;
    }

    /// <summary>
    /// Muestra un banner flotante en el HUD anunciando el intercambio o entrega de un objeto clave.
    /// </summary>
    public static void NotificarIntercambio(string mensaje)
    {
        notificacionTexto = mensaje;
        notificacionTimer = 3.8f;
        AudioManager.Instance?.PlayItemPickupSound();
    }

    private void Update()
    {
        if (notificacionTimer > 0f)
        {
            notificacionTimer -= Time.deltaTime;
        }
    }

    private void CargarTexturasSiEsNecesario()
    {
        if (texCopper == null) texCopper = ZoneWaveSpawner.LoadSpriteTexture("BarCopper");
        if (texMithril == null) texMithril = ZoneWaveSpawner.LoadSpriteTexture("BarMithril");
        if (texGreenGem == null) texGreenGem = ZoneWaveSpawner.LoadSpriteTexture("GemGreen");
        if (texGoldKey == null) texGoldKey = ZoneWaveSpawner.LoadSpriteTexture("GoldKey");
        if (texHeart == null) texHeart = ZoneWaveSpawner.LoadSpriteTexture("Heart");
        if (texGoldBar == null) texGoldBar = ZoneWaveSpawner.LoadSpriteTexture("BarGold");

        if (texEcoActivo == null) texEcoActivo = CrearTexturaOrbeEco(true);
        if (texEcoInactivo == null) texEcoInactivo = CrearTexturaOrbeEco(false);
    }

    private static Texture2D CrearTexturaOrbeEco(bool recuperado)
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float maxR = size * 0.46f;

        Color colExterior = recuperado ? new Color(0f, 0.90f, 1f, 1f) : new Color(0.27f, 0.33f, 0.40f, 0.85f); // #00E5FF o #445566
        Color colNucleo = recuperado ? new Color(0.85f, 0.98f, 1f, 1f) : new Color(0.14f, 0.18f, 0.22f, 0.92f);
        Color colBordeOro = recuperado ? new Color(1f, 0.88f, 0.35f, 1f) : new Color(0.27f, 0.33f, 0.40f, 0.95f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                if (d > maxR)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else if (d >= maxR - 2.1f)
                {
                    tex.SetPixel(x, y, colBordeOro);
                }
                else
                {
                    float t = d / (maxR - 2.1f);
                    Color c = Color.Lerp(colNucleo, colExterior, t);
                    // Destellos dorados en cruz para el orbe recuperado
                    if (recuperado && ((Mathf.Abs(x - (int)center.x) <= 1 && d < maxR * 0.72f) || (Mathf.Abs(y - (int)center.y) <= 1 && d < maxR * 0.72f)))
                    {
                        c = Color.Lerp(c, new Color(1f, 0.95f, 0.55f, 1f), 0.55f);
                    }
                    tex.SetPixel(x, y, c);
                }
            }
        }
        tex.Apply();
        tex.hideFlags = HideFlags.HideAndDontSave;
        return tex;
    }

    private void OnGUI()
    {
        if (Time.timeScale == 0f) return;

        CargarTexturasSiEsNecesario();

        // 1. Nombre de la Zona Activa
        string zonaNombre = "Nivel 1: Atrio Parroquial";
        if (LevelManager.Instance != null)
        {
            switch (LevelManager.Instance.currentZone)
            {
                case LevelManager.ZoneId.ParroquiaExterior:
                    zonaNombre = "Nivel 1: Atrio Parroquial";
                    break;
                case LevelManager.ZoneId.ParroquiaInterior:
                    zonaNombre = "Nivel 1: Interior Parroquia";
                    break;
                case LevelManager.ZoneId.Mercado:
                    zonaNombre = "Nivel 2: Mercado Municipal";
                    break;
                case LevelManager.ZoneId.CerroDelFraile:
                    zonaNombre = "Nivel 3: Cerro del Fraile";
                    break;
            }
        }

        bool eco1 = SaveStateManager.Instance != null && SaveStateManager.Instance.ecoFeRecuperado;
        bool eco2 = SaveStateManager.Instance != null && SaveStateManager.Instance.ecoTradicionRecuperado;
        bool eco3 = SaveStateManager.Instance != null && SaveStateManager.Instance.ecoMemoriaRecuperado;

        int flechas = 0;
        int oro = 0;
        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
        {
            flechas = pm.flechas;
            oro = pm.oro;
        }
        else if (SaveStateManager.Instance != null)
        {
            oro = SaveStateManager.Instance.oro;
        }

        // 1. Margen Seguro (Safe Area) contra bordes cortados y ancho proporcional
        float safeMarginX = Mathf.Max(UITheme.Px(26f), Screen.width * 0.03f);
        float safeMarginY = Mathf.Max(UITheme.Px(22f), Screen.height * 0.03f);
        float pad = UITheme.Px(14f);

        float width = Mathf.Clamp(UITheme.Px(340f), 310f, Screen.width * 0.35f);
        float innerW = width - pad * 2f;

        float row1H = UITheme.Px(18f);
        float ecoBoxH = UITheme.Px(32f);
        float bagTitleH = UITheme.Px(15f);
        float slotGap = UITheme.Px(8f);
        float slotSize = Mathf.Min(UITheme.Px(46f), (innerW - slotGap * 3f) / 4f);
        float gapY = UITheme.Px(6f);

        float height = pad * 2f + row1H + gapY + ecoBoxH + gapY + bagTitleH + UITheme.Px(4f) + slotSize;

        Rect rect = new Rect(Screen.width - width - safeMarginX, safeMarginY, width, height);
        UITheme.Panel(rect, UITheme.BordeDorado, 1.5f);

        // Fila 1: Zona Activa y Recursos Rápidos (Oro y Flechas) dentro de innerW
        float row1Y = rect.y + pad;
        GUI.Label(new Rect(rect.x + pad, row1Y, innerW * 0.60f, row1H),
            $"📍 <b><color=#FFE066>{zonaNombre}</color></b>",
            UITheme.Estilo(11, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white));

        GUI.Label(new Rect(rect.x + pad + innerW * 0.56f, row1Y, innerW * 0.44f, row1H),
            $"🏹 <b>{flechas}</b>  💰 <b>{oro}</b>",
            UITheme.Estilo(11, FontStyle.Bold, TextAnchor.MiddleRight, new Color(1f, 0.92f, 0.55f)));

        // Fila 2: Exhibidor Visual de los 3 ECOS SAGRADOS ([1. Fe] | [2. Tradición] | [3. Memoria])
        float ecosY = row1Y + row1H + gapY;
        float ecoGap = UITheme.Px(5f);
        float ecoBoxW = (innerW - ecoGap * 2f) / 3f;

        DibujarCasillaEco(new Rect(rect.x + pad, ecosY, ecoBoxW, ecoBoxH), 1, "1. Fe", eco1);
        DibujarCasillaEco(new Rect(rect.x + pad + ecoBoxW + ecoGap, ecosY, ecoBoxW, ecoBoxH), 2, "2. Tradición", eco2);
        DibujarCasillaEco(new Rect(rect.x + pad + (ecoBoxW + ecoGap) * 2f, ecosY, ecoBoxW, ecoBoxH), 3, "3. Memoria", eco3);

        // Fila 3: Encabezado de Mochila Gráfica
        float bagTitleY = ecosY + ecoBoxH + gapY;
        GUI.Label(new Rect(rect.x + pad, bagTitleY, innerW, bagTitleH),
            "🎒 <b>MOCHILA DE OBJETOS CLAVE:</b>",
            UITheme.Estilo(10, FontStyle.Bold, TextAnchor.MiddleLeft, UITheme.Dorado));

        // Fila 4: 4 Ranuras cuadradas tipo Zelda distribuidas equitativamente dentro de innerW
        float eqGap = (innerW - slotSize * 4f) / 3f;
        float slotY = bagTitleY + bagTitleH + UITheme.Px(4f);

        InventoryManager inv = InventoryManager.Instance;
        bool hasCopper = inv != null && inv.HasItem(InventoryManager.ITEM_COPPER_BAR);
        bool hasMithril = inv != null && inv.HasItem(InventoryManager.ITEM_MITHRIL_BAR);
        bool hasGreenGem = inv != null && inv.HasItem(InventoryManager.ITEM_GREEN_GEM);
        bool hasGoldKey = (inv != null && inv.HasItem(InventoryManager.ITEM_GOLD_KEY)) || (inv != null && inv.goldKeysCount > 0);

        // Slot 1: BarCopper.png o BarMithril.png
        Texture2D t1 = hasMithril ? texMithril : texCopper;
        string lbl1 = hasMithril ? "Lingote Templado" : (hasCopper ? "Lingote de Cobre" : "Vacío: Lingote");
        string sub1 = hasMithril ? "Templado" : "Cobre";
        DibujarSlotMochila(new Rect(rect.x + pad, slotY, slotSize, slotSize), t1, hasCopper || hasMithril, lbl1, sub1);

        // Slot 2: GemGreen.png
        DibujarSlotMochila(new Rect(rect.x + pad + (slotSize + eqGap), slotY, slotSize, slotSize), texGreenGem, hasGreenGem, "Esmeralda de San Felipe", "Esmeralda");

        // Slot 3: GoldKey.png
        DibujarSlotMochila(new Rect(rect.x + pad + (slotSize + eqGap) * 2f, slotY, slotSize, slotSize), texGoldKey, hasGoldKey, "Llave Dorada del Gremio", "Llave Oro");

        // Slot 4: BarGold.png o Heart.png
        bool tieneReservaOro = oro > 0;
        Texture2D t4 = texGoldBar != null ? texGoldBar : texHeart;
        string lbl4 = $"Reserva: {oro} Oro / Salud";
        DibujarSlotMochila(new Rect(rect.x + pad + (slotSize + eqGap) * 3f, slotY, slotSize, slotSize), t4, tieneReservaOro, lbl4, $"{oro} Oro");

        // 5. Notificación flotante de trueque (centro superior)
        if (notificacionTimer > 0f && !string.IsNullOrEmpty(notificacionTexto))
        {
            float notifW = Mathf.Clamp(UITheme.Px(580f), 420f, Screen.width - safeMarginX * 2f);
            float notifH = UITheme.Px(38f);
            float notifX = (Screen.width - notifW) * 0.5f;
            float notifY = UITheme.Px(58f);

            Rect rNotif = new Rect(notifX, notifY, notifW, notifH);
            UITheme.Panel(rNotif, UITheme.Turquesa, 2f);
            GUI.Label(rNotif, notificacionTexto, UITheme.Estilo(13, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
        }
    }

    private void DibujarCasillaEco(Rect r, int numero, string etiqueta, bool recuperado)
    {
        Color prev = GUI.color;

        // Fondo de la casilla
        GUI.color = recuperado ? new Color(0.03f, 0.16f, 0.24f, 0.92f) : new Color(0.07f, 0.09f, 0.13f, 0.88f);
        GUI.DrawTexture(r, UITheme.Blanca);

        // Borde: celeste/dorado si fue recuperado (#00E5FF), gris azulado (#445566) si falta
        Color colBorde = recuperado ? new Color(0f, 0.90f, 1f, 0.95f) : new Color(0.27f, 0.33f, 0.40f, 0.85f);
        UITheme.Borde(r, colBorde, 1.5f);

        // Orbe circular compacto a la izquierda para dejar amplio espacio al texto
        float orbSize = Mathf.Min(r.height - UITheme.Px(10f), UITheme.Px(20f));
        Rect rOrb = new Rect(r.x + UITheme.Px(4f), r.y + (r.height - orbSize) * 0.5f, orbSize, orbSize);
        GUI.color = Color.white;
        Texture2D orbTex = recuperado ? texEcoActivo : texEcoInactivo;
        if (orbTex != null)
        {
            GUI.DrawTexture(rOrb, orbTex, ScaleMode.ScaleToFit);
        }

        if (!recuperado)
        {
            // Número del eco visible sobre la silueta atenuada (#445566)
            GUI.Label(rOrb, numero.ToString(), UITheme.Estilo(9, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.72f, 0.80f, 0.90f, 0.92f)));
        }

        // Texto del Eco a la derecha del orbe con margen suficiente para no cortarse
        Rect rTxt = new Rect(rOrb.xMax + UITheme.Px(3f), r.y, r.xMax - rOrb.xMax - UITheme.Px(5f), r.height);
        Color txtColor = recuperado ? new Color(0f, 0.90f, 1f, 1f) : new Color(0.56f, 0.65f, 0.75f, 0.95f);
        GUI.Label(rTxt, etiqueta, UITheme.Estilo(9, FontStyle.Bold, TextAnchor.MiddleLeft, txtColor));

        GUI.color = prev;
    }

    private void DibujarSlotMochila(Rect r, Texture2D tex, bool activo, string tooltip, string etiquetaCorta)
    {
        Color prev = GUI.color;

        // Fondo oscuro de cantera
        GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);
        GUI.DrawTexture(r, UITheme.Blanca);

        // Marco de cantera/oro: dorado intenso si está activo, piedra bronceada si está vacío
        Color borde = activo ? new Color(1f, 0.86f, 0.32f, 0.98f) : new Color(0.42f, 0.38f, 0.32f, 0.78f);
        UITheme.Borde(r, borde, 1.5f);

        float iconPad = UITheme.Px(5f);
        Rect rIcon = new Rect(r.x + iconPad, r.y + UITheme.Px(3f), r.width - iconPad * 2f, r.height - UITheme.Px(14f));

        if (activo && tex != null)
        {
            // Resplandor cálido detrás del objeto activo
            GUI.color = new Color(1f, 0.85f, 0.28f, 0.20f);
            GUI.DrawTexture(new Rect(r.x + 2f, r.y + 2f, r.width - 4f, r.height - 4f), UITheme.Blanca);

            GUI.color = Color.white;
            GUI.DrawTexture(rIcon, tex, ScaleMode.ScaleToFit);
        }
        else if (tex != null)
        {
            // Icono translúcido atenuado indicando qué objeto va en esta ranura
            GUI.color = new Color(0.55f, 0.65f, 0.78f, 0.24f);
            GUI.DrawTexture(rIcon, tex, ScaleMode.ScaleToFit);
        }

        // Etiqueta inferior compacta dentro de la ranura
        Rect rSub = new Rect(r.x + 1f, r.yMax - UITheme.Px(12f), r.width - 2f, UITheme.Px(11f));
        Color colSub = activo ? new Color(1f, 0.92f, 0.55f, 1f) : new Color(0.55f, 0.62f, 0.70f, 0.75f);
        GUI.color = Color.white;
        GUI.Label(rSub, etiquetaCorta, UITheme.Estilo(9, FontStyle.Bold, TextAnchor.MiddleCenter, colSub));

        // Tooltip al pasar el cursor
        if (r.Contains(Event.current.mousePosition))
        {
            float tw = UITheme.Px(195f);
            float th = UITheme.Px(22f);
            float tx = Mathf.Clamp(Event.current.mousePosition.x - tw * 0.5f, 10f, Screen.width - tw - 10f);
            Rect rTip = new Rect(tx, r.y + r.height + 4f, tw, th);
            GUI.color = new Color(0.04f, 0.06f, 0.10f, 0.96f);
            GUI.DrawTexture(rTip, UITheme.Blanca);
            UITheme.Borde(rTip, UITheme.BordeDorado, 1f);
            GUI.color = Color.white;
            GUI.Label(rTip, tooltip, UITheme.Estilo(10, FontStyle.Bold, TextAnchor.MiddleCenter, activo ? UITheme.Dorado : Color.gray));
        }

        GUI.color = prev;
    }
}
