using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    [Header("Spawning Settings")]
    public GameObject enemyPrefab;
    public Transform player;
    
    [Tooltip("Lowered cap for better pacing.")]
    public int maxConcurrentEnemies = 6;

    [Tooltip("Slower spawn rates.")]
    public float initialSpawnDelay = 3f;
    public float minimumSpawnDelay = 1.5f;
    public float minimumSpawnRadius = 10f;
    public float maximumSpawnRadius = 25f;

    [Header("Spawn Adjustments")]
    public float groundOffset = 1.2f;
    public int maxSpawnAttempts = 15;
    
    [Header("Visibility Checks")]
    public bool spawnOutOfSight = true; 
    public LayerMask obstacleMask = ~0; 
    public float lineOfSightPlayerHeight = 1.5f;
    public float lineOfSightEnemyHeight = 1.0f;

    public static int killCount = 0; 
    
    private PlayerMovement cachedPlayerMovement;
    private List<GameObject> activeEnemies = new List<GameObject>();

    private void Start()
    {
        killCount = 0; 
        if (UIManager.Instance != null) UIManager.Instance.UpdateKills(killCount);

        if (player != null) cachedPlayerMovement = player.GetComponent<PlayerMovement>();
        
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        float currentDelay = initialSpawnDelay;

        while (true)
        {
            activeEnemies.RemoveAll(enemy => enemy == null);

            if (cachedPlayerMovement != null && !cachedPlayerMovement.isInGraceZone)
            {
                if (activeEnemies.Count < maxConcurrentEnemies)
                {
                    SpawnSingleEnemy();
                }
            }
            
            currentDelay = Mathf.Max(minimumSpawnDelay, currentDelay - 0.05f);
            yield return new WaitForSeconds(currentDelay);
        }
    }

    private void SpawnSingleEnemy()
    {
        if (player == null) return;

        Vector3 fallbackPosition = Vector3.zero;
        bool foundFallback = false;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            float spawnDistance = Random.Range(minimumSpawnRadius, maximumSpawnRadius);
            float spawnAngle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 randomOffset = new Vector3(Mathf.Cos(spawnAngle), 0f, Mathf.Sin(spawnAngle)) * spawnDistance;

            Vector3 randomCoordinate = player.position + randomOffset;

            if (NavMesh.SamplePosition(randomCoordinate, out NavMeshHit navHit, 10f, NavMesh.AllAreas))
            {
                Vector3 validSpawnPosition = navHit.position + new Vector3(0, groundOffset, 0);

                if (!foundFallback)
                {
                    fallbackPosition = validSpawnPosition;
                    foundFallback = true;
                }

                bool canSeePlayer = HasLineOfSight(validSpawnPosition);
                
                if (spawnOutOfSight && canSeePlayer) continue; 
                if (!spawnOutOfSight && !canSeePlayer) continue; 

                InstantiateAndTrackEnemy(validSpawnPosition);
                return; 
            }
        }

        if (foundFallback)
        {
            InstantiateAndTrackEnemy(fallbackPosition);
        }
    }

    private void InstantiateAndTrackEnemy(Vector3 position)
    {
        GameObject newEnemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        activeEnemies.Add(newEnemy);

        if (newEnemy.TryGetComponent<Enemy>(out Enemy chaseScript))
        {
            chaseScript.player = player;
        }
    }

    private bool HasLineOfSight(Vector3 spawnPosition)
    {
        Vector3 origin = spawnPosition + (Vector3.up * lineOfSightEnemyHeight);
        Vector3 target = player.position + (Vector3.up * lineOfSightPlayerHeight);

        return !Physics.Linecast(origin, target, obstacleMask);
    }
}