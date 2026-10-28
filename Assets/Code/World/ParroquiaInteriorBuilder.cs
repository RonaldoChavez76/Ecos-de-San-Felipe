using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Constructor y controlador de la Escena dedicada "ParroquiaInterior.unity"
/// (Nivel 1 Interior: Gran Nave de la Parroquia de San Felipe Apóstol).
/// Construye un templo detallado de 33x43 unidades usando 100% sprites reales de los tilesets:
/// bancas coloniales, columnas, alfombras, capillas laterales con bibliotecas y relicarios,
/// oleada del crucero, Códice del Altar, Acertijo de las Tres Campanas de la Luz y el Primer Eco.
/// </summary>
public class ParroquiaInteriorBuilder : MonoBehaviour
{
    public Transform playerTransform;
    public float cameraSmoothTime = 0.14f;
    private Vector3 cameraVelocity = Vector3.zero;
    private Camera mainCam;

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "ParroquiaInterior")
        {
            InicializarEscenaParroquiaInterior();
        }
    }

    private void LateUpdate()
    {
        if (SceneManager.GetActiveScene().name != "ParroquiaInterior") return;

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

    public void InicializarEscenaParroquiaInterior()
    {
        DialogueManager.EnsureInstance();
        InventoryManager.EnsureInstance();
        PuzzleManager.EnsureInstance();
        InventoryHUD.EnsureInstance();
        PauseAndVictoryUI.EnsureInstance();

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.currentZone = LevelManager.ZoneId.ParroquiaInterior;
        }

        ConstruirInteriorParroquia();

        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
        {
            playerTransform = pm.transform;
        }

        mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5.5f;
            mainCam.backgroundColor = new Color(0.11f, 0.08f, 0.07f);
            if (playerTransform != null)
            {
                mainCam.transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y, -10f);
            }
        }
    }

    public static GameObject ConstruirInteriorParroquia(Transform parentRoot = null)
    {
        GameObject existente = GameObject.Find("Zona_Parroquia_Interior_Detallada");
        if (existente != null)
        {
            if (GameObject.Find("Marca_Parroquia_Interior_V4") != null)
            {
                return existente;
            }
            if (Application.isPlaying) Destroy(existente);
            else DestroyImmediate(existente);
        }

        GameObject root = new GameObject("Zona_Parroquia_Interior_Detallada");
        if (parentRoot != null) root.transform.SetParent(parentRoot);
        GameObject marca = new GameObject("Marca_Parroquia_Interior_V4");
        marca.transform.SetParent(root.transform);

        // Si estamos en SampleScene, solo construimos el interior cuando se carga la escena ParroquiaInterior
        // (o si se pide explícitamente en el editor para inspección, lo centramos en 0,0 de su escena)
        Vector2 offset = SceneManager.GetActiveScene().name == "ParroquiaInterior"
            ? Vector2.zero
            : new Vector2(0f, 110f);

        // 1. EXTRAER SPRITES REALES DE LOS TILESETS (CON TEXTURAS AISLADAS SIN COSTURAS)
        Sprite spLosaNave = CortarCelda("TilesetInteriorFloor", 1, 1, 1, 1);
        Sprite spLosaCapilla = CortarCelda("TilesetInteriorFloor", 13, 1, 1, 1);
        Sprite spLosaPresbiterio = CortarCelda("TilesetInteriorFloor", 1, 7, 1, 1);
        Sprite spPasilloCentral = CortarCelda("TilesetInteriorFloor", 1, 13, 1, 1);
        Sprite spMuroPiedra = CortarCelda("TilesetInteriorFloor", 13, 13, 1, 1);

        Sprite spAlfombraV = CortarCelda("tileset_bed", 0, 6, 2, 3);
        Sprite spAlfombraH = CortarCelda("tileset_bed", 2, 6, 3, 2);
        Sprite spBancaMadera = CortarCelda("tileset_camp", 0, 7, 3, 1);

        Sprite spColumna = CortarCelda("TilesetVillageAbandoned", 2, 3, 1, 3);
        Sprite spEscaleraAncha = CortarCelda("TilesetHouse", 10, 19, 3, 3);
        Sprite spEstandarteDorado = CortarCelda("TilesetHouse", 28, 20, 1, 2);
        Sprite spEstandarteMorado = CortarCelda("TilesetHouse", 27, 20, 1, 2);
        Sprite spEstandarteRojo = CortarCelda("TilesetHouse", 23, 20, 1, 2);
        Sprite spEstatuaSabio = CortarCelda("TilesetHouse", 3, 15, 2, 2);
        Sprite spEstatuaGuerrero = CortarCelda("TilesetHouse", 5, 17, 2, 2);

        Sprite spEstanteriaLibros = CortarCelda("TilesetElement", 8, 7, 2, 2);
        Sprite spArmario = CortarCelda("TilesetElement", 6, 7, 2, 2);
        Sprite spMesaBanquete = CortarCelda("TilesetElement", 11, 1, 3, 1);
        Sprite spMesaEstudio = CortarCelda("TilesetElement", 2, 10, 3, 1);
        Sprite spFarola = CortarCelda("TilesetElement", 6, 3, 1, 2);
        Sprite spVasija = CortarCelda("TilesetElement", 1, 0, 1, 1);
        Sprite spCofre = CortarCelda("TilesetElement", 6, 0, 1, 1);

        Sprite spRetabloAltar = CortarCelda("TilesetTowers", 18, 2, 2, 2);
        Sprite spCristalSagrado = CortarCelda("TilesetTowers", 18, 4, 2, 2);

        Sprite spPedestalCodice = CortarCelda("TilesetDungeon", 2, 2, 1, 1);
        Sprite spEsferaAlbaAzul = CortarCelda("TilesetDungeon", 4, 2, 1, 1);
        Sprite spEsferaMediodiaOro = CortarCelda("TilesetDungeon", 7, 2, 1, 1);
        Sprite spEsferaOcasoRoja = CortarCelda("TilesetDungeon", 6, 2, 1, 1);

        // 2. CONSTRUIR SUELO DEL GRAN TEMPLO (X: -15..15, Y: -17..23)
        GameObject pisoRoot = new GameObject("Capa_Suelo_ParroquiaInterior");
        pisoRoot.transform.SetParent(root.transform);

        for (int x = -15; x <= 15; x++)
        {
            for (int y = -17; y <= 23; y++)
            {
                // Recortar las esquinas exteriores para dar forma de Cruz Latina (Crucero en Y = -2..9)
                bool enCrucero = (y >= -2 && y <= 9);
                if (!enCrucero && Mathf.Abs(x) > 9) continue;

                Sprite sp = spLosaNave;
                if (Mathf.Abs(x) <= 2 && y <= 14) sp = spPasilloCentral;
                else if (y >= 11) sp = spLosaPresbiterio;
                else if (Mathf.Abs(x) >= 9) sp = spLosaCapilla;

                GameObject tile = new GameObject($"Tile_Int_{x}_{y}");
                tile.transform.SetParent(pisoRoot.transform);
                tile.transform.position = offset + new Vector2(x, y);
                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sortingOrder = -500;
            }
        }

        // Alfombras ceremoniales a lo largo del pasillo central y capillas
        for (int y = -13; y <= 8; y += 4)
        {
            ColocarDecoracion($"Alfombra_Nave_{y}", spAlfombraV, offset + new Vector2(0f, y), Vector2.zero, false, root.transform, -480);
        }
        ColocarDecoracion("Alfombra_Capilla_Oeste", spAlfombraH, offset + new Vector2(-11.5f, 3.5f), Vector2.zero, false, root.transform, -480);
        ColocarDecoracion("Alfombra_Capilla_Este", spAlfombraH, offset + new Vector2(11.5f, 3.5f), Vector2.zero, false, root.transform, -480);
        ColocarDecoracion("Alfombra_Altar_Mayor", spAlfombraH, offset + new Vector2(0f, 18.5f), Vector2.zero, false, root.transform, -480);

        // Escalinata monumental al Presbiterio (Y = 10.5)
        ColocarDecoracion("Escalinata_Presbiterio_Izq", spEscaleraAncha, offset + new Vector2(-2.0f, 10.5f), Vector2.zero, false, root.transform, -470);
        ColocarDecoracion("Escalinata_Presbiterio_Der", spEscaleraAncha, offset + new Vector2(2.0f, 10.5f), Vector2.zero, false, root.transform, -470);

        // 3. MUROS PERIMETRALES EN FORMA DE CRUZ LATINA CON COLISIÓN SÓLIDA
        GameObject murosRoot = new GameObject("Muros_Templo");
        murosRoot.transform.SetParent(root.transform);

        // Muro Norte del Altar
        for (int x = -9; x <= 9; x++)
            ColocarMuroCelda(spMuroPiedra, offset + new Vector2(x, 24f), murosRoot.transform);

        // Muro Sur (con abertura central en X = -1..1 para la Puerta de Salida)
        for (int x = -9; x <= 9; x++)
        {
            if (Mathf.Abs(x) <= 1) continue;
            ColocarMuroCelda(spMuroPiedra, offset + new Vector2(x, -18f), murosRoot.transform);
        }

        // Muros Laterales de la Nave Sur (Y = -17..-3) y Presbiterio Norte (Y = 10..23)
        for (int y = -17; y <= 23; y++)
        {
            if (y < -2 || y > 9)
            {
                ColocarMuroCelda(spMuroPiedra, offset + new Vector2(-10f, y), murosRoot.transform);
                ColocarMuroCelda(spMuroPiedra, offset + new Vector2(10f, y), murosRoot.transform);
            }
            else
            {
                // Extremos del Crucero (X = -16 y +16)
                ColocarMuroCelda(spMuroPiedra, offset + new Vector2(-16f, y), murosRoot.transform);
                ColocarMuroCelda(spMuroPiedra, offset + new Vector2(16f, y), murosRoot.transform);
            }
        }

        // Cierres horizontales del Crucero (X = -15..-10 y 10..15 en Y = -3 y Y = 10)
        for (int x = 10; x <= 15; x++)
        {
            ColocarMuroCelda(spMuroPiedra, offset + new Vector2(-x, -3f), murosRoot.transform);
            ColocarMuroCelda(spMuroPiedra, offset + new Vector2(x, -3f), murosRoot.transform);
            ColocarMuroCelda(spMuroPiedra, offset + new Vector2(-x, 10f), murosRoot.transform);
            ColocarMuroCelda(spMuroPiedra, offset + new Vector2(x, 10f), murosRoot.transform);
        }

        // 4. COLUMNAS DE CANTERA, BANCAS COLONIALES, FAROLAS Y ESTANDARTES
        float[] filasBancas = { -13f, -10f, -7f, -4f, -1f, 2f };
        foreach (float yb in filasBancas)
        {
            ColocarDecoracion($"Banca_Izq_{yb}", spBancaMadera, offset + new Vector2(-5.2f, yb), new Vector2(2.8f, 0.8f), true, root.transform);
            ColocarDecoracion($"Banca_Der_{yb}", spBancaMadera, offset + new Vector2(5.2f, yb), new Vector2(2.8f, 0.8f), true, root.transform);
        }

        float[] posColumnasY = { -14f, -8f, -2f, 4f, 10f, 16f };
        foreach (float yc in posColumnasY)
        {
            ColocarDecoracion($"Columna_Nave_Izq_{yc}", spColumna, offset + new Vector2(-8.2f, yc), new Vector2(0.9f, 1.2f), true, root.transform);
            ColocarDecoracion($"Columna_Nave_Der_{yc}", spColumna, offset + new Vector2(8.2f, yc), new Vector2(0.9f, 1.2f), true, root.transform);
            ColocarDecoracion($"Farola_Nave_Izq_{yc}", spFarola, offset + new Vector2(-3.0f, yc), new Vector2(0.6f, 0.6f), true, root.transform);
            ColocarDecoracion($"Farola_Nave_Der_{yc}", spFarola, offset + new Vector2(3.0f, yc), new Vector2(0.6f, 0.6f), true, root.transform);
        }

        // Estandartes litúrgicos en el Presbiterio
        ColocarDecoracion("Estandarte_Altar_Oro_Izq", spEstandarteDorado, offset + new Vector2(-6.5f, 22.2f), Vector2.zero, false, root.transform);
        ColocarDecoracion("Estandarte_Altar_Oro_Der", spEstandarteDorado, offset + new Vector2(6.5f, 22.2f), Vector2.zero, false, root.transform);
        ColocarDecoracion("Estandarte_Altar_Morado_Izq", spEstandarteMorado, offset + new Vector2(-4.5f, 22.2f), Vector2.zero, false, root.transform);
        ColocarDecoracion("Estandarte_Altar_Morado_Der", spEstandarteMorado, offset + new Vector2(4.5f, 22.2f), Vector2.zero, false, root.transform);
        ColocarDecoracion("Estandarte_Crucero_Izq", spEstandarteRojo, offset + new Vector2(-12.5f, 8.8f), Vector2.zero, false, root.transform);
        ColocarDecoracion("Estandarte_Crucero_Der", spEstandarteRojo, offset + new Vector2(12.5f, 8.8f), Vector2.zero, false, root.transform);

        // 5. CAPILLA PONIENTE (ARCHIVO Y BIBLIOTECA PARROQUIAL) Y CAPILLA ORIENTE (RELICARIO)
        ColocarDecoracion("Estanteria_Archivo_1", spEstanteriaLibros, offset + new Vector2(-13.8f, 7.8f), new Vector2(1.8f, 1.2f), true, root.transform);
        ColocarDecoracion("Estanteria_Archivo_2", spEstanteriaLibros, offset + new Vector2(-11.2f, 7.8f), new Vector2(1.8f, 1.2f), true, root.transform);
        ColocarDecoracion("Mesa_Archivo_Oeste", spMesaEstudio, offset + new Vector2(-12.5f, 4.5f), new Vector2(2.6f, 0.9f), true, root.transform);
        ColocarDecoracion("Cofre_Archivo_Oeste", spCofre, offset + new Vector2(-14.2f, 1.0f), new Vector2(0.9f, 0.9f), true, root.transform);
        ColocarDecoracion("Vasija_Archivo_Oeste", spVasija, offset + new Vector2(-14.2f, -0.5f), new Vector2(0.8f, 0.8f), true, root.transform);

        // NPC Padre Mateo en la Capilla Poniente
        GameObject padreMateo = ParroquiaExteriorBuilder.CrearNPCConSpriteExacto(
            "NPC_Padre_Mateo",
            "Mage-Cyan",
            0, 0, 32, 32, 20f,
            offset + new Vector2(-11.5f, 2.2f),
            root.transform);
        InteractableSignOrNPC npcMateo = padreMateo.AddComponent<InteractableSignOrNPC>();
        npcMateo.speakerTitle = "Padre Mateo, Sacristán de San Felipe";
        npcMateo.interactionPrompt = "[E] Hablar con el Padre Mateo";
        npcMateo.dialoguePages = new string[]
        {
            "¡Bendito sea el cielo que entraste a la Parroquia, Ronan! El Eco de la Fe está sellado sobre el Altar Mayor al Norte.",
            "Para disipar el sello oscuro, sube al Presbiterio y activa las Tres Esferas de Campana en el orden del Sol:",
            "1° Esfera Azul del Alba (Izquierda) -> 2° Esfera Dorada del Mediodía (Centro) -> 3° Esfera Roja del Ocaso (Derecha)."
        };

        // Capilla Oriente (Relicario y Estatuas Sagradas)
        ColocarDecoracion("Armario_Sacristia_1", spArmario, offset + new Vector2(11.2f, 7.8f), new Vector2(1.8f, 1.2f), true, root.transform);
        ColocarDecoracion("Armario_Sacristia_2", spArmario, offset + new Vector2(13.8f, 7.8f), new Vector2(1.8f, 1.2f), true, root.transform);
        ColocarDecoracion("Mesa_Relicario_Este", spMesaBanquete, offset + new Vector2(12.5f, 4.5f), new Vector2(2.8f, 0.9f), true, root.transform);
        ColocarDecoracion("Estatua_Sabio_Este", spEstatuaSabio, offset + new Vector2(13.5f, 0.5f), new Vector2(1.5f, 1.2f), true, root.transform);
        ColocarDecoracion("Estatua_Guerrero_Presbiterio_Izq", spEstatuaGuerrero, offset + new Vector2(-6.8f, 13.5f), new Vector2(1.5f, 1.2f), true, root.transform);
        ColocarDecoracion("Estatua_Guerrero_Presbiterio_Der", spEstatuaGuerrero, offset + new Vector2(6.8f, 13.5f), new Vector2(1.5f, 1.2f), true, root.transform);

        // 6. PUERTA DE SALIDA AL ATRIO EXTERIOR (CAMBIO DE ESCENA A "SampleScene")
        ParroquiaExteriorBuilder.CrearPortalTransicionEscena(
            "Puerta_Salida_A_SampleScene",
            offset + new Vector2(0f, -16.8f),
            new Vector2(2.6f, 1.4f),
            "SampleScene",
            LevelManager.ZoneId.ParroquiaExterior,
            new Vector2(0f, 27.5f),
            new Color(1f, 0.90f, 0.40f, 0.85f),
            root.transform);

        // 7. HORDA DEL CRUCERO DE LA NAVE (ZONE WAVE SPAWNER)
        GameObject spawnerCruceroGO = new GameObject("Horda_Crucero_Parroquia");
        spawnerCruceroGO.transform.SetParent(root.transform);
        spawnerCruceroGO.transform.position = offset + new Vector2(0f, 6.5f);

        ZoneWaveSpawner spawnerCrucero = spawnerCruceroGO.AddComponent<ZoneWaveSpawner>();
        spawnerCrucero.zoneTitle = "Crucero de la Parroquia";
        spawnerCrucero.activationRadius = 6.5f;

        ZoneWaveSpawner.WaveDefinition waveCrucero = new ZoneWaveSpawner.WaveDefinition
        {
            waveTitle = "Profanadores del Santuario"
        };
        waveCrucero.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.CorruptedGuard, spawnPosition = offset + new Vector2(-3.5f, 7.5f) });
        waveCrucero.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.OblivionMage, spawnPosition = offset + new Vector2(3.5f, 7.5f) });
        waveCrucero.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.CorruptedArcher, spawnPosition = offset + new Vector2(0.0f, 8.8f) });
        spawnerCrucero.waves.Add(waveCrucero);

        // Corazones Sagrados de curación dentro de la Parroquia (+25 HP cada uno)
        HeartPickup.SpawnHeart(offset + new Vector2(-9.5f, 2.2f), 25f, root.transform);
        HeartPickup.SpawnHeart(offset + new Vector2(9.5f, 2.2f), 25f, root.transform);
        HeartPickup.SpawnHeart(offset + new Vector2(-4.5f, 11.5f), 25f, root.transform);
        HeartPickup.SpawnHeart(offset + new Vector2(4.5f, 11.5f), 25f, root.transform);

        // 8. ALTAR MAYOR, CÓDICE LITÚRGICO, ACERTIJO DE LAS TRES CAMPANAS Y ECO DE LA FE
        ColocarDecoracion("Mesa_Altar_Mayor", spMesaBanquete, offset + new Vector2(0f, 20.5f), new Vector2(3.0f, 1.1f), true, root.transform);
        ColocarDecoracion("Retablo_Sagrado_Altar", spRetabloAltar, offset + new Vector2(0f, 22.2f), new Vector2(2.0f, 1.6f), true, root.transform);
        ColocarDecoracion("Cristal_Luz_Altar_Izq", spCristalSagrado, offset + new Vector2(-3.2f, 21.5f), new Vector2(1.4f, 1.4f), true, root.transform);
        ColocarDecoracion("Cristal_Luz_Altar_Der", spCristalSagrado, offset + new Vector2(3.2f, 21.5f), new Vector2(1.4f, 1.4f), true, root.transform);

        // Códice con la pista del Acertijo
        GameObject placaPista = ColocarDecoracion("Inscripcion_Liturgica_Campanas", spPedestalCodice, offset + new Vector2(0f, 13.5f), new Vector2(0.9f, 0.9f), true, root.transform);
        InteractableSignOrNPC signPista = placaPista.AddComponent<InteractableSignOrNPC>();
        signPista.speakerTitle = "Códice Sagrado del Altar de San Felipe";
        signPista.interactionPrompt = "[E] Leer Códice de las Campanas";
        signPista.dialoguePages = new string[]
        {
            "«Para disipar el Velo del Olvido que aprisiona el Eco de la Fe, haz resonar las Tres Campanas de la Luz siguiendo el curso del Sol:»",
            "«1° Toca la Campana del Alba al Poniente (Esfera Azul — Izquierda).»",
            "«2° Toca la Campana del Mediodía en el Cenit (Esfera Dorada — Centro).»",
            "«3° Toca la Campana del Ocaso al Oriente (Esfera Roja — Derecha).»"
        };

        // Primer Eco: Eco de la Fe
        GameObject ecoFe = new GameObject("Primer_Eco_De_La_Fe");
        ecoFe.transform.SetParent(root.transform);
        ecoFe.transform.position = offset + new Vector2(0f, 19.2f);
        CircleCollider2D colEco = ecoFe.AddComponent<CircleCollider2D>();
        colEco.isTrigger = true;
        colEco.radius = 0.70f;
        EchoCollectible echoComp = ecoFe.AddComponent<EchoCollectible>();
        echoComp.indiceEco = 1;
        echoComp.nombreEco = "Eco de la Fe (Parroquia)";
        ecoFe.SetActive(false);

        // Barrera de Sombra sobre el Altar
        GameObject barreraSombra = new GameObject("Barrera_Sombra_Altar");
        barreraSombra.transform.SetParent(root.transform);
        barreraSombra.transform.position = offset + new Vector2(0f, 19.2f);
        barreraSombra.transform.localScale = new Vector3(2.4f, 2.4f, 1f);
        SpriteRenderer srBarrera = barreraSombra.AddComponent<SpriteRenderer>();
        srBarrera.sprite = ParroquiaExteriorBuilder.CrearSpriteRadial(new Color(0.50f, 0.12f, 0.75f, 0.85f));
        srBarrera.sortingOrder = 25;

        // Controlador del Acertijo de las Tres Campanas
        BellLightPuzzle puzzleCampanas = root.AddComponent<BellLightPuzzle>();
        puzzleCampanas.barreraSombraAltar = barreraSombra;
        puzzleCampanas.ecoDeLaFeObjeto = ecoFe;

        BellPedestalNode ped1 = CrearPedestalCampanaReal(1, "Campana del Alba [1]", spEsferaAlbaAzul, offset + new Vector2(-4.8f, 16.0f), puzzleCampanas, root.transform);
        BellPedestalNode ped2 = CrearPedestalCampanaReal(2, "Campana del Mediodía [2]", spEsferaMediodiaOro, offset + new Vector2(0.0f, 16.8f), puzzleCampanas, root.transform);
        BellPedestalNode ped3 = CrearPedestalCampanaReal(3, "Campana del Ocaso [3]", spEsferaOcasoRoja, offset + new Vector2(4.8f, 16.0f), puzzleCampanas, root.transform);

        puzzleCampanas.pedestales.Add(ped1);
        puzzleCampanas.pedestales.Add(ped2);
        puzzleCampanas.pedestales.Add(ped3);

        return root;
    }

    private static BellPedestalNode CrearPedestalCampanaReal(int orden, string nombre, Sprite spPedestal, Vector2 pos, BellLightPuzzle puzzle, Transform parent)
    {
        GameObject go = new GameObject($"Pedestal_Campana_{orden}");
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(1.35f, 1.35f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spPedestal;
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10);

        GameObject halo = new GameObject("Halo_Luz");
        halo.transform.SetParent(go.transform);
        halo.transform.localPosition = Vector3.zero;
        halo.transform.localScale = Vector3.one * 2.2f;
        SpriteRenderer srHalo = halo.AddComponent<SpriteRenderer>();
        srHalo.sprite = ParroquiaExteriorBuilder.CrearSpriteRadial(new Color(1f, 0.92f, 0.35f, 0.65f));
        srHalo.sortingOrder = sr.sortingOrder - 1;

        BellPedestalNode node = go.AddComponent<BellPedestalNode>();
        node.ordenCampana = orden;
        node.nombreCampana = nombre;
        node.puzzlePadre = puzzle;
        node.haloRenderer = srHalo;

        return node;
    }

    private static Sprite CortarCelda(string sheetName, int col, int rowTop, int wCells, int hCells)
    {
        return ParroquiaExteriorBuilder.ExtraerSpriteExactoSinCosturas(sheetName, col, rowTop, wCells * 16, hCells * 16, 16f);
    }

    private static void ColocarMuroCelda(Sprite sp, Vector2 pos, Transform parent)
    {
        GameObject go = new GameObject($"Muro_{pos.x}_{pos.y}");
        go.transform.SetParent(parent);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10);
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
        sr.sortingOrder = customOrder ?? Mathf.RoundToInt(-pos.y * 10);

        if (conColision && sizeCol.sqrMagnitude > 0.01f)
        {
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = sizeCol;
        }
        return go;
    }
}
