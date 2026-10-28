using UnityEngine;

/// <summary>
/// Constructor modular del Nivel 2: El Mercado Municipal y Callejones de San Felipe.
/// Construye la explanada del mercado artesanal (centrada en X=-58, Y=0), puestos de trueque,
/// el Acertijo de Intercambio del Gremio (MarketTradePuzzle), el Segundo Eco (Eco de la Tradición),
/// transiciones hacia la Plaza y el Cerro del Fraile, y enemigos variados (Goblins, Bombers, Orcos, Magos).
/// </summary>
public class MercadoBuilder : MonoBehaviour
{
    public static GameObject ConstruirZonaMercado(Transform parentRoot = null)
    {
        GameObject existente = GameObject.Find("Zona_Mercado_Municipal");
        if (existente != null) return existente;

        GameObject root = new GameObject("Zona_Mercado_Municipal");
        if (parentRoot != null) root.transform.SetParent(parentRoot);

        // Conectar también el letrero del mercado en la plaza exterior si existe
        GameObject letreroExt = GameObject.Find("Letrero_Mercado");
        if (letreroExt != null && letreroExt.GetComponent<InteractableSignOrNPC>() == null)
        {
            InteractableSignOrNPC signExt = letreroExt.AddComponent<InteractableSignOrNPC>();
            signExt.speakerTitle = "Arco del Mercado Municipal de San Felipe";
            signExt.interactionPrompt = "[E] Leer Directorio del Mercado";
            signExt.dialoguePages = new string[]
            {
                "«Hacia el Poniente (Oeste) se extiende el Gran Mercado Municipal de Artesanos y Alfareros.»",
                "«Avanza hasta el portal naranja del extremo Oeste (-15, 0) para adentrarte en los callejones del Mercado y recuperar el Eco de la Tradición.»"
            };
        }

        // 1. SUELO EMPEDRADO Y TERRACOTA DEL MERCADO (-70.. -45, -10.. 10)
        GameObject pisoRoot = new GameObject("Piso_Mercado");
        pisoRoot.transform.SetParent(root.transform);

        Sprite spTerracota = CrearSpriteLosa(new Color(0.76f, 0.54f, 0.36f), new Color(0.60f, 0.40f, 0.25f));
        Sprite spPasilloPiedra = CrearSpriteLosa(new Color(0.62f, 0.58f, 0.52f), new Color(0.46f, 0.42f, 0.38f));

        for (int x = -70; x <= -45; x++)
        {
            for (int y = -10; y <= 10; y++)
            {
                bool esPasillo = (Mathf.Abs(y) <= 1) || (Mathf.Abs(x - (-58)) <= 1);
                GameObject tile = new GameObject($"Tile_Merc_{x}_{y}");
                tile.transform.SetParent(pisoRoot.transform);
                tile.transform.position = new Vector3(x, y, 0f);
                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = esPasillo ? spPasilloPiedra : spTerracota;
                sr.sortingOrder = -500;
            }
        }

        // 2. MUROS PERIMETRALES DEL MERCADO
        CrearBloqueSolido("Muro_Merc_Norte_Izq", new Vector2(-65.0f, 11.0f), new Vector2(12f, 1.5f), root.transform, new Color(0.52f, 0.36f, 0.22f));
        CrearBloqueSolido("Muro_Merc_Norte_Der", new Vector2(-51.0f, 11.0f), new Vector2(12f, 1.5f), root.transform, new Color(0.52f, 0.36f, 0.22f));
        CrearBloqueSolido("Muro_Merc_Sur", new Vector2(-58.0f, -11.0f), new Vector2(26f, 1.5f), root.transform, new Color(0.52f, 0.36f, 0.22f));
        CrearBloqueSolido("Muro_Merc_Oeste", new Vector2(-71.0f, 0.0f), new Vector2(1.5f, 22f), root.transform, new Color(0.52f, 0.36f, 0.22f));
        CrearBloqueSolido("Muro_Merc_Este_Sup", new Vector2(-44.5f, 6.5f), new Vector2(1.5f, 9f), root.transform, new Color(0.52f, 0.36f, 0.22f));
        CrearBloqueSolido("Muro_Merc_Este_Inf", new Vector2(-44.5f, -6.5f), new Vector2(1.5f, 9f), root.transform, new Color(0.52f, 0.36f, 0.22f));

        // 3. PORTALES DE TRANSICIÓN (REGRESO A PLAZA PRINCIPAL Y PASO AL CERRO DEL FRAILE)
        ParroquiaExteriorBuilder.CrearPortalTransicion(
            "Portal_Regreso_A_Plaza",
            new Vector2(-45.2f, 0.0f),
            new Vector2(1.6f, 3.0f),
            LevelManager.ZoneId.ParroquiaExterior,
            new Vector2(-13.5f, 0.0f),
            new Color(0.95f, 0.75f, 0.30f, 0.80f),
            root.transform);

        ParroquiaExteriorBuilder.CrearPortalTransicion(
            "Portal_Mercado_A_CerroDelFraile",
            new Vector2(-58.0f, 10.2f),
            new Vector2(3.0f, 1.5f),
            LevelManager.ZoneId.CerroDelFraile,
            new Vector2(46.0f, -8.0f),
            new Color(0.25f, 0.85f, 1.0f, 0.80f),
            root.transform);

        // 4. ACERTIJO DE TRUEQUE E INTERCAMBIO DEL MERCADO Y SEGUNDO ECO (ECO DE LA TRADICIÓN)
        MarketTradePuzzle tradePuzzle = root.AddComponent<MarketTradePuzzle>();

        GameObject ecoTradicion = new GameObject("Segundo_Eco_De_La_Tradicion");
        ecoTradicion.transform.SetParent(root.transform);
        ecoTradicion.transform.position = new Vector3(-58.0f, 2.8f, 0f);
        CircleCollider2D colEco = ecoTradicion.AddComponent<CircleCollider2D>();
        colEco.isTrigger = true;
        colEco.radius = 0.65f;
        EchoCollectible echoComp = ecoTradicion.AddComponent<EchoCollectible>();
        echoComp.indiceEco = 2;
        echoComp.nombreEco = "Eco de la Tradición (Mercado)";
        ecoTradicion.SetActive(false);

        GameObject barreraCofre = new GameObject("Sello_Gremial_Cofre");
        barreraCofre.transform.SetParent(root.transform);
        barreraCofre.transform.position = new Vector3(-58.0f, 1.2f, 0f);
        SpriteRenderer srSello = barreraCofre.AddComponent<SpriteRenderer>();
        srSello.sprite = CrearSpriteRadial(new Color(1f, 0.75f, 0.2f, 0.55f));
        srSello.sortingOrder = 2;
        barreraCofre.transform.localScale = Vector3.one * 2.0f;

        tradePuzzle.ecoDeLaTradicionObjeto = ecoTradicion;
        tradePuzzle.barreraCofreGremio = barreraCofre;

        // Nodo 0: Arcón de Suministros de Cobre (Sur)
        CrearPuestoTrueque(
            "Arcon_Suministros_Cobre",
            "Assets/Sprites/LittleTreasureChest.png",
            new Vector2(-58.0f, -7.5f),
            new Color(0.85f, 0.52f, 0.25f),
            MarketStallTradeNode.NodeRole.SupplyCrateCopper,
            "[E] Inspeccionar Arcón de la Fragua",
            tradePuzzle,
            root.transform);

        // Nodo 1: Herrería de Don Braulio (Noroeste)
        CrearBloqueSolido("Toldo_Herreria", new Vector2(-64.5f, 6.2f), new Vector2(3.4f, 1.4f), root.transform, new Color(0.75f, 0.25f, 0.20f));
        CrearPuestoTrueque(
            "Puesto_Herrero_DonBraulio",
            "Assets/Sprites/BarCopper.png",
            new Vector2(-64.5f, 4.6f),
            new Color(0.90f, 0.45f, 0.20f),
            MarketStallTradeNode.NodeRole.BlacksmithStall,
            "[E] Herrero Don Braulio (Trueque)",
            tradePuzzle,
            root.transform);

        // Nodo 2: Alfarería de Doña Remedios (Noreste)
        CrearBloqueSolido("Toldo_Alfareria", new Vector2(-51.5f, 6.2f), new Vector2(3.4f, 1.4f), root.transform, new Color(0.20f, 0.65f, 0.55f));
        CrearPuestoTrueque(
            "Puesto_Alfarera_DonaRemedios",
            "Assets/Sprites/BarMithril.png",
            new Vector2(-51.5f, 4.6f),
            new Color(0.30f, 0.85f, 0.75f),
            MarketStallTradeNode.NodeRole.PotterStall,
            "[E] Alfarera Doña Remedios (Trueque)",
            tradePuzzle,
            root.transform);

        // Nodo 3: Joyería de Don Teodoro (Suroeste)
        CrearBloqueSolido("Toldo_Joyeria", new Vector2(-64.5f, -4.2f), new Vector2(3.4f, 1.4f), root.transform, new Color(0.55f, 0.25f, 0.75f));
        CrearPuestoTrueque(
            "Puesto_Joyero_DonTeodoro",
            "Assets/Sprites/GemGreen.png",
            new Vector2(-64.5f, -5.8f),
            new Color(0.25f, 0.90f, 0.45f),
            MarketStallTradeNode.NodeRole.JewelerStall,
            "[E] Joyero Don Teodoro (Trueque)",
            tradePuzzle,
            root.transform);

        // Nodo 4: Cofre Sagrado del Gremio (Centro)
        CrearPuestoTrueque(
            "Cofre_Sagrado_Del_Gremio",
            "Assets/Sprites/BigTreasureChest.png",
            new Vector2(-58.0f, 1.2f),
            new Color(0.98f, 0.82f, 0.25f),
            MarketStallTradeNode.NodeRole.GuildTreasureChest,
            "[E] Abrir Cofre Sagrado del Gremio",
            tradePuzzle,
            root.transform);

        // Letrero de pistas del Mercado
        GameObject letreroPista = CrearBloqueSolido("Tablon_Gremio_Artesanos", new Vector2(-53.5f, -4.5f), new Vector2(1.2f, 1.2f), root.transform, new Color(0.85f, 0.68f, 0.32f));
        InteractableSignOrNPC signGremio = letreroPista.AddComponent<InteractableSignOrNPC>();
        signGremio.speakerTitle = "Tablón del Gremio de Artesanos de San Felipe";
        signGremio.interactionPrompt = "[E] Leer Orden del Trueque";
        signGremio.dialoguePages = new string[]
        {
            "«ORDEN TRADICIONAL DEL TRUEQUE DE SAN FELIPE:»",
            "«1° Toma el Lingote de Cobre del Arcón de Suministros al Sur (-58, -7.5).»",
            "«2° Cámbialo con Don Braulio (Herrería Noroeste) por el Lingote Templado; llévalo a Doña Remedios (Alfarería Noreste) por la Esmeralda; y entrégala a Don Teodoro (Joyería Suroeste) para recibir la Llave Dorada del Gremio.»"
        };

        // 5. ENEMIGOS VARIADOS DEL NIVEL 2 (MERCADO Y CALLEJONES)
        CrearEnemigoMercado<GoblinEnemyAI>("Goblin_Saqueador_Merc_1", "Assets/Sprites/goblin_spritesheet.png", new Vector2(-52.5f, -1.5f), root.transform);
        CrearEnemigoMercado<GoblinEnemyAI>("Goblin_Saqueador_Merc_2", "Assets/Sprites/goblin_spritesheet.png", new Vector2(-63.5f, 0.5f), root.transform);
        CrearEnemigoMercado<GoblinBomberEnemyAI>("Goblin_Bombardero_Merc_1", "Assets/Sprites/goblin_bomber_spritesheet.png", new Vector2(-61.5f, 6.8f), root.transform);
        CrearEnemigoMercado<GoblinBomberEnemyAI>("Goblin_Bombardero_Merc_2", "Assets/Sprites/goblin_bomber_spritesheet.png", new Vector2(-54.5f, -6.8f), root.transform);
        CrearEnemigoMercado<DarkOrcBruteAI>("Orco_Bruto_Guardian_Cofre", "Assets/Sprites/Orc-Soldier-Red.png", new Vector2(-58.0f, -2.0f), root.transform);
        CrearEnemigoMercado<OblivionMageAI>("Mago_Del_Olvido_Callejon_Norte", "Assets/Sprites/Mage-Cyan.png", new Vector2(-58.0f, 7.5f), root.transform);

        return root;
    }

    private static MarketStallTradeNode CrearPuestoTrueque(
        string nombre,
        string assetPath,
        Vector2 pos,
        Color fallbackColor,
        MarketStallTradeNode.NodeRole role,
        string prompt,
        MarketTradePuzzle puzzle,
        Transform parent)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10);

        Texture2D tex = null;
#if UNITY_EDITOR
        tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
#endif
        if (tex != null)
        {
            int w = Mathf.Min(16, tex.width);
            int h = Mathf.Min(16, tex.height);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 14f);
        }
        else
        {
            sr.sprite = CrearSpriteLosa(fallbackColor, fallbackColor * 0.7f);
        }

        MarketStallTradeNode node = go.AddComponent<MarketStallTradeNode>();
        node.role = role;
        node.labelPrompt = prompt;
        node.puzzlePadre = puzzle;
        return node;
    }

    private static void CrearEnemigoMercado<T>(string nombre, string assetPath, Vector2 pos, Transform parent) where T : BaseEnemyAI
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
            sr.sprite = CrearSpriteRadial(new Color(0.9f, 0.35f, 0.2f, 1f));
        }

        go.AddComponent<T>();
    }

    private static GameObject CrearBloqueSolido(string nombre, Vector2 pos, Vector2 size, Transform parent, Color color)
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

    private static Sprite CrearSpriteRadial(Color color)
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

