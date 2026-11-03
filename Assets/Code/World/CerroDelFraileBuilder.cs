using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Constructor y controlador del Nivel 3: El Cerro del Fraile y Mirador de la Gran Cruz Azul.
/// Construye una escena modular completa de 45x65 unidades que abarca el ascenso épico a la cumbre:
///  • Faldas del Cerro (Y = -28..-10): arroyo con puente de piedra, pinos, rocas y primera horda.
///  • Desfiladero Rocoso (Y = -10..+6): paso estrecho entre riscos, campamento de peregrinos con el Ermitaño (NPC) y segunda horda.
///  • Mirador de la Gran Cruz Azul (Y = +6..+28): explanada ceremonial de piedra, 3 pedestales de sellos (Viento, Cantera, Alba),
///    la Monumental Cruz Azul cristalina con halos y destellos, y el combate multi-fase contra La Sombra Guardiana (Jefe Final).
///  • Auto-inicialización autónoma (cámara ortográfica suave, Ronan y todos los singletons de UI y sistemas).
/// </summary>
public class CerroDelFraileBuilder : MonoBehaviour
{
    [Header("Seguimiento de Cámara")]
    public Transform playerTransform;
    public float cameraSmoothTime = 0.14f;
    private Vector3 cameraVelocity = Vector3.zero;
    private Camera mainCam;

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "Nivel3_CerroDelFraile")
        {
            InicializarEscenaCerroDelFraile();
        }
    }

    private void LateUpdate()
    {
        if (SceneManager.GetActiveScene().name != "Nivel3_CerroDelFraile") return;

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

    public void InicializarEscenaCerroDelFraile()
    {
        // 1. Asegurar singletons de sistema y UI
        DialogueManager.EnsureInstance();
        InventoryManager.EnsureInstance();
        PuzzleManager.EnsureInstance();
        EnemyManager.EnsureInstance();
        ShopManager.EnsureInstance();
        InventoryHUD.EnsureInstance();
        PauseAndVictoryUI.EnsureInstance();
        BossHealthBarUI.EnsureInstance();
        if (AudioManager.Instance == null)
        {
            GameObject am = new GameObject("AudioManager");
            am.AddComponent<AudioManager>();
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.currentZone = LevelManager.ZoneId.CerroDelFraile;
        }

        // 2. Asegurar a Ronan (Player) al pie de la montaña
        GameObject playerObj = MercadoBuilder.AsegurarPlayer(new Vector2(0f, -25.5f));
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
            mainCam.backgroundColor = new Color(0.08f, 0.11f, 0.16f); // Noche estrellada de montaña
            if (playerTransform != null)
            {
                mainCam.transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y, -10f);
            }
        }

        // 4. Construir el mundo del Cerro del Fraile
        ConstruirZonaCerroDelFraile();
    }

    public static GameObject ConstruirZonaCerroDelFraile(Transform parentRoot = null)
    {
        GameObject existente = GameObject.Find("Zona_Cerro_Del_Fraile_Detallada");
        if (existente != null)
        {
            if (GameObject.Find("Marca_Cerro_V2") != null) return existente;
            if (Application.isPlaying) Destroy(existente);
            else DestroyImmediate(existente);
        }

        GameObject root = new GameObject("Zona_Cerro_Del_Fraile_Detallada");
        if (parentRoot != null) root.transform.SetParent(parentRoot);
        GameObject marca = new GameObject("Marca_Cerro_V2");
        marca.transform.SetParent(root.transform);

        // 1. RECORTE DE SPRITES REALES DE LOS TILESETS
        Sprite spPastoMontana = CortarCelda("TilesetField", 1, 1, 1, 1);
        Sprite spSenderoTierra = CortarCelda("TilesetFloor", 0, 0, 1, 1);
        Sprite spLosaPlataforma = CortarCelda("TilesetInteriorFloor", 13, 1, 1, 1);
        Sprite spMuroRisco = CortarCelda("TilesetRelief", 0, 0, 2, 2);
        Sprite spAguaArroyo = CortarCelda("TilesetWater", 0, 0, 1, 1);
        Sprite spPuentePiedra = CortarCelda("TilesetVillageAbandoned", 0, 0, 3, 2);

        Sprite spPinoGrande = CortarCelda("TilesetNature", 0, 0, 3, 4);
        Sprite spPinoChico = CortarCelda("TilesetNature", 3, 0, 2, 3);
        Sprite spRocaMontana = CortarCelda("TilesetRelief", 4, 0, 2, 2);
        Sprite spColumnaCeremonial = CortarCelda("TilesetVillageAbandoned", 2, 3, 1, 3);
        Sprite spEscaleraCumbre = CortarCelda("TilesetHouse", 10, 19, 3, 2);
        Sprite spEstandarteAzul = CortarCelda("TilesetHouse", 27, 20, 1, 2);

        // Campamento de Peregrinos
        Sprite spCarpaCamp = CortarCelda("tileset_camp", 0, 0, 3, 3);
        Sprite spFogata = CortarCelda("tileset_camp", 3, 5, 2, 2);
        Sprite spBancaCamp = CortarCelda("tileset_camp", 0, 7, 3, 1);
        Sprite spCajasCamp = CortarCelda("TilesetElement", 0, 1, 2, 2);

        // 2. SUELO DEL ASCENSO (45x65 unidades, X: -22..+22, Y: -28..+28)
        GameObject pisoRoot = new GameObject("Piso_Cerro");
        pisoRoot.transform.SetParent(root.transform);

        for (int x = -22; x <= 22; x++)
        {
            for (int y = -28; y <= 28; y++)
            {
                // Determinar el terreno según la elevación (Y)
                bool esArroyo = (y == -23 || y == -22) && (x < -2 || x > 2);
                bool esPuente = (y == -23 || y == -22) && (Mathf.Abs(x) <= 2);
                bool esSendero = (Mathf.Abs(x) <= 2 && y <= 6) || (Mathf.Abs(x - Mathf.Sin(y * 0.35f) * 3f) <= 2.2f && y > -10 && y <= 6);
                bool esCumbre = (y >= 6 && Mathf.Abs(x) <= 14);

                GameObject tile = new GameObject($"Tile_{x}_{y}");
                tile.transform.SetParent(pisoRoot.transform);
                tile.transform.position = new Vector3(x, y, 0f);
                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();

                if (esArroyo)
                {
                    sr.sprite = spAguaArroyo;
                    sr.sortingOrder = -520;
                    BoxCollider2D colAgua = tile.AddComponent<BoxCollider2D>();
                    colAgua.size = Vector2.one;
                }
                else if (esPuente)
                {
                    sr.sprite = spSenderoTierra;
                    sr.sortingOrder = -400; // Por encima de los tiles del arroyo
                }
                else if (esCumbre)
                {
                    sr.sprite = spLosaPlataforma;
                    sr.sortingOrder = -500;
                }
                else if (esSendero)
                {
                    sr.sprite = spSenderoTierra;
                    sr.sortingOrder = -490;
                }
                else
                {
                    sr.sprite = spPastoMontana;
                    sr.sortingOrder = -500;
                }
            }
        }

        // Puente ornamental de piedra sobre el arroyo
        ColocarDecoracion("Puente_Piedra_Arroyo", spPuentePiedra, new Vector2(0f, -22.5f), Vector2.zero, false, root.transform, -400);

        // 3. DESFILADERO ROCOSO Y RISCOS (Y: -10..+6)
        GameObject riscosRoot = new GameObject("Riscos_Y_Barrancos");
        riscosRoot.transform.SetParent(root.transform);

        // Paredes de riscos que encajonan el desfiladero
        for (int y = -10; y <= 5; y += 2)
        {
            // Riscos poniente
            ColocarRisco(spMuroRisco, new Vector2(-10f, y), riscosRoot.transform);
            ColocarRisco(spMuroRisco, new Vector2(-14f, y), riscosRoot.transform);
            ColocarRisco(spMuroRisco, new Vector2(-18f, y), riscosRoot.transform);

            // Riscos oriente
            ColocarRisco(spMuroRisco, new Vector2(10f, y), riscosRoot.transform);
            ColocarRisco(spMuroRisco, new Vector2(14f, y), riscosRoot.transform);
            ColocarRisco(spMuroRisco, new Vector2(18f, y), riscosRoot.transform);
        }

        // Muros perimetrales exteriores de la cumbre
        for (int x = -15; x <= 15; x += 2)
        {
            ColocarRisco(spMuroRisco, new Vector2(x, 27f), riscosRoot.transform); // Límite norte
        }
        for (int y = 6; y <= 27; y += 2)
        {
            ColocarRisco(spMuroRisco, new Vector2(-15f, y), riscosRoot.transform);
            ColocarRisco(spMuroRisco, new Vector2(15f, y), riscosRoot.transform);
        }

        // 4. ELEMENTOS DE LAS FALDAS (Y = -28..-10)
        ColocarDecoracion("Pino_Entrada_Izq", spPinoGrande, new Vector2(-6f, -26f), new Vector2(1.2f, 1.0f), true, root.transform);
        ColocarDecoracion("Pino_Entrada_Der", spPinoGrande, new Vector2(6f, -26f), new Vector2(1.2f, 1.0f), true, root.transform);
        ColocarDecoracion("Roca_Arroyo_1", spRocaMontana, new Vector2(-5f, -21f), new Vector2(1.5f, 1.2f), true, root.transform);
        ColocarDecoracion("Roca_Arroyo_2", spRocaMontana, new Vector2(5f, -21f), new Vector2(1.5f, 1.2f), true, root.transform);
        ColocarDecoracion("Pino_Faldas_1", spPinoChico, new Vector2(-8f, -16f), new Vector2(1f, 0.8f), true, root.transform);
        ColocarDecoracion("Pino_Faldas_2", spPinoGrande, new Vector2(8f, -15f), new Vector2(1.2f, 1.0f), true, root.transform);

        // Letrero inicial al pie del cerro
        GameObject cartelInicio = ColocarDecoracion("Cartel_Pie_Cerro", CortarCelda("TilesetElement", 1, 2, 1, 1), new Vector2(3.0f, -26.0f), new Vector2(0.8f, 0.8f), true, root.transform);
        InteractableSignOrNPC signInicio = cartelInicio.AddComponent<InteractableSignOrNPC>();
        signInicio.speakerTitle = "Sendero de Ascenso al Cerro del Fraile";
        signInicio.interactionPrompt = "[E] Leer Señal del Sendero";
        signInicio.dialoguePages = new string[]
        {
            "«Camino de Peregrinación al Cerro del Fraile y Mirador de la Gran Cruz Azul.»",
            "«Asciende por el desfiladero y cruza el campamento de peregrinos hacia la cumbre sagrada.»"
        };

        // 5. CAMPAMENTO DE PEREGRINOS Y ERMITAÑO (Y = -3..-1)
        Vector2 posCamp = new Vector2(-6.5f, -2.0f);
        ColocarDecoracion("Carpa_Peregrinos", spCarpaCamp, posCamp + new Vector2(0f, 1.2f), new Vector2(3.0f, 2.2f), true, root.transform);
        ColocarDecoracion("Fogata_Campamento", spFogata, posCamp + new Vector2(2.5f, -0.4f), new Vector2(1.2f, 1.0f), true, root.transform);
        ColocarDecoracion("Banca_Campamento", spBancaCamp, posCamp + new Vector2(-2.2f, -0.6f), new Vector2(2.0f, 0.6f), true, root.transform);
        ColocarDecoracion("Cajas_Campamento", spCajasCamp, posCamp + new Vector2(2.8f, 1.2f), new Vector2(1.2f, 1.2f), true, root.transform);

        // Ermitaño del Cerro (NPC con recorte de celda)
        GameObject ermitaño = ParroquiaExteriorBuilder.CrearNPCConSpriteExacto(
            "NPC_Ermitaño_Del_Cerro",
            "Mage-Red",
            0, 0, 32, 32, 20f,
            posCamp + new Vector2(0.2f, -0.8f),
            root.transform);
        InteractableSignOrNPC signErmitaño = ermitaño.AddComponent<InteractableSignOrNPC>();
        signErmitaño.speakerTitle = "Hermano Mateo, Ermitaño del Cerro";
        signErmitaño.interactionPrompt = "[E] Hablar con el Ermitaño";
        signErmitaño.dialoguePages = new string[]
        {
            "«¡Ronan! Los vientos de la montaña traen el eco de tus victorias en la Parroquia y el Mercado.»",
            "«En la cumbre reposa la Gran Cruz Azul, pero La Sombra Guardiana ha corrompido sus tres sellos sagrados: el Sello del Viento al Poniente, el Sello de la Cantera al Oriente y el Sello del Alba al frente.»",
            "«Mientras esos sellos permanezcan oscurecidos, su escudo sombrío mitigará tus golpes. ¡Purifícalos con tu espada [J] o interactuando [E] para romper su escudo antes de entablar combate!»",
            "«Recuerda usar tu contraataque [K] (0.20s) contra sus orbes reflejables y desata el Modo Asalto [L] cuando esté vulnerable. ¡Devuélvele la memoria a San Felipe!»"
        };

        // 6. ENTRADA A LA CUMBRE Y ESCALINATA (Y = 5.5..7.5)
        ColocarDecoracion("Escalera_Acceso_Cumbre", spEscaleraCumbre, new Vector2(0f, 6.2f), Vector2.zero, false, root.transform, -480);
        ColocarDecoracion("Columna_Cumbre_Izq", spColumnaCeremonial, new Vector2(-4.5f, 7.5f), new Vector2(0.8f, 1.5f), true, root.transform);
        ColocarDecoracion("Columna_Cumbre_Der", spColumnaCeremonial, new Vector2(4.5f, 7.5f), new Vector2(0.8f, 1.5f), true, root.transform);
        ColocarDecoracion("Estandarte_Cumbre_Izq", spEstandarteAzul, new Vector2(-4.5f, 9.2f), Vector2.zero, false, root.transform);
        ColocarDecoracion("Estandarte_Cumbre_Der", spEstandarteAzul, new Vector2(4.5f, 9.2f), Vector2.zero, false, root.transform);

        // Estela con la pista del acertijo de la Cruz Azul
        GameObject estela = ColocarDecoracion("Estela_Mirador_CruzAzul", CortarCelda("TilesetElement", 1, 2, 1, 1), new Vector2(3.5f, 8.5f), new Vector2(1f, 1f), true, root.transform);
        InteractableSignOrNPC signMirador = estela.AddComponent<InteractableSignOrNPC>();
        signMirador.speakerTitle = "Estela Ancestral de la Gran Cruz Azul";
        signMirador.interactionPrompt = "[E] Leer Inscripción de la Cumbre";
        signMirador.dialoguePages = new string[]
        {
            "«CUMBRE DEL CERRO DEL FRAILE — MIRADOR DE LA CRUZ AZUL:»",
            "«Los Tres Sellos de la Cruz Azul (Viento al Poniente, Cantera al Oriente y Alba al Sur) sostienen el equilibrio espiritual de Torres Mochas.»",
            "«Purifica los 3 sellos para derribar la invulnerabilidad de La Sombra Guardiana y recuperar el Eco de la Memoria.»"
        };

        // 7. MONUMENTAL GRAN CRUZ AZUL EN LA CIMA (0, 22.5)
        GameObject cruzRoot = new GameObject("Monumento_Gran_Cruz_Azul");
        cruzRoot.transform.SetParent(root.transform);
        cruzRoot.transform.position = new Vector3(0f, 22.5f, 0f);

        // Base escalonada de piedra
        ColocarDecoracion("Base_Cruz_Inferior", spEscaleraCumbre, new Vector2(0f, 20.8f), new Vector2(3.0f, 1.0f), true, cruzRoot.transform, -460);

        // Sprite procedural cristalino de la Cruz Azul (4.5 x 6.5 unidades)
        Sprite spCruzCristal = CrearSpriteCruzAzulCristalina();
        GameObject cruzGO = new GameObject("Cruz_Azul_Sprite");
        cruzGO.transform.SetParent(cruzRoot.transform);
        cruzGO.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        SpriteRenderer srCruz = cruzGO.AddComponent<SpriteRenderer>();
        srCruz.sprite = spCruzCristal;
        srCruz.sortingOrder = 15;

        // Halos luminosos (dual aura azul celestial)
        GameObject haloCruz = new GameObject("Halo_Luminoso_CruzAzul");
        haloCruz.transform.SetParent(cruzRoot.transform);
        haloCruz.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        haloCruz.transform.localScale = Vector3.one * 5.0f;
        SpriteRenderer srHaloCruz = haloCruz.AddComponent<SpriteRenderer>();
        srHaloCruz.sprite = ParroquiaExteriorBuilder.CrearSpriteRadial(new Color(0.12f, 0.78f, 1.0f, 0.65f));
        srHaloCruz.sortingOrder = 10;

        GameObject haloCruzExterior = new GameObject("Halo_Exterior_CruzAzul");
        haloCruzExterior.transform.SetParent(cruzRoot.transform);
        haloCruzExterior.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        haloCruzExterior.transform.localScale = Vector3.one * 8.0f;
        SpriteRenderer srHaloExt = haloCruzExterior.AddComponent<SpriteRenderer>();
        srHaloExt.sprite = ParroquiaExteriorBuilder.CrearSpriteRadial(new Color(0.05f, 0.35f, 0.90f, 0.28f));
        srHaloExt.sortingOrder = 5;

        // Columnas ceremoniales alrededor del monumento
        ColocarDecoracion("Columna_Cruz_NO", spColumnaCeremonial, new Vector2(-3.5f, 23.5f), new Vector2(0.8f, 1.5f), true, cruzRoot.transform);
        ColocarDecoracion("Columna_Cruz_NE", spColumnaCeremonial, new Vector2(3.5f, 23.5f), new Vector2(0.8f, 1.5f), true, cruzRoot.transform);
        ColocarDecoracion("Columna_Cruz_SO", spColumnaCeremonial, new Vector2(-4.2f, 20.0f), new Vector2(0.8f, 1.5f), true, cruzRoot.transform);
        ColocarDecoracion("Columna_Cruz_SE", spColumnaCeremonial, new Vector2(4.2f, 20.0f), new Vector2(0.8f, 1.5f), true, cruzRoot.transform);

        // 8. JEFE FINAL: LA SOMBRA GUARDIANA (0, 16.5)
        SombraGuardianaBossAI bossAI = ConstruirJefeSombraGuardiana(new Vector2(0f, 16.5f), root.transform, out GameObject escudoVisual);

        // 9. ACERTIJO DE LOS TRES SELLOS DE LA CRUZ AZUL
        BlueCrossShrinePuzzle puzzleCruz = root.AddComponent<BlueCrossShrinePuzzle>();
        puzzleCruz.jefeSombraGuardiana = bossAI;
        puzzleCruz.cruzAzulHaloRenderer = srHaloCruz;
        puzzleCruz.escudoVisualJefe = escudoVisual;

        BlueCrossSealNode s1 = CrearSelloCruzAzul("Sello del Viento [Poniente]", new Vector2(-8.0f, 15.5f), puzzleCruz, root.transform);
        BlueCrossSealNode s2 = CrearSelloCruzAzul("Sello de la Cantera [Oriente]", new Vector2(8.0f, 15.5f), puzzleCruz, root.transform);
        BlueCrossSealNode s3 = CrearSelloCruzAzul("Sello del Alba [Sur]", new Vector2(0.0f, 10.5f), puzzleCruz, root.transform);

        puzzleCruz.sellos.Add(s1);
        puzzleCruz.sellos.Add(s2);
        puzzleCruz.sellos.Add(s3);

        // 10. COMBATES POR OLEADAS DEL ASCENSO (ZONE WAVE SPAWNERS)
        // A) Horda 1: Faldas de la Montaña (Y = -17)
        List<ZoneWaveSpawner.WaveDefinition> horda1Oleadas = new List<ZoneWaveSpawner.WaveDefinition>();
        ZoneWaveSpawner.WaveDefinition h1w1 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Esporas del Sotobosque" };
        h1w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.SporeMushroom, spawnPosition = new Vector2(-3.5f, -17f) });
        h1w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.SporeMushroom, spawnPosition = new Vector2(3.5f, -17f) });
        h1w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.QuarryWorm, spawnPosition = new Vector2(0f, -19f) });
        horda1Oleadas.Add(h1w1);

        ZoneWaveSpawner.WaveDefinition h1w2 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Gusanos de la Cantera Antigua" };
        h1w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.QuarryWorm, spawnPosition = new Vector2(-2.5f, -16f) });
        h1w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.QuarryWorm, spawnPosition = new Vector2(2.5f, -16f) });
        h1w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.CorruptedGuard, spawnPosition = new Vector2(0f, -15f) });
        horda1Oleadas.Add(h1w2);

        CrearZonaHorda("Horda_Faldas_Montana", "Faldas del Cerro del Fraile", new Vector2(0f, -17f), 7.5f, horda1Oleadas, root.transform);

        // B) Horda 2: Desfiladero Rocoso (Y = 0)
        List<ZoneWaveSpawner.WaveDefinition> horda2Oleadas = new List<ZoneWaveSpawner.WaveDefinition>();
        ZoneWaveSpawner.WaveDefinition h2w1 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Tiradores del Barranco" };
        h2w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.DarkOrcBrute, spawnPosition = new Vector2(0f, 1f) });
        h2w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.CorruptedArcher, spawnPosition = new Vector2(-3.5f, 0f) });
        h2w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.CorruptedArcher, spawnPosition = new Vector2(3.5f, 0f) });
        horda2Oleadas.Add(h2w1);

        ZoneWaveSpawner.WaveDefinition h2w2 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Vanguardia de la Niebla de Montaña" };
        h2w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.DarkOrcBrute, spawnPosition = new Vector2(-2.5f, 2f) });
        h2w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.SombraLanzadora, spawnPosition = new Vector2(2.5f, 2f) });
        h2w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.OblivionMage, spawnPosition = new Vector2(0f, 3.5f) });
        horda2Oleadas.Add(h2w2);

        CrearZonaHorda("Horda_Desfiladero_Rocoso", "Desfiladero del Fraile", new Vector2(0f, 0f), 7.5f, horda2Oleadas, root.transform);

        // 11. CORAZONES CURATIVOS (+25 HP) EN PUNTOS ESTRATÉGICOS
        HeartPickup.SpawnHeart(new Vector3(0f, -23.5f, 0f), 25f, root.transform);  // Tras cruzar el puente
        HeartPickup.SpawnHeart(new Vector3(-4.5f, -1.5f, 0f), 25f, root.transform); // En el Campamento
        HeartPickup.SpawnHeart(new Vector3(-7f, 9f, 0f), 25f, root.transform);      // En la entrada a la cumbre
        HeartPickup.SpawnHeart(new Vector3(7f, 9f, 0f), 25f, root.transform);       // En la entrada a la cumbre

        // 12. PORTAL DE REGRESO AL MERCADO MUNICIPAL (SUR)
        ParroquiaExteriorBuilder.CrearPortalTransicionEscena(
            "Portal_Cerro_A_Mercado",
            new Vector2(0.0f, -27.5f),
            new Vector2(3.0f, 1.8f),
            "Nivel2_Mercado",
            LevelManager.ZoneId.Mercado,
            new Vector2(0.0f, 18.5f),
            new Color(0.95f, 0.65f, 0.20f, 0.85f),
            root.transform);

        return root;
    }

    private static SombraGuardianaBossAI ConstruirJefeSombraGuardiana(Vector2 pos, Transform parent, out GameObject escudoVisual)
    {
        GameObject bossGO = new GameObject("JefeFinal_La_Sombra_Guardiana");
        bossGO.transform.SetParent(parent);
        bossGO.transform.position = pos;
        bossGO.tag = "Enemy";
        EnemyManager.EnsureInstance()?.RegisterEnemy(bossGO);

        SpriteRenderer sr = bossGO.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 8;

        CircleCollider2D col = bossGO.AddComponent<CircleCollider2D>();
        col.radius = 0.85f;

        BlackCloverDemonRenderer demonRend = bossGO.AddComponent<BlackCloverDemonRenderer>();
        demonRend.tipoDemonio = BlackCloverDemonRenderer.DemonType.SombraGuardianaJefe;
        demonRend.customDemonImage = ZoneWaveSpawner.LoadSpriteTexture("SombraGuardiana_SpriteSheet");
        demonRend.InicializarSpritesDemonio();

        SombraGuardianaBossAI bossAI = bossGO.AddComponent<SombraGuardianaBossAI>();
        bossAI.isShieldedBySeals = true;
        bossAI.echoIndexOnDefeat = 3;

        escudoVisual = new GameObject("Escudo_Abismal_Sellos");
        escudoVisual.transform.SetParent(bossGO.transform);
        escudoVisual.transform.localPosition = Vector3.zero;
        escudoVisual.transform.localScale = Vector3.one * 2.4f;
        SpriteRenderer srEscudo = escudoVisual.AddComponent<SpriteRenderer>();
        srEscudo.sprite = ParroquiaExteriorBuilder.CrearSpriteRadial(new Color(0.55f, 0.10f, 0.90f, 0.55f));
        srEscudo.sortingOrder = 9;

        return bossAI;
    }

    private static BlueCrossSealNode CrearSelloCruzAzul(string nombre, Vector2 pos, BlueCrossShrinePuzzle puzzle, Transform parent)
    {
        GameObject go = new GameObject(nombre.Replace(" ", "_"));
        go.transform.SetParent(parent);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CortarCelda("TilesetVillageAbandoned", 2, 3, 1, 3);
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10f);

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 1.0f;

        GameObject flama = new GameObject("Flama_Sello");
        flama.transform.SetParent(go.transform);
        flama.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        flama.transform.localScale = Vector3.one * 2.2f;
        SpriteRenderer srFlama = flama.AddComponent<SpriteRenderer>();
        srFlama.sprite = ParroquiaExteriorBuilder.CrearSpriteRadial(new Color(0.65f, 0.15f, 0.85f, 0.75f));
        srFlama.sortingOrder = sr.sortingOrder + 1;

        BlueCrossSealNode node = go.AddComponent<BlueCrossSealNode>();
        node.nombreSello = nombre;
        node.puzzlePadre = puzzle;
        node.flamaRenderer = srFlama;
        return node;
    }

    /// <summary>
    /// Genera un sprite monumental de alta resolución para la Gran Cruz Azul (4.5 x 6.5 unidades),
    /// con facetado cristalino en tonos celestes/azur profundos y un núcleo refulgente de luz sagrada.
    /// </summary>
    public static Sprite CrearSpriteCruzAzulCristalina()
    {
        int w = 72;
        int h = 104;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color transparente = new Color(0, 0, 0, 0);
        Color bordeAzulOscuro = new Color(0.04f, 0.16f, 0.42f, 1f);
        Color azulZafiro = new Color(0.10f, 0.38f, 0.78f, 1f);
        Color azulCeleste = new Color(0.18f, 0.78f, 1.0f, 1f);
        Color blancoBrillo = new Color(0.88f, 0.98f, 1.0f, 1f);

        int vertMinX = 26;
        int vertMaxX = 45;
        int horizMinY = 56;
        int horizMaxY = 75;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool enVertical = (x >= vertMinX && x <= vertMaxX && y >= 6 && y <= 98);
                bool enHorizontal = (y >= horizMinY && y <= horizMaxY && x >= 6 && x <= 65);

                if (enVertical || enHorizontal)
                {
                    // Determinar bordes
                    bool esBordeVert = (x == vertMinX || x == vertMaxX || y == 6 || y == 98);
                    bool esBordeHoriz = (y == horizMinY || y == horizMaxY || x == 6 || x == 65);
                    bool esBordeExterior = false;

                    if (enVertical && !enHorizontal && esBordeVert) esBordeExterior = true;
                    if (enHorizontal && !enVertical && esBordeHoriz) esBordeExterior = true;
                    if (enVertical && enHorizontal)
                    {
                        if ((x == vertMinX || x == vertMaxX) && (y < horizMinY || y > horizMaxY)) esBordeExterior = true;
                        if ((y == horizMinY || y == horizMaxY) && (x < vertMinX || x > vertMaxX)) esBordeExterior = true;
                    }

                    if (esBordeExterior)
                    {
                        tex.SetPixel(x, y, bordeAzulOscuro);
                    }
                    else
                    {
                        // Eje central de brillo
                        bool enEjeX = (x == 35 || x == 36);
                        bool enEjeY = (y == 65 || y == 66);

                        if (enEjeX || enEjeY)
                        {
                            tex.SetPixel(x, y, blancoBrillo);
                        }
                        else if (Mathf.Abs(x - 35.5f) <= 4 || Mathf.Abs(y - 65.5f) <= 4)
                        {
                            tex.SetPixel(x, y, azulCeleste);
                        }
                        else
                        {
                            tex.SetPixel(x, y, azulZafiro);
                        }
                    }
                }
                else
                {
                    tex.SetPixel(x, y, transparente);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.15f), 16f);
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

    private static void ColocarRisco(Sprite sp, Vector2 pos, Transform parent)
    {
        GameObject go = new GameObject($"Risco_{pos.x}_{pos.y}");
        go.transform.SetParent(parent);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10f);
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2f, 2f);
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
