using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gestiona la transición suave con efecto Fade entre zonas o escenas dedicadas
/// (ej. Entrar y salir de la escena ParroquiaInterior.unity desde SampleScene.unity).
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
