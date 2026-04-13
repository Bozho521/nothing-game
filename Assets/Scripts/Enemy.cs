using System.Collections;
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
    [SerializeField] private AudioClip idleClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip attackClip;
    [SerializeField] private AudioSource webAudioSource;
    [Header("Drop Settings")]
    public float dropChance = 0.25f; 
    public GameObject healthPickupPrefab;
    public GameObject ammoPickupPrefab;
    public int maxPlayerReserveAmmo = 120; 

    [Header("Visibility Despawn")]
    public bool despawnWhenOutOfView = true;
    public float outOfViewDespawnDelay = 6f;
    public float visibilityCheckInterval = 0.25f;
    public LayerMask visibilityBlockers = ~0;

    private float verticalVelocity = 0f;
    private float outOfViewTimer = 0f;
    private float nextVisibilityCheckTime = 0f;

    private void OnEnable()
    {
        outOfViewTimer = 0f;
        nextVisibilityCheckTime = 0f;
        EnsurePlayerReference();
    }

    private void Start()
    {
        currentHealth = maxHealth; 
        if (enemyRenderer == null) enemyRenderer = GetComponentInChildren<Renderer>();
        if (enemyRenderer != null) originalColor = enemyRenderer.material.color;

        EnsurePlayerReference();
    }

    private void Update()
    {
        if (player == null)
        {
            EnsurePlayerReference();
        }

        UpdateOutOfViewDespawn();

        if (this == null)
        {
            return;
        }

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

            if (movement.magnitude > 0f)
            {
                animator.SetBool("IsMoving", true);
            }
            else
            {
                animator.SetBool("IsMoving", false);
            }
        }
    }

    private void UpdateOutOfViewDespawn()
    {
        if (!despawnWhenOutOfView)
        {
            return;
        }

        if (Time.time < nextVisibilityCheckTime)
        {
            return;
        }

        nextVisibilityCheckTime = Time.time + visibilityCheckInterval;

        if (IsVisibleToCamera())
        {
            outOfViewTimer = 0f;
            return;
        }

        outOfViewTimer += visibilityCheckInterval;

        if (outOfViewTimer >= outOfViewDespawnDelay)
        {
            Destroy(gameObject);
        }
    }

    private bool IsVisibleToCamera()
    {
        Camera activeCamera = Camera.main;
        if (activeCamera == null)
        {
            return true;
        }

        if (enemyRenderer == null)
        {
            enemyRenderer = GetComponentInChildren<Renderer>();
        }

        if (enemyRenderer == null)
        {
            return true;
        }

        Plane[] cameraPlanes = GeometryUtility.CalculateFrustumPlanes(activeCamera);
        Bounds enemyBounds = enemyRenderer.bounds;

        if (!GeometryUtility.TestPlanesAABB(cameraPlanes, enemyBounds))
        {
            return false;
        }

        Vector3 origin = activeCamera.transform.position;
        Vector3 target = enemyBounds.center;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            return true;
        }

        RaycastHit[] hits = Physics.RaycastAll(origin, direction.normalized, distance, visibilityBlockers);

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            if (player != null && (hit.transform == player || hit.transform.IsChildOf(player)))
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

    private void EnsurePlayerReference()
    {
        if (player != null)
        {
            return;
        }

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
            PlayIdleSound();
        }
    }

    private void AttackPlayer()
    {
        lastAttackTime = Time.time;

        animator.SetTrigger("IsAttacking");
	PlayAttackSound();
        
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
        PlayDeathSound();
        if (UIManager.Instance != null) UIManager.Instance.UpdateKills(EnemyManager.killCount);

        HandleDrops(); 

        Destroy(gameObject);
    }

    private void PlayIdleSound()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        PlayWebClip(idleClip);
#else
        if (IdleSound != null)
        {
            IdleSound.Post(gameObject);
        }
#endif
    }

    private void PlayDeathSound()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        PlayWebClip(deathClip);
#else
        if (DeathSound != null)
        {
            DeathSound.Post(gameObject);
        }
#endif
    }

    private void PlayAttackSound()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        PlayWebClip(attackClip);
#else
        if (AttackSound != null)
        {
            AttackSound.Post(gameObject);
        }
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private void PlayWebClip(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (webAudioSource == null)
        {
            webAudioSource = GetComponent<AudioSource>();
            if (webAudioSource == null)
            {
                webAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (webAudioSource != null)
        {
            webAudioSource.PlayOneShot(clip);
        }
    }
#endif

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