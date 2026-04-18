using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PanelThreatRadar : MonoBehaviour
{
    public static PanelThreatRadar Instance; 

    [Header("UI Image References (Scene Objects)")]
    public Image topPanel;    
    public Image bottomPanel; 
    public Image leftPanel;   
    public Image rightPanel;  

    [Header("Radar Settings")]
    public Camera playerCamera;
    public float detectionRadius = 30f;
    public float flashSpeed = 5f;
    
    public Color warningColor = new Color(1f, 0f, 0f, 0.4f); 
    private Color clearColor = new Color(1f, 0f, 0f, 0f);

    private bool threatFront = false;
    private bool threatBack = false;
    private bool threatLeft = false;
    private bool threatRight = false;
    
    private float hitFlashTimer = 0f;
    private Collider[] hitColliders = new Collider[30];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;

        InitializeImage(topPanel);
        InitializeImage(bottomPanel);
        InitializeImage(leftPanel);
        InitializeImage(rightPanel);

        StartCoroutine(RadarScanRoutine());
    }

    private void InitializeImage(Image img)
    {
        if (img != null)
        {
            img.gameObject.SetActive(true);
            img.color = clearColor;
        }
    }

    public void TriggerHitFlash()
    {
        hitFlashTimer = 0.4f;
    }

    private void Update()
    {
        if (hitFlashTimer > 0) hitFlashTimer -= Time.deltaTime;
        bool isHit = hitFlashTimer > 0;

        UpdatePanelColor(topPanel, isHit || threatFront);
        UpdatePanelColor(bottomPanel, isHit || threatBack);
        UpdatePanelColor(leftPanel, isHit || threatLeft);
        UpdatePanelColor(rightPanel, isHit || threatRight);
    }

    private void UpdatePanelColor(Image img, bool isThreat)
    {
        if (img == null) return;
        Color targetColor = isThreat ? warningColor : clearColor;
        img.color = Color.Lerp(img.color, targetColor, Time.deltaTime * flashSpeed);
    }

    private IEnumerator RadarScanRoutine()
    {
        WaitForSeconds waitTime = new WaitForSeconds(0.1f);

        while (true)
        {
            if (playerCamera == null) yield break;

            threatFront = false;
            threatBack = false;
            threatLeft = false;
            threatRight = false;

            int hitCount = Physics.OverlapSphereNonAlloc(playerCamera.transform.position, detectionRadius, hitColliders);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = hitColliders[i];
                Enemy enemy = hit.GetComponentInParent<Enemy>(); 
                
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue; 

                Vector3 dirToEnemy = enemy.transform.position - playerCamera.transform.position;
                Vector3 flatDirToEnemy = new Vector3(dirToEnemy.x, 0, dirToEnemy.z).normalized;
                Vector3 flatCamForward = new Vector3(playerCamera.transform.forward.x, 0, playerCamera.transform.forward.z).normalized;

                float angle = Vector3.SignedAngle(flatCamForward, flatDirToEnemy, Vector3.up);

                if (angle >= -45f && angle <= 45f) threatFront = true;
                else if (angle > 45f && angle < 135f) threatRight = true;
                else if (angle < -45f && angle > -135f) threatLeft = true;
                else threatBack = true;
            }
            yield return waitTime;
        }
    }
}