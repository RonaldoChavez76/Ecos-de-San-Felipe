using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Acertijo del Nivel 1 (Interior de la Parroquia de San Felipe Apóstol):
/// "Las Tres Campanas de la Luz".
/// Ronan lee la inscripción del altar y debe activar los 3 pedestales de campana
/// en el orden litúrgico correcto: 1 (Alba) -> 2 (Mediodía) -> 3 (Ocaso).
/// Al resolverlo, la barrera de sombra del altar mayor se disipa y libera el Eco de la Fe (+25 HP).
/// </summary>
public class BellLightPuzzle : MonoBehaviour
{
    [Header("Pedestales del Acertijo (Orden 1, 2, 3)")]
    public List<BellPedestalNode> pedestales = new List<BellPedestalNode>();

    [Header("Recompensa y Barrera del Altar")]
    public GameObject barreraSombraAltar;
    public GameObject ecoDeLaFeObjeto;

    [Header("Estado")]
    public bool acertijoResuelto = false;
    private readonly List<int> secuenciaActual = new List<int>();
    private readonly int[] secuenciaCorrecta = { 1, 2, 3 };

    public void RegistrarActivacionPedestal(BellPedestalNode pedestal)
    {
        if (acertijoResuelto || pedestal == null) return;

        int pasoActual = secuenciaActual.Count;
        int idEsperado = secuenciaCorrecta[pasoActual];

        AudioManager.Instance?.PlayBellSound();

        if (pedestal.ordenCampana == idEsperado)
        {
            secuenciaActual.Add(pedestal.ordenCampana);
            pedestal.EstablecerEstadoEncendido(true);

            if (secuenciaActual.Count >= secuenciaCorrecta.Length)
            {
                CompletarAcertijo();
            }
        }
        else
        {
            StartCoroutine(ReiniciarPorError());
        }
    }

    private IEnumerator ReiniciarPorError()
    {
        secuenciaActual.Clear();
        foreach (var p in pedestales)
        {
            if (p != null) p.MostrarFalloTemporal();
        }

        DialogueManager.EnsureInstance().ShowDialogue(
            "Altar de San Felipe",
            new string[]
            {
                "Las campanas resuenan en discordia... La sombra del altar permanece intacta.",
                "Recuerda la inscripción sagrada: primero el Alba (Izquierda), luego el Mediodía (Centro) y al final el Ocaso (Derecha)."
            });

        yield return new WaitForSeconds(0.65f);

        foreach (var p in pedestales)
        {
            if (p != null) p.EstablecerEstadoEncendido(false);
        }
    }

    private void CompletarAcertijo()
    {
        acertijoResuelto = true;
        PuzzleManager.EnsureInstance().MarcarAcertijoResuelto(1);

        if (barreraSombraAltar != null)
        {
            barreraSombraAltar.SetActive(false);
        }

        if (ecoDeLaFeObjeto != null)
        {
            ecoDeLaFeObjeto.SetActive(true);
        }

        DialogueManager.EnsureInstance().ShowDialogue(
            "Parroquia de San Felipe Apóstol",
            new string[]
            {
                "¡Las tres campanas cantan al unísono! Un haz de luz dorada atraviesa la cúpula de la Parroquia.",
                "La barrera de olvido sobre el Altar Mayor se ha disipado, revelando el Primer Eco: el Eco de la Fe."
            });
    }
}
