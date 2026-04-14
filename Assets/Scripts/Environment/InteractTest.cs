using Interfaces;
using UnityEngine;

public class InteractTest : MonoBehaviour, IInteractable
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public string InteractPrompt { get; } = "Press E to interact";
    public void Interact(IInteractor interactor)
    {
        if (interactor is PlayerMovement player)
        {
            Debug.Log($"Interacting with {player.name}");
            player.OnInteractComplete(this);
        }
    }
}
