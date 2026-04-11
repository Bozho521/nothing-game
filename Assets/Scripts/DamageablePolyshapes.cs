using Interfaces;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

/// <summary>
/// Destructible component for ProBuilder-based meshes.
/// Instead of destroying the full GameObject on hit, this removes the impacted face.
/// </summary>
public class DamageablePolyshapes : MonoBehaviour, IDestructable
{
    [Header("Destructable settings")]
    [SerializeField] private float maxHealthPerFace = 50f;
    [SerializeField] private float health = 50f;
    [SerializeField] private int armor = 0;
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Polyshape settings")]
    [SerializeField] private ProBuilderMesh[] polyshapeMeshes;
    [SerializeField] private bool autoFindChildPolyshapes = true;
    [SerializeField] private bool destroyWhenNoFacesRemain = false;

    private readonly Dictionary<ProBuilderMesh, Dictionary<Face, float>> faceHealthByMesh = new Dictionary<ProBuilderMesh, Dictionary<Face, float>>();

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

    // Auto-populates target meshes to reduce manual setup in the Inspector.
    private void Awake()
    {
        if (maxHealthPerFace <= 0f)
        {
            maxHealthPerFace = 1f;
            LogDebug("maxHealthPerFace was <= 0, clamped to 1.");
        }

        health = Mathf.Clamp(health, 0f, maxHealthPerFace);
        LogDebug($"Awake complete. health={health}, maxHealthPerFace={maxHealthPerFace}, armor={armor}");

        if (autoFindChildPolyshapes && (polyshapeMeshes == null || polyshapeMeshes.Length == 0))
        {
            polyshapeMeshes = GetComponentsInChildren<ProBuilderMesh>();
            LogDebug($"Auto-found {polyshapeMeshes.Length} ProBuilder mesh(es).");
        }

        InitializeFaceHealthCache();
    }

    public void TakeDamage(float damage)
    {
        float finalDamage = Mathf.Max(0f, damage - armor);
        health -= finalDamage;
        LogDebug($"TakeDamage called. incoming={damage}, final={finalDamage}, healthNow={health}");

        if (health <= 0f)
        {
            LogDebug("Health reached zero in TakeDamage. Destroying object.");
            DestroyObject();
        }
    }

    public void TakeDamageAtHit(float damage, RaycastHit hit)
    {
        float finalDamage = Mathf.Max(0f, damage - armor);
        if (!TryResolveHitFace(hit, out ProBuilderMesh targetMesh, out Face targetFace, out int faceIndex, out MeshCollider targetCollider))
        {
            LogDebug($"TakeDamageAtHit failed to resolve a face. triangleIndex={hit.triangleIndex}");
            return;
        }

        float faceHealth = GetFaceHealth(targetMesh, targetFace);
        faceHealth = Mathf.Max(0f, faceHealth - finalDamage);
        SetFaceHealth(targetMesh, targetFace, faceHealth);

        // Keep interface health in sync with the most recently hit face for debugging/UI.
        health = faceHealth;
        LogDebug($"TakeDamageAtHit called. incoming={damage}, final={finalDamage}, mesh='{targetMesh.name}', faceIndex={faceIndex}, faceHealthNow={faceHealth}, triangleIndex={hit.triangleIndex}");

        if (faceHealth > 0f)
        {
            LogDebug("Hit face not depleted yet. No face removed.");
            return;
        }

        // Remove only the depleted face.
        bool deletedFace = TryDeleteFace(targetMesh, targetFace, faceIndex, targetCollider);
        if (deletedFace)
        {
            RemoveFaceHealth(targetMesh, targetFace);
            health = maxHealthPerFace;
            LogDebug($"Face removed successfully. Next face starts at {maxHealthPerFace} health.");
        }
        else
        {
            LogDebug("Face removal failed after health depletion.");
        }
    }

    public void DestroyObject()
    {
        LogDebug("DestroyObject called. Destroying GameObject.");
        Destroy(gameObject);
    }

    private bool TryResolveHitFace(RaycastHit hit, out ProBuilderMesh targetMesh, out Face targetFace, out int faceIndex, out MeshCollider meshCollider)
    {
        targetFace = null;
        faceIndex = -1;
        meshCollider = null;

        // Try direct hit object first, then parent in case collider is on a child.
        targetMesh = hit.collider.GetComponent<ProBuilderMesh>();
        if (targetMesh == null)
        {
            targetMesh = hit.collider.GetComponentInParent<ProBuilderMesh>();
        }

        if (targetMesh == null)
        {
            LogDebug("No ProBuilderMesh found on hit collider or parent.");
            return false;
        }

        if (polyshapeMeshes != null && polyshapeMeshes.Length > 0 && System.Array.IndexOf(polyshapeMeshes, targetMesh) < 0)
        {
            LogDebug($"Hit mesh '{targetMesh.name}' is not in configured polyshapeMeshes list.");
            return false;
        }

        meshCollider = hit.collider.GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = targetMesh.GetComponent<MeshCollider>();
        }

        if (meshCollider == null || meshCollider.sharedMesh == null || hit.triangleIndex < 0)
        {
            LogDebug("Missing valid MeshCollider/sharedMesh or invalid triangleIndex; cannot delete face.");
            return false;
        }

        // Convert Physics triangle index into a ProBuilder face index.
        faceIndex = FindFaceIndexFromTriangleOrdinal(targetMesh, hit.triangleIndex);
        if (faceIndex < 0)
        {
            // Fallback path for unusual meshes where face triangle ordering may differ.
            faceIndex = FindFaceIndexFromTriangleVertices(targetMesh, meshCollider.sharedMesh, hit.triangleIndex);
        }

        if (faceIndex < 0 || faceIndex >= targetMesh.faces.Count)
        {
            LogDebug($"Could not map triangleIndex {hit.triangleIndex} to a ProBuilder face.");
            return false;
        }

        targetFace = targetMesh.faces[faceIndex];
        return true;
    }

    private bool TryDeleteFace(ProBuilderMesh targetMesh, Face targetFace, int fallbackFaceIndex, MeshCollider meshCollider)
    {
        int faceIndex = -1;
        for (int i = 0; i < targetMesh.faces.Count; i++)
        {
            if (ReferenceEquals(targetMesh.faces[i], targetFace))
            {
                faceIndex = i;
                break;
            }
        }

        if (faceIndex < 0)
        {
            faceIndex = fallbackFaceIndex;
        }

        if (faceIndex < 0 || faceIndex >= targetMesh.faces.Count)
        {
            LogDebug("Could not find target face index at delete time.");
            return false;
        }

        // Equivalent to deleting a face in ProBuilder edit mode.
        targetMesh.DeleteFaces(new[] { faceIndex });
        targetMesh.ToMesh();
        targetMesh.Refresh();

        // Force collider to pick up the rebuilt render mesh.
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
            MeshFilter meshFilter = targetMesh.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshCollider.sharedMesh = meshFilter.sharedMesh;
            }
        }

        if (destroyWhenNoFacesRemain && targetMesh.faces.Count == 0)
        {
            LogDebug($"Mesh '{targetMesh.name}' has no faces left. Destroying mesh GameObject.");
            Destroy(targetMesh.gameObject);
        }

        LogDebug($"Deleted face index {faceIndex} on mesh '{targetMesh.name}'. Remaining faces: {targetMesh.faces.Count}");

        return true;
    }

    private void InitializeFaceHealthCache()
    {
        faceHealthByMesh.Clear();

        if (polyshapeMeshes == null)
        {
            return;
        }

        foreach (ProBuilderMesh mesh in polyshapeMeshes)
        {
            if (mesh == null)
            {
                continue;
            }

            Dictionary<Face, float> perFaceHealth = new Dictionary<Face, float>();
            for (int i = 0; i < mesh.faces.Count; i++)
            {
                perFaceHealth[mesh.faces[i]] = maxHealthPerFace;
            }

            faceHealthByMesh[mesh] = perFaceHealth;
        }

        LogDebug($"Initialized per-face health cache for {faceHealthByMesh.Count} mesh(es).");
    }

    private float GetFaceHealth(ProBuilderMesh mesh, Face face)
    {
        if (!faceHealthByMesh.TryGetValue(mesh, out Dictionary<Face, float> perFaceHealth))
        {
            perFaceHealth = new Dictionary<Face, float>();
            faceHealthByMesh[mesh] = perFaceHealth;
        }

        if (!perFaceHealth.TryGetValue(face, out float value))
        {
            value = maxHealthPerFace;
            perFaceHealth[face] = value;
        }

        return value;
    }

    private void SetFaceHealth(ProBuilderMesh mesh, Face face, float value)
    {
        if (!faceHealthByMesh.TryGetValue(mesh, out Dictionary<Face, float> perFaceHealth))
        {
            perFaceHealth = new Dictionary<Face, float>();
            faceHealthByMesh[mesh] = perFaceHealth;
        }

        perFaceHealth[face] = Mathf.Clamp(value, 0f, maxHealthPerFace);
    }

    private void RemoveFaceHealth(ProBuilderMesh mesh, Face face)
    {
        if (!faceHealthByMesh.TryGetValue(mesh, out Dictionary<Face, float> perFaceHealth))
        {
            return;
        }

        perFaceHealth.Remove(face);
    }

    /// <summary>
    /// Fast path: maps triangle index to face by counting triangles per face in face order.
    /// </summary>
    private static int FindFaceIndexFromTriangleOrdinal(ProBuilderMesh proBuilderMesh, int hitTriangleIndex)
    {
        if (hitTriangleIndex < 0)
        {
            return -1;
        }

        int runningTriangleCount = 0;

        for (int faceIndex = 0; faceIndex < proBuilderMesh.faces.Count; faceIndex++)
        {
            int triangleCountForFace = proBuilderMesh.faces[faceIndex].indexes.Count / 3;
            runningTriangleCount += triangleCountForFace;

            if (hitTriangleIndex < runningTriangleCount)
            {
                return faceIndex;
            }
        }

        return -1;
    }

    /// <summary>
    /// Fallback path: finds which face contains the hit triangle by comparing triangle vertex sets.
    /// </summary>
    private static int FindFaceIndexFromTriangleVertices(ProBuilderMesh proBuilderMesh, Mesh colliderMesh, int hitTriangleIndex)
    {
        int[] triangles = colliderMesh.triangles;
        int triStart = hitTriangleIndex * 3;

        if (triStart + 2 >= triangles.Length)
        {
            return -1;
        }

        int a = triangles[triStart];
        int b = triangles[triStart + 1];
        int c = triangles[triStart + 2];

        // A ProBuilder face can include multiple triangles, so scan each triangle triplet.
        for (int faceIndex = 0; faceIndex < proBuilderMesh.faces.Count; faceIndex++)
        {
            var face = proBuilderMesh.faces[faceIndex];
            var indices = face.indexes;

            for (int i = 0; i < indices.Count; i += 3)
            {
                if (TriangleMatches(indices[i], indices[i + 1], indices[i + 2], a, b, c))
                {
                    return faceIndex;
                }
            }
        }

        return -1;
    }

    private static bool TriangleMatches(int ta, int tb, int tc, int ha, int hb, int hc)
    {
        return ContainsVertex(ta, tb, tc, ha)
               && ContainsVertex(ta, tb, tc, hb)
               && ContainsVertex(ta, tb, tc, hc);
    }

    private static bool ContainsVertex(int a, int b, int c, int value)
    {
        return a == value || b == value || c == value;
    }

    private void LogDebug(string message)
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.Log($"[DamageablePolyshapes] {message}", this);
    }
}
