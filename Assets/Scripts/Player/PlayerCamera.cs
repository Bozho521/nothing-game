using UnityEngine;
using System.Collections;

public class PlayerCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform cameraTransform;
    public float mouseSensitivity = 0.2f;
    public bool canLookUpAndDown = false;

    private float xRotation = 0f;

    private void Start()
    {
        if (cameraTransform == null) cameraTransform = GetComponentInChildren<Camera>().transform;
    }

    public void ProcessLook(Vector2 lookInput, Transform playerBody)
    {
        playerBody.Rotate(Vector3.up * lookInput.x * mouseSensitivity);

        if (canLookUpAndDown)
        {
            xRotation -= lookInput.y * mouseSensitivity;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    public void StartScreenShake(float duration, float magnitude)
    {
        StartCoroutine(ScreenShakeRoutine(duration, magnitude));
    }

    private IEnumerator ScreenShakeRoutine(float duration, float magnitude)
    {
        Vector3 originalPos = cameraTransform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            cameraTransform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        cameraTransform.localPosition = originalPos;
    }
}