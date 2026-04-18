using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    [Header("Spawning Settings")]
    public GameObject enemyPrefab;
    public Transform player;
    
    public int maxConcurrentEnemies = 6;

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
            
            float randomDelay = Random.Range(minimumSpawnDelay, initialSpawnDelay);
            yield return new WaitForSeconds(randomDelay);
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
            
            float localAngle = Random.Range(-135f, 135f);
            Vector3 randomOffset = Quaternion.Euler(0, localAngle, 0) * player.forward * spawnDistance;

            Vector3 randomCoordinate = player.position + randomOffset;

            if (NavMesh.SamplePosition(randomCoordinate, out NavMeshHit navHit, 25f, NavMesh.AllAreas))
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