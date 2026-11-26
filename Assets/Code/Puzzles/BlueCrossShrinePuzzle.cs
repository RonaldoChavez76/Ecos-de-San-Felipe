using System.Collections.Generic;
using System.Collections;
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
    public SpriteRenderer cruzAzulRenderer;
    public GameObject escudoVisualJefe;
    public Vector2 ritualPosition = new Vector2(0f, 32.8f);

    [Header("Estado")]
    public int sellosPurificados = 0;
    public bool acertijoResuelto = false;
    private Transform playerTransform;
    private bool consagracionEnCurso;

    private void Start()
    {
        if (jefeSombraGuardiana != null)
        {
            jefeSombraGuardiana.isShieldedBySeals = true;
        }
    }

    private void Update()
    {
        SaveStateManager save = SaveStateManager.Instance;
        if (save == null || consagracionEnCurso ||
            !save.ecoFeRecuperado || !save.ecoTradicionRecuperado || !save.ecoMemoriaRecuperado)
            return;

        if (playerTransform == null)
        {
            PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
            if (player != null) playerTransform = player.transform;
        }

        if (playerTransform == null || Vector2.Distance(playerTransform.position, ritualPosition) > 3.2f)
            return;

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            return;

        MobileInput.ReportInteractableNearby("Consagrar");
        if (MobileInput.GetInteractDown(KeyCode.E))
        {
            PlayerMovement player = playerTransform.GetComponent<PlayerMovement>();
            if (save.cruzAzulConsagrada)
            {
                PauseAndVictoryUI.EnsureInstance().ReabrirVictoriaFinalDesdeCruz(player);
            }
            else
            {
                StartCoroutine(ConsagrarLosTresEcos(save, player));
            }
        }
    }

    private IEnumerator ConsagrarLosTresEcos(SaveStateManager save, PlayerMovement player)
    {
        consagracionEnCurso = true;
        PauseAndVictoryUI.SecuenciaEcoEnCurso = true;
        player?.ActivarPoseDeVictoriaConEco("Los Tres Ecos Sagrados", 3, true);
        AudioManager.Instance?.PlayBellSound();
        AudioManager.Instance?.PlayEchoSound();

        Vector3 escalaInicial = cruzAzulHaloRenderer != null
            ? cruzAzulHaloRenderer.transform.localScale
            : Vector3.one * 4.5f;
        Color colorInicial = cruzAzulRenderer != null ? cruzAzulRenderer.color : Color.white;
        float tiempo = 0f;
        const float duracion = 2.2f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float progreso = Mathf.Clamp01(tiempo / duracion);
            float pulso = 1f + Mathf.Sin(tiempo * 12f) * 0.08f;

            if (cruzAzulHaloRenderer != null)
            {
                cruzAzulHaloRenderer.color = Color.Lerp(
                    new Color(0.12f, 0.82f, 1f, 0.68f),
                    new Color(1f, 0.88f, 0.42f, 0.9f),
                    progreso);
                cruzAzulHaloRenderer.transform.localScale = escalaInicial * Mathf.Lerp(1f, 1.8f, progreso) * pulso;
            }

            if (cruzAzulRenderer != null)
                cruzAzulRenderer.color = Color.Lerp(colorInicial, Color.white, Mathf.PingPong(tiempo * 2.5f, 1f));

            yield return null;
        }

        if (cruzAzulHaloRenderer != null)
        {
            cruzAzulHaloRenderer.color = new Color(0.25f, 0.9f, 1f, 0.95f);
            cruzAzulHaloRenderer.transform.localScale = escalaInicial * 1.35f;
        }
        if (cruzAzulRenderer != null) cruzAzulRenderer.color = Color.white;

        save.ConsagrarCruzAzul();
        consagracionEnCurso = false;
        PauseAndVictoryUI.SecuenciaEcoEnCurso = false;
        PauseAndVictoryUI.EnsureInstance().MostrarVictoriaFinal(player);
    }

    private void OnGUI()
    {
        SaveStateManager save = SaveStateManager.Instance;
        if (save == null || consagracionEnCurso ||
            !save.ecoFeRecuperado || !save.ecoTradicionRecuperado || !save.ecoMemoriaRecuperado ||
            playerTransform == null || Vector2.Distance(playerTransform.position, ritualPosition) > 3.2f)
            return;

        Camera camera = Camera.main;
        if (camera == null) return;

        Vector3 screenPosition = camera.WorldToScreenPoint(ritualPosition + Vector2.up * 1.2f);
        if (screenPosition.z <= 0f) return;

        float width = UITheme.Px(430f);
        float height = UITheme.Px(42f);
        Rect prompt = new Rect(screenPosition.x - width * 0.5f, Screen.height - screenPosition.y - height * 0.5f, width, height);
        UITheme.Panel(prompt, UITheme.Turquesa, 2f);
        bool esTactil = MobileInput.IsTouchControlsActive;
        string promptText = save.cruzAzulConsagrada
            ? (esTactil ? "Toca Consagrar para ver la victoria" : "[E] Volver a ver la victoria en la Cruz Azul")
            : (esTactil ? "Toca Consagrar en la Cruz Azul" : "[E] Reunir los Tres Ecos en la Cruz Azul");
        GUI.Label(prompt, promptText, UITheme.Estilo(13, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
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
            DialogueManager.EnsureInstance().ShowTimedDialogue(
                "Mirador de la Cruz Azul",
                new string[]
                {
                    $"¡Has purificado el {sello.nombreSello}! ({sellosPurificados}/{sellos.Count} Sellos de la Cruz Azul encendidos).",
                    "Purifica los sellos restantes para romper el manto de invulnerabilidad de La Sombra Guardiana."
                },
                3.5f);
        }

        SaveStateManager.Instance?.RegistrarEstadoMundoActual();
    }

    public void NotificarSelloCorrompido(BlueCrossSealNode sello)
    {
        if (sello == null || acertijoResuelto) return;

        sellosPurificados = 0;
        foreach (BlueCrossSealNode nodo in sellos)
        {
            if (nodo != null && nodo.purificado) sellosPurificados++;
        }

        if (jefeSombraGuardiana != null)
            jefeSombraGuardiana.isShieldedBySeals = true;
        if (escudoVisualJefe != null)
            escudoVisualJefe.SetActive(true);
        if (cruzAzulHaloRenderer != null)
        {
            cruzAzulHaloRenderer.color = new Color(0.55f, 0.12f, 0.82f, 0.85f);
            cruzAzulHaloRenderer.transform.localScale = Vector3.one * 3.8f;
        }
        SaveStateManager.Instance?.RegistrarEstadoMundoActual();
    }

    public void CapturarEstadoCheckpoint(WorldCheckpointState estado)
    {
        if (estado == null) return;
        estado.santuarioPresente = true;
        estado.santuarioResuelto = acertijoResuelto;
        estado.sellos.Clear();
        foreach (BlueCrossSealNode sello in sellos)
        {
            if (sello == null) continue;
            estado.sellos.Add(new SealCheckpointState
            {
                id = sello.nombreSello,
                purificado = sello.purificado
            });
        }
    }

    public void RestaurarEstadoCheckpoint(WorldCheckpointState estado)
    {
        if (estado == null || !estado.santuarioPresente) return;

        acertijoResuelto = estado.santuarioResuelto;
        foreach (BlueCrossSealNode sello in sellos)
        {
            if (sello == null) continue;
            SealCheckpointState guardado = estado.sellos != null
                ? estado.sellos.Find(
                    item => item != null && item.id == sello.nombreSello)
                : null;
            if (guardado != null)
                sello.RestaurarEstadoCheckpoint(guardado.purificado);
        }

        sellosPurificados = 0;
        foreach (BlueCrossSealNode sello in sellos)
            if (sello != null && sello.purificado) sellosPurificados++;

        bool escudoActivo = !acertijoResuelto &&
                            (estado.jefe == null || estado.jefe.escudoActivo);
        if (jefeSombraGuardiana != null)
            jefeSombraGuardiana.isShieldedBySeals = escudoActivo;
        if (escudoVisualJefe != null)
            escudoVisualJefe.SetActive(escudoActivo);

        if (cruzAzulHaloRenderer != null)
        {
            cruzAzulHaloRenderer.color = acertijoResuelto
                ? new Color(0.15f, 0.92f, 1.0f, 0.90f)
                : new Color(0.55f, 0.12f, 0.82f, 0.85f);
            cruzAzulHaloRenderer.transform.localScale = Vector3.one * (acertijoResuelto ? 4.5f : 3.8f);
        }

        if (acertijoResuelto)
            PuzzleManager.EnsureInstance().MarcarAcertijoResuelto(3);
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

        DialogueManager.EnsureInstance().ShowTimedDialogue(
            "La Cruz Azul del Cerro del Fraile",
            new string[]
            {
                "¡Los Tres Sellos de la Cruz Azul resplandecen sobre el valle de San Felipe!",
                "El escudo abismal de La Sombra Guardiana se ha hecho añicos. ¡Usa tu Contraataque [K] y el Modo Asalto [L] para derrotarla y recuperar el Tercer Eco: el Eco de la Memoria!"
            },
            4f);
    }
}
