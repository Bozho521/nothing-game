using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WeaponPickup : MonoBehaviour
{
    public int weaponVisualIndex = 0;
    
    public int damage = 15;
    public float range = 150f;
    public float fireRate = 0.1f;
    public int destructivePower = 1;
    
    public int magazineSize = 30;
    public int reserveAmmo = 90;

    public float rotationSpeed = 90f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.25f;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        float newY = startPos.y + (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerMovement>(out PlayerMovement player))
        {
            Gun playerGun = player.GetComponentInChildren<Gun>();
            
            if (playerGun != null)
            {
                playerGun.EquipWeapon(
                    weaponVisualIndex, 
                    damage, 
                    range, 
                    fireRate, 
                    destructivePower, 
                    magazineSize, 
                    reserveAmmo
                );

                Destroy(gameObject);
            }
        }
    }
}