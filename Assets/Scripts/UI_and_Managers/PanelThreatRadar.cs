using UnityEngine;
using UnityEngine.UI;

public class PanelThreatRadar : MonoBehaviour
{
    [Header("UI Panels")]
    public Image leftPanelWarning;
    public Image rightPanelWarning;

    [Header("Radar Settings")]
    public Camera playerCamera;
    public float detectionRadius = 30f;
    public float flashSpeed = 5f;
    
    public Color warningColor = new Color(1f, 0f, 0f, 0.4f); 
    private Color clearColor = new Color(1f, 0f, 0f, 0f);

    private void Start()
    {
        if (playerCamera == null) 
        {
            playerCamera = Camera.main;
        }

        if (leftPanelWarning) leftPanelWarning.color = clearColor;
        if (rightPanelWarning) rightPanelWarning.color = clearColor;
    }

    private void Update()
    {
        if (playerCamera == null) return;

        bool threatLeft = false;
        bool threatRight = false;

        Collider[] hits = Physics.OverlapSphere(playerCamera.transform.position, detectionRadius);

        foreach (Collider hit in hits)
        {
            Enemy enemy = hit.GetComponentInParent<Enemy>(); 
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue; 

            Vector3 dirToEnemy = enemy.transform.position - playerCamera.transform.position;
            
            Vector3 viewportPos = playerCamera.WorldToViewportPoint(enemy.transform.position);
            bool isOnScreen = viewportPos.z > 0 && viewportPos.x > 0 && viewportPos.x < 1 && viewportPos.y > 0 && viewportPos.y < 1;

            if (!isOnScreen)
            {
                Vector3 flatDirToEnemy = new Vector3(dirToEnemy.x, 0, dirToEnemy.z).normalized;
                Vector3 flatCamForward = new Vector3(playerCamera.transform.forward.x, 0, playerCamera.transform.forward.z).normalized;

                float angle = Vector3.SignedAngle(flatCamForward, flatDirToEnemy, Vector3.up);

                if (angle < -5f) threatLeft = true;
                if (angle > 5f) threatRight = true;
            }
        }

        if (leftPanelWarning)
            leftPanelWarning.color = Color.Lerp(leftPanelWarning.color, threatLeft ? warningColor : clearColor, Time.deltaTime * flashSpeed);
        
        if (rightPanelWarning)
            rightPanelWarning.color = Color.Lerp(rightPanelWarning.color, threatRight ? warningColor : clearColor, Time.deltaTime * flashSpeed);
    }
}