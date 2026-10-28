using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BlackCloverDemonRenderer : MonoBehaviour
{
    public enum DemonType { SombraLanzadora, SombraErrante, SombraGuardianaJefe }

    [Header("Imagen de tu Demonio")]
    public Texture2D customDemonImage;
    [Range(0.05f, 0.35f)]
    public float toleranciaQuitarFondo = 0.17f;
    public DemonType tipoDemonio = DemonType.SombraLanzadora;

    [Header("Ajustes de Animación por Zonas")]
    public float framesPerSecond = 8f;
    public float levitationAmplitude = 0.14f;
    public float levitationSpeed = 3.2f;

    private SpriteRenderer sr;
    private Animator animator;
    private Sprite[] animatedFrames;    // 6 cuadros de aleteo + cola ondulante + pulso de núcleo
    private Sprite[] attackFrames;      // 2 cuadros especiales de preparación y disparo
    private float animTimer = 0f;
    private float attackTimer = 0f;

    private Vector3 baseLocalScale = Vector3.one;
    private Transform shadowTransform;
    private ParticleSystem darkAuraParticles;
    private Transform playerTarget;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        sr.sortingOrder = 4;

        PlayerMovement p = FindFirstObjectByType<PlayerMovement>();
        if (p != null) playerTarget = p.transform;

        bool hasAnimatorController = animator != null && animator.runtimeAnimatorController != null;
        if (customDemonImage != null && !hasAnimatorController)
        {
            GenerarSpritesAnimadosDesdeImagen(customDemonImage);
        }

        CrearSombraDeSuelo();
        CrearParticulasDeMateriaOscura();
    }

    public void ActivarPoseDeAtaque(float duracion = 0.45f)
    {
        attackTimer = duracion;
        if (animator != null && animator.enabled && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger("Atacar");
        }
        StartCoroutine(ImpulsoDeAtaque());
    }

    private IEnumerator ImpulsoDeAtaque()
    {
        // El demonio se inclina y se lanza brevemente hacia Ronan al disparar
        Vector3 posOriginal = transform.position;
        Vector3 dir = playerTarget != null ? (playerTarget.position - posOriginal).normalized : Vector3.down;

        // 1. Toma impulso hacia atrás levantando las alas
        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(posOriginal, posOriginal - dir * 0.18f + Vector3.up * 0.15f, t / 0.12f);
            yield return null;
        }

        // 2. Latigazo hacia adelante al escupir el proyectil
        t = 0f;
        Vector3 posImpulso = transform.position;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(posImpulso, posOriginal + dir * 0.25f, t / 0.15f);
            yield return null;
        }

        // 3. Regresa flotando a su posición
        t = 0f;
        Vector3 posAtaque = transform.position;
        while (t < 0.18f)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(posAtaque, posOriginal, t / 0.18f);
            yield return null;
        }
    }

    void Update()
    {
        bool hasAnimatorController = animator != null && animator.enabled && animator.runtimeAnimatorController != null;

        // 1. Si no hay Animator activo, usar el motor de deformación procedimental de fotogramas
        if (!hasAnimatorController)
        {
            animTimer += Time.deltaTime * framesPerSecond;

            if (attackTimer > 0f)
            {
                attackTimer -= Time.deltaTime;
                if (attackFrames != null && attackFrames.Length == 2)
                {
                    sr.sprite = attackTimer > 0.22f ? attackFrames[0] : attackFrames[1];
                }
            }
            else if (animatedFrames != null && animatedFrames.Length > 0)
            {
                int frameIdx = Mathf.FloorToInt(animTimer) % animatedFrames.Length;
                sr.sprite = animatedFrames[frameIdx];
            }
        }
        else if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }

        // 2. Movimiento de levitación suave y sincronización con la sombra del piso
        float ondaFlote = Mathf.Sin(Time.time * levitationSpeed);
        if (shadowTransform != null)
        {
            shadowTransform.localPosition = new Vector3(0f, -1.25f - ondaFlote * 0.08f, 0f);
            float s = 0.9f - ondaFlote * 0.12f;
            shadowTransform.localScale = new Vector3(s, s * 0.35f, 1f);
        }

        // 3. Orientar sutilmente hacia Ronan
        if (playerTarget != null)
        {
            float dx = playerTarget.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.3f)
            {
                sr.flipX = dx < 0f;
            }
        }

        // 4. Orden de renderizado dinámico Top-Down
        if (sr != null)
        {
            sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 10);
            SpriteRenderer srShadow = shadowTransform != null ? shadowTransform.GetComponent<SpriteRenderer>() : null;
            if (srShadow != null) srShadow.sortingOrder = sr.sortingOrder - 1;
        }
    }

    // =========================================================================
    // MOTOR DE ANIMACIÓN PROCEDURAL DE PÍXELES (ALETEO, COLA Y BRILLO DE OJOS)
    // =========================================================================
    private void GenerarSpritesAnimadosDesdeImagen(Texture2D original)
    {
        // Reducimos a una resolución pixel-art limpia (128x128) más un margen de 20 píxeles por lado (168x168)
        // para que las alas NUNCA se corten al batirse y no dé lag al iniciar.
        int baseSize = 128;
        int padding = 20;
        int w = baseSize + padding * 2;
        int h = baseSize + padding * 2;

        RenderTexture rt = RenderTexture.GetTemporary(baseSize, baseSize, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(original, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D resizedTex = new Texture2D(baseSize, baseSize, TextureFormat.RGBA32, false);
        resizedTex.ReadPixels(new Rect(0, 0, baseSize, baseSize), 0, 0);
        resizedTex.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        Color[] rawPixels = resizedTex.GetPixels();
        Color bgCol = rawPixels[(baseSize - 2) * baseSize + 2]; // Color café de la esquina

        // Crear el lienzo ampliado con margen transparente (Padding)
        Color[] paddedPixels = new Color[w * h];
        for (int i = 0; i < paddedPixels.Length; i++) paddedPixels[i] = Color.clear;

        for (int y = 0; y < baseSize; y++)
        {
            for (int x = 0; x < baseSize; x++)
            {
                Color c = rawPixels[y * baseSize + x];

                // Si la imagen ya viene en PNG transparente, respetamos su transparencia
                if (c.a < 0.1f) continue;

                // Distancia euclidiana en RGB respecto al fondo café
                float dist = Vector3.Distance(new Vector3(c.r, c.g, c.b), new Vector3(bgCol.r, bgCol.g, bgCol.b));

                if (dist > toleranciaQuitarFondo)
                {
                    // Suavizado en el borde exacto para quitar el halo café del JPG
                    if (dist < toleranciaQuitarFondo + 0.08f)
                    {
                        c.a = Mathf.InverseLerp(toleranciaQuitarFondo, toleranciaQuitarFondo + 0.08f, dist);
                    }
                    paddedPixels[(y + padding) * w + (x + padding)] = c;
                }
            }
        }

        float ppu = baseSize / 2.8f;

        // Generar los 6 fotogramas de vuelo y 2 de ataque
        int totalFrames = 6;
        animatedFrames = new Sprite[totalFrames];
        for (int f = 0; f < totalFrames; f++)
        {
            float phase = (f / (float)totalFrames) * Mathf.PI * 2f;
            animatedFrames[f] = CrearFotogramaDeformado(paddedPixels, w, h, ppu, phase, false, false);
        }

        attackFrames = new Sprite[2];
        attackFrames[0] = CrearFotogramaDeformado(paddedPixels, w, h, ppu, Mathf.PI * 0.5f, true, false);
        attackFrames[1] = CrearFotogramaDeformado(paddedPixels, w, h, ppu, Mathf.PI * 1.5f, false, true);

#if UNITY_EDITOR
        // BONUS: Exporta automáticamente la Hoja de Sprites de 8 cuadros en PNG a tu carpeta Assets/Sprites
        ExportarHojaDeSpritesPNG(w, h);
#endif
    }

    private Sprite CrearFotogramaDeformado(Color[] src, int w, int h, float ppu, float phase, bool poseCarga, bool poseDisparo)
    {
        Texture2D frameTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        frameTex.filterMode = FilterMode.Point;
        Color[] dst = new Color[w * h];

        float wingSway = Mathf.Sin(phase);
        float tailWave = Mathf.Cos(phase);

        if (poseCarga) wingSway = 1.35f;
        if (poseDisparo) wingSway = -1.25f;

        for (int y = 0; y < h; y++)
        {
            float ny = y / (float)h;
            for (int x = 0; x < w; x++)
            {
                float nx = (x - w * 0.5f) / (w * 0.5f);
                float absX = Mathf.Abs(nx);

                float sampleX = x;
                float sampleY = y;

                // 1. Alas laterales (Con el nuevo margen ya nunca tocan el borde)
                if (absX > 0.18f)
                {
                    float factorAla = (absX - 0.18f) / 0.82f;
                    sampleY -= wingSway * factorAla * (h * 0.065f);
                    sampleX -= Mathf.Sign(nx) * wingSway * factorAla * (w * 0.025f);
                }

                // 2. Cola espectral inferior
                if (ny < 0.45f && absX < 0.40f)
                {
                    float factorCola = (0.45f - ny) / 0.45f;
                    sampleX -= Mathf.Sin(phase + ny * 8f) * factorCola * (w * 0.035f);
                }

                // 3. Respiración del torso
                if (absX <= 0.22f && ny >= 0.35f)
                {
                    sampleY -= tailWave * (h * 0.012f);
                }

                int sx = Mathf.Clamp(Mathf.RoundToInt(sampleX), 0, w - 1);
                int sy = Mathf.Clamp(Mathf.RoundToInt(sampleY), 0, h - 1);

                Color c = src[sy * w + sx];

                if (c.a > 0.05f)
                {
                    bool esNucleoBrillante = (c.g > 0.55f && c.b > 0.55f) || (c.r > 0.65f && c.g < 0.35f);
                    if (esNucleoBrillante)
                    {
                        float brilloExtra = poseDisparo ? 1.55f : (1.05f + 0.30f * Mathf.Sin(phase));
                        c.r = Mathf.Clamp01(c.r * brilloExtra);
                        c.g = Mathf.Clamp01(c.g * brilloExtra);
                        c.b = Mathf.Clamp01(c.b * brilloExtra);
                    }
                    else if (poseDisparo)
                    {
                        c = Color.Lerp(c, new Color(0.4f, 0.95f, 1f, c.a), 0.25f);
                    }
                }

                dst[y * w + x] = c;
            }
        }

        frameTex.SetPixels(dst);
        frameTex.Apply();
        return Sprite.Create(frameTex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
    }

#if UNITY_EDITOR
    private void ExportarHojaDeSpritesPNG(int frameW, int frameH)
    {
        string path = $"Assets/Sprites/{tipoDemonio}_SpriteSheet.png";
        if (System.IO.File.Exists(path)) return; // Solo la crea una vez para no sobreescribir

        int totalCols = 8; // 6 de vuelo + 2 de ataque
        Texture2D sheet = new Texture2D(frameW * totalCols, frameH, TextureFormat.RGBA32, false);

        for (int i = 0; i < 6; i++)
        {
            sheet.SetPixels(i * frameW, 0, frameW, frameH, animatedFrames[i].texture.GetPixels());
        }
        sheet.SetPixels(6 * frameW, 0, frameW, frameH, attackFrames[0].texture.GetPixels());
        sheet.SetPixels(7 * frameW, 0, frameW, frameH, attackFrames[1].texture.GetPixels());
        sheet.Apply();

        byte[] bytes = sheet.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
        UnityEditor.AssetDatabase.Refresh();
        Debug.Log($"¡Hoja de sprites generada y guardada en: {path}!");
    }
#endif

    // =========================================================================
    // SOMBRA DE PISO Y PARTÍCULAS DE ENERGÍA ESPECTRAL
    // =========================================================================
    private void CrearSombraDeSuelo()
    {
        GameObject sombraFloor = new GameObject("SombraPiso");
        sombraFloor.transform.SetParent(transform);
        sombraFloor.transform.localPosition = new Vector3(0f, -1.25f, 0f);
        shadowTransform = sombraFloor.transform;

        SpriteRenderer srShadow = sombraFloor.AddComponent<SpriteRenderer>();
        srShadow.sortingOrder = 1;

        Texture2D tex = new Texture2D(16, 16);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                tex.SetPixel(x, y, d <= 7f ? new Color(0f, 0f, 0f, 0.45f) : Color.clear);
            }
        }
        tex.Apply();
        srShadow.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
    }

    private void CrearParticulasDeMateriaOscura()
    {
        GameObject partObj = new GameObject("AuraEspectral");
        partObj.transform.SetParent(transform);
        partObj.transform.localPosition = Vector3.zero;

        darkAuraParticles = partObj.AddComponent<ParticleSystem>();
        var main = darkAuraParticles.main;
        main.startLifetime = 1.1f;
        main.startSpeed = 0.6f;
        main.startSize = 0.14f;
        main.startColor = new Color(0.1f, 0.85f, 1f, 0.65f); // Brasas azules/espectrales flotando hacia arriba
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = darkAuraParticles.emission;
        emission.rateOverTime = 8f;

        var shape = darkAuraParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.7f;

        var vel = darkAuraParticles.velocityOverLifetime;
        vel.enabled = true;
        vel.y = 0.8f;

        // Asignar material por defecto de Sprites para que no se vean cuadros rosas en Unity 6
        ParticleSystemRenderer psr = partObj.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(Shader.Find("Sprites/Default"));
        psr.sortingOrder = 3;
    }
}