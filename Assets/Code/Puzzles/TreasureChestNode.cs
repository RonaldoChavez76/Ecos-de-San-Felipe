using UnityEngine;

public class TreasureChestNode : MonoBehaviour
{
    public enum RewardType
    {
        Gold,
        Arrows,
        Mana
    }

    private static Sprite spriteCerrado;
    private static Sprite spriteAbierto;

    public string chestId;
    public RewardType rewardType;
    public int rewardAmount;
    public int goldCost;

    private SpriteRenderer chestRenderer;
    private bool opened;
    private float nextInteractionTime;

    public bool Opened => opened;

    public void RestaurarEstadoCheckpoint(bool wasOpened)
    {
        opened = wasOpened;
        ActualizarVisual();
    }

    public static TreasureChestNode Crear(
        string id,
        Vector2 position,
        Transform parent,
        RewardType reward,
        int amount,
        int cost = 0)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogError("[TreasureChestNode] El cofre necesita un identificador persistente.");
            return null;
        }

        EnsureSprites();
        GameObject chest = new GameObject($"Cofre_{id.Replace('.', '_')}");
        chest.transform.SetParent(parent);
        chest.transform.position = position;

        SpriteRenderer renderer = chest.AddComponent<SpriteRenderer>();
        renderer.sprite = spriteCerrado;
        renderer.sortingOrder = Mathf.RoundToInt(-position.y * 10f) + 1;

        BoxCollider2D collider = chest.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.9f, 0.55f);
        collider.offset = new Vector2(0f, -0.12f);

        TreasureChestNode node = chest.AddComponent<TreasureChestNode>();
        node.chestId = id;
        node.rewardType = reward;
        node.rewardAmount = Mathf.Max(1, amount);
        node.goldCost = Mathf.Max(0, cost);
        node.chestRenderer = renderer;
        return node;
    }

    private static void EnsureSprites()
    {
        if (spriteCerrado != null && spriteAbierto != null) return;
        MercadoBuilder.CargarCofreSprites("LittleTreasureChest", out spriteCerrado, out spriteAbierto);
    }

    private void Start()
    {
        if (chestRenderer == null) chestRenderer = GetComponent<SpriteRenderer>();
        SaveStateManager save = SaveStateManager.Instance;
        opened = save != null && save.EstaCofreAbierto(chestId);
        ActualizarVisual();
    }

    private void Update()
    {
        if (opened || Time.time < nextInteractionTime) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
        if (player == null || Vector2.Distance(transform.position, player.transform.position) > 1.8f) return;
        MobileInput.ReportInteractableNearby("Abrir");
        if (!MobileInput.GetInteractDown(KeyCode.E)) return;

        nextInteractionTime = Time.time + 0.35f;
        Abrir(player);
    }

    private void Abrir(PlayerMovement player)
    {
        if (opened || player == null) return;
        if (!PuedeRecibirRecompensa(player))
        {
            player.MostrarAvisoRecurso("Libera espacio en tus provisiones antes de abrir el cofre.");
            return;
        }

        opened = true;
        if (goldCost > 0)
        {
            SaveStateManager save = SaveStateManager.Instance;
            if (save == null || player.oro < goldCost)
            {
                opened = false;
                player.MostrarAvisoRecurso($"Necesitas {goldCost} de oro para abrir este cofre.");
                return;
            }

            save.oro = player.oro;
            if (!save.GastarOro(goldCost))
            {
                opened = false;
                player.MostrarAvisoRecurso($"Necesitas {goldCost} de oro para abrir este cofre.");
                return;
            }
            player.oro = save.oro;
        }

        ActualizarVisual();
        switch (rewardType)
        {
            case RewardType.Gold:
                player.AgregarOro(rewardAmount);
                break;
            case RewardType.Arrows:
                player.AgregarFlechas(rewardAmount);
                player.MostrarAvisoRecurso($"+{rewardAmount} Flechas");
                break;
            case RewardType.Mana:
                player.RestaurarMana(rewardAmount);
                player.MostrarAvisoRecurso($"+{rewardAmount} Maná");
                break;
        }

        AudioManager.Instance?.PlaySecretSound();
        SaveStateManager.Instance?.RegistrarEstadoMundoActual();
        string rewardDescription = rewardType == RewardType.Gold
            ? $"{rewardAmount} monedas de oro"
            : rewardType == RewardType.Arrows
                ? $"{rewardAmount} flechas"
                : $"{rewardAmount} puntos de maná";
        DialogueManager.EnsureInstance().ShowTimedDialogue(
            "Cofre de provisiones",
            new[] { $"Encontraste {rewardDescription}." },
            3.5f);
    }

    private bool PuedeRecibirRecompensa(PlayerMovement player)
    {
        switch (rewardType)
        {
            case RewardType.Arrows:
                return player.flechas < player.maxFlechas;
            case RewardType.Mana:
                return player.manaActual < player.manaMaximo;
            default:
                return true;
        }
    }

    private void ActualizarVisual()
    {
        if (chestRenderer != null)
            chestRenderer.sprite = opened ? spriteAbierto : spriteCerrado;
    }

    private void OnGUI()
    {
        if (opened) return;
        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
        if (player == null || Vector2.Distance(transform.position, player.transform.position) > 1.8f) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        Camera camera = Camera.main;
        if (camera == null) return;
        Vector3 screenPosition = camera.WorldToScreenPoint(transform.position + Vector3.up * 0.8f);
        if (screenPosition.z <= 0f) return;

        float width = UITheme.Px(220f);
        float height = UITheme.Px(30f);
        Rect prompt = new Rect(screenPosition.x - width * 0.5f, Screen.height - screenPosition.y - height * 0.5f, width, height);
        UITheme.Panel(prompt, UITheme.Dorado, 2f);
        bool esTactil = MobileInput.IsTouchControlsActive;
        string promptText = goldCost > 0
            ? (esTactil ? $"Abrir cofre ({goldCost} oro)" : $"[E] Abrir cofre ({goldCost} oro)")
            : (esTactil ? "Abrir cofre" : "[E] Abrir cofre");
        GUI.Label(prompt, promptText, UITheme.Estilo(12, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
    }
}
