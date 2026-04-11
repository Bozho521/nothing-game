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
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

        GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        if (newEnemy.TryGetComponent<Enemy>(out Enemy chaseScript))
        {
            chaseScript.player = player;
        }
    }
}