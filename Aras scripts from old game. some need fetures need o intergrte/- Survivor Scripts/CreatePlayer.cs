using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode] // Allows setup in Editor when script is added
public class CreatePlayer : MonoBehaviour
{
    private bool hasSetupRun = false;

    private void Reset()
    {
        if (hasSetupRun) return;
        SetupCharacter();
    }

    private void Awake()
    {
        if (hasSetupRun) return;
        SetupCharacter();
    }

    private void OnEnable()
    {
        if (hasSetupRun)
        {
#if UNITY_EDITOR
            // Delay destruction in Editor to avoid serialization spam
            EditorApplication.delayCall += () => DestroyImmediate(this);
#else
            DestroyImmediate(this);
#endif
            Debug.Log("CreatePlayer setup complete on " + gameObject.name + " — script removed itself");
        }
    }

    private void SetupCharacter()
    {
        if (hasSetupRun) return;
        hasSetupRun = true;

        // 1. PlayerHealth
        if (GetComponent<PlayerHealth>() == null)
        {
            gameObject.AddComponent<PlayerHealth>();
        }

        // 2. TopDownPlayerController
        if (GetComponent<TopDownPlayerController>() == null)
        {
            gameObject.AddComponent<TopDownPlayerController>();
        }

        // 3. WeaponHandler
        if (GetComponent<WeaponHandler>() == null)
        {
            gameObject.AddComponent<WeaponHandler>();
        }

        // 4. PartyMember
        if (GetComponent<PartyMember>() == null)
        {
            gameObject.AddComponent<PartyMember>();
        }

        // 5. NPCBehavior
        NPCBehavior npc = GetComponent<NPCBehavior>();
        if (npc == null)
        {
            npc = gameObject.AddComponent<NPCBehavior>();
        }
        npc.CharacterAssignment = 1; // Default to Party Member

        // 6. Two CapsuleColliders
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

        // 7. CharacterController
        CharacterController cc = GetComponent<CharacterController>();
        if (cc == null)
        {
            cc = gameObject.AddComponent<CharacterController>();
        }
        cc.center = new Vector3(0f, 1f, 0f);
        cc.height = 1.8f;
        cc.radius = 0.4f;

        // 8. PlayerInput (set to ActionInputs)
        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            playerInput = gameObject.AddComponent<PlayerInput>();
        }
        playerInput.actions = Resources.Load<InputActionAsset>("ActionInputs"); // Adjust path if needed
        playerInput.defaultActionMap = "Gamplay";
        playerInput.defaultControlScheme = "GamepadOrKeyboard";

        // 9. TargetDetection
        TargetDetection td = GetComponent<TargetDetection>();
        if (td == null)
        {
            td = gameObject.AddComponent<TargetDetection>();
        }
        td.priorityTargetLayer = LayerMask.GetMask("Infected");
        td.targetLayer = LayerMask.GetMask("Enemy", "Survivor");
        td.obstacleLayer = LayerMask.GetMask("Door", "Windows", "Cover", "Obstacle");

        // 10. NavMeshAgent
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }
        agent.radius = 0.4f;
        agent.height = 1.8f;
        agent.speed = 3.5f; // Walk speed default

        // 11. AudioSource
        if (GetComponent<AudioSource>() == null)
        {
            gameObject.AddComponent<AudioSource>();
        }

        // 12. Animator (just in case)
        if (GetComponent<Animator>() == null)
        {
            gameObject.AddComponent<Animator>();
        }

        Debug.Log("CreatePlayer: All components added and configured on " + gameObject.name);
    }
}