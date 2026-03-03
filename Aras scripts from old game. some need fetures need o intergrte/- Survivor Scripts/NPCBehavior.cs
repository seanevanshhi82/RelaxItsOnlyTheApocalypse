using UnityEngine;

public class NPCBehavior : MonoBehaviour
{
    [Header("Character Assignment")]
    [Tooltip("0 = Stationed at BaseCamp (Tag: Ally, Layer: Camp Resident)\n" +
             "1 = Party Member (Tag: Ally, Layer: Ally)\n" +
             "2 = Party Leader (Tag: Player, Layer: Ally)")]
    public int CharacterAssignment = 0;

    [Header("Debug")]
    [Tooltip("Log assignment changes in console?")]
    public bool debugAssignment = true;

    void Start()
    {
        ApplyCharacterAssignment();
    }

    public void ApplyCharacterAssignment()
    {
        switch (CharacterAssignment)
        {
            case 0: // BaseCamp resident
                gameObject.tag = "Ally";
                gameObject.layer = LayerMask.NameToLayer("Camp Resident");
                break;

            case 1: // Party Member
                gameObject.tag = "Ally";
                gameObject.layer = LayerMask.NameToLayer("Ally");
                break;

            case 2: // Party Leader
                gameObject.tag = "Player";
                gameObject.layer = LayerMask.NameToLayer("Ally");
                // Disable NPC behavior when leading (player controls it)
                enabled = false;
                break;

            default:
                Debug.LogWarning($"{gameObject.name} has invalid CharacterAssignment ({CharacterAssignment}). " +
                                 "Using default: Ally / Ally layer.");
                gameObject.tag = "Ally";
                gameObject.layer = LayerMask.NameToLayer("Ally");
                break;
        }

        if (debugAssignment)
        {
            Debug.Log($"{gameObject.name} assigned as type {CharacterAssignment} " +
                      $"(Tag: {gameObject.tag}, Layer: {LayerMask.LayerToName(gameObject.layer)}, " +
                      $"NPCBehavior enabled: {enabled})");
        }
    }

    // Called by PartyManager when leadership changes
    public void UpdateAssignment(int newAssignment)
    {
        CharacterAssignment = newAssignment;
        ApplyCharacterAssignment();
    }
}