using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Componente de salud universal para Ronan, enemigos y jefes.
/// Soporta invulnerabilidad temporal, mitigación por bloqueo, contraataque activo, eventos de vida y muerte.
/// </summary>
public class HealthComponent : MonoBehaviour
{
    [Header("Valores de Salud")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public bool isPlayer = false;

    [Header("Invulnerabilidad tras daño")]
    public float invulnerabilityDuration = 0.5f;
    [HideInInspector] public float invulnerableTimer;
    [HideInInspector] public bool isDead;
    private bool isInvulnerable = false;
    private Coroutine invulnerabilityCoroutine;

    [Header("Efecto de Parpadeo")]
    public SpriteRenderer spriteRenderer;
    public Color hurtColor = new Color(1f, 0.3f, 0.3f, 0.9f);

    [Header("Eventos")]
    public UnityEvent<float, float> onHealthChanged;
    public UnityEvent<float> onDamaged;
    public UnityEvent<float> onHealed;
    public UnityEvent onDeath;

    private Rigidbody2D rb;
    private CombatStateMachine playerCombatState;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCombatState = GetComponent<CombatStateMachine>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        // Si es Ronan y existe el SaveStateManager, sincronizar vida máxima acumulada
        if (isPlayer && SaveStateManager.Instance != null)
        {
            maxHealth = SaveStateManager.Instance.ObtenerVidaMaximaTotal();
            currentHealth = Mathf.Clamp(SaveStateManager.Instance.currentHealth, 1f, maxHealth);
        }
        else
        {
            currentHealth = maxHealth;
        }
    }

    private void Start()
    {
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public bool IsInvulnerable => isInvulnerable || invulnerableTimer > 0f;

    /// <summary>
    /// Aplica daño a la entidad considerando bloqueo, ventana de contraataque y knockback.
    /// Retorna true si el daño fue aplicado o false si fue evitado/desviado.
    /// </summary>
    public bool TakeDamage(float damage, Vector2 knockbackDir, float knockbackForce = 5f, GameObject attacker = null)
    {
        if (isDead || currentHealth <= 0f || IsInvulnerable) return false;

        // Comprobación de combate para Ronan
        if (isPlayer)
        {
            PlayerMovement pm = GetComponent<PlayerMovement>();

            // 1. Si está esquivando o en contraataque, es totalmente invulnerable
            if ((playerCombatState != null && playerCombatState.IsInvulnerable) ||
                (pm != null && (pm.currentState == PlayerMovement.CombatState.Counter || pm.currentState == PlayerMovement.CombatState.Dodge)))
            {
                return false;
            }

            // 2. Si está en la ventana de contraataque, desvía el golpe y daña al enemigo con 200% de ataque
            if ((playerCombatState != null && playerCombatState.IsInCounterWindow) ||
                (pm != null && pm.EstaEnVentanaDeContraataque()))
            {
                if (playerCombatState != null) playerCombatState.TriggerCounter();
                if (pm != null)
                {
                    pm.EjecutarContraataque(attacker, 2.0f);
                }

                SaveStateManager.Instance?.RegistrarContraataqueExitoso();
                AudioManager.Instance?.PlayCounterSound();
                Debug.Log("<color=#00E5FF>[HealthComponent] ¡Contraataque perfecto! Daño anulado y ataque devuelto al enemigo.</color>");
                return false;
            }

            // 3. Si está bloqueando fuera de la ventana inicial, mitiga 75% y devuelve golpe de guardia (125%)
            if ((playerCombatState != null && playerCombatState.CurrentState == CombatStateMachine.State.Block) ||
                (pm != null && pm.currentState == PlayerMovement.CombatState.Block))
            {
                damage *= 0.25f;
                if (pm != null)
                {
                    pm.EjecutarContraataque(attacker, 1.25f);
                }
                Debug.Log($"<color=#70A0FF>[HealthComponent] Contraataque de guardia: Daño mitigado a {damage:F1} HP y golpe devuelto.</color>");
            }
            else if (playerCombatState != null)
            {
                playerCombatState.TriggerHurt();
            }
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        if (isPlayer)
        {
            PlayerMovement player = GetComponent<PlayerMovement>();
            if (player != null) player.currentHealth = currentHealth;
            if (SaveStateManager.Instance != null) SaveStateManager.Instance.currentHealth = currentHealth;
        }
        onDamaged?.Invoke(damage);
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        // Knockback
        if (rb != null && knockbackForce > 0f)
        {
            rb.AddForce(knockbackDir.normalized * knockbackForce, ForceMode2D.Impulse);
        }

        // Efecto visual de daño
        if (currentHealth > 0f)
        {
            invulnerabilityCoroutine = StartCoroutine(EfectoParpadeoInvulnerabilidad());
        }
        else
        {
            HandleDeath();
        }

        return true;
    }

    public void Heal(float amount)
    {
        if (currentHealth <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        if (isPlayer)
        {
            PlayerMovement player = GetComponent<PlayerMovement>();
            if (player != null) player.currentHealth = currentHealth;
        }
        onHealed?.Invoke(amount);
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (isPlayer && SaveStateManager.Instance != null)
        {
            SaveStateManager.Instance.currentHealth = currentHealth;
        }
    }

    public void ActualizarVidaMaxima(float nuevaVidaMax)
    {
        maxHealth = nuevaVidaMax;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void RestablecerTrasDerrota(float nuevaVidaMaxima, float nuevaVidaActual)
    {
        if (invulnerabilityCoroutine != null)
        {
            StopCoroutine(invulnerabilityCoroutine);
            invulnerabilityCoroutine = null;
        }

        isDead = false;
        isInvulnerable = false;
        invulnerableTimer = 0f;
        maxHealth = nuevaVidaMaxima;
        currentHealth = Mathf.Clamp(nuevaVidaActual, 0f, maxHealth);
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SincronizarVida(float nuevaVidaActual)
    {
        currentHealth = Mathf.Clamp(nuevaVidaActual, 0f, maxHealth);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        if (currentHealth <= 0f) HandleDeath();
    }

    public void ActivarInvulnerabilidadTemporal(float duracion)
    {
        if (invulnerabilityCoroutine != null)
        {
            StopCoroutine(invulnerabilityCoroutine);
        }

        invulnerableTimer = Mathf.Max(0f, duracion);
        isInvulnerable = invulnerableTimer > 0f;
        invulnerabilityCoroutine = StartCoroutine(EfectoParpadeoRespawn());
    }

    private IEnumerator EfectoParpadeoInvulnerabilidad()
    {
        isInvulnerable = true;
        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        float elapsed = 0f;
        float blinkRate = 0.08f;

        while (elapsed < invulnerabilityDuration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = hurtColor;
            }
            yield return new WaitForSeconds(blinkRate);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
            yield return new WaitForSeconds(blinkRate);
            elapsed += blinkRate * 2;
        }

        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        isInvulnerable = false;
        invulnerabilityCoroutine = null;
    }

    private IEnumerator EfectoParpadeoRespawn()
    {
        bool visible = true;
        while (invulnerableTimer > 0f)
        {
            invulnerableTimer -= Time.unscaledDeltaTime;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = visible ? new Color(1f, 0.88f, 0.35f, 0.65f) : Color.white;
            }
            visible = !visible;
            yield return null;
        }

        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        isInvulnerable = false;
        invulnerabilityCoroutine = null;
    }

    public void HandleDeath()
    {
        if (isDead) return;
        isDead = true;
        isInvulnerable = false;
        invulnerableTimer = 0f;
        if (invulnerabilityCoroutine != null)
        {
            StopCoroutine(invulnerabilityCoroutine);
            invulnerabilityCoroutine = null;
        }

        if (isPlayer)
        {
            PlayerMovement player = GetComponent<PlayerMovement>();
            if (player != null) player.currentHealth = 0f;
            if (SaveStateManager.Instance != null) SaveStateManager.Instance.currentHealth = 0f;
            if (playerCombatState != null) playerCombatState.TriggerDeath();
            GameManager.Instance?.ActivarGameOver();
        }
        else
        {
            SaveStateManager.Instance?.RegistrarEnemigoDerrotado();
        }

        onDeath?.Invoke();
    }
}
