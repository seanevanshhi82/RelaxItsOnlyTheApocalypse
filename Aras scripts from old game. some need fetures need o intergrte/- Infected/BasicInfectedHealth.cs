using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
public class BasicInfectedHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Starting health will be randomly chosen between Min and Max")]
    public float minHealth = 50f;
    public float maxHealth = 120f;

    [Header("Current Health (Visible in Inspector)")]
    [SerializeField, ReadOnly] private float currentHealthDisplay;

    public float CurrentHealth
    {
        get => currentHealthDisplay;
        private set => currentHealthDisplay = value;
    }

    [Header("Death Settings")]
    [Tooltip("Layer to switch to when dead (for feeding detection)")]
    public LayerMask deadLayer;
    public float deadYPosition = 0.1f;

    [Header("Feeding Regen")]
    [Tooltip("How much health regained per tick while feeding")]
    public float healthRegenAmount = 5f;
    [Tooltip("Time in seconds between regen ticks while feeding")]
    public float healthRegenRate = 2f;

    [Header("Reaction Settings")]
    [Tooltip("How long the hit reaction animation should play")]
    public float hitReactionDuration = 0.4f;

    [Header("Future Use")]
    public float rage = 0f;

    private Animator anim;
    private BasicInfectedAI aiScript;  // Reference to AI for hit-turn reaction
    private bool isDead = false;
    private float regenTimer = 0f;
    private float hitTimer = 0f;

    void Awake()
    {
        anim = GetComponent<Animator>();
        aiScript = GetComponent<BasicInfectedAI>();
        if (aiScript == null)
        {
            Debug.LogWarning("BasicInfectedHealth: No BasicInfectedAI component found on " + gameObject.name);
        }
    }

    void Start()
    {
        CurrentHealth = Random.Range(minHealth, maxHealth);
        currentHealthDisplay = CurrentHealth;
        isDead = false;

        anim.SetBool("isDead", false);
        anim.SetBool("isHit", false);
        anim.SetBool("isStunned", false);
        anim.SetBool("isKnockdown", false);
    }

    void Update()
    {
        if (isDead) return;

        // Feeding regen
        if (anim.GetBool("isFeeding"))
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= healthRegenRate)
            {
                CurrentHealth += healthRegenAmount;
                CurrentHealth = Mathf.Clamp(CurrentHealth, minHealth, maxHealth);
                currentHealthDisplay = CurrentHealth;
                regenTimer = 0f;
            }
        }
        else
        {
            regenTimer = 0f;
        }

        // Auto-reset hit reaction
        if (anim.GetBool("isHit"))
        {
            hitTimer += Time.deltaTime;
            if (hitTimer >= hitReactionDuration)
            {
                anim.SetBool("isHit", false);
                hitTimer = 0f;
            }
        }
    }

    // Overload 1: Damage without direction (e.g. fall, poison, old calls)
    public void TakeDamage(float damageAmount)
    {
        TakeDamage(damageAmount, Vector3.zero); // No direction → no turn reaction
    }

    // Overload 2: Damage WITH direction (from projectile or melee swing)
    public void TakeDamage(float damageAmount, Vector3 damageDirection)
    {
        if (isDead) return;

        CurrentHealth -= damageAmount;
        CurrentHealth = Mathf.Max(CurrentHealth, 0f);
        currentHealthDisplay = CurrentHealth;

        // Pass to AI for hit-turn reaction (rotate toward source)
        if (aiScript != null)
        {
            aiScript.TakeDamage(damageAmount, damageDirection.normalized);
        }

        // Trigger hit reaction animation
        anim.SetBool("isHit", true);
        hitTimer = 0f;

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    public void ApplyStun()
    {
        anim.SetBool("isStunned", true);
    }

    public void ApplyKnockdown()
    {
        anim.SetBool("isKnockdown", true);
    }

    private void Die()
    {
        isDead = true;
        anim.SetBool("isDead", true);
        anim.SetBool("isMoving", false);
        anim.SetBool("isAttacking", false);
        anim.SetBool("isFeeding", false);
        anim.SetBool("isIdle", false);
        anim.SetBool("isHit", false);
        anim.SetBool("isStunned", false);
        anim.SetBool("isKnockdown", false);
        anim.SetFloat("RandomAttack", 0f);

        if (aiScript != null)
        {
            aiScript.enabled = false;
        }

        var agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        gameObject.layer = LayerMask.NameToLayer("Dead");

        Vector3 pos = transform.position;
        pos.y = deadYPosition;
        transform.position = pos;
    }
}