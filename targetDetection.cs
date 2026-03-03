using UnityEngine;

public class TargetDetection : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Max distance for detection")]
    public float detectionRange = 15f;

    [Tooltip("Field of view angle (degrees)")]
    [Range(10f, 180f)]
    public float detectionAngle = 90f;

    [Tooltip("Eye level height offset")]
    public float eyeHeight = 1.6f;

    [Tooltip("Knee level height offset (for crouch detection)")]
    public float kneeHeight = 0.5f;

    [Tooltip("Rotation speed toward target (deg/sec)")]
    public float rotationSpeed = 360f;

    [Header("Lock-on & Behavior Settings")]
    [Tooltip("Once locked on, only switch if new target is significantly better")]
    public bool useStickyLockOn = true;
    [Tooltip("How much closer (in meters) a new target must be to steal lock")]
    public float lockHysteresis = 3.5f;

    [Tooltip("When true and moving fast, face movement direction instead of target (simulates fleeing)")]
    public bool faceMovementWhenRunning = true;

    [Header("NEW: Relationship / Social Fallback")]
    [Tooltip("When no combat target, gently face the closest 'social' character (later tied to our loyalty/faith system)")]
    public LayerMask socialLayer;           // ← Create layer "SocialCharacters" and assign all NPCs
    [Tooltip("How strongly social facing pulls (0 = off, 1 = full strength)")]
    public float socialPullStrength = 0.4f;
    [Tooltip("Max distance for social glance")]
    public float socialRange = 8f;

    [Header("Layers - Must match object layers!")]
    public LayerMask priorityTargetLayer;
    public LayerMask targetLayer;
    public LayerMask obstacleLayer;

    [Header("Movement Input from JUCharacterController")]
    [SerializeField] private bool isFleeing = false;
    [SerializeField] private Vector3 movementDirection = Vector3.zero;

    [Header("Debug")]
    public bool drawDebugCone = true;

    [Header("Current Target (Read-Only)")]
    [SerializeField, ReadOnly] private GameObject currentTargetDisplay;

    private Transform currentTarget;
    private bool hasPriorityTarget;
    private Transform socialTarget;

    // Public method for main controller to call every frame
    public void SetMovementInfo(Vector3 moveDir, bool fleeing)
    {
        movementDirection = moveDir.normalized;
        isFleeing = fleeing;
    }

    public Transform CurrentTarget => currentTarget;

    void Update()
    {
        DetectAndFaceTarget();
        ValidateCurrentTarget();
    }

    private void DetectAndFaceTarget()
    {
        // 1. Fleeing override
        if (faceMovementWhenRunning && isFleeing && movementDirection.sqrMagnitude > 0.01f)
        {
            Quaternion lookRot = Quaternion.LookRotation(movementDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
            return;
        }

        // 2. Combat target acquisition (sticky lock-on)
        Transform bestTarget = null;
        bool bestIsPriority = false;
        float closestDist = Mathf.Infinity;

        CheckCone(priorityTargetLayer, ref closestDist, ref bestTarget, ref bestIsPriority);
        if (bestTarget == null)
            CheckCone(targetLayer, ref closestDist, ref bestTarget, ref bestIsPriority);

        if (bestTarget != null)
        {
            bool shouldSwitch = !useStickyLockOn || currentTarget == null ||
                                (bestIsPriority && !hasPriorityTarget) ||
                                (currentTarget != null && closestDist + lockHysteresis < Vector3.Distance(transform.position, currentTarget.position));

            if (shouldSwitch)
            {
                currentTarget = bestTarget;
                hasPriorityTarget = bestIsPriority;
            }
        }

        currentTargetDisplay = currentTarget ? currentTarget.gameObject : null;

        // 3. Final facing
        if (currentTarget != null)
        {
            FaceTarget(currentTarget.position, 1f);
        }
        else
        {
            // NEW: Social relationship fallback
            socialTarget = FindHighestPrioritySocialTarget();
            if (socialTarget != null)
            {
                FaceTarget(socialTarget.position, socialPullStrength);
            }
        }
    }

    private Transform FindHighestPrioritySocialTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up * eyeHeight, socialRange, socialLayer);
        Transform best = null;
        float bestScore = -1f;

        foreach (Collider col in hits)
        {
            if (col.transform == transform) continue;

            Vector3 toTarget = col.transform.position - (transform.position + Vector3.up * eyeHeight);
            if (Physics.Raycast(transform.position + Vector3.up * eyeHeight, toTarget.normalized, toTarget.magnitude, obstacleLayer))
                continue;

            // TODO: Later replace with real Relationship Score (loyalty, faith, romance, etc.)
            float score = 10f - Vector3.Distance(transform.position, col.transform.position);

            if (score > bestScore)
            {
                bestScore = score;
                best = col.transform;
            }
        }
        return best;
    }

    private void FaceTarget(Vector3 targetPos, float strength)
    {
        Vector3 dir = (targetPos - transform.position).normalized;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, rotationSpeed * Time.deltaTime * strength);
        }
    }

    private void CheckCone(LayerMask layerMask, ref float closestDist, ref Transform bestTarget, ref bool bestPriority)
    {
        CheckConeAtHeight(eyeHeight, layerMask, ref closestDist, ref bestTarget, ref bestPriority);
        if (bestTarget == null)
            CheckConeAtHeight(kneeHeight, layerMask, ref closestDist, ref bestTarget, ref bestPriority);
    }

    private void CheckConeAtHeight(float height, LayerMask layerMask, ref float closestDist, ref Transform bestTarget, ref bool bestPriority)
    {
        Vector3 origin = transform.position + Vector3.up * height;
        Collider[] hits = Physics.OverlapSphere(origin, detectionRange, layerMask | obstacleLayer);

        foreach (Collider col in hits)
        {
            if (col.transform == transform) continue;

            Vector3 toTarget = col.transform.position - origin;
            float dist = toTarget.magnitude;
            if (dist > detectionRange) continue;

            float angle = Vector3.Angle(transform.forward, toTarget.normalized);
            if (angle > detectionAngle * 0.5f) continue;

            if (Physics.Raycast(origin, toTarget.normalized, dist, obstacleLayer))
                continue;

            bool isPriority = ((1 << col.gameObject.layer) & priorityTargetLayer.value) != 0;

            if (isPriority || bestTarget == null)
            {
                if (dist < closestDist)
                {
                    closestDist = dist;
                    bestTarget = col.transform;
                    bestPriority = isPriority;
                }
            }
        }
    }

    private void ValidateCurrentTarget()
    {
        if (currentTarget == null || currentTarget.gameObject == null || !currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget = null;
            currentTargetDisplay = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawDebugCone) return;

        DrawConeGizmo(transform.position + Vector3.up * eyeHeight, Color.yellow);
        DrawConeGizmo(transform.position + Vector3.up * kneeHeight, Color.cyan);

        if (currentTarget != null)
        {
            Gizmos.color = hasPriorityTarget ? Color.magenta : Color.green;
            Gizmos.DrawLine(transform.position + Vector3.up * 1f, currentTarget.position + Vector3.up * 1f);
        }
    }

    private void DrawConeGizmo(Vector3 origin, Color color)
    {
        Gizmos.color = color;
        Vector3 fwd = transform.forward;

        Gizmos.DrawRay(origin, fwd * detectionRange);

        Vector3 left = Quaternion.AngleAxis(-detectionAngle * 0.5f, Vector3.up) * fwd;
        Vector3 right = Quaternion.AngleAxis(detectionAngle * 0.5f, Vector3.up) * fwd;

        Gizmos.DrawRay(origin, left * detectionRange);
        Gizmos.DrawRay(origin, right * detectionRange);

        int segments = 16;
        Vector3 prev = origin + left * detectionRange;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = -detectionAngle * 0.5f + detectionAngle * t;
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * fwd;
            Vector3 next = origin + dir * detectionRange;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}

// Read-only Inspector field
[System.Serializable]
public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
{
    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        UnityEditor.EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true;
    }
}
#endif