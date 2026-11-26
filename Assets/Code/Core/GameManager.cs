using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Menu,
        Exploring,
        Combat,
        Dialogue,
        Puzzle,
        Paused,
        GameOver,
        Victory
    }

    [Header("Estado del Juego")]
    public GameState currentState = GameState.Exploring;

    [Header("Escenas Principales")]
    public string menuSceneName = "PantallaInicio";
    public string gameplaySceneName = "SampleScene";

    public event Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == menuSceneName)
        {
            SetState(GameState.Menu);
        }
        else
        {
            SetState(GameState.Exploring);
        }
    }

    private void Update()
    {
        if (PauseAndVictoryUI.Instance == null && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)))
        {
            if (currentState == GameState.Exploring || currentState == GameState.Combat)
            {
                PausarJuego();
            }
            else if (currentState == GameState.Paused)
            {
                ReanudarJuego();
            }
        }
    }

    public void SetState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        Debug.Log($"<color=#F0C14B>[GameManager] Estado cambiado a: {currentState}</color>");

        switch (currentState)
        {
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            case GameState.GameOver:
                Time.timeScale = 1f; // La cámara lenta la maneja SanFelipeUIManager
                break;
            default:
                Time.timeScale = 1f;
                break;
        }

        OnGameStateChanged?.Invoke(currentState);
    }

    public void PausarJuego()
    {
        SetState(GameState.Paused);
    }

    public void ReanudarJuego()
    {
        SetState(GameState.Exploring);
    }

    public void IniciarPartidaNueva()
    {
        SaveStateManager.Instance?.ReiniciarProgreso();
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameplaySceneName);
        SetState(GameState.Exploring);
    }

    public void ContinuarPartida()
    {
        if (SaveStateManager.EnsureInstance().RegresarAlUltimoCheckpoint())
        {
            SetState(GameState.Exploring);
            return;
        }

        Debug.LogWarning("[GameManager] No hay un checkpoint válido para continuar la partida.");
        Time.timeScale = 1f;
    }

    public void IrAlMenuPrincipal()
    {
        SaveStateManager.Instance?.GuardarProgreso();
        Time.timeScale = 1f;
        SetState(GameState.Menu);
        SceneManager.LoadScene(menuSceneName);
    }

    public void ActivarVictoria()
    {
        SetState(GameState.Victory);
        Debug.Log("<color=#4AFF88>[GameManager] ¡VICTORIA! Todos los Ecos de San Felipe han sido recuperados.</color>");
    }

    public void ActivarGameOver()
    {
        SetState(GameState.GameOver);
    }
}
