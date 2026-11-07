using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor global del nivel para registro, seguimiento y conteo de enemigos vivos.
/// Provee el conteo unificado (por registro activo y por tag "Enemy") para controlar
/// la apertura de la puerta de la Parroquia y los estados del mapa.
/// </summary>
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("Enemigos Registrados")]
    private readonly HashSet<GameObject> enemigosRegistrados = new HashSet<GameObject>();

    public event Action<int> OnEnemyCountChanged;
    public event Action OnAllEnemiesDefeated;

    public int ActiveEnemyCount
    {
        get
        {
            LimpiarEnemigosInactivos();
            return enemigosRegistrados.Count;
        }
    }

    public static EnemyManager EnsureInstance()
    {
        if (Instance != null) return Instance;
        EnemyManager existing = FindAnyObjectByType<EnemyManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }
        GameObject go = new GameObject("EnemyManager");
        Instance = go.AddComponent<EnemyManager>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        EscanearEnemigosIniciales();
    }

    public void RegisterEnemy(GameObject enemy)
    {
        if (enemy == null) return;
        if (!enemy.CompareTag("Player") && !enemigosRegistrados.Contains(enemy))
        {
            enemigosRegistrados.Add(enemy);
            if (!enemy.CompareTag("Enemy"))
            {
                enemy.tag = "Enemy";
            }
            OnEnemyCountChanged?.Invoke(ActiveEnemyCount);
        }
    }

    public void UnregisterEnemy(GameObject enemy)
    {
        if (enemy != null && enemigosRegistrados.Remove(enemy))
        {
            int remaining = ActiveEnemyCount;
            OnEnemyCountChanged?.Invoke(remaining);
            if (remaining == 0)
            {
                OnAllEnemiesDefeated?.Invoke();
            }
        }
    }

    public void EscanearEnemigosIniciales()
    {
        // 1. Buscar por tag "Enemy"
        GameObject[] porTag = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject go in porTag)
        {
            if (go != null && !go.CompareTag("Player"))
            {
                RegisterEnemy(go);
            }
        }

        // 2. Buscar por componentes de IA y HealthComponent no jugador
        BaseEnemyAI[] aiList = FindObjectsByType<BaseEnemyAI>();
        foreach (BaseEnemyAI ai in aiList)
        {
            if (ai != null && ai.currentState != BaseEnemyAI.AIState.Dead)
            {
                RegisterEnemy(ai.gameObject);
            }
        }

        HealthComponent[] hcList = FindObjectsByType<HealthComponent>();
        foreach (HealthComponent hc in hcList)
        {
            if (hc != null && !hc.isPlayer && hc.currentHealth > 0f)
            {
                RegisterEnemy(hc.gameObject);
            }
        }

        LimpiarEnemigosInactivos();
        OnEnemyCountChanged?.Invoke(ActiveEnemyCount);
    }

    public HashSet<GameObject> ObtenerListaEnemigosVivos()
    {
        LimpiarEnemigosInactivos();
        return new HashSet<GameObject>(enemigosRegistrados);
    }

    private void LimpiarEnemigosInactivos()
    {
        enemigosRegistrados.RemoveWhere(e =>
        {
            if (e == null) return true;
            HealthComponent hc = e.GetComponent<HealthComponent>();
            if (hc != null && hc.currentHealth <= 0f) return true;
            BaseEnemyAI ai = e.GetComponent<BaseEnemyAI>();
            if (ai != null && ai.currentState == BaseEnemyAI.AIState.Dead) return true;
            return !e.activeInHierarchy;
        });
    }
}
