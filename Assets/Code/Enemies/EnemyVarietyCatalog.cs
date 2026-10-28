using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Animador universal en tiempo de ejecución para todas las hojas de sprites de enemigos
/// (Goblins, Bombarderos, Slimes, Hongos, Gusanos, Orcos, Magos y Soldados Corrompidos).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class EnemySpriteSheetAnimator : MonoBehaviour
{
    public Texture2D spriteSheet;
    public int columns = 6;
    public int rows = 4;
    public float pixelsPerUnit = 16f;
    public float fps = 9f;
    public bool isPuny24x8Format = false;

    private SpriteRenderer sr;
    private BaseEnemyAI enemyAI;
    private Sprite[,] grid;
    private float timer = 0f;
    private bool loaded = false;

    private static readonly Dictionary<Texture2D, Texture2D> readableCopies = new Dictionary<Texture2D, Texture2D>();

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        enemyAI = GetComponent<BaseEnemyAI>();
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
                // Crear sub-textura aislada con Clamp + Point para evitar sangrado de frames vecinos
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

    private static Texture2D GetReadableTexture(Texture2D source)
    {
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

        timer += Time.deltaTime * fps;
        int row = 0;
        int col = 0;

        if (isPuny24x8Format && columns >= 24 && rows >= 8)
        {
            // Determinar fila direccional (0=Sur, 2=Oeste, 4=Norte, 6=Este)
            row = 0;
            if (enemyAI != null)
            {
                PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
                if (pm != null)
                {
                    Vector2 dir = (pm.transform.position - transform.position).normalized;
                    if (Mathf.Abs(dir.y) >= Mathf.Abs(dir.x))
                        row = dir.y > 0f ? 4 : 0;
                    else
                        row = dir.x > 0f ? 6 : 2;
                    if (sr != null) sr.flipX = false;
                }

                if (enemyAI.currentState == BaseEnemyAI.AIState.ChargeAttack)
                {
                    col = 4;
                }
                else if (enemyAI.currentState == BaseEnemyAI.AIState.ExecuteAttack)
                {
                    col = 5 + (Mathf.FloorToInt(timer * 1.5f) % 3);
                }
                else if (enemyAI.currentState == BaseEnemyAI.AIState.Chase || enemyAI.currentState == BaseEnemyAI.AIState.Patrol)
                {
                    int[] walkCols = { 1, 2, 3, 2 };
                    col = walkCols[Mathf.FloorToInt(timer) % 4];
                }
                else
                {
                    col = 0;
                }
            }
        }
        else if (columns == 6 && rows == 5)
        {
            // goblin_spritesheet.png (96x80): RowTop 0=Walk(6), RowTop 3=Idle(4)
            bool moving = enemyAI != null && (enemyAI.currentState == BaseEnemyAI.AIState.Chase || enemyAI.currentState == BaseEnemyAI.AIState.Patrol || enemyAI.currentState == BaseEnemyAI.AIState.ExecuteAttack);
            row = moving ? 0 : 3;
            int maxC = moving ? 6 : 4;
            col = Mathf.FloorToInt(timer) % maxC;
        }
        else if (columns == 6 && rows == 4)
        {
            // goblin_bomber_spritesheet.png (96x64): RowTop 0=Throw(6), RowTop 2=Idle/Walk(4)
            bool attacking = enemyAI != null && (enemyAI.currentState == BaseEnemyAI.AIState.ChargeAttack || enemyAI.currentState == BaseEnemyAI.AIState.ExecuteAttack);
            row = attacking ? 0 : 2;
            int maxC = attacking ? 6 : 4;
            col = Mathf.FloorToInt(timer) % maxC;
        }
        else if (columns == 8 && rows == 4)
        {
            // mushroom_spritesheet.png (128x64): RowTop 0=Walk(8), RowTop 1=Attack(6)
            bool attacking = enemyAI != null && (enemyAI.currentState == BaseEnemyAI.AIState.ChargeAttack || enemyAI.currentState == BaseEnemyAI.AIState.ExecuteAttack);
            row = attacking ? 1 : 0;
            int maxC = attacking ? 6 : 8;
            col = Mathf.FloorToInt(timer) % maxC;
        }
        else if (columns == 15 && rows == 5)
        {
            // slime_spritesheet.png (240x80): RowTop 1=Walk(0..5), RowTop 3=Idle(0..4)
            bool moving = enemyAI != null && (enemyAI.currentState == BaseEnemyAI.AIState.Chase || enemyAI.currentState == BaseEnemyAI.AIState.Patrol || enemyAI.currentState == BaseEnemyAI.AIState.ExecuteAttack);
            row = moving ? 1 : 3;
            int maxC = moving ? 6 : 5;
            col = Mathf.FloorToInt(timer) % maxC;
        }
        else if (columns == 6 && rows == 3)
        {
            // worm_spritesheet.png (96x24, 16x8): RowTop 0=Walk(0..5)
            row = 0;
            col = Mathf.FloorToInt(timer) % 6;
        }
        else if (columns == 15 && rows == 1)
        {
            // Slime.png (480x32, 32x32): RowTop 0, cols 0..5
            row = 0;
            col = Mathf.FloorToInt(timer) % 6;
        }
        else
        {
            row = 0;
            col = Mathf.FloorToInt(timer) % Mathf.Max(1, columns);
        }

        sr.sprite = grid[Mathf.Clamp(row, 0, rows - 1), Mathf.Clamp(col, 0, columns - 1)];
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
        moveSpeed = 3.6f;
        detectionRange = 8.0f;
        attackRange = 1.2f;
        attackDamage = 10f;
        chargeTime = 0.22f;
        attackCooldown = 1.1f;
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
        moveSpeed = 2.4f;
        detectionRange = 9.5f;
        attackRange = 5.2f;
        attackDamage = 14f;
        chargeTime = 0.30f;
        attackCooldown = 2.2f;
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
                6.2f,
                attackDamage,
                new Color(0.20f, 0.12f, 0.05f, 1f),
                new Color(1f, 0.55f, 0.10f, 1f),
                0.62f
            );
        }
        yield return new WaitForSeconds(0.20f);
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
        moveSpeed = 2.5f;
        detectionRange = 7.5f;
        attackRange = 1.15f;
        attackDamage = 9f;
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
/// 7. Hongo Espora: Emboscador en jardines que libera proyectiles de esporas reflejables en cruz.
/// </summary>
public class SporeMushroomEnemyAI : BaseEnemyAI
{
    protected override void Awake()
    {
        enemyDisplayName = "Hongo Espora";
        maxHealth = 34f;
        moveSpeed = 1.6f;
        detectionRange = 8.5f;
        attackRange = 4.6f;
        attackDamage = 11f;
        chargeTime = 0.35f;
        attackCooldown = 2.5f;
        base.Awake();
    }

    protected override IEnumerator PerformAttackAction()
    {
        if (playerTarget != null)
        {
            Vector2 mainDir = (playerTarget.position - transform.position).normalized;
            SpawnReflectableProjectile(mainDir, 5.8f, attackDamage, new Color(0.35f, 0.08f, 0.45f), new Color(0.85f, 0.35f, 1f), 0.48f);
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
        maxHealth = 38f;
        moveSpeed = 1.8f;
        detectionRange = 9.0f;
        attackRange = 5.0f;
        attackDamage = 13f;
        chargeTime = 0.32f;
        attackCooldown = 2.3f;
        base.Awake();
    }

    protected override IEnumerator PerformAttackAction()
    {
        if (playerTarget != null)
        {
            Vector2 dir = (playerTarget.position - transform.position).normalized;
            SpawnReflectableProjectile(dir, 6.6f, attackDamage, new Color(0.10f, 0.35f, 0.08f), new Color(0.45f, 1f, 0.20f), 0.52f);
        }
        yield return new WaitForSeconds(0.22f);
    }
}

/// <summary>
/// 9. Guerrero / Bruto Oscuro (Orco): Enemigo pesado con ataque de barrido amplio que incentiva usar Esquive (Espacio).
/// </summary>
public class DarkOrcBruteAI : BaseEnemyAI
{
    protected override void Awake()
    {
        enemyDisplayName = "Bruto Oscuro";
        maxHealth = 58f;
        moveSpeed = 2.3f;
        detectionRange = 8.5f;
        attackRange = 1.55f;
        attackDamage = 18f;
        chargeTime = 0.42f;
        attackCooldown = 1.75f;
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
                transform.position = Vector3.Lerp(startPos, startPos + dir * 0.50f, t / 0.16f);
                yield return null;
            }

            if (Vector2.Distance(transform.position, playerTarget.position) <= attackRange + 0.50f)
            {
                playerMovement?.RecibirDanio(attackDamage);
            }
        }
        yield return new WaitForSeconds(0.25f);
    }
}

/// <summary>
/// 10. Mago del Olvido: Conjurador de élite que dispara ráfagas triples reflejables para cargar Modo Asalto.
/// </summary>
public class OblivionMageAI : BaseEnemyAI
{
    protected override void Awake()
    {
        enemyDisplayName = "Mago del Olvido";
        maxHealth = 42f;
        moveSpeed = 2.2f;
        detectionRange = 10.5f;
        attackRange = 6.0f;
        attackDamage = 14f;
        chargeTime = 0.36f;
        attackCooldown = 2.6f;
        base.Awake();
    }

    protected override IEnumerator PerformAttackAction()
    {
        if (playerTarget != null)
        {
            Vector2 baseDir = (playerTarget.position - transform.position).normalized;
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
            float[] offsets = { -16f, 0f, 16f };

            foreach (float off in offsets)
            {
                float rad = (baseAngle + off) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                SpawnReflectableProjectile(dir, 6.8f, attackDamage, new Color(0.08f, 0.12f, 0.38f), new Color(0.20f, 0.90f, 1f), 0.50f);
            }
        }
        yield return new WaitForSeconds(0.25f);
    }
}

/// <summary>
/// 11. Soldado Corrompido: Guardia caído bajo la niebla con estocada de lanza/espada.
/// </summary>
public class CorruptedGuardAI : BaseEnemyAI
{
    protected override void Awake()
    {
        enemyDisplayName = "Soldado Corrompido";
        maxHealth = 45f;
        moveSpeed = 2.8f;
        detectionRange = 8.5f;
        attackRange = 1.40f;
        attackDamage = 15f;
        chargeTime = 0.30f;
        attackCooldown = 1.45f;
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
                transform.position = Vector3.Lerp(startPos, startPos + dir * 0.48f, t / 0.15f);
                yield return null;
            }

            if (Vector2.Distance(transform.position, playerTarget.position) <= attackRange + 0.45f)
            {
                playerMovement?.RecibirDanio(attackDamage);
            }
        }
        yield return new WaitForSeconds(0.20f);
    }
}

