using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "Party", menuName = "Party/Party Data", order = 1)]
public class Party : ScriptableObject
{
    [Header("Party Leader")]
    [Tooltip("The current party leader (Tag and Layer will be set to 'Player' and 'Ally')")]
    public GameObject PartyLeader;

    [Header("Party Members")]
    [Tooltip("List of current party members (Tag and Layer set to 'Ally')")]
    public List<GameObject> PartyMembers = new List<GameObject>();

    [Header("Debug")]
    [Tooltip("Log party changes in console?")]
    public bool debugPartyChanges = true;

    /// <summary>
    /// Call this when switching leader (e.g., from input).
    /// Moves first member to leader position, old leader to bottom of members.
    /// Updates tags/layers and enables/disables input components.
    /// </summary>
    public void SwitchLeader()
    {
        if (PartyMembers == null || PartyMembers.Count == 0)
        {
            if (debugPartyChanges) Debug.LogWarning("SwitchLeader failed: No party members available.");
            return;
        }

        // Get old leader
        GameObject oldLeader = PartyLeader;

        // Get first member (new leader)
        GameObject newLeader = PartyMembers[0];

        // Only allow members with CharacterAssignment = 1 or 2
        NPCBehavior newLeaderBehavior = newLeader?.GetComponent<NPCBehavior>();
        if (newLeaderBehavior == null || (newLeaderBehavior.CharacterAssignment != 1 && newLeaderBehavior.CharacterAssignment != 2))
        {
            if (debugPartyChanges) Debug.LogWarning("SwitchLeader failed: New leader is not a valid party member (assignment must be 1 or 2).");
            return;
        }

        // Update leader
        PartyLeader = newLeader;

        // Move first member to leader, old leader to bottom of list
        PartyMembers.RemoveAt(0);
        if (oldLeader != null)
        {
            PartyMembers.Add(oldLeader);
        }

        // Update tags/layers and input components
        UpdateCharacterState(oldLeader, false);  // Old leader ? no longer player
        UpdateCharacterState(newLeader, true);   // New leader ? now player

        if (debugPartyChanges)
        {
            Debug.Log($"Party leader switched: {newLeader.name} is now leader. Old leader {oldLeader?.name} moved to members.");
        }
    }

    private void UpdateCharacterState(GameObject character, bool isLeader)
    {
        if (character == null) return;

        NPCBehavior behavior = character.GetComponent<NPCBehavior>();
        if (behavior == null) return;

        if (isLeader)
        {
            character.tag = "Player";
            character.layer = LayerMask.NameToLayer("Ally");

            // Enable input components
            PlayerInput playerInput = character.GetComponent<PlayerInput>();
            if (playerInput != null) playerInput.enabled = true;

            ActionInputs inputActions = character.GetComponent<ActionInputs>();
            if (inputActions != null) inputActions.Enable();

            if (debugPartyChanges) Debug.Log($"{character.name} is now Party Leader (Tag: Player, Layer: Ally, Inputs enabled)");
        }
        else
        {
            character.tag = "Ally";
            character.layer = LayerMask.NameToLayer("Ally");

            // Disable input components
            PlayerInput playerInput = character.GetComponent<PlayerInput>();
            if (playerInput != null) playerInput.enabled = false;

            ActionInputs inputActions = character.GetComponent<ActionInputs>();
            if (inputActions != null) inputActions.Disable();

            if (debugPartyChanges) Debug.Log($"{character.name} is now Party Member (Tag: Ally, Layer: Ally, Inputs disabled)");
        }
    }
}