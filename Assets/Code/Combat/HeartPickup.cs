using UnityEngine;

/// <summary>
/// Corazón curativo recolectable que usa el sprite real Assets/Sprites/Heart.png (celda col=4 en x=64 de 16x16)
/// para restaurar la salud de Ronan (+25 HP) durante la exploración y los combates por oleadas.
/// </summary>
public class HeartPickup : MonoBehaviour
{
    public float healAmount = 25f;
    private Vector3 basePos;
    private float spawnTime;
    private static Sprite cachedHeartSprite;

    public static GameObject SpawnHeart(Vector3 position, float heal = 25f, Transform parent = null)
    {
        GameObject go = new GameObject("Corazon_Curativo");
        if (parent != null) go.transform.SetParent(parent);
        go.transform.position = position;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetHeartSprite();
        sr.sortingOrder = Mathf.RoundToInt(-position.y * 10f) + 2;
        go.transform.localScale = new Vector3(1.25f, 1.25f, 1f);

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.48f;

        HeartPickup hp = go.AddComponent<HeartPickup>();
        hp.healAmount = heal;
        return go;
    }

    public static Sprite GetHeartSprite()
    {
        if (cachedHeartSprite != null) return cachedHeartSprite;

        Texture2D tex = ZoneWaveSpawner.LoadSpriteTexture("Heart");
        if (tex != null && tex.width >= 16 && tex.height >= 16)
        {
            int w = 16;
            int h = 16;
            Texture2D sub = new Texture2D(w, h, TextureFormat.RGBA32, false);
            sub.filterMode = FilterMode.Point;
            sub.wrapMode = TextureWrapMode.Clamp;
            if (tex.isReadable)
            {
                sub.SetPixels(tex.GetPixels(64, 0, w, h));
                sub.Apply();
                cachedHeartSprite = Sprite.Create(sub, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
                return cachedHeartSprite;
            }
        }

        cachedHeartSprite = BaseEnemyAI.CreateCircleSprite(new Color(0.95f, 0.18f, 0.25f, 1f));
        return cachedHeartSprite;
    }

    private void Start()
    {
        basePos = transform.position;
        spawnTime = Time.time;
    }

    private void Update()
    {
        float bob = Mathf.Sin((Time.time - spawnTime) * 4.2f) * 0.10f;
        float pulse = 1.20f + Mathf.Sin((Time.time - spawnTime) * 5.5f) * 0.10f;
        transform.position = basePos + new Vector3(0f, bob, 0f);
        transform.localScale = new Vector3(pulse, pulse, 1f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement pm = other.GetComponent<PlayerMovement>();
        if (pm == null) return;

        if (pm.currentHealth >= pm.maxHealth) return;

        pm.CurarSalud(healAmount);
        AudioManager.Instance?.PlayEchoSound();
        Destroy(gameObject);
    }
}

