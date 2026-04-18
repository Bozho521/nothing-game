using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; 
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance; 

    [Header("UI References")]
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI killsText;
    
    [Header("Menus")]
    public GameObject pauseMenuUI; 

    public bool isPaused = false; 

    private int currentHP;
    private string currentAmmo = "0 / 0";
    private int currentKills = 0;

    private PlayerMovement cachedPlayer;
    private Gun cachedGun;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        cachedPlayer = FindFirstObjectByType<PlayerMovement>();
        cachedGun = FindFirstObjectByType<Gun>();
    }

    private void Update()
    {
        bool isDead = cachedPlayer != null && cachedPlayer.isDead;

        if ((isPaused || isDead) && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartGame();
            return; 
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isDead)
            {
                return;
            }

            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; 
        
        if (pauseMenuUI != null) 
        {
            pauseMenuUI.SetActive(true);
            
            foreach (Transform child in pauseMenuUI.transform)
            {
                child.gameObject.SetActive(true);
            }
        }

        if (cachedGun != null) cachedGun.UpdateCursorAndPlayerState();
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; 
        
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

        if (cachedGun != null) cachedGun.UpdateCursorAndPlayerState();
    }

    public void QuitGame()
    {
        Application.Quit(); 
    }

    public void UpdateHP(int hp)
    {
        currentHP = Mathf.Max(0, hp);
        RefreshUI();
    }

    public void UpdateAmmo(int current, int reserve)
    {
        currentAmmo = current + " / " + reserve;
        RefreshUI();
    }

    public void UpdateKills(int kills)
    {
        currentKills = kills;
        RefreshUI();
    }

    public void ShowReloadingText()
    {
        if (ammoText != null) ammoText.text = "RELOADING...";
    }

    private void RefreshUI()
    {
        if (ammoText != null) ammoText.text = $"AMMO: {currentAmmo}";
        if (hpText != null) hpText.text = $"HP: {currentHP}";
        if (killsText != null) killsText.text = $"KILLS: {currentKills}";
    }
}