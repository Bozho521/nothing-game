using UnityEngine;
using Interfaces;

public class PlayerInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public bool canInteract = true;

    private void Start()
    {
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
    }

    public void CheckAndInteract(IInteractor sourceInteractor)
    {
        if (!canInteract) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));
        
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
            {
                interactable.Interact(sourceInteractor);
            }
        }
    }
}