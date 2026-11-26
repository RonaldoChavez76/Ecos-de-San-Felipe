using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Capa unificada de entrada y configuración para móvil (Android / iOS) y PC.
/// Soporta simultáneamente teclado/ratón y controles táctiles multi-touch (MobileControlsUI),
/// evitando clics accidentales sobre UI en pantalla táctil y configurando 60 FPS,
/// multitouch y orientación horizontal al arranque.
/// </summary>
public static class MobileInput
{
    private const string PREF_FORCE_TOUCH = "ForceTouchControls";
    private const string PREF_CONTROL_SCALE = "MobileControlScale";
    private const string PREF_CONTROL_OPACITY = "MobileControlOpacity";
    private const string PREF_TARGET_FPS = "MobileTargetFPS";

    // Estado del joystick virtual
    public static Vector2 JoystickDirection { get; set; }

    // Estado mantenido del escudo / bloqueo táctil
    public static bool IsBlockHeld { get; private set; }

    // Pulsos por frame para que múltiples scripts lean el mismo toque sin perderlo
    private static int attackFrame = -1;
    private static int blockDownFrame = -1;
    private static int dodgeFrame = -1;
    private static int assaultFrame = -1;
    private static int interactFrame = -1;
    private static int nextWeaponFrame = -1;
    private static int pauseFrame = -1;
    private static int shopFrame = -1;

    // Contexto del interactuable más cercano para mostrar el botón de interacción táctil
    private static int lastInteractableFrame = -1;
    private static string lastInteractableLabel = "Interactuar";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InicializarRuntimeMovil()
    {
        Input.multiTouchEnabled = true;

        int fps = TargetFPS;
        if (Application.isMobilePlatform || PlayerPrefs.HasKey(PREF_TARGET_FPS))
        {
            Application.targetFrameRate = fps;
        }

        if (Application.isMobilePlatform)
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }
    }

    /// <summary>Permite activar los controles táctiles en PC/Editor para pruebas.</summary>
    public static bool ForceTouchControls
    {
        get => PlayerPrefs.GetInt(PREF_FORCE_TOUCH, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(PREF_FORCE_TOUCH, value ? 1 : 0);
            PlayerPrefs.Save();
            if (!value) ResetHeldStates();
        }
    }

    public static bool IsTouchControlsActive =>
        Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld || ForceTouchControls;

    public static float ControlScale
    {
        get => Mathf.Clamp(PlayerPrefs.GetFloat(PREF_CONTROL_SCALE, 1.0f), 0.75f, 1.35f);
        set
        {
            PlayerPrefs.SetFloat(PREF_CONTROL_SCALE, Mathf.Clamp(value, 0.75f, 1.35f));
            PlayerPrefs.Save();
        }
    }

    public static float ControlOpacity
    {
        get => Mathf.Clamp(PlayerPrefs.GetFloat(PREF_CONTROL_OPACITY, 0.78f), 0.35f, 1.0f);
        set
        {
            PlayerPrefs.SetFloat(PREF_CONTROL_OPACITY, Mathf.Clamp(value, 0.35f, 1.0f));
            PlayerPrefs.Save();
        }
    }

    public static int TargetFPS
    {
        get => PlayerPrefs.GetInt(PREF_TARGET_FPS, 60) <= 30 ? 30 : 60;
    }

    public static void SetTargetFPS(int fps)
    {
        int clamped = fps <= 30 ? 30 : 60;
        PlayerPrefs.SetInt(PREF_TARGET_FPS, clamped);
        PlayerPrefs.Save();
        Application.targetFrameRate = clamped;
    }

    // ---- Disparadores desde MobileControlsUI ----
    public static void TriggerAttack() => attackFrame = Time.frameCount;
    public static void TriggerBlockDown() => blockDownFrame = Time.frameCount;
    public static void SetBlockHeld(bool held) => IsBlockHeld = held;
    public static void TriggerDodge() => dodgeFrame = Time.frameCount;
    public static void TriggerAssault() => assaultFrame = Time.frameCount;
    public static void TriggerInteract() => interactFrame = Time.frameCount;
    public static void TriggerNextWeapon() => nextWeaponFrame = Time.frameCount;
    public static void TriggerPause() => pauseFrame = Time.frameCount;
    public static void TriggerShop() => shopFrame = Time.frameCount;

    public static void ResetHeldStates()
    {
        JoystickDirection = Vector2.zero;
        IsBlockHeld = false;
    }

    // ---- Contexto interactuable ----
    public static void ReportInteractableNearby(string label = "Interactuar")
    {
        lastInteractableFrame = Time.frameCount;
        lastInteractableLabel = label;
    }

    public static bool HasNearbyInteractable => (Time.frameCount - lastInteractableFrame) <= 2;

    public static string NearbyInteractableLabel =>
        string.IsNullOrEmpty(lastInteractableLabel) ? "Interactuar" : lastInteractableLabel;

    // ---- Protección de puntero sobre UI ----
    public static bool IsPointerOverUI()
    {
        EventSystem es = EventSystem.current;
        if (es == null) return false;

        if (es.IsPointerOverGameObject()) return true;

        for (int i = 0; i < Input.touchCount; i++)
        {
            if (es.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                return true;
        }
        return false;
    }

    // ---- Lectura unificada (Teclado/Ratón + Táctil) ----
    public static Vector2 GetMovementAxis()
    {
        if (JoystickDirection.sqrMagnitude > 0.01f)
            return JoystickDirection;

        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }

    public static bool GetAttackDown(KeyCode key = KeyCode.J)
    {
        return Input.GetKeyDown(key)
            || (!IsTouchControlsActive && !IsPointerOverUI() && Input.GetMouseButtonDown(0))
            || attackFrame == Time.frameCount;
    }

    public static bool GetBlockDown(KeyCode key = KeyCode.K)
    {
        return Input.GetKeyDown(key)
            || (!IsTouchControlsActive && !IsPointerOverUI() && Input.GetMouseButtonDown(1))
            || blockDownFrame == Time.frameCount;
    }

    public static bool GetBlockHeld(KeyCode key = KeyCode.K)
    {
        return Input.GetKey(key)
            || (!IsTouchControlsActive && Input.GetMouseButton(1))
            || IsBlockHeld;
    }

    public static bool GetDodgeDown(KeyCode key = KeyCode.Space)
    {
        return Input.GetKeyDown(key) || dodgeFrame == Time.frameCount;
    }

    public static bool GetAssaultDown(KeyCode key = KeyCode.L)
    {
        return Input.GetKeyDown(key) || assaultFrame == Time.frameCount;
    }

    public static bool GetInteractDown(KeyCode key = KeyCode.E)
    {
        return Input.GetKeyDown(key) || interactFrame == Time.frameCount;
    }

    public static bool GetNextWeaponDown(KeyCode key = KeyCode.Q)
    {
        return Input.GetKeyDown(key) || nextWeaponFrame == Time.frameCount;
    }

    public static bool GetPauseDown()
    {
        return Input.GetKeyDown(KeyCode.Escape) || pauseFrame == Time.frameCount;
    }

    public static bool GetShopDown(KeyCode key = KeyCode.B)
    {
        return Input.GetKeyDown(key) || shopFrame == Time.frameCount;
    }
}
