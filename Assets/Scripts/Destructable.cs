using Interfaces;
using UnityEngine;

public class Damageable : MonoBehaviour, IDestructable
{
    [Header("Destructable settings")]
    public float Health { get; set; } = 50;
    public int Armor { get; set; } = 0;
    
    public void TakeDamage(float damage)
    {
        float finalDamage = Mathf.Max(0, damage - Armor);
        
        Health -= finalDamage;
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