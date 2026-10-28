using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SanFelipeDemoBuilder : MonoBehaviour
{
    [Header("1. Referencias de Personaje y Audio")]
    public Transform playerTransform;
    public AudioClip customExplorationMusic;
    public AudioClip customCombatMusic;

    [Header("2. Imágenes de tus Demonios (Arrástralas aquí)")]
    public Texture2D imagenSombraLanzadora;
    public Texture2D imagenSombraErrante;
    public Texture2D imagenSombraGuardiana;

    [Header("3. Hojas de Ninja Adventure (Arrástralas aquí en el Inspector)")]
    public Texture2D tilesetFloor;   // Arrastra TilesetFloor.png
    public Texture2D tilesetDesert;  // Arrastra TilesetDesert.png (Parroquia, Fuentes, Mercado)
    public Texture2D tilesetHouse;   // Arrastra TilesetHouse.png (Casas, Muros, Estatuas)
    public Texture2D tilesetNature;  // Arrastra TilesetNature.png (Árboles, Rocas del Mirador, Flores)
    public Texture2D tilesetElement; // Arrastra TilesetElement.png (Letreros, Carretas, Cajas, Rejas)

    [Header("Configuración de Cámara (Sección 2.3 del Documento)")]
    public float cameraSmoothTime = 0.15f;
    private Vector3 cameraVelocity = Vector3.zero;
    private Camera mainCam;

    // Copias legibles en memoria de los tilesets
    private Dictionary<Texture2D, Texture2D> readableCache = new Dictionary<Texture2D, Texture2D>();

    // Audio
    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioClip sfxSlash;
    private AudioClip sfxCounter;
    private AudioClip sfxEcho;

    void Start()
    {
        // 1. Buscar a Ronan
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            playerTransform = player.transform;
            if (player.GetComponent<Collider2D>() == null)
            {
                BoxCollider2D col = player.gameObject.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.7f, 0.8f);
            }
        }

        // 2. Configurar Cámara
        mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.backgroundColor = new Color(0.74f, 0.58f, 0.42f);
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5.5f;
        }

        // 3. Construir el Mapa con los sprites reales de Ninja Adventure (si no fue generado previamente en el Editor)
        if (GameObject.Find("Mapa_SanFelipe_NinjaAdventure") == null)
        {
            ConstruirMapaSanFelipeConSprites();
        }

        // 4. Conectar o Crear Demonios Sombra y Eco Luminoso
        GameObject grupoEnemigos = GameObject.Find("Enemigos_SanFelipe");
        if (grupoEnemigos == null && GameObject.Find("Sombra_Lanzadora") == null)
        {
            ConstruirGrupoEnemigosYEco();
        }
        else
        {
            ConectarEnemigosExistentes();
        }

        // 4.5. Construir Zonas Modulares (Nivel 1: Parroquia Exterior e Interior con Acertijo de Campanas)
        ParroquiaExteriorBuilder.ConstruirElementosExteriorParroquia();
        ParroquiaInteriorBuilder.ConstruirInteriorParroquia();
        MercadoBuilder.ConstruirZonaMercado();
        CerroDelFraileBuilder.ConstruirZonaCerroDelFraile();

        // 5. Inicializar Audio
        InicializarSistemaDeAudio();
    }

    void LateUpdate()
    {
        if (mainCam != null && playerTransform != null)
        {
            Vector3 destino = new Vector3(playerTransform.position.x, playerTransform.position.y, -10f);
            mainCam.transform.position = Vector3.SmoothDamp(mainCam.transform.position, destino, ref cameraVelocity, cameraSmoothTime);
        }

        if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))
        {
            if (sfxSource != null && sfxSlash != null) sfxSource.PlayOneShot(sfxSlash, 0.5f);
        }
    }

    // =========================================================================
    // EXTRACTOR AUTOMÁTICO DE SPRITES DE NINJA ADVENTURE (CUADRÍCULA 16x16)
    // =========================================================================
    private Texture2D ObtenerTexturaLegible(Texture2D original)
    {
        if (original == null) return null;
        if (readableCache.ContainsKey(original)) return readableCache[original];

        int w = original.width;
        int h = original.height;
        RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(original, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D copy = new Texture2D(w, h, TextureFormat.RGBA32, false);
        copy.filterMode = FilterMode.Point;
        copy.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        copy.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        readableCache[original] = copy;
        return copy;
    }

    /// <summary>
    /// Recorta una región exacta en celdas de 16x16 píxeles con PPU de 16f para un encaje milimétrico sin desalineaciones.
    /// </summary>
    private Sprite RecortarSpriteDeHoja(Texture2D sheet, int col, int rowTop, int widthCells = 1, int heightCells = 1)
    {
        Texture2D tex = ObtenerTexturaLegible(sheet);
        if (tex == null) return null;

        int cell = 16;
        int px = col * cell;
        int pw = widthCells * cell;
        int ph = heightCells * cell;
        int py = tex.height - ((rowTop + heightCells) * cell);

        px = Mathf.Clamp(px, 0, tex.width);
        py = Mathf.Clamp(py, 0, tex.height);
        pw = Mathf.Clamp(pw, 1, tex.width - px);
        ph = Mathf.Clamp(ph, 1, tex.height - py);

        return Sprite.Create(tex, new Rect(px, py, pw, ph), new Vector2(0.5f, 0.5f), 16f);
    }

    // =========================================================================
    // GENERACIÓN Y EDICIÓN DESDE EL EDITOR (MENÚ DE CLIC DERECHO EN EL INSPECTOR)
    // =========================================================================
    [ContextMenu("1. Generar Mapa Permanente en la Escena")]
    public void GenerarMapaEnEditor()
    {
        LimpiarMapaEnEditor();
        ConstruirMapaSanFelipeConSprites();
        Debug.Log("<color=green>✓ Mapa de San Felipe generado permanentemente en la Hierarchy.</color>");
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
    }

    [ContextMenu("2. Borrar Mapa de la Escena")]
    public void LimpiarMapaEnEditor()
    {
        GameObject viejo = GameObject.Find("Mapa_SanFelipe_NinjaAdventure");
        if (viejo != null)
        {
            if (Application.isPlaying) Destroy(viejo);
            else DestroyImmediate(viejo);
            Debug.Log("<color=yellow>✓ Mapa anterior eliminado de la escena.</color>");
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
        }
    }

    [ContextMenu("3. Generar Enemigos Permanentes en Hierarchy")]
    public void GenerarEnemigosEnEditor()
    {
        LimpiarEnemigosEnEditor();
        ConstruirGrupoEnemigosYEco();
        Debug.Log("<color=magenta>✓ Demonios Sombra y Eco generados permanentemente en la Hierarchy con Animators.</color>");
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
    }

    [ContextMenu("4. Borrar Enemigos de la Escena")]
    public void LimpiarEnemigosEnEditor()
    {
        GameObject grupo = GameObject.Find("Enemigos_SanFelipe");
        if (grupo != null)
        {
            if (Application.isPlaying) Destroy(grupo);
            else DestroyImmediate(grupo);
        }

        string[] nombres = { "Sombra_Lanzadora", "Sombra_Errante_Abismal", "Sombra_Errante_Guardiana", "Eco_De_SanFelipe" };
        foreach (var nom in nombres)
        {
            GameObject go = GameObject.Find(nom);
            if (go != null)
            {
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
        }
        Debug.Log("<color=yellow>✓ Enemigos eliminados de la escena.</color>");
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
    }

    [ContextMenu("5. Generar TODO (Mapa + Enemigos en Hierarchy)")]
    public void GenerarTodoEnEditor()
    {
        GenerarMapaEnEditor();
        GenerarEnemigosEnEditor();
        ParroquiaExteriorBuilder.ConstruirElementosExteriorParroquia();
        ParroquiaInteriorBuilder.ConstruirInteriorParroquia();
        MercadoBuilder.ConstruirZonaMercado();
        CerroDelFraileBuilder.ConstruirZonaCerroDelFraile();
    }

    // =========================================================================
    // CONSTRUCCIÓN DE SAN FELIPE CON TILESETS DE NINJA ADVENTURE
    // =========================================================================
    private void ConstruirMapaSanFelipeConSprites()
    {
        GameObject root = new GameObject("Mapa_SanFelipe_NinjaAdventure");
        GameObject capaSuelo = new GameObject("Capa_Suelo");
        capaSuelo.transform.SetParent(root.transform);

        Sprite spPastoBase = RecortarSpriteDeHoja(tilesetFloor, 1, 12, 1, 1);
        Sprite spPlazaArena = RecortarSpriteDeHoja(tilesetFloor, 1, 1, 1, 1);
        Sprite spCaminoPiedra = RecortarSpriteDeHoja(tilesetFloor, 12, 15, 1, 1);

        for (int x = -18; x <= 18; x++)
        {
            for (int y = -10; y <= 18; y++)
            {
                Sprite tileElegido = spPastoBase;

                bool esAvenidaVertical = (Mathf.Abs(x) <= 2 && y >= -8 && y <= 14);
                bool esAvenidaHorizontal = (Mathf.Abs(y) <= 1 && x >= -14 && x <= 12);
                bool esExplanadaPlaza = (Mathf.Abs(x) <= 6 && Mathf.Abs(y) <= 5);
                bool esExplanadaMercado = (x >= -15 && x <= -9 && Mathf.Abs(y) <= 4);

                if (esAvenidaVertical || esAvenidaHorizontal)
                {
                    tileElegido = spCaminoPiedra;
                }
                else if (esExplanadaPlaza || esExplanadaMercado)
                {
                    tileElegido = spPlazaArena;
                }

                ColocarTileSuelo(tileElegido, new Vector2(x, y), new Color(0.45f, 0.65f, 0.35f), -500, capaSuelo.transform);
            }
        }

        Sprite spPozoCentral = RecortarSpriteDeHoja(tilesetDesert, 0, 3, 2, 2);
        Sprite spPuestoRojo = RecortarSpriteDeHoja(tilesetDesert, 2, 10, 2, 2);
        Sprite spPuestoAmarillo = RecortarSpriteDeHoja(tilesetDesert, 4, 10, 2, 2);
        Sprite spCupulaParroquia = RecortarSpriteDeHoja(tilesetDesert, 0, 0, 3, 3);
        Sprite spTorreParroquia = RecortarSpriteDeHoja(tilesetDesert, 3, 0, 3, 5);

        Sprite spArbolLimpio = RecortarSpriteDeHoja(tilesetNature, 3, 18, 3, 3);
        Sprite spArbusto = RecortarSpriteDeHoja(tilesetNature, 10, 9, 1, 1);
        Sprite spGirasol = RecortarSpriteDeHoja(tilesetNature, 1, 10, 1, 1);

        Sprite spCasaTeja = RecortarSpriteDeHoja(tilesetHouse, 0, 0, 4, 3);
        Sprite spCasaAdobe = RecortarSpriteDeHoja(tilesetHouse, 4, 0, 4, 3);
        Sprite spCasaRoja = RecortarSpriteDeHoja(tilesetHouse, 12, 0, 4, 3);
        Sprite spEstatua = RecortarSpriteDeHoja(tilesetHouse, 3, 15, 2, 2);

        Sprite spLetrero = RecortarSpriteDeHoja(tilesetElement, 0, 2, 1, 1);

        // A) PLAZA PRINCIPAL
        ColocarObjetoDecorativo("Pozo_Central_Plaza", spPozoCentral, new Vector2(0f, -2.5f), new Vector2(1.8f, 1.4f), true, root.transform, Color.gray);
        ColocarObjetoDecorativo("Letrero_Tutorial", spLetrero, new Vector2(-2.5f, 1.5f), new Vector2(0.9f, 0.9f), true, root.transform, Color.yellow);

        Vector2[] esquinasPlaza = {
            new Vector2(-6.0f, 4.0f), new Vector2(6.0f, 4.0f),
            new Vector2(-6.0f, -4.5f), new Vector2(6.0f, -4.5f)
        };
        foreach (Vector2 posArbol in esquinasPlaza)
        {
            ColocarObjetoDecorativo("Arbol_Plaza", spArbolLimpio, posArbol, new Vector2(1.4f, 1.2f), true, root.transform, Color.green);
            ColocarObjetoDecorativo("Girasol", spGirasol, posArbol + new Vector2(-1.5f, -1.2f), Vector2.one, false, root.transform, Color.yellow);
            ColocarObjetoDecorativo("Arbusto", spArbusto, posArbol + new Vector2(1.5f, -1.2f), Vector2.one, false, root.transform, Color.green);
        }

        // B) ZONA OESTE: EL MERCADO DE SAN FELIPE
        ColocarObjetoDecorativo("Puesto_Rojo_1", spPuestoRojo, new Vector2(-11.0f, 3.2f), new Vector2(1.8f, 1.4f), true, root.transform, Color.red);
        ColocarObjetoDecorativo("Puesto_Amarillo_1", spPuestoAmarillo, new Vector2(-13.8f, 3.2f), new Vector2(1.8f, 1.4f), true, root.transform, Color.yellow);
        ColocarObjetoDecorativo("Puesto_Amarillo_2", spPuestoAmarillo, new Vector2(-11.0f, -2.8f), new Vector2(1.8f, 1.4f), true, root.transform, Color.yellow);
        ColocarObjetoDecorativo("Puesto_Rojo_2", spPuestoRojo, new Vector2(-13.8f, -2.8f), new Vector2(1.8f, 1.4f), true, root.transform, Color.red);
        ColocarObjetoDecorativo("Letrero_Mercado", spLetrero, new Vector2(-8.5f, 1.8f), new Vector2(0.9f, 0.9f), true, root.transform, Color.yellow);

        // C) CALLEJONES DEL CENTRO
        ColocarObjetoDecorativo("Estatua_Entrada_Izq", spEstatua, new Vector2(-3.5f, 5.5f), new Vector2(1.5f, 1.2f), true, root.transform, Color.gray);
        ColocarObjetoDecorativo("Estatua_Entrada_Der", spEstatua, new Vector2(3.5f, 5.5f), new Vector2(1.5f, 1.2f), true, root.transform, Color.gray);

        ColocarObjetoDecorativo("Casa_Callejon_Izq1", spCasaTeja, new Vector2(-6.2f, 8.0f), new Vector2(3.6f, 1.8f), true, root.transform, Color.brown);
        ColocarObjetoDecorativo("Casa_Callejon_Der1", spCasaAdobe, new Vector2(6.2f, 8.0f), new Vector2(3.6f, 1.8f), true, root.transform, Color.brown);
        ColocarObjetoDecorativo("Casa_Callejon_Izq2", spCasaRoja, new Vector2(-6.2f, 12.5f), new Vector2(3.6f, 1.8f), true, root.transform, Color.brown);
        ColocarObjetoDecorativo("Casa_Callejon_Der2", spCasaTeja, new Vector2(6.2f, 12.5f), new Vector2(3.6f, 1.8f), true, root.transform, Color.brown);

        // D) FONDO NORTE: LA PARROQUIA DE SAN FELIPE APÓSTOL
        ColocarObjetoDecorativo("Parroquia_Centro", spCupulaParroquia, new Vector2(0f, 15.5f), new Vector2(2.8f, 2.0f), true, root.transform, Color.white);
        ColocarObjetoDecorativo("Parroquia_Torre_Izq", spTorreParroquia, new Vector2(-3.0f, 16.5f), new Vector2(2.8f, 3.0f), true, root.transform, Color.white);
        ColocarObjetoDecorativo("Parroquia_Torre_Der", spTorreParroquia, new Vector2(3.0f, 16.5f), new Vector2(2.8f, 3.0f), true, root.transform, Color.white);

        // E) BORDES PERIMETRALES DE ÁRBOLES
        for (int x = -16; x <= 16; x += 3)
        {
            ColocarObjetoDecorativo("Bosque_Sur_" + x, spArbolLimpio, new Vector2(x, -8.5f), new Vector2(1.8f, 1.8f), true, root.transform, Color.green);
        }
        for (int y = -6; y <= 14; y += 4)
        {
            ColocarObjetoDecorativo("Bosque_Oeste_" + y, spArbolLimpio, new Vector2(-17.0f, y), new Vector2(1.8f, 1.8f), true, root.transform, Color.green);
            ColocarObjetoDecorativo("Bosque_Este_" + y, spArbolLimpio, new Vector2(14.0f, y), new Vector2(1.8f, 1.8f), true, root.transform, Color.green);
        }
    }

    private void ColocarTileSuelo(Sprite sprite, Vector2 pos, Color fallbackColor, int order, Transform parent)
    {
        GameObject tile = new GameObject("Tile_" + pos.x + "_" + pos.y);
        tile.transform.SetParent(parent);
        tile.transform.position = pos;

        SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
        sr.sortingOrder = order;

        if (sprite != null)
        {
            sr.sprite = sprite;
        }
        else
        {
            sr.sprite = CrearSpriteTexturizado(fallbackColor);
        }
    }

    private GameObject ColocarObjetoDecorativo(string nombre, Sprite sprite, Vector2 pos, Vector2 sizeColision, bool conColision, Transform parent, Color fallbackColor)
    {
        GameObject obj = new GameObject(nombre);
        obj.transform.SetParent(parent);
        obj.transform.position = pos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10);

        if (sprite != null)
        {
            sr.sprite = sprite;
        }
        else
        {
            sr.sprite = CrearSpriteTexturizado(fallbackColor);
            obj.transform.localScale = sizeColision;
        }

        if (conColision)
        {
            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            if (sprite != null)
            {
                col.size = sizeColision;
                col.offset = new Vector2(0f, -sizeColision.y * 0.2f);
            }
        }
        return obj;
    }

    private void CrearColisionadorInvisible(string nombre, Vector2 pos, Vector2 size, Transform parent)
    {
        GameObject lim = new GameObject(nombre);
        lim.transform.SetParent(parent);
        lim.transform.position = pos;
        BoxCollider2D col = lim.AddComponent<BoxCollider2D>();
        col.size = size;
    }

    private Sprite CrearSpriteTexturizado(Color colorBase)
    {
        Texture2D tex = new Texture2D(16, 16);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                bool esBorde = (x == 0 || y == 0 || x == 15 || y == 15);
                tex.SetPixel(x, y, esBorde ? colorBase * 0.82f : colorBase);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
    }

    // =========================================================================
    // DEMONIOS SOMBRA Y ECO LUMINOSO
    // =========================================================================
    public GameObject ConstruirGrupoEnemigosYEco()
    {
        GameObject root = new GameObject("Enemigos_SanFelipe");

        RuntimeAnimatorController animCtrlLanzadora = null;
        RuntimeAnimatorController animCtrlErrante = null;
        Sprite spriteLanzadora = null;
        Sprite spriteErrante = null;

#if UNITY_EDITOR
        animCtrlLanzadora = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/SombraLanzadora.controller");
        animCtrlErrante = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/SombraErrante.controller");

        Object[] assetsLanz = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/SombraLanzadora_SpriteSheet.png");
        if (assetsLanz != null)
        {
            foreach (var a in assetsLanz)
            {
                if (a is Sprite sp) { spriteLanzadora = sp; break; }
            }
        }

        Object[] assetsErr = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/SombraErrante_SpriteSheet.png");
        if (assetsErr != null)
        {
            foreach (var a in assetsErr)
            {
                if (a is Sprite sp) { spriteErrante = sp; break; }
            }
        }
#endif

        if (spriteLanzadora == null) spriteLanzadora = CrearSpriteCircular(new Color(0.9f, 0.15f, 0.25f));
        if (spriteErrante == null) spriteErrante = CrearSpriteCircular(new Color(0.2f, 0.8f, 0.95f));

        Transform target = playerTransform;
        if (target == null)
        {
            PlayerMovement p = FindFirstObjectByType<PlayerMovement>();
            if (p != null) target = p.transform;
        }

        // 1. Sombra Lanzadora (Norte, avenida principal frente a las casas)
        GameObject sombraLanz = new GameObject("Sombra_Lanzadora");
        sombraLanz.transform.SetParent(root.transform);
        sombraLanz.transform.position = new Vector3(0f, 6.5f, 0f);

        SpriteRenderer srLanz = sombraLanz.AddComponent<SpriteRenderer>();
        srLanz.sprite = spriteLanzadora;
        srLanz.sortingOrder = 4;

        if (animCtrlLanzadora != null)
        {
            Animator anim = sombraLanz.AddComponent<Animator>();
            anim.runtimeAnimatorController = animCtrlLanzadora;
        }

        CircleCollider2D colLanz = sombraLanz.AddComponent<CircleCollider2D>();
        colLanz.radius = 0.48f;

        BlackCloverDemonRenderer rendLanz = sombraLanz.AddComponent<BlackCloverDemonRenderer>();
        rendLanz.tipoDemonio = BlackCloverDemonRenderer.DemonType.SombraLanzadora;
        rendLanz.customDemonImage = imagenSombraLanzadora;

        SombraLanzadoraAI aiLanz = sombraLanz.AddComponent<SombraLanzadoraAI>();
        aiLanz.objetivo = target;
        aiLanz.builderRef = this;

        // 2. Sombra Errante Abismal (Oeste, cerca de los puestos de mercado)
        GameObject sombraErr1 = new GameObject("Sombra_Errante_Abismal");
        sombraErr1.transform.SetParent(root.transform);
        sombraErr1.transform.position = new Vector3(-3.0f, 4.0f, 0f);

        SpriteRenderer srErr1 = sombraErr1.AddComponent<SpriteRenderer>();
        srErr1.sprite = spriteErrante;
        srErr1.sortingOrder = 4;

        if (animCtrlErrante != null)
        {
            Animator anim = sombraErr1.AddComponent<Animator>();
            anim.runtimeAnimatorController = animCtrlErrante;
        }

        CircleCollider2D colErr1 = sombraErr1.AddComponent<CircleCollider2D>();
        colErr1.radius = 0.48f;

        BlackCloverDemonRenderer rendErr1 = sombraErr1.AddComponent<BlackCloverDemonRenderer>();
        rendErr1.tipoDemonio = BlackCloverDemonRenderer.DemonType.SombraErrante;
        rendErr1.customDemonImage = imagenSombraErrante != null ? imagenSombraErrante : imagenSombraLanzadora;

        SombraErranteAI aiErr1 = sombraErr1.AddComponent<SombraErranteAI>();
        aiErr1.objetivo = target;
        aiErr1.builderRef = this;

        // 3. Sombra Errante Guardiana (Este, plaza de la derecha)
        GameObject sombraErr2 = new GameObject("Sombra_Errante_Guardiana");
        sombraErr2.transform.SetParent(root.transform);
        sombraErr2.transform.position = new Vector3(3.0f, 4.0f, 0f);

        SpriteRenderer srErr2 = sombraErr2.AddComponent<SpriteRenderer>();
        srErr2.sprite = spriteErrante;
        srErr2.sortingOrder = 4;

        if (animCtrlErrante != null)
        {
            Animator anim = sombraErr2.AddComponent<Animator>();
            anim.runtimeAnimatorController = animCtrlErrante;
        }

        CircleCollider2D colErr2 = sombraErr2.AddComponent<CircleCollider2D>();
        colErr2.radius = 0.48f;

        BlackCloverDemonRenderer rendErr2 = sombraErr2.AddComponent<BlackCloverDemonRenderer>();
        rendErr2.tipoDemonio = BlackCloverDemonRenderer.DemonType.SombraErrante;
        rendErr2.customDemonImage = imagenSombraErrante != null ? imagenSombraErrante : imagenSombraLanzadora;

        SombraErranteAI aiErr2 = sombraErr2.AddComponent<SombraErranteAI>();
        aiErr2.objetivo = target;
        aiErr2.builderRef = this;

        // 4. Eco de San Felipe (Frente al atrio de la Parroquia al Norte)
        GameObject eco = new GameObject("Eco_De_SanFelipe");
        eco.transform.SetParent(root.transform);
        eco.transform.position = new Vector3(0f, 10.2f, 0f);

        SpriteRenderer srEco = eco.AddComponent<SpriteRenderer>();
        srEco.sprite = CrearSpriteCircular(new Color(0f, 0.95f, 1f));
        srEco.sortingOrder = 5;

        CircleCollider2D colEco = eco.AddComponent<CircleCollider2D>();
        colEco.isTrigger = true;
        colEco.radius = 0.5f;

        EcoLuminosoItem item = eco.AddComponent<EcoLuminosoItem>();
        item.builderRef = this;

        return root;
    }

    public void ConectarEnemigosExistentes()
    {
        if (playerTransform == null)
        {
            PlayerMovement p = FindFirstObjectByType<PlayerMovement>();
            if (p != null) playerTransform = p.transform;
        }

        SombraLanzadoraAI[] lanzadoras = FindObjectsByType<SombraLanzadoraAI>(FindObjectsSortMode.None);
        foreach (var l in lanzadoras)
        {
            l.objetivo = playerTransform;
            l.builderRef = this;
        }

        SombraErranteAI[] errantes = FindObjectsByType<SombraErranteAI>(FindObjectsSortMode.None);
        foreach (var e in errantes)
        {
            e.objetivo = playerTransform;
            e.builderRef = this;
        }

        EcoLuminosoItem[] ecos = FindObjectsByType<EcoLuminosoItem>(FindObjectsSortMode.None);
        foreach (var eco in ecos)
        {
            eco.builderRef = this;
        }
    }

    private void CrearSombraLanzadora(Vector3 posicion) => ConstruirGrupoEnemigosYEco();
    private void CrearEcoLuminoso(Vector3 posicion) { }

    public Sprite CrearSpriteCircular(Color color)
    {
        Texture2D tex = new Texture2D(16, 16);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                tex.SetPixel(x, y, d <= 7f ? color : Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
    }

    // =========================================================================
    // SISTEMA DE AUDIO PROCEDURAL / MP3
    // =========================================================================
    private void InicializarSistemaDeAudio()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = 0.35f;

        sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.clip = customExplorationMusic != null ? customExplorationMusic : GenerarMelodiaAmbientalSanFelipe();
        musicSource.Play();

        sfxSlash = GenerarSonidoEfecto(320f, 120f, 0.12f);
        sfxCounter = GenerarSonidoEfecto(480f, 980f, 0.22f);
        sfxEcho = GenerarSonidoEfecto(523.25f, 1046.5f, 0.45f);
    }

    public void ReproducirSonidoContraataque() => sfxSource.PlayOneShot(sfxCounter, 0.85f);
    public void ReproducirSonidoEco() => sfxSource.PlayOneShot(sfxEcho, 0.85f);

    private AudioClip GenerarMelodiaAmbientalSanFelipe()
    {
        int sampleRate = 44100;
        int duracionSegundos = 8;
        int totalSamples = sampleRate * duracionSegundos;
        float[] samples = new float[totalSamples];

        float[] notas = { 220f, 261.63f, 329.63f, 392f, 329.63f, 261.63f, 196f, 246.94f,
                          293.66f, 349.23f, 293.66f, 246.94f, 174.61f, 220f, 261.63f, 329.63f };

        int samplesPorNota = totalSamples / notas.Length;
        for (int i = 0; i < totalSamples; i++)
        {
            int indiceNota = Mathf.Min(i / samplesPorNota, notas.Length - 1);
            float frecuencia = notas[indiceNota];
            float t = (i % samplesPorNota) / (float)sampleRate;
            float envolvente = Mathf.Exp(-3.5f * t);
            float onda = Mathf.Sin(2f * Mathf.PI * frecuencia * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * frecuencia * t);
            samples[i] = onda * envolvente * 0.35f;
        }

        AudioClip clip = AudioClip.Create("Musica_SanFelipe_Procedural", totalSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerarSonidoEfecto(float freqInicio, float freqFin, float duracion)
    {
        int sampleRate = 44100;
        int count = Mathf.RoundToInt(sampleRate * duracion);
        float[] data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float progreso = i / (float)count;
            float freq = Mathf.Lerp(freqInicio, freqFin, progreso);
            float envolvente = 1f - progreso;
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * envolvente * 0.5f;
        }
        AudioClip clip = AudioClip.Create("SFX_Procedural", count, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}

public class SombraLanzadoraAI : MonoBehaviour
{
    public Transform objetivo;
    public SanFelipeDemoBuilder builderRef;
    public float vida = 36f;
    public float velocidadPersecucion = 2.2f;
    public float distanciaIdealDisparo = 4.2f;
    public float rangoDeteccion = 11f;
    private float temporizadorDisparo = 1.8f;
    private bool ejecutandoAtaque = false;

    void Start()
    {
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null && col.radius > 0.55f) col.radius = 0.48f;
    }

    void Update()
    {
        if (objetivo == null || ejecutandoAtaque) return;
        float distancia = Vector2.Distance(transform.position, objetivo.position);
        Vector2 direccion = (objetivo.position - transform.position).normalized;

        if (distancia < rangoDeteccion)
        {
            if (distancia > distanciaIdealDisparo)
                transform.position += (Vector3)(direccion * velocidadPersecucion * Time.deltaTime);
            else if (distancia < 2.0f)
                transform.position -= (Vector3)(direccion * (velocidadPersecucion * 0.6f) * Time.deltaTime);

            temporizadorDisparo -= Time.deltaTime;
            if (temporizadorDisparo <= 0f)
            {
                StartCoroutine(RutinaDisparoConCarga());
                temporizadorDisparo = 2.6f;
            }
        }
    }

    private IEnumerator RutinaDisparoConCarga()
    {
        ejecutandoAtaque = true;
        BlackCloverDemonRenderer demon = GetComponent<BlackCloverDemonRenderer>();
        if (demon != null) demon.ActivarPoseDeAtaque(0.60f);

        // Esperar a que termine la fase de carga (Frame 6) antes de escupir el proyectil en Frame 7
        yield return new WaitForSeconds(0.24f);

        if (objetivo != null)
        {
            GameObject proj = new GameObject("Proyectil_Sombra");
            proj.transform.position = transform.position;

            Sprite fallback = builderRef != null ? builderRef.CrearSpriteCircular(new Color(0.1f, 0.02f, 0.08f)) : null;
            SpriteRenderer sr = proj.AddComponent<SpriteRenderer>();
            if (fallback != null) sr.sprite = fallback;
            sr.sortingOrder = 5;
            proj.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

            GameObject nucleo = new GameObject("NucleoCarmesi");
            nucleo.transform.SetParent(proj.transform);
            nucleo.transform.localPosition = Vector3.zero;
            nucleo.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            SpriteRenderer srNucleo = nucleo.AddComponent<SpriteRenderer>();
            if (builderRef != null) srNucleo.sprite = builderRef.CrearSpriteCircular(new Color(1f, 0.1f, 0.25f));
            srNucleo.sortingOrder = 6;

            CircleCollider2D col = proj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;

            ProjectileBehaviour pb = proj.AddComponent<ProjectileBehaviour>();
            pb.Inicializar((objetivo.position - transform.position).normalized, this, builderRef);
        }

        yield return new WaitForSeconds(0.36f);
        ejecutandoAtaque = false;
    }

    public void RecibirDanio(float cantidad)
    {
        vida -= cantidad;
        AudioManager.Instance?.PlayHitSound();
        if (vida <= 0f)
        {
            SaveStateManager.Instance?.RegistrarEnemigoDerrotado();
            Destroy(gameObject);
        }
    }
}

public class SombraErranteAI : MonoBehaviour
{
    public Transform objetivo;
    public SanFelipeDemoBuilder builderRef;
    public float vida = 40f;
    public float velocidadPersecucion = 3.1f;
    public float rangoDeteccion = 9.5f;
    public float rangoAtaqueMelee = 1.35f;
    public float danioMelee = 14f;
    private Vector3 puntoOrigen;
    private float temporizadorAtaque = 0f;
    private bool ejecutandoAtaque = false;

    void Start()
    {
        puntoOrigen = transform.position;
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null && col.radius > 0.55f) col.radius = 0.48f;
    }

    void Update()
    {
        if (objetivo == null) return;
        if (temporizadorAtaque > 0f) temporizadorAtaque -= Time.deltaTime;
        if (ejecutandoAtaque) return;

        float distancia = Vector2.Distance(transform.position, objetivo.position);
        if (distancia <= rangoDeteccion)
        {
            if (distancia > rangoAtaqueMelee)
            {
                Vector2 dir = (objetivo.position - transform.position).normalized;
                transform.position += (Vector3)(dir * velocidadPersecucion * Time.deltaTime);
            }
            else if (temporizadorAtaque <= 0f)
            {
                StartCoroutine(RutinaZarpazoCuerpoACuerpo());
                temporizadorAtaque = 1.35f;
            }
        }
        else
        {
            Vector3 patrulla = puntoOrigen + new Vector3(Mathf.Sin(Time.time * 1.5f) * 1.5f, 0f, 0f);
            transform.position = Vector3.MoveTowards(transform.position, patrulla, (velocidadPersecucion * 0.5f) * Time.deltaTime);
        }
    }

    private IEnumerator RutinaZarpazoCuerpoACuerpo()
    {
        ejecutandoAtaque = true;
        BlackCloverDemonRenderer demon = GetComponent<BlackCloverDemonRenderer>();
        if (demon != null) demon.ActivarPoseDeAtaque(0.60f);

        // Esperar el tiempo de anticipación / carga (Frame 6) antes de conectar el zarpazo (Frame 7)
        yield return new WaitForSeconds(0.24f);

        if (objetivo != null)
        {
            float distActual = Vector2.Distance(transform.position, objetivo.position);
            if (distActual <= rangoAtaqueMelee + 0.55f)
            {
                PlayerMovement player = objetivo.GetComponent<PlayerMovement>();
                if (player != null) player.RecibirDanio(danioMelee);
            }
        }

        yield return new WaitForSeconds(0.36f);
        ejecutandoAtaque = false;
    }

    public void RecibirDanio(float cantidad)
    {
        vida -= cantidad;
        AudioManager.Instance?.PlayHitSound();
        if (vida <= 0f)
        {
            SaveStateManager.Instance?.RegistrarEnemigoDerrotado();
            Destroy(gameObject);
        }
    }
}

public class EcoLuminosoItem : MonoBehaviour
{
    public SanFelipeDemoBuilder builderRef;
    public int echoIndex = 0;

    void Update()
    {
        float s = 0.85f + Mathf.Sin(Time.time * 4f) * 0.15f;
        transform.localScale = new Vector3(s, s, 1f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player != null)
        {
            player.maxHealth += 25f;
            player.currentHealth = player.maxHealth;
            SaveStateManager.Instance?.RecuperarEco(echoIndex);
            if (builderRef != null) builderRef.ReproducirSonidoEco();
            AudioManager.Instance?.PlayEchoSound();
            Destroy(gameObject);
        }
    }
}
