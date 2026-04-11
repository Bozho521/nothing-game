using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Interfaces; 
using UnityEngine.EventSystems; 
using UnityEngine.UI; 

[System.Serializable]
public class WeaponVisual
{
    public GameObject weaponModel;
    public Transform firePoint;
    public ParticleSystem muzzleFlash;
}

public class Gun : MonoBehaviour
{
    [Header("Weapon Visuals")]
    public WeaponVisual[] weaponVisuals;
    public int currentWeaponIndex = 0;

    [Header("Gun Stats")]
    public int damage = 10;
    public float range = 100f;         
    public float fireRate = 0.15f;
    public float visualBulletSpeed = 0.15f; 
    public int destructivePower = 0; 
    
    [Header("Ammo System")]
    public int currentAmmo;
    public int magazineSize = 10;      
    public int reserveAmmo = 30;       
    public float reloadTime = 1.5f;    
    private bool isReloading = false;

    [Header("Setup & Effects")]
    public GameObject sparkEffectPrefab; 
    public GameObject visualBulletPrefab; 
    public GameObject bloodDecalPrefab;

    [Header("Meta UI Mode")]
    public bool isUIModeActive = false;
    public GameObject aimingReticle; 
    private Quaternion originalLocalRotation; 

    private float nextFireTime = 0f;
    private Camera mainCam;

    private void Start()
    {
        mainCam = GetComponentInParent<Camera>();
        originalLocalRotation = transform.localRotation;
        
        if (aimingReticle != null) aimingReticle.SetActive(false);

        RefreshWeaponVisuals();
        currentAmmo = magazineSize; 
        UpdateAmmoUI(); 
    }

    private void Update()
    {
        PlayerMovement player = GetComponentInParent<PlayerMovement>();
        bool isDead = player != null && player.isDead;
        bool isPaused = UIManager.Instance != null && UIManager.Instance.isPaused;

        if (!isPaused && !isDead && Keyboard.current.mKey.wasPressedThisFrame)
        {
            isUIModeActive = !isUIModeActive;
            UpdateCursorAndPlayerState();
        }

        bool effectivelyInUIMode = isUIModeActive || isPaused || isDead;

        if (effectivelyInUIMode)
        {
            AimGunAtMouse();
            UpdateReticlePosition(); 
            
            if (aimingReticle != null && !aimingReticle.activeSelf) 
                aimingReticle.SetActive(true);
        }
        else
        {
            if (aimingReticle != null && aimingReticle.activeSelf) 
                aimingReticle.SetActive(false);
        }

        if (isReloading) return;

        if (currentAmmo <= 0 || Keyboard.current.rKey.wasPressedThisFrame)
        {
            StartCoroutine(Reload());
            return;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Time.unscaledTime >= nextFireTime)
        {
            nextFireTime = Time.unscaledTime + fireRate;
            Shoot(effectivelyInUIMode, isPaused, isDead);
        }
    }

    public void EquipWeapon(int index, int newDamage, float newRange, float newFireRate, int newDestructivePower, int newMagSize, int newReserve, float newReloadTime)
    {
        if (index < 0 || index >= weaponVisuals.Length) return;

        currentWeaponIndex = index;
        damage = newDamage;
        range = newRange;
        fireRate = newFireRate;
        destructivePower = newDestructivePower;
        magazineSize = newMagSize;
        reserveAmmo = newReserve;
        reloadTime = newReloadTime;
        
        currentAmmo = magazineSize;
        isReloading = false;

        RefreshWeaponVisuals();
        UpdateAmmoUI();
    }

    private void RefreshWeaponVisuals()
    {
        for (int i = 0; i < weaponVisuals.Length; i++)
        {
            if (weaponVisuals[i].weaponModel != null)
            {
                weaponVisuals[i].weaponModel.SetActive(i == currentWeaponIndex);
            }
        }
    }

    public void UpdateCursorAndPlayerState()
    {
        PlayerMovement player = GetComponentInParent<PlayerMovement>();
        bool isDead = player != null && player.isDead;
        bool isPaused = UIManager.Instance != null && UIManager.Instance.isPaused;
        bool effectivelyInUIMode = isUIModeActive || isPaused || isDead;

        if (effectivelyInUIMode)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;
            
            if (player != null) player.canLookUpAndDown = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            transform.localRotation = originalLocalRotation;

            if (player != null) player.canLookUpAndDown = true;
        }
    }

    private void UpdateReticlePosition()
    {
        if (aimingReticle != null && Mouse.current != null)
        {
            aimingReticle.transform.position = Mouse.current.position.ReadValue();
        }
    }

    private void AimGunAtMouse()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCam.ScreenPointToRay(mousePos);
        Vector3 targetPoint = ray.GetPoint(50f);

        Quaternion targetRotation = Quaternion.LookRotation(targetPoint - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.unscaledDeltaTime * 15f);
    }

    private IEnumerator Reload()
    {
        if (reserveAmmo <= 0) yield break;

        isReloading = true;
        
        if (UIManager.Instance != null) UIManager.Instance.statsText.text = "RELOADING...\nHP: " + UIManager.Instance.statsText.text.Split('\n')[1].Substring(4) + "\n" + UIManager.Instance.statsText.text.Split('\n')[2];

        yield return new WaitForSecondsRealtime(reloadTime);

        int bulletsNeeded = magazineSize - currentAmmo;
        int bulletsToReload = Mathf.Min(bulletsNeeded, reserveAmmo);

        currentAmmo += bulletsToReload;
        reserveAmmo -= bulletsToReload;

        isReloading = false;
        
        UpdateAmmoUI(); 
    }

    private void Shoot(bool effectivelyInUIMode, bool isPaused, bool isDead)
    {
        currentAmmo--; 
        UpdateAmmoUI();

        WeaponVisual activeVisual = weaponVisuals[currentWeaponIndex];

        if (activeVisual.muzzleFlash != null) activeVisual.muzzleFlash.Play();

        if (effectivelyInUIMode && CheckAndDestroyUI())
        {
            return; 
        }

        if (isPaused || isDead) return;

        if (Physics.Raycast(activeVisual.firePoint.position, activeVisual.firePoint.forward, out RaycastHit hit, range))
        {
            StartCoroutine(AnimateVisualBullet(activeVisual.firePoint.position, hit.point));

            if (hit.collider.TryGetComponent<IDestructable>(out var destructible) && destructivePower >= destructible.Armor)
            {
                destructible.TakeDamage(damage);
                return;
            }

            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                bool isHeadshot = hit.collider.CompareTag("Headshot");
                int finalDamage = isHeadshot ? 1000 : damage;

                enemy.TakeDamage(finalDamage, hit.point, hit.normal, isHeadshot);

                if (bloodDecalPrefab != null)
                {
                    Vector3 passThroughStart = hit.point + (activeVisual.firePoint.forward * 0.5f); 
                    if (Physics.Raycast(passThroughStart, activeVisual.firePoint.forward, out RaycastHit wallHit, 3f))
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
                IDestructable destructableFallback = hit.collider.GetComponentInParent<IDestructable>();
                if (destructableFallback != null) 
                {
                    destructableFallback.TakeDamage(damage);
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
            StartCoroutine(AnimateVisualBullet(activeVisual.firePoint.position, activeVisual.firePoint.position + activeVisual.firePoint.forward * range));
        }
    }

    private bool CheckAndDestroyUI()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            GameObject uiElement = result.gameObject;
            
            if (uiElement.GetComponent<Canvas>() != null || 
                uiElement.name.Contains("Panel") || 
                (aimingReticle != null && uiElement.transform.IsChildOf(aimingReticle.transform))) 
            {
                continue;
            }

            Button hitButton = uiElement.GetComponentInParent<Button>();
            if (hitButton != null)
            {
                if (sparkEffectPrefab != null)
                {
                    Vector3 sparkPos = mainCam.ScreenToWorldPoint(new Vector3(Mouse.current.position.ReadValue().x, Mouse.current.position.ReadValue().y, 1f));
                    GameObject sparks = Instantiate(sparkEffectPrefab, sparkPos, Quaternion.identity);
                    Destroy(sparks, 2f);
                }

                hitButton.onClick.Invoke(); 
                
                StartCoroutine(MakeUIFallAndDie(hitButton.gameObject));
                return true; 
            }

            StartCoroutine(MakeUIFallAndDie(uiElement));
            return true; 
        }

        return false;
    }

    private IEnumerator MakeUIFallAndDie(GameObject uiElement)
    {
        RectTransform rt = uiElement.GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector3 originalPos = rt.anchoredPosition;
        Quaternion originalRot = rt.rotation;
        Vector3 originalScale = rt.localScale;

        float timer = 0;
        float downwardVelocity = Random.Range(50f, 150f); 
        float gravity = 1000f; 
        float subtleTilt = Random.Range(-30f, 30f);

        while (timer < 2f && rt != null)
        {
            timer += Time.unscaledDeltaTime;
            downwardVelocity -= gravity * Time.unscaledDeltaTime; 
            
            rt.position += new Vector3(0, downwardVelocity * Time.unscaledDeltaTime, 0);
            rt.Rotate(Vector3.forward, subtleTilt * Time.unscaledDeltaTime);

            yield return null;
        }

        if (rt != null) 
        {
            uiElement.SetActive(false);
            rt.anchoredPosition = originalPos;
            rt.rotation = originalRot;
            rt.localScale = originalScale;
        }
    }

    private IEnumerator AnimateVisualBullet(Vector3 startPosition, Vector3 endPosition)
    {
        if (visualBulletPrefab != null)
        {
            GameObject bullet = Instantiate(visualBulletPrefab, startPosition, Quaternion.LookRotation(endPosition - startPosition));
            float startTime = Time.unscaledTime;
            while (Time.unscaledTime < startTime + visualBulletSpeed)
            {
                float journeyFraction = (Time.unscaledTime - startTime) / visualBulletSpeed;
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