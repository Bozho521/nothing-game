using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Enemy : MonoBehaviour
{
    [Header("References")]
    public Transform player;       
    public Transform spriteVisual; 

    [Header("Stats")]
    public float speed = 3f;
    public float stopDistance = 1.5f; 

    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (player == null || spriteVisual == null) return;

        Vector3 lookDirection = player.position - spriteVisual.position;
        lookDirection.y = 0f; 
        
        if (lookDirection != Vector3.zero)
        {
            spriteVisual.rotation = Quaternion.LookRotation(lookDirection);
        }

        Vector3 moveDirection = player.position - transform.position;
        moveDirection.y = 0f; 
        
        if (moveDirection.magnitude > stopDistance)
        {
            controller.Move(moveDirection.normalized * speed * Time.deltaTime);
        }

        controller.Move(Vector3.down * 9.8f * Time.deltaTime);
    }
}