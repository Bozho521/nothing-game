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
        
        if (parentTransform != null)
        {
            brokenCubeParent = parentTransform.Find("1_BROKEN CUBE");
        }

        _meshRenderer = mainBlock.GetComponent<MeshRenderer>();
        if (_meshRenderer == null)
        {
            _meshRenderer = mainBlock.GetComponentInChildren<MeshRenderer>();
        }

        if (_meshRenderer != null)
        {
            if (_meshRenderer.materials.Length > 1)
            {
                mainMaterial = _meshRenderer.materials[1];
            }
            else if (_meshRenderer.materials.Length > 0)
            {
                Debug.LogWarning($"[BreakableVoxelBlock] {_meshRenderer.name} only has 1 material. Using index 0 instead.", this);
                mainMaterial = _meshRenderer.materials[0];
            }
        }
        else
        {
            Debug.LogError($"[BreakableVoxelBlock] No MeshRenderer found on {mainBlock.name} or its children!", this);
            return;
        }

        if (brokenCubeParent != null && mainMaterial != null)
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