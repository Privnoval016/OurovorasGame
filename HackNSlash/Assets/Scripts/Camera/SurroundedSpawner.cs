using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class SurroundedSpawner : MonoBehaviour
{
    [Title("Spawn Configuration")]
    [SerializeField]
    [ValidateInput(nameof(ValidatePrefabs), "Must have at least one prefab")]
    private GameObject[] prefabs;

    [SerializeField]
    [MinValue(1)]
    private int totalSpawnCount = 50;

    [SerializeField]
    private Vector3 centerPosition = Vector3.zero;

    [Title("Ring Configuration")]
    [SerializeField]
    [MinValue(1)]
    private int numberOfRings = 3;

    [SerializeField]
    [MinValue(1f)]
    private float firstRingRadius = 5f;

    [SerializeField]
    [MinValue(1f)]
    private float ringSpacing = 5f;

    [Title("Randomness")]
    [SerializeField]
    [Range(0f, 1f)]
    private float radialRandomness = 0.15f;

    [SerializeField]
    [Range(0f, 180f)]
    private float angularRandomness = 10f;

    [SerializeField]
    [Range(0f, 5f)]
    private float heightVariation;

    [Title("Spawn Parent")]
    [SerializeField]
    private Transform spawnParent;

    [Title("Debug")]
    [SerializeField]
    private bool showSpawnPositions;

    private bool ValidatePrefabs(GameObject[] prefs)
    {
        return prefs != null && prefs.Length > 0;
    }

    [Button(ButtonSizes.Large), GUIColor(0.2f, 0.8f, 0.2f)]
    public void SpawnSurroundingArmy()
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogError("[SurroundedSpawner] No prefabs assigned. Cannot spawn.");
            return;
        }

        List<SpawnData> allSpawnData = new();

        // Generate spawn positions for each ring
        for (int ringIndex = 0; ringIndex < numberOfRings; ringIndex++)
        {
            float ringRadius = firstRingRadius + (ringIndex * ringSpacing);
            
            // Calculate how many enemies in this ring
            int enemiesPerRing = totalSpawnCount / numberOfRings;
            int remainderEnemies = totalSpawnCount % numberOfRings;
            int enemiesInThisRing = enemiesPerRing + (ringIndex < remainderEnemies ? 1 : 0);

            // Generate positions around this ring
            for (int i = 0; i < enemiesInThisRing; i++)
            {
                SpawnData data = GenerateSpawnDataForRing(i, enemiesInThisRing, ringRadius);
                allSpawnData.Add(data);
            }
        }

        // Shuffle to randomize which prefab gets which position
        for (int i = allSpawnData.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (allSpawnData[i], allSpawnData[randomIndex]) = (allSpawnData[randomIndex], allSpawnData[i]);
        }

        // Distribute prefabs evenly
        int spawnsPerPrefab = allSpawnData.Count / prefabs.Length;
        int remainder = allSpawnData.Count % prefabs.Length;

        int spawnIndex = 0;
        for (int prefabIndex = 0; prefabIndex < prefabs.Length; prefabIndex++)
        {
            GameObject prefab = prefabs[prefabIndex];
            if (prefab == null)
            {
                Debug.LogWarning($"[SurroundedSpawner] Prefab at index {prefabIndex} is null. Skipping.");
                continue;
            }

            int spawnCount = spawnsPerPrefab + (prefabIndex < remainder ? 1 : 0);

            for (int i = 0; i < spawnCount && spawnIndex < allSpawnData.Count; i++)
            {
                SpawnInstance(prefab, allSpawnData[spawnIndex]);
                spawnIndex++;
            }
        }

        Debug.Log($"[SurroundedSpawner] Spawned {spawnIndex} enemies in {numberOfRings} rings around {centerPosition}");
    }

    private SpawnData GenerateSpawnDataForRing(int positionInRing, int totalInRing, float ringRadius)
    {
        // Calculate angle around the circle (0 to 2π)
        float baseAngle = (2f * Mathf.PI * positionInRing) / totalInRing;

        // Add angular randomness
        float angularVariationRad = (angularRandomness * Mathf.Deg2Rad) * (Random.value - 0.5f) * 2f;
        float finalAngle = baseAngle + angularVariationRad;

        // Add radial randomness
        float radialVariation = ringRadius * radialRandomness * (Random.value - 0.5f) * 2f;
        float finalRadius = Mathf.Max(0.1f, ringRadius + radialVariation);

        // Calculate position on circle
        float x = centerPosition.x + finalRadius * Mathf.Cos(finalAngle);
        float z = centerPosition.z + finalRadius * Mathf.Sin(finalAngle);

        // Add height variation
        float heightVar = heightVariation * (Random.value - 0.5f) * 2f;
        float finalHeight = centerPosition.y + heightVar;

        Vector3 spawnPosition = new Vector3(x, finalHeight, z);

        // Calculate direction to face center
        Vector3 directionToCenter = (centerPosition - spawnPosition).normalized;

        return new SpawnData
        {
            Position = spawnPosition,
            DirectionToCenter = directionToCenter
        };
    }

    private void SpawnInstance(GameObject prefab, SpawnData data)
    {
        GameObject instance = Instantiate(prefab, data.Position, Quaternion.identity, spawnParent);

        // Make instance face the center
        if (data.DirectionToCenter.sqrMagnitude > 0.01f)
        {
            instance.transform.rotation = Quaternion.LookRotation(data.DirectionToCenter);
        }

        instance.name = $"{prefab.name} [Surrounded]";
    }

    [Button(ButtonSizes.Medium), GUIColor(0.8f, 0.2f, 0.2f)]
    public void ClearSpawned()
    {
        if (spawnParent == null)
        {
            Debug.LogWarning("[SurroundedSpawner] No spawn parent set. Clearing all '[Surrounded]' objects in scene.");
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("[Surrounded]"))
                {
                    DestroyImmediate(obj);
                }
            }
        }
        else
        {
            int childCount = spawnParent.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(spawnParent.GetChild(i).gameObject);
            }
        }

        Debug.Log("[SurroundedSpawner] Cleared spawned objects.");
    }

    private void OnDrawGizmos()
    {
        if (!showSpawnPositions)
            return;

        // Draw center
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(centerPosition, 0.3f);

        // Draw ring circles
        Gizmos.color = Color.cyan;
        for (int ringIndex = 0; ringIndex < numberOfRings; ringIndex++)
        {
            float ringRadius = firstRingRadius + (ringIndex * ringSpacing);

            const int circleSegments = 64;
            Vector3 previousPoint = centerPosition + new Vector3(ringRadius, 0, 0);

            for (int i = 1; i <= circleSegments; i++)
            {
                float angle = (2f * Mathf.PI * i) / circleSegments;
                Vector3 point = centerPosition + new Vector3(ringRadius * Mathf.Cos(angle), 0, ringRadius * Mathf.Sin(angle));
                Gizmos.DrawLine(previousPoint, point);
                previousPoint = point;
            }
        }

        // Draw spawn preview points
        Gizmos.color = Color.yellow;
        int previewCount = 0;
        for (int ringIndex = 0; ringIndex < numberOfRings; ringIndex++)
        {
            float ringRadius = firstRingRadius + (ringIndex * ringSpacing);
            int enemiesPerRing = totalSpawnCount / numberOfRings;
            int remainderEnemies = totalSpawnCount % numberOfRings;
            int enemiesInThisRing = enemiesPerRing + (ringIndex < remainderEnemies ? 1 : 0);

            for (int i = 0; i < enemiesInThisRing && previewCount < 100; i++)
            {
                SpawnData data = GenerateSpawnDataForRing(i, enemiesInThisRing, ringRadius);
                Gizmos.DrawWireSphere(data.Position, 0.2f);
                previewCount++;
            }
        }
    }

    private struct SpawnData
    {
        public Vector3 Position;
        public Vector3 DirectionToCenter;
    }
}

