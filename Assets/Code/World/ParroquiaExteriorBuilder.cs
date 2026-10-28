using UnityEngine;

/// <summary>
/// Constructor modular de elementos interactivos, transiciones de zona, NPCs, letreros
/// y variedad de enemigos del Nivel 1 Exterior: Plaza Principal y Atrio de la Parroquia.
/// </summary>
public class ParroquiaExteriorBuilder : MonoBehaviour
{
    public static GameObject ConstruirElementosExteriorParroquia(Transform parentRoot = null)
    {
        GameObject existente = GameObject.Find("Zona_Parroquia_Exterior_Interactivos");
        if (existente != null) return existente;

        GameObject root = new GameObject("Zona_Parroquia_Exterior_Interactivos");
        if (parentRoot != null) root.transform.SetParent(parentRoot);

        // 1. PUERTA DE ENTRADA AL INTERIOR DE LA PARROQUIA (NORTE: 0, 14.0 -> 0, 54.5)
        CrearPortalTransicion(
            "Puerta_Entrada_Parroquia",
            new Vector2(0f, 14.0f),
            new Vector2(2.2f, 1.2f),
            LevelManager.ZoneId.ParroquiaInterior,
            new Vector2(0f, 54.5f),
            new Color(1f, 0.90f, 0.40f, 0.80f),
            root.transform);

        // 2. ARCO OESTE: TRANSICIÓN AL NIVEL 2 (MERCADO MUNICIPAL Y CALLEJONES)
        CrearPortalTransicion(
            "Portal_Hacia_Mercado_Oeste",
            new Vector2(-15.2f, 0f),
            new Vector2(1.5f, 3.0f),
            LevelManager.ZoneId.Mercado,
            new Vector2(-46.0f, 0f),
            new Color(0.95f, 0.65f, 0.25f, 0.75f),
            root.transform);

        // 3. SENDERO ESTE: TRANSICIÓN AL NIVEL 3 (CERRO DEL FRAILE Y CRUZ AZUL)
        CrearPortalTransicion(
            "Portal_Hacia_CerroDelFraile_Este",
            new Vector2(12.5f, 0f),
            new Vector2(1.5f, 3.0f),
            LevelManager.ZoneId.CerroDelFraile,
            new Vector2(46.0f, -8.0f),
            new Color(0.25f, 0.80f, 1.0f, 0.75f),
            root.transform);

        // 4. CONECTAR LEtreros EXISTENTES O CREAR NPCS DE HISTORIA Y PISTAS
        GameObject letreroTut = GameObject.Find("Letrero_Tutorial");
        if (letreroTut != null && letreroTut.GetComponent<InteractableSignOrNPC>() == null)
        {
            InteractableSignOrNPC sign = letreroTut.AddComponent<InteractableSignOrNPC>();
            ConfigurarLetreroTutorial(sign);
        }
        else if (letreroTut == null)
        {
            GameObject nuevoTut = CrearObjetoNPC("Letrero_Tutorial_Plaza", "Assets/Sprites/TilesetElement.png", new Vector2(-2.5f, 1.5f), root.transform, new Color(0.85f, 0.65f, 0.30f));
            ConfigurarLetreroTutorial(nuevoTut.AddComponent<InteractableSignOrNPC>());
        }

        // NPC: Cronista Don Anselmo junto al Pozo Central
        GameObject donAnselmo = CrearObjetoNPC("NPC_Cronista_DonAnselmo", "Assets/Sprites/Mage-Cyan.png", new Vector2(2.4f, -1.8f), root.transform, new Color(0.35f, 0.85f, 0.95f));
        InteractableSignOrNPC npcAnselmo = donAnselmo.AddComponent<InteractableSignOrNPC>();
        npcAnselmo.speakerTitle = "Don Anselmo, Cronista de San Felipe";
        npcAnselmo.interactionPrompt = "[E] Hablar con Don Anselmo";
        npcAnselmo.dialoguePages = new string[]
        {
            "¡Ronan! Gracias al cielo que aún recuerdas quién eres. La Niebla del Olvido bajó del Cerro del Fraile y fragmentó el alma del pueblo en Tres Ecos Sagrados.",
            "Al Norte se alza la Parroquia de San Felipe Apóstol. Entra por su pórtico dorado y resuelve el Acertijo de las Tres Campanas para recuperar el Eco de la Fe.",
            "Al Oeste se extiende el Mercado Municipal, donde los artesanos guardan el Eco de la Tradición. Y al Este asciende el sendero hacia el Cerro del Fraile y la Cruz Azul, morada de La Sombra Guardiana."
        };

        // Placa histórica en el Atrio de la Parroquia
        GameObject placaAtrio = CrearObjetoNPC("Placa_Atrio_Parroquia", "Assets/Sprites/GemYellow.png", new Vector2(-2.4f, 13.0f), root.transform, new Color(0.95f, 0.85f, 0.35f));
        InteractableSignOrNPC signAtrio = placaAtrio.AddComponent<InteractableSignOrNPC>();
        signAtrio.speakerTitle = "Inscripción del Atrio Parroquial";
        signAtrio.interactionPrompt = "[E] Leer Inscripción del Atrio";
        signAtrio.dialoguePages = new string[]
        {
            "«Parroquia de San Felipe Apóstol (Torres Nuevas) — Corazón espiritual de Torres Mochas.»",
            "«Cruza el umbral luminoso del Norte para ingresar al templo y enfrentar la prueba de las Campanas de la Luz.»"
        };

        // 5. VARIEDAD DE ENEMIGOS DEL NIVEL 1 EXTERIOR
        CrearEnemigoExterior<MistSlimeEnemyAI>("Slime_De_Niebla_Plaza_Sur", "Assets/Sprites/slime_spritesheet.png", new Vector2(-4.5f, -5.5f), root.transform);
        CrearEnemigoExterior<MistSlimeEnemyAI>("Slime_De_Niebla_Plaza_Este", "Assets/Sprites/slime_spritesheet.png", new Vector2(5.2f, -3.0f), root.transform);
        CrearEnemigoExterior<SporeMushroomEnemyAI>("Hongo_Espora_Jardin_Oeste", "Assets/Sprites/mushroom_spritesheet.png", new Vector2(-7.5f, 2.5f), root.transform);
        CrearEnemigoExterior<GoblinEnemyAI>("Saqueador_Goblin_Callejon", "Assets/Sprites/goblin_spritesheet.png", new Vector2(3.8f, 9.5f), root.transform);

        return root;
    }

    private static void ConfigurarLetreroTutorial(InteractableSignOrNPC sign)
    {
        sign.speakerTitle = "Manual del Guardián Ronan";
        sign.interactionPrompt = "[E] Leer Controles y Mecánicas";
        sign.dialoguePages = new string[]
        {
            "MOVIMIENTO Y COMBATE: Usa [WASD / Flechas] para moverte en 8 direcciones. Presiona [J] o [Clic Izquierdo] para atacar con tu espada y [Espacio] para esquivar.",
            "CONTRAATAQUE PERFECTO (0.20s): Presiona [K] o [Clic Derecho] justo antes de que un proyectil te impacte (ventana de 0.20s) para devolverlo con daño amplificado y recargar tu energía.",
            "MODO ASALTO [L]: Cuando la barra de energía esté llena, presiona [L] para desatar el Modo Asalto (+60% Ataque y +30% Velocidad)."
        };
    }

    public static GameObject CrearPortalTransicion(
        string nombre,
        Vector2 pos,
        Vector2 size,
        LevelManager.ZoneId destino,
        Vector2 coordenadasDestino,
        Color colorLuz,
        Transform parent)
    {
        GameObject portal = new GameObject(nombre);
        portal.transform.SetParent(parent);
        portal.transform.position = pos;
        portal.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = portal.AddComponent<SpriteRenderer>();
        sr.sprite = CrearSpriteRadial(colorLuz);
        sr.sortingOrder = -420;

        BoxCollider2D col = portal.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.85f, 0.85f);

        ZoneTransition zt = portal.AddComponent<ZoneTransition>();
        zt.tipoTransicion = ZoneTransition.TransitionType.MismoMapaTeleport;
        zt.zonaDestino = destino;
        zt.coordenadasDestino = coordenadasDestino;

        return portal;
    }

    private static GameObject CrearObjetoNPC(string nombre, string assetPath, Vector2 pos, Transform parent, Color fallbackColor)
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
            sr.sprite = Sprite.Create(tex, new Rect(0, tex.height - h, w, h), new Vector2(0.5f, 0.5f), 16f);
        }
        else
        {
            sr.sprite = CrearSpriteRadial(fallbackColor);
        }

        return go;
    }

    private static void CrearEnemigoExterior<T>(string nombre, string assetPath, Vector2 pos, Transform parent) where T : BaseEnemyAI
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 4;

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
            sr.sprite = CrearSpriteRadial(new Color(0.85f, 0.3f, 0.3f, 1f));
        }

        go.AddComponent<T>();
    }

    private static Sprite CrearSpriteRadial(Color color)
    {
        Texture2D tex = new Texture2D(32, 32);
        tex.filterMode = FilterMode.Point;
        Vector2 center = new Vector2(15.5f, 15.5f);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / 15.5f;
                float a = Mathf.Clamp01(1f - dist) * color.a;
                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16f);
    }
}
