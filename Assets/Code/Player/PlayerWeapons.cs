using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Tipo de arma de Ronan. Determina qué columnas de la hoja Puny se reproducen al atacar.</summary>
public enum WeaponType
{
    Sword,  // Cuerpo a cuerpo   — columnas 4-7  (4 cuadros)
    Bow,    // A distancia       — columnas 8-12 (5 cuadros)
    Staff   // Mágica a distancia — columnas 13-15 (3 cuadros)
}

/// <summary>
/// Definición de datos de un arma. Añadir un arma nueva = añadir un elemento a la lista de
/// <see cref="PlayerMovement.armas"/> (o a <see cref="CrearArmasPorDefecto"/>); no hace falta tocar la lógica de combate.
/// </summary>
[Serializable]
public class WeaponDefinition
{
    [Header("Identidad")]
    public string nombre = "Espada";
    public WeaponType tipo = WeaponType.Sword;
    public string descripcion = "";
    public bool desbloqueada = true;

    [Header("Animación (hoja Puny 24x8 de Ronan)")]
    public int columnaInicial = 4;
    public int cuadros = 4;

    [Header("Estadísticas")]
    public float multiplicadorDanio = 1f;
    public float duracionAtaque = 0.25f;
    public float enfriamiento = 0f;
    public float alcance = 1.1f;
    public float empuje = 5f;

    [Header("Coste de Recursos")]
    public int costoFlechas = 0;
    public float costoMana = 0f;

    [Header("Proyectil (solo armas a distancia)")]
    public bool esADistancia = false;
    public float velocidadProyectil = 11f;
    public float distanciaMaxima = 9f;
    public float radioProyectil = 0.18f;
    [Tooltip("Nombre (sin extensión) de un PNG en Assets/Sprites. El báculo usa su orbe arcano procedural.")]
    public string spriteProyectil = "";
    public Color colorProyectil = Color.white;
    public float escalaProyectil = 1f;

    public static List<WeaponDefinition> CrearArmasPorDefecto()
    {
        return new List<WeaponDefinition>
        {
            new WeaponDefinition
            {
                nombre = "Espada de San Felipe",
                tipo = WeaponType.Sword,
                descripcion = "Tajo cuerpo a cuerpo equilibrado.",
                desbloqueada = true,
                columnaInicial = 4, cuadros = 4,
                multiplicadorDanio = 1.0f, duracionAtaque = 0.25f, alcance = 1.1f, empuje = 5f,
                costoFlechas = 0, costoMana = 0f
            },
            new WeaponDefinition
            {
                nombre = "Arco del Atrio",
                tipo = WeaponType.Bow,
                descripcion = "Flecha a distancia, daño moderado. Consume 1 flecha.",
                desbloqueada = false, // Bloqueada inicialmente; se compra en la Tienda con oro
                columnaInicial = 8, cuadros = 5,
                multiplicadorDanio = 0.85f, duracionAtaque = 0.36f, enfriamiento = 0.20f, empuje = 2.5f,
                costoFlechas = 1, costoMana = 0f,
                esADistancia = true, velocidadProyectil = 13f, distanciaMaxima = 11f, radioProyectil = 0.20f,
                spriteProyectil = "Arrow", colorProyectil = Color.white, escalaProyectil = 0.85f
            },
            new WeaponDefinition
            {
                nombre = "Báculo del Eco",
                tipo = WeaponType.Staff,
                descripcion = "Orbe arcano de gran daño. Consume 15 de Maná.",
                desbloqueada = false, // Bloqueada inicialmente; se compra en la Tienda con oro
                columnaInicial = 13, cuadros = 3,
                multiplicadorDanio = 1.40f, duracionAtaque = 0.42f, enfriamiento = 0.35f, empuje = 4f,
                costoFlechas = 0, costoMana = 15f,
                esADistancia = true, velocidadProyectil = 8.5f, distanciaMaxima = 9f, radioProyectil = 0.30f,
                spriteProyectil = "", colorProyectil = new Color(0.45f, 0.85f, 1f, 1f), escalaProyectil = 1.25f
            }
        };
    }
}

/// <summary>Utilidades de daño compartidas por ataques cuerpo a cuerpo, contraataques y proyectiles del jugador.</summary>
public static class PlayerCombatUtil
{
    /// <summary>Aplica daño al collider si pertenece a un enemigo. Devuelve true si impactó a algo dañable.</summary>
    public static bool AplicarDanio(Collider2D col, float danio, Vector2 direccion, float empuje, GameObject origen)
    {
        if (col == null || col.gameObject == origen) return false;
        if (col.CompareTag("Player")) return false;

        GameObject targetObj = col.gameObject;

        // 1. Enemigos clásicos del builder (SombraLanzadoraAI / SombraErranteAI)
        SombraLanzadoraAI lanzadora = targetObj.GetComponent<SombraLanzadoraAI>() ?? targetObj.GetComponentInParent<SombraLanzadoraAI>();
        if (lanzadora != null)
        {
            lanzadora.RecibirDanio(danio);
            return true;
        }

        SombraErranteAI errante = targetObj.GetComponent<SombraErranteAI>() ?? targetObj.GetComponentInParent<SombraErranteAI>();
        if (errante != null)
        {
            errante.RecibirDanio(danio);
            return true;
        }

        // 2. Enemigos basados en BaseEnemyAI y jefes con HealthComponent
        BaseEnemyAI baseEnemy = targetObj.GetComponent<BaseEnemyAI>() ?? targetObj.GetComponentInParent<BaseEnemyAI>();
        if (baseEnemy != null)
        {
            baseEnemy.RecibirDanio(danio);
            return true;
        }

        HealthComponent hc = targetObj.GetComponent<HealthComponent>() ?? targetObj.transform.root.GetComponent<HealthComponent>();
        if (hc != null && !hc.isPlayer)
        {
            hc.TakeDamage(danio, direccion, empuje, origen);
            return true;
        }

        if (col.CompareTag("Enemy") || col.name.Contains("Sombra"))
        {
            col.SendMessage("RecibirDanio", danio, SendMessageOptions.DontRequireReceiver);
            return true;
        }
        return false;
    }
}

/// <summary>Proyectil disparado por Ronan (flecha u orbe mágico). Detecta impactos por superposición.</summary>
public class PlayerProjectile : MonoBehaviour
{
    private Vector2 direccion;
    private float velocidad;
    private float danio;
    private float empuje;
    private float distanciaRestante;
    private float radio;
    private GameObject origen;
    private static readonly Dictionary<string, Sprite> cacheSprites = new Dictionary<string, Sprite>();
    private static Sprite spriteOrbeArcano;
    private static Material materialEstela;
    private WeaponType tipoArma;

    public static PlayerProjectile Disparar(WeaponDefinition arma, Vector2 posicion, Vector2 dir, float danio, GameObject origen, int ordenBase)
    {
        GameObject go = new GameObject("Proyectil_" + arma.nombre.Replace(' ', '_'));
        go.transform.position = posicion;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = ordenBase + 2;
        sr.color = arma.colorProyectil;

        Sprite sprite = null;
        bool apuntaDiagonal = false;
        if (arma.tipo == WeaponType.Staff)
        {
            sprite = ObtenerSpriteOrbeArcano();
        }
        else if (!string.IsNullOrEmpty(arma.spriteProyectil))
        {
            if (!cacheSprites.TryGetValue(arma.spriteProyectil, out sprite) || sprite == null)
            {
                Texture2D tex = ZoneWaveSpawner.LoadSpriteTexture(arma.spriteProyectil);
                if (tex != null)
                {
                    float ppu = arma.tipo == WeaponType.Bow ? 24f : 16f;
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
                    cacheSprites[arma.spriteProyectil] = sprite;
                }
            }
            apuntaDiagonal = sprite != null; // Arrow/Kunai/Shuriken de la colección apuntan hacia arriba-derecha (45°)
        }
        if (sprite == null)
        {
            sprite = BaseEnemyAI.CreateCircleSprite(Color.white);
        }
        sr.sprite = sprite;

        float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, 0f, arma.tipo == WeaponType.Staff ? 0f : (apuntaDiagonal ? angulo - 45f : angulo));

        float escalaFinal = arma.tipo == WeaponType.Bow
            ? 0.85f
            : (arma.tipo == WeaponType.Staff ? 1f : arma.escalaProyectil);
        go.transform.localScale = Vector3.one * escalaFinal;

        CrearEstela(go, arma, sr.sortingOrder);

        PlayerProjectile p = go.AddComponent<PlayerProjectile>();
        p.direccion = dir.normalized;
        p.velocidad = arma.velocidadProyectil;
        p.danio = danio;
        p.empuje = arma.empuje;
        p.distanciaRestante = arma.distanciaMaxima;
        p.radio = arma.radioProyectil;
        p.origen = origen;
        p.tipoArma = arma.tipo;
        return p;
    }

    private void Update()
    {
        if (tipoArma == WeaponType.Staff)
        {
            transform.Rotate(0f, 0f, 360f * Time.deltaTime);
            float pulso = 1f + Mathf.PingPong(Time.time * 6f, 0.25f);
            transform.localScale = Vector3.one * pulso;
        }

        float paso = velocidad * Time.deltaTime;
        transform.position += (Vector3)(direccion * paso);
        distanciaRestante -= paso;

        Collider2D[] buffer = Physics2D.OverlapCircleAll(transform.position, radio);
        for (int i = 0; i < buffer.Length; i++)
        {
            Collider2D c = buffer[i];
            if (c == null || c.gameObject == origen || c.transform.IsChildOf(origen.transform)) continue;
            if (c.CompareTag("Player")) continue;

            BlueCrossSealNode sello = c.GetComponent<BlueCrossSealNode>() ?? c.GetComponentInParent<BlueCrossSealNode>();
            if (sello != null && tipoArma != WeaponType.Sword)
            {
                sello.Purificar();
                if (tipoArma == WeaponType.Staff) CrearEstallidoArcano(transform.position);
                Destroy(gameObject);
                return;
            }

            if (PlayerCombatUtil.AplicarDanio(c, danio, direccion, empuje, origen))
            {
                origen.GetComponent<PlayerMovement>()?.AñadirCargaAsalto(8f);
                if (tipoArma == WeaponType.Staff) CrearEstallidoArcano(transform.position);
                Destroy(gameObject);
                return;
            }

            // Muros y decorados sólidos detienen el proyectil (los triggers y pickups no)
            if (!c.isTrigger && c.GetComponent<HealthComponent>() == null)
            {
                if (tipoArma == WeaponType.Staff) CrearEstallidoArcano(transform.position);
                Destroy(gameObject);
                return;
            }
        }

        if (distanciaRestante <= 0f) Destroy(gameObject);
    }

    private static Sprite ObtenerSpriteOrbeArcano()
    {
        if (spriteOrbeArcano != null) return spriteOrbeArcano;

        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Orbe_Arcano_Celestial",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        Vector2 centro = new Vector2(15.5f, 15.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - centro.x;
                float dy = y - centro.y;
                float distancia = Mathf.Sqrt(dx * dx + dy * dy);
                Color pixel = Color.clear;

                if (Mathf.Abs(distancia - 14f) < 1.3f)
                    pixel = new Color(0f, 0.35f, 1f, 1f);
                if (Mathf.Abs(distancia - 10f) < 1.2f)
                    pixel = new Color(0f, 0.9f, 1f, 1f);

                float elipseCian = Mathf.Abs(dy * 0.78f - dx * 0.62f);
                float elipseTurquesa = Mathf.Abs(dy * 0.78f + dx * 0.62f);
                if (Mathf.Abs(elipseCian - 8f) < 0.9f && distancia < 13f)
                    pixel = new Color(0f, 0.95f, 1f, 1f);
                if (Mathf.Abs(elipseTurquesa - 5f) < 0.8f && distancia < 10f)
                    pixel = new Color(0.1f, 0.65f, 1f, 1f);

                if (distancia <= 4.5f)
                    pixel = Color.Lerp(Color.white, new Color(0.55f, 1f, 1f), distancia / 4.5f);
                texture.SetPixel(x, y, pixel);
            }
        }
        texture.Apply();
        spriteOrbeArcano = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 24f);
        return spriteOrbeArcano;
    }

    private static void CrearEstela(GameObject proyectil, WeaponDefinition arma, int sortingOrder)
    {
        TrailRenderer trail = proyectil.AddComponent<TrailRenderer>();
        trail.time = arma.tipo == WeaponType.Bow ? 0.12f : 0.22f;
        trail.startWidth = arma.tipo == WeaponType.Bow ? 0.08f : 0.14f;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.04f;
        trail.numCapVertices = 2;
        trail.sortingOrder = sortingOrder - 1;
        if (materialEstela == null)
            materialEstela = new Material(Shader.Find("Sprites/Default"));
        trail.sharedMaterial = materialEstela;
        Gradient gradient = new Gradient();
        Color estela = arma.tipo == WeaponType.Staff
            ? new Color(0f, 0.85f, 1f, 0.9f)
            : new Color(0.75f, 0.88f, 1f, 0.55f);
        gradient.SetKeys(
            new[] { new GradientColorKey(estela, 0f), new GradientColorKey(estela, 1f) },
            new[] { new GradientAlphaKey(estela.a, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = gradient;
    }

    private static void CrearEstallidoArcano(Vector2 posicion)
    {
        GameObject efecto = new GameObject("Estallido_Arcano_Celestial");
        efecto.transform.position = posicion;
        SpriteRenderer rendererEfecto = efecto.AddComponent<SpriteRenderer>();
        rendererEfecto.sprite = ObtenerSpriteOrbeArcano();
        rendererEfecto.sortingOrder = 20;
        PlayerArcaneImpactEffect impacto = efecto.AddComponent<PlayerArcaneImpactEffect>();
        impacto.Iniciar(rendererEfecto);
        AudioManager.Instance?.PlayEchoSound();
    }
}

public class PlayerArcaneImpactEffect : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private float elapsed;

    public void Iniciar(SpriteRenderer renderer)
    {
        spriteRenderer = renderer;
        transform.localScale = Vector3.one * 0.3f;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / 0.24f);
        transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.4f, progress);
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f - progress;
            spriteRenderer.color = color;
        }
        if (progress >= 1f) Destroy(gameObject);
    }
}
