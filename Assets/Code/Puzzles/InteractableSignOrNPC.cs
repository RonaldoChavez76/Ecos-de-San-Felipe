using System;
using UnityEngine;

/// <summary>
/// Componente interactivo para habitantes (NPCs), carteles, inscripciones coloniales y altares.
/// Al acercarse Ronan y presionar [E], abre el DialogueManager o ejecuta una acción de puzzle.
/// </summary>
public class InteractableSignOrNPC : MonoBehaviour
{
    [Header("Datos del NPC o Inscripción")]
    public string speakerName = "Habitante de San Felipe";
    public string interactionPrompt = "[E] Interactuar";
    [TextArea(2, 4)]
    public string[] dialogueLines = new string[]
    {
        "La niebla cubrió la Parroquia al amanecer y se llevó nuestros recuerdos..."
    };

    public string speakerTitle
    {
        get => speakerName;
        set => speakerName = value;
    }

    public string[] dialoguePages
    {
        get => dialogueLines;
        set => dialogueLines = value;
    }

    [Header("Interacción")]
    public float interactRadius = 2.1f;
    public KeyCode interactKey = KeyCode.E;

    public event Action OnInteracted;

    private Transform playerTransform;
    private GameObject promptBadge;
    private float nextInteractAllowedTime = 0f;

    private void Start()
    {
        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null) playerTransform = pm.transform;

        CreateFloatingPromptBadge();
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
            if (pm != null) playerTransform = pm.transform;
            return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        bool inRange = dist <= interactRadius;
        bool dialogueBusy = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;

        if (promptBadge != null)
        {
            promptBadge.SetActive(inRange && !dialogueBusy);
            if (inRange)
            {
                promptBadge.transform.localPosition = new Vector3(0f, 1.15f + Mathf.Sin(Time.time * 4f) * 0.08f, 0f);
            }
        }

        if (inRange && !dialogueBusy && Time.time >= nextInteractAllowedTime && Input.GetKeyDown(interactKey))
        {
            nextInteractAllowedTime = Time.time + 0.35f;
            TriggerInteraction();
        }
    }

    public void TriggerInteraction()
    {
        if (DialogueManager.Instance == null)
        {
            GameObject dmObj = new GameObject("DialogueManager");
            dmObj.AddComponent<DialogueManager>();
        }

        if (dialogueLines != null && dialogueLines.Length > 0)
        {
            DialogueManager.Instance.StartDialogue(speakerName, dialogueLines, () => OnInteracted?.Invoke());
        }
        else
        {
            OnInteracted?.Invoke();
        }
    }

    private void CreateFloatingPromptBadge()
    {
        promptBadge = new GameObject("Indicador_TeclaE");
        promptBadge.transform.SetParent(transform);
        promptBadge.transform.localPosition = new Vector3(0f, 1.15f, 0f);

        SpriteRenderer sr = promptBadge.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 50;

        Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                bool border = (x == 1 || x == 14 || y == 1 || y == 14);
                bool inside = (x > 1 && x < 14 && y > 1 && y < 14);
                // Dibuja una letra 'E' pixel art en el centro (x: 5..10, y: 4..11)
                bool letterE = (x == 5 && y >= 4 && y <= 11) ||
                               ((y == 4 || y == 7 || y == 11) && x >= 5 && x <= 10);

                Color c = Color.clear;
                if (letterE) c = new Color(0f, 0.95f, 1f, 1f);
                else if (border) c = new Color(0.95f, 0.82f, 0.35f, 1f);
                else if (inside) c = new Color(0.10f, 0.08f, 0.06f, 0.92f);
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 20f);
        promptBadge.SetActive(false);
    }

    private void OnGUI()
    {
        if (Time.timeScale == 0f || playerTransform == null) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist > interactRadius) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.45f);
        if (screenPos.z <= 0) return;

        float safeMarginX = Mathf.Max(UITheme.Px(26f), Screen.width * 0.03f);
        float safeMarginY = Mathf.Max(UITheme.Px(22f), Screen.height * 0.03f);
        float w = UITheme.Px(260f);
        float h = UITheme.Px(32f);
        float rx = Mathf.Clamp(screenPos.x - w * 0.5f, safeMarginX, Screen.width - w - safeMarginX);
        float ry = Mathf.Clamp(Screen.height - screenPos.y - h * 0.5f, safeMarginY, Screen.height - h - safeMarginY);

        // Evitar que se encime con el banner superior de oleadas (Y = UITheme.Px(105f))
        if (ZoneWaveSpawner.HordaActivaEnEscena != null && ry >= UITheme.Px(85f) && ry <= UITheme.Px(150f))
        {
            ry = UITheme.Px(155f);
        }

        Color colBorde = new Color(0.98f, 0.88f, 0.42f, 1f);
        Rect r = new Rect(rx, ry, w, h);
        UITheme.Panel(r, colBorde, 1.5f);
        GUI.Label(r, interactionPrompt, UITheme.Estilo(13, FontStyle.Bold, TextAnchor.MiddleCenter, colBorde));
    }
}
