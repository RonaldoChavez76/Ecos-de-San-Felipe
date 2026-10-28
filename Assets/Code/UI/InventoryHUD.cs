using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HUD de Inventario, Zona Activa y Progreso de los 3 Ecos Sagrados de San Felipe.
/// Muestra en la esquina superior derecha la zona actual, llaves doradas, objetos de trueque
/// del Mercado y el estado de los Ecos (Fe, Tradición y Memoria).
/// </summary>
public class InventoryHUD : MonoBehaviour
{
    public static InventoryHUD Instance { get; private set; }

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

    private void OnGUI()
    {
        if (Time.timeScale == 0f) return;

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 12,
            padding = new RectOffset(10, 10, 8, 8)
        };
        boxStyle.normal.textColor = new Color(0.96f, 0.92f, 0.80f);

        string zonaNombre = "Plaza y Atrio de la Parroquia";
        if (LevelManager.Instance != null)
        {
            switch (LevelManager.Instance.currentZone)
            {
                case LevelManager.ZoneId.ParroquiaExterior:
                    zonaNombre = "Nivel 1: Plaza y Atrio Parroquial";
                    break;
                case LevelManager.ZoneId.ParroquiaInterior:
                    zonaNombre = "Nivel 1: Interior de la Parroquia";
                    break;
                case LevelManager.ZoneId.Mercado:
                    zonaNombre = "Nivel 2: Mercado Municipal y Callejones";
                    break;
                case LevelManager.ZoneId.CerroDelFraile:
                    zonaNombre = "Nivel 3: Cerro del Fraile — Cruz Azul";
                    break;
            }
        }

        bool eco1 = SaveStateManager.Instance != null && SaveStateManager.Instance.ecoFeRecuperado;
        bool eco2 = SaveStateManager.Instance != null && SaveStateManager.Instance.ecoTradicionRecuperado;
        bool eco3 = SaveStateManager.Instance != null && SaveStateManager.Instance.ecoMemoriaRecuperado;
        int totalEcos = (eco1 ? 1 : 0) + (eco2 ? 1 : 0) + (eco3 ? 1 : 0);

        string bolsaItems = "Vacía";
        if (InventoryManager.Instance != null)
        {
            IReadOnlyList<string> items = InventoryManager.Instance.GetItems();
            if (items != null && items.Count > 0)
            {
                bolsaItems = string.Join(", ", items);
            }
        }

        string textoHUD =
            $"<b>ZONA:</b> {zonaNombre}\n" +
            $"<b>ECOS DE SAN FELIPE ({totalEcos}/3):</b>\n" +
            $"  [{(eco1 ? "✓" : " ")}] 1. Eco de la Fe (Parroquia)\n" +
            $"  [{(eco2 ? "✓" : " ")}] 2. Eco de la Tradición (Mercado)\n" +
            $"  [{(eco3 ? "✓" : " ")}] 3. Eco de la Memoria (Cruz Azul)\n" +
            $"<b>BOLSA / TRUEQUE:</b> {bolsaItems}";

        float width = 310f;
        float height = 118f;
        Rect rect = new Rect(Screen.width - width - 16f, 16f, width, height);
        GUI.Box(rect, textoHUD, boxStyle);
    }
}

