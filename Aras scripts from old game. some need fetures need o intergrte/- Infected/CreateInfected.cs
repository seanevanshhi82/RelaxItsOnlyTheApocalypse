using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[ExecuteInEditMode] // Allows Reset() and Awake() to work in Editor
public class CreateInfected : MonoBehaviour
{
    private bool hasRunSetup = false; // Safety flag to prevent double-run

    // Called automatically when the script is first added to the GameObject (in Editor or Play)
    private void Reset()
    {
        if (hasRunSetup) return;

        SetupInfected();

        hasRunSetup = true;

        // Remove this component after setup (cleans up the object)
        DestroyImmediate(this);
        Debug.Log("CreateInfected setup complete on " + gameObject.name + " — script removed itself");
    }

    private void Awake()
    {
        // Safety net: if Reset() didn't run (e.g., in Play mode on existing object)
        if (!hasRunSetup)
        {
            SetupInfected();
            hasRunSetup = true;

            DestroyImmediate(this);
            Debug.Log("CreateInfected setup complete on " + gameObject.name + " — script removed itself (from Awake)");
        }
    }

    private void SetupInfected()
    {
        // 1. BasicInfectedAI
        if (GetComponent<BasicInfectedAI>() == null)
        {
            gameObject.AddComponent<BasicInfectedAI>();
        }

        // 2. BasicInfectedHealth
        if (GetComponent<BasicInfectedHealth>() == null)
        {
            gameObject.AddComponent<BasicInfectedHealth>();
        }

        // 3. Two CapsuleColliders
        CapsuleCollider triggerCollider = GetComponents<CapsuleCollider>().FirstOrDefault(c => c.isTrigger);
        CapsuleCollider solidCollider = GetComponents<CapsuleCollider>().FirstOrDefault(c => !c.isTrigger);

        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<CapsuleCollider>();
        }
        triggerCollider.center = new Vector3(0f, 1f, 0f);
        triggerCollider.height = 1.8f;
        triggerCollider.radius = 0.4f;
        triggerCollider.isTrigger = true;

        if (solidCollider == null)
        {
            solidCollider = gameObject.AddComponent<CapsuleCollider>();
        }
        solidCollider.center = new Vector3(0f, 1f, 0f);
        solidCollider.height = 1.8f;
        solidCollider.radius = 0.4f;
        solidCollider.isTrigger = false;

        // 4. NavMeshAgent
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }
        // Optional: default settings — tweak in Inspector later
        agent.radius = 0.4f;
        agent.height = 1.8f;
        agent.speed = 3.5f; // Walk speed default

        // 5. Rigidbody (kinematic, no gravity)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;

        // 6. Animator (should already exist, but just in case)
        if (GetComponent<Animator>() == null)
        {
            gameObject.AddComponent<Animator>();
        }

        Debug.Log("CreateInfected: All components added and configured on " + gameObject.name);
    }
}