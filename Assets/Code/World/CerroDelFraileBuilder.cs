using UnityEngine;

/// <summary>
/// Constructor modular del Nivel 3: El Cerro del Fraile y Mirador de la Cruz Azul.
/// Construye el sendero rocoso de ascenso y la cumbre (centrada en X=56, Y=2),
/// la monumental Cruz Azul luminosa, el Acertijo de los 3 Sellos de la Cruz Azul (BlueCrossShrinePuzzle),
/// enemigos de montaña (Gusanos de Cantera, Orcos, Magos) y el Jefe Final: La Sombra Guardiana.
/// </summary>
public class CerroDelFraileBuilder : MonoBehaviour
{
    public static GameObject ConstruirZonaCerroDelFraile(Transform parentRoot = null)
    {
        GameObject existente = GameObject.Find("Zona_Cerro_Del_Fraile");
        if (existente != null) return existente;

        GameObject root = new GameObject("Zona_Cerro_Del_Fraile");
        if (parentRoot != null) root.transform.SetParent(parentRoot);

        // 1. SUELO ROCOSO DEL SENDERO Y EXPLANADA DEL MIRADOR (X: 45..67, Y: -11..15)
        GameObject pisoRoot = new GameObject("Piso_CerroDelFraile");
        pisoRoot.transform.SetParent(root.transform);

        Sprite spRocaMontana = CrearSpriteLosa(new Color(0.46f, 0.44f, 0.42f), new Color(0.32f, 0.30f, 0.28f));
        Sprite spSenderoAscenso = CrearSpriteLosa(new Color(0.62f, 0.56f, 0.46f), new Color(0.48f, 0.42f, 0.34f));
        Sprite spMiradorSagrado = CrearSpriteLosa(new Color(0.38f, 0.52f, 0.64f), new Color(0.22f, 0.72f, 0.92f));

        for (int x = 45; x <= 67; x++)
        {
            for (int y = -11; y <= 15; y++)
            {
                Sprite sp = spRocaMontana;
                if (Mathf.Abs(x - 56) <= 2 && y < 4) sp = spSenderoAscenso;
                else if (Vector2.Distance(new Vector2(x, y), new Vector2(56f, 7f)) <= 6.5f) sp = spMiradorSagrado;

                GameObject tile = new GameObject($"Tile_Cerro_{x}_{y}");
                tile.transform.SetParent(pisoRoot.transform);
                tile.transform.position = new Vector3(x, y, 0f);
                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sortingOrder = -500;
            }
        }

        // 2. RISCOS Y MUROS PERIMETRALES DEL CERRO DEL FRAILE
        CrearBloqueRoca("Risco_Norte_Cumbre", new Vector2(56.0f, 16.0f), new Vector2(24f, 1.5f), root.transform, new Color(0.28f, 0.30f, 0.35f));
        CrearBloqueRoca("Risco_Sur_Base", new Vector2(56.0f, -12.0f), new Vector2(24f, 1.5f), root.transform, new Color(0.28f, 0.30f, 0.35f));
        CrearBloqueRoca("Risco_Este_Barranco", new Vector2(68.0f, 2.0f), new Vector2(1.5f, 28f), root.transform, new Color(0.28f, 0.30f, 0.35f));
        CrearBloqueRoca("Risco_Oeste_Sup", new Vector2(44.0f, 5.5f), new Vector2(1.5f, 21f), root.transform, new Color(0.28f, 0.30f, 0.35f));
        CrearBloqueRoca("Risco_Oeste_Inf", new Vector2(44.0f, -10.5f), new Vector2(1.5f, 3f), root.transform, new Color(0.28f, 0.30f, 0.35f));

        // 3. PORTAL DE REGRESO A LA PLAZA PRINCIPAL DE SAN FELIPE
        ParroquiaExteriorBuilder.CrearPortalTransicion(
            "Portal_Regreso_Cerro_A_Plaza",
            new Vector2(45.2f, -8.0f),
            new Vector2(1.6f, 2.8f),
            LevelManager.ZoneId.ParroquiaExterior,
            new Vector2(11.0f, 0.0f),
            new Color(0.25f, 0.85f, 1.0f, 0.80f),
            root.transform);

        // Placa del Mirador y Pista de los Sellos de la Cruz Azul
        GameObject placaMirador = CrearBloqueRoca("Estela_Mirador_CruzAzul", new Vector2(53.0f, -6.5f), new Vector2(1.1f, 1.1f), root.transform, new Color(0.25f, 0.80f, 0.98f));
        InteractableSignOrNPC signMirador = placaMirador.AddComponent<InteractableSignOrNPC>();
        signMirador.speakerTitle = "Estela del Cerro del Fraile y la Cruz Azul";
        signMirador.interactionPrompt = "[E] Leer Estela del Cerro del Fraile";
        signMirador.dialoguePages = new string[]
        {
            "«En la cumbre del Cerro del Fraile se erige la monumental Cruz Azul, faro protector de San Felipe.»",
            "«La Sombra Guardiana ha corrompido los Tres Sellos del Mirador para protegerse con un escudo abismal.»",
            "«Purifica los 3 Sellos alrededor de la cumbre golpeándolos con tu espada [J], presionando [E] o reflejando proyectiles hacia ellos para romper el escudo del Jefe Final y recuperar el Eco de la Memoria.»"
        };

        // 4. MONUMENTAL CRUZ AZUL EN LA CIMA DEL CERRO DEL FRAILE (56, 13.0)
        GameObject cruzRoot = new GameObject("Monumento_Cruz_Azul");
        cruzRoot.transform.SetParent(root.transform);
        cruzRoot.transform.position = new Vector3(56.0f, 13.2f, 0f);

        GameObject haloCruz = new GameObject("Halo_Luminoso_CruzAzul");
        haloCruz.transform.SetParent(cruzRoot.transform);
        haloCruz.transform.localPosition = Vector3.zero;
        haloCruz.transform.localScale = Vector3.one * 3.2f;
        SpriteRenderer srHaloCruz = haloCruz.AddComponent<SpriteRenderer>();
        srHaloCruz.sprite = CrearSpriteRadial(new Color(0.15f, 0.85f, 1.0f, 0.55f));
        srHaloCruz.sortingOrder = -400;

        CrearBloqueRoca("Pedestal_Cruz_Azul", new Vector2(56.0f, 11.8f), new Vector2(2.6f, 0.9f), cruzRoot.transform, new Color(0.30f, 0.45f, 0.65f));
        CrearBloqueRoca("Poste_Vertical_CruzAzul", new Vector2(56.0f, 13.4f), new Vector2(0.75f, 3.2f), cruzRoot.transform, new Color(0.10f, 0.82f, 1.0f));
        CrearBloqueRoca("Brazo_Horizontal_CruzAzul", new Vector2(56.0f, 13.9f), new Vector2(2.4f, 0.65f), cruzRoot.transform, new Color(0.10f, 0.82f, 1.0f));

        // 5. JEFE FINAL: LA SOMBRA GUARDIANA (56, 5.8)
        SombraGuardianaBossAI bossAI = ConstruirJefeSombraGuardiana(new Vector2(56.0f, 5.8f), root.transform, out GameObject escudoVisual);

        // 6. ACERTIJO DE LOS TRES SELLOS DE LA CRUZ AZUL
        BlueCrossShrinePuzzle puzzleCruz = root.AddComponent<BlueCrossShrinePuzzle>();
        puzzleCruz.jefeSombraGuardiana = bossAI;
        puzzleCruz.cruzAzulHaloRenderer = srHaloCruz;
        puzzleCruz.escudoVisualJefe = escudoVisual;

        BlueCrossSealNode s1 = CrearSelloCruzAzul("Sello de la Cantera Poniente [1]", new Vector2(49.5f, 6.2f), puzzleCruz, root.transform);
        BlueCrossSealNode s2 = CrearSelloCruzAzul("Sello del Viento Norte [2]", new Vector2(56.0f, 10.2f), puzzleCruz, root.transform);
        BlueCrossSealNode s3 = CrearSelloCruzAzul("Sello del Valle Oriente [3]", new Vector2(62.5f, 6.2f), puzzleCruz, root.transform);

        puzzleCruz.sellos.Add(s1);
        puzzleCruz.sellos.Add(s2);
        puzzleCruz.sellos.Add(s3);

        // 7. ENEMIGOS DEL ASCENSO AL CERRO DEL FRAILE
        CrearEnemigoCerro<QuarryWormEnemyAI>("Gusano_Cantera_Ascenso_1", "Assets/Sprites/worm_spritesheet.png", new Vector2(51.5f, -4.5f), root.transform);
        CrearEnemigoCerro<QuarryWormEnemyAI>("Gusano_Cantera_Ascenso_2", "Assets/Sprites/worm_spritesheet.png", new Vector2(60.5f, -3.5f), root.transform);
        CrearEnemigoCerro<DarkOrcBruteAI>("Orco_Bruto_Paso_Cerro", "Assets/Sprites/Orc-Grunt.png", new Vector2(56.0f, -1.5f), root.transform);
        CrearEnemigoCerro<OblivionMageAI>("Mago_Del_Olvido_Cumbre", "Assets/Sprites/Mage-Red.png", new Vector2(61.5f, 1.5f), root.transform);

        return root;
    }

    private static SombraGuardianaBossAI ConstruirJefeSombraGuardiana(Vector2 pos, Transform parent, out GameObject escudoVisual)
    {
        GameObject bossGO = new GameObject("JefeFinal_La_Sombra_Guardiana");
        bossGO.transform.SetParent(parent);
        bossGO.transform.position = pos;

        SpriteRenderer sr = bossGO.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 8;

        RuntimeAnimatorController animCtrl = null;
        Sprite spBoss = null;
#if UNITY_EDITOR
        animCtrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/SombraGuardiana.controller");
        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/SombraGuardiana_SpriteSheet.png");
        if (assets != null)
        {
            foreach (var a in assets)
            {
                if (a is Sprite s) { spBoss = s; break; }
            }
        }
#endif
        if (spBoss == null) spBoss = CrearSpriteRadial(new Color(0.85f, 0.15f, 0.95f, 1f));
        sr.sprite = spBoss;

        if (animCtrl != null)
        {
            Animator anim = bossGO.AddComponent<Animator>();
            anim.runtimeAnimatorController = animCtrl;
        }

        BlackCloverDemonRenderer demonRend = bossGO.AddComponent<BlackCloverDemonRenderer>();
        demonRend.tipoDemonio = BlackCloverDemonRenderer.DemonType.SombraGuardianaJefe;

        SombraGuardianaBossAI bossAI = bossGO.AddComponent<SombraGuardianaBossAI>();
        bossAI.isShieldedBySeals = true;
        bossAI.echoIndexOnDefeat = 3;

        escudoVisual = new GameObject("Escudo_Abismal_Sellos");
        escudoVisual.transform.SetParent(bossGO.transform);
        escudoVisual.transform.localPosition = Vector3.zero;
        escudoVisual.transform.localScale = Vector3.one * 2.2f;
        SpriteRenderer srEscudo = escudoVisual.AddComponent<SpriteRenderer>();
        srEscudo.sprite = CrearSpriteRadial(new Color(0.65f, 0.12f, 0.95f, 0.45f));
        srEscudo.sortingOrder = 9;

        return bossAI;
    }

    private static BlueCrossSealNode CrearSelloCruzAzul(string nombre, Vector2 pos, BlueCrossShrinePuzzle puzzle, Transform parent)
    {
        GameObject go = new GameObject(nombre.Replace(" ", "_"));
        go.transform.SetParent(parent);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CrearSpriteLosa(new Color(0.45f, 0.25f, 0.65f), new Color(0.25f, 0.85f, 1.0f));
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10);

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.85f;

        GameObject flama = new GameObject("Flama_Sello");
        flama.transform.SetParent(go.transform);
        flama.transform.localPosition = Vector3.zero;
        flama.transform.localScale = Vector3.one * 2.1f;
        SpriteRenderer srFlama = flama.AddComponent<SpriteRenderer>();
        srFlama.sprite = CrearSpriteRadial(new Color(0.65f, 0.15f, 0.85f, 0.75f));
        srFlama.sortingOrder = sr.sortingOrder - 1;

        BlueCrossSealNode node = go.AddComponent<BlueCrossSealNode>();
        node.nombreSello = nombre;
        node.puzzlePadre = puzzle;
        node.flamaRenderer = srFlama;
        return node;
    }

    private static void CrearEnemigoCerro<T>(string nombre, string assetPath, Vector2 pos, Transform parent) where T : BaseEnemyAI
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
            sr.sprite = CrearSpriteRadial(new Color(0.75f, 0.25f, 0.35f, 1f));
        }

        go.AddComponent<T>();
    }

    private static GameObject CrearBloqueRoca(string nombre, Vector2 pos, Vector2 size, Transform parent, Color color)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CrearSpriteLosa(color, color * 0.72f);
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
