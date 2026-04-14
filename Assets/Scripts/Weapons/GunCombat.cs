using UnityEngine;
using Interfaces;

public class GunCombat : MonoBehaviour
{
    [Header("Impact Effects")]
    public GameObject sparkEffectPrefab;
    public GameObject bloodDecalPrefab;

    public void ProcessHitscan(RaycastHit hit, Vector3 aimDirection, int damage, int destructivePower)
    {
        if (TryHitDamageablePolyshape(hit, damage, destructivePower)) return;

        if (hit.collider.TryGetComponent<IDestructable>(out var destructible) && destructivePower >= destructible.Armor)
        {
            destructible.TakeDamage(damage);
            return;
        }

        Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            bool isHeadshot = hit.collider.CompareTag("Headshot");
            int finalDamage = isHeadshot ? 1000 : damage;

            enemy.TakeDamage(finalDamage, hit.point, hit.normal, isHeadshot);
            SpawnBloodDecal(hit, aimDirection);
        }
        else 
        {
            IDestructable destructableFallback = hit.collider.GetComponentInParent<IDestructable>();
            if (destructableFallback != null && destructivePower >= destructableFallback.Armor) 
            {
                destructableFallback.TakeDamage(damage);
            }
            SpawnSparks(hit);
        }
    }

    private void SpawnBloodDecal(RaycastHit hit, Vector3 direction)
    {
        if (bloodDecalPrefab == null) return;
        
        Vector3 passThroughStart = hit.point + (direction * 0.5f); 
        if (Physics.Raycast(passThroughStart, direction, out RaycastHit wallHit, 3f))
        {
            if (wallHit.collider.GetComponentInParent<Enemy>() == null)
            {
                GameObject decal = Instantiate(bloodDecalPrefab, wallHit.point + (wallHit.normal * 0.01f), Quaternion.LookRotation(wallHit.normal));
                decal.transform.Rotate(Vector3.forward, Random.Range(0f, 360f));
                Destroy(decal, 10f); 
            }
        }
    }

    private void SpawnSparks(RaycastHit hit)
    {
        if (sparkEffectPrefab != null)
        {
            GameObject sparks = Instantiate(sparkEffectPrefab, hit.point + (hit.normal * 0.05f), Quaternion.LookRotation(hit.normal));
            Destroy(sparks, 2f);
        }
    }

    private bool TryHitDamageablePolyshape(RaycastHit hit, float damage, int gunDestructivePower)
    {
        DamageablePolyshapes polyshape = hit.collider.GetComponent<DamageablePolyshapes>();
        if (polyshape == null) polyshape = hit.collider.GetComponentInParent<DamageablePolyshapes>();
        if (polyshape == null) return false;

        if (gunDestructivePower >= polyshape.Armor)
        {
            polyshape.TakeDamageAtHit(damage, hit);
        }
        return true;
    }
}