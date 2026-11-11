using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class CheckpointTrigger : MonoBehaviour
{
    private static Sprite checkpointSprite;

    [Header("Checkpoint")]
    public LevelManager.ZoneId zona = LevelManager.ZoneId.ParroquiaExterior;
    public bool guardarAlEntrar = true;
    public float radioInteraccion = 1.25f;

    private float mensajeTimer;
    private float ultimoGuardado = -10f;
    private SpriteRenderer spriteRenderer;

    public static CheckpointTrigger Crear(string nombre, Vector2 posicion, LevelManager.ZoneId zona, Transform parent)
    {
        GameObject checkpoint = new GameObject(nombre);
        checkpoint.transform.SetParent(parent);
        checkpoint.transform.position = posicion;
        checkpoint.transform.localScale = Vector3.one;

        CheckpointTrigger trigger = checkpoint.AddComponent<CheckpointTrigger>();
        trigger.zona = zona;
        return trigger;
    }

    private void Awake()
    {
        CircleCollider2D trigger = GetComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = radioInteraccion;

        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = ObtenerSpriteCheckpoint();
        spriteRenderer.color = new Color(1f, 0.76f, 0.22f, 0.8f);
        spriteRenderer.sortingOrder = -10;
    }

    private void Update()
    {
        if (mensajeTimer > 0f) mensajeTimer -= Time.unscaledDeltaTime;

        if (spriteRenderer != null)
        {
            float pulso = 0.68f + Mathf.Sin(Time.unscaledTime * 3f) * 0.18f;
            spriteRenderer.color = new Color(1f, 0.76f, 0.22f, pulso);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (guardarAlEntrar && EsJugador(other))
        {
            Guardar();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (EsJugador(other) && Input.GetKeyDown(KeyCode.E))
        {
            Guardar();
        }
    }

    private void OnGUI()
    {
        if (mensajeTimer <= 0f || Camera.main == null) return;

        Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 0.9f);
        if (screenPosition.z <= 0f) return;

        Rect mensaje = new Rect(screenPosition.x - 145f, Screen.height - screenPosition.y - 20f, 290f, 34f);
        GUI.color = new Color(0.10f, 0.07f, 0.03f, 0.92f);
        GUI.Box(mensaje, GUIContent.none);
        GUI.color = new Color(1f, 0.82f, 0.30f, 1f);
        GUI.Label(mensaje, "PUNTO DE CONTROL GUARDADO", UITheme.Estilo(15, FontStyle.Bold, TextAnchor.MiddleCenter, GUI.color));
        GUI.color = Color.white;
    }

    private void Guardar()
    {
        if (Time.unscaledTime - ultimoGuardado < 1f) return;

        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
        if (player == null) return;

        LevelManager.EnsureInstance().currentZone = zona;
        SaveStateManager.EnsureInstance().RegistrarCheckpoint(zona.ToString(), transform.position, player);
        ultimoGuardado = Time.unscaledTime;
        mensajeTimer = 2.5f;
        AudioManager.Instance?.PlaySecretSound();
    }

    private static bool EsJugador(Collider2D other)
    {
        return other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null;
    }

    private static Sprite ObtenerSpriteCheckpoint()
    {
        if (checkpointSprite != null) return checkpointSprite;

        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float distancia = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                float alpha = Mathf.Clamp01(1f - (distancia - 3f) / 5f);
                texture.SetPixel(x, y, new Color(1f, 0.78f, 0.22f, alpha));
            }
        }

        texture.Apply();
        checkpointSprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 12f);
        return checkpointSprite;
    }
}
