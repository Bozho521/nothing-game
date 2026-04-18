using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class WeaponVisual
{
    public GameObject weaponModel;
    public Transform firePoint;
    public ParticleSystem muzzleFlash;
}

[RequireComponent(typeof(GunCombat), typeof(GunMeta))]
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

    [Header("Visual Effects")]
    public GameObject visualBulletPrefab;

    [Header("Meta UI Mode")]
    public bool isUIModeActive = false;
    public float uiAimSwayWeight = 0.15f;
    private Quaternion originalLocalRotation;

    [Header("Animation")]
    public Animator weaponAnimator;
    public string shotFiredBool = "ShotFired";
    public string reloadTrigger = "Reload";

    [Header("Weapon Sounds")]
    [SerializeField] private List<AK.Wwise.Event> FiredSound;
    [SerializeField] private List<AK.Wwise.Event> ReloadSound;
    [SerializeField] private List<AK.Wwise.Event> EmptySound;

    private GunCombat combatModule;
    private GunMeta metaModule;
    private Camera mainCam;
    private PlayerMovement cachedPlayer;

    private float nextFireTime = 0f;
    private Quaternion currentAimRotation;
    private bool wasInUIMode = false;
    private bool reloadAnimationCompleted = false;

    private void Awake()
    {
        combatModule = GetComponent<GunCombat>();
        metaModule = GetComponent<GunMeta>();
        mainCam = GetComponentInParent<Camera>();
        cachedPlayer = GetComponentInParent<PlayerMovement>();
    }

    private void Start()
    {
        originalLocalRotation = transform.localRotation;
        currentAimRotation = transform.rotation;

        RefreshWeaponVisuals();
        currentAmmo = magazineSize; 
        UpdateAmmoUI(); 

        if (weaponAnimator != null) weaponAnimator.SetBool(shotFiredBool, false);
    }

    private void Update()
    {
        bool isDead = cachedPlayer != null && cachedPlayer.isDead;
        bool isPaused = UIManager.Instance != null && UIManager.Instance.isPaused;

        if (!isPaused && !isDead && Mouse.current.rightButton.wasPressedThisFrame)
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
            
            metaModule.UpdateReticlePosition(); 
            metaModule.SetReticleActive(true);
        }
        else
        {
            wasInUIMode = false;
            metaModule.SetReticleActive(false);
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
        bool isDead = cachedPlayer != null && cachedPlayer.isDead;
        bool isPaused = UIManager.Instance != null && UIManager.Instance.isPaused;
        bool effectivelyInUIMode = isUIModeActive || isPaused || isDead;

        if (effectivelyInUIMode) AimGunAtMouse();
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

        if (weaponAnimator != null) weaponAnimator.SetBool(shotFiredBool, false);
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
        bool isDead = cachedPlayer != null && cachedPlayer.isDead;
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

    private void AimGunAtMouse()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCam.ScreenPointToRay(mousePos);
        Vector3 targetPoint = ray.GetPoint(50f);

        Quaternion absoluteTargetRotation = Quaternion.LookRotation(targetPoint - transform.position);
        Quaternion neutralRotation = mainCam.transform.rotation;

        Quaternion subtleTargetRotation = Quaternion.Slerp(neutralRotation, absoluteTargetRotation, uiAimSwayWeight);

        currentAimRotation = Quaternion.Slerp(currentAimRotation, subtleTargetRotation, Time.unscaledDeltaTime * 15f);
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
        if (isReloading) reloadAnimationCompleted = true;
    }

    private void Shoot(bool effectivelyInUIMode, bool isPaused, bool isDead)
    {
        WeaponVisual activeVisual = weaponVisuals[currentWeaponIndex];

        if (effectivelyInUIMode)
        {
            if (activeVisual.muzzleFlash != null) activeVisual.muzzleFlash.Play();
            if (FiredSound != null && FiredSound.Count > currentWeaponIndex) FiredSound[currentWeaponIndex].Post(gameObject);
            
            if (weaponAnimator == null) weaponAnimator = GetComponentInChildren<Animator>();
            if (weaponAnimator != null) weaponAnimator.SetBool(shotFiredBool, true);

            metaModule.TryShootUI(destructivePower, isPaused);

            Ray uiRay = mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
            
            if (Physics.Raycast(uiRay, out RaycastHit uiHit, range))
            {
                StartCoroutine(AnimateVisualBullet(activeVisual.firePoint.position, uiHit.point));
                combatModule.ProcessHitscan(uiHit, uiRay.direction, damage, destructivePower); 
            }
            else
            {
                StartCoroutine(AnimateVisualBullet(activeVisual.firePoint.position, uiRay.GetPoint(range)));
            }

            return; 
        }

        if (currentAmmo <= 0)
        {
            if (EmptySound != null && EmptySound.Count > currentWeaponIndex) EmptySound[currentWeaponIndex].Post(gameObject);
            return;
        }
        
        currentAmmo--; 
        UpdateAmmoUI();

        if (activeVisual.muzzleFlash != null) activeVisual.muzzleFlash.Play();
        if (FiredSound != null && FiredSound.Count > currentWeaponIndex) FiredSound[currentWeaponIndex].Post(gameObject);
        
        if (weaponAnimator == null) weaponAnimator = GetComponentInChildren<Animator>();
        if (weaponAnimator != null) weaponAnimator.SetBool(shotFiredBool, true);

        Ray aimRay = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(aimRay, out RaycastHit hit, range))
        {
            StartCoroutine(AnimateVisualBullet(activeVisual.firePoint.position, hit.point));
            combatModule.ProcessHitscan(hit, aimRay.direction, damage, destructivePower);
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

        if (weaponAnimator != null) weaponAnimator.SetBool(shotFiredBool, false);
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