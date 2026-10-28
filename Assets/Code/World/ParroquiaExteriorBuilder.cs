using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Constructor modular de elementos interactivos, puerta de entrada a la escena ParroquiaInterior,
/// NPCs con recorte exacto de sprite (32x32), letreros y encuentros por Oleadas/Hordas (ZoneWaveSpawner)
/// para el Nivel 1 Exterior: Gran Villa y Atrio de la Parroquia de San Felipe.
/// </summary>
public class ParroquiaExteriorBuilder : MonoBehaviour
{
    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    public static GameObject ConstruirElementosExteriorParroquia(Transform parentRoot = null)
    {
        string[] nombresPrevios = { "Zona_Parroquia_Exterior_Interactivos", "Parroquia_Exterior_Elementos" };
        foreach (string nombrePrevio in nombresPrevios)
        {
            int guard = 0;
            GameObject existente = GameObject.Find(nombrePrevio);
            while (existente != null && guard < 10)
            {
                guard++;
                existente.name = "_Destroyed_" + nombrePrevio;
                existente.SetActive(false);
                if (Application.isPlaying) Destroy(existente);
                else DestroyImmediate(existente);
                existente = GameObject.Find(nombrePrevio);
            }
        }

        GameObject root = new GameObject("Zona_Parroquia_Exterior_Interactivos");
        if (parentRoot != null) root.transform.SetParent(parentRoot);

        // 1. PUERTA PRINCIPAL DE ENTRADA AL INTERIOR DE LA PARROQUIA (CAMBIO A ESCENA "ParroquiaInterior")
        GameObject puertaParroquia = CrearPortalTransicionEscena(
            "Puerta_Entrada_Escena_ParroquiaInterior",
            new Vector2(0f, 29.8f),
            new Vector2(2.4f, 1.5f),
            "ParroquiaInterior",
            LevelManager.ZoneId.ParroquiaInterior,
            new Vector2(0f, -14.0f),
            new Color(1f, 0.92f, 0.42f, 0.85f),
            root.transform);

        // Barrera de Sombra frente al pórtico de la Parroquia que se disipa al vencer las 2 oleadas del Atrio
        GameObject barreraPortico = new GameObject("Barrera_Sombra_Portico_Parroquia");
        barreraPortico.transform.SetParent(root.transform);
        barreraPortico.transform.position = new Vector3(0f, 29.2f, 0f);
        barreraPortico.transform.localScale = new Vector3(3.2f, 1.4f, 1f);
        SpriteRenderer srBarrera = barreraPortico.AddComponent<SpriteRenderer>();
        srBarrera.sprite = CrearSpriteRadial(new Color(0.55f, 0.12f, 0.85f, 0.80f));
        srBarrera.sortingOrder = 20;
        BoxCollider2D colBarrera = barreraPortico.AddComponent<BoxCollider2D>();
        colBarrera.size = new Vector2(1.0f, 0.8f);

        // Si el jugador ya recuperó el Eco de la Fe o viene saliendo de la Parroquia, la barrera ya está abierta
        bool atrioYaPurificado = SaveStateManager.Instance != null &&
                                 (SaveStateManager.Instance.ecoFeRecuperado || SaveStateManager.Instance.ultimaZonaId == "ParroquiaInterior");
        if (atrioYaPurificado)
        {
            barreraPortico.SetActive(false);
        }

        // 2. NPCS Y LETREROS CON RECORTE EXACTO DE CELDA (SIN CORTES ERRÓNEOS)
        GameObject letreroTut = GameObject.Find("Letrero_Tutorial");
        if (letreroTut != null && letreroTut.GetComponent<InteractableSignOrNPC>() == null)
        {
            ConfigurarLetreroTutorial(letreroTut.AddComponent<InteractableSignOrNPC>());
        }

        // Cronista Don Anselmo junto al Pozo Central de la Gran Plaza (Celda 0,0 de 32x32 en Mage-Cyan.png)
        GameObject donAnselmo = CrearNPCConSpriteExacto(
            "NPC_Cronista_DonAnselmo",
            "Mage-Cyan",
            0, 0, 32, 32, 20f,
            new Vector2(2.8f, -2.2f),
            root.transform);
        InteractableSignOrNPC npcAnselmo = donAnselmo.AddComponent<InteractableSignOrNPC>();
        npcAnselmo.speakerTitle = "Don Anselmo, Cronista de San Felipe";
        npcAnselmo.interactionPrompt = "[E] Hablar con Don Anselmo";
        npcAnselmo.dialoguePages = new string[]
        {
            "¡Bienvenido a la Gran Villa de San Felipe, Ronan! La Niebla del Olvido ha tomado los callejones exteriores y el Atrio de nuestra Parroquia.",
            "Aquí en la Plaza de Armas estás a salvo. Camina hacia el Norte por la Calzada Real y sube la escalinata hacia el Gran Atrio de la Parroquia de San Felipe Apóstol.",
            "Purifica las 2 oleadas de sombras que custodian el Atrio para abrir las puertas del templo, entra a la Parroquia y resuelve el Acertijo de las Tres Campanas de la Luz."
        };

        // Guardia Capitán Rodrigo al pie de la Calzada Real Norte (Celda 0,0 de 32x32 en Human-Soldier-Cyan.png)
        GameObject capitanRodrigo = CrearNPCConSpriteExacto(
            "NPC_Capitan_Rodrigo",
            "Human-Soldier-Cyan",
            0, 0, 32, 32, 20f,
            new Vector2(-2.6f, 17.5f),
            root.transform);
        InteractableSignOrNPC npcRodrigo = capitanRodrigo.AddComponent<InteractableSignOrNPC>();
        npcRodrigo.speakerTitle = "Capitán Rodrigo, Guardia del Atrio";
        npcRodrigo.interactionPrompt = "[E] Hablar con el Capitán Rodrigo";
        npcRodrigo.dialoguePages = new string[]
        {
            "¡Cuidado al subir las escaleras del Atrio, Ronan! Las criaturas de la Niebla atacan en hordas organizadas.",
            "En la segunda oleada aparecerá una Sombra Lanzadora: usa tu bloqueo [K] o [Clic Derecho] justo dentro de los primeros 0.20 segundos antes del impacto para devolverle sus orbes oscuros.",
            "Cuando derrotes a las dos oleadas del Atrio, el sello púrpura de la puerta principal desaparecerá y podrás entrar al interior de la Parroquia."
        };

        // Panadera Doña Lucía en el Barrio Poniente
        GameObject donaLucia = CrearNPCConSpriteExacto(
            "NPC_Panadera_DonaLucia",
            "Character-Base",
            0, 0, 32, 32, 20f,
            new Vector2(-22.5f, 2.0f),
            root.transform);
        InteractableSignOrNPC npcLucia = donaLucia.AddComponent<InteractableSignOrNPC>();
        npcLucia.speakerTitle = "Doña Lucía, Hornera del Barrio Poniente";
        npcLucia.interactionPrompt = "[E] Hablar con Doña Lucía";
        npcLucia.dialoguePages = new string[]
        {
            "Los duendes saqueadores tomaron la plazoleta del Oeste cerca de nuestros hornos de pan.",
            "Si te acercas a su zona, prepárate para vencer a sus dos oleadas."
        };

        // Placa histórica en el Atrio de la Parroquia
        GameObject placaAtrio = CrearNPCConSpriteExacto(
            "Placa_Atrio_Parroquia",
            "TilesetElement",
            1, 2, 16, 16, 16f,
            new Vector2(-3.2f, 28.5f),
            root.transform);
        InteractableSignOrNPC signAtrio = placaAtrio.AddComponent<InteractableSignOrNPC>();
        signAtrio.speakerTitle = "Pórtico de la Parroquia de San Felipe Apóstol";
        signAtrio.interactionPrompt = "[E] Leer Inscripción del Pórtico";
        signAtrio.dialoguePages = new string[]
        {
            "«Parroquia de San Felipe Apóstol — Templo Histórico de Torres Mochas.»",
            "«Derrota a las oleadas del Atrio y cruza el umbral dorado para ingresar a la escena del Interior de la Parroquia.»"
        };

        // 3. SISTEMA DE HORDAS / OLEADAS POR ZONA (ZONE WAVE SPAWNERS)
        // A) Horda Principal: Atrio de la Parroquia (Norte)
        if (!atrioYaPurificado)
        {
            GameObject spawnerAtrioGO = new GameObject("Horda_Zona_Atrio_Parroquia");
            spawnerAtrioGO.transform.SetParent(root.transform);
            spawnerAtrioGO.transform.position = new Vector3(0f, 24.5f, 0f);

            ZoneWaveSpawner spawnerAtrio = spawnerAtrioGO.AddComponent<ZoneWaveSpawner>();
            spawnerAtrio.zoneTitle = "Atrio de la Parroquia";
            spawnerAtrio.activationRadius = 7.5f;
            spawnerAtrio.barrierToDisableOnClear = barreraPortico;

            // Oleada 1: 2 Slimes de Niebla + 1 Sombra Errante
            ZoneWaveSpawner.WaveDefinition wave1 = new ZoneWaveSpawner.WaveDefinition
            {
                waveTitle = "Vanguardia de la Niebla"
            };
            wave1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.MistSlime, spawnPosition = new Vector2(-4.5f, 25.0f) });
            wave1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.MistSlime, spawnPosition = new Vector2(4.5f, 25.0f) });
            wave1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.SombraErrante, spawnPosition = new Vector2(0f, 26.8f) });

            // Oleada 2: 1 Sombra Lanzadora (Contraataque 0.20s) + 1 Guardia Corrompido
            ZoneWaveSpawner.WaveDefinition wave2 = new ZoneWaveSpawner.WaveDefinition
            {
                waveTitle = "Custodios del Pórtico Sagrado"
            };
            wave2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.SombraLanzadora, spawnPosition = new Vector2(0f, 27.2f) });
            wave2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.CorruptedGuard, spawnPosition = new Vector2(-3.5f, 25.5f) });

            spawnerAtrio.waves.Add(wave1);
            spawnerAtrio.waves.Add(wave2);
        }

        // B) Horda Opcional Poniente: Plazoleta de los Hornos (-28, 4)
        GameObject spawnerOesteGO = new GameObject("Horda_Zona_Barrio_Poniente");
        spawnerOesteGO.transform.SetParent(root.transform);
        spawnerOesteGO.transform.position = new Vector3(-28f, 4f, 0f);

        ZoneWaveSpawner spawnerOeste = spawnerOesteGO.AddComponent<ZoneWaveSpawner>();
        spawnerOeste.zoneTitle = "Plazoleta del Barrio Poniente";
        spawnerOeste.activationRadius = 6.5f;

        ZoneWaveSpawner.WaveDefinition wOeste1 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Saqueadores del Callejón" };
        wOeste1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.GoblinRaider, spawnPosition = new Vector2(-30f, 5.5f) });
        wOeste1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.GoblinRaider, spawnPosition = new Vector2(-26f, 5.5f) });

        ZoneWaveSpawner.WaveDefinition wOeste2 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Artillero y Arquero de la Niebla" };
        wOeste2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.GoblinBomber, spawnPosition = new Vector2(-28f, 6.5f) });
        wOeste2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.CorruptedArcher, spawnPosition = new Vector2(-29.5f, 3.0f) });

        spawnerOeste.waves.Add(wOeste1);
        spawnerOeste.waves.Add(wOeste2);

        // C) Horda Opcional Oriente: Jardín de las Ruinas Antiguas (+28, 6)
        GameObject spawnerEsteGO = new GameObject("Horda_Zona_Ruinas_Oriente");
        spawnerEsteGO.transform.SetParent(root.transform);
        spawnerEsteGO.transform.position = new Vector3(28f, 6f, 0f);

        ZoneWaveSpawner spawnerEste = spawnerEsteGO.AddComponent<ZoneWaveSpawner>();
        spawnerEste.zoneTitle = "Jardín de las Ruinas Antiguas";
        spawnerEste.activationRadius = 6.5f;

        ZoneWaveSpawner.WaveDefinition wEste1 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Brotes del Musgo Antiguo" };
        wEste1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.SporeMushroom, spawnPosition = new Vector2(26f, 7.5f) });
        wEste1.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.CorruptedArcher, spawnPosition = new Vector2(30f, 7.5f) });

        ZoneWaveSpawner.WaveDefinition wEste2 = new ZoneWaveSpawner.WaveDefinition { waveTitle = "Conjurador de las Columnas" };
        wEste2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.MistSlime, spawnPosition = new Vector2(27f, 5.0f) });
        wEste2.enemies.Add(new ZoneWaveSpawner.SpawnEntry { kind = ZoneWaveSpawner.EnemyKind.OblivionMage, spawnPosition = new Vector2(29f, 8.0f) });

        spawnerEste.waves.Add(wEste1);
        spawnerEste.waves.Add(wEste2);

        // 5. CORAZONES CURATIVOS EN EL MAPA EXTERIOR PARA RECUPERAR VIDA (+25 HP CADA UNO)
        HeartPickup.SpawnHeart(new Vector3(-3.2f, -1.8f, 0f), 25f, root.transform);  // Junto a Don Anselmo en la Plaza
        HeartPickup.SpawnHeart(new Vector3(-12.0f, -21.5f, 0f), 25f, root.transform); // En el Campamento de Viajeros al Sur
        HeartPickup.SpawnHeart(new Vector3(-17.5f, 0.5f, 0f), 25f, root.transform);   // Entrada al Barrio Poniente
        HeartPickup.SpawnHeart(new Vector3(17.5f, 0.5f, 0f), 25f, root.transform);    // Entrada a la Villa Oriente
        HeartPickup.SpawnHeart(new Vector3(-2.5f, 18.2f, 0f), 25f, root.transform);   // Antes de subir al Atrio de la Parroquia
        HeartPickup.SpawnHeart(new Vector3(2.5f, 18.2f, 0f), 25f, root.transform);    // Antes de subir al Atrio de la Parroquia

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

    public static GameObject CrearPortalTransicionEscena(
        string nombre,
        Vector2 pos,
        Vector2 size,
        string escenaDestino,
        LevelManager.ZoneId zonaId,
        Vector2 coordenadasEnNuevaEscena,
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
        zt.tipoTransicion = ZoneTransition.TransitionType.CambioDeEscena;
        zt.nombreEscenaDestino = escenaDestino;
        zt.zonaDestino = zonaId;
        zt.coordenadasDestino = coordenadasEnNuevaEscena;
        zt.usarCoordenadasEnNuevaEscena = true;

        return portal;
    }

    public static GameObject CrearNPCConSpriteExacto(
        string nombre,
        string sheetNameWithoutExt,
        int col,
        int rowTop,
        int cellW,
        int cellH,
        float ppu,
        Vector2 pos,
        Transform parent)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10);
        sr.sprite = ExtraerSpriteExactoSinCosturas(sheetNameWithoutExt, col, rowTop, cellW, cellH, ppu, cellW);

        return go;
    }

    public static Texture2D CargarTextura(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return null;
        string nameNoExt = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        return ZoneWaveSpawner.LoadSpriteTexture(nameNoExt);
    }

    public static Sprite ExtraerSpriteExactoSinCosturas(
        string sheetNameWithoutExt,
        int col,
        int rowTop,
        int pixelW,
        int pixelH,
        float ppu = 16f,
        int gridCellSize = 16)
    {
        string key = $"{sheetNameWithoutExt}_{col}_{rowTop}_{pixelW}_{pixelH}_{ppu}_{gridCellSize}";
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
        {
            return cached;
        }

        Texture2D sheet = ZoneWaveSpawner.LoadSpriteTexture(sheetNameWithoutExt);
        if (sheet == null)
        {
            return CrearSpriteRadial(new Color(0.95f, 0.85f, 0.35f, 1f));
        }

        int px = Mathf.Clamp(col * gridCellSize, 0, Mathf.Max(0, sheet.width - pixelW));
        int pyUnity = Mathf.Clamp(sheet.height - ((rowTop * gridCellSize) + pixelH), 0, Mathf.Max(0, sheet.height - pixelH));

        Color[] pixels;
        if (sheet.isReadable)
        {
            pixels = sheet.GetPixels(px, pyUnity, pixelW, pixelH);
        }
        else
        {
            RenderTexture rt = RenderTexture.GetTemporary(sheet.width, sheet.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(sheet, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D fullCopy = new Texture2D(sheet.width, sheet.height, TextureFormat.RGBA32, false);
            fullCopy.ReadPixels(new Rect(0, 0, sheet.width, sheet.height), 0, 0);
            fullCopy.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            pixels = fullCopy.GetPixels(px, pyUnity, pixelW, pixelH);
            DestroyImmediateIfEditor(fullCopy);
        }

        Texture2D subTex = new Texture2D(pixelW, pixelH, TextureFormat.RGBA32, false);
        subTex.filterMode = FilterMode.Point;
        subTex.wrapMode = TextureWrapMode.Clamp;
        subTex.SetPixels(pixels);
        subTex.Apply();

        Sprite sp = Sprite.Create(subTex, new Rect(0, 0, pixelW, pixelH), new Vector2(0.5f, 0.5f), ppu);
        spriteCache[key] = sp;
        return sp;
    }

    private static void DestroyImmediateIfEditor(UnityEngine.Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    public static Sprite CrearSpriteRadial(Color color)
    {
        Texture2D tex = new Texture2D(32, 32);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
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
