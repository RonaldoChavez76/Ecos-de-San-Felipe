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
        CorruptedArcher,
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
    public float activationRadius = 5.5f;
    public List<WaveDefinition> waves = new List<WaveDefinition>();

    /// <summary>
    /// Spawner de horda actualmente activo en la escena.
    /// Garantiza exclusión mutua para evitar que Ronan active múltiples hordas simultáneas al esquivar o retroceder.
    /// </summary>
    public static ZoneWaveSpawner HordaActivaEnEscena { get; private set; }

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
            if (HordaActivaEnEscena != null && HordaActivaEnEscena != this && !HordaActivaEnEscena.encounterCompleted)
            {
                return; // Exclusión mutua: no activar si otra horda ya está en combate
            }

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
        if (HordaActivaEnEscena != null && HordaActivaEnEscena != this && !HordaActivaEnEscena.encounterCompleted) return;

        HordaActivaEnEscena = this;
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
        if (HordaActivaEnEscena == this)
        {
            HordaActivaEnEscena = null;
        }

        AudioManager.Instance?.PlaySecretSound();

        if (barrierToDisableOnClear != null)
        {
            barrierToDisableOnClear.SetActive(false);
        }

        if (rewardToEnableOnClear != null)
        {
            rewardToEnableOnClear.SetActive(true);
        }

        // Soltar botín abundante de oro, corazón sagrado y provisiones al purificar la zona
        HeartPickup.SpawnHeart(transform.position, 35f, transform);
        ResourcePickup.SpawnDrops(transform.position, 1.0f, 35, 60, 1.0f, 8, 1.0f, 40f, 0.5f, 30f, transform);

        MostrarBanner($"★ ¡{zoneTitle.ToUpperInvariant()} PURIFICADO! (+Oro y Corazón Sagrado) ★", 4.0f);
    }

    private void OnDisable()
    {
        if (HordaActivaEnEscena == this) HordaActivaEnEscena = null;
    }

    private void OnDestroy()
    {
        if (HordaActivaEnEscena == this) HordaActivaEnEscena = null;
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
        go.tag = "Enemy";
        EnemyManager.EnsureInstance()?.RegisterEnemy(go);

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
                rend.customDemonImage = LoadSpriteTexture("SombraLanzadora_SpriteSheet");
                rend.InicializarSpritesDemonio();
                SombraLanzadoraAI ai = go.AddComponent<SombraLanzadoraAI>();
                ai.objetivo = playerTransform;
                ai.builderRef = builder;
            }
            else
            {
                rend.tipoDemonio = BlackCloverDemonRenderer.DemonType.SombraErrante;
                rend.customDemonImage = LoadSpriteTexture("SombraErrante_SpriteSheet");
                rend.InicializarSpritesDemonio();
                SombraErranteAI ai = go.AddComponent<SombraErranteAI>();
                ai.objetivo = playerTransform;
                ai.builderRef = builder;
            }
            return go;
        }

        string sheetName = "slime_spritesheet";
        EnemySpriteSheetAnimator.PunyWeaponType forcedWeapon = EnemySpriteSheetAnimator.PunyWeaponType.AutoDetect;
        switch (kind)
        {
            case EnemyKind.MistSlime: sheetName = "slime_spritesheet"; break;
            case EnemyKind.SporeMushroom: sheetName = "mushroom_spritesheet"; break;
            case EnemyKind.GoblinRaider: sheetName = "goblin_spritesheet"; break;
            case EnemyKind.GoblinBomber: sheetName = "goblin_bomber_spritesheet"; break;
            case EnemyKind.QuarryWorm: sheetName = "worm_spritesheet"; break;
            case EnemyKind.DarkOrcBrute:
                sheetName = "Orc-Soldier-Red";
                forcedWeapon = EnemySpriteSheetAnimator.PunyWeaponType.Sword;
                break;
            case EnemyKind.OblivionMage:
                sheetName = "Mage-Red";
                forcedWeapon = EnemySpriteSheetAnimator.PunyWeaponType.Staff;
                break;
            case EnemyKind.CorruptedArcher:
                sheetName = "Orc-Peon-Cyan";
                forcedWeapon = EnemySpriteSheetAnimator.PunyWeaponType.Bow;
                break;
            case EnemyKind.CorruptedGuard:
                // Antes compartía la hoja Human-Soldier-Cyan con Ronan; ahora usa el Orco Soldado (misma rejilla Puny 24x8)
                sheetName = "Orc-Soldier-Cyan";
                forcedWeapon = EnemySpriteSheetAnimator.PunyWeaponType.Sword;
                break;
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
            case EnemyKind.CorruptedArcher: go.AddComponent<CorruptedArcherAI>(); break;
            case EnemyKind.CorruptedGuard: go.AddComponent<CorruptedGuardAI>(); break;
        }

        Texture2D tex = LoadSpriteTexture(sheetName);
        EnemySpriteSheetAnimator anim = go.AddComponent<EnemySpriteSheetAnimator>();
        anim.weaponType = forcedWeapon;
        if (tex != null)
        {
            anim.AutoDetectAndInitialize(tex);
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

        float y = UITheme.Px(110f);
        if (bannerTimer > 0f && !string.IsNullOrEmpty(bannerMessage))
        {
            UITheme.Banner(y, bannerMessage, UITheme.Dorado, 760f, 18);
        }
        else if (encounterStarted && !encounterCompleted && activeEnemies.Count > 0)
        {
            string txt = $"{zoneTitle} — Oleada {currentWaveIndex + 1}/{waves.Count}  ·  Enemigos restantes: {activeEnemies.Count}";
            UITheme.Banner(y, txt, UITheme.Turquesa, 720f, 17);
        }
    }
}