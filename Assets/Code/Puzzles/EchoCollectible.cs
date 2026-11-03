using System.Collections;
using UnityEngine;

/// <summary>
/// Coleccionable sagrado de los Ecos de San Felipe.
/// Al recogerlo, restaura recuerdos, incrementa +25 HP permanentemente a Ronan,
/// restaura su vida, activa la animación de victoria con el eco en manos y despliega
/// la pantalla de nivel completado.
/// </summary>
public class EchoCollectible : MonoBehaviour
{
    [Header("Identificador del Eco")]
    [Range(1, 3)]
    public int indiceEco = 1;
    public string nombreEco = "Eco de la Fe";

    [Header("Efecto de Levitación y Pulso")]
    public float velocidadLevitacion = 2.5f;
    public float amplitudLevitacion = 0.20f;
    public float velocidadPulso = 3.0f;

    [Header("Secuencia de Recolección")]
    [Tooltip("Segundos que tarda el Eco en subir hasta las manos de Ronan.")]
    public float duracionAscenso = 1.0f;
    [Tooltip("Pausa (segundos) con Ronan celebrando antes de mostrar la pantalla de Nivel Completado.")]
    public float retrasoPantallaVictoria = 1.8f;

    private Vector3 posicionInicial;
    private SpriteRenderer sr;
    private bool recolectado = false;

    private void Start()
    {
        posicionInicial = transform.position;
        sr = GetComponent<SpriteRenderer>();

        // Si ya fue recuperado en el SaveStateManager, desactivar
        if (SaveStateManager.Instance != null)
        {
            if (indiceEco == 1 && SaveStateManager.Instance.ecoFeRecuperado) gameObject.SetActive(false);
            if (indiceEco == 2 && SaveStateManager.Instance.ecoTradicionRecuperado) gameObject.SetActive(false);
            if (indiceEco == 3 && SaveStateManager.Instance.ecoMemoriaRecuperado) gameObject.SetActive(false);
        }

        AsegurarSpriteLuminoso();
    }

    private void Update()
    {
        if (recolectado) return;

        // Levitación flotante
        float nuevoY = posicionInicial.y + Mathf.Sin(Time.time * velocidadLevitacion) * amplitudLevitacion;
        transform.position = new Vector3(posicionInicial.x, nuevoY, posicionInicial.z);

        // Pulso de escala y color celeste/dorado
        float escala = 1f + Mathf.Sin(Time.time * velocidadPulso) * 0.12f;
        transform.localScale = new Vector3(escala, escala, 1f);

        if (sr != null)
        {
            float lerp = (Mathf.Sin(Time.time * velocidadPulso) + 1f) * 0.5f;
            sr.color = Color.Lerp(new Color(0f, 0.85f, 1f, 1f), new Color(1f, 0.95f, 0.5f, 1f), lerp);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (recolectado) return;

        if (collision.CompareTag("Player") || collision.GetComponent<PlayerMovement>() != null)
        {
            Recolectar(collision.gameObject);
        }
    }

    private void Recolectar(GameObject player)
    {
        if (recolectado) return;
        recolectado = true;
        PauseAndVictoryUI.SecuenciaEcoEnCurso = true; // impide que cualquier pantalla de victoria aparezca antes de tiempo

        // Desactivar colisiones (el sprite sigue visible: va a ascender hacia las manos de Ronan)
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 1. Guardar en SaveStateManager (+25 HP permanente y vida restaurada)
        SaveStateManager.Instance?.RecuperarEco(indiceEco);

        // 2. Curar y actualizar a Ronan
        PlayerMovement pm = player.GetComponent<PlayerMovement>();
        HealthComponent hc = player.GetComponent<HealthComponent>();
        if (SaveStateManager.Instance != null)
        {
            float nuevaVidaMax = SaveStateManager.Instance.ObtenerVidaMaximaTotal();
            if (pm != null)
            {
                pm.maxHealth = nuevaVidaMax;
                pm.currentHealth = nuevaVidaMax;
            }
            if (hc != null)
            {
                hc.ActualizarVidaMaxima(nuevaVidaMax);
                hc.Heal(nuevaVidaMax);
            }
        }

        Debug.Log($"<color=#FFDF00>[EchoCollectible] ¡{nombreEco} recolectado con éxito por Ronan!</color>");

        // 3. Ronan adopta la pose de victoria (el Eco aún no está en sus manos) y arranca la secuencia
        if (pm != null) pm.ActivarPoseDeVictoriaConEco(nombreEco, indiceEco, false);
        StartCoroutine(RutinaRecoleccion(pm));
    }

    /// <summary>
    /// Secuencia completa: el Eco asciende hasta las manos de Ronan → destello → Ronan lo sostiene en alto
    /// → pausa de celebración → pantalla de «Nivel Completado».
    /// </summary>
    private IEnumerator RutinaRecoleccion(PlayerMovement pm)
    {
        AudioManager.Instance?.PlayEchoSound();
        CrearExplosionLuminosa();

        // Fase 1: ascenso del Eco hasta las manos (la animación de recolección completa)
        Vector3 inicio = transform.position;
        float t = 0f;
        while (t < duracionAscenso)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duracionAscenso));
            Vector3 destino = pm != null ? pm.PosicionEcoEnManos : inicio + Vector3.up * 1.2f;
            transform.position = Vector3.Lerp(inicio, destino, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.35f;

            float escala = Mathf.Lerp(1.15f, 0.85f, k) + Mathf.Sin(Time.time * 14f) * 0.08f;
            transform.localScale = new Vector3(escala, escala, 1f);
            if (sr != null)
            {
                float lerp = (Mathf.Sin(Time.time * 12f) + 1f) * 0.5f;
                sr.color = Color.Lerp(new Color(0f, 0.95f, 1f, 1f), new Color(1f, 0.95f, 0.5f, 1f), lerp);
            }
            yield return null;
        }

        // Fase 2: el Eco llega a las manos → destello y Ronan lo sostiene en alto
        CrearExplosionLuminosa();
        AudioManager.Instance?.PlayBellSound();
        if (sr != null) sr.enabled = false;
        if (pm != null) pm.MostrarEcoEnManos(indiceEco);

        // Fase 3: pausa de celebración con la pose de victoria visible (el juego sigue corriendo)
        yield return new WaitForSeconds(retrasoPantallaVictoria);

        // Fase 4: pantalla de Nivel Completado
        PauseAndVictoryUI.SecuenciaEcoEnCurso = false;
        PauseAndVictoryUI ui = PauseAndVictoryUI.EnsureInstance();
        if (ui != null) ui.MostrarVictoriaDeNivel(indiceEco, nombreEco, pm);

        Destroy(gameObject, 0.1f);
    }
    private void AsegurarSpriteLuminoso()
    {
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        if (sr.sprite == null)
        {
            Texture2D tex = new Texture2D(24, 24);
            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 24; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(12, 12)) / 12f;
                    float a = Mathf.Clamp01(1f - dist);
                    tex.SetPixel(x, y, new Color(0.2f, 0.9f, 1f, a));
                }
            }
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 24, 24), new Vector2(0.5f, 0.5f), 16f);
        }
        sr.sortingOrder = 10;
    }

    private void CrearExplosionLuminosa()
    {
        GameObject burst = new GameObject("EchoBurst", typeof(SpriteRenderer));
        burst.transform.position = transform.position;
        burst.transform.localScale = Vector3.one * 1.5f;

        SpriteRenderer bsr = burst.GetComponent<SpriteRenderer>();
        bsr.color = new Color(0f, 1f, 1f, 0.9f);
        bsr.sortingOrder = 20;

        Texture2D tex = new Texture2D(32, 32);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16)) / 16f;
                float a = Mathf.Clamp01(1f - dist);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        bsr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16f);

        Destroy(burst, 0.35f);
    }
}
