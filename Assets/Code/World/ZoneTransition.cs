using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gestiona la transición suave con efecto Fade entre zonas o escenas dedicadas
/// (ej. Entrar y salir de la escena ParroquiaInterior.unity desde SampleScene.unity).
/// Incluye validación estricta de progresión por Ecos para impedir teletransportes adelantados.
/// </summary>
public class ZoneTransition : MonoBehaviour
{
    public enum TransitionType { MismoMapaTeleport, CambioDeEscena }

    [Header("Tipo de Transición")]
    public TransitionType tipoTransicion = TransitionType.MismoMapaTeleport;
    public LevelManager.ZoneId zonaDestino = LevelManager.ZoneId.ParroquiaInterior;

    [Header("Destino (Mismo Mapa o Nueva Escena)")]
    public Vector2 coordenadasDestino = new Vector2(0f, -14f);
    public Transform transformDestino;
    public bool usarCoordenadasEnNuevaEscena = true;

    [Header("Destino (Otra Escena)")]
    public string nombreEscenaDestino = "ParroquiaInterior";

    [Header("Requisito Estricto de Eco (0 = Ninguno, 1 = Fe, 2 = Tradición)")]
    public int ecoRequeridoParaCruzar = 0;
    public string tituloBloqueoEco = "Camino Sellado";
    [TextArea(2, 3)]
    public string mensajeBloqueoEco = "";

    [Header("Tiempos de Fade")]
    public float duracionFadeOut = 0.35f;
    public float duracionEspera = 0.15f;
    public float duracionFadeIn = 0.35f;

    [Header("Dirección de salida de Ronan tras teleport")]
    public Vector2 direccionSalida = Vector2.up;

    private static bool transicionEnCurso = false;
    private static Canvas fadeCanvas;
    private static Image fadeImage;
    private float ultimoAvisoTiempo = -10f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (transicionEnCurso) return;

        if (collision.CompareTag("Player") || collision.GetComponent<PlayerMovement>() != null)
        {
            if (!CumpleRequisitoDeEco())
            {
                MostrarAvisoBloqueo();
                return;
            }
            StartCoroutine(EjecutarTransicion(collision.gameObject));
        }
    }

    public bool CumpleRequisitoDeEco()
    {
        if (ecoRequeridoParaCruzar <= 0) return true;
        SaveStateManager ssm = SaveStateManager.Instance;
        if (ssm == null) return false;

        if (ecoRequeridoParaCruzar == 1) return ssm.ecoFeRecuperado;
        if (ecoRequeridoParaCruzar == 2) return ssm.ecoTradicionRecuperado;
        if (ecoRequeridoParaCruzar == 3) return ssm.ecoMemoriaRecuperado;
        return true;
    }

    private void MostrarAvisoBloqueo()
    {
        if (Time.time - ultimoAvisoTiempo < 1.2f) return;
        ultimoAvisoTiempo = Time.time;

        string msg = !string.IsNullOrEmpty(mensajeBloqueoEco)
            ? mensajeBloqueoEco
            : (ecoRequeridoParaCruzar == 1
                ? "Una densa niebla mágica bloquea el camino hacia el Mercado Municipal. Debes entrar a la Parroquia y recuperar primero el Eco de la Fe."
                : "El sendero hacia el Cerro del Fraile está sellado por el Gremio de Artesanos. Debes completar la cadena de trueque y recuperar el Eco de la Tradición.");

        DialogueManager.EnsureInstance().ShowDialogue(tituloBloqueoEco, new string[] { msg });
    }

    private IEnumerator EjecutarTransicion(GameObject player)
    {
        transicionEnCurso = true;
        AsegurarFadeOverlay();

        PlayerMovement pm = player != null ? player.GetComponent<PlayerMovement>() : null;
        Rigidbody2D rb = player != null ? player.GetComponent<Rigidbody2D>() : null;
        if (pm != null) pm.enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Asegurar que este objeto sobreviva si cambiamos de escena para completar el fade-in
        if (tipoTransicion == TransitionType.CambioDeEscena)
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        // 1. Fade a negro
        float t = 0f;
        while (t < duracionFadeOut)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(t / duracionFadeOut);
            fadeImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        fadeImage.color = Color.black;

        yield return new WaitForSecondsRealtime(duracionEspera);

        // 2. Realizar el cambio de zona o de escena
        Vector2 posFinal = transformDestino != null ? (Vector2)transformDestino.position : coordenadasDestino;

        if (tipoTransicion == TransitionType.MismoMapaTeleport)
        {
            if (player != null)
            {
                player.transform.position = posFinal;
                if (rb != null) rb.position = posFinal;
            }

            LevelManager.Instance?.CambiarZona(zonaDestino, posFinal);

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(posFinal.x, posFinal.y, -10f);
            }
        }
        else
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nombreEscenaDestino);
            while (asyncLoad != null && !asyncLoad.isDone)
            {
                yield return null;
            }

            // Esperar un frame para que Inicializar de la nueva escena termine
            yield return null;

            PlayerMovement nuevoPlayer = FindAnyObjectByType<PlayerMovement>();
            if (nuevoPlayer != null && usarCoordenadasEnNuevaEscena)
            {
                nuevoPlayer.transform.position = posFinal;
                Rigidbody2D nuevoRb = nuevoPlayer.GetComponent<Rigidbody2D>();
                if (nuevoRb != null)
                {
                    nuevoRb.position = posFinal;
                    nuevoRb.linearVelocity = Vector2.zero;
                }
            }

            LevelManager.Instance?.CambiarZona(zonaDestino, posFinal);

            Camera cam = Camera.main;
            if (cam != null && usarCoordenadasEnNuevaEscena)
            {
                cam.transform.position = new Vector3(posFinal.x, posFinal.y, -10f);
            }
        }

        // 3. Fade desde negro
        t = 0f;
        while (t < duracionFadeIn)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(1f - (t / duracionFadeIn));
            fadeImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        fadeImage.color = Color.clear;

        PlayerMovement activePlayer = FindAnyObjectByType<PlayerMovement>();
        if (activePlayer != null) activePlayer.enabled = true;

        transicionEnCurso = false;

        if (tipoTransicion == TransitionType.CambioDeEscena)
        {
            Destroy(gameObject);
        }
    }

    private static void AsegurarFadeOverlay()
    {
        if (fadeCanvas != null && fadeImage != null) return;

        GameObject canvasGO = new GameObject("FadeTransitionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        fadeCanvas = canvasGO.GetComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 999;
        DontDestroyOnLoad(canvasGO);

        GameObject imgGO = new GameObject("FadeImage", typeof(RectTransform), typeof(Image));
        imgGO.transform.SetParent(canvasGO.transform, false);
        fadeImage = imgGO.GetComponent<Image>();
        fadeImage.color = Color.clear;
        fadeImage.raycastTarget = false;

        RectTransform rt = imgGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

/// <summary>
/// Barrera física con BoxCollider2D sólido que bloquea el paso al siguiente nivel
/// hasta que Ronan haya recuperado el Eco correspondiente en SaveStateManager.
/// </summary>
public class EchoProgressionBarrier : MonoBehaviour
{
    public int requiredEchoIndex = 1;
    public string barrierTitle = "Barrera de Niebla Impenetrable";
    public string promptLabel = "[E] Inspeccionar Barrera";
    [TextArea(2, 4)]
    public string blockedMessage = "Una densa niebla mágica bloquea el camino hacia el Mercado Municipal. Debes entrar a la Parroquia y recuperar primero el Eco de la Fe.";

    private Transform playerTransform;
    private float cooldown = 0f;

    private void Start()
    {
        VerificarEstadoEco();
    }

    private void Update()
    {
        if (cooldown > 0f) cooldown -= Time.deltaTime;

        if (VerificarEstadoEco()) return;

        if (playerTransform == null)
        {
            PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
            if (pm != null) playerTransform = pm.transform;
            return;
        }

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist <= 2.4f)
        {
            MobileInput.ReportInteractableNearby("Ver");
            if (cooldown <= 0f && MobileInput.GetInteractDown(KeyCode.E))
            {
                MostrarMensajeBloqueo();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (VerificarEstadoEco()) return;
        if (collision.collider != null && (collision.collider.CompareTag("Player") || collision.collider.GetComponent<PlayerMovement>() != null))
        {
            if (cooldown <= 0f)
            {
                MostrarMensajeBloqueo();
            }
        }
    }

    private bool VerificarEstadoEco()
    {
        SaveStateManager ssm = SaveStateManager.Instance;
        if (ssm == null) return false;

        bool desbloqueado =
            (requiredEchoIndex == 1 && ssm.ecoFeRecuperado) ||
            (requiredEchoIndex == 2 && ssm.ecoTradicionRecuperado) ||
            (requiredEchoIndex == 3 && ssm.ecoMemoriaRecuperado);

        if (desbloqueado)
        {
            gameObject.SetActive(false);
            return true;
        }
        return false;
    }

    private void MostrarMensajeBloqueo()
    {
        cooldown = 1.2f;
        DialogueManager.EnsureInstance().ShowDialogue(barrierTitle, new string[] { blockedMessage });
    }

    private void OnGUI()
    {
        if (playerTransform == null) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist > 2.5f) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.4f);
        if (screenPos.z <= 0) return;

        Color c = new Color(1f, 0.78f, 0.35f, 1f);
        float w = UITheme.Px(290f);
        float h = UITheme.Px(34f);
        Rect r = new Rect(screenPos.x - w * 0.5f, Screen.height - screenPos.y - h * 0.5f, w, h);
        UITheme.Panel(r, c, 2f);
        string msgPortal = MobileInput.IsTouchControlsActive ? "Toca ✋ Ver" : promptLabel;
        GUI.Label(r, msgPortal, UITheme.Estilo(13, FontStyle.Bold, TextAnchor.MiddleCenter, c));
    }
}
