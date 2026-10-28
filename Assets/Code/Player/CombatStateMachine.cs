using System;
using UnityEngine;

/// <summary>
/// Máquina de estados finita para el combate de Ronan.
/// Controla transiciones, ventanas de contraataque (0.20s), esquive con invulnerabilidad y modo asalto.
/// </summary>
public class CombatStateMachine : MonoBehaviour
{
    public enum State
    {
        Idle,
        Attack,
        Block,
        Counter,
        Dodge,
        Hurt,
        Dead
    }

    [Header("Estado Actual")]
    [SerializeField] private State currentState = State.Idle;
    public State CurrentState => currentState;

    [Header("Parámetros Temporales (GDD Secciones 2.3 - 2.5)")]
    public float counterWindow = 0.20f;   // Ventana exacta para desviar proyectiles y parry
    public float attackDuration = 0.25f;  // Duración del tajo de espada
    public float dodgeDuration = 0.18f;   // Duración del impulso de esquive
    public float dodgeCooldown = 0.80f;   // Enfriamiento del esquive
    public float hurtRecovery = 0.35f;    // Tiempo de recuperación tras recibir golpe

    private float stateTimer = 0f;
    private float blockStartTime = -10f;
    private float lastDodgeTime = -10f;

    public bool IsInCounterWindow => currentState == State.Block && (Time.time - blockStartTime) <= counterWindow;
    public bool IsInvulnerable => currentState == State.Dodge || currentState == State.Counter;

    public event Action<State, State> OnStateChanged;
    public event Action OnCounterTriggered;
    public event Action OnAttackExecuted;
    public event Action OnDodgeExecuted;

    private void Update()
    {
        stateTimer += Time.deltaTime;

        switch (currentState)
        {
            case State.Attack:
                if (stateTimer >= attackDuration)
                {
                    ChangeState(State.Idle);
                }
                break;

            case State.Dodge:
                if (stateTimer >= dodgeDuration)
                {
                    ChangeState(State.Idle);
                }
                break;

            case State.Hurt:
                if (stateTimer >= hurtRecovery)
                {
                    ChangeState(State.Idle);
                }
                break;

            case State.Counter:
                // Permanece en contraataque brevemente para dar feedback visual y sonoro
                if (stateTimer >= 0.25f)
                {
                    ChangeState(State.Idle);
                }
                break;
        }
    }

    public bool CanAttack()
    {
        return currentState == State.Idle;
    }

    public bool CanBlock()
    {
        return currentState == State.Idle;
    }

    public bool CanDodge()
    {
        return (currentState == State.Idle || currentState == State.Block) && (Time.time - lastDodgeTime >= dodgeCooldown);
    }

    public void TriggerAttack()
    {
        if (!CanAttack()) return;
        ChangeState(State.Attack);
        OnAttackExecuted?.Invoke();
    }

    public void StartBlock()
    {
        if (!CanBlock()) return;
        blockStartTime = Time.time;
        ChangeState(State.Block);
    }

    public void ReleaseBlock()
    {
        if (currentState == State.Block)
        {
            ChangeState(State.Idle);
        }
    }

    public void TriggerDodge()
    {
        if (!CanDodge()) return;
        lastDodgeTime = Time.time;
        ChangeState(State.Dodge);
        OnDodgeExecuted?.Invoke();
    }

    public void TriggerCounter()
    {
        ChangeState(State.Counter);
        OnCounterTriggered?.Invoke();
    }

    public void TriggerHurt()
    {
        if (currentState == State.Dead) return;
        ChangeState(State.Hurt);
    }

    public void TriggerDeath()
    {
        ChangeState(State.Dead);
    }

    public void ForceIdle()
    {
        ChangeState(State.Idle);
    }

    public void ChangeState(State newState)
    {
        if (currentState == newState) return;

        State previous = currentState;
        currentState = newState;
        stateTimer = 0f;

        if (newState == State.Block)
        {
            blockStartTime = Time.time;
        }

        OnStateChanged?.Invoke(previous, newState);
    }
}
