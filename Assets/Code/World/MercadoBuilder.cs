using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Constructor y controlador del Nivel 2: El Mercado Municipal y Callejones Coloniales de San Felipe.
/// Construye una escena de 77x77 unidades (X: -38..+38, Y: -38..+38) 100% libre de fondos negros vacíos y
/// sin recortes de tileset_bed, usando exclusivamente arquitectura colonial real:
///  • Suelo variado: adoquín empedrado (TilesetFloor), losas de cantera con detalles de desgaste (TilesetFloorDetail)
///    y alfombras ceremoniales frente a cada pabellón y en la glorieta central.
///  • Manzanas completas de casonas coloniales de 1 y 2 pisos con techos de teja roja, hornos, chimeneas y arcos de cantera.
///  • 4 Pabellones Gremiales con NPCs físicos visibles y ambientación viva:
///      - Don Braulio, el Herrero (Noroeste: fragua humeante, horno, yunque, armas en exhibición).
///      - Doña Remedios, la Alfarera (Noreste: taller de cerámica, vasijas de barro y platos coloniales).
///      - Don Teodoro, el Joyero (Suroeste: vitrina elegante con gemas y lingotes relucientes).
///      - Doña Lucía, Vendedora de Pan y Especias (Sureste: panadería, carretas y sacos de grano).
///  • Habitantes y guardias secundarios con diálogos de historia y pistas del mercado.
///  • Bloqueo estricto hacia el Nivel 3 mediante el «Gran Portón de Hierro Gremial con Sello de Tradición»,
///    que solo se abre al recuperar el Eco de la Tradición (Eco 2).
/// </summary>
public class MercadoBuilder : MonoBehaviour
{
    [Header("Seguimiento de Cámara")]
    public Transform playerTransform;
    public float cameraSmoothTime = 0.14f;
    private Vector3 cameraVelocity = Vector3.zero;
    private Camera mainCam;

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "Nivel2_Mercado")
        {
            InicializarEscenaMercado();
        }
    }

    private void LateUpdate()
    {
        if (SceneManager.GetActiveScene().name != "Nivel2_Mercado") return;

        if (mainCam == null) mainCam = Camera.main;
        if (playerTransform == null)
        {
            PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
            if (pm != null) playerTransform = pm.transform;
        }

        if (mainCam != null && playerTransform != null)
        {
            Vector3 targetPos = new Vector3(playerTransform.position.x, playerTransform.position.y, -10f);
            mainCam.transform.position = Vector3.SmoothDamp(mainCam.transform.position, targetPos, ref cameraVelocity, cameraSmoothTime);
        }
    }

    public void InicializarEscenaMercado()
    {
        // 1. Asegurar singletons de sistema y UI
        SaveStateManager.EnsureInstance();
        DialogueManager.EnsureInstance();
        InventoryManager.EnsureInstance();
        PuzzleManager.EnsureInstance();
        EnemyManager.EnsureInstance();
        ShopManager.EnsureInstance();
        InventoryHUD.EnsureInstance();
        PauseAndVictoryUI.EnsureInstance();
        if (AudioManager.Instance == null)
        {
            GameObject am = new GameObject("AudioManager");
            am.AddComponent<AudioManager>();
        }

        LevelManager.EnsureInstance().currentZone = LevelManager.ZoneId.Mercado;

        // 2. Asegurar a Ronan (Player) en la entrada sur del callejón
        GameObject playerObj = AsegurarPlayer(new Vector2(0f, -32f));
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        // 3. Configurar Cámara Principal
        mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5.5f;
            mainCam.backgroundColor = new Color(0.24f, 0.36f, 0.20f);
            if (playerTransform != null)
            {
                mainCam.transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y, -10f);
            }
        }

        // 4. Construir el mundo del Mercado Municipal
        ConstruirZonaMercado();
    }

    public static GameObject AsegurarPlayer(Vector2 posicionInicial)
    {
        PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
        {
            pm.transform.position = posicionInicial;
            return pm.gameObject;
        }

        GameObject player = new GameObject("Player");
        player.transform.position = posicionInicial;
        player.tag = "Player";

        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 0;

        BoxCollider2D col = player.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 0.6f);
        col.offset = new Vector2(0f, -0.1f);

        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearDamping = 4.5f;

        pm = player.AddComponent<PlayerMovement>();
        pm.maxHealth = 100f;
        pm.currentHealth = 100f;
        pm.moveSpeed = 5.0f;

        player.AddComponent<CombatStateMachine>();
        player.AddComponent<HealthComponent>();

        PunyCharacterAnimator puny = player.AddComponent<PunyCharacterAnimator>();
        Texture2D ronanTex = ZoneWaveSpawner.LoadSpriteTexture("Human-Soldier-Cyan");
        if (ronanTex != null)
        {
            puny.spriteSheet = ronanTex;
            puny.pixelsPerUnit = 16f;
        }

        return player;
    }

    public static GameObject ConstruirZonaMercado(Transform parentRoot = null)
    {
        GameObject existente = GameObject.Find("Zona_Mercado_Municipal_Detallada");
        if (existente != null)
        {
            if (GameObject.Find("Marca_Mercado_V5") != null) return existente;
            if (Application.isPlaying) Destroy(existente);
            else DestroyImmediate(existente);
        }

        GameObject root = new GameObject("Zona_Mercado_Municipal_Detallada");
        if (parentRoot != null) root.transform.SetParent(parentRoot);
        CheckpointTrigger.Crear("Checkpoint_Plaza_Trueque", new Vector2(0f, 0f), LevelManager.ZoneId.Mercado, root.transform);
        CheckpointTrigger.Crear("Checkpoint_Santuario_Gremial", new Vector2(0f, 20f), LevelManager.ZoneId.Mercado, root.transform);
        GameObject marca = new GameObject("Marca_Mercado_V5");
        marca.transform.SetParent(root.transform);

        // 1. RECORTE DE SPRITES REALES VERIFICADOS (CERO tileset_bed)
        // Suelos y detalles
        Sprite spPastoBase      = CortarCelda("TilesetFloor", 0, 12, 1, 1);
        Sprite spPastoClaro     = CortarCelda("TilesetFloor", 1, 12, 1, 1);
        Sprite spPlazaCantera   = CortarCelda("TilesetFloor", 1, 1, 1, 1);
        Sprite spCaminoPiedra   = CortarCelda("TilesetFloor", 12, 15, 1, 1);
        Sprite spAdoquinReal    = CortarCelda("TilesetFloor", 12, 1, 1, 1);
        Sprite spDetallePiedra1 = CortarCelda("TilesetFloorDetail", 2, 1, 1, 1);
        Sprite spDetallePiedra2 = CortarCelda("TilesetFloorDetail", 0, 1, 1, 1);
        Sprite spAlfombraRoja   = CortarCelda("TilesetInteriorFloor", 1, 13, 1, 1);
        Sprite spAlfombraOro    = CortarCelda("TilesetInteriorFloor", 13, 1, 1, 1);

        // Arquitectura Colonial (TilesetHouse, TilesetDesert y TilesetVillageAbandoned)
        Sprite spCasaTejaRoja    = CortarCelda("TilesetHouse", 0, 0, 4, 3);
        Sprite spCasaPaja        = CortarCelda("TilesetHouse", 4, 0, 4, 3);
        Sprite spCasaAdobe       = CortarCelda("TilesetHouse", 8, 0, 4, 3);
        Sprite spCasaTejaOscura  = CortarCelda("TilesetHouse", 12, 0, 4, 3);
        Sprite spCasonaPanaderia = CortarCelda("TilesetHouse", 16, 0, 3, 3);
        Sprite spCasaVerdeMadera = CortarCelda("TilesetHouse", 19, 0, 4, 3);
        Sprite spCasaAlta2Pisos  = CortarCelda("TilesetHouse", 25, 7, 4, 7);
        Sprite spCasaAltaPiedra  = CortarCelda("TilesetHouse", 25, 14, 4, 6);
        Sprite spHornoPiedra     = CortarCelda("TilesetHouse", 29, 11, 2, 2);
        Sprite spChimeneaAlta    = CortarCelda("TilesetHouse", 31, 12, 2, 3);
        Sprite spRejaParque      = CortarCelda("TilesetHouse", 9, 10, 5, 3);
        Sprite spEstatuaDorada   = CortarCelda("TilesetHouse", 3, 15, 2, 2);
        Sprite spEscalera        = CortarCelda("TilesetHouse", 10, 19, 3, 2);
        Sprite spArcoPiedra      = CortarCelda("TilesetHouse", 29, 20, 4, 3);
        Sprite spEstandarteRojo  = CortarCelda("TilesetHouse", 23, 20, 1, 2);
        Sprite spEstandarteAzul  = CortarCelda("TilesetHouse", 24, 20, 1, 2);
        Sprite spEstandarteOro   = CortarCelda("TilesetHouse", 28, 20, 1, 2);

        Sprite spMuroAlmenas     = CortarCelda("TilesetDesert", 17, 4, 3, 3);
        Sprite spEdificio2Pisos  = CortarCelda("TilesetDesert", 6, 3, 4, 4);
        Sprite spCasaBlancaAncha = CortarCelda("TilesetDesert", 11, 0, 3, 3);
        Sprite spPuestoRojo      = CortarCelda("TilesetDesert", 2, 10, 2, 2);
        Sprite spPuestoAmarillo  = CortarCelda("TilesetDesert", 4, 10, 2, 2);
        Sprite spToldoAzul       = CortarCelda("TilesetDesert", 6, 7, 2, 2);
        Sprite spFuenteLeon      = CortarCelda("TilesetDesert", 0, 6, 2, 3);
        Sprite spColumna         = CortarCelda("TilesetVillageAbandoned", 2, 3, 1, 3);

        // Mostradores, Carretas, Vasijas y Elementos Urbanos (TilesetElement y tileset_camp)
        Sprite spMostradorMadera = CortarCelda("tileset_camp", 0, 7, 3, 1);
        Sprite spTiendaMercader  = CortarCelda("tileset_camp", 4, 0, 3, 3);
        Sprite spCarretaToldo    = CortarCelda("TilesetElement", 0, 3, 2, 2);
        Sprite spCarretaMadera   = CortarCelda("TilesetElement", 2, 3, 2, 2);
        Sprite spVasijas         = CortarCelda("TilesetElement", 4, 0, 2, 1);
        Sprite spFarolHierro     = CortarCelda("TilesetElement", 6, 3, 1, 2);
        Sprite spCajasApiladas   = CortarCelda("TilesetElement", 10, 5, 2, 2);
        Sprite spBarriles        = CortarCelda("TilesetElement", 12, 5, 2, 2);
        Sprite spVallaHierro     = CortarCelda("TilesetElement", 2, 11, 3, 1);
        Sprite spLetrero         = CortarCelda("TilesetElement", 0, 2, 1, 1);

        // Naturaleza (TilesetNature)
        Sprite spArbolCerezo     = CortarCelda("TilesetNature", 0, 18, 3, 3);
        Sprite spArbolVerde      = CortarCelda("TilesetNature", 3, 18, 3, 3);
        Sprite spArbolDorado     = CortarCelda("TilesetNature", 9, 18, 3, 3);
        Sprite spPinoGrande      = CortarCelda("TilesetNature", 0, 2, 4, 3);

        // Sprites de cofres divididos en Frame 0 (Cerrado) y Frame 1 (Abierto) para evitar doble cofre
        CargarCofreSprites("LittleTreasureChest", out Sprite spArconCerrado, out Sprite spArconAbierto);
        CargarCofreSprites("BigTreasureChest", out Sprite spCofreCerrado, out Sprite spCofreAbierto);

        // Sprites de ítems completos para exhibición y trueque
        Sprite spLingoteCobre    = CargarItemSprite("BarCopper");
        Sprite spLingoteTemplado = CargarItemSprite("BarMithril");
        Sprite spLingoteOro      = CargarItemSprite("BarGold");
        Sprite spLingoteHierro   = CargarItemSprite("BarIron");
        Sprite spEsmeralda       = CargarItemSprite("GemGreen");
        Sprite spGemaPurpura     = CargarItemSprite("GemPurple");
        Sprite spGemaRoja        = CargarItemSprite("GemRed");
        Sprite spKunai           = CargarItemSprite("Kunai");

        // 2. SUELO CONTINUO DE 77x77 UNIDADES (CERO VACÍOS NEGROS + VARIACIÓN COLONIAL)
        GameObject pisoRoot = new GameObject("Piso_Mercado_Colonial");
        pisoRoot.transform.SetParent(root.transform);

        for (int x = -38; x <= 38; x++)
        {
            for (int y = -38; y <= 38; y++)
            {
                bool enCallejonSur   = (y <= -15 && Mathf.Abs(x) <= 4);
                bool enCallejonNorte = (y >= 15 && Mathf.Abs(x) <= 4);
                bool enCallejonEste  = (x >= 15 && Mathf.Abs(y) <= 4);
                bool enCallejonOeste = (x <= -15 && Mathf.Abs(y) <= 4);
                bool enPlazaCentral  = (Mathf.Abs(x) <= 16 && Mathf.Abs(y) <= 15);

                bool esAvenidaCrucero = (Mathf.Abs(x) <= 2 && Mathf.Abs(y) <= 15) || (Mathf.Abs(y) <= 2 && Mathf.Abs(x) <= 16);
                bool esAnilloAdoquin  = enPlazaCentral && (Mathf.Abs(x) >= 14 || Mathf.Abs(y) >= 13);
                bool esDaisCentral    = (Mathf.Abs(x) <= 4 && Mathf.Abs(y) <= 3);

                // Alfombras ceremoniales frente a los 4 puestos de artesanos
                bool esAlfombraNO = (x >= -15 && x <= -11 && y >= 8 && y <= 10);
                bool esAlfombraNE = (x >= 11 && x <= 15 && y >= 8 && y <= 10);
                bool esAlfombraSO = (x >= -15 && x <= -11 && y >= -10 && y <= -8);
                bool esAlfombraSE = (x >= 11 && x <= 15 && y >= -10 && y <= -8);

                GameObject tile = new GameObject($"Tile_{x}_{y}");
                tile.transform.SetParent(pisoRoot.transform);
                tile.transform.position = new Vector3(x, y, 0f);
                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                sr.sortingOrder = -500;

                if (esDaisCentral)
                {
                    sr.sprite = (Mathf.Abs(x) == 4 || Mathf.Abs(y) == 3) ? spAlfombraOro : spAlfombraRoja;
                    sr.sortingOrder = -480;
                }
                else if (esAlfombraNO || esAlfombraNE || esAlfombraSO || esAlfombraSE)
                {
                    sr.sprite = ((x + y) % 2 == 0) ? spAlfombraRoja : spAlfombraOro;
                    sr.sortingOrder = -485;
                }
                else if (enCallejonSur || enCallejonNorte || enCallejonEste || enCallejonOeste || esAvenidaCrucero)
                {
                    sr.sprite = spCaminoPiedra;
                }
                else if (esAnilloAdoquin)
                {
                    sr.sprite = spAdoquinReal != null ? spAdoquinReal : spCaminoPiedra;
                }
                else if (enPlazaCentral)
                {
                    sr.sprite = spPlazaCantera;
                    // Detalles de desgaste de cantera para romper cualquier repetición visual
                    if (((x * 17 + y * 31) % 7 == 0) && spDetallePiedra1 != null)
                    {
                        GameObject det = new GameObject($"Detalle_{x}_{y}");
                        det.transform.SetParent(pisoRoot.transform);
                        det.transform.position = new Vector3(x, y, 0f);
                        SpriteRenderer srDet = det.AddComponent<SpriteRenderer>();
                        srDet.sprite = ((x + y) % 2 == 0) ? spDetallePiedra1 : spDetallePiedra2;
                        srDet.sortingOrder = -492;
                    }
                }
                else
                {
                    // Jardines y solares detrás de las manzanas coloniales (nunca fondo negro)
                    sr.sprite = ((x * 11 + y * 23) % 4 == 0 && spPastoClaro != null) ? spPastoClaro : spPastoBase;
                }
            }
        }

        // 3. MANZANAS DE CASONAS COLONIALES Y MURALLAS PERIMETRALES (CERO BORDES VACÍOS)
        GameObject manzanasRoot = new GameObject("Manzanas_Coloniales_Y_Murallas");
        manzanasRoot.transform.SetParent(root.transform);

        // Fachadas coloniales Norte de la Plaza (Oeste y Este del Callejón Norte)
        ColocarDecoracion("Casona_Norte_NO_1", spCasaAlta2Pisos, new Vector2(-13.5f, 17.5f), new Vector2(3.8f, 3.2f), true, manzanasRoot.transform);
        ColocarDecoracion("Casona_Norte_NO_2", spCasaTejaRoja,   new Vector2(-9.5f,  16.8f), new Vector2(3.8f, 2.4f), true, manzanasRoot.transform);
        ColocarDecoracion("Casona_Norte_NO_3", spCasaAdobe,      new Vector2(-6.0f,  16.8f), new Vector2(3.2f, 2.4f), true, manzanasRoot.transform);

        ColocarDecoracion("Casona_Norte_NE_1", spCasaAdobe,      new Vector2(6.0f,   16.8f), new Vector2(3.2f, 2.4f), true, manzanasRoot.transform);
        ColocarDecoracion("Casona_Norte_NE_2", spCasaTejaOscura, new Vector2(9.5f,   16.8f), new Vector2(3.8f, 2.4f), true, manzanasRoot.transform);
        ColocarDecoracion("Casona_Norte_NE_3", spCasaAltaPiedra, new Vector2(13.5f,  17.5f), new Vector2(3.8f, 3.2f), true, manzanasRoot.transform);

        // Fachadas coloniales Sur de la Plaza (Oeste y Este del Callejón Sur)
        ColocarDecoracion("Casona_Sur_SO_1",   spEdificio2Pisos, new Vector2(-13.5f, -16.5f), new Vector2(3.8f, 2.8f), true, manzanasRoot.transform);
        ColocarDecoracion("Casona_Sur_SO_2",   spCasaTejaRoja,   new Vector2(-9.5f,  -16.5f), new Vector2(3.8f, 2.4f), true, manzanasRoot.transform);
        ColocarDecoracion("Casona_Sur_SO_3",   spCasaVerdeMadera,new Vector2(-6.0f,  -16.5f), new Vector2(3.2f, 2.4f), true, manzanasRoot.transform);

        ColocarDecoracion("Casona_Sur_SE_1",   spCasaBlancaAncha,new Vector2(6.0f,   -16.5f), new Vector2(3.2f, 2.4f), true, manzanasRoot.transform);
        ColocarDecoracion("Casona_Sur_SE_2",   spCasaPaja,       new Vector2(9.5f,   -16.5f), new Vector2(3.8f, 2.4f), true, manzanasRoot.transform);
        ColocarDecoracion("Casona_Sur_SE_3",   spEdificio2Pisos, new Vector2(13.5f,  -16.5f), new Vector2(3.8f, 2.8f), true, manzanasRoot.transform);

        // Manzanas laterales Oeste y Este de la Plaza (Y = -13..+13)
        float[] yManzanas = { -12.0f, -8.0f, 8.0f, 12.0f };
        for (int i = 0; i < yManzanas.Length; i++)
        {
            float ym = yManzanas[i];
            Sprite spOeste = (i % 2 == 0) ? spCasaTejaRoja : spCasaAdobe;
            Sprite spEste  = (i % 2 == 0) ? spCasaTejaOscura : spCasaVerdeMadera;
            ColocarDecoracion($"Casona_Borde_Oeste_{i}", spOeste, new Vector2(-18.0f, ym), new Vector2(3.6f, 2.8f), true, manzanasRoot.transform);
            ColocarDecoracion($"Casona_Borde_Este_{i}",  spEste,  new Vector2(18.0f,  ym), new Vector2(3.6f, 2.8f), true, manzanasRoot.transform);
        }

        // Flancos tupidos de los 4 Callejones (Casas coloniales, muros de almenas, faroles y árboles)
        // Callejón Sur (Y: -36..-18) y Callejón Norte (Y: 18..36)
        for (int y = -35; y <= -19; y += 4)
        {
            Sprite spIzq = (Mathf.Abs(y) % 8 == 0) ? spCasaTejaRoja : spCasaAdobe;
            Sprite spDer = (Mathf.Abs(y) % 8 == 0) ? spCasaTejaOscura : spCasaPaja;
            ColocarDecoracion($"Callejon_Sur_Casa_Izq_{y}", spIzq, new Vector2(-6.8f, y), new Vector2(3.6f, 3.0f), true, manzanasRoot.transform);
            ColocarDecoracion($"Callejon_Sur_Casa_Der_{y}", spDer, new Vector2(6.8f,  y), new Vector2(3.6f, 3.0f), true, manzanasRoot.transform);
            ColocarDecoracion($"Callejon_Sur_Arbol_Izq_{y}", spArbolVerde, new Vector2(-11.0f, y), Vector2.zero, false, manzanasRoot.transform);
            ColocarDecoracion($"Callejon_Sur_Arbol_Der_{y}", spArbolDorado, new Vector2(11.0f, y), Vector2.zero, false, manzanasRoot.transform);
            ColocarDecoracion($"Farol_Callejon_Sur_Izq_{y}", spFarolHierro, new Vector2(-3.8f, y), new Vector2(0.6f, 0.8f), true, manzanasRoot.transform);
            ColocarDecoracion($"Farol_Callejon_Sur_Der_{y}", spFarolHierro, new Vector2(3.8f,  y), new Vector2(0.6f, 0.8f), true, manzanasRoot.transform);
        }

        for (int y = 19; y <= 35; y += 4)
        {
            Sprite spIzq = (y % 8 == 0) ? spCasaAltaPiedra : spCasaTejaRoja;
            Sprite spDer = (y % 8 == 0) ? spCasaAlta2Pisos : spCasaAdobe;
            ColocarDecoracion($"Callejon_Norte_Casa_Izq_{y}", spIzq, new Vector2(-6.8f, y), new Vector2(3.6f, 3.0f), true, manzanasRoot.transform);
            ColocarDecoracion($"Callejon_Norte_Casa_Der_{y}", spDer, new Vector2(6.8f,  y), new Vector2(3.6f, 3.0f), true, manzanasRoot.transform);
            ColocarDecoracion($"Callejon_Norte_Pino_Izq_{y}", spPinoGrande, new Vector2(-11.0f, y), Vector2.zero, false, manzanasRoot.transform);
            ColocarDecoracion($"Callejon_Norte_Pino_Der_{y}", spPinoGrande, new Vector2(11.0f,  y), Vector2.zero, false, manzanasRoot.transform);
            ColocarDecoracion($"Farol_Callejon_Norte_Izq_{y}", spFarolHierro, new Vector2(-3.8f, y), new Vector2(0.6f, 0.8f), true, manzanasRoot.transform);
            ColocarDecoracion($"Farol_Callejon_Norte_Der_{y}", spFarolHierro, new Vector2(3.8f,  y), new Vector2(0.6f, 0.8f), true, manzanasRoot.transform);
        }

        // Callejón Este (X: 19..35) y Callejón Oeste (X: -35..-19)
        for (int x = 19; x <= 35; x += 4)
        {
            ColocarDecoracion($"Callejon_Este_Casa_Norte_{x}", spCasaTejaRoja, new Vector2(x, 6.2f),  new Vector2(3.6f, 2.6f), true, manzanasRoot.transform);
            ColocarDecoracion($"Callejon_Este_Casa_Sur_{x}",   spCasaAdobe,    new Vector2(x, -6.2f), new Vector2(3.6f, 2.6f), true, manzanasRoot.transform);
            ColocarDecoracion($"Farol_Callejon_Este_{x}",      spFarolHierro,  new Vector2(x, 3.6f),  new Vector2(0.6f, 0.8f), true, manzanasRoot.transform);
        }
        for (int x = -35; x <= -19; x += 4)
        {
            ColocarDecoracion($"Callejon_Oeste_Casa_Norte_{x}", spCasaTejaOscura, new Vector2(x, 6.2f),  new Vector2(3.6f, 2.6f), true, manzanasRoot.transform);
            ColocarDecoracion($"Callejon_Oeste_Casa_Sur_{x}",   spCasaVerdeMadera,new Vector2(x, -6.2f), new Vector2(3.6f, 2.6f), true, manzanasRoot.transform);
            ColocarDecoracion($"Farol_Callejon_Oeste_{x}",      spFarolHierro,    new Vector2(x, 3.6f),  new Vector2(0.6f, 0.8f), true, manzanasRoot.transform);
        }

        // Cierres monumentales en los extremos de los callejones y colisionadores sólidos perimetrales
        ColocarDecoracion("Muralla_Cierre_Sur",   spMuroAlmenas, new Vector2(0f, -36.5f),   new Vector2(12f, 3f), true, manzanasRoot.transform);
        ColocarDecoracion("Muralla_Cierre_Norte", spMuroAlmenas, new Vector2(0f,  36.5f),   new Vector2(12f, 3f), true, manzanasRoot.transform);
        ColocarDecoracion("Muralla_Cierre_Oeste", spCasaAlta2Pisos, new Vector2(-36.5f, 0f), new Vector2(3.8f, 10f), true, manzanasRoot.transform);
        ColocarDecoracion("Muralla_Cierre_Este",  spCasaAltaPiedra, new Vector2(36.5f,  0f), new Vector2(3.8f, 10f), true, manzanasRoot.transform);

        // Colisionadores invisibles continuos sin huecos para blindar los muros de la plaza y callejones
        CrearMuroColision("Col_Plaza_Norte_Izq", new Vector2(-10.5f, 15.5f), new Vector2(13f, 2.0f), manzanasRoot.transform);
        CrearMuroColision("Col_Plaza_Norte_Der", new Vector2(10.5f,  15.5f), new Vector2(13f, 2.0f), manzanasRoot.transform);
        CrearMuroColision("Col_Plaza_Sur_Izq",   new Vector2(-10.5f, -15.5f),new Vector2(13f, 2.0f), manzanasRoot.transform);
        CrearMuroColision("Col_Plaza_Sur_Der",   new Vector2(10.5f,  -15.5f),new Vector2(13f, 2.0f), manzanasRoot.transform);
        CrearMuroColision("Col_Plaza_Oeste_Sup", new Vector2(-16.2f, 10.0f), new Vector2(2.0f, 12.5f), manzanasRoot.transform);
        CrearMuroColision("Col_Plaza_Oeste_Inf", new Vector2(-16.2f, -10.0f),new Vector2(2.0f, 12.5f), manzanasRoot.transform);
        CrearMuroColision("Col_Plaza_Este_Sup",  new Vector2(16.2f,  10.0f), new Vector2(2.0f, 12.5f), manzanasRoot.transform);
        CrearMuroColision("Col_Plaza_Este_Inf",  new Vector2(16.2f,  -10.0f),new Vector2(2.0f, 12.5f), manzanasRoot.transform);

        CrearMuroColision("Col_Callejon_Sur_Izq",   new Vector2(-4.8f, -26.5f), new Vector2(1.6f, 24f), manzanasRoot.transform);
        CrearMuroColision("Col_Callejon_Sur_Der",   new Vector2(4.8f,  -26.5f), new Vector2(1.6f, 24f), manzanasRoot.transform);
        CrearMuroColision("Col_Callejon_Norte_Izq", new Vector2(-4.8f, 26.5f),  new Vector2(1.6f, 24f), manzanasRoot.transform);
        CrearMuroColision("Col_Callejon_Norte_Der", new Vector2(4.8f,  26.5f),  new Vector2(1.6f, 24f), manzanasRoot.transform);
        CrearMuroColision("Col_Callejon_Este_Sup",  new Vector2(26.5f,  4.8f),  new Vector2(24f, 1.6f), manzanasRoot.transform);
        CrearMuroColision("Col_Callejon_Este_Inf",  new Vector2(26.5f, -4.8f),  new Vector2(24f, 1.6f), manzanasRoot.transform);
        CrearMuroColision("Col_Callejon_Oeste_Sup", new Vector2(-26.5f, 4.8f),  new Vector2(24f, 1.6f), manzanasRoot.transform);
        CrearMuroColision("Col_Callejon_Oeste_Inf", new Vector2(-26.5f,-4.8f),  new Vector2(24f, 1.6f), manzanasRoot.transform);

        // Colisionadores de cierre en los extremos de los 4 callejones (antes de llegar al borde del mapa)
        CrearMuroColision("Col_Cierre_Callejon_Sur",   new Vector2(0f, -35.8f),   new Vector2(14f, 2.0f), manzanasRoot.transform);
        CrearMuroColision("Col_Cierre_Callejon_Norte", new Vector2(0f,  35.8f),   new Vector2(14f, 2.0f), manzanasRoot.transform);
        CrearMuroColision("Col_Cierre_Callejon_Oeste", new Vector2(-35.8f, 0f),   new Vector2(2.0f, 14f), manzanasRoot.transform);
        CrearMuroColision("Col_Cierre_Callejon_Este",  new Vector2(35.8f,  0f),   new Vector2(2.0f, 14f), manzanasRoot.transform);

        // 4 Muros Colisionadores Invisibles Perimetrales (Evitar salirse al vacío verde/negro exterior)
        CrearMuroColision("Muro_Perimetral_Norte", new Vector2(0f,  39f), new Vector2(85f, 2f), manzanasRoot.transform);
        CrearMuroColision("Muro_Perimetral_Sur",   new Vector2(0f, -39f), new Vector2(85f, 2f), manzanasRoot.transform);
        CrearMuroColision("Muro_Perimetral_Este",  new Vector2(39f,  0f), new Vector2(2f, 85f), manzanasRoot.transform);
        CrearMuroColision("Muro_Perimetral_Oeste", new Vector2(-39f, 0f), new Vector2(2f, 85f), manzanasRoot.transform);

        // Arcos monumentales de cantera en los 4 accesos de la Plaza
        ColocarDecoracion("Arco_Cantera_Norte", spArcoPiedra, new Vector2(0f, 15.2f),   Vector2.zero, false, root.transform);
        ColocarDecoracion("Arco_Cantera_Sur",   spArcoPiedra, new Vector2(0f, -14.5f),  Vector2.zero, false, root.transform);
        ColocarDecoracion("Arco_Cantera_Este",  spArcoPiedra, new Vector2(15.5f, 0f),   Vector2.zero, false, root.transform);
        ColocarDecoracion("Arco_Cantera_Oeste", spArcoPiedra, new Vector2(-15.5f, 0f),  Vector2.zero, false, root.transform);

        // Árboles ornamentales en las esquinas de la plaza
        ColocarDecoracion("Cerezo_Plaza_NO", spArbolCerezo, new Vector2(-8.5f, 12.5f), new Vector2(1.5f, 1.4f), true, root.transform);
        ColocarDecoracion("Cerezo_Plaza_NE", spArbolCerezo, new Vector2(8.5f,  12.5f), new Vector2(1.5f, 1.4f), true, root.transform);
        ColocarDecoracion("ArbolDorado_SO",  spArbolDorado, new Vector2(-8.5f, -12.5f),new Vector2(1.5f, 1.4f), true, root.transform);
        ColocarDecoracion("ArbolDorado_SE",  spArbolDorado, new Vector2(8.5f,  -12.5f),new Vector2(1.5f, 1.4f), true, root.transform);

        // 4. CUATRO PABELLONES ARTESANALES CON ARQUITECTURA COLONIAL REAL Y NPCS FÍSICOS VISIBLES
        // ---------------------------------------------------------------------------------------
        // A) PABELLÓN NOROESTE (-13, 10): HERRERÍA DE CANTERA — DON BRAULIO
        Vector2 posHerreria = new Vector2(-13.0f, 10.0f);
        ColocarDecoracion("Taller_Herreria_Casona", spCasaTejaOscura,  posHerreria + new Vector2(0f, 2.8f),     new Vector2(3.6f, 2.2f), true, root.transform);
        ColocarDecoracion("Fragua_Horno_Piedra",    spHornoPiedra,     posHerreria + new Vector2(-2.6f, 1.4f),  new Vector2(1.6f, 1.4f), true, root.transform);
        ColocarDecoracion("Chimenea_Fragua",        spChimeneaAlta,    posHerreria + new Vector2(2.6f, 2.0f),   new Vector2(1.4f, 1.8f), true, root.transform);
        ColocarDecoracion("Puesto_Toldo_Herreria",  spPuestoRojo,      posHerreria + new Vector2(0f, 1.1f),     new Vector2(2.2f, 1.4f), true, root.transform);
        ColocarDecoracion("Mostrador_Herreria",     spMostradorMadera, posHerreria + new Vector2(0f, -0.4f),    new Vector2(2.8f, 0.8f), true, root.transform);
        ColocarDecoracion("Exhibicion_Armas_1",     spKunai,           posHerreria + new Vector2(-0.9f, -0.25f),Vector2.zero, false, root.transform);
        ColocarDecoracion("Exhibicion_Hierro",      spLingoteHierro,   posHerreria + new Vector2(0.9f, -0.25f), Vector2.zero, false, root.transform);
        ColocarDecoracion("Cajas_Herreria",         spCajasApiladas,   posHerreria + new Vector2(-2.5f, -0.3f), new Vector2(1.4f, 1.2f), true, root.transform);
        ColocarDecoracion("Farol_Herreria",         spFarolHierro,     posHerreria + new Vector2(2.4f, 0.2f),   new Vector2(0.6f, 0.8f), true, root.transform);

        // NPC Físico: Don Braulio, el Maestro Herrero
        GameObject npcBraulio = ParroquiaExteriorBuilder.CrearNPCConSpriteExacto(
            "NPC_Herrero_DonBraulio",
            "Orc-Soldier-Cyan",
            0, 0, 32, 32, 20f,
            posHerreria + new Vector2(0f, 0.35f),
            root.transform);
        SpriteRenderer srBraulio = npcBraulio.GetComponent<SpriteRenderer>();
        if (srBraulio != null) srBraulio.color = new Color(1f, 0.92f, 0.82f, 1f);

        // B) PABELLÓN NORESTE (13, 10): ALFARERÍA TRADICIONAL — DOÑA REMEDIOS
        Vector2 posAlfareria = new Vector2(13.0f, 10.0f);
        ColocarDecoracion("Taller_Alfareria_Casona", spCasonaPanaderia, posAlfareria + new Vector2(0f, 2.8f),    new Vector2(3.2f, 2.2f), true, root.transform);
        ColocarDecoracion("Horno_Barro_Alfareria",   spHornoPiedra,     posAlfareria + new Vector2(2.6f, 1.4f),  new Vector2(1.6f, 1.4f), true, root.transform);
        ColocarDecoracion("Puesto_Toldo_Alfareria",  spPuestoAmarillo,  posAlfareria + new Vector2(0f, 1.1f),    new Vector2(2.2f, 1.4f), true, root.transform);
        ColocarDecoracion("Mostrador_Alfareria",     spMostradorMadera, posAlfareria + new Vector2(0f, -0.4f),   new Vector2(2.8f, 0.8f), true, root.transform);
        ColocarDecoracion("Estante_Vasijas_1",       spVasijas,         posAlfareria + new Vector2(-2.2f, 0.2f), new Vector2(1.4f, 0.9f), true, root.transform);
        ColocarDecoracion("Estante_Vasijas_2",       spVasijas,         posAlfareria + new Vector2(2.2f, 0.2f),  new Vector2(1.4f, 0.9f), true, root.transform);
        ColocarDecoracion("Platos_Barro_Mesa",       spVasijas,         posAlfareria + new Vector2(-0.85f, -0.25f), Vector2.zero, false, root.transform);
        ColocarDecoracion("Farol_Alfareria",         spFarolHierro,     posAlfareria + new Vector2(-2.4f, 1.2f), new Vector2(0.6f, 0.8f), true, root.transform);

        // NPC Físico: Doña Remedios, Maestra Alfarera
        ParroquiaExteriorBuilder.CrearNPCConSpriteExacto(
            "NPC_Alfarera_DonaRemedios",
            "Mage-Red",
            0, 0, 32, 32, 20f,
            posAlfareria + new Vector2(0f, 0.35f),
            root.transform);

        // C) PABELLÓN SUROESTE (-13, -9): JOYERÍA DEL CALLEJÓN — DON TEODORO
        Vector2 posJoyeria = new Vector2(-13.0f, -9.0f);
        ColocarDecoracion("Taller_Joyeria_Casona",   spCasaBlancaAncha, posJoyeria + new Vector2(0f, 2.8f),     new Vector2(3.2f, 2.2f), true, root.transform);
        ColocarDecoracion("Toldo_Azul_Joyeria",      spToldoAzul,       posJoyeria + new Vector2(0f, 1.1f),     new Vector2(2.2f, 1.4f), true, root.transform);
        ColocarDecoracion("Vitrina_Joyeria",         spMostradorMadera, posJoyeria + new Vector2(0f, -0.4f),    new Vector2(2.8f, 0.8f), true, root.transform);
        ColocarDecoracion("Exhibicion_Gema_Purpura", spGemaPurpura,     posJoyeria + new Vector2(-0.9f, -0.25f),Vector2.zero, false, root.transform);
        ColocarDecoracion("Exhibicion_Gema_Roja",    spGemaRoja,        posJoyeria + new Vector2(0.9f, -0.25f), Vector2.zero, false, root.transform);
        ColocarDecoracion("Exhibicion_Lingote_Oro",  spLingoteOro,      posJoyeria + new Vector2(0f, -0.15f),   Vector2.zero, false, root.transform);
        ColocarDecoracion("Barriles_Joyeria",        spBarriles,        posJoyeria + new Vector2(-2.3f, 0f),    new Vector2(1.3f, 1.2f), true, root.transform);
        ColocarDecoracion("Farol_Joyeria",           spFarolHierro,     posJoyeria + new Vector2(2.3f, 0.4f),   new Vector2(0.6f, 0.8f), true, root.transform);

        // NPC Físico: Don Teodoro, el Joyero Sabio
        ParroquiaExteriorBuilder.CrearNPCConSpriteExacto(
            "NPC_Joyero_DonTeodoro",
            "Mage-Cyan",
            0, 0, 32, 32, 20f,
            posJoyeria + new Vector2(0f, 0.35f),
            root.transform);

        // D) PABELLÓN SURESTE (13, -9): PANADERÍA Y ALMACÉN DE ESPECIAS — DOÑA LUCÍA
        Vector2 posAlmacen = new Vector2(13.0f, -9.0f);
        ColocarDecoracion("Casona_Almacen_Especias", spCasaVerdeMadera, posAlmacen + new Vector2(0f, 2.8f),    new Vector2(3.6f, 2.2f), true, root.transform);
        ColocarDecoracion("Tienda_Especias",         spTiendaMercader,  posAlmacen + new Vector2(-2.2f, 1.1f), new Vector2(2.4f, 1.8f), true, root.transform);
        ColocarDecoracion("Carreta_Toldo_Almacen",   spCarretaToldo,    posAlmacen + new Vector2(2.2f, 0.8f),  new Vector2(1.8f, 1.4f), true, root.transform);
        ColocarDecoracion("Mostrador_Panaderia",     spMostradorMadera, posAlmacen + new Vector2(0f, -0.4f),   new Vector2(2.8f, 0.8f), true, root.transform);
        ColocarDecoracion("Barriles_Almacen",        spBarriles,        posAlmacen + new Vector2(-2.2f, -0.6f),new Vector2(1.3f, 1.2f), true, root.transform);
        ColocarDecoracion("Cajas_Almacen",           spCajasApiladas,   posAlmacen + new Vector2(2.2f, -0.6f), new Vector2(1.4f, 1.2f), true, root.transform);

        // 5. HABITANTES Y VENDEDORES SECUNDARIOS CON DIÁLOGOS RICOS (AMBIENTACIÓN VIVA)
        // Aldeano 1: Doña Lucía, Vendedora de Pan y Frutas (Pabellón Sureste)
        GameObject npcLucia = ParroquiaExteriorBuilder.CrearNPCConSpriteExacto(
            "NPC_Vendedora_DonaLucia",
            "Character-Base",
            0, 0, 32, 32, 20f,
            posAlmacen + new Vector2(0f, 0.35f),
            root.transform);
        SpriteRenderer srLucia = npcLucia.GetComponent<SpriteRenderer>();
        if (srLucia != null) srLucia.color = new Color(1f, 0.88f, 0.78f, 1f);
        InteractableSignOrNPC diagLucia = npcLucia.AddComponent<InteractableSignOrNPC>();
        diagLucia.speakerTitle = "Doña Lucía, Vendedora de Pan y Especias";
        diagLucia.interactionPrompt = "[E] Hablar con Doña Lucía";
        diagLucia.dialoguePages = new string[]
        {
            "«¡Bendito sea el cielo, Ronan! Desde que cayó la niebla, los mercaderes cerraron el Cofre Sagrado del centro por temor a los saqueadores.»",
            "«Si buscas abrirlo, empieza en el Arcón de la Antigua Fragua en el Callejón Sur (0, -18). Ahí quedó resguardado el último Lingote de Cobre.»",
            "«Llévaselo a Don Braulio en la Herrería del Noroeste; entre él, Doña Remedios y Don Teodoro forjarán la Llave Dorada del Gremio.»"
        };

        // Aldeano 2: Sargento Martín, Guardia del Gremio (Patrullando junto a la Glorieta Central)
        GameObject npcMartin = ParroquiaExteriorBuilder.CrearNPCConSpriteExacto(
            "NPC_Guardia_SargentoMartin",
            "Human-Soldier-Cyan",
            0, 0, 32, 32, 20f,
            new Vector2(-5.2f, -3.5f),
            root.transform);
        InteractableSignOrNPC diagMartin = npcMartin.AddComponent<InteractableSignOrNPC>();
        diagMartin.speakerTitle = "Sargento Martín, Custodio del Mercado";
        diagMartin.interactionPrompt = "[E] Hablar con Sargento Martín";
        diagMartin.dialoguePages = new string[]
        {
            "«¡Firme, Guardián Ronan! El pasaje Norte hacia el Cerro del Fraile está bloqueado por el Gran Portón de Hierro Gremial.»",
            "«Ningún viajero puede ascender a la montaña sagrada sin antes portar el Eco de la Tradición que descansa en el Cofre Sagrado del centro.»",
            "«Recuerda que puedes pulsar [B] en cualquier momento para abrir la Tienda de Suministros si necesitas flechas, maná o nuevas armas.»"
        };

        // Aldeano 3: Mateo el Arriero, Transeúnte del Callejón Sur
        GameObject npcMateo = ParroquiaExteriorBuilder.CrearNPCConSpriteExacto(
            "NPC_Arriero_Mateo",
            "Character-Base",
            0, 0, 32, 32, 20f,
            new Vector2(-2.5f, -16.2f),
            root.transform);
        SpriteRenderer srMateo = npcMateo.GetComponent<SpriteRenderer>();
        if (srMateo != null) srMateo.color = new Color(0.85f, 0.95f, 0.80f, 1f);
        ColocarDecoracion("Carreta_Mateo_Sur", spCarretaMadera, new Vector2(-2.6f, -14.5f), new Vector2(1.8f, 1.3f), true, root.transform);
        InteractableSignOrNPC diagMateo = npcMateo.AddComponent<InteractableSignOrNPC>();
        diagMateo.speakerTitle = "Mateo, Arriero de Torres Mochas";
        diagMateo.interactionPrompt = "[E] Hablar con Mateo el Arriero";
        diagMateo.dialoguePages = new string[]
        {
            "«Mis mulas apenas lograron cruzar el callejón Sur antes de que los goblins pusieran barricadas.»",
            "«Mira tu Mochila en la esquina superior derecha de tu pantalla: ahí verás en todo momento qué objeto de trueque llevas contigo.»"
        };

        // Aldeano 4: Don Prudencio, Cronista del Comercio (Acceso Este)
        GameObject npcPrudencio = ParroquiaExteriorBuilder.CrearNPCConSpriteExacto(
            "NPC_Comerciante_DonPrudencio",
            "Mage-Cyan",
            0, 0, 32, 32, 20f,
            new Vector2(14.0f, 2.4f),
            root.transform);
        InteractableSignOrNPC diagPrudencio = npcPrudencio.AddComponent<InteractableSignOrNPC>();
        diagPrudencio.speakerTitle = "Don Prudencio, Síndico del Gremio";
        diagPrudencio.interactionPrompt = "[E] Hablar con Don Prudencio";
        diagPrudencio.dialoguePages = new string[]
        {
            "«La tradición alfarera y platera de San Felipe es el alma de nuestro pueblo.»",
            "«El orden ancestral del trueque nunca cambia: Cobre en el Sur -> Herrero al Noroeste -> Alfarera al Noreste -> Joyero al Suroeste -> Cofre Central.»"
        };

        // Fuentes de León y Faroles en la Plaza Central
        ColocarDecoracion("Fuente_Leon_Oeste_Plaza", spFuenteLeon, new Vector2(-6.5f, 0f), new Vector2(1.8f, 2.0f), true, root.transform);
        ColocarDecoracion("Fuente_Leon_Este_Plaza",  spFuenteLeon, new Vector2(6.5f,  0f), new Vector2(1.8f, 2.0f), true, root.transform);
        ColocarDecoracion("Farol_NO", spFarolHierro, new Vector2(-5.5f, 5.5f),  new Vector2(0.6f, 0.8f), true, root.transform);
        ColocarDecoracion("Farol_NE", spFarolHierro, new Vector2(5.5f,  5.5f),  new Vector2(0.6f, 0.8f), true, root.transform);
        ColocarDecoracion("Farol_SO", spFarolHierro, new Vector2(-5.5f, -5.5f), new Vector2(0.6f, 0.8f), true, root.transform);
        ColocarDecoracion("Farol_SE", spFarolHierro, new Vector2(5.5f,  -5.5f), new Vector2(0.6f, 0.8f), true, root.transform);

        // 6. SANTUARIO CENTRAL DEL COFRE GREMIAL Y EL SEGUNDO ECO en (0, 0)
        Vector2 posCentroGremio = Vector2.zero;
        ColocarDecoracion("Escalera_Dais_Sur",   spEscalera,       posCentroGremio + new Vector2(0f, -2.2f),    Vector2.zero, false, root.transform, -470);
        ColocarDecoracion("Columna_NO",          spColumna,        posCentroGremio + new Vector2(-3.4f, 2.5f),  new Vector2(0.8f, 1.5f), true, root.transform);
        ColocarDecoracion("Columna_NE",          spColumna,        posCentroGremio + new Vector2(3.4f,  2.5f),  new Vector2(0.8f, 1.5f), true, root.transform);
        ColocarDecoracion("Columna_SO",          spColumna,        posCentroGremio + new Vector2(-3.4f, -2.0f), new Vector2(0.8f, 1.5f), true, root.transform);
        ColocarDecoracion("Columna_SE",          spColumna,        posCentroGremio + new Vector2(3.4f,  -2.0f), new Vector2(0.8f, 1.5f), true, root.transform);
        ColocarDecoracion("Estatua_Gremio_Izq",  spEstatuaDorada,  posCentroGremio + new Vector2(-2.4f, 2.5f),  new Vector2(1.2f, 1.2f), true, root.transform);
        ColocarDecoracion("Estatua_Gremio_Der",  spEstatuaDorada,  posCentroGremio + new Vector2(2.4f,  2.5f),  new Vector2(1.2f, 1.2f), true, root.transform);
        ColocarDecoracion("Estandarte_Oro_Izq",  spEstandarteOro,  posCentroGremio + new Vector2(-1.4f, 2.8f),  Vector2.zero, false, root.transform);
        ColocarDecoracion("Estandarte_Oro_Der",  spEstandarteOro,  posCentroGremio + new Vector2(1.4f,  2.8f),  Vector2.zero, false, root.transform);
        ColocarDecoracion("Estandarte_Rojo_Cen", spEstandarteRojo, posCentroGremio + new Vector2(0f,    3.0f),  Vector2.zero, false, root.transform);

        // Puzzle de Trueque
        MarketTradePuzzle tradePuzzle = root.AddComponent<MarketTradePuzzle>();

        // Segundo Eco: Eco de la Tradición
        GameObject ecoTradicion = new GameObject("Segundo_Eco_De_La_Tradicion");
        ecoTradicion.transform.SetParent(root.transform);
        ecoTradicion.transform.position = posCentroGremio + new Vector2(0f, 1.4f);
        CircleCollider2D colEco = ecoTradicion.AddComponent<CircleCollider2D>();
        colEco.isTrigger = true;
        colEco.radius = 0.65f;
        EchoCollectible echoComp = ecoTradicion.AddComponent<EchoCollectible>();
        echoComp.indiceEco = 2;
        echoComp.nombreEco = "Eco de la Tradición";
        ecoTradicion.SetActive(false);

        // Barrera de sello mágico alrededor del cofre
        GameObject barreraCofre = new GameObject("Sello_Gremial_Cofre");
        barreraCofre.transform.SetParent(root.transform);
        barreraCofre.transform.position = posCentroGremio;
        SpriteRenderer srSello = barreraCofre.AddComponent<SpriteRenderer>();
        srSello.sprite = ParroquiaExteriorBuilder.CrearSpriteRadial(new Color(1f, 0.82f, 0.25f, 0.65f));
        srSello.sortingOrder = 2;
        barreraCofre.transform.localScale = Vector3.one * 2.2f;

        tradePuzzle.ecoDeLaTradicionObjeto = ecoTradicion;
        tradePuzzle.barreraCofreGremio = barreraCofre;

        // 7. NODOS INTERACTIVOS DE TRUEQUE (MARKET STALL TRADE NODES)
        // Nodo 0: Arcón de Suministros de Cobre (Sur, callejón de entrada en (0, -18))
        MarketStallTradeNode nodoArcon = CrearPuestoTrueque(
            "Arcon_Suministros_Cobre",
            spArconCerrado,
            new Vector2(0f, -18.0f),
            MarketStallTradeNode.NodeRole.SupplyCrateCopper,
            "[E] Abrir Arcón de la Fragua",
            tradePuzzle,
            root.transform,
            spArconAbierto);

        // Nodo 1: Herrería de Don Braulio (NW en (-13, 10))
        CrearPuestoTrueque(
            "Puesto_Herrero_DonBraulio",
            spLingoteCobre,
            posHerreria + new Vector2(0f, -0.35f),
            MarketStallTradeNode.NodeRole.BlacksmithStall,
            "[E] Comerciar con Don Braulio (Herrero)",
            tradePuzzle,
            root.transform);

        // Nodo 2: Alfarería de Doña Remedios (NE en (13, 10))
        CrearPuestoTrueque(
            "Puesto_Alfarera_DonaRemedios",
            spLingoteTemplado,
            posAlfareria + new Vector2(0f, -0.35f),
            MarketStallTradeNode.NodeRole.PotterStall,
            "[E] Comerciar con Doña Remedios (Alfarera)",
            tradePuzzle,
            root.transform);

        // Nodo 3: Joyería de Don Teodoro (SW en (-13, -9))
        CrearPuestoTrueque(
            "Puesto_Joyero_DonTeodoro",
            spEsmeralda,
            posJoyeria + new Vector2(0f, -0.35f),
            MarketStallTradeNode.NodeRole.JewelerStall,
            "[E] Comerciar con Don Teodoro (Joyero)",
            tradePuzzle,
            root.transform);

        // Nodo 4: Cofre Sagrado del Gremio (Centro en (0, 0))
        MarketStallTradeNode nodoCofreGremio = CrearPuestoTrueque(
            "Cofre_Sagrado_Del_Gremio",
            spCofreCerrado,
            posCentroGremio,
            MarketStallTradeNode.NodeRole.GuildTreasureChest,
            "[E] Abrir Cofre Sagrado del Gremio",
            tradePuzzle,
            root.transform,
            spCofreAbierto);
        if (nodoCofreGremio != null)
        {
            tradePuzzle.cofreGremioRenderer = nodoCofreGremio.chestRenderer;
            tradePuzzle.spriteCofreGremioAbierto = spCofreAbierto;
        }

        // Tablón de pistas del Gremio (en el callejón sur junto al arcón)
        GameObject tablon = ColocarDecoracion("Tablon_Gremio_Artesanos", spLetrero, new Vector2(2.6f, -17.5f), new Vector2(0.9f, 0.9f), true, root.transform);
        InteractableSignOrNPC signGremio = tablon.AddComponent<InteractableSignOrNPC>();
        signGremio.speakerTitle = "Tablón del Gremio de Artesanos de San Felipe";
        signGremio.interactionPrompt = "[E] Leer Orden del Trueque";
        signGremio.dialoguePages = new string[]
        {
            "«ORDEN TRADICIONAL DEL TRUEQUE DE SAN FELIPE:»",
            "«1° Toma el Lingote de Cobre del Arcón de Suministros al Sur (0, -18).»",
            "«2° Cámbialo con Don Braulio (Herrería Noroeste, -13, 10) por el Lingote Templado.»",
            "«3° Lleva el Lingote Templado a Doña Remedios (Alfarería Noreste, 13, 10) por la Esmeralda de San Felipe.»",
            "«4° Entrega la Esmeralda a Don Teodoro (Joyería Suroeste, -13, -9) para forjar la Llave Dorada del Gremio.»",
            "«5° Inserta la Llave Dorada en el Cofre Sagrado del centro (0, 0) para liberar el Segundo Eco: Eco de la Tradición.»"
        };

        // 8. SISTEMA DE COMBATE Y HORDAS POR ZONA (3 ENCUENTROS CON RADIO 5.5f Y EXCLUSIÓN MUTUA)
        // A) Horda 1: Callejones de Entrada al Mercado (Sur en (0, -25))
        List<ZoneWaveSpawner.WaveDefinition> horda1Oleadas = new List<ZoneWaveSpawner.WaveDefinition>();
        ZoneWaveSpawner.WaveDefinition h1w1 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Saqueadores del Callejón Sur" };
        h1w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.GoblinRaider, spawnPosition = new Vector2(-2.0f, -24.5f) });
        h1w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.GoblinRaider, spawnPosition = new Vector2(2.0f, -24.5f) });
        h1w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.GoblinBomber, spawnPosition = new Vector2(0f, -27.0f) });
        horda1Oleadas.Add(h1w1);

        ZoneWaveSpawner.WaveDefinition h1w2 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Emboscada de Pólvora y Acero" };
        h1w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.GoblinBomber, spawnPosition = new Vector2(-2.2f, -23.5f) });
        h1w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.GoblinBomber, spawnPosition = new Vector2(2.2f, -23.5f) });
        h1w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.CorruptedGuard, spawnPosition = new Vector2(0f, -22.5f) });
        horda1Oleadas.Add(h1w2);

        CrearZonaHorda("Horda_Entrada_Callejones", "Callejón de Entrada al Mercado", new Vector2(0f, -25.0f), 5.5f, horda1Oleadas, root.transform);

        // B) Horda 2: Pasaje de Artesanos (Norte en (0, 10))
        List<ZoneWaveSpawner.WaveDefinition> horda2Oleadas = new List<ZoneWaveSpawner.WaveDefinition>();
        ZoneWaveSpawner.WaveDefinition h2w1 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Niebla en los Puestos de Alfarería" };
        h2w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.MistSlime, spawnPosition = new Vector2(-3.2f, 9.5f) });
        h2w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.MistSlime, spawnPosition = new Vector2(3.2f, 9.5f) });
        h2w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.OblivionMage, spawnPosition = new Vector2(0f, 11.5f) });
        horda2Oleadas.Add(h2w1);

        ZoneWaveSpawner.WaveDefinition h2w2 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Conjuradores del Olvido y Sombras" };
        h2w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.SombraLanzadora, spawnPosition = new Vector2(0f, 12.2f) });
        h2w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.OblivionMage, spawnPosition = new Vector2(-2.8f, 10.0f) });
        h2w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.OblivionMage, spawnPosition = new Vector2(2.8f, 10.0f) });
        horda2Oleadas.Add(h2w2);

        CrearZonaHorda("Horda_Pasaje_Artesanos", "Pasaje de Artesanos del Norte", new Vector2(0f, 10.0f), 5.5f, horda2Oleadas, root.transform);

        // C) Horda 3: Custodios del Cofre Sagrado (Centro de la Plaza en (0, 0))
        List<ZoneWaveSpawner.WaveDefinition> horda3Oleadas = new List<ZoneWaveSpawner.WaveDefinition>();
        ZoneWaveSpawner.WaveDefinition h3w1 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Custodia de la Fragua Olvidada" };
        h3w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.DarkOrcBrute, spawnPosition = new Vector2(0f, -3.0f) });
        h3w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.GoblinBomber, spawnPosition = new Vector2(-3.8f, 0f) });
        h3w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.GoblinBomber, spawnPosition = new Vector2(3.8f, 0f) });
        horda3Oleadas.Add(h3w1);

        ZoneWaveSpawner.WaveDefinition h3w2 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Furia de las Sombras Gremiales" };
        h3w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.DarkOrcBrute, spawnPosition = new Vector2(0f, 3.2f) });
        h3w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.SombraLanzadora, spawnPosition = new Vector2(3.2f, 2.2f) });
        h3w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.GoblinRaider, spawnPosition = new Vector2(-2.8f, -1.0f) });
        h3w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.GoblinRaider, spawnPosition = new Vector2(2.8f, -1.0f) });
        horda3Oleadas.Add(h3w2);

        CrearZonaHorda("Horda_Guardia_Cofre_Sagrado", "Plaza del Santuario Gremial", new Vector2(0f, 0f), 5.5f, horda3Oleadas, root.transform);

        // 9. CORAZONES CURATIVOS (+25 HP) EN PUNTOS ESTRATÉGICOS
        HeartPickup.SpawnHeart(new Vector3(0f, -21.0f, 0f), 25f, root.transform);
        HeartPickup.SpawnHeart(new Vector3(-11.0f, 0f, 0f), 25f, root.transform);
        HeartPickup.SpawnHeart(new Vector3(11.0f, 0f, 0f), 25f, root.transform);
        HeartPickup.SpawnHeart(new Vector3(0f, 13.5f, 0f), 25f, root.transform);

        // 10. PORTALES DE CONEXIÓN Y BLOQUEO ESTRICTO HACIA EL NIVEL 3
        // A) Portal Este: Regreso a la Gran Villa y Atrio de la Parroquia (SampleScene)
        ParroquiaExteriorBuilder.CrearPortalTransicionEscena(
            "Portal_Mercado_A_PlazaPrincipal",
            new Vector2(34.0f, 0.0f),
            new Vector2(1.8f, 3.0f),
            "SampleScene",
            LevelManager.ZoneId.ParroquiaExterior,
            new Vector2(-12.5f, 0.0f),
            new Color(0.95f, 0.75f, 0.30f, 0.85f),
            root.transform);

        // B) Portal Norte: Avance al Nivel 3: El Cerro del Fraile y la Gran Cruz Azul
        GameObject portalCerro = ParroquiaExteriorBuilder.CrearPortalTransicionEscena(
            "Portal_Mercado_A_CerroDelFraile",
            new Vector2(0.0f, 34.5f),
            new Vector2(3.0f, 1.8f),
            "Nivel3_CerroDelFraile",
            LevelManager.ZoneId.CerroDelFraile,
            new Vector2(0.0f, -40.0f),
            new Color(0.20f, 0.85f, 1.0f, 0.85f),
            root.transform);

        ZoneTransition ztCerro = portalCerro.GetComponent<ZoneTransition>();
        if (ztCerro != null)
        {
            ztCerro.ecoRequeridoParaCruzar = 2;
            ztCerro.tituloBloqueoEco = "Gran Portón de Hierro Gremial";
            ztCerro.mensajeBloqueoEco = "El sendero hacia el Cerro del Fraile está sellado por el Gremio de Artesanos. Debes completar la cadena de trueque y recuperar el Eco de la Tradición.";
        }

        // Gran Portón de Hierro Gremial con Sello de Tradición (físicamente sólido hasta tener Eco 2)
        GameObject portonGremial = new GameObject("Porton_Hierro_Gremial_Cerro");
        portonGremial.transform.SetParent(root.transform);
        portonGremial.transform.position = new Vector3(0f, 32.5f, 0f);
        SpriteRenderer srPorton = portonGremial.AddComponent<SpriteRenderer>();
        srPorton.sprite = spRejaParque != null ? spRejaParque : spVallaHierro;
        srPorton.sortingOrder = Mathf.RoundToInt(-32.5f * 10f);
        BoxCollider2D colPorton = portonGremial.AddComponent<BoxCollider2D>();
        colPorton.size = new Vector2(6.5f, 1.6f);
        colPorton.isTrigger = false;

        // Sello dorado luminoso sobre el portón
        GameObject selloPorton = new GameObject("Sello_Tradicion_Porton");
        selloPorton.transform.SetParent(portonGremial.transform);
        selloPorton.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        selloPorton.transform.localScale = new Vector3(2.8f, 1.8f, 1f);
        SpriteRenderer srSelloPorton = selloPorton.AddComponent<SpriteRenderer>();
        srSelloPorton.sprite = ParroquiaExteriorBuilder.CrearSpriteRadial(new Color(1f, 0.78f, 0.22f, 0.82f));
        srSelloPorton.sortingOrder = srPorton.sortingOrder + 2;

        EchoProgressionBarrier progPorton = portonGremial.AddComponent<EchoProgressionBarrier>();
        progPorton.requiredEchoIndex = 2;
        progPorton.barrierTitle = "Gran Portón de Hierro Gremial";
        progPorton.promptLabel = "[E] Portón Gremial (Eco de la Tradición requerido)";
        progPorton.blockedMessage = "El sendero hacia el Cerro del Fraile está sellado por el Gremio de Artesanos. Debes completar la cadena de trueque y recuperar el Eco de la Tradición.";

        if (SaveStateManager.Instance != null && SaveStateManager.Instance.ecoTradicionRecuperado)
        {
            portonGremial.SetActive(false);
        }

        return root;
    }

    private static void CrearMuroColision(string nombre, Vector2 pos, Vector2 size, Transform parent)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = size;
    }

    private static MarketStallTradeNode CrearPuestoTrueque(
        string nombre,
        Sprite spriteItem,
        Vector2 pos,
        MarketStallTradeNode.NodeRole role,
        string prompt,
        MarketTradePuzzle puzzle,
        Transform parent,
        Sprite spriteAbierto = null)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spriteItem;
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10f) + 3;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.1f, 0.55f);
        col.offset = new Vector2(0f, -0.15f);
        col.isTrigger = false;

        MarketStallTradeNode node = go.AddComponent<MarketStallTradeNode>();
        node.role = role;
        node.labelPrompt = prompt;
        node.puzzlePadre = puzzle;
        node.chestRenderer = sr;
        node.spriteCerrado = spriteItem;
        node.spriteAbierto = spriteAbierto;
        return node;
    }

    public static void CargarCofreSprites(string assetName, out Sprite spCerrado, out Sprite spAbierto)
    {
        Texture2D tex = ZoneWaveSpawner.LoadSpriteTexture(assetName);
        if (tex == null)
        {
#if UNITY_EDITOR
            tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Sprites/{assetName}.png");
#endif
        }
        if (tex != null && tex.width >= 32)
        {
            float halfW = tex.width * 0.5f;
            spCerrado = Sprite.Create(tex, new Rect(0f, 0f, halfW, tex.height), new Vector2(0.5f, 0.5f), 16f);
            spAbierto = Sprite.Create(tex, new Rect(halfW, 0f, halfW, tex.height), new Vector2(0.5f, 0.5f), 16f);
            return;
        }
        Sprite fallback = CargarItemSprite(assetName);
        spCerrado = fallback;
        spAbierto = fallback;
    }

    private static GameObject CrearZonaHorda(
        string nombre,
        string tituloZona,
        Vector2 centro,
        float radioActivacion,
        List<ZoneWaveSpawner.WaveDefinition> oleadas,
        Transform parent)
    {
        GameObject spawnerGO = new GameObject(nombre);
        spawnerGO.transform.SetParent(parent);
        spawnerGO.transform.position = centro;

        ZoneWaveSpawner spawner = spawnerGO.AddComponent<ZoneWaveSpawner>();
        spawner.zoneTitle = tituloZona;
        spawner.activationRadius = radioActivacion;
        spawner.waves = oleadas;

        return spawnerGO;
    }

    private static Sprite CortarCelda(string sheetName, int col, int rowTop, int wCells, int hCells)
    {
        return ParroquiaExteriorBuilder.ExtraerSpriteExactoSinCosturas(sheetName, col, rowTop, wCells * 16, hCells * 16, 16f);
    }

    public static Sprite CargarItemSprite(string assetName)
    {
        Texture2D tex = ZoneWaveSpawner.LoadSpriteTexture(assetName);
        if (tex == null)
        {
#if UNITY_EDITOR
            tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Sprites/{assetName}.png");
#endif
        }
        if (tex != null)
        {
            // Si es un cofre de 2 fotogramas horizontales, devolver exclusivamente el fotograma izquierdo (Cerrado)
            if (assetName != null && assetName.Contains("TreasureChest") && tex.width >= 32)
            {
                return Sprite.Create(tex, new Rect(0f, 0f, tex.width * 0.5f, tex.height), new Vector2(0.5f, 0.5f), 16f);
            }
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16f);
        }
        return ParroquiaExteriorBuilder.CrearSpriteRadial(new Color(1f, 0.85f, 0.2f, 1f));
    }

    private static GameObject ColocarDecoracion(string nombre, Sprite sp, Vector2 pos, Vector2 sizeCol, bool conColision, Transform parent, int? customOrder = null)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = customOrder ?? Mathf.RoundToInt(-pos.y * 10f);

        if (conColision && sizeCol.sqrMagnitude > 0.01f)
        {
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(sizeCol.x, sizeCol.y * 0.45f);
            col.offset = new Vector2(0f, -sizeCol.y * 0.25f);
        }
        return go;
    }
}
