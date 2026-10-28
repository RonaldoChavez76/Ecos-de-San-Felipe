using UnityEngine;

/// <summary>
/// Constructor modular del Interior de la Parroquia de San Felipe Apóstol (Nivel 1 - Zona Interior).
/// Incluye nave colonial, bancas, vitrales luminosos, guardias corrompidos por la niebla,
/// inscripción litúrgica, el Acertijo de las Tres Campanas de la Luz (BellLightPuzzle),
/// el Primer Eco (Eco de la Fe) y la puerta de retorno al Atrio Exterior.
/// </summary>
public class ParroquiaInteriorBuilder : MonoBehaviour
{
    public static GameObject ConstruirInteriorParroquia(Transform parentRoot = null)
    {
        GameObject existente = GameObject.Find("Zona_Parroquia_Interior");
        if (existente != null) return existente;

        GameObject root = new GameObject("Zona_Parroquia_Interior");
        if (parentRoot != null) root.transform.SetParent(parentRoot);

        Vector2 centro = new Vector2(0f, 61f);

        // 1. SUELO DE CANTERA Y ALFOMBRA LITÚRGICA ROJA/DORADA
        GameObject pisoRoot = new GameObject("Piso_Nave_Parroquia");
        pisoRoot.transform.SetParent(root.transform);

        Sprite spLosaCantera = CrearSpriteLosa(new Color(0.68f, 0.62f, 0.54f), new Color(0.55f, 0.48f, 0.40f));
        Sprite spAlfombraReal = CrearSpriteLosa(new Color(0.68f, 0.16f, 0.20f), new Color(0.88f, 0.72f, 0.25f));
        Sprite spAltarPiso = CrearSpriteLosa(new Color(0.82f, 0.76f, 0.62f), new Color(0.95f, 0.85f, 0.45f));

        for (int x = -8; x <= 8; x++)
        {
            for (int y = 53; y <= 70; y++)
            {
                Sprite sp = spLosaCantera;
                if (Mathf.Abs(x) <= 1 && y >= 53 && y <= 67) sp = spAlfombraReal;
                else if (y >= 67) sp = spAltarPiso;

                GameObject tile = new GameObject($"Tile_Int_{x}_{y}");
                tile.transform.SetParent(pisoRoot.transform);
                tile.transform.position = new Vector3(x, y, 0f);
                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sortingOrder = -500;
            }
        }

        // 2. MUROS PERIMETRALES DE LA PARROQUIA Y COLUMNAS
        CrearMuroSolido("Muro_Norte_Altar", new Vector2(0f, 71.0f), new Vector2(18f, 1.5f), root.transform, new Color(0.38f, 0.32f, 0.28f));
        CrearMuroSolido("Muro_Oeste_Nave", new Vector2(-9.0f, 61.5f), new Vector2(1.5f, 19f), root.transform, new Color(0.38f, 0.32f, 0.28f));
        CrearMuroSolido("Muro_Este_Nave", new Vector2(9.0f, 61.5f), new Vector2(1.5f, 19f), root.transform, new Color(0.38f, 0.32f, 0.28f));
        CrearMuroSolido("Muro_Sur_Izq", new Vector2(-5.5f, 52.5f), new Vector2(7.5f, 1.5f), root.transform, new Color(0.38f, 0.32f, 0.28f));
        CrearMuroSolido("Muro_Sur_Der", new Vector2(5.5f, 52.5f), new Vector2(7.5f, 1.5f), root.transform, new Color(0.38f, 0.32f, 0.28f));

        // 3. BANCAS DE MADERA COLONIAL
        float[] filasBancas = { 56.0f, 58.5f, 61.0f };
        foreach (float yBanca in filasBancas)
        {
            CrearBancaParroquia($"Banca_Izq_{yBanca}", new Vector2(-4.5f, yBanca), root.transform);
            CrearBancaParroquia($"Banca_Der_{yBanca}", new Vector2(4.5f, yBanca), root.transform);
        }

        // 4. PUERTA DE SALIDA AL ATRIO EXTERIOR (CON ZONE TRANSITION)
        GameObject puertaSalida = new GameObject("Puerta_Salida_Al_Atrio");
        puertaSalida.transform.SetParent(root.transform);
        puertaSalida.transform.position = new Vector3(0f, 52.8f, 0f);

        SpriteRenderer srPuerta = puertaSalida.AddComponent<SpriteRenderer>();
        srPuerta.sprite = CrearSpritePortal(new Color(1f, 0.88f, 0.45f, 0.85f));
        srPuerta.sortingOrder = -450;
        puertaSalida.transform.localScale = new Vector3(2.2f, 1.0f, 1f);

        BoxCollider2D colSalida = puertaSalida.AddComponent<BoxCollider2D>();
        colSalida.isTrigger = true;
        colSalida.size = new Vector2(1.4f, 0.9f);

        ZoneTransition transicionSalida = puertaSalida.AddComponent<ZoneTransition>();
        transicionSalida.tipoTransicion = ZoneTransition.TransitionType.MismoMapaTeleport;
        transicionSalida.zonaDestino = LevelManager.ZoneId.ParroquiaExterior;
        transicionSalida.coordenadasDestino = new Vector2(0f, 12.8f);
        transicionSalida.direccionSalida = Vector2.down;

        // 5. ALTAR MAYOR, INSCRIPCIÓN DEL ACERTIJO Y TRES PEDESTALES DE CAMPANA
        GameObject altarMayor = CrearMuroSolido("Altar_Mayor_SanFelipe", new Vector2(0f, 69.2f), new Vector2(4.2f, 1.4f), root.transform, new Color(0.88f, 0.78f, 0.42f));

        // Placa con la pista del acertijo
        GameObject placaPista = new GameObject("Inscripcion_Liturgica_Campanas");
        placaPista.transform.SetParent(root.transform);
        placaPista.transform.position = new Vector3(0f, 63.6f, 0f);
        SpriteRenderer srPlaca = placaPista.AddComponent<SpriteRenderer>();
        srPlaca.sprite = CrearSpriteLosa(new Color(0.95f, 0.82f, 0.35f), new Color(0.55f, 0.42f, 0.15f));
        srPlaca.sortingOrder = Mathf.RoundToInt(-63.6f * 10);

        InteractableSignOrNPC signPista = placaPista.AddComponent<InteractableSignOrNPC>();
        signPista.speakerTitle = "Códice del Altar de San Felipe Apóstol";
        signPista.interactionPrompt = "[E] Leer Códice de las Campanas";
        signPista.dialoguePages = new string[]
        {
            "«Para disipar el Velo del Olvido que aprisiona el Eco de la Fe, haz resonar las Tres Campanas de la Luz siguiendo el curso del Sol:»",
            "«1° Toca la Campana del Alba al Poniente (Pedestal Izquierdo).»",
            "«2° Toca la Campana del Mediodía en el Cenit (Pedestal Central).»",
            "«3° Toca la Campana del Ocaso al Oriente (Pedestal Derecho).»"
        };

        // 6. PRIMER ECO: ECO DE LA FE (Oculto tras barrera de sombra hasta resolver BellLightPuzzle)
        GameObject ecoFe = new GameObject("Primer_Eco_De_La_Fe");
        ecoFe.transform.SetParent(root.transform);
        ecoFe.transform.position = new Vector3(0f, 68.0f, 0f);
        CircleCollider2D colEco = ecoFe.AddComponent<CircleCollider2D>();
        colEco.isTrigger = true;
        colEco.radius = 0.65f;
        EchoCollectible echoComp = ecoFe.AddComponent<EchoCollectible>();
        echoComp.indiceEco = 1;
        echoComp.nombreEco = "Eco de la Fe (Parroquia)";
        ecoFe.SetActive(false);

        // Barrera de Sombra sobre el Altar
        GameObject barreraSombra = new GameObject("Barrera_Sombra_Altar");
        barreraSombra.transform.SetParent(root.transform);
        barreraSombra.transform.position = new Vector3(0f, 68.0f, 0f);
        SpriteRenderer srBarrera = barreraSombra.AddComponent<SpriteRenderer>();
        srBarrera.sprite = CrearSpritePortal(new Color(0.45f, 0.1f, 0.65f, 0.85f));
        srBarrera.sortingOrder = 15;
        barreraSombra.transform.localScale = new Vector3(2.2f, 2.2f, 1f);

        // Controlador del Acertijo de las Campanas
        BellLightPuzzle puzzleCampanas = root.AddComponent<BellLightPuzzle>();
        puzzleCampanas.barreraSombraAltar = barreraSombra;
        puzzleCampanas.ecoDeLaFeObjeto = ecoFe;

        BellPedestalNode ped1 = CrearPedestalCampana(1, "Campana del Alba [1]", new Vector2(-4.2f, 65.5f), puzzleCampanas, root.transform);
        BellPedestalNode ped2 = CrearPedestalCampana(2, "Campana del Mediodía [2]", new Vector2(0.0f, 66.2f), puzzleCampanas, root.transform);
        BellPedestalNode ped3 = CrearPedestalCampana(3, "Campana del Ocaso [3]", new Vector2(4.2f, 65.5f), puzzleCampanas, root.transform);

        puzzleCampanas.pedestales.Add(ped1);
        puzzleCampanas.pedestales.Add(ped2);
        puzzleCampanas.pedestales.Add(ped3);

        // 7. GUARDIANES CORROMPIDOS DEL INTERIOR DE LA PARROQUIA
        CrearEnemigoInterior<CorruptedGuardAI>("Guardia_Corrompido_Nave_Izq", "Assets/Sprites/Human-Soldier-Cyan.png", new Vector2(-5.5f, 60.0f), root.transform);
        CrearEnemigoInterior<OblivionMageAI>("Mago_Del_Olvido_Nave_Der", "Assets/Sprites/Mage-Red.png", new Vector2(5.5f, 62.0f), root.transform);

        return root;
    }

    private static BellPedestalNode CrearPedestalCampana(int orden, string nombre, Vector2 pos, BellLightPuzzle puzzle, Transform parent)
    {
        GameObject go = new GameObject($"Pedestal_Campana_{orden}");
        go.transform.SetParent(parent);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CrearSpriteLosa(new Color(0.85f, 0.78f, 0.35f), new Color(0.45f, 0.35f, 0.15f));
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10);

        GameObject halo = new GameObject("Halo_Luz");
        halo.transform.SetParent(go.transform);
        halo.transform.localPosition = Vector3.zero;
        halo.transform.localScale = Vector3.one * 2.2f;
        SpriteRenderer srHalo = halo.AddComponent<SpriteRenderer>();
        srHalo.sprite = CrearSpritePortal(new Color(1f, 0.92f, 0.35f, 0.65f));
        srHalo.sortingOrder = sr.sortingOrder - 1;

        BellPedestalNode node = go.AddComponent<BellPedestalNode>();
        node.ordenCampana = orden;
        node.nombreCampana = nombre;
        node.puzzlePadre = puzzle;
        node.haloRenderer = srHalo;

        return node;
    }

    private static void CrearEnemigoInterior<T>(string nombre, string assetPath, Vector2 pos, Transform parent) where T : BaseEnemyAI
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;

        Texture2D tex = null;
#if UNITY_EDITOR
        tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
#endif
        EnemySpriteSheetAnimator anim = go.AddComponent<EnemySpriteSheetAnimator>();
        if (tex != null)
        {
            anim.spriteSheet = tex;
        }
        else
        {
            sr.sprite = CrearSpritePortal(new Color(0.85f, 0.25f, 0.35f, 1f));
        }

        go.AddComponent<T>();
    }

    private static GameObject CrearMuroSolido(string nombre, Vector2 pos, Vector2 size, Transform parent, Color color)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CrearSpriteLosa(color, color * 0.75f);
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10);

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
        return go;
    }

    private static void CrearBancaParroquia(string nombre, Vector2 pos, Transform parent)
    {
        CrearMuroSolido(nombre, pos, new Vector2(3.2f, 0.85f), parent, new Color(0.46f, 0.28f, 0.14f));
    }

    private static Sprite CrearSpriteLosa(Color fill, Color border)
    {
        Texture2D tex = new Texture2D(16, 16);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                bool isBorder = (x == 0 || y == 0 || x == 15 || y == 15);
                tex.SetPixel(x, y, isBorder ? border : fill);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
    }

    private static Sprite CrearSpritePortal(Color color)
    {
        Texture2D tex = new Texture2D(32, 32);
        tex.filterMode = FilterMode.Point;
        Vector2 c = new Vector2(15.5f, 15.5f);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / 15.5f;
                float a = Mathf.Clamp01(1f - d) * color.a;
                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16f);
    }
}
