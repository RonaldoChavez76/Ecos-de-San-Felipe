using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Animador universal en tiempo de ejecución para todas las hojas de sprites de enemigos
/// (Goblins, Bombarderos, Slimes, Hongos, Gusanos, Orcos, Arqueros, Magos y Soldados Corrompidos).
/// Incluye soporte completo de 8 direcciones reales, ataques con Espada (cols 4-7),
/// Arco (cols 8-11), Báculo de Mago (cols 12-14), Daño (cols 18-20) y Muerte (cols 21-23).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class EnemySpriteSheetAnimator : MonoBehaviour
{
    public enum PunyWeaponType
    {
        AutoDetect,
        Sword,  // Columnas 4, 5, 6, 7
        Bow,    // Columnas 8, 9, 10, 11
        Staff   // Columnas 12, 13, 14
    }

    public Texture2D spriteSheet;
    public int columns = 6;
    public int rows = 4;
    public float pixelsPerUnit = 16f;
    public float fps = 9f;
    public bool isPuny24x8Format = false;
    public PunyWeaponType weaponType = PunyWeaponType.AutoDetect;

    private SpriteRenderer sr;
    private BaseEnemyAI enemyAI;
    private Sprite[,] grid;
    private float timer = 0f;
    private float deathAnimTimer = 0f;
    private int currentDirectionRow = 0;
    private Vector3 lastPosition;
    private bool loaded = false;

    private static readonly Dictionary<Texture2D, Texture2D> readableCopies = new Dictionary<Texture2D, Texture2D>();

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        enemyAI = GetComponent<BaseEnemyAI>();
        lastPosition = transform.position;
        if (spriteSheet != null && !loaded)
        {
            AutoDetectAndInitialize(spriteSheet);
        }
    }

    public void AutoDetectAndInitialize(Texture2D sheet)
    {
        if (sheet == null) return;
        int w = sheet.width;
        int h = sheet.height;

        if (w == 768 && h == 256)
        {
            // Puny Characters (24 cols x 8 rows de 32x32, PPU = 20)
            InitializeFromTexture(sheet, 24, 8, 20f, true);
        }
        else if (w == 96 && h == 80)
        {
            // goblin_spritesheet.png (6 cols x 5 rows de 16x16, PPU = 16)
            InitializeFromTexture(sheet, 6, 5, 16f, false);
        }
        else if (w == 96 && h == 64)
        {
            // goblin_bomber_spritesheet.png (6 cols x 4 rows de 16x16, PPU = 16)
            InitializeFromTexture(sheet, 6, 4, 16f, false);
        }
        else if (w == 128 && h == 64)
        {
            // mushroom_spritesheet.png (8 cols x 4 rows de 16x16, PPU = 16)
            InitializeFromTexture(sheet, 8, 4, 16f, false);
        }
        else if (w == 240 && h == 80)
        {
            // slime_spritesheet.png (15 cols x 5 rows de 16x16, PPU = 16)
            InitializeFromTexture(sheet, 15, 5, 16f, false);
        }
        else if (w == 96 && h == 24)
        {
            // worm_spritesheet.png (6 cols x 3 rows de 16x8, PPU = 16)
            InitializeFromTexture(sheet, 6, 3, 16f, false);
        }
        else if (w == 480 && h == 32)
        {
            // Slime.png (15 cols x 1 row de 32x32, PPU = 16)
            InitializeFromTexture(sheet, 15, 1, 16f, false);
        }
        else
        {
            InitializeFromTexture(sheet, columns, rows, pixelsPerUnit, isPuny24x8Format);
        }
    }

    public void InitializeFromTexture(Texture2D sheet, int cols, int rws, float ppu = 16f, bool puny24x8 = false)
    {
        if (sheet == null) return;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (enemyAI == null) enemyAI = GetComponent<BaseEnemyAI>();

        spriteSheet = sheet;
        columns = Mathf.Max(1, cols);
        rows = Mathf.Max(1, rws);
        pixelsPerUnit = ppu;
        isPuny24x8Format = puny24x8;
        lastPosition = transform.position;

        if (weaponType == PunyWeaponType.AutoDetect && isPuny24x8Format)
        {
            weaponType = DetectWeaponType(sheet.name);
        }

        Texture2D readable = GetReadableTexture(sheet);
        int cellW = Mathf.Max(1, readable.width / columns);
        int cellH = Mathf.Max(1, readable.height / rows);

        grid = new Sprite[rows, columns];
        for (int r = 0; r < rows; r++)
        {
            int texY = (rows - 1 - r) * cellH;
            for (int c = 0; c < columns; c++)
            {
                int texX = c * cellW;
                Texture2D cellTex = new Texture2D(cellW, cellH, TextureFormat.RGBA32, false);
                cellTex.filterMode = FilterMode.Point;
                cellTex.wrapMode = TextureWrapMode.Clamp;
                cellTex.SetPixels(readable.GetPixels(texX, texY, cellW, cellH));
                cellTex.Apply();
                grid[r, c] = Sprite.Create(cellTex, new Rect(0, 0, cellW, cellH), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            }
        }

        loaded = true;
        if (sr != null) sr.sprite = grid[0, 0];
    }

    private PunyWeaponType DetectWeaponType(string sheetName)
    {
        if (GetComponent<OblivionMageAI>() != null) return PunyWeaponType.Staff;
        if (GetComponent<CorruptedArcherAI>() != null) return PunyWeaponType.Bow;

        if (!string.IsNullOrEmpty(sheetName))
        {
            string lower = sheetName.ToLowerInvariant();
            if (lower.Contains("mage")) return PunyWeaponType.Staff;
            if (lower.Contains("peon")) return PunyWeaponType.Bow;
        }
        return PunyWeaponType.Sword;
    }

    private static Texture2D GetReadableTexture(Texture2D source)
    {
        if (source == null) return null;
        if (source.isReadable) return source;

        if (readableCopies.TryGetValue(source, out Texture2D cached) && cached != null)
            return cached;

        int w = source.width;
        int h = source.height;
        RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(source, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D copy = new Texture2D(w, h, TextureFormat.RGBA32, false);
        copy.filterMode = FilterMode.Point;
        copy.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        copy.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        readableCopies[source] = copy;
        return copy;
    }

    private void LateUpdate()
    {
        if (!loaded || sr == null || grid == null) return;
        if (enemyAI == null) enemyAI = GetComponent<BaseEnemyAI>();

        Vector2 moveDelta = (Vector2)(transform.position - lastPosition);
        lastPosition = transform.position;

        bool isDead = enemyAI != null && enemyAI.currentState == BaseEnemyAI.AIState.Dead;
        if (isDead)
        {
            deathAnimTimer += Time.deltaTime;
        }
        else
        {
            deathAnimTimer = 0f;
            timer += Time.deltaTime * fps;
        }

        int row = 0;
        int col = 0;

        if (isPuny24x8Format && columns >= 24 && rows >= 8)
        {
            sr.flipX = false;

            if (!isDead)
            {
                // Si se está moviendo (caminando en patrulla o persecución), mira en la dirección exacta de su desplazamiento
                if (moveDelta.sqrMagnitude > 0.00002f &&
                    enemyAI != null &&
                    (enemyAI.currentState == BaseEnemyAI.AIState.Patrol || enemyAI.currentState == BaseEnemyAI.AIState.Chase))
                {
                    currentDirectionRow = ObtenerFilaDeDireccion8(moveDelta.normalized);
                }
                else if (enemyAI != null && enemyAI.PlayerTarget != null)
                {
                    Vector2 toPlayer = (Vector2)(enemyAI.PlayerTarget.position - transform.position);
                    if (toPlayer.sqrMagnitude > 0.001f)
                    {
                        currentDirectionRow = ObtenerFilaDeDireccion8(toPlayer.normalized);
                    }
                }
            }

            row = currentDirectionRow;

            if (isDead)
            {
                // Columnas 20 -> 21 -> 22 -> 23 (en 23 queda tendido en el suelo)
                int deathFrame = Mathf.Clamp(Mathf.FloorToInt(deathAnimTimer * 6.5f), 0, 3);
                col = 20 + deathFrame;
            }
            else if (enemyAI != null && enemyAI.currentState == BaseEnemyAI.AIState.Hurt)
            {
                // Columnas 18 y 19: reacción de impacto
                col = 18 + (Mathf.FloorToInt(timer * 1.5f) % 2);
            }
            else if (enemyAI != null && (enemyAI.currentState == BaseEnemyAI.AIState.ChargeAttack || enemyAI.currentState == BaseEnemyAI.AIState.ExecuteAttack))
            {
                bool isCharging = enemyAI.currentState == BaseEnemyAI.AIState.ChargeAttack;
                PunyWeaponType activeWeapon = weaponType == PunyWeaponType.AutoDetect ? DetectWeaponType(spriteSheet != null ? spriteSheet.name : "") : weaponType;

                if (activeWeapon == PunyWeaponType.Staff)
                {
                    // Báculo de Mago: cols 12, 13 (alzar báculo canalizando magia) -> col 14 (extender báculo disparando hechizo)
                    if (isCharging)
                        col = 12 + (Mathf.FloorToInt(timer) % 2);
                    else
                        col = 14;
                }
                else if (activeWeapon == PunyWeaponType.Bow)
                {
                    // Arco y Flecha: cols 8, 9, 10 (tensar arco y apuntar) -> col 11 (soltar flecha)
                    if (isCharging)
                        col = 8 + Mathf.Clamp(Mathf.FloorToInt((enemyAI.StateTimer / Mathf.Max(0.05f, enemyAI.chargeTime)) * 3f), 0, 2);
                    else
                        col = 11;
                }
                else
                {
                    // Espada Cuerpo a Cuerpo: col 4 (preparar) -> cols 5, 6, 7 (tajo de espada)
                    if (isCharging)
                        col = 4;
                    else
                        col = 5 + (Mathf.FloorToInt(timer * 1.6f) % 3);
                }
            }
            else if (moveDelta.sqrMagnitude > 0.00001f || (enemyAI != null && (enemyAI.currentState == BaseEnemyAI.AIState.Chase || enemyAI.currentState == BaseEnemyAI.AIState.Patrol)))
            {
                int[] walkCols = { 1, 2, 3, 2 };
                col = walkCols[Mathf.FloorToInt(timer) % 4];
            }
            else
            {
                col = 0;
            }
        }
        else if (columns == 6 && rows == 5)
        {
            // goblin_spritesheet.png (96x80): Row0=Walk(6), Row1=Attack(6), Row2=Hurt(3), Row3=Idle(4), Row4=Death(6)
            if (isDead)
            {
                row = 4;
                col = Mathf.Clamp(Mathf.FloorToInt(deathAnimTimer * 8f), 0, 5);
            }
            else if (enemyAI != null && enemyAI.currentState == BaseEnemyAI.AIState.Hurt)
            {
                row = 2;
                col = Mathf.FloorToInt(timer) % 3;
            }
            else if (enemyAI != null && (enemyAI.currentState == BaseEnemyAI.AIState.ChargeAttack || enemyAI.currentState == BaseEnemyAI.AIState.ExecuteAttack))
            {
                row = 1;
                col = Mathf.FloorToInt(timer * 1.4f) % 6;
            }
            else
            {
                bool moving = moveDelta.sqrMagnitude > 0.00001f || (enemyAI != null && (enemyAI.currentState == BaseEnemyAI.AIState.Chase || enemyAI.currentState == BaseEnemyAI.AIState.Patrol));
                row = moving ? 0 : 3;
                int maxC = moving ? 6 : 4;
                col = Mathf.FloorToInt(timer) % maxC;
            }
        }
        else if (columns == 6 && rows == 4)
        {
            // goblin_bomber_spritesheet.png (96x64): Row0=Throw(6), Row1=Hurt(3), Row2=Idle/Walk(4), Row3=Death(6)
            if (isDead)
            {
                row = 3;
                col = Mathf.Clamp(Mathf.FloorToInt(deathAnimTimer * 8f), 0, 5);
            }
            else if (enemyAI != null && enemyAI.currentState == BaseEnemyAI.AIState.Hurt)
            {
                row = 1;
                col = Mathf.FloorToInt(timer) % 3;
            }
            else
            {
                bool attacking = enemyAI != null && (enemyAI.currentState == BaseEnemyAI.AIState.ChargeAttack || enemyAI.currentState == BaseEnemyAI.AIState.ExecuteAttack);
                row = attacking ? 0 : 2;
                int maxC = attacking ? 6 : 4;
                col = Mathf.FloorToInt(timer) % maxC;
            }
        }
        else if (columns == 8 && rows == 4)
        {
            // mushroom_spritesheet.png (128x64): Row0=Walk(8), Row1=Attack(6), Row2=Hurt(3), Row3=Death(8)
            if (isDead)
            {
                row = 3;
                col = Mathf.Clamp(Mathf.FloorToInt(deathAnimTimer * 10f), 0, 7);
            }
            else if (enemyAI != null && enemyAI.currentState == BaseEnemyAI.AIState.Hurt)
            {
                row = 2;
                col = Mathf.FloorToInt(timer) % 3;
            }
            else
            {
                bool attacking = enemyAI != null && (enemyAI.currentState == BaseEnemyAI.AIState.ChargeAttack || enemyAI.currentState == BaseEnemyAI.AIState.ExecuteAttack);
                row = attacking ? 1 : 0;
                int maxC = attacking ? 6 : 8;
                col = Mathf.FloorToInt(timer) % maxC;
            }
        }
        else if (columns == 15 && rows == 5)
        {
            // slime_spritesheet.png (240x80): Row1=Walk(6), Row2=Attack(7), Row3=Idle(5), Row4=Death(10)
            if (isDead)
            {
                row = 4;
                col = Mathf.Clamp(Mathf.FloorToInt(deathAnimTimer * 12f), 0, 9);
            }
            else
            {
                bool moving = enemyAI != null && (enemyAI.currentState == BaseEnemyAI.AIState.Chase || enemyAI.currentState == BaseEnemyAI.AIState.Patrol || enemyAI.currentState == BaseEnemyAI.AIState.ExecuteAttack);
                row = moving ? 1 : 3;
                int maxC = moving ? 6 : 5;
                col = Mathf.FloorToInt(timer) % maxC;
            }
        }
        else if (columns == 6 && rows == 3)
        {
            // worm_spritesheet.png (96x24, 16x8): Row0=Walk(6), Row1=Attack(6), Row2=Death(6)
            if (isDead)
            {
                row = 2;
                col = Mathf.Clamp(Mathf.FloorToInt(deathAnimTimer * 8f), 0, 5);
            }
            else
            {
                bool attacking = enemyAI != null && (enemyAI.currentState == BaseEnemyAI.AIState.ChargeAttack || enemyAI.currentState == BaseEnemyAI.AIState.ExecuteAttack);
                row = attacking ? 1 : 0;
                col = Mathf.FloorToInt(timer) % 6;
            }
        }
        else if (columns == 15 && rows == 1)
        {
            // Slime.png (480x32, 32x32)
            row = 0;
            if (isDead)
                col = Mathf.Clamp(9 + Mathf.FloorToInt(deathAnimTimer * 8f), 9, 14);
            else
                col = Mathf.FloorToInt(timer) % 6;
        }
        else
        {
            row = 0;
            col = Mathf.FloorToInt(timer) % Mathf.Max(1, columns);
        }

        sr.sprite = grid[Mathf.Clamp(row, 0, rows - 1), Mathf.Clamp(col, 0, columns - 1)];
    }

    /// <summary>
    /// Mapea cualquier vector de dirección a las 8 filas exactas de Puny-Characters:
    /// Fila 0: Sur (Abajo)
    /// Fila 1: Sureste (Abajo-Derecha)
    /// Fila 2: Este (Derecha)
    /// Fila 3: Noreste (Arriba-Derecha)
    /// Fila 4: Norte (Arriba)
    /// Fila 5: Noroeste (Arriba-Izquierda)
    /// Fila 6: Oeste (Izquierda)
    /// Fila 7: Suroeste (Abajo-Izquierda)
    /// </summary>
    public static int ObtenerFilaDeDireccion8(Vector2 dir)
    {
        float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (angulo < 0f) angulo += 360f;

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

/// <summary>
/// 4. Duende Sombrío (Goblin): Emboscador rápido cuerpo a cuerpo en callejones.
/// </summary>
public class GoblinEnemyAI : BaseEnemyAI
{
    protected override void Awake()
    {
        enemyDisplayName = "Duende Sombrío";
        maxHealth = 28f;
        moveSpeed = 3.4f;
        detectionRange = 8.0f;
        attackRange = 1.2f;
        attackDamage = 9f;
        chargeTime = 0.24f;
        attackCooldown = 1.2f;
        base.Awake();
    }

    protected override IEnumerator PerformAttackAction()
    {
        if (playerTarget != null)
        {
            Vector3 startPos = transform.position;
            Vector3 dir = (playerTarget.position - startPos).normalized;
            float t = 0f;
            while (t < 0.14f)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, startPos + dir * 0.45f, t / 0.14f);
                yield return null;
            }

            if (Vector2.Distance(transform.position, playerTarget.position) <= attackRange + 0.45f)
            {
                playerMovement?.RecibirDanio(attackDamage);
            }
        }
        yield return new WaitForSeconds(0.15f);
    }
}

/// <summary>
/// 5. Bombardero Corrupto: Lanza bombas oscuras reflejables por el Contraataque de Ronan (0.20s).
/// </summary>
public class GoblinBomberEnemyAI : BaseEnemyAI
{
    protected override void Awake()
    {
        enemyDisplayName = "Bombardero Corrupto";
        maxHealth = 32f;
        moveSpeed = 2.3f;
        detectionRange = 9.5f;
        attackRange = 5.2f;
        attackDamage = 12f;
        chargeTime = 0.32f;
        attackCooldown = 2.3f;
        base.Awake();
    }

    protected override void ChaseBehavior(float distToPlayer)
    {
        Vector2 dir = (playerTarget.position - transform.position).normalized;
        if (distToPlayer > attackRange * 0.9f)
        {
            transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);
        }
        else if (distToPlayer < 2.2f)
        {
            transform.position -= (Vector3)(dir * (moveSpeed * 0.7f) * Time.deltaTime);
        }
    }

    protected override IEnumerator PerformAttackAction()
    {
        if (playerTarget != null)
        {
            Vector2 dir = (playerTarget.position - transform.position).normalized;
            SpawnReflectableProjectile(
                dir,
                6.0f,
                attackDamage,
                new Color(0.20f, 0.12f, 0.05f, 1f),
                new Color(1f, 0.55f, 0.10f, 1f),
                0.62f
            );
        }
        yield return new WaitForSeconds(0.22f);
    }
}

/// <summary>
/// 6. Slime de Niebla: Enemigo de salto corto que ralentiza o estorba el paso.
/// </summary>
public class MistSlimeEnemyAI : BaseEnemyAI
{
    protected override void Awake()
    {
        enemyDisplayName = "Slime de Niebla";
        maxHealth = 24f;
        moveSpeed = 2.4f;
        detectionRange = 7.5f;
        attackRange = 1.15f;
        attackDamage = 8f;
        chargeTime = 0.25f;
        attackCooldown = 1.3f;
        base.Awake();
    }

    protected override IEnumerator PerformAttackAction()
    {
        if (playerTarget != null)
        {
            Vector3 startPos = transform.position;
            Vector3 dir = (playerTarget.position - startPos).normalized;
            float t = 0f;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                float hop = Mathf.Sin((t / 0.18f) * Mathf.PI) * 0.35f;
                transform.position = Vector3.Lerp(startPos, startPos + dir * 0.55f, t / 0.18f) + Vector3.up * hop;
                yield return null;
            }

            if (Vector2.Distance(transform.position, playerTarget.position) <= attackRange + 0.45f)
            {
                playerMovement?.RecibirDanio(attackDamage);
            }
        }
    }
}

/// <summary>
/// 7. Hongo Espora: Emboscador en jardines que libera proyectiles de esporas reflejables.
/// </summary>
public class SporeMushroomEnemyAI : BaseEnemyAI
{
    protected override void Awake()
    {
        enemyDisplayName = "Hongo Espora";
        maxHealth = 32f;
        moveSpeed = 1.6f;
        detectionRange = 8.5f;
        attackRange = 4.6f;
        attackDamage = 10f;
        chargeTime = 0.35f;
        attackCooldown = 2.5f;
        base.Awake();
    }

    protected override IEnumerator PerformAttackAction()
    {
        if (playerTarget != null)
        {
            Vector2 mainDir = (playerTarget.position - transform.position).normalized;
            SpawnReflectableProjectile(
                mainDir,
                4.5f,
                attackDamage,
                new Color(0.20f, 0.75f, 0.22f),
                new Color(0.85f, 0.98f, 0.25f),
                0.55f,
                ProjectileBehaviour.ProjectileType.Spore_Ball);
        }
        yield return new WaitForSeconds(0.25f);
    }
}

/// <summary>
/// 8. Gusano de Cantera: Emerge del suelo de arena/tierra y escupe ácido reflejable.
/// </summary>
public class QuarryWormEnemyAI : BaseEnemyAI
{
    protected override void Awake()
    {
        enemyDisplayName = "Gusano de Cantera";
        maxHealth = 34f;
        moveSpeed = 1.8f;
        detectionRange = 9.0f;
        attackRange = 5.0f;
        attackDamage = 11f;
        chargeTime = 0.32f;
        attackCooldown = 2.3f;
        base.Awake();
    }

    protected override IEnumerator PerformAttackAction()
    {
        if (playerTarget != null)
        {
            Vector2 dir = (playerTarget.position - transform.position).normalized;
            SpawnReflectableProjectile(dir, 6.2f, attackDamage, new Color(0.10f, 0.35f, 0.08f), new Color(0.45f, 1f, 0.20f), 0.52f);
        }
        yield return new WaitForSeconds(0.22f);
    }
}

/// <summary>
/// 9. Guerrero / Bruto Oscuro (Orco): Enemigo pesado cuerpo a cuerpo (usa animación de Espada cols 4-7).
/// </summary>
public class DarkOrcBruteAI : BaseEnemyAI
{
    protected override void Awake()
    {
        enemyDisplayName = "Bruto Oscuro";
        maxHealth = 52f;
        moveSpeed = 2.3f;
        detectionRange = 8.5f;
        attackRange = 1.50f;
        attackDamage = 15f;
        chargeTime = 0.40f;
        attackCooldown = 1.75f;
        heartDropChance = 1.0f;
        base.Awake();
    }

    protected override IEnumerator PerformAttackAction()
    {
        if (playerTarget != null)
        {
            Vector3 startPos = transform.position;
            Vector3 dir = (playerTarget.position - startPos).normalized;
            float t = 0f;
            while (t < 0.16f)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, startPos + dir * 0.45f, t / 0.16f);
                yield return null;
            }

            if (Vector2.Distance(transform.position, playerTarget.position) <= attackRange + 0.45f)
            {
                playerMovement?.RecibirDanio(attackDamage);
            }
        }
        yield return new WaitForSeconds(0.25f);
    }
}

/// <summary>
/// 10. Mago del Olvido: Conjurador a distancia que canaliza y dispara con su Báculo de Mago (Columnas 12, 13 y 14 de Puny).
/// </summary>
public class OblivionMageAI : BaseEnemyAI
{
    protected override void Awake()
    {
        enemyDisplayName = "Mago del Olvido";
        maxHealth = 38f;
        moveSpeed = 2.2f;
        detectionRange = 10.5f;
        attackRange = 6.2f;
        attackDamage = 12f;
        chargeTime = 0.42f;
        attackCooldown = 2.6f;
        heartDropChance = 1.0f;
        base.Awake();
    }

    protected override void ChaseBehavior(float distToPlayer)
    {
        Vector2 dir = (playerTarget.position - transform.position).normalized;
        if (distToPlayer > attackRange * 0.88f)
        {
            transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);
        }
        else if (distToPlayer < 2.6f)
        {
            transform.position -= (Vector3)(dir * (moveSpeed * 0.75f) * Time.deltaTime);
        }
    }

    protected override IEnumerator PerformAttackAction()
    {
        if (playerTarget != null)
        {
            Vector2 baseDir = (playerTarget.position - transform.position).normalized;
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
            float[] offsets = { -14f, 0f, 14f };

            foreach (float off in offsets)
            {
                float rad = (baseAngle + off) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                SpawnReflectableProjectile(
                    dir,
                    6.5f,
                    attackDamage,
                    new Color(0.25f, 0.05f, 0.45f),
                    new Color(0.10f, 0.85f, 0.95f),
                    0.54f,
                    ProjectileBehaviour.ProjectileType.Abyssal_Orb);
            }
        }
        yield return new WaitForSeconds(0.32f);
    }
}

/// <summary>
/// 11. Arquero Sombrío (Puny Ranged Bow): Tensa su arco en las columnas 8-10 y dispara flechas reflejables en la columna 11.
/// </summary>
public class CorruptedArcherAI : BaseEnemyAI
{
    protected override void Awake()
    {
        enemyDisplayName = "Arquero Sombrío";
        maxHealth = 34f;
        moveSpeed = 2.5f;
        detectionRange = 10.0f;
        attackRange = 6.0f;
        attackDamage = 11f;
        chargeTime = 0.38f;
        attackCooldown = 2.2f;
        heartDropChance = 0.85f;
        base.Awake();
    }

    protected override void ChaseBehavior(float distToPlayer)
    {
        Vector2 dir = (playerTarget.position - transform.position).normalized;
        if (distToPlayer > attackRange * 0.88f)
        {
            transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);
        }
        else if (distToPlayer < 2.4f)
        {
            transform.position -= (Vector3)(dir * (moveSpeed * 0.75f) * Time.deltaTime);
        }
    }

    protected override IEnumerator PerformAttackAction()
    {
        if (playerTarget != null)
        {
            Vector2 dir = (playerTarget.position - transform.position).normalized;
            SpawnReflectableProjectile(
                dir,
                11.5f,
                attackDamage,
                new Color(0.18f, 0.12f, 0.22f, 1f),
                new Color(0.95f, 0.25f, 0.35f, 1f),
                0.46f,
                ProjectileBehaviour.ProjectileType.Shadow_Arrow
            );
        }
        yield return new WaitForSeconds(0.28f);
    }
}

/// <summary>
/// 12. Soldado Corrompido: Guardia caído cuerpo a cuerpo con tajo de espada (Columnas 4-7 de Puny).
/// </summary>
public class CorruptedGuardAI : BaseEnemyAI
{
    protected override void Awake()
    {
        enemyDisplayName = "Soldado Corrompido";
        maxHealth = 42f;
        moveSpeed = 2.7f;
        detectionRange = 8.5f;
        attackRange = 1.40f;
        attackDamage = 13f;
        chargeTime = 0.30f;
        attackCooldown = 1.45f;
        heartDropChance = 0.85f;
        base.Awake();
    }

    protected override IEnumerator PerformAttackAction()
    {
        if (playerTarget != null)
        {
            Vector3 startPos = transform.position;
            Vector3 dir = (playerTarget.position - startPos).normalized;
            float t = 0f;
            while (t < 0.15f)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, startPos + dir * 0.45f, t / 0.15f);
                yield return null;
            }

            if (Vector2.Distance(transform.position, playerTarget.position) <= attackRange + 0.45f)
            {
                playerMovement?.RecibirDanio(attackDamage);
            }
        }
        yield return new WaitForSeconds(0.22f);
    }
}
