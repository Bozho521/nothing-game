using System.Collections;
using UnityEngine;
using Interfaces;
public class Door : MonoBehaviour, IInteractable, IDestructable
{
    [SerializeField]
    public Transform openTransform;
    [SerializeField]
    public float secondsToOpen = 1.5f;

    private bool Opened = false;
    
    public float Health { get; set; } = 50.0f;
    public int Armor { get; set; } = 0;
    
    public string InteractPrompt { get; } = "Press E to open";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    

    // Update is called once per frame
    void Update()
    {
        
    }

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
        Debug.Log("Door interacted");
        if (interactor is PlayerMovement player && !Opened)
        {
            Debug.Log("Opening Door");
            Opened = true;
            StartCoroutine(Open());
        }
        //throw new System.NotImplementedException();
    }
    
    
    public void TakeDamage(float damage)
    {
        Health -= damage;
        if (Health<= 0)
        {
            DestroyObject();
        }
    }

    public void DestroyObject()
    {
        Destroy(this);
    }
}
