using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HUD de Inventario, Zona Activa, Progreso de Ecos y Mochila Gráfica de Objetos Clave.
/// Presenta:
///  • Encabezado de Zona, Ecos recuperados, Oro y Flechas.
///  • Panel gráfico de la Mochila de Ronan con 4 ranuras estilo RPG clásico y texturas reales de sprites:
///      Slot 1: Lingote de Cobre / Lingote Templado (BarCopper.png / BarMithril.png)
///      Slot 2: Esmeralda de San Felipe (GemGreen.png)
///      Slot 3: Llave Dorada del Gremio (GoldKey.png)
///      Slot 4: Reliquia Sagrada / Códice
///  • Notificaciones flotantes en pantalla al intercambiar o recolectar objetos en el Mercado.
/// </summary>
public class InventoryHUD : MonoBehaviour
{
    public static InventoryHUD Instance { get; private set; }

    private static string notificacionTexto = "";
    private static float notificacionTimer = 0f;

    // Cache de texturas de ítems
    private Texture2D texCopper;
    private Texture2D texMithril;
    private Texture2D texGreenGem;
    private Texture2D texGoldKey;
    private Texture2D texRelic;

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
        if (texRelic == null) texRelic = ZoneWaveSpawner.LoadSpriteTexture("LittleTreasureChest");
    }

    private void OnGUI()
    {
        if (Time.timeScale == 0f) return;

        CargarTexturasSiEsNecesario();

        // 1. Zona activa
        string zonaNombre = "Plaza y Atrio";
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
        int totalEcos = (eco1 ? 1 : 0) + (eco2 ? 1 : 0) + (eco3 ? 1 : 0);

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

        string Marca(bool ok) => ok ? "<color=#7CFF8E>✔</color>" : "<color=#9AA6B8>○</color>";

        // Dimensiones del panel superior derecho
        float width = Mathf.Clamp(UITheme.Px(320f), 290f, 360f);
        float height = UITheme.Px(112f);
        float pad = UITheme.Px(10f);

        Rect rect = new Rect(Screen.width - width - UITheme.Px(18f), UITheme.Px(16f), width, height);
        UITheme.Panel(rect, UITheme.BordeDorado, 1.5f);

        // Línea 1: Zona y Ecos
        GUI.Label(new Rect(rect.x + pad, rect.y + UITheme.Px(6f), (width - pad * 2f) * 0.62f, UITheme.Px(18f)),
            $"📍 <b><color=#FFE066>{zonaNombre}</color></b>",
            UITheme.Estilo(12, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white));

        GUI.Label(new Rect(rect.x + pad + (width - pad * 2f) * 0.60f, rect.y + UITheme.Px(6f), (width - pad * 2f) * 0.40f, UITheme.Px(18f)),
            $"✨ <b>ECOS:</b> 1:{Marca(eco1)} 2:{Marca(eco2)} 3:{Marca(eco3)}",
            UITheme.Estilo(11, FontStyle.Bold, TextAnchor.MiddleRight, Color.white));

        // Línea 2: Munición y Oro
        GUI.Label(new Rect(rect.x + pad, rect.y + UITheme.Px(24f), width - pad * 2f, UITheme.Px(16f)),
            $"🏹 <b>{flechas}</b> Flechas     💰 <b>{oro}</b> Monedas de Oro",
            UITheme.Estilo(11, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.92f, 0.95f, 1f)));

        // Línea 3: Título de Mochila
        float bagY = rect.y + UITheme.Px(42f);
        GUI.Label(new Rect(rect.x + pad, bagY, width - pad * 2f, UITheme.Px(16f)),
            "🎒 <b>MOCHILA DE OBJETOS CLAVE:</b>",
            UITheme.Estilo(10, FontStyle.Bold, TextAnchor.MiddleLeft, UITheme.Dorado));

        // 4 Slots de Mochila con estilo RPG clásico
        float slotSize = UITheme.Px(40f);
        float gap = UITheme.Px(8f);
        float startX = rect.x + pad + UITheme.Px(6f);
        float slotY = bagY + UITheme.Px(18f);

        InventoryManager inv = InventoryManager.Instance;
        bool hasCopper = inv != null && inv.HasItem(InventoryManager.ITEM_COPPER_BAR);
        bool hasMithril = inv != null && inv.HasItem(InventoryManager.ITEM_MITHRIL_BAR);
        bool hasGreenGem = inv != null && inv.HasItem(InventoryManager.ITEM_GREEN_GEM);
        bool hasGoldKey = (inv != null && inv.HasItem(InventoryManager.ITEM_GOLD_KEY)) || (inv != null && inv.goldKeysCount > 0);

        // Slot 1: Lingotes (Cobre o Templado)
        Texture2D t1 = hasMithril ? texMithril : (hasCopper ? texCopper : null);
        string name1 = hasMithril ? "Lingote Templado" : (hasCopper ? "Lingote de Cobre" : "Ranura: Lingote");
        DibujarSlotMochila(new Rect(startX, slotY, slotSize, slotSize), t1, hasCopper || hasMithril, name1);

        // Slot 2: Gema de San Felipe
        Texture2D t2 = hasGreenGem ? texGreenGem : null;
        DibujarSlotMochila(new Rect(startX + (slotSize + gap), slotY, slotSize, slotSize), t2, hasGreenGem, "Esmeralda de San Felipe");

        // Slot 3: Llave Dorada del Gremio
        Texture2D t3 = hasGoldKey ? texGoldKey : null;
        DibujarSlotMochila(new Rect(startX + (slotSize + gap) * 2f, slotY, slotSize, slotSize), t3, hasGoldKey, "Llave Dorada del Gremio");

        // Slot 4: Reliquia / Códice
        bool hasEco2 = eco2 || (inv != null && inv.HasItem("Eco de la Tradición"));
        Texture2D t4 = hasEco2 ? texRelic : null;
        DibujarSlotMochila(new Rect(startX + (slotSize + gap) * 3f, slotY, slotSize, slotSize), t4, hasEco2, "Reliquia Sagrada");

        // 5. Notificación flotante de trueque (centro superior)
        if (notificacionTimer > 0f && !string.IsNullOrEmpty(notificacionTexto))
        {
            float notifW = Mathf.Clamp(UITheme.Px(580f), 450f, Screen.width - 40f);
            float notifH = UITheme.Px(38f);
            float notifX = (Screen.width - notifW) * 0.5f;
            float notifY = UITheme.Px(20f);

            Rect rNotif = new Rect(notifX, notifY, notifW, notifH);
            UITheme.Panel(rNotif, UITheme.Turquesa, 2f);
            GUI.Label(rNotif, notificacionTexto, UITheme.Estilo(14, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
        }
    }

    private void DibujarSlotMochila(Rect r, Texture2D tex, bool activo, string tooltip)
    {
        Color prev = GUI.color;

        // Fondo del recuadro
        GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);
        GUI.DrawTexture(r, UITheme.Blanca);

        // Marco: dorado brillante si tiene ítem, bronce tenue si está vacío
        Color borde = activo ? new Color(1f, 0.85f, 0.35f, 0.95f) : new Color(0.35f, 0.32f, 0.28f, 0.70f);
        UITheme.Borde(r, borde, 1.5f);

        if (activo && tex != null)
        {
            // Sutil resplandor cálido en el fondo del ítem
            GUI.color = new Color(1f, 0.82f, 0.25f, 0.18f);
            GUI.DrawTexture(new Rect(r.x + 2f, r.y + 2f, r.width - 4f, r.height - 4f), UITheme.Blanca);

            // Textura del sprite con aspect ratio correcto
            GUI.color = Color.white;
            float iconPad = 4f;
            Rect rIcon = new Rect(r.x + iconPad, r.y + iconPad, r.width - iconPad * 2f, r.height - iconPad * 2f);
            GUI.DrawTexture(rIcon, tex, ScaleMode.ScaleToFit);
        }
        else
        {
            // Ranura vacía: silueta sutil o punto atenuado
            GUI.color = new Color(1f, 1f, 1f, 0.12f);
            if (tex != null)
            {
                float iconPad = 5f;
                Rect rIcon = new Rect(r.x + iconPad, r.y + iconPad, r.width - iconPad * 2f, r.height - iconPad * 2f);
                GUI.DrawTexture(rIcon, tex, ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.Label(r, "○", UITheme.Estilo(14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.25f)));
            }
        }

        // Tooltip al pasar el mouse por encima del slot
        if (r.Contains(Event.current.mousePosition))
        {
            GUI.color = Color.white;
            float tw = UITheme.Px(180f);
            float th = UITheme.Px(22f);
            Rect rTip = new Rect(Event.current.mousePosition.x - tw * 0.5f, r.y + r.height + 4f, tw, th);
            GUI.color = new Color(0.04f, 0.06f, 0.10f, 0.95f);
            GUI.DrawTexture(rTip, UITheme.Blanca);
            UITheme.Borde(rTip, UITheme.BordeDorado, 1f);
            GUI.color = Color.white;
            GUI.Label(rTip, tooltip, UITheme.Estilo(10, FontStyle.Bold, TextAnchor.MiddleCenter, activo ? UITheme.Dorado : Color.gray));
        }

        GUI.color = prev;
    }
}
