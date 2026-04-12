using UnityEngine;

public class PlayerAnimatorEvents : MonoBehaviour
{
    [SerializeField] private Gun gun;

    private void Awake()
    {
        if (gun == null)
        {
            gun = GetComponentInParent<Gun>();
        }
    }

    public void ResetShotFired()
    {
        if (gun == null)
        {
            gun = GetComponentInParent<Gun>();
        }

        if (gun != null)
        {
            gun.ResetShotFired();
        }
    }
}
