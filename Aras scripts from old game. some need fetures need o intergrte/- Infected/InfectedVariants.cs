using UnityEngine;

[CreateAssetMenu(fileName = "InfectedVariants", menuName = "Infected/Variants", order = 1)]
public class InfectedVariants : ScriptableObject
{
    [Header("Zombie Prefabs")]
    [Tooltip("List of all BasicInfected prefab variants")]
    public GameObject[] infectedPrefabs; // Drag your zombie prefabs here
}