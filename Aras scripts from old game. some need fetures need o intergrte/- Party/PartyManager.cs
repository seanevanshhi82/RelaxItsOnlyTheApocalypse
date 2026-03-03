using UnityEngine;
using UnityEngine.InputSystem;

public class PartyManager : MonoBehaviour
{
    [Header("ProvingGroundZ - Party Data")]
    [Tooltip("Drag your Party ScriptableObject here")]
    public Party partyData;

    [Header("ProvingGroundZ - Party Formation")]
    [Tooltip("Current party formation type")]
    public FormationType currentFormation = FormationType.Wedge;

    public enum FormationType { Wedge, Line, Column, Stagger }

    [Tooltip("The child GO on the leader named after the formation (e.g., 'Wedge') with 4 'Party Member X' children on Layer 'Marker'")]
    public GameObject formationGO; // Drag the formation child here or auto-find

    [Header("Debug")]
    [Tooltip("Log party changes in console?")]
    public bool debugLogs = true;

    private ActionInputs inputActions;

    void Awake()
    {
        inputActions = new ActionInputs();
    }

    void OnEnable()
    {
        inputActions.Gamplay.Enable();
        inputActions.Gamplay.SwitchLeader.performed += OnSwitchLeader;
    }

    void OnDisable()
    {
        inputActions.Gamplay.Disable();
        inputActions.Gamplay.SwitchLeader.performed -= OnSwitchLeader;
    }

    // Called when SwitchLeader input is pressed
    private void OnSwitchLeader(InputAction.CallbackContext context)
    {
        if (partyData == null)
        {
            if (debugLogs) Debug.LogWarning("ProvingGroundZ - PartyManager: No Party data assigned!");
            return;
        }

        // Old leader → become member
        if (partyData.PartyLeader != null)
        {
            GameObject oldLeader = partyData.PartyLeader;
            PartyMember oldMemberScript = oldLeader.GetComponent<PartyMember>();
            NPCBehavior oldNPC = oldLeader.GetComponent<NPCBehavior>();

            if (oldMemberScript != null) oldMemberScript.OnBecomeMember();
            if (oldNPC != null) oldNPC.UpdateAssignment(1); // Party Member

            // Disable player input & controller components
            ActionInputs oldInput = oldLeader.GetComponent<ActionInputs>();
            if (oldInput != null) oldInput.Disable();

            CharacterController oldCC = oldLeader.GetComponent<CharacterController>();
            if (oldCC != null) oldCC.enabled = false;

            if (debugLogs) Debug.Log(oldLeader.name + " demoted to member — inputs & controller disabled");
        }

        partyData.SwitchLeader();

        // New leader → become leader
        if (partyData.PartyLeader != null)
        {
            GameObject newLeader = partyData.PartyLeader;
            PartyMember newMemberScript = newLeader.GetComponent<PartyMember>();
            NPCBehavior newNPC = newLeader.GetComponent<NPCBehavior>();

            if (newMemberScript != null) newMemberScript.OnBecomeLeader();
            if (newNPC != null) newNPC.UpdateAssignment(2); // Party Leader

            // Enable player input & controller components
            ActionInputs newInput = newLeader.GetComponent<ActionInputs>();
            if (newInput != null) newInput.Enable();

            CharacterController newCC = newLeader.GetComponent<CharacterController>();
            if (newCC != null) newCC.enabled = true;

            if (debugLogs) Debug.Log(newLeader.name + " promoted to leader — inputs & controller enabled");
        }

        if (debugLogs)
        {
            Debug.Log("ProvingGroundZ - Party leader switched via input!");
        }

        // Update formations after switch
        UpdatePartyFormation();
    }

    // Update party formation positions based on currentFormation
    private void UpdatePartyFormation()
    {
        if (partyData.PartyLeader == null || partyData.PartyMembers.Count == 0) return;

        // Auto-find formationGO on leader if not assigned
        if (formationGO == null)
        {
            formationGO = partyData.PartyLeader.transform.Find(currentFormation.ToString())?.gameObject;
            if (formationGO == null)
            {
                if (debugLogs) Debug.LogWarning("ProvingGroundZ - PartyManager: No formation GO named '" + currentFormation.ToString() + "' found on leader!");
                return;
            }
        }

        // Get the 4 member positions from the formation GO
        Transform[] memberPositions = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            memberPositions[i] = formationGO.transform.Find("Party Member " + (i + 1));
            if (memberPositions[i] == null)
            {
                if (debugLogs) Debug.LogWarning("ProvingGroundZ - PartyManager: 'Party Member " + (i + 1) + "' not found in formation GO!");
                return;
            }
        }

        // Assign positions to party members (up to 4)
        int memberIndex = 0;
        foreach (GameObject member in partyData.PartyMembers)
        {
            if (memberIndex >= memberPositions.Length) break;

            PartyMember memberScript = member.GetComponent<PartyMember>();
            if (memberScript != null)
            {
                // Send local offset position (relative to leader)
                Vector3 localPos = memberPositions[memberIndex].localPosition;
                memberScript.SetFormationPosition(localPos);
                if (debugLogs) Debug.Log("ProvingGroundZ - " + member.name + " assigned to formation position " + (memberIndex + 1));
            }

            memberIndex++;
        }

        if (debugLogs)
        {
            Debug.Log("ProvingGroundZ - Party formation updated to " + currentFormation);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // Additional Methods
    // ──────────────────────────────────────────────────────────────────────────────

    public void ChangeFormation(FormationType newFormation)
    {
        currentFormation = newFormation;
        UpdatePartyFormation();
        if (debugLogs) Debug.Log("ProvingGroundZ - Party formation changed to " + newFormation);
    }
}