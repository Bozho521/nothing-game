using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LevelMiddleGround : MonoBehaviour
{
    [Header("Teleport Settings")]
    public Transform destinationPoint;

    public PlayerMovement _pm;

    public bool matchRotation = true;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerMovement>(out PlayerMovement player))
        {
            if (destinationPoint == null) return;

            CharacterController cc = player.GetComponent<CharacterController>();
            
            if (cc != null)
            {
                cc.enabled = false;
                
                player.transform.position = destinationPoint.position;
                if (matchRotation)
                {
                    player.transform.rotation = destinationPoint.rotation;
                }
                
                cc.enabled = true;

                if (_pm != null)
                {
                    _pm.UnlockMovementConstraints();
                }
            }
            else
            {
                player.transform.position = destinationPoint.position;
                if (matchRotation)
                {
                    player.transform.rotation = destinationPoint.rotation;
                }
            }
            
            return;
        }

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            Destroy(enemy.gameObject);
        }
    }
}