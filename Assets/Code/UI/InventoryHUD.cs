using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HUD de Inventario, Zona Activa y Progreso de los 3 Ecos Sagrados de San Felipe.
/// Compacto y anclado estrictamente a la esquina superior derecha para no obstaculizar el centro del mapa.
/// Muestra zona actual, estado de Ecos (Fe, Tradición y Memoria), flechas, oro y objetos de trueque.
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

        string bolsaItems = "Vacía";
        if (InventoryManager.Instance != null)
        {
            IReadOnlyList<string> items = InventoryManager.Instance.GetItems();
            if (items != null && items.Count > 0)
            {
                bolsaItems = string.Join(", ", items);
            }
        }

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

        float width = UITheme.Px(260f); // Escala compactada a 260px (libera el centro de la pantalla)
        float height = UITheme.Px(72f);  // Altura estilizada de 72px
        float pad = UITheme.Px(7f);

        // Anclado estrictamente a la esquina superior derecha
        Rect rect = new Rect(Screen.width - width - UITheme.Px(8f), UITheme.Px(8f), width, height);
        UITheme.Panel(rect, UITheme.BordeDorado, 1.5f);

        string textoHUD =
            $"📍 <b><color=#FFE066>{zonaNombre}</color></b>\n" +
            $"✨ <b>ECOS ({totalEcos}/3):</b> 1:{Marca(eco1)} 2:{Marca(eco2)} 3:{Marca(eco3)}\n" +
            $"🏹 <b>{flechas}</b>   💰 <b>{oro}</b> oro   🎒 <b>{bolsaItems}</b>";

        GUI.Label(new Rect(rect.x + pad, rect.y + UITheme.Px(4f), rect.width - pad * 2f, rect.height - UITheme.Px(8f)),
            textoHUD, UITheme.Estilo(11, FontStyle.Normal, TextAnchor.UpperLeft, Color.white, true));
    }
}
