using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hitbox trigger 2D para golpes cuerpo a cuerpo (espada de Ronan, garras de enemigos, barridos de jefes).
/// Aplica daño y knockback a través del HealthComponent.
/// </summary>
public class HitboxComponent : MonoBehaviour
{
    [Header("Parámetros de Ataque")]
    public float damage = 12f;
    public float knockbackForce = 6f;
    public string targetTag = "Enemy";
    public bool canHitMultipleTargets = true;

    private List<GameObject> hitObjects = new List<GameObject>();
    private Collider2D col;
    private GameObject attackerOwner;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    public void ActivateHitbox(float attackDamage, GameObject owner, string target = "Enemy")
    {
        damage = attackDamage;
        attackerOwner = owner;
        targetTag = target;
        hitObjects.Clear();
        gameObject.SetActive(true);
        if (col != null) col.enabled = true;
    }

    public void DeactivateHitbox()
    {
        hitObjects.Clear();
        if (col != null) col.enabled = false;
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(targetTag) && !collision.transform.root.CompareTag(targetTag))
        {
            return;
        }

        GameObject rootTarget = collision.transform.root.gameObject;
        if (hitObjects.Contains(rootTarget)) return;

        HealthComponent hc = collision.GetComponent<HealthComponent>() ?? rootTarget.GetComponent<HealthComponent>();
        if (hc != null)
        {
            Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
            if (knockbackDir == Vector2.zero) knockbackDir = Vector2.up;

            bool damaged = hc.TakeDamage(damage, knockbackDir, knockbackForce, attackerOwner);
            if (damaged)
            {
                hitObjects.Add(rootTarget);
                // Si el atacante es Ronan, aumentar carga de Modo Asalto
                if (attackerOwner != null && attackerOwner.CompareTag("Player"))
                {
                    PlayerMovement pm = attackerOwner.GetComponent<PlayerMovement>();
                    if (pm != null) pm.AñadirCargaAsalto(10f);
                }
            }

            if (!canHitMultipleTargets)
            {
                DeactivateHitbox();
            }
        }
    }
}
