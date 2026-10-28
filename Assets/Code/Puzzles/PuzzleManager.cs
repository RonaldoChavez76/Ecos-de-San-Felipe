using System;
using UnityEngine;

/// <summary>
/// Gestor global del estado de los acertijos de los 3 niveles de Ecos de San Felipe:
/// - Nivel 1 (Parroquia): Acertijo de las Campanas y la Luz (Alba -> Mediodía -> Ocaso).
/// - Nivel 2 (Mercado): Acertijo de Trueque e Intercambio Artesanal.
/// - Nivel 3 (Cerro del Fraile): Acertijo de Purificación de los 3 Sellos de la Cruz Azul.
/// </summary>
public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance { get; private set; }

    [Header("Estado de Acertijos por Nivel")]
    public bool parroquiaBellPuzzleSolved;
    public bool marketTradePuzzleSolved;
    public bool blueCrossSealsSolved;

    public event Action<int> OnPuzzleSolved;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public static PuzzleManager EnsureInstance()
    {
        if (Instance != null) return Instance;
        PuzzleManager existing = FindAnyObjectByType<PuzzleManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }
        GameObject go = new GameObject("PuzzleManager");
        Instance = go.AddComponent<PuzzleManager>();
        return Instance;
    }

    public void MarcarAcertijoResuelto(int nivelIndex)
    {
        switch (nivelIndex)
        {
            case 1:
                if (parroquiaBellPuzzleSolved) return;
                parroquiaBellPuzzleSolved = true;
                break;
            case 2:
                if (marketTradePuzzleSolved) return;
                marketTradePuzzleSolved = true;
                break;
            case 3:
                if (blueCrossSealsSolved) return;
                blueCrossSealsSolved = true;
                break;
        }

        AudioManager.Instance?.PlaySecretSound();
        OnPuzzleSolved?.Invoke(nivelIndex);
        Debug.Log($"<color=#FFD700>[PuzzleManager] ¡Acertijo del Nivel {nivelIndex} resuelto!</color>");
    }
}

