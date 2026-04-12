using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Interfaces; 
using UnityEngine.EventSystems; 
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    private bool isReloading = false;

    [Header("Setup & Effects")]
    public GameObject sparkEffectPrefab;
    public GameObject visualBulletPrefab;
    public GameObject bloodDecalPrefab;

    [Header("Meta UI Mode")]
    public bool isUIModeActive = false;
    public GameObject aimingReticle;
    private Quaternion originalLocalRotation;

    [Header("Animation")]
    public Animator weaponAnimator;
    public string shotFiredBool = "ShotFired";
    public string reloadTrigger = "Reload";

    int uiDestroyedCount = 0;

    [Header("Weapon Sounds")]
    [SerializeField] private List<AK.Wwise.Event> FiredSound;
    [SerializeField] private List<AK.Wwise.Event> ReloadSound;
    [SerializeField] private List<AK.Wwise.Event> EmptySound;

    private List<Canvas> uiElements;

    private float nextFireTime = 0f;
    private Camera mainCam;

    private Quaternion currentAimRotation;
    private bool wasInUIMode = false;
    private bool reloadAnimationCompleted = false;

    private bool winConditionMet = false;

    private void Start()
    {
        mainCam = GetComponentInParent<Camera>();
        originalLocalRotation = transform.localRotation;
        currentAimRotation = transform.rotation;
        
        if (aimingReticle != null) aimingReticle.SetActive(false);

        RefreshWeaponVisuals();
        currentAmmo = magazineSize; 
        UpdateAmmoUI(); 

        if (weaponAnimator != null)
        {
            weaponAnimator.SetBool(shotFiredBool, false);
        }
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
            Cursor.visible = false;
            
            if (!wasInUIMode)
            {
                currentAimRotation = transform.rotation;
                wasInUIMode = true;
            }
            
            UpdateReticlePosition(); 
            
            if (aimingReticle != null && !aimingReticle.activeSelf) 
                aimingReticle.SetActive(true);
        }
        else
        {
            wasInUIMode = false;
            
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

    private void LateUpdate()
    {
        bool isDead = GetComponentInParent<PlayerMovement>()?.isDead ?? false;
        bool isPaused = UIManager.Instance != null && UIManager.Instance.isPaused;
        bool effectivelyInUIMode = isUIModeActive || isPaused || isDead;

        if (effectivelyInUIMode)
        {
            AimGunAtMouse();
        }
    }

    public void EquipWeapon(int index, int newDamage, float newRange, float newFireRate, int newDestructivePower, int newMagSize, int newReserve)
    {
        if (index < 0 || index >= weaponVisuals.Length) return;

        currentWeaponIndex = index;
        damage = newDamage;
        range = newRange;
        fireRate = newFireRate;
        destructivePower = newDestructivePower;
        magazineSize = newMagSize;
        reserveAmmo = newReserve;
        
        currentAmmo = magazineSize;
        isReloading = false;

        RefreshWeaponVisuals();
        UpdateAmmoUI();

        if (weaponAnimator != null)
        {
            weaponAnimator.SetBool(shotFiredBool, false);
        }
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
        bool isDead = GetComponentInParent<PlayerMovement>()?.isDead ?? false;
        bool isPaused = UIManager.Instance != null && UIManager.Instance.isPaused;
        bool effectivelyInUIMode = isUIModeActive || isPaused || isDead;

        if (effectivelyInUIMode)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            transform.localRotation = originalLocalRotation;
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
        
        currentAimRotation = Quaternion.Slerp(currentAimRotation, targetRotation, Time.unscaledDeltaTime * 15f);
        transform.rotation = currentAimRotation;
    }

    private IEnumerator Reload()
    {
        if (reserveAmmo <= 0) yield break;

        isReloading = true;
        reloadAnimationCompleted = false;

        if (weaponAnimator == null)
        {
            weaponAnimator = weaponVisuals != null && weaponVisuals.Length > currentWeaponIndex
                ? weaponVisuals[currentWeaponIndex].weaponModel != null
                    ? weaponVisuals[currentWeaponIndex].weaponModel.GetComponent<Animator>()
                    : GetComponentInChildren<Animator>()
                : GetComponentInChildren<Animator>();
        }

        if (weaponAnimator != null && !string.IsNullOrEmpty(reloadTrigger))
        {
            weaponAnimator.SetTrigger(reloadTrigger);
        }
        else
        {
            reloadAnimationCompleted = true;
        }
        
        if (ReloadSound != null && ReloadSound.Count > currentWeaponIndex)
        {
            ReloadSound[currentWeaponIndex].Post(gameObject);
        }
        
        if (UIManager.Instance != null) UIManager.Instance.ShowReloadingText();

        yield return new WaitUntil(() => reloadAnimationCompleted);

        int bulletsNeeded = magazineSize - currentAmmo;
        int bulletsToReload = Mathf.Min(bulletsNeeded, reserveAmmo);

        currentAmmo += bulletsToReload;
        reserveAmmo -= bulletsToReload;

        isReloading = false;
        
        UpdateAmmoUI(); 
    }

    public void OnReloadAnimationComplete()
    {
        if (!isReloading)
        {
            return;
        }

        reloadAnimationCompleted = true;
    }

    private void Shoot(bool effectivelyInUIMode, bool isPaused, bool isDead)
    {
        if (currentAmmo == 0)
        {
            if (EmptySound != null && EmptySound.Count > currentWeaponIndex)
            {
                EmptySound[currentWeaponIndex].Post(gameObject);
            }
            return;
        }
        
        if (FiredSound != null && FiredSound.Count > currentWeaponIndex)
        {
            FiredSound[currentWeaponIndex].Post(gameObject);
        }
        
        currentAmmo--; 
        UpdateAmmoUI();

        WeaponVisual activeVisual = weaponVisuals[currentWeaponIndex];

        if (activeVisual.muzzleFlash != null) activeVisual.muzzleFlash.Play();

        if (effectivelyInUIMode && CheckAndDestroyUI())
        {
            return; 
        }

        if (isPaused || isDead) return;

        if (weaponAnimator == null)
        {
            weaponAnimator = weaponVisuals != null && weaponVisuals.Length > currentWeaponIndex
                ? weaponVisuals[currentWeaponIndex].weaponModel != null
                    ? weaponVisuals[currentWeaponIndex].weaponModel.GetComponent<Animator>()
                    : GetComponentInChildren<Animator>()
                : GetComponentInChildren<Animator>();
        }

        if (weaponAnimator != null)
        {
            weaponAnimator.SetBool(shotFiredBool, true);
        }

        Ray aimRay;
        if (effectivelyInUIMode)
        {
            aimRay = mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
        }
        else
        {
            aimRay = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }

        if (Physics.Raycast(aimRay, out RaycastHit hit, range))
        {
            StartCoroutine(AnimateVisualBullet(activeVisual.firePoint.position, hit.point));

            if (TryHitDamageablePolyshape(hit, damage, destructivePower))
            {
                return;
            }

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
                    Vector3 passThroughStart = hit.point + (aimRay.direction * 0.5f); 
                    if (Physics.Raycast(passThroughStart, aimRay.direction, out RaycastHit wallHit, 3f))
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
                if (destructableFallback != null && destructivePower >= destructableFallback.Armor) 
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
            StartCoroutine(AnimateVisualBullet(activeVisual.firePoint.position, aimRay.GetPoint(range)));
        }
    }

    public void ResetShotFired()
    {
        if (weaponAnimator == null)
        {
            weaponAnimator = weaponVisuals != null && weaponVisuals.Length > currentWeaponIndex
                ? weaponVisuals[currentWeaponIndex].weaponModel != null
                    ? weaponVisuals[currentWeaponIndex].weaponModel.GetComponent<Animator>()
                    : GetComponentInChildren<Animator>()
                : GetComponentInChildren<Animator>();
        }

        if (weaponAnimator != null)
        {
            weaponAnimator.SetBool(shotFiredBool, false);
        }
    }

    private bool CheckAndDestroyUI()
    {
        if(destructivePower<2) return false;
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
                
                StartCoroutine(MakeUIFallAndDie(hitButton.gameObject));
                StartCoroutine(DelayedButtonInvoke(hitButton, 1.0f));
                
                return true; 
            }

            StartCoroutine(MakeUIFallAndDie(uiElement));

            if(uiDestroyedCount == 16)
            {
                SceneManager.LoadScene("EndScene");
                winConditionMet = true;
            }
            return true; 
        }

        return false;
    }

    private IEnumerator DelayedButtonInvoke(Button btn, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        
        if (btn != null)
        {
            btn.onClick.Invoke(); 
        }
    }
    
    private IEnumerator MakeUIFallAndDie(GameObject uiElement)
    {
        RectTransform rt = uiElement.GetComponent<RectTransform>();
        if (rt == null) yield break;
        uiDestroyedCount++;

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

    private static bool TryHitDamageablePolyshape(RaycastHit hit, float damage, int gunDestructivePower)
    {
        DamageablePolyshapes polyshape = hit.collider.GetComponent<DamageablePolyshapes>();
        if (polyshape == null)
        {
            polyshape = hit.collider.GetComponentInParent<DamageablePolyshapes>();
        }

        if (polyshape == null)
        {
            return false;
        }

        if (gunDestructivePower >= polyshape.Armor)
        {
            polyshape.TakeDamageAtHit(damage, hit);
        }
        
        return true;
    }
}