using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema de inventario de objetos clave, lingotes, gemas y llaves de Ronan
/// para resolver acertijos como el Trueque Artesanal del Mercado Municipal.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public const string ITEM_COPPER_BAR = "Lingote de Cobre";
    public const string ITEM_MITHRIL_BAR = "Lingote Templado";
    public const string ITEM_GREEN_GEM = "Esmeralda de San Felipe";
    public const string ITEM_GOLD_KEY = "Llave Dorada del Gremio";
    public const string ITEM_RED_GEM = "Rubí de Cantera";

    [Header("Estado del Inventario")]
    [SerializeField] private List<string> itemsPoseidos = new List<string>();
    public int goldKeysCount = 0;

    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public static InventoryManager EnsureInstance()
    {
        if (Instance != null) return Instance;
        InventoryManager existing = FindAnyObjectByType<InventoryManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }
        GameObject go = new GameObject("InventoryManager");
        Instance = go.AddComponent<InventoryManager>();
        return Instance;
    }

    public void AddItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return;

        if (!itemsPoseidos.Contains(itemName))
        {
            itemsPoseidos.Add(itemName);
        }

        if (itemName == ITEM_GOLD_KEY)
        {
            goldKeysCount++;
            SaveStateManager.Instance?.ModificarLlaves(1);
        }

        AudioManager.Instance?.PlaySecretSound();
        OnInventoryChanged?.Invoke();
        Debug.Log($"<color=#FFD700>[InventoryManager] Objeto obtenido: {itemName}</color>");
    }

    public bool HasItem(string itemName)
    {
        return itemsPoseidos.Contains(itemName);
    }

    public bool ConsumeItem(string itemName)
    {
        if (!itemsPoseidos.Contains(itemName)) return false;

        itemsPoseidos.Remove(itemName);
        if (itemName == ITEM_GOLD_KEY && goldKeysCount > 0)
        {
            goldKeysCount--;
            SaveStateManager.Instance?.ModificarLlaves(-1);
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public IReadOnlyList<string> GetItems()
    {
        return itemsPoseidos;
    }
}

