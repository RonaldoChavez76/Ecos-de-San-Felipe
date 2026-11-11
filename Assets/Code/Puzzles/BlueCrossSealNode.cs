using UnityEngine;

/// <summary>
/// Nodo individual de Sello de la Cruz Azul en la cima del Cerro del Fraile.
/// Puede ser purificado por Ronan [E / J] o corrompido por La Sombra Guardiana en Fase 2.
/// Cuando está corrupto, ejerce un suave vórtice gravitacional sobre Ronan y emite pulsos radiales.
/// </summary>
public class BlueCrossSealNode : MonoBehaviour
{
    public string nombreSello = "Sello del Viento";
    public BlueCrossShrinePuzzle puzzlePadre;
    public SpriteRenderer flamaRenderer;
    public bool purificado = false;

    private SpriteRenderer baseRenderer;
    private Transform playerTransform;
    private float ultimoPulsoTiempo = 0f;

    private void Start()
    {
        baseRenderer = GetComponent<SpriteRenderer>();
        ActualizarVisual(false);
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
            if (pm != null) playerTransform = pm.transform;
        }

        if (purificado) return;

        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);

            // Vórtice gravitacional en Fase 2 (cuando está corrupto)
            if (dist > 1.2f && dist <= 7.5f)
            {
                Vector2 dirHaciaSello = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;
                playerTransform.position += (Vector3)(dirHaciaSello * 1.35f * Time.deltaTime);
            }

            // Pulso de advertencia de la corrupción cada 3.5 segundos
            if (dist <= 2.2f && Time.time - ultimoPulsoTiempo > 3.5f)
            {
                ultimoPulsoTiempo = Time.time;
                PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
                if (pm != null)
                {
                    pm.RecibirDanio(4f);
                }
            }

            // Interacción de purificación
            if (dist <= 2.2f)
            {
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))
                {
                    Purificar();
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (purificado) return;

        ProjectileBehaviour proj = other.GetComponent<ProjectileBehaviour>();
        PlayerProjectile playerProj = other.GetComponent<PlayerProjectile>();
        if (proj != null || playerProj != null)
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

    public void Corromper()
    {
        purificado = false;
        ActualizarVisual(false);
        ultimoPulsoTiempo = Time.time;
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
                : new Color(0.75f, 0.12f, 0.85f, 0.85f);
        }
    }

    private void OnGUI()
    {
        if (purificado || playerTransform == null) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist > 2.2f) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.25f);
        if (screenPos.z <= 0) return;

        float w = UITheme.Px(340f);
        float h = UITheme.Px(34f);
        Rect r = new Rect(screenPos.x - w * 0.5f, Screen.height - screenPos.y - h * 0.5f, w, h);
        Color bordeColor = new Color(0.85f, 0.25f, 1.0f, 0.95f);
        UITheme.Panel(r, bordeColor, 2f);
        GUIStyle lblStyle = UITheme.Estilo(12, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        lblStyle.wordWrap = false;
        GUI.Label(r, $"[E / J] Purificar {nombreSello}", lblStyle);
    }
}
