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

/// <summary>
/// Nodo individual de pedestal de campana y luz dentro de la Parroquia.
/// Se puede activar con la tecla E o golpeándolo con la espada (J / Clic Izquierdo).
/// </summary>
public class BellPedestalNode : MonoBehaviour
{
    public int ordenCampana = 1;
    public string nombreCampana = "Campana del Alba";
    public BellLightPuzzle puzzlePadre;
    public SpriteRenderer haloRenderer;

    private SpriteRenderer baseRenderer;
    private Transform playerTransform;
    private bool encendido = false;
    private float cooldownInteraccion = 0f;

    private void Start()
    {
        baseRenderer = GetComponent<SpriteRenderer>();
        EstablecerEstadoEncendido(false);
    }

    private void Update()
    {
        if (cooldownInteraccion > 0f) cooldownInteraccion -= Time.deltaTime;

        if (playerTransform == null)
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) playerTransform = pm.transform;
            return;
        }

        if (encendido || (puzzlePadre != null && puzzlePadre.acertijoResuelto)) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist <= 1.75f && cooldownInteraccion <= 0f)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))
            {
                cooldownInteraccion = 0.45f;
                puzzlePadre?.RegistrarActivacionPedestal(this);
            }
        }
    }

    public void EstablecerEstadoEncendido(bool activo)
    {
        encendido = activo;
        if (baseRenderer != null)
        {
            baseRenderer.color = activo ? new Color(1f, 0.92f, 0.35f, 1f) : new Color(0.72f, 0.76f, 0.85f, 1f);
        }
        if (haloRenderer != null)
        {
            haloRenderer.gameObject.SetActive(activo);
            haloRenderer.color = new Color(1f, 0.9f, 0.3f, 0.65f);
        }
    }

    public void MostrarFalloTemporal()
    {
        encendido = false;
        if (baseRenderer != null)
        {
            baseRenderer.color = new Color(1f, 0.25f, 0.25f, 1f);
        }
        if (haloRenderer != null)
        {
            haloRenderer.gameObject.SetActive(true);
            haloRenderer.color = new Color(1f, 0.15f, 0.15f, 0.65f);
        }
    }

    private void OnGUI()
    {
        if (encendido || playerTransform == null || (puzzlePadre != null && puzzlePadre.acertijoResuelto)) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist > 1.75f) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.1f);
        if (screenPos.z <= 0) return;

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = new Color(1f, 0.95f, 0.55f);

        float w = 230f;
        float h = 28f;
        Rect r = new Rect(screenPos.x - w * 0.5f, Screen.height - screenPos.y - h * 0.5f, w, h);
        GUI.Box(r, $"[E / J] Tocar {nombreCampana}", style);
    }
}

