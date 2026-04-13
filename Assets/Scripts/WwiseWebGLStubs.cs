#if UNITY_WEBGL && !UNITY_EDITOR
using UnityEngine;

namespace AK.Wwise
{
    // Minimal WebGL player stub to keep serialized field layouts consistent when Wwise runtime assemblies are excluded.
    [System.Serializable]
    public class Event
    {
        public void Post(GameObject gameObject)
        {
            // Intentionally no-op on WebGL player builds.
        }
    }
}
#endif
