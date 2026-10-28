using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor de Enemigos por Zona y Sistema de Hordas / Oleadas Controladas.
/// Evita saturar el mapa con enemigos estáticos y activa encuentros tácticos por oleadas
/// cuando Ronan entra en una zona clave (ej. Atrio de la Parroquia, Jardín de Ruinas, Nave Interior).
/// </summary>
public class ZoneWaveSpawner : MonoBehaviour
{
    public enum EnemyKind
    {
        MistSlime,
        SporeMushroom,
        GoblinRaider,
        GoblinBomber,
        QuarryWorm,
        DarkOrcBrute,
        OblivionMage,
        CorruptedGuard,
        SombraErrante,
        SombraLanzadora
    }

    [Serializable]
    public class SpawnEntry
    {
        public EnemyKind kind = EnemyKind.MistSlime;
        public Vector2 spawnPosition;
    }

    [Serializable]
    public class WaveDefinition
    {
        public string waveTitle = "Oleada 1";
        public List<SpawnEntry> enemies = new List<SpawnEntry>();
    }

    [Header("Configuración de la Zona de Horda")]
    public string zoneTitle = "Atrio de la Parroquia";
    public float activationRadius = 8.5f;
    public List<WaveDefinition> waves = new List<WaveDefinition>();

    [Header("Objetos Bloqueados / Recompensas al Limpiar la Zona")]
    public GameObject barrierToDisableOnClear;
    public GameObject rewardToEnableOnClear;

    [Header("Estado en Tiempo de Ejecución")]
    public bool encounterStarted = false;
    public bool encounterCompleted = false;
    public int currentWaveIndex = -1;

    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    private Transform playerTransform;
    private string bannerMessage = "";
    private float bannerTimer = 0f;

    private void Update()
    {
        if (bannerTimer > 0f) bannerTimer -= Time.deltaTime;
        if (encounterCompleted) return;

        if (playerTransform == null)
        {
            PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
            if (pm != null) playerTransform = pm.transform;
            return;
        }

        if (!encounterStarted)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist <= activationRadius)
            {
                IniciarEncuentroPorOleadas();
            }
            return;
        }

        // Limpiar referencias nulas de enemigos ya derrotados
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null)
            {
                activeEnemies.RemoveAt(i);
            }
            else
            {
                HealthComponent hc = activeEnemies[i].GetComponent<HealthComponent>();
                if (hc != null && hc.currentHealth <= 0f)
                {
                    activeEnemies.RemoveAt(i);
                }
            }
        }
    }

    public void IniciarEncuentroPorOleadas()
    {
        if (encounterStarted || encounterCompleted) return;
        encounterStarted = true;
        StartCoroutine(RutinaOleadas());
    }

    private IEnumerator RutinaOleadas()
    {
        for (int w = 0; w < waves.Count; w++)
        {
            currentWaveIndex = w;
            WaveDefinition wave = waves[w];
            MostrarBanner($"⚔ {zoneTitle.ToUpperInvariant()} — OLEADA {w + 1}/{waves.Count}: {wave.waveTitle} ⚔", 3.2f);

            yield return new WaitForSeconds(0.65f);

            foreach (SpawnEntry entry in wave.enemies)
            {
                GameObject spawned = SpawnEnemyByKind(entry.kind, entry.spawnPosition);
                if (spawned != null)
                {
                    activeEnemies.Add(spawned);
                }
            }

            // Esperar hasta que todos los enemigos de la oleada actual sean derrotados
            while (activeEnemies.Count > 0)
            {
                yield return new WaitForSeconds(0.25f);
            }

            if (w < waves.Count - 1)
            {
                MostrarBanner($"¡Oleada {w + 1} superada! Prepárate para la siguiente...", 2.0f);
                yield return new WaitForSeconds(1.5f);
            }
        }

        CompletarEncuentro();
    }

    private void CompletarEncuentro()
    {
        encounterCompleted = true;
        AudioManager.Instance?.PlaySecretSound();

        if (barrierToDisableOnClear != null)
        {
            barrierToDisableOnClear.SetActive(false);
        }

        if (rewardToEnableOnClear != null)
        {
            rewardToEnableOnClear.SetActive(true);
        }

        MostrarBanner($"★ ¡{zoneTitle.ToUpperInvariant()} PURIFICADO! ★", 4.0f);
    }

    private void MostrarBanner(string msg, float duration)
    {
        bannerMessage = msg;
        bannerTimer = duration;
    }

    private GameObject SpawnEnemyByKind(EnemyKind kind, Vector2 pos)
    {
        GameObject go = new GameObject($"Enemigo_{kind}_{ UnityEngine.Random.Range(100, 999) }");
        go.transform.SetParent(transform);
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;

        if (kind == EnemyKind.SombraErrante || kind == EnemyKind.SombraLanzadora)
        {
            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.48f;

            BlackCloverDemonRenderer rend = go.AddComponent<BlackCloverDemonRenderer>();
            SanFelipeDemoBuilder builder = FindAnyObjectByType<SanFelipeDemoBuilder>();

            if (kind == EnemyKind.SombraLanzadora)
            {
                rend.tipoDemonio = BlackCloverDemonRenderer.DemonType.SombraLanzadora;
                SombraLanzadoraAI ai = go.AddComponent<SombraLanzadoraAI>();
                ai.objetivo = playerTransform;
                ai.builderRef = builder;
            }
            else
            {
                rend.tipoDemonio = BlackCloverDemonRenderer.DemonType.SombraErrante;
                SombraErranteAI ai = go.AddComponent<SombraErranteAI>();
                ai.objetivo = playerTransform;
                ai.builderRef = builder;
            }
            return go;
        }

        string sheetName = "slime_spritesheet";
        switch (kind)
        {
            case EnemyKind.MistSlime: sheetName = "slime_spritesheet"; break;
            case EnemyKind.SporeMushroom: sheetName = "mushroom_spritesheet"; break;
            case EnemyKind.GoblinRaider: sheetName = "goblin_spritesheet"; break;
            case EnemyKind.GoblinBomber: sheetName = "goblin_bomber_spritesheet"; break;
            case EnemyKind.QuarryWorm: sheetName = "worm_spritesheet"; break;
            case EnemyKind.DarkOrcBrute: sheetName = "Orc-Soldier-Red"; break;
            case EnemyKind.OblivionMage: sheetName = "Mage-Red"; break;
            case EnemyKind.CorruptedGuard: sheetName = "Human-Soldier-Cyan"; break;
        }

        Texture2D tex = LoadSpriteTexture(sheetName);
        EnemySpriteSheetAnimator anim = go.AddComponent<EnemySpriteSheetAnimator>();
        if (tex != null)
        {
            anim.AutoDetectAndInitialize(tex);
        }

        switch (kind)
        {
            case EnemyKind.MistSlime: go.AddComponent<MistSlimeEnemyAI>(); break;
            case EnemyKind.SporeMushroom: go.AddComponent<SporeMushroomEnemyAI>(); break;
            case EnemyKind.GoblinRaider: go.AddComponent<GoblinEnemyAI>(); break;
            case EnemyKind.GoblinBomber: go.AddComponent<GoblinBomberEnemyAI>(); break;
            case EnemyKind.QuarryWorm: go.AddComponent<QuarryWormEnemyAI>(); break;
            case EnemyKind.DarkOrcBrute: go.AddComponent<DarkOrcBruteAI>(); break;
            case EnemyKind.OblivionMage: go.AddComponent<OblivionMageAI>(); break;
            case EnemyKind.CorruptedGuard: go.AddComponent<CorruptedGuardAI>(); break;
        }

        return go;
    }

    public static Texture2D LoadSpriteTexture(string fileNameWithoutExt)
    {
#if UNITY_EDITOR
        string path = $"Assets/Sprites/{fileNameWithoutExt}.png";
        Texture2D edTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (edTex != null) return edTex;
#endif
        // Fallback en modo ejecución si ya está cargada en memoria
        Texture2D[] loaded = Resources.FindObjectsOfTypeAll<Texture2D>();
        foreach (var t in loaded)
        {
            if (t != null && string.Equals(t.name, fileNameWithoutExt, StringComparison.OrdinalIgnoreCase))
            {
                return t;
            }
        }
        return null;
    }

    private void OnGUI()
    {
        if (Time.timeScale == 0f) return;

        if (bannerTimer > 0f && !string.IsNullOrEmpty(bannerMessage))
        {
            GUIStyle bannerStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            bannerStyle.normal.textColor = new Color(1f, 0.92f, 0.45f);

            float bw = 560f;
            float bh = 36f;
            GUI.Box(new Rect((Screen.width - bw) * 0.5f, 92f, bw, bh), bannerMessage, bannerStyle);
        }
        else if (encounterStarted && !encounterCompleted && activeEnemies.Count > 0)
        {
            GUIStyle statusStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            statusStyle.normal.textColor = new Color(0.45f, 0.95f, 1.0f);

            float sw = 420f;
            float sh = 28f;
            string txt = $"{zoneTitle} — Oleada {currentWaveIndex + 1}/{waves.Count} (Enemigos restantes: {activeEnemies.Count})";
            GUI.Box(new Rect((Screen.width - sw) * 0.5f, 92f, sw, sh), txt, statusStyle);
        }
    }
}
