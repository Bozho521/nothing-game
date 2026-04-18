using UnityEngine;
using UnityEngine.Pool;
using System.Collections;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance;

    [Header("Prefabs")]
    public GameObject sparkPrefab;
    public GameObject bloodDecalPrefab;

    private ObjectPool<GameObject> sparkPool;
    private ObjectPool<GameObject> bloodPool;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        sparkPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(sparkPrefab, transform),
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj),
            collectionCheck: false,
            defaultCapacity: 20,
            maxSize: 50
        );

        bloodPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(bloodDecalPrefab, transform),
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj),
            collectionCheck: false,
            defaultCapacity: 20,
            maxSize: 100
        );
    }

    public void SpawnSpark(Vector3 position, Vector3 normal)
    {
        GameObject spark = sparkPool.Get();
        spark.transform.position = position;
        spark.transform.rotation = Quaternion.LookRotation(normal);
        
        StartCoroutine(ReturnToPoolRoutine(spark, sparkPool, 2f));
    }

    public void SpawnBlood(Vector3 position, Vector3 normal)
    {
        GameObject blood = bloodPool.Get();
        blood.transform.position = position;
        blood.transform.rotation = Quaternion.LookRotation(normal);
        blood.transform.Rotate(Vector3.forward, Random.Range(0f, 360f));
        
        StartCoroutine(ReturnToPoolRoutine(blood, bloodPool, 10f));
    }

    private IEnumerator ReturnToPoolRoutine(GameObject obj, ObjectPool<GameObject> pool, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj.activeSelf) pool.Release(obj);
    }
}