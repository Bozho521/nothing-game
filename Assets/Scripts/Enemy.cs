using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))] 
public class Enemy : MonoBehaviour
{
    [Header("Enemy Stats")]
    public int maxHealth = 30;
    private int currentHealth;

    [Header("Movement Settings")]
    public Transform player;       
    public float moveSpeed = 3f;   

    [Header("Attack Settings")]
    public int attackDamage = 10;
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    private float lastAttackTime = 0f;

    [Header("Visual Feedback")]
    public Renderer enemyRenderer;   
    public Color damageColor = Color.red; 
    private Color originalColor;
    private float flashDuration = 0.1f;   
    
    [Header("Impact Effects")]
    public GameObject bloodEffectPrefab;
    public GameObject headshotTextPrefab;

    private void Start()
    {
        currentHealth = maxHealth; 
        
        if (enemyRenderer == null) enemyRenderer = GetComponentInChildren<Renderer>();
        if (enemyRenderer != null) originalColor = enemyRenderer.material.color;
    }

    private void Update()
    {
        if (player != null)
        {
            transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer > attackRange)
            {
                Vector3 direction = (player.position - transform.position).normalized;
                transform.position += direction * moveSpeed * Time.deltaTime;
            }
            else
            {
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    AttackPlayer();
                }
            }
        }
    }

    private void AttackPlayer()
    {
        lastAttackTime = Time.time;
        
        if (player.TryGetComponent<PlayerMovement>(out PlayerMovement playerStats))
        {
            playerStats.TakeDamage(attackDamage);
        }
    }

    public void TakeDamage(int damageAmount, Vector3 hitPoint, Vector3 hitNormal, bool isHeadshot)
    {
        currentHealth -= damageAmount;

        if (bloodEffectPrefab != null)
        {
            GameObject blood = Instantiate(bloodEffectPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
            Destroy(blood, 2f);
        }

        if (isHeadshot && headshotTextPrefab != null)
        {
            Instantiate(headshotTextPrefab, hitPoint + (Vector3.up * 0.5f), Quaternion.identity);
        }

        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = damageColor;
            Invoke(nameof(ResetColor), flashDuration); 
        }

        if (currentHealth <= 0) Die();
    }

    private void ResetColor()
    {
        if (enemyRenderer != null) enemyRenderer.material.color = originalColor;
    }

    private void Die()
    {
        EnemyManager.killCount++;
        
        if (UIManager.Instance != null) UIManager.Instance.UpdateKills(EnemyManager.killCount);

        Destroy(gameObject);
    }
}