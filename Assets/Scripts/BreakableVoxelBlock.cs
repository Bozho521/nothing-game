using Interfaces;
using UnityEngine;

public class BreakableVoxelBlock : MonoBehaviour, IDestructable
{

    [SerializeField] private GameObject mainBlock;

    [SerializeField] private GameObject voxelBlocks;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public float Health { get; set; }
    public int Armor { get; set; }
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
        mainBlock.SetActive(false);
        voxelBlocks.SetActive(true);
        
    }
}
