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

    [Header("Layers - Must match object layers!")]
    public LayerMask priorityTargetLayer;
    public LayerMask targetLayer;
    public LayerMask obstacleLayer;

    [Header("Debug")]
    public bool drawDebugCone = true;

    [Header("Current Target (Read-Only)")]
    [SerializeField, ReadOnly] private GameObject currentTargetDisplay;

    private Transform currentTarget;
    private bool hasPriorityTarget;

    void Update()
    {
        DetectAndFaceTarget();
        ValidateCurrentTarget();
    }

    private void DetectAndFaceTarget()
    {
        currentTarget = null;
        hasPriorityTarget = false;
        float closestDist = Mathf.Infinity;

        // Check priority first
        CheckCone(priorityTargetLayer, ref closestDist, ref hasPriorityTarget);

        // If no priority, check regular
        if (!hasPriorityTarget)
        {
            CheckCone(targetLayer, ref closestDist, ref hasPriorityTarget);
        }

        // Update display field
        currentTargetDisplay = currentTarget ? currentTarget.gameObject : null;

        // Rotate toward target
        if (currentTarget != null)
        {
            Vector3 dir = (currentTarget.position - transform.position).normalized;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private void CheckCone(LayerMask layerMask, ref float closestDist, ref bool foundPriority)
    {
        // Check eye and knee
        CheckConeAtHeight(eyeHeight, layerMask, ref closestDist, ref foundPriority);
        if (currentTarget == null)
            CheckConeAtHeight(kneeHeight, layerMask, ref closestDist, ref foundPriority);
    }

    private void CheckConeAtHeight(float height, LayerMask layerMask, ref float closestDist, ref bool foundPriority)
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

            // Blocked by obstacle?
            if (Physics.Raycast(origin, toTarget.normalized, out RaycastHit hit, dist, obstacleLayer))
            {
                continue;
            }

            // Valid target
            bool isPriority = ((1 << col.gameObject.layer) & priorityTargetLayer.value) != 0;

            if (isPriority || currentTarget == null)
            {
                if (dist < closestDist)
                {
                    closestDist = dist;
                    currentTarget = col.transform;
                    foundPriority = isPriority;
                }
            }
        }
    }

    private void ValidateCurrentTarget()
    {
        if (currentTarget == null) return;

        if (currentTarget.gameObject == null || !currentTarget.gameObject.activeInHierarchy)
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

        // Arc
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