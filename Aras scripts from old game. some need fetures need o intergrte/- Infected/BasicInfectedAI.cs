using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(BasicInfectedHealth))]
public class BasicInfectedAI : MonoBehaviour
{
    [Header("Detection Settings")]
    public LayerMask targetLayer;           // Priority: live targets
    public LayerMask deadLayer;             // Secondary: dead bodies (feed only)
    public LayerMask obstacleLayer;
    public float detectionRadius = 10f;

    [Header("Vision Settings (Dual LOS Raycasts)")]
    public float viewAngle = 120f;
    public float eyeHeight = 1.6f;
    public float ankleHeight = 0.15f;

    [Header("Behavior Settings")]
    public float attackRange = 1.5f;
    public float feedingDistance = 1f;

    [Header("Search Settings")]
    public float searchDuration = 4f;
    public float searchRotationSpeed = 90f;

    [Header("Horde Spacing & Push Prevention")]
    public float spacingRadius = 0.6f;
    public float spacingForce = 2f;
    public float attackStopDistance = 1.2f;
    public float attackChaseDistance = 1.5f;

    [Header("Horde Pack Formation")]
    public float packRadius = 5f;
    [Range(0f, 1f)] public float followLeaderChance = 0.4f;

    [Header("Hit Reaction")]
    public float turnSpeed = 5f;
    public float heavyHitThreshold = 30f;

    [Header("Speed Settings")]
    [Range(2.5f, 5.5f)] public float minWalkSpeed = 3f;
    [Range(3f, 6f)] public float maxWalkSpeed = 5f;
    [Range(6.5f, 9.5f)] public float minRunSpeed = 7f;
    [Range(7f, 10f)] public float maxRunSpeed = 9f;
    [Range(0.3f, 1.8f)] public float minCrawlSpeed = 0.5f;
    [Range(0.5f, 2f)] public float maxCrawlSpeed = 1.5f;

    [Header("Feeding Settings")]
    public float minFeedDuration = 10f;
    public float maxFeedDuration = 20f;

    [Header("Hand Colliders")]
    public Collider leftHandCollider;
    public Collider rightHandCollider;

    [Header("Alert & Rotation")]
    public bool alert = false;
    public float alertRotationMultiplier = 1.5f;

    [Header("Debug Info (Read-only)")]
    [Tooltip("Current target the zombie is tracking (updates live in Play mode)")]
    public Transform currentTargetDebug;

    private float walkSpeed, runSpeed, crawlSpeed;
    private NavMeshAgent agent;
    private Animator anim;
    private BasicInfectedHealth health;
    private Transform currentTarget;
    private bool isFeedingActive = false;
    private bool readyToAttack = true;
    private bool isSearching = false;
    private bool isAttacking = false;
    public Vector3 lastKnownTargetPos;
    private Coroutine searchCoroutine;
    private Coroutine attackCoroutine;
    private Vector3 lastDamageDirection;
    private float lastDamageTime;
    private float feedEndTime;
    private Transform packLeader;
    private float lastTargetLostTime;

    private AudioSource audioSource;
    private float currentAttackAnimID = 0f;

    private Vector3 EyePosition => transform.position + transform.up * eyeHeight;
    private Vector3 AnklePosition => transform.position + transform.up * ankleHeight;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        health = GetComponent<BasicInfectedHealth>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.volume = 0.8f;
        }

        if (leftHandCollider != null) leftHandCollider.isTrigger = true;
        if (rightHandCollider != null) rightHandCollider.isTrigger = true;
    }

    void Start()
    {
        walkSpeed = Random.Range(minWalkSpeed, maxWalkSpeed);
        runSpeed = Random.Range(minRunSpeed, maxRunSpeed);
        crawlSpeed = Random.Range(minCrawlSpeed, maxCrawlSpeed);
        agent.speed = walkSpeed;

        SetIdle();
    }

    void Update()
    {
        currentTargetDebug = currentTarget;

        ApplySpacing();
        UpdatePackFormation();

        bool hadLiveTarget = currentTarget != null;

        FindClosestTargetOnLayer(targetLayer);

        if (currentTarget == null || currentTarget.gameObject.layer == LayerMask.NameToLayer("Dead"))
        {
            if (currentTarget != null && currentTarget.gameObject.layer == LayerMask.NameToLayer("Dead"))
            {
                if (currentTarget == null || Vector3.Distance(transform.position, currentTarget.position) > detectionRadius)
                {
                    currentTarget = null;
                }
            }

            if (currentTarget == null)
            {
                FindClosestTargetOnLayer(deadLayer);
            }
        }

        if (currentTarget != null)
        {
            float currentTurnSpeed = turnSpeed * (alert ? alertRotationMultiplier : 1f);
            RotateToFace(currentTarget.position);
        }

        if (isAttacking)
        {
            if (currentTarget == null ||
                Vector3.Distance(transform.position, currentTarget.position) > attackChaseDistance ||
                !IsTargetInCone(currentTarget.position))
            {
                StopAttack();
                lastTargetLostTime = Time.time;
            }
        }

        if (currentTarget != null)
        {
            float dist = Vector3.Distance(transform.position, currentTarget.position);

            if (currentTarget.gameObject.layer == LayerMask.NameToLayer("Dead") && dist <= feedingDistance)
            {
                RotateToFace(currentTarget.position);
                Feed();
                if (isSearching) StopSearch();
            }
            else if (dist > attackStopDistance)
            {
                MoveTo(currentTarget.position);
                if (isSearching) StopSearch();
                if (isFeedingActive) StopFeeding();
            }
            else if (readyToAttack && !isAttacking)
            {
                StartAttackCoroutine();
            }
        }
        else if (hadLiveTarget)
        {
            lastKnownTargetPos = agent.destination;
            StartSearch();
            if (isFeedingActive) StopFeeding();

            if (Time.time - lastTargetLostTime < 4f)
            {
                MoveTo(lastKnownTargetPos);
            }
            else
            {
                SetIdle();
            }
        }
        else
        {
            SetIdle();
            if (isFeedingActive) StopFeeding();
        }

        if (Time.time - lastDamageTime < 2f)
        {
            StartCoroutine(TurnTowardDamage(lastDamageDirection));
        }

        if (isFeedingActive && Time.time > feedEndTime)
        {
            StopFeeding();
        }

        bool isActuallyMoving = agent.isOnNavMesh && agent.hasPath && !agent.isStopped;
        anim.SetBool("isMoving", isActuallyMoving);
        anim.SetBool("isIdle", !isActuallyMoving && !isAttacking && !isFeedingActive && !isSearching);
    }

    private bool IsAgentReady()
    {
        return agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled;
    }

    private void RotateToFace(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        direction.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
    }

    private bool IsTargetInCone(Vector3 targetPos)
    {
        Vector3 dirToTarget = (targetPos - EyePosition).normalized;
        float angle = Vector3.Angle(transform.forward, dirToTarget);
        return angle <= viewAngle * 0.5f;
    }

    private void ApplySpacing()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, spacingRadius, LayerMask.GetMask("Enemy"));
        foreach (var col in nearby)
        {
            if (col.gameObject == gameObject) continue;
            Vector3 dirAway = (transform.position - col.transform.position).normalized;
            Vector3 sideDir = Vector3.Cross(Vector3.up, dirAway).normalized;
            agent.Move(sideDir * spacingForce * Time.deltaTime);
        }
    }

    private void FindClosestTargetOnLayer(LayerMask layer)
    {
        currentTarget = null;
        float closestDist = float.MaxValue;

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, layer);
        foreach (Collider hit in hits)
        {
            Vector3 targetCenter = hit.bounds.center;
            float distToTarget = Vector3.Distance(transform.position, targetCenter);
            if (distToTarget > detectionRadius || distToTarget >= closestDist) continue;

            Vector3 dirToTarget = (targetCenter - EyePosition).normalized;
            float angle = Vector3.Angle(transform.forward, dirToTarget);
            if (angle > viewAngle * 0.5f) continue;

            bool eyeClear = !Physics.Raycast(EyePosition, (targetCenter - EyePosition).normalized, Vector3.Distance(EyePosition, targetCenter), obstacleLayer);
            bool ankleClear = !Physics.Raycast(AnklePosition, (targetCenter - AnklePosition).normalized, Vector3.Distance(AnklePosition, targetCenter), obstacleLayer);
            if (!eyeClear && !ankleClear) continue;

            closestDist = distToTarget;
            currentTarget = hit.transform;
        }
    }

    public void MoveTo(Vector3 position)
    {
        if (!IsAgentReady())
        {
            Debug.LogWarning("MoveTo called but agent not ready on " + gameObject.name);
            return;
        }

        agent.stoppingDistance = (currentTarget != null && currentTarget.gameObject.layer == LayerMask.NameToLayer("Dead"))
            ? feedingDistance * 0.5f : 0f;
        agent.SetDestination(position);
        agent.speed = walkSpeed;
        isFeedingActive = false;
        isAttacking = false;
    }

    private void StartSearch()
    {
        if (isSearching || searchCoroutine != null) return;
        isSearching = true;
        searchCoroutine = StartCoroutine(SearchCoroutine());
    }

    private void StopSearch()
    {
        isSearching = false;
        if (searchCoroutine != null)
        {
            StopCoroutine(searchCoroutine);
            searchCoroutine = null;
        }
        if (IsAgentReady()) agent.ResetPath();
    }

    private IEnumerator SearchCoroutine()
    {
        if (!IsAgentReady()) yield break;

        agent.speed = walkSpeed * 0.5f;
        anim.SetBool("isAttacking", false);
        anim.SetBool("isFeeding", false);
        anim.SetBool("isIdle", false);

        Vector3 dirToLast = (lastKnownTargetPos - transform.position).normalized;
        Quaternion lookRot = Quaternion.LookRotation(dirToLast);
        float timer = 0f;
        while (timer < 1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, timer / 1f);
            yield return null;
            timer += Time.deltaTime;

            FindClosestTargetOnLayer(targetLayer);
            if (currentTarget != null) yield break;
        }

        timer = 0f;
        while (timer < searchDuration - 1f)
        {
            transform.Rotate(0, searchRotationSpeed * Time.deltaTime, 0);

            FindClosestTargetOnLayer(targetLayer);
            if (currentTarget != null) yield break;

            timer += Time.deltaTime;
            yield return null;
        }

        StopSearch();
        SetIdle();
    }

    private void StartAttackCoroutine()
    {
        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
        attackCoroutine = StartCoroutine(PerformAttack());
    }

    private void StopAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        anim.SetBool("isAttacking", false);
        anim.SetFloat("RandomAttack", 0f);
        readyToAttack = true;
        if (IsAgentReady()) agent.isStopped = false;
    }

    private IEnumerator PerformAttack()
    {
        readyToAttack = false;
        if (IsAgentReady()) agent.isStopped = true;

        currentAttackAnimID = Random.Range(0.7f, 2.0f);
        anim.SetFloat("RandomAttack", currentAttackAnimID);

        anim.SetBool("isAttacking", true);
        anim.SetBool("isMoving", false);
        anim.SetBool("isFeeding", false);
        anim.SetBool("isIdle", false);

        yield return new WaitForSeconds(0.05f);

        float elapsed = 0f;
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        float animLength = stateInfo.length;

        while (elapsed < animLength)
        {
            elapsed += Time.deltaTime;

            if (currentTarget == null || Vector3.Distance(transform.position, currentTarget.position) > attackChaseDistance || !IsTargetInCone(currentTarget.position))
            {
                anim.SetBool("isAttacking", false);
                anim.SetFloat("RandomAttack", 0f);
                readyToAttack = true;
                if (IsAgentReady()) agent.isStopped = false;
                yield break;
            }

            yield return null;
        }

        anim.SetBool("isAttacking", false);
        anim.SetFloat("RandomAttack", 0f);

        if (currentTarget != null && Vector3.Distance(transform.position, currentTarget.position) <= attackRange)
        {
            readyToAttack = true;
        }
        else
        {
            readyToAttack = false;
            if (IsAgentReady()) agent.isStopped = false;
        }
    }

    private void Feed()
    {
        if (IsAgentReady())
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        if (!isFeedingActive)
        {
            anim.SetInteger("RandomAction", Random.Range(1, 5));
            isFeedingActive = true;
            feedEndTime = Time.time + Random.Range(minFeedDuration, maxFeedDuration);
        }

        if (currentTarget != null)
        {
            RotateToFace(currentTarget.position);
        }

        anim.SetBool("isFeeding", true);
        anim.SetBool("isMoving", false);
        anim.SetBool("isAttacking", false);
        anim.SetBool("isIdle", false);
        anim.SetFloat("RandomAttack", 0f);
    }

    private void StopFeeding()
    {
        if (!isFeedingActive) return;

        isFeedingActive = false;
        anim.SetBool("isFeeding", false);
        if (IsAgentReady()) agent.isStopped = false;
        anim.SetInteger("RandomAction", 0);
    }

    private void SetIdle()
    {
        if (IsAgentReady())
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        anim.SetBool("isIdle", true);
        anim.SetBool("isMoving", false);
        anim.SetBool("isFeeding", false);
        anim.SetBool("isAttacking", false);
        anim.SetFloat("RandomAttack", 0f);
        isFeedingActive = false;
        isAttacking = false;
        StopSearch();
    }

    public void TakeDamage(float damage, Vector3 damageDirection)
    {
        lastDamageDirection = damageDirection.normalized;
        lastDamageTime = Time.time;

        if (damage >= heavyHitThreshold)
        {
            anim.SetBool("isStunned", true);
            Debug.Log("Heavy hit - staggering");
        }
        else
        {
            anim.SetBool("isHit", true);
            Debug.Log("Light hit - flinching");
        }

        StartCoroutine(TurnTowardDamage(damageDirection));
    }

    private IEnumerator TurnTowardDamage(Vector3 dir)
    {
        Quaternion targetRot = Quaternion.LookRotation(dir);
        float t = 0f;
        while (t < 1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
            t += Time.deltaTime * turnSpeed;
            yield return null;
        }
    }

    public void EnableHandColliders()
    {
        if (leftHandCollider != null) leftHandCollider.enabled = true;
        if (rightHandCollider != null) rightHandCollider.enabled = true;
        Debug.Log("Zombie hand colliders ENABLED");
    }

    public void DisableHandColliders()
    {
        if (leftHandCollider != null) leftHandCollider.enabled = false;
        if (rightHandCollider != null) rightHandCollider.enabled = false;
        Debug.Log("Zombie hand colliders DISABLED");
    }

    private void UpdatePackFormation()
    {
        if (currentTarget == null) return;

        Collider[] nearby = Physics.OverlapSphere(transform.position, packRadius, LayerMask.GetMask("Enemy"));
        List<BasicInfectedAI> pack = new List<BasicInfectedAI>();
        foreach (var col in nearby)
        {
            if (col.gameObject == gameObject) continue;
            BasicInfectedAI ai = col.GetComponent<BasicInfectedAI>();
            if (ai != null && ai.currentTarget == currentTarget) pack.Add(ai);
        }

        if (pack.Count > 0)
        {
            BasicInfectedAI packLeaderAI = pack[0];
            float closestDist = Vector3.Distance(packLeaderAI.transform.position, currentTarget.position);
            foreach (var ai in pack)
            {
                float dist = Vector3.Distance(ai.transform.position, currentTarget.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    packLeaderAI = ai;
                }
            }

            if (Random.value <= followLeaderChance)
            {
                packLeader = packLeaderAI.transform;
            }
            else
            {
                packLeader = null;
            }

            if (pack.Count >= 3)
            {
                int index = pack.IndexOf(this);
                if (index >= 0)
                {
                    float angle = index * 90f;
                    Vector3 offset = Quaternion.Euler(0, angle, 0) * (currentTarget.position - transform.position).normalized * 2f;
                    Vector3 surroundPos = currentTarget.position + offset;
                    MoveTo(surroundPos);
                }
            }
        }
        else
        {
            packLeader = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Vector3 left = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward * detectionRadius;
        Vector3 right = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward * detectionRadius;
        Gizmos.DrawRay(EyePosition, left);
        Gizmos.DrawRay(EyePosition, right);

        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(EyePosition, 0.08f);
        Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(AnklePosition, 0.08f);

        if (currentTarget != null)
        {
            Vector3 tCenter = currentTarget.GetComponent<Collider>().bounds.center;
            Gizmos.color = Color.green; Gizmos.DrawLine(EyePosition, tCenter);
            Gizmos.color = Color.magenta; Gizmos.DrawLine(AnklePosition, tCenter);
        }

        if (isSearching)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lastKnownTargetPos, 0.5f);
            Vector3 searchDir = Quaternion.Euler(0, Time.time * searchRotationSpeed, 0) * transform.forward * 2f;
            Gizmos.DrawRay(transform.position, searchDir);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, packRadius);
    }
}