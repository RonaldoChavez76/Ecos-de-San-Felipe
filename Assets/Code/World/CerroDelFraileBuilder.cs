using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Constructor y controlador del Nivel 3: El Cerro del Fraile y Mirador de la Gran Cruz Azul.
/// Construye una escena épica de montaña de 65x95 unidades (X: -32..+32, Y: -45..+45) libre de vacíos negros:
///  • Paisaje de montaña orgánico: paredes de roca escalonadas en capas con relieve natural (TilesetRelief,
///    TilesetReliefDetail y rocas de TilesetNature) y densos bosques perimetrales de coníferas y pinos gigantes.
///  • Elementos de la Peregrinación:
///      - Arroyo ancho con agua cristalina y puente colonial macizo (sortingOrder = -400) flanqueado por farolas.
///      - Campamento del Ermitaño acogedor: carpa amplia (tileset_camp), fogata con brasas brillantes,
///        leños/bancas para sentarse, farolas de aceite y el Hermano Mateo (Ermitaño NPC).
///  • Cumbre Monumental de la Cruz Azul:
///      - Amplia explanada circular de cantera pulida con calzada ceremonial.
///      - Gran Cruz Azul en el altar norte (0, 36) sobre pedestal escalonado, con partículas azules flotantes y halos duales.
///      - Los 3 sellos mágicos (Viento, Cantera y Alba) sobre pedestales sagrados con copas de fuego místico.
///      - Combate multi-fase contra La Sombra Guardiana (Jefe Final) para obtener el Eco de la Memoria (Eco 3).
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
        SaveStateManager.EnsureInstance();
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

        // 2. Asegurar a Ronan (Player) al pie del sendero ANTES del puente y del letrero
        GameObject playerObj = MercadoBuilder.AsegurarPlayer(new Vector2(0f, -40.0f));
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
            mainCam.backgroundColor = new Color(0.14f, 0.24f, 0.16f);
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
            if (GameObject.Find("Marca_Cerro_V6") != null) return existente;
            if (Application.isPlaying) Destroy(existente);
            else DestroyImmediate(existente);
        }

        GameObject root = new GameObject("Zona_Cerro_Del_Fraile_Detallada");
        if (parentRoot != null) root.transform.SetParent(parentRoot);
        GameObject marca = new GameObject("Marca_Cerro_V6");
        marca.transform.SetParent(root.transform);

        // 1. TABLA OFICIAL DE COORDENADAS VERIFICADAS (0 PÍXELES DE SANGRADO / BLEED)
        // Suelos y Agua
        Sprite spPastoBase          = CortarCelda("TilesetFloor", 0, 12, 1, 1);
        Sprite spPastoClaro         = CortarCelda("TilesetFloor", 1, 12, 1, 1);
        Sprite spSenderoPiedra      = CortarCelda("TilesetFloor", 12, 15, 1, 1);
        Sprite spCanteraPulida      = CortarCelda("TilesetFloor", 1, 1, 1, 1);
        Sprite spAdoquinCumbre      = CortarCelda("TilesetFloor", 12, 1, 1, 1);
        Sprite spDetallePiedra      = CortarCelda("TilesetFloorDetail", 2, 1, 1, 1);
        Sprite spAlfombraAzul       = CortarCelda("TilesetInteriorFloor", 13, 1, 1, 1);
        Sprite spAguaArroyo         = CortarCelda("TilesetWater", 1, 1, 1, 1);

        // A) TILESET NATURE (Assets/Sprites/TilesetNature.png — 384x336 px, 24x21 celdas)
        Sprite spPinoGrande         = CortarCelda("TilesetNature", 0, 2, 4, 3);   // Macizo de pinos coníferas verdes (64x48 px, completo)
        Sprite spBosqueFrondoso     = CortarCelda("TilesetNature", 4, 2, 4, 3);   // Macizo de árboles verdes frondosos (64x48 px, completo)
        Sprite spBosqueOlivo        = CortarCelda("TilesetNature", 20, 2, 4, 3);  // Macizo de árboles verde olivo (64x48 px, completo)
        Sprite spArbolVerde         = CortarCelda("TilesetNature", 3, 18, 3, 3);  // Árbol verde individual completo (48x48 px)
        Sprite spArbolDorado        = CortarCelda("TilesetNature", 9, 18, 3, 3);  // Árbol dorado otoñal completo (48x48 px)
        Sprite spArbolCerezo        = CortarCelda("TilesetNature", 0, 18, 3, 3);  // Árbol florido sagrado para la cumbre (48x48 px)
        Sprite spPinoChico          = CortarCelda("TilesetNature", 2, 0, 2, 2);   // Pino joven completo (32x32 px)
        Sprite spArbustoRedondo     = CortarCelda("TilesetNature", 0, 0, 2, 2);   // Arbusto grande completo (32x32 px)
        Sprite spMacizoRocaCafe     = CortarCelda("TilesetNature", 12, 5, 4, 3);  // Gran formación de 3 peñascos cafés (64x48 px, 0 bleed)
        Sprite spMacizoRocaGris     = CortarCelda("TilesetNature", 16, 5, 4, 3);  // Gran formación de 3 peñascos grises (64x48 px, 0 bleed)
        Sprite spRocaMedianaCafe    = CortarCelda("TilesetNature", 13, 8, 2, 2);  // Roca mediana café completa (32x32 px, 0 bleed)
        Sprite spRocaMedianaGris    = CortarCelda("TilesetNature", 16, 8, 2, 2);  // Roca mediana gris completa (32x32 px, 0 bleed)
        Sprite spPenascoAltoCafe    = CortarCelda("TilesetNature", 21, 10, 2, 3); // Peñasco vertical escarpado café (32x48 px, 0 bleed)
        Sprite spPenascoAltoGris    = CortarCelda("TilesetNature", 21, 14, 2, 3); // Peñasco vertical escarpado gris (32x48 px, 0 bleed)
        Sprite spToconOscuro        = CortarCelda("TilesetNature", 0, 8, 2, 2);   // Tocón de árbol antiguo (32x32 px, 0 bleed)
        Sprite spDetallePasto       = CortarCelda("TilesetNature", 2, 10, 1, 1);  // Trébol de sotobosque en fila 10 (16x16 px, 0 bleed)

        // B) TILESET HOUSE, VILLAGE ABANDONED, ELEMENT Y CAMP (Verificados sin cortes)
        Sprite spArcoPiedra         = CortarCelda("TilesetHouse", 29, 20, 3, 3);  // Arco monumental de piedra gris (48x48 px, 0 bleed)
        Sprite spEstatuaDorada      = CortarCelda("TilesetHouse", 3, 15, 2, 2);   // Estatua de sabio en cantera dorada (32x32 px, 0 bleed)
        Sprite spEstatuaCustodio    = CortarCelda("TilesetHouse", 3, 19, 2, 2);   // Estatua de sabio en piedra gris oscura (32x32 px, 0 bleed)
        Sprite spEstatuaGuerrero    = CortarCelda("TilesetHouse", 5, 19, 2, 2);   // Estatua de guardián en piedra gris (32x32 px, 0 bleed)
        Sprite spEstandarteAzul     = CortarCelda("TilesetHouse", 24, 20, 1, 2);  // Estandarte ceremonial (16x32 px)
        Sprite spEstandarteOro      = CortarCelda("TilesetHouse", 28, 20, 1, 2);  // Estandarte dorado (16x32 px)
        Sprite spColumnaMusgo       = CortarCelda("TilesetVillageAbandoned", 2, 3, 1, 3); // Columna antigua con musgo (16x48 px)
        Sprite spBosqueMusgo        = CortarCelda("TilesetVillageAbandoned", 0, 6, 4, 3); // Árboles antiguos musgosos (64x48 px)
        Sprite spCajasApiladas      = CortarCelda("TilesetElement", 4, 3, 2, 2);  // Cajas de madera apiladas (32x32 px, 0 bleed)
        Sprite spCajasCamp          = CortarCelda("TilesetElement", 10, 5, 2, 2); // Cajas de provisiones (32x32 px, 0 bleed)
        Sprite spVasijasCopa        = CortarCelda("TilesetElement", 4, 0, 2, 1);  // Vasijas ceremoniales (32x16 px, 0 bleed)
        Sprite spLetrero            = CortarCelda("TilesetElement", 0, 2, 1, 1);  // Cartel de madera (16x16 px, 0 bleed)
        Sprite spTiendaGrande       = CortarCelda("tileset_camp", 4, 0, 3, 3);    // Carpa del ermitaño (48x48 px)
        Sprite spFogata             = CortarCelda("tileset_camp", 10, 3, 2, 2);   // Fogata encendida (32x32 px, 0 bleed)
        Sprite spBancaMadera        = CortarCelda("tileset_camp", 0, 7, 3, 1);    // Troncos para sentarse (48x16 px, 0 bleed)
        Sprite spFarolAceite        = CortarCelda("tileset_camp", 6, 5, 1, 2);    // Farol colgante (16x32 px)

        // 2. SUELO ORGÁNICO DEL ASCENSO (65x95 unidades, X: -32..+32, Y: -45..+45)
        GameObject pisoRoot = new GameObject("Piso_Cerro_Organico");
        pisoRoot.transform.SetParent(root.transform);

        Vector2 centroCumbre = new Vector2(0f, 28.0f);
        float radioExplanadaCircular = 15.5f;

        for (int x = -32; x <= 32; x++)
        {
            for (int y = -45; y <= 45; y++)
            {
                // Franja continua del río (y >= -34 && y <= -32): agua continua en todo el ancho del río
                bool enFranjaRio = (y >= -34 && y <= -32);
                bool esOrillaRio = (y == -35 || y == -31) && (Mathf.Abs(x) > 3 && Mathf.Abs(x) <= 22);

                // Plataforma ceremonial limpia del Altar de la Gran Cruz Azul (X = -4..+4, Y = 34..38)
                bool enPlataformaAltar = (x >= -4 && x <= 4 && y >= 34 && y <= 38);
                bool esBordePlataformaAltar = enPlataformaAltar && (Mathf.Abs(x) == 4 || y == 34 || y == 38);

                // Paso empedrado limpio en la entrada a la cumbre (Y = 12..14, X = -2..+2)
                bool esPasoEntradaCumbre = (y >= 12 && y <= 14 && Mathf.Abs(x) <= 2);

                float distCumbre = Vector2.Distance(new Vector2(x, y), centroCumbre);
                bool esExplanadaCircular = (y >= 13 && distCumbre <= radioExplanadaCircular) || (y >= 13 && y <= 39 && Mathf.Abs(x) <= 13);
                bool esBordeExplanada    = esExplanadaCircular && (distCumbre >= radioExplanadaCircular - 1.4f || Mathf.Abs(x) == 13);
                bool esCalzadaAltar      = (y >= 15 && y <= 34 && Mathf.Abs(x) <= 2);

                float curvaDesfiladero = Mathf.Sin(y * 0.25f) * 3.5f;
                bool esSendero = !enFranjaRio && (
                                 (Mathf.Abs(x) <= 3 && y < 14) ||
                                 (Mathf.Abs(x - curvaDesfiladero) <= 2.6f && y >= -22 && y < 14) ||
                                 (x >= -12 && x <= -2 && y >= -6 && y <= -2)); // ramal al campamento del ermitaño

                GameObject tile = new GameObject($"Tile_{x}_{y}");
                tile.transform.SetParent(pisoRoot.transform);
                tile.transform.position = new Vector3(x, y, 0f);
                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();

                if (enFranjaRio)
                {
                    sr.sprite = spAguaArroyo;
                    sr.sortingOrder = -520;
                    sr.color = new Color(0.75f, 0.92f, 1f, 0.98f);
                    // Colisión de agua únicamente fuera del puente (Mathf.Abs(x) > 3)
                    if (Mathf.Abs(x) > 3 && Mathf.Abs(x) <= 22)
                    {
                        BoxCollider2D colAgua = tile.AddComponent<BoxCollider2D>();
                        colAgua.size = Vector2.one;
                    }

                    // Pasarela del puente limpia sobre el agua (Mathf.Abs(x) <= 2 calzada y X = -3, +3 bordes de cantera)
                    if (Mathf.Abs(x) <= 2)
                    {
                        GameObject calzadaPuente = new GameObject($"Puente_Calzada_{x}_{y}");
                        calzadaPuente.transform.SetParent(pisoRoot.transform);
                        calzadaPuente.transform.position = new Vector3(x, y, 0f);
                        SpriteRenderer srP = calzadaPuente.AddComponent<SpriteRenderer>();
                        srP.sprite = spAdoquinCumbre;
                        srP.sortingOrder = -400;
                    }
                    else if (Mathf.Abs(x) == 3)
                    {
                        GameObject bordePuente = new GameObject($"Puente_Borde_{x}_{y}");
                        bordePuente.transform.SetParent(pisoRoot.transform);
                        bordePuente.transform.position = new Vector3(x, y, 0f);
                        SpriteRenderer srB = bordePuente.AddComponent<SpriteRenderer>();
                        srB.sprite = spCanteraPulida;
                        srB.sortingOrder = -395;
                    }
                }
                else if (enPlataformaAltar)
                {
                    sr.sprite = esBordePlataformaAltar ? spAdoquinCumbre : (spAlfombraAzul != null ? spAlfombraAzul : spCanteraPulida);
                    sr.sortingOrder = -482;
                }
                else if (esPasoEntradaCumbre)
                {
                    sr.sprite = spAdoquinCumbre;
                    sr.sortingOrder = -486;
                }
                else if (esCalzadaAltar)
                {
                    sr.sprite = spAlfombraAzul != null ? spAlfombraAzul : spAdoquinCumbre;
                    sr.sortingOrder = -485;
                }
                else if (esBordeExplanada)
                {
                    sr.sprite = spAdoquinCumbre;
                    sr.sortingOrder = -495;
                }
                else if (esExplanadaCircular)
                {
                    sr.sprite = spCanteraPulida;
                    sr.sortingOrder = -500;
                    if (((x * 19 + y * 37) % 9 == 0) && spDetallePiedra != null)
                    {
                        GameObject det = new GameObject($"DetalleCumbre_{x}_{y}");
                        det.transform.SetParent(pisoRoot.transform);
                        det.transform.position = new Vector3(x, y, 0f);
                        SpriteRenderer srDet = det.AddComponent<SpriteRenderer>();
                        srDet.sprite = spDetallePiedra;
                        srDet.sortingOrder = -492;
                    }
                }
                else if (esSendero)
                {
                    sr.sprite = spSenderoPiedra;
                    sr.sortingOrder = -490;
                }
                else if (esOrillaRio)
                {
                    sr.sprite = spAdoquinCumbre != null ? spAdoquinCumbre : spSenderoPiedra;
                    sr.sortingOrder = -492;
                }
                else
                {
                    sr.sprite = ((x * 13 + y * 29) % 4 == 0 && spPastoClaro != null) ? spPastoClaro : spPastoBase;
                    sr.sortingOrder = -500;
                    if (((x * 23 + y * 41) % 31 == 0) && spDetallePasto != null)
                    {
                        GameObject trebol = new GameObject($"Trebol_{x}_{y}");
                        trebol.transform.SetParent(pisoRoot.transform);
                        trebol.transform.position = new Vector3(x, y, 0f);
                        SpriteRenderer srT = trebol.AddComponent<SpriteRenderer>();
                        srT.sprite = spDetallePasto;
                        srT.sortingOrder = -495;
                    }
                }
            }
        }

        // Faroles en las 4 esquinas del puente colonial limpio
        ColocarDecoracion("Farol_Puente_Sur_Izq",   spFarolAceite, new Vector2(-3.8f, -35.2f), new Vector2(0.7f, 0.9f), true, root.transform);
        ColocarDecoracion("Farol_Puente_Sur_Der",   spFarolAceite, new Vector2(3.8f,  -35.2f), new Vector2(0.7f, 0.9f), true, root.transform);
        ColocarDecoracion("Farol_Puente_Norte_Izq", spFarolAceite, new Vector2(-3.8f, -30.8f), new Vector2(0.7f, 0.9f), true, root.transform);
        ColocarDecoracion("Farol_Puente_Norte_Der", spFarolAceite, new Vector2(3.8f,  -30.8f), new Vector2(0.7f, 0.9f), true, root.transform);

        // 3. REESTRUCTURACIÓN VISUAL DE LA LADERA, DESFILADERO Y CUMBRE (0 BLEED, ESPACIADO DE 3.5 A 4 UNIDADES)
        GameObject montanaRoot = new GameObject("Riscos_Escalonados_Y_Bosque_Coniferas");
        montanaRoot.transform.SetParent(root.transform);

        // A) Ladera y Desfiladero Montañoso (Y = -22 a +12, espaciado cada 3.8 unidades para que cada roca/árbol luzca íntegro)
        Sprite[] rocasLinea1 = { spRocaMedianaGris, spPenascoAltoCafe, spRocaMedianaCafe, spPenascoAltoGris };
        Vector2[] colLinea1  = { new Vector2(1.8f, 1.4f), new Vector2(1.8f, 2.2f), new Vector2(1.8f, 1.4f), new Vector2(1.8f, 2.2f) };

        int stepIdx = 0;
        for (float y = -22.0f; y <= 11.5f; y += 3.8f)
        {
            float offsetCurva = Mathf.Sin(y * 0.2f) * 1.1f;
            // Dejar claro amplio para el Campamento del Ermitaño en el Poniente (Y: -8.5..+1.5)
            float xRiscoIzq = (y >= -8.5f && y <= 1.5f) ? -16.0f : (-12.5f + offsetCurva);
            float xRiscoDer = 12.5f + offsetCurva;

            int rIdxIzq = stepIdx % rocasLinea1.Length;
            int rIdxDer = (stepIdx + 2) % rocasLinea1.Length;

            // Primera línea junto al valle: peñascos y rocas medianas con colisionador ajustado a su base
            ColocarDecoracion($"Risco_Valle_Izq_{stepIdx}", rocasLinea1[rIdxIzq], new Vector2(xRiscoIzq, y), colLinea1[rIdxIzq], true, montanaRoot.transform);
            ColocarDecoracion($"Risco_Valle_Der_{stepIdx}", rocasLinea1[rIdxDer], new Vector2(xRiscoDer, y), colLinea1[rIdxDer], true, montanaRoot.transform);

            // Segunda línea (3.6 unidades más afuera): macizos de 3 peñascos (4x3) alternados con árboles verdes y dorados (3x3)
            Sprite spSegundaIzq = (stepIdx % 2 == 0) ? ((stepIdx % 4 == 0) ? spMacizoRocaGris : spMacizoRocaCafe) : ((stepIdx % 3 == 0) ? spArbolDorado : spArbolVerde);
            Sprite spSegundaDer = (stepIdx % 2 == 1) ? ((stepIdx % 4 == 1) ? spMacizoRocaCafe : spMacizoRocaGris) : ((stepIdx % 3 == 0) ? spArbolVerde : spArbolDorado);
            ColocarDecoracion($"Macizo_Ladera_Izq_{stepIdx}", spSegundaIzq, new Vector2(xRiscoIzq - 3.6f, y + 0.6f), new Vector2(2.6f, 1.8f), true, montanaRoot.transform);
            ColocarDecoracion($"Macizo_Ladera_Der_{stepIdx}", spSegundaDer, new Vector2(xRiscoDer + 3.6f, y + 0.6f), new Vector2(2.6f, 1.8f), true, montanaRoot.transform);

            stepIdx++;
        }

        // B) Perímetro de la Explanada de la Cumbre (X = -16.5 y X = +16.5, Y = 13 a 41, paso de 3.5 unidades)
        int cumbreIdx = 0;
        for (float y = 13.5f; y <= 41.0f; y += 3.5f)
        {
            Sprite spBordeCumbre;
            Vector2 colSize;
            if (cumbreIdx % 3 == 0)
            {
                spBordeCumbre = spMacizoRocaGris;
                colSize = new Vector2(3.2f, 2.0f);
            }
            else if (cumbreIdx % 3 == 1)
            {
                spBordeCumbre = spPenascoAltoGris;
                colSize = new Vector2(1.8f, 2.2f);
            }
            else
            {
                spBordeCumbre = spPinoChico;
                colSize = new Vector2(1.6f, 1.4f);
            }

            ColocarDecoracion($"Cumbre_Borde_Izq_{cumbreIdx}", spBordeCumbre, new Vector2(-16.5f, y), colSize, true, montanaRoot.transform);
            ColocarDecoracion($"Cumbre_Borde_Der_{cumbreIdx}", spBordeCumbre, new Vector2(16.5f,  y), colSize, true, montanaRoot.transform);
            cumbreIdx++;
        }

        // Borde Norte superior detrás de la explanada de la cumbre (Y = 42.0, X = -14 a +14, paso de 4.0 unidades)
        int norteIdx = 0;
        for (float x = -14.0f; x <= 14.0f; x += 4.0f)
        {
            Sprite spNorte = (norteIdx % 2 == 0) ? spMacizoRocaGris : spPenascoAltoGris;
            ColocarDecoracion($"Cumbre_Borde_Norte_{norteIdx}", spNorte, new Vector2(x, 42.0f), new Vector2(2.8f, 2.0f), true, montanaRoot.transform);
            norteIdx++;
        }

        // C) Tercera línea perimetral exterior: Bosques tupidos de spPinoGrande, spBosqueFrondoso y spBosqueOlivo (paso de 4.0 unidades)
        Sprite[] bosquesPerimetro = { spPinoGrande, spBosqueFrondoso, spBosqueOlivo, spBosqueMusgo };
        int bosqueIdx = 0;
        for (float y = -44.0f; y <= 44.0f; y += 4.0f)
        {
            Sprite b1 = bosquesPerimetro[bosqueIdx % 3];
            Sprite b2 = bosquesPerimetro[(bosqueIdx + 1) % 3];
            Sprite b3 = bosquesPerimetro[(bosqueIdx + 2) % 4];

            // Bosques Poniente
            ColocarDecoracion($"Bosque_Oeste_1_{bosqueIdx}", b1, new Vector2(-21.5f, y),        new Vector2(2.8f, 2.0f), true,  montanaRoot.transform);
            ColocarDecoracion($"Bosque_Oeste_2_{bosqueIdx}", b2, new Vector2(-26.0f, y + 1.5f), Vector2.zero,            false, montanaRoot.transform);
            ColocarDecoracion($"Bosque_Oeste_3_{bosqueIdx}", b3, new Vector2(-30.0f, y),        Vector2.zero,            false, montanaRoot.transform);

            // Bosques Oriente
            ColocarDecoracion($"Bosque_Este_1_{bosqueIdx}",  b2, new Vector2(21.5f,  y),        new Vector2(2.8f, 2.0f), true,  montanaRoot.transform);
            ColocarDecoracion($"Bosque_Este_2_{bosqueIdx}",  b1, new Vector2(26.0f,  y + 1.5f), Vector2.zero,            false, montanaRoot.transform);
            ColocarDecoracion($"Bosque_Este_3_{bosqueIdx}",  b3, new Vector2(30.0f,  y),        Vector2.zero,            false, montanaRoot.transform);
            bosqueIdx++;
        }

        // Cierre boscoso al Sur y al Norte
        for (int x = -28; x <= 28; x += 5)
        {
            Sprite spBosqueCierre = (Mathf.Abs(x) % 2 == 0) ? spPinoGrande : spBosqueFrondoso;
            ColocarDecoracion($"Bosque_Cumbre_Norte_{x}", spBosqueCierre, new Vector2(x, 44.5f), new Vector2(2.8f, 2.0f), true, montanaRoot.transform);
            if (Mathf.Abs(x) > 5)
            {
                ColocarDecoracion($"Bosque_Base_Sur_{x}", spBosqueCierre, new Vector2(x, -43.5f), new Vector2(2.8f, 2.0f), true, montanaRoot.transform);
            }
        }

        // 4 Muros Colisionadores Perimetrales del Cerro del Fraile (Evitar salirse del mapa)
        CrearMuroColision("Muro_Perimetral_Cerro_Norte", new Vector2(0f,  42.5f), new Vector2(70f, 2f), montanaRoot.transform);
        CrearMuroColision("Muro_Perimetral_Cerro_Sur",   new Vector2(0f, -45.5f), new Vector2(70f, 2f), montanaRoot.transform);
        CrearMuroColision("Muro_Perimetral_Cerro_Este",  new Vector2(18f,   0f),  new Vector2(2f, 95f), montanaRoot.transform);
        CrearMuroColision("Muro_Perimetral_Cerro_Oeste", new Vector2(-18f,  0f),  new Vector2(2f, 95f), montanaRoot.transform);

        // 4. ELEMENTOS DE LAS FALDAS Y PEREGRINACIÓN (Y = -43..-22)
        ColocarDecoracion("Pino_Entrada_Izq",   spPinoGrande,      new Vector2(-7.5f, -38.5f), new Vector2(2.2f, 1.6f), true, root.transform);
        ColocarDecoracion("Pino_Entrada_Der",   spPinoGrande,      new Vector2(7.5f,  -38.5f), new Vector2(2.2f, 1.6f), true, root.transform);
        ColocarDecoracion("Arbusto_Sendero_1",  spArbustoRedondo,  new Vector2(-4.5f, -39.5f), new Vector2(1.2f, 1.0f), true, root.transform);
        ColocarDecoracion("Arbusto_Sendero_2",  spArbustoRedondo,  new Vector2(4.5f,  -39.5f), new Vector2(1.2f, 1.0f), true, root.transform);
        ColocarDecoracion("Roca_Arroyo_1",      spRocaMedianaGris, new Vector2(-6.0f, -30.5f), new Vector2(1.6f, 1.2f), true, root.transform);
        ColocarDecoracion("Roca_Arroyo_2",      spRocaMedianaCafe, new Vector2(6.0f,  -30.5f), new Vector2(1.6f, 1.2f), true, root.transform);
        ColocarDecoracion("Tocon_Faldas_Izq",   spToconOscuro,     new Vector2(-5.5f, -26.5f), new Vector2(1.2f, 1.0f), true, root.transform);
        ColocarDecoracion("Arbol_Faldas_1",     spArbolDorado,     new Vector2(-8.8f, -24.5f), new Vector2(1.6f, 1.2f), true, root.transform);
        ColocarDecoracion("Arbol_Faldas_2",     spArbolVerde,      new Vector2(8.8f,  -24.5f), new Vector2(1.6f, 1.2f), true, root.transform);

        // Letrero inicial al pie del cerro (al lado del camino en (2.0, -37.5), ANTES del puente)
        GameObject cartelInicio = ColocarDecoracion("Cartel_Pie_Cerro", spLetrero, new Vector2(2.0f, -37.5f), new Vector2(0.8f, 0.8f), true, root.transform);
        InteractableSignOrNPC signInicio = cartelInicio.AddComponent<InteractableSignOrNPC>();
        signInicio.speakerTitle = "Sendero de Ascenso al Cerro del Fraile";
        signInicio.interactionPrompt = "[E] Leer Señal del Sendero";
        signInicio.dialoguePages = new string[]
        {
            "«Camino de Peregrinación al Cerro del Fraile y Mirador de la Gran Cruz Azul.»",
            "«Cruza el puente de piedra sobre el arroyo y visita el Campamento del Ermitaño en el desfiladero antes de ascender a la cumbre.»"
        };

        // NPC Peregrina al cruzar el puente
        GameObject npcPeregrina = ParroquiaExteriorBuilder.CrearNPCConSpriteExacto(
            "NPC_Peregrina_Elena",
            "Character-Base",
            0, 0, 32, 32, 20f,
            new Vector2(-3.2f, -29.2f),
            root.transform);
        SpriteRenderer srElena = npcPeregrina.GetComponent<SpriteRenderer>();
        if (srElena != null) srElena.color = new Color(0.88f, 0.96f, 1f, 1f);
        InteractableSignOrNPC diagElena = npcPeregrina.AddComponent<InteractableSignOrNPC>();
        diagElena.speakerTitle = "Elena, Peregrina de la Cruz Azul";
        diagElena.interactionPrompt = "[E] Hablar con Peregrina Elena";
        diagElena.dialoguePages = new string[]
        {
            "«¡Alabado sea tu valor, Ronan! Has traído de vuelta la luz de la Parroquia y el orgullo de nuestros artesanos del Mercado.»",
            "«Más arriba, en el recodo poniente del desfiladero (-10, -4), el Hermano Mateo mantiene encendida la fogata de los peregrinos.»"
        };

        // 5. CAMPAMENTO ACOGEDOR DEL ERMITAÑO (Y = -4 en (-10.0, -4.0))
        Vector2 posCamp = new Vector2(-10.0f, -4.0f);
        ColocarDecoracion("Carpa_Ermitano",     spTiendaGrande,  posCamp + new Vector2(-1.2f, 1.8f),  new Vector2(2.8f, 2.0f), true, root.transform);
        ColocarDecoracion("Fogata_Campamento",  spFogata,        posCamp + new Vector2(1.6f, -0.3f),  new Vector2(1.3f, 1.0f), true, root.transform);
        ColocarDecoracion("Lenios_Sentarse_1",  spBancaMadera,   posCamp + new Vector2(-1.4f, -1.2f), new Vector2(2.2f, 0.7f), true, root.transform);
        ColocarDecoracion("Lenios_Sentarse_2",  spBancaMadera,   posCamp + new Vector2(1.6f,  1.2f),  new Vector2(2.2f, 0.7f), true, root.transform);
        ColocarDecoracion("Farol_Aceite_Camp1", spFarolAceite,   posCamp + new Vector2(-3.2f, 0.4f),  new Vector2(0.7f, 0.9f), true, root.transform);
        ColocarDecoracion("Farol_Aceite_Camp2", spFarolAceite,   posCamp + new Vector2(3.4f, -0.4f),  new Vector2(0.7f, 0.9f), true, root.transform);
        ColocarDecoracion("Cajas_Provisiones",  spCajasCamp,     posCamp + new Vector2(-3.2f, 2.0f),  new Vector2(1.3f, 1.2f), true, root.transform);
        ColocarDecoracion("Cajas_Madera_Extra", spCajasApiladas, posCamp + new Vector2(2.2f,  2.2f),  new Vector2(1.3f, 1.2f), true, root.transform);

        // Resplandor cálido de las brasas de la fogata
        GameObject brasasLuz = new GameObject("Brasas_Resplandor_Campamento");
        brasasLuz.transform.SetParent(root.transform);
        brasasLuz.transform.position = posCamp + new Vector2(1.6f, -0.3f);
        brasasLuz.transform.localScale = Vector3.one * 3.2f;
        SpriteRenderer srBrasas = brasasLuz.AddComponent<SpriteRenderer>();
        srBrasas.sprite = ParroquiaExteriorBuilder.CrearSpriteRadial(new Color(1f, 0.55f, 0.12f, 0.42f));
        srBrasas.sortingOrder = -450;

        // Ermitaño del Cerro (NPC con recorte exacto de celda)
        GameObject ermitano = ParroquiaExteriorBuilder.CrearNPCConSpriteExacto(
            "NPC_Ermitano_Del_Cerro",
            "Mage-Red",
            0, 0, 32, 32, 20f,
            posCamp + new Vector2(0.0f, 0.1f),
            root.transform);
        InteractableSignOrNPC signErmitano = ermitano.AddComponent<InteractableSignOrNPC>();
        signErmitano.speakerTitle = "Hermano Mateo, Ermitaño del Cerro";
        signErmitano.interactionPrompt = "[E] Hablar con el Ermitaño";
        signErmitano.dialoguePages = new string[]
        {
            "«¡Ronan! Descansa un instante junto al calor de las brasas; la cima del Cerro del Fraile está a pocos pasos.»",
            "«En la explanada superior se alza la Gran Cruz Azul, pero La Sombra Guardiana ha corrompido sus tres sellos sagrados: el Sello del Viento al Poniente (-11.5, 28), el Sello de la Cantera al Oriente (11.5, 28) y el Sello del Alba al Sur (0, 20).»",
            "«Mientras las copas de fuego místico estén oscurecidas, su escudo abismal mitigará tus ataques. ¡Golpéalos con tu espada [J] o interactúa con [E] para purificarlos!»",
            "«Usa tu Contraataque [K] (0.20s) para devolverle sus propios orbes y desata el Modo Asalto [L] para reclamar el Eco de la Memoria.»"
        };

        // 6. ENTRADA MONUMENTAL A LA CUMBRE (Y = 13..16, paso empedrado limpio flanqueado por columnas y custodios)
        ColocarDecoracion("Arco_Acceso_Izq",         spArcoPiedra,      new Vector2(-4.5f, 13.8f), new Vector2(2.2f, 1.5f), true, root.transform);
        ColocarDecoracion("Arco_Acceso_Der",         spArcoPiedra,      new Vector2(4.5f,  13.8f), new Vector2(2.2f, 1.5f), true, root.transform);
        ColocarDecoracion("Columna_Cumbre_Izq",      spColumnaMusgo,    new Vector2(-3.0f, 14.5f), new Vector2(0.9f, 1.5f), true, root.transform);
        ColocarDecoracion("Columna_Cumbre_Der",      spColumnaMusgo,    new Vector2(3.0f,  14.5f), new Vector2(0.9f, 1.5f), true, root.transform);
        ColocarDecoracion("Estatua_Custodio_Izq",    spEstatuaCustodio, new Vector2(-6.8f, 15.0f), new Vector2(1.5f, 1.6f), true, root.transform);
        ColocarDecoracion("Estatua_Custodio_Der",    spEstatuaCustodio, new Vector2(6.8f,  15.0f), new Vector2(1.5f, 1.6f), true, root.transform);
        ColocarDecoracion("Estandarte_Cumbre_Izq",   spEstandarteAzul,  new Vector2(-3.0f, 16.2f), Vector2.zero, false, root.transform);
        ColocarDecoracion("Estandarte_Cumbre_Der",   spEstandarteAzul,  new Vector2(3.0f,  16.2f), Vector2.zero, false, root.transform);
        ColocarDecoracion("Arbol_Cerezo_Cumbre_NO",  spArbolCerezo,     new Vector2(-11.5f,35.5f), new Vector2(1.6f, 1.4f), true, root.transform);
        ColocarDecoracion("Arbol_Cerezo_Cumbre_NE",  spArbolCerezo,     new Vector2(11.5f, 35.5f), new Vector2(1.6f, 1.4f), true, root.transform);
        ColocarDecoracion("Arbol_Dorado_Cumbre_SO",  spArbolDorado,     new Vector2(-11.5f,19.5f), new Vector2(1.6f, 1.4f), true, root.transform);
        ColocarDecoracion("Arbol_Dorado_Cumbre_SE",  spArbolDorado,     new Vector2(11.5f, 19.5f), new Vector2(1.6f, 1.4f), true, root.transform);

        // Estela con la pista del acertijo de la Cruz Azul
        GameObject estela = ColocarDecoracion("Estela_Mirador_CruzAzul", spLetrero, new Vector2(3.8f, 16.0f), new Vector2(0.9f, 0.9f), true, root.transform);
        InteractableSignOrNPC signMirador = estela.AddComponent<InteractableSignOrNPC>();
        signMirador.speakerTitle = "Estela Ancestral de la Gran Cruz Azul";
        signMirador.interactionPrompt = "[E] Leer Inscripción de la Cumbre";
        signMirador.dialoguePages = new string[]
        {
            "«CUMBRE DEL CERRO DEL FRAILE — MIRADOR DE LA GRAN CRUZ AZUL:»",
            "«Los Tres Sellos de la Cruz Azul (Viento al Poniente, Cantera al Oriente y Alba al Sur) sostienen el espíritu de San Felipe.»",
            "«Purifica los 3 pedestales sagrados con [E] o [J] para disipar el escudo de La Sombra Guardiana y liberar el Eco de la Memoria.»"
        };

        // 7. MONUMENTAL GRAN CRUZ AZUL EN EL ALTAR NORTE (0, 36) SIN TEXTURAS CORTADAS DETRÁS
        GameObject cruzRoot = new GameObject("Monumento_Gran_Cruz_Azul");
        cruzRoot.transform.SetParent(root.transform);
        cruzRoot.transform.position = new Vector3(0f, 36.0f, 0f);

        // Sprite procedural cristalino de la Cruz Azul (4.5 x 6.5 unidades)
        Sprite spCruzCristal = CrearSpriteCruzAzulCristalina();
        GameObject cruzGO = new GameObject("Cruz_Azul_Sprite");
        cruzGO.transform.SetParent(cruzRoot.transform);
        cruzGO.transform.localPosition = new Vector3(0f, 2.0f, 0f);
        SpriteRenderer srCruz = cruzGO.AddComponent<SpriteRenderer>();
        srCruz.sprite = spCruzCristal;
        srCruz.sortingOrder = 15;

        // Halos duales luminosos (aura azul celestial e irradiación profunda)
        GameObject haloCruz = new GameObject("Halo_Luminoso_CruzAzul");
        haloCruz.transform.SetParent(cruzRoot.transform);
        haloCruz.transform.localPosition = new Vector3(0f, 2.0f, 0f);
        haloCruz.transform.localScale = Vector3.one * 5.8f;
        SpriteRenderer srHaloCruz = haloCruz.AddComponent<SpriteRenderer>();
        srHaloCruz.sprite = ParroquiaExteriorBuilder.CrearSpriteRadial(new Color(0.12f, 0.82f, 1.0f, 0.68f));
        srHaloCruz.sortingOrder = 10;

        GameObject haloCruzExterior = new GameObject("Halo_Exterior_CruzAzul");
        haloCruzExterior.transform.SetParent(cruzRoot.transform);
        haloCruzExterior.transform.localPosition = new Vector3(0f, 2.0f, 0f);
        haloCruzExterior.transform.localScale = Vector3.one * 9.5f;
        SpriteRenderer srHaloExt = haloCruzExterior.AddComponent<SpriteRenderer>();
        srHaloExt.sprite = ParroquiaExteriorBuilder.CrearSpriteRadial(new Color(0.05f, 0.40f, 0.95f, 0.32f));
        srHaloExt.sortingOrder = 5;

        // Partículas azules flotantes alrededor del altar de la Gran Cruz Azul
        Vector2[] offsetsParticulas = {
            new Vector2(-2.2f, 1.2f), new Vector2(2.2f, 1.2f),
            new Vector2(-1.6f, 3.2f), new Vector2(1.6f, 3.2f),
            new Vector2(-2.8f, 2.4f), new Vector2(2.8f, 2.4f),
            new Vector2(0f,    4.4f), new Vector2(0f,   0.4f)
        };
        for (int i = 0; i < offsetsParticulas.Length; i++)
        {
            GameObject part = new GameObject($"Particula_Azul_Altar_{i}");
            part.transform.SetParent(cruzRoot.transform);
            part.transform.localPosition = offsetsParticulas[i];
            part.transform.localScale = Vector3.one * 0.75f;
            SpriteRenderer srP = part.AddComponent<SpriteRenderer>();
            srP.sprite = ParroquiaExteriorBuilder.CrearSpriteRadial(new Color(0.35f, 0.95f, 1f, 0.88f));
            srP.sortingOrder = 16;
        }

        // Flanqueo ceremonial del altar con spColumnaMusgo, spEstatuaDorada, spEstatuaGuerrero y spEstandarteOro
        ColocarDecoracion("Columna_Cruz_NO",   spColumnaMusgo,    new Vector2(-3.8f, 38.2f), new Vector2(0.8f, 1.5f), true, cruzRoot.transform);
        ColocarDecoracion("Columna_Cruz_NE",   spColumnaMusgo,    new Vector2(3.8f,  38.2f), new Vector2(0.8f, 1.5f), true, cruzRoot.transform);
        ColocarDecoracion("Columna_Cruz_SO",   spColumnaMusgo,    new Vector2(-4.4f, 34.5f), new Vector2(0.8f, 1.5f), true, cruzRoot.transform);
        ColocarDecoracion("Columna_Cruz_SE",   spColumnaMusgo,    new Vector2(4.4f,  34.5f), new Vector2(0.8f, 1.5f), true, cruzRoot.transform);
        ColocarDecoracion("Estatua_Altar_Izq", spEstatuaDorada,   new Vector2(-2.8f, 37.8f), new Vector2(1.4f, 1.4f), true, cruzRoot.transform);
        ColocarDecoracion("Estatua_Altar_Der", spEstatuaDorada,   new Vector2(2.8f,  37.8f), new Vector2(1.4f, 1.4f), true, cruzRoot.transform);
        ColocarDecoracion("Guerrero_Altar_SO", spEstatuaGuerrero, new Vector2(-3.2f, 34.5f), new Vector2(1.4f, 1.4f), true, cruzRoot.transform);
        ColocarDecoracion("Guerrero_Altar_SE", spEstatuaGuerrero, new Vector2(3.2f,  34.5f), new Vector2(1.4f, 1.4f), true, cruzRoot.transform);
        ColocarDecoracion("Estandarte_Cruz_I", spEstandarteOro,   new Vector2(-2.0f, 36.8f), Vector2.zero, false, cruzRoot.transform);
        ColocarDecoracion("Estandarte_Cruz_D", spEstandarteOro,   new Vector2(2.0f,  36.8f), Vector2.zero, false, cruzRoot.transform);

        // 8. JEFE FINAL: LA SOMBRA GUARDIANA en (0, 28)
        SombraGuardianaBossAI bossAI = ConstruirJefeSombraGuardiana(new Vector2(0f, 28.0f), root.transform, out GameObject escudoVisual);

        // 9. ACERTIJO DE LOS TRES SELLOS DE LA CRUZ AZUL (Pedestales Sagrados con Copas de Fuego Místico)
        BlueCrossShrinePuzzle puzzleCruz = root.AddComponent<BlueCrossShrinePuzzle>();
        puzzleCruz.jefeSombraGuardiana = bossAI;
        puzzleCruz.cruzAzulHaloRenderer = srHaloCruz;
        puzzleCruz.escudoVisualJefe = escudoVisual;

        BlueCrossSealNode s1 = CrearSelloCruzAzul("Sello del Viento [Poniente]", new Vector2(-11.5f, 28.0f), spEstatuaDorada, spVasijasCopa, puzzleCruz, root.transform);
        BlueCrossSealNode s2 = CrearSelloCruzAzul("Sello de la Cantera [Oriente]", new Vector2(11.5f, 28.0f), spEstatuaDorada, spVasijasCopa, puzzleCruz, root.transform);
        BlueCrossSealNode s3 = CrearSelloCruzAzul("Sello del Alba [Sur]", new Vector2(0.0f, 20.0f), spEstatuaDorada, spVasijasCopa, puzzleCruz, root.transform);

        puzzleCruz.sellos.Add(s1);
        puzzleCruz.sellos.Add(s2);
        puzzleCruz.sellos.Add(s3);

        // 10. COMBATES POR OLEADAS DEL ASCENSO (ZONE WAVE SPAWNERS CON RADIO 5.5f)
        // A) Horda 1: Faldas de la Montaña (Y = -25)
        List<ZoneWaveSpawner.WaveDefinition> horda1Oleadas = new List<ZoneWaveSpawner.WaveDefinition>();
        ZoneWaveSpawner.WaveDefinition h1w1 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Esporas del Sotobosque" };
        h1w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.SporeMushroom, spawnPosition = new Vector2(-3.5f, -25f) });
        h1w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.SporeMushroom, spawnPosition = new Vector2(3.5f, -25f) });
        h1w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.QuarryWorm, spawnPosition = new Vector2(0f, -27f) });
        horda1Oleadas.Add(h1w1);

        ZoneWaveSpawner.WaveDefinition h1w2 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Gusanos de la Cantera Antigua" };
        h1w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.QuarryWorm, spawnPosition = new Vector2(-2.5f, -24f) });
        h1w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.QuarryWorm, spawnPosition = new Vector2(2.5f, -24f) });
        h1w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.CorruptedGuard, spawnPosition = new Vector2(0f, -23f) });
        horda1Oleadas.Add(h1w2);

        CrearZonaHorda("Horda_Faldas_Montana", "Faldas del Cerro del Fraile", new Vector2(0f, -25f), 5.5f, horda1Oleadas, root.transform);

        // B) Horda 2: Desfiladero Rocoso (Y = 3)
        List<ZoneWaveSpawner.WaveDefinition> horda2Oleadas = new List<ZoneWaveSpawner.WaveDefinition>();
        ZoneWaveSpawner.WaveDefinition h2w1 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Tiradores del Barranco" };
        h2w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.DarkOrcBrute, spawnPosition = new Vector2(0f, 4f) });
        h2w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.CorruptedArcher, spawnPosition = new Vector2(-3.5f, 3f) });
        h2w1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.CorruptedArcher, spawnPosition = new Vector2(3.5f, 3f) });
        horda2Oleadas.Add(h2w1);

        ZoneWaveSpawner.WaveDefinition h2w2 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Vanguardia de la Niebla de Montaña" };
        h2w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.DarkOrcBrute, spawnPosition = new Vector2(-2.5f, 5f) });
        h2w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.SombraLanzadora, spawnPosition = new Vector2(2.5f, 5f) });
        h2w2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.OblivionMage, spawnPosition = new Vector2(0f, 6.5f) });
        horda2Oleadas.Add(h2w2);

        CrearZonaHorda("Horda_Desfiladero_Rocoso", "Desfiladero del Fraile", new Vector2(0f, 3f), 5.5f, horda2Oleadas, root.transform);

        // 11. CORAZONES CURATIVOS (+25 HP) EN PUNTOS ESTRATÉGICOS
        HeartPickup.SpawnHeart(new Vector3(0f, -29.0f, 0f), 25f, root.transform);   // Tras cruzar el puente
        HeartPickup.SpawnHeart(new Vector3(-7.5f, -4.5f, 0f), 25f, root.transform); // Junto al Campamento del Ermitaño
        HeartPickup.SpawnHeart(new Vector3(-8.0f, 17.5f, 0f), 25f, root.transform); // Entrada poniente de la cumbre
        HeartPickup.SpawnHeart(new Vector3(8.0f,  17.5f, 0f), 25f, root.transform); // Entrada oriente de la cumbre

        // 12. PORTAL DE REGRESO AL MERCADO MUNICIPAL (SUR en (0, -43.5))
        ParroquiaExteriorBuilder.CrearPortalTransicionEscena(
            "Portal_Cerro_A_Mercado",
            new Vector2(0.0f, -43.5f),
            new Vector2(3.0f, 1.8f),
            "Nivel2_Mercado",
            LevelManager.ZoneId.Mercado,
            new Vector2(0.0f, 30.0f),
            new Color(0.95f, 0.65f, 0.20f, 0.85f),
            root.transform);

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

    private static BlueCrossSealNode CrearSelloCruzAzul(
        string nombre,
        Vector2 pos,
        Sprite spPedestalBase,
        Sprite spCopa,
        BlueCrossShrinePuzzle puzzle,
        Transform parent)
    {
        GameObject go = new GameObject(nombre.Replace(" ", "_"));
        go.transform.SetParent(parent);
        go.transform.position = pos;

        // Pedestal sagrado de cantera
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spPedestalBase != null ? spPedestalBase : CortarCelda("TilesetVillageAbandoned", 2, 3, 1, 3);
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10f);

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 1.15f;

        // Copa ceremonial sobre el pedestal
        if (spCopa != null)
        {
            GameObject copa = new GameObject("Copa_Ceremonial");
            copa.transform.SetParent(go.transform);
            copa.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            SpriteRenderer srCopa = copa.AddComponent<SpriteRenderer>();
            srCopa.sprite = spCopa;
            srCopa.sortingOrder = sr.sortingOrder + 1;
        }

        // Llama mística sobre la copa
        GameObject flama = new GameObject("Flama_Sello");
        flama.transform.SetParent(go.transform);
        flama.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        flama.transform.localScale = Vector3.one * 2.4f;
        SpriteRenderer srFlama = flama.AddComponent<SpriteRenderer>();
        srFlama.sprite = ParroquiaExteriorBuilder.CrearSpriteRadial(new Color(0.65f, 0.15f, 0.85f, 0.82f));
        srFlama.sortingOrder = sr.sortingOrder + 2;

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
        int width = 96;
        int height = 144;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color transparente = new Color(0, 0, 0, 0);
        Color[] pixeles = new Color[width * height];
        for (int i = 0; i < pixeles.Length; i++) pixeles[i] = transparente;

        Color azulProfundo = new Color(0.04f, 0.25f, 0.65f, 1f);
        Color azulCristal = new Color(0.15f, 0.65f, 0.98f, 1f);
        Color azulCielo = new Color(0.55f, 0.88f, 1.0f, 1f);
        Color blancoNucleo = new Color(0.95f, 0.98f, 1.0f, 1f);

        int barraVAncho = 24;
        int barraVXMin = (width - barraVAncho) / 2;
        int barraVXMax = barraVXMin + barraVAncho;
        int barraVYMin = 10;
        int barraVYMax = height - 10;

        int barraHAlto = 22;
        int barraHYMin = (int)(height * 0.58f);
        int barraHYMax = barraHYMin + barraHAlto;
        int barraHXMin = 8;
        int barraHXMax = width - 8;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool enVertical = (x >= barraVXMin && x <= barraVXMax && y >= barraVYMin && y <= barraVYMax);
                bool enHorizontal = (x >= barraHXMin && x <= barraHXMax && y >= barraHYMin && y <= barraHYMax);

                if (enVertical || enHorizontal)
                {
                    float distCentroX = Mathf.Abs(x - width * 0.5f) / (barraVAncho * 0.5f);
                    float distCentroY = Mathf.Abs(y - (barraHYMin + barraHAlto * 0.5f)) / (barraHAlto * 0.5f);
                    float factorBorde = Mathf.Max(distCentroX, distCentroY);

                    Color col;
                    if (factorBorde > 0.85f) col = azulProfundo;
                    else if (factorBorde > 0.45f) col = Color.Lerp(azulProfundo, azulCristal, (1f - factorBorde) / 0.4f);
                    else if (factorBorde > 0.18f) col = Color.Lerp(azulCristal, azulCielo, (0.45f - factorBorde) / 0.27f);
                    else col = Color.Lerp(azulCielo, blancoNucleo, (0.18f - factorBorde) / 0.18f);

                    pixeles[y * width + x] = col;
                }
            }
        }

        tex.SetPixels(pixeles);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 16f);
    }

    private static void ColocarRisco(Sprite sp, Vector2 pos, Transform parent, bool conColision)
    {
        GameObject go = new GameObject($"Risco_{pos.x:0.0}_{pos.y:0.0}");
        go.transform.SetParent(parent);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10f);
        if (conColision)
        {
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.8f, 1.2f);
            col.offset = new Vector2(0f, -0.3f);
        }
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
}
