using UnityEngine;
using System.Collections;

public class EnemyManager : MonoBehaviour
{
    [Header("Spawning Settings")]
    public GameObject enemyPrefab;
    public Transform player;
    
    public float initialSpawnDelay = 3f;
    public float minimumSpawnDelay = 0.5f;
    public float minimumSpawnRadius = 15f;
    public float maximumSpawnRadius = 30f;

    [Header("Spawn Adjustments")]
    public float groundOffset = 1f;
    public float maxElevationDifference = 2f;
    public int maxSpawnAttempts = 10;
    public LayerMask spawnSurfaceMask = ~0;

    [Header("Visibility Checks")]
    public bool requireLineOfSightToPlayer = true;
    public LayerMask lineOfSightBlockers = ~0;
    public float lineOfSightPlayerHeight = 1.5f;
    public float lineOfSightEnemyHeight = 1.0f;

    public static int killCount = 0; 

    private void Start()
    {
        killCount = 0; 
        
        if (UIManager.Instance != null) UIManager.Instance.UpdateKills(killCount);
        
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        float currentDelay = initialSpawnDelay;

        while (true)
        {
            SpawnSingleEnemy();
            
            currentDelay = Mathf.Max(minimumSpawnDelay, currentDelay - 0.05f);
            
            yield return new WaitForSeconds(currentDelay);
        }
    }

    private void SpawnSingleEnemy()
    {
        if (player == null || EnemyGrace.IsSpawnSuppressed) return;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            float minRadius = Mathf.Max(0f, minimumSpawnRadius);
            float maxRadius = Mathf.Max(minRadius, maximumSpawnRadius);

            float spawnDistance = Random.Range(minRadius, maxRadius);
            float spawnAngle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 offset = new Vector3(Mathf.Cos(spawnAngle), 0f, Mathf.Sin(spawnAngle)) * spawnDistance;

            Vector3 rayStart = player.position + offset + Vector3.up * 5f;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 10f, spawnSurfaceMask))
            {
                float heightDifference = Mathf.Abs(hit.point.y - player.position.y);
                float horizontalDistance = Vector3.Distance(
                    new Vector3(hit.point.x, 0f, hit.point.z),
                    new Vector3(player.position.x, 0f, player.position.z)
                );

                if (heightDifference <= maxElevationDifference &&
                    horizontalDistance >= minRadius && horizontalDistance <= maxRadius)
                {
                    Vector3 spawnPosition = hit.point + new Vector3(0, groundOffset, 0);

                    if (requireLineOfSightToPlayer && !HasLineOfSight(spawnPosition))
                    {
                        continue;
                    }

                    GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

                    if (newEnemy.TryGetComponent<Enemy>(out Enemy chaseScript))
                    {
                        chaseScript.player = player;
                    }
                    
                    return; 
                }
            }
        }
    }

    private bool HasLineOfSight(Vector3 spawnPosition)
    {
        Vector3 origin = spawnPosition + Vector3.up * lineOfSightEnemyHeight;
        Vector3 target = player.position + Vector3.up * lineOfSightPlayerHeight;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            return true;
        }

        RaycastHit[] hits = Physics.RaycastAll(origin, direction.normalized, distance, lineOfSightBlockers);

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == player || hit.transform.IsChildOf(player))
            {
                continue;
            }

            if (hit.distance < distance - 0.05f)
            {
                return false;
            }
        }

        return true;
    }
}