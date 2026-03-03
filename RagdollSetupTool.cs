using UnityEngine;

public class RagdollToggle : MonoBehaviour
{
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>(true);
        ragdollColliders = GetComponentsInChildren<Collider>(true);

        // Start with ragdoll off (kinematic)
        SetRagdollActive(false);
    }

    public void SetRagdollActive(bool active)
    {
        if (animator != null)
        {
            animator.enabled = !active;
        }

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = !active;
        }

        foreach (Collider col in ragdollColliders)
        {
            col.enabled = active;
        }
    }

    // Example: Call this from death event
    public void TriggerRagdoll()
    {
        SetRagdollActive(true);
    }
}