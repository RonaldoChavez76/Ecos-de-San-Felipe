using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PunyCharacterAnimator : MonoBehaviour
{
    [Header("Arrastra aquí la imagen Human-Soldier-Cyan")]
    public Texture2D spriteSheet;

    [Header("Ajustes Visuales")]
    public float pixelsPerUnit = 16f;
    public float walkFPS = 10f;
    public float attackFPS = 16f;

    // Matriz de [8 filas de dirección, 24 columnas de animación]
    private Sprite[,] grid = new Sprite[8, 24];
    private SpriteRenderer sr;
    private PlayerMovement player;

    private int currentDirectionRow = 0; // 0 = Sur (Abajo) por defecto
    private float animTimer = 0f;
    private float deathAnimTimer = 0f;
    private bool sheetLoaded = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        player = GetComponent<PlayerMovement>();

        // Si el Animator viejo del Character-Base sigue activo, lo apagamos para que no sobreescriba el sprite
        Animator oldAnim = GetComponent<Animator>();
        if (oldAnim != null)
        {
            oldAnim.enabled = false;
        }

        CortarHojaDeSpritesAutomaticamente();
    }

    private void CortarHojaDeSpritesAutomaticamente()
    {
        if (spriteSheet == null)
        {
            Debug.LogWarning("¡Falta arrastrar la imagen Human-Soldier-Cyan al script PunyCharacterAnimator!");
            return;
        }

        // La hoja de Puny-Characters tiene exactamente 24 columnas y 8 filas
        int cols = 24;
        int rows = 8;
        int cellWidth = spriteSheet.width / cols;   // 32 px
        int cellHeight = spriteSheet.height / rows; // 32 px

        for (int row = 0; row < rows; row++)
        {
            // En Unity el eje Y de las texturas empieza abajo (0 = fila inferior),
            // así que invertimos el índice para que row 0 sea la fila superior (Mirando hacia Abajo/Sur)
            int texY = (rows - 1 - row) * cellHeight;

            for (int col = 0; col < cols; col++)
            {
                int texX = col * cellWidth;
                Rect rect = new Rect(texX, texY, cellWidth, cellHeight);
                grid[row, col] = Sprite.Create(spriteSheet, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
            }
        }

        sheetLoaded = true;
        sr.sprite = grid[0, 0]; // Poner el primer cuadro de Human-Soldier-Cyan
    }

    private PlayerMovement.CombatState estadoAnterior = PlayerMovement.CombatState.Idle;

    void LateUpdate()
    {
        if (!sheetLoaded || player == null) return;

        // Si acabamos de cambiar de estado (por ejemplo, de Idle a Attack), reiniciamos el reloj de animación
        if (player.currentState != estadoAnterior)
        {
            animTimer = 0f;
            estadoAnterior = player.currentState;
        }

        // 1. Leer hacia dónde se está moviendo o mirando el jugador (PC + Joystick móvil)
        Vector2 inputDir = MobileInput.GetMovementAxis();

        // Si está en estado de Victoria, siempre mira hacia el frente/cámara (Fila 0 = Sur)
        if (player.currentState == PlayerMovement.CombatState.Victory)
        {
            currentDirectionRow = 0;
        }
        else if (player.LookDirection.sqrMagnitude > 0.01f)
        {
            currentDirectionRow = ObtenerFilaDeDireccion(player.LookDirection);
        }
        else if (player.currentState == PlayerMovement.CombatState.Idle && inputDir.sqrMagnitude > 0.01f)
        {
            currentDirectionRow = ObtenerFilaDeDireccion(inputDir.normalized);
        }

        // 2. Elegir qué columnas de la hoja reproducir según el estado de Ronan
        int columnaElegida = 0;

        if (player.currentHealth <= 0f)
        {
            // Secuencia de caída / derrota (Columnas 20 -> 21 -> 22 -> 23)
            // Se usa Time.unscaledDeltaTime para que se reproduzca fluido incluso en cámara lenta
            deathAnimTimer += Time.unscaledDeltaTime * 3.5f;
            int frameMuerte = Mathf.Clamp(Mathf.FloorToInt(deathAnimTimer), 0, 3);
            columnaElegida = 20 + frameMuerte;
        }
        else if (player.currentState == PlayerMovement.CombatState.Victory)
        {
            // Pose heroica de triunfo con el Eco en alto mirando a la cámara (Columna 4 = Brazos alzados)
            currentDirectionRow = 0;
            columnaElegida = 4;
        }
        else if (player.currentState == PlayerMovement.CombatState.ItemGet)
        {
            currentDirectionRow = 0;
            columnaElegida = 4;
        }
        else
        {
            deathAnimTimer = 0f;

            if (player.currentState == PlayerMovement.CombatState.Attack)
            {
                // Reproduce los cuadros del arma equipada (espada 4-7, arco 8-12, báculo 13-15) sincronizados con su duración
                WeaponDefinition arma = player.ArmaActual;
                int cuadros = Mathf.Max(1, arma.cuadros);
                animTimer += Time.deltaTime;
                float progreso = Mathf.Clamp01(animTimer / Mathf.Max(0.01f, arma.duracionAtaque));
                int frameAtaque = Mathf.Clamp(Mathf.FloorToInt(progreso * cuadros), 0, cuadros - 1);
                columnaElegida = Mathf.Clamp(arma.columnaInicial + frameAtaque, 0, 23);
            }
            else if (player.currentState == PlayerMovement.CombatState.Block || player.currentState == PlayerMovement.CombatState.Counter)
            {
                columnaElegida = 4; // Guardia firme con espada en alto (Columna 4)
            }
            else if (player.currentState == PlayerMovement.CombatState.Dodge)
            {
                columnaElegida = 2; // Impulso de esquive
            }
            else
            {
                if (inputDir.sqrMagnitude > 0.01f)
                {
                    animTimer += Time.deltaTime * walkFPS;
                    int[] walkCycleCols = { 1, 2, 3, 2 };
                    int frameCaminata = Mathf.FloorToInt(animTimer) % 4;
                    columnaElegida = walkCycleCols[frameCaminata];
                }
                else
                {
                    animTimer = 0f;
                    columnaElegida = 0;
                }
            }
        }

        // 3. Asignar el sprite exacto de la fila (dirección) y columna (acción)
        sr.sprite = grid[currentDirectionRow, columnaElegida];
    }

    // Mapea cualquier dirección de movimiento a las 8 filas exactas de Puny-Characters:
    // Fila 0: Sur (Abajo)
    // Fila 1: Sureste (Abajo-Derecha)
    // Fila 2: Este (Derecha)
    // Fila 3: Noreste (Arriba-Derecha)
    // Fila 4: Norte (Arriba)
    // Fila 5: Noroeste (Arriba-Izquierda)
    // Fila 6: Oeste (Izquierda)
    // Fila 7: Suroeste (Abajo-Izquierda)
    private int ObtenerFilaDeDireccion(Vector2 dir)
    {
        float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (angulo < 0f) angulo += 360f;

        // Dividir los 360 grados en 8 sectores de 45 grados cada uno:
        // 0° = Este (Fila 2), 90° = Norte (Fila 4), 180° = Oeste (Fila 6), 270° = Sur (Fila 0)
        int sector = Mathf.RoundToInt(angulo / 45f) % 8;

        switch (sector)
        {
            case 0: return 2; // Este (Derecha)
            case 1: return 3; // Noreste (Arriba-Derecha)
            case 2: return 4; // Norte (Arriba)
            case 3: return 5; // Noroeste (Arriba-Izquierda)
            case 4: return 6; // Oeste (Izquierda)
            case 5: return 7; // Suroeste (Abajo-Izquierda)
            case 6: return 0; // Sur (Abajo)
            case 7: return 1; // Sureste (Abajo-Derecha)
            default: return 0;
        }
    }
}
