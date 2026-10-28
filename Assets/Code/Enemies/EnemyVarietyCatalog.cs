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
        if (spriteSheet != null)
        {
            InitializeFromTexture(spriteSheet, columns, rows, pixelsPerUnit, isPuny24x8Format);
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
                grid[r, c] = Sprite.Create(readable, new Rect(texX, texY, cellW, cellH), new Vector2(0.5f, 0.5f), pixelsPerUnit);
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
            if (enemyAI != null)
            {
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
        else
        {
            if (enemyAI != null)
            {
                if (enemyAI.currentState == BaseEnemyAI.AIState.ChargeAttack)
                {
                    row = Mathf.Min(rows - 1, 2);
                    col = 0;
                }
                else if (enemyAI.currentState == BaseEnemyAI.AIState.ExecuteAttack)
                {
                    row = Mathf.Min(rows - 1, 2);
                    col = Mathf.Clamp(1 + (Mathf.FloorToInt(timer * 1.4f) % Mathf.Max(1, columns - 1)), 0, columns - 1);
                }
                else if (enemyAI.currentState == BaseEnemyAI.AIState.Chase)
                {
                    row = Mathf.Min(rows - 1, 1);
                    col = Mathf.FloorToInt(timer) % columns;
                }
                else
                {
                    row = 0;
                    col = Mathf.FloorToInt(timer * 0.7f) % columns;
                }
            }
            else
            {
                col = Mathf.FloorToInt(timer) % columns;
            }
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
