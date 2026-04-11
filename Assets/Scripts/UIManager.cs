using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance; 

    [Header("UI Reference")]
    public TextMeshProUGUI statsText;

    private int currentHP;
    private string currentAmmo = "0 / 0";
    private int currentKills = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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