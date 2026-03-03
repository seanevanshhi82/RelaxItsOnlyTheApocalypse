using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("Speed of the projectile")]
    public float speed = 20f;

    [Tooltip("Damage dealt on hit")]
    public float damage = 20f;

    [Tooltip("Lifetime before self-destruct (seconds)")]
    public float lifetime = 5f;

    [Header("Hit Layers")]
    [Tooltip("Layers this projectile can hit and apply damage to (e.g., Infected for player shots)")]
    public LayerMask hitLayers = -1; // Set in Inspector (e.g., only "Infected" + "Player")

    [Header("Knockback")]
    [Tooltip("Force applied to target on hit")]
    public float knockbackForce = 5f;

    [Tooltip("Upward force for knockback (helps with ragdoll feel)")]
    public float knockbackUpwardForce = 2f;

    [Header("Critical Hit")]
    [Tooltip("Chance of critical hit (0-1)")]
    [Range(0f, 1f)] public float critChance = 0.15f;

    [Tooltip("Damage multiplier on critical hit")]
    public float critMultiplier = 2f;

    [Header("Stun & Knockdown")]
    [Tooltip("Chance to stun target on hit (0-1)")]
    [Range(0f, 1f)] public float stunChance = 0.3f;

    [Tooltip("Chance to knockdown target on hit (0-1)")]
    [Range(0f, 1f)] public float knockdownChance = 0.4f;

    private GameObject shooter; // Who fired this
    private float spawnTime;

    public void Launch(GameObject shooter, Vector3 direction)
    {
        this.shooter = shooter;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * speed;
        }
        spawnTime = Time.time;
    }

    void Update()
    {
        if (Time.time > spawnTime + lifetime)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == shooter) return; // Don't hit self

        // Check if hit object is on allowed layer
        if (((1 << other.gameObject.layer) & hitLayers.value) == 0)
        {
            Debug.Log("Projectile ignored hit on " + other.gameObject.name + " — not on hitLayers");
            return;
        }

        // Try to find health component on the hit object or its root
        BasicInfectedHealth zombieHealth = other.GetComponentInParent<BasicInfectedHealth>();
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (zombieHealth != null)
        {
            ApplyDamage(zombieHealth, other.transform.root.gameObject);
        }
        else if (playerHealth != null)
        {
            ApplyDamage(playerHealth, other.transform.root.gameObject);
        }
        else
        {
            Debug.Log("Projectile hit object with no health: " + other.gameObject.name);
        }
    }

    private void ApplyDamage(object healthComponent, GameObject targetRoot)
    {
        float finalDamage = damage;

        // Critical hit check
        bool isCrit = Random.value <= critChance;
        if (isCrit)
        {
            finalDamage *= critMultiplier;
            Debug.Log("Critical hit! Damage: " + finalDamage + " on " + targetRoot.name);
        }

        // Apply damage
        if (healthComponent is BasicInfectedHealth zombie)
        {
            Vector3 damageDir = (targetRoot.transform.position - transform.position).normalized;
            zombie.TakeDamage(finalDamage, damageDir);
            Debug.Log("Projectile damaged zombie! Damage: " + finalDamage + " to " + targetRoot.name);
        }
        else if (healthComponent is PlayerHealth player)
        {
            Vector3 damageDir = (targetRoot.transform.position - transform.position).normalized;
            player.TakeDamage(finalDamage, damageDir);
            Debug.Log("Projectile damaged player! Damage: " + finalDamage + " to " + targetRoot.name);
        }

        // Knockback
        Rigidbody rb = targetRoot.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 knockDir = (targetRoot.transform.position - transform.position).normalized;
            knockDir.y = knockbackUpwardForce;
            rb.AddForce(knockDir * knockbackForce, ForceMode.Impulse);
        }
        else
        {
            CharacterController cc = targetRoot.GetComponent<CharacterController>();
            if (cc != null)
            {
                Vector3 knockDir = (targetRoot.transform.position - transform.position).normalized;
                knockDir.y = knockbackUpwardForce;
                cc.Move(knockDir * knockbackForce * Time.deltaTime);
            }
        }

        // Stun chance
        if (Random.value <= stunChance)
        {
            if (healthComponent is BasicInfectedHealth zombieStun)
            {
                // Add stun logic if BasicInfectedHealth has ApplyStun()
                Debug.Log("Zombie stunned by projectile!");
            }
            else if (healthComponent is PlayerHealth playerStun)
            {
                playerStun.ApplyStun();
                Debug.Log("Player stunned by projectile!");
            }
        }

        // Knockdown chance
        if (Random.value <= knockdownChance)
        {
            if (healthComponent is BasicInfectedHealth zombieKnock)
            {
                // Add knockdown logic if BasicInfectedHealth has ApplyKnockdown()
                Debug.Log("Zombie knocked down by projectile!");
            }
            else if (healthComponent is PlayerHealth playerKnock)
            {
                playerKnock.ApplyKnockdown();
                Debug.Log("Player knocked down by projectile!");
            }
        }

        // Optional: trigger isHit animation if target has Animator
        Animator targetAnim = targetRoot.GetComponent<Animator>();
        if (targetAnim != null)
        {
            targetAnim.SetTrigger("isHit");
            Debug.Log("Triggered isHit on " + targetRoot.name);
        }
    }
}