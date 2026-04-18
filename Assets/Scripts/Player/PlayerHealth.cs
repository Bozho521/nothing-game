using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Stats")]
    public int maxHealth = 100;
    public int currentHealth { get; private set; }
    public bool isDead { get; private set; }

    [Header("UI References")]
    public GameObject deathScreenUI;
    
    [Header("Damage Visual Feedback")]
    public Image[] damageFlashImages; 
    public float flashFadeSpeed = 3f;
    public Color flashColor = new Color(0.8f, 0f, 0f, 0.8f);

    private PlayerCamera playerCamera;

    private void Awake()
    {
        playerCamera = GetComponent<PlayerCamera>();
        currentHealth = maxHealth;
    }

    private void Start()
    {
        if (deathScreenUI != null) deathScreenUI.SetActive(false);
        if (UIManager.Instance != null) UIManager.Instance.UpdateHP(currentHealth);
        
        foreach (Image img in damageFlashImages)
        {
            if (img != null) img.color = Color.clear;
        }
    }

    private void Update()
    {
        foreach (Image img in damageFlashImages)
        {
            if (img != null && img.color != Color.clear)
            {
                img.color = Color.Lerp(img.color, Color.clear, flashFadeSpeed * Time.deltaTime);
            }
        }
    }

    public void SetHealth(int amount)
    {
        currentHealth = Mathf.Clamp(amount, 0, maxHealth);
        if (UIManager.Instance != null) UIManager.Instance.UpdateHP(currentHealth);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (UIManager.Instance != null) UIManager.Instance.UpdateHP(currentHealth);

        if (playerCamera != null) playerCamera.StartScreenShake(0.15f, 0.2f);
        
        foreach (Image img in damageFlashImages)
        {
            if (img != null) img.color = flashColor;
        }

        if (PanelThreatRadar.Instance != null) PanelThreatRadar.Instance.TriggerHitFlash();

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        isDead = true;
        
        if (deathScreenUI != null)
        {
            deathScreenUI.SetActive(true);
            foreach (Transform child in deathScreenUI.transform) child.gameObject.SetActive(true);
        }
        
        Time.timeScale = 0f; 

        Gun playerGun = GetComponentInChildren<Gun>();
        if (playerGun != null) playerGun.UpdateCursorAndPlayerState();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}