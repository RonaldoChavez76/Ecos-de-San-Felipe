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
    private Sprite[] attackFrames;      // 2 cuadros especiales de preparación (carga) y ataque (zarpazo/disparo)
    private float animTimer = 0f;
    private float attackTimer = 0f;
    private float totalAttackDuration = 0.55f;

    private Vector3 baseLocalScale = Vector3.one;
    private Transform shadowTransform;
    private ParticleSystem darkAuraParticles;
    private Transform playerTarget;

    // VFX dedicado para que se vea claramente la carga y el zarpazo cuerpo a cuerpo / ráfaga
    private GameObject attackVfxObj;
    private SpriteRenderer attackVfxSr;
    private Sprite chargeOrbSprite;
    private Sprite meleeSlashSprite;
    private Sprite rangedBurstSprite;
    private Coroutine currentAttackRoutine;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        baseLocalScale = transform.localScale;
        sr.sortingOrder = 4;

        PlayerMovement p = FindAnyObjectByType<PlayerMovement>();
        if (p != null) playerTarget = p.transform;

        InicializarSpritesDemonio();

        if (shadowTransform == null) CrearSombraDeSuelo();
        if (darkAuraParticles == null) CrearParticulasDeMateriaOscura();
        if (attackVfxObj == null) CrearEfectosDeCargaYZarpazo();
    }

    /// <summary>
    /// Carga y recorta automáticamente los 8 fotogramas de la hoja de sprites correspondiente
    /// (SombraErrante_SpriteSheet, SombraLanzadora_SpriteSheet o SombraGuardiana_SpriteSheet),
    /// o genera los fotogramas desde customDemonImage / respaldo procedural para que jamás sea invisible.
    /// </summary>
    public void InicializarSpritesDemonio()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();

        if (tipoDemonio == DemonType.SombraGuardianaJefe)
        {
            GenerarSpriteDemonioImponente();
            return;
        }

        bool hasAnimatorController = animator != null && animator.runtimeAnimatorController != null;
        if (hasAnimatorController) return;

        if (animatedFrames != null && animatedFrames.Length > 0 && animatedFrames[0] != null)
        {
            if (sr != null && sr.sprite == null) sr.sprite = animatedFrames[0];
            return;
        }

        // 1. Intentar cargar la hoja pre-generada de 8 columnas en Assets/Sprites/
        string sheetName = tipoDemonio == DemonType.SombraGuardianaJefe
            ? "SombraGuardiana_SpriteSheet"
            : (tipoDemonio == DemonType.SombraErrante
                ? "SombraErrante_SpriteSheet"
                : "SombraLanzadora_SpriteSheet");

        Texture2D tex = ZoneWaveSpawner.LoadSpriteTexture(sheetName);

        // Si no se encontró por nombre directo, revisar las referencias serializadas en SanFelipeDemoBuilder
        if (tex == null)
        {
            SanFelipeDemoBuilder builder = FindAnyObjectByType<SanFelipeDemoBuilder>();
            if (builder != null)
            {
                if (tipoDemonio == DemonType.SombraErrante) tex = builder.imagenSombraErrante;
                else if (tipoDemonio == DemonType.SombraLanzadora) tex = builder.imagenSombraLanzadora;
                else if (tipoDemonio == DemonType.SombraGuardianaJefe) tex = builder.imagenSombraGuardiana;
            }
        }

        if (tex == null) tex = customDemonImage;

        if (tex != null)
        {
            if (tex.width >= tex.height * 4)
            {
                CargarFotogramasDesdeHoja8Columnas(tex);
            }
            else
            {
                GenerarSpritesAnimadosDesdeImagen(tex);
            }
        }

        // 2. Respaldo procedural garantizado si por cualquier razón no hubiera textura cargada
        if (animatedFrames == null || animatedFrames.Length == 0 || animatedFrames[0] == null)
        {
            GenerarSpritesProceduralesDeRespaldo();
        }

        if (sr != null && animatedFrames != null && animatedFrames.Length > 0)
        {
            sr.sprite = animatedFrames[0];
        }
    }

    private void GenerarSpriteDemonioImponente()
    {
        const int size = 96;
        const float pixelsPerUnit = 48f;
        animatedFrames = new Sprite[6];
        for (int frame = 0; frame < animatedFrames.Length; frame++)
            animatedFrames[frame] = CrearFrameDemonioImponente(size, pixelsPerUnit, frame);

        attackFrames = new[]
        {
            CrearFrameDemonioImponente(size, pixelsPerUnit, 1, true, false),
            CrearFrameDemonioImponente(size, pixelsPerUnit, 4, false, true)
        };
        animTimer = 0f;
        if (sr != null) sr.sprite = animatedFrames[0];
    }

    private static Sprite CrearFrameDemonioImponente(int size, float pixelsPerUnit, int frame, bool poseCarga = false, bool poseAtaque = false)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = $"SombraGuardiana_Imponente_{frame}",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        Color carbon = new Color(0.05f, 0.03f, 0.08f, 1f);
        Color abismo = new Color(0.16f, 0.03f, 0.27f, 1f);
        Color violeta = new Color(0.39f, 0.08f, 0.60f, 1f);
        Color runa = new Color(0.72f, 0.20f, 0.95f, 1f);
        Color cian = new Color(0f, 0.85f, 1f, 1f);
        float wingPulse = Mathf.Sin(frame * Mathf.PI / 3f) * 3f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x - 47.5f;
                float py = y - 43f;
                float ax = Mathf.Abs(px);
                Color pixel = Color.clear;
                float wingSpan = ax - 15f;
                if (wingSpan >= 0f && wingSpan <= 32f)
                {
                    float wingTop = 25f + wingSpan * 0.78f + wingPulse * (wingSpan / 32f);
                    float tear = ((int)(wingSpan / 5f) % 2) * 3f;
                    float wingBottom = -8f - wingSpan * 0.30f + tear;
                    if (py >= wingBottom && py <= wingTop)
                    {
                        pixel = abismo;
                        if (Mathf.Abs(py - wingTop) < 1.5f || Mathf.Abs(py - wingBottom) < 1.5f)
                            pixel = carbon;
                        else if (Mathf.Abs(py - (wingBottom + (wingTop - wingBottom) * 0.48f)) < 1.2f)
                            pixel = violeta;
                    }
                }

                if (ax <= 15f && py >= -29f && py <= 18f &&
                    (px * px / 225f + (py - 1f) * (py - 1f) / 650f <= 1f))
                {
                    pixel = carbon;
                    if (ax > 10f || py < -18f) pixel = abismo;
                    if (py > 4f && py < 10f && ax < 5f) pixel = violeta;
                    if (py > 0f && py < 5f && ax < 2f) pixel = runa;
                }

                if (ax <= 9f && py >= 15f && py <= 31f &&
                    (px * px / 81f + (py - 23f) * (py - 23f) / 100f <= 1f))
                    pixel = ax > 7f || py < 18f ? carbon : abismo;

                if (py >= -44f && py < -25f && ax < 12f)
                {
                    float legCenter = ax < 6f ? 4f : 9f;
                    if (ax > legCenter - 3.5f && ax < legCenter + 2.5f)
                        pixel = py < -40f ? carbon : abismo;
                }

                if (ax >= 11f && ax <= 25f && py >= -12f && py <= 13f &&
                    Mathf.Abs(py - (7f - (ax - 11f) * 1.25f)) < 4.5f)
                {
                    pixel = ax > 22f ? carbon : abismo;
                    if (Mathf.Abs(py - (7f - (ax - 11f) * 1.25f)) < 1.4f) pixel = violeta;
                }

                if (py >= 28f && py <= 49f)
                {
                    float t = (py - 28f) / 21f;
                    float hornX = 8f + t * 9f - t * t * 3f;
                    if (Mathf.Abs(ax - hornX) < 2.1f)
                        pixel = py > 44f ? runa : (py > 38f ? violeta : carbon);
                }

                if (poseAtaque && ax > 19f && ax < 34f && py > -4f && py < 5f)
                    pixel = (x + y) % 3 == 0 ? runa : carbon;
                if (poseCarga && ax < 8f && py > 0f && py < 13f)
                    pixel = (x + y) % 2 == 0 ? cian : violeta;

                bool ojo = (Mathf.Abs(px + 6f) < 2f && Mathf.Abs(py - 24f) < 1.5f) ||
                           (Mathf.Abs(px) < 2f && Mathf.Abs(py - 27f) < 1.5f) ||
                           (Mathf.Abs(px - 6f) < 2f && Mathf.Abs(py - 30f) < 1.5f);
                if (ojo) pixel = Color.white;
                else if ((Mathf.Abs(px + 6f) < 3f && Mathf.Abs(py - 24f) < 2.5f) ||
                         (Mathf.Abs(px) < 3f && Mathf.Abs(py - 27f) < 2.5f) ||
                         (Mathf.Abs(px - 6f) < 3f && Mathf.Abs(py - 30f) < 2.5f))
                    pixel = cian;

                if (ax > 19f && ax < 31f && py > -15f && py < -8f &&
                    Mathf.Abs(py - (-10f - (ax - 20f) * 0.55f)) < 1.4f)
                    pixel = ax > 27f ? Color.white : carbon;

                if (pixel.a > 0f && pixel == abismo && (x * 17 + y * 31 + frame * 13) % 37 == 0)
                    pixel = violeta;
                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.42f), pixelsPerUnit);
    }

    private void CargarFotogramasDesdeHoja8Columnas(Texture2D sheetTex)
    {
        Texture2D readable = sheetTex;
        if (!sheetTex.isReadable)
        {
            RenderTexture rt = RenderTexture.GetTemporary(sheetTex.width, sheetTex.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(sheetTex, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            readable = new Texture2D(sheetTex.width, sheetTex.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, sheetTex.width, sheetTex.height), 0, 0);
            readable.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
        }

        int totalCols = 8;
        int frameW = readable.width / totalCols;
        int frameH = readable.height;
        float ppu = 64f;

        animatedFrames = new Sprite[6];
        for (int i = 0; i < 6; i++)
        {
            animatedFrames[i] = ExtraerFrameDeHoja(readable, i * frameW, 0, frameW, frameH, ppu, $"{sheetTex.name}_Idle_{i}");
        }

        attackFrames = new Sprite[2];
        attackFrames[0] = ExtraerFrameDeHoja(readable, 6 * frameW, 0, frameW, frameH, ppu, $"{sheetTex.name}_Charge");
        attackFrames[1] = ExtraerFrameDeHoja(readable, 7 * frameW, 0, frameW, frameH, ppu, $"{sheetTex.name}_Strike");
    }

    private Sprite ExtraerFrameDeHoja(Texture2D readable, int px, int py, int w, int h, float ppu, string frameName)
    {
        Color[] pixels = readable.GetPixels(px, py, w, h);
        Texture2D sub = new Texture2D(w, h, TextureFormat.RGBA32, false);
        sub.name = frameName;
        sub.filterMode = FilterMode.Point;
        sub.wrapMode = TextureWrapMode.Clamp;
        sub.SetPixels(pixels);
        sub.Apply();
        Sprite sp = Sprite.Create(sub, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
        sp.name = frameName;
        return sp;
    }

    private void GenerarSpritesProceduralesDeRespaldo()
    {
        int size = 64;
        float ppu = 32f;
        Color bodyColor = tipoDemonio == DemonType.SombraLanzadora
            ? new Color(0.88f, 0.12f, 0.38f, 0.95f)
            : (tipoDemonio == DemonType.SombraGuardianaJefe
                ? new Color(0.68f, 0.18f, 0.95f, 0.95f)
                : new Color(0.18f, 0.32f, 0.55f, 0.95f));
        Color coreColor = tipoDemonio == DemonType.SombraLanzadora
            ? new Color(1f, 0.55f, 0.75f, 1f)
            : new Color(0.25f, 0.95f, 1f, 1f);

        animatedFrames = new Sprite[6];
        for (int f = 0; f < 6; f++)
        {
            float wingOffset = Mathf.Sin((f / 6f) * Mathf.PI * 2f) * 5f;
            animatedFrames[f] = CrearSiluetaDemonioProcedural(size, ppu, bodyColor, coreColor, wingOffset);
        }
        attackFrames = new Sprite[2];
        attackFrames[0] = CrearSiluetaDemonioProcedural(size, ppu, bodyColor, Color.white, 7f);
        attackFrames[1] = CrearSiluetaDemonioProcedural(size, ppu, coreColor, Color.white, -7f);
    }

    private Sprite CrearSiluetaDemonioProcedural(int size, float ppu, Color bodyCol, Color coreCol, float wingYOffset)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2(size * 0.5f, size * 0.52f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float absDx = Mathf.Abs(dx);
                Color c = Color.clear;

                // Torso y cabeza con cuernos
                if (absDx < 8f && dy > -16f && dy < 14f) c = bodyCol;
                // Cuernos superiores
                if (dy >= 12f && dy < 22f && Mathf.Abs(absDx - (6f + (dy - 12f) * 0.4f)) < 2.2f) c = bodyCol;
                // Alas de murciélago laterales
                if (absDx >= 8f && absDx < 27f)
                {
                    float wingTop = 12f + wingYOffset * ((absDx - 8f) / 19f);
                    float wingBottom = -8f + Mathf.Sin(absDx * 0.6f) * 3f;
                    if (dy >= wingBottom && dy <= wingTop) c = bodyCol;
                }
                // Núcleo brillante en el pecho y ojos
                if (Vector2.Distance(new Vector2(dx, dy), new Vector2(0f, 2f)) < 3.5f) c = coreCol;
                if (Mathf.Abs(dy - 9f) < 1.5f && Mathf.Abs(absDx - 3f) < 1.5f) c = coreCol;

                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
    }

    public void ActivarPoseDeAtaque(float duracion = 0.55f)
    {
        totalAttackDuration = Mathf.Max(0.35f, duracion);
        attackTimer = totalAttackDuration;

        if (animator != null && animator.enabled && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger("Atacar");
        }

        if (currentAttackRoutine != null)
        {
            StopCoroutine(currentAttackRoutine);
            transform.localScale = baseLocalScale;
            transform.localRotation = Quaternion.identity;
        }
        currentAttackRoutine = StartCoroutine(ImpulsoYAnimacionDeAtaque(totalAttackDuration));
    }

    private IEnumerator ImpulsoYAnimacionDeAtaque(float duracionTotal)
    {
        Vector3 posOriginal = transform.position;
        if (playerTarget == null)
        {
            PlayerMovement p = FindAnyObjectByType<PlayerMovement>();
            if (p != null) playerTarget = p.transform;
        }

        Vector3 dir = playerTarget != null ? (playerTarget.position - posOriginal).normalized : Vector3.down;
        if (dir.sqrMagnitude < 0.01f) dir = Vector3.down;

        bool esMelee = (tipoDemonio == DemonType.SombraErrante || tipoDemonio == DemonType.SombraGuardianaJefe);
        float anguloZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // =====================================================================
        // FASE 1: CARGA / ANTICIPACIÓN DEL ATAQUE (40% del tiempo)
        // Se pliegan las alas hacia arriba, retrocede tomando impulso y condensa energía oscura
        // =====================================================================
        float tCarga = duracionTotal * 0.40f;
        float t = 0f;

        if (attackVfxObj != null && attackVfxSr != null)
        {
            attackVfxObj.SetActive(true);
            attackVfxSr.sprite = chargeOrbSprite;
            attackVfxSr.color = esMelee ? new Color(0.15f, 0.95f, 1f, 0.9f) : new Color(1f, 0.15f, 0.45f, 0.9f);
            attackVfxObj.transform.localPosition = (Vector3)(dir * 0.35f);
            attackVfxObj.transform.localRotation = Quaternion.Euler(0f, 0f, anguloZ);
        }

        while (t < tCarga)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / tCarga);

            // Toma impulso hacia atrás y arriba mientras expande sus alas
            transform.position = Vector3.Lerp(posOriginal, posOriginal - dir * 0.28f + Vector3.up * 0.22f, p);
            transform.localScale = new Vector3(
                baseLocalScale.x * (1f + 0.18f * p),
                baseLocalScale.y * (1f + 0.22f * p),
                baseLocalScale.z
            );

            // Pulso de carga en el sprite y en el orbe de anticipación
            if (sr != null)
            {
                sr.color = Color.Lerp(Color.white, esMelee ? new Color(0.55f, 0.95f, 1f, 1f) : new Color(1f, 0.55f, 0.75f, 1f), p);
            }
            if (attackVfxObj != null)
            {
                float sOrb = Mathf.Lerp(0.35f, 1.15f, p);
                attackVfxObj.transform.localScale = new Vector3(sOrb, sOrb, 1f);
                attackVfxObj.transform.Rotate(0f, 0f, 360f * Time.deltaTime);
            }
            yield return null;
        }

        // =====================================================================
        // FASE 2: ZARPAZO CUERPO A CUERPO / DISPARO EXPLOSIVO (30% del tiempo)
        // Embiste con fuerza hacia Ronan y despliega el tajo doble de garras o explosión
        // =====================================================================
        float tGolpe = duracionTotal * 0.30f;
        t = 0f;
        Vector3 posFinCarga = transform.position;
        float distanciaEstocada = esMelee ? 0.62f : 0.32f;

        if (attackVfxObj != null && attackVfxSr != null)
        {
            attackVfxSr.sprite = esMelee ? meleeSlashSprite : rangedBurstSprite;
            attackVfxSr.color = tipoDemonio == DemonType.SombraGuardianaJefe
                ? new Color(0.92f, 0.45f, 1f, 1f)
                : (esMelee ? new Color(0.25f, 0.98f, 1f, 1f) : new Color(1f, 0.20f, 0.50f, 1f));
            attackVfxObj.transform.localPosition = (Vector3)(dir * (esMelee ? 0.75f : 0.50f));
            attackVfxObj.transform.localRotation = Quaternion.Euler(0f, 0f, anguloZ + 90f);
            attackVfxObj.transform.localScale = esMelee ? new Vector3(1.45f, 1.45f, 1f) : new Vector3(1.25f, 1.25f, 1f);
        }

        while (t < tGolpe)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / tGolpe);

            transform.position = Vector3.Lerp(posFinCarga, posOriginal + dir * distanciaEstocada, p);
            transform.localScale = new Vector3(
                baseLocalScale.x * Mathf.Lerp(1.22f, 1.05f, p),
                baseLocalScale.y * Mathf.Lerp(0.86f, 1.0f, p),
                baseLocalScale.z
            );

            if (attackVfxSr != null)
            {
                Color c = attackVfxSr.color;
                c.a = Mathf.Lerp(1f, 0.25f, p);
                attackVfxSr.color = c;
            }
            yield return null;
        }

        if (attackVfxObj != null) attackVfxObj.SetActive(false);
        if (sr != null) sr.color = Color.white;

        // =====================================================================
        // FASE 3: RECUPERACIÓN SUAVE (30% del tiempo)
        // =====================================================================
        float tRecup = duracionTotal * 0.30f;
        t = 0f;
        Vector3 posFinGolpe = transform.position;
        while (t < tRecup)
        {
            t += Time.deltaTime;
            float p = t / tRecup;
            transform.position = Vector3.Lerp(posFinGolpe, posOriginal, p);
            transform.localScale = Vector3.Lerp(transform.localScale, baseLocalScale, p);
            yield return null;
        }

        transform.localScale = baseLocalScale;
        transform.localRotation = Quaternion.identity;
        currentAttackRoutine = null;
    }

    void Update()
    {
        bool hasAnimatorController = tipoDemonio != DemonType.SombraGuardianaJefe &&
                                     animator != null && animator.enabled && animator.runtimeAnimatorController != null;

        // 1. Si no hay Animator activo, usar el motor de fotogramas de hoja de sprites / procedural
        if (!hasAnimatorController)
        {
            if (animatedFrames == null || animatedFrames.Length == 0 || animatedFrames[0] == null)
            {
                InicializarSpritesDemonio();
            }

            animTimer += Time.deltaTime * framesPerSecond;

            if (attackTimer > 0f)
            {
                attackTimer -= Time.deltaTime;
                if (attackFrames != null && attackFrames.Length == 2)
                {
                    // Primera mitad = pose de carga (Frame 6), segunda mitad = zarpazo/disparo (Frame 7)
                    sr.sprite = attackTimer > (totalAttackDuration * 0.5f) ? attackFrames[0] : attackFrames[1];
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
            shadowTransform.localPosition = new Vector3(0f, -0.95f - ondaFlote * 0.08f, 0f);
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
            if (attackVfxSr != null) attackVfxSr.sortingOrder = sr.sortingOrder + 2;

            ParticleSystemRenderer auraRenderer = darkAuraParticles != null
                ? darkAuraParticles.GetComponent<ParticleSystemRenderer>()
                : null;
            if (auraRenderer != null)
            {
                SpriteRenderer playerRenderer = playerTarget != null ? playerTarget.GetComponent<SpriteRenderer>() : null;
                bool jefeDetrasDeRonan = playerTarget != null && transform.position.y < playerTarget.position.y;
                auraRenderer.sortingOrder = jefeDetrasDeRonan && playerRenderer != null
                    ? playerRenderer.sortingOrder + 1
                    : sr.sortingOrder + 1;
            }
        }
    }

    // =========================================================================
    // MOTOR DE ANIMACIÓN PROCEDURAL DE PÍXELES (ALETEO, GARRAS, CARGA Y ZARPAZO)
    // =========================================================================
    private void GenerarSpritesAnimadosDesdeImagen(Texture2D original)
    {
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
        Color bgCol = rawPixels[(baseSize - 2) * baseSize + 2];

        Color[] paddedPixels = new Color[w * h];
        for (int i = 0; i < paddedPixels.Length; i++) paddedPixels[i] = Color.clear;

        for (int y = 0; y < baseSize; y++)
        {
            for (int x = 0; x < baseSize; x++)
            {
                Color c = rawPixels[y * baseSize + x];
                if (c.a < 0.1f) continue;

                float dist = Vector3.Distance(new Vector3(c.r, c.g, c.b), new Vector3(bgCol.r, bgCol.g, bgCol.b));
                if (dist > toleranciaQuitarFondo)
                {
                    if (dist < toleranciaQuitarFondo + 0.08f)
                    {
                        c.a = Mathf.InverseLerp(toleranciaQuitarFondo, toleranciaQuitarFondo + 0.08f, dist);
                    }
                    paddedPixels[(y + padding) * w + (x + padding)] = c;
                }
            }
        }

        float ppu = baseSize / 2.4f;

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

        if (poseCarga) wingSway = 1.65f;
        if (poseDisparo) wingSway = -1.75f;

        for (int y = 0; y < h; y++)
        {
            float ny = y / (float)h;
            for (int x = 0; x < w; x++)
            {
                float nx = (x - w * 0.5f) / (w * 0.5f);
                float absX = Mathf.Abs(nx);

                float sampleX = x;
                float sampleY = y;

                // 1. Alas laterales con batido pronunciado
                if (absX > 0.16f)
                {
                    float factorAla = (absX - 0.16f) / 0.84f;
                    sampleY -= wingSway * factorAla * (h * 0.11f);
                    sampleX -= Mathf.Sign(nx) * wingSway * factorAla * (w * 0.045f);
                }

                // 2. Brazos y garras articuladas en carga y zarpazo
                if (ny > 0.18f && ny < 0.60f && absX > 0.14f && absX < 0.52f)
                {
                    if (poseCarga)
                    {
                        sampleY -= 9f;
                        sampleX += Mathf.Sign(nx) * 5f;
                    }
                    else if (poseDisparo)
                    {
                        sampleY += 10f;
                        sampleX -= Mathf.Sign(nx) * 7f;
                    }
                    else
                    {
                        sampleY -= tailWave * 3f;
                    }
                }

                // 3. Cola espectral inferior
                if (ny < 0.45f && absX < 0.42f)
                {
                    float factorCola = (0.45f - ny) / 0.45f;
                    sampleX -= Mathf.Sin(phase + ny * 9f) * factorCola * (w * 0.055f);
                }

                // 4. Respiración del torso
                if (absX <= 0.22f && ny >= 0.35f)
                {
                    sampleY -= tailWave * (h * 0.015f);
                }

                int sx = Mathf.Clamp(Mathf.RoundToInt(sampleX), 0, w - 1);
                int sy = Mathf.Clamp(Mathf.RoundToInt(sampleY), 0, h - 1);

                Color c = src[sy * w + sx];

                if (c.a > 0.05f)
                {
                    bool esNucleoBrillante = (c.g > 0.55f && c.b > 0.55f) || (c.r > 0.65f && c.g < 0.35f);
                    if (esNucleoBrillante)
                    {
                        float brilloExtra = (poseCarga || poseDisparo) ? 1.65f : (1.05f + 0.30f * Mathf.Sin(phase));
                        c.r = Mathf.Clamp01(c.r * brilloExtra);
                        c.g = Mathf.Clamp01(c.g * brilloExtra);
                        c.b = Mathf.Clamp01(c.b * brilloExtra);
                    }
                    else if (poseCarga)
                    {
                        c = Color.Lerp(c, new Color(0.65f, 0.25f, 0.95f, c.a), 0.28f);
                    }
                    else if (poseDisparo)
                    {
                        c = Color.Lerp(c, new Color(0.35f, 0.95f, 1f, c.a), 0.30f);
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
        string nombreArchivo = tipoDemonio == DemonType.SombraGuardianaJefe
            ? "SombraGuardiana_SpriteSheet.png"
            : $"{tipoDemonio}_SpriteSheet.png";
        string path = $"Assets/Sprites/{nombreArchivo}";
        if (System.IO.File.Exists(path)) return;

        int totalCols = 8;
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
    // GENERADOR DE SPRITES VFX PARA CARGA DE ENERGÍA Y ZARPAZO CUERPO A CUERPO
    // =========================================================================
    private void CrearEfectosDeCargaYZarpazo()
    {
        attackVfxObj = new GameObject("EfectoAtaqueSombra_VFX");
        attackVfxObj.transform.SetParent(transform);
        attackVfxObj.transform.localPosition = Vector3.zero;

        attackVfxSr = attackVfxObj.AddComponent<SpriteRenderer>();
        attackVfxSr.sortingOrder = 10;

        // 1. Orbe de carga con anillo rúnico (32x32)
        Texture2D orbTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        orbTex.filterMode = FilterMode.Point;
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                Color col = Color.clear;
                if (d < 5.5f) col = Color.white;
                else if (d < 9.5f) col = new Color(0.85f, 0.85f, 1f, 0.85f);
                else if (d > 12f && d < 14.5f) col = new Color(1f, 1f, 1f, 0.9f);
                orbTex.SetPixel(x, y, col);
            }
        }
        orbTex.Apply();
        chargeOrbSprite = Sprite.Create(orbTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 24f);

        // 2. Zarpazo doble de garras en medialuna (48x48)
        Texture2D slashTex = new Texture2D(48, 48, TextureFormat.RGBA32, false);
        slashTex.filterMode = FilterMode.Point;
        for (int y = 0; y < 48; y++)
        {
            for (int x = 0; x < 48; x++)
            {
                float dx = x - 24f;
                float dy = y - 20f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float span = Mathf.Clamp01(1f - Mathf.Abs(dx) / 21f);
                float distArc1 = Mathf.Abs(r - 17f);
                float distArc2 = Mathf.Abs(r - 11f);
                Color col = Color.clear;

                if (dy < 2f && span > 0.05f)
                {
                    if (distArc1 < 3.2f * span || distArc2 < 2.5f * span)
                    {
                        bool inner = (distArc1 < 1.5f * span) || (distArc2 < 1.2f * span);
                        col = inner ? Color.white : new Color(0.15f, 0.15f, 0.25f, 0.95f);
                    }
                }
                slashTex.SetPixel(x, y, col);
            }
        }
        slashTex.Apply();
        meleeSlashSprite = Sprite.Create(slashTex, new Rect(0, 0, 48, 48), new Vector2(0.5f, 0.5f), 24f);

        // 3. Onda expansiva de disparo oscuro (32x32)
        Texture2D burstTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        burstTex.filterMode = FilterMode.Point;
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                Color col = Color.clear;
                if (d < 4.5f || (d > 9f && d < 13.5f)) col = Color.white;
                burstTex.SetPixel(x, y, col);
            }
        }
        burstTex.Apply();
        rangedBurstSprite = Sprite.Create(burstTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 24f);

        attackVfxObj.SetActive(false);
    }

    // =========================================================================
    // SOMBRA DE PISO Y PARTÍCULAS DE ENERGÍA ESPECTRAL
    // =========================================================================
    private void CrearSombraDeSuelo()
    {
        GameObject sombraFloor = new GameObject("SombraPiso");
        sombraFloor.transform.SetParent(transform);
        sombraFloor.transform.localPosition = new Vector3(0f, -0.95f, 0f);
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
        main.startColor = tipoDemonio == DemonType.SombraLanzadora
            ? new Color(1f, 0.18f, 0.45f, 0.65f)
            : (tipoDemonio == DemonType.SombraGuardianaJefe
                ? new Color(0.75f, 0.25f, 1f, 0.75f)
                : new Color(0.1f, 0.85f, 1f, 0.65f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = darkAuraParticles.emission;
        emission.rateOverTime = 8f;

        var shape = darkAuraParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.7f;

        var vel = darkAuraParticles.velocityOverLifetime;
        vel.enabled = true;
        vel.y = 0.8f;

        ParticleSystemRenderer psr = partObj.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(Shader.Find("Sprites/Default"));
        psr.sortingOrder = 3;
    }
}
