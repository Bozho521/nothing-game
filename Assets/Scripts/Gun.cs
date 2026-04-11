using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Interfaces;

public class Gun : MonoBehaviour
{
    [Header("Gun Stats")]
    public int damage = 10;
    public float range = 100f;         
    public float fireRate = 0.15f;
    public float visualBulletSpeed = 0.15f; 

    [Header("Ammo System")]
    public int currentAmmo;
    public int magazineSize = 10;      
    public int reserveAmmo = 30;       
    public float reloadTime = 1.5f;    
    private bool isReloading = false;

    [Header("Setup & Effects")]
    public Transform firePoint;
    public ParticleSystem muzzleFlash;
    public GameObject sparkEffectPrefab; 
    public GameObject visualBulletPrefab; 
    public GameObject bloodDecalPrefab;

    private float nextFireTime = 0f;

    private void Start()
    {
        currentAmmo = magazineSize; 
        UpdateAmmoUI(); 
    }

    private void Update()
    {
        if (isReloading) return;

        if (currentAmmo <= 0 || Keyboard.current.rKey.wasPressedThisFrame)
        {
            StartCoroutine(Reload());
            return;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    private IEnumerator Reload()
    {
        if (reserveAmmo <= 0) yield break;

        isReloading = true;
        
        if (UIManager.Instance != null) UIManager.Instance.statsText.text = "RELOADING...\nHP: " + UIManager.Instance.statsText.text.Split('\n')[1].Substring(4) + "\n" + UIManager.Instance.statsText.text.Split('\n')[2];

        yield return new WaitForSeconds(reloadTime);

        int bulletsNeeded = magazineSize - currentAmmo;
        int bulletsToReload = Mathf.Min(bulletsNeeded, reserveAmmo);

        currentAmmo += bulletsToReload;
        reserveAmmo -= bulletsToReload;

        isReloading = false;
        
        UpdateAmmoUI(); 
    }

    private void Shoot()
    {
        currentAmmo--; 
        UpdateAmmoUI();

        if (muzzleFlash != null) muzzleFlash.Play();

        if (Physics.Raycast(firePoint.position, firePoint.forward, out RaycastHit hit, range))
        {
            StartCoroutine(AnimateVisualBullet(firePoint.position, hit.point));

            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                bool isHeadshot = hit.collider.CompareTag("Headshot");
                int finalDamage = isHeadshot ? 1000 : damage;

                enemy.TakeDamage(finalDamage, hit.point, hit.normal, isHeadshot);

                if (bloodDecalPrefab != null)
                {
                    Vector3 passThroughStart = hit.point + (firePoint.forward * 0.5f); 
                    
                    if (Physics.Raycast(passThroughStart, firePoint.forward, out RaycastHit wallHit, 3f))
                    {
                        if (wallHit.collider.GetComponentInParent<Enemy>() == null)
                        {
                            GameObject decal = Instantiate(bloodDecalPrefab, wallHit.point + (wallHit.normal * 0.01f), Quaternion.LookRotation(wallHit.normal));
                            decal.transform.Rotate(Vector3.forward, Random.Range(0f, 360f));
                            Destroy(decal, 10f); 
                        }
                    }
                }
            }
            else 
            {
                IDestructable destructable = hit.collider.GetComponentInParent<IDestructable>();
                
                if (destructable != null)
                {
                    destructable.TakeDamage(damage);
                }

                if (sparkEffectPrefab != null)
                {
                    GameObject sparks = Instantiate(sparkEffectPrefab, hit.point + (hit.normal * 0.05f), Quaternion.LookRotation(hit.normal));
                    Destroy(sparks, 2f);
                }
            }
        }
        else
        {
            StartCoroutine(AnimateVisualBullet(firePoint.position, firePoint.position + firePoint.forward * range));
        }
    }

    private IEnumerator AnimateVisualBullet(Vector3 startPosition, Vector3 endPosition)
    {
        if (visualBulletPrefab != null)
        {
            GameObject bullet = Instantiate(visualBulletPrefab, startPosition, Quaternion.LookRotation(endPosition - startPosition));
            
            float startTime = Time.time;
            
            while (Time.time < startTime + visualBulletSpeed)
            {
                float journeyFraction = (Time.time - startTime) / visualBulletSpeed;
                bullet.transform.position = Vector3.Lerp(startPosition, endPosition, journeyFraction);
                yield return null;
            }
            
            bullet.transform.position = endPosition;
            Destroy(bullet);
        }
    }

    private void UpdateAmmoUI()
    {
        if (UIManager.Instance != null) UIManager.Instance.UpdateAmmo(currentAmmo, reserveAmmo);
    }
}