using Interfaces;
using UnityEngine;

public class BreakableVoxelBlock : MonoBehaviour, IDestructable
{
    [Header("Block Setup")]
    [SerializeField] private GameObject mainBlock;
    [SerializeField] private GameObject voxelBlocks;

    [Header("Destructable Settings")]
    [SerializeField] private float health = 50f;
    [SerializeField] private int armor = 0;

    private MeshRenderer _meshRenderer;
    private Transform parentTransform;
    private Transform brokenCubeParent;
    private Material mainMaterial;

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

    void Start()
    {
        parentTransform = transform.parent;
        brokenCubeParent = parentTransform.Find("1_BROKEN CUBE");
     
        _meshRenderer = mainBlock.GetComponent<MeshRenderer>();
        mainMaterial = _meshRenderer.materials[1];

        if (brokenCubeParent != null)
        {
            foreach(var child in brokenCubeParent.GetComponentsInChildren<MeshRenderer>())
            {
                var materials = child.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i].name == "Icon_Main")
                    {
                        materials[i] = mainMaterial;
                    }
                }
                child.sharedMaterials = materials;
            }
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
        mainBlock.SetActive(false);
        voxelBlocks.SetActive(true);
    }
}