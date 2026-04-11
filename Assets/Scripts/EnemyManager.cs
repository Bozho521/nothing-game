using UnityEngine;
using System.Collections;

public class EnemyManager : MonoBehaviour
{
    [Header("Spawning Settings")]
    public GameObject enemyPrefab;
    public Transform player;
    
    public float initialSpawnDelay = 3f;
    public float minimumSpawnDelay = 0.5f;
    public float spawnRadius = 30f;

    [Header("Spawn Adjustments")]
    public float groundOffset = 1f;
    public float maxElevationDifference = 2f;
    public int maxSpawnAttempts = 10;

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
        if (player == null) return;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            
            Vector3 rayStart = new Vector3(
                player.position.x + randomCircle.x, 
                player.position.y + 5f, 
                player.position.z + randomCircle.y
            );

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 10f))
            {
                float heightDifference = Mathf.Abs(hit.point.y - player.position.y);

                if (heightDifference <= maxElevationDifference)
                {
                    Vector3 spawnPosition = hit.point + new Vector3(0, groundOffset, 0);
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
}