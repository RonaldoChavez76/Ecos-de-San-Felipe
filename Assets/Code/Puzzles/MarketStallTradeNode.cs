using UnityEngine;

/// <summary>
/// Nodo interactivo para cada puesto de trueque del Mercado o para el Cofre del Gremio.
/// </summary>
public class MarketStallTradeNode : MonoBehaviour
{
    public enum NodeRole
    {
        SupplyCrateCopper,
        BlacksmithStall,
        PotterStall,
        JewelerStall,
        GuildTreasureChest
    }

    public NodeRole role = NodeRole.SupplyCrateCopper;
    public string labelPrompt = "[E] Comerciar en Puesto";
    public MarketTradePuzzle puzzlePadre;
    public SpriteRenderer chestRenderer;
    public Sprite spriteCerrado;
    public Sprite spriteAbierto;

    private Transform playerTransform;
    private bool completado = false;
    private float cooldown = 0f;

    private void Update()
    {
        if (cooldown > 0f) cooldown -= Time.deltaTime;

        if (playerTransform == null)
        {
            PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
            if (pm != null) playerTransform = pm.transform;
            return;
        }

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist <= 1.95f && cooldown <= 0f && Input.GetKeyDown(KeyCode.E))
        {
            cooldown = 0.4f;
            EjecutarInteraccion();
        }
    }

    private void EjecutarInteraccion()
    {
        InventoryManager inv = InventoryManager.EnsureInstance();

        switch (role)
        {
            case NodeRole.SupplyCrateCopper:
                if (completado || inv.HasItem(InventoryManager.ITEM_COPPER_BAR))
                {
                    if (chestRenderer != null && spriteAbierto != null)
                    {
                        chestRenderer.sprite = spriteAbierto;
                    }
                    DialogueManager.EnsureInstance().ShowDialogue(
                        "Arcón de la Antigua Fragua",
                        new string[] { "Ya recogiste el Lingote de Cobre Antiguo. Llévaselo a Don Braulio en la Herrería del Noroeste." });
                }
                else
                {
                    completado = true;
                    if (chestRenderer != null && spriteAbierto != null)
                    {
                        chestRenderer.sprite = spriteAbierto;
                    }
                    AudioManager.Instance?.PlayItemPickupSound();
                    inv.AddItem(InventoryManager.ITEM_COPPER_BAR);
                    InventoryHUD.NotificarIntercambio("¡Obtuviste [Lingote de Cobre Antiguo]!");
                    ResourcePickup.SpawnDrops(transform.position + Vector3.down * 0.7f, 1f, 35, 60, 1f, 6, 0.8f, 30f, 0.8f, 30f);
                    DialogueManager.EnsureInstance().ShowDialogue(
                        "Arcón de la Antigua Fragua",
                        new string[]
                        {
                            "¡Has encontrado un [Lingote de Cobre] intacto y provisiones entre las cajas del mercado!",
                            "Llévalo al puesto de Don Braulio, el Herrero de Cantera, al Noroeste de la plaza del mercado."
                        });
                }
                break;

            case NodeRole.BlacksmithStall:
                if (completado)
                {
                    DialogueManager.EnsureInstance().ShowDialogue(
                        "Don Braulio, Herrero de Cantera",
                        new string[] { "Lleva ese Lingote Templado con Doña Remedios en el puesto de Alfarería al Noreste." });
                }
                else if (inv.ConsumeItem(InventoryManager.ITEM_COPPER_BAR))
                {
                    completado = true;
                    inv.AddItem(InventoryManager.ITEM_MITHRIL_BAR);
                    InventoryHUD.NotificarIntercambio("¡Intercambio: Lingote de Cobre -> [Lingote Templado]!");
                    DialogueManager.EnsureInstance().ShowDialogue(
                        "Don Braulio, Herrero de Cantera",
                        new string[]
                        {
                            "«¡Cobre legítimo de las minas antiguas! Justo lo que necesitaba para encender mi fragua.»",
                            "«Toma a cambio este [Lingote Templado] de plata pura. Doña Remedios, la Alfarera del puesto Noreste, lo busca para sus esmaltes ceremoniales.»"
                        });
                }
                else
                {
                    DialogueManager.EnsureInstance().ShowDialogue(
                        "Don Braulio, Herrero de Cantera",
                        new string[]
                        {
                            "«La niebla apagó mis hornos, Ronan... Si me traes un [Lingote de Cobre] del Arcón de Suministros del Sur, te forjaré un Lingote Templado.»"
                        });
                }
                break;

            case NodeRole.PotterStall:
                if (completado)
                {
                    DialogueManager.EnsureInstance().ShowDialogue(
                        "Doña Remedios, Maestra Alfarera",
                        new string[] { "Entrega la Esmeralda de San Felipe a Don Teodoro, el Joyero del puesto Suroeste." });
                }
                else if (inv.ConsumeItem(InventoryManager.ITEM_MITHRIL_BAR))
                {
                    completado = true;
                    inv.AddItem(InventoryManager.ITEM_GREEN_GEM);
                    InventoryHUD.NotificarIntercambio("¡Intercambio: Lingote Templado -> [Esmeralda de San Felipe]!");
                    DialogueManager.EnsureInstance().ShowDialogue(
                        "Doña Remedios, Maestra Alfarera",
                        new string[]
                        {
                            "«¡El Lingote Templado de Don Braulio! Con él podré restaurar las vasijas tradicionales de San Felipe.»",
                            "«Recibe esta [Esmeralda de San Felipe]. Llévasela a Don Teodoro en la Joyería del Suroeste; él guarda la llave del cofre gremial.»"
                        });
                }
                else
                {
                    DialogueManager.EnsureInstance().ShowDialogue(
                        "Doña Remedios, Maestra Alfarera",
                        new string[]
                        {
                            "«Nuestras vasijas de barro guardan la memoria del pueblo. Tráeme el [Lingote Templado] del Herrero Don Braulio y te daré la Esmeralda del Gremio.»"
                        });
                }
                break;

            case NodeRole.JewelerStall:
                if (completado)
                {
                    DialogueManager.EnsureInstance().ShowDialogue(
                        "Don Teodoro, Joyero del Callejón",
                        new string[] { "Usa la Llave Dorada del Gremio en el Cofre Sagrado del centro del Mercado." });
                }
                else if (inv.ConsumeItem(InventoryManager.ITEM_GREEN_GEM))
                {
                    completado = true;
                    inv.AddItem(InventoryManager.ITEM_GOLD_KEY);
                    InventoryHUD.NotificarIntercambio("¡Intercambio: Esmeralda -> [Llave Dorada del Gremio]!");
                    DialogueManager.EnsureInstance().ShowDialogue(
                        "Don Teodoro, Joyero del Callejón",
                        new string[]
                        {
                            "«¡La Esmeralda de San Felipe! Al engarzarla en el sello del gremio, la cerradura ancestral vuelve a responder.»",
                            "«Aquí tienes la [Llave Dorada del Gremio]. ¡Ve al centro del Mercado y abre el Cofre Sagrado para liberar el Eco de la Tradición!»"
                        });
                }
                else
                {
                    DialogueManager.EnsureInstance().ShowDialogue(
                        "Don Teodoro, Joyero del Callejón",
                        new string[]
                        {
                            "«Para terminar la [Llave Dorada del Gremio] necesito engarzar la [Esmeralda de San Felipe] que custodia Doña Remedios en la Alfarería.»"
                        });
                }
                break;

            case NodeRole.GuildTreasureChest:
                puzzlePadre?.IntentarAbrirCofreDelGremio();
                break;
        }
    }

    private void OnGUI()
    {
        if (playerTransform == null) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist > 1.95f) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.55f);
        if (screenPos.z <= 0) return;

        Color colBorde = new Color(0.98f, 0.88f, 0.42f);
        float safeMarginX = Mathf.Max(UITheme.Px(26f), Screen.width * 0.03f);
        float safeMarginY = Mathf.Max(UITheme.Px(22f), Screen.height * 0.03f);
        float w = UITheme.Px(290f);
        float h = UITheme.Px(34f);
        float rx = Mathf.Clamp(screenPos.x - w * 0.5f, safeMarginX, Screen.width - w - safeMarginX);
        float ry = Mathf.Clamp(Screen.height - screenPos.y - h * 0.5f, safeMarginY, Screen.height - h - safeMarginY);

        if (ZoneWaveSpawner.HordaActivaEnEscena != null && ry >= UITheme.Px(85f) && ry <= UITheme.Px(150f))
        {
            ry = UITheme.Px(155f);
        }

        Rect r = new Rect(rx, ry, w, h);
        UITheme.Panel(r, colBorde, 2f);
        GUI.Label(r, labelPrompt, UITheme.Estilo(14, FontStyle.Bold, TextAnchor.MiddleCenter, colBorde));
    }
}

