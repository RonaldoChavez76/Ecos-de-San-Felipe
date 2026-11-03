using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gestor global de diálogos con habitantes de San Felipe, placas históricas e inscripciones de acertijos.
/// Funciona sin cinemáticas intrusivas, mostrando el texto con efecto de máquina de escribir.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    public bool IsDialogueActive { get; private set; }

    private Canvas dialogueCanvas;
    private GameObject dialoguePanel;
    private Text speakerNameText;
    private Text bodyText;
    private Text promptText;

    private string[] currentLines;
    private string currentSpeaker;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private Action onDialogueFinished;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildDialogueUI();
    }

    public static DialogueManager EnsureInstance()
    {
        if (Instance != null) return Instance;
        DialogueManager existing = FindAnyObjectByType<DialogueManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }
        GameObject go = new GameObject("DialogueManager");
        Instance = go.AddComponent<DialogueManager>();
        return Instance;
    }

    public void ShowDialogue(string speaker, string[] lines, Action onComplete = null)
    {
        StartDialogue(speaker, lines, onComplete);
    }

    private void Update()
    {
        if (!IsDialogueActive) return;

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            AdvanceDialogue();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseDialogue();
        }
    }

    public void StartDialogue(string speaker, string[] lines, Action onComplete = null)
    {
        if (lines == null || lines.Length == 0) return;
        if (dialoguePanel == null) BuildDialogueUI();

        currentSpeaker = string.IsNullOrEmpty(speaker) ? "Inscripción de San Felipe" : speaker;
        currentLines = lines;
        currentLineIndex = 0;
        onDialogueFinished = onComplete;
        IsDialogueActive = true;

        dialoguePanel.SetActive(true);
        speakerNameText.text = currentSpeaker.ToUpperInvariant();
        ShowCurrentLine();
    }

    public void AdvanceDialogue()
    {
        if (!IsDialogueActive || currentLines == null) return;

        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            isTyping = false;
            bodyText.text = currentLines[currentLineIndex];
            return;
        }

        currentLineIndex++;
        if (currentLineIndex < currentLines.Length)
        {
            ShowCurrentLine();
        }
        else
        {
            CloseDialogue();
        }
    }

    public void CloseDialogue()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        isTyping = false;
        IsDialogueActive = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        Action callback = onDialogueFinished;
        onDialogueFinished = null;
        callback?.Invoke();
    }

    private void ShowCurrentLine()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeLineRoutine(currentLines[currentLineIndex]));
    }

    private IEnumerator TypeLineRoutine(string line)
    {
        isTyping = true;
        bodyText.text = "";
        foreach (char c in line)
        {
            bodyText.text += c;
            yield return new WaitForSecondsRealtime(0.015f);
        }
        isTyping = false;
    }

    private void BuildDialogueUI()
    {
        GameObject canvasObj = new GameObject("Canvas_DialogueManager");
        canvasObj.transform.SetParent(transform);
        dialogueCanvas = canvasObj.AddComponent<Canvas>();
        dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dialogueCanvas.sortingOrder = 150;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasObj.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 18);

        dialoguePanel = new GameObject("Panel_Dialogo");
        dialoguePanel.transform.SetParent(canvasObj.transform, false);
        Image bg = dialoguePanel.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.03f, 0.06f, 0.98f);
        RectTransform rt = dialoguePanel.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0f, -220f);
        rt.sizeDelta = new Vector2(1000f, 200f);

        // Nombre del hablante / placa
        GameObject speakerObj = new GameObject("Txt_Speaker");
        speakerObj.transform.SetParent(dialoguePanel.transform, false);
        speakerNameText = speakerObj.AddComponent<Text>();
        speakerNameText.font = font;
        speakerNameText.fontSize = 26;
        speakerNameText.fontStyle = FontStyle.Bold;
        speakerNameText.color = new Color(0.55f, 0.97f, 1f, 1f);
        speakerNameText.alignment = TextAnchor.MiddleLeft;
        RectTransform speakerRt = speakerObj.GetComponent<RectTransform>();
        speakerRt.anchoredPosition = new Vector2(0f, 72f);
        speakerRt.sizeDelta = new Vector2(940f, 40f);

        // Cuerpo del diálogo
        GameObject bodyObj = new GameObject("Txt_Body");
        bodyObj.transform.SetParent(dialoguePanel.transform, false);
        bodyText = bodyObj.AddComponent<Text>();
        bodyText.font = font;
        bodyText.fontSize = 26;
        bodyText.color = Color.white;
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        RectTransform bodyRt = bodyObj.GetComponent<RectTransform>();
        bodyRt.anchoredPosition = new Vector2(0f, 0f);
        bodyRt.sizeDelta = new Vector2(940f, 110f);

        // Indicador de avance
        GameObject promptObj = new GameObject("Txt_Prompt");
        promptObj.transform.SetParent(dialoguePanel.transform, false);
        promptText = promptObj.AddComponent<Text>();
        promptText.font = font;
        promptText.fontSize = 18;
        promptText.color = new Color(1f, 0.90f, 0.50f, 1f);
        promptText.alignment = TextAnchor.MiddleRight;
        promptText.text = "[E] / [ESPACIO] Continuar";
        RectTransform promptRt = promptObj.GetComponent<RectTransform>();
        promptRt.anchoredPosition = new Vector2(0f, -80f);
        promptRt.sizeDelta = new Vector2(940f, 28f);

        // Contorno oscuro para máximo contraste sobre cualquier fondo
        foreach (Text tx in new Text[] { speakerNameText, bodyText, promptText })
        {
            Outline ol = tx.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0f, 0f, 0f, 0.9f);
            ol.effectDistance = new Vector2(1.5f, -1.5f);
        }

        dialoguePanel.SetActive(false);
    }
}
