using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NavMeshAgent))]
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

    [Header("Despawn Settings")]
    public float despawnRadius = 40f; 
    public float timeToDespawn = 10f;
    private float outOfRangeTimer = 0f;

    [Header("Visual Feedback")]
    public Transform spriteVisual; 
    public Renderer enemyRenderer;
    [SerializeField] private Animator animator;
    public Color damageColor = Color.red; 
    private Color originalColor;
    private float flashDuration = 0.1f;   
    
    [Header("Impact Effects")]
    public GameObject bloodEffectPrefab;
    public GameObject headshotTextPrefab;

    [SerializeField] private AK.Wwise.Event IdleSound;
    [SerializeField] private AK.Wwise.Event DeathSound;
    [SerializeField] private AK.Wwise.Event AttackSound;

    [Header("Drop Settings")]
    public float dropChance = 0.25f; 
    public GameObject healthPickupPrefab;
    public GameObject ammoPickupPrefab;
    public int maxPlayerReserveAmmo = 120; 

    private CharacterController controller;
    private NavMeshAgent agent;
    private float verticalVelocity = 0f;
    private Camera mainCam;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        agent = GetComponent<NavMeshAgent>();
        
        agent.updatePosition = false;
        agent.updateRotation = false; 

        agent.speed = moveSpeed;
        agent.stoppingDistance = attackRange - 0.2f; 
        agent.acceleration = 20f; 

        mainCam = Camera.main;
    }

    private void OnEnable()
    {
        EnsurePlayerReference();
    }

    private void Start()
    {
        currentHealth = maxHealth; 
        
        if (enemyRenderer == null && spriteVisual != null) 
        {
            enemyRenderer = spriteVisual.GetComponent<Renderer>();
        }
            
        if (enemyRenderer != null) originalColor = enemyRenderer.material.color;

        EnsurePlayerReference();
        StartCoroutine(IdleSoundDelay());
    }

    private void Update()
    {
        if (player == null)
        {
            EnsurePlayerReference();
            if (player == null) return;
        }

        if (spriteVisual != null && mainCam != null)
        {
            spriteVisual.rotation = Quaternion.LookRotation(spriteVisual.position - mainCam.transform.position);
            spriteVisual.eulerAngles = new Vector3(0f, spriteVisual.eulerAngles.y, 0f);
        }
        else
        {
            Vector3 lookPos = new Vector3(player.position.x, transform.position.y, player.position.z);
            transform.LookAt(lookPos);
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > despawnRadius)
        {
            outOfRangeTimer += Time.deltaTime;
            
            if (outOfRangeTimer >= timeToDespawn)
            {
                Destroy(gameObject); 
                return;
            }
        }
        else
        {
            outOfRangeTimer = 0f;
        }

        agent.nextPosition = transform.position;

        Vector3 movement = Vector3.zero;

        if (distanceToPlayer > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            movement = agent.desiredVelocity;
        }
        else
        {
            agent.isStopped = true;
            
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                AttackPlayer();
            }
        }

        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        movement.y = verticalVelocity;
        controller.Move(movement * Time.deltaTime);

        if (animator != null)
        {
            Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
            animator.SetBool("IsMoving", horizontalVelocity.magnitude > 0.1f);
        }
    }

    private void EnsurePlayerReference()
    {
        if (player != null) return;

        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            player = playerMovement.transform;
        }
    }

    private IEnumerator IdleSoundDelay()
    {
        while (true)
        {
            var random_wait = Random.Range(2.0f, 7.5f);
            yield return new WaitForSeconds(random_wait);
            if (IdleSound != null) IdleSound.Post(gameObject);
        }
    }

    private void AttackPlayer()
    {
        lastAttackTime = Time.time;

        if (animator != null) animator.SetTrigger("IsAttacking");
        if (AttackSound != null) AttackSound.Post(gameObject);
        
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
        if (DeathSound != null) DeathSound.Post(gameObject);
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
                Instantiate(Random.value > 0.5f ? healthPickupPrefab : ammoPickupPrefab, transform.position + Vector3.up * 0.5f, new Quaternion(0f, 0f, -90f, transform.rotation.w));
            }
            else if (needsHealth && healthPickupPrefab != null)
            {
                Instantiate(healthPickupPrefab, transform.position + Vector3.up, new Quaternion(0f, 0f, -90f, transform.rotation.w));
            }
            else if (needsAmmo && ammoPickupPrefab != null)
            {
                Instantiate(ammoPickupPrefab, transform.position + Vector3.up, new Quaternion(0f, 0f, -90f, transform.rotation.w));
            }
        }
    }
}