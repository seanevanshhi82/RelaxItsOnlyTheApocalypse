using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float healthRegenAmount = 2f;
    public float healthRegenRate = 3f;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float staminaRegenAmount = 8f;
    public float staminaRegenRate = 0.8f;

    [Header("Reaction Settings")]
    [Tooltip("How long the hit reaction animation should play")]
    public float hitReactionDuration = 0.35f;

    [Header("Current Health (Visible in Inspector)")]
    [SerializeField, ReadOnly] private float currentHealthDisplay;
    public float CurrentHealth
    {
        get => currentHealthDisplay;
        set => currentHealthDisplay = value;  // FIXED: public setter
    }

    [Header("Current Stamina (Visible in Inspector)")]
    [SerializeField, ReadOnly] private float currentStaminaDisplay;
    public float CurrentStamina
    {
        get => currentStaminaDisplay;
        set => currentStaminaDisplay = value;  // FIXED: public setter
    }

    private bool isDead = false;
    private float healthRegenTimer = 0f;
    private float staminaRegenTimer = 0f;
    private float hitTimer = 0f;
    private Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    void Start()
    {
        CurrentHealth = maxHealth;
        currentHealthDisplay = CurrentHealth;
        CurrentStamina = maxStamina;
        currentStaminaDisplay = CurrentStamina;
        isDead = false;
    }

    void Update()
    {
        if (isDead) return;

        // Health regen
        healthRegenTimer += Time.deltaTime;
        if (healthRegenTimer >= healthRegenRate)
        {
            CurrentHealth += healthRegenAmount;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);
            currentHealthDisplay = CurrentHealth;
            healthRegenTimer = 0f;
        }

        // Stamina regen
        staminaRegenTimer += Time.deltaTime;
        if (staminaRegenTimer >= staminaRegenRate)
        {
            CurrentStamina += staminaRegenAmount;
            CurrentStamina = Mathf.Clamp(CurrentStamina, 0f, maxStamina);
            currentStaminaDisplay = CurrentStamina;
            staminaRegenTimer = 0f;
        }

        // Auto-reset hit reaction
        if (anim != null && anim.GetBool("isHit"))
        {
            hitTimer += Time.deltaTime;
            if (hitTimer >= hitReactionDuration)
            {
                anim.SetBool("isHit", false);
                hitTimer = 0f;
            }
        }
    }

    // Overload 1: Damage without direction
    public void TakeDamage(float damageAmount)
    {
        TakeDamage(damageAmount, Vector3.zero);
    }

    // Overload 2: Damage WITH direction (for hit feedback)
    public void TakeDamage(float damageAmount, Vector3 damageDirection)
    {
        if (isDead) return;

        CurrentHealth -= damageAmount;
        CurrentHealth = Mathf.Max(CurrentHealth, 0f);
        currentHealthDisplay = CurrentHealth;

        if (anim != null)
        {
            anim.SetBool("isHit", true);
            hitTimer = 0f;
        }

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    public bool UseStamina(float amount)
    {
        if (CurrentStamina >= amount)
        {
            CurrentStamina -= amount;
            CurrentStamina = Mathf.Max(CurrentStamina, 0f);
            currentStaminaDisplay = CurrentStamina;
            staminaRegenTimer = 0f;
            return true;
        }
        return false;
    }

    public void TakeStaminaDamage(float amount)
    {
        UseStamina(amount);
    }

    public void ApplyStun()
    {
        if (anim != null)
        {
            anim.SetBool("isStunned", true);
        }
    }

    public void ApplyKnockdown()
    {
        if (anim != null)
        {
            anim.SetBool("isKnockdown", true);
        }
    }

    private void Die()
    {
        isDead = true;
        CurrentHealth = 0f;
        currentHealthDisplay = CurrentHealth;
        Debug.Log("Player died!");
        // TODO: death screen, ragdoll, respawn, etc.
    }

    public float GetHealthNormalized() => CurrentHealth / maxHealth;
    public float GetStaminaNormalized() => CurrentStamina / maxStamina;
}