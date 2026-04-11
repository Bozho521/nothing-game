using System.Collections;
using UnityEngine;
using Interfaces;

public class Door : MonoBehaviour, IInteractable, IDestructable
{
    [SerializeField]
    public Transform openTransform;
    [SerializeField]
    public float secondsToOpen = 1.5f;

    [Header("Destructable Settings")]
    [SerializeField] private float health = 50f;
    [SerializeField] private int armor = 0;

    private bool Opened = false;
    
    public float Health 
    { 
        get => health; 
        set => health = value; 
    }
    
    public int Armor 
    { 
        get => armor; 
        set => armor = value; 
    }
    
    public string InteractPrompt { get; } = "Press E to open";

    IEnumerator Open()
    {
        float elapsed_time = 0.0f;
        while (openTransform.position.y > transform.position.y)
        {
            elapsed_time += Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, openTransform.position, elapsed_time/secondsToOpen);
            yield return null;
        }
    }

    public void Interact(IInteractor interactor)
    {
        if (interactor is PlayerMovement player && !Opened)
        {
            Opened = true;
            StartCoroutine(Open());
        }
    }
    
    public void TakeDamage(float damage)
    {
        Health -= damage;
        if (Health <= 0)
        {
            DestroyObject();
        }
    }

    public void DestroyObject()
    {
        Destroy(gameObject);
    }
}