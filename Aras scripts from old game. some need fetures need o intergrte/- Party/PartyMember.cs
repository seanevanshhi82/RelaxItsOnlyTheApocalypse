using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
public class PartyMember : MonoBehaviour
{
    [Header("Follower Settings")]
    [Tooltip("The current party leader (auto-set by PartyManager)")]
    public Transform leader;

    [Tooltip("How close the member tries to stay to the leader (meters)")]
    public float followDistance = 3f;

    [Tooltip("How far behind the leader they aim to be (meters)")]
    public float minFollowDistance = 1.5f;

    [Tooltip("Speed multiplier when following (relative to leader speed)")]
    [Range(0.8f, 1.2f)] public float followSpeedMultiplier = 1.0f;

    [Header("Wedge Formation Offsets (for 3 total party)")]
    [Tooltip("Left offset from leader forward direction (meters)")]
    public float wedgeLeftOffset = -1.2f;

    [Tooltip("Right offset from leader forward direction (meters)")]
    public float wedgeRightOffset = 1.2f;

    [Header("Character Stats (1-100 scale)")]
    [Tooltip("Discipline determines how tightly they follow formation (low = loose, high = tight)")]
    [Range(1, 100)] public int Discipline = 50;

    [Tooltip("Morale (affects mood/idle behaviors later)")]
    [Range(1, 100)] public int Morale = 70;

    [Tooltip("Hopefulness (affects reaction to danger later)")]
    [Range(1, 100)] public int Hopefulness = 60;

    [Tooltip("Loyalty (affects willingness to follow orders later)")]
    [Range(1, 100)] public int Loyalty = 80;

    [Header("Idle Behavior")]
    [Tooltip("Random idle time range (seconds) when close to leader")]
    public Vector2 idleTimeRange = new Vector2(5f, 15f);

    [Tooltip("Chance to play idle animation when stopped (0-1)")]
    [Range(0f, 1f)] public float idleAnimationChance = 0.6f;

    [Header("Attack Behavior")]
    [Tooltip("Only attack if leader attacks or enemy detected")]
    public bool attackOnlyIfLeaderAttacksOrDetected = true;

    [Tooltip("Range to detect enemies (meters)")]
    public float detectionRange = 15f;

    [Tooltip("Attack range (meters)")]
    public float attackRange = 5f;

    [Header("Debug")]
    [Tooltip("Log follower status in console?")]
    public bool debugLogs = false;

    private NavMeshAgent agent;
    public Animator anim;
    private WeaponHandler weaponHandler;
    private PlayerInput playerInput;
    private PlayerHealth playerHealth;
    private float idleTimer = 0f;
    private bool isIdling = false;
    private Transform targetEnemy;
    private float lastHealth; // For detecting damage

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        weaponHandler = GetComponent<WeaponHandler>();
        playerInput = GetComponent<PlayerInput>();
        playerHealth = GetComponent<PlayerHealth>();

        if (agent == null)
        {
            Debug.LogError("PartyMember: No NavMeshAgent found on " + gameObject.name);
            enabled = false;
            return;
        }

        if (weaponHandler == null)
        {
            Debug.LogWarning("PartyMember: No WeaponHandler found on " + gameObject.name + " — weapon mirroring disabled");
        }

        if (playerHealth != null)
        {
            lastHealth = playerHealth.CurrentHealth; // Initialize last health
        }
    }

    void Start()
    {
        idleTimer = Random.Range(idleTimeRange.x, idleTimeRange.y);
    }

    void Update()
    {
        if (leader == null)
        {
            if (debugLogs) Debug.LogWarning(gameObject.name + " has no leader assigned!");
            return;
        }

        // Mirror leader's combat mode
        if (weaponHandler != null)
        {
            int leaderMode = leader.GetComponent<Animator>().GetInteger("CombatMode");
            weaponHandler.anim.SetInteger("CombatMode", leaderMode);
        }

        // Check for closest enemy in detection range
        FindClosestEnemy();

        float distanceToLeader = Vector3.Distance(transform.position, leader.position);

        // Wedge formation offset based on Discipline (lower = more spread)
        float offsetMultiplier = (100f - Discipline) / 100f; // 0 Discipline = full spread, 100 = tight
        Vector3 leftPos = leader.position - leader.forward * minFollowDistance + leader.right * wedgeLeftOffset * offsetMultiplier;
        Vector3 rightPos = leader.position - leader.forward * minFollowDistance + leader.right * wedgeRightOffset * offsetMultiplier;

        // Alternate positions for 2 members (simple 2-person wedge)
        Vector3 targetPos = (gameObject.name.Contains("1") || gameObject.name.Contains("Left")) ? leftPos : rightPos;

        // If leader is unarmed (CombatMode = 0), use loose formation/free walk
        if (leader.GetComponent<Animator>().GetInteger("CombatMode") == 0)
        {
            weaponHandler?.anim.SetInteger("CombatMode", 0);
            agent.speed = leader.GetComponent<NavMeshAgent>().speed * 0.8f; // Slightly slower
            anim.SetBool("isMoving", true);
            agent.SetDestination(leader.position); // Loose follow
        }
        else
        {
            // Normal wedge formation
            if (distanceToLeader > followDistance)
            {
                // Too far → chase leader position
                agent.SetDestination(targetPos);
                agent.speed = leader.GetComponent<NavMeshAgent>().speed * followSpeedMultiplier;
                anim.SetBool("isMoving", true);
                isIdling = false;
                idleTimer = Random.Range(idleTimeRange.x, idleTimeRange.y);
            }
            else if (distanceToLeader < minFollowDistance)
            {
                // Too close → stop and idle
                agent.ResetPath();
                anim.SetBool("isMoving", false);

                idleTimer -= Time.deltaTime;
                if (idleTimer <= 0f && !isIdling)
                {
                    isIdling = true;
                    if (Random.value < idleAnimationChance)
                    {
                        anim.SetTrigger("IdleVariation");
                        if (debugLogs) Debug.Log(gameObject.name + " playing idle variation");
                    }
                    idleTimer = Random.Range(idleTimeRange.x, idleTimeRange.y);
                }
            }
            else
            {
                // In sweet spot → walk slowly or idle
                agent.speed = leader.GetComponent<NavMeshAgent>().speed * 0.5f;
                anim.SetBool("isMoving", true);
                isIdling = false;
            }
        }

        // Leader mode: read move input and set animator Speed
        if (playerInput != null && playerInput.enabled)
        {
            Vector2 moveInput = playerInput.actions["Move"].ReadValue<Vector2>();
            float speed = moveInput.magnitude;
            anim.SetFloat("Speed", speed);
        }

        // Health monitoring (for damage & death)
        if (playerHealth != null)
        {
            float currentHealth = playerHealth.CurrentHealth;

            // Damage taken → trigger isHit
            if (currentHealth < lastHealth)
            {
                anim.SetTrigger("isHit");
                if (debugLogs) Debug.Log(gameObject.name + " took damage — triggered isHit");
            }

            // Health <= 0 → set isDead
            if (currentHealth <= 0f)
            {
                anim.SetBool("isDead", true);
                if (debugLogs) Debug.Log(gameObject.name + " health <= 0 — isDead set to true");
            }

            lastHealth = currentHealth; // Update last known health
        }

        // Attack logic
        if (targetEnemy != null && Vector3.Distance(transform.position, targetEnemy.position) <= attackRange)
        {
            // Face enemy
            Vector3 direction = (targetEnemy.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);

            // Attack if leader is attacking or enemy is close
            WeaponHandler leaderWH = leader.GetComponent<WeaponHandler>();
            if (leaderWH != null && (leaderWH.anim.GetBool("isAttacking") || distanceToLeader <= attackRange * 1.5f))
            {
                anim.SetTrigger("Attack");
                // TODO: trigger actual attack logic (projectile, melee, etc.)
            }
        }
    }

    private void FindClosestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, LayerMask.GetMask("Enemy"));
        if (hits.Length == 0)
        {
            targetEnemy = null;
            return;
        }

        float closestDist = float.MaxValue;
        Transform closest = null;

        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = hit.transform;
            }
        }

        targetEnemy = closest;
    }

    public void SetLeader(Transform newLeader)
    {
        leader = newLeader;
        if (debugLogs) Debug.Log(gameObject.name + " now following new leader: " + (newLeader ? newLeader.name : "none"));
    }

    public void OnBecomeLeader()
    {
        enabled = false; // Disable follower AI when leading
        if (debugLogs) Debug.Log(gameObject.name + " is now leader — PartyMember disabled");
    }

    public void OnBecomeMember()
    {
        enabled = true;
        if (debugLogs) Debug.Log(gameObject.name + " is now member — PartyMember enabled");
    }

    // Party Members spot in Formation.
    public void SetFormationPosition(Vector3 localOffset)
    {
        if (leader == null) return;

        // Convert local offset to world position relative to leader
        Vector3 targetWorldPos = leader.TransformPoint(localOffset);

        agent.SetDestination(targetWorldPos);
        if (debugLogs) Debug.Log(gameObject.name + " moving to formation position: " + targetWorldPos);
    }
}