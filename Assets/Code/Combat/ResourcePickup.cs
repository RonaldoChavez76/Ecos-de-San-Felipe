using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Objeto recolectable en el suelo para recursos dinámicos:
/// Maná, Flechas, Monedas de Oro y Corazones Curativos.
/// Incluye animación suave de levitación, pulso, doble validación de colisiones (Trigger + Proximidad)
/// y extracción de sprites reales.
/// </summary>
public class ResourcePickup : MonoBehaviour
{
    public enum ResourceType
    {
        Health,
        Mana,
        Arrows,
        Gold
    }

    [Header("Tipo de Recurso")]
    public ResourceType resourceType = ResourceType.Gold;
    public float amount = 10f;
    public float targetScale = 0.60f;

    private SpriteRenderer sr;
    private Vector3 basePos;
    private float spawnTime;
    private bool recolectado = false;
    private Transform playerTransform;

    private static readonly Dictionary<ResourceType, Sprite> cachedSprites = new Dictionary<ResourceType, Sprite>();

    public static GameObject SpawnPickup(Vector3 position, ResourceType type, float amount, Transform parent = null)
    {
        string namePrefix = type switch
        {
            ResourceType.Health => "Pickup_Corazon",
            ResourceType.Mana => "Pickup_Mana",
            ResourceType.Arrows => "Pickup_Flechas",
            ResourceType.Gold => "Pickup_Oro",
            _ => "Pickup_Recurso"
        };

        GameObject go = new GameObject($"{namePrefix}_{Random.Range(100, 999)}");
        if (parent != null) go.transform.SetParent(parent);
        go.transform.position = position;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = GetSpriteForType(type);
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = Mathf.RoundToInt(-position.y * 10f) + 2;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.45f;

        // Físicas 2D para dispersión aleatoria
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearDamping = 4.5f;
        rb.freezeRotation = true;

        Vector2 dirImpulso = Random.insideUnitCircle;
        if (dirImpulso.sqrMagnitude < 0.05f) dirImpulso = Random.insideUnitCircle.normalized;
        rb.AddForce(dirImpulso.normalized * Random.Range(2.0f, 3.8f), ForceMode2D.Impulse);

        ResourcePickup rp = go.AddComponent<ResourcePickup>();
        rp.resourceType = type;
        rp.amount = amount;
        rp.sr = renderer;

        // Escala reducida y proporcional a los sprites del juego
        float baseScale = type switch
        {
            ResourceType.Health => 0.65f,
            ResourceType.Mana   => 0.60f,
            ResourceType.Arrows => 0.60f,
            ResourceType.Gold   => 0.58f,
            _                   => 0.60f
        };
        rp.targetScale = baseScale;
        go.transform.localScale = Vector3.one * baseScale;

        return go;
    }

    public static GameObject SpawnMana(Vector3 pos, float mana = 25f, Transform parent = null) =>
        SpawnPickup(pos, ResourceType.Mana, mana, parent);

    public static GameObject SpawnArrows(Vector3 pos, int count = 3, Transform parent = null) =>
        SpawnPickup(pos, ResourceType.Arrows, count, parent);

    public static GameObject SpawnGold(Vector3 pos, int gold = 10, Transform parent = null) =>
        SpawnPickup(pos, ResourceType.Gold, gold, parent);

    public static GameObject SpawnHealth(Vector3 pos, float heal = 25f, Transform parent = null) =>
        SpawnPickup(pos, ResourceType.Health, heal, parent);

    /// <summary>
    /// Drop Table balanceada para la muerte de enemigos comunes y sombríos.
    /// Incorpora probabilidad de no soltar nada (25%) y porcentajes moderados
    /// para oro, flechas, maná y salud, expulsados con salto físico disperso.
    /// </summary>
    public static void SpawnEnemyLoot(Vector3 pos, Transform parent = null)
    {
        // 25% de probabilidad: No suelta botín
        if (Random.value < 0.25f)
        {
            return;
        }

        // 1. Oro: 52% de probabilidad (cantidad moderada: 4 a 12 de oro)
        if (Random.value < 0.52f)
        {
            SpawnGold(pos, Random.Range(4, 13), parent);
        }

        // 2. Flechas: 22% de probabilidad (2 a 4 flechas)
        if (Random.value < 0.22f)
        {
            SpawnArrows(pos, Random.Range(2, 5), parent);
        }

        // 3. Maná: 20% de probabilidad (15 a 25 puntos)
        if (Random.value < 0.20f)
        {
            SpawnMana(pos, Random.Range(15f, 26f), parent);
        }

        // 4. Corazón Curativo: 15% de probabilidad (restaura 20 HP)
        if (Random.value < 0.15f)
        {
            SpawnHealth(pos, 20f, parent);
        }
    }

    /// <summary>
    /// Genera recompensas con parámetros personalizados (usado en cofres o eventos especiales).
    /// </summary>
    public static void SpawnDrops(
        Vector3 pos,
        float chanceOro = 1.0f, int minOro = 8, int maxOro = 18,
        float chanceFlechas = 0.40f, int cantidadFlechas = 3,
        float chanceMana = 0.40f, float cantidadMana = 25f,
        float chanceVida = 0.25f, float cantidadVida = 25f,
        Transform parent = null)
    {
        if (Random.value <= chanceOro)
        {
            SpawnGold(pos, Random.Range(minOro, maxOro + 1), parent);
        }

        if (Random.value <= chanceFlechas)
        {
            SpawnArrows(pos, cantidadFlechas, parent);
        }

        if (Random.value <= chanceMana)
        {
            SpawnMana(pos, cantidadMana, parent);
        }

        if (Random.value <= chanceVida)
        {
            SpawnHealth(pos, cantidadVida, parent);
        }
    }

    public static Sprite GetSpriteForType(ResourceType type)
    {
        if (cachedSprites.TryGetValue(type, out Sprite sp) && sp != null) return sp;

        Sprite result = null;
        switch (type)
        {
            case ResourceType.Health:
                result = HeartPickup.GetHeartSprite();
                break;

            case ResourceType.Mana:
                Texture2D gemTex = ZoneWaveSpawner.LoadSpriteTexture("GemPurple");
                if (gemTex != null)
                {
                    result = Sprite.Create(gemTex, new Rect(0, 0, gemTex.width, gemTex.height), new Vector2(0.5f, 0.5f), 16f);
                }
                else
                {
                    result = BaseEnemyAI.CreateCircleSprite(new Color(0.2f, 0.7f, 1f, 1f));
                }
                break;

            case ResourceType.Arrows:
                Texture2D arrowTex = ZoneWaveSpawner.LoadSpriteTexture("Arrow");
                if (arrowTex != null)
                {
                    result = Sprite.Create(arrowTex, new Rect(0, 0, arrowTex.width, arrowTex.height), new Vector2(0.5f, 0.5f), 16f);
                }
                else
                {
                    result = BaseEnemyAI.CreateCircleSprite(new Color(1f, 0.85f, 0.3f, 1f));
                }
                break;

            case ResourceType.Gold:
                Texture2D goldTex = ZoneWaveSpawner.LoadSpriteTexture("BarGold");
                if (goldTex != null)
                {
                    result = Sprite.Create(goldTex, new Rect(0, 0, goldTex.width, goldTex.height), new Vector2(0.5f, 0.5f), 16f);
                }
                else
                {
                    result = BaseEnemyAI.CreateCircleSprite(new Color(1f, 0.85f, 0.2f, 1f));
                }
                break;
        }

        if (result != null) cachedSprites[type] = result;
        return result;
    }

    private void Start()
    {
        basePos = transform.position;
        spawnTime = Time.time;
    }

    private void Update()
    {
        if (recolectado) return;

        float pulse = targetScale * (1.0f + Mathf.Sin((Time.time - spawnTime) * 4.5f) * 0.08f);
        transform.localScale = Vector3.one * pulse;
        if (sr != null)
        {
            sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 10f) + 2;
        }

        // Verificación de proximidad con Ronan (garantía secundaria ante colisiones de física)
        if (playerTransform == null)
        {
            PlayerMovement p = FindAnyObjectByType<PlayerMovement>();
            if (p != null) playerTransform = p.transform;
        }

        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist <= 1.0f)
            {
                PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
                if (pm != null) Recolectar(pm);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ProcesarTrigger(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        ProcesarTrigger(other);
    }

    private void ProcesarTrigger(Collider2D other)
    {
        if (recolectado || other == null) return;

        PlayerMovement pm = other.GetComponent<PlayerMovement>() 
                         ?? other.GetComponentInParent<PlayerMovement>()
                         ?? other.GetComponentInChildren<PlayerMovement>();

        if (pm == null && (other.CompareTag("Player") || other.transform.root.CompareTag("Player") || other.name.Contains("Player") || other.name.Contains("Ronan")))
        {
            pm = FindAnyObjectByType<PlayerMovement>();
        }

        if (pm != null)
        {
            Recolectar(pm);
        }
    }

    public void Recolectar(PlayerMovement pm)
    {
        if (recolectado) return;

        bool recogido = false;

        switch (resourceType)
        {
            case ResourceType.Gold:
                int cantOro = Mathf.Max(1, Mathf.RoundToInt(amount));
                if (pm != null)
                {
                    pm.AgregarOro(cantOro);
                }
                else if (SaveStateManager.Instance != null)
                {
                    SaveStateManager.Instance.AgregarOro(cantOro);
                }
                recogido = true;
                break;

            case ResourceType.Health:
                if (pm != null && pm.currentHealth < pm.maxHealth)
                {
                    pm.CurarSalud(amount);
                    pm.MostrarAvisoRecurso($"♥ +{amount:0} Vida");
                    recogido = true;
                }
                break;

            case ResourceType.Mana:
                if (pm != null && pm.manaActual < pm.manaMaximo)
                {
                    pm.RestaurarMana(amount);
                    pm.MostrarAvisoRecurso($"💧 +{amount:0} Maná");
                    recogido = true;
                }
                break;

            case ResourceType.Arrows:
                if (pm != null && pm.flechas < pm.maxFlechas)
                {
                    int cantFlechas = Mathf.Max(1, (int)amount);
                    pm.AgregarFlechas(cantFlechas);
                    pm.MostrarAvisoRecurso($"🏹 +{cantFlechas} Flechas");
                    recogido = true;
                }
                break;
        }

        if (recogido)
        {
            recolectado = true;
            AudioManager.Instance?.PlaySecretSound();
            Destroy(gameObject);
        }
    }
}
