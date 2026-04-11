using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; 

public class UIManager : MonoBehaviour
{
    public static UIManager Instance; 

    [Header("UI Reference")]
    public TextMeshProUGUI statsText;
    public GameObject pauseMenuUI; 

    public bool isPaused = false; 

    private int currentHP;
    private string currentAmmo = "0 / 0";
    private int currentKills = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
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

        Gun playerGun = FindFirstObjectByType<Gun>();
        if (playerGun != null) playerGun.UpdateCursorAndPlayerState();
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; 
        
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

        Gun playerGun = FindFirstObjectByType<Gun>();
        if (playerGun != null) playerGun.UpdateCursorAndPlayerState();
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
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

    private void RefreshUI()
    {
        if (statsText != null)
        {
            statsText.text = $"AMMO: {currentAmmo}\nHP: {currentHP}\nKILLS: {currentKills}";
        }
    }
}