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
    public float gravity = 19.62f;

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

    [Header("Drop Settings")]
    public float dropChance = 0.25f; 
    public GameObject healthPickupPrefab;
    public GameObject ammoPickupPrefab;
    public int maxPlayerReserveAmmo = 120; 

    private float verticalVelocity = 0f;

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
            Vector3 movement = Vector3.zero;

            if (distanceToPlayer > attackRange)
            {
                Vector3 flatTargetPos = new Vector3(player.position.x, transform.position.y, player.position.z);
                Vector3 direction = (flatTargetPos - transform.position).normalized;
                movement = direction * moveSpeed;
            }
            else
            {
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    AttackPlayer();
                }
            }

            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.2f))
            {
                verticalVelocity = 0f;
                
                transform.position = new Vector3(transform.position.x, hit.point.y + 1f, transform.position.z);
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
            }

            movement.y = verticalVelocity;
            
            transform.position += movement * Time.deltaTime;
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

        HandleDrops(); 

        Destroy(gameObject);
    }

    private void HandleDrops()
    {
        if (Random.value > dropChance) return; 

        if (player != null)
        {
            PlayerMovement pm = player.GetComponent<PlayerMovement>();
            Gun gun = player.GetComponentInChildren<Gun>();

            bool needsHealth = pm != null && pm.currentHealth < pm.maxHealth;
            bool needsAmmo = gun != null && gun.reserveAmmo < maxPlayerReserveAmmo;

            if (needsHealth && needsAmmo)
            {
                Instantiate(Random.value > 0.5f ? healthPickupPrefab : ammoPickupPrefab, transform.position + Vector3.up, Quaternion.identity);
            }
            else if (needsHealth && healthPickupPrefab != null)
            {
                Instantiate(healthPickupPrefab, transform.position + Vector3.up, Quaternion.identity);
            }
            else if (needsAmmo && ammoPickupPrefab != null)
            {
                Instantiate(ammoPickupPrefab, transform.position + Vector3.up, Quaternion.identity);
            }
        }
    }
}