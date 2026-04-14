using UnityEngine;

public class EnemyGrace : MonoBehaviour
{
    [SerializeField] private Collider triggerArea;
    [SerializeField] private float despawnCheckInterval = 0.25f;

    private static int activeGraceZoneCount;

    private bool playerInside;
    private float nextDespawnCheckTime;

    public static bool IsSpawnSuppressed => activeGraceZoneCount > 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionState()
    {
        activeGraceZoneCount = 0;
    }

    private void Reset()
    {
        AutoAssignTrigger();
    }

    private void OnValidate()
    {
        AutoAssignTrigger();
    }

    private void Awake()
    {
        AutoAssignTrigger();
        ConfigureTriggerRelay();
    }

    private void Update()
    {
        if (!playerInside)
        {
            return;
        }

        if (Time.time < nextDespawnCheckTime)
        {
            return;
        }

        nextDespawnCheckTime = Time.time + despawnCheckInterval;
        DespawnEnabledEnemies();
    }

    private void OnDisable()
    {
        if (!playerInside)
        {
            return;
        }

        playerInside = false;
        activeGraceZoneCount = Mathf.Max(0, activeGraceZoneCount - 1);
    }

    private void AutoAssignTrigger()
    {
        if (triggerArea != null)
        {
            return;
        }

        Transform triggerTransform = transform.Find("Trigger Sphere");
        if (triggerTransform != null)
        {
            triggerArea = triggerTransform.GetComponent<Collider>();
        }
    }

    private void ConfigureTriggerRelay()
    {
        if (triggerArea == null)
        {
            Debug.LogWarning($"{nameof(EnemyGrace)} on {name} has no trigger collider assigned.", this);
            return;
        }

        triggerArea.isTrigger = true;

        TriggerRelay relay = triggerArea.GetComponent<TriggerRelay>();
        if (relay == null)
        {
            relay = triggerArea.gameObject.AddComponent<TriggerRelay>();
        }

        relay.Initialize(this);
    }

    private bool IsPlayer(Collider other)
    {
        return other.GetComponentInParent<PlayerMovement>() != null;
    }

    private void HandleTriggerEnter(Collider other)
    {
        if (playerInside || !IsPlayer(other))
        {
            return;
        }

        playerInside = true;
        activeGraceZoneCount++;
        nextDespawnCheckTime = 0f;
        DespawnEnabledEnemies();
    }

    private void HandleTriggerExit(Collider other)
    {
        if (!playerInside || !IsPlayer(other))
        {
            return;
        }

        playerInside = false;
        activeGraceZoneCount = Mathf.Max(0, activeGraceZoneCount - 1);
    }

    private static void DespawnEnabledEnemies()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.isActiveAndEnabled)
            {
                continue;
            }

            Destroy(enemy.gameObject);
        }
    }

    private sealed class TriggerRelay : MonoBehaviour
    {
        private EnemyGrace owner;

        public void Initialize(EnemyGrace enemyGrace)
        {
            owner = enemyGrace;
        }

        private void OnTriggerEnter(Collider other)
        {
            owner?.HandleTriggerEnter(other);
        }

        private void OnTriggerExit(Collider other)
        {
            owner?.HandleTriggerExit(other);
        }
    }
}
