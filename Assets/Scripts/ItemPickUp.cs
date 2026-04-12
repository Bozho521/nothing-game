using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    public enum PickupType { Health, Ammo }
    
    [Header("Pickup Settings")]
    public PickupType type;
    public int amount = 20;

    [Header("Animation Settings")]
    public float rotationSpeed = 90f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.25f;

    private Vector3 startPos;
    [SerializeField] private AK.Wwise.Event itemPickupSound;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        transform.RotateAround(transform.position, Vector3.up, rotationSpeed * Time.deltaTime);
        float newY = startPos.y + (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerMovement>(out PlayerMovement player))
        {
            itemPickupSound.Post(gameObject);
            if (type == PickupType.Health)
            {
                if (player.currentHealth < player.maxHealth)
                {
                    player.currentHealth = Mathf.Min(player.maxHealth, player.currentHealth + amount);
                    if (UIManager.Instance != null) UIManager.Instance.UpdateHP(player.currentHealth);
                    Destroy(gameObject);
                }
            }
            else if (type == PickupType.Ammo)
            {
                Gun gun = player.GetComponentInChildren<Gun>();
                if (gun != null)
                {
                    gun.reserveAmmo += amount;
                    if (UIManager.Instance != null) UIManager.Instance.UpdateAmmo(gun.currentAmmo, gun.reserveAmmo);
                    Destroy(gameObject);
                }
            }
        }
    }
}