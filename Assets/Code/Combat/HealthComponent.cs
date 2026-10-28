using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Componente de salud universal para Ronan, enemigos y jefes.
/// Soporta invulnerabilidad temporal, mitigación por bloqueo, eventos de vida y muerte.
/// </summary>
public class HealthComponent : MonoBehaviour
{
    [Header("Valores de Salud")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public bool isPlayer = false;

    [Header("Invulnerabilidad tras daño")]
    public float invulnerabilityDuration = 0.5f;
    private bool isInvulnerable = false;

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

    /// <summary>
    /// Aplica daño a la entidad considerando bloqueo, ventana de contraataque y knockback.
    /// Retorna true si el daño fue aplicado o false si fue evitado/desviado.
    /// </summary>
    public bool TakeDamage(float damage, Vector2 knockbackDir, float knockbackForce = 5f, GameObject attacker = null)
    {
        if (currentHealth <= 0f || isInvulnerable) return false;

        // Comprobación de combate para Ronan
        if (isPlayer && playerCombatState != null)
        {
            // 1. Si está esquivando o en contraataque, es totalmente invulnerable
            if (playerCombatState.IsInvulnerable) return false;

            // 2. Si está en la ventana exacta de contraataque (0.20s), desvía el golpe
            if (playerCombatState.IsInCounterWindow)
            {
                playerCombatState.TriggerCounter();
                PlayerMovement pm = GetComponent<PlayerMovement>();
                if (pm != null) pm.AñadirCargaAsalto(35f);

                SaveStateManager.Instance?.RegistrarContraataqueExitoso();
                AudioManager.Instance?.PlayCounterSound();
                Debug.Log("<color=#00E5FF>[HealthComponent] ¡Contraataque perfecto! Daño anulado y ataque devuelto.</color>");
                return false;
            }

            // 3. Si está bloqueando fuera de la ventana de 0.20s, mitiga el daño un 70%
            if (playerCombatState.CurrentState == CombatStateMachine.State.Block)
            {
                damage *= 0.30f; // Solo recibe el 30% del daño
                Debug.Log($"<color=#70A0FF>[HealthComponent] Bloqueo simple: Daño mitigado a {damage:F1} HP.</color>");
            }
            else
            {
                playerCombatState.TriggerHurt();
            }
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);
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
            StartCoroutine(EfectoParpadeoInvulnerabilidad());
        }
        else
        {
            Morir();
        }

        return true;
    }

    public void Heal(float amount)
    {
        if (currentHealth <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
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
    }

    private void Morir()
    {
        if (isPlayer)
        {
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
