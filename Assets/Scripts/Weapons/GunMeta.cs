using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GunMeta : MonoBehaviour
{
    [Header("Meta Settings")]
    public GameObject aimingReticle;
    public GameObject uiSparkEffectPrefab;

    [Header("Glory Finisher")]
    [SerializeField] private int uiTargetsToDestroyForWin = 50; 
    public float slowMoTimeScale = 0.1f;
    public float slowMoDuration = 6.0f;
    
    public GameObject totalDominationPanel; 

    private int uiDestroyedCount = 0;
    private Camera mainCam;
    private bool winConditionMet = false;
    
    private readonly HashSet<GameObject> destroyedUIElements = new HashSet<GameObject>();

    private void Awake()
    {
        mainCam = GetComponentInParent<Camera>();
        if (aimingReticle != null) aimingReticle.SetActive(false);
    }

    private void Start()
    {
        if (totalDominationPanel != null)
        {
            totalDominationPanel.SetActive(false);
        }
    }

    public void SetReticleActive(bool isActive)
    {
        if (aimingReticle != null && aimingReticle.activeSelf != isActive)
        {
            aimingReticle.SetActive(isActive);
        }
    }

    public void UpdateReticlePosition()
    {
        if (aimingReticle != null && Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            
            RectTransform rt = aimingReticle.GetComponent<RectTransform>();
            float widthOffset = rt != null ? (rt.rect.width * rt.lossyScale.x) / 2f : 0f;
            float heightOffset = rt != null ? (rt.rect.height * rt.lossyScale.y) / 2f : 0f;

            mousePos.x = Mathf.Clamp(mousePos.x, widthOffset, Screen.width - widthOffset);
            mousePos.y = Mathf.Clamp(mousePos.y, heightOffset, Screen.height - heightOffset);

            aimingReticle.transform.position = mousePos;
        }
    }

    public bool TryShootUI(int destructivePower, bool isPaused)
    {
        if (EventSystem.current == null || Mouse.current == null) return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count == 0) return false;

        foreach (RaycastResult result in results)
        {
            Button hitButton = result.gameObject.GetComponentInParent<Button>();
            
            if (hitButton != null && hitButton.interactable)
            {
                if (!TryRegisterDestroyedUIElementAndChildren(hitButton.gameObject)) return false;

                SpawnSpark();
                StartCoroutine(MakeUIFallAndDie(hitButton.gameObject));
                
                hitButton.onClick.Invoke();
                
                TryCompleteWinCondition();
                return true; 
            }
        }

        foreach (RaycastResult result in results)
        {
            GameObject uiElement = result.gameObject;
            
            if (uiElement.GetComponent<Canvas>() != null || 
                uiElement.name.ToLower().Contains("panel") || 
                (aimingReticle != null && uiElement.transform.IsChildOf(aimingReticle.transform))) 
            {
                continue;
            }

            if (!TryRegisterDestroyedUIElementAndChildren(uiElement)) return false;

            SpawnSpark();
            StartCoroutine(MakeUIFallAndDie(uiElement));
            TryCompleteWinCondition();
            
            return true; 
        }

        return false;
    }

    private bool TryRegisterDestroyedUIElementAndChildren(GameObject fallingRoot)
    {
        if (fallingRoot == null || !fallingRoot.activeInHierarchy) return false;
        
        bool destroyedAnything = false;
        Graphic[] fallingGraphics = fallingRoot.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic g in fallingGraphics)
        {
            GameObject childGO = g.gameObject;

            if (childGO.GetComponent<Canvas>() != null || 
                childGO.name.ToLower().Contains("panel") || 
                childGO.name.ToLower().Contains("background") || 
                (aimingReticle != null && childGO.transform.IsChildOf(aimingReticle.transform))) 
            {
                continue;
            }

            if (!destroyedUIElements.Contains(childGO))
            {
                destroyedUIElements.Add(childGO);
                uiDestroyedCount++;
                destroyedAnything = true;
            }
        }

        return destroyedAnything;
    }

    private void TryCompleteWinCondition()
    {
        if (winConditionMet) return; 

        if (uiDestroyedCount >= uiTargetsToDestroyForWin)
        {
            winConditionMet = true;
            StartCoroutine(GloryFinisherSequence());
        }
    }

    private IEnumerator GloryFinisherSequence()
    {
        Time.timeScale = slowMoTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale; 

        yield return new WaitForSecondsRealtime(slowMoDuration);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        AkSoundEngine.StopAll();

        if (totalDominationPanel != null)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            totalDominationPanel.SetActive(true);
            
            RectTransform rt = totalDominationPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                StartCoroutine(AnimateDominationGraphic(rt));
            }
        }
    }

    private IEnumerator AnimateDominationGraphic(RectTransform rt)
    {
        Vector2 startPos = new Vector2(rt.anchoredPosition.x, -1200f); 
        Vector2 endPos = new Vector2(rt.anchoredPosition.x, 0f);              
        rt.anchoredPosition = startPos;

        float elapsed = 0f;
        float animDuration = 3.0f; 

        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime; 
            float t = Mathf.Clamp01(elapsed / animDuration);
            
            float easeOut = 1f - Mathf.Pow(1f - t, 3f); 
            
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, easeOut);
            yield return null;
        }
        
        rt.anchoredPosition = endPos;
    }

    private void SpawnSpark()
    {
        if (uiSparkEffectPrefab != null && mainCam != null && Mouse.current != null)
        {
            Vector3 sparkPos = mainCam.ScreenToWorldPoint(new Vector3(Mouse.current.position.ReadValue().x, Mouse.current.position.ReadValue().y, 1f));
            GameObject sparks = Instantiate(uiSparkEffectPrefab, sparkPos, Quaternion.identity);
            Destroy(sparks, 2f);
        }
    }
    
    private IEnumerator MakeUIFallAndDie(GameObject uiElement)
    {
        RectTransform rt = uiElement.GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector3 originalPos = rt.anchoredPosition;
        Quaternion originalRot = rt.rotation;
        Vector3 originalScale = rt.localScale;
        Transform originalParent = rt.parent;

        Canvas rootCanvas = uiElement.GetComponentInParent<Canvas>()?.rootCanvas;
        if (rootCanvas != null)
        {
            rt.SetParent(rootCanvas.transform, true);
            rt.SetAsLastSibling(); 
        }

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
            rt.SetParent(originalParent, true); 
            rt.anchoredPosition = originalPos;
            rt.rotation = originalRot;
            rt.localScale = originalScale;
        }
    }
}