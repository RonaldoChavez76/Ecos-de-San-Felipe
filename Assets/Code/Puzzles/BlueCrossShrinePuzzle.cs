using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Acertijo del Nivel 3 (Mirador de la Cruz Azul en el Cerro del Fraile):
/// "Purificación de los Tres Sellos de la Cruz Azul".
/// Tres altares corrompidos mantienen un escudo sombrío sobre La Sombra Guardiana.
/// Al purificar los 3 sellos con la espada [J], interacción [E] o proyectiles reflejados,
/// la Cruz Azul resplandece y rompe la barrera de invulnerabilidad del Jefe Final.
/// </summary>
public class BlueCrossShrinePuzzle : MonoBehaviour
{
    [Header("Referencias del Acertijo de la Cruz Azul")]
    public List<BlueCrossSealNode> sellos = new List<BlueCrossSealNode>();
    public SombraGuardianaBossAI jefeSombraGuardiana;
    public SpriteRenderer cruzAzulHaloRenderer;
    public GameObject escudoVisualJefe;

    [Header("Estado")]
    public int sellosPurificados = 0;
    public bool acertijoResuelto = false;

    private void Start()
    {
        if (jefeSombraGuardiana != null)
        {
            jefeSombraGuardiana.isShieldedBySeals = true;
        }
    }

    public void NotificarSelloPurificado(BlueCrossSealNode sello)
    {
        if (acertijoResuelto || sello == null) return;

        sellosPurificados = 0;
        foreach (var s in sellos)
        {
            if (s != null && s.purificado) sellosPurificados++;
        }

        AudioManager.Instance?.PlayBellSound();

        if (sellosPurificados >= sellos.Count && sellos.Count > 0)
        {
            CompletarPurificacionCruzAzul();
        }
        else
        {
            DialogueManager.EnsureInstance().ShowDialogue(
                "Mirador de la Cruz Azul",
                new string[]
                {
                    $"¡Has purificado el {sello.nombreSello}! ({sellosPurificados}/{sellos.Count} Sellos de la Cruz Azul encendidos).",
                    "Purifica los sellos restantes para romper el manto de invulnerabilidad de La Sombra Guardiana."
                });
        }
    }

    private void CompletarPurificacionCruzAzul()
    {
        acertijoResuelto = true;
        PuzzleManager.EnsureInstance().MarcarAcertijoResuelto(3);

        if (jefeSombraGuardiana != null)
        {
            jefeSombraGuardiana.isShieldedBySeals = false;
        }

        if (escudoVisualJefe != null)
        {
            escudoVisualJefe.SetActive(false);
        }

        if (cruzAzulHaloRenderer != null)
        {
            cruzAzulHaloRenderer.color = new Color(0.15f, 0.92f, 1.0f, 0.90f);
            cruzAzulHaloRenderer.transform.localScale = Vector3.one * 4.5f;
        }

        DialogueManager.EnsureInstance().ShowDialogue(
            "La Cruz Azul del Cerro del Fraile",
            new string[]
            {
                "¡Los Tres Sellos de la Cruz Azul resplandecen sobre el valle de San Felipe!",
                "El escudo abismal de La Sombra Guardiana se ha hecho añicos. ¡Usa tu Contraataque [K] y el Modo Asalto [L] para derrotarla y recuperar el Tercer Eco: el Eco de la Memoria!"
            });
    }
}
