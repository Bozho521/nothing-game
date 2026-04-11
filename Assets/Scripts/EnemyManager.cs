using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Spawning Settings")]
    public GameObject enemyPrefab;
    public Transform player;
    
    public int numberOfEnemies = 100;
    public float spawnRadius = 30f;

    void Start()
    {
        SpawnEnemies();
    }

    void SpawnEnemies()
    {
        for (int i = 0; i < numberOfEnemies; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            
            Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

            Enemy chaseScript = newEnemy.GetComponent<Enemy>();
            if (chaseScript != null)
            {
                chaseScript.player = this.player;
            }
        }
    }
}