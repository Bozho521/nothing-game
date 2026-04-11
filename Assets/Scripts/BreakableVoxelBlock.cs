using Interfaces;
using UnityEngine;

public class BreakableVoxelBlock : MonoBehaviour, IDestructable
{

    [SerializeField] private GameObject mainBlock;

    [SerializeField] private GameObject voxelBlocks;

    private MeshRenderer _meshRenderer;

    private Transform parentTransform;

    private Transform brokenCubeParent;

    private Material mainMaterial;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        parentTransform = transform.parent;
        brokenCubeParent = parentTransform.Find("1_BROKEN CUBE");
     
        _meshRenderer = mainBlock.GetComponent<MeshRenderer>();
        mainMaterial = _meshRenderer.materials[1];

        foreach(var child in brokenCubeParent.GetComponentsInChildren<MeshRenderer>())
        {
            //Debug.Log("Found children with meshrenderer in broken cube parent");
            var materials = child.sharedMaterials;
            
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i].name == "Icon_Main")
                {
                    Debug.Log("ICON_MAIN FOUND");
                    materials[i] = mainMaterial;
                }
            }

            child.sharedMaterials = materials;
        }
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
