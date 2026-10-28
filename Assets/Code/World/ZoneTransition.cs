using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gestiona la transición suave con efecto Fade entre zonas o escenas
/// (ej. Entrada/Salida de la Parroquia, paso al Mercado o al Cerro del Fraile).
/// </summary>
public class ZoneTransition : MonoBehaviour
{
    public enum TransitionType { MismoMapaTeleport, CambioDeEscena }

    [Header("Tipo de Transición")]
    public TransitionType tipoTransicion = TransitionType.MismoMapaTeleport;
    public LevelManager.ZoneId zonaDestino = LevelManager.ZoneId.ParroquiaInterior;

    [Header("Destino (Mismo Mapa)")]
    public Vector2 coordenadasDestino = new Vector2(0f, 60f);
    public Transform transformDestino;

    [Header("Destino (Otra Escena)")]
    public string nombreEscenaDestino = "SampleScene";

    [Header("Tiempos de Fade")]
    public float duracionFadeOut = 0.35f;
    public float duracionEspera = 0.15f;
    public float duracionFadeIn = 0.35f;

    [Header("Dirección de salida de Ronan tras teleport")]
    public Vector2 direccionSalida = Vector2.up;

    private static bool transicionEnCurso = false;
    private static Canvas fadeCanvas;
    private static Image fadeImage;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (transicionEnCurso) return;

        if (collision.CompareTag("Player") || collision.GetComponent<PlayerMovement>() != null)
        {
            StartCoroutine(EjecutarTransicion(collision.gameObject));
        }
    }

    private IEnumerator EjecutarTransicion(GameObject player)
    {
        transicionEnCurso = true;
        AsegurarFadeOverlay();

        // Bloquear movimiento de Ronan temporalmente
        PlayerMovement pm = player.GetComponent<PlayerMovement>();
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (pm != null) pm.enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // 1. Fade a negro
        float t = 0f;
        while (t < duracionFadeOut)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / duracionFadeOut);
            fadeImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        fadeImage.color = Color.black;

        yield return new WaitForSeconds(duracionEspera);

        // 2. Realizar el cambio
        if (tipoTransicion == TransitionType.MismoMapaTeleport)
        {
            Vector2 posFinal = transformDestino != null ? (Vector2)transformDestino.position : coordenadasDestino;
            player.transform.position = posFinal;
            if (rb != null) rb.position = posFinal;

            LevelManager.Instance?.CambiarZona(zonaDestino, posFinal);

            // Centrar cámara al instante
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(posFinal.x, posFinal.y, -10f);
            }
        }
        else
        {
            SceneManager.LoadScene(nombreEscenaDestino);
        }

        // 3. Fade desde negro
        t = 0f;
        while (t < duracionFadeIn)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - (t / duracionFadeIn));
            fadeImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        fadeImage.color = Color.clear;

        // Restaurar movimiento
        if (pm != null) pm.enabled = true;
        transicionEnCurso = false;
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
