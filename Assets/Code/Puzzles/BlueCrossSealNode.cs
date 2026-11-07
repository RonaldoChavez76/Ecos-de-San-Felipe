using UnityEngine;

/// <summary>
/// Nodo individual de Sello de la Cruz Azul en la cima del Cerro del Fraile.
/// </summary>
public class BlueCrossSealNode : MonoBehaviour
{
    public string nombreSello = "Sello del Viento";
    public BlueCrossShrinePuzzle puzzlePadre;
    public SpriteRenderer flamaRenderer;
    public bool purificado = false;

    private SpriteRenderer baseRenderer;
    private Transform playerTransform;

    private void Start()
    {
        baseRenderer = GetComponent<SpriteRenderer>();
        ActualizarVisual(false);
    }

    private void Update()
    {
        if (purificado) return;

        if (playerTransform == null)
        {
            PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
            if (pm != null) playerTransform = pm.transform;
            return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist <= 2.0f)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))
            {
                Purificar();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (purificado) return;

        ProjectileBehaviour proj = other.GetComponent<ProjectileBehaviour>();
        if (proj != null)
        {
            Purificar();
        }
    }

    public void Purificar()
    {
        if (purificado) return;
        purificado = true;
        ActualizarVisual(true);
        puzzlePadre?.NotificarSelloPurificado(this);
    }

    private void ActualizarVisual(bool limpio)
    {
        if (baseRenderer != null)
        {
            baseRenderer.color = limpio
                ? new Color(0.20f, 0.90f, 1.0f, 1f)
                : new Color(0.55f, 0.15f, 0.65f, 1f);
        }

        if (flamaRenderer != null)
        {
            flamaRenderer.color = limpio
                ? new Color(0.15f, 0.95f, 1.0f, 0.85f)
                : new Color(0.65f, 0.10f, 0.75f, 0.75f);
        }
    }

    private void OnGUI()
    {
        if (purificado || playerTransform == null) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist > 2.0f) return;

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
        style.normal.textColor = new Color(0.35f, 0.95f, 1.0f);

        float w = UITheme.Px(240f * 1.25f);
        float h = UITheme.Px(36f);
        Rect r = new Rect(screenPos.x - w * 0.5f, Screen.height - screenPos.y - h * 0.5f, w, h);
        UITheme.Panel(r, style.normal.textColor, 2f);
        GUI.Label(r, $"[E / J] Purificar {nombreSello}", UITheme.Estilo(15, FontStyle.Bold, TextAnchor.MiddleCenter, style.normal.textColor));
    }
}

