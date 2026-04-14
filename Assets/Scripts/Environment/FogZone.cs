using System.Collections;
using UnityEngine;

public class FogZone : MonoBehaviour
{
    [System.Serializable]
    private struct FogState
    {
        public bool enabled;
        public FogMode mode;
        public Color color;
        public float density;
        public float linearStart;
        public float linearEnd;
    }

    [SerializeField] private Collider triggerArea;
    [SerializeField] private float transitionDuration = 1.25f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private bool captureOutsideFogOnAwake = true;
    [SerializeField] private FogState outsideFog;
    [SerializeField] private FogState insideFog;

    private int playerOverlapCount;
    private Coroutine transitionRoutine;

    private void Reset()
    {
        AutoAssignTriggerArea();
    }

    private void OnValidate()
    {
        AutoAssignTriggerArea();
    }

    private void Awake()
    {
        AutoAssignTriggerArea();
        ConfigureTriggerRelay();

        if (captureOutsideFogOnAwake)
        {
            outsideFog = CaptureCurrentFog();
        }

        ApplyFogImmediate(outsideFog);
    }

    private void OnDisable()
    {
        playerOverlapCount = 0;

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }
    }

    private void AutoAssignTriggerArea()
    {
        if (triggerArea != null)
        {
            return;
        }

        triggerArea = GetComponentInChildren<Collider>();
    }

    private void ConfigureTriggerRelay()
    {
        if (triggerArea == null)
        {
            Debug.LogWarning($"{nameof(FogZone)} on {name} has no trigger collider assigned.", this);
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
        if (!IsPlayer(other))
        {
            return;
        }

        playerOverlapCount++;
        if (playerOverlapCount == 1)
        {
            StartTransition(insideFog);
        }
    }

    private void HandleTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerOverlapCount = Mathf.Max(0, playerOverlapCount - 1);
        if (playerOverlapCount == 0)
        {
            StartTransition(outsideFog);
        }
    }

    private void StartTransition(FogState target)
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        if (transitionDuration <= 0f)
        {
            ApplyFogImmediate(target);
            return;
        }

        transitionRoutine = StartCoroutine(TransitionFog(target));
    }

    private IEnumerator TransitionFog(FogState target)
    {
        FogState start = CaptureCurrentFog();
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / transitionDuration);
            float blend = transitionCurve != null ? transitionCurve.Evaluate(normalized) : normalized;

            ApplyFogBlended(start, target, blend);
            yield return null;
        }

        ApplyFogImmediate(target);
        transitionRoutine = null;
    }

    private static FogState CaptureCurrentFog()
    {
        FogState state = new FogState
        {
            enabled = RenderSettings.fog,
            mode = RenderSettings.fogMode,
            color = RenderSettings.fogColor,
            density = RenderSettings.fogDensity,
            linearStart = RenderSettings.fogStartDistance,
            linearEnd = RenderSettings.fogEndDistance
        };

        return state;
    }

    private static void ApplyFogImmediate(FogState state)
    {
        RenderSettings.fog = state.enabled;
        RenderSettings.fogMode = state.mode;
        RenderSettings.fogColor = state.color;
        RenderSettings.fogDensity = Mathf.Max(0f, state.density);
        RenderSettings.fogStartDistance = state.linearStart;
        RenderSettings.fogEndDistance = Mathf.Max(state.linearStart, state.linearEnd);
    }

    private static void ApplyFogBlended(FogState from, FogState to, float t)
    {
        bool showFogDuringTransition = from.enabled || to.enabled;

        RenderSettings.fog = showFogDuringTransition;
        RenderSettings.fogMode = to.mode;
        RenderSettings.fogColor = Color.LerpUnclamped(from.color, to.color, t);
        RenderSettings.fogDensity = Mathf.LerpUnclamped(from.density, to.density, t);
        RenderSettings.fogStartDistance = Mathf.LerpUnclamped(from.linearStart, to.linearStart, t);
        RenderSettings.fogEndDistance = Mathf.LerpUnclamped(from.linearEnd, to.linearEnd, t);
    }

    private sealed class TriggerRelay : MonoBehaviour
    {
        private FogZone owner;

        public void Initialize(FogZone fogZone)
        {
            owner = fogZone;
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
