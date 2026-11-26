using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// HUD táctil multi-touch en uGUI para Android / iOS (y modo de prueba en PC).
/// Incluye joystick virtual izquierdo, botones coloniales de combate (Atacar, Escudo/Contraataque,
/// Esquive, Modo Asalto, Cambiar Arma), botón contextual de Interacción y accesos de Pausa y Tienda.
/// Respeta Screen.safeArea en todo momento.
/// </summary>
public class MobileControlsUI : MonoBehaviour
{
    public static MobileControlsUI Instance { get; private set; }

    private Canvas canvas;
    private RectTransform safeAreaRoot;
    private CanvasGroup safeAreaGroup;
    private RectTransform leftCluster;
    private RectTransform rightCluster;
    private RectTransform topRightCluster;

    private GameObject interactButtonObj;
    private Text interactLabel;

    private GameObject assaultButtonObj;
    private Image assaultBg;
    private Text assaultLabel;

    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private float lastScale = -1f;
    private bool wasVisible = false;
    private PlayerMovement cachedPlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        EnsureInstance();
    }

    public static MobileControlsUI EnsureInstance()
    {
        if (Instance != null) return Instance;
        MobileControlsUI existing = FindAnyObjectByType<MobileControlsUI>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }
        GameObject go = new GameObject("MobileControlsUI");
        Instance = go.AddComponent<MobileControlsUI>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        BuildTouchCanvas();
        EnsureEventSystem();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            MobileInput.ResetHeldStates();
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        cachedPlayer = null;
        MobileInput.ResetHeldStates();
        EnsureEventSystem();
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private void Update()
    {
        if (safeAreaRoot == null) return;

        UpdateSafeAreaIfChanged();
        ApplyControlScaleIfChanged();

        bool visible = ShouldShowControls();
        if (visible != wasVisible)
        {
            wasVisible = visible;
            if (!visible) MobileInput.ResetHeldStates();
        }

        if (safeAreaGroup != null)
        {
            safeAreaGroup.alpha = visible ? MobileInput.ControlOpacity : 0f;
            safeAreaGroup.interactable = visible;
            safeAreaGroup.blocksRaycasts = visible;
        }

        if (!visible) return;

        // Botón contextual de interacción
        bool hasInteractable = MobileInput.HasNearbyInteractable;
        if (interactButtonObj != null)
        {
            if (interactButtonObj.activeSelf != hasInteractable)
                interactButtonObj.SetActive(hasInteractable);

            if (hasInteractable && interactLabel != null)
                interactLabel.text = $"✋\n{MobileInput.NearbyInteractableLabel.ToUpperInvariant()}";
        }

        // Estado visual del Modo Asalto
        if (cachedPlayer == null) cachedPlayer = FindAnyObjectByType<PlayerMovement>();
        if (cachedPlayer != null && assaultBg != null && assaultLabel != null)
        {
            float pct = cachedPlayer.isAssaultActive
                ? 1f
                : Mathf.Clamp01(cachedPlayer.assaultCharge / Mathf.Max(1f, cachedPlayer.maxAssaultCharge));
            bool ready = cachedPlayer.isAssaultActive || pct >= 1f;

            if (ready)
            {
                float pulse = 0.85f + Mathf.PingPong(Time.unscaledTime * 2.2f, 0.15f);
                assaultBg.color = new Color(1f, 0.92f, 0.48f, pulse);
                assaultLabel.text = cachedPlayer.isAssaultActive ? "★\nACTIVO" : "★\nASALTO";
            }
            else
            {
                assaultBg.color = new Color(0.55f, 0.60f, 0.70f, 0.38f);
                assaultLabel.text = $"★\n{Mathf.RoundToInt(pct * 100f)}%";
            }
        }
    }

    private bool ShouldShowControls()
    {
        if (!MobileInput.IsTouchControlsActive) return false;

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "PantallaInicio") return false;
        if (Time.timeScale == 0f) return false;

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return false;
        if (ShopManager.Instance != null && ShopManager.Instance.tiendaAbierta) return false;
        if (OptionsModalUI.Instance != null && OptionsModalUI.Instance.isOptionsOpen) return false;

        if (PauseAndVictoryUI.Instance != null &&
            (PauseAndVictoryUI.Instance.menuPausaAbierto ||
             PauseAndVictoryUI.Instance.mostrarVictoriaNivel ||
             PauseAndVictoryUI.Instance.mostrarPantallaVictoria))
            return false;

        if (cachedPlayer == null) cachedPlayer = FindAnyObjectByType<PlayerMovement>();
        if (cachedPlayer != null && cachedPlayer.currentHealth <= 0f) return false;

        return true;
    }

    private void UpdateSafeAreaIfChanged()
    {
        Rect sa = Screen.safeArea;
        Vector2Int sz = new Vector2Int(Screen.width, Screen.height);
        if (sa == lastSafeArea && sz == lastScreenSize) return;
        if (sz.x <= 0 || sz.y <= 0) return;

        lastSafeArea = sa;
        lastScreenSize = sz;

        Vector2 anchorMin = new Vector2(sa.x / sz.x, sa.y / sz.y);
        Vector2 anchorMax = new Vector2(sa.xMax / sz.x, sa.yMax / sz.y);
        safeAreaRoot.anchorMin = anchorMin;
        safeAreaRoot.anchorMax = anchorMax;
        safeAreaRoot.offsetMin = Vector2.zero;
        safeAreaRoot.offsetMax = Vector2.zero;
    }

    private void ApplyControlScaleIfChanged()
    {
        float s = MobileInput.ControlScale;
        if (Mathf.Approximately(s, lastScale)) return;
        lastScale = s;

        Vector3 v = new Vector3(s, s, 1f);
        if (leftCluster != null) leftCluster.localScale = v;
        if (rightCluster != null) rightCluster.localScale = v;
        if (topRightCluster != null) topRightCluster.localScale = v;
    }

    private void BuildTouchCanvas()
    {
        GameObject canvasObj = new GameObject("Canvas_MobileControls");
        canvasObj.transform.SetParent(transform, false);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject rootObj = new GameObject("SafeAreaRoot");
        rootObj.transform.SetParent(canvasObj.transform, false);
        safeAreaRoot = rootObj.AddComponent<RectTransform>();
        safeAreaGroup = rootObj.AddComponent<CanvasGroup>();
        safeAreaGroup.alpha = 0f;
        safeAreaGroup.interactable = false;
        safeAreaGroup.blocksRaycasts = false;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);

        // ---- 1. Cluster Inferior Izquierdo: Joystick Virtual ----
        GameObject leftObj = new GameObject("LeftCluster_Joystick");
        leftObj.transform.SetParent(safeAreaRoot, false);
        leftCluster = leftObj.AddComponent<RectTransform>();
        leftCluster.anchorMin = Vector2.zero;
        leftCluster.anchorMax = Vector2.zero;
        leftCluster.pivot = Vector2.zero;
        leftCluster.sizeDelta = new Vector2(280f, 280f);
        leftCluster.anchoredPosition = Vector2.zero;

        CrearJoystick(leftCluster);

        // ---- 2. Cluster Inferior Derecho: Combate e Interacción ----
        GameObject rightObj = new GameObject("RightCluster_Combat");
        rightObj.transform.SetParent(safeAreaRoot, false);
        rightCluster = rightObj.AddComponent<RectTransform>();
        rightCluster.anchorMin = new Vector2(1f, 0f);
        rightCluster.anchorMax = new Vector2(1f, 0f);
        rightCluster.pivot = new Vector2(1f, 0f);
        rightCluster.sizeDelta = new Vector2(360f, 360f);
        rightCluster.anchoredPosition = Vector2.zero;

        // Botón Atacar (principal)
        CrearBotonTactil(rightCluster, "Btn_Attack", "⚔\nATACAR",
            new Vector2(-88f, 92f), 114f, 18,
            new Color(0.28f, 0.08f, 0.08f, 0.85f), new Color(0.98f, 0.72f, 0.25f, 0.95f),
            font, MobileInput.TriggerAttack);

        // Botón Bloqueo / Contraataque (mantenido)
        CrearBotonTactil(rightCluster, "Btn_Block", "🛡\nESCUDO",
            new Vector2(-212f, 76f), 88f, 14,
            new Color(0.05f, 0.16f, 0.28f, 0.85f), new Color(0.15f, 0.90f, 1f, 0.95f),
            font,
            () => { MobileInput.TriggerBlockDown(); MobileInput.SetBlockHeld(true); },
            () => MobileInput.SetBlockHeld(false));

        // Botón Esquive
        CrearBotonTactil(rightCluster, "Btn_Dodge", "⚡\nESQUIVE",
            new Vector2(-190f, 182f), 82f, 13,
            new Color(0.06f, 0.22f, 0.18f, 0.85f), new Color(0.35f, 0.95f, 0.72f, 0.95f),
            font, MobileInput.TriggerDodge);

        // Botón Modo Asalto
        GameObject assaultGo = CrearBotonTactil(rightCluster, "Btn_Assault", "★\n0%",
            new Vector2(-82f, 214f), 82f, 13,
            new Color(0.16f, 0.14f, 0.05f, 0.88f), new Color(1f, 0.85f, 0.30f, 0.95f),
            font, MobileInput.TriggerAssault);
        assaultButtonObj = assaultGo;
        assaultBg = assaultGo.GetComponent<Image>();
        assaultLabel = assaultGo.GetComponentInChildren<Text>();

        // Botón Cambiar Arma
        CrearBotonTactil(rightCluster, "Btn_Weapon", "↻\nARMA",
            new Vector2(-290f, 152f), 68f, 12,
            new Color(0.14f, 0.11f, 0.08f, 0.85f), new Color(0.90f, 0.75f, 0.35f, 0.90f),
            font, MobileInput.TriggerNextWeapon);

        // Botón Contextual Interactuar
        interactButtonObj = CrearBotonTactil(rightCluster, "Btn_Interact", "✋\nUSAR",
            new Vector2(-188f, 290f), 92f, 13,
            new Color(0.10f, 0.24f, 0.32f, 0.92f), new Color(1f, 0.88f, 0.38f, 1f),
            font, MobileInput.TriggerInteract);
        interactLabel = interactButtonObj.GetComponentInChildren<Text>();
        interactButtonObj.SetActive(false);

        // ---- 3. Cluster Superior Derecho: Pausa y Tienda ----
        GameObject topObj = new GameObject("TopRightCluster_Utility");
        topObj.transform.SetParent(safeAreaRoot, false);
        topRightCluster = topObj.AddComponent<RectTransform>();
        topRightCluster.anchorMin = new Vector2(1f, 1f);
        topRightCluster.anchorMax = new Vector2(1f, 1f);
        topRightCluster.pivot = new Vector2(1f, 1f);
        topRightCluster.sizeDelta = new Vector2(90f, 160f);
        topRightCluster.anchoredPosition = Vector2.zero;

        CrearBotonTactil(topRightCluster, "Btn_Pause", "⏸",
            new Vector2(-38f, -38f), 54f, 20,
            new Color(0.08f, 0.08f, 0.12f, 0.88f), new Color(0.92f, 0.76f, 0.32f, 0.95f),
            font, () =>
            {
                MobileInput.ResetHeldStates();
                PauseAndVictoryUI.EnsureInstance().AlternarPausa();
            });

        CrearBotonTactil(topRightCluster, "Btn_Shop", "🛒",
            new Vector2(-38f, -104f), 54f, 20,
            new Color(0.12f, 0.10f, 0.06f, 0.88f), new Color(0.92f, 0.76f, 0.32f, 0.95f),
            font, () =>
            {
                MobileInput.ResetHeldStates();
                ShopManager.EnsureInstance().AlternarTienda();
            });
    }

    private void CrearJoystick(Transform parent)
    {
        GameObject baseObj = new GameObject("Joystick_Base");
        baseObj.transform.SetParent(parent, false);
        Image baseImg = baseObj.AddComponent<Image>();
        baseImg.sprite = CrearSpriteCirculoBorde(new Color(0.05f, 0.06f, 0.10f, 0.65f), new Color(0.92f, 0.74f, 0.28f, 0.85f), 128);
        RectTransform baseRt = baseObj.GetComponent<RectTransform>();
        baseRt.anchorMin = Vector2.zero;
        baseRt.anchorMax = Vector2.zero;
        baseRt.pivot = new Vector2(0.5f, 0.5f);
        baseRt.sizeDelta = new Vector2(164f, 164f);
        baseRt.anchoredPosition = new Vector2(124f, 124f);

        GameObject handleObj = new GameObject("Joystick_Handle");
        handleObj.transform.SetParent(baseObj.transform, false);
        Image handleImg = handleObj.AddComponent<Image>();
        handleImg.sprite = CrearSpriteCirculoBorde(new Color(0.18f, 0.32f, 0.44f, 0.90f), new Color(1f, 0.88f, 0.42f, 1f), 96);
        handleImg.raycastTarget = false;
        RectTransform handleRt = handleObj.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(74f, 74f);
        handleRt.anchoredPosition = Vector2.zero;

        MobileVirtualJoystick joy = baseObj.AddComponent<MobileVirtualJoystick>();
        joy.Configurar(baseRt, handleRt, 58f, 0.12f);
    }

    private GameObject CrearBotonTactil(
        Transform parent,
        string nombre,
        string texto,
        Vector2 anchoredPos,
        float diameter,
        int fontSize,
        Color bgColor,
        Color borderColor,
        Font font,
        Action onDown,
        Action onUp = null)
    {
        GameObject btnObj = new GameObject(nombre);
        btnObj.transform.SetParent(parent, false);

        Image img = btnObj.AddComponent<Image>();
        img.sprite = CrearSpriteCirculoBorde(bgColor, borderColor, 96);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = parent.GetComponent<RectTransform>().anchorMin;
        rt.anchorMax = parent.GetComponent<RectTransform>().anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(diameter, diameter);
        rt.anchoredPosition = anchoredPos;

        GameObject txtObj = new GameObject("Label");
        txtObj.transform.SetParent(btnObj.transform, false);
        Text t = txtObj.AddComponent<Text>();
        t.font = font;
        t.text = texto;
        t.fontSize = fontSize;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(4f, 4f);
        txtRt.offsetMax = new Vector2(-4f, -4f);

        Outline ol = txtObj.AddComponent<Outline>();
        ol.effectColor = new Color(0f, 0f, 0f, 0.9f);
        ol.effectDistance = new Vector2(1.2f, -1.2f);

        MobileTouchButton touchBtn = btnObj.AddComponent<MobileTouchButton>();
        touchBtn.Configurar(rt, onDown, onUp);

        return btnObj;
    }

    private static Sprite CrearSpriteCirculoBorde(Color fondo, Color borde, int size = 96)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float maxR = size * 0.48f;
        float borderW = Mathf.Max(2.5f, size * 0.045f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                if (d > maxR)
                {
                    pixels[y * size + x] = Color.clear;
                }
                else if (d >= maxR - borderW)
                {
                    float edgeAlpha = Mathf.Clamp01((maxR - d) * 1.4f);
                    Color c = borde;
                    c.a *= edgeAlpha;
                    pixels[y * size + x] = c;
                }
                else
                {
                    float shade = 1f - (d / maxR) * 0.18f;
                    pixels[y * size + x] = new Color(fondo.r * shade, fondo.g * shade, fondo.b * shade, fondo.a);
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.hideFlags = HideFlags.HideAndDontSave;
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        sp.hideFlags = HideFlags.HideAndDontSave;
        return sp;
    }
}

/// <summary>
/// Joystick virtual multi-touch que sigue exclusivamente al dedo que lo presionó.
/// </summary>
public class MobileVirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private RectTransform baseRt;
    private RectTransform handleRt;
    private float maxRadius = 58f;
    private float deadZone = 0.12f;
    private int activePointerId = int.MinValue;

    public void Configurar(RectTransform baseRect, RectTransform handleRect, float radius, float dead)
    {
        baseRt = baseRect;
        handleRt = handleRect;
        maxRadius = radius;
        deadZone = dead;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointerId != int.MinValue) return;
        activePointerId = eventData.pointerId;
        UpdateJoystick(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;
        UpdateJoystick(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;
        ResetJoystick();
    }

    private void OnDisable()
    {
        ResetJoystick();
    }

    private void UpdateJoystick(PointerEventData eventData)
    {
        if (baseRt == null || handleRt == null) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRt, eventData.position, eventData.pressEventCamera, out Vector2 localPos))
            return;

        Vector2 clamped = Vector2.ClampMagnitude(localPos, maxRadius);
        handleRt.anchoredPosition = clamped;

        Vector2 normalized = clamped / Mathf.Max(1f, maxRadius);
        if (normalized.magnitude < deadZone)
        {
            MobileInput.JoystickDirection = Vector2.zero;
        }
        else
        {
            float scaledMag = Mathf.Clamp01((normalized.magnitude - deadZone) / (1f - deadZone));
            MobileInput.JoystickDirection = normalized.normalized * scaledMag;
        }
    }

    private void ResetJoystick()
    {
        activePointerId = int.MinValue;
        if (handleRt != null) handleRt.anchoredPosition = Vector2.zero;
        MobileInput.JoystickDirection = Vector2.zero;
    }
}

/// <summary>
/// Botón táctil multi-touch con respuesta visual de pulsación y soporte de mantener presionado.
/// </summary>
public class MobileTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private RectTransform targetRt;
    private Action onDownAction;
    private Action onUpAction;
    private int activePointerId = int.MinValue;

    public void Configurar(RectTransform rt, Action onDown, Action onUp = null)
    {
        targetRt = rt;
        onDownAction = onDown;
        onUpAction = onUp;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointerId != int.MinValue) return;
        activePointerId = eventData.pointerId;
        if (targetRt != null) targetRt.localScale = new Vector3(0.92f, 0.92f, 1f);
        onDownAction?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;
        ReleaseButton();
    }

    private void OnDisable()
    {
        ReleaseButton();
    }

    private void ReleaseButton()
    {
        if (activePointerId == int.MinValue) return;
        activePointerId = int.MinValue;
        if (targetRt != null) targetRt.localScale = Vector3.one;
        onUpAction?.Invoke();
    }
}
