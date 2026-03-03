using UnityEngine;

public class InfectedSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Minimum number of zombies to spawn")]
    public int minSpawn = 3;
    [Tooltip("Maximum number of zombies to spawn")]
    public int maxSpawn = 7;

    [Header("Variants")]
    [Tooltip("ScriptableObject with all zombie prefab variants")]
    public InfectedVariants variants;

    [Header("Placement")]
    [Tooltip("Radius around this GameObject to spawn zombies")]
    public float spawnRadius = 10f;

    [Header("Debug")]
    [Tooltip("Spawn immediately on Start?")]
    public bool spawnOnStart = true;
    [Tooltip("Log spawn details?")]
    public bool debugSpawn = true;

    void Start()
    {
        if (spawnOnStart)
        {
            SpawnInfected();
        }
    }

    // Public method — call from button, timer, or other scripts for respawns
    public void SpawnInfected()
    {
        if (variants == null || variants.infectedPrefabs == null || variants.infectedPrefabs.Length == 0)
        {
            Debug.LogError("InfectedSpawner: No variants assigned or empty list!");
            return;
        }

        // Random number between min and max
        int spawnedCount = Random.Range(minSpawn, maxSpawn + 1); // +1 for inclusive max

        if (debugSpawn)
        {
            Debug.Log("Spawning " + spawnedCount + " zombies at " + transform.position);
        }

        for (int i = 0; i < spawnedCount; i++)
        {
            // Pick random prefab from variants
            GameObject randomPrefab = variants.infectedPrefabs[Random.Range(0, variants.infectedPrefabs.Length)];

            // Random position around spawner (on a circle for even spacing)
            Vector3 randomDirection = Random.insideUnitSphere.normalized;
            randomDirection.y = 0; // Keep on ground
            Vector3 spawnPos = transform.position + randomDirection * Random.Range(spawnRadius * 0.5f, spawnRadius);

            // Instantiate
            GameObject zombie = Instantiate(randomPrefab, spawnPos, Quaternion.identity);
        }

        if (debugSpawn)
        {
            Debug.Log("Spawn complete!");
        }
    }
}