using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Constructor y controlador del Nivel 2: El Mercado Municipal y Callejones de San Felipe.
/// Construye una escena modular completa de 45x45 unidades centrada en (0, 0) usando 100% sprites
/// reales extraídos de los tilesets (TilesetFloor, TilesetInteriorFloor, TilesetHouse, TilesetElement,
/// tileset_bed, tileset_camp, TilesetVillageAbandoned).
/// 
/// Incluye:
///  • Explanada artesanal de terracota, pasillos de piedra empedrada y perímetros amurallados.
///  • Cuatro pabellones con toldos y puestos gremiales (Herrero, Alfarera, Joyero y Especias).
///  • Cadena de trueque del Gremio (MarketTradePuzzle + MarketStallTradeNode).
///  • Cofre Sagrado con sprite completo de BigTreasureChest y Segundo Eco: Eco de la Tradición.
///  • 3 encuentros de combate por oleadas (ZoneWaveSpawner) con Goblins, Magos, Orcos y Sombras.
///  • Portales de interconexión con la Plaza Principal (SampleScene) y el Cerro del Fraile (Nivel 3).
///  • Auto-inicialización autónoma (cámara suave ortográfica, singletons y Ronan).
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

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.currentZone = LevelManager.ZoneId.Mercado;
        }

        // 2. Asegurar a Ronan (Player)
        GameObject playerObj = AsegurarPlayer(new Vector2(18.5f, 0f));
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
            mainCam.backgroundColor = new Color(0.12f, 0.09f, 0.07f);
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
            if (GameObject.Find("Marca_Mercado_V2") != null) return existente;
            if (Application.isPlaying) Destroy(existente);
            else DestroyImmediate(existente);
        }

        GameObject root = new GameObject("Zona_Mercado_Municipal_Detallada");
        if (parentRoot != null) root.transform.SetParent(parentRoot);
        GameObject marca = new GameObject("Marca_Mercado_V2");
        marca.transform.SetParent(root.transform);

        // 1. RECORTE DE SPRITES REALES DE LOS TILESETS
        Sprite spTerracota = CortarCelda("TilesetInteriorFloor", 1, 1, 1, 1);
        Sprite spPasilloPiedra = CortarCelda("TilesetFloor", 0, 0, 1, 1);
        Sprite spCentroMosaico = CortarCelda("TilesetInteriorFloor", 1, 13, 1, 1);
        Sprite spBordePlaza = CortarCelda("TilesetInteriorFloor", 13, 1, 1, 1);

        Sprite spMuroPiedra = CortarCelda("TilesetInteriorFloor", 13, 13, 1, 1);
        Sprite spColumna = CortarCelda("TilesetVillageAbandoned", 2, 3, 1, 3);
        Sprite spArco = CortarCelda("TilesetVillageAbandoned", 0, 0, 3, 3);
        Sprite spEscalera = CortarCelda("TilesetHouse", 10, 19, 3, 2);

        Sprite spToldoRojo = CortarCelda("tileset_bed", 0, 6, 2, 3);
        Sprite spToldoVerde = CortarCelda("tileset_bed", 2, 6, 3, 2);
        Sprite spCarpaCamp = CortarCelda("tileset_camp", 0, 0, 3, 3);
        Sprite spMostradorMadera = CortarCelda("tileset_camp", 0, 7, 3, 1);
        Sprite spMesa = CortarCelda("TilesetElement", 0, 3, 2, 1);

        Sprite spBarriles = CortarCelda("TilesetElement", 2, 1, 1, 2);
        Sprite spCajas = CortarCelda("TilesetElement", 0, 1, 2, 2);
        Sprite spVasijas = CortarCelda("TilesetElement", 4, 0, 2, 1);
        Sprite spFarol = CortarCelda("TilesetElement", 8, 2, 1, 2);
        Sprite spEstandarteOro = CortarCelda("TilesetHouse", 28, 20, 1, 2);
        Sprite spEstandarteRojo = CortarCelda("TilesetHouse", 23, 20, 1, 2);

        // Sprites de ítems completos (sin recorte a 16x16)
        Sprite spArconCobre = CargarItemSprite("LittleTreasureChest");
        Sprite spCofreGremio = CargarItemSprite("BigTreasureChest");
        Sprite spLingoteCobre = CargarItemSprite("BarCopper");
        Sprite spLingoteTemplado = CargarItemSprite("BarMithril");
        Sprite spEsmeralda = CargarItemSprite("GemGreen");
        Sprite spLlaveOro = CargarItemSprite("GoldKey");

        // 2. SUELO DEL MERCADO Y CALLEJONES (-22..+22)
        GameObject pisoRoot = new GameObject("Piso_Mercado");
        pisoRoot.transform.SetParent(root.transform);

        for (int x = -22; x <= 22; x++)
        {
            for (int y = -22; y <= 22; y++)
            {
                // Determinar si es zona de callejones o plaza
                bool enCallejonSur = (y <= -14 && Mathf.Abs(x) <= 3);
                bool enCallejonNorte = (y >= 14 && Mathf.Abs(x) <= 3);
                bool enCallejonEste = (x >= 14 && Mathf.Abs(y) <= 3);
                bool enCallejonOeste = (x <= -14 && Mathf.Abs(y) <= 3);
                bool enPlazaCentral = (Mathf.Abs(x) <= 14 && Mathf.Abs(y) <= 14);

                if (!enCallejonSur && !enCallejonNorte && !enCallejonEste && !enCallejonOeste && !enPlazaCentral)
                {
                    continue; // Fuera del recinto del mercado
                }

                GameObject tile = new GameObject($"Tile_{x}_{y}");
                tile.transform.SetParent(pisoRoot.transform);
                tile.transform.position = new Vector3(x, y, 0f);
                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                sr.sortingOrder = -500;

                // Ejes principales y callejones usan piedra empedrada; la plaza usa terracota colonial
                bool esEje = (Mathf.Abs(x) <= 2) || (Mathf.Abs(y) <= 2) || enCallejonSur || enCallejonNorte || enCallejonEste || enCallejonOeste;
                bool esCentroDais = (Mathf.Abs(x) <= 4 && Mathf.Abs(y - 1) <= 3);

                if (esCentroDais)
                {
                    sr.sprite = spCentroMosaico;
                    sr.sortingOrder = -480;
                }
                else if (esEje)
                {
                    sr.sprite = spPasilloPiedra;
                }
                else if (Mathf.Abs(x) == 14 || Mathf.Abs(y) == 14)
                {
                    sr.sprite = spBordePlaza;
                }
                else
                {
                    sr.sprite = spTerracota;
                }
            }
        }

        // 3. MUROS PERIMETRALES Y ENTRADAS ARQUEADAS
        GameObject murosRoot = new GameObject("Muros_Y_Perimetros");
        murosRoot.transform.SetParent(root.transform);

        // Muros exteriores de la plaza
        for (int x = -14; x <= 14; x++)
        {
            // Norte
            if (Mathf.Abs(x) > 3) ColocarMuro(spMuroPiedra, new Vector2(x, 15f), murosRoot.transform);
            // Sur
            if (Mathf.Abs(x) > 3) ColocarMuro(spMuroPiedra, new Vector2(x, -15f), murosRoot.transform);
        }
        for (int y = -14; y <= 14; y++)
        {
            // Este
            if (Mathf.Abs(y) > 3) ColocarMuro(spMuroPiedra, new Vector2(15f, y), murosRoot.transform);
            // Oeste
            if (Mathf.Abs(y) > 3) ColocarMuro(spMuroPiedra, new Vector2(-15f, y), murosRoot.transform);
        }

        // Muros de los callejones
        for (int y = 15; y <= 22; y++)
        {
            ColocarMuro(spMuroPiedra, new Vector2(-4f, y), murosRoot.transform);
            ColocarMuro(spMuroPiedra, new Vector2(4f, y), murosRoot.transform);
        }
        for (int y = -22; y <= -15; y++)
        {
            ColocarMuro(spMuroPiedra, new Vector2(-4f, y), murosRoot.transform);
            ColocarMuro(spMuroPiedra, new Vector2(4f, y), murosRoot.transform);
        }
        for (int x = 15; x <= 22; x++)
        {
            ColocarMuro(spMuroPiedra, new Vector2(x, 4f), murosRoot.transform);
            ColocarMuro(spMuroPiedra, new Vector2(x, -4f), murosRoot.transform);
        }
        for (int x = -22; x <= -15; x++)
        {
            ColocarMuro(spMuroPiedra, new Vector2(x, 4f), murosRoot.transform);
            ColocarMuro(spMuroPiedra, new Vector2(x, -4f), murosRoot.transform);
        }
        // Cierre del callejón Oeste (fondo sin salida)
        for (int y = -3; y <= 3; y++)
        {
            ColocarMuro(spMuroPiedra, new Vector2(-22.5f, y), murosRoot.transform);
        }

        // Arcos monumentales en las 4 entradas
        ColocarDecoracion("Arco_Norte", spArco, new Vector2(0f, 15.5f), new Vector2(3f, 1.2f), false, root.transform);
        ColocarDecoracion("Arco_Sur", spArco, new Vector2(0f, -14.5f), new Vector2(3f, 1.2f), false, root.transform);
        ColocarDecoracion("Arco_Este", spArco, new Vector2(14.5f, 0f), new Vector2(1.2f, 3f), false, root.transform);
        ColocarDecoracion("Arco_Oeste", spArco, new Vector2(-14.5f, 0f), new Vector2(1.2f, 3f), false, root.transform);

        // 4. CUATRO PABELLONES ARTESANALES
        // -------------------------------------------------------------
        // A) PABELLÓN NOROESTE: HERRERÍA DE CANTERA (Don Braulio)
        Vector2 posHerreria = new Vector2(-9.5f, 8.5f);
        ColocarDecoracion("Toldo_Herreria", spToldoRojo, posHerreria + new Vector2(0f, 1.2f), new Vector2(3.2f, 1.8f), true, root.transform);
        ColocarDecoracion("Mostrador_Herreria", spMostradorMadera, posHerreria + new Vector2(0f, -0.4f), new Vector2(2.8f, 0.8f), true, root.transform);
        ColocarDecoracion("Cajas_Herreria", spCajas, posHerreria + new Vector2(-2.2f, 0f), new Vector2(1.5f, 1.5f), true, root.transform);
        ColocarDecoracion("Farol_Herreria", spFarol, posHerreria + new Vector2(2.2f, 1.0f), new Vector2(0.5f, 0.5f), false, root.transform);

        // B) PABELLÓN NORESTE: ALFARERÍA TRADICIONAL (Doña Remedios)
        Vector2 posAlfareria = new Vector2(9.5f, 8.5f);
        ColocarDecoracion("Toldo_Alfareria", spToldoVerde, posAlfareria + new Vector2(0f, 1.2f), new Vector2(3.2f, 1.8f), true, root.transform);
        ColocarDecoracion("Mostrador_Alfareria", spMostradorMadera, posAlfareria + new Vector2(0f, -0.4f), new Vector2(2.8f, 0.8f), true, root.transform);
        ColocarDecoracion("Vasijas_Alfareria_1", spVasijas, posAlfareria + new Vector2(-2.0f, 0.2f), new Vector2(1.2f, 0.8f), true, root.transform);
        ColocarDecoracion("Vasijas_Alfareria_2", spVasijas, posAlfareria + new Vector2(2.0f, 0.2f), new Vector2(1.2f, 0.8f), true, root.transform);

        // C) PABELLÓN SUROESTE: JOYERÍA DEL CALLEJÓN (Don Teodoro)
        Vector2 posJoyeria = new Vector2(-9.5f, -8.5f);
        ColocarDecoracion("Toldo_Joyeria", spToldoVerde, posJoyeria + new Vector2(0f, 1.2f), new Vector2(3.2f, 1.8f), true, root.transform);
        ColocarDecoracion("Mesa_Joyeria", spMesa, posJoyeria + new Vector2(0f, -0.2f), new Vector2(2.0f, 0.8f), true, root.transform);
        ColocarDecoracion("Barriles_Joyeria", spBarriles, posJoyeria + new Vector2(-2.2f, 0f), new Vector2(1.0f, 1.5f), true, root.transform);

        // D) PABELLÓN SURESTE: ALMACÉN DE GRANOS Y ESPECIAS
        Vector2 posAlmacen = new Vector2(9.5f, -8.5f);
        ColocarDecoracion("Carpa_Almacen", spCarpaCamp, posAlmacen + new Vector2(0f, 1.0f), new Vector2(3f, 2.5f), true, root.transform);
        ColocarDecoracion("Barriles_Almacen", spBarriles, posAlmacen + new Vector2(-1.8f, -0.5f), new Vector2(1.0f, 1.5f), true, root.transform);
        ColocarDecoracion("Cajas_Almacen", spCajas, posAlmacen + new Vector2(1.8f, -0.5f), new Vector2(1.5f, 1.5f), true, root.transform);

        // Faroles perimetrales de la plaza
        ColocarDecoracion("Farol_NO", spFarol, new Vector2(-4.5f, 5.5f), Vector2.zero, false, root.transform);
        ColocarDecoracion("Farol_NE", spFarol, new Vector2(4.5f, 5.5f), Vector2.zero, false, root.transform);
        ColocarDecoracion("Farol_SO", spFarol, new Vector2(-4.5f, -4.5f), Vector2.zero, false, root.transform);
        ColocarDecoracion("Farol_SE", spFarol, new Vector2(4.5f, -4.5f), Vector2.zero, false, root.transform);

        // 5. SANTUARIO CENTRAL DEL COFRE GREMIAL Y EL SEGUNDO ECO
        Vector2 posCentroGremio = new Vector2(0f, 1.0f);
        ColocarDecoracion("Escalera_Dais_Sur", spEscalera, posCentroGremio + new Vector2(0f, -1.8f), Vector2.zero, false, root.transform, -470);
        ColocarDecoracion("Columna_NO", spColumna, posCentroGremio + new Vector2(-2.8f, 2.0f), new Vector2(0.8f, 1.5f), true, root.transform);
        ColocarDecoracion("Columna_NE", spColumna, posCentroGremio + new Vector2(2.8f, 2.0f), new Vector2(0.8f, 1.5f), true, root.transform);
        ColocarDecoracion("Columna_SO", spColumna, posCentroGremio + new Vector2(-2.8f, -1.5f), new Vector2(0.8f, 1.5f), true, root.transform);
        ColocarDecoracion("Columna_SE", spColumna, posCentroGremio + new Vector2(2.8f, -1.5f), new Vector2(0.8f, 1.5f), true, root.transform);

        ColocarDecoracion("Estandarte_Oro_Izq", spEstandarteOro, posCentroGremio + new Vector2(-1.8f, 2.2f), Vector2.zero, false, root.transform);
        ColocarDecoracion("Estandarte_Oro_Der", spEstandarteOro, posCentroGremio + new Vector2(1.8f, 2.2f), Vector2.zero, false, root.transform);
        ColocarDecoracion("Estandarte_Rojo_Centro", spEstandarteRojo, posCentroGremio + new Vector2(0f, 2.4f), Vector2.zero, false, root.transform);

        // Puzzle de Trueque
        MarketTradePuzzle tradePuzzle = root.AddComponent<MarketTradePuzzle>();

        // Segundo Eco: Eco de la Tradición
        GameObject ecoTradicion = new GameObject("Segundo_Eco_De_La_Tradicion");
        ecoTradicion.transform.SetParent(root.transform);
        ecoTradicion.transform.position = posCentroGremio + new Vector2(0f, 1.6f);
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

        // 6. PUESTOS DE TRUEQUE (MARKET STALL TRADE NODES)
        // Nodo 0: Arcón de Suministros de Cobre (Sur, salida del callejón)
        CrearPuestoTrueque(
            "Arcon_Suministros_Cobre",
            spArconCobre,
            new Vector2(0f, -14.0f),
            MarketStallTradeNode.NodeRole.SupplyCrateCopper,
            "[E] Inspeccionar Arcón de la Fragua",
            tradePuzzle,
            root.transform);

        // Nodo 1: Herrería de Don Braulio (NW)
        CrearPuestoTrueque(
            "Puesto_Herrero_DonBraulio",
            spLingoteCobre,
            posHerreria + new Vector2(0f, -0.3f),
            MarketStallTradeNode.NodeRole.BlacksmithStall,
            "[E] Herrero Don Braulio (Trueque)",
            tradePuzzle,
            root.transform);

        // Nodo 2: Alfarería de Doña Remedios (NE)
        CrearPuestoTrueque(
            "Puesto_Alfarera_DonaRemedios",
            spLingoteTemplado,
            posAlfareria + new Vector2(0f, -0.3f),
            MarketStallTradeNode.NodeRole.PotterStall,
            "[E] Alfarera Doña Remedios (Trueque)",
            tradePuzzle,
            root.transform);

        // Nodo 3: Joyería de Don Teodoro (SW)
        CrearPuestoTrueque(
            "Puesto_Joyero_DonTeodoro",
            spEsmeralda,
            posJoyeria + new Vector2(0f, -0.3f),
            MarketStallTradeNode.NodeRole.JewelerStall,
            "[E] Joyero Don Teodoro (Trueque)",
            tradePuzzle,
            root.transform);

        // Nodo 4: Cofre Sagrado del Gremio (Centro)
        CrearPuestoTrueque(
            "Cofre_Sagrado_Del_Gremio",
            spCofreGremio,
            posCentroGremio,
            MarketStallTradeNode.NodeRole.GuildTreasureChest,
            "[E] Abrir Cofre Sagrado del Gremio",
            tradePuzzle,
            root.transform);

        // Tablón de pistas del Gremio
        GameObject tablón = ColocarDecoracion("Tablon_Gremio_Artesanos", CortarCelda("TilesetElement", 1, 2, 1, 1), new Vector2(3.5f, -13.5f), new Vector2(1f, 1f), true, root.transform);
        InteractableSignOrNPC signGremio = tablón.AddComponent<InteractableSignOrNPC>();
        signGremio.speakerTitle = "Tablón del Gremio de Artesanos de San Felipe";
        signGremio.interactionPrompt = "[E] Leer Orden del Trueque";
        signGremio.dialoguePages = new string[]
        {
            "«ORDEN TRADICIONAL DEL TRUEQUE DE SAN FELIPE:»",
            "«1° Toma el Lingote de Cobre del Arcón de Suministros al Sur (0, -14).»",
            "«2° Cámbialo con Don Braulio (Herrería Noroeste) por el Lingote Templado.»",
            "«3° Lleva el Lingote Templado a Doña Remedios (Alfarería Noreste) por la Esmeralda de San Felipe.»",
            "«4° Entrega la Esmeralda a Don Teodoro (Joyería Suroeste) para forjar la Llave Dorada del Gremio.»",
            "«5° Inserta la Llave Dorada en el Cofre Sagrado del centro para liberar el Segundo Eco: Eco de la Tradición.»"
        };

        // 7. SISTEMA DE COMBATE Y HORDAS POR ZONA (3 ENCUENTROS TÁCTICOS)
        // A) Horda 1: Callejones de Entrada al Mercado (Sur)
        List<ZoneWaveSpawner.WaveDefinition> horda1Oleadas = new List<ZoneWaveSpawner.WaveDefinition>();
        ZoneWaveSpawner.WaveDefinition h1w1 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Saqueadores del Callejón Sur" };
        h1w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.GoblinRaider, spawnPosition = new Vector2(-2.5f, -8.0f) });
        h1w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.GoblinRaider, spawnPosition = new Vector2(2.5f, -8.0f) });
        h1w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.GoblinBomber, spawnPosition = new Vector2(0f, -10.5f) });
        horda1Oleadas.Add(h1w1);

        ZoneWaveSpawner.WaveDefinition h1w2 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Emboscada de Pólvora y Acero" };
        h1w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.GoblinBomber, spawnPosition = new Vector2(-3.0f, -7.0f) });
        h1w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.GoblinBomber, spawnPosition = new Vector2(3.0f, -7.0f) });
        h1w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.CorruptedGuard, spawnPosition = new Vector2(0f, -6.0f) });
        horda1Oleadas.Add(h1w2);

        CrearZonaHorda("Horda_Entrada_Callejones", "Callejón de Entrada al Mercado", new Vector2(0f, -8.0f), 7.5f, horda1Oleadas, root.transform);

        // B) Horda 2: Pasaje de Artesanos (Norte)
        List<ZoneWaveSpawner.WaveDefinition> horda2Oleadas = new List<ZoneWaveSpawner.WaveDefinition>();
        ZoneWaveSpawner.WaveDefinition h2w1 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Niebla en los Puestos de Alfarería" };
        h2w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.MistSlime, spawnPosition = new Vector2(-4.0f, 8.5f) });
        h2w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.MistSlime, spawnPosition = new Vector2(4.0f, 8.5f) });
        h2w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.OblivionMage, spawnPosition = new Vector2(0f, 11.0f) });
        horda2Oleadas.Add(h2w1);

        ZoneWaveSpawner.WaveDefinition h2w2 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Conjuradores del Olvido y Sombras" };
        h2w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.SombraLanzadora, spawnPosition = new Vector2(0f, 12.0f) });
        h2w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.OblivionMage, spawnPosition = new Vector2(-3.5f, 9.5f) });
        h2w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.OblivionMage, spawnPosition = new Vector2(3.5f, 9.5f) });
        horda2Oleadas.Add(h2w2);

        CrearZonaHorda("Horda_Pasaje_Artesanos", "Pasaje de Artesanos del Norte", new Vector2(0f, 9.0f), 8.0f, horda2Oleadas, root.transform);

        // C) Horda 3: Custodios del Cofre Sagrado (Centro de la Plaza)
        List<ZoneWaveSpawner.WaveDefinition> horda3Oleadas = new List<ZoneWaveSpawner.WaveDefinition>();
        ZoneWaveSpawner.WaveDefinition h3w1 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Custodia de la Fragua Olvidada" };
        h3w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.DarkOrcBrute, spawnPosition = new Vector2(0f, -1.8f) });
        h3w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.GoblinBomber, spawnPosition = new Vector2(-4.2f, 1.0f) });
        h3w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.GoblinBomber, spawnPosition = new Vector2(4.2f, 1.0f) });
        horda3Oleadas.Add(h3w1);

        ZoneWaveSpawner.WaveDefinition h3w2 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Furia de las Sombras Gremiales" };
        h3w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.DarkOrcBrute, spawnPosition = new Vector2(0f, 3.8f) });
        h3w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.SombraLanzadora, spawnPosition = new Vector2(3.5f, 3.2f) });
        h3w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.GoblinRaider, spawnPosition = new Vector2(-2.8f, 0f) });
        h3w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.GoblinRaider, spawnPosition = new Vector2(2.8f, 0f) });
        horda3Oleadas.Add(h3w2);

        CrearZonaHorda("Horda_Guardia_Cofre_Sagrado", "Plaza del Santuario Gremial", new Vector2(0f, 1.0f), 7.0f, horda3Oleadas, root.transform);

        // 8. CORAZONES CURATIVOS (+25 HP) EN PUNTOS ESTRATÉGICOS
        HeartPickup.SpawnHeart(new Vector3(0f, -12.5f, 0f), 25f, root.transform);
        HeartPickup.SpawnHeart(new Vector3(-12.5f, 0f, 0f), 25f, root.transform);
        HeartPickup.SpawnHeart(new Vector3(12.5f, 0f, 0f), 25f, root.transform);
        HeartPickup.SpawnHeart(new Vector3(0f, 13.5f, 0f), 25f, root.transform);

        // 9. PORTALES DE CONEXIÓN CON OTRAS ESCENAS
        // A) Portal Este: Regreso a la Gran Villa y Atrio de la Parroquia (SampleScene)
        ParroquiaExteriorBuilder.CrearPortalTransicionEscena(
            "Portal_Mercado_A_PlazaPrincipal",
            new Vector2(21.0f, 0.0f),
            new Vector2(1.8f, 3.0f),
            "SampleScene",
            LevelManager.ZoneId.ParroquiaExterior,
            new Vector2(-13.5f, 0.0f),
            new Color(0.95f, 0.75f, 0.30f, 0.85f),
            root.transform);

        // B) Portal Norte: Avance al Nivel 3: El Cerro del Fraile y la Gran Cruz Azul
        ParroquiaExteriorBuilder.CrearPortalTransicionEscena(
            "Portal_Mercado_A_CerroDelFraile",
            new Vector2(0.0f, 21.0f),
            new Vector2(3.0f, 1.8f),
            "Nivel3_CerroDelFraile",
            LevelManager.ZoneId.CerroDelFraile,
            new Vector2(0.0f, -25.0f),
            new Color(0.20f, 0.85f, 1.0f, 0.85f),
            root.transform);

        return root;
    }

    private static MarketStallTradeNode CrearPuestoTrueque(
        string nombre,
        Sprite spriteItem,
        Vector2 pos,
        MarketStallTradeNode.NodeRole role,
        string prompt,
        MarketTradePuzzle puzzle,
        Transform parent)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spriteItem;
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10f);

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.2f, 1.0f);
        col.isTrigger = false;

        MarketStallTradeNode node = go.AddComponent<MarketStallTradeNode>();
        node.role = role;
        node.labelPrompt = prompt;
        node.puzzlePadre = puzzle;
        return node;
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
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16f);
        }
        return ParroquiaExteriorBuilder.CrearSpriteRadial(new Color(1f, 0.85f, 0.2f, 1f));
    }

    private static void ColocarMuro(Sprite sp, Vector2 pos, Transform parent)
    {
        GameObject go = new GameObject($"Muro_{pos.x}_{pos.y}");
        go.transform.SetParent(parent);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10f);
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
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
            col.size = sizeCol;
        }
        return go;
    }
}
