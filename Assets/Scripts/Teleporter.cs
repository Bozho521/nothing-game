using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Teleporter : MonoBehaviour
{
    [Header("Teleport Settings")]
    public Transform destinationPoint;
    
    public bool matchRotation = true;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (destinationPoint == null)
        {
            return;
        }

        if (other.TryGetComponent<PlayerMovement>(out PlayerMovement player))
        {
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
            }
            else
            {
                player.transform.position = destinationPoint.position;
                if (matchRotation)
                {
                    player.transform.rotation = destinationPoint.rotation;
                }
            }
        }
    }
}