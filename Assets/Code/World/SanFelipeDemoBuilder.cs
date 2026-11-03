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

    // Copias legibles en memoria de los tilesets y caché de sub-sprites sin costuras
    private Dictionary<Texture2D, Texture2D> readableCache = new Dictionary<Texture2D, Texture2D>();
    private Dictionary<string, Sprite> seamFreeSpriteCache = new Dictionary<string, Sprite>();

    // Audio
    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioClip sfxSlash;
    private AudioClip sfxCounter;
    private AudioClip sfxEcho;

    void Start()
    {
        // 1. Buscar a Ronan
        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
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
            mainCam.backgroundColor = new Color(0.24f, 0.38f, 0.22f);
            mainCam.orthographic = true;
            mainCam.orthographicSize = 6.2f;
        }

        // 3. Asegurar que exista el Gran Mapa V4 (81x65 unidades) sin costuras y sin enemigos sueltos
        if (GameObject.Find("Marca_GranMapa_SanFelipe_V4") == null)
        {
            LimpiarMapaEnEditor();
            LimpiarEnemigosEnEditor();
            ConstruirMapaSanFelipeConSprites();
        }
        else
        {
            // Limpiar cualquier enemigo suelto antiguo que haya quedado en la escena
            LimpiarEnemigosEnEditor();
        }

        AsegurarTexturasCargadas();

        // 4. Construir elementos interactivos, NPCs, Puerta de Escena e Hordas por Zona del Nivel 1 (La Parroquia)
        ParroquiaExteriorBuilder.ConstruirElementosExteriorParroquia();

        // 5. Asegurar Sistemas Globales y HUD de Juego Completo
        SaveStateManager.EnsureInstance();
        DialogueManager.EnsureInstance();
        InventoryManager.EnsureInstance();
        PuzzleManager.EnsureInstance();
        EnemyManager.EnsureInstance();
        ShopManager.EnsureInstance();
        InventoryHUD.EnsureInstance();
        BossHealthBarUI.EnsureInstance();
        PauseAndVictoryUI.EnsureInstance();

        // 5.1 Garantizar que los puentes estén siempre en orden -400 (suelo) y capa Default
        foreach (SpriteRenderer sr in FindObjectsByType<SpriteRenderer>())
        {
            if (sr != null)
            {
                string n = sr.gameObject.name;
                if (n.StartsWith("Puente_") || n.Contains("Escalinata") || n.Contains("Escalera") || n.Contains("Bridge"))
                {
                    sr.sortingLayerName = "Default";
                    sr.sortingOrder = -400;
                }
                else if (sr.sortingLayerName == "Characters")
                {
                    sr.sortingLayerName = "Default";
                }
            }
        }

        // 5.2 Asegurar que las luces URP 2D iluminen todas las capas de ordenación
        AsegurarIluminacionUniversal2D();

        // 6. Inicializar Audio
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

    private void AsegurarIluminacionUniversal2D()
    {
        try
        {
            System.Type light2DType = System.Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.2D.Runtime");
            if (light2DType == null) return;

            var lights = FindObjectsByType(light2DType, FindObjectsInactive.Include);
            if (lights == null || lights.Length == 0) return;

            var field = light2DType.GetField("m_ApplyToSortingLayers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null) return;

            int[] allLayerIDs = new int[SortingLayer.layers.Length];
            for (int i = 0; i < SortingLayer.layers.Length; i++)
            {
                allLayerIDs[i] = SortingLayer.layers[i].id;
            }

            foreach (var l in lights)
            {
                if (l != null)
                {
                    field.SetValue(l, allLayerIDs);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[SanFelipeDemoBuilder] No se pudo sincronizar m_ApplyToSortingLayers: " + e.Message);
        }
    }

    // =========================================================================
    // EXTRACTOR AUTOMÁTICO DE SPRITES SIN COSTURAS (SUB-TEXTURA AISLADA CLAMP)
    // =========================================================================
    private Texture2D ObtenerTexturaLegible(Texture2D original)
    {
        if (original == null) return null;
        if (readableCache.ContainsKey(original) && readableCache[original] != null) return readableCache[original];

        if (original.isReadable)
        {
            readableCache[original] = original;
            return original;
        }

        int w = original.width;
        int h = original.height;
        RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(original, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D copy = new Texture2D(w, h, TextureFormat.RGBA32, false);
        copy.filterMode = FilterMode.Point;
        copy.wrapMode = TextureWrapMode.Clamp;
        copy.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        copy.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        readableCache[original] = copy;
        return copy;
    }

    /// <summary>
    /// Extrae una sub-textura aislada con FilterMode.Point y TextureWrapMode.Clamp
    /// para eliminar al 100% las líneas negras o sangrado de píxeles entre tiles de suelo y estructuras.
    /// </summary>
    private Sprite RecortarSpriteDeHoja(Texture2D sheet, int col, int rowTop, int widthCells = 1, int heightCells = 1)
    {
        if (sheet == null) return null;
        string key = $"{sheet.name}_{col}_{rowTop}_{widthCells}_{heightCells}";
        if (seamFreeSpriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        Texture2D tex = ObtenerTexturaLegible(sheet);
        if (tex == null) return null;

        int cell = 16;
        int px = col * cell;
        int pw = widthCells * cell;
        int ph = heightCells * cell;
        int py = tex.height - ((rowTop + heightCells) * cell);

        px = Mathf.Clamp(px, 0, tex.width - 1);
        py = Mathf.Clamp(py, 0, tex.height - 1);
        pw = Mathf.Clamp(pw, 1, tex.width - px);
        ph = Mathf.Clamp(ph, 1, tex.height - py);

        Color[] pixels = tex.GetPixels(px, py, pw, ph);
        Texture2D subTex = new Texture2D(pw, ph, TextureFormat.RGBA32, false);
        subTex.name = key;
        subTex.filterMode = FilterMode.Point;
        subTex.wrapMode = TextureWrapMode.Clamp;
        subTex.SetPixels(pixels);
        subTex.Apply();

        Sprite sp = Sprite.Create(subTex, new Rect(0, 0, pw, ph), new Vector2(0.5f, 0.5f), 16f);
        sp.name = key;
        seamFreeSpriteCache[key] = sp;
        return sp;
    }

    private void AsegurarTexturasCargadas()
    {
        if (tilesetFloor == null) tilesetFloor = ParroquiaExteriorBuilder.CargarTextura("Assets/Sprites/TilesetFloor.png");
        if (tilesetDesert == null) tilesetDesert = ParroquiaExteriorBuilder.CargarTextura("Assets/Sprites/TilesetDesert.png");
        if (tilesetHouse == null) tilesetHouse = ParroquiaExteriorBuilder.CargarTextura("Assets/Sprites/TilesetHouse.png");
        if (tilesetNature == null) tilesetNature = ParroquiaExteriorBuilder.CargarTextura("Assets/Sprites/TilesetNature.png");
        if (tilesetElement == null) tilesetElement = ParroquiaExteriorBuilder.CargarTextura("Assets/Sprites/TilesetElement.png");
        if (imagenSombraErrante == null) imagenSombraErrante = ParroquiaExteriorBuilder.CargarTextura("Assets/Sprites/SombraErrante_SpriteSheet.png");
        if (imagenSombraLanzadora == null) imagenSombraLanzadora = ParroquiaExteriorBuilder.CargarTextura("Assets/Sprites/SombraLanzadora_SpriteSheet.png");
        if (imagenSombraGuardiana == null) imagenSombraGuardiana = ParroquiaExteriorBuilder.CargarTextura("Assets/Sprites/SombraGuardiana_SpriteSheet.png");
    }

    // =========================================================================
    // GENERACIÓN Y EDICIÓN DESDE EL EDITOR (MENÚ DE CLIC DERECHO EN EL INSPECTOR)
    // =========================================================================
    [ContextMenu("1. Generar Mapa Permanente en la Escena")]
    public void GenerarMapaEnEditor()
    {
        LimpiarMapaEnEditor();
        ConstruirMapaSanFelipeConSprites();
        Debug.Log("<color=green>✓ Gran Mapa de San Felipe V3 (81x65) generado permanentemente en la Hierarchy.</color>");
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
    }

    [ContextMenu("2. Borrar Mapa de la Escena")]
    public void LimpiarMapaEnEditor()
    {
        string[] objetosMapa = {
            "Mapa_SanFelipe_NinjaAdventure",
            "Parroquia_Exterior_Elementos",
            "Parroquia_Interior_Zona",
            "Mercado_SanFelipe_Zona",
            "CerroDelFraile_Zona"
        };
        foreach (string nombre in objetosMapa)
        {
            GameObject viejo = GameObject.Find(nombre);
            if (viejo != null)
            {
                if (Application.isPlaying) Destroy(viejo);
                else DestroyImmediate(viejo);
            }
        }
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
    }

    [ContextMenu("3. Generar Hordas y Elementos de Parroquia en Hierarchy")]
    public void GenerarEnemigosEnEditor()
    {
        LimpiarEnemigosEnEditor();
        ParroquiaExteriorBuilder.ConstruirElementosExteriorParroquia();
        Debug.Log("<color=magenta>✓ Zonas de Hordas y NPCs del Nivel 1 (Parroquia) generados en la Hierarchy.</color>");
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
    }

    [ContextMenu("4. Borrar Enemigos Sueltos de la Escena")]
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
    }

    [ContextMenu("5. Generar TODO Nivel 1 (Gran Mapa + Hordas + Puerta a Escena ParroquiaInterior)")]
    public void GenerarTodoEnEditor()
    {
        LimpiarMapaEnEditor();
        LimpiarEnemigosEnEditor();
        ConstruirMapaSanFelipeConSprites();
        ParroquiaExteriorBuilder.ConstruirElementosExteriorParroquia();
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
    }

    // =========================================================================
    // CONSTRUCCIÓN DE LA GRAN VILLA Y PARROQUIA DE SAN FELIPE (81 x 65 UNIDADES)
    // =========================================================================
    private void ConstruirMapaSanFelipeConSprites()
    {
        AsegurarTexturasCargadas();

        Texture2D texFloorDetail = ParroquiaExteriorBuilder.CargarTextura("Assets/Sprites/TilesetFloorDetail.png");
        Texture2D texWater       = ParroquiaExteriorBuilder.CargarTextura("Assets/Sprites/TilesetWater.png");
        Texture2D texCamp        = ParroquiaExteriorBuilder.CargarTextura("Assets/Sprites/tileset_camp.png");
        Texture2D texTowers      = ParroquiaExteriorBuilder.CargarTextura("Assets/Sprites/TilesetTowers.png");
        Texture2D texAbandoned   = ParroquiaExteriorBuilder.CargarTextura("Assets/Sprites/TilesetVillageAbandoned.png");

        GameObject root = new GameObject("Mapa_SanFelipe_NinjaAdventure");
        GameObject marcaV4 = new GameObject("Marca_GranMapa_SanFelipe_V4");
        marcaV4.transform.SetParent(root.transform);

        GameObject capaSuelo = new GameObject("Capa_Suelo_81x65");
        capaSuelo.transform.SetParent(root.transform);

        GameObject distritoSur      = new GameObject("Distrito_Sur_Rio_Puentes_Campamento");
        GameObject distritoCentro   = new GameObject("Distrito_Centro_Gran_Plaza_De_Armas");
        GameObject distritoPoniente = new GameObject("Distrito_Poniente_Barrio_Artesanos_Y_Puestos");
        GameObject distritoOriente  = new GameObject("Distrito_Oriente_Villa_Molino_Y_Ruinas");
        GameObject distritoNorte    = new GameObject("Distrito_Norte_Calzada_Y_Atrio_Parroquia");
        GameObject bordesBosque     = new GameObject("Perimetro_Bosques_Y_Murallas");

        distritoSur.transform.SetParent(root.transform);
        distritoCentro.transform.SetParent(root.transform);
        distritoPoniente.transform.SetParent(root.transform);
        distritoOriente.transform.SetParent(root.transform);
        distritoNorte.transform.SetParent(root.transform);
        bordesBosque.transform.SetParent(root.transform);

        // ---------------------------------------------------------------------
        // 1. TILES DE SUELO VERIFICADOS EN PYTHON (SIN COSTURAS NI CUADROS DE TIERRA)
        // ---------------------------------------------------------------------
        Sprite spPastoBase     = RecortarSpriteDeHoja(tilesetFloor, 0, 12, 1, 1);
        Sprite spPastoClaro    = RecortarSpriteDeHoja(tilesetFloor, 1, 12, 1, 1);
        Sprite spPlazaArena    = RecortarSpriteDeHoja(tilesetFloor, 1, 1, 1, 1);
        Sprite spCaminoPiedra  = RecortarSpriteDeHoja(tilesetFloor, 12, 15, 1, 1);
        Sprite spAdoquinAtrio  = RecortarSpriteDeHoja(tilesetFloor, 12, 1, 1, 1);
        Sprite spSueloRuinas   = RecortarSpriteDeHoja(tilesetFloor, 16, 1, 1, 1);
        Sprite spAguaRio       = RecortarSpriteDeHoja(texWater, 1, 1, 1, 1);
        Sprite spDetallePasto  = RecortarSpriteDeHoja(tilesetNature, 2, 11, 1, 1);
        Sprite spDetallePiedra = RecortarSpriteDeHoja(texFloorDetail, 2, 1, 1, 1);

        for (int x = -40; x <= 40; x++)
        {
            for (int y = -26; y <= 38; y++)
            {
                Sprite tileElegido = ((x * 7 + y * 13) % 4 == 0 && spPastoClaro != null) ? spPastoClaro : spPastoBase;

                bool esRioSur = (y >= -20 && y <= -18);
                bool esPuenteCentral = (Mathf.Abs(x) <= 2 && y >= -21 && y <= -17);
                bool esPuenteEste = (x >= 23 && x <= 26 && y >= -21 && y <= -17);

                bool esCalzadaNorteSur = (Mathf.Abs(x) <= 3 && y >= -25 && y <= 28);
                bool esAvenidaOesteEste = (Mathf.Abs(y) <= 2 && x >= -36 && x <= 36);
                bool esSenderoCampamento = (x >= -18 && x <= -4 && y >= -24 && y <= -22);
                bool esSenderoPuenteEste = (x >= 24 && x <= 25 && y >= -24 && y <= -1);

                bool esGranPlazaCentral = (Mathf.Abs(x) <= 13 && y >= -10 && y <= 8);
                bool esAtrioParroquia = (Mathf.Abs(x) <= 16 && y >= 20 && y <= 34);
                bool esPlazoletaPoniente = (x >= -34 && x <= -18 && y >= -8 && y <= 14);
                bool esVillaOriente = (x >= 18 && x <= 34 && y >= -10 && y <= 4);
                bool esJardinRuinasOriente = (x >= 18 && x <= 35 && y >= 6 && y <= 19);

                if (esRioSur && !esPuenteCentral && !esPuenteEste)
                {
                    tileElegido = spAguaRio != null ? spAguaRio : spCaminoPiedra;
                }
                else if (esCalzadaNorteSur || esAvenidaOesteEste || esSenderoCampamento || esSenderoPuenteEste)
                {
                    tileElegido = spCaminoPiedra;
                }
                else if (esAtrioParroquia)
                {
                    tileElegido = spAdoquinAtrio != null ? spAdoquinAtrio : spPlazaArena;
                }
                else if (esGranPlazaCentral || esPlazoletaPoniente || esVillaOriente)
                {
                    tileElegido = spPlazaArena;
                }
                else if (esJardinRuinasOriente)
                {
                    tileElegido = spSueloRuinas != null ? spSueloRuinas : spPlazaArena;
                }

                ColocarTileSuelo(tileElegido, new Vector2(x, y), new Color(0.45f, 0.65f, 0.35f), -500, capaSuelo.transform);

                // Tréboles naturales discretos sobre el césped (sin troncos cortados)
                if (!esRioSur && !esGranPlazaCentral && !esAtrioParroquia && !esPlazoletaPoniente && !esVillaOriente && !esCalzadaNorteSur && !esAvenidaOesteEste && ((x * 19 + y * 37) % 41 == 0))
                {
                    if (spDetallePasto != null)
                    {
                        ColocarTileSuelo(spDetallePasto, new Vector2(x, y), Color.white, -495, capaSuelo.transform);
                    }
                }
            }
        }

        // ---------------------------------------------------------------------
        // 2. EXTRACCIÓN DE ESTRUCTURAS REALES (MEDIDAS CON PYTHON)
        // ---------------------------------------------------------------------
        // Estructuras de Agua y Campamento (Distrito Sur)
        Sprite spPuenteMadera = RecortarSpriteDeHoja(texWater, 0, 12, 2, 4);
        Sprite spLanchaH      = RecortarSpriteDeHoja(texWater, 26, 0, 2, 1);
        Sprite spTiendaGrande = RecortarSpriteDeHoja(texCamp, 4, 0, 3, 3);
        Sprite spFogata       = RecortarSpriteDeHoja(texCamp, 10, 3, 2, 2);
        Sprite spBancaMadera  = RecortarSpriteDeHoja(texCamp, 0, 7, 3, 1);
        Sprite spFarolMadera  = RecortarSpriteDeHoja(texCamp, 6, 5, 1, 2);

        // Estructuras de Desierto / Parroquia / Plazas (TilesetDesert y TilesetTowers)
        Sprite spCupulaParroquia   = RecortarSpriteDeHoja(tilesetDesert, 0, 0, 3, 3);
        Sprite spTorreParroquia    = RecortarSpriteDeHoja(tilesetDesert, 3, 0, 3, 5);
        Sprite spCastilloParroquia = RecortarSpriteDeHoja(tilesetDesert, 14, 4, 3, 5);
        Sprite spMuroAlmenas       = RecortarSpriteDeHoja(tilesetDesert, 17, 4, 3, 3);
        Sprite spTorreCupulaVerde  = RecortarSpriteDeHoja(texTowers, 12, 2, 2, 2);
        Sprite spEdificio2Pisos    = RecortarSpriteDeHoja(tilesetDesert, 6, 3, 4, 4);
        Sprite spCasaBlancaAncha   = RecortarSpriteDeHoja(tilesetDesert, 11, 0, 3, 3);
        Sprite spPozoCentral       = RecortarSpriteDeHoja(tilesetDesert, 0, 3, 2, 2);
        Sprite spFuenteLeon        = RecortarSpriteDeHoja(tilesetDesert, 0, 6, 2, 3);
        Sprite spEstatuaLeon       = RecortarSpriteDeHoja(tilesetDesert, 0, 9, 2, 3);
        Sprite spAlbercaCuadrada   = RecortarSpriteDeHoja(tilesetDesert, 8, 7, 2, 2);
        Sprite spPuestoRojo        = RecortarSpriteDeHoja(tilesetDesert, 2, 10, 2, 2);
        Sprite spPuestoAmarillo    = RecortarSpriteDeHoja(tilesetDesert, 4, 10, 2, 2);
        Sprite spToldoAzul         = RecortarSpriteDeHoja(tilesetDesert, 6, 7, 2, 2);
        Sprite spToldoRojo         = RecortarSpriteDeHoja(tilesetDesert, 2, 10, 2, 2);
        Sprite spPalmera           = RecortarSpriteDeHoja(tilesetDesert, 10, 4, 3, 3);

        // Estructuras Coloniales y Casas (TilesetHouse)
        Sprite spCasaTejaRoja      = RecortarSpriteDeHoja(tilesetHouse, 0, 0, 4, 3);
        Sprite spCasaPaja          = RecortarSpriteDeHoja(tilesetHouse, 4, 0, 4, 3);
        Sprite spCasaAdobe         = RecortarSpriteDeHoja(tilesetHouse, 8, 0, 4, 3);
        Sprite spCasaTejaOscura    = RecortarSpriteDeHoja(tilesetHouse, 12, 0, 4, 3);
        Sprite spCasonaPanaderia   = RecortarSpriteDeHoja(tilesetHouse, 16, 0, 3, 3);
        Sprite spCasaVerdeMadera   = RecortarSpriteDeHoja(tilesetHouse, 19, 0, 4, 3);
        Sprite spTorreRedonda      = RecortarSpriteDeHoja(tilesetHouse, 23, 0, 3, 3);
        Sprite spGraneroMadera     = RecortarSpriteDeHoja(tilesetHouse, 26, 0, 3, 3);
        Sprite spTorreMolino       = RecortarSpriteDeHoja(tilesetHouse, 29, 4, 3, 4);
        Sprite spCasaAlta2Pisos    = RecortarSpriteDeHoja(tilesetHouse, 25, 7, 4, 7);
        Sprite spCasaAltaPiedra    = RecortarSpriteDeHoja(tilesetHouse, 25, 14, 4, 6);
        Sprite spHornoPiedra       = RecortarSpriteDeHoja(tilesetHouse, 29, 11, 2, 2);
        Sprite spChimeneaAlta      = RecortarSpriteDeHoja(tilesetHouse, 31, 12, 2, 3);
        Sprite spRejaParque        = RecortarSpriteDeHoja(tilesetHouse, 9, 10, 5, 3);
        Sprite spEstatuaDorada     = RecortarSpriteDeHoja(tilesetHouse, 3, 15, 2, 2);
        Sprite spEstatuaGuerrero   = RecortarSpriteDeHoja(tilesetHouse, 5, 17, 2, 2);
        Sprite spEscaleraAncha     = RecortarSpriteDeHoja(tilesetHouse, 10, 19, 3, 3);
        Sprite spArcoPiedra        = RecortarSpriteDeHoja(tilesetHouse, 29, 20, 4, 3);
        Sprite spEstandarteRojo    = RecortarSpriteDeHoja(tilesetHouse, 23, 20, 1, 2);
        Sprite spEstandarteAzul    = RecortarSpriteDeHoja(tilesetHouse, 24, 20, 1, 2);

        // Estructuras de Ruinas Antiguas (TilesetVillageAbandoned)
        Sprite spCasonaAntigua     = RecortarSpriteDeHoja(texAbandoned, 12, 6, 4, 5);
        Sprite spTemploAntiguo     = RecortarSpriteDeHoja(texAbandoned, 11, 3, 3, 3);
        Sprite spCasaMusgo1        = RecortarSpriteDeHoja(texAbandoned, 16, 0, 4, 3);
        Sprite spCasaMusgo2        = RecortarSpriteDeHoja(texAbandoned, 20, 0, 4, 3);
        Sprite spEstatuaMusgo      = RecortarSpriteDeHoja(texAbandoned, 0, 3, 2, 3);
        Sprite spColumnaMusgo      = RecortarSpriteDeHoja(texAbandoned, 2, 3, 1, 3);
        Sprite spPozoMusgo         = RecortarSpriteDeHoja(texAbandoned, 4, 3, 2, 2);
        Sprite spRuinaPilar        = RecortarSpriteDeHoja(texAbandoned, 9, 5, 2, 2);

        // Naturaleza (TilesetNature)
        Sprite spArbolCerezo       = RecortarSpriteDeHoja(tilesetNature, 0, 18, 3, 3);
        Sprite spArbolVerde        = RecortarSpriteDeHoja(tilesetNature, 3, 18, 3, 3);
        Sprite spArbolSagrado      = RecortarSpriteDeHoja(tilesetNature, 6, 18, 3, 3);
        Sprite spArbolDorado       = RecortarSpriteDeHoja(tilesetNature, 9, 18, 3, 3);
        Sprite spPinoGrande        = RecortarSpriteDeHoja(tilesetNature, 0, 2, 4, 3);
        Sprite spArbusto           = RecortarSpriteDeHoja(tilesetNature, 10, 9, 1, 1);
        Sprite spGirasol           = RecortarSpriteDeHoja(tilesetNature, 1, 10, 1, 1);
        Sprite spFloresRojas       = RecortarSpriteDeHoja(tilesetNature, 0, 11, 2, 2);
        Sprite spRocaGrande        = RecortarSpriteDeHoja(tilesetNature, 16, 4, 2, 2);

        // Elementos Urbanos (TilesetElement y TilesetTowers)
        Sprite spLetrero           = RecortarSpriteDeHoja(tilesetElement, 0, 2, 1, 1);
        Sprite spCarretaToldo      = RecortarSpriteDeHoja(tilesetElement, 0, 3, 2, 2);
        Sprite spCarretaMadera     = RecortarSpriteDeHoja(tilesetElement, 2, 3, 2, 2);
        Sprite spFarolHierro       = RecortarSpriteDeHoja(tilesetElement, 6, 3, 1, 2);
        Sprite spCajasApiladas     = RecortarSpriteDeHoja(tilesetElement, 10, 5, 2, 2);
        Sprite spBarriles          = RecortarSpriteDeHoja(tilesetElement, 12, 5, 2, 2);
        Sprite spVallaHierro       = RecortarSpriteDeHoja(tilesetElement, 2, 11, 3, 1);
        Sprite spSantuarioRojo     = RecortarSpriteDeHoja(texTowers, 18, 2, 2, 2);

        // ---------------------------------------------------------------------
        // A) DISTRITO SUR: RÍO SAN FELIPE, PUENTES Y CAMPAMENTO DE VIAJEROS
        // ---------------------------------------------------------------------
        // Los puentes son SUELO transitable: se dibujan en un orden fijo (justo sobre el terreno y bajo cualquier personaje/objeto).
        // Antes usaban orden = -y*10 y tapaban a Ronan al cruzar la mitad norte del puente.
        // Puente central: 3 tramos de 2u cubren exactamente el hueco del río (x = -3..3).
        PonerComoSueloTransitable(ColocarObjetoDecorativo("Puente_Sur_Principal_Izq", spPuenteMadera, new Vector2(-2.0f, -19.0f), Vector2.zero, false, distritoSur.transform, Color.brown));
        PonerComoSueloTransitable(ColocarObjetoDecorativo("Puente_Sur_Principal_Centro", spPuenteMadera, new Vector2(0.0f, -19.0f), Vector2.zero, false, distritoSur.transform, Color.brown));
        PonerComoSueloTransitable(ColocarObjetoDecorativo("Puente_Sur_Principal_Der", spPuenteMadera, new Vector2(2.0f, -19.0f), Vector2.zero, false, distritoSur.transform, Color.brown));
        // Puente oriente: 2 tramos cubren el hueco x = 22..26
        PonerComoSueloTransitable(ColocarObjetoDecorativo("Puente_Sur_Oriente_A", spPuenteMadera, new Vector2(23.0f, -19.0f), Vector2.zero, false, distritoSur.transform, Color.brown));
        PonerComoSueloTransitable(ColocarObjetoDecorativo("Puente_Sur_Oriente_B", spPuenteMadera, new Vector2(25.0f, -19.0f), Vector2.zero, false, distritoSur.transform, Color.brown));

        ColocarObjetoDecorativo("Lancha_Rio_Oeste", spLanchaH, new Vector2(-10.5f, -19.0f), new Vector2(1.8f, 0.9f), true, distritoSur.transform, Color.brown);
        ColocarObjetoDecorativo("Lancha_Rio_Este", spLanchaH, new Vector2(12.5f, -19.0f), new Vector2(1.8f, 0.9f), true, distritoSur.transform, Color.brown);

        // Colisionadores del Río (excepto en los puentes)
        CrearColisionadorInvisible("Colision_Rio_Oeste", new Vector2(-21.5f, -19.0f), new Vector2(37f, 2.6f), distritoSur.transform);
        CrearColisionadorInvisible("Colision_Rio_Centro", new Vector2(12.5f, -19.0f), new Vector2(19f, 2.6f), distritoSur.transform);
        CrearColisionadorInvisible("Colision_Rio_Este", new Vector2(33.0f, -19.0f), new Vector2(14f, 2.6f), distritoSur.transform);

        // Campamento de Viajeros al Suroeste del Río
        ColocarObjetoDecorativo("Tienda_Viajeros_1", spTiendaGrande, new Vector2(-14.5f, -22.0f), new Vector2(2.6f, 2.0f), true, distritoSur.transform, Color.yellow);
        ColocarObjetoDecorativo("Tienda_Viajeros_2", spTiendaGrande, new Vector2(-9.5f, -22.0f), new Vector2(2.6f, 2.0f), true, distritoSur.transform, Color.yellow);
        ColocarObjetoDecorativo("Fogata_Campamento", spFogata, new Vector2(-12.0f, -24.0f), new Vector2(1.4f, 1.2f), true, distritoSur.transform, Color.red);
        ColocarObjetoDecorativo("Banca_Campamento", spBancaMadera, new Vector2(-12.0f, -25.4f), new Vector2(2.2f, 0.8f), true, distritoSur.transform, Color.brown);
        ColocarObjetoDecorativo("Farol_Puente_Sur_Izq", spFarolMadera, new Vector2(-3.5f, -16.5f), new Vector2(0.8f, 1.0f), true, distritoSur.transform, Color.yellow);
        ColocarObjetoDecorativo("Farol_Puente_Sur_Der", spFarolMadera, new Vector2(3.5f, -16.5f), new Vector2(0.8f, 1.0f), true, distritoSur.transform, Color.yellow);

        // ---------------------------------------------------------------------
        // B) DISTRITO CENTRAL: GRAN PLAZA DE ARMAS DE SAN FELIPE
        // ---------------------------------------------------------------------
        ColocarObjetoDecorativo("Pozo_Monumental_Plaza", spPozoCentral, new Vector2(0f, -3.2f), new Vector2(1.8f, 1.4f), true, distritoCentro.transform, Color.gray);
        ColocarObjetoDecorativo("Fuente_Leon_Oeste", spFuenteLeon, new Vector2(-8.5f, 3.8f), new Vector2(1.8f, 2.2f), true, distritoCentro.transform, Color.gray);
        ColocarObjetoDecorativo("Fuente_Leon_Este", spFuenteLeon, new Vector2(8.5f, 3.8f), new Vector2(1.8f, 2.2f), true, distritoCentro.transform, Color.gray);
        ColocarObjetoDecorativo("Estanque_Sur_Oeste", spAlbercaCuadrada, new Vector2(-8.5f, -6.5f), new Vector2(1.8f, 1.6f), true, distritoCentro.transform, Color.cyan);
        ColocarObjetoDecorativo("Estanque_Sur_Este", spAlbercaCuadrada, new Vector2(8.5f, -6.5f), new Vector2(1.8f, 1.6f), true, distritoCentro.transform, Color.cyan);

        // Jardineras enrejadas y bancas de la Gran Plaza
        ColocarObjetoDecorativo("Jardinera_Reja_NO", spRejaParque, new Vector2(-9.5f, 7.2f), new Vector2(4.2f, 2.0f), true, distritoCentro.transform, Color.gray);
        ColocarObjetoDecorativo("Jardinera_Reja_NE", spRejaParque, new Vector2(9.5f, 7.2f), new Vector2(4.2f, 2.0f), true, distritoCentro.transform, Color.gray);
        ColocarObjetoDecorativo("Banca_Plaza_NO", spBancaMadera, new Vector2(-5.5f, 3.5f), new Vector2(2.2f, 0.8f), true, distritoCentro.transform, Color.brown);
        ColocarObjetoDecorativo("Banca_Plaza_NE", spBancaMadera, new Vector2(5.5f, 3.5f), new Vector2(2.2f, 0.8f), true, distritoCentro.transform, Color.brown);
        ColocarObjetoDecorativo("Banca_Plaza_SO", spBancaMadera, new Vector2(-5.5f, -4.5f), new Vector2(2.2f, 0.8f), true, distritoCentro.transform, Color.brown);
        ColocarObjetoDecorativo("Banca_Plaza_SE", spBancaMadera, new Vector2(5.5f, -4.5f), new Vector2(2.2f, 0.8f), true, distritoCentro.transform, Color.brown);

        // Faroles coloniales de hierro alrededor de la Plaza y Avenidas
        Vector2[] posFaroles = {
            new Vector2(-4.2f, 5.2f), new Vector2(4.2f, 5.2f),
            new Vector2(-4.2f, -1.8f), new Vector2(4.2f, -1.8f),
            new Vector2(-4.2f, -8.5f), new Vector2(4.2f, -8.5f),
            new Vector2(-13.5f, 2.8f), new Vector2(13.5f, 2.8f),
            new Vector2(-13.5f, -2.8f), new Vector2(13.5f, -2.8f)
        };
        for (int i = 0; i < posFaroles.Length; i++)
        {
            ColocarObjetoDecorativo("Farol_Hierro_Plaza_" + i, spFarolHierro, posFaroles[i], new Vector2(0.7f, 0.9f), true, distritoCentro.transform, Color.yellow);
        }

        // Árboles ornamentales (Cerezos y Árboles Dorados) en la Gran Plaza
        ColocarObjetoDecorativo("Cerezo_Plaza_NO", spArbolCerezo, new Vector2(-12.0f, 5.0f), new Vector2(1.6f, 1.4f), true, distritoCentro.transform, Color.magenta);
        ColocarObjetoDecorativo("Cerezo_Plaza_NE", spArbolCerezo, new Vector2(12.0f, 5.0f), new Vector2(1.6f, 1.4f), true, distritoCentro.transform, Color.magenta);
        ColocarObjetoDecorativo("ArbolDorado_Plaza_SO", spArbolDorado, new Vector2(-12.0f, -7.5f), new Vector2(1.6f, 1.4f), true, distritoCentro.transform, Color.yellow);
        ColocarObjetoDecorativo("ArbolDorado_Plaza_SE", spArbolDorado, new Vector2(12.0f, -7.5f), new Vector2(1.6f, 1.4f), true, distritoCentro.transform, Color.yellow);

        // ---------------------------------------------------------------------
        // C) DISTRITO PONIENTE: BARRIO DE ARTESANOS, PANADERÍA Y PLAZOLETA OESTE
        // ---------------------------------------------------------------------
        ColocarObjetoDecorativo("Arco_Entrada_Poniente", spArcoPiedra, new Vector2(-16.5f, 2.2f), new Vector2(3.2f, 1.4f), false, distritoPoniente.transform, Color.gray);

        // Grandes Edificios de 2 pisos y Panadería al Norte de la Plazoleta Poniente
        ColocarObjetoDecorativo("Casona_2Pisos_Poniente_1", spCasaAlta2Pisos, new Vector2(-22.0f, 14.5f), new Vector2(3.6f, 3.2f), true, distritoPoniente.transform, Color.brown);
        ColocarObjetoDecorativo("Casona_2Pisos_Poniente_2", spCasaAltaPiedra, new Vector2(-27.5f, 14.5f), new Vector2(3.6f, 3.0f), true, distritoPoniente.transform, Color.gray);
        ColocarObjetoDecorativo("Edificio_Comercial_Oeste", spEdificio2Pisos, new Vector2(-33.0f, 14.0f), new Vector2(3.6f, 2.6f), true, distritoPoniente.transform, Color.white);

        ColocarObjetoDecorativo("Panaderia_SanFelipe", spCasonaPanaderia, new Vector2(-21.0f, 7.8f), new Vector2(2.8f, 2.0f), true, distritoPoniente.transform, Color.red);
        ColocarObjetoDecorativo("Horno_Panaderia", spHornoPiedra, new Vector2(-18.2f, 7.5f), new Vector2(1.6f, 1.4f), true, distritoPoniente.transform, Color.gray);
        ColocarObjetoDecorativo("Chimenea_Artesanos", spChimeneaAlta, new Vector2(-35.5f, 8.2f), new Vector2(1.6f, 2.0f), true, distritoPoniente.transform, Color.gray);
        ColocarObjetoDecorativo("Casa_Artesano_Verde", spCasaVerdeMadera, new Vector2(-32.5f, 7.8f), new Vector2(3.6f, 2.0f), true, distritoPoniente.transform, Color.green);

        // Puestos de Artesanos, Toldos y Carretas en el Sur del Barrio Poniente
        ColocarObjetoDecorativo("Puesto_Rojo_Oeste_1", spPuestoRojo, new Vector2(-21.0f, -4.5f), new Vector2(1.8f, 1.4f), true, distritoPoniente.transform, Color.red);
        ColocarObjetoDecorativo("Puesto_Amarillo_Oeste_1", spPuestoAmarillo, new Vector2(-25.0f, -4.5f), new Vector2(1.8f, 1.4f), true, distritoPoniente.transform, Color.yellow);
        ColocarObjetoDecorativo("Toldo_Azul_Oeste", spToldoAzul, new Vector2(-29.0f, -4.5f), new Vector2(1.8f, 1.4f), true, distritoPoniente.transform, Color.cyan);
        ColocarObjetoDecorativo("Toldo_Rojo_Oeste", spToldoRojo, new Vector2(-33.0f, -4.5f), new Vector2(1.8f, 1.4f), true, distritoPoniente.transform, Color.red);

        ColocarObjetoDecorativo("Casa_Sur_Poniente_1", spCasaTejaRoja, new Vector2(-22.5f, -9.5f), new Vector2(3.6f, 2.0f), true, distritoPoniente.transform, Color.red);
        ColocarObjetoDecorativo("Casa_Sur_Poniente_2", spCasaAdobe, new Vector2(-29.5f, -9.5f), new Vector2(3.6f, 2.0f), true, distritoPoniente.transform, Color.brown);
        ColocarObjetoDecorativo("Carreta_Toldo_Poniente", spCarretaToldo, new Vector2(-18.5f, -4.2f), new Vector2(1.8f, 1.4f), true, distritoPoniente.transform, Color.brown);
        ColocarObjetoDecorativo("Carreta_Carga_Poniente", spCarretaMadera, new Vector2(-35.5f, -3.5f), new Vector2(1.8f, 1.4f), true, distritoPoniente.transform, Color.brown);
        ColocarObjetoDecorativo("Cajas_Mercancia_Oeste", spCajasApiladas, new Vector2(-19.5f, 5.2f), new Vector2(1.5f, 1.2f), true, distritoPoniente.transform, Color.brown);
        ColocarObjetoDecorativo("Barriles_Oeste", spBarriles, new Vector2(-35.0f, 5.2f), new Vector2(1.5f, 1.2f), true, distritoPoniente.transform, Color.brown);

        // ---------------------------------------------------------------------
        // D) DISTRITO ORIENTE: VILLA RESIDENCIAL, MOLINO Y JARDÍN DE LAS RUINAS
        // ---------------------------------------------------------------------
        ColocarObjetoDecorativo("Arco_Entrada_Oriente", spArcoPiedra, new Vector2(16.5f, 2.2f), new Vector2(3.2f, 1.4f), false, distritoOriente.transform, Color.gray);

        // Villa Residencial y Molino (Sur-Oriente)
        ColocarObjetoDecorativo("Casa_Colonial_Este_1", spCasaTejaRoja, new Vector2(21.5f, -4.5f), new Vector2(3.6f, 2.0f), true, distritoOriente.transform, Color.red);
        ColocarObjetoDecorativo("Casa_Colonial_Este_2", spCasaPaja, new Vector2(27.5f, -4.5f), new Vector2(3.6f, 2.0f), true, distritoOriente.transform, Color.yellow);
        ColocarObjetoDecorativo("Torre_Molino_Oriente", spTorreMolino, new Vector2(33.5f, -4.0f), new Vector2(2.6f, 2.8f), true, distritoOriente.transform, Color.brown);
        ColocarObjetoDecorativo("Granero_Oriente", spGraneroMadera, new Vector2(21.5f, -9.8f), new Vector2(2.8f, 2.0f), true, distritoOriente.transform, Color.brown);
        ColocarObjetoDecorativo("Casa_TejaOscura_Este", spCasaTejaOscura, new Vector2(28.5f, -9.8f), new Vector2(3.6f, 2.0f), true, distritoOriente.transform, Color.gray);
        ColocarObjetoDecorativo("Torre_Vigia_Este", spTorreRedonda, new Vector2(34.5f, -9.5f), new Vector2(2.6f, 2.2f), true, distritoOriente.transform, Color.gray);

        // Jardín de las Ruinas Antiguas (Norte-Oriente, X: 18..35, Y: 6..19)
        ColocarObjetoDecorativo("Casona_Abandonada_Ruinas", spCasonaAntigua, new Vector2(26.0f, 17.5f), new Vector2(3.6f, 3.2f), true, distritoOriente.transform, Color.gray);
        ColocarObjetoDecorativo("Templo_Antiguo_Ruinas", spTemploAntiguo, new Vector2(32.5f, 16.8f), new Vector2(2.8f, 2.4f), true, distritoOriente.transform, Color.gray);
        ColocarObjetoDecorativo("Casa_Musgo_Ruinas_1", spCasaMusgo1, new Vector2(20.0f, 16.5f), new Vector2(3.6f, 2.0f), true, distritoOriente.transform, Color.green);
        ColocarObjetoDecorativo("Casa_Musgo_Ruinas_2", spCasaMusgo2, new Vector2(34.5f, 10.5f), new Vector2(3.6f, 2.0f), true, distritoOriente.transform, Color.green);
        ColocarObjetoDecorativo("Pozo_Antiguo_Musgo", spPozoMusgo, new Vector2(26.0f, 11.5f), new Vector2(1.8f, 1.4f), true, distritoOriente.transform, Color.gray);
        ColocarObjetoDecorativo("Estatua_Musgo_Izq", spEstatuaMusgo, new Vector2(21.5f, 12.5f), new Vector2(1.6f, 2.0f), true, distritoOriente.transform, Color.gray);
        ColocarObjetoDecorativo("Estatua_Musgo_Der", spEstatuaMusgo, new Vector2(30.5f, 12.5f), new Vector2(1.6f, 2.0f), true, distritoOriente.transform, Color.gray);
        ColocarObjetoDecorativo("Columna_Musgo_1", spColumnaMusgo, new Vector2(22.5f, 8.0f), new Vector2(0.9f, 1.8f), true, distritoOriente.transform, Color.gray);
        ColocarObjetoDecorativo("Columna_Musgo_2", spColumnaMusgo, new Vector2(29.5f, 8.0f), new Vector2(0.9f, 1.8f), true, distritoOriente.transform, Color.gray);
        ColocarObjetoDecorativo("Pilar_Ruina_1", spRuinaPilar, new Vector2(19.5f, 9.5f), new Vector2(1.5f, 1.4f), true, distritoOriente.transform, Color.gray);

        // ---------------------------------------------------------------------
        // E) DISTRITO NORTE: CALZADA REAL Y GRAN ATRIO AMURALLADO DE LA PARROQUIA
        // ---------------------------------------------------------------------
        // Calzada Real (Y: 9..19) flanqueada por Casonas Coloniales, Estatuas de León y Estandartes
        ColocarObjetoDecorativo("Estatua_Leon_Calzada_Izq", spEstatuaLeon, new Vector2(-4.5f, 9.5f), new Vector2(1.8f, 2.2f), true, distritoNorte.transform, Color.gray);
        ColocarObjetoDecorativo("Estatua_Leon_Calzada_Der", spEstatuaLeon, new Vector2(4.5f, 9.5f), new Vector2(1.8f, 2.2f), true, distritoNorte.transform, Color.gray);

        ColocarObjetoDecorativo("Casa_Calzada_Izq_1", spCasaTejaRoja, new Vector2(-9.5f, 11.5f), new Vector2(3.6f, 2.0f), true, distritoNorte.transform, Color.red);
        ColocarObjetoDecorativo("Casa_Calzada_Der_1", spCasaAdobe, new Vector2(9.5f, 11.5f), new Vector2(3.6f, 2.0f), true, distritoNorte.transform, Color.brown);
        ColocarObjetoDecorativo("Casa_Calzada_Izq_2", spCasaBlancaAncha, new Vector2(-9.5f, 16.0f), new Vector2(2.8f, 2.0f), true, distritoNorte.transform, Color.white);
        ColocarObjetoDecorativo("Casa_Calzada_Der_2", spCasaBlancaAncha, new Vector2(9.5f, 16.0f), new Vector2(2.8f, 2.0f), true, distritoNorte.transform, Color.white);

        ColocarObjetoDecorativo("Estandarte_Calzada_Izq_1", spEstandarteRojo, new Vector2(-4.0f, 13.5f), new Vector2(0.8f, 1.2f), true, distritoNorte.transform, Color.red);
        ColocarObjetoDecorativo("Estandarte_Calzada_Der_1", spEstandarteRojo, new Vector2(4.0f, 13.5f), new Vector2(0.8f, 1.2f), true, distritoNorte.transform, Color.red);
        ColocarObjetoDecorativo("Estandarte_Calzada_Izq_2", spEstandarteAzul, new Vector2(-4.0f, 17.5f), new Vector2(0.8f, 1.2f), true, distritoNorte.transform, Color.blue);
        ColocarObjetoDecorativo("Estandarte_Calzada_Der_2", spEstandarteAzul, new Vector2(4.0f, 17.5f), new Vector2(0.8f, 1.2f), true, distritoNorte.transform, Color.blue);

        // Escalinata Monumental / Puente de acceso al Atrio (Y = 19.5, suelo transitable en orden -400)
        PonerComoSueloTransitable(ColocarObjetoDecorativo("Escalinata_Atrio_Parroquia", spEscaleraAncha, new Vector2(0f, 19.5f), Vector2.zero, false, distritoNorte.transform, Color.gray));

        // Muralla Frontal del Atrio (con entrada abierta en X = -3..3)
        ColocarObjetoDecorativo("Muro_Atrio_Sur_Izq_1", spMuroAlmenas, new Vector2(-6.0f, 19.8f), new Vector2(2.8f, 2.0f), true, distritoNorte.transform, Color.gray);
        ColocarObjetoDecorativo("Muro_Atrio_Sur_Izq_2", spMuroAlmenas, new Vector2(-9.5f, 19.8f), new Vector2(2.8f, 2.0f), true, distritoNorte.transform, Color.gray);
        ColocarObjetoDecorativo("Muro_Atrio_Sur_Izq_3", spMuroAlmenas, new Vector2(-13.0f, 19.8f), new Vector2(2.8f, 2.0f), true, distritoNorte.transform, Color.gray);
        ColocarObjetoDecorativo("Torre_Atrio_Esquina_SO", spTorreCupulaVerde, new Vector2(-16.0f, 20.0f), new Vector2(1.8f, 1.8f), true, distritoNorte.transform, Color.green);

        ColocarObjetoDecorativo("Muro_Atrio_Sur_Der_1", spMuroAlmenas, new Vector2(6.0f, 19.8f), new Vector2(2.8f, 2.0f), true, distritoNorte.transform, Color.gray);
        ColocarObjetoDecorativo("Muro_Atrio_Sur_Der_2", spMuroAlmenas, new Vector2(9.5f, 19.8f), new Vector2(2.8f, 2.0f), true, distritoNorte.transform, Color.gray);
        ColocarObjetoDecorativo("Muro_Atrio_Sur_Der_3", spMuroAlmenas, new Vector2(13.0f, 19.8f), new Vector2(2.8f, 2.0f), true, distritoNorte.transform, Color.gray);
        ColocarObjetoDecorativo("Torre_Atrio_Esquina_SE", spTorreCupulaVerde, new Vector2(16.0f, 20.0f), new Vector2(1.8f, 1.8f), true, distritoNorte.transform, Color.green);

        // Interior del Gran Atrio de la Parroquia (Y = 21..30)
        ColocarObjetoDecorativo("Estatua_Guerrero_Atrio_Izq", spEstatuaGuerrero, new Vector2(-5.5f, 23.5f), new Vector2(1.6f, 1.6f), true, distritoNorte.transform, Color.gray);
        ColocarObjetoDecorativo("Estatua_Guerrero_Atrio_Der", spEstatuaGuerrero, new Vector2(5.5f, 23.5f), new Vector2(1.6f, 1.6f), true, distritoNorte.transform, Color.gray);
        ColocarObjetoDecorativo("Estatua_Dorada_Atrio_Izq", spEstatuaDorada, new Vector2(-5.5f, 27.5f), new Vector2(1.6f, 1.6f), true, distritoNorte.transform, Color.yellow);
        ColocarObjetoDecorativo("Estatua_Dorada_Atrio_Der", spEstatuaDorada, new Vector2(5.5f, 27.5f), new Vector2(1.6f, 1.6f), true, distritoNorte.transform, Color.yellow);

        ColocarObjetoDecorativo("Santuario_Atrio_Oeste", spSantuarioRojo, new Vector2(-12.5f, 26.5f), new Vector2(1.8f, 1.8f), true, distritoNorte.transform, Color.red);
        ColocarObjetoDecorativo("Santuario_Atrio_Este", spSantuarioRojo, new Vector2(12.5f, 26.5f), new Vector2(1.8f, 1.8f), true, distritoNorte.transform, Color.red);
        ColocarObjetoDecorativo("Arbol_Sagrado_Atrio_Oeste", spArbolSagrado, new Vector2(-11.5f, 23.0f), new Vector2(1.6f, 1.4f), true, distritoNorte.transform, Color.white);
        ColocarObjetoDecorativo("Arbol_Sagrado_Atrio_Este", spArbolSagrado, new Vector2(11.5f, 23.0f), new Vector2(1.6f, 1.4f), true, distritoNorte.transform, Color.white);

        // COMPLEJO MONUMENTAL DE LA PARROQUIA DE SAN FELIPE APÓSTOL (Y = 31.5..33.5)
        ColocarObjetoDecorativo("Parroquia_Nave_Central", spCupulaParroquia, new Vector2(0f, 31.5f), new Vector2(2.8f, 2.2f), true, distritoNorte.transform, Color.white);
        ColocarObjetoDecorativo("Parroquia_Campanario_Izq", spTorreParroquia, new Vector2(-3.2f, 32.5f), new Vector2(2.8f, 3.2f), true, distritoNorte.transform, Color.white);
        ColocarObjetoDecorativo("Parroquia_Campanario_Der", spTorreParroquia, new Vector2(3.2f, 32.5f), new Vector2(2.8f, 3.2f), true, distritoNorte.transform, Color.white);
        ColocarObjetoDecorativo("Parroquia_Ala_Oeste", spCastilloParroquia, new Vector2(-6.5f, 32.5f), new Vector2(2.8f, 3.2f), true, distritoNorte.transform, Color.white);
        ColocarObjetoDecorativo("Parroquia_Ala_Este", spCastilloParroquia, new Vector2(6.5f, 32.5f), new Vector2(2.8f, 3.2f), true, distritoNorte.transform, Color.white);
        ColocarObjetoDecorativo("Parroquia_Claustro_Oeste", spMuroAlmenas, new Vector2(-10.0f, 31.8f), new Vector2(2.8f, 2.2f), true, distritoNorte.transform, Color.gray);
        ColocarObjetoDecorativo("Parroquia_Claustro_Este", spMuroAlmenas, new Vector2(10.0f, 31.8f), new Vector2(2.8f, 2.2f), true, distritoNorte.transform, Color.gray);
        ColocarObjetoDecorativo("Parroquia_Torre_NO", spTorreCupulaVerde, new Vector2(-13.0f, 32.2f), new Vector2(1.8f, 1.8f), true, distritoNorte.transform, Color.green);
        ColocarObjetoDecorativo("Parroquia_Torre_NE", spTorreCupulaVerde, new Vector2(13.0f, 32.2f), new Vector2(1.8f, 1.8f), true, distritoNorte.transform, Color.green);

        // ---------------------------------------------------------------------
        // F) PERÍMETRO DE BOSQUES DENSOS Y LÍMITES DEL MAPA (81 x 65)
        // ---------------------------------------------------------------------
        for (int x = -38; x <= 38; x += 4)
        {
            ColocarObjetoDecorativo("Bosque_Norte_" + x, spPinoGrande, new Vector2(x, 36.5f), new Vector2(2.4f, 2.2f), true, bordesBosque.transform, Color.green);
            if (Mathf.Abs(x) > 4)
            {
                ColocarObjetoDecorativo("Bosque_Sur_" + x, spArbolVerde, new Vector2(x, -25.0f), new Vector2(2.0f, 1.8f), true, bordesBosque.transform, Color.green);
            }
        }

        for (int y = -22; y <= 34; y += 4)
        {
            ColocarObjetoDecorativo("Bosque_Oeste_" + y, spPinoGrande, new Vector2(-38.5f, y), new Vector2(2.4f, 2.2f), true, bordesBosque.transform, Color.green);
            ColocarObjetoDecorativo("Bosque_Este_" + y, spPinoGrande, new Vector2(38.5f, y), new Vector2(2.4f, 2.2f), true, bordesBosque.transform, Color.green);
        }

        // Colisionadores perimetrales invisibles para asegurar que Ronan no salga del mundo 81x65
        CrearColisionadorInvisible("Limite_Mundo_Norte", new Vector2(0f, 37.5f), new Vector2(84f, 2f), bordesBosque.transform);
        CrearColisionadorInvisible("Limite_Mundo_Sur", new Vector2(0f, -26.5f), new Vector2(84f, 2f), bordesBosque.transform);
        CrearColisionadorInvisible("Limite_Mundo_Oeste", new Vector2(-40.5f, 6f), new Vector2(2f, 68f), bordesBosque.transform);
        CrearColisionadorInvisible("Limite_Mundo_Este", new Vector2(40.5f, 6f), new Vector2(2f, 68f), bordesBosque.transform);
    }

    /// <summary>Fija el orden de dibujo de un objeto de suelo (puentes, alfombras) para que nunca oculte a los personajes.</summary>
    private void PonerComoSueloTransitable(GameObject obj)
    {
        if (obj == null) return;
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Default";
            sr.sortingOrder = -400; // terreno = -500, personajes/objetos >= -300 aprox.
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
            obj.transform.localScale = sizeColision != Vector2.zero ? sizeColision : Vector2.one;
        }

        if (conColision && sizeColision != Vector2.zero)
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
        tex.wrapMode = TextureWrapMode.Clamp;
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

    public GameObject ConstruirGrupoEnemigosYEco()
    {
        ParroquiaExteriorBuilder.ConstruirElementosExteriorParroquia();
        return GameObject.Find("Parroquia_Exterior_Elementos");
    }

    public void ConectarEnemigosExistentes()
    {
        if (playerTransform == null)
        {
            PlayerMovement p = FindAnyObjectByType<PlayerMovement>();
            if (p != null) playerTransform = p.transform;
        }
    }

    public Sprite CrearSpriteCircular(Color color)
    {
        Texture2D tex = new Texture2D(16, 16);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
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

    private bool muerto = false;

    void Start()
    {
        if (!CompareTag("Enemy") && !CompareTag("Player")) gameObject.tag = "Enemy";
        EnemyManager.EnsureInstance()?.RegisterEnemy(gameObject);

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null && col.radius > 0.55f) col.radius = 0.48f;

        HealthComponent hc = GetComponent<HealthComponent>();
        if (hc == null) hc = gameObject.AddComponent<HealthComponent>();
        hc.isPlayer = false;
        hc.maxHealth = vida;
        hc.currentHealth = vida;
        if (hc.onDeath == null) hc.onDeath = new UnityEngine.Events.UnityEvent();
        hc.onDeath.AddListener(() => RecibirDanio(999f));
    }

    void Update()
    {
        if (objetivo == null)
        {
            PlayerMovement p = FindAnyObjectByType<PlayerMovement>();
            if (p != null) objetivo = p.transform;
        }
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
        if (muerto) return;
        vida -= cantidad;
        HealthComponent hc = GetComponent<HealthComponent>();
        if (hc != null) hc.currentHealth = Mathf.Max(0f, vida);
        AudioManager.Instance?.PlayHitSound();
        if (vida <= 0f)
        {
            muerto = true;
            ejecutandoAtaque = true;
            EnemyManager.Instance?.UnregisterEnemy(gameObject);
            SaveStateManager.Instance?.RegistrarEnemigoDerrotado();
            ResourcePickup.SpawnEnemyLoot(transform.position, transform.parent);
            StartCoroutine(RutinaMuerteSombra());
        }
    }

    private IEnumerator RutinaMuerteSombra()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Vector3 startScale = transform.localScale;
        float t = 0f;
        while (t < 0.45f)
        {
            t += Time.deltaTime;
            float p = t / 0.45f;
            transform.localScale = Vector3.Lerp(startScale, startScale * 0.15f, p);
            if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f - p);
            yield return null;
        }
        Destroy(gameObject);
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
    public float danioMelee = 12f;
    private Vector3 puntoOrigen;
    private float temporizadorAtaque = 0f;
    private bool ejecutandoAtaque = false;
    private bool muerto = false;

    void Start()
    {
        puntoOrigen = transform.position;
        if (!CompareTag("Enemy") && !CompareTag("Player")) gameObject.tag = "Enemy";
        EnemyManager.EnsureInstance()?.RegisterEnemy(gameObject);
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null && col.radius > 0.55f) col.radius = 0.48f;

        HealthComponent hc = GetComponent<HealthComponent>();
        if (hc == null) hc = gameObject.AddComponent<HealthComponent>();
        hc.isPlayer = false;
        hc.maxHealth = vida;
        hc.currentHealth = vida;
        if (hc.onDeath == null) hc.onDeath = new UnityEngine.Events.UnityEvent();
        hc.onDeath.AddListener(() => RecibirDanio(999f));
    }

    void Update()
    {
        if (objetivo == null)
        {
            PlayerMovement p = FindAnyObjectByType<PlayerMovement>();
            if (p != null) objetivo = p.transform;
        }
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
        if (muerto) return;
        vida -= cantidad;
        HealthComponent hc = GetComponent<HealthComponent>();
        if (hc != null) hc.currentHealth = Mathf.Max(0f, vida);
        AudioManager.Instance?.PlayHitSound();
        if (vida <= 0f)
        {
            muerto = true;
            ejecutandoAtaque = true;
            EnemyManager.Instance?.UnregisterEnemy(gameObject);
            SaveStateManager.Instance?.RegistrarEnemigoDerrotado();
            ResourcePickup.SpawnEnemyLoot(transform.position, transform.parent);
            StartCoroutine(RutinaMuerteSombraErrante());
        }
    }

    private IEnumerator RutinaMuerteSombraErrante()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Vector3 startScale = transform.localScale;
        float t = 0f;
        while (t < 0.45f)
        {
            t += Time.deltaTime;
            float p = t / 0.45f;
            transform.localScale = Vector3.Lerp(startScale, startScale * 0.15f, p);
            if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f - p);
            yield return null;
        }
        Destroy(gameObject);
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
