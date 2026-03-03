using UnityEngine;
using System.Collections.Generic;

public class MeleeDamageCollider : MonoBehaviour
{
    [Header("Damage")]
    [Tooltip("Damage dealt to infected targets (player/NPC attacking)")]
    public float damagePlayer = 25f;

    [Tooltip("Damage dealt to players/NPCs (infected attacking)")]
    public float damageInfected = 15f;

    [Header("Hit Layers")]
    [Tooltip("Layers player/NPC colliders can hit (e.g., Infected)")]
    public LayerMask playerHitLayers = -1;

    [Tooltip("Layers infected colliders can hit (e.g., Player)")]
    public LayerMask infectedHitLayers = -1;

    private Collider playerCol;    // For player/NPC hands
    private Collider infectedCol;  // For infected hands

    private HashSet<GameObject> infectedHitThisSwing = new HashSet<GameObject>();
    private HashSet<GameObject> playerHitThisSwing = new HashSet<GameObject>();

    private bool isInfected; // Auto-detected from root object layer

    void Awake()
    {
        // Auto-detect if this is on an infected character (check ROOT GameObject layer)
        Transform root = transform.root;
        isInfected = root.gameObject.layer == LayerMask.NameToLayer("Infected");

        Collider[] colliders = GetComponents<Collider>();
        if (colliders.Length == 0)
        {
            Debug.LogError("MeleeDamageCollider: No Collider found on " + gameObject.name);
            enabled = false;
            return;
        }

        // Assign colliders (first = player/NPC, second = infected — or drag specific ones in Inspector)
        playerCol = colliders.Length > 0 ? colliders[0] : null;
        infectedCol = colliders.Length > 1 ? colliders[1] : null;

        if (playerCol != null)
        {
            playerCol.isTrigger = true;
            playerCol.enabled = false;
        }
        if (infectedCol != null)
        {
            infectedCol.isTrigger = true;
            infectedCol.enabled = false;
        }

        Debug.Log(gameObject.name + " MeleeDamageCollider initialized — isInfected: " + isInfected +
                  " (detected from root layer: " + LayerMask.LayerToName(root.gameObject.layer) + ")");
    }

    // Called by animation event at start of swing (for infected hands)
    public void EnableInfectedCollider()
    {
        if (infectedCol == null) return;
        infectedCol.enabled = true;
        infectedHitThisSwing.Clear();
        Debug.Log("Infected melee collider ENABLED on " + gameObject.name);
    }

    // Called by animation event at end of swing (for infected hands)
    public void DisableInfectedCollider()
    {
        if (infectedCol == null) return;
        infectedCol.enabled = false;
        infectedHitThisSwing.Clear();
        Debug.Log("Infected melee collider DISABLED on " + gameObject.name);
    }

    // Called by animation event at start of swing (for player/NPC hands)
    public void EnablePlayerCollider()
    {
        if (playerCol == null) return;
        playerCol.enabled = true;
        playerHitThisSwing.Clear();
        Debug.Log("Player/NPC melee collider ENABLED on " + gameObject.name);
    }

    // Called by animation event at end of swing (for player/NPC hands)
    public void DisablePlayerCollider()
    {
        if (playerCol == null) return;
        playerCol.enabled = false;
        playerHitThisSwing.Clear();
        Debug.Log("Player/NPC melee collider DISABLED on " + gameObject.name);
    }

    void OnTriggerEnter(Collider other)
    {
        Collider activeCol = isInfected ? infectedCol : playerCol;
        if (activeCol == null || !activeCol.enabled) return;

        // Choose the correct layer mask and hit set based on attacker type
        LayerMask activeLayers = isInfected ? infectedHitLayers : playerHitLayers;
        HashSet<GameObject> hitSet = isInfected ? infectedHitThisSwing : playerHitThisSwing;

        // Ignore if not on allowed layer
        if (((1 << other.gameObject.layer) & activeLayers.value) == 0) return;

        GameObject target = other.gameObject;

        // Only hit once per swing per target
        if (hitSet.Contains(target)) return;
        hitSet.Add(target);

        // Apply damage based on who is attacking
        if (isInfected)
        {
            // Infected attacking players/NPCs
            PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Vector3 damageDir = (target.transform.position - transform.position).normalized;
                playerHealth.TakeDamage(damageInfected, damageDir);
                Debug.Log("Infected melee hit player/NPC! Damage: " + damageInfected);
            }
        }
        else
        {
            // Player/NPC attacking infected
            BasicInfectedHealth zombieHealth = target.GetComponent<BasicInfectedHealth>();
            if (zombieHealth != null)
            {
                Vector3 damageDir = (target.transform.position - transform.position).normalized;
                zombieHealth.TakeDamage(damagePlayer, damageDir);
                Debug.Log("Player/NPC melee hit infected! Damage: " + damagePlayer);
            }
        }
    }
}