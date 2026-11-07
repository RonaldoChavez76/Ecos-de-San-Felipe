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

            Vector3 spawnPos = barreraCofreGremio != null ? barreraCofreGremio.transform.position : transform.position;
            ResourcePickup.SpawnDrops(spawnPos + Vector3.down * 0.8f, 1f, 60, 100, 1f, 10, 1f, 50f, 1f, 35f);

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
