using UnityEngine;

/// <summary>
/// Acertijo del Nivel 2 (El Mercado Municipal y Callejones de San Felipe):
/// "Cadena de Trueque Artesanal de los Gremios".
/// Ronan obtiene un Lingote de Cobre Antiguo y realiza el intercambio tradicional entre
/// 3 puestos de artesanos (Herrero -> Alfarera -> Joyero) para forjar la Llave Dorada del Gremio
/// y abrir el Cofre Sagrado que resguarda el Segundo Eco: Eco de la Tradición (+25 HP).
/// </summary>
public class MarketTradePuzzle : MonoBehaviour
{
    [Header("Referencias del Acertijo del Mercado")]
    public GameObject ecoDeLaTradicionObjeto;
    public GameObject barreraCofreGremio;
    public bool acertijoResuelto = false;

    public void IntentarAbrirCofreDelGremio()
    {
        if (acertijoResuelto)
        {
            DialogueManager.EnsureInstance().ShowDialogue(
                "Cofre del Gremio de Artesanos",
                new string[] { "El Cofre Sagrado ya fue abierto y el Eco de la Tradición brilla en libertad." });
            return;
        }

        InventoryManager inv = InventoryManager.EnsureInstance();
        if (inv.ConsumeItem(InventoryManager.ITEM_GOLD_KEY))
        {
            acertijoResuelto = true;
            PuzzleManager.EnsureInstance().MarcarAcertijoResuelto(2);

            if (barreraCofreGremio != null) barreraCofreGremio.SetActive(false);
            if (ecoDeLaTradicionObjeto != null) ecoDeLaTradicionObjeto.SetActive(true);

            DialogueManager.EnsureInstance().ShowDialogue(
                "Gremio de Artesanos de San Felipe",
                new string[]
                {
                    "¡Insertas la Llave Dorada del Gremio! El antiguo candado cede con un destello cálido.",
                    "Del interior del cofre emerge el Segundo Eco: el Eco de la Tradición, restaurando la memoria viva de los artesanos de Torres Mochas."
                });
        }
        else
        {
            DialogueManager.EnsureInstance().ShowDialogue(
                "Cofre Sagrado del Gremio (Cerrado)",
                new string[]
                {
                    "Este cofre ancestral requiere la Llave Dorada del Gremio para abrirse.",
                    "PISTA DEL MERCADO: Busca el Lingote de Cobre en el Arcón de Suministros al Sur, llévalo al Herrero Don Braulio (Noroeste), cambia el metal templado con la Alfarera Doña Remedios (Noreste) y entrega la Esmeralda al Joyero Don Teodoro (Suroeste)."
                });
        }
    }
}

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
                    DialogueManager.EnsureInstance().ShowDialogue(
                        "Arcón de la Antigua Fragua",
                        new string[] { "Ya recogiste el Lingote de Cobre Antiguo. Llévaselo a Don Braulio en la Herrería del Noroeste." });
                }
                else
                {
                    completado = true;
                    inv.AddItem(InventoryManager.ITEM_COPPER_BAR);
                    DialogueManager.EnsureInstance().ShowDialogue(
                        "Arcón de la Antigua Fragua",
                        new string[]
                        {
                            "¡Has encontrado un [Lingote de Cobre] intacto entre las cajas del mercado!",
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

        Vector3 screenPos = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.25f);
        if (screenPos.z <= 0) return;

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = new Color(0.98f, 0.88f, 0.42f);

        float w = 250f;
        float h = 28f;
        Rect r = new Rect(screenPos.x - w * 0.5f, Screen.height - screenPos.y - h * 0.5f, w, h);
        GUI.Box(r, labelPrompt, style);
    }
}
