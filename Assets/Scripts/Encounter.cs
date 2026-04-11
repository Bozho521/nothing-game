using System.Collections.Generic;
using UnityEngine;

public class Encounter : MonoBehaviour
{
    [SerializeField] private Collider triggerSphere;
    [SerializeField] private GameObject enemiesRoot;
    [SerializeField] private List<Door> doorsToOpen = new();

    private static readonly HashSet<string> TriggeredEncounterKeys = new();

    private string encounterKey;
    private bool isArmed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionCache()
    {
        TriggeredEncounterKeys.Clear();
    }

    private void Reset()
    {
        AutoAssignChildren();
    }

    private void OnValidate()
    {
        AutoAssignChildren();
    }

    private void Awake()
    {
        AutoAssignChildren();
        ApplyDoorInteractionPolicy();

        encounterKey = BuildEncounterKey();
        ConfigureTriggerRelay();

        if (TriggeredEncounterKeys.Contains(encounterKey))
        {
            isArmed = false;
            DisableTrigger();
            return;
        }

        if (enemiesRoot != null)
        {
            enemiesRoot.SetActive(false);
        }

        isArmed = true;
    }

    private void ActivateEncounter()
    {
        if (!isArmed)
        {
            return;
        }

        isArmed = false;
        TriggeredEncounterKeys.Add(encounterKey);

        if (enemiesRoot != null)
        {
            enemiesRoot.SetActive(true);
        }

        OpenAssignedDoors();

        DisableTrigger();
    }

    private void ApplyDoorInteractionPolicy()
    {
        for (int i = 0; i < doorsToOpen.Count; i++)
        {
            Door door = doorsToOpen[i];
            if (door == null)
            {
                continue;
            }

            door.SetEncounterManaged(true);
        }
    }

    private void OpenAssignedDoors()
    {
        for (int i = 0; i < doorsToOpen.Count; i++)
        {
            Door door = doorsToOpen[i];
            if (door == null)
            {
                continue;
            }

            door.OpenFromEncounter();
        }
    }

    private void DisableTrigger()
    {
        if (triggerSphere != null)
        {
            triggerSphere.enabled = false;
        }
    }

    private void AutoAssignChildren()
    {
        if (triggerSphere == null)
        {
            Transform triggerTransform = transform.Find("Trigger Sphere");
            if (triggerTransform != null)
            {
                triggerSphere = triggerTransform.GetComponent<Collider>();
            }
        }

        if (enemiesRoot == null)
        {
            Transform enemiesTransform = transform.Find("Enemies");
            if (enemiesTransform != null)
            {
                enemiesRoot = enemiesTransform.gameObject;
            }
        }
    }

    private void ConfigureTriggerRelay()
    {
        if (triggerSphere == null)
        {
            Debug.LogWarning($"{nameof(Encounter)} on {name} has no Trigger Sphere collider assigned.", this);
            return;
        }

        triggerSphere.isTrigger = true;

        TriggerRelay relay = triggerSphere.GetComponent<TriggerRelay>();
        if (relay == null)
        {
            relay = triggerSphere.gameObject.AddComponent<TriggerRelay>();
        }

        relay.Initialize(this);
    }

    private string BuildEncounterKey()
    {
        return gameObject.scene.name + ":" + GetHierarchyPath(transform);
    }

    private static string GetHierarchyPath(Transform current)
    {
        string path = current.name;

        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }

    private bool IsPlayer(Collider other)
    {
        return other.GetComponentInParent<PlayerMovement>() != null;
    }

    private void HandleTriggerEnter(Collider other)
    {
        if (!isArmed)
        {
            return;
        }

        if (!IsPlayer(other))
        {
            return;
        }

        ActivateEncounter();
    }

    private sealed class TriggerRelay : MonoBehaviour
    {
        private Encounter owner;

        public void Initialize(Encounter encounter)
        {
            owner = encounter;
        }

        private void OnTriggerEnter(Collider other)
        {
            owner?.HandleTriggerEnter(other);
        }
    }
}
