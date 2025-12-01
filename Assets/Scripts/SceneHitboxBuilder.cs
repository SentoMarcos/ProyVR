using UnityEngine;

/// <summary>
/// Regenera hitboxes (colliders) para todos los MeshFilter que cuelgan de este GameObject.
/// Útil para asegurar que la escena siempre tenga colisiones aun cuando los prefabs lleguen sin collider.
/// </summary>
[ExecuteAlways]
public class SceneHitboxBuilder : MonoBehaviour
{
    [Header("Alcance")]
    public bool includeInactive = true;
    public LayerMask layerFilter = ~0;

    [Header("Creación de colliders")]
    public bool replaceExistingCollider = false;
    public bool forceConvex = false;
    public PhysicsMaterial defaultMaterial;

    [Header("Autoejecución")]
    public bool rebuildOnEnable = true;
    public bool rebuildInEditor = false;
    public bool logSummary = true;

    private void OnEnable()
    {
        if (rebuildOnEnable)
        {
            RebuildColliders();
        }
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying && rebuildInEditor)
        {
            RebuildColliders();
        }
    }
#endif

    [ContextMenu("Rebuild Hitboxes Now")]
    public void RebuildColliders()
    {
        int created = 0;
        int skipped = 0;
        var meshFilters = GetComponentsInChildren<MeshFilter>(includeInactive);

        foreach (var meshFilter in meshFilters)
        {
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                skipped++;
                continue;
            }

            GameObject go = meshFilter.gameObject;
            if (((1 << go.layer) & layerFilter) == 0)
            {
                skipped++;
                continue;
            }

            Collider existing = go.GetComponent<Collider>();
            if (existing != null && !replaceExistingCollider)
            {
                skipped++;
                continue;
            }

            if (existing != null && replaceExistingCollider)
            {
                DestroyCollider(existing);
            }

            MeshCollider meshCollider = go.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = go.AddComponent<MeshCollider>();
            }

            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = forceConvex;
            if (defaultMaterial != null)
            {
                meshCollider.sharedMaterial = defaultMaterial;
            }

            created++;
        }

        if (logSummary)
        {
            Debug.Log($"[SceneHitboxBuilder] Colliders creados: {created}, omitidos: {skipped}");
        }
    }

    private void DestroyCollider(Collider collider)
    {
        if (collider == null)
            return;

        if (Application.isPlaying)
        {
            Destroy(collider);
        }
        else
        {
            DestroyImmediate(collider);
        }
    }
}
