using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CampResidents", menuName = "Party/Camp Residents", order = 1)]
public class CampResidents : ScriptableObject
{
    [Header("Base Camp NPCs")]
    [Tooltip("All characters stationed at base camp (CharacterAssignment = 0)")]
    public List<GameObject> campResidents = new List<GameObject>();

    [Header("Debug")]
    [Tooltip("Log when camp residents are loaded?")]
    public bool debugLogs = true;

    public void LogCampResidents()
    {
        if (!debugLogs) return;

        Debug.Log($"Camp Residents loaded ({campResidents.Count} NPCs):");
        foreach (var resident in campResidents)
        {
            if (resident != null)
            {
                Debug.Log($" - {resident.name} (Tag: {resident.tag}, Layer: {LayerMask.LayerToName(resident.layer)})");
            }
        }
    }
}