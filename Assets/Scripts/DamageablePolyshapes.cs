using Interfaces;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

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
    [SerializeField] private bool useDoubleSidedColliders = true;

    [SerializeField] private AK.Wwise.Event wallDestroySound;

    private readonly Dictionary<ProBuilderMesh, Dictionary<Face, float>> faceHealthByMesh = new Dictionary<ProBuilderMesh, Dictionary<Face, float>>();
    private readonly Dictionary<MeshCollider, Mesh> runtimeColliderMeshes = new Dictionary<MeshCollider, Mesh>();
    private readonly Dictionary<MeshCollider, RuntimeMeshState> runtimeMeshStatesByCollider = new Dictionary<MeshCollider, RuntimeMeshState>();
    private readonly List<Mesh> runtimeRenderMeshes = new List<Mesh>();

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

    private void Awake()
    {
        if (maxHealthPerFace <= 0f) maxHealthPerFace = 1f;

        health = Mathf.Clamp(health, 0f, maxHealthPerFace);

        if (autoFindChildPolyshapes && (polyshapeMeshes == null || polyshapeMeshes.Length == 0))
        {
            polyshapeMeshes = GetComponentsInChildren<ProBuilderMesh>();
        }

        RefreshConfiguredMeshColliders();
        InitializeFaceHealthCache();
        InitializeRuntimeMeshFallback();
    }

    public void TakeDamage(float damage)
    {
        health -= damage; 

        if (health <= 0f)
        {
            DestroyObject();
        }
    }

    public void TakeDamageAtHit(float damage, RaycastHit hit)
    {
        if (!TryResolveHitFace(hit, out ProBuilderMesh targetMesh, out Face targetFace, out int faceIndex, out MeshCollider targetCollider))
        {
            // Build-safe fallback when ProBuilder runtime face data is unavailable.
            HandleRuntimeMeshDamage(hit, damage);
            return;
        }

        float faceHealth = GetFaceHealth(targetMesh, targetFace);
        
        faceHealth = Mathf.Max(0f, faceHealth - damage);
        SetFaceHealth(targetMesh, targetFace, faceHealth);

        health = faceHealth;

        if (faceHealth > 0f) return;

        bool deletedFace = TryDeleteFace(targetMesh, targetFace, faceIndex, targetCollider);
        if (deletedFace)
        {
            PostWallDestroySound(targetCollider.gameObject);
            RemoveFaceHealth(targetMesh, targetFace);
            health = maxHealthPerFace;
        }
    }

    public void DestroyObject()
    {
        LogDebug("DestroyObject called. Destroying GameObject.");
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        foreach (Mesh runtimeMesh in runtimeColliderMeshes.Values)
        {
            if (runtimeMesh != null)
            {
                Destroy(runtimeMesh);
            }
        }

        runtimeColliderMeshes.Clear();

        foreach (Mesh runtimeMesh in runtimeRenderMeshes)
        {
            if (runtimeMesh != null)
            {
                Destroy(runtimeMesh);
            }
        }

        runtimeRenderMeshes.Clear();
        runtimeMeshStatesByCollider.Clear();
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

        // Vertex matching is robust for reordered or doubled collider triangles.
        faceIndex = FindFaceIndexFromTriangleVertices(targetMesh, meshCollider.sharedMesh, hit.triangleIndex);
        if (faceIndex < 0)
        {
            // Fast fallback for normal collider meshes where face ordering is aligned.
            faceIndex = FindFaceIndexFromTriangleOrdinal(targetMesh, hit.triangleIndex);
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
                ApplyColliderMesh(targetMesh.name, meshCollider, meshFilter.sharedMesh);
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

    private void RefreshConfiguredMeshColliders()
    {
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

            MeshCollider meshCollider = mesh.GetComponent<MeshCollider>();
            MeshFilter meshFilter = mesh.GetComponent<MeshFilter>();

            if (meshCollider == null || meshFilter == null || meshFilter.sharedMesh == null)
            {
                continue;
            }

            meshCollider.sharedMesh = null;
            ApplyColliderMesh(mesh.name, meshCollider, meshFilter.sharedMesh);
        }
    }

    private void InitializeRuntimeMeshFallback()
    {
        runtimeMeshStatesByCollider.Clear();

        HashSet<MeshFilter> candidateFilters = new HashSet<MeshFilter>();

        if (polyshapeMeshes != null)
        {
            foreach (ProBuilderMesh mesh in polyshapeMeshes)
            {
                if (mesh == null)
                {
                    continue;
                }

                MeshFilter filter = mesh.GetComponent<MeshFilter>();
                if (filter != null)
                {
                    candidateFilters.Add(filter);
                }
            }
        }

        if (candidateFilters.Count == 0)
        {
            // Covers player builds where ProBuilder components can be stripped.
            foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>())
            {
                if (filter != null)
                {
                    candidateFilters.Add(filter);
                }
            }
        }

        foreach (MeshFilter meshFilter in candidateFilters)
        {
            if (meshFilter == null)
            {
                continue;
            }

            MeshCollider meshCollider = meshFilter.GetComponent<MeshCollider>();
            Mesh sourceMesh = meshFilter.sharedMesh;

            if (meshCollider == null || sourceMesh == null)
            {
                continue;
            }

            Mesh runtimeMesh = Instantiate(sourceMesh);
            runtimeMesh.name = sourceMesh.name + "_RuntimeDestructible";
            meshFilter.sharedMesh = runtimeMesh;
            runtimeRenderMeshes.Add(runtimeMesh);

            RuntimeMeshState runtimeState = new RuntimeMeshState(runtimeMesh, maxHealthPerFace);
            if (!runtimeState.Initialize())
            {
                continue;
            }

            runtimeMeshStatesByCollider[meshCollider] = runtimeState;

            meshCollider.sharedMesh = null;
            ApplyColliderMesh(meshFilter.name, meshCollider, runtimeMesh);
        }

        LogDebug($"Initialized runtime mesh fallback for {runtimeMeshStatesByCollider.Count} collider(s).");
    }

    private bool HandleRuntimeMeshDamage(RaycastHit hit, float damage)
    {
        if (!TryResolveRuntimeFace(hit, out RuntimeMeshState runtimeState, out int faceId, out MeshCollider meshCollider))
        {
            return false;
        }

        float faceHealth = runtimeState.GetFaceHealth(faceId);
        faceHealth = Mathf.Max(0f, faceHealth - damage);
        runtimeState.SetFaceHealth(faceId, faceHealth);

        health = faceHealth;

        if (faceHealth > 0f)
        {
            return true;
        }

        if (runtimeState.DeleteFace(faceId))
        {
            PostWallDestroySound(meshCollider.gameObject);
            health = maxHealthPerFace;

            meshCollider.sharedMesh = null;
            ApplyColliderMesh(meshCollider.name, meshCollider, runtimeState.RuntimeMesh);

            if (destroyWhenNoFacesRemain && runtimeState.RemainingFaceCount == 0)
            {
                Destroy(meshCollider.gameObject);
            }

            return true;
        }

        return false;
    }

    private bool TryResolveRuntimeFace(RaycastHit hit, out RuntimeMeshState runtimeState, out int faceId, out MeshCollider meshCollider)
    {
        runtimeState = null;
        faceId = -1;
        meshCollider = hit.collider != null ? hit.collider.GetComponent<MeshCollider>() : null;

        if (meshCollider == null && hit.collider != null)
        {
            meshCollider = hit.collider.GetComponentInParent<MeshCollider>();
        }

        if (meshCollider == null || hit.triangleIndex < 0)
        {
            return false;
        }

        if (!runtimeMeshStatesByCollider.TryGetValue(meshCollider, out runtimeState) || runtimeState == null)
        {
            return false;
        }

        int mappedTriangleIndex = hit.triangleIndex;
        if (useDoubleSidedColliders && runtimeState.TriangleCount > 0)
        {
            mappedTriangleIndex %= runtimeState.TriangleCount;
        }

        if (!runtimeState.TryGetFaceFromTriangle(mappedTriangleIndex, out faceId))
        {
            return false;
        }

        return true;
    }

    private void ApplyColliderMesh(string ownerName, MeshCollider meshCollider, Mesh sourceMesh)
    {
        if (meshCollider == null || sourceMesh == null)
        {
            return;
        }

        if (!useDoubleSidedColliders)
        {
            meshCollider.sharedMesh = sourceMesh;
            return;
        }

        if (runtimeColliderMeshes.TryGetValue(meshCollider, out Mesh previousRuntimeMesh) && previousRuntimeMesh != null)
        {
            Destroy(previousRuntimeMesh);
        }

        Mesh doubleSidedMesh = BuildDoubleSidedMesh(sourceMesh, ownerName + "_DoubleSidedCollider");
        runtimeColliderMeshes[meshCollider] = doubleSidedMesh;
        meshCollider.sharedMesh = doubleSidedMesh;
    }

    private void PostWallDestroySound(GameObject target)
    {
        if (wallDestroySound != null)
        {
            wallDestroySound.Post(target);
        }
    }

    private static Mesh BuildDoubleSidedMesh(Mesh sourceMesh, string runtimeName)
    {
        Mesh mesh = new Mesh
        {
            name = runtimeName,
            vertices = sourceMesh.vertices,
            uv = sourceMesh.uv,
            normals = sourceMesh.normals,
            tangents = sourceMesh.tangents,
            colors = sourceMesh.colors,
            bounds = sourceMesh.bounds,
            indexFormat = sourceMesh.indexFormat
        };

        mesh.subMeshCount = sourceMesh.subMeshCount;

        for (int subMeshIndex = 0; subMeshIndex < sourceMesh.subMeshCount; subMeshIndex++)
        {
            int[] frontTriangles = sourceMesh.GetTriangles(subMeshIndex);
            int[] doubleSidedTriangles = new int[frontTriangles.Length * 2];

            // Keep original winding for front faces.
            System.Array.Copy(frontTriangles, doubleSidedTriangles, frontTriangles.Length);

            // Append reversed winding so physics hits from the opposite side as well.
            for (int i = 0; i < frontTriangles.Length; i += 3)
            {
                int dst = frontTriangles.Length + i;
                doubleSidedTriangles[dst] = frontTriangles[i];
                doubleSidedTriangles[dst + 1] = frontTriangles[i + 2];
                doubleSidedTriangles[dst + 2] = frontTriangles[i + 1];
            }

            mesh.SetTriangles(doubleSidedTriangles, subMeshIndex, true);
        }

        mesh.RecalculateBounds();
        return mesh;
    }

    private void LogDebug(string message)
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.Log($"[DamageablePolyshapes] {message}", this);
    }

    private sealed class RuntimeMeshState
    {
        private readonly Mesh runtimeMesh;
        private readonly float maxFaceHealth;
        private readonly Dictionary<int, float> healthByTriangle = new Dictionary<int, float>();

        private const float QuadPairNormalDotThreshold = 0.9995f;
        private const float QuadPairAreaSimilarityThreshold = 0.55f;

        public RuntimeMeshState(Mesh runtimeMesh, float maxFaceHealth)
        {
            this.runtimeMesh = runtimeMesh;
            this.maxFaceHealth = maxFaceHealth;
        }

        public Mesh RuntimeMesh => runtimeMesh;
        public int TriangleCount { get; private set; }
        public int RemainingFaceCount => TriangleCount;

        public bool Initialize()
        {
            int[] triangles = runtimeMesh.triangles;
            if (triangles == null || triangles.Length < 3)
            {
                return false;
            }

            TriangleCount = triangles.Length / 3;
            return TriangleCount > 0;
        }

        public bool TryGetFaceFromTriangle(int triangleIndex, out int faceId)
        {
            if (triangleIndex >= 0 && triangleIndex < TriangleCount)
            {
                faceId = triangleIndex;
                return true;
            }

            faceId = -1;
            return false;
        }

        public float GetFaceHealth(int faceId)
        {
            if (!healthByTriangle.TryGetValue(faceId, out float value))
            {
                value = maxFaceHealth;
                healthByTriangle[faceId] = value;
            }

            return value;
        }

        public void SetFaceHealth(int faceId, float value)
        {
            healthByTriangle[faceId] = Mathf.Clamp(value, 0f, maxFaceHealth);
        }

        public bool DeleteFace(int faceId)
        {
            if (faceId < 0 || faceId >= TriangleCount)
            {
                return false;
            }

            int[] oldTriangles = runtimeMesh.triangles;
            int oldTriangleCount = oldTriangles.Length / 3;
            Vector3[] vertices = runtimeMesh.vertices;

            HashSet<int> trianglesToDelete = new HashSet<int> { faceId };
            if (TryFindBestQuadPartner(faceId, oldTriangles, vertices, oldTriangleCount, out int partnerTriangle))
            {
                trianglesToDelete.Add(partnerTriangle);
            }

            List<int> newTriangles = new List<int>(oldTriangles.Length);
            int[] newIndexByOldTriangle = new int[oldTriangleCount];

            int nextTriangleIndex = 0;
            for (int triangleIndex = 0; triangleIndex < oldTriangleCount; triangleIndex++)
            {
                if (trianglesToDelete.Contains(triangleIndex))
                {
                    newIndexByOldTriangle[triangleIndex] = -1;
                    continue;
                }

                int src = triangleIndex * 3;
                newTriangles.Add(oldTriangles[src]);
                newTriangles.Add(oldTriangles[src + 1]);
                newTriangles.Add(oldTriangles[src + 2]);
                newIndexByOldTriangle[triangleIndex] = nextTriangleIndex;
                nextTriangleIndex++;
            }

            runtimeMesh.triangles = newTriangles.ToArray();
            runtimeMesh.RecalculateBounds();
            runtimeMesh.RecalculateNormals();

            Dictionary<int, float> remappedHealth = new Dictionary<int, float>();
            foreach (KeyValuePair<int, float> pair in healthByTriangle)
            {
                int oldIndex = pair.Key;
                if (oldIndex < 0 || oldIndex >= oldTriangleCount)
                {
                    continue;
                }

                int newIndex = newIndexByOldTriangle[oldIndex];
                if (newIndex >= 0)
                {
                    remappedHealth[newIndex] = pair.Value;
                }
            }

            healthByTriangle.Clear();
            foreach (KeyValuePair<int, float> pair in remappedHealth)
            {
                healthByTriangle[pair.Key] = pair.Value;
            }

            TriangleCount = newTriangles.Count / 3;
            return true;
        }

        private static bool TryFindBestQuadPartner(int hitTriangle, int[] triangles, Vector3[] vertices, int triangleCount, out int bestPartner)
        {
            bestPartner = -1;

            if (!TryGetTriangleData(hitTriangle, triangles, vertices, out TriangleData source))
            {
                return false;
            }

            float bestScore = float.MinValue;

            for (int candidateIndex = 0; candidateIndex < triangleCount; candidateIndex++)
            {
                if (candidateIndex == hitTriangle)
                {
                    continue;
                }

                if (!TryGetTriangleData(candidateIndex, triangles, vertices, out TriangleData candidate))
                {
                    continue;
                }

                if (!TryGetSharedEdge(source, candidate, out int sharedA, out int sharedB))
                {
                    continue;
                }

                float normalDot = Mathf.Abs(Vector3.Dot(source.Normal, candidate.Normal));
                if (normalDot < QuadPairNormalDotThreshold)
                {
                    continue;
                }

                float areaSimilarity = ComputeAreaSimilarity(source.Area, candidate.Area);
                if (areaSimilarity < QuadPairAreaSimilarityThreshold)
                {
                    continue;
                }

                float sharedEdgeLengthSq = (vertices[sharedA] - vertices[sharedB]).sqrMagnitude;

                // Heuristic: diagonal shared edge in a split quad is usually the longest shared edge.
                float score = (sharedEdgeLengthSq * 10f) + (areaSimilarity * 3f) + normalDot;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPartner = candidateIndex;
                }
            }

            return bestPartner >= 0;
        }

        private static bool TryGetTriangleData(int triangleIndex, int[] triangles, Vector3[] vertices, out TriangleData data)
        {
            data = default;
            int triStart = triangleIndex * 3;

            if (triStart + 2 >= triangles.Length)
            {
                return false;
            }

            int a = triangles[triStart];
            int b = triangles[triStart + 1];
            int c = triangles[triStart + 2];

            if (a < 0 || b < 0 || c < 0 || a >= vertices.Length || b >= vertices.Length || c >= vertices.Length)
            {
                return false;
            }

            Vector3 v0 = vertices[a];
            Vector3 v1 = vertices[b];
            Vector3 v2 = vertices[c];
            Vector3 cross = Vector3.Cross(v1 - v0, v2 - v0);

            float area = cross.magnitude * 0.5f;
            Vector3 normal = cross.sqrMagnitude > 0f ? cross.normalized : Vector3.up;

            data = new TriangleData(a, b, c, normal, area);
            return true;
        }

        private static bool TryGetSharedEdge(TriangleData a, TriangleData b, out int sharedA, out int sharedB)
        {
            sharedA = -1;
            sharedB = -1;

            int count = 0;

            if (b.Contains(a.A))
            {
                sharedA = a.A;
                count++;
            }

            if (b.Contains(a.B))
            {
                if (count == 0)
                {
                    sharedA = a.B;
                }
                else
                {
                    sharedB = a.B;
                }

                count++;
            }

            if (b.Contains(a.C))
            {
                if (count == 0)
                {
                    sharedA = a.C;
                }
                else
                {
                    sharedB = a.C;
                }

                count++;
            }

            if (count == 2 && sharedB >= 0)
            {
                return true;
            }

            sharedA = -1;
            sharedB = -1;
            return false;
        }

        private static float ComputeAreaSimilarity(float a, float b)
        {
            float max = Mathf.Max(a, b);
            if (max <= Mathf.Epsilon)
            {
                return 1f;
            }

            return 1f - Mathf.Abs(a - b) / max;
        }

        private readonly struct TriangleData
        {
            public TriangleData(int a, int b, int c, Vector3 normal, float area)
            {
                A = a;
                B = b;
                C = c;
                Normal = normal;
                Area = area;
            }

            public int A { get; }
            public int B { get; }
            public int C { get; }
            public Vector3 Normal { get; }
            public float Area { get; }

            public bool Contains(int vertexIndex)
            {
                return A == vertexIndex || B == vertexIndex || C == vertexIndex;
            }
        }
    }
}
